# Konzept: Installationsprogramm für EPOS-Plan mit Inno Setup

**Fassung 2** · Stand 22.08.2026 · Status: umgesetzt
Bezug: `EPOS-Plan_Konzept_Lizenzierung.md`, `BETRIEB_Mehrbenutzer_Datenbank.md`,
ADR-001 (Schema-Ausrollung, umgesetzt in `Allgemein/Update/SchemaMigration.cs`),
[`Konzept_Umstellung_64Bit_EPOS-Plan.md`](../Konzept_Umstellung_64Bit_EPOS-Plan.md)
Codebasis: erstmals verifiziert am Stand 16.08.2026 (`WindowsFormsApplication1.csproj`,
`Program.cs`, `Allgemein/DataRepository.cs`), Bitness nachgezogen am 22.08.2026
(`bin\x64\Release\net8.0-windows`)
Vorlage: `BHKWPlan_Vollversion_1.25.000.iss` (Setup des Vorgängerprodukts)

Mitgeliefert: `EPOS-Plan.iss` (lauffähiges Setup-Skript) und `build-setup.ps1`
(Veröffentlichung und Übersetzung in einem Aufruf).

> **Stand 22.08.2026 — Bitness.** Fassung 1 dieses Konzepts legte EPOS-Plan auf
> **x86** fest, weil `Microsoft.ACE.OLEDB.12.0` an die Prozess-Bitness gebunden
> ist. Mit der x64-Umstellung (Paket P4, Commit `c64716f`) ist das überholt: Die
> Anwendung wird ausschließlich als **x64** gebaut und ausgeliefert, das Setup
> läuft im 64-Bit-Modus, beigelegt wird die 64-Bit-Access-Engine. Alle
> Festlegungen dieses Dokuments sind entsprechend nachgezogen; Herleitung,
> Entscheidungen und Abnahmeplan stehen in
> [`Konzept_Umstellung_64Bit_EPOS-Plan.md`](../Konzept_Umstellung_64Bit_EPOS-Plan.md).

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
| Plattform | zwingend **x64** — `Microsoft.ACE.OLEDB.12.0` ist bitness-gebunden | Installation nach `Programme`, 64-Bit-Modus (`ArchitecturesInstallIn64BitMode`) |
| Ausgabe heute | framework-abhängig, ≈ 40 MB in `bin\x64\Release\net8.0-windows` | reicht als Auslieferungsstand nicht |
| Datenhaltung | **eine** Datei `Kenndaten.accdb`, 92 MB, Kataloge **und** Projektdaten | Kernfrage des Setups (Abschnitt 6) |
| Datenbanktreiber | ACE OLEDB 12.0, 64 Bit — einziger Zugriffsweg (der ODBC-Pfad in `RecordSet.cs` ist seit der OleDb-Umstellung entfallen) | eine Voraussetzung, ein Zugriffsweg |
| Schemapflege | `SchemaMigration.Ausfuehren()` beim Start (ADR-001) | das Setup migriert **nicht** — die Anwendung tut es |
| Lizenz | Aktivierung im Programm, Token per DPAPI im Benutzerprofil | Setup liefert nur den beschreibbaren Ablageort |
| Eigene Update-Funktion | **keine** — `Allgemein/Update/` enthält die Schema-, nicht die Programmaktualisierung | das Setup ist der einzige Update-Weg |
| Zusatzdateien im Programmordner | `Vorlagen\Berichtsvorlage.docx`, Satelliten `de-DE`/`en-US`, `runtimes\` (SkiaSharp, HarfBuzz) | müssen vollständig mit — deshalb `dotnet publish`, nicht Handauswahl |

---

## 2. Zielbild

### 2.1 Verzeichnisse und Rechte

| Ort | Inhalt | Rechte | Wer schreibt |
|---|---|---|---|
| `%ProgramFiles%\EPOS-Plan` | Programm, Laufzeit, Satelliten, `Vorlagen\`, `runtimes\` | Standard (Benutzer: nur lesen) | nur das Setup |
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
4. Fehlt `Microsoft.ACE.OLEDB.12.0` (64 Bit): `AccessDatabaseEngine_X64.exe /quiet`,
   bei vorhandenem 32-Bit-Office vorher ein Hinweisdialog, danach Gegenprüfung (5.1)
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

**Sonderfall Update über eine 32-bit-Installation.** Ein Setup im 64-Bit-Modus
legt seinen Uninstall-Eintrag in der 64-Bit-Registry-Sicht an und installiert
nach `Programme` — eine Installation aus der x86-Ära gilt damit **nicht** als
dieselbe Anwendung. Ohne Gegenmaßnahme blieben zwei Einträge in „Apps und
Features" und zwei Programmordner stehen. Das Setup entfernt die
Vorinstallation deshalb still, bevor es selbst installiert: `PrepareToInstall`
ruft `AlteX86InstallationEntfernen()`, liest die `UninstallString` derselben
`AppId` aus `HKLM32` und führt sie mit `/VERYSILENT /SUPPRESSMSGBOXES
/NORESTART` aus; anschließend wird gewartet, bis deren Registry-Eintrag
verschwindet (höchstens zwei Minuten), und der zurückgebliebene
`HKLM32\SOFTWARE\INEKON\EPOS-Plan` gelöscht. Bewusst in `PrepareToInstall` und
nicht in `InitializeSetup` — erst dort steht fest, dass wirklich installiert
wird; wer den Assistenten vorher abbricht, stünde sonst ganz ohne Programm da.

Die Nutzdaten sind davon nicht berührt: Der alte Deinstallierer fasst laut
seinem `[UninstallDelete]` nur den Programmordner an, Datenbank
(`%ProgramData%\EPOS_PLAN` bzw. je Konto) sowie Lizenz und KI-Schlüssel unter
`%APPDATA%\wp-plan` bleiben liegen.

> **Bekannte Einschränkung beim Setup-Test:** Der **alte** Deinstallierer stellt
> seine Rückfrage „Projektdatenbank löschen?" über eine eigene `MsgBox` — auf
> die wirkt `/SUPPRESSMSGBOXES` nicht. Beim Update über eine 32-bit-Installation
> erscheint sie deshalb sichtbar; die Voreinstellung ist *Nein*, und dabei muss
> es bleiben. Nachbessern lässt sich das nicht mehr — der betreffende
> Deinstallierer ist bereits ausgeliefert.

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
| **E1** | **Eigenständige Veröffentlichung** (`SelfContained=true`, `win-x64`) | Kein Kunde muss die .NET-8-Desktop-Laufzeit x64 beschaffen, keine Abhängigkeit von Update-Ständen, keine Fehlersuche „warum startet es auf dem einen Rechner nicht". Preis: Nutzlast ≈ 350 MB statt 40 MB. Nach lzma2 erfahrungsgemäß 120–180 MB Setup-Datei — **vor der ersten Auslieferung messen** |
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
Architekturbezeichner `x64compatible` noch UTF-8 ohne BOM).

| Abschnitt | Inhalt |
|---|---|
| Präprozessor | Alle Pfade relativ zu `AddBackslash(SourcePath)`. Version über `GetVersionNumbersString` aus der gebauten EXE; fehlt sie, bricht die Übersetzung mit `#error` ab statt ein Setup mit leerer Version zu erzeugen |
| `[Setup]` | `AppId` als feste GUID, `{autopf}`, `PrivilegesRequired=admin`, `ArchitecturesAllowed=x64compatible`, `ArchitecturesInstallIn64BitMode=x64compatible`, `MinVersion=10.0`, `CloseApplications=yes` |
| `[Languages]` | Deutsch und Englisch — passend zur zweisprachigen Oberfläche |
| `[CustomMessages]` | Alle eigenen Texte zweisprachig, keine Zeichenkette im Code |
| `[Tasks]` | Desktopsymbol |
| `[Dirs]` | `%ProgramData%\EPOS_PLAN` mit `Permissions: users-modify` |
| `[Files]` | Veröffentlichungsordner rekursiv (ohne `*.pdb`, `*.xml`), Vorlagendatenbank, ACE-Installer nach `{tmp}` — letzterer nur, wenn er gebraucht wird |
| `[Icons]` | Startmenü, Web-Verknüpfung, Deinstallation, optional Desktop |
| `[Registry]` | `HKLM\SOFTWARE\INEKON\EPOS-Plan` (64-Bit-Sicht): `InstallDir`, `Version` |
| `[Run]` | ACE-Installation mit Gegenprüfung, `icacls` bei Altbestand, Programmstart anbieten |
| `[UninstallDelete]` | Gezielt: Protokolle, `Vorlage\`, dann `dirifempty` auf `{app}`. **Kein** pauschales Löschen des gewählten Ordners |
| `[Code]` | `AceVorhanden`, `AceNachpruefen`, `Office32Vorhanden` mit Hinweisdialog, `AlteX86InstallationEntfernen` (aus `PrepareToInstall`), Zustandsaufnahme in `InitializeSetup`, Hinweisseite, Rückfrage bei der Deinstallation |

Drei Feinheiten, die beim Ändern leicht kippen:

- **`ArchitecturesAllowed=x64compatible`.** Der Bezeichner gibt es erst ab Inno
  Setup 6.3; er umfasst x64-Windows **und** ARM64-Windows mit x64-Emulation. Das
  frühere `x64` bedeutet dort nur noch `x64os` und sperrte ARM64-Rechner aus.
- **`ArchitecturesInstallIn64BitMode=x64compatible` gehört zwingend dazu.** Erst
  diese zweite Zeile schaltet das Setup in den 64-Bit-Modus — nur dann zeigt
  `{autopf}` auf `Programme` statt auf `Programme (x86)` und `HKLM` auf die
  64-Bit-Registry-Sicht. Fehlt sie, landet eine x64-Anwendung im 32-Bit-Zweig,
  und die Übernahme der 32-bit-Vorinstallation (2.3) greift ins Leere.
- **Zustandsaufnahme in `InitializeSetup`.** `[Dirs]` läuft vor `[Run]` und legt
  `%ProgramData%\EPOS_PLAN` an. Prüfte die `Check`-Funktion des `icacls`-Laufs
  erst dort, wäre die Antwort immer „vorhanden". Deshalb wird der Zustand vor
  der Installation festgehalten.

---

## 5. Voraussetzungen und Prüfungen

### 5.1 Microsoft Access Database Engine, 64 Bit

Die einzige echte Voraussetzung. Geprüft wird `Microsoft.ACE.OLEDB.12.0` in der
**64-Bit-Sicht** (`HKCR64`) — genau die Kennung, die
`DataRepository.GetConnectionString()` anfordert. Ein vorhandenes
`Microsoft.ACE.OLEDB.16.0` allein genügt nicht; die 64-Bit-Redist registriert
ohnehin beide ProgIDs auf dieselbe `ACEOLEDB.DLL`.

Geprüft wird nicht die ProgID allein, sondern die **ganze Kette**:

```
Microsoft.ACE.OLEDB.12.0\CLSID  →  CLSID\{…}\InprocServer32  →  Datei existiert
```

Der Grund ist ein Befund vom Entwicklungsrechner: Eine ProgID kann als Leiche
ohne Server dastehen, wenn eine Engine unsauber entfernt oder von einem
Office-Update auf einen nicht mehr vorhandenen VFS-Pfad umgebogen wurde. Die
kurze ProgID-Prüfung meldete dort „vorhanden", während der erste Datenbank­zugriff
scheiterte.

Fehlt die Engine, läuft das mitgelieferte `AccessDatabaseEngine_X64.exe /quiet`.
Danach wird **erneut geprüft** — und hier liegt die bekannte Falle, seit der
x64-Umstellung mit umgekehrtem Vorzeichen:

> **32-Bit-Office blockiert die 64-Bit-Engine.** Ist auf dem Rechner ein
> 32-Bit-Microsoft-Office installiert, verweigert das Redistributable die
> Installation. Ohne Vorwarnung sähe der Anwender davon nichts (`/quiet`), und
> EPOS-Plan scheiterte später am ersten Datenbankzugriff.

Deshalb zwei Sicherungen. **Vor** dem stillen Lauf ermittelt
`Office32Vorhanden()` die Office-Bitness aus
`SOFTWARE\Microsoft\Office\ClickToRun\Configuration\Platform` — in **beiden**
Registry-Sichten, weil je nach Office-Bitness nur eine davon gefüllt ist — und
zeigt bei `x86` einen Hinweisdialog, der die Lage benennt und die Optionen
nennt. Die Installation wird danach trotzdem versucht; sie kann gelingen, und
ein Abbruch nähme dem Anwender das bereits kopierte Programm (E5). **Nach** dem
Lauf greift die Gegenprüfung mit sprechender Meldung.

Der Weg für den Supportfall gehört in die Liesmich-Datei: der in **KB 5004577**
dokumentierte Weg — Engine mit `/extract:<Ordner>` entpacken und die enthaltene
`AceRedist.msi` per `msiexec /i … /qn` installieren; auf manchen Ständen ist
zusätzlich der Registry-Wert
`HKLM\SOFTWARE\Microsoft\Office\<Version>\Common\FilesPaths\mso.dll` für die
Dauer der Installation zu entfernen. Microsoft dokumentiert diesen Weg, stuft
Mischbitness aber ausdrücklich als **nicht unterstützt** ein: Office-Reparaturen
und -Updates können die Registrierung wieder zerstören. Der saubere Weg für
solche Kunden bleibt der Wechsel auf 64-Bit-Office.

> **Vor dem ersten Setup-Build zu beschaffen:** Die
> `AccessDatabaseEngine_X64.exe` muss als *Access Database Engine 2016
> Redistributable, 64 Bit* aus dem Microsoft Download Center in die Repo-Wurzel
> gelegt werden (`build-setup.ps1` kopiert sie von dort nach
> `Setup\Voraussetzungen\` und bricht ab, solange sie fehlt). Die 32-bit-Fassung
> aus der x86-Ära ist nicht mehr verwendbar. Der Support der ADE 2016 endete am
> 14.10.2025; sie ist weiter herunterladbar und funktionsfähig. Designierter
> Nachfolger ist die *Microsoft 365 Access Runtime* — sie erzwingt allerdings die
> Bitness eines vorhandenen C2R-Office und ist daher kein Weg an 32-Bit-Office
> vorbei (Entscheidung 5.3 des Umstellungskonzepts).

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

### 5.5 Microsoft Edge WebView2 Runtime (seit Paket iU8)

Die **zweite** echte Voraussetzung, seit die ersten Dialoge Blazor-Komponenten
sind und in einer WebView2 laufen (`Allgemein\Blazor\BlazorDialogForm.cs`).
Ohne die Laufzeit startet EPOS-Plan zwar, aber jeder Blazor-Dialog bliebe leer.
Der erste davon ist „Energieträger Variante" aus `Form_Kosten`.

Was das Setup mitbringt, ist nur das **SDK** — `Microsoft.Web.WebView2.Core.dll`
und `WebView2Loader.dll` kommen mit `dotnet publish`. Die **Laufzeit** ist ein
Systembestandteil und muss auf dem Rechner sein: Auf Windows 11 ist sie es, auf
Windows 10, LTSC und Server nicht zwingend.

Geprüft wird die Fassung unter der festen Produkt-GUID
`F3017226-FE2A-4295-8BDF-00C3A9A7E4C5` im EdgeUpdate-Zweig — die von Microsoft
dokumentierte Erkennung:

```
HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{…}\pv      (maschinenweit)
HKCU\Software\Microsoft\EdgeUpdate\Clients\{…}\pv                   (je Benutzer)
```

Eine der beiden genügt. Der Wert `0.0.0.0` gilt ausdrücklich als **nicht
vorhanden**: Ihn hinterlässt eine entfernte Laufzeit — der Schlüssel steht dann
noch da, die Laufzeit nicht. Derselbe Befund wie bei der ACE-Leiche in 5.1.

Fehlt die Laufzeit, läuft der mitgelieferte
`MicrosoftEdgeWebview2Setup.exe /silent /install`; danach wird erneut geprüft
und bei Fehlschlag gemeldet. **Abgebrochen wird nichts** — ohne WebView2
arbeitet alles außer den neueren Dialogen weiter (dieselbe Linie wie E5 bei der
Access-Engine).

> **Online oder offline — offene Anwenderentscheidung.** Mitgeliefert wird der
> *Evergreen-Bootstrapper* (rund 2 MB,
> <https://go.microsoft.com/fwlink/p/?LinkId=2124703>). Er lädt die Laufzeit
> beim Anwender **online** nach; ohne Internetverbindung auf dem Zielrechner
> schlägt er fehl. Die Alternativen sind der *Standalone-Installer* (rund
> 150 MB, offline lauffähig) und die *Fixed-Version-Verteilung* (die Laufzeit
> liegt im Programmordner, angesprochen über
> `CoreWebView2CreationProperties.BrowserExecutableFolder`; dann liegt die
> Aktualisierung bei uns statt bei Microsoft). Welcher Weg gilt, entscheidet
> der Anwender — bis dahin bleibt der Bootstrapper.


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
  Voraussetzungen\AccessDatabaseEngine_X64.exe
  Voraussetzungen\MicrosoftEdgeWebview2Setup.exe
  Ausgabe\                             Ergebnis (NICHT versionieren)
```

