# WP-Plan / EPOS-Plan — Projektkontext

Windows-Desktop-Anwendung zur Planung und Simulation von Energie- und Wärmeversorgungskonzepten
(Wärmebedarf, Brauchwasser, Prozesswärme, Heizkessel, BHKW, Wärmepumpe, Solarthermie, Photovoltaik,
Speicher, Klimadaten) mit Wizard-Workflow, Herstellerdaten-Import, Simulation, Berichten und
Wirtschaftlichkeitsrechnung.

Diese Datei beschreibt **Fachdomäne, Datenmodell, Migration und Umgang mit der Datenbank**.
Alles zu Code, Build und Architektur steht in
[`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md).

Der **Rechenkern liegt seit dem 03.09.2026 (Paket iU4) in einem eigenen Projekt**
[`EPOS.Kern`](EPOS.Kern/CLAUDE.md) — 168 Dateien, `net10.0` **ohne** WinForms: Simulation,
Wirtschaftlichkeit, Modelle, Zugriffsschicht und die Daten-Hälfte des Berichts. Die
Windows-Anwendung referenziert das Projekt und übersetzt diese Dateien nicht mehr. **Eine
Fachänderung am Rechenkern wird dort gemacht, nicht in `WindowsFormsApplication1/`.**

C#, `net10.0-windows` (Anhebung am 02.09.2026, Paket iU1), WinForms (MDI), Build zwingend
**x64**. Bis 22.08.2026 x86; Umstellungsplan, offene Pakete und Rückweg
(Git-Tag `letzter-x86-stand`) in
[`Konzept_Umstellung_64Bit_EPOS-Plan.md`](Konzept_Umstellung_64Bit_EPOS-Plan.md).

Die Datenhaltung ist seit dem 02.09.2026 **SQLite** (`Kenndaten.sqlite`, siehe
[`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md)); die native Bibliothek bringt
`Microsoft.Data.Sqlite` mit. Die Access-Engine (ACE OLEDB, 64-Bit-Fassung) wird nur noch für die
**Erststart-Migration vorhandener `.accdb`-Bestände** gebraucht — für einen Neustand ist sie
nicht mehr erforderlich.


## Datenhaltung

> Dieser Abschnitt beschreibt den Stand **vor** der SQLite-Umstellung vom 02.09.2026. Er gilt
> weiterhin für Altbestände und für das Verständnis des Schemas; der laufende Betrieb steht in
> [`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md).

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


## Brauchwasser / TWW-Profile

Der Brauchwasserkatalog wurde am 02.08.2026 um 11 Wochen-Stundenprofile und 13 Monatswertsätze nach
VDI 6002 erweitert. Alles dazu — Datenmodell, sämtliche Zahlenwerte, Herleitung, Werkzeugkette zum
Bearbeiten der `.accdb` ohne Access und die offene Migrationsbaustelle — steht in
[`KONTEXT_Brauchwassertypen_VDI6002.md`](KONTEXT_Brauchwassertypen_VDI6002.md).


## Grundlagen- und Konzeptdokumente

Nehme aktuelle .md Dateien jeweils zur bearbeteten Thematik. Teilweise gibt es ältere .MD Dateien, die nur teilweise noch Gültigkeit haben.
Lizenzierungskonzept als
`EPOS-Plan_Konzept_Lizenzierung.md` in der Wurzel.
