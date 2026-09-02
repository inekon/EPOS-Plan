<#
.SYNOPSIS
    Baut EPOS-Plan als eigenständige 64-Bit-Veröffentlichung und übersetzt daraus
    das Installationsprogramm mit Inno Setup.

.DESCRIPTION
    Ein Aufruf erledigt beide Schritte:

      1. dotnet publish  (win-x64, eigenständig)
      2. ISCC.exe        Setup\EPOS-Plan.iss

    Das Ergebnis liegt anschließend unter Setup\Ausgabe.

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

.PARAMETER SkipPublish
    Überspringt die Veröffentlichung und übersetzt nur das Setup — praktisch,
    wenn nur am .iss gearbeitet wird.

.PARAMETER Schnell
    Übersetzt mit lzma2/normal statt lzma2/max. Etwa halbe Übersetzungszeit,
    größere Datei. Nur für Testläufe.

.PARAMETER Sign
    Signiert das fertige Setup mit signtool. Setzt -Thumbprint voraus.

.PARAMETER Thumbprint
    Fingerabdruck des Codesignaturzertifikats im Zertifikatspeicher.

.EXAMPLE
    .\build-setup.ps1

.EXAMPLE
    .\build-setup.ps1 -SkipPublish -Schnell
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [switch] $SkipPublish,
    [switch] $Schnell,
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
$VorlageDb  = Join-Path $SetupDir 'Vorlage\Kenndaten.accdb'
$AceZiel    = Join-Path $SetupDir 'Voraussetzungen\AccessDatabaseEngine_X64.exe'
$AceQuelle  = Join-Path $RepoDir  'AccessDatabaseEngine_X64.exe'

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

# Auslieferungsdatenbank. Bewusst KEIN Rueckgriff auf die Arbeitsdatenbank im
# Repository - die enthaelt reale Projektdaten und darf nicht ausgeliefert werden.
if (-not (Test-Path $VorlageDb)) {
    throw @"
Auslieferungsdatenbank fehlt: $VorlageDb

Sie ist nicht identisch mit WindowsFormsApplication1\Kenndaten.accdb - diese
enthaelt Projektdaten aus der Entwicklung und darf nicht ausgeliefert werden.
Erzeugung des Auslieferungsstands: siehe Konzept, Abschnitt 6.1.
"@
}

# Voraussetzungs-Installer bei Bedarf aus der Repowurzel uebernehmen.
if (-not (Test-Path $AceZiel)) {
    if (Test-Path $AceQuelle) {
        Hinweis 'AccessDatabaseEngine_X64.exe wird nach Setup\Voraussetzungen kopiert'
        New-Item -ItemType Directory -Force -Path (Split-Path $AceZiel) | Out-Null
        Copy-Item $AceQuelle $AceZiel
    }
    else {
        throw @"
Access Database Engine (64 Bit) nicht gefunden - weder $AceZiel noch $AceQuelle

Bezugsquelle: Microsoft-Download "Access Database Engine 2016 Redistributable,
64 Bit" (AccessDatabaseEngine_X64.exe). Die Datei gehoert unveraendert in die
Repowurzel; von dort uebernimmt dieses Skript sie nach Setup\Voraussetzungen.
"@
    }
}

# Inno-Setup-Uebersetzer suchen
$Kandidaten = @()
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
    throw 'ISCC.exe nicht gefunden. Inno Setup 6.3 oder neuer installieren: https://jrsoftware.org/isdl.php'
}

# 6.3 ist Pflicht: davor gibt es weder den Architekturbezeichner x64compatible
# noch UTF-8 ohne BOM.
$IsccVersion = [version]((Get-Item $Iscc).VersionInfo.FileVersion -replace '[^0-9.].*$', '')
if ($IsccVersion -lt [version]'6.3') {
    throw "Inno Setup $IsccVersion gefunden, benoetigt wird 6.3 oder neuer: $Iscc"
}
Hinweis "Inno Setup $IsccVersion : $Iscc"

# .NET SDK pruefen. Veroeffentlicht wird mit "dotnet publish": Seit dem
# 02.09.2026 haelt das Projekt keine COM-Referenzen mehr (Excel-Interop auf
# ClosedXML umgestellt), das frueher noetige MSBuild aus Visual Studio
# entfaellt damit.
if (-not $SkipPublish) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw @"
dotnet wurde nicht gefunden.

Benoetigt wird das .NET SDK in der Fassung aus global.json - 10.0.400 oder
hoeher im Band 10.0.x. Bezugsquelle: https://dotnet.microsoft.com/download
"@
    }

    $DotnetVersion = & dotnet --version
    Hinweis "dotnet SDK $DotnetVersion"
}

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
