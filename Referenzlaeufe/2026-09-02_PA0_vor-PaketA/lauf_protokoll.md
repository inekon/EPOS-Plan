# Referenzlauf-Protokoll

**Zeitpunkt:** 02.09.2026 22:11:06

**Quelle (produktiv, nur gelesen):** `P:\pa0\Quelle\Kenndaten.sqlite`

**Arbeitskopie (beschrieben):** `P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite`

**Zielordner:** `C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\2026-09-02_PA0_vor-PaketA`

**Gesamtdauer:** 00:00:19  |  **Timeout je Projekt:** 900 s

**Warnungen:** 24  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 21 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:01 | 29 | OK |
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
Projektwurzel: P:\pa0\src
Zielordner:    C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\2026-09-02_PA0_vor-PaketA
Timeout je Projekt: 900 s

Quelle vorgegeben (--quelle): P:\pa0\Quelle\Kenndaten.sqlite
Arbeitskopie angelegt: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite (64 MB)
DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite

Schema-Migration der Arbeitskopie ...
  P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
  Zeitpunkt: 02.09.2026 22:10:47
  Schemastand vorher: 61   (Zielstand 61)
  Schemastand nachher: 61   (Zielstand 61)
Migration: ERFOLG (Zielstand 61).

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
      | [22:10:48] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:49] Simulation startet fuer Projekt 1007 ...
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [22:10:50] Simulation beendet, Ergebnis-Kopf-ID 208.
      | [22:10:50] Projekt 1007: 29 CSV-Dateien, 99 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:01
--- Projekt 1008 (Heinestr 15) ---
      | [22:10:50] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:51] Simulation startet fuer Projekt 1008 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Anlage „CS7800iLW 16": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS5800i AW 12 M + AW 5 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:10:51] Simulation beendet, Ergebnis-Kopf-ID 209.
      | [22:10:51] Projekt 1008: 21 CSV-Dateien, 101 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:01
--- Projekt 1011 (test1) ---
      | [22:10:51] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:52] Simulation startet fuer Projekt 1011 ...
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25 (2)" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T (2)': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [22:10:53] Simulation beendet, Ergebnis-Kopf-ID 210.
      | [22:10:53] Projekt 1011: 29 CSV-Dateien, 121 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:01
--- Projekt 1017 (WP_PV-Speicher) ---
      | [22:10:53] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:53] Simulation startet fuer Projekt 1017 ...
      | Simulation Warnung: Anlage „WPE-I 59 H 400 Premium": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der BHKW-Anlage „BHKW EW K 10 S [K] Heizol" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „eloBLOCK VE 28" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Zum Stichtag 01.01.2026 gab es noch keine Preisversion - es gilt die aelteste vorhandene.
      | [22:10:54] Simulation beendet, Ergebnis-Kopf-ID 211.
      | [22:10:54] Projekt 1017: 21 CSV-Dateien, 114 Skalare.
Projekt 1017: OK, 21 CSV-Dateien, 00:01
--- Projekt 1018 (BHKW Test München) ---
      | [22:10:54] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:55] Simulation startet fuer Projekt 1018 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054175 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „Vitocrossal 200 CM2 raumluftabh�ngig" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | [22:10:55] Simulation beendet, Ergebnis-Kopf-ID 212.
      | [22:10:55] Projekt 1018: 22 CSV-Dateien, 141 Skalare.
Projekt 1018: OK, 22 CSV-Dateien, 00:01
--- Projekt 1021 (TestSpeichernUnter) ---
      | [22:10:55] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:56] Simulation startet fuer Projekt 1021 ...
      | Simulation Warnung: Anlage „CS7800iLW 12": Der Speicher „allSTOR exclusiv VPS 800/3-7" ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen. Nach der Startfüllung liefe die Quelle leer.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:10:56] Simulation beendet, Ergebnis-Kopf-ID 213.
      | [22:10:56] Projekt 1021: 21 CSV-Dateien, 94 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:01
--- Projekt 1023 (Wöhler - Test1) ---
      | [22:10:56] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:57] Simulation startet fuer Projekt 1023 ...
      | Simulation Warnung: Anlage „CS7800iLW 12": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Anlage „CS6800iAW MB + AW 10 OR-T": Der Erzeuger-Vorlauf 45 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers „Vitocell 140-E 600 Ltr". Der Erzeuger kann den Speicher nie auf Solltemperatur laden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoVIT VKK 186/5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:10:57] Simulation beendet, Ergebnis-Kopf-ID 214.
      | [22:10:57] Projekt 1023: 25 CSV-Dateien, 136 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:01
