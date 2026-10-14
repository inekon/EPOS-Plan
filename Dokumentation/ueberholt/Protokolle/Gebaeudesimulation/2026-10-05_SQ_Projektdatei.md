# Protokoll SQ — Nutzungsprofile, Tagesganglinien und Kalender aus der HottCAD-Projektdatei (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Wellen SQ-1 (Kern-Leser), SQ-2 (Dialog), SQ-3 (Export, Rundlauf, Papiere) nach Entscheid E80, drei Opus-Agenten nacheinander im Worktree `sqproj`: SQ-1 `38e835dc`, `bf34291c`, `85aeb987`, `94ff0235`; SQ-2 `e0783ac9`, `ad27fc39`, `27dda89d`, `a0479bae`, `0512bc7e`, `0702ac8a`, `dda44fa1`; SQ-3 `94d23bc6`, `4cea8fbc`, `f6a8c286`, `e5fa98a3`, `fe167e2f`; Merge `f98ebeae`. Statuszeile #731.
**Entscheid:** E80 (Anwender, 05.10.2026) mit der Lesart der Orchestrierung zum IFC-Export; Konzept als Nachtrag 3 (Datenaustauschkonzept Kapitel 16). Kein Schemaschritt, kein neues Paket, Basis unverändert, Referenzlauf nicht betroffen.

## 1 Auftrag

Der IFC-Export von HottCAD trägt weder Zonen noch Nutzungsprofile, Tagesganglinien oder Kalender; die Projektdatei (`.sqproj`, SQLite) trägt sie relational ([Befund](../../../aktuell/Gebaeudesimulation/2026-10-05_Befund_HottCAD_Projektdatei.md), an sieben Dateien geprüft). E80: kein Klimaimport, Hülle weiter aus dem IFC-Export, Nutzungsprofile mit DIN-V-18599-Nummer, 24-Stunden-Ganglinien je Profilklasse und Kalender aus der Projektdatei übernehmen; der eigene IFC-Export schreibt alle vorhandenen Daten.

## 2 Vorgehen

Konzept Kapitel 16 durch die Orchestrierung; drei Opus-Agenten (116, 113, 111 Aufrufe), die Codes aus dem zweiten Befund (Schlüssel `GId`, Zonentypen, Betriebsart, Tagesart) während SQ-1 nachgereicht; Merge ohne Konflikt; Gate 731.

## 3 Ergebnis

- **Leser** `EPOS.Kern/Allgemein/Import/Sqproj/` (`SqprojLeser`, `SqprojAbbild`, `SqprojProfil`, `SqprojProtokoll`, `SqprojRaumabgleich`, `SqprojZonen`, `SqprojKonditionierung`, `SqprojStand`, `Din18599Nutzung`): nur lesend (`Mode=ReadOnly`, Arbeitskopie für Ströme, Größengrenze des IFC-Profils), Fassungs- und Tabellenprüfung mit benannter Ablehnung; Räume, Geschosse, Zonen (Typ 6 vor 5; 2, 7, 10, 0, 8 gezählt), Nutzungsprofil (Profilnummer), Profilgruppen mit Zeitprofilen Heizen, Kühlen, Lüftung, Geräte, Personen (übrige Klassen gezählt), 24 Stunden je Profil mit Betriebsart, Abschnitte. Raumabgleich über `BmRoom.GId` ↔ `IfcSpace.GlobalId`, zweitens Raumname je Geschoss, `RoomType` als dritter Beleg. Nutzung aus der Profilnummer: 1–5 Büro, 8/9/28/29 Schule, 70/71 Wohnen, sonst keine. Konditionierung je Zone: Standardwoche (168 Zellen) aus der Ganglinie mit Tagesart 4/5/6 als 5-, 6-, 7-Tage-Woche (benannte Annahme), Nennwerte für Geräte und Personen, Abschnitte als Perioden, Nutzungsprofil als Matrixzellen; Rangfolge Ganglinie vor Profil vor Vorlage; Beleg je Zelle, gespeichert in der `Bemerkung` des Kalenders; 24 Meldungsschlüssel de/en. Einzonenweg: Gebäudegruppe bzw. einzige Zone als Gebäudekalender. SQL-Prüfer mit Liste `FREMDSCHEMA` (nur `EXPLAIN` entfällt). `Quellen/*.sqproj` in `.gitignore`; Diagnose liest lokal liegende Dateien.
- **Dialog** (`GebaeudeImportDialog.razor`, Hülle, DTOs `GebaeudeProjektdateiDaten`): Knopf „Projektdatei dazuladen (.sqproj)…“ nur bei HottCAD-IFC (`IstHottcad`), Bilanz im Kopf (Datei, Fassung, Räume abgeglichen / nicht / ohne Gegenstück, Zonen, Zeitprofile je Größe, Abschnitte, Übersprungenes), Übernahme als Planschritt `PROJEKTDATEI` mit Rückfrage bei Handschritten, Kennzeichen „aus Projektdatei“ je Zone mit Profilnummer als Tooltip, „Projektdatei entfernen“; iOS-Dateifilter `.sqproj` (`public.data`). 25 Texte de/en. Die Kalenderkarte zeigt den Beleg als Vermerk.
- **Export** (`IfcKonditionierungssatz`, `IfcSchreiber`, `IfcAnreicherung`): `EPOS_Zone` mit Nutzung, Heizsollwert Tag/Nacht, Kühlsollwert, Luftwechsel; `EPOS_Kalender_<Größe>` mit Grundwert/Aus/Woche, Nennwert, Bemerkung und `Periode_<Rang>` als Text `Art;Beginn;Ende;Feiertagsregel;Angabe` (Bezeichner im `Description`); der Leser nimmt sie zurück (`AbbildKonditionierung`, Herkunft „aus IFC-Datei (EPOS)“), der Zonenplan übernimmt sie; die Anreicherung (G7d) ergänzt ohne Dopplung. Probe 38: Referenzprojekt 1052 (3 Zonen, 2 Zonenkalender, 9 Perioden) kommt nach Export und Lesen gleich zurück, zweiter Export byte-gleich; Gegenproben ohne `EPOS_*` ohne Konditionierung.
- **Tests:** Sqproj 44 + Hülle 6 + Probe 37 (5 bunit) + Rundlauf 38; Kern-Filter 910/836, UI 220/123 grün; Wachen grün.
- **Papiere:** Datenaustauschkonzept 16 als gebauter Stand und 6.3, Konditionierungskonzept 5.5, Mehrzonenkonzept 6.4, Softwarearchitektur, Wiki-Quelle „Gebäudeimport“ (Projektdatei dazuladen, Export je Zone), `LIESMICH_Importproben.md`, `Werkzeuge/SqlDialektPruefer/LIESMICH.md`.

