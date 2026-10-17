# KBP-5 — Eingabearten, Normkalender, Ferien wie Sonntag

Sitzung Dialoge und Korrekturen (Übersicht Berichte & Kosten), 16.10.2026, Zweig `kbp5`. Konzept:
[`Konzept_Kaeltebedarfsprofile_EPOS-Plan.md`](../../Konzept_Kaeltebedarfsprofile_EPOS-Plan.md), Abschnitt 10.

## Stand

| Welle | Statuszeile | Inhalt | Nachweis |
|---|---|---|---|
| KBP-5a | #1116 | Schemaschritt 234 `BedarfsprofilEingabeartSchema` (Eingabeart an `Z_Projekt_Prozesswaerme` und `Z_Projekt_Brauchwasser`, `Nutzungstage_Norm` an `Tab_Kaeltebedarf(_STAMM)`, `Ferien_wie_Sonntag` an `Tab_Betriebskalender`); Eingabearten Monatswerte und Wochenprofil für Prozesswärme und Brauchwasser; Testdatenbank 234 | Kern-Tests, bUnit, Referenzlauf |
| KBP-5b | #1117 | Normkalender der Kältevorlage 8 (230 Nutzungstage), KI-Text der Eingabeart, Wiki-Quellen | Kern-Tests, bUnit |
| KBP-5c | #1118 | Feiertagsland im Namen des Normkalenders | `NormkalenderTests` |
| KBP-5d | #1119 | Schalter „Ferien wie Sonntag“, ungenutzte Grafikschlüssel entfernt | Kern-Tests, bUnit |
| FP-D | #1120 | Fensterprobe `dubletten`: Gegenprobe „negativer Rand“ prüft den Abstand Fuß–Protokoll | `fensterprobe.mjs` |

## Gate

Referenzlauf 37/37 PASS gegen R55 (12 736 541 Werte in Toleranz), CSV byte-gleich 1 204/1 205; byte-verschieden nur
`Projekt_1072/aggregate.csv` mit zwei zusätzlichen Zeilen `Kaelte.DeckungsgradGesamtProzent` und
`Kaelte.DeckungsgradKaeltefolgeProzent` (Kälteschlüssel einer anderen Sitzung aus #1105, wird mit R56 eingefroren).
EPOS.UI.Tests 9 601/0, KiKern 553/0, SpeicherEngine 397/0, SpeicherPlanung 27/0 (1 übersprungen), gefilterte Kern-Tests 1 770/0,
Kern-Wächter 35/0, SQL-Prüfer ohne Fundstelle, Kern-Mappe und Windows-Schale 0 Fehler.

## Offen

KBP-4 (Typtage, Schemaschritt B); Wiki-Upload der Quellen mit dem Sammel-Upload, Version beim Anwender; Sichtprüfung unter Windows.