Nach `.gitignore`: `Setup/Ausgabe/`, `Setup/Vorlage/*.accdb`,
`Setup/Voraussetzungen/*.exe`, `artifacts/`.

Ein Durchlauf:

```powershell
cd C:\Waermeplan\WP_Plan\Setup
.\build-setup.ps1
```

Das Skript veröffentlicht eigenständig nach `artifacts\publish\win-x64`, prüft
Vorlagendatenbank, Voraussetzungs-Installer und Inno-Setup-Version, übersetzt
und meldet Pfad und Größe. `-SkipPublish` überspringt den Bau, `-Schnell`
schaltet auf `lzma2/normal` für Testläufe.

Veröffentlicht wird bewusst mit dem **MSBuild aus Visual Studio 2022**
(`-restore -t:Publish -p:Platform=x64 -p:RuntimeIdentifier=win-x64
-p:SelfContained=true`), nicht mit `dotnet publish`: Das Projekt hält
COM-Referenzen (Excel-Interop, VBIDE), und das SDK-MSBuild bricht dabei mit
**MSB4803** ab — `ResolveComReference` gibt es nur im vollen MSBuild. Eine
Bitness-Option hat das Skript nicht; es gibt nur noch `win-x64`
(Entscheidung 5.1 des Umstellungskonzepts).

