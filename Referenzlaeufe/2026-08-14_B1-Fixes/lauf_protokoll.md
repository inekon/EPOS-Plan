# Referenzlauf-Protokoll

**Zeitpunkt:** 14.08.2026 21:53:14

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\dev\basis_b1fixes`

**Gesamtdauer:** 00:00:51  |  **Timeout je Projekt:** 300 s

**Warnungen:** 5  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:06 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:05 | 21 | OK |
| 1010 | Kurs EE | Tools: Wärmepumpe / Anlagen: WP | per --projekte vorgegeben | 00:03 | 18 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:07 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:04 | 20 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:04 | 19 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:04 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:07 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:05 | 26 | OK |

## Automatisch beantwortete Dialoge

Die Engine stellt im Grenzfall Rueckfragen per MessageBox. Der Dialogwaechter
drueckt den bejahenden Knopf, damit der Lauf denselben Weg geht wie bei einem
Anwender. Jede Rueckfrage ist hier dokumentiert:

- Projekt 1007: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
- Projekt 1008: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
- Projekt 1011: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
- Projekt 1023: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
- Projekt 1024: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a
Zielordner:    C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\dev\basis_b1fixes
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (88 MB)
DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Schema-Migration der Arbeitskopie ...
  C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
  Zeitpunkt: 14.08.2026 21:52:23
  Schemastand vorher: 6   (Zielstand 6)
  Bootstrap Schemamarker Tab_Applikation.SchemaVersion: OK
  Schritt 1  Spalten in Tab_Energieanlagen (Konzept 5.3): bereits erledigt
  Schritt 2  Spalten in Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen (Konzept 5.1/12): bereits erledigt
  Schritt 3  Ergebnistabelle Tab_ErgebnisPufferspeicher (Konzept 6.6): bereits erledigt
  Schritt 4  Beziehungen der Pufferspeicher (Konzept 5.3 / B0-6b): bereits erledigt
  Schritt 5  Datenmigration Quellen/Senken (Konzept 5.5): bereits erledigt
  Schritt 6  Feature-Flag Kaskade_Zweikanalig in Tab_Einstellungen (Konzept Kapitel 9): bereits erledigt
  Schemastand nachher: 6   (Zielstand 6)
Migration: ERFOLG (Zielstand 6).

Projektlandschaft wird gelesen ...
18 Projekte in Tab_Projekt.

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
      | [21:52:23] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:52:24] Simulation startet fuer Projekt 1007 ...
      | Speicher-Registry: Puffer 1007007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der Projektkopie - es gilt die Zuordnungszeile (50/30 °C).
      | --- PV TESTLAUF ---
      | Potenzielle Produktion: 8,75 kW
      | ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.
      | [21:52:28] Simulation beendet, Ergebnis-Kopf-ID 165.
      | [21:52:29] Projekt 1007: 29 CSV-Dateien, 90 Skalare.
      | [21:52:29] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1007: OK, 29 CSV-Dateien, 00:06
--- Projekt 1008 (Heinestr 15) ---
      | [21:52:30] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:52:30] Simulation startet fuer Projekt 1008 ...
      | [21:52:34] Simulation beendet, Ergebnis-Kopf-ID 166.
      | [21:52:35] Projekt 1008: 21 CSV-Dateien, 87 Skalare.
      | [21:52:35] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1008: OK, 21 CSV-Dateien, 00:05
--- Projekt 1010 (Kurs EE) ---
      | [21:52:35] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:52:35] Simulation startet fuer Projekt 1010 ...
      | [21:52:38] Simulation beendet, Ergebnis-Kopf-ID 167.
      | [21:52:39] Projekt 1010: 18 CSV-Dateien, 60 Skalare.
Projekt 1010: OK, 18 CSV-Dateien, 00:03
--- Projekt 1011 (test1) ---
      | [21:52:39] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:52:39] Simulation startet fuer Projekt 1011 ...
      | --- PV TESTLAUF ---
      | Potenzielle Produktion: 8,75 kW
      | ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.
      | [21:52:46] Simulation beendet, Ergebnis-Kopf-ID 168.
      | [21:52:47] Projekt 1011: 29 CSV-Dateien, 112 Skalare.
      | [21:52:47] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1011: OK, 29 CSV-Dateien, 00:07
--- Projekt 1017 (WP_PV-Speicher) ---
      | [21:52:47] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:52:47] Simulation startet fuer Projekt 1017 ...
      | [21:52:50] Simulation beendet, Ergebnis-Kopf-ID 169.
      | [21:52:51] Projekt 1017: 20 CSV-Dateien, 98 Skalare.
Projekt 1017: OK, 20 CSV-Dateien, 00:04
--- Projekt 1018 (BHKW Test München) ---
      | [21:52:51] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:52:51] Simulation startet fuer Projekt 1018 ...
      | Speicher-Registry: Puffer 1018007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der Projektkopie - es gilt die Zuordnungszeile (70/55 °C).
      | [21:52:54] Simulation beendet, Ergebnis-Kopf-ID 170.
      | [21:52:55] Projekt 1018: 19 CSV-Dateien, 103 Skalare.
Projekt 1018: OK, 19 CSV-Dateien, 00:04
--- Projekt 1021 (TestSpeichernUnter) ---
      | [21:52:55] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:52:56] Simulation startet fuer Projekt 1021 ...
      | [21:52:59] Simulation beendet, Ergebnis-Kopf-ID 171.
      | [21:53:00] Projekt 1021: 21 CSV-Dateien, 80 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:04
--- Projekt 1023 (Wöhler - Test1) ---
      | [21:53:00] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:53:01] Simulation startet fuer Projekt 1023 ...
      | [21:53:07] Simulation beendet, Ergebnis-Kopf-ID 172.
      | [21:53:08] Projekt 1023: 25 CSV-Dateien, 117 Skalare.
      | [21:53:08] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1023: OK, 25 CSV-Dateien, 00:07
--- Projekt 1024 (Wöhler - Test2) ---
      | [21:53:08] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\gallant-ishizaka-e7153a\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [21:53:08] Simulation startet fuer Projekt 1024 ...
      | Ladeprioritäten: 5 Feld(er) ohne Vorgabe auf 0 gesetzt (Konzept 3.4, Vorbelegung wie Migrationsregel R5).
      | [21:53:13] Simulation beendet, Ergebnis-Kopf-ID 173.
      | [21:53:14] Projekt 1024: 26 CSV-Dateien, 126 Skalare.
      | [21:53:14] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1024: OK, 26 CSV-Dateien, 00:05

Fertig. Gesamtdauer 00:00:51
Erfolgreich: 9 von 9
```
