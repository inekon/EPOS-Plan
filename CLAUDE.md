# WP-Plan / EPOS-Plan — Projektkontext

.NET Framework 4.8, C#, WinForms (MDI). Rechenkern liegt nativ in `BHKWPLAN.DLL`, eingebunden über
den Out-of-Proc-COM-Server `CSExeCOMServer` — alle Signaturen sind fest auf 8760 Stunden, 168
Wochenwerte, 365 Tage bzw. 12 Monate ausgelegt. Datenhaltung in `Kenndaten.accdb` (Access/ACE);
Kataloge und Projektdaten liegen in derselben Datei, eine Projektdatei gibt es nicht.

## Datenzugriff

Zwei parallele Schichten: die ältere über `Program.DBConnection` (ODBC, DSN `TEST`) mit dem Wrapper
`Allgemein/RecordSet.cs` und stringkonkateniertem SQL, die neuere über `Allgemein/DataRepository.cs`
(OLE DB, `?`-Parameter). **Neuer Code ausschließlich über `DataRepository`.** Verknüpfungen laufen
vielfach über Textfelder (`Bezeichner`, `Typname`) statt über IDs — bei neuen Beziehungen IDs
verwenden.

## Namenskonventionen im Schema

`Tab_*` sind Stamm- und Projektdaten, `Tab_*_STAMM` der Auslieferungskatalog, `Z_*` die Zuordnung
Projekt ↔ Katalogobjekt, `Abfrage_*` gespeicherte Access-Abfragen. Das Feld `ReadOnly` in den
`_STAMM`-Tabellen bedeutet faktisch „gehört zur Auslieferung": Das Migrationsskript behält
`ReadOnly = TRUE` aus der Vorlage und ersetzt alles Übrige durch die Anwenderdaten.

## Migration

`migration.manuell.sql` in der Repo-Wurzel hat im Auto-Modus Vorrang vor dem generierten Entwurf;
`migration.config.json` steuert nur den Generator. Wer eine `_STAMM`-Tabelle um
Auslieferungsdaten erweitert, muss dieses Skript mitpflegen — sonst sind die Daten nach dem
nächsten Update weg.

## Brauchwasser / TWW-Profile

Der Brauchwasserkatalog wurde am 02.08.2026 um 11 Wochen-Stundenprofile und 13 Monatswertsätze nach
VDI 6002 erweitert. Alles dazu — Datenmodell, sämtliche Zahlenwerte, Herleitung, Werkzeugkette zum
Bearbeiten der `.accdb` ohne Access und die offene Migrationsbaustelle — steht in
[`KONTEXT_Brauchwassertypen_VDI6002.md`](KONTEXT_Brauchwassertypen_VDI6002.md).

## Umgang mit der Datenbank

Vor jedem Schreibzugriff prüfen, ob `Kenndaten.laccdb` existiert (dann ist die DB geöffnet), und
vorher eine datierte Kopie anlegen. `C:\ProgramData\EPOS_PLAN` erlaubt normalen Benutzern nur das
Anlegen neuer Dateien, nicht das Ändern vorhandener — eine vom Installer angelegte
`Kenndaten.accdb` ist deshalb schreibgeschützt, bis sie einmal über „Komprimieren und reparieren"
neu geschrieben wurde.

## Bekannte Altlasten

`BrauchwasserCtrl.Insert()/Update()` schreiben auf `M1…M12`, gelesen wird `Monat_n`.
`Views/Brauchwasser/` hat als einziger View-Ordner keine Lokalisierungs-resx. `ChartManager.cs`,
`ChartManagerNeu.cs` und `Form_ChartZoom.cs` existieren doppelt in `Allgemein/Chart/` und
`Allgemein/GrafikTools/` — vor Wiederverwendung anhand der `.csproj`-Compile-Items klären, welche
Kopie gebaut wird. Etliche `.cs`-Dateien sind nicht UTF-8 kodiert (Umlaute als Ersatzzeichen).
