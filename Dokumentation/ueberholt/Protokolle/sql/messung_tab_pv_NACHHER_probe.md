# Messung PV-Modulkatalog - NACHHER (Probe-Kopie nach Reparatur)

- Datenbank: V:\db\probe_reparatur.sqlite
- Gemessen: 02.09.2026 23:19:58 (read-only, URI mode=ro)
- Skript: V:\messung\messung_pv_katalog.py

## Plausibilitaetsfenster

| Feld | Einheit | Fenster |
|---|---|---|
| alpha_SC | A/K | 0 < x <= 0,05 (typisch 0,002...0,006) |
| beta_OC | V/K | -0,5 <= x < 0 (typisch -0,10...-0,15) |
| gamma_PMP | %/K | -1,0 <= x < 0 (typisch -0,30...-0,45) |
| T_NOCT | Grad C | 20 <= x <= 60 (typisch 42...48; 0/NULL = nicht vorhanden) |

Klassen: =I_Kurzschluss (Wert != 0 und exakt gleich I_Kurzschluss), NULL, 0, plausibel, unplausibel.

## Tab_PV_STAMM (6 Zeilen)

| ID | Bezeichner | I_Kurzschluss | alpha_SC | beta_OC | gamma_PMP | T_NOCT | Laenge | Breite |
|---|---|---|---|---|---|---|---|---|
| 5 | Ablytek 6MN6A270 | 9.34 | 0.00486614 [plausibel] | -0.121182 [plausibel] | -0.4509 [plausibel] | 47.4 [plausibel] | 1.64 | 0.992 |
| 6 | Ablytek 6MN6A275 | 9.42 | 0.00490782 [plausibel] | -0.122249 [plausibel] | -0.4509 [plausibel] | 47.4 [plausibel] | 1.64 | 0.992 |
| 7 | Ablytek 6MN6A290 | 9.67 | 0.00503807 [plausibel] | -0.125449 [plausibel] | -0.4509 [plausibel] | 47.4 [plausibel] | 1.64 | 0.992 |
| 8 | Jinkosolar JKM 260P-60 | 9.014 | 0.0034 [plausibel] | -0.1181 [plausibel] | -0.418 [plausibel] | 0.0 [0] | 1.65 | 0.992 |
| 9 | LG Electronics LG 320 N1K-A5 | 10.35 | 0.0031 [plausibel] | -0.1102 [plausibel] | -0.394 [plausibel] | 0.0 [0] | 1.686 | 1.016 |
| 21 | Philadelphia Solar PS-M144(HCBF)-530W | 13.6 | 0.00272 [plausibel] | -0.128904 [plausibel] | -0.385 [plausibel] | 45.3 [plausibel] | 0.0 | 0.0 |

## Tab_PV (6 Zeilen)

| ID | ID_Projekt | Bezeichner | I_Kurzschluss | alpha_SC | beta_OC | gamma_PMP | T_NOCT | Laenge | Breite |
|---|---|---|---|---|---|---|---|---|---|
| 1007005 | 1007 | Ablytek 6MN6A270 | 9.34 | 0.00486614 [plausibel] | -0.121182 [plausibel] | -0.4509 [plausibel] | 47.4 [plausibel] | 1.64 | 0.992 |
| 1007006 | 1007 | Ablytek 6MN6A275 | 9.42 | 0.00490782 [plausibel] | -0.122249 [plausibel] | -0.4509 [plausibel] | 47.4 [plausibel] | 1.64 | 0.992 |
| 1011008 | 1011 | Jinkosolar JKM 260P-60 | 9.014 | 0.0034 [plausibel] | -0.1181 [plausibel] | -0.418 [plausibel] | 0.0 [0] | 1.65 | 0.992 |
| 1015244 | 1026 | Jinkosolar JKM 260P-60 | 9.014 | 0.0034 [plausibel] | -0.1181 [plausibel] | -0.418 [plausibel] | 0.0 [0] | 1.65 | 0.992 |
| 1015245 | 1028 | Jinkosolar JKM 260P-60 | 9.014 | 0.0034 [plausibel] | -0.1181 [plausibel] | -0.418 [plausibel] | 0.0 [0] | 1.65 | 0.992 |
| 1015246 | 1029 | Jinkosolar JKM 260P-60 | 9.014 | 0.0034 [plausibel] | -0.1181 [plausibel] | -0.418 [plausibel] | 0.0 [0] | 1.65 | 0.992 |

## Zusammenfassung (Anzahl Zeilen je Klasse je Feld)

### Tab_PV_STAMM (6 Zeilen)

| Feld | plausibel | =I_Kurzschluss | 0 | NULL | unplausibel |
|---|---|---|---|---|---|
| alpha_SC | 6 | 0 | 0 | 0 | 0 |
| beta_OC | 6 | 0 | 0 | 0 | 0 |
| gamma_PMP | 6 | 0 | 0 | 0 | 0 |
| T_NOCT | 4 | 0 | 2 | 0 | 0 |

### Tab_PV (6 Zeilen)

| Feld | plausibel | =I_Kurzschluss | 0 | NULL | unplausibel |
|---|---|---|---|---|---|
| alpha_SC | 6 | 0 | 0 | 0 | 0 |
| beta_OC | 6 | 0 | 0 | 0 | 0 |
| gamma_PMP | 6 | 0 | 0 | 0 | 0 |
| T_NOCT | 2 | 0 | 4 | 0 | 0 |

### Beide Tabellen zusammen

| Feld | plausibel | =I_Kurzschluss | 0 | NULL | unplausibel |
|---|---|---|---|---|---|
| alpha_SC | 12 | 0 | 0 | 0 | 0 |
| beta_OC | 12 | 0 | 0 | 0 | 0 |
| gamma_PMP | 12 | 0 | 0 | 0 | 0 |
| T_NOCT | 6 | 0 | 6 | 0 | 0 |

## Reparaturbeduerftig

Zeilen mit mindestens einer Klasse ungleich plausibel. Bei T_NOCT gilt der Wert 0 als hinnehmbar (Koeffizient in der Quelle nicht vorhanden), wird aber ausgewiesen.

| Tabelle | ID | Bezeichner | betroffene Felder (Klasse) | Handlungsbedarf |
|---|---|---|---|---|
| Tab_PV_STAMM | 8 | Jinkosolar JKM 260P-60 | T_NOCT=0.0 [0] | nur T_NOCT=0 (hinnehmbar) |
| Tab_PV_STAMM | 9 | LG Electronics LG 320 N1K-A5 | T_NOCT=0.0 [0] | nur T_NOCT=0 (hinnehmbar) |
| Tab_PV | 1011008 | Jinkosolar JKM 260P-60 | T_NOCT=0.0 [0] | nur T_NOCT=0 (hinnehmbar) |
| Tab_PV | 1015244 | Jinkosolar JKM 260P-60 | T_NOCT=0.0 [0] | nur T_NOCT=0 (hinnehmbar) |
| Tab_PV | 1015245 | Jinkosolar JKM 260P-60 | T_NOCT=0.0 [0] | nur T_NOCT=0 (hinnehmbar) |
| Tab_PV | 1015246 | Jinkosolar JKM 260P-60 | T_NOCT=0.0 [0] | nur T_NOCT=0 (hinnehmbar) |

**6 reparaturbeduerftige Zeile(n)**, davon 6 nur wegen T_NOCT = 0 (hinnehmbar), also 0 mit echtem Handlungsbedarf.
