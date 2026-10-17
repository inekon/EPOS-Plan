# KBP-4 — Typtage der Kältebedarfszuordnung

Sitzung Dialoge und Korrekturen (Übersicht Berichte & Kosten), 16.10.2026, Zweig `kbp4`. Konzept:
[`Konzept_Kaeltebedarfsprofile_EPOS-Plan.md`](../../Konzept_Kaeltebedarfsprofile_EPOS-Plan.md).

## Stand

| Welle | Statuszeile | Inhalt | Nachweis |
|---|---|---|---|
| KBP-4a | #1130 | Schemaschritt 236 `KaeltetyptagSchema` (`Tab_Kaeltetyptag`, `Tab_Kaeltetyptag_Matrix`), Typtagschicht in `ProfilBedarf`, `KaeltetyptagCtrl`, Matrix in KINDER des Duplizierens; Testdatenbank 236 | Kern-Tests, Wachen, Auslieferungsvorlage-Tests |
| KBP-4b | #1131 | Typtag-Editor `KaelteTyptagEditor.razor`, Speicherweg, Fensterprobe `kaeltetyptage`, Wiki-Quelle, Auslieferungsvorlage 197 | bUnit, Fensterprobe |

## Abnahme

Gefilterte `EPOS.Kern.Tests` 2 175/0, gefilterte `EPOS.UI.Tests` 411/0, `Auslieferungsvorlage.Tests` 63/63,
SQL-Dialekt-Prüfer ohne Fundstelle (3 044 Texte), Kern-Mappe und Windows-Schale 0 Fehler. Restgate vor dem Merge:
Referenzlauf 37/37 gegen R56, CSV byte-gleich 1 205/1 205; der Merge mit `origin/ios_migration_september`
(Gebäudesimulation BK-7b bis BK-8b, Status #1126–#1129) berührte `EPOS.Kern/Simulation` nicht, der Lauf wurde nicht
wiederholt. Ohne Konflikte zusammengeführt.

## Offen

Wiki-Upload der Quellen mit dem Sammel-Upload, Version beim Anwender; Sichtprüfung unter Windows.
