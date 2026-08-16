# Konzept: Installationsprogramm für EPOS-Plan mit Inno Setup

**Fassung 1** · Stand 16.08.2026 · Status: Entwurf zur Umsetzung
Bezug: `EPOS-Plan_Konzept_Lizenzierung.md`, `BETRIEB_Mehrbenutzer_Datenbank.md`,
ADR-001 (Schema-Ausrollung, umgesetzt in `Allgemein/Update/SchemaMigration.cs`)
Codebasis: verifiziert am Stand 16.08.2026 (`WindowsFormsApplication1.csproj`,
`Program.cs`, `Allgemein/DataRepository.cs`, `bin\x86\Release\net8.0-windows`)
Vorlage: `BHKWPlan_Vollversion_1.25.000.iss` (Setup des Vorgängerprodukts)

Mitgeliefert: `EPOS-Plan.iss` (lauffähiges Setup-Skript) und `build-setup.ps1`
(Veröffentlichung und Übersetzung in einem Aufruf).

---

## 1. Ausgangslage

### 1.1 Das Setup des Vorgängers

Das vorliegende Skript beschreibt BHKW-WP-Plan — ein Excel-Produkt: eine `.XLSM`
als Programm, ein Dutzend `.XLS`-Datenbanken, eine native `BHKWPLAN.DLL`, dazu
ein nachgelagerter Miniconda-Bootstrap für `xlwings`. Fast nichts davon trägt
noch.

| Bestandteil des alten Skripts | Für EPOS-Plan |
|---|---|
| `.XLSM` als Startobjekt, Excel als Laufzeit | entfällt — eigenständige `.exe` |
| `BHKWPLAN.DLL`, `bhkwplan.GID`, COM-Server | entfällt — Rechenkern liegt verwaltet in `Allgemein/BhkwPlan.cs` |
| Miniconda + `python.bat` + `fehler.txt`-Auswertung | entfällt — kein Python mehr |
| `checkExcel` blockiert die Installation, wenn Excel läuft | entfällt — Excel wird nicht mehr überschrieben (siehe 5.4) |
| `DefaultDirName={sd}\BHKW-WP-Plan` (Wurzel des Systemlaufwerks) | ersetzt durch `{autopf}` — die Wurzel war nur ein Umweg um den Schreibschutz von „Programme" |
| Datenordner `{app}\PROJEKTE`, `{app}\Klima` … neben dem Programm | ersetzt — Programm und Daten werden getrennt (Abschnitt 2) |
| Absolute Quellpfade `Z:\70-Material_Informationen\…` | ersetzt durch Pfade relativ zum Skript — das Setup baut auf jedem Rechner, nicht nur auf einem |
| Deutschsprachiger Assistent, `LicenseFile`, `InfoBeforeFile`, Desktopsymbol | **übernommen** |
| `AppId` als GUID, `ignoreversion`, `onlyifdoesntexist` | **übernommen**, mit neuer GUID |

Ein Punkt daraus verdient Beachtung, weil er auch in Zukunft trägt: Das alte
Skript hat die Installation **abgebrochen**, wenn eine Voraussetzung fehlte
(`checkInstall` → stille Deinstallation). Das ist eine Haltung, keine Technik —
und sie ist richtig. EPOS-Plan bricht nur nicht mehr ab, weil die verbliebene
Voraussetzung (der Datenbanktreiber) nachträglich von Hand nachgerüstet werden
kann, ohne die Installation zu wiederholen.

### 1.2 Was EPOS-Plan heute technisch ist

