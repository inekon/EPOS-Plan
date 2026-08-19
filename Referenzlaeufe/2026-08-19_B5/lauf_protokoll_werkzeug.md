# Referenzlauf-Protokoll

**Zeitpunkt:** 19.08.2026 03:38:21

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-19_B5`

**Gesamtdauer:** 00:00:53  |  **Timeout je Projekt:** 300 s

**Warnungen:** 13  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:05 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:05 | 21 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:07 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:05 | 21 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:05 | 22 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:05 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:05 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:06 | 26 | OK |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:05 | 22 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: C:\Waermeplan\_b5
Zielordner:    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-19_B5
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (91 MB)
DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Schema-Migration der Arbeitskopie ...
  C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
  Zeitpunkt: 19.08.2026 03:37:27
  Schemastand vorher: 17   (Zielstand 18)
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
  Schritt 10  Ergebnisspalte Quellwaerme in Tab_ErgebnisHeizkessel (Etappe D4): bereits erledigt
  Schritt 11  Stromspeicher: Gerätespalten, Tab_StromspeicherVariante, Tab_ErgebnisStromspeicher, Ladeparameter (AP3): bereits erledigt
  Schritt 12  Preismodell: Aufschlagsspalten, Tab_Preisreihe(Daten), Tab_Kostenprofil, Vorbelegung (AP4): bereits erledigt
  Schritt 13  BHKW-Regulär: Spalte Schwelle_Reserve, Vorbelegung 10 %, Leistungsgrenze 30 %: bereits erledigt
  Schritt 14  Parallelverbund: Tabelle Z_AnlagePufferVerbund samt Index und Beziehungen: bereits erledigt
  Schritt 15  Kessel-Wartungseinheit: Spalte Wartungskosten_Einheit, Vorbelegung €/a: bereits erledigt
  Schritt 16  Anlagenzeilen-Eindeutigkeit: Indizes auf (ID_Projekt, ID_WP | ID_Kessel | ID_BHKW | ID_PUFFER): bereits erledigt
  Schritt 17  Doppelt belegte Anlagenzeilen in eigene Gerätekopien überführen: bereits erledigt
  Schritt 18  BHKW-Vollbenutzungsstunden: VbhElektrisch in Tab_ErgebnisBHKW, VbhThermisch und VbhElektrisch in Tab_ErgebnisBHKWModul (Etappe E2): OK
          - Tab_ErgebnisBHKW: 1 Spalten angelegt, 0 bereits vorhanden
          - Tab_ErgebnisBHKWModul: 2 Spalten angelegt, 0 bereits vorhanden
  Abschlussprüfung Anlagenzeilen-Eindeutigkeit
          - Eindeutigkeitsindex idx_Anlage_ID_WP (Wärmepumpe): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_Kessel (Heizkessel): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_BHKW (BHKW): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_PUFFER (Pufferspeicher): bereits vorhanden
  Schemastand nachher: 18   (Zielstand 18)
  Parallelverbund (Schritt 14): 0 Zeilen in Z_AnlagePufferVerbund - kein Projekt führt einen Pufferverbund, der Rechenweg bleibt unverändert.
  Dublettenauflösung (Schritt 17): 0 Anlagenzeilen auf eine eigene Gerätekopie überführt - es gab keine doppelt belegte Anlagenzeile.
  Anlagenzeilen-Eindeutigkeit (Schritt 16): 4 von 4 Eindeutigkeitsindizes aktiv, 0 doppelt belegte Anlagenzeilen - je Projekt und Gerät genau eine Zeile.
Migration: ERFOLG (Zielstand 18).

Projektlandschaft wird gelesen ...
19 Projekte in Tab_Projekt.

Gewaehlte Referenzprojekte (9):
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
  - Projekt 1030 "Referenz BHKW-Kaskade (Regressionstest)"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben

--- Projekt 1007 (Laurentiuskirche) ---
      | [03:37:28] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:37:29] Simulation startet fuer Projekt 1007 ...
      | Simulation Hinweis: Speicher-Registry: Puffer 1007007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der Projektkopie - es gilt die Zuordnungszeile (50/30 °C).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [03:37:33] Simulation beendet, Ergebnis-Kopf-ID 172.
      | [03:37:34] Projekt 1007: 29 CSV-Dateien, 90 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:05
--- Projekt 1008 (Heinestr 15) ---
      | [03:37:34] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:37:34] Simulation startet fuer Projekt 1008 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Speicher-Registry: Puffer 1008008 (allSTOR exclusiv VPS 800/3-7) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 9,025 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | [03:37:39] Simulation beendet, Ergebnis-Kopf-ID 173.
      | [03:37:40] Projekt 1008: 21 CSV-Dateien, 87 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:05
--- Projekt 1011 (test1) ---
      | [03:37:40] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:37:40] Simulation startet fuer Projekt 1011 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1011007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25 (2)" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [03:37:46] Simulation beendet, Ergebnis-Kopf-ID 174.
      | [03:37:47] Projekt 1011: 29 CSV-Dateien, 112 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:07
--- Projekt 1017 (WP_PV-Speicher) ---
      | [03:37:48] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:37:48] Simulation startet fuer Projekt 1017 ...
      | Simulation Hinweis: Das Projekt enthält ein BHKW - dieser Lauf rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).
      | Simulation Warnung: Energieträger-Zuordnung: Der BHKW-Anlage „BHKW EW K 10 S [K] Heizol" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „eloBLOCK VE 28" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Zum Stichtag 01.01.2026 gab es noch keine Preisversion - es gilt die aelteste vorhandene.
      | [03:37:51] Simulation beendet, Ergebnis-Kopf-ID 175.
      | [03:37:52] Projekt 1017: 21 CSV-Dateien, 103 Skalare.
Projekt 1017: OK, 21 CSV-Dateien, 00:05
--- Projekt 1018 (BHKW Test München) ---
      | [03:37:53] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:37:53] Simulation startet fuer Projekt 1018 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054168 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Speicher-Registry: Puffer 1054169 (Stora B 1000-6 ER 1 B (2)) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Parallelverbund: Speicher 1054168 (Stora B 1000-6 ER 1 B) rechnet als EIN gemeinsamer Vorrat aus 2 Behältern - nutzbare Kapazität Q_max 11,194 kWh (Leitspeicher) + 11,194 kWh (1 Mitglieder) = 22,388 kWh. Schwellen, Notreserve, Entladepriorität und Verwendung gelten aus dem Leitspeicher; es entsteht EINE Ergebniszeile unter seiner ID.
      | Simulation Hinweis: Das Projekt enthält ein BHKW - dieser Lauf rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „Vitocrossal 200 CM2 raumluftabh�ngig" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | [03:37:57] Simulation beendet, Ergebnis-Kopf-ID 176.
      | [03:37:58] Projekt 1018: 22 CSV-Dateien, 122 Skalare.
Projekt 1018: OK, 22 CSV-Dateien, 00:05
--- Projekt 1021 (TestSpeichernUnter) ---
      | [03:37:58] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:37:58] Simulation startet fuer Projekt 1021 ...
      | [03:38:02] Simulation beendet, Ergebnis-Kopf-ID 177.
      | [03:38:03] Projekt 1021: 21 CSV-Dateien, 80 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:05
--- Projekt 1023 (Wöhler - Test1) ---
      | [03:38:03] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:38:03] Simulation startet fuer Projekt 1023 ...
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoVIT VKK 186/5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | [03:38:08] Simulation beendet, Ergebnis-Kopf-ID 178.
      | [03:38:08] Projekt 1023: 25 CSV-Dateien, 118 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:05
--- Projekt 1024 (Wöhler - Test2) ---
      | [03:38:09] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:38:09] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Ladeprioritäten: 5 Feld(er) ohne Vorgabe auf 0 gesetzt (Konzept 3.4, Vorbelegung wie Migrationsregel R5).
      | Simulation Hinweis: Das Projekt enthält ein BHKW - dieser Lauf rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).
      | Simulation Hinweis: Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der Speicherstufe. Er rechnet deshalb als Mitglied der Stundenschleife an seiner Kaskadenposition mit (Phase B) - ohne Puffer-Senke als reine Heizkreis-Stufe.
      | [03:38:14] Simulation beendet, Ergebnis-Kopf-ID 179.
      | [03:38:15] Projekt 1024: 26 CSV-Dateien, 135 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:06
--- Projekt 1030 (Referenz BHKW-Kaskade (Regressionstest)) ---
      | [03:38:15] DB-Pfad der App verifiziert: C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [03:38:15] Simulation startet fuer Projekt 1030 ...
      | Simulation Hinweis: Das Projekt enthält ein BHKW - dieser Lauf rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).
      | [03:38:20] Simulation beendet, Ergebnis-Kopf-ID 180.
      | [03:38:21] Projekt 1030: 22 CSV-Dateien, 130 Skalare.
Projekt 1030: OK, 22 CSV-Dateien, 00:05

Fertig. Gesamtdauer 00:00:53
Erfolgreich: 9 von 9
```
