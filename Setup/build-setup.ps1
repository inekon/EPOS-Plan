<#
.SYNOPSIS
    Baut EPOS-Plan als eigenständige 64-Bit-Veröffentlichung und übersetzt daraus
    das Installationsprogramm mit Inno Setup.

.DESCRIPTION
    Ein Aufruf erledigt beide Schritte:

      1. dotnet publish  (win-x64, eigenständig)
      2. ISCC.exe        Setup\EPOS-Plan.iss

    Das Ergebnis liegt anschließend unter Setup\Ausgabe.

    Seit dem Anwenderentscheid #157-E-1 (Weg W3, 09.09.2026) kommt ein dritter
    Schritt DAVOR: die AUSLIEFERUNGSVORLAGE. Sie ist eine SQLite-Datei und wird
    vor jedem ISCC-Lauf neu erzeugt (Werkzeuge\Auslieferungsvorlage), weil das
    Setup sie nach {app}\Vorlage legt und die Anwendung sie beim ersten Start in
    den Datenordner kopiert. Die Vorlage liegt NICHT im Repository.

    Geprüft wird vorher unter anderem, dass der Ordner VDI-3805-Daten in der
    Repowurzel liegt: Er wird seit dem 06.09.2026 (Anwenderentscheid W6-O-9)
    als vorgewählte, abwählbare Komponente "Herstellerdaten (VDI 3805, CEC)"
    nach {app}\VDI-3805-Daten ausgeliefert - rund 186 MB unkomprimiert.

    Veröffentlicht wird mit "dotnet publish". Seit dem 02.09.2026 hält das
    Projekt keine COM-Referenzen mehr (Excel-Interop auf ClosedXML umgestellt),
    damit genügt "dotnet publish" — Visual Studio wird zum Bauen nicht mehr
    benötigt. Voraussetzung ist das .NET SDK laut global.json: 10.0.400 oder
    höher im Band 10.0.x.

    Es gibt nur noch eine Bitness — win-x64 (Konzept Umstellung 64 Bit,
    Entscheidung 5.1). Deshalb bewusst kein Plattform-Parameter.

    Die Versionsnummer wird NICHT hier vergeben. Sie steht in
    WindowsFormsApplication1\Properties\AssemblyInfo.cs
    (AssemblyFileVersion) und wandert von dort über die gebaute EXE in
    Setup-Dateinamen, Softwareliste und Registry. Grund: das Projekt setzt
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>, damit ist -p:Version
    am Publish-Aufruf wirkungslos.

.PARAMETER Quelldatenbank
    Die Datenbank, AUS DER die Auslieferungsvorlage erzeugt wird (ein gepflegter
    Katalogstand). Ersatzweise die Umgebungsvariable EPOS_VORLAGE_QUELLE. Es gibt
    bewusst KEINEN Rückgriff auf die Arbeitsdatenbank im Repository: Sie enthält
    reale Projektdaten und darf nicht ausgeliefert werden (Konzept, Abschnitt 6.1).

.PARAMETER Beispiele
    Wird unverändert als --beispiele an Werkzeuge\Auslieferungsvorlage
    durchgereicht (welche Beispielprojekte in der Auslieferung stehen sollen).
    Ohne Angabe entscheidet das Werkzeug mit seiner Vorgabe.

