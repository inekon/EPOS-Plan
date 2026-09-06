; ============================================================================
;  EPOS-Plan — Installationsprogramm
;  Inno Setup 6.3 oder neuer (wegen der Architekturbezeichner, siehe unten)
;
;  Ablage:      <Repo>\Setup\EPOS-Plan.iss
;  Übersetzen:  Setup\build-setup.ps1   (ruft dotnet publish und ISCC auf)
;  Von Hand:    ISCC.exe EPOS-Plan.iss  (setzt eine fertige Veröffentlichung
;                                        unter <Repo>\artifacts\publish\win-x64
;                                        voraus)
;
;  Konzept und Begründung der Entscheidungen:
;  Setup\Konzept_Setup_InnoSetup_EPOS-Plan.md
; ============================================================================


; ---------------------------------------------------------------------------
;  1. Bezeichner und Pfade
; ---------------------------------------------------------------------------

#define AppName        "EPOS-Plan"
#define AppPublisher   "INEKON"
#define AppURL         "https://epos-plan.de"
#define AppSupportURL  "https://epos-plan.de/support"

; Name der ausführbaren Datei im Veröffentlichungsordner.
; Das Projekt trägt seit dem 29.08.2026 <AssemblyName>EPOS_Plan</AssemblyName>
; (Umbenennung nach Konzept 7.3), daher dieser Name. Bei einer weiteren
; Umbenennung nur diese Zeile ändern — build-setup.ps1 liest den Namen aus
; dieser Datei.
#define AppExeName     "EPOS_Plan.exe"

; SourcePath ist der Ordner dieser Datei. Ob er einen abschließenden Backslash
; trägt, sagt die Dokumentation nicht zu — deshalb wie in allen offiziellen
; Beispielen über AddBackslash().
#define SetupDir       AddBackslash(SourcePath)
#define RepoDir        SetupDir + "..\"

; Ergebnis von dotnet publish (win-x64, eigenständig) — siehe build-setup.ps1.
#ifndef PublishDir
  #define PublishDir   RepoDir + "artifacts\publish\win-x64"
#endif

; Auslieferungsdatenbank — NICHT die Arbeitsdatenbank aus dem Repository!
; Wie dieser Stand erzeugt wird, steht im Konzept, Abschnitt 6.1.
#define VorlageDb      SetupDir + "Vorlage\Kenndaten.accdb"

; Herstellerdaten (VDI 3805 und die zwei CEC-Listen) — Anwenderentscheid W6-O-9
; vom 06.09.2026: „ja". Der Ordner liegt im Repository und wandert unveraendert
; nach {app}\VDI-3805-Daten; rund 186 MB (WP 134, KWK 25, PV 13, SPK 10,
; Pufferspeicher 4,4, Solarthermie 1,1). Er ist eine eigene, VORGEWAEHLTE und
; ABWAEHLBARE Komponente — siehe [Components].
#define HerstellerdatenDir  RepoDir + "VDI-3805-Daten"
#if !DirExists(HerstellerdatenDir)
  #error Der Ordner VDI-3805-Daten fehlt neben dem Setup-Ordner. Ohne ihn laesst sich die Komponente "Herstellerdaten" nicht packen (Konzept 6.3).
#endif

; Microsoft Access Database Engine 2016 Redistributable, 64 Bit
#define AceInstaller   SetupDir + "Voraussetzungen\AccessDatabaseEngine_X64.exe"

; Microsoft Edge WebView2 Runtime — der ONLINE-Bootstrapper (rund 2 MB), der
; die passende Fassung selbst nachlaedt. Gebraucht seit Paket iU8: Die neuen
; Dialoge sind Blazor-Komponenten und laufen in einer WebView2. Auf Windows 11
; ist die Laufzeit Bestandteil des Systems, auf Windows 10, LTSC und Server
; nicht zwingend.
#define WebView2Installer  SetupDir + "Voraussetzungen\MicrosoftEdgeWebview2Setup.exe"