--- Projekt 1024 (Wöhler - Test2) ---
      | [22:10:57] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:58] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der Speicherstufe. Er rechnet deshalb als Mitglied der Stundenschleife an seiner Kaskadenposition mit (Phase B) - ohne Puffer-Senke als reine Heizkreis-Stufe.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:10:58] Simulation beendet, Ergebnis-Kopf-ID 215.
      | [22:10:58] Projekt 1024: 26 CSV-Dateien, 157 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:01
--- Projekt 1026 (Beispiel WP WG 1) ---
      | [22:10:59] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:10:59] Simulation startet fuer Projekt 1026 ...
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [22:10:59] Simulation beendet, Ergebnis-Kopf-ID 216.
      | [22:10:59] Projekt 1026: 29 CSV-Dateien, 137 Skalare.
Projekt 1026: OK, 29 CSV-Dateien, 00:01
--- Projekt 1028 (Beispiel WP WG mit Erdwärme) ---
      | [22:11:00] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:11:00] Simulation startet fuer Projekt 1028 ...
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [22:11:01] Simulation beendet, Ergebnis-Kopf-ID 217.
      | [22:11:01] Projekt 1028: 29 CSV-Dateien, 137 Skalare.
Projekt 1028: OK, 29 CSV-Dateien, 00:01
--- Projekt 1029 (Beispiel WP WG 1 - Erdwärme) ---
      | [22:11:01] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:11:02] Simulation startet fuer Projekt 1029 ...
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [22:11:02] Simulation beendet, Ergebnis-Kopf-ID 218.
      | [22:11:02] Projekt 1029: 29 CSV-Dateien, 152 Skalare.
Projekt 1029: OK, 29 CSV-Dateien, 00:01
--- Projekt 1030 (Referenz BHKW-Kaskade (Regressionstest)) ---
      | [22:11:03] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:11:03] Simulation startet fuer Projekt 1030 ...
      | [22:11:04] Simulation beendet, Ergebnis-Kopf-ID 219.
      | [22:11:04] Projekt 1030: 22 CSV-Dateien, 150 Skalare.
Projekt 1030: OK, 22 CSV-Dateien, 00:01
--- Projekt 1039 (Wärmepumpe WG - BHKW) ---
      | [22:11:04] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:11:04] Simulation startet fuer Projekt 1039 ...
      | [22:11:05] Simulation beendet, Ergebnis-Kopf-ID 220.
      | [22:11:05] Projekt 1039: 18 CSV-Dateien, 60 Skalare.
Projekt 1039: OK, 18 CSV-Dateien, 00:01
--- Projekt 1043 (Booster-Kette mit Kombi-Speicher) ---
      | [22:11:05] DB-Pfad der App verifiziert: P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite
      | [22:11:05] Simulation startet fuer Projekt 1043 ...
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
      | [22:11:06] Simulation beendet, Ergebnis-Kopf-ID 221.
      | [22:11:06] Projekt 1043: 34 CSV-Dateien, 197 Skalare.
Projekt 1043: OK, 34 CSV-Dateien, 00:01

