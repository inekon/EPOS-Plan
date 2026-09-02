#Requires -Version 7.0
<#
.SYNOPSIS
    Erzeugt das SQLite-Zielschema von EPOS-Plan aus der Access-Quelle (Arbeitspaket S2).

.DESCRIPTION
    Liest C:\ProgramData\EPOS_PLAN\Kenndaten.accdb STRIKT LESEND ueber DAO.DBEngine.120
    (nur DAO zeigt Autowert, Kaskadenattribute und Memo zuverlaessig) und schreibt nach
    sql\schema\:

        001_grundschema.sql    CREATE TABLE ... STRICT inkl. PRIMARY KEY und FOREIGN KEY
        002_views.sql          die 14 Views
        003_indizes_fk.sql     CREATE [UNIQUE] INDEX (FKs stehen in 001, s. Kopfkommentar dort)
        SchemaTypKatalog.g.cs  Bool-/Datum-Spaltennamen fuer die Typangleichung in S4
        typkatalog.json        dieselbe Information mit exakter Tabelle->Spalte-Zuordnung
        inventar.json          vollstaendiges Strukturinventar + Gesamtzaehlungen

    Der Lauf ist wiederholbar und deterministisch: Tabellen alphabetisch ORDINAL sortiert,
    Spalten in Original-Ordinalreihenfolge, Indizes und Fremdschluessel je Tabelle ordinal
    nach Namen. Es wird nichts an der Quelldatenbank und nichts an der Anwendung geaendert.

.PARAMETER Quelle
    Pfad zur Access-Quelle. Vorgabe: C:\ProgramData\EPOS_PLAN\Kenndaten.accdb

.PARAMETER Ausgabe
    Zielverzeichnis. Vorgabe: ..\schema (relativ zum Skriptverzeichnis)

.PARAMETER AutowertPK
    Behandlung von Autowertspalten, die in Access nicht alleiniger Primaerschluessel sind
    (gemessen: 50 von 80 - 49 mehrspaltige PKs mit Autowert plus Tab_Projekt).

      AutowertBevorzugen  (Vorgabe) Die Autowertspalte wird alleiniger
                          INTEGER PRIMARY KEY AUTOINCREMENT. Alle Spalten des urspruenglichen
                          Access-PK behalten NOT NULL; ist die Eindeutigkeit des alten
                          PK-Tupels nicht schon durch den Autowert impliziert, wird sie durch
                          einen UNIQUE-Index in 003 gesichert. Verlustfrei, siehe Protokoll.
      Originaltreu        Der Access-PK wird woertlich uebernommen; Autowertspalten ohne
                          alleinigen PK werden INTEGER NOT NULL OHNE AUTOINCREMENT
                          (die Anwendung muesste die IDs dann selbst vergeben).
      Streng              Bricht bei jedem solchen Konflikt hart ab (Kurationslauf).

.PARAMETER IndexEntdoppelung
    NurPK          (Vorgabe) uebersprungen werden nur der Primaerindex und Indizes, deren
                   geordnete Spaltenliste exakt der PK-Spaltenliste entspricht.
    Spaltenliste   zusaetzlich werden je Tabelle Indizes mit identischer Spaltenliste auf
                   einen zusammengefasst (Access legt Beziehungs-Stuetzindizes doppelt an).

.PARAMETER ErwarteSchemaVersion
    Vorabpruefung. Vorgabe: 61. 0 schaltet die Pruefung ab.

.EXAMPLE
    pwsh -File .\Erzeuge-Schema.ps1
