# KBP-2 bis KBP-3b — Kältebedarfsprofile: Vorlagen, Eingabearten, Grafik

Sitzung Dialoge und Korrekturen (Übersicht Berichte & Kosten), 16.10.2026, Zweig `welle5`. Konzept:
[`Konzept_Kaeltebedarfsprofile_EPOS-Plan.md`](../../../aktuell/Konzept_Kaeltebedarfsprofile_EPOS-Plan.md), Abschnitt 10.

## Stand

| Welle | Inhalt | Nachweis |
|---|---|---|
| KBP-2 | Schemaschritt 230 `KaeltebedarfsprofilVorlagenSchema` (hängt an 229): `Z_Projekt_Kaeltebedarf.Eingabeart` mit CHECK, Art-Vorschlag `Kaelte_Art`, Saat der Vorlagen 7 bis 16; Zeilen 1 bis 6 bytegleich; Testdatenbank 230 | Saatwache, Katalogfassung, Auslieferungsvorlagen |
| KBP-3a | Eingabearten Monatswerte und Wochenprofil der Kältezeile, Art-Vorschlag beim Übernehmen, KI-Feld `eingabeart` | Kern-Tests, bUnit |
| KBP-3b | Grafikblätter Jahresgang, Monate, Woche, Dauerlinie und Kennzahlenzeile | Kern-Tests, bUnit, Probe-Ansicht `kaelteprofile` |
| Nachzug | Nutzungszeiten nach DIN/TS 18599-10:2025-10, Labor Profil 38, Prozesskälte einschichtig nach Hallenprofilen 22 bis 24 | Saatwache |

## Gate

Referenzlauf 37/37 PASS gegen R55, CSV byte-gleich 1 205/1 205; EPOS.UI.Tests 9 572/0, KiKern 553/0, SpeicherEngine 397/0,
gefilterte Kern-Tests 4 342/0, SQL-Prüfer 3 002 Texte ohne Fundstelle. Testdatenbank 230, LFS-SHA-256
`a901f445a430231ab6c68c1cc0e3a1ef71a3717cc922bb07c426328b27b3b3f1`.

## Offen

KBP-4 (Typtage, Schemaschritt B) und KBP-5 (Angleichung, Wiki-Quellen) laut Konzept; Sichtprüfung unter Windows.