| Merkmal | Stand | Folge fürs Setup |
|---|---|---|
| Zielframework | `net8.0-windows`, WinForms **und** WPF | Laufzeit muss vorhanden sein oder mitkommen (E1) |
| Plattform | zwingend **x86** — `Microsoft.ACE.OLEDB.12.0` ist bitness-gebunden | Installation nach `Programme (x86)`, kein 64-Bit-Modus |
| Ausgabe heute | framework-abhängig, ≈ 40 MB in `bin\x86\Release\net8.0-windows` | reicht als Auslieferungsstand nicht |
| Datenhaltung | **eine** Datei `Kenndaten.accdb`, 92 MB, Kataloge **und** Projektdaten | Kernfrage des Setups (Abschnitt 6) |
| Datenbanktreiber | ACE OLEDB 12.0, 32 Bit; zusätzlich ODBC in `RecordSet.cs` | eine Voraussetzung, zwei Zugriffswege |
| Schemapflege | `SchemaMigration.Ausfuehren()` beim Start (ADR-001) | das Setup migriert **nicht** — die Anwendung tut es |
| Lizenz | Aktivierung im Programm, Token per DPAPI im Benutzerprofil | Setup liefert nur den beschreibbaren Ablageort |
| Eigene Update-Funktion | **keine** — `Allgemein/Update/` enthält die Schema-, nicht die Programmaktualisierung | das Setup ist der einzige Update-Weg |
| Zusatzdateien im Programmordner | `Vorlagen\Berichtsvorlage.docx`, Satelliten `de-DE`/`en-US`, `runtimes\` (SkiaSharp, HarfBuzz) | müssen vollständig mit — deshalb `dotnet publish`, nicht Handauswahl |

---

## 2. Zielbild

### 2.1 Verzeichnisse und Rechte

| Ort | Inhalt | Rechte | Wer schreibt |
|---|---|---|---|
| `%ProgramFiles(x86)%\EPOS-Plan` | Programm, Laufzeit, Satelliten, `Vorlagen\`, `runtimes\` | Standard (Benutzer: nur lesen) | nur das Setup |
| `…\EPOS-Plan\Vorlage\Kenndaten.accdb` | Auslieferungsdatenbank, unverändert | Standard | nur das Setup |
| `%LOCALAPPDATA%\EPOS_PLAN` | **Arbeitsdatenbank des Kontos**, Protokolle | Konto hat Vollzugriff | die Anwendung |
| `%LOCALAPPDATA%\EPOS-Plan\…\user.config` | Einstellungen (`DBPath`, `WordPressUrl` …) | Konto | .NET-Einstellungssystem |
| `%ProgramData%\EPOS_PLAN` | leer im Regelbetrieb; Ablage für die Betriebsart „gemeinsame Datenbank" und für Altbestände | Gruppe *Benutzer*: ändern (vererbend) | Anwendung, wenn ausdrücklich konfiguriert |
| `HKCU\Software\wp-plan` | Sprache, KI-Schlüssel | Konto | die Anwendung |
| `HKLM\SOFTWARE\INEKON\EPOS-Plan` | `InstallDir`, `Version` — für Support | Standard | nur das Setup |

Der Bruch mit dem heutigen Verhalten liegt in Zeile 3: Die Arbeitsdatenbank
zieht von `%ProgramData%` in das Benutzerprofil. Damit verschwinden zwei
Befunde auf einen Schlag, die bisher jeder Installation angehängt haben — die
frisch installierte Datenbank ist sofort beschreibbar (kein „Komprimieren und
reparieren" mehr), und ein zweites Windows-Konto kann EPOS-Plan öffnen, während
das erste läuft (`BETRIEB_Mehrbenutzer_Datenbank.md`).

Der Preis ist ebenso klar zu benennen: **Jedes Konto hat seine eigenen
Projekte.** Wer heute bewusst mit einer gemeinsamen Datenbank arbeitet — zwei
Konten am selben Rechner, dieselben Projekte — verliert das. Für diesen Fall
bleibt der bestehende Weg offen: `Properties.Settings.Default.DBPath` in den
Admin-Einstellungen auf einen gemeinsamen Ordner setzen. Der Ordner
`%ProgramData%\EPOS_PLAN` wird deshalb weiter angelegt und mit
Änderungsrechten für die Gruppe *Benutzer* versehen (E6).

### 2.2 Erstinstallation

1. Assistent: Sprache → Willkommen → Lizenz → Liesmich → Zielordner → Aufgaben
2. Liegt bereits eine `%ProgramData%\EPOS_PLAN\Kenndaten.accdb`, erscheint eine
   zusätzliche Hinweisseite: die Projekte werden beim ersten Start übernommen,
   das Setup selbst rührt nichts an
3. Dateien kopieren
4. Fehlt `Microsoft.ACE.OLEDB.12.0` (32 Bit): `AccessDatabaseEngine.exe /quiet`,
   danach Gegenprüfung (5.1)
5. Bestand `%ProgramData%\EPOS_PLAN` schon vorher, Rechte darauf mit `icacls`
   reparieren
6. Verknüpfungen, Registry-Eintrag
7. Angebot, EPOS-Plan zu starten

Beim **ersten Programmstart** — nicht im Setup — legt die Anwendung ihre
Arbeitsdatenbank an (6.2) und führt anschließend die Schemamigration aus.

### 2.3 Update

Gleiche Datei, gleiche `AppId`. Inno erkennt die Vorgängerversion, schlägt
deren Zielordner vor und ersetzt die Programmdateien. Läuft EPOS-Plan noch,
bietet der Restart Manager das Schließen an, statt mit „Datei in Benutzung"
abzubrechen.

Die Arbeitsdatenbank wird **nicht angefasst**. Neue Katalogeinträge und
Schemaänderungen kommen ausschließlich über `SchemaMigration` beim nächsten
Start. Das ist die einzige Stelle des Konzepts, die ohne Alternative ist: Ein
Setup, das eine 92-MB-Datei mit Kundenprojekten überschreibt, ist ein
Datenverlust mit Ansage.

### 2.4 Deinstallation

Programmdateien und Verknüpfungen verschwinden. Die Deinstallation fragt
einmal, ob die Datenbank des angemeldeten Kontos mitgelöscht werden soll —
Vorgabe *Nein*. Daten anderer Konten bleiben grundsätzlich liegen; das steht
so auch im Meldungstext.

---

## 3. Entscheidungen

| Nr. | Entscheidung | Begründung |
|---|---|---|
| **E1** | **Eigenständige Veröffentlichung** (`--self-contained`, `win-x86`) | Kein Kunde muss die .NET-8-Desktop-Laufzeit x86 beschaffen, keine Abhängigkeit von Update-Ständen, keine Fehlersuche „warum startet es auf dem einen Rechner nicht". Preis: Nutzlast ≈ 350 MB statt 40 MB. Nach lzma2 erfahrungsgemäß 120–180 MB Setup-Datei — **vor der ersten Auslieferung messen** |
| **E2** | **Arbeitsdatenbank je Windows-Konto** unter `%LOCALAPPDATA%\EPOS_PLAN` | Löst Schreibschutz- und Mehrbenutzerbefund an der Wurzel statt per Rechte-Reparatur. Gemeinsamer Betrieb bleibt über `DBPath` möglich |
| **E3** | **Auslieferungsdatenbank als Vorlage** unter `{app}\Vorlage`, nie direkt benutzt | Der Auslieferungsstand bleibt unverändert und nachvollziehbar; Erstkopie und Bestandsübernahme entscheidet die Anwendung, die den Kontext kennt |
| **E4** | **Maschinenweite Installation** (`PrivilegesRequired=admin`) | Ein Programmstand für alle Konten; Voraussetzungsinstallation und Rechtevergabe brauchen ohnehin erhöhte Rechte |
| **E5** | **ACE geprüft installieren, Fehlschlag melden, nicht abbrechen** | Der Treiber lässt sich nachrüsten, ohne die Installation zu wiederholen. Ein Abbruch nähme dem Anwender das bereits installierte Programm |
| **E6** | **Rechte am gemeinsamen Ordner über `[Dirs] Permissions` und — bei Altbestand — `icacls /T`** | `[Dirs]` setzt vererbende Rechte am Ordner; bestehende Dateien mit unterbrochener Vererbung erreicht nur `icacls` |
| **E7** | **Ein Setup, keine Produktvarianten** | Demo und Vollversion unterscheiden sich ausschließlich im Lizenz-Token. Zwei Setups zu pflegen brächte nichts als zwei Fehlerquellen |
| **E8** | **Version einzig aus `AssemblyInfo.cs`** | Setup-Dateiname, Softwareliste, Registry und `Hilfe → Info` zeigen zwangsläufig denselben Stand |
| **E9** | **Klimadaten nicht im Setup** | Rund 330 `.xls` mit etwa 300 MB verdoppelten das Setup. Offen ist, wie sie stattdessen zum Anwender kommen — siehe Abschnitt 11 |

---

## 4. Aufbau des Skripts

`Setup\EPOS-Plan.iss`, Inno Setup **6.3 oder neuer** (davor gibt es weder den
Architekturbezeichner `x86compatible` noch UTF-8 ohne BOM).

| Abschnitt | Inhalt |
|---|---|
| Präprozessor | Alle Pfade relativ zu `AddBackslash(SourcePath)`. Version über `GetVersionNumbersString` aus der gebauten EXE; fehlt sie, bricht die Übersetzung mit `#error` ab statt ein Setup mit leerer Version zu erzeugen |
| `[Setup]` | `AppId` als feste GUID, `{autopf}`, `PrivilegesRequired=admin`, `ArchitecturesAllowed=x86compatible`, `MinVersion=10.0`, `CloseApplications=yes` |
| `[Languages]` | Deutsch und Englisch — passend zur zweisprachigen Oberfläche |
| `[CustomMessages]` | Alle eigenen Texte zweisprachig, keine Zeichenkette im Code |
| `[Tasks]` | Desktopsymbol |
| `[Dirs]` | `%ProgramData%\EPOS_PLAN` mit `Permissions: users-modify` |
| `[Files]` | Veröffentlichungsordner rekursiv (ohne `*.pdb`, `*.xml`), Vorlagendatenbank, ACE-Installer nach `{tmp}` — letzterer nur, wenn er gebraucht wird |
| `[Icons]` | Startmenü, Web-Verknüpfung, Deinstallation, optional Desktop |
| `[Registry]` | `HKLM32\SOFTWARE\INEKON\EPOS-Plan`: `InstallDir`, `Version` |
| `[Run]` | ACE-Installation mit Gegenprüfung, `icacls` bei Altbestand, Programmstart anbieten |
| `[UninstallDelete]` | Gezielt: Protokolle, `Vorlage\`, dann `dirifempty` auf `{app}`. **Kein** pauschales Löschen des gewählten Ordners |
| `[Code]` | `AceVorhanden`, `AceNachpruefen`, Zustandsaufnahme in `InitializeSetup`, Hinweisseite, Rückfrage bei der Deinstallation |

Zwei Feinheiten, die beim Ändern leicht kippen:

- **`ArchitecturesAllowed=x86compatible`.** Seit Inno Setup 6.3 bedeutet das
  frühere `x86` nicht mehr „x86-fähig", sondern `x86os` — *natives*
  32-Bit-Windows. Wer die Zeile auf `x86` zurückstellt, sperrt das Setup auf
  allen 64-Bit-Rechnern aus.
- **Zustandsaufnahme in `InitializeSetup`.** `[Dirs]` läuft vor `[Run]` und legt
  `%ProgramData%\EPOS_PLAN` an. Prüfte die `Check`-Funktion des `icacls`-Laufs
  erst dort, wäre die Antwort immer „vorhanden". Deshalb wird der Zustand vor
  der Installation festgehalten.

---

## 5. Voraussetzungen und Prüfungen

### 5.1 Microsoft Access Database Engine, 32 Bit

Die einzige echte Voraussetzung. Geprüft wird die Registrierung der ProgID
`Microsoft.ACE.OLEDB.12.0` in der 32-Bit-Sicht — genau die Kennung, die
`DataRepository.GetConnectionString()` anfordert. Ein vorhandenes
`Microsoft.ACE.OLEDB.16.0` allein genügt nicht.

Fehlt sie, läuft das mitgelieferte `AccessDatabaseEngine.exe /quiet`. Danach
wird **erneut geprüft** — und hier liegt die bekannte Falle:

> **64-Bit-Office blockiert die 32-Bit-Engine.** Ist auf dem Rechner ein
> 64-Bit-Microsoft-Office installiert, verweigert das Redistributable die
> Installation. Der Anwender sieht nichts davon (`/quiet`), und EPOS-Plan
> scheitert später am ersten Datenbankzugriff.

Deshalb die Gegenprüfung mit sprechender Meldung. Der Weg für den Supportfall
gehört in die Liesmich-Datei: Engine mit `/extract:<Ordner>` entpacken und die
enthaltene `AceRedist.msi` per `msiexec /i … /qn` installieren; auf manchen
Ständen ist zusätzlich der Registry-Wert
`HKLM\SOFTWARE\Microsoft\Office\<Version>\Common\FilesPaths\mso.dll` für die
Dauer der Installation zu entfernen.

> **Vor der ersten Auslieferung einmal verifizieren:** Das
> `AccessDatabaseEngine.exe` in der Repowurzel (26,5 MB) muss die **2016er**
> Fassung sein — die 2010er ist außer Support. Beide registrieren die ProgID
> 12.0, die Prüfung stimmt also in jedem Fall; die Unterstützungslage nicht.

### 5.2 Laufende Instanz

`CloseApplications=yes` lässt den Restart Manager offene Instanzen erkennen und
anbieten, sie zu schließen. Das greift zuverlässig erst, wenn die Anwendung
einen benannten Mutex setzt — dann genügt `AppMutex=Global\EPOS-Plan` (7.4).
Die Zeile ist im Skript vorbereitet und auskommentiert.

### 5.3 Betriebssystem

`MinVersion=10.0`. Für eine .NET-8-Anwendung ist das die untere Grenze, die
Microsoft selbst zieht.

### 5.4 Was bewusst **nicht** geprüft wird

Der Excel-Blocker des Vorgängers entfällt. Das alte Setup überschrieb eine
`.XLSM`, die Excel geöffnet halten konnte. EPOS-Plan nutzt Excel nur noch über
COM-Interop für Import und Export — eine laufende Excel-Sitzung stört die
Installation nicht. Die Prüfung zu übernehmen hieße, Anwender ohne Grund
auszusperren.

---

## 6. Die Datenbank

### 6.1 Den Auslieferungsstand erzeugen

**Die Datenbank im Repository darf nicht ausgeliefert werden.**
`WindowsFormsApplication1\Kenndaten.accdb` ist 92 MB groß und enthält reale
Projekte aus der Entwicklung — also Kunden- und Objektdaten. Sie in ein Setup
zu packen, das an Dritte geht, wäre eine Datenpanne.

Der Auslieferungsstand liegt getrennt unter `Setup\Vorlage\Kenndaten.accdb` und
entsteht in vier Schritten:

1. Kopie der produktiven Datenbank ziehen (vorher prüfen, ob `Kenndaten.laccdb`
   existiert — dann ist sie geöffnet)
2. Alle Projektdaten löschen. Die Löschweitergaben tragen das meiste mit: Ein
   `DELETE FROM Tab_Projekt` räumt über die 68 Beziehungen mit `DEL-CASCADE`
   die abhängigen Tabellen ab. Die dokumentierten Ausnahmen —
   `Tab_Pufferspeicher` hängt **nicht** an der Projektkaskade, `ID_PUFFER` hat
   **keine** Beziehung — sind einzeln nachzuziehen
3. In den `*_STAMM`-Tabellen behalten, was `ReadOnly = TRUE` trägt; das ist
   laut Namenskonvention genau der Auslieferungskatalog
4. „Komprimieren und reparieren", dann die Datei schreibgeschützt ablegen

Dieser Schritt ist **noch nicht automatisiert** und gehört als Skript in
`Setup\Vorlage\` (Aufwandsschätzung in Abschnitt 12). Bis dahin ist er von Hand
zu gehen und das Ergebnis vor jeder Auslieferung gegenzuprüfen: Projektliste
leer, Katalogzahlen plausibel, Dateigröße deutlich unter 92 MB.

Das Build-Skript bricht ab, wenn `Setup\Vorlage\Kenndaten.accdb` fehlt — und
greift bewusst **nicht** ersatzweise auf die Arbeitsdatenbank zurück.

### 6.2 Erstkopie und Übernahme des Bestands

Die Anwendung entscheidet, welche Datenbank sie benutzt — nicht das Setup. Der
Vorschlag für `DataRepository`:

```csharp
public static string GetDBPath()
{
    // Ausdrücklich konfigurierter Ordner (Admin-Einstellungen) hat Vorrang.
    // Das ist zugleich die Betriebsart "gemeinsame Datenbank".
    string ordner = Properties.Settings.Default.DBPath;
    if (!string.IsNullOrWhiteSpace(ordner))
        return Path.Combine(ordner, DB_DATEINAME);

    string benutzerOrdner = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EPOS_PLAN");
    string ziel = Path.Combine(benutzerOrdner, DB_DATEINAME);

    if (!File.Exists(ziel))
        DatenbankBereitstellen(benutzerOrdner, ziel);

    return ziel;
}

