# Protokoll NP1 — Nutzungsprofile: Schemaschritt 189, Katalog, Generator, Controller (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Wellen NP1a und NP1b (E92), NP1a `971dd72`, `7b710d0` (Datenbank 189), `6b28e46`, `065a51f`, NP1b `3e79c2e`, `cf95873`, `cef82ca`, `9aec54f`, `d1434d8`.
**Entscheide:** E90 (Q37 Weg 3 erweitert: frei definierbare Nutzungsprofile), E91 (Q38–Q47), E92 (die Sitzung baut die ganze Stufe NP).

## 1 Auftrag

Nutzungsprofile als Katalog mit Kategorien, aus dem ein Generator die Konditionierungskalender einer Zone erzeugt (Konzept Nutzungsprofile, Stufe NP1: Schema, Katalog, Generator, bitgleiche Muster).

## 2 Vorgehen

Zwei Opus-Agenten nacheinander: NP1a (Schema 189, Katalog, Saat, Testdatenbank, Auslieferungsvorlage), NP1b (freier Text für `Nutzung`, Generator, Controller, Tests). Die Papiere schrieb ein Sonnet-Agent. Gate 744 fährt die Orchestrierung nach dem Merge.

## 3 Ergebnis

**NP1a (Schema 189, Katalog).** Fünf Tabellen `Tab_Raumnutzung*` (Katalog, Profil, Zeilenbild, Stundenprofil, Zuordnung), `Tab_Zone.Nutzungsprofil`; Saat 4 Kategorien, 33 Profile, 25 Zuordnungen, 50 Zeilenbildzeilen; Auslieferungsvorlage mit `--kataloge readonly`; STRICT-Wache 173; Testdatenbank 189 (87 793 664 Byte, OID `e90fc05f…`).

**NP1b (Freitext, Generator, Controller).** `Nutzung` an Kalender und Vorlage als freier Text (Tabellenneubau); `Raumnutzungsgenerator` erzeugt die Kalender in der Reihenfolge Zeilenbild, Stundenprofil, Kennwerte; `RaumnutzungCtrl` mit `ProfilUebernehmen` und `ZuordnungAufloesen`; die Herkunft steht als freier Text. Tests `RaumnutzungSchemaTests` 13, `RaumnutzungGeneratorTests` 16.

## 4 Festlegungen

Die Festlegungen NP-F1 bis NP-F24 stehen im Konzept Nutzungsprofile; Q38–Q47 sind mit E91 entschieden. Ergebnisneutral: Referenzbasis unverändert.

## 5 Nachweise

Bitgleichheit der drei alten Muster zur Vorlagenübernahme nachgewiesen; Merge-Abnahme: Kern-Filter 0 Fehler, 958 Tests der Auswahl (Raumnutzung, Konditionierung, Schema, Dokumentation, Repositoryordnung, Ressourcen) grün, SQL-Dialekt 2 432 Texte, 0 Fundstellen; Referenzlauf 1047/1051/1052/1054 PASS. Gate 744 steht in der Statuszeile #744 der Statusdatei.

## 6 Offenes

Sichtabnahme unter Windows, CI-Vermerk nach dem Push. NP2 (Zonenbaum, Zuordnung, Import) nach dem Push der Sitzung IFC-Ganglinie; NP3 (Blatt, Zugänge), NP4 (CSV-Import, Projektdatei, Wiki). Fachlich zu prüfen: Personennennwert mindert den Gerätenennwert über P1; `Feiertage_Wie_Sonntag` bei Lager und Verkehr ist eine eigene Setzung.
