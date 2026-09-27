# Startet den Wiki-Sammel-Upload. Fragt Benutzer und Passwort ab (Passwort verdeckt), speichert nichts auf der Platte.
# Aufruf: powershell -ExecutionPolicy Bypass -File Werkzeuge\WikiUpload\upload_start.ps1 [--trocken] [--nur 19,20] [--logbuch --version 1.2.0.5]
param([string[]]$Rest)
$hier = Split-Path -Parent $MyInvocation.MyCommand.Path
$user = Read-Host "Wiki-Benutzer (Bot-Passwort: Benutzer@Botname)"
$pw   = Read-Host "Wiki-Passwort" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pw)
try { $env:WIKI_BOT_PASS = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
$env:WIKI_BOT_USER = $user
$args2 = if ($Rest) { $Rest } else { @("--seiten") }
& py -3 (Join-Path $hier "wiki_upload.py") @args2
$env:WIKI_BOT_PASS = $null; $env:WIKI_BOT_USER = $null
Write-Host ""
Write-Host "Fertig. Ausgabe oben bitte an die Sitzung geben (Revisionen)."
Read-Host "Eingabetaste zum Schliessen"