private static void DatenbankBereitstellen(string ordner, string ziel)
{
    Directory.CreateDirectory(ordner);

    // 1. Bestand aus der bisherigen gemeinsamen Ablage übernehmen -
    //    der Anwender behält seine Projekte.
    string alt = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "EPOS_PLAN", DB_DATEINAME);

    // 2. sonst die Auslieferungsvorlage neben dem Programm.
    string vorlage = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Vorlage", DB_DATEINAME);

    string quelle = File.Exists(alt) ? alt : vorlage;
    if (!File.Exists(quelle))
        throw new FileNotFoundException(
            "Weder Bestandsdatenbank noch Auslieferungsvorlage gefunden.", quelle);

    // Über eine Zwischendatei, damit ein Abbruch keine halbe
    // Datenbank hinterlässt, die beim nächsten Start als gültig gilt.
    string zwischen = ziel + ".neu";
    File.Copy(quelle, zwischen, true);
    File.SetAttributes(zwischen, File.GetAttributes(zwischen) & ~FileAttributes.ReadOnly);
    File.Move(zwischen, ziel);
}
```

Drei Punkte dazu:

- **Reihenfolge.** Das muss vor `SchemaMigration.Ausfuehren()` in `Program.Main`
  greifen. Da die Migration ihren Pfad laut Entscheidung 13.2 des
  Simulationskonzepts über `DataRepository.GetDBPath()` bezieht, geschieht das
  von selbst — beim Umbau dieser Stelle aber mit prüfen.
- **Dauer.** 92 MB kopieren dauert je nach Datenträger einige Sekunden. Der
  erste Start braucht deshalb einen sichtbaren Hinweis, sonst wirkt das Programm
  hängengeblieben.
- **Der Altbestand bleibt liegen.** Bewusst: Erst wenn der Anwender bestätigt
  hat, dass seine Projekte da sind, darf die alte Datei weg. Das gehört in die
  Liesmich-Datei, nicht in eine automatische Löschung.

### 6.3 Was das Setup mit der Datenbank nie tut

Es überschreibt sie nicht, es migriert sie nicht, es löscht sie nicht (außer auf
ausdrückliche Rückfrage bei der Deinstallation, und dann nur die des
angemeldeten Kontos). Alles Weitere macht die Anwendung, die den Schema- und
Lizenzzustand kennt.

---

## 7. Nötige Änderungen an der Anwendung

| Nr. | Änderung | Notwendig? | Aufwand |
|---|---|---|---|
| 7.1 | `DataRepository.GetDBPath` auf `%LOCALAPPDATA%` mit Erstkopie und Bestandsübernahme (6.2) | **Pflicht** — ohne sie greift E2 nicht | 0,5 PT |
| 7.2 | Wartehinweis beim ersten Start während der Erstkopie | **Pflicht** | 0,25 PT |
| 7.3 | `AssemblyName` von `WindowsFormsApplication1` auf `EPOS-Plan` | empfohlen | 0,5 PT + Regressionsprobe |
| 7.4 | Benannter Mutex `Global\EPOS-Plan` beim Start | empfohlen | 0,25 PT |
| 7.5 | Versionsnummer in `Properties\AssemblyInfo.cs` pflegen | **Pflicht** | — |
| 7.6 | `<ApplicationIcon>` im Projekt setzen | empfohlen | 0,1 PT |
| 7.7 | `Settings.Default.Upgrade()` beim Versionswechsel — **prüfen, ob vorhanden** | zu klären | 0,25 PT |

**7.3 Assembly- und Dateiname.** Die ausführbare Datei heißt heute
`WindowsFormsApplication1.exe`. Im Startmenü, im Taskmanager, in der
Softwareliste und in Virenschutz-Meldungen steht damit ein Name, der nicht zum
Produkt gehört. Die Umbenennung über `<AssemblyName>` zieht Satellitendateien,
`deps.json`, `runtimeconfig.json` und `.dll.config` automatisch mit; der
Abschnittsname in `app.config` (`WindowsFormsApplication1.Properties.Settings`)
bleibt unberührt, weil er aus dem Namensraum stammt, nicht aus dem
Assemblynamen. Zwei Nebenwirkungen sind einzuplanen: Der Ordner der
Benutzereinstellungen unter `%LOCALAPPDATA%` ändert sich — die Anwender
verlieren einmalig ihre gespeicherten Einstellungen — und Code, der über
`Assembly.GetName().Name` oder `Application.ProductName` sucht, ist
durchzusehen. Deshalb: sinnvoll, aber als eigener Schritt mit eigener Probe,
nicht nebenbei. Im Setup-Skript ist dafür nur `#define AppExeName` zu ändern.

