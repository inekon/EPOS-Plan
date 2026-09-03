# Messung PV-Modulkatalog - VORHER (Stand 02.09.2026)

- Datenbank: V:\db\Kenndaten.sqlite
- Gemessen: 02.09.2026 23:13:13 (read-only, URI mode=ro)
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
| 5 | Ablytek 6MN6A270 | 9.34 | 0.0 [0] | 0.0 [0] | -0.4509 [plausibel] | 0.0 [0] | 1.64 | 0.992 |
| 6 | Ablytek 6MN6A275 | 9.42 | 9.42 [=I_Kurzschluss] | 9.42 [=I_Kurzschluss] | -0.4509 [plausibel] | 9.42 [=I_Kurzschluss] | 1.64 | 0.992 |
| 7 | Ablytek 6MN6A290 | 9.67 | 9.67 [=I_Kurzschluss] | 9.67 [=I_Kurzschluss] | -0.4509 [plausibel] | 9.67 [=I_Kurzschluss] | 1.64 | 0.992 |
| 8 | Jinkosolar JKM 260P-60 | 9.014 | 9.014 [=I_Kurzschluss] | 9.014 [=I_Kurzschluss] | 0.0 [0] | 9.014 [=I_Kurzschluss] | 1.65 | 0.992 |
| 9 | LG Electronics LG 320 N1K-A5 | 10.35 | NULL [NULL] | NULL [NULL] | -0.394 [plausibel] | NULL [NULL] | 1.686 | 1.016 |
| 21 | Philadelphia Solar PS-M144(HCBF)-530W | 13.6 | 0.00272 [plausibel] | -0.128904 [plausibel] | -0.385 [plausibel] | 45.3 [plausibel] | 0.0 | 0.0 |

## Tab_PV (6 Zeilen)

| ID | ID_Projekt | Bezeichner | I_Kurzschluss | alpha_SC | beta_OC | gamma_PMP | T_NOCT | Laenge | Breite |
|---|---|---|---|---|---|---|---|---|---|
| 1007005 | 1007 | Ablytek 6MN6A270 | 9.34 | 9.34 [=I_Kurzschluss] | 9.34 [=I_Kurzschluss] | -0.4509 [plausibel] | 9.34 [=I_Kurzschluss] | 1.64 | 0.992 |
| 1007006 | 1007 | Ablytek 6MN6A275 | 9.42 | 9.42 [=I_Kurzschluss] | 9.42 [=I_Kurzschluss] | -0.4509 [plausibel] | 9.42 [=I_Kurzschluss] | 1.64 | 0.992 |
| 1011008 | 1011 | Jinkosolar JKM 260P-60 | 9.014 | 9.014 [=I_Kurzschluss] | 9.014 [=I_Kurzschluss] | 0.0 [0] | 9.014 [=I_Kurzschluss] | 1.65 | 0.992 |
| 1015244 | 1026 | Jinkosolar JKM 260P-60 | 9.014 | 9.014 [=I_Kurzschluss] | 9.014 [=I_Kurzschluss] | 0.0 [0] | 9.014 [=I_Kurzschluss] | 1.65 | 0.992 |
| 1015245 | 1028 | Jinkosolar JKM 260P-60 | 9.014 | 9.014 [=I_Kurzschluss] | 9.014 [=I_Kurzschluss] | 0.0 [0] | 9.014 [=I_Kurzschluss] | 1.65 | 0.992 |
| 1015246 | 1029 | Jinkosolar JKM 260P-60 | 9.014 | 9.014 [=I_Kurzschluss] | 9.014 [=I_Kurzschluss] | 0.0 [0] | 9.014 [=I_Kurzschluss] | 1.65 | 0.992 |

## Zusammenfassung (Anzahl Zeilen je Klasse je Feld)

### Tab_PV_STAMM (6 Zeilen)

| Feld | plausibel | =I_Kurzschluss | 0 | NULL | unplausibel |
|---|---|---|---|---|---|
| alpha_SC | 1 | 3 | 1 | 1 | 0 |
| beta_OC | 1 | 3 | 1 | 1 | 0 |
| gamma_PMP | 5 | 0 | 1 | 0 | 0 |
| T_NOCT | 1 | 3 | 1 | 1 | 0 |

### Tab_PV (6 Zeilen)