.PARAMETER Kataloge
    'readonly' oder 'alle' - wird als --kataloge <Wert> an
    Werkzeuge\Auslieferungsvorlage durchgereicht. Ohne Angabe entscheidet das
    Werkzeug selbst; dessen Katalogwächter (#160-F-1) bricht dabei mit Code 4
    ab, sobald die Quelle Katalogzeilen ohne ReadOnly = TRUE führt - also bei
    praktisch jeder echten Quelle. Bis zum Entscheid #160-E-1 deshalb mit
    -Kataloge alle aufrufen.

.PARAMETER VorlageNurPruefen
    Ruft das Vorlagenwerkzeug mit --trocken auf: Es rechnet und meldet, schreibt
    aber nichts. Nur sinnvoll zusammen mit einer bereits vorhandenen Vorlage.

.PARAMETER SkipPublish
    Überspringt die Veröffentlichung und übersetzt nur das Setup — praktisch,
    wenn nur am .iss gearbeitet wird. Die Auslieferungsvorlage wird trotzdem
    erzeugt: ISCC bricht ohne sie ab.

.PARAMETER Schnell
    Übersetzt mit lzma2/normal statt lzma2/max. Etwa halbe Übersetzungszeit,
    größere Datei. Nur für Testläufe.

.PARAMETER Iscc
    Pfad zu ISCC.exe oder zu dessen Ordner - für Anwender, die Inno Setup nicht
    zentral installiert haben, sondern z. B. im Setup-Ordner ihres
    Repository-Klons vorhalten (etwa C:\Waermeplan\WP_Plan\Setup). Suchreihenfolge:
    dieser Parameter, dann die Umgebungsvariable EPOS_ISCC, dann neben diesem
    Skript ($PSScriptRoot\ISCC.exe, $PSScriptRoot\Inno Setup 6\ISCC.exe oder ein
    Unterordner, der mit "Inno Setup" beginnt), dann
    %ProgramFiles%/%ProgramFiles(x86)%, dann die Registry. Ohne Angabe wie bisher.

    Die gefundene ISCC.exe muss 6.3 oder neuer sein. Seit #180 ermittelt
    IsccVersionErmitteln das ueber mehrere Quellen statt nur ueber
    VersionInfo.FileVersion: Lauf 34588593433 (11.09.2026) zeigte, dass dieser
    String auf windows-latest "0.0.0.0" liefert, obwohl das Image tatsaechlich
    Inno Setup 6.4.x fuehrt - FileVersionRaw, Compil32.exe daneben oder die
    Registry tragen dort weiterhin.

.PARAMETER IsccVersionIgnorieren
    Notschalter fuer den Fall, dass keine der Quellen aus IsccVersionErmitteln
    eine brauchbare Version liefert oder alle unter 6.3 liegen: Aus dem sonst
    ueblichen Abbruch wird dann nur ein Warnhinweis, das Skript baut ungeprueft
    weiter. Die 6.3-Pflicht selbst wird dadurch NICHT abgesenkt - der Schalter
    umgeht nur ihre Pruefung fuer einen einzelnen Lauf. Gedacht fuer den
    Notfall auf einem Arbeitsplatz mit bekannt guter, aber nicht erkennbarer
    Installation; im CI-Workflow bewusst NICHT gesetzt (#180).

.PARAMETER Sign
    Signiert das fertige Setup mit signtool. Setzt -Thumbprint voraus.

.PARAMETER Thumbprint
    Fingerabdruck des Codesignaturzertifikats im Zertifikatspeicher.

.EXAMPLE
    .\build-setup.ps1 -Quelldatenbank D:\Auslieferung\Kenndaten_Stand.sqlite

.EXAMPLE
    .\build-setup.ps1 -SkipPublish -Schnell
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $Quelldatenbank,
    [string[]] $Beispiele,
    [ValidateSet('readonly', 'alle')] [string] $Kataloge,
    [switch] $VorlageNurPruefen,
    [switch] $SkipPublish,
    [switch] $Schnell,
    [string] $Iscc,
    [switch] $IsccVersionIgnorieren,
    [switch] $Sign,
    [string] $Thumbprint
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# ---------------------------------------------------------------------------
#  Pfade
# ---------------------------------------------------------------------------

$SetupDir   = $PSScriptRoot
$RepoDir    = Split-Path -Parent $SetupDir
$Projekt    = Join-Path $RepoDir 'WindowsFormsApplication1\WindowsFormsApplication1.csproj'
$PublishDir = Join-Path $RepoDir 'artifacts\publish\win-x64'   # muss zu #define PublishDir passen
$IssDatei   = Join-Path $SetupDir 'EPOS-Plan.iss'
$AusgabeDir = Join-Path $SetupDir 'Ausgabe'
$VorlageDb  = Join-Path $SetupDir 'Vorlage\Kenndaten.sqlite'   # muss zu #define VorlageDb passen
$VorlageWerkzeug = Join-Path $RepoDir 'Werkzeuge\Auslieferungsvorlage'
$VdiOrdner  = Join-Path $RepoDir  'VDI-3805-Daten'            # muss zu #define HerstellerdatenDir passen
$WvZiel     = Join-Path $SetupDir 'Voraussetzungen\MicrosoftEdgeWebview2Setup.exe'
$WvQuelle   = Join-Path $RepoDir  'MicrosoftEdgeWebview2Setup.exe'

# MSBuild erwartet in PublishDir einen Ordner MIT abschliessendem Backslash,
# sonst landet die Ausgabe eine Ebene hoeher.
$PublishZiel = if ($PublishDir.EndsWith('\')) { $PublishDir } else { "$PublishDir\" }

function Schritt($text) { Write-Host "`n=== $text" -ForegroundColor Cyan }
function Hinweis($text) { Write-Host "    $text" -ForegroundColor DarkGray }

# ---------------------------------------------------------------------------
#  Vorbedingungen
# ---------------------------------------------------------------------------

Schritt 'Vorbedingungen pruefen'

if (-not (Test-Path $Projekt))  { throw "Projektdatei nicht gefunden: $Projekt" }
if (-not (Test-Path $IssDatei)) { throw "Inno-Setup-Skript nicht gefunden: $IssDatei" }

# Name der EXE aus dem .iss lesen, damit er nur an EINER Stelle gepflegt wird.
$IssText = Get-Content $IssDatei -Raw -Encoding UTF8
if ($IssText -match '(?m)^\s*#define\s+AppExeName\s+"([^"]+)"') {
    $ExeName = $Matches[1]
}
else {
    throw "In $IssDatei wurde kein '#define AppExeName ""...""' gefunden."
}
Hinweis "Anwendung: $ExeName"

# Auslieferungsvorlage (Anwenderentscheid #157-E-1, Weg W3 vom 09.09.2026).
# Sie wird weiter unten aus einer QUELLE erzeugt; hier wird nur frueh geprueft,
# dass die Quelle ueberhaupt benannt und vorhanden ist - ein Abbruch nach dem
# minutenlangen Publish waere aergerlich.
#
# Bewusst KEIN Rueckgriff auf die Arbeitsdatenbank im Repository: Die enthaelt
# reale Projektdaten und darf nicht ausgeliefert werden (Konzept, Abschnitt 6.1).
# Deshalb auch keine Vorgabe - wer nichts angibt, bekommt einen Abbruch und
# keinen stillen Griff in die naechstbeste Datei.
if (-not $Quelldatenbank) { $Quelldatenbank = $env:EPOS_VORLAGE_QUELLE }

if (-not $Quelldatenbank) {
    throw @"
Es ist keine Quelle fuer die Auslieferungsvorlage angegeben.

    .\build-setup.ps1 -Quelldatenbank <Pfad zur gepflegten Kenndaten.sqlite>

oder die Umgebungsvariable EPOS_VORLAGE_QUELLE setzen. Aus dieser Datei erzeugt
Werkzeuge\Auslieferungsvorlage den Auslieferungsstand
$VorlageDb.

Die Arbeitsdatenbank aus dem Repository ist ausdruecklich NICHT die Quelle: Sie
enthaelt reale Projektdaten (Konzept, Abschnitt 6.1).
"@
}

if (-not (Test-Path $Quelldatenbank)) {
    throw "Quelle fuer die Auslieferungsvorlage nicht gefunden: $Quelldatenbank"
}

if (-not (Test-Path $VorlageWerkzeug)) {
    throw @"
Werkzeug fuer die Auslieferungsvorlage nicht gefunden: $VorlageWerkzeug

Es gehoert zum Repository und erzeugt aus einem gepflegten Katalogstand die
Datei, die das Setup nach {app}\Vorlage legt (Konzept, Abschnitt 6.1).
"@
}
Hinweis "Vorlagenquelle: $Quelldatenbank"

# Herstellerdaten (VDI 3805 und die zwei CEC-Listen). Anwenderentscheid W6-O-9 vom
# 06.09.2026: Sie werden als eigene, vorgewaehlte Komponente ausgeliefert. Fehlt der
# Ordner, bricht schon ISCC mit #error ab - hier kommt der Abbruch frueher und mit
# der Bezugsquelle.
if (-not (Test-Path $VdiOrdner)) {
    throw @"
Herstellerdatenordner nicht gefunden: $VdiOrdner

Er gehoert zum Repository und wird als Komponente "Herstellerdaten (VDI 3805, CEC)"
nach {app}\VDI-3805-Daten ausgeliefert (Konzept 6.3, Anwenderentscheid W6-O-9).
Ohne ihn laesst sich das Setup nicht uebersetzen.
"@
}

$VdiMb = [math]::Round(((Get-ChildItem $VdiOrdner -Recurse -File |
          Measure-Object Length -Sum).Sum / 1MB), 1)
Hinweis "Herstellerdaten: $VdiMb MB in $VdiOrdner"

# Voraussetzungs-Installer bei Bedarf aus der Repowurzel uebernehmen.
#
# Die ACCESS DATABASE ENGINE ist mit dem Anwenderentscheid #157-E-1 (Weg W3,
# 09.09.2026) ENTFALLEN: Access wurde beim Kunden nie produktiv eingesetzt, die
# Uebernahme eines Altbestands ist ein Hauswerkzeug (EposSqliteMigrator) und kein
# Kundenweg. AccessDatabaseEngine_X64.exe wird weder in Setup\Voraussetzungen
# noch in der Repowurzel gebraucht; eine liegengebliebene Datei schadet nicht,
# wird aber nicht mehr mitgepackt.
#
# WebView2-Bootstrapper: Gebraucht seit Paket iU8 - die neuen Dialoge sind
# Blazor-Komponenten und laufen in einer WebView2; seit iU9-W15c startet die
# Anwendung ohne die Laufzeit gar nicht.
if (-not (Test-Path $WvZiel)) {
    if (Test-Path $WvQuelle) {
        Hinweis 'MicrosoftEdgeWebview2Setup.exe wird nach Setup\Voraussetzungen kopiert'
        New-Item -ItemType Directory -Force -Path (Split-Path $WvZiel) | Out-Null
        Copy-Item $WvQuelle $WvZiel
    }
    else {
        throw @"
WebView2-Bootstrapper nicht gefunden - weder $WvZiel noch $WvQuelle

Bezugsquelle: https://go.microsoft.com/fwlink/p/?LinkId=2124703
(Microsoft Edge WebView2 Runtime, "Evergreen Bootstrapper", rund 2 MB). Die
Datei gehoert unveraendert in die Repowurzel; von dort uebernimmt dieses Skript
sie nach Setup\Voraussetzungen.

Der Bootstrapper laedt die Laufzeit beim Anwender online nach. Wer offline
ausliefern muss, nimmt stattdessen den Standalone-Installer (rund 150 MB) oder
bindet eine Fixed-Version-Verteilung ein - das ist eine Anwenderentscheidung
und im Konzept unter 5.5 offen vermerkt.
"@
    }
}

# Inno-Setup-Uebersetzer suchen. Reihenfolge (Anwender fuehrt Inno Setup u. a. im
# Setup-Ordner seines Repository-Klons, z. B. C:\Waermeplan\WP_Plan\Setup, nicht
# zentral installiert): -Iscc-Parameter -> Umgebungsvariable EPOS_ISCC -> neben
# diesem Skript -> Program Files -> Registry. Parameter und Umgebungsvariable
# duerfen auf die EXE selbst ODER auf deren Ordner zeigen.
function ZuIsccPfad([string] $Pfad) {
    if (-not $Pfad) { return $null }
    if ($Pfad -like '*.exe') { return $Pfad }
    return (Join-Path $Pfad 'ISCC.exe')
}

# ---------------------------------------------------------------------------
#  ISCC-Version robust ermitteln (#180)
# ---------------------------------------------------------------------------
#
# Befund Lauf 34588593433 (11.09.2026, windows.yml Job "installer"): Auf
# windows-latest liefert "(Get-Item ISCC.exe).VersionInfo.FileVersion" den
# String "0.0.0.0", obwohl das Image tatsaechlich Inno Setup 6.4.x fuehrt -
# der StringFileInfo-Eintrag FileVersion traegt dort nicht (leer oder nicht
# gepflegt), waehrend FileVersionRaw (der feste VS_FIXEDFILEINFO-Versions-
# block), Compil32.exe daneben oder die Registry weiterhin brauchbare Werte
# liefern. IsccVersionErmitteln fragt deshalb mehrere Quellen der Reihe nach
# ab und nimmt die ERSTE brauchbare (Version >= 1.0).

function VersionAusString([string] $Roh) {
    # Bereinigt einen Versionsstring wie bisher ("-replace '[^0-9.].*$', ''")
    # und liefert [version], oder $null wenn nichts Brauchbares uebrig bleibt.
    # [version]'' wirft - deshalb die Leerpruefung davor.
    if (-not $Roh) { return $null }
    $Bereinigt = $Roh -replace '[^0-9.].*$', ''
    if (-not $Bereinigt) { return $null }
    try { return [version] $Bereinigt } catch { return $null }
}

function IsccDateiQuellen([string] $Datei, [string] $Kennung) {
    # Liefert die Quellen a-c (FileVersionRaw, ProductVersionRaw, FileVersion,
    # ProductVersion) fuer eine Datei (ISCC.exe oder Compil32.exe) als
    # [pscustomobject[]] mit Quelle/Roh - ungeprueft, ob eine davon brauchbar
    # ist. Jeder Eigenschaftszugriff ist unter Set-StrictMode -Version Latest
    # vorher gegen PSObject.Properties.Name geprueft.
    $DateiVorhanden = $false
    if ($Datei) { $DateiVorhanden = Test-Path $Datei -ErrorAction SilentlyContinue }
    if (-not $DateiVorhanden) {
        return @([pscustomobject]@{ Quelle = "$Kennung"; Roh = '(nicht vorhanden)' })
    }

    $vi = $null
    try { $vi = (Get-Item $Datei -ErrorAction Stop).VersionInfo }
    catch { $vi = $null }
    if (-not $vi) {
        return @([pscustomobject]@{ Quelle = "$Kennung VersionInfo"; Roh = '(nicht lesbar)' })
    }

    $Eigenschaften = $vi.PSObject.Properties.Name
    $Ergebnis = @()

    $RohFileVersionRaw = $null
    if ($Eigenschaften -contains 'FileVersionRaw' -and $null -ne $vi.FileVersionRaw) {
        $RohFileVersionRaw = $vi.FileVersionRaw.ToString()
    }
    $Ergebnis += [pscustomobject]@{ Quelle = "$Kennung FileVersionRaw"; Roh = $(if ($RohFileVersionRaw) { $RohFileVersionRaw } else { '(leer)' }) }

    $RohProductVersionRaw = $null
    if ($Eigenschaften -contains 'ProductVersionRaw' -and $null -ne $vi.ProductVersionRaw) {
        $RohProductVersionRaw = $vi.ProductVersionRaw.ToString()
    }
    $Ergebnis += [pscustomobject]@{ Quelle = "$Kennung ProductVersionRaw"; Roh = $(if ($RohProductVersionRaw) { $RohProductVersionRaw } else { '(leer)' }) }

    $RohFileVersion = $null
    if ($Eigenschaften -contains 'FileVersion' -and $vi.FileVersion) {
        $RohFileVersion = $vi.FileVersion
    }
    $Ergebnis += [pscustomobject]@{ Quelle = "$Kennung FileVersion"; Roh = $(if ($RohFileVersion) { $RohFileVersion } else { '(leer)' }) }

    $RohProductVersion = $null
    if ($Eigenschaften -contains 'ProductVersion' -and $vi.ProductVersion) {
        $RohProductVersion = $vi.ProductVersion
    }
    $Ergebnis += [pscustomobject]@{ Quelle = "$Kennung ProductVersion"; Roh = $(if ($RohProductVersion) { $RohProductVersion } else { '(leer)' }) }

    return $Ergebnis
}

function IsccVersionErmitteln([string] $Pfad) {
    # Quellen der Reihe nach: a) FileVersionRaw, b) ProductVersionRaw,
    # c) FileVersion/ProductVersion als String - jeweils von ISCC.exe, danach
    # d) dieselben vier von Compil32.exe im selben Ordner, danach e) die
    # Registry (DisplayVersion des Uninstall-Schluessels "Inno Setup 6_is1",
    # nur gewertet, wenn deren InstallLocation zum Ordner von ISCC.exe passt -
    # fehlt InstallLocation, zaehlt die Registry trotzdem als LETZTE Quelle,
    # mit Hinweis in der Diagnose). Zuletzt f) die Bannerzeile von "ISCC.exe"
    # ohne Argumente: Sie traegt nur die Hauptversion ("Inno Setup 6
    # Command-Line Compiler...") und zaehlt deshalb NIE als brauchbare Quelle
    # (eine blosse "6" belegt 6.3 nicht), erscheint aber in der Diagnose.
    #
    # Rueckgabe: [pscustomobject] mit
    #   Version  - [version] der ersten brauchbaren Quelle (>= 1.0), sonst $null
    #   Quelle   - Name dieser Quelle (String), sonst $null
    #   Diagnose - String[] "Quelle: Rohwert" ALLER geprueften Quellen, auch
    #              der erfolglosen - fuer die Fehlermeldung im Aufrufer.
    $Diagnose   = [System.Collections.Generic.List[string]]::new()
    $GefVersion = $null
    $GefQuelle  = $null

    foreach ($Eintrag in (IsccDateiQuellen $Pfad 'ISCC.exe')) {
        $Diagnose.Add("$($Eintrag.Quelle): $($Eintrag.Roh)")
        if (-not $GefVersion) {
            $v = VersionAusString $Eintrag.Roh
            if ($v -and $v -ge [version]'1.0') { $GefVersion = $v; $GefQuelle = $Eintrag.Quelle }
        }
    }

    $Compil32 = $null
    if ($Pfad) {
        $Ordner = Split-Path -Parent $Pfad
        if ($Ordner) { $Compil32 = Join-Path $Ordner 'Compil32.exe' }
    }
    foreach ($Eintrag in (IsccDateiQuellen $Compil32 'Compil32.exe')) {
        $Diagnose.Add("$($Eintrag.Quelle): $($Eintrag.Roh)")
        if (-not $GefVersion) {
            $v = VersionAusString $Eintrag.Roh
            if ($v -and $v -ge [version]'1.0') { $GefVersion = $v; $GefQuelle = $Eintrag.Quelle }
        }
    }

    try {
        $RegSchluessel = 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1'
        $RegDa = Test-Path $RegSchluessel -ErrorAction SilentlyContinue
        if ($RegDa) {
            $Reg = Get-ItemProperty $RegSchluessel -ErrorAction Stop
            $RegEigenschaften = $Reg.PSObject.Properties.Name

            $RohDisplayVersion = $null
            if ($RegEigenschaften -contains 'DisplayVersion' -and $Reg.DisplayVersion) {
                $RohDisplayVersion = $Reg.DisplayVersion
            }

            if ($RohDisplayVersion) {
                $RohInstallLocation = $null
                if ($RegEigenschaften -contains 'InstallLocation' -and $Reg.InstallLocation) {
                    $RohInstallLocation = $Reg.InstallLocation
                }

                $Passt = $false
                if ($RohInstallLocation -and $Pfad) {
                    $OrdnerIscc = Split-Path -Parent $Pfad
                    if ($OrdnerIscc) { $OrdnerIscc = $OrdnerIscc.TrimEnd('\') }
                    $OrdnerReg = $RohInstallLocation.TrimEnd('\')
                    if ($OrdnerIscc -and $OrdnerReg) {
                        $Passt = $OrdnerIscc.Equals($OrdnerReg, [System.StringComparison]::OrdinalIgnoreCase)
                    }
                }

                if ($Passt) {
                    $RegQuelle = 'Registry DisplayVersion'
                    $Diagnose.Add("${RegQuelle}: $RohDisplayVersion")
                }
                elseif (-not $RohInstallLocation) {
                    $RegQuelle = 'Registry DisplayVersion (ohne InstallLocation)'
                    $Diagnose.Add("${RegQuelle}: $RohDisplayVersion")
                }
                else {
                    $RegQuelle = $null
                    $Diagnose.Add("Registry DisplayVersion: $RohDisplayVersion (InstallLocation '$RohInstallLocation' passt nicht zu '$Pfad' - uebersprungen)")
                }

                if ($RegQuelle -and -not $GefVersion) {
                    $v = VersionAusString $RohDisplayVersion
                    if ($v -and $v -ge [version]'1.0') { $GefVersion = $v; $GefQuelle = $RegQuelle }
                }
            }
            else {
                $Diagnose.Add('Registry DisplayVersion: (leer)')
            }
        }
        else {
            $Diagnose.Add('Registry: Schluessel nicht vorhanden')
        }
    }
    catch {
        $Diagnose.Add("Registry: (Fehler: $($_.Exception.Message))")
    }

    $PfadVorhanden = $false
    if ($Pfad) { $PfadVorhanden = Test-Path $Pfad -ErrorAction SilentlyContinue }
    if ($PfadVorhanden) {
        try {
            $Zeile = & $Pfad 2>&1 | Select-Object -First 1
            $Diagnose.Add("ISCC.exe Bannerzeile: $Zeile")
        }
        catch {
            $Diagnose.Add("ISCC.exe Bannerzeile: (Fehler: $($_.Exception.Message))")
        }
    }

    return [pscustomobject]@{
        Version  = $GefVersion
        Quelle   = $GefQuelle
        Diagnose = $Diagnose.ToArray()
    }
}

function AbbruchOderWarnung([string] $Meldung, [switch] $Ignorieren) {
    # Buendelt die Entscheidung "werfen oder nur warnen" fuer -IsccVersionIgnorieren
    # an einer Stelle statt sie an jedem Aufrufer zu wiederholen.
    if ($Ignorieren) { Write-Warning $Meldung } else { throw $Meldung }
}

$Kandidaten = @()
if ($Iscc)          { $Kandidaten += (ZuIsccPfad $Iscc) }
if ($env:EPOS_ISCC) { $Kandidaten += (ZuIsccPfad $env:EPOS_ISCC) }

$Kandidaten += (Join-Path $PSScriptRoot 'ISCC.exe')
$Kandidaten += (Join-Path $PSScriptRoot 'Inno Setup 6\ISCC.exe')
Get-ChildItem $PSScriptRoot -Directory -Filter 'Inno Setup*' -ErrorAction SilentlyContinue |
    ForEach-Object { $Kandidaten += (Join-Path $_.FullName 'ISCC.exe') }

foreach ($pf in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
    if ($pf) { $Kandidaten += (Join-Path $pf 'Inno Setup 6\ISCC.exe') }
}
$RegPfad = 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1'
if (Test-Path $RegPfad) {
    $RegEintrag = Get-ItemProperty $RegPfad
    if ($RegEintrag.PSObject.Properties.Name -contains 'InstallLocation') {
        if ($RegEintrag.InstallLocation) {
            $Kandidaten += (Join-Path $RegEintrag.InstallLocation 'ISCC.exe')
        }
    }
}

$Iscc = $Kandidaten | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Iscc) {
    $KandidatenListe = ($Kandidaten | ForEach-Object { "  - $_" }) -join "`n"
    throw @"
ISCC.exe nicht gefunden. Geprueft wurden:
$KandidatenListe

Inno Setup 6.3 oder neuer installieren (https://jrsoftware.org/isdl.php) oder den
Pfad angeben - als Parameter:

    .\build-setup.ps1 ... -Iscc <Pfad zu ISCC.exe oder dessen Ordner>

oder ueber die Umgebungsvariable EPOS_ISCC.
"@
}

# 6.3 ist Pflicht: davor gibt es weder den Architekturbezeichner x64compatible
# noch UTF-8 ohne BOM. Die Version wird ueber IsccVersionErmitteln bestimmt
# (#180): Lauf 34588593433 (11.09.2026) zeigte, dass VersionInfo.FileVersion
# auf windows-latest "0.0.0.0" liefert, obwohl das Image Inno Setup 6.4.x
# fuehrt - FileVersionRaw, Compil32.exe daneben oder die Registry tragen dort
# weiterhin, deshalb der Faecher aus mehreren Quellen statt einer einzigen.
$IsccErmittlung   = IsccVersionErmitteln $Iscc
$IsccDiagnoseText = ($IsccErmittlung.Diagnose | ForEach-Object { "  - $_" }) -join "`n"

if (-not $IsccErmittlung.Version) {
    AbbruchOderWarnung -Ignorieren:$IsccVersionIgnorieren -Meldung @"
Inno-Setup-Version nicht ermittelbar: $Iscc

Geprueft wurden:
$IsccDiagnoseText

Benoetigt wird Inno Setup 6.3 oder neuer - die Pflicht bleibt bestehen. Mit
-IsccVersionIgnorieren laesst sich dieser Abbruch im Notfall auf einen
Warnhinweis absenken (siehe Kommentar zum Parameter im Skriptkopf).
"@
    Hinweis "Inno Setup Version unbekannt (mit -IsccVersionIgnorieren fortgesetzt) : $Iscc"
}
elseif ($IsccErmittlung.Version -lt [version]'6.3') {
    AbbruchOderWarnung -Ignorieren:$IsccVersionIgnorieren -Meldung @"
Inno Setup $($IsccErmittlung.Version) gefunden ($($IsccErmittlung.Quelle)), benoetigt wird 6.3 oder neuer: $Iscc

Geprueft wurden:
$IsccDiagnoseText
"@
    Hinweis "Inno Setup $($IsccErmittlung.Version) ($($IsccErmittlung.Quelle)), unter 6.3 - mit -IsccVersionIgnorieren fortgesetzt : $Iscc"
}
else {
    Hinweis "Inno Setup $($IsccErmittlung.Version) ($($IsccErmittlung.Quelle)) : $Iscc"
}

# .NET SDK pruefen. Veroeffentlicht wird mit "dotnet publish": Seit dem
# 02.09.2026 haelt das Projekt keine COM-Referenzen mehr (Excel-Interop auf
# ClosedXML umgestellt), das frueher noetige MSBuild aus Visual Studio
# entfaellt damit.
#
# Seit W3 wird dotnet AUCH BEI -SkipPublish gebraucht: Die Auslieferungsvorlage
# entsteht ueber "dotnet run --project Werkzeuge\Auslieferungsvorlage", und ohne
# sie bricht schon ISCC ab.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw @"
dotnet wurde nicht gefunden.

Benoetigt wird das .NET SDK in der Fassung aus global.json - 10.0.400 oder
hoeher im Band 10.0.x. Bezugsquelle: https://dotnet.microsoft.com/download
"@
}

$DotnetVersion = & dotnet --version
Hinweis "dotnet SDK $DotnetVersion"

# ---------------------------------------------------------------------------
#  Veroeffentlichung
# ---------------------------------------------------------------------------

if (-not $SkipPublish) {
    Schritt 'Veroeffentlichung bauen (win-x64, eigenstaendig)'

    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

    # "dotnet publish" stellt die Pakete selbst wieder her; der frueher noetige
    # eigene Wiederherstellungsdurchgang (-restore gegen NETSDK1004) entfaellt.
    & dotnet publish $Projekt -c $Configuration -r win-x64 --self-contained true `
        -p:Platform=x64 -p:PublishDir=$PublishZiel `
        -p:DebugType=none -p:DebugSymbols=false -nologo -v:m
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish ist mit Code $LASTEXITCODE fehlgeschlagen." }
}
else {
    Schritt 'Veroeffentlichung uebersprungen'
}

$ExePfad = Join-Path $PublishDir $ExeName
if (-not (Test-Path $ExePfad)) {
    throw "Veroeffentlichung unvollstaendig - $ExeName fehlt in $PublishDir"
}

$Version = (Get-Item $ExePfad).VersionInfo.FileVersion
if (-not $Version -or $Version -eq '0.0.0.0') {
    throw @"
Die gebaute EXE traegt keine brauchbare Versionsnummer ('$Version').

Sie wird in WindowsFormsApplication1\Properties\AssemblyInfo.cs gepflegt
(AssemblyFileVersion). -p:Version am Publish-Aufruf wirkt nicht, solange
<GenerateAssemblyInfo>false</GenerateAssemblyInfo> gesetzt ist.
"@
}

$groesse = [math]::Round(((Get-ChildItem $PublishDir -Recurse -File |
            Measure-Object Length -Sum).Sum / 1MB), 1)
Hinweis "Version $Version, $groesse MB in $PublishDir"

# ---------------------------------------------------------------------------
#  Auslieferungsvorlage erzeugen (#157-E-1, Weg W3)
# ---------------------------------------------------------------------------
#
# VOR ISCC, weil das .iss die Datei mit #if !FileExists(VorlageDb) prueft und
# sie sonst gar nicht erst uebersetzt. Erzeugt wird IMMER neu: Eine
# liegengebliebene Vorlage aus einem frueheren Lauf waere der leiseste denkbare
# Auslieferungsfehler.

Schritt 'Auslieferungsvorlage erzeugen'

New-Item -ItemType Directory -Force -Path (Split-Path $VorlageDb) | Out-Null

# Der Trockenlauf schreibt nichts und braucht die vorhandene Datei noch.
if ((-not $VorlageNurPruefen) -and (Test-Path $VorlageDb)) { Remove-Item $VorlageDb -Force }

$vorlageArgs = @($Quelldatenbank, $VorlageDb)
if ($Beispiele)          { $vorlageArgs += '--beispiele'; $vorlageArgs += $Beispiele }
if ($Kataloge)           { $vorlageArgs += '--kataloge'; $vorlageArgs += $Kataloge }
if ($VorlageNurPruefen)  { $vorlageArgs += '--trocken' }

& dotnet run --project $VorlageWerkzeug -c Release -- @vorlageArgs
if ($LASTEXITCODE -ne 0) {
    if ($LASTEXITCODE -eq 4) {
        throw @"
Werkzeuge\Auslieferungsvorlage ist mit Code 4 fehlgeschlagen (Katalogwaechter,
#160-F-1): Die Quelle fuehrt Katalogzeilen ohne ReadOnly = TRUE. Bis zum
Entscheid #160-E-1 mit -Kataloge alle aufrufen.
"@
    }
    throw "Werkzeuge\Auslieferungsvorlage ist mit Code $LASTEXITCODE fehlgeschlagen - kein Setup gebaut."
}

if (-not $VorlageNurPruefen) {
    if (-not (Test-Path $VorlageDb)) {
        throw @"
Das Werkzeug meldete Erfolg, aber die Auslieferungsvorlage fehlt: $VorlageDb

Ohne sie laesst sich das Setup nicht uebersetzen (#define VorlageDb im .iss).
"@
    }

    $vorlageMb = [math]::Round(((Get-Item $VorlageDb).Length / 1MB), 1)
    Hinweis "Auslieferungsvorlage: $vorlageMb MB in $VorlageDb"
}
else {
    Hinweis 'Trockenlauf - die vorhandene Vorlage bleibt, wie sie ist.'
    if (-not (Test-Path $VorlageDb)) {
        throw "Trockenlauf ohne vorhandene Vorlage: $VorlageDb - ISCC braeuchte sie."
    }
}


# ---------------------------------------------------------------------------
#  Setup uebersetzen
# ---------------------------------------------------------------------------

Schritt 'Setup uebersetzen'

New-Item -ItemType Directory -Force -Path $AusgabeDir | Out-Null

$isccArgs = @()
if ($Schnell) { $isccArgs += '/DSchnell' }   # wertloses /D - kein Quoting-Problem
$isccArgs += $IssDatei

& $Iscc @isccArgs
if ($LASTEXITCODE -ne 0) { throw "ISCC ist mit Code $LASTEXITCODE fehlgeschlagen." }

$Setup = Get-ChildItem $AusgabeDir -Filter 'EPOS-Plan_Setup_*.exe' |
         Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $Setup) { throw "Kein Setup in $AusgabeDir gefunden." }

# ---------------------------------------------------------------------------
#  Signieren
# ---------------------------------------------------------------------------

if ($Sign) {
    Schritt 'Setup signieren'
    if (-not $Thumbprint) { throw 'Fuer -Sign wird -Thumbprint benoetigt.' }

    $KitBin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    if (-not (Test-Path $KitBin)) { throw "Windows SDK nicht gefunden: $KitBin" }

    $SignTool =
        Get-ChildItem $KitBin -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\(\d+(\.\d+){2,3})\\x64\\signtool\.exe$' } |
        Sort-Object { [version]($_.FullName -replace '.*\\(\d+(?:\.\d+){2,3})\\x64\\.*', '$1') } |
        Select-Object -Last 1
    if (-not $SignTool) { throw "signtool.exe (x64) nicht gefunden unter $KitBin" }
    Hinweis $SignTool.FullName

    & $SignTool.FullName sign /sha1 $Thumbprint /fd SHA256 `
        /tr http://timestamp.digicert.com /td SHA256 $Setup.FullName
    if ($LASTEXITCODE -ne 0) { throw "Signieren ist mit Code $LASTEXITCODE fehlgeschlagen." }
}

# ---------------------------------------------------------------------------
#  Ergebnis
# ---------------------------------------------------------------------------

$Setup.Refresh()
$mb = [math]::Round($Setup.Length / 1MB, 1)
Write-Host "`nFertig: $($Setup.FullName)  ($mb MB)" -ForegroundColor Green
