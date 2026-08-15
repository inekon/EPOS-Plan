# Referenzlauf-Protokoll

**Zeitpunkt:** 15.08.2026 23:04:48

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:/Waermeplan/wt_k3/Referenzlaeufe/B3_8P_lauf1`

**Gesamtdauer:** 00:00:41  |  **Timeout je Projekt:** 300 s

**Warnungen:** 3  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:05 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:05 | 21 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:07 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:03 | 20 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:04 | 19 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:04 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:05 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:04 | 26 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: C:\Waermeplan\wt_k3
Zielordner:    C:/Waermeplan/wt_k3/Referenzlaeufe/B3_8P_lauf1
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (89 MB)
DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Schema-Migration der Arbeitskopie ...
  C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
  Zeitpunkt: 15.08.2026 23:04:06
  Schemastand vorher: 9   (Zielstand 9)
  Bootstrap Schemamarker Tab_Applikation.SchemaVersion: OK
  Schritt 1  Spalten in Tab_Energieanlagen (Konzept 5.3): bereits erledigt
  Schritt 2  Spalten in Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen (Konzept 5.1/12): bereits erledigt
  Schritt 3  Ergebnistabelle Tab_ErgebnisPufferspeicher (Konzept 6.6): bereits erledigt
  Schritt 4  Beziehungen der Pufferspeicher (Konzept 5.3 / B0-6b): bereits erledigt
  Schritt 5  Datenmigration Quellen/Senken (Konzept 5.5): bereits erledigt
  Schritt 6  Feature-Flag Kaskade_Zweikanalig in Tab_Einstellungen (Konzept Kapitel 9): bereits erledigt
  Schritt 7  Vorbelegung Extrapolation_erlaubt in Tab_Einstellungen (Konzept 13.4): bereits erledigt
  Schritt 8  Energieträger-Verweis ID_Carrier in Tab_Energieanlagen: bereits erledigt
  Schritt 9  Quellpuffer-Fremdschlüssel WQ_ID_Puffer (Etappe E0, Regel R7): bereits erledigt
  Schemastand nachher: 9   (Zielstand 9)
Migration: ERFOLG (Zielstand 9).

Projektlandschaft wird gelesen ...
16 Projekte in Tab_Projekt.

Gewaehlte Referenzprojekte (8):
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
      | [23:04:07] DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [23:04:07] Simulation startet fuer Projekt 1007 ...
      | Simulation Hinweis: Speicher-Registry: Puffer 1007007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der Projektkopie - es gilt die Zuordnungszeile (50/30 °C).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | --- PV TESTLAUF ---
      | Potenzielle Produktion: 8,75 kW
      | ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.
      | [23:04:12] Simulation beendet, Ergebnis-Kopf-ID 169.
      | [23:04:13] Projekt 1007: 29 CSV-Dateien, 90 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:05
--- Projekt 1008 (Heinestr 15) ---
      | [23:04:13] DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [23:04:13] Simulation startet fuer Projekt 1008 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Speicher-Registry: Puffer 1008008 (allSTOR exclusiv VPS 800/3-7) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 9,025 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | [23:04:17] Simulation beendet, Ergebnis-Kopf-ID 170.
      | [23:04:18] Projekt 1008: 21 CSV-Dateien, 87 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:05
--- Projekt 1011 (test1) ---
      | [23:04:18] DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [23:04:18] Simulation startet fuer Projekt 1011 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1011007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | --- PV TESTLAUF ---
      | Potenzielle Produktion: 8,75 kW
      | ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.
      | [23:04:24] Simulation beendet, Ergebnis-Kopf-ID 171.
      | [23:04:25] Projekt 1011: 29 CSV-Dateien, 112 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:07
--- Projekt 1017 (WP_PV-Speicher) ---
      | [23:04:25] DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [23:04:25] Simulation startet fuer Projekt 1017 ...
      | [23:04:28] Simulation beendet, Ergebnis-Kopf-ID 172.
      | [23:04:29] Projekt 1017: 20 CSV-Dateien, 98 Skalare.
Projekt 1017: OK, 20 CSV-Dateien, 00:03
--- Projekt 1018 (BHKW Test München) ---
      | [23:04:29] DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [23:04:29] Simulation startet fuer Projekt 1018 ...
      | Simulation Hinweis: Speicher-Registry: Puffer 1018007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der Projektkopie - es gilt die Zuordnungszeile (70/55 °C).
      | [23:04:32] Simulation beendet, Ergebnis-Kopf-ID 173.
      | [23:04:33] Projekt 1018: 19 CSV-Dateien, 103 Skalare.
Projekt 1018: OK, 19 CSV-Dateien, 00:04
--- Projekt 1021 (TestSpeichernUnter) ---
      | [23:04:33] DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [23:04:33] Simulation startet fuer Projekt 1021 ...
      | [23:04:37] Simulation beendet, Ergebnis-Kopf-ID 174.
      | [23:04:38] Projekt 1021: 21 CSV-Dateien, 80 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:04
--- Projekt 1023 (Wöhler - Test1) ---
      | [23:04:38] DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [23:04:38] Simulation startet fuer Projekt 1023 ...
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | [23:04:42] Simulation beendet, Ergebnis-Kopf-ID 175.
      | [23:04:43] Projekt 1023: 25 CSV-Dateien, 117 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:05
--- Projekt 1024 (Wöhler - Test2) ---
      | [23:04:43] DB-Pfad der App verifiziert: C:\Waermeplan\wt_k3\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [23:04:43] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Ladeprioritäten: 5 Feld(er) ohne Vorgabe auf 0 gesetzt (Konzept 3.4, Vorbelegung wie Migrationsregel R5).
      | [23:04:47] Simulation beendet, Ergebnis-Kopf-ID 176.
      | [23:04:48] Projekt 1024: 26 CSV-Dateien, 120 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:04

Fertig. Gesamtdauer 00:00:41
Erfolgreich: 8 von 8
```
