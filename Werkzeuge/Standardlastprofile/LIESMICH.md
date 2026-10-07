# Standardlastprofile — Ableitung der Katalogsaat Strom

`ableiten.py` leitet aus den BDEW-Standardlastprofilen Strom 2025
([`Quellen/Standardlastprofile`](../../Quellen/Standardlastprofile/LIESMICH.md)) die drei Katalogsätze der
„Datenbank Strombedarf" ab — H25 (Haushalt), G25 (Gewerbe allgemein), L25 (Landwirtschaftsbetriebe) — und
schreibt sie als C#-Konstanten nach
[`EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs`](../../EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs).
Der Schemaschritt `StandardlastprofilSchema` (gleicher Ordner) sät daraus die Zeilen in
`Tab_Stromverbraucher_STAMM` und `Tab_Stromverbrauchertyp_STAMM`. Die Konstantendatei wird nie von Hand
geändert.

## Aufruf

Aus der Repowurzel, auf Windows mit dem Starter `py` und `PYTHONIOENCODING=utf-8` davor (sonst `python3`);
ohne Zusatzpakete (die Excel wird mit `zipfile` und XML gelesen):

| Aufruf | Wirkung |
|---|---|
| `py Werkzeuge/Standardlastprofile/ableiten.py` | prüft nur: „unverändert", wenn die Konstantendatei dem Stand der Excel entspricht (Rückgabe 0), sonst „VERALTET" mit der ersten abweichenden Zeile (Rückgabe 1) |
| `py Werkzeuge/Standardlastprofile/ableiten.py schreiben` | schreibt die Konstantendatei (UTF-8 mit BOM, CRLF) |
| `py Werkzeuge/Standardlastprofile/ableiten.py kennzahlen` | Monatswerte, Spitzenfaktor der Woche, Verhältnis Werktag/Samstag/Sonntag je Profil |
| Option `--excel <pfad>` | eine andere Excel desselben Aufbaus |

Rückgabe 2: Excel fehlt oder ist anders aufgebaut (Blattnamen und Titel, Hinweis zur Dynamisierung, Einheit
`[kWh]`, Typtagfolge SA/FT/WT, Monatskopf, 96 Viertelstunden, Werte > 0, keine Formeln, nichts außerhalb des
Rasters). Die Ausgabe ist deterministisch: gleiche Excel, gleiche Datei Byte für Byte; der Prüfmodus
vergleicht ohne Rücksicht auf die Zeilenenden. Die Datei trägt die SHA-256 der Excel.

## Rechenregeln

Grundlage je Profil: 12 Monate × 3 Typtage (SA Samstag, FT Sonn- und Feiertag, WT Werktag) × 96
Viertelstunden in kWh, normiert auf 1.000 MWh/a.

**Wochenwerte (168, `Tab_Stromverbrauchertyp_STAMM`, Spalten „1" bis „168", Montag 0 Uhr zuerst):**

- Montag bis Freitag aus dem Werktag, Samstag aus dem Samstag, Sonntag aus dem Sonn- und Feiertag.
- Stundenwert = Summe der vier Viertelstundenenergien [kWh je Stunde = mittlere kW].
- Jahresmittel je Typtag und Stunde: die zwölf Monate gewichtet mit der Zahl ihrer Tage (28, 30, 31). Im
  28-Jahre-Kalenderzyklus fällt jeder Typtag anteilig gleich auf jeden Tag eines Monats; das Gewicht ist
  deshalb für alle drei Typtage dasselbe.
- Ohne Dynamisierung (sie hebt einen Tag als Ganzes und ändert seine Form nicht).
- Skala: kWh je Stunde bei 1.000 MWh/a, Mittel rund 114 kWh je Stunde. Für die Rechnung ist sie
  gleichgültig: `BhkwPlan.StromWocheToJahr` kachelt die Woche ab dem Wochentag des 1. Januar über
  8 760 Stunden und normiert je Monat auf den Monatswert (× 1.000, MWh nach kWh).

**Monatswerte (12, `Tab_Stromverbraucher_STAMM`, `Monat_1` bis `Monat_12` in MWh):**

- Abrollung über das Raster von 365 Tagen (kein Schaltjahr) mit Werktag, Samstag und Sonntag ohne
  Feiertage, gemittelt über die sieben möglichen Wochentage des 1. Januar — auf diesem Raster ist das der
  28-Jahre-Kalenderzyklus; das Ergebnis hängt an keinem Kalenderjahr.
- H25 mit der Dynamisierung je Tag des Jahres t:
  F(t) = −3,92·10⁻¹⁰ t⁴ + 3,2·10⁻⁷ t³ − 7,02·10⁻⁵ t² + 2,1·10⁻³ t + 1,24, F auf 4 und jeder Viertelstundenwert
  danach auf 3 Nachkommastellen kaufmännisch gerundet (BDEW-Veröffentlichung S. 4); G25 und L25 ohne.
- Danach exakt auf 1.000 MWh skaliert; die Werte stehen mit 6 Nachkommastellen, der Rundungsrest geht nach
  dem größten Rest, die Summe ist 1.000,000000 MWh. Vor der Skalierung ergibt die Abrollung ohne Feiertage
  995,1 (H25), 1.017,6 (G25) und 1.000,2 MWh (L25).

**Feiertage** zählen beim BDEW wie Sonntag. In EPOS-Plan legt sie ein Betriebskalender an der Zuordnung
des Projekts auf den Sonntag des Wochenprofils (Bundesland, „Feiertag wie Sonntag"); er verteilt nur
innerhalb des Monats um. Ohne Kalender rechnet ein Feiertag wie ein Werktag.

## Was verloren geht

Das Wochenprofil führt eine Tagesform je Typtag für das ganze Jahr: Die Monatsform der Typtage (36 Typtage
werden 3), die Viertelstunde (der Rechenweg spreizt eine Stunde auf vier gleiche Viertel) und die
Dynamik innerhalb eines Monats entfallen. Die Monatsmengen bleiben erhalten. P25 und S25 werden nicht
abgeleitet (Netzbezugsprofile mit Photovoltaik bzw. Photovoltaik und Speicher).