Fertig. Gesamtdauer 00:00:19
Erfolgreich: 14 von 14
```


---

# Einordnung: Referenzbasis PA0 — der Stand VOR Paket A (B1 + E1)

Diese Basis friert den Rechenkern **vor** der Umsetzung von Paket A des
`Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` ein (Befund **B1** Zeitbasis UTC/Ortszeit
und Stufe **E1** „Eine Wahrheit"). Paket A verschiebt die Solarreihe beim Lesen um
+1 h (MEZ) bzw. +2 h (MESZ) und ändert damit die Paarung Erzeugung ↔ Bedarf; die
**Jahressummen bleiben, Eigenverbrauchsquote, Speicherfahrweise und Solarthermie-Deckung
ändern sich**. Zusätzlich verschieben E1.1 (P_STC statt Fläche×η), E1.2 (T_NOCT) und
E1.4 (1-basierter Tagindex) die PV-Erträge.

**Betroffen sind alle Projekte mit PV oder Solarthermie.** Die Basis enthält sie deshalb
vollständig (Abschnitt „Projektmenge").

## Codestand

| | |
|---|---|
| **Commit (gebaut)** | `d46e200d282242770ee3e79cf5acc7cc0f5a2696`, Branch `ios_migration` |
| **HEAD bei Ablage** | `1f6142e` — der Zwischencommit der Sync-Automatik fügt **nur** `Konzept_Projektstammdaten_EPOS-Plan.md` hinzu (`git diff --stat d46e200d..HEAD` = 1 Datei, 156 Zeilen, kein Quelltext). Code-identisch. |
| **Bauweg** | `git archive HEAD` nach `P:\pa0\src` (außerhalb des Repos), dann `MSBuild.exe` aus `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\` mit `-restore -p:Configuration=Debug -p:Platform=x64`. **0 Fehler**, Bestandswarnungen (CS0108/CS0109/WFO1000/WFO0003). |
| **Warum außerhalb des Repos** | Im Arbeitsbaum lief `EPOS_Plan.exe` (PID 25272, Start 22:00:54) aus `WindowsFormsApplication1\bin\x64\Debug\net10.0-windows\` — der Ausgabeordner war gesperrt. Zugleich hält eine **fremde Session** uncommittete Änderungen; der Export von `HEAD` hält sie aus der Referenz heraus. |
| **Nicht im Build enthalten** (uncommittet, fremde Session) | `Controller/MenueCtrl.cs`, `MyResource/Resource.resx`, `MyResource/Resource.en-US.resx`, `Views/Klimadaten/Form_Klimadaten.Designer.cs`, `Views/Projekt/Form_ProjektDelete.{cs,Designer.cs}`, `Views/Projekt/Form_ProjektExportImport.cs`, `Views/Projekt/ProjektAuswahl.cs`, `Views/Wizard/{WizardParent,Wizard_Projekt}.cs`, `Allgemein/Reporting/KD6_Protokoll.md` — keine davon liegt im Rechenkern. |
| **Werkzeug** | `Referenzlauf.exe` aus demselben Export, `P:\pa0\src\Referenzlauf\bin\x64\Debug\net10.0-windows\` |

## Datenquelle

Produktiv ist seit dem 02.09.2026 **SQLite**. Die produktive Datei wurde **nie beschrieben**.

| | |
|---|---|
| **Produktive Datei** | `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`, Zeitstempel **02.09.2026 22:07:36**, 67 706 880 Byte |
| **Besonderheit** | Die Anwendung lief während der Entnahme (`-wal`/`-shm` vorhanden, WAL um 22:01 fortgeschrieben). Eine reine Dateikopie hätte nur den eingecheckpointeten Stand gezeigt. |
| **Entnahme** | Konsistenter Snapshot über die **SQLite-Backup-API** (`sqlite3.Connection.backup`, Quelle read-only geöffnet) nach `P:\pa0\Quelle\Kenndaten.sqlite`, 67 706 880 Byte, **MD5 `47bcefaca0f18d2180ba37786c6cb6b3`** |
| **Schemastand** | **61** — die Migration der Arbeitskopie war ein **No-op** (61 → 61, siehe Ablaufprotokoll) |
| **Arbeitskopie** | `P:\pa0\src\Referenzlaeufe\Arbeitskopie\Kenndaten.sqlite` (außerhalb des Repos, per Reflection umgebogen und je Kindprozess verifiziert) |

## Projektmenge — bewusster Basiswechsel gegenüber `2026-08-30_B3-Kaskade`

**Vierzehn Projekte, 355 CSV, 53,6 MB:**

```powershell
& $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1026,1028,1029,1030,1039,1043
```

Zwei Gründe für die Änderung:

**1. Der Anwender hat 1040, 1041, 1042 und 1044 gelöscht.** `Tab_Projekt` führt sie nicht
mehr (26 Projekte im Bestand). Die B3-Liste ist damit nicht mehr lauffähig. Die Ganglinien
von 1040 (zwei Puffer je Kanal), 1041 (Prozesswärme mit eigenem Puffer) und 1042
(Booster-Kette) bleiben nur in `2026-08-30_B3-Kaskade/` erhalten.

**2. Paket A verlangt vollständige PV-/Solarthermie-Abdeckung.** Der Bestand führt
folgende Projekte mit PV oder Solarthermie — **alle sind jetzt in der Basis**:

| Projekt | PV-Anlage (`ID_Type=3`) | Solarthermie-Anlage (`ID_Type=2`) | Gewerk „Photovoltaik" | Gewerk „Solarthermie" | war in B3? |
|---|:--:|:--:|:--:|:--:|:--:|
| 1007 Laurentiuskirche | 2 Zeilen | – | ja | ja | ja |
| 1011 test1 | 2 Zeilen | 2 Zeilen | ja | ja | ja |
| **1026** Beispiel WP WG 1 | 1 Zeile | 1 Zeile | ja | – | **nein → neu** |
| **1028** Beispiel WP WG mit Erdwärme | 1 Zeile | 1 Zeile | ja | – | **nein → neu** |
| **1029** Beispiel WP WG 1 - Erdwärme | 1 Zeile | 1 Zeile | ja | – | **nein → neu** |
| **1043** Booster-Kette mit Kombi-Speicher | – | – | ja | ja | **nein → neu** |

> Die `Tool_1..Tool_6`-Spalten in `Tab_Einstellungen` sind **keine festen Gewerkeslots**,
> sondern eine Namensliste — „Solarthermie" steht bei 1007/1011 in `Tool_2`, bei 1043 in
> `Tool_3`. Die Auswahl oben wertet deshalb den **Inhalt** aller sechs Spalten aus, nicht
> die Spaltennummer.

1043 ersetzt das gelöschte 1042 als Booster-Projekt und führt beide Gewerke aktiviert,
aber **ohne** PV-/Solarthermie-Anlagenzeile — genau der Randfall „Gewerk aktiviert, kein
Modul", den Paket A nicht brechen darf.

Die weiteren neun Projekte (1008, 1017, 1018, 1021, 1023, 1024, 1030, 1039 und die oben
genannten) sind der unveränderte Kern der B3-Linie: sie sichern ab, dass Paket A
**außerhalb** von PV/Solarthermie nichts bewegt.

## Laufzeiten

Gesamtdauer **00:00:19** für vierzehn Projekte, je Projekt rund **1 s** (siehe Projekttabelle).
Timeout je Projekt 900 s, nicht ausgeschöpft. **14 von 14 erfolgreich**, 0 Fehler,
0 automatisch beantwortete Dialoge.

## Selbstvergleich — die Basis ist reproduzierbar

Zweiter Lauf desselben Codes im Modus `projekt` auf **derselben** Arbeitskopie:

```
vergleich 2026-09-02_PA0_vor-PaketA P:\pa0\selbstvergleich
  → 14/14 PASS (3 882 476 Werte)