; Version. Einzige Quelle ist die gebaute EXE; gepflegt wird sie in
; WindowsFormsApplication1\Properties\AssemblyInfo.cs
; (AssemblyFileVersion). Fehlt die Datei, würde
; GetVersionNumbersString einen leeren Wert liefern und das Setup mit
; unbrauchbarem Namen und leerer Version übersetzen — deshalb der Abbruch.
#ifndef AppVersion
  #define ExePfad      PublishDir + "\" + AppExeName
  #if !FileExists(ExePfad)
    #error Die Anwendung ist nicht gebaut. Zuerst Setup\build-setup.ps1 ausfuehren.
  #endif
  #define AppVersion   GetVersionNumbersString(ExePfad)
#endif


; ---------------------------------------------------------------------------
;  2. Grundeinstellungen
; ---------------------------------------------------------------------------

[Setup]
; AppId identifiziert das Produkt über alle Versionen hinweg.
; NIEMALS ändern — sonst erkennt ein Update die Vorgängerversion nicht mehr
; und beide stehen parallel in der Softwareliste.
AppId={{3033FD58-1082-4A6E-B1F7-9D0348A36F97}

AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Installationsprogramm
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppSupportURL}
AppUpdatesURL={#AppURL}

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableDirPage=no
DisableProgramGroupPage=yes
DisableWelcomePage=no

; Maschinenweite Installation nach "Programme"; die Anwenderdaten liegen
; je Windows-Konto (siehe Konzept, Abschnitt 2).
PrivilegesRequired=admin

; Dokumentiert die Zielplattform. x64compatible = jedes System, das 64-Bit-
; x64-Binärdateien ausführen kann, also x64-Windows und ARM64-Windows mit
; x64-Emulation.
ArchitecturesAllowed=x64compatible
; EPOS-Plan ist seit der Umstellung eine x64-Anwendung und braucht
; Microsoft.ACE.OLEDB.12.0 als 64-Bit-Engine. Mit dem 64-Bit-Modus zeigt
; {autopf} auf "Programme" und HKLM auf die 64-Bit-Registry-Sicht.
ArchitecturesInstallIn64BitMode=x64compatible

MinVersion=10.0

OutputDir={#SetupDir}Ausgabe
OutputBaseFilename={#AppName}_Setup_{#AppVersion}
UninstallDisplayName={#AppName} {#AppVersion}
UninstallDisplayIcon={app}\{#AppExeName}

WizardStyle=modern
; lzma2/max ist bereits der Standardwert und für die rund 350 MB richtig.
; Für Testläufe halbiert lzma2/normal die Übersetzungszeit —
; build-setup.ps1 -Schnell setzt dafür /DSchnell.
#ifdef Schnell
Compression=lzma2/normal
#else
Compression=lzma2/max
#endif
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Läuft EPOS-Plan noch, bietet der Restart Manager das Schließen an, statt
; mit "Datei in Benutzung" abzubrechen.
CloseApplications=yes
CloseApplicationsFilter=*.exe,*.dll,*.json,*.config
RestartApplications=no
; Sobald die Anwendung beim Start einen benannten Mutex setzt (Konzept 7.4),
; ist das der zuverlässigere Weg — dann diese Zeile aktivieren:
;AppMutex=Global\EPOS-Plan

; Optionale Gestaltungs- und Textdateien. Die #if-Abfragen sorgen dafür, dass
; das Skript auch dann übersetzt, wenn eine davon noch fehlt.
#if FileExists(SetupDir + "EPOS-Plan.ico")
SetupIconFile={#SetupDir}EPOS-Plan.ico
#endif
#if FileExists(SetupDir + "Lizenz.rtf")
LicenseFile={#SetupDir}Lizenz.rtf
#endif
#if FileExists(SetupDir + "Liesmich.rtf")
InfoBeforeFile={#SetupDir}Liesmich.rtf
#endif
#if FileExists(SetupDir + "WizardImage.bmp")
WizardImageFile={#SetupDir}WizardImage.bmp
#endif
#if FileExists(SetupDir + "WizardSmallImage.bmp")
WizardSmallImageFile={#SetupDir}WizardSmallImage.bmp
#endif

; Code-Signierung: erst aktivieren, wenn in den Inno-Setup-Einstellungen eine
; Signierwerkzeug-Definition namens "signtool" hinterlegt ist (Konzept 9).
;SignTool=signtool
;SignedUninstaller=yes


; ---------------------------------------------------------------------------
;  3. Sprachen
; ---------------------------------------------------------------------------

[Languages]
Name: "german";  MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"


[CustomMessages]
german.AceInstallieren=Microsoft Access Database Engine (64 Bit) wird installiert …
english.AceInstallieren=Installing Microsoft Access Database Engine (64-bit) …

german.WebView2Installieren=Microsoft Edge WebView2 Runtime wird installiert …
english.WebView2Installieren=Installing Microsoft Edge WebView2 Runtime …

german.RechteSetzen=Zugriffsrechte des gemeinsamen Datenordners werden gesetzt …
english.RechteSetzen=Setting permissions on the shared data folder …

german.DesktopSymbol=&Desktopsymbol anlegen
english.DesktopSymbol=Create a &desktop shortcut

german.Deinstallieren={#AppName} deinstallieren
english.Deinstallieren=Uninstall {#AppName}

german.Dokumentation=Dokumentation im Internet
english.Dokumentation=Online documentation

german.UebernahmeTitel=Vorhandene Datenbank gefunden
english.UebernahmeTitel=Existing database found
german.UebernahmeKopf=Ihre Projekte bleiben erhalten
english.UebernahmeKopf=Your projects will be kept
german.UebernahmeText=Auf diesem Rechner liegt bereits eine Datenbank unter%n%n    C:\ProgramData\EPOS_PLAN\Kenndaten.accdb%n%nAb dieser Version arbeitet EPOS-Plan mit einer Datenbank je Windows-Konto. Beim ersten Start übernimmt das Programm die vorhandene Datenbank einschließlich aller Projekte in Ihr Benutzerprofil. Die bisherige Datei bleibt unverändert liegen und kann nach einer Kontrolle von Hand entfernt werden.%n%nDas Setup selbst verändert Ihre Daten nicht.
english.UebernahmeText=This computer already holds a database at%n%n    C:\ProgramData\EPOS_PLAN\Kenndaten.accdb%n%nFrom this version on, EPOS-Plan uses one database per Windows account. On first start the application copies the existing database including all projects into your user profile. The previous file is left untouched and may be removed manually after verification.%n%nSetup itself does not modify your data.

german.Office32Hinweis=Auf diesem Rechner ist ein 32-Bit-Microsoft-Office installiert.%n%nDie 64-Bit-Access-Engine kann daneben von Microsoft offiziell nicht unterstützt installiert werden.%n%nDie Installation wird trotzdem versucht. Schlägt sie fehl, aktualisieren Sie Office auf 64 Bit oder folgen Sie dem Microsoft-Artikel KB 5004577.
english.Office32Hinweis=A 32-bit Microsoft Office is installed on this computer.%n%nMicrosoft does not officially support installing the 64-bit Access engine alongside it.%n%nSetup will try anyway. Should it fail, update Office to 64-bit or follow Microsoft article KB 5004577.

german.AceFehlt=Die Microsoft Access Database Engine (64 Bit) konnte nicht installiert werden.%n%nOhne sie kann EPOS-Plan nicht auf seine Datenbank zugreifen.%n%nHäufigste Ursache ist ein installiertes 32-Bit-Microsoft-Office, das die 64-Bit-Engine blockiert. Abhilfe ist ein Wechsel auf 64-Bit-Office oder der Weg aus dem Microsoft-Artikel KB 5004577; er steht auch in der Liesmich-Datei im Programmordner. Im Zweifel hilft der Support weiter.%n%nDie Installation wird fortgesetzt.
english.AceFehlt=The Microsoft Access Database Engine (64-bit) could not be installed.%n%nWithout it EPOS-Plan cannot access its database.%n%nThe most common cause is an installed 32-bit Microsoft Office blocking the 64-bit engine. Either switch Office to 64-bit or follow Microsoft article KB 5004577, which is also described in the readme file in the program folder. When in doubt, contact support.%n%nSetup will continue.

german.WebView2Fehlt=Die Microsoft Edge WebView2 Runtime konnte nicht installiert werden.%n%nOhne sie bleiben die neueren Dialoge von EPOS-Plan leer; alles Uebrige arbeitet weiter.%n%nHaeufigste Ursache ist eine fehlende Internetverbindung: Der mitgelieferte Installer laedt die Laufzeit nach. Sie laesst sich jederzeit nachtraeglich installieren — Bezugsquelle "Microsoft Edge WebView2" auf den Microsoft-Seiten. Im Zweifel hilft der Support weiter.%n%nDie Installation wird fortgesetzt.
english.WebView2Fehlt=The Microsoft Edge WebView2 Runtime could not be installed.%n%nWithout it the newer EPOS-Plan dialogs stay blank; everything else keeps working.%n%nThe most common cause is a missing internet connection: the bundled installer downloads the runtime. It can be installed later at any time — look for "Microsoft Edge WebView2" on the Microsoft pages. When in doubt, contact support.%n%nSetup will continue.

german.DatenLoeschen=Sollen auch die Projektdatenbank und die Einstellungen des angemeldeten Windows-Kontos gelöscht werden?%n%n%1%n%nDiese Daten lassen sich danach nicht wiederherstellen. Daten anderer Windows-Konten bleiben in jedem Fall erhalten und sind dort von Hand zu entfernen.
english.DatenLoeschen=Do you also want to delete the project database and settings of the signed-in Windows account?%n%n%1%n%nThis cannot be undone. Data belonging to other Windows accounts is always kept and must be removed there manually.

german.TypVoll=Vollständige Installation
english.TypVoll=Full installation
german.TypBenutzer=Benutzerdefinierte Installation
english.TypBenutzer=Custom installation

german.KompProgramm=Programm und Auslieferungsdatenbank
english.KompProgramm=Program and shipped database
german.KompHerstellerdaten=Herstellerdaten (VDI 3805, CEC)
english.KompHerstellerdaten=Manufacturer data (VDI 3805, CEC)


; ---------------------------------------------------------------------------
;  4. Auswahl
; ---------------------------------------------------------------------------

; 4.1 Bestandteile (W6-O-9, 06.09.2026). Das Programm ist "fixed" — es abzuwählen
;     ergäbe keine Installation. Die Herstellerdaten sind VORGEWÄHLT (sie stehen im
;     Typ "voll", und der ist der Vorschlag) und ABWÄHLBAR: Wer die 186 MB nicht
;     braucht — etwa, weil die Datensätze im Netz liegen und der Pfad in den
;     Einstellungen darauf zeigt —, nimmt das Häkchen heraus; Inno wechselt dann von
;     selbst auf den Typ "benutzerdefiniert".
[Types]
Name: "voll";   Description: "{cm:TypVoll}"
Name: "custom"; Description: "{cm:TypBenutzer}"; Flags: iscustom

[Components]
Name: "programm";        Description: "{cm:KompProgramm}";        Types: voll custom; Flags: fixed
Name: "herstellerdaten"; Description: "{cm:KompHerstellerdaten}"; Types: voll

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopSymbol}"; GroupDescription: "{cm:AdditionalIcons}"


; ---------------------------------------------------------------------------
;  5. Ordner
; ---------------------------------------------------------------------------

[Dirs]
; Gemeinsamer Datenordner. Im Regelbetrieb wird er für die Datenbank nicht
; mehr gebraucht (die liegt je Konto), bleibt aber für die Betriebsart
; "eine gemeinsame Datenbank für alle Konten" und für maschinenweite
; Protokolle bestehen. users-modify vergibt der Gruppe Benutzer vererbende
; Änderungsrechte — sprachneutral über die bekannte SID.
Name: "{commonappdata}\EPOS_PLAN"; Permissions: users-modify


; ---------------------------------------------------------------------------
;  6. Dateien
; ---------------------------------------------------------------------------

[Files]
; Die vollständige, eigenständige Veröffentlichung (Programm + .NET-Laufzeit +
; native Abhängigkeiten unter runtimes\ + Satellitenressourcen de-DE/en-US +
; Vorlagen\Berichtsvorlage.docx).
Source: "{#PublishDir}\*"; DestDir: "{app}"; \
    Excludes: "*.pdb,*.xml"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Components: programm

; Auslieferungsdatenbank als Vorlage. Sie wird nie direkt benutzt — die
; Anwendung legt daraus beim ersten Start die Datenbank des Kontos an.
Source: "{#VorlageDb}"; DestDir: "{app}\Vorlage"; Flags: ignoreversion; \
    Components: programm

; Herstellerdaten (W6-O-9). NEBEN das Programm, nicht nach {commonappdata}:
;   * Die Importmasken LESEN daraus und schreiben nie hinein — damit gehört der
;     Ordner in die Zeile "nur das Setup schreibt" der Rechtetabelle (Konzept 2.1),
;     also nach %ProgramFiles%\EPOS-Plan. Genau dort liegt aus demselben Grund
;     schon die Vorlagendatenbank ({app}\Vorlage).
;   * Ein Update ersetzt den Bestand mit dem Programm (ignoreversion), und die
;     Deinstallation nimmt ihn mit. %ProgramData%\EPOS_PLAN bleibt dagegen
;     ABSICHTLICH stehen (dort liegen Anwenderdaten) — 186 MB Auslieferungsbestand
;     blieben dann als Leiche zurück.
;   * Die Anwendung findet den Ordner ohne Einstellung: Der Pfaddienst sucht
;     VDI-3805-Daten von der laufenden EXE aus aufwärts und trifft ihn auf der
;     ersten Stufe (IPfade.Herstellerdaten, EinstellungenCtrl.HerstellerdatenpfadOderVorgabe).
Source: "{#HerstellerdatenDir}\*"; DestDir: "{app}\VDI-3805-Daten"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Components: herstellerdaten

; Voraussetzung: nur mitnehmen, wenn sie auf diesem Rechner fehlt.
Source: "{#AceInstaller}"; DestDir: "{tmp}"; \
    Flags: deleteafterinstall; Check: not AceVorhanden

Source: "{#WebView2Installer}"; DestDir: "{tmp}"; \
    Flags: deleteafterinstall; Check: not WebView2Vorhanden

#if FileExists(SetupDir + "Lizenz.rtf")
Source: "{#SetupDir}Lizenz.rtf";   DestDir: "{app}"; Flags: ignoreversion; Components: programm
#endif
#if FileExists(SetupDir + "Liesmich.rtf")
Source: "{#SetupDir}Liesmich.rtf"; DestDir: "{app}"; Flags: ignoreversion; Components: programm
#endif


; ---------------------------------------------------------------------------
;  7. Verknüpfungen
; ---------------------------------------------------------------------------

[Icons]
Name: "{group}\{#AppName}";          Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:Dokumentation}";  Filename: "{#AppURL}"
Name: "{group}\{cm:Deinstallieren}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";    Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon


; ---------------------------------------------------------------------------
;  8. Registry
; ---------------------------------------------------------------------------

[Registry]
; Für Support und für Werkzeuge, die den Installationsort brauchen.
; HKLM (also die 64-Bit-Sicht), weil das Setup seit der x64-Umstellung im
; 64-Bit-Modus läuft. Der alte HKLM32-Zweig einer 32-bit-Vorinstallation wird
; in AlteX86InstallationEntfernen() mitgenommen.
Root: HKLM; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
    ValueType: string; ValueName: "InstallDir"; ValueData: "{app}"; \
    Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKLM; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
    ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"; \
    Flags: uninsdeletevalue uninsdeletekeyifempty


; ---------------------------------------------------------------------------
;  9. Nach dem Kopieren
; ---------------------------------------------------------------------------

[Run]
; 9.1 Datenbanktreiber. BeforeInstall warnt vor der Mischbitness mit einem
;     vorhandenen 32-bit-Office, AfterInstall prüft den Erfolg nach.
Filename: "{tmp}\AccessDatabaseEngine_X64.exe"; Parameters: "/quiet"; \
    StatusMsg: "{cm:AceInstallieren}"; \
    Check: not AceVorhanden; \
    Flags: waituntilterminated skipifdoesntexist; \
    BeforeInstall: Office32Hinweisen; AfterInstall: AceNachpruefen

; 9.2 WebView2-Laufzeit. Der Bootstrapper laedt die passende Fassung online
;     nach und ist danach fertig; er bringt selbst keine Oberflaeche mit.
;     AfterInstall prueft den Erfolg nach — ohne die Laufzeit startet EPOS-Plan
;     zwar, aber jeder Blazor-Dialog bliebe leer.
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; \
    StatusMsg: "{cm:WebView2Installieren}"; \
    Check: not WebView2Vorhanden; \
    Flags: waituntilterminated skipifdoesntexist; \
    AfterInstall: WebView2Nachpruefen

; 9.3 Rechte am gemeinsamen Datenordner reparieren.
;     [Dirs] setzt die vererbenden Rechte am Ordner; Dateien einer
;     Vorgängerinstallation, deren Vererbung unterbrochen wurde, erreicht
;     zuverlässig nur icacls mit /T. Läuft deshalb nur, wenn der Ordner
;     bereits vor dieser Installation bestand.
Filename: "{sys}\icacls.exe"; \
    Parameters: """{commonappdata}\EPOS_PLAN"" /grant *S-1-5-32-545:(OI)(CI)M /T /C /Q"; \
    StatusMsg: "{cm:RechteSetzen}"; \
    Check: LegacyOrdnerVorhanden; \
    Flags: runhidden waituntilterminated

; 9.4 Programmstart anbieten
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; \
    WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent


; ---------------------------------------------------------------------------
; 10. Deinstallation
; ---------------------------------------------------------------------------

[UninstallDelete]
; Bewusst gezielt statt "{app}" pauschal: Der Anwender darf das
; Installationsverzeichnis frei wählen (DisableDirPage=no), und ein pauschales
; Löschen des gewählten Ordners kann fremde Daten mitnehmen.
Type: files;      Name: "{app}\*.log"
Type: files;      Name: "{app}\db_update_log.txt"
Type: filesandordirs; Name: "{app}\Vorlage"
; Herstellerdaten (W6-O-9): Der Deinstallierer entfernt zwar, was er selbst
; kopiert hat — aber nicht, was der Anwender nachträglich hineingelegt hat. Der
; Ordner gehört der Auslieferung; er geht mit ihr.
Type: filesandordirs; Name: "{app}\VDI-3805-Daten"
Type: dirifempty; Name: "{app}"


; ---------------------------------------------------------------------------
; 11. Prüfungen und Sonderfälle
; ---------------------------------------------------------------------------

[Code]

const
  { Uninstall-Schlüssel der 32-bit-Vorinstallation in der 32-Bit-Registry-Sicht.
    Der GUID-Teil muss zum AppId-Wert oben passen; dort ist die erste Klammer
    verdoppelt, weil das für den Übersetzer eine wörtliche Klammer bedeutet — in
    einer Pascal-Zeichenkette entfällt diese Verdopplung. }
  AltUninstallKey = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{3033FD58-1082-4A6E-B1F7-9D0348A36F97}_is1';

var
  G_LegacyOrdner:  Boolean;   { C:\ProgramData\EPOS_PLAN gab es schon vor dieser Installation }
  G_LegacyDb:      Boolean;   { … und darin lag eine Kenndaten.accdb }
  G_HinweisSeite:  TOutputMsgWizardPage;


{ ---- Voraussetzung: Microsoft.ACE.OLEDB.12.0 in der 64-Bit-Registrierung ----
  Die Anwendung fordert ausdrücklich 12.0 an; ein vorhandenes 16.0 allein
  genügt nicht. HKCR64 ist die 64-Bit-Sicht der zusammengeführten
  Klassenregistrierung — dort registriert sich die 64-Bit-Engine.
  Geprüft wird die ganze Kette ProgID → CLSID → InprocServer32 → Datei: Eine
  ProgID allein kann als Leiche ohne Server dastehen, wenn eine Engine unsauber
  entfernt wurde. }
function AceVorhanden(): Boolean;
var
  Clsid, Server: String;
begin
  Result := False;
  if RegQueryStringValue(HKCR64, 'Microsoft.ACE.OLEDB.12.0\CLSID', '', Clsid) then
    if RegQueryStringValue(HKCR64, 'CLSID\' + Clsid + '\InprocServer32', '', Server) then
    begin
      Server := RemoveQuotes(Server);
      { Pfade mit Umgebungsvariablen (REG_EXPAND_SZ) lassen sich hier nicht
        auflösen — sie gelten als vorhanden, statt fälschlich zu fehlen. }
      Result := (Pos('%', Server) > 0) or FileExists(Server);
    end;
end;


{ 32-Bit-Microsoft-Office auf diesem Rechner? Click-to-Run hinterlegt die
  Bitness in Configuration\Platform als 'x86' oder 'x64'. Je nachdem, welcher
  Installer den Schlüssel geschrieben hat, steht er in der 32- oder in der
  64-Bit-Sicht — deshalb beide prüfen. }
function Office32Vorhanden(): Boolean;
var
  Plattform: String;
begin
  Result := False;
  if RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\Office\ClickToRun\Configuration',
                         'Platform', Plattform) then
    Result := (CompareText(Plattform, 'x86') = 0);

  if not Result then
    if RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\Office\ClickToRun\Configuration',
                           'Platform', Plattform) then
      Result := (CompareText(Plattform, 'x86') = 0);
end;


{ Hinweis VOR dem stillen Lauf des Redistributables: Neben einem 32-Bit-Office
  ist die 64-Bit-Engine offiziell nicht unterstützt, sie lässt sich nur über den
  in KB 5004577 beschriebenen Weg daneben registrieren. Versucht wird es
  trotzdem, abgebrochen wird nichts. Hängt an der [Run]-Zeile, deren Check
  bereits sicherstellt, dass die Engine fehlt — die Meldung kommt daher genau
  einmal. }
procedure Office32Hinweisen();
begin
  if Office32Vorhanden() and (not AceVorhanden()) then
    MsgBox(CustomMessage('Office32Hinweis'), mbInformation, MB_OK);
end;


{ Nach dem stillen Lauf des Redistributables prüfen, ob er tatsächlich
  gegriffen hat. Häufigster Fehlschlag: installiertes 32-Bit-Office. Die
  Installation wird nicht abgebrochen — ohne Treiber startet EPOS-Plan zwar,
  meldet aber beim ersten Datenbankzugriff einen Fehler. }
procedure AceNachpruefen();
begin
  if not AceVorhanden() then
    MsgBox(CustomMessage('AceFehlt'), mbError, MB_OK);
end;


{ ---- Voraussetzung: Microsoft Edge WebView2 Runtime (iU8) ----
  Die Evergreen-Laufzeit traegt ihre Fassung unter der festen Produkt-GUID
  F3017226-FE2A-4295-8BDF-00C3A9A7E4C5 im EdgeUpdate-Zweig (in den beiden
  Zeichenketten unten steht sie mit den geschweiften Klammern, hier ohne -
  eine schliessende Klammer wuerde diesen Kommentar beenden). Microsoft
  dokumentiert genau diese Abfrage zur Erkennung.

  Beide Ablagen zaehlen: Die maschinenweite Installation schreibt nach
  HKLM (auf einem 64-Bit-System in die 32-Bit-Sicht WOW6432Node), die
  Installation je Benutzer nach HKCU. Eine davon genuegt.

  '0.0.0.0' ist ausdruecklich AUSGESCHLOSSEN: Diesen Wert hinterlaesst eine
  entfernte Laufzeit — der Schluessel steht dann noch da, die Laufzeit nicht.
  Derselbe Befund wie bei der ACE-Leiche oben. }
function WebView2Vorhanden(): Boolean;
var
  Fassung: String;
begin
  Result := False;

  if RegQueryStringValue(HKLM,
       'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
       'pv', Fassung) then
    Result := (Fassung <> '') and (Fassung <> '0.0.0.0');

  if not Result then
    if RegQueryStringValue(HKCU,
         'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
         'pv', Fassung) then
      Result := (Fassung <> '') and (Fassung <> '0.0.0.0');
end;


{ Nach dem stillen Lauf des Bootstrappers pruefen, ob er gegriffen hat.
  Haeufigster Fehlschlag: keine Internetverbindung — der Bootstrapper laedt die
  Laufzeit nach. Abgebrochen wird nichts: Ohne WebView2 bleiben nur die neueren
  Dialoge leer, das uebrige Programm arbeitet weiter. }
procedure WebView2Nachpruefen();
begin
  if not WebView2Vorhanden() then
    MsgBox(CustomMessage('WebView2Fehlt'), mbError, MB_OK);
end;


function LegacyOrdnerVorhanden(): Boolean;
begin
  Result := G_LegacyOrdner;
end;


function InitializeSetup(): Boolean;
begin
  { Zustand VOR der Installation festhalten — [Dirs] läuft vor [Run] und legt
    den Ordner sonst an, bevor die Check-Funktion ausgewertet wird. }
  G_LegacyOrdner := DirExists(ExpandConstant('{commonappdata}\EPOS_PLAN'));
  G_LegacyDb     := FileExists(ExpandConstant('{commonappdata}\EPOS_PLAN\Kenndaten.accdb'));
  Result := True;
end;


{ ---- Übernahme einer 32-bit-Vorinstallation (Konzept, Entscheidung 5.2) ----
  Dieses Setup installiert nach "Programme" und legt seinen Uninstall-Eintrag in
  der 64-Bit-Sicht an. Eine vorhandene 32-bit-Installation gilt damit NICHT als
  dieselbe Anwendung — es blieben zwei Einträge in "Apps und Features" und zwei
  Programmordner. Sie wird deshalb vorher still entfernt.
  Die Nutzdaten sind davon nicht berührt: Datenbank unter %ProgramData%\EPOS_PLAN
  bzw. je Windows-Konto, Lizenz und KI-Schlüssel unter %APPDATA%\wp-plan; der
  alte Deinstallierer fasst laut seinem [UninstallDelete] nur den Programmordner
  an. Seine Rückfrage nach den Kontodaten kommt mit Voreinstellung "Nein" und
  ist beim Setup-Test zu erwarten. }
procedure AlteX86InstallationEntfernen();
var
  Befehl: String;
  Ergebnis, Wartezeit: Integer;
begin
  if RegQueryStringValue(HKLM32, AltUninstallKey, 'UninstallString', Befehl) then
  begin
    Befehl := RemoveQuotes(Befehl);
    if FileExists(Befehl) then
    begin
      Exec(Befehl, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '',
           SW_HIDE, ewWaitUntilTerminated, Ergebnis);

      { Der Inno-Deinstallierer startet sich aus dem Temp-Ordner neu, damit er
        sich selbst löschen kann; Exec kehrt deshalb zurück, bevor die Arbeit
        getan ist. Also warten, bis sein Registry-Eintrag verschwunden ist —
        höchstens zwei Minuten, danach wird ohnehin fortgefahren. }
      Wartezeit := 0;
      while RegKeyExists(HKLM32, AltUninstallKey) and (Wartezeit < 120000) do
      begin
        Sleep(500);
        Wartezeit := Wartezeit + 500;
      end;
    end;
  end;

  { Rest der alten 32-Bit-Sicht: [Registry] schreibt jetzt nach HKLM, ein
    zurückgebliebener Zweig wäre eine Karteileiche. }
  if RegKeyExists(HKLM32, 'SOFTWARE\{#AppPublisher}\{#AppName}') then
    RegDeleteKeyIncludingSubkeys(HKLM32, 'SOFTWARE\{#AppPublisher}\{#AppName}');
end;


function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  { Bewusst hier und nicht in InitializeSetup: Erst an dieser Stelle steht fest,
    dass wirklich installiert wird. Früher entfernt, stünde ein Anwender, der
    den Assistenten noch abbricht, ganz ohne Programm da. }
  AlteX86InstallationEntfernen();
  Result := '';
end;


procedure InitializeWizard();
begin
  G_HinweisSeite := CreateOutputMsgPage(wpSelectTasks,
    CustomMessage('UebernahmeTitel'),
    CustomMessage('UebernahmeKopf'),
    CustomMessage('UebernahmeText'));
end;


function ShouldSkipPage(PageID: Integer): Boolean;
begin
  { Die Übernahme-Seite nur zeigen, wenn es wirklich eine Bestandsdatenbank gibt. }
  Result := (PageID = G_HinweisSeite.ID) and (not G_LegacyDb);
end;


procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Ordner: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    { Achtung: {localappdata} ist das Profil des Kontos, unter dem die
      Deinstallation läuft. Wird sie mit fremden Administratorrechten
      gestartet, bleiben die Daten des eigentlichen Anwenders liegen —
      der Meldungstext benennt das. }
    Ordner := ExpandConstant('{localappdata}\EPOS_PLAN');
    if DirExists(Ordner) then
      if MsgBox(FmtMessage(CustomMessage('DatenLoeschen'), [Ordner]),
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(Ordner, True, True, True);
  end;
end;