**7.5 Version.** Das Projekt setzt `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`;
`-p:Version` an `dotnet publish` bleibt damit **wirkungslos**. Einzige Quelle ist
`AssemblyFileVersion` in `Properties\AssemblyInfo.cs`. Von dort liest das
Setup-Skript sie aus der gebauten EXE. Das Build-Skript bricht ab, wenn dort
`0.0.0.0` steht.

**7.7 Einstellungen über Versionsgrenzen.** .NET legt `user.config` je
Assemblyversion getrennt ab. Ohne einen `Settings.Default.Upgrade()`-Aufruf beim
ersten Start einer neuen Version verlieren Anwender bei **jedem** Update ihre
Einstellungen — bei E2 unter anderem einen bewusst gesetzten `DBPath`. Ob der
Aufruf vorhanden ist, wurde nicht geprüft; er gehört mit in dieselbe Runde wie
7.1.

---

## 8. Build- und Freigabekette

```
Setup\
  EPOS-Plan.iss                        Setup-Skript (versioniert)
  build-setup.ps1                      Veröffentlichen + Übersetzen
  EPOS-Plan.ico                        aus Resources\wpplan.ico ableiten
  Lizenz.rtf                           Lizenzvereinbarung
  Liesmich.rtf                         Neuerungen, ACE-Supportfall, DB-Übernahme
  Vorlage\Kenndaten.accdb              Auslieferungsstand (NICHT versionieren)
  Voraussetzungen\AccessDatabaseEngine.exe
  Ausgabe\                             Ergebnis (NICHT versionieren)
```

