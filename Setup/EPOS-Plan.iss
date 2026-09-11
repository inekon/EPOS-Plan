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
; Seit dem Anwenderentscheid #157-E-1 (Weg W3, 09.09.2026) ist sie eine
; SQLITE-Datei; die Anwendung kopiert sie beim ersten Start in den Datenordner
; (EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs). Erzeugt wird sie vor
; jedem Uebersetzungslauf neu von build-setup.ps1 ueber das Werkzeug
; Werkzeuge/Auslieferungsvorlage; sie liegt deshalb nicht im Repository
; (.gitignore: Setup/Vorlage/*.sqlite). Konzept, Abschnitt 6.1.
#define VorlageDb      SetupDir + "Vorlage\Kenndaten.sqlite"
#if !FileExists(VorlageDb)
  #error Die Auslieferungsvorlage Setup\Vorlage\Kenndaten.sqlite fehlt. Sie wird von build-setup.ps1 ueber Werkzeuge\Auslieferungsvorlage erzeugt und gehoert NICHT ins Repository; siehe Konzept, Abschnitt 6.1.
#endif

; Herstellerdaten (VDI 3805 und die zwei CEC-Listen) — Anwenderentscheid W6-O-9
; vom 06.09.2026: „ja". Der Ordner liegt im Repository und wandert unveraendert
; nach {app}\VDI-3805-Daten; rund 186 MB (WP 134, KWK 25, PV 13, SPK 10,
; Pufferspeicher 4,4, Solarthermie 1,1). Er ist eine eigene, VORGEWAEHLTE und
; ABWAEHLBARE Komponente — siehe [Components].
#define HerstellerdatenDir  RepoDir + "VDI-3805-Daten"
#if !DirExists(HerstellerdatenDir)
  #error Der Ordner VDI-3805-Daten fehlt in der Repowurzel. Ohne ihn laesst sich die Komponente Herstellerdaten nicht packen; siehe Konzept, Entscheidung E10.
#endif

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
; EPOS-Plan ist seit der Umstellung eine x64-Anwendung. Mit dem 64-Bit-Modus
; zeigt {autopf} auf "Programme" und HKLM auf die 64-Bit-Registry-Sicht.
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
;
; Symbol des Installationsprogramms — Auftrag #229 (Anwenderwunsch 11.09.2026:
; "nehme das EPOS-ICON als Programm-Symbol"). EINE Quelle, kein zweites Bild:
; dieselbe Datei, die die Anwendung selbst über <ApplicationIcon> einbettet
; (WindowsFormsApplication1.csproj) — vorher lag hier kein Setup\EPOS-Plan.ico,
; und der Installer trug das Inno-Setup-Standardsymbol.
#if FileExists(RepoDir + "WindowsFormsApplication1\Resources\EPOS-Plan.ico")
SetupIconFile={#RepoDir}WindowsFormsApplication1\Resources\EPOS-Plan.ico
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

german.WebView2Fehlt=Die Microsoft Edge WebView2 Runtime konnte nicht installiert werden.%n%nOhne sie bleiben die neueren Dialoge von EPOS-Plan leer; alles Uebrige arbeitet weiter.%n%nHaeufigste Ursache ist eine fehlende Internetverbindung: Der mitgelieferte Installer laedt die Laufzeit nach. Sie laesst sich jederzeit nachtraeglich installieren — Bezugsquelle "Microsoft Edge WebView2" auf den Microsoft-Seiten. Im Zweifel hilft der Support weiter.%n%nDie Installation wird fortgesetzt.
english.WebView2Fehlt=The Microsoft Edge WebView2 Runtime could not be installed.%n%nWithout it the newer EPOS-Plan dialogs stay blank; everything else keeps working.%n%nThe most common cause is a missing internet connection: the bundled installer downloads the runtime. It can be installed later at any time — look for "Microsoft Edge WebView2" on the Microsoft pages. When in doubt, contact support.%n%nSetup will continue.

; Die Uebernahme-Seite (UebernahmeTitel/Kopf/Text) und die drei ACE-Meldungen
; (AceInstallieren, Office32Hinweis, AceFehlt) sind mit dem Anwenderentscheid
; #157-E-1 (Weg W3, 09.09.2026) ENTFALLEN: Access wurde beim Kunden nie
; produktiv eingesetzt, die Uebernahme eines Altbestands ist ein Hauswerkzeug
; (EposSqliteMigrator) und kein Kundenweg. Das Setup liefert stattdessen
; {app}\Vorlage\Kenndaten.sqlite aus.

; Seit Auftrag #161 (09.09.2026): Die Rückfrage zeigt den tatsächlichen, seit
; dem SQLite-Cutover für ALLE Windows-Konten dieses Rechners gemeinsamen
; Datenordner {commonappdata}\EPOS_PLAN (Datenbank samt Sicherungsordner
; DB-Backup) — vorher richtete sie sich an {localappdata}\EPOS_PLAN, einen
; Ordner, den nichts anlegt (Befund Auftrag #157). Der alte Satz "Daten
; anderer Windows-Konten bleiben in jedem Fall erhalten" traf deshalb nie zu:
; Ein gemeinsamer Ordner trifft beim Löschen zwangsläufig alle Konten. Die
; Registrierungseinstellungen (HKEY_CURRENT_USER\Software\wp-plan) und die
; zwei Datenverzeichnisse WP-Plan löscht der Code nach wie vor nicht — das
; sagt der Text jetzt ausdrücklich, statt es fälschlich mitzuversprechen.
german.DatenLoeschen=Soll auch die Datenbank samt Sicherungsordner unter%n%n    %1%n%ngelöscht werden?%n%nDieser Ordner gehört gemeinsam ALLEN Windows-Konten auf diesem Rechner — das Löschen trifft also nicht nur das angemeldete Konto, sondern auch die Projekte der anderen Konten. Ein Rückweg besteht danach nicht.%n%nErhalten bleiben in jedem Fall die beiden Datenverzeichnisse WP-Plan sowie die Registrierungseinstellungen des angemeldeten Kontos.
english.DatenLoeschen=Do you also want to delete the database and its backup folder under%n%n    %1%n%nThis folder is shared by ALL Windows accounts on this computer — deleting it therefore also removes the projects of the other accounts, not just those of the signed-in one. This cannot be undone.%n%nThe two WP-Plan data directories and the registry settings of the signed-in account are kept in any case.

; Neu seit Auftrag #161: DelTree scheiterte bislang still, wenn eine Datei im
; Ordner noch geöffnet war (z. B. eine laufende EPOS-Plan-Instanz oder ein
; Sicherungswerkzeug) — der Anwender glaubte dann an ein vollständiges
; Löschen, das nicht stattgefunden hatte.
german.DatenLoeschenFehlgeschlagen=Der Ordner%n%n    %1%n%nkonnte nicht vollständig gelöscht werden — vermutlich ist eine Datei darin noch geöffnet, etwa weil EPOS-Plan oder ein Sicherungswerkzeug noch läuft.%n%nSchließen Sie alle Programme, die auf diesen Ordner zugreifen, und entfernen Sie den Rest von Hand.
english.DatenLoeschenFehlgeschlagen=The folder%n%n    %1%n%ncould not be deleted completely — most likely a file inside it is still open, for example because EPOS-Plan or a backup tool is still running.%n%nClose every program accessing this folder and remove the remainder by hand.

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
; Gemeinsamer Datenordner. Er IST der Datenbankordner: DataRepository.GetDBPath
; fällt ohne gesetzte Einstellung DBPath auf %ProgramData%\EPOS_PLAN zurück, und
; genau dorthin kopiert die Anwendung beim ersten Start die ausgelieferte Vorlage
; {app}\Vorlage\Kenndaten.sqlite (Erstbereitstellung, #157-E-1 vom 09.09.2026).
; (Bis Auftrag #157, 09.09.2026, stand hier "die liegt je Konto" — eine Datenbank
; je Windows-Konto war ein Vorschlag des Setup-Konzepts, den der Code nie
; umgesetzt hat.)
; users-modify vergibt der Gruppe Benutzer vererbende Änderungsrechte —
; sprachneutral über die bekannte SID; ohne sie könnte die Anwendung die Vorlage
; hier gar nicht ablegen.
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

; Auslieferungsdatenbank als Vorlage. Sie wird nie direkt benutzt: Findet die
; Anwendung beim Start keine Datenbank im Datenordner, kopiert sie diese Datei
; einmalig dorthin und laesst sie danach unberuehrt liegen (Erstbereitstellung,
; Anwenderentscheid #157-E-1 vom 09.09.2026). Der Datenordner ist und bleibt
; %ProgramData%\EPOS_PLAN — der Ablageort aendert sich nicht.
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
; 9.1 WebView2-Laufzeit. Der Bootstrapper laedt die passende Fassung online
;     nach und ist danach fertig; er bringt selbst keine Oberflaeche mit.
;     AfterInstall prueft den Erfolg nach — ohne die Laufzeit startet EPOS-Plan
;     zwar, aber jeder Blazor-Dialog bliebe leer.
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; \
    StatusMsg: "{cm:WebView2Installieren}"; \
    Check: not WebView2Vorhanden; \
    Flags: waituntilterminated skipifdoesntexist; \
    AfterInstall: WebView2Nachpruefen

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


{ ---- KEINE Access-Engine mehr (Anwenderentscheid #157-E-1, Weg W3, 09.09.2026) ----
  Bis hierher standen an dieser Stelle AceVorhanden, Office32Vorhanden,
  Office32Hinweisen und AceNachpruefen: Das Setup schleppte den 64-Bit-Redist der
  Microsoft Access Database Engine mit und installierte ihn still nach, damit die
  Anwendung eine vorhandene Kenndaten.accdb uebernehmen konnte.

  Access wurde beim Kunden nie produktiv eingesetzt. Die Uebernahme eines
  Altbestands ist damit ein HAUSWERKZEUG (EposSqliteMigrator, Konsolenfassung) und
  kein Kundenweg; die Anwendung selbst kommt ohne Fremdtreiber aus
  (Microsoft.Data.Sqlite bringt die native Bibliothek mit). Die Datenbank einer
  Neuinstallation entsteht aus der Vorlage Kenndaten.sqlite im Unterordner Vorlage
  des Programmordners (Konstante app, ohne Klammern geschrieben - siehe den
  Hinweis im naechsten Kommentar).

  Der WebView2-Bootstrapper darunter bleibt: Ohne die Laufzeit startet EPOS-Plan
  seit iU9-W15c gar nicht. }

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
  (Dieselbe Falle hatte die gefallene ACE-Pruefung: eine ProgID ohne Server.) }
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
  Result := True;
end;


{ ---- Übernahme einer 32-bit-Vorinstallation (Konzept, Entscheidung 5.2) ----
  Dieses Setup installiert nach "Programme" und legt seinen Uninstall-Eintrag in
  der 64-Bit-Sicht an. Eine vorhandene 32-bit-Installation gilt damit NICHT als
  dieselbe Anwendung — es blieben zwei Einträge in "Apps und Features" und zwei
  Programmordner. Sie wird deshalb vorher still entfernt.
  Die Nutzdaten sind davon nicht berührt: Datenbank unter %ProgramData%\EPOS_PLAN,
  Lizenz und KI-Schlüssel unter %APPDATA%\wp-plan; der alte Deinstallierer fasst
  laut seinem [UninstallDelete] nur den Programmordner an. Seine Rückfrage nach
  den Kontodaten kommt mit Voreinstellung "Nein" und ist beim Setup-Test zu
  erwarten. }
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


{ InitializeWizard und ShouldSkipPage sind mit dem Anwenderentscheid #157-E-1
  (Weg W3, 09.09.2026) ENTFALLEN. Beide gab es nur fuer die eine zusaetzliche
  Assistentenseite "Vorhandene Datenbank gefunden", die den Anwender auf die
  einmalige Umstellung seiner Kenndaten.accdb vorbereitete; die Umstellung ist
  jetzt ein Hauswerkzeug und findet im Setup nicht mehr statt. }


procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Ordner: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    { Bis zum 09.09.2026 stand hier die Konstante localappdata, aufgelöst zu
      %LocalAppData%\EPOS_PLAN — dem Profil des Kontos, unter dem die
      Deinstallation läuft. Herkunft:
      Konzept_Setup_InnoSetup_EPOS-Plan.md, Abschnitt 6.2, schlug ursprünglich
      EINE Datenbank je Windows-Konto im Benutzerprofil vor; die
      SQLite-Umstellung hat das nie umgesetzt, DataRepository.GetDBPath kennt
      kein Benutzerprofil (siehe Kommentar bei [Dirs] oben). Die Rückfrage
      zielte damit auf einen Ordner, den nichts anlegt, und DirExists lieferte
      praktisch immer False — sie erschien de facto nie (Befund Auftrag #157).
      Richtiggestellt mit Auftrag #161 (09.09.2026): Die Datenbank samt dem
      Sicherungsordner DB-Backup liegt unter %ProgramData%\EPOS_PLAN — dem
      Ordner, den [Dirs] oben tatsächlich anlegt (dort als Konstante
      commonappdata), gemeinsam für alle Windows-Konten dieses Rechners. Die
      zwei Datenverzeichnisse WP-Plan und die Registrierungseinstellungen
      (HKEY_CURRENT_USER\Software\wp-plan) löscht dieser Code bewusst nicht —
      der bestehende Code hat sie noch nie gelöscht (er zielte ja nie auf die
      Registry, sondern auf einen Ordner, der nie entstand), und der
      Meldungstext sagt das jetzt auch so. Achtung beim Weiterschreiben dieses
      Kommentars: eine der Ordnerkonstanten oben wörtlich in geschweiften
      Klammern hineinzuschreiben würde ihn an deren schließender Klammer
      vorzeitig beenden, denn geschweifte Klammern kommentieren hier nicht
      verschachtelt (siehe die Warnung bei WebView2Vorhanden oben). }
    Ordner := ExpandConstant('{commonappdata}\EPOS_PLAN');
    if DirExists(Ordner) then
      if MsgBox(FmtMessage(CustomMessage('DatenLoeschen'), [Ordner]),
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        { DelTree meldet per Rückgabewert, ob wirklich alles weg ist — bei
          einer offenen Datei (laufendes EPOS-Plan, Sicherungswerkzeug) löscht
          es, was es kann, und lässt den Rest stehen. Was früher stillschweigend
          hingenommen wurde, meldet seit Auftrag #161 eine eigene Meldung. }
        if not DelTree(Ordner, True, True, True) then
          MsgBox(FmtMessage(CustomMessage('DatenLoeschenFehlgeschlagen'), [Ordner]),
                 mbError, MB_OK);
  end;
end;
