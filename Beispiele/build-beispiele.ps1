<#
.SYNOPSIS
    Baut die Projektbeispiele und veroeffentlicht sie auf epos-plan.de.

.DESCRIPTION
    Konzept: claude/Konzept_Projektbeispiele_Dokumentation.md

    STAND: Geruest. Umgesetzt sind die Ermittlung der Beispiele, die
    Statusuebersicht und -Pruefen (Kapitel 9.2). Die Schritte 2 bis 7 des
    Bauvorgangs melden, welches Paket sie nachreicht.

    Die sieben Schritte je Beispiel (Konzept Kapitel 7):
      1. Pruefen      Schemata, Platzhalter gegen KennzahlenKatalog     B-1
      2. Rechnen      SimulationRunner headless -> ergebnis.json        B-2
      3. Vergleichen  gegen zuletzt veroeffentlichtes ergebnis.json     B-2
      4. Zeichnen     Diagramme ueber ScottPlot                         B-3
      5. Aufnehmen    Masken ueber den Aufnehmer nach Drehbuch          B-7
      6. Setzen       Platzhalter aufloesen, Markdown -> HTML           B-1
      7. Veroeffentl. WordPress REST, Marker, Idempotenz                B-4

.EXAMPLE
    .\build-beispiele.ps1
    Statusuebersicht aller Beispiele, ohne etwas zu bauen.

.EXAMPLE
    .\build-beispiele.ps1 -Pruefen
    Meldet, welche Beispiele wegen Codeaenderungen durchgesehen werden muessen.

.EXAMPLE
    .\build-beispiele.ps1 -Beispiel einfamilienhaus-waermepumpe -OhneBilder
    Baut ein Beispiel ohne Bildaufnahme (schnell, fuer Textarbeit).

.EXAMPLE
    .\build-beispiele.ps1 -Veroeffentlichen -Freigeben
    Laedt alle Beispiele hoch und schaltet sie live.
#>