Nach `.gitignore`: `Setup/Ausgabe/`, `Setup/Vorlage/*.accdb`,
`Setup/Voraussetzungen/*.exe`, `artifacts/`.

Ein Durchlauf:

```powershell
cd C:\Waermeplan\WP_Plan\Setup
.\build-setup.ps1
```

Das Skript veröffentlicht eigenständig nach `artifacts\publish\win-x86`, prüft
Vorlagendatenbank, Voraussetzungs-Installer und Inno-Setup-Version, übersetzt
und meldet Pfad und Größe. `-SkipPublish` überspringt den Bau, `-Schnell`
schaltet auf `lzma2/normal` für Testläufe.

**Freigabeprobe vor jeder Auslieferung** — auf einer frischen
Windows-Installation, nicht auf dem Entwicklungsrechner:

1. Erstinstallation ohne ACE → Treiber wird installiert, Programm startet,
   Datenbank entsteht im Profil
2. Erstinstallation mit 64-Bit-Office → Meldung erscheint, Installation läuft
   durch
3. Update über eine Vorgängerversion → Projekte bleiben, Schemamigration läuft
4. Installation auf einem Rechner mit Altbestand in `%ProgramData%` → Hinweisseite
   erscheint, Projekte werden übernommen
5. Zweites Windows-Konto startet EPOS-Plan, während das erste läuft
6. Deinstallation mit und ohne Datenlöschung
7. `dotnet list package --include-transitive` — Lizenzprüfung, insbesondere die
   Bindung von `SixLabors.Fonts` auf 1.0.x

