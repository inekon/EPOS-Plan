# WP-Plan / EPOS-Plan — Projektkontext

Windows-Desktop-Anwendung zur Planung und Simulation von Energie- und Wärmeversorgungskonzepten
(Wärmebedarf, Brauchwasser, Prozesswärme, Heizkessel, BHKW, Wärmepumpe, Solarthermie, Photovoltaik,
Speicher, Klimadaten) mit Wizard-Workflow, Herstellerdaten-Import, Simulation, Berichten und
Wirtschaftlichkeitsrechnung.

Diese Datei beschreibt **Fachdomäne, Datenmodell, Migration und Umgang mit der Datenbank**.
Alles zu Code, Build und Architektur steht in
[`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md).

C#, `net8.0-windows`, WinForms (MDI), Build zwingend **x86** (ACE OLEDB); die Umstellung auf x64
ist analysiert und geplant — siehe
[`Konzept_Umstellung_64Bit_EPOS-Plan.md`](Konzept_Umstellung_64Bit_EPOS-Plan.md). Der Rechenkern liegt
vollständig in verwaltetem C# (`Allgemein/BhkwPlan.cs`) — die frühere native `BHKWPLAN.DLL` und der
COM-Server `CSExeCOMServer` werden nicht mehr verwendet.

## Datenhaltung

Alles in einer einzigen Access-Datei `Kenndaten.accdb` — Kataloge **und** Projektdaten. Eine
separate Projektdatei gibt es nicht.

Der Rechenkern arbeitet mit fest verdrahteten Feldgrößen: 8760 Stunden, 168 Wochenwerte, 365 Tage,
12 Monate. Profile und Ganglinien im Datenmodell müssen zu diesem Raster passen.

## Namenskonventionen im Schema

`Tab_*` sind Stamm- und Projektdaten, `Tab_*_STAMM` der Auslieferungskatalog, `Z_*` die Zuordnung
Projekt ↔ Katalogobjekt, `Abfrage_*` gespeicherte Access-Abfragen. Verknüpfungen laufen vielfach über
Textfelder (`Bezeichner`, `Typname`) statt über IDs — **bei neuen Beziehungen IDs verwenden.**

Das Feld `ReadOnly` in den `_STAMM`-Tabellen bedeutet faktisch „gehört zur Auslieferung": Das
Migrationsskript behält `ReadOnly = TRUE` aus der Vorlage und ersetzt alles Übrige durch die
Anwenderdaten.

## Migration

Die DB Migration ist eine separate Anwendung. Das sql migrationsskript wird jedes mal neu erstellt und ist daher nicht als Referenz geeignet.


## Umgang mit der Datenbank

Vor jedem Schreibzugriff prüfen, ob `Kenndaten.laccdb` existiert (dann ist die DB geöffnet), und
vorher eine datierte Kopie anlegen. `C:\ProgramData\EPOS_PLAN` erlaubt normalen Benutzern nur das
Anlegen neuer Dateien, nicht das Ändern vorhandener — eine vom Installer angelegte `Kenndaten.accdb`
ist deshalb schreibgeschützt, bis sie einmal über „Komprimieren und reparieren" neu geschrieben
wurde. Dieselbe ACL blockiert den Start auf einem **zweiten Windows-Konto**, solange das erste das
Programm offen hat (Sperrdatei nicht beschreibbar) — Ursache, `icacls`-Lösung und Installer-Hinweis
in [`BETRIEB_Mehrbenutzer_Datenbank.md`](BETRIEB_Mehrbenutzer_Datenbank.md).

`.accdb` ist in `.gitignore` ausgeschlossen: Änderungen an der Datenbank landen nie in einem Commit
und müssen separat gesichert werden (`DB-Backup/`).

Bekannte Altdaten-Beschädigung: In `Tab_Heizkessel.Bezeichner` und `Tab_Energieanlagen.Bezeichner`
stehen Namen mit U+FFFD aus früheren Importen mit falscher Kodierung — Ursache, Fundstellen und
Entscheidung (kein Skript-Fix, Neuimport heilt) in
[`KONTEXT_Importkodierung_ANSI.md`](KONTEXT_Importkodierung_ANSI.md).

## Brauchwasser / TWW-Profile

Der Brauchwasserkatalog wurde am 02.08.2026 um 11 Wochen-Stundenprofile und 13 Monatswertsätze nach
VDI 6002 erweitert. Alles dazu — Datenmodell, sämtliche Zahlenwerte, Herleitung, Werkzeugkette zum
Bearbeiten der `.accdb` ohne Access und die offene Migrationsbaustelle — steht in
[`KONTEXT_Brauchwassertypen_VDI6002.md`](KONTEXT_Brauchwassertypen_VDI6002.md).

Bekannte Inkonsistenz: `BrauchwasserCtrl.Insert()/Update()` schreiben auf `M1…M12`, gelesen wird
`Monat_n`.

## Grundlagen- und Konzeptdokumente

Normen und Auswertungen liegen als `Grundlagen_*.md` in der Wurzel (TWW-Zapfprofile,
DIN EN 12831-3, VDI 4655). Konzepte zu Bericht, Wirtschaftlichkeit und Variantenvergleich stehen in
`WindowsFormsApplication1/Allgemein/Reporting/`, das Lizenzierungskonzept als
`EPOS-Plan_Konzept_Lizenzierung.md` in der Wurzel.
