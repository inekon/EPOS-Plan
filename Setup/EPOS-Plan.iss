; ============================================================================
;  EPOS-Plan — Installationsprogramm
;  Inno Setup 6.3 oder neuer (wegen der Architekturbezeichner, siehe unten)
;
;  Ablage:      <Repo>\Setup\EPOS-Plan.iss
;  Übersetzen:  Setup\build-setup.ps1   (ruft dotnet publish und ISCC auf)
;  Von Hand:    ISCC.exe EPOS-Plan.iss  (setzt eine fertige Veröffentlichung
;                                        unter <Repo>\artifacts\publish\win-x86
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
; Solange das Projekt <AssemblyName>WindowsFormsApplication1</AssemblyName>
; trägt, muss hier dieser Name stehen. Nach der Umbenennung (Konzept 7.3) nur
; diese Zeile ändern — build-setup.ps1 liest den Namen aus dieser Datei.
#define AppExeName     "WindowsFormsApplication1.exe"

; SourcePath ist der Ordner dieser Datei. Ob er einen abschließenden Backslash
; trägt, sagt die Dokumentation nicht zu — deshalb wie in allen offiziellen
; Beispielen über AddBackslash().
#define SetupDir       AddBackslash(SourcePath)
#define RepoDir        SetupDir + "..\"

; Ergebnis von: dotnet publish -r win-x86 --self-contained true
#ifndef PublishDir
  #define PublishDir   RepoDir + "artifacts\publish\win-x86"
#endif

; Auslieferungsdatenbank — NICHT die Arbeitsdatenbank aus dem Repository!
; Wie dieser Stand erzeugt wird, steht im Konzept, Abschnitt 6.1.
#define VorlageDb      SetupDir + "Vorlage\Kenndaten.accdb"

; Microsoft Access Database Engine 2016 Redistributable, 32 Bit
#define AceInstaller   SetupDir + "Voraussetzungen\AccessDatabaseEngine.exe"

; Version. Einzige Quelle ist die gebaute EXE; build-setup.ps1 setzt sie über
; -p:Version an dotnet publish. Fehlt die Datei, würde
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

; Maschinenweite Installation nach "Programme (x86)"; die Anwenderdaten liegen
; je Windows-Konto (siehe Konzept, Abschnitt 2).
PrivilegesRequired=admin

; Dokumentiert die Zielplattform. x86compatible = jedes System, das 32-Bit-
; x86-Binärdateien ausführen kann, also x64 und ARM64 eingeschlossen.
; ACHTUNG beim Ändern: Das frühere "x86" bedeutet seit Inno Setup 6.3
; "x86os" — nur natives 32-Bit-Windows — und wäre hier falsch.
ArchitecturesAllowed=x86compatible
; ArchitecturesInstallIn64BitMode wird bewusst NICHT gesetzt: EPOS-Plan ist
; eine 32-Bit-Anwendung (Microsoft.ACE.OLEDB.12.0 ist bitness-gebunden) und
; gehört nach "Programme (x86)".

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
german.AceInstallieren=Microsoft Access Database Engine (32 Bit) wird installiert …
english.AceInstallieren=Installing Microsoft Access Database Engine (32-bit) …

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

german.AceFehlt=Die Microsoft Access Database Engine (32 Bit) konnte nicht installiert werden.%n%nOhne sie kann EPOS-Plan nicht auf seine Datenbank zugreifen.%n%nHäufigste Ursache ist ein installiertes 64-Bit-Microsoft-Office, das die 32-Bit-Engine blockiert. Der Weg dorthin steht in der Liesmich-Datei im Programmordner; im Zweifel hilft der Support weiter.%n%nDie Installation wird fortgesetzt.
english.AceFehlt=The Microsoft Access Database Engine (32-bit) could not be installed.%n%nWithout it EPOS-Plan cannot access its database.%n%nThe most common cause is an installed 64-bit Microsoft Office blocking the 32-bit engine. See the readme file in the program folder, or contact support.%n%nSetup will continue.

german.DatenLoeschen=Sollen auch die Projektdatenbank und die Einstellungen des angemeldeten Windows-Kontos gelöscht werden?%n%n%1%n%nDiese Daten lassen sich danach nicht wiederherstellen. Daten anderer Windows-Konten bleiben in jedem Fall erhalten und sind dort von Hand zu entfernen.
english.DatenLoeschen=Do you also want to delete the project database and settings of the signed-in Windows account?%n%n%1%n%nThis cannot be undone. Data belonging to other Windows accounts is always kept and must be removed there manually.


; ---------------------------------------------------------------------------
;  4. Auswahl
; ---------------------------------------------------------------------------

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
    Flags: ignoreversion recursesubdirs createallsubdirs

; Auslieferungsdatenbank als Vorlage. Sie wird nie direkt benutzt — die
; Anwendung legt daraus beim ersten Start die Datenbank des Kontos an.
Source: "{#VorlageDb}"; DestDir: "{app}\Vorlage"; Flags: ignoreversion

; Voraussetzung: nur mitnehmen, wenn sie auf diesem Rechner fehlt.
Source: "{#AceInstaller}"; DestDir: "{tmp}"; \
    Flags: deleteafterinstall; Check: not AceVorhanden

#if FileExists(SetupDir + "Lizenz.rtf")
Source: "{#SetupDir}Lizenz.rtf";   DestDir: "{app}"; Flags: ignoreversion
#endif
#if FileExists(SetupDir + "Liesmich.rtf")
Source: "{#SetupDir}Liesmich.rtf"; DestDir: "{app}"; Flags: ignoreversion
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
; HKLM32, weil das Setup im 32-Bit-Modus läuft.
Root: HKLM32; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
    ValueType: string; ValueName: "InstallDir"; ValueData: "{app}"; \
    Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKLM32; Subkey: "SOFTWARE\{#AppPublisher}\{#AppName}"; \
    ValueType: string; ValueName: "Version"; ValueData: "{#AppVersion}"; \
    Flags: uninsdeletevalue uninsdeletekeyifempty


; ---------------------------------------------------------------------------
;  9. Nach dem Kopieren
; ---------------------------------------------------------------------------

[Run]
; 9.1 Datenbanktreiber
Filename: "{tmp}\AccessDatabaseEngine.exe"; Parameters: "/quiet"; \
    StatusMsg: "{cm:AceInstallieren}"; \
    Check: not AceVorhanden; \
    Flags: waituntilterminated skipifdoesntexist; \
    AfterInstall: AceNachpruefen

; 9.2 Rechte am gemeinsamen Datenordner reparieren.
;     [Dirs] setzt die vererbenden Rechte am Ordner; Dateien einer
;     Vorgängerinstallation, deren Vererbung unterbrochen wurde, erreicht
;     zuverlässig nur icacls mit /T. Läuft deshalb nur, wenn der Ordner
;     bereits vor dieser Installation bestand.
Filename: "{sys}\icacls.exe"; \
    Parameters: """{commonappdata}\EPOS_PLAN"" /grant *S-1-5-32-545:(OI)(CI)M /T /C /Q"; \
    StatusMsg: "{cm:RechteSetzen}"; \
    Check: LegacyOrdnerVorhanden; \
    Flags: runhidden waituntilterminated

; 9.3 Programmstart anbieten
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
Type: dirifempty; Name: "{app}"


; ---------------------------------------------------------------------------
; 11. Prüfungen und Sonderfälle
; ---------------------------------------------------------------------------

[Code]

var
  G_LegacyOrdner:  Boolean;   { C:\ProgramData\EPOS_PLAN gab es schon vor dieser Installation }
  G_LegacyDb:      Boolean;   { … und darin lag eine Kenndaten.accdb }
  G_HinweisSeite:  TOutputMsgWizardPage;


{ ---- Voraussetzung: Microsoft.ACE.OLEDB.12.0 in der 32-Bit-Registrierung ----
  Die Anwendung fordert ausdrücklich 12.0 an; ein vorhandenes 16.0 allein
  genügt nicht. HKCR ist die zusammengeführte Sicht auf HKLM\SOFTWARE\Classes,
  eine zweite Prüfung dort wäre redundant. }
function AceVorhanden(): Boolean;
begin
  Result := RegKeyExists(HKCR32, 'Microsoft.ACE.OLEDB.12.0');
end;


{ Nach dem stillen Lauf des Redistributables prüfen, ob er tatsächlich
  gegriffen hat. Häufigster Fehlschlag: installiertes 64-Bit-Office. Die
  Installation wird nicht abgebrochen — ohne Treiber startet EPOS-Plan zwar,
  meldet aber beim ersten Datenbankzugriff einen Fehler. }
procedure AceNachpruefen();
begin
  if not AceVorhanden() then
    MsgBox(CustomMessage('AceFehlt'), mbError, MB_OK);
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