| Feld | plausibel | =I_Kurzschluss | 0 | NULL | unplausibel |
|---|---|---|---|---|---|
| alpha_SC | 0 | 6 | 0 | 0 | 0 |
| beta_OC | 0 | 6 | 0 | 0 | 0 |
| gamma_PMP | 2 | 0 | 4 | 0 | 0 |
| T_NOCT | 0 | 6 | 0 | 0 | 0 |

### Beide Tabellen zusammen

| Feld | plausibel | =I_Kurzschluss | 0 | NULL | unplausibel |
|---|---|---|---|---|---|
| alpha_SC | 1 | 9 | 1 | 1 | 0 |
| beta_OC | 1 | 9 | 1 | 1 | 0 |
| gamma_PMP | 7 | 0 | 5 | 0 | 0 |
| T_NOCT | 1 | 9 | 1 | 1 | 0 |

## Reparaturbeduerftig

Zeilen mit mindestens einer Klasse ungleich plausibel. Bei T_NOCT gilt der Wert 0 als hinnehmbar (Koeffizient in der Quelle nicht vorhanden), wird aber ausgewiesen.

| Tabelle | ID | Bezeichner | betroffene Felder (Klasse) | Handlungsbedarf |
|---|---|---|---|---|
| Tab_PV_STAMM | 5 | Ablytek 6MN6A270 | alpha_SC=0.0 [0], beta_OC=0.0 [0], T_NOCT=0.0 [0] | ja |
| Tab_PV_STAMM | 6 | Ablytek 6MN6A275 | alpha_SC=9.42 [=I_Kurzschluss], beta_OC=9.42 [=I_Kurzschluss], T_NOCT=9.42 [=I_Kurzschluss] | ja |
| Tab_PV_STAMM | 7 | Ablytek 6MN6A290 | alpha_SC=9.67 [=I_Kurzschluss], beta_OC=9.67 [=I_Kurzschluss], T_NOCT=9.67 [=I_Kurzschluss] | ja |
| Tab_PV_STAMM | 8 | Jinkosolar JKM 260P-60 | alpha_SC=9.014 [=I_Kurzschluss], beta_OC=9.014 [=I_Kurzschluss], gamma_PMP=0.0 [0], T_NOCT=9.014 [=I_Kurzschluss] | ja |
| Tab_PV_STAMM | 9 | LG Electronics LG 320 N1K-A5 | alpha_SC=NULL [NULL], beta_OC=NULL [NULL], T_NOCT=NULL [NULL] | ja |
| Tab_PV | 1007005 | Ablytek 6MN6A270 | alpha_SC=9.34 [=I_Kurzschluss], beta_OC=9.34 [=I_Kurzschluss], T_NOCT=9.34 [=I_Kurzschluss] | ja |
| Tab_PV | 1007006 | Ablytek 6MN6A275 | alpha_SC=9.42 [=I_Kurzschluss], beta_OC=9.42 [=I_Kurzschluss], T_NOCT=9.42 [=I_Kurzschluss] | ja |
| Tab_PV | 1011008 | Jinkosolar JKM 260P-60 | alpha_SC=9.014 [=I_Kurzschluss], beta_OC=9.014 [=I_Kurzschluss], gamma_PMP=0.0 [0], T_NOCT=9.014 [=I_Kurzschluss] | ja |
| Tab_PV | 1015244 | Jinkosolar JKM 260P-60 | alpha_SC=9.014 [=I_Kurzschluss], beta_OC=9.014 [=I_Kurzschluss], gamma_PMP=0.0 [0], T_NOCT=9.014 [=I_Kurzschluss] | ja |
| Tab_PV | 1015245 | Jinkosolar JKM 260P-60 | alpha_SC=9.014 [=I_Kurzschluss], beta_OC=9.014 [=I_Kurzschluss], gamma_PMP=0.0 [0], T_NOCT=9.014 [=I_Kurzschluss] | ja |
| Tab_PV | 1015246 | Jinkosolar JKM 260P-60 | alpha_SC=9.014 [=I_Kurzschluss], beta_OC=9.014 [=I_Kurzschluss], gamma_PMP=0.0 [0], T_NOCT=9.014 [=I_Kurzschluss] | ja |

**11 reparaturbeduerftige Zeile(n)**, davon 0 nur wegen T_NOCT = 0 (hinnehmbar), also 11 mit echtem Handlungsbedarf.