Byte-/MD5-Vergleich: 355 von 355 CSV identisch
pruefen 2026-09-02_PA0_vor-PaketA
  → GESAMT: plausibel (keine NaN/Inf, Rasterlängen 8760/35040 korrekt)
```

## Vergleich gegen die Vorbasis `2026-08-30_B3-Kaskade`

**Der Rechenkern ist unverändert.** Acht Projekte sind **byte-/MD5-gleich**, jede
Abweichung ist eine Datenänderung des Anwenders oder ein Projektzugang/-abgang:

| Projekt | Ergebnis | Ursache |
|---|---|---|
| 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024 | **PASS, alle 194 CSV byte-/MD5-gleich** | — |
| 1030 | FAIL (55 338 Abweichungen, 7 von 22 CSV) | **Datenänderung des Anwenders**: das zweite Kaskadenmodul ist wieder „Agenitor 306 (250 kw.el) Gas" statt „EC-POWER XRGI 9" (`aggregate.csv [BHKWModul[1].Modul]`). Kein Codeeffekt. |
| 1039 | FAIL (155 052 Abweichungen, 13 von 18 CSV, 7 CSV nur in B3) | **Datenänderung des Anwenders**: Kessel und Pufferspeicher entfernt (`Sim.bSimulationKessel` True→False, `Sim.PufferWP_vorhanden` True→False, alle `Puffer.*`- und `kessel_*`-Ausgaben entfallen), Bezeichner „Simulation Mehrgebäude" → „Simulation Wärmepumpe WG - BHKW". Kein Codeeffekt. |
| 1040, 1041, 1042 | nur in B3 | **vom Anwender gelöscht** |
| 1026, 1028, 1029, 1043 | nur in PA0 | **bewusst aufgenommen** (PV/Solarthermie, Booster) |

Ein PASS/FAIL-Gesamturteil B3 → PA0 wird deshalb **nicht** geführt; die Zuordnung oben tritt
an seine Stelle. Für die acht datenunveränderten Projekte gilt der Byte-Beweis.

---

# Kennzahlen, die Paket A verändern wird

Aus den `aggregate.csv` der Basis (Vektorsummen in **kWh**, Skalare wie im Ergebnisbaum,
MWh bei `PhotovoltaikModul[i].Stromproduktion`). **Diese Werte sind der Vorher-Stand.**

| Projekt | PV | ST | PV theor. Erzeugung [kWh] | PV genutzt (Direkt+Speicher) [kWh] | PV Überschuss [kWh] | Netzbezug (pv_reststrom) [kWh] | Strombedarf [kWh] | Eigenverbrauchsquote | Autarkiegrad | P_max [W/m²·…] |
|---|:--:|:--:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1007 | ja | ja | 6 053,14 | 5 202,64 | 850,50 | 50 834,71 | 56 037,34 | 85,95 % | 9,28 % | 968,27 |
| 1011 | ja | ja | 17 986,63 | 17 986,63 | 0,00 | 6 879 072,71 | 6 897 059,34 | 100,00 % | 0,26 % | 937,58 |
| 1026 | ja | – | 6 713,37 | 4 334,47 | 2 378,91 | 23 036,04 | 27 370,50 | 64,56 % | 15,84 % | 1 058,90 |
| 1028 | ja | – | 6 713,37 | 4 334,47 | 2 378,91 | 23 036,04 | 27 370,50 | 64,56 % | 15,84 % | 1 058,90 |
| 1029 | ja | – | 6 713,37 | 4 155,01 | 2 558,37 | 18 152,40 | 22 307,40 | 61,89 % | 18,63 % | 1 058,90 |
| 1043 | ja | ja | 0,00 | 0,00 | 0,00 | 27 596,53 | 27 596,53 | - | 0,00 % | 0,00 |

| Projekt | Solarth. Wärmebedarf [kWh] | Solarth. Jahresertrag [kWh] | Überschuss [kWh] | Deckungsgrad | Deckung Heizung [MWh] | Deckung Brauchw. [MWh] | Deckung Prozess [MWh] |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1007 | 56 898,32 | 0,00 | 0,00 | 0,00 % | 0,000 | 0,000 | 0,000 |
| 1011 | 5 105 253,77 | 643,58 | 0,00 | 0,01 % | 0,000 | 0,010 | 0,000 |
| 1043 | 0,00 | 0,00 | 0,00 | - | 0,000 | 0,000 | 0,000 |

Je-Modul-Erzeugung (PhotovoltaikModul[i].Stromproduktion, MWh):
- 1007: PhotovoltaikModul[0].Modul=Ablytek 6MN6A270; PhotovoltaikModul[0].Flaeche=32.54; PhotovoltaikModul[0].Anzahl=20; PhotovoltaikModul[0].Stromproduktion=6.05; PhotovoltaikModul[1].Modul=Ablytek 6MN6A275; PhotovoltaikModul[1].Flaeche=0; PhotovoltaikModul[1].Anzahl=0; PhotovoltaikModul[1].Stromproduktion=0
- 1011: PhotovoltaikModul[0].Modul=Jinkosolar JKM 260P-60; PhotovoltaikModul[0].Flaeche=49.1; PhotovoltaikModul[0].Anzahl=30; PhotovoltaikModul[0].Stromproduktion=8.99; PhotovoltaikModul[1].Modul=Jinkosolar JKM 260P-60; PhotovoltaikModul[1].Flaeche=49.1; PhotovoltaikModul[1].Anzahl=30; PhotovoltaikModul[1].Stromproduktion=8.99
- 1026: PhotovoltaikModul[0].Modul=Jinkosolar JKM 260P-60; PhotovoltaikModul[0].Flaeche=32.74; PhotovoltaikModul[0].Anzahl=20; PhotovoltaikModul[0].Stromproduktion=6.71
- 1028: PhotovoltaikModul[0].Modul=Jinkosolar JKM 260P-60; PhotovoltaikModul[0].Flaeche=32.74; PhotovoltaikModul[0].Anzahl=20; PhotovoltaikModul[0].Stromproduktion=6.71
- 1029: PhotovoltaikModul[0].Modul=Jinkosolar JKM 260P-60; PhotovoltaikModul[0].Flaeche=32.74; PhotovoltaikModul[0].Anzahl=20; PhotovoltaikModul[0].Stromproduktion=6.71

**Lesehilfe zu den Spalten**

- *PV theor. Erzeugung* = `Vektor.pv_produktion_theoretisch.Summe` — die Stundenreihe der
  PV-Erzeugung **vor** Bilanzierung. Das ist die Größe, die E1.1/E1.2/E1.4 direkt bewegen;
  B1 lässt ihre **Jahressumme unverändert** und verschiebt nur die Stundenzuordnung.
- *PV genutzt* = `Vektor.pv_produktion.Summe` — was gegen den Strombedarf und in den
  Stromspeicher gerechnet wurde (Direktverbrauch + Speicherladung).
- *PV Überschuss* = `Vektor.pv_ueberschuss.Summe` — Einspeisung.
- *Netzbezug* = `Vektor.pv_reststrom.Summe`; *Strombedarf* = `Vektor.pv_strombedarf.Summe`.
- *Eigenverbrauchsquote* = genutzt / theoretisch; *Autarkiegrad* = genutzt / Strombedarf.
  **Beide sind die Kennzahlen, die B1 verschieben wird** (mehrere Prozentpunkte laut Konzept
  Abschnitt 5).
- Solarthermie: *Jahresertrag* = `Vektor.solar_produktion.Summe`, *Deckungsgrad* =
  Ertrag / `Vektor.solar_waermebedarf.Summe`.

---

# Auffälligkeiten

## A1 — Katalogdaten: `T_NOCT`, `alpha_SC` und `beta_OC` tragen den Wert von `I_Kurzschluss`

In **allen sechs** von der Basis benutzten PV-Modulen steht in `Tab_PV` derselbe Zahlenwert
in `I_Kurzschluss`, `alpha_SC`, `beta_OC` **und** `T_NOCT`:

| Modul (ID) | Projekt | `Leistung` [W] | `Wirkungsgrad` [%] | `gamma_PMP` | `T_NOCT` | `I_Kurzschluss` | `Laenge`×`Breite` |
|---|---|---:|---:|---:|---:|---:|---|
| 1007005 Ablytek 6MN6A270 | 1007 | 270,64 | 16,63 | −0,4509 | **9,34** | 9,34 | 1,64 × 0,992 |
| 1007006 Ablytek 6MN6A275 | 1007 | 275,19 | 16,914 | −0,4509 | **9,42** | 9,42 | 1,64 × 0,992 |
| 1011008 / 1015244 / 1015245 / 1015246 Jinkosolar JKM 260P-60 | 1011/1026/1028/1029 | 260,00 | 15,885 | **0,0** | **9,014** | 9,014 | 1,65 × 0,992 |

**Warum das für E1.2 kritisch ist:** Der vorgeschlagene Rückfall lautet „45 °C, wenn
`T_NOCT ≤ 0` oder NULL". Ein Wert von **9,014** ist positiv und liefe damit in die
Formel — `(T_NOCT − 20)/800 = −0,0137`, also eine Zelltemperatur **unter** der
Außentemperatur bei Einstrahlung. Bei 1007 (`gamma_PMP` = −0,4509 %/K) hieße das einen
**Mehrertrag** statt der erwarteten ±0,5 %. Die Plausibilitätsschranke muss deshalb ein
**physikalisches Fenster** prüfen (etwa 20 °C ≤ `T_NOCT` ≤ 60 °C), nicht nur „> 0".

**Warum das für E1.1 kritisch ist:** `gamma_PMP = 0` beim Jinkosolar-Modul bedeutet, dass
1011, 1026, 1028 und 1029 heute **ohne jeden Temperaturgang** rechnen. Die von E1.5
vorgeschlagene γ-Plausibilität (`−1,0 ≤ γ ≤ 0`) lässt 0 durch — richtig, aber ein Hinweis
im Protokoll wäre angebracht.

**Konsistenzprobe zu E1.1** (`P_STC` gegen `Laenge·Breite·Wirkungsgrad·1000`):

| Modul | `Leistung` | `L·B·η·1000` | Abweichung |
|---|---:|---:|---:|
| Ablytek 6MN6A270 | 270,6400 W | 270,5501 W | **+0,033 %** |
| Ablytek 6MN6A275 | 275,1912 W | 275,1707 W | **+0,007 %** |
| Jinkosolar JKM 260P-60 | 260,0000 W | 260,0000 W | **0,000 %** |

→ **Der Katalog der Referenzmenge ist konsistent.** E1.1 muss auf dieser Basis
**bitgleich** bleiben; jede Abweichung nach dem Umbau ist ein Fehler, nicht die
angekündigte Katalogkorrektur.

## A2 — Randfälle, die Paket A nicht brechen darf

| Fall | Projekt | Beleg in der Basis |
|---|---|---|
| PV-Anlagenzeile mit **0 Modulen** (`PV_Leistung = 0`) | 1007, Anlage 10352 | `PhotovoltaikModul[1].Anzahl=0`, `.Flaeche=0`, `.Stromproduktion=0` |
| Gewerk **Solarthermie aktiviert, kein Modul** | 1007, 1043 | `pruefen`-Hinweis „solar_produktion.csv: Jahressumme 0 — Gewerk aktiviert, aber kein Modul zugeordnet" |
| Gewerk **Photovoltaik aktiviert, kein Modul** | 1043 | `pruefen`-Hinweis „pv_produktion.csv: Jahressumme 0" |
| Solarthermie mit **Ertrag praktisch null** trotz zweier Kollektorzeilen | 1011 | 643,58 kWh auf 5 105 253,77 kWh Bedarf = 0,01 % Deckung |
| PV **ohne Überschuss** (Bedarf ≫ Erzeugung) | 1011 | Eigenverbrauchsquote 100,00 %, Autarkiegrad 0,26 % — B1 kann hier **nichts** verschieben; das Projekt ist als B1-Nachweis untauglich |
| PV mit **hohem Überschussanteil** | 1026, 1028, 1029 | EVQ 61,9–64,6 % — **die aussagekräftigsten B1-Nachweisprojekte** der Basis |
| Klimaregion **ohne `Sonnenwinkel`** | 1012/1022 (Region Bocholt) | nicht in der Basis; Nebenbefund des Konzept-Nachtrags 1 |

## A3 — Warnungen des Laufs (24, alle Bestand)

Alle Warnungen stammen aus **1043**: Puffer ohne Temperaturpaar (ΔT-Rückfall 10 K, drei
Speicher), Mindest-Nutztemperatur Brauchwasser 55 °C über wirksamem Vorlauf 10 °C,
Anlage 14786 auf `PufferKombi` ohne Puffer (rechnet auf den Heizkreis), Zweitsenke von
14818 ohne Puffer, `WQ_Unbegrenzt` an „CS7800iLW 16" schaltet die Booster-Kopplung ab
(konstant 45 °C), Klassen-Set-Konflikt am Speicher „Stora B 1000-6 ER 1 B".
**1043 ist damit ein unvollständig konfiguriertes Projekt** — es taugt als Regressionsanker
(Struktur, Randfälle), nicht als fachliches Vorbild.

Hinweise (nicht gezählt): WP-Kennlinien-Obergrenze in 1024/1026/1028/1043, fehlender
Strom-Energieträger in 1026/1028/1029 (Rückfall 20 ct/kWh), fehlendes WP-Modul in 1039.

## A4 — Zeitraster der Solarreihe (die Ausgangslage von B1)

Alle vierzehn Projektklimaregionen führen exakt **8 760** Zeilen in `Tab_Solar`. Die Reihe
hat **keine Zeitspalte** — der einzige Zeitbezug ist `ORDER BY ID`, also die
PVGIS-Empfangsreihenfolge und damit **UTC**. Das ist die Voraussetzung, auf der die
geplante Verschiebung beim Lesen aufsetzt.

## A5 — Offene Punkte nach dieser Basis

1. **`Referenzlaeufe/LIESMICH.md` ist nicht mitgeändert.** Der Abschnitt „Aktuelle Basis"
   nennt weiter `2026-08-30_B3-Kaskade` und die dreizehn IDs, von denen drei nicht mehr
   existieren. Der Wechsel auf PA0 und die vierzehn IDs gehören dort nachgetragen —
   bewusst getrennt gehalten, weil dieser Commit nur den Basisordner umfassen soll.
2. **1030 und 1039 haben sich durch den Anwender geändert.** Wer PA0 gegen B3 stellt, muss
   das wissen; innerhalb von PA0 ist beides eingefroren.
3. Die Anwendung lief während der Entnahme. Die Basis bildet den Stand
   **02.09.2026 22:07:36** ab; spätere Eingaben des Anwenders sind nicht enthalten.