#>
[CmdletBinding()]
param(
    [string] $Quelle  = 'C:\ProgramData\EPOS_PLAN\Kenndaten.accdb',
    [string] $Ausgabe = (Join-Path $PSScriptRoot '..\schema'),
    [ValidateSet('AutowertBevorzugen', 'Originaltreu', 'Streng')]
    [string] $AutowertPK = 'AutowertBevorzugen',
    [ValidateSet('NurPK', 'Spaltenliste')]
    [string] $IndexEntdoppelung = 'NurPK',
    [int]    $ErwarteSchemaVersion = 61
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# ---------------------------------------------------------------------------
# Konstanten
# ---------------------------------------------------------------------------
# DAO-Feldtypen (DataTypeEnum)
$TYP_BOOLEAN = 1; $TYP_BYTE = 2; $TYP_INTEGER = 3; $TYP_LONG = 4
$TYP_SINGLE  = 6; $TYP_DOUBLE = 7; $TYP_DATE = 8; $TYP_TEXT = 10; $TYP_MEMO = 12
$TYP_NAMEN = @{
    1 = 'dbBoolean'; 2 = 'dbByte'; 3 = 'dbInteger'; 4 = 'dbLong'; 6 = 'dbSingle'
    7 = 'dbDouble'; 8 = 'dbDate'; 10 = 'dbText'; 12 = 'dbMemo'
}
$ATTR_AUTOINCREMENT   = 16      # dbAutoIncrField
$REL_DONTENFORCE      = 2       # dbRelationDontEnforce
$REL_UPDATECASCADE    = 256     # dbRelationUpdateCascade
$REL_DELETECASCADE    = 4096    # dbRelationDeleteCascade

$VIEWS_ENTFALLEN = @('Abfrage_Max_Vorlauf', 'Abfrage_Min_Vorlauf', 'Abfrage_MaxMin_Vorlauf')

# Fest hinterlegte Uebersetzungen (Original nutzt IIf bzw. CREATE PROCEDURE)
$VIEWS_UEBERSETZT = [ordered]@{
    'Abfrage_Energietraeger_Effektiv' = @'
CREATE VIEW [Abfrage_Energietraeger_Effektiv] AS
SELECT s.ID_Projekt, s.[ID_Energieträger] AS carrier_id,
       ec.code, ec.name, ec.billing_unit,
       CASE WHEN s.custom_hi IS NULL OR s.custom_hi = 0
            THEN ec.hi_kwh_per_unit ELSE s.custom_hi END AS eff_hi,
       CASE WHEN s.custom_hs IS NULL OR s.custom_hs = 0
            THEN ec.hs_kwh_per_unit ELSE s.custom_hs END AS eff_hs
FROM energy_project_settings AS s
     INNER JOIN energy_carrier AS ec ON s.[ID_Energieträger] = ec.id;
'@
    'Abfrage_Kostenfaktoren' = @'
CREATE VIEW [Abfrage_Kostenfaktoren] AS
SELECT w.ID, w.ProjektID, w.StammID, w.KategorieID,
       CASE w.KategorieID WHEN 1 THEN 'Investitionskosten'
                          WHEN 2 THEN 'Betriebskosten'
                          WHEN 3 THEN 'Energiekosten' ELSE '' END AS KategorieName,
       k.Komponente, f.Bezeichnung, w.Gruppe, w.EingegebenerWert, w.WorstCase,
       w.BestCase, w.Nutzungsdauer, w.WorstCase_Nutzungsdauer,
       w.BestCase_Nutzungsdauer, w.Einheit, f.IsMainComponent
FROM (Tab_ProjektWerte AS w
      INNER JOIN Tab_Kostenfaktor AS f ON w.StammID = f.StammID)
     INNER JOIN Tab_KostenKomponente AS k ON w.KomponentenID = k.ID
ORDER BY f.IsMainComponent,
         CASE w.KategorieID WHEN 1 THEN 'Investitionskosten'
                            WHEN 2 THEN 'Betriebskosten'
                            WHEN 3 THEN 'Energiekosten' ELSE '' END,
         k.Komponente, w.Gruppe, f.Bezeichnung;
'@
    # ---- Kuration Befund B1 (Arbeitspaket S7, 02.09.2026) -------------------
    # Diese drei Access-Abfragen waehlen die Spalte ID BEIDER verbundener Tabellen.
    # SQLite entdoppelt das selbsttaetig zu "ID" und "ID:1" - der zweite Name ist fuer
    # Konsumenten unbrauchbar. Die zweite ID (immer die der *Daten-Tabelle) heisst
    # deshalb ID_Daten; alle uebrigen Ausgabespalten behalten ihren Namen, damit
    # bestehende Konsumenten unveraendert weiterlaufen.
    # Die Texte stehen hier WOERTLICH, weil der Generator die QueryDefs sonst
    # unveraendert uebernimmt und die Kuration bei der naechsten Generierung
    # verloren ginge. Aufrufer: SimulationWaermebedarf.cs:305/:602,
    # SimulationStrombedarf.cs:121, StromTestClass.cs:48.
    'Abfrage_ProjektGebaeudeGanglinie' = @'
CREATE VIEW [Abfrage_ProjektGebaeudeGanglinie] AS
SELECT Tab_Waermebedarf.ID, Tab_WaermebedarfDaten.ID AS ID_Daten, Tab_WaermebedarfDaten.Wert
FROM Tab_Waermebedarf INNER JOIN Tab_WaermebedarfDaten ON Tab_Waermebedarf.ID = Tab_WaermebedarfDaten.ID_Ganglinie;
'@
    'Abfrage_ProjektStromGanglinie' = @'
CREATE VIEW [Abfrage_ProjektStromGanglinie] AS
SELECT Tab_Stromganglinie.ID, Tab_StromganglinieDaten.ID AS ID_Daten, Tab_StromganglinieDaten.Wert, Tab_Stromganglinie.Zeitinterval
FROM Tab_Stromganglinie INNER JOIN Tab_StromganglinieDaten ON Tab_Stromganglinie.ID = Tab_StromganglinieDaten.ID_Ganglinie;
'@
    'Abfrage_Tagverteilung' = @'
CREATE VIEW [Abfrage_Tagverteilung] AS
SELECT Tab_DBTagV.ID, Tab_DBTagV.Bezeichner, Tab_DBTagVDaten.ID AS ID_Daten, Tab_DBTagVDaten.Verteilung
FROM Tab_DBTagV INNER JOIN Tab_DBTagVDaten ON Tab_DBTagV.ID = Tab_DBTagVDaten.ID_TagV
ORDER BY Tab_DBTagVDaten.ID;
'@
}
$VIEW_KOMMENTAR = @{
    'Abfrage_Energietraeger_Effektiv' =
        '-- uebersetzt, Original IIf (SchemaMigration.cs:6344-6351)'
    'Abfrage_Kostenfaktoren' =
        "-- uebersetzt, Original IIf/PROCEDURE (ACE laesst kein ORDER BY in Views zu, SQLite schon).`n" +
        '-- Der IIf-Ausdruck steht im ORDER BY ausgeschrieben, damit der Text dem Original entspricht.'
    'Abfrage_ProjektGebaeudeGanglinie' =
        '-- kuriert (Befund B1, S7): zweite ID als ID_Daten benannt - siehe Kopfhinweis.'
    'Abfrage_ProjektStromGanglinie' =
        '-- kuriert (Befund B1, S7): zweite ID als ID_Daten benannt - siehe Kopfhinweis.'
    'Abfrage_Tagverteilung' =
        '-- kuriert (Befund B1, S7): zweite ID als ID_Daten benannt - siehe Kopfhinweis.'
}

# ---------------------------------------------------------------------------
# Hilfsfunktionen
# ---------------------------------------------------------------------------
function Sortiere-Ordinal {
    param([object[]] $Objekte, [string] $Eigenschaft)
    $kopie = @($Objekte)
    if ($kopie.Count -le 1) { return $kopie }
    $schluessel = [string[]]@($kopie | ForEach-Object { [string]$_.$Eigenschaft })
    $werte      = [object[]]$kopie
    [Array]::Sort($schluessel, $werte, [System.StringComparer]::Ordinal)
    return $werte
}

function Q {
    param([string] $Name)
    return '"' + $Name.Replace('"', '""') + '"'
}

function Neue-Liste { , (New-Object System.Collections.Generic.List[object]) }

# PowerShell 7.6.5 wirft bei @($liste) auf einer List[object] mit PSCustomObject-Inhalt
# "Argument types do not match". ToArray() ist der verlaessliche Weg.
# Das Komma haelt auch leere Arrays am Leben (sonst gibt PowerShell $null zurueck).
function Als-Array {
    param($Liste)
    if ($null -eq $Liste) { return , @() }
    if ($Liste -is [System.Collections.Generic.List[object]]) { return , $Liste.ToArray() }
    return , @($Liste)
}

$script:Fehler = New-Object System.Collections.Generic.List[string]
function Melde-Fehler { param([string] $Text) $script:Fehler.Add($Text) | Out-Null }

function Schreibe-Datei {
    param([string] $Pfad, [string] $Inhalt)
    # LF-Zeilenenden wie im uebrigen Repo; UTF-8 OHNE BOM.
    $norm = $Inhalt -replace "`r`n", "`n" -replace "`r", "`n"
    [System.IO.File]::WriteAllText($Pfad, $norm, (New-Object System.Text.UTF8Encoding($false)))
    return (Get-Item -LiteralPath $Pfad)
}

function Schreibe-Json {
    param([string] $Pfad, $Objekt)
    Schreibe-Datei -Pfad $Pfad -Inhalt (($Objekt | ConvertTo-Json -Depth 12) + "`n")
}

# DEFAULT-Literal aus dem Access-Rohwert
function Uebersetze-Default {
    param([string] $Roh, [int] $Typ, [string] $Wo)
    if ([string]::IsNullOrWhiteSpace($Roh)) { return $null }
    $t = $Roh.Trim()
    if ($t -match '[()=]') {
        Melde-Fehler "DEFAULT-Ausdruck statt Literal: $Wo -> '$Roh'"
        return $null
    }
    if ($t -ieq 'Null') { return $null }
    if ($Typ -eq $TYP_BOOLEAN) { return '0' }          # Boolean: Quelle wird ignoriert (D3-a)
    if ($t -ieq 'No' -or $t -ieq 'False') { return '0' }
    if ($t -ieq 'Yes' -or $t -ieq 'True')  { return '1' }
    if ($t -match '^-?\d+(\.\d+)?$') { return $t }     # numerisches Literal
    $ohne = $t
    if ($ohne.Length -ge 2 -and $ohne.StartsWith('"') -and $ohne.EndsWith('"')) {
        $ohne = $ohne.Substring(1, $ohne.Length - 2)
    } elseif ($ohne.Length -ge 2 -and $ohne.StartsWith("'") -and $ohne.EndsWith("'")) {
        $ohne = $ohne.Substring(1, $ohne.Length - 2)
    }
    return "'" + $ohne.Replace("'", "''") + "'"
}

# ---------------------------------------------------------------------------
# 1) Quelle oeffnen und Vorabpruefung
# ---------------------------------------------------------------------------
if (-not (Test-Path -LiteralPath $Quelle)) { throw "Quelle nicht gefunden: $Quelle" }
$quellDatei = Get-Item -LiteralPath $Quelle
$vorher = @{ Laenge = $quellDatei.Length; Geaendert = $quellDatei.LastWriteTimeUtc }

Write-Host "Quelle : $($quellDatei.FullName)"
Write-Host "         $($quellDatei.Length) Bytes, geaendert $($quellDatei.LastWriteTime)"

$engine = New-Object -ComObject DAO.DBEngine.120
$db = $engine.OpenDatabase($quellDatei.FullName, $false, $true)   # exklusiv=false, readonly=TRUE
try {
    $alleTabDefs = @($db.TableDefs | Where-Object { $_.Name -notlike 'MSys*' })
    $anzahlTabellen  = $alleTabDefs.Count
    $anzahlVerknuepft = @($alleTabDefs | Where-Object { $_.Connect -ne '' }).Count

    $schemaVersionen = @()
    $rs = $db.OpenRecordset('SELECT DISTINCT SchemaVersion FROM Tab_Applikation')
    while (-not $rs.EOF) { $schemaVersionen += [int]$rs.Fields.Item(0).Value; $rs.MoveNext() }
    $rs.Close()

    Write-Host "Vorab  : SchemaVersion=$($schemaVersionen -join ',')  Tabellen=$anzahlTabellen  verknuepft=$anzahlVerknuepft  Relations=$($db.Relations.Count)"

    $abbruch = @()
    if ($ErwarteSchemaVersion -gt 0) {
        if ($schemaVersionen.Count -ne 1 -or $schemaVersionen[0] -ne $ErwarteSchemaVersion) {
            $abbruch += "Tab_Applikation.SchemaVersion = [$($schemaVersionen -join ', ')], erwartet $ErwarteSchemaVersion"
        }
    }
    if ($anzahlTabellen -ne 114)  { $abbruch += "TableDefs ohne MSys = $anzahlTabellen, erwartet 114 (S0 gelaufen?)" }
    if ($anzahlVerknuepft -ne 0)  { $abbruch += "verknuepfte Tabellen = $anzahlVerknuepft, erwartet 0 (S0 gelaufen?)" }
    if ($abbruch.Count -gt 0) {
        throw ("Vorabpruefung fehlgeschlagen:`n  - " + ($abbruch -join "`n  - "))
    }

    # -----------------------------------------------------------------------
    # 2) Metadaten einlesen
    # -----------------------------------------------------------------------
    $tabellen = Neue-Liste
    foreach ($td in $alleTabDefs) {
        $spalten = Neue-Liste
        for ($i = 0; $i -lt $td.Fields.Count; $i++) {
            $f = $td.Fields.Item($i)
            $anzeige = $null; $rowSource = $null; $rowSourceTyp = $null
            try { $anzeige      = [int]$f.Properties.Item('DisplayControl').Value } catch { }
            try { $rowSource    = [string]$f.Properties.Item('RowSource').Value }   catch { }
            try { $rowSourceTyp = [string]$f.Properties.Item('RowSourceType').Value } catch { }
            $spalten.Add([pscustomobject]@{
                Ordinal        = $i
                Name           = [string]$f.Name
                DaoTyp         = [int]$f.Type
                Groesse        = [int]$f.Size
                Required       = [bool]$f.Required
                Autowert       = ((([int]$f.Attributes) -band $ATTR_AUTOINCREMENT) -ne 0)
                DefaultRoh     = [string]$f.DefaultValue
                DisplayControl = $anzeige
                RowSource      = $rowSource
                RowSourceTyp   = $rowSourceTyp
            }) | Out-Null
        }
        $indizes = Neue-Liste
        for ($j = 0; $j -lt $td.Indexes.Count; $j++) {
            $ix = $td.Indexes.Item($j)
            $ixSpalten = @()
            for ($k = 0; $k -lt $ix.Fields.Count; $k++) { $ixSpalten += [string]$ix.Fields.Item($k).Name }
            $indizes.Add([pscustomobject]@{
                Name = [string]$ix.Name; Primary = [bool]$ix.Primary
                Unique = [bool]$ix.Unique; Foreign = [bool]$ix.Foreign
                Spalten = $ixSpalten
            }) | Out-Null
        }
        $tabellen.Add([pscustomobject]@{
            Name = [string]$td.Name; Spalten = $spalten; Indizes = $indizes
        }) | Out-Null
    }
    $tabellen = @(Sortiere-Ordinal -Objekte $tabellen.ToArray() -Eigenschaft 'Name')
    $tabNamen = [System.Collections.Generic.HashSet[string]]::new([string[]]@($tabellen | ForEach-Object { $_.Name }))

    # Beziehungen (Systembeziehungen zwischen MSys-Tabellen werden ausgeschlossen)
    $relationenRoh = Neue-Liste
    $relSystem = Neue-Liste
    for ($r = 0; $r -lt $db.Relations.Count; $r++) {
        $rel = $db.Relations.Item($r)
        $paare = @()
        for ($k = 0; $k -lt $rel.Fields.Count; $k++) {
            $rf = $rel.Fields.Item($k)
            $paare += [pscustomobject]@{ Eltern = [string]$rf.Name; Kind = [string]$rf.ForeignName }
        }
        $eintrag = [pscustomobject]@{
            Name = [string]$rel.Name; Eltern = [string]$rel.Table; Kind = [string]$rel.ForeignTable
            Attribute = [int]$rel.Attributes; Paare = $paare
        }
        if ($eintrag.Eltern -like 'MSys*' -or $eintrag.Kind -like 'MSys*') {
            $relSystem.Add($eintrag) | Out-Null
        } else {
            $relationenRoh.Add($eintrag) | Out-Null
        }
    }
    $relationen = @(Sortiere-Ordinal -Objekte $relationenRoh.ToArray() -Eigenschaft 'Name')

    foreach ($rel in $relationen) {
        if (($rel.Attribute -band $REL_DONTENFORCE) -ne 0) {
            Melde-Fehler "Beziehung ohne referentielle Integritaet (dbRelationDontEnforce): $($rel.Name)"
        }
        if (-not $tabNamen.Contains($rel.Eltern) -or -not $tabNamen.Contains($rel.Kind)) {
            Melde-Fehler "Beziehung auf unbekannte Tabelle: $($rel.Name) ($($rel.Eltern) -> $($rel.Kind))"
        }
    }

    # Gespeicherte Abfragen
    $abfragen = Neue-Liste
    for ($q = 0; $q -lt $db.QueryDefs.Count; $q++) {
        $qd = $db.QueryDefs.Item($q)
        if ($qd.Name -like '~*') { continue }
        $abfragen.Add([pscustomobject]@{ Name = [string]$qd.Name; SQL = [string]$qd.SQL }) | Out-Null
    }
    $abfragen = @(Sortiere-Ordinal -Objekte $abfragen.ToArray() -Eigenschaft 'Name')
} finally {
    $db.Close()
}

# Nachweis, dass die Quelle unveraendert ist
$quellDateiNachher = Get-Item -LiteralPath $Quelle
if ($quellDateiNachher.Length -ne $vorher.Laenge -or $quellDateiNachher.LastWriteTimeUtc -ne $vorher.Geaendert) {
    Melde-Fehler "Quelldatei hat sich waehrend des Lesens geaendert (Laenge/Zeitstempel) - Lauf verwerfen!"
}

# ---------------------------------------------------------------------------
# 3) Typpruefung
# ---------------------------------------------------------------------------
$erlaubteTypen = @($TYP_BOOLEAN, $TYP_BYTE, $TYP_INTEGER, $TYP_LONG, $TYP_SINGLE, $TYP_DOUBLE, $TYP_DATE, $TYP_TEXT, $TYP_MEMO)
foreach ($t in $tabellen) {
    foreach ($s in $t.Spalten) {
        if ($erlaubteTypen -notcontains $s.DaoTyp) {
            Melde-Fehler "Unbekannter DAO-Typcode $($s.DaoTyp): $($t.Name).$($s.Name)"
        }
    }
}

function SqliteTyp {
    param([int] $DaoTyp)
    switch ($DaoTyp) {
        1  { 'INTEGER' }  # Boolean
        2  { 'INTEGER' }  # Byte
        3  { 'INTEGER' }  # Integer
        4  { 'INTEGER' }  # Long
        6  { 'REAL' }     # Single
        7  { 'REAL' }     # Double
        8  { 'TEXT' }     # Datum -> ISO-8601-Text
        10 { 'TEXT' }     # Text
        12 { 'TEXT' }     # Memo
        default { throw "Unbekannter DAO-Typcode $DaoTyp" }
    }
}

# ---------------------------------------------------------------------------
# 4) Primaerschluessel bestimmen
# ---------------------------------------------------------------------------
$pkEntscheidungen = @{}
$pkErgaenzt   = Neue-Liste   # Tabellen ohne Access-PK, Autowert wird PK
$pkGeaendert  = Neue-Liste   # Access-PK weicht vom effektiven PK ab
$autowertKonflikte = Neue-Liste
$zusatzUnique = Neue-Liste   # UNIQUE-Indizes, die einen aufgegebenen Access-PK sichern

foreach ($t in $tabellen) {
    $primIndizes = @($t.Indizes | Where-Object { $_.Primary })
    if ($primIndizes.Count -gt 1) { Melde-Fehler "Mehr als ein Primaerindex: $($t.Name)" }
    # ACHTUNG: 'if' als Ausdruck entrollt einelementige Arrays - deshalb getrennte Zuweisung.
    $accessPk = $null
    if ($primIndizes.Count -ge 1) { $accessPk = [string[]]@($primIndizes[0].Spalten) }
    $autos    = [string[]]@($t.Spalten | Where-Object { $_.Autowert } | ForEach-Object { $_.Name })
    if ($autos.Count -gt 1) { Melde-Fehler "Mehr als eine Autowertspalte: $($t.Name) ($($autos -join ', '))" }

    $effektivPk = $accessPk
    $vermerk    = 'unveraendert'

    if ($autos.Count -eq 1) {
        $a = $autos[0]
        $istAlleinPk = ($null -ne $accessPk) -and ($accessPk.Count -eq 1) -and ($accessPk[0] -eq $a)
        if (-not $istAlleinPk) {
            if ($null -eq $accessPk) {
                $effektivPk = @($a); $vermerk = 'PK ergaenzt'
                $pkErgaenzt.Add([pscustomobject]@{ Tabelle = $t.Name; Spalte = $a }) | Out-Null
            } else {
                $autowertKonflikte.Add([pscustomobject]@{
                    Tabelle = $t.Name; Autowert = $a; AccessPk = $accessPk
                }) | Out-Null
                switch ($AutowertPK) {
                    'Streng' {
                        Melde-Fehler ("Autowertspalte kann nicht PK werden (anderer PK existiert): " +
                                      "$($t.Name).$a, Access-PK = [$($accessPk -join ', ')]")
                    }
                    'Originaltreu' { }   # Access-PK bleibt, Autowert ohne AUTOINCREMENT
                    'AutowertBevorzugen' {
                        $effektivPk = @($a); $vermerk = 'PK gewechselt'
                        # Eindeutigkeit des alten PK-Tupels sichern, falls nicht impliziert
                        if ($accessPk -notcontains $a) {
                            $gedeckt = @($t.Indizes | Where-Object {
                                $_.Unique -and -not $_.Primary -and
                                (($_.Spalten -join "`u{1}") -eq ($accessPk -join "`u{1}"))
                            }).Count -gt 0
                            if (-not $gedeckt) {
                                $zusatzUnique.Add([pscustomobject]@{
                                    Tabelle = $t.Name
                                    Name    = 'uq_' + $t.Name + '_' + ($accessPk -join '_')
                                    Spalten = $accessPk
                                }) | Out-Null
                            }
                        }
                        $pkGeaendert.Add([pscustomobject]@{
                            Tabelle = $t.Name; AccessPk = $accessPk; EffektivPk = @($a)
                            Impliziert = ($accessPk -contains $a)
                        }) | Out-Null
                    }
                }
            }
        }
    }
    if ($null -eq $effektivPk) { Melde-Fehler "Tabelle ohne Primaerschluessel und ohne Autowert: $($t.Name)" }

    $pkEntscheidungen[$t.Name] = [pscustomobject]@{
        AccessPk = $accessPk; EffektivPk = $effektivPk; Autowert = $(if ($autos.Count -eq 1) { $autos[0] } else { $null })
        Vermerk = $vermerk
    }
}

if ($script:Fehler.Count -gt 0) {
    throw ("Harte Fehler - Kuration noetig:`n  - " + ($script:Fehler -join "`n  - "))
}

# ---------------------------------------------------------------------------
# 5) 001_grundschema.sql
# ---------------------------------------------------------------------------
$fkNachKind = @{}
foreach ($rel in $relationen) {
    if (-not $fkNachKind.ContainsKey($rel.Kind)) { $fkNachKind[$rel.Kind] = Neue-Liste }
    $fkNachKind[$rel.Kind].Add($rel) | Out-Null
}

# Deterministisch: der Zeitstempel im Kopf ist der Stand der QUELLE, nicht die Laufzeit.
# So sind Wiederholungslaeufe gegen dieselbe Quelle byte-identisch (saubere Git-Diffs).
$stand = $quellDatei.LastWriteTime.ToString('yyyy-MM-dd HH:mm')
$kopf001 = @"
-- 001_grundschema.sql - EPOS-Plan, Zielschema SQLite (Arbeitspaket S2)
-- Erzeugt von sql/tools/Erzeuge-Schema.ps1, Quellenstand $stand
-- Quelle: $($quellDatei.FullName) (Schemastand $ErwarteSchemaVersion, nach S0)
-- NICHT VON HAND AENDERN - neu erzeugen. Ab S4-Beginn eingefroren.
--
-- Alle Tabellen sind STRICT. Die FREMDSCHLUESSEL stehen hier und nicht in 003,
-- weil SQLite sie nach dem CREATE TABLE nicht nachruesten kann.
-- PRAGMA foreign_keys = ON ist Pflicht je Verbindung, sonst sind sie wirkungslos.
--
-- Reihenfolge beim Aufbau: 001 -> 002 -> 003.

PRAGMA foreign_keys = OFF;

"@

$zeilen001 = Neue-Liste
$zeilen001.Add($kopf001) | Out-Null

$anzFkKlauseln = 0
$anzCheckText  = 0
$anzNotNullAusPk = 0
$notNullAusPk = Neue-Liste

foreach ($t in $tabellen) {
    $pk = $pkEntscheidungen[$t.Name]
    $spaltenText = Neue-Liste
    $tabellenKlauseln = Neue-Liste

    $pkAmSpalte = $false
    if ($pk.EffektivPk.Count -eq 1) {
        $pkSpalte = @($t.Spalten | Where-Object { $_.Name -eq $pk.EffektivPk[0] })
        if ($pkSpalte.Count -ne 1) { throw "PK-Spalte $($pk.EffektivPk[0]) in $($t.Name) nicht gefunden" }
        $pkAmSpalte = ((SqliteTyp $pkSpalte[0].DaoTyp) -eq 'INTEGER')
    }

    foreach ($s in $t.Spalten) {
        $typ = SqliteTyp $s.DaoTyp
        $teile = Neue-Liste
        $teile.Add((Q $s.Name)) | Out-Null
        $teile.Add($typ) | Out-Null

        $istPkSpalte = $pkAmSpalte -and ($s.Name -eq $pk.EffektivPk[0])
        if ($istPkSpalte) {
            if ($s.Autowert) { $teile.Add('PRIMARY KEY AUTOINCREMENT') | Out-Null }
            else             { $teile.Add('PRIMARY KEY') | Out-Null }
        }

        # NOT NULL: Boolean immer (D3-a); Field.Required; Mitglied des Access-PK
        # (Access verbietet NULL in PK-Spalten - beim PK-Wechsel muss das erhalten bleiben).
        $ausPk = ($null -ne $pk.AccessPk) -and ($pk.AccessPk -contains $s.Name)
        $notNull = ($s.DaoTyp -eq $TYP_BOOLEAN) -or $s.Required -or $ausPk
        if ($notNull -and -not $istPkSpalte) {
            $teile.Add('NOT NULL') | Out-Null
            if ($ausPk -and -not $s.Required -and $s.DaoTyp -ne $TYP_BOOLEAN) {
                $anzNotNullAusPk++
                $notNullAusPk.Add("$($t.Name).$($s.Name)") | Out-Null
            }
        }

        if ($s.DaoTyp -eq $TYP_BOOLEAN) {
            $teile.Add('DEFAULT 0') | Out-Null
            $teile.Add("CHECK ($(Q $s.Name) IN (0,1))") | Out-Null
        } else {
            $std = Uebersetze-Default -Roh $s.DefaultRoh -Typ $s.DaoTyp -Wo "$($t.Name).$($s.Name)"
            if ($null -ne $std -and -not $istPkSpalte) { $teile.Add("DEFAULT $std") | Out-Null }
            if ($s.DaoTyp -eq $TYP_TEXT -and $s.Groesse -gt 0 -and $s.Groesse -lt 255) {
                $teile.Add("CHECK (length($(Q $s.Name)) <= $($s.Groesse))") | Out-Null
                $anzCheckText++
            }
        }
        $spaltenText.Add('    ' + ($teile -join ' ')) | Out-Null
    }

    if (-not $pkAmSpalte) {
        $tabellenKlauseln.Add('    PRIMARY KEY (' + (($pk.EffektivPk | ForEach-Object { Q $_ }) -join ', ') + ')') | Out-Null
    }

    if ($fkNachKind.ContainsKey($t.Name)) {
        foreach ($rel in @(Sortiere-Ordinal -Objekte $fkNachKind[$t.Name].ToArray() -Eigenschaft 'Name')) {
            $kindSp   = ($rel.Paare | ForEach-Object { Q $_.Kind })   -join ', '
            $elternSp = ($rel.Paare | ForEach-Object { Q $_.Eltern }) -join ', '
            $klausel  = "    FOREIGN KEY ($kindSp) REFERENCES $(Q $rel.Eltern) ($elternSp)"
            if (($rel.Attribute -band $REL_UPDATECASCADE) -ne 0) { $klausel += ' ON UPDATE CASCADE' }
            if (($rel.Attribute -band $REL_DELETECASCADE) -ne 0) { $klausel += ' ON DELETE CASCADE' }
            $tabellenKlauseln.Add($klausel) | Out-Null
            $anzFkKlauseln++
        }
    }

    $alle = New-Object System.Collections.Generic.List[string]
    foreach ($z in $spaltenText)      { $alle.Add([string]$z) | Out-Null }
    foreach ($z in $tabellenKlauseln) { $alle.Add([string]$z) | Out-Null }
    $block = "CREATE TABLE $(Q $t.Name) (`n" + ((Als-Array $alle) -join ",`n") + "`n) STRICT;`n"
    $zeilen001.Add($block) | Out-Null
}

$zeilen001.Add("PRAGMA foreign_keys = ON;`n") | Out-Null

# ---------------------------------------------------------------------------
# 6) 002_views.sql
# ---------------------------------------------------------------------------
$views = Neue-Liste
foreach ($a in $abfragen) {
    if ($VIEWS_ENTFALLEN -contains $a.Name) { continue }
    if ($VIEWS_UEBERSETZT.Contains($a.Name)) {
        $views.Add([pscustomobject]@{
            Name = $a.Name; Text = $VIEWS_UEBERSETZT[$a.Name].TrimEnd()
            Kommentar = $VIEW_KOMMENTAR[$a.Name]; Uebersetzt = $true
        }) | Out-Null
        continue
    }
    $sql = ($a.SQL -replace "`r`n", "`n" -replace "`r", "`n").TrimEnd()
    foreach ($wort in @('PARAMETERS', 'TRANSFORM', 'DISTINCTROW')) {
        if ($sql -match "(?i)\b$wort\b") { Melde-Fehler "Abfrage $($a.Name) enthaelt $wort - Kuration noetig" }
    }
    while ($sql.EndsWith(';')) { $sql = $sql.Substring(0, $sql.Length - 1).TrimEnd() }
    $views.Add([pscustomobject]@{
        Name = $a.Name; Text = "CREATE VIEW [$($a.Name)] AS`n$sql;"
        Kommentar = $null; Uebersetzt = $false
    }) | Out-Null
}
$views = @(Sortiere-Ordinal -Objekte $views.ToArray() -Eigenschaft 'Name')

$zeilen002 = Neue-Liste
$zeilen002.Add(@"
-- 002_views.sql - EPOS-Plan, Zielschema SQLite (Arbeitspaket S2)
-- Erzeugt von sql/tools/Erzeuge-Schema.ps1, Quellenstand $stand
-- $($views.Count) Views aus $($abfragen.Count) gespeicherten Access-Abfragen.
-- Entfallen (Ergebnis der Schemapflege, kein Migrationsgegenstand):
--   $($VIEWS_ENTFALLEN -join ' . ')
-- Access-Schreibweisen ([eckige Bezeichner], Klammer-Joins) bleiben unveraendert.
-- Hinweis: Abfrage_KenndatenKuehlung_Max baut auf Abfrage_Kuehlung_MaxLast auf;
-- SQLite loest Viewrumpfe erst beim SELECT auf, die alphabetische Reihenfolge stoert nicht.
--
-- Kuriert (Befund B1, Arbeitspaket S7, 02.09.2026):
--   Abfrage_ProjektGebaeudeGanglinie . Abfrage_ProjektStromGanglinie . Abfrage_Tagverteilung
-- Diese drei Abfragen waehlen die Spalte ID BEIDER verbundener Tabellen. SQLite entdoppelt
-- das selbsttaetig zu "ID" und "ID:1"; der zweite Name ist fuer Konsumenten unbrauchbar
-- (er laesst sich in WHERE/ORDER BY nur gequotet ansprechen und traegt keine Bedeutung).
-- Die zweite ID - immer die des Datensatzes der *Daten-Tabelle - heisst deshalb ID_Daten.
-- ALLE uebrigen Ausgabespalten behalten ihren Namen (ID, Bezeichner, Wert, Verteilung,
-- Zeitinterval), damit bestehende Konsumenten unveraendert weiterlaufen.
-- Aufrufer (angepasst im selben Zug): SimulationWaermebedarf.cs:305/:602,
-- SimulationStrombedarf.cs:121, StromTestClass.cs:48 - sie sprachen die Sichtspalten
-- ueber den Namen der zugrunde liegenden TABELLE an (Tab_Waermebedarf.ID usw.). Jet loest
-- das auf, SQLite nicht: eine Sicht hat nur ihre eigenen Ausgabespalten.
-- Der Generator Erzeuge-Schema.ps1 fuehrt die drei Texte als feste Ueberschreibung
-- (`$VIEWS_UEBERSETZT), sonst wuerde die naechste Generierung die Kuration verwerfen.

"@) | Out-Null
foreach ($v in $views) {
    if ($v.Kommentar) { $zeilen002.Add($v.Kommentar) | Out-Null }
    $zeilen002.Add($v.Text + "`n") | Out-Null
}

# ---------------------------------------------------------------------------
# 7) 003_indizes_fk.sql
# ---------------------------------------------------------------------------
$indexKandidaten = Neue-Liste
$anzUebersprungenPk = 0
$anzUebersprungenSpaltenliste = 0
$spaltenlistenDubletten = Neue-Liste

foreach ($t in $tabellen) {
    $pk = $pkEntscheidungen[$t.Name].EffektivPk
    $gesehen = @{}
    foreach ($ix in @(Sortiere-Ordinal -Objekte $t.Indizes.ToArray() -Eigenschaft 'Name')) {
        if ($ix.Primary) { $anzUebersprungenPk++; continue }
        $schluessel = ($ix.Spalten -join "`u{1}")
        if ($null -ne $pk -and $schluessel -eq ($pk -join "`u{1}")) { $anzUebersprungenPk++; continue }
        if ($gesehen.ContainsKey($schluessel)) {
            $spaltenlistenDubletten.Add("$($t.Name): $($ix.Name) = $($gesehen[$schluessel]) auf ($($ix.Spalten -join ', '))") | Out-Null
            if ($IndexEntdoppelung -eq 'Spaltenliste') { $anzUebersprungenSpaltenliste++; continue }
        } else {
            $gesehen[$schluessel] = $ix.Name
        }
        $indexKandidaten.Add([pscustomobject]@{
            Tabelle = $t.Name; Name = $ix.Name; Unique = $ix.Unique
            Foreign = $ix.Foreign; Spalten = $ix.Spalten
        }) | Out-Null
    }
}
foreach ($z in $zusatzUnique) {
    $indexKandidaten.Add([pscustomobject]@{
        Tabelle = $z.Tabelle; Name = $z.Name; Unique = $true; Foreign = $false; Spalten = $z.Spalten
    }) | Out-Null
}

# Namenskollisionen aufloesen: Access-Indexnamen sind nur je Tabelle eindeutig.
$namensZaehler = @{}
foreach ($ix in $indexKandidaten) {
    if (-not $namensZaehler.ContainsKey($ix.Name)) { $namensZaehler[$ix.Name] = 0 }
    $namensZaehler[$ix.Name]++
}
$kollisionen = Neue-Liste
$vergeben = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($n in $tabNamen) { $vergeben.Add($n) | Out-Null }
foreach ($ix in $indexKandidaten) {
    $zielName = $ix.Name
    if ($namensZaehler[$ix.Name] -gt 1) {
        $zielName = $ix.Tabelle + '_' + $ix.Name
        $kollisionen.Add("$($ix.Tabelle).$($ix.Name) -> $zielName") | Out-Null
    }
    if ($zielName -like 'sqlite_*') { Melde-Fehler "Indexname beginnt mit sqlite_: $zielName" }
    if (-not $vergeben.Add($zielName)) { Melde-Fehler "Indexname weiterhin doppelt oder gleich einem Tabellennamen: $zielName" }
    $ix | Add-Member -NotePropertyName 'ZielName' -NotePropertyValue $zielName -Force
}

$zeilen003 = Neue-Liste
$zeilen003.Add(@"
-- 003_indizes_fk.sql - EPOS-Plan, Zielschema SQLite (Arbeitspaket S2)
-- Erzeugt von sql/tools/Erzeuge-Schema.ps1, Quellenstand $stand
--
-- ACHTUNG zum Dateinamen: hier stehen NUR INDIZES. Die Fremdschluessel stehen in
-- 001_grundschema.sql, weil SQLite eine FOREIGN-KEY-Klausel nach dem CREATE TABLE
-- nicht nachruesten kann (kein ALTER TABLE ADD CONSTRAINT). Der Dateiname bleibt aus
-- dem Konzept erhalten.
--
-- Uebersprungen: Primaerindizes und Indizes, deren Spaltenliste dem Primaerschluessel
-- entspricht (SQLite legt die selbst an). Entdoppelung: $IndexEntdoppelung.
-- Access-Indexnamen sind nur je Tabelle eindeutig, SQLite-weit global - bei Kollision
-- wird auf Tabelle_Indexname umbenannt.

"@) | Out-Null

$letzteTabelle = ''
foreach ($ix in $indexKandidaten) {
    if ($ix.Tabelle -ne $letzteTabelle) {
        $zeilen003.Add("-- $($ix.Tabelle)") | Out-Null
        $letzteTabelle = $ix.Tabelle
    }
    $u = if ($ix.Unique) { 'UNIQUE ' } else { '' }
    $sp = ($ix.Spalten | ForEach-Object { Q $_ }) -join ', '
    $zeilen003.Add("CREATE ${u}INDEX $(Q $ix.ZielName) ON $(Q $ix.Tabelle) ($sp);") | Out-Null
}
$zeilen003.Add('') | Out-Null

# ---------------------------------------------------------------------------
# 8) Typkatalog (C# + JSON)
# ---------------------------------------------------------------------------
$typJeName = @{}
foreach ($t in $tabellen) {
    foreach ($s in $t.Spalten) {
        if (-not $typJeName.ContainsKey($s.Name)) { $typJeName[$s.Name] = New-Object System.Collections.Generic.HashSet[int] }
        $typJeName[$s.Name].Add($s.DaoTyp) | Out-Null
    }
}
function Sammle-Katalog {
    param([int] $DaoTyp)
    $namen = New-Object System.Collections.Generic.HashSet[string]
    $zuordnung = [ordered]@{}
    $mehrdeutig = Neue-Liste
    foreach ($t in $tabellen) {
        $treffer = @($t.Spalten | Where-Object { $_.DaoTyp -eq $DaoTyp } | ForEach-Object { $_.Name })
        if ($treffer.Count -gt 0) { $zuordnung[$t.Name] = $treffer }
        foreach ($n in $treffer) {
            if ($typJeName[$n].Count -gt 1) {
                $wo = @()
                foreach ($t2 in $tabellen) {
                    foreach ($s2 in $t2.Spalten) {
                        if ($s2.Name -eq $n) { $wo += "$($t2.Name)=$($TYP_NAMEN[$s2.DaoTyp])" }
                    }
                }
                if (-not ($mehrdeutig | Where-Object { $_.Spalte -eq $n })) {
                    $mehrdeutig.Add([pscustomobject]@{ Spalte = $n; Vorkommen = $wo }) | Out-Null
                }
            } else {
                $namen.Add($n) | Out-Null
            }
        }
    }
    $liste = [System.Collections.Generic.List[string]]::new([string[]]@($namen))
    $liste.Sort([System.StringComparer]::Ordinal)
    return [pscustomobject]@{
        Namen = $liste.ToArray(); Zuordnung = $zuordnung; Mehrdeutig = (Als-Array $mehrdeutig)
    }
}
$boolKat  = Sammle-Katalog -DaoTyp $TYP_BOOLEAN
$datumKat = Sammle-Katalog -DaoTyp $TYP_DATE

function Csharp-Block {
    param([string[]] $Namen)
    if ($Namen.Count -eq 0) { return '' }
    return (($Namen | ForEach-Object { '            "' + $_.Replace('\', '\\').Replace('"', '\"') + '",' }) -join "`n")
}
$csharp = @"
// <auto-generated />
// Erzeugt von sql/tools/Erzeuge-Schema.ps1, Quellenstand $stand
// Quelle: $($quellDatei.Name) (Schemastand $ErwarteSchemaVersion)
// NICHT VON HAND AENDERN - neu erzeugen.
//
// Zweck (Implementierungskonzept 2.4): zentrale Typangleichung im neuen GetDataTable.
// SQLite kennt weder Boolean noch Datum; beide kommen als INTEGER bzw. TEXT zurueck.
// Spaltennamen, die in einer Tabelle Boolean/Datum und in einer anderen einen anderen
// Typ tragen, sind hier BEWUSST NICHT enthalten (siehe S2-Protokoll, Kurationspunkte):
//   Boolean mehrdeutig: $(if ($boolKat.Mehrdeutig.Count -gt 0) { ($boolKat.Mehrdeutig | ForEach-Object { $_.Spalte }) -join ', ' } else { '-' })
//   Datum mehrdeutig  : $(if ($datumKat.Mehrdeutig.Count -gt 0) { ($datumKat.Mehrdeutig | ForEach-Object { $_.Spalte }) -join ', ' } else { '-' })

using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1.Allgemein
{
    internal static class SchemaTypKatalog
    {
        /// <summary>Access-Boolean-Spalten (SQLite: INTEGER 0/1).</summary>
        internal static readonly HashSet<string> BoolSpalten =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
$(Csharp-Block $boolKat.Namen)
        };

        /// <summary>Access-Datumsspalten (SQLite: TEXT 'YYYY-MM-DD HH:MM:SS').</summary>
        internal static readonly HashSet<string> DatumSpalten =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
$(Csharp-Block $datumKat.Namen)
        };
    }
}
"@

$typkatalogJson = [ordered]@{
    Erzeugt        = $stand
    Quelle         = $quellDatei.FullName
    SchemaVersion  = $ErwarteSchemaVersion
    Bool = [ordered]@{
        SpaltenGesamt      = @($tabellen | ForEach-Object { $_.Spalten } | Where-Object { $_.DaoTyp -eq $TYP_BOOLEAN }).Count
        NamenEindeutig     = $boolKat.Namen.Count
        Namen              = $boolKat.Namen
        JeTabelle          = $boolKat.Zuordnung
        MehrdeutigeNamen   = $boolKat.Mehrdeutig
    }
    Datum = [ordered]@{
        SpaltenGesamt      = @($tabellen | ForEach-Object { $_.Spalten } | Where-Object { $_.DaoTyp -eq $TYP_DATE }).Count
        NamenEindeutig     = $datumKat.Namen.Count
        Namen              = $datumKat.Namen
        JeTabelle          = $datumKat.Zuordnung
        MehrdeutigeNamen   = $datumKat.Mehrdeutig
    }
}

# ---------------------------------------------------------------------------
# 9) inventar.json
# ---------------------------------------------------------------------------
$invTabellen = [ordered]@{}
foreach ($t in $tabellen) {
    $pk = $pkEntscheidungen[$t.Name]
    $invSpalten = Neue-Liste
    foreach ($s in $t.Spalten) {
        $invSpalten.Add([ordered]@{
            Name       = $s.Name
            Ordinal    = $s.Ordinal
            DaoTyp     = $s.DaoTyp
            DaoTypName = $TYP_NAMEN[$s.DaoTyp]
            SqliteTyp  = (SqliteTyp $s.DaoTyp)
            Autowert   = $s.Autowert
            Required   = $s.Required
            NotNull    = (($s.DaoTyp -eq $TYP_BOOLEAN) -or $s.Required -or (($null -ne $pk.AccessPk) -and ($pk.AccessPk -contains $s.Name)))
            DefaultRoh = $s.DefaultRoh
            Default    = (Uebersetze-Default -Roh $s.DefaultRoh -Typ $s.DaoTyp -Wo "$($t.Name).$($s.Name)")
            Textlaenge = $(if ($s.DaoTyp -eq $TYP_TEXT) { $s.Groesse } else { $null })
        }) | Out-Null
    }
    $invFk = Neue-Liste
    if ($fkNachKind.ContainsKey($t.Name)) {
        foreach ($rel in @(Sortiere-Ordinal -Objekte $fkNachKind[$t.Name].ToArray() -Eigenschaft 'Name')) {
            $invFk.Add([ordered]@{
                Beziehung    = $rel.Name
                KindSpalten  = @($rel.Paare | ForEach-Object { $_.Kind })
                Eltern       = $rel.Eltern
                ElternSpalten= @($rel.Paare | ForEach-Object { $_.Eltern })
                OnUpdate     = $(if (($rel.Attribute -band $REL_UPDATECASCADE) -ne 0) { 'CASCADE' } else { 'NO ACTION' })
                OnDelete     = $(if (($rel.Attribute -band $REL_DELETECASCADE) -ne 0) { 'CASCADE' } else { 'NO ACTION' })
                Attribute    = $rel.Attribute
            }) | Out-Null
        }
    }
    $invIx = Neue-Liste
    foreach ($ix in @($indexKandidaten | Where-Object { $_.Tabelle -eq $t.Name })) {
        $invIx.Add([ordered]@{ Name = $ix.ZielName; AccessName = $ix.Name; Unique = $ix.Unique; Spalten = @($ix.Spalten) }) | Out-Null
    }
    $invTabellen[$t.Name] = [ordered]@{
        Spalten        = (Als-Array $invSpalten)
        AccessPk       = $pk.AccessPk
        PrimaerSchluessel = $pk.EffektivPk
        PkVermerk      = $pk.Vermerk
        Autowert       = $pk.Autowert
        Fremdschluessel= (Als-Array $invFk)
        Indizes        = (Als-Array $invIx)
    }
}

$alleSpalten = @($tabellen | ForEach-Object { $_.Spalten })
$inventar = [ordered]@{
    Erzeugt   = $stand
    Generator = 'sql/tools/Erzeuge-Schema.ps1'
    Quelle    = [ordered]@{
        Pfad = $quellDatei.FullName; Bytes = $quellDatei.Length
        Geaendert = $quellDatei.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
        SchemaVersion = $ErwarteSchemaVersion
    }
    Politik = [ordered]@{ AutowertPK = $AutowertPK; IndexEntdoppelung = $IndexEntdoppelung }
    Zaehlungen = [ordered]@{
        Tabellen           = $tabellen.Count
        Spalten            = $alleSpalten.Count
        Autowerte          = @($alleSpalten | Where-Object { $_.Autowert }).Count
        Fremdschluessel    = $anzFkKlauseln
        RelationsGesamt    = ($relationen.Count + $relSystem.Count)
        RelationsSystem    = $relSystem.Count
        Views              = $views.Count
        Indizes            = $indexKandidaten.Count
        IndexKollisionen   = $kollisionen.Count
        Memo               = @($alleSpalten | Where-Object { $_.DaoTyp -eq $TYP_MEMO }).Count
        Boolean            = @($alleSpalten | Where-Object { $_.DaoTyp -eq $TYP_BOOLEAN }).Count
        Datum              = @($alleSpalten | Where-Object { $_.DaoTyp -eq $TYP_DATE }).Count
        Text               = @($alleSpalten | Where-Object { $_.DaoTyp -eq $TYP_TEXT }).Count
        TextCheck          = $anzCheckText
        Double             = @($alleSpalten | Where-Object { $_.DaoTyp -eq $TYP_DOUBLE }).Count
        Long               = @($alleSpalten | Where-Object { $_.DaoTyp -eq $TYP_LONG }).Count
        Required           = @($alleSpalten | Where-Object { $_.Required }).Count
        NotNullAusAccessPk = $anzNotNullAusPk
        OnUpdateCascade    = @($relationen | Where-Object { ($_.Attribute -band $REL_UPDATECASCADE) -ne 0 }).Count
        OnDeleteCascade    = @($relationen | Where-Object { ($_.Attribute -band $REL_DELETECASCADE) -ne 0 }).Count
    }
    PkErgaenzt        = (Als-Array $pkErgaenzt)
    PkGewechselt      = (Als-Array $pkGeaendert)
    AutowertKonflikte = (Als-Array $autowertKonflikte)
    ZusatzUnique      = (Als-Array $zusatzUnique)
    NotNullAusAccessPk= (Als-Array $notNullAusPk)
    IndexKollisionen  = (Als-Array $kollisionen)
    SpaltenlistenDubletten = (Als-Array $spaltenlistenDubletten)
    SystemBeziehungen = @(Als-Array $relSystem | ForEach-Object { $_.Name })
    ViewsEntfallen    = $VIEWS_ENTFALLEN
    ViewsUebersetzt   = @($VIEWS_UEBERSETZT.Keys)
    Nachschlagefelder = @($tabellen | ForEach-Object { $tn = $_.Name; $_.Spalten | Where-Object { $_.DisplayControl -in @(110, 111) } | ForEach-Object {
        [ordered]@{ Tabelle = $tn; Spalte = $_.Name; DisplayControl = $_.DisplayControl; RowSource = $_.RowSource }
    } })
    DisplayControlVerteilung = ($alleSpalten | Group-Object DisplayControl | ForEach-Object { [ordered]@{ Wert = $_.Name; Anzahl = $_.Count } })
    Tabellen  = $invTabellen
}

if ($script:Fehler.Count -gt 0) {
    throw ("Harte Fehler - Kuration noetig:`n  - " + ($script:Fehler -join "`n  - "))
}

# ---------------------------------------------------------------------------
# 10) Schreiben
# ---------------------------------------------------------------------------
$ziel = (New-Item -ItemType Directory -Force -Path $Ausgabe).FullName
$geschrieben = @()
$geschrieben += Schreibe-Datei (Join-Path $ziel '001_grundschema.sql')   (((Als-Array $zeilen001) -join "`n"))
$geschrieben += Schreibe-Datei (Join-Path $ziel '002_views.sql')         (((Als-Array $zeilen002) -join "`n"))
$geschrieben += Schreibe-Datei (Join-Path $ziel '003_indizes_fk.sql')    (((Als-Array $zeilen003) -join "`n"))
$geschrieben += Schreibe-Datei (Join-Path $ziel 'SchemaTypKatalog.g.cs') ($csharp + "`n")
$geschrieben += Schreibe-Json  (Join-Path $ziel 'typkatalog.json')       $typkatalogJson
$geschrieben += Schreibe-Json  (Join-Path $ziel 'inventar.json')         $inventar

Write-Host ''
Write-Host '--- Ergebnis -------------------------------------------------'
$inventar.Zaehlungen.GetEnumerator() | ForEach-Object { '{0,-20} {1}' -f $_.Key, $_.Value } | Write-Host
Write-Host ''
Write-Host "PK ergaenzt        : $(($pkErgaenzt | ForEach-Object { $_.Tabelle }) -join ', ')"
Write-Host "PK gewechselt      : $($pkGeaendert.Count) Tabelle(n) (Politik $AutowertPK)"
Write-Host "Indexkollisionen   : $($kollisionen.Count)"
Write-Host "Spaltenlisten-Dubl.: $($spaltenlistenDubletten.Count) (Politik $IndexEntdoppelung)"
Write-Host "Nachschlagefelder  : $(@($inventar.Nachschlagefelder).Count) (DisplayControl 110/111)"
Write-Host ''
foreach ($g in $geschrieben) { Write-Host ("geschrieben: {0} ({1} Bytes)" -f $g.FullName, $g.Length) }