Eine Automatisierung über GitHub Actions ist möglich (`windows-latest` bringt
das .NET-SDK mit, Inno Setup ist per `choco install innosetup` nachrüstbar),
scheitert aber vorerst an der Vorlagendatenbank: 92 MB Binärdatei mit
Kundenbezug gehören nicht in ein Repository. Solange dieser Schritt manuell ist,
bleibt die Kette es auch.

---

## 9. Code-Signierung

Ohne Signatur zeigt Windows bei jedem Start des Setups den SmartScreen-Filter
mit „Unbekannter Herausgeber". Für ein Produkt, das an Ingenieurbüros und
Stadtwerke geht — oft über IT-Abteilungen mit Softwarefreigabe — ist das ein
echtes Vertriebshindernis, kein Schönheitsfehler.

Nötig ist ein Codesignaturzertifikat auf INEKON. Seit Juni 2023 verlangen die
Zertifizierungsstellen, dass der private Schlüssel auf einem Hardwaretoken oder
in einem HSM liegt; reine Dateizertifikate gibt es nicht mehr. Ein
OV-Zertifikat lässt den SmartScreen-Ruf über die ersten Auslieferungen langsam
aufbauen, ein EV-Zertifikat wirkt sofort.

Im Skript ist beides vorbereitet: `SignTool=signtool` und
`SignedUninstaller=yes` in `[Setup]` (auskommentiert), `-Sign -Thumbprint …` im
Build-Skript. Signiert werden sollte **beides** — die Anwendung vor dem
Verpacken und das Setup danach.

