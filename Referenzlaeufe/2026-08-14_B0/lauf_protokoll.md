# Referenzlauf-Protokoll

**Zeitpunkt:** 14.08.2026 10:08:19

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B0`

**Gesamtdauer:** 00:00:35  |  **Timeout je Projekt:** 300 s

**Warnungen:** 5  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | Pflichtkategorie: Solarthermie | 00:05 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel / Puffer(WP) | abweichende Anlagenausstattung (Tools: Wärmepumpe / Anlagen: WP,Kessel / Puffer(WP)) | 00:04 | 21 | OK |
| 1010 | Kurs EE | Tools: Wärmepumpe / Anlagen: WP | Pflichtkategorie: nur Waermepumpe (Minimalfall) | 00:02 | 18 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel / Puffer(anderer Erzeuger) | abweichende Anlagenausstattung (Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel / Puffer(anderer Erzeuger)) | 00:06 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | Pflichtkategorie: Heizkessel | 00:03 | 20 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW / Puffer(anderer Erzeuger) | neue Erzeugerkombination (BHKW+Heizkessel) | 00:03 | 19 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | Pflichtkategorie: Waermepumpe mit Pufferspeicher | 00:04 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | Pflichtkategorie: BHKW | 00:04 | 22 | OK |

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
Projektwurzel: C:\Waermeplan\WP_Plan
Zielordner:    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B0
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (88 MB)
DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Projektlandschaft wird gelesen ...
18 Projekte in Tab_Projekt.
  uebergangen: 19 Wöhler WP (keine Erzeuger in Tab_Einstellungen)
  uebergangen: 1006 Stromspeicher mit Wärmepumpe (keine Erzeuger in Tab_Einstellungen)
  uebergangen: 1016 WP Stammdaten (keine Erzeuger in Tab_Einstellungen)
  uebergangen: 1020 Migrationstest (keine Erzeuger in Tab_Einstellungen)

Gewaehlte Referenzprojekte (8):
  - Projekt 1007 "Laurentiuskirche"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       Pflichtkategorie: Solarthermie
  - Projekt 1008 "Heinestr 15"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP,Kessel | Puffer(WP)
      Grund:       abweichende Anlagenausstattung (Tools: Wärmepumpe | Anlagen: WP,Kessel | Puffer(WP))
  - Projekt 1010 "Kurs EE"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP
      Grund:       Pflichtkategorie: nur Waermepumpe (Minimalfall)
  - Projekt 1011 "test1"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel | Puffer(anderer Erzeuger)
      Grund:       abweichende Anlagenausstattung (Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel | Puffer(anderer Erzeuger))
  - Projekt 1017 "WP_PV-Speicher"
      Ausstattung: Tools: BHKW, Heizkessel, Stromspeicher | Anlagen: WP,Batterie,Kessel,BHKW
      Grund:       Pflichtkategorie: Heizkessel
  - Projekt 1018 "BHKW Test München"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW | Puffer(anderer Erzeuger)
      Grund:       neue Erzeugerkombination (BHKW+Heizkessel)
  - Projekt 1023 "Wöhler - Test1"
      Ausstattung: Tools: Wärmepumpe, Heizkessel | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       Pflichtkategorie: Waermepumpe mit Pufferspeicher
  - Projekt 1024 "Wöhler - Test2"
      Ausstattung: Tools: Wärmepumpe, BHKW | Anlagen: WP,Kessel,BHKW,Puffer
      Grund:       Pflichtkategorie: BHKW

--- Projekt 1007 (Laurentiuskirche) ---
      | [10:07:44] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:07:44] Simulation startet fuer Projekt 1007 ...
      | --- PV TESTLAUF ---
      | Potenzielle Produktion: 8,75 kW
      | ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.
      | [10:07:48] Simulation beendet, Ergebnis-Kopf-ID 165.
      | [10:07:49] Projekt 1007: 29 CSV-Dateien, 89 Skalare.
      | [10:07:49] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1007: OK, 29 CSV-Dateien, 00:05
--- Projekt 1008 (Heinestr 15) ---
      | [10:07:49] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:07:49] Simulation startet fuer Projekt 1008 ...
      | Fehler beim Öffnen des RecordSets: Für mindestens einen erforderlichen Parameter wurde kein Wert angegeben.
      | Fehler beim Öffnen des RecordSets: Für mindestens einen erforderlichen Parameter wurde kein Wert angegeben.
      | [10:07:53] Simulation beendet, Ergebnis-Kopf-ID 166.
      | [10:07:53] Projekt 1008: 21 CSV-Dateien, 72 Skalare.
      | [10:07:53] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1008: OK, 21 CSV-Dateien, 00:04
--- Projekt 1010 (Kurs EE) ---
      | [10:07:53] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:07:54] Simulation startet fuer Projekt 1010 ...
      | [10:07:56] Simulation beendet, Ergebnis-Kopf-ID 167.
      | [10:07:56] Projekt 1010: 18 CSV-Dateien, 59 Skalare.
Projekt 1010: OK, 18 CSV-Dateien, 00:02
--- Projekt 1011 (test1) ---
      | [10:07:56] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:07:57] Simulation startet fuer Projekt 1011 ...
      | ProzessnameFehler beim Öffnen des RecordSets: Für mindestens einen erforderlichen Parameter wurde kein Wert angegeben.
      | --- PV TESTLAUF ---
      | Potenzielle Produktion: 8,75 kW
      | ERGEBNIS OK: Die Formel arbeitet physikalisch korrekt.
      | [10:08:02] Simulation beendet, Ergebnis-Kopf-ID 168.
      | [10:08:03] Projekt 1011: 29 CSV-Dateien, 111 Skalare.
      | [10:08:03] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1011: OK, 29 CSV-Dateien, 00:06
--- Projekt 1017 (WP_PV-Speicher) ---
      | [10:08:03] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:03] Simulation startet fuer Projekt 1017 ...
      | [10:08:06] Simulation beendet, Ergebnis-Kopf-ID 169.
      | [10:08:06] Projekt 1017: 20 CSV-Dateien, 97 Skalare.
Projekt 1017: OK, 20 CSV-Dateien, 00:03
--- Projekt 1018 (BHKW Test München) ---
      | [10:08:06] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:07] Simulation startet fuer Projekt 1018 ...
      | [10:08:09] Simulation beendet, Ergebnis-Kopf-ID 170.
      | [10:08:09] Projekt 1018: 19 CSV-Dateien, 102 Skalare.
Projekt 1018: OK, 19 CSV-Dateien, 00:03
--- Projekt 1023 (Wöhler - Test1) ---
      | [10:08:10] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:10] Simulation startet fuer Projekt 1023 ...
      | [10:08:14] Simulation beendet, Ergebnis-Kopf-ID 171.
      | [10:08:14] Projekt 1023: 25 CSV-Dateien, 102 Skalare.
      | [10:08:14] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1023: OK, 25 CSV-Dateien, 00:04
--- Projekt 1024 (Wöhler - Test2) ---
      | [10:08:15] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:15] Simulation startet fuer Projekt 1024 ...
      | [10:08:18] Simulation beendet, Ergebnis-Kopf-ID 172.
      | [10:08:19] Projekt 1024: 22 CSV-Dateien, 95 Skalare.
      | [10:08:19] WARNUNG: MessageBox der Anwendung automatisch geschlossen: Titel='Temperatur unter Minimum Kennlinie' Antwort='Ja' Text='Wärmepumpen Simulation: Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden? Bei nein wird Simulation abgebrochen!'
Projekt 1024: OK, 22 CSV-Dateien, 00:04

Fertig. Gesamtdauer 00:00:35
Erfolgreich: 8 von 8
```