## 4 Entscheide der Orchestrierung

1. Lesart von E80 „IFC-Export: alle vorhandenen Daten“: der eigene Export schreibt Zonen-Nutzung, Sollwerte und Kalender als `EPOS_*`-Sätze (Widerspruch möglich).
2. Tagesart 4/5/6 als Montag–Freitag, Montag–Samstag, alle Tage — benannte Annahme bis zur Diagnose an einer echten Datei.

## 5 Abweichungen

Arbeitskopie unter dem temporären Pfad statt dem Pfad-Dienst; Beleg nur in `Bemerkung` (keine Spalte an der Vorgabetabelle ohne Schemaschritt); NACHT absolut; Angabetexte der Perioden mit Präfix; Probenerzeuger geteilt zwischen Kern- und UI-Tests.

## 6 Abnahme

Gate 731 im Hauptbaum auf `f98ebeae`: Kern-Filter 0 Fehler, ChartProben 208 gleich, Kern 11 048 grün (3 übersprungen) und 1 rot — eigen: `IdsWacheTests` verlangte die vier neuen `EPOS_Zone`-Eigenschaften in `Setup/Vorlage/EPOS_Export.ids`, nachgetragen (Nutzung, Heizsollwert_Tag/_Nacht, Kuehlsollwert, Luftwechsel_Nutzer), Wache danach 30/30 grün —, UI 7 533, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), Dokumentationswachen 35/35, Referenzlauf 20/20 PASS gegen R36, 608/608 byte-gleich, gestörter Lauf PASS. Windows-Schale auf Linux 0 Fehler. SQL-Dialekt-Prüfer 2 365 Texte, 0 Fundstellen, 19 fremdes Schema.

## 7 Offen

- **Diagnose an einer echten Projektdatei** (lokale Sitzung): Tagesart, `RatedPersonOccupancyRate` als Personenzahl, Geräteleistungsspalte in `PdProfileDevice` — `SqprojQuelldateienDiagnoseTests` mit `Quellen/<name>.sqproj` neben `Quellen/<name>.ifc`.
- iOS-Nachweis des Dateifilters im nächsten iOS-Lauf (nur nach Rückfrage); Windows-Sichtabnahme von Knopf, Bilanz und Zonenbaum.
- Vollständiger Speicherweg des Einzonenwegs über die Gebäudeliste ungeprüft (nur über die Hülle getestet).
- Logbuch-Sätze (Version offen): „Im Gebäudeimport lässt sich zu einer IFC-Datei die Projektdatei des CAD-Programms (.sqproj) dazuladen; ihre Zonen kommen mit Nutzung und Zeitprofilen in den Zonenbaum und werden mit der Konditionierung je Zone gespeichert.“ — „Der IFC-Export schreibt je Zone Nutzung, Sollwerte und Konditionierungskalender mit; beim Wiedereinlesen übernimmt jede Zone ihre Konditionierung.“

## 8 Aufwand

Rund 340 Agentenaufrufe, 5 PT.