> **Nachtrag 02.09.2026:** Der Absatz zu MSB4803 ist überholt. Mit der
> Umstellung des Excel-Interops auf ClosedXML hält das Projekt keine
> COM-Referenzen mehr; `build-setup.ps1` veröffentlicht seither mit
> `dotnet publish`, Visual Studio wird zum Bauen nicht mehr benötigt.

**Freigabeprobe vor jeder Auslieferung** — auf einer frischen
Windows-Installation, nicht auf dem Entwicklungsrechner:

1. Erstinstallation ohne ACE → Treiber wird installiert, Programm startet,
   Datenbank entsteht im Profil
2. Erstinstallation mit **64-Bit-Office** mit Access → Engine ist bereits
   systemweit registriert, die Redist wird übersprungen
3. Erstinstallation mit **32-Bit-Office** → Hinweisdialog vor der
   Redist-Installation, danach entweder Erfolg oder die sprechende Meldung aus
   5.1 — in keinem Fall ein stiller Fehlschlag
4. Update über eine Vorgängerversion → Projekte bleiben, Schemamigration läuft
5. **Update über eine 32-bit-Installation** → alter Eintrag und Programmordner
   verschwinden, genau **ein** Eintrag in „Apps und Features" bleibt; dabei
   erscheint die Rückfrage des alten Deinstallierers — *Nein* wählen (2.3)