---

## 10. Was das Setup bewusst nicht tut

- **Keine Datenbankmigration.** Die macht die Anwendung beim Start (ADR-001).
- **Keine Lizenzaktivierung.** Sie läuft im Programm gegen epos-plan.de. Das
  Setup stellt nur sicher, dass die Token-Ablage im Benutzerprofil beschreibbar
  ist — was sie dort ohnehin ist.
- **Keine Dateiverknüpfung für Lizenzdateien.** Wäre Komfort (Doppelklick öffnet
  den Aktivierungsdialog), setzt aber eine Codeänderung voraus — `Program.Main`
  nimmt heute keine Argumente entgegen. Jederzeit nachrüstbar, ohne am Aufbau
  des Setups etwas zu ändern. Bei der Umsetzung eine eigene Endung wählen
  (`.eposlic`); `.lic` ist nicht geschützt und wird von anderen Programmen belegt.
- **Kein automatischer Programmupdater.** Es gibt keinen im Code, und das Setup
  ersetzt ihn nicht.
- **Keine Klimadaten** (E9).

---

## 11. Offene Punkte

| Nr. | Punkt | Nächster Schritt |
|---|---|---|
| S1 | Wie kommen die Klimadaten zum Anwender? Rund 330 `.xls`, etwa 300 MB. Nachladen aus der Anwendung, eigenes Datenpaket oder doch ins Setup? | Klären, wie die Anwendung sie heute erwartet — Pfad, Zeitpunkt, Pflicht oder Kür |
| S2 | `help_mapping.txt` liegt in der heutigen Release-Ausgabe, ist aber nicht im Projekt eingetragen — bei `dotnet publish` fehlt es | Prüfen, ob die Anwendung es braucht; wenn ja, als `Content` ins `.csproj` |
| S3 | Ist das `AccessDatabaseEngine.exe` in der Repowurzel die 2016er Fassung? | Dateiversion einmal prüfen, gegebenenfalls durch den aktuellen Microsoft-Stand ersetzen |
| S4 | Herausgebername: „INEKON" oder die vollständige Firmierung? Steht in Setup, Softwareliste und später im Zertifikat | Festlegen, danach `#define AppPublisher` |
| S5 | Automatisierte Erzeugung der Auslieferungsdatenbank (6.1) | Skript schreiben; bis dahin Handlauf mit Gegenprüfung |
| S6 | `Settings.Default.Upgrade()` beim Versionswechsel vorhanden? (7.7) | Im Code nachsehen |
| S7 | Wird noch ein 64-Bit-Stand gebraucht? Solange ACE OLEDB 12.0 x86 die Datenhaltung trägt: nein | Bei einem späteren Wechsel des Datenbankzugriffs neu bewerten |

---

## 12. Aufwand

| Paket | Inhalt | PT |
|---|---|---|
| S-1 | Setup-Skript einrichten, Symbol, Lizenz- und Liesmich-Text, erster Übersetzungslauf | 0,5 |
| S-2 | Änderungen an der Anwendung 7.1, 7.2, 7.5, 7.6 | 1,0 |
| S-3 | Auslieferungsdatenbank: Bereinigung festlegen und einmal durchführen (6.1) | 1,0 |
| S-4 | Freigabeprobe auf frischer Windows-Installation, sieben Fälle (Abschnitt 8) | 1,0 |
| S-5 | Assemblyumbenennung 7.3 mit Regressionsprobe | 0,5 |
| S-6 | Mutex 7.4, `Settings.Upgrade()` 7.7 | 0,5 |
| | **Summe ohne Signierung** | **4,5** |
| S-7 | Code-Signierung: Zertifikat beschaffen, Token einrichten, Kette einbauen | 0,5 PT + Beschaffungszeit und Zertifikatskosten |

Die ersten vier Pakete ergeben ein auslieferbares Setup. S-5 bis S-7 heben die
Außenwirkung — und sollten vor der ersten Auslieferung an zahlende Kunden
erledigt sein, nicht danach.
