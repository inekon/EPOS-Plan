# Referenzlauf-Protokoll

**Zeitpunkt:** 14.08.2026 20:17:33

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8\Referenzlaeufe\2026-08-14_Fix_bhkw_Namen`

**Gesamtdauer:** 00:00:08  |  **Timeout je Projekt:** 300 s

**Warnungen:** 0  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:03 | 20 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:03 | 19 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8
Zielordner:    C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8\Referenzlaeufe\2026-08-14_Fix_bhkw_Namen
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (88 MB)
DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Schema-Migration der Arbeitskopie ...
  C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
  Zeitpunkt: 14.08.2026 20:17:25
  Schemastand vorher: 6   (Zielstand 5)
  Bootstrap Schemamarker Tab_Applikation.SchemaVersion: OK
  Schritt 1  Spalten in Tab_Energieanlagen (Konzept 5.3): bereits erledigt
  Schritt 2  Spalten in Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen (Konzept 5.1/12): bereits erledigt
  Schritt 3  Ergebnistabelle Tab_ErgebnisPufferspeicher (Konzept 6.6): bereits erledigt
  Schritt 4  Beziehungen der Pufferspeicher (Konzept 5.3 / B0-6b): bereits erledigt
  Schritt 5  Datenmigration Quellen/Senken (Konzept 5.5): bereits erledigt
  Schemastand nachher: 6   (Zielstand 5)
Migration: ERFOLG (Zielstand 5).

Projektlandschaft wird gelesen ...
18 Projekte in Tab_Projekt.

Gewaehlte Referenzprojekte (2):
  - Projekt 1017 "WP_PV-Speicher"
      Ausstattung: Tools: BHKW, Heizkessel, Stromspeicher | Anlagen: WP,Batterie,Kessel,BHKW
      Grund:       per --projekte vorgegeben
  - Projekt 1018 "BHKW Test München"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben

--- Projekt 1017 (WP_PV-Speicher) ---
      | [20:17:26] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [20:17:26] Simulation startet fuer Projekt 1017 ...
      | [20:17:28] Simulation beendet, Ergebnis-Kopf-ID 165.
      | [20:17:29] Projekt 1017: 20 CSV-Dateien, 98 Skalare.
Projekt 1017: OK, 20 CSV-Dateien, 00:03
--- Projekt 1018 (BHKW Test München) ---
      | [20:17:29] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\WindowsFormsApplication1\.claude\worktrees\interesting-joliot-f240d8\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [20:17:29] Simulation startet fuer Projekt 1018 ...
      | [20:17:32] Simulation beendet, Ergebnis-Kopf-ID 166.
      | [20:17:32] Projekt 1018: 19 CSV-Dateien, 103 Skalare.
Projekt 1018: OK, 19 CSV-Dateien, 00:03

Fertig. Gesamtdauer 00:00:08
Erfolgreich: 2 von 2
```