6. Installation auf einem Rechner mit Altbestand in `%ProgramData%` → Hinweisseite
   erscheint, Projekte werden übernommen
7. Zweites Windows-Konto startet EPOS-Plan, während das erste läuft
8. Deinstallation mit und ohne Datenlöschung
9. `dotnet list package --include-transitive` — Lizenzprüfung, insbesondere die
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
| S3 | `AccessDatabaseEngine_X64.exe` (ADE 2016 Redistributable, **64 Bit**) liegt noch nicht in der Repo-Wurzel — ohne sie bricht `build-setup.ps1` ab | Aus dem Microsoft Download Center beschaffen und unverändert in die Repo-Wurzel legen (5.1). Die 32-bit-Fassung der x86-Ära entfällt ersatzlos |
| S8 | `MicrosoftEdgeWebview2Setup.exe` (Evergreen-Bootstrapper) liegt noch nicht in der Repo-Wurzel — ohne sie bricht `build-setup.ps1` ab | Von <https://go.microsoft.com/fwlink/p/?LinkId=2124703> beschaffen und unverändert in die Repo-Wurzel legen (5.5) |
| S9 | `.gitignore` deckt `/AccessDatabaseEngine*.exe` ab, den WebView2-Bootstrapper in der Repo-Wurzel aber **nicht** — `GitHub_Sync.bat` committet mit `git add -A` | Zeile `/MicrosoftEdgeWebview2Setup.exe` in `.gitignore` ergänzen |
| S10 | Online- oder Offline-Verteilung der WebView2-Laufzeit (5.5) | **Entschieden 03.09.2026 (iF20): Bootstrapper.** Der Standalone-Installer wird erst beigelegt, wenn ein Kunde ohne Internet installiert |
| S4 | Herausgebername: „INEKON" oder die vollständige Firmierung? Steht in Setup, Softwareliste und später im Zertifikat | Festlegen, danach `#define AppPublisher` |
| S5 | Automatisierte Erzeugung der Auslieferungsdatenbank (6.1) | Skript schreiben; bis dahin Handlauf mit Gegenprüfung |
| S6 | `Settings.Default.Upgrade()` beim Versionswechsel vorhanden? (7.7) | Im Code nachsehen |
| S7 | ~~Wird noch ein 64-Bit-Stand gebraucht?~~ **Erledigt 22.08.2026:** ja — EPOS-Plan ist vollständig auf x64 umgestellt, einen x86-Stand gibt es nicht mehr | Keiner. Herleitung und Abnahmeplan in [`Konzept_Umstellung_64Bit_EPOS-Plan.md`](../Konzept_Umstellung_64Bit_EPOS-Plan.md) |

---

## 12. Aufwand

| Paket | Inhalt | PT |
|---|---|---|
| S-1 | Setup-Skript einrichten, Symbol, Lizenz- und Liesmich-Text, erster Übersetzungslauf | 0,5 |
| S-2 | Änderungen an der Anwendung 7.1, 7.2, 7.5, 7.6 | 1,0 |
| S-3 | Auslieferungsdatenbank: Bereinigung festlegen und einmal durchführen (6.1) | 1,0 |
| S-4 | Freigabeprobe auf frischer Windows-Installation, neun Fälle (Abschnitt 8) | 1,0 |
| S-5 | Assemblyumbenennung 7.3 mit Regressionsprobe | 0,5 |
| S-6 | Mutex 7.4, `Settings.Upgrade()` 7.7 | 0,5 |
| | **Summe ohne Signierung** | **4,5** |
| S-7 | Code-Signierung: Zertifikat beschaffen, Token einrichten, Kette einbauen | 0,5 PT + Beschaffungszeit und Zertifikatskosten |

Die ersten vier Pakete ergeben ein auslieferbares Setup. S-5 bis S-7 heben die
Außenwirkung — und sollten vor der ersten Auslieferung an zahlende Kunden
erledigt sein, nicht danach.