[CmdletBinding()]
param(
    # Nur dieses Beispiel bearbeiten (Slug oder Ordnername).
    [string] $Beispiel,

    # Bildaufnahme ueberspringen. Diagramme und Masken bleiben, wie sie sind.
    [switch] $OhneBilder,

    # Nur die Pflegepruefung fahren, nichts bauen.
    [switch] $Pruefen,

    # Ergebnis nach WordPress laden - als Entwurf bzw. Revision.
    [switch] $Veroeffentlichen,

    # Zusammen mit -Veroeffentlichen: Seite live schalten.
    [switch] $Freigeben,

    # Abweichende Kennzahlen bestaetigen und ergebnis.json fortschreiben.
    [switch] $Uebernehmen,

    # Sprache der erzeugten Fassung.
    [ValidateSet('de', 'en')]
    [string] $Sprache = 'de',

    # Abbruchschwelle des Kennzahlvergleichs in Prozent (Konzept E11).
    [double] $Schwelle = 2.0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Wurzel        = Split-Path -Parent $PSScriptRoot
$script:BeispielePfad = $PSScriptRoot
$script:Offen         = @()

# ---------------------------------------------------------------------------
# Ausgabe
# ---------------------------------------------------------------------------

function Write-Titel([string] $Text) {
    Write-Host ''
    Write-Host $Text -ForegroundColor Cyan
    Write-Host ('-' * $Text.Length) -ForegroundColor DarkCyan
}

function Write-Schritt([int] $Nummer, [string] $Name, [string] $Status, [string] $Hinweis = '') {
    $farbe = switch ($Status) {
        'ok'      { 'Green' }
        'offen'   { 'DarkYellow' }
        'sprung'  { 'DarkGray' }
        default   { 'Red' }
    }
    $zeile = '  {0}. {1,-14} {2}' -f $Nummer, $Name, $Status
    Write-Host $zeile -ForegroundColor $farbe -NoNewline
    if ($Hinweis) { Write-Host "   $Hinweis" -ForegroundColor DarkGray } else { Write-Host '' }
}

function Add-Offen([string] $Paket, [string] $Was) {
    if ($script:Offen -notcontains "$Paket|$Was") { $script:Offen += "$Paket|$Was" }
}

# ---------------------------------------------------------------------------
# Beispiele ermitteln
#
# Die Feldermittlung arbeitet vorlaeufig mit regulaeren Ausdrucken auf den
# obersten Ebenen von beispiel.yaml. Paket B-1 ersetzt sie durch einen echten
# YAML-Leser (Modul powershell-yaml) samt Schemapruefung gegen schema/.
# ---------------------------------------------------------------------------

function Get-YamlFeld {
    param([string] $Inhalt, [string] $Pfad)

    $teile = $Pfad -split '\.'
    if ($teile.Count -eq 1) {
        if ($Inhalt -match ('(?m)^' + [regex]::Escape($teile[0]) + '\s*:\s*(.+?)\s*$')) {
            return $Matches[1].Trim('"', "'")
        }
        return $null
    }

    # Eine Ebene tiefer: Elternschluessel suchen, danach eingerueckte Zeile.
    $muster = '(?ms)^' + [regex]::Escape($teile[0]) + '\s*:\s*$(.*?)(?=^\S|\z)'
    if ($Inhalt -match $muster) {
        $block = $Matches[1]
        if ($block -match ('(?m)^\s+' + [regex]::Escape($teile[1]) + '\s*:\s*(.+?)\s*$')) {
            return $Matches[1].Trim('"', "'")
        }
    }
    return $null
}

function Get-YamlListe {
    param([string] $Inhalt, [string] $Schluessel)

    $muster = '(?ms)^' + [regex]::Escape($Schluessel) + '\s*:\s*$(.*?)(?=^\S|\z)'
    if ($Inhalt -match $muster) { $block = $Matches[1] } else { return @() }

    $eintraege = @()
    foreach ($zeile in ($block -split "`n")) {
        if ($zeile -match '^\s*-\s*(.+?)\s*$') {
            $wert = $Matches[1].Trim('"', "'")
            if ($wert -and -not $wert.StartsWith('#')) { $eintraege += $wert }
        }
    }
    return $eintraege
}

function Get-Beispiele {
    $gefunden = @()

    $ordner = Get-ChildItem -Path $script:BeispielePfad -Directory |
              Where-Object { $_.Name -match '^\d{2,3}-' } |
              Sort-Object Name

    foreach ($o in $ordner) {
        $yamlPfad = Join-Path $o.FullName 'beispiel.yaml'
        if (-not (Test-Path $yamlPfad)) {
            Write-Warning "$($o.Name): beispiel.yaml fehlt - uebersprungen."
            continue
        }
        $inhalt = Get-Content $yamlPfad -Raw -Encoding UTF8

        $gefunden += [pscustomobject]@{
            Ordner      = $o.Name
            Pfad        = $o.FullName
            Slug        = Get-YamlFeld $inhalt 'slug'
            Titel       = Get-YamlFeld $inhalt 'titel.de'
            Commit      = Get-YamlFeld $inhalt 'stand.commit'
            Version     = Get-YamlFeld $inhalt 'stand.programmversion'
            GepruefetAm = Get-YamlFeld $inhalt 'stand.geprueft_am'
            CodeAnker   = @(Get-YamlListe $inhalt 'code_anker')
            Bereiche    = @(Get-YamlListe $inhalt 'bereiche')
            HatErgebnis = Test-Path (Join-Path $o.FullName 'ergebnis.json')
            HatSeite    = Test-Path (Join-Path $o.FullName 'veroeffentlichung.json')
            Bilder      = @(Get-ChildItem -Path (Join-Path (Join-Path $o.FullName 'bilder') $Sprache) -Filter *.png -ErrorAction SilentlyContinue).Count
        }
    }

    if ($Beispiel) {
        $gefunden = $gefunden | Where-Object { $_.Slug -eq $Beispiel -or $_.Ordner -eq $Beispiel }
        if (-not $gefunden) { throw "Kein Beispiel mit Slug oder Ordner '$Beispiel' gefunden." }
    }

    return $gefunden
}

# ---------------------------------------------------------------------------
# Statusuebersicht  (umgesetzt)
# ---------------------------------------------------------------------------

function Show-Uebersicht($Liste) {
    Write-Titel 'Beispiele'

    if (-not $Liste) {
        Write-Host '  Noch kein Beispiel angelegt.' -ForegroundColor DarkYellow
        Write-Host '  .\werkzeuge\neues-beispiel.ps1 -Nummer 10 -Slug mein-beispiel -Titel "Mein Beispiel"' -ForegroundColor DarkGray
        return
    }

    $Liste |
        Select-Object @{n='Ordner';   e={$_.Ordner}},
                      @{n='Titel';    e={$_.Titel}},
                      @{n='Version';  e={$_.Version}},
                      @{n='Zahlen';   e={ if ($_.HatErgebnis) {'ja'} else {'-'} }},
                      @{n='Bilder';   e={$_.Bilder}},
                      @{n='Anker';    e={$_.CodeAnker.Count}},
                      @{n='Seite';    e={ if ($_.HatSeite) {'ja'} else {'-'} }},
                      @{n='Geprueft'; e={$_.GepruefetAm}} |
        Format-Table -AutoSize
}

# ---------------------------------------------------------------------------
# Pflegepruefung  (umgesetzt - Konzept Kapitel 9.2)
# ---------------------------------------------------------------------------

function Invoke-Pflegepruefung($Liste) {
    Write-Titel 'Pflegepruefung'

    Push-Location $script:Wurzel
    try {
        $null = git rev-parse --is-inside-work-tree 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host '  Kein Git-Repository gefunden - Pruefung nicht moeglich.' -ForegroundColor Red
            return
        }

        $pruefbeduerftig = 0
        $ohneAnker       = 0

        foreach ($b in $Liste) {
            if (-not $b.Commit) {
                Write-Host ('  {0,-38} ? kein stand.commit hinterlegt' -f $b.Ordner) -ForegroundColor DarkYellow
                $ohneAnker++
                continue
            }
            if ($b.CodeAnker.Count -eq 0) {
                Write-Host ('  {0,-38} ? Bezugsliste leer - meldet sich nie' -f $b.Ordner) -ForegroundColor DarkYellow
                $ohneAnker++
                continue
            }

            $treffer = @()
            foreach ($anker in $b.CodeAnker) {
                if (-not (Test-Path (Join-Path $script:Wurzel $anker))) {
                    $treffer += [pscustomobject]@{ Datei = $anker; Anzahl = -1; Letzte = 'Datei fehlt' }
                    continue
                }
                $log = git log --oneline "$($b.Commit)..HEAD" -- $anker 2>$null
                if ($LASTEXITCODE -ne 0) { continue }
                $zeilen = @($log | Where-Object { $_ })
                if ($zeilen.Count -gt 0) {
                    $datum = git log -1 --format=%ad --date=short -- $anker 2>$null
                    $treffer += [pscustomobject]@{ Datei = $anker; Anzahl = $zeilen.Count; Letzte = $datum }
                }
            }

            if ($treffer.Count -eq 0) {
                Write-Host ('  {0,-38} ok' -f $b.Ordner) -ForegroundColor Green
            } else {
                Write-Host ('  {0,-38} pruefbeduerftig' -f $b.Ordner) -ForegroundColor Yellow
                foreach ($t in $treffer) {
                    if ($t.Anzahl -lt 0) {
                        Write-Host ('      {0}  <- {1}' -f $t.Datei, $t.Letzte) -ForegroundColor Red
                    } else {
                        Write-Host ('      {0}  ({1} Commits, zuletzt {2})' -f $t.Datei, $t.Anzahl, $t.Letzte) -ForegroundColor DarkGray
                    }
                }
                $pruefbeduerftig++
            }
        }

        Write-Host ''
        Write-Host ('  {0} pruefbeduerftig, {1} ohne belastbare Bezugsliste, {2} in Ordnung.' -f `
                    $pruefbeduerftig, $ohneAnker, ($Liste.Count - $pruefbeduerftig - $ohneAnker))

        if ($pruefbeduerftig -gt 0) {
            Write-Host ''
            Write-Host '  Naechster Schritt: Auftrag an Claude Code, Konzept Kapitel 10.2.' -ForegroundColor DarkGray
        }
    }
    finally { Pop-Location }
}

# ---------------------------------------------------------------------------
# Die sieben Schritte  (Geruest)
# ---------------------------------------------------------------------------

function Invoke-Beispiel($B) {
    Write-Titel ('{0}  [{1}]' -f $B.Titel, $B.Ordner)

    # 1 - Pruefen
    $schemaDa = Test-Path (Join-Path $script:BeispielePfad 'schema\beispiel.schema.json')
    if ($schemaDa) {
        Write-Schritt 1 'Pruefen' 'offen' 'B-1: Schemapruefung und Platzhalterabgleich'
        Add-Offen 'B-1' 'Schemapruefung, Platzhalteraufloesung, Markdown -> HTML'
    } else {
        Write-Schritt 1 'Pruefen' 'fehler' 'schema/ fehlt'
    }

    # 2 - Rechnen
    Write-Schritt 2 'Rechnen' 'offen' 'B-2: SimulationRunner headless -> ergebnis.json'
    Add-Offen 'B-2' 'Kennzahlausgabe ueber BerichtsDatenSammler und KennzahlenKatalog'

    # 3 - Vergleichen
    if ($B.HatErgebnis) {
        Write-Schritt 3 'Vergleichen' 'offen' ("B-2: Schwelle {0} %" -f $Schwelle)
    } else {
        Write-Schritt 3 'Vergleichen' 'sprung' 'kein frueheres ergebnis.json - Erstlauf'
    }

    # 4 - Zeichnen
    if ($OhneBilder) {
        Write-Schritt 4 'Zeichnen' 'sprung' '-OhneBilder gesetzt'
    } else {
        Write-Schritt 4 'Zeichnen' 'offen' 'B-3: ScottPlot-Bildpfad des Berichts'
        Add-Offen 'B-3' 'Diagramme ueber den vorhandenen ScottPlot-Bildpfad'
    }

    # 5 - Aufnehmen
    if ($OhneBilder) {
        Write-Schritt 5 'Aufnehmen' 'sprung' '-OhneBilder gesetzt'
    } else {
        Write-Schritt 5 'Aufnehmen' 'offen' 'B-7: Aufnehmer in der Anwendung'
        Add-Offen 'B-7' 'Aufnehmer - setzt Argumentbehandlung in Program.Main voraus'
    }

    # 6 - Setzen
    Write-Schritt 6 'Setzen' 'offen' 'B-1: Platzhalter aufloesen, HTML erzeugen'

    # 7 - Veroeffentlichen
    if ($Veroeffentlichen) {
        $modus = if ($Freigeben) { 'live' } else { 'Entwurf' }
        Write-Schritt 7 'Veroeffentl.' 'offen' "B-4: WordPress REST ($modus)"
        Add-Offen 'B-4' 'Veroeffentlichung: REST, Marker, Idempotenz, Medienwiederverwendung'
    } else {
        Write-Schritt 7 'Veroeffentl.' 'sprung' '-Veroeffentlichen nicht gesetzt'
    }
}

# ---------------------------------------------------------------------------
# Ablauf
# ---------------------------------------------------------------------------

if ($Freigeben -and -not $Veroeffentlichen) {
    throw '-Freigeben wirkt nur zusammen mit -Veroeffentlichen.'
}

Write-Host ''
Write-Host 'EPOS-Plan - Projektbeispiele' -ForegroundColor White
Write-Host ('Repowurzel: {0}   Sprache: {1}' -f $script:Wurzel, $Sprache) -ForegroundColor DarkGray

$liste = @(Get-Beispiele)

if ($Pruefen) {
    Invoke-Pflegepruefung $liste
    Write-Host ''
    return
}

Show-Uebersicht $liste

if (-not $liste) { Write-Host ''; return }

foreach ($b in $liste) { Invoke-Beispiel $b }

if ($script:Offen.Count -gt 0) {
    Write-Titel 'Noch nicht umgesetzt'
    foreach ($e in ($script:Offen | Sort-Object)) {
        $t = $e -split '\|', 2
        Write-Host ('  {0}  {1}' -f $t[0], $t[1]) -ForegroundColor DarkYellow
    }
    Write-Host ''
    Write-Host '  Reihenfolge und Aufwand: Konzept Kapitel 11.' -ForegroundColor DarkGray
}

Write-Host ''
