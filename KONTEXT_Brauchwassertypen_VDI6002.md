# Brauchwassertypen VDI 6002 — Arbeitskontext WP-Plan / EPOS-Plan

Stand 02.08.2026. Übergabepunkt für Folgearbeiten: was im Brauchwasserkatalog der
Kenndaten-Datenbank angelegt wurde, wie die Werte entstanden sind, womit sie sich reproduzieren
lassen und was noch zu entscheiden ist. Die Zahlen stehen vollständig hier, damit ohne
Datenbankzugriff damit gearbeitet werden kann.

> **Vor jeder Änderung am Katalog Abschnitt 6 lesen.** Das Migrationsskript löscht den neuen
> Katalog beim nächsten Lauf — das ist die wichtigste offene Baustelle.

**Kurzfassung:** In `Tab_Brauchwassertyp_STAMM` liegen 11 Wochen-Stundenprofile (ID 99–109),
in `Tab_Brauchwasser_STAMM` 13 Monatswertsätze (ID 97–109). Vier Typen stammen unmittelbar aus
VDI 6002 Blatt 1 und 2, sieben sind generische INEKON-Profile. Eingespielt und verifiziert in
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` (von der Anwendung genutzt) und
`C:\Waermeplan\WP_Plan\Kenndaten-ok.accdb`.

## 1. Datenmodell

Der Brauchwasserpfad besteht aus zwei Katalogtabellen in `Kenndaten.accdb`, die über den
Textschlüssel `Tab_Brauchwasser_STAMM.Typ = Tab_Brauchwassertyp_STAMM.Bezeichner` verbunden sind.

| Tabelle | Spalten | Inhalt |
|---|---|---|
| `Tab_Brauchwassertyp_STAMM` | `ID` (AutoWert), `Bezeichner`, `Beschreibung`, `1`…`168` (Double), `ReadOnly` (Bool) | Wochen-Stundenprofil 7 × 24, Index = Tag·24 + Stunde + 1, Tag 0 = Montag |
| `Tab_Brauchwasser_STAMM` | `ID`, `Bezeichner`, `Typ`, `Beschreibung`, `Monat_1`…`Monat_12` (Double), `ReadOnly` | Katalog der Brauchwasserprofile mit 12 Monatswerten |
| `Tab_Brauchwassertyp` / `Tab_Brauchwasser` | wie oben, zusätzlich `ID_Projekt` bzw. `ID_Brauchwasser`, ohne `ReadOnly` | Projektkopien, erzeugt von `BrauchwasserStammCtrl.CopyFromStamm()` |
| `Z_Projekt_Brauchwasser` | `ID`, `ID_Projekt`, `ID_Brauchwasser`, `Bezeichner`, `Summe` | Projektzuordnung mit überschriebener Jahressumme |

Rechenweg in `Allgemein/Simulation/SimulationWaermebedarf.Brauchwasserwaerme_berechnen()`:
die 12 Monatswerte und die 168 Wochenwerte gehen in `I_strom_wochetojahr` (native `bhkwplan.dll`).
Die Funktion kachelt das Wochenprofil über 8760 h und **normiert anschließend monatsweise auf die
Monatswerte**. Daraus folgt zweierlei: die Skala des Stundenprofils ist frei (hier bewusst % des
Wochenbedarfs, Summe 168 h = 100), und die Monatswerte bestimmen allein die Energiemenge.
Ist im Projekt eine abweichende `Summe` hinterlegt, werden die Monatswerte vorher linear skaliert.

## 2. Was angelegt wurde

11 Typen (Stundenprofile) und 13 Brauchwasserdatensätze (Monatswerte). Die vier VDI-Einträge sind
`ReadOnly = true` (Normdaten), alle übrigen bleiben editierbar.

| Typ (ID) | ReadOnly | Wochenfaktoren Mo–So [%] | Quelle Stundenprofil |
|---|---|---|---|
| Wohnen groß (VDI 6002) (99) | true | 13,8 / 13,8 / 13,8 / 13,8 / 13,8 / 15,0 / 16,0 | VDI 6002 Bl. 1 Anh. D, Bilder D3/D4/D5 + Wochenprofil D2 |
| Studentenwohnheim (VDI 6002-2) (100) | true | 14,0 / 16,5 / 17,0 / 16,5 / 14,0 / 10,5 / 11,5 | VDI 6002 Bl. 2 Tab. 3 (Mo / Di–Do / Fr / Sa / So) + Bild 3 |
| Seniorenheim (VDI 6002-2) (101) | true | 15,4 / 15,4 / 15,4 / 15,4 / 15,4 / 11,5 / 11,5 | VDI 6002 Bl. 2 Bild 8 (Mo–Fr), Bild 9 (Sa = So), Bild 7 |
| Krankenhaus (VDI 6002-2) (102) | true | 15,4 / 15,4 / 15,4 / 15,4 / 15,4 / 11,5 / 11,5 | VDI 6002 Bl. 2 Bild 10 (alle Tage gleich), Wochengang wie Bild 7 |
| MFH Wohnen (103) | false | 13,8 / 13,8 / 13,8 / 13,8 / 13,8 / 15,0 / 16,0 | generisch INEKON (TWW-Auslegung_V4.xlsx, Blatt Zapfprofil) |
| EFH Wohnen (104) | false | 13,5 / 13,5 / 13,5 / 13,5 / 13,5 / 15,5 / 17,0 | generisch INEKON |
| Hotel (105) | false | 13,5 / 13,5 / 13,5 / 13,5 / 14,0 / 16,0 / 16,0 | generisch INEKON |
| Pflegeheim (106) | false | 14,6 / 14,6 / 14,6 / 14,6 / 14,6 / 13,5 / 13,5 | generisch INEKON |
| Büro/Verwaltung (107) | false | 19,0 / 19,0 / 19,0 / 19,0 / 19,0 / 2,5 / 2,5 | generisch INEKON |
| Sportstätte/Duschen (108) | false | 13,0 / 13,0 / 13,0 / 13,0 / 13,0 / 19,0 / 16,0 | generisch INEKON |
| Schule (109) | false | 19,4 / 19,4 / 19,4 / 19,4 / 19,4 / 1,5 / 1,5 | generisch INEKON |

### Monatswerte [MWh je Bezugseinheit, 60/10 °C]

| Datensatz (ID) | Typ | Jan | Feb | Mär | Apr | Mai | Jun | Jul | Aug | Sep | Okt | Nov | Dez | Summe |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Wohnen groß (VDI 6002), 1 Person (97) | Wohnen groß (VDI 6002) | 0,0565 | 0,0517 | 0,0568 | 0,0499 | 0,0523 | 0,0462 | 0,0377 | 0,0453 | 0,0464 | 0,0456 | 0,0512 | 0,0547 | 0.5943 |
| Studentenwohnheim (VDI 6002-2), 1 Bewohner (98) | Studentenwohnheim (VDI 6002-2) | 0,0869 | 0,0754 | 0,0601 | 0,0711 | 0,0762 | 0,0718 | 0,0708 | 0,0381 | 0,0369 | 0,0695 | 0,0750 | 0,0535 | 0.7853 |
| Seniorenheim (VDI 6002-2), 1 Bett (99) | Seniorenheim (VDI 6002-2) | 0,0702 | 0,0639 | 0,0701 | 0,0647 | 0,0682 | 0,0641 | 0,0598 | 0,0617 | 0,0610 | 0,0650 | 0,0647 | 0,0507 | 0.7641 |
| Krankenhaus (VDI 6002-2), 1 Bett (100) | Krankenhaus (VDI 6002-2) | 0,0740 | 0,0675 | 0,0740 | 0,0683 | 0,0720 | 0,0677 | 0,0631 | 0,0651 | 0,0644 | 0,0686 | 0,0683 | 0,0535 | 0.8065 |
| MFH Wohnen, 1 Person (101) | MFH Wohnen | 0,0706 | 0,0647 | 0,0710 | 0,0623 | 0,0654 | 0,0577 | 0,0472 | 0,0566 | 0,0581 | 0,0570 | 0,0640 | 0,0683 | 0.7429 |
| EFH Wohnen, 1 Person (102) | EFH Wohnen | 0,0706 | 0,0647 | 0,0710 | 0,0623 | 0,0654 | 0,0577 | 0,0472 | 0,0566 | 0,0581 | 0,0570 | 0,0640 | 0,0683 | 0.7429 |
| Pflegeheim, 1 Bett (104) | Pflegeheim | 0,0975 | 0,0888 | 0,0974 | 0,0899 | 0,0947 | 0,0890 | 0,0830 | 0,0857 | 0,0847 | 0,0902 | 0,0899 | 0,0704 | 1.0612 |
| Hotel, 1 Zimmer (103) | Hotel | 0,0694 | 0,0755 | 0,0916 | 0,1117 | 0,1255 | 0,1269 | 0,1378 | 0,1410 | 0,1155 | 0,1119 | 0,0818 | 0,0849 | 1.2735 |
| Hotel Stadt/Geschäft, 1 Zimmer (108) | Hotel | 0,0842 | 0,0914 | 0,1214 | 0,1218 | 0,1267 | 0,1143 | 0,1032 | 0,0958 | 0,1123 | 0,1190 | 0,1023 | 0,0811 | 1.2735 |
| Hotel Ferien/Freizeit, 1 Zimmer (109) | Hotel | 0,0643 | 0,0668 | 0,0929 | 0,1072 | 0,1292 | 0,1352 | 0,1652 | 0,1665 | 0,1173 | 0,0984 | 0,0606 | 0,0699 | 1.2735 |
| Büro/Verwaltung, 1 Person (105) | Büro/Verwaltung | 0,0148 | 0,0150 | 0,0159 | 0,0156 | 0,0130 | 0,0143 | 0,0123 | 0,0120 | 0,0134 | 0,0134 | 0,0151 | 0,0150 | 0.1698 |
| Sportstätte/Duschen, 1 Nutzer (106) | Sportstätte/Duschen | 0,0682 | 0,0627 | 0,0693 | 0,0625 | 0,0556 | 0,0424 | 0,0241 | 0,0235 | 0,0513 | 0,0604 | 0,0608 | 0,0559 | 0.6367 |
| Schule, 1 Person (107) | Schule | 0,0102 | 0,0097 | 0,0102 | 0,0105 | 0,0097 | 0,0108 | 0,0028 | 0,0047 | 0,0105 | 0,0067 | 0,0117 | 0,0086 | 0.1061 |

Die Jahressumme ergibt sich aus dem spezifischen Bedarf: `MWh/a = l/(Einheit·d) · 365 · 50 K · 1,163 / 10⁶`.
Richtwerte: Wohnen groß 28, Studentenwohnheim 37, Seniorenheim 36, Krankenhaus 38, MFH/EFH 35,
Hotel 60 je Zimmer, Pflegeheim 50 je Bett, Büro 8, Sportstätte 30, Schule 5 l/(Einheit·d) bei 60 °C.
Im Projekt wird die Jahressumme über `Z_Projekt_Brauchwasser.Summe` auf die Objektgröße skaliert.

## 3. Herkunft der Jahresgänge

Normwerte (unverändert lassen, sie sind `ReadOnly`): Wohnen groß nach **VDI 6002 Bl. 1 Bild D1**
(111,8 / 113,4 / 112,2 / 102,0 / 103,6 / 94,4 / 74,7 / 89,7 / 95,0 / 90,3 / 104,8 / 108,2 % des
Jahresmittels, tagesbezogen), Studentenwohnheim nach **Bl. 2 Bild 2** (130 / 125 / 90 / 110 / 114 /
111 / 106 / 57 / 57 / 104 / 116 / 80 %), Seniorenheim nach **Bl. 2 Bild 6** (108 / 109 / 108 / 103 /
105 / 102 / 92 / 95 / 97 / 100 / 103 / 78 %). Krankenhaus übernimmt laut Abschnitt 6.3.3 das
Seniorenheimprofil, Pflegeheim analog, MFH/EFH folgen Bild D1.

Für Hotel, Büro, Sportstätte und Schule gibt die VDI **kein** Jahresprofil vor. Dort gilt seit
dem 02.08.2026 `f(m) = Nutzung(m) · k_Kaltwasser(m)`, normiert auf die unveränderte Jahressumme:

- **Kaltwasser:** Kaltwassertemperatur = Erdreichtemperatur in Verlegetiefe. Aus den TRY-2010-Daten
  (`Waermespeicher-Tool/vendor/try_weather`, Mittel der 15 Klimaregionen) per Fourier-Analyse:
  Jahresmittel Luft 8,51 °C, Amplitude 8,76 K, Maximum Tag 200. Gedämpfte Wärmewelle mit
  z = 2,0 m und α = 0,8·10⁻⁶ m²/s → Dämpfungstiefe 2,83 m, Dämpfung 0,494, Phasenverzug 41 d.
  Auf Normbezug 10 °C gesetzt: 6,8 / 5,8 / 5,9 / 7,0 / 9,0 / 11,2 / 13,1 / 14,2 / 14,1 / 12,9 /
  11,0 / 8,7 °C. Faktor Wärme je Liter gegenüber 60/10 °C: 1,063 / 1,083 / 1,082 / 1,060 / 1,021 /
  0,976 / 0,938 / 0,917 / 0,918 / 0,942 / 0,981 / 1,025.
- **Schule:** Schultage je Monat im Mittel über alle 16 Bundesländer aus den KMK-Ferienterminen
  2026/27 (193 Schultage/a): 17,6 / 16,5 / 17,4 / 18,2 / 17,4 / 20,2 / 5,6 / 9,5 / 20,9 / 13,1 /
  21,7 / 15,4. Oster- und Pfingstferien sind über die Monatspaare März/April und Mai/Juni
  geglättet, weil ihre Lage jährlich wandert.
- **Büro:** Arbeitstage ohne bundesweite Feiertage (20 / 20 / 21 / 22 / 19 / 22 / 22 / 22 / 22 /
  21 / 22 / 23 = 256) abzüglich Urlaub (Annahme 30 d/a: 1,5 / 1,5 / 1,5 / 2,5 / 2,0 / 2,5 / 4,5 /
  4,5 / 2,5 / 2,0 / 1,5 / 3,5).
- **Sportstätte:** Kalendertage × Saisonfaktor Hallen-/Vereinsbetrieb (Annahme: 1,00 / 1,00 / 1,00 /
  0,95 / 0,85 / 0,70 / 0,40 / 0,40 / 0,90 / 1,00 / 1,00 / 0,85).
- **Hotel:** drei Varianten auf demselben Stundenprofil. „Hotel, 1 Zimmer" = Übernachtungen aller
  Beherbergungsbetriebe 2025 (Destatis: 25,2 / 26,9 / 32,7 / 40,7 / 47,5 / 50,2 / 56,7 / 59,4 /
  48,6 / 45,9 / 32,2 / 32,0 Mio.). „Hotel Stadt/Geschäft" = Tagungs-/Messebetrieb (Faktoren je Tag
  0,72 / 0,85 / 1,02 / 1,08 / 1,13 / 1,10 / 1,00 / 0,95 / 1,15 / 1,15 / 0,98 / 0,72).
  „Hotel Ferien/Freizeit" = Sommermaximum (0,55 / 0,62 / 0,78 / 0,95 / 1,15 / 1,30 / 1,60 / 1,65 /
  1,20 / 0,95 / 0,58 / 0,62).

Die VDI-Typen bekommen **keinen** Kaltwasserfaktor: ihre Monatsprofile sind Messwerte des
Wärmebedarfs, in denen der Effekt bereits enthalten ist.

## 4. Dateien und Werkzeugkette

Alle Quell- und Generatordateien liegen in `C:\Waermeplan\Wärmespeicher\`:

| Datei | Zweck |
|---|---|
| `TWW-Auslegung_V4.xlsx`, Blatt `Zapfprofil` | Quelle der 11 Stundenprofile (Tagtypen + Wochenfaktoren) |
| `VDI 6002 Blatt 1/2 …pdf` | Normquellen: Bilder D1–D5, Bilder 1–10, Tabelle 3 |
| `gen_profiles.py` | erzeugt `typen.csv` + `daten.csv` aus Excel und VDI-Jahresgängen |
| `kaltwasser.py` | Kaltwasser-Jahresgang aus `Waermespeicher-Tool/vendor/try_weather/TRY2010_*.dat` |
| `jahresgang_neu.py` | erzeugt `daten_neu.csv` (Hotel ×3, Büro, Sport, Schule) |
| `Brauchwassertypen_VDI6002_Stundenprofile.csv` | 11 Typen × 168 Stundenwerte, so wie eingespielt |
| `Brauchwasserdaten_VDI6002_Monatswerte.csv`, `Brauchwasserdaten_Jahresgang_neu.csv` | Monatswerte, so wie eingespielt |

### Access-Datenbank ohne Access bearbeiten

Das Einspielen lief über **Jackcess** (Java), nicht über ODBC oder Access-Automation — das
funktioniert headless, ohne installierte ACE-Engine und ohne Rücksicht auf 32/64 Bit:

```bash
# Bibliothek holen (einmalig)
curl -sfLO https://repo1.maven.org/maven2/com/healthmarketscience/jackcess/jackcess/4.0.5/jackcess-4.0.5.jar
curl -sfLO https://repo1.maven.org/maven2/org/apache/commons/commons-lang3/3.12.0/commons-lang3-3.12.0.jar
curl -sfLO https://repo1.maven.org/maven2/commons-logging/commons-logging/1.2/commons-logging-1.2.jar
javac -encoding UTF-8 -cp jackcess-4.0.5.jar MeinTool.java
java -Dfile.encoding=UTF-8 -cp .:jackcess-4.0.5.jar:commons-lang3-3.12.0.jar:commons-logging-1.2.jar MeinTool Kenndaten.accdb
```

Wesentliche Punkte im Code: `DatabaseBuilder.open(new File(pfad))`, danach
`db.setAllowAutoNumberInsert(true)`, wenn eigene IDs vergeben werden sollen. Einfügen mit
`table.addRowFromMap(map)`, Ändern über die aus der Iteration erhaltene `Row` mit
`row.put(spalte, wert)` und `table.updateRow(row)`. Die Zahlenspalten heißen `"1"`…`"168"` und
werden als String angesprochen. Am Ende `db.flush()` und `db.close()`.

**Vorher immer prüfen, ob `Kenndaten.laccdb` existiert** — dann hat die Anwendung oder Access die
Datenbank offen und ein Schreibzugriff würde den Stand zerreißen. Nach dem Schreiben lohnt ein
unabhängiger Prüflauf, der die Sollwerte neu aus der Norm berechnet, statt die Generatorlogik zu
wiederholen: geprüft wurden Tagessummen = Wochenfaktoren, Wochensumme = 100, Jahressummen,
alle Monatswerte > 0, Beschreibungslänge ≤ 255 und Unverändertheit des Altbestands.

## 5. Zieldatenbanken

| Pfad | Rolle | Sicherungen |
|---|---|---|
| `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` | **von der Anwendung genutzt** (DSN `TEST`) | `Kenndaten_vor_VDI6002_2026-08-02.accdb`, `…_kompakt_…`, `Kenndaten_vor_Jahresgang_2026-08-02.accdb` |
| `C:\Waermeplan\WP_Plan\Kenndaten-ok.accdb` | Repo-Arbeitskopie | `DB-Backup\Kenndaten-ok_vor_VDI6002_2026-08-02.accdb` |

Achtung bei Schreibzugriffen auf `ProgramData`: der Standard-ACL erlaubt Benutzern nur das Anlegen
neuer Dateien, nicht das Ändern vorhandener. Eine vom Installer angelegte `Kenndaten.accdb` ist
dadurch schreibgeschützt — Access meldet das beim Öffnen. Nach „Komprimieren und reparieren"
gehört die Datei dem angemeldeten Benutzer und ist beschreibbar.

## 6. Kritisch: die Migration löscht den neuen Katalog

`migration.manuell.sql` (Repo-Wurzel, wird von der Migrations-GUI im Auto-Modus **mit Vorrang**
verwendet) folgt in Teil A der Regel „Zeilen mit `ReadOnly = TRUE` bleiben aus der **Vorlage**
erhalten, alles Übrige kommt aus der **Quelle**". Für den Brauchwasserpfad ist diese Regel aber
nur halb umgesetzt:

```sql
-- Zeile 50
DELETE FROM [Tab_Brauchwassertyp_STAMM];                              -- ohne ReadOnly-Filter!
-- Zeile 51
DELETE FROM [Tab_Brauchwasser_STAMM] WHERE [ReadOnly] = FALSE;        -- korrekt gefiltert
```

Konsequenz beim nächsten Migrationslauf:

| Objekt | Schicksal |
|---|---|
| alle 11 Stundenprofile (auch die vier VDI-Typen mit `ReadOnly = TRUE`) | **werden gelöscht** und durch `Tab_Brauchwassertyp` der Quell-DB ersetzt |
| die 4 VDI-Monatsdatensätze (`ReadOnly = TRUE`, ID 97–100) | bleiben erhalten — verlieren aber ihre Typen und damit ihr Stundenprofil |
| die 9 übrigen Monatsdatensätze (`ReadOnly = FALSE`, ID 101–109) | **werden gelöscht** |

Zusätzlich ist der Kommentar in Zeile 123 („ReadOnly-Zeilen der Vorlage bleiben: 2") überholt — es
sind jetzt 6 (94, 95 und 97–100). Der Import in Zeile 126 schließt mit `WHERE [ID] NOT IN (94, 95)`
nur die beiden alten Vorlagenzeilen aus; hat eine Anwender-DB eigene Einträge mit ID 97 ff., kollidiert
der Insert mit den erhalten gebliebenen Vorlagenzeilen.

**Zu entscheiden, bevor die nächste Version ausgeliefert wird:**

1. Zeile 50 auf `DELETE FROM [Tab_Brauchwassertyp_STAMM] WHERE [ReadOnly] = FALSE;` ändern —
   dann verhält sich der Typ-Katalog wie alle anderen Kataloge.
2. In dieser Architektur bedeutet `ReadOnly = TRUE` faktisch **„gehört zur Auslieferung"**, nicht
   bloß „schreibgeschützt". Wenn die 7 generischen Typen und die 9 zugehörigen Datensätze
   Auslieferungsbestand sein sollen — und dafür spricht alles, sie stammen aus der INEKON-Vorlage —
   müssen sie ebenfalls auf `ReadOnly = TRUE` gesetzt werden. Anwender ändern sie dann über
   „Speichern unter" statt in place. Das ist eine bewusste Entscheidung, kein Automatismus.
3. Den Ausschluss in Zeile 126 auf alle erhaltenen Vorlagen-IDs erweitern oder den Quell-Import mit
   einem ID-Versatz einlesen. Achtung: `Bezeichner` bzw. `Typname` sind global eindeutig indiziert —
   heißt in einer Anwender-DB ein Typ ebenfalls „Hotel", scheitert der Insert. Für die
   Stromverbraucher löst das Skript denselben Konflikt bereits über einen Namenszusatz.

## 7. Weitere offene Punkte

1. **Spaltennamen-Bug.** `Controller/BrauchwasserCtrl.cs` schreibt in `Insert()`/`Update()` auf
   `M1…M12`, gelesen wird `Monat_n`. Der Pfad ist derzeit nicht aktiv (die Views speichern über
   `BrauchwasserStammCtrl`), sollte aber korrigiert werden.
2. **Lokalisierung.** `Views/Brauchwasser/` enthält nur `.resx`, keine `.de-DE.resx`/`.en-US.resx` —
   als einziger View-Ordner.
3. **Schulferien** sind bundesweit gemittelt; für konkrete Objekte lohnt ein landesspezifisches
   Profil. Analog beim Hotel die Wahl der passenden der drei Varianten.
4. **Kaltwassertiefe** ist mit z = 2,0 m angesetzt. Bei bekannter Verlegetiefe (1,0–1,5 m in milden
   Regionen) wird die Amplitude größer — Parameter in `kaltwasser.py`.
5. **Hallenbad und Camping** aus VDI 6002 Blatt 2 sind bewusst nicht aufgenommen (die Richtlinie rät
   bei Camping ausdrücklich von der Verwendung ihrer Profile anstelle von Messdaten ab).

## 8. Relevante Dateien im Repo

| Pfad | Rolle |
|---|---|
| `migration.manuell.sql` | Migrationsskript, Teil A Zeilen 50/51 und 123–131 betreffen den Brauchwasserkatalog |
| `migration.config.json` | `excludeTables` des Auto-Generators; das manuelle Skript hat Vorrang |
| `WindowsFormsApplication1/Controller/BrauchwasserStammCtrl.cs` | Katalog lesen/schreiben, `CopyFromStamm()` Stamm → Projekt |
| `WindowsFormsApplication1/Controller/BrauchwasserCtrl.cs` | Projekttabellen, enthält den `M1…M12`-Bug |
| `WindowsFormsApplication1/Views/Brauchwasser/Form_EingBrauchwasserTyp.cs` | Maske „Brauchwassertypen Stundenverteilung", schreibt Spalten `1`…`168` einzeln per UPDATE |
| `WindowsFormsApplication1/Views/Brauchwasser/Form_EingDBBrauchwasser.cs` | Maske „Eingabe Brauchwasser Daten" (12 Monatswerte) |
| `WindowsFormsApplication1/Views/Brauchwasser/Form_Brauchwasser.cs` | Projektzuordnung inkl. überschreibbarer Jahressumme |
| `WindowsFormsApplication1/Allgemein/Simulation/SimulationWaermebedarf.cs` | `Brauchwasserwaerme_berechnen()`, Aufruf von `I_strom_wochetojahr` |
| `BHKWPLAN.DLL` / `CSExeCOMServer` | nativer Rechenkern, feste Feldgrößen 8760/168/365/12 |
