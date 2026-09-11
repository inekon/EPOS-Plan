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

> **Stand 06.09.2026 — Herstellerdaten.** Anwenderentscheid **W6‑O‑9** („ja"):
> Das Setup liefert den Ordner `VDI-3805-Daten` (rund 186 MB) als **vorgewählte,
> abwählbare Komponente** nach `{app}\VDI-3805-Daten` aus. Neu sind dadurch die
> Abschnitte `[Types]` und `[Components]` im Skript, eine Zeile in `[Files]`, eine
> in `[UninstallDelete]` und eine Vorbedingung in `build-setup.ps1`. Begründung
> der Lage: Entscheidung **E10** in Abschnitt 3. Fachlicher Bezug:
> [`Konzept_Wechselrichter_EPOS-Plan.md`](../Konzept_Wechselrichter_EPOS-Plan.md),
> Kapitel 12.

> **Stand 09.09.2026 — Deinstallations-Rückfrage.** Anwenderentscheid
> **#157‑E‑3** („Empfehlung"): Die Rückfrage `DatenLoeschen` zielte auf
> `%LocalAppData%\EPOS_PLAN` — einen Ordner, den nichts anlegt (Befund
> Auftrag #157) — und erschien deshalb praktisch nie. Auftrag #161 stellt sie
> auf den tatsächlichen, seit dem SQLite-Cutover für alle Windows-Konten
> gemeinsamen Datenordner `%ProgramData%\EPOS_PLAN` um (Datenbank samt
> `DB-Backup`), Voreinstellung weiterhin *Nein*, und meldet einen
> fehlgeschlagenen `DelTree` (offene Datei) statt still weiterzulaufen.
> Abschnitte 2.4 und 6.3 sind entsprechend nachgezogen.

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
| `…\EPOS-Plan\VDI-3805-Daten` | **Herstellerdaten** (VDI 3805, CEC-Modul- und Wechselrichterliste), rund 186 MB — abwählbare Komponente (E10) | Standard (Benutzer: nur lesen) | nur das Setup |
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
einmal, ob der gemeinsame Datenordner `%ProgramData%\EPOS_PLAN` — Datenbank
samt Sicherungsordner `DB-Backup` — mitgelöscht werden soll; Vorgabe *Nein*.
Dieser Ordner gehört seit dem SQLite-Cutover allen Windows-Konten des
Rechners gemeinsam, ein „Ja" trifft also auch deren Projekte, nicht nur die
des angemeldeten Kontos — das steht so auch im Meldungstext (Auftrag #161,
09.09.2026; zuvor richtete sich die Rückfrage fälschlich an
`%LocalAppData%\EPOS_PLAN`, einen Ordner, den nichts anlegt, siehe Auftrag
#157). Ausdrücklich **nicht** angefasst werden dabei die beiden
Datenverzeichnisse `WP-Plan` und die Registrierungseinstellungen
(`HKEY_CURRENT_USER\Software\wp-plan`) des angemeldeten Kontos.

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
| **E10** | **Herstellerdaten im Setup — als abwählbare Komponente unter `{app}\VDI-3805-Daten`** | Anwenderentscheid **W6‑O‑9** vom 06.09.2026: „ja". Ohne die Datensätze steht der Kunde vor leeren Importmasken; die zwei CEC-Listen sind zudem der einzige Weg zu einem gefüllten Modul- und Wechselrichterkatalog (W6‑O‑3). 186 MB rechtfertigen aber keine Zwangsinstallation, deshalb eine **vorgewählte, abwählbare** Komponente. **Nach `{app}` und nicht nach `%ProgramData%`**, weil die Masken den Bestand nur LESEN: Er gehört damit in die Zeile „nur das Setup schreibt" der Tabelle 2.1 — dieselbe Lage und derselbe Grund wie bei der Vorlagendatenbank (E3). `%ProgramData%\EPOS_PLAN` bleibt dagegen bei der Deinstallation absichtlich stehen (dort liegen Anwenderdaten); 186 MB Auslieferungsbestand blieben dort als Leiche zurück |

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
| `[Types]` / `[Components]` | Zwei Typen (`voll`, `custom`) und zwei Bestandteile: `programm` (`Flags: fixed`) und `herstellerdaten` — vorgewählt, abwählbar (E10) |
| `[Tasks]` | Desktopsymbol |
| `[Dirs]` | `%ProgramData%\EPOS_PLAN` mit `Permissions: users-modify` |
| `[Files]` | Veröffentlichungsordner rekursiv (ohne `*.pdb`, `*.xml`), Vorlagendatenbank, Herstellerdatenordner `VDI-3805-Daten` rekursiv (`Components: herstellerdaten`), ACE- und WebView2-Installer nach `{tmp}` — letztere nur, wenn sie gebraucht werden |
| `[Icons]` | Startmenü, Web-Verknüpfung, Deinstallation, optional Desktop |
| `[Registry]` | `HKLM\SOFTWARE\INEKON\EPOS-Plan` (64-Bit-Sicht): `InstallDir`, `Version` |
| `[Run]` | ACE-Installation mit Gegenprüfung, `icacls` bei Altbestand, Programmstart anbieten |
| `[UninstallDelete]` | Gezielt: Protokolle, `Vorlage\`, `VDI-3805-Daten\`, dann `dirifempty` auf `{app}`. **Kein** pauschales Löschen des gewählten Ordners |
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

### 5.1 Microsoft Access Database Engine, 64 Bit — GEFALLEN (W3, 09.09.2026)

> **Dieser Abschnitt beschreibt einen Stand, den es nicht mehr gibt.** Mit dem
> Anwenderentscheid `#157-E-1` (Weg W3) liefert das Setup die Access-Engine nicht mehr
> mit und prueft sie nicht mehr: Access wurde beim Kunden nie produktiv eingesetzt, und
> die Anwendung liest ihre `Kenndaten.sqlite` ohne Fremdtreiber. Was aus dem Skript
> verschwunden ist, steht in Abschnitt 6.3. Der Abschnitt bleibt zur Geschichte stehen.

Die einzige echte Voraussetzung (bis 09.09.2026). Geprüft wird `Microsoft.ACE.OLEDB.12.0` in der
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

> **Stand 09.09.2026 — Anwenderentscheid `#157‑E‑1`, Weg W3.** Dieser Abschnitt ist
> gebaut. Das Setup liefert `{app}\Vorlage\Kenndaten.sqlite` aus, der Kern kopiert sie
> beim ersten Start in den Datenordner, und der Access-Weg (Übernahme-Assistent,
> ACE-Engine, `.accdb`-Vorlage) ist gefallen. Der Betriebsstand steht in
> [`../BETRIEB_SQLITE.md`](../BETRIEB_SQLITE.md), Abschnitt 1.

### 6.1 Den Auslieferungsstand erzeugen

**Die Datenbank im Repository darf nicht ausgeliefert werden.** Die Arbeitsdatenbank
enthält reale Projekte aus der Entwicklung — also Kunden- und Objektdaten. Sie in ein
Setup zu packen, das an Dritte geht, wäre eine Datenpanne.

Der Auslieferungsstand liegt getrennt unter `Setup\Vorlage\Kenndaten.sqlite`.
**Seit dem 09.09.2026 erzeugt ihn ein Werkzeug** — Anwenderentscheid
**#157‑E‑2** („Empfehlung" angenommen: automatisieren, und die Vorlage enthält
Beispielprojekte), umgesetzt als `Werkzeuge\Auslieferungsvorlage` (Auftrag
#160). Die vier Handgriffe von früher sind damit fünf Schritte des Werkzeugs;
sie stehen hier weiterhin, weil sie erklären, **was** geschieht:

1. **Arbeitskopie** über `Datenbanksicherung.KopieAnlegen` — die eine
   Sicherungswahrheit des Kerns seit Auftrag #158, ein `VACUUM INTO` über eine
   frisch geöffnete Verbindung. Es liest durch das WAL hindurch und lässt die
   Quelle byte-gleich, auch wenn EPOS-Plan gerade läuft; eine reine Dateikopie
   griffe nur den letzten Checkpoint ab (`BETRIEB_SQLITE.md` § 2 und § 3.2).
   Die alte Prüfung auf `Kenndaten.laccdb` entfällt mit Access.
2. **Alle Projektdaten löschen.** Ein `DELETE FROM Tab_Projekt` räumt über die
   Beziehungen mit `ON DELETE CASCADE` das meiste mit ab; die Tabellen ohne
   Fremdschlüssel und die Detailtabellen darunter räumt das Werkzeug einzeln
   nach — in **einer** Transaktion mit `PRAGMA defer_foreign_keys = ON`, damit
   die Reihenfolge unkritisch ist (dieselbe Bauart wie
   `sql\tools\Reduziere-Testdatenbank.sql`). Die dokumentierte Ausnahme
   `Tab_Pufferspeicher` ist damit erledigt: Sie hängt seit der SQLite-Migration
   **doch** an der Projektkaskade, und `Tab_Energieanlagen.ID_PUFFER` ist ein
   `NO ACTION`-Verweis, den die aufgeschobene Prüfung abfängt. **Die
   Tabellenliste wird nicht gepflegt, sondern abgeleitet** — aus dem Schema der
   geöffneten Datei: jede Tabelle mit Spalte `ID_Projekt`/`ProjektID` (47) plus
   die transitive Hülle darunter (25) plus `Tab_Projekt`. Ein Schemaschritt, der
   eine Projekttabelle ergänzt (zuletzt 65/66 mit `Tab_Wechselrichter` und
   `Z_AnlageStrang`), wird damit von selbst erfasst.
3. **Kataloge und Personenbezug.** Der Katalog wird vollständig ausgeliefert
   (`--kataloge alle`, Vorgabe seit Anwenderentscheid **#160‑E‑1a**,
   11.09.2026) — siehe den **Befund** unten. `--kataloge readonly` bleibt als
   ausdrücklich wählbarer Schalter: Dann bleibt in den `*_STAMM`-Tabellen nur,
   was `ReadOnly = TRUE` trägt. Dazu leert das Werkzeug `Tab_Applikation`: `Projektname`,
   `Beschreibung`, `Icon` und `ID_Projekt` (auf 0, der Zustand „kein Projekt
   geöffnet", den auch `ProjektCtrl.LoeschenMitVorarbeiten` schreibt). Nötig ist
   das, weil diese Tabelle an keinem Projekt hängt und sonst den Namen des
   zuletzt geöffneten **Kunden**projekts mit ausliefern würde. Lizenztoken,
   Zeitanker und KI-Schlüssel liegen nicht in der Datenbank, sondern über
   `Dienste.Lizenzablage` im Anmeldeinformationsspeicher.
4. **Beispielprojekte einspielen** als `.wpx`-Pakete über
   `ProjektExportImportCtrl` (`--beispiele <ordner-oder-liste>`). Damit ist der
   offene Punkt aus `Konzept_Projektbeispiele_Dokumentation.md` § 6.2 („ein
   Projektexport existiert nicht") geschlossen; Katalogbezüge lösen sich beim
   Import über die fachlichen Schlüssel neu auf, nicht über IDs — genau so, wie
   es das Beispielkonzept verlangt. Ohne `--beispiele` bleibt die Vorlage
   projektfrei.
5. **Verdichten und prüfen.** `VACUUM`, dann `PRAGMA journal_mode = WAL` (die
   Betriebserwartung aus `BETRIEB_SQLITE.md`; `VACUUM INTO` liefert sonst eine
   Datei im Standardmodus). Danach Schemastand, STRICT-Tabellenzahl,
   `integrity_check`, `foreign_key_check`, Projektliste, Datenschutzwächter und
   die Frage, ob wirklich nur **eine** Datei entstanden ist.

```bash
dotnet run --project Werkzeuge/Auslieferungsvorlage -c Release -- \
    <quelle.sqlite> <ziel.sqlite> [--beispiele <ordner-oder-liste>] [--trocken]
```

Rückgabe `0` = erzeugt und abgenommen. Jeder andere Wert ist ein Abbruch mit
Grund auf `stderr`, und dann entsteht **keine** Zieldatei: `2` Aufruf oder Quelle,
`3` Ziel im Repository außerhalb von `Setup\Vorlage\`, `4` Katalogwächter (nur bei
`--kataloge readonly`), `5` fachlicher Abbruch, `1` unerwartet. Neben der Vorlage entsteht
`<ziel>.bericht.txt` — der **Prüfbericht**, der die frühere Gegenprüfung von Hand
ersetzt: je Tabelle die Zeilen vorher und nachher, Katalogzahlen, geleerte Felder,
Projektliste, Größe vorher/nachher und jede Prüfzeile. Er ist vor jeder
Auslieferung zu lesen; `Setup\Vorlage\LIESMICH.md` nennt die drei Zeilen, auf die
es ankommt.

> **Befund #160‑F‑1 — die Marke `ReadOnly` trägt die Regel heute nicht.**
> Schritt 3 in seiner ursprünglichen Fassung („in `*_STAMM` bleibt nur
> `ReadOnly = TRUE`") leert am Bestand der Testdatenbank **22 der 28
> Katalogtabellen: 419 722 Zeilen bleiben 101.** Betroffen sind unter anderem
> `Tab_Kenndaten_STAMM` (1 960 Wärmepumpen-Kennfelder), `Tab_Heizkessel_STAMM`
> (63), `Tab_Gebaeude_STAMM` (277) und `Tab_PV_STAMM` (6 — dieselben Module,
> deren Koeffizienten Schemaschritt 69 gerade erst repariert hat). Drei weitere
> Tabellen **ohne** Spalte `ReadOnly` reißt die Kaskade mit:
> `Tab_Klimaregion_STAMM` nimmt `Tab_Klimadaten_STAMM` (11 680) und
> `Tab_Solar_STAMM` (280 320) mit, `Tab_WP_STAMM` nimmt
> `Tab_Kenndaten_Kuehlung_STAMM` (174) mit. Ursache: Im Code ist `ReadOnly` ein
> **Schreibschutz der Oberfläche** (`HeizkesselStammCtrl`, `GebaeudeStammCtrl`,
> `KostenVorlagenCtrl` verweigern damit das Ändern), nicht die
> Auslieferungsmarke, als die die Namenskonvention sie beschreibt.
> **Deshalb bricht das Werkzeug mit Code 4 ab**, statt eine Vorlage mit leerem
> Katalog abzulegen — das fiele erst beim Kunden auf. Zwei Wege standen offen und
> beide waren ausdrücklich zu wählen: `--kataloge alle` liefert den vollständigen
> Katalog aus, `--katalogleerung-zulassen` setzt die Regel trotzdem durch.
>
> **Entscheid #160‑E‑1a (Anwender, 11.09.2026, „a").** Der Katalog wird
> vollständig ausgeliefert: Die **Vorgabe** des Werkzeugs wird `alle`,
> `--kataloge readonly` bleibt als ausdrücklich wählbarer Schalter samt
> Katalogwächter (Code 4) und `--katalogleerung-zulassen` — verworfen wurde Weg
> b, die Marke `ReadOnly` im Bestand nachzupflegen. Begründung: Die Trennung
> zwischen „gehört zur Auslieferung" und „ist in der Oberfläche gesperrt" hat
> heute keinen Abnehmer — kein Bericht, keine Prüfung und kein Anwender
> unterscheidet danach, und eine Marke zu pflegen, die niemand liest, wäre reine
> Mehrarbeit ohne Nutzen. Umgesetzt in Auftrag #182.

Der Stand **entsteht vor jedem Übersetzungslauf neu** — er liegt deshalb NICHT im Repository
(`.gitignore`: `Setup/Vorlage/*.sqlite`, dazu `-wal`/`-shm` und der Prüfbericht `*.bericht.txt`). Aufruf des
Werkzeugs (Rückgabe 0 = erzeugt und abgenommen, sonst Grund auf stderr und keine Zieldatei):

```powershell
dotnet run --project Werkzeuge/Auslieferungsvorlage -c Release -- <quelle.sqlite> <ziel.sqlite> [--beispiele …] [--trocken]
```

`build-setup.ps1` ruft es selbst auf, unmittelbar vor `ISCC`:

```powershell
.\build-setup.ps1 -Quelldatenbank D:\Auslieferung\Kenndaten_Stand.sqlite
```

Die Quelle kommt aus dem Parameter `-Quelldatenbank` oder aus der Umgebungsvariablen
`EPOS_VORLAGE_QUELLE`. **Ohne Angabe bricht das Skript ab** und greift bewusst **nicht**
ersatzweise auf die Arbeitsdatenbank zurück; ebenso bei einem Rückgabecode ≠ 0 des
Werkzeugs oder einer fehlenden Zieldatei. Auch `ISCC` selbst prüft die Datei noch einmal
(`#if !FileExists(VorlageDb)` im `.iss`).

Was das Werkzeug tut — Projektdaten entfernen, den Auslieferungskatalog behalten,
Beispielprojekte auswählen, verdichten —, steht bei ihm; hier zählt nur, dass es genau
eine Quelle für diesen Stand gibt.

### 6.2 Erstkopie beim ersten Programmstart

**Gebaut, und zwar im Kern.** Die Anwendung entscheidet, welche Datenbank sie benutzt —
nicht das Setup:

| Schritt | Fundstelle |
|---|---|
| Startprüfung | `Program.Main` → `DataRepository.DatenbankVorhanden()` |
| Bereitstellung | `Program.DatenbankBereitstellen()` → `Erstbereitstellung.Sicherstellen(Ziel, Vorlage)` |
| Ablauf | `EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs` |
| Pfad der Vorlage | `IPfade.Auslieferungsvorlage` (`StandardPfade`, Aufstieg von `AppContext.BaseDirectory`) |

**Der Ablageort ist und bleibt `%ProgramData%\EPOS_PLAN`.** Der Vorschlag der früheren
Fassung dieses Abschnitts — eine Datenbank je Windows-Konto unter `%LOCALAPPDATA%` — ist
nie gebaut worden; `DataRepository.GetDBPath()` kennt kein Benutzerprofil (Befund #157
vom 09.09.2026). Der `[Dirs]`-Eintrag des Setups gibt der Gruppe Benutzer deshalb
vererbende Änderungsrechte auf diesen Ordner (`users-modify`) — ohne sie könnte die
Anwendung die Vorlage dort gar nicht ablegen.

Drei Zusicherungen der Bereitstellung: **nie überschreiben** (eine vorhandene Datei bleibt
unberührt, auch eine beschädigte — dort stehen die Projekte des Anwenders), **nie halb
liegen lassen** (bei jedem Fehler wird die Zieldatei wieder entfernt), **erst prüfen, dann
melden** (`PRAGMA integrity_check` und `Tab_Applikation.SchemaVersion`). Eine Vorlage mit
älterem Schemastand ist zulässig; `SchemaMigration.Ausfuehren` hebt sie beim selben Start
an.

**Fehlt auch die Vorlage**, startet das Programm nicht: Meldung `START_DB_FEHLT`, ergänzt
um den erwarteten Vorlagenpfad (`START_VORLAGE_FEHLT`).

**Auf iOS gilt derselbe Gedanke** — `EPOS.iOS/Datenbankbereitstellung.cs` kopiert die
mitgelieferte `Kenndaten.sqlite` aus dem Anwendungspaket in die Sandbox.

### 6.3 Kein Access mehr im Setup

Mit Weg W3 sind aus diesem Skript verschwunden: `#define AceInstaller`, die
`[Files]`- und `[Run]`-Zeile des Redistributables, die vier Pascal-Funktionen
`AceVorhanden`/`Office32Vorhanden`/`Office32Hinweisen`/`AceNachpruefen`, die drei
Meldungen `AceInstallieren`/`Office32Hinweis`/`AceFehlt` (de+en) und die
Assistentenseite „Vorhandene Datenbank gefunden" samt `G_LegacyDb`,
`InitializeWizard` und `ShouldSkipPage`. Abschnitt 5.1 dieses Konzepts beschreibt
damit einen Stand, den es nicht mehr gibt.

Die Übernahme eines `.accdb`-Altbestands ist seither ein **Hauswerkzeug**
(`EposSqliteMigrator.exe`); wer sie fährt, installiert die ACE-Engine dort, wo sie
läuft — beim Anwender wird sie nicht mehr gebraucht. `AccessDatabaseEngine_X64.exe`
gehört damit auch nicht mehr in `Setup\Voraussetzungen\`.

### 6.4 Was das Setup mit der Datenbank nie tut

Es überschreibt sie nicht, es migriert sie nicht, es löscht sie nicht (außer
auf ausdrückliche Rückfrage bei der Deinstallation — und dann den ganzen
gemeinsamen Ordner `%ProgramData%\EPOS_PLAN`, nicht nur einen Anteil des
angemeldeten Kontos; Auftrag #161, 09.09.2026, siehe Abschnitt 2.4). Alles
Weitere macht die Anwendung, die den Schema- und Lizenzzustand kennt.

---

## 7. Nötige Änderungen an der Anwendung

| Nr. | Änderung | Notwendig? | Aufwand |
|---|---|---|---|
| 7.1 | `DataRepository.GetDBPath` auf `%LOCALAPPDATA%` mit Erstkopie und Bestandsübernahme (6.2) | **Pflicht** — ohne sie greift E2 nicht | 0,5 PT |
| 7.2 | Wartehinweis beim ersten Start während der Erstkopie | **Pflicht** | 0,25 PT |
| 7.3 | `AssemblyName` von `WindowsFormsApplication1` auf `EPOS-Plan` | empfohlen | 0,5 PT + Regressionsprobe |
| 7.4 | Benannter Mutex `Global\EPOS-Plan` beim Start | empfohlen | 0,25 PT |
| 7.5 | Versionsnummer in `Properties\AssemblyInfo.cs` pflegen | **Pflicht** | — |
| 7.6 | `<ApplicationIcon>` im Projekt setzen | **erledigt** (Auftrag #229, 11.09.2026 — `WindowsFormsApplication1/Resources/EPOS-Plan.ico`, dieselbe Datei nutzt `SetupIconFile`) | 0,1 PT |
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

Stand 11.09.2026: 1.2.0.0 (#191), gleich dem Update-Logbuch auf epos-plan.de; der nächste
Setup-Lauf liefert `EPOS-Plan_Setup_1.2.0.0.exe`.

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
  Lizenz.rtf                           Lizenzvereinbarung
  Liesmich.rtf                         Neuerungen, ACE-Supportfall, DB-Übernahme
  Vorlage\Kenndaten.accdb              Auslieferungsstand (NICHT versionieren)
  Voraussetzungen\AccessDatabaseEngine_X64.exe
  Voraussetzungen\MicrosoftEdgeWebview2Setup.exe
  Ausgabe\                             Ergebnis (NICHT versionieren)

<Repo>\VDI-3805-Daten\                 Herstellerdaten, rund 186 MB (versioniert),
                                      Komponente "herstellerdaten" (E10)
```

Nach `.gitignore`: `Setup/Ausgabe/`, `Setup/Vorlage/*.accdb`,
`Setup/Voraussetzungen/*.exe`, `artifacts/`.

Seit Auftrag #229 (11.09.2026) gibt es kein eigenes `Setup\EPOS-Plan.ico` mehr: `SetupIconFile`
nimmt dieselbe Datei wie das `<ApplicationIcon>` der Anwendung,
`WindowsFormsApplication1\Resources\EPOS-Plan.ico` (die EPOS-Plan-Marke, nicht mehr das alte
`wpplan.ico`) — eine Quelle für Installer-, Programm- und Fenstersymbol.

Ein Durchlauf:

```powershell
cd C:\Waermeplan\WP_Plan\Setup
.\build-setup.ps1
```

Das Skript veröffentlicht eigenständig nach `artifacts\publish\win-x64`, prüft
Vorlagendatenbank, **Herstellerdatenordner**, Voraussetzungs-Installer und
Inno-Setup-Version, übersetzt und meldet Pfad und Größe. `-SkipPublish`
überspringt den Bau, `-Schnell` schaltet auf `lzma2/normal` für Testläufe.

Fehlt `VDI-3805-Daten`, bricht schon `build-setup.ps1` mit der Bezugsquelle ab;
`EPOS-Plan.iss` hat für den Handlauf mit `ISCC.exe` denselben Abbruch als
`#error` (`DirExists`). **Ein Setup ohne Herstellerdaten entsteht nicht aus
Versehen** — nur, wenn der Anwender die Komponente im Assistenten abwählt.

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
10. **Komponentenseite** (E10): Der Assistent zeigt zwischen Zielordner und
    Aufgaben eine Seite „Komponenten auswählen" mit dem Typ „Vollständige
    Installation" und zwei Häkchen — „Programm und Auslieferungsdatenbank"
    (fest) und „Herstellerdaten (VDI 3805, CEC)" (gesetzt, rund 186 MB). Zwei
    Fälle: **mit** Häkchen → nach der Installation liegt `VDI-3805-Daten` neben
    dem Programm, und der Dateiwähler von Administration → Datenimport macht
    ohne jede Einstellung darin auf; **ohne** Häkchen → der Ordner fehlt, das
    Programm läuft unverändert, und der Wähler startet im bisherigen
    Vorgabeordner

Eine Automatisierung über GitHub Actions ist möglich (`windows-latest` bringt
das .NET-SDK mit, Inno Setup ist per `choco install innosetup` nachrüstbar),
scheitert aber vorerst an der Vorlagendatenbank: 92 MB Binärdatei mit
Kundenbezug gehören nicht in ein Repository. Solange dieser Schritt manuell ist,
bleibt die Kette es auch.

> **Nachtrag 11.09.2026:** Der Einwand ist erledigt. Die Vorlagendatenbank liegt
> nicht mehr im Repository, sondern **entsteht im Lauf** aus einer benannten
> Quelle (`Werkzeuge\Auslieferungsvorlage`, Abschnitt 6.1) — in der CI aus
> `Referenzlaeufe/Kenndaten_Test.sqlite`. Damit gibt es den Job: seit #177 der
> Job `installer` in `.github/workflows/windows.yml` (davor die eigene Datei
> `setup.yml`, #176), siehe „Lauf in der CI" am Ende von 8.1. Handarbeit bleibt
> allein die Auslieferung selbst, deren Quelle ein gepflegter Katalogstand vom
> Arbeitsplatz ist.

### 8.1 Laufanleitung Windows

Anlass (Anwender, 10.09.2026): Inno Setup läuft nicht zentral installiert,
sondern im Ordner `Setup` des eigenen Repository-Klons — beim Anwender unter
`C:\Waermeplan\WP_Plan\Setup`. `build-setup.ps1` findet `ISCC.exe` seit dem
Parameter `-Iscc` auch dort (Auftrag #164); dieser Abschnitt beschreibt den
vollständigen Handlauf.

1. Klon aktualisieren: im Repository-Wurzelordner `git checkout ios_migration`,
   dann `git pull`.
2. PowerShell im Ordner `Setup` öffnen, z. B. `cd C:\Waermeplan\WP_Plan\Setup`.
3. Aufruf:

   ```powershell
   .\build-setup.ps1 -Quelldatenbank <Pfad>\Kenndaten.sqlite -Beispiele <Ordner mit .wpx oder leer> [-Iscc <Pfad>]
   ```

   Kein `-Kataloge` nötig: Seit Anwenderentscheid **#160‑E‑1a** (Befund
   #160‑F‑1, Abschnitt 6.1) ist die Vorgabe von `Werkzeuge\Auslieferungsvorlage`
   bereits `alle` — der Katalog wird vollständig ausgeliefert. `-Kataloge
   readonly` bleibt als ausdrücklich wählbarer Schalter; nur damit bricht das
   Werkzeug mit Code 4 ab. `-Iscc` nur angeben, wenn das
   Skript `ISCC.exe` nicht selbst findet (Suchreihenfolge: `-Iscc` →
   Umgebungsvariable `EPOS_ISCC` → neben `build-setup.ps1` → Program Files →
   Registry); Pfad zur `ISCC.exe` selbst oder zu deren Ordner, z. B.
   `-Iscc C:\Waermeplan\WP_Plan\Setup\Inno Setup 6`.
4. Was der Lauf ausgibt: die vier Schritte „Vorbedingungen prüfen",
   „Veröffentlichung bauen", „Auslieferungsvorlage erzeugen" und „Setup
   übersetzen" auf der Konsole (Abschnitt 4), dazwischen Version und Größe der
   Veröffentlichung sowie der Vorlage. Der Prüfbericht der Vorlage entsteht
   daneben als `Setup\Vorlage\Kenndaten.sqlite.bericht.txt` (Abschnitt 6.1); das
   fertige Setup liegt danach unter `Setup\Ausgabe`.
5. Rückgabecode von `Werkzeuge\Auslieferungsvorlage` — `build-setup.ps1` bricht
   in jedem Fall mit ab und gibt die Meldung des Werkzeugs weiter:
   - **2** — Aufruf oder Quelle falsch: `-Quelldatenbank` und den angegebenen
     Pfad prüfen.
   - **3** — Ziel liegt im Repository außerhalb von `Setup\Vorlage\`: nicht
     selbst eingreifen, den Pfad setzt das Skript.
   - **4** — Katalogwächter (#160‑F‑1): tritt nur bei ausdrücklichem `-Kataloge
     readonly` auf — ohne `-Kataloge` (Vorgabe seit #160‑E‑1a: `alle`) erneut
     aufrufen (siehe Schritt 3 oben).
   - **5** — fachlicher Abbruch: Meldung auf der Konsole lesen, betrifft die
     Quelle selbst (z. B. eine gescheiterte Prüfung).
6. Nach dem Lauf zurückmelden: die vollständige Konsolenausgabe, der Inhalt von
   `Setup\Vorlage\Kenndaten.sqlite.bericht.txt` und — sobald `ISCC.exe` lief —
   dessen Meldungen (Erfolg mit Pfad und Größe, oder der Fehlertext mit
   Zeilennummer im `.iss`).

**Lauf in der CI (Anwenderentscheid „#160‑E‑1: CI" vom 11.09.2026).** Dieselbe
Kette fährt der Job `installer` in `.github/workflows/windows.yml` auf
`windows-latest`, weil es Anwender und Agenten gibt, die weder Windows noch
Inno Setup zur Hand haben. Der Job stand bis zum 11.09.2026 in einer eigenen
Datei `setup.yml` (#176) und ist mit **#177** hierher verlegt: GitHub nimmt
eine Workflow-Datei erst dann in die Actions-Liste auf, wenn sie auf dem
Standardzweig `main` liegt, ein `workflow_dispatch` auf eine neue Datei eines
Nebenzweigs scheitert bis dahin mit 404. `windows.yml` ist bereits registriert
— ein `workflow_dispatch` mit `ref = <Zweig>` benutzt dessen Datei genau
dieses Zweigs, auch bevor er auf `main` steht. Ausgelöst wird der Setup-Bau
weiterhin **nur von Hand** — GitHub → Actions → **Windows** → *Run workflow* →
Häkchen „setup" (kein Push-, kein Zeitauslöser für diesen Zweig des Laufs: der
Windows-Läufer zählt doppelt, und Veröffentlichung, 186 MB Herstellerdaten und
LZMA2-Solidkompression füllen den Lauf; Zeitlimit 60 Minuten, zusätzliches
Häkchen „schnell" schaltet auf `lzma2/normal`). Ohne das Häkchen „setup" läuft
weiterhin nur der bisherige Job `build-test` — die zwei Jobs schließen sich
über ihre `if`-Bedingungen gegenseitig aus, ein Setup-Lauf fährt nicht
zusätzlich die Testkette. Der Job `installer` lädt den WebView2-Bootstrapper
nach (er steht in `.gitignore` und fehlt im Klon), prüft `ISCC.exe` im
Runner-Image und ruft dann Schritt 3 von oben mit `-Quelldatenbank
Referenzlaeufe/Kenndaten_Test.sqlite`, ohne `-Kataloge` (Vorgabe seit
#160‑E‑1a: `alle`) und ohne `-Beispiele` — das Repository führt keinen
gepflegten Beispielsatz, und ohne diesen Schalter bleibt die Vorlage
projektfrei (6.1, Schritt 4). Zurück kommen drei Dinge, 14 Tage
lang: der übersetzte Installer aus `Setup\Ausgabe`, der Prüfbericht der
Vorlage (er steht zusätzlich im Lauf selbst — er ist der Beleg, dass keines
der 24 Prüfprojekte in den Installer gewandert ist) und das Skriptprotokoll.
**Grenzen:** Das Ergebnis ist ein Prüfstück, kein Auslieferungsstand — die
Quelle ist die Testdatenbank, nicht der gepflegte Katalogstand. Signiert wird
nicht (Abschnitt 9; der Job hat bewusst keine Geheimnisse), installiert wird
nicht, gestartet wird nicht: Die Freigabeprobe oben bleibt Handarbeit auf
einer frischen Windows-Installation.

Der **erste** Setup-Lauf (34588593433, 11.09.2026) fiel rot: `ISCC.exe` stand
zwar im Runner-Image, aber `VersionInfo.FileVersion` lieferte dort den String
„0.0.0.0" statt der tatsächlich installierten Fassung 6.4.x, und die
6.3-Prüfung von `build-setup.ps1` brach folgerichtig ab. Seit **#180** ermittelt
die Funktion `IsccVersionErmitteln` die Version stattdessen über mehrere
Quellen der Reihe nach — `FileVersionRaw`, `ProductVersionRaw`, dieselben zwei
Zeichenketten von `Compil32.exe` daneben, zuletzt die `DisplayVersion` des
Registry-Schlüssels „Inno Setup 6_is1" — und nimmt die erste brauchbare; der
Schritt „Inno Setup bereitstellen" gibt seither zusätzlich alle geprüften
Rohwerte als `Diagnose: …`-Zeilen aus, damit ein künftiger Lauf sofort zeigt,
welche Quelle auf dem jeweiligen Image trägt. Die 6.3-Pflicht bleibt
unverändert; der Notschalter `-IsccVersionIgnorieren` ist für den
Arbeitsplatz-Notfall gedacht und wird im Workflow bewusst nicht gesetzt.

Der **zweite** Setup-Lauf (34591291099, 11.09.2026) kam an der 6.3-Prüfung
vorbei — die Versionsermittlung aus #180 trug; welche Quelle dabei zog, nennt
das Laufprotokoll. Veröffentlichung und Auslieferungsvorlage liefen
vollständig durch (Prüfbericht 0 Auffälligkeiten, 22,6 MB, Schemastand 73,
117 STRICT), erst `ISCC.exe` 6.7.1 selbst fiel — an der Kommentarklammer in
Zeile 436 des `[Code]`-Abschnitts (`{app}` schloss einen `{ … }`-Kommentar
vorzeitig, Spalte 37). Behoben mit Auftrag #181, der die Zeile umformuliert
und mit `Setup/pruefe_iss_kommentare.py` einen CI-Vorschritt „ISS-Kommentare
prüfen" vor „Setup bauen" einzieht, der denselben Fehler künftig vor dem
minutenlangen Lauf abfängt.

Der **dritte** Setup-Lauf (34592377805, Lauf Nr. 256, 11.09.2026, 11:05:56–11:10:10 UTC,
Kopf `d677c61` mit #181) ist **grün** — der erste vollständige Nachweis der Kette in der
CI. Dauer **4 min 14 s** statt der befürchteten Stunde: Checkout 25 s, .NET-SDK 39 s,
Inno-Setup-Prüfung 1 s, WebView2-Bootstrapper unter 1 s, ISS-Kommentar-Wächter 2 s,
„Setup bauen" **2 min 51 s** (Veröffentlichung win-x64 eigenständig: Version 1.1.0.0,
254,7 MB; Auslieferungsvorlage aus `Referenzlaeufe/Kenndaten_Test.sqlite` mit
`-Kataloge alle`: 22,6 MB, Prüfbericht 0 Auffälligkeiten, Schemastand 73, 117 STRICT,
0 Projekte, alle vier Datenschutzwächter grün; `ISCC.exe` 6.7.1 „Successful compile
54.000 sec", LZMA in eigenem Prozess `islzma64.exe`, 691 Compressing-Zeilen). Ergebnis
**`EPOS-Plan_Setup_1.1.0.0.exe`, 153,5 MB**, als Artefakt `epos-plan-setup` (160 455 754
Byte gepackt), dazu `setup-protokoll` (Prüfbericht und Skriptprotokoll), 14 Tage. **Welche
Versionsquelle trug:** Das Runner-Image führt `ISCC.exe` und `Compil32.exe` von Inno Setup
6.7.1 **ohne jede Versionsangabe** — `FileVersion`, `FileVersionRaw`, `ProductVersion` und
`ProductVersionRaw` beider Dateien melden 0.0.0.0; getragen hat die **Registry**
(`DisplayVersion = 6.7.1` des Uninstall-Schlüssels, `InstallLocation` passend), das Skript
meldet „Inno Setup 6.7.1 (Registry DisplayVersion)". Die Vier-Quellen-Ermittlung aus #180
war also keine Vorsicht, sondern nötig. Damit ist der Anwenderentscheid „#160‑E‑1: CI"
eingelöst (Aufgabe #176; Zwischenbefunde #177 Registrierung nur vom Standardzweig, #180
Versionsquelle, #181 Kommentarklammer); zum Zeitpunkt dieses Laufs blieb die Vorgabe
`-Kataloge readonly` des Werkzeugs unverändert — der Lauf übergab `alle` ausdrücklich
(6.1, #160‑F‑1). **Seit Anwenderentscheid #160‑E‑1a** (11.09.2026, Auftrag #182) ist die
Grundsatzfrage entschieden: Die Vorgabe des Werkzeugs ist jetzt `alle`, und der Job
übergibt seither keinen `-Kataloge`-Schalter mehr — Schritt 3 oben und `windows.yml`
zeigen den aktuellen Aufruf.

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
- **Keine Klimadaten** (E9). Die **Herstellerdaten** dagegen liegen seit dem
  06.09.2026 bei (E10) — der Unterschied ist die Größe: 186 MB gegen 300 MB,
  und die Herstellerdaten sind die Voraussetzung dafür, dass die Importmasken
  überhaupt etwas zu lesen finden.

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
| S5 | ~~Automatisierte Erzeugung der Auslieferungsdatenbank (6.1)~~ **Erledigt 09.09.2026 (Entscheid #157‑E‑2, Auftrag #160):** `Werkzeuge/Auslieferungsvorlage`, 17 Proben, Prüfbericht neben der Zieldatei. ~~Befund #160‑F‑1~~ **entschieden 11.09.2026 (Entscheid #160‑E‑1a, Auftrag #182):** Die Vorgabe des Werkzeugs ist jetzt `alle` — der Katalog wird vollständig ausgeliefert, `--kataloge readonly` bleibt als Schalter wählbar | Keiner |
| S6 | `Settings.Default.Upgrade()` beim Versionswechsel vorhanden? (7.7) | Im Code nachsehen |
| S7 | ~~Wird noch ein 64-Bit-Stand gebraucht?~~ **Erledigt 22.08.2026:** ja — EPOS-Plan ist vollständig auf x64 umgestellt, einen x86-Stand gibt es nicht mehr | Keiner. Herleitung und Abnahmeplan in [`Konzept_Umstellung_64Bit_EPOS-Plan.md`](../Konzept_Umstellung_64Bit_EPOS-Plan.md) |

---

## 12. Aufwand

| Paket | Inhalt | PT |
|---|---|---|
| S-1 | Setup-Skript einrichten, Symbol, Lizenz- und Liesmich-Text, erster Übersetzungslauf | 0,5 |
| S-2 | Änderungen an der Anwendung 7.1, 7.2, 7.5, 7.6 | 1,0 |
| S-3 | ~~Auslieferungsdatenbank: Bereinigung festlegen und einmal durchführen (6.1)~~ **erledigt 09.09.2026 als Werkzeug** (#160) | 1,0 |
| S-4 | Freigabeprobe auf frischer Windows-Installation, neun Fälle (Abschnitt 8) | 1,0 |
| S-5 | Assemblyumbenennung 7.3 mit Regressionsprobe | 0,5 |
| S-6 | Mutex 7.4, `Settings.Upgrade()` 7.7 | 0,5 |
| | **Summe ohne Signierung** | **4,5** |
| S-7 | Code-Signierung: Zertifikat beschaffen, Token einrichten, Kette einbauen | 0,5 PT + Beschaffungszeit und Zertifikatskosten |

Die ersten vier Pakete ergeben ein auslieferbares Setup. S-5 bis S-7 heben die
Außenwirkung — und sollten vor der ersten Auslieferung an zahlende Kunden
erledigt sein, nicht danach.
