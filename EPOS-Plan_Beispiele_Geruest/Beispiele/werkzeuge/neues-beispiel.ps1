<#
.SYNOPSIS
    Legt ein neues Projektbeispiel aus _vorlage/ an.

.EXAMPLE
    .\werkzeuge\neues-beispiel.ps1 -Nummer 40 -Slug schwimmbad -Titel "Schwimmbad"
#>

[CmdletBinding()]
param(
    # Reihenfolge auf der Uebersichtsseite. Zehnerschritte.
    [Parameter(Mandatory)]
    [ValidateRange(10, 990)]
    [int] $Nummer,

    # Adressbestandteil: kleinschreibung, Bindestriche, keine Umlaute.
    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9]+(-[a-z0-9]+)*$')]
    [string] $Slug,

    [Parameter(Mandatory)]
    [string] $Titel,

    [ValidateSet('einsteiger', 'fortgeschritten', 'experte')]
    [string] $Schwierigkeit = 'einsteiger'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$beispiele = Split-Path -Parent $PSScriptRoot
$vorlage   = Join-Path $beispiele '_vorlage'
$ordner    = '{0:D2}-{1}' -f $Nummer, $Slug
$ziel      = Join-Path $beispiele $ordner

if (-not (Test-Path $vorlage)) { throw "Vorlage nicht gefunden: $vorlage" }
if (Test-Path $ziel)           { throw "Ordner existiert bereits: $ordner" }

$doppelt = Get-ChildItem $beispiele -Directory |
           Where-Object { $_.Name -match ('^\d{2,3}-' + [regex]::Escape($Slug) + '$') }
if ($doppelt) { throw "Slug '$Slug' wird bereits verwendet: $($doppelt.Name)" }

Copy-Item $vorlage $ziel -Recurse
Remove-Item (Join-Path $ziel 'ausgabe') -Recurse -Force -ErrorAction SilentlyContinue
New-Item (Join-Path $ziel 'ausgabe') -ItemType Directory | Out-Null

# beispiel.yaml vorbelegen
$yamlPfad = Join-Path $ziel 'beispiel.yaml'
$yaml = Get-Content $yamlPfad -Raw -Encoding UTF8
$yaml = $yaml -replace '(?m)^slug:.*$',          "slug: $Slug"
$yaml = $yaml -replace '(?m)^reihenfolge:.*$',   "reihenfolge: $Nummer"
$yaml = $yaml -replace '(?m)^  de: Titel des Beispiels$', "  de: $Titel"
$yaml = $yaml -replace '(?m)^schwierigkeit:.*$', "schwierigkeit: $Schwierigkeit"

$ohneBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($yamlPfad, $yaml, $ohneBom)

Write-Host ''
Write-Host "Angelegt: $ordner" -ForegroundColor Green
Write-Host ''
Write-Host '  Naechste Schritte:' -ForegroundColor DarkGray
Write-Host '   1. Projekt in EPOS-Plan gegen Beispiele.accdb bauen - keine Kundendaten'
Write-Host '   2. beispiel.yaml ausfuellen, besonders bereiche und code_anker'
Write-Host '   3. text.md schreiben, Zahlen nur als Platzhalter'
Write-Host '   4. schritte.yaml festlegen'
Write-Host "   5. .\build-beispiele.ps1 -Beispiel $Slug"
Write-Host ''
