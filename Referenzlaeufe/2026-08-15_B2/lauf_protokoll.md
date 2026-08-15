# Referenzlauf-Protokoll

**Zeitpunkt:** 15.08.2026 12:37:41

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-15_B2`

**Gesamtdauer:** 00:00:46  |  **Timeout je Projekt:** 300 s

**Warnungen:** 0  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:05 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:05 | 21 | OK |
| 1010 | Kurs EE | Tools: Wärmepumpe / Anlagen: WP | per --projekte vorgegeben | 00:03 | 18 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:07 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:04 | 20 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:04 | 19 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:05 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:05 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:05 | 26 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: C:\Waermeplan\WP_Plan
Zielordner:    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-15_B2
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (88 MB)
DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Schema-Migration der Arbeitskopie ...
  C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
  Zeitpunkt: 15.08.2026 12:36:54
  Schemastand vorher: 7   (Zielstand 7)
  Bootstrap Schemamarker Tab_Applikation.SchemaVersion: OK
  Schritt 1  Spalten in Tab_Energieanlagen (Konzept 5.3): bereits erledigt
  Schritt 2  Spalten in Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen (Konzept 5.1/12): bereits erledigt
  Schritt 3  Ergebnistabelle Tab_ErgebnisPufferspeicher (Konzept 6.6): bereits erledigt
  Schritt 4  Beziehungen der Pufferspeicher (Konzept 5.3 / B0-6b): bereits erledigt
  Schritt 5  Datenmigration Quellen/Senken (Konzept 5.5): bereits erledigt
  Schritt 6  Feature-Flag Kaskade_Zweikanalig in Tab_Einstellungen (Konzept Kapitel 9): bereits erledigt
  Schritt 7  Vorbelegung Extrapolation_erlaubt in Tab_Einstellungen (Konzept 13.4): bereits erledigt
  Schemastand nachher: 7   (Zielstand 7)
Migration: ERFOLG (Zielstand 7).

Projektlandschaft wird gelesen ...
19 Projekte in Tab_Projekt.

Gewaehlte Referenzprojekte (9):
  - Projekt 1007 "Laurentiuskirche"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben
  - Projekt 1008 "Heinestr 15"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1010 "Kurs EE"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP
      Grund:       per --projekte vorgegeben
  - Projekt 1011 "test1"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben
  - Projekt 1017 "WP_PV-Speicher"
      Ausstattung: Tools: BHKW, Heizkessel, Stromspeicher | Anlagen: WP,Batterie,Kessel,BHKW
      Grund:       per --projekte vorgegeben
  - Projekt 1018 "BHKW Test München"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer | Puffer(anderer Erzeuger)
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

--- Projekt 1007 (Laurentiuskirche) ---
      | [12:36:55] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:36:55] Simulation startet fuer Projekt 1007 ...
      | Speicher-Registry: Puffer 1007007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der Projektkopie - es gilt die Zuordnungszeile (50/30 °C).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | --- PV TESTLAUF ---
      | Potenzielle Produktion: 8,75 kW
      | ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.
      | [12:37:00] Simulation beendet, Ergebnis-Kopf-ID 165.
      | [12:37:01] Projekt 1007: 29 CSV-Dateien, 90 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:05
--- Projekt 1008 (Heinestr 15) ---
      | [12:37:01] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:37:01] Simulation startet fuer Projekt 1008 ...
      | Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Speicher-Registry: Puffer 1008008 (allSTOR exclusiv VPS 800/3-7) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 9,025 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | [12:37:05] Simulation beendet, Ergebnis-Kopf-ID 166.
      | [12:37:06] Projekt 1008: 21 CSV-Dateien, 87 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:05
--- Projekt 1010 (Kurs EE) ---
      | [12:37:06] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:37:07] Simulation startet fuer Projekt 1010 ...
      | [12:37:09] Simulation beendet, Ergebnis-Kopf-ID 167.
      | [12:37:10] Projekt 1010: 18 CSV-Dateien, 60 Skalare.
Projekt 1010: OK, 18 CSV-Dateien, 00:03
--- Projekt 1011 (test1) ---
      | [12:37:10] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:37:10] Simulation startet fuer Projekt 1011 ...
      | Speicher-Registry: Puffer 1011007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | --- PV TESTLAUF ---
      | Potenzielle Produktion: 8,75 kW
      | ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.
      | [12:37:17] Simulation beendet, Ergebnis-Kopf-ID 168.
      | [12:37:17] Projekt 1011: 29 CSV-Dateien, 112 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:07
--- Projekt 1017 (WP_PV-Speicher) ---
      | [12:37:18] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:37:18] Simulation startet fuer Projekt 1017 ...
      | [12:37:21] Simulation beendet, Ergebnis-Kopf-ID 169.
      | [12:37:22] Projekt 1017: 20 CSV-Dateien, 98 Skalare.
Projekt 1017: OK, 20 CSV-Dateien, 00:04
--- Projekt 1018 (BHKW Test München) ---
      | [12:37:22] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:37:22] Simulation startet fuer Projekt 1018 ...
      | Speicher-Registry: Puffer 1018007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der Projektkopie - es gilt die Zuordnungszeile (70/55 °C).
      | [12:37:25] Simulation beendet, Ergebnis-Kopf-ID 170.
      | [12:37:26] Projekt 1018: 19 CSV-Dateien, 103 Skalare.
Projekt 1018: OK, 19 CSV-Dateien, 00:04
--- Projekt 1021 (TestSpeichernUnter) ---
      | [12:37:26] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:37:26] Simulation startet fuer Projekt 1021 ...
      | [12:37:30] Simulation beendet, Ergebnis-Kopf-ID 171.
      | [12:37:31] Projekt 1021: 21 CSV-Dateien, 80 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:05
--- Projekt 1023 (Wöhler - Test1) ---
      | [12:37:31] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:37:31] Simulation startet fuer Projekt 1023 ...
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | [12:37:35] Simulation beendet, Ergebnis-Kopf-ID 172.
      | [12:37:36] Projekt 1023: 25 CSV-Dateien, 117 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:05
--- Projekt 1024 (Wöhler - Test2) ---
      | [12:37:36] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [12:37:36] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Ladeprioritäten: 5 Feld(er) ohne Vorgabe auf 0 gesetzt (Konzept 3.4, Vorbelegung wie Migrationsregel R5).
      | [12:37:40] Simulation beendet, Ergebnis-Kopf-ID 173.
      | [12:37:41] Projekt 1024: 26 CSV-Dateien, 120 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:05

Fertig. Gesamtdauer 00:00:46
Erfolgreich: 9 von 9
```
