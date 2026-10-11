# Quellen: Testmaterial

## HottCAD-IFC-Testdateien

Sechs IFC-Exporte der Windows-Anwendung HottCAD, erzeugt aus eigenen Projekten des Anwenders
(Property-Sets `HSETU_*`, Erzeugerkopf „Hottgenroth Model Creator“).

| Datei | Größe |
|---|---|
| `MFH-Klein-unsaniert-1964.ifc` | 3,27 MB |
| `MFH_mittel_1984.ifc` | 3,61 MB |
| `Produktion_groß_mit_Verwaltung_EG55-2026.ifc` | 11,95 MB |
| `Sportheim_1970_unsaniert.ifc` | 6,99 MB |
| `Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc` | 13,95 MB |
| `WG-EH55_Poroton-GModG-2026.ifc` | 3,69 MB |

**Zweck:** Diagnosetests des Gebäudeimports (`EPOS.Kern.Tests/*Quelldateien*Diagnose*Tests.cs`,
`IfcProjektdateiVergleichDiagnoseTests`, `RaumgrundrissKantenTests`). Ohne die Dateien
überspringen sich die Tests.

**Regeln:**

- Die Dateien sind Testmaterial und kein Teil der Auslieferung: Die Setup-Kette
  (`Setup/EPOS-Plan.iss`, `Setup/build-setup.ps1`) nimmt weder den Ordner `Quellen/` noch
  `*.ifc` in Veröffentlichung oder Installer.
- Keine Produktdaten aus den Dateien in Wiki, Papieren oder Katalogen.
- Projektdateien `.sqproj` kommen nie ins Repositorium (`.gitignore`-Regel `Quellen/*.sqproj`);
  ihre Diagnose läuft nur, wenn eine Datei lokal liegt.
