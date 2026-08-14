# Paket 3 — Erdreichmodell, Stufe 1 (Umsetzungsprotokoll)

Stand: 14.08.2026, nachgearbeitet nach adversarialer Review (siehe Kapitel 9) · Grundlage:
[`Konzept_Simulation_QuellenSenken.md`](Konzept_Simulation_QuellenSenken.md),
Kapitel 4.5, 5.3 und 13.1 · Normstand: **VDI 4640 Blatt 1, Entwurf 2021-12** (Gründruck)
und **VDI 4640 Blatt 2:2019-06** (Weißdruck).

Nicht committet. Migration bewusst zurückgestellt: Schemaänderungen laufen ausschließlich
additiv über den bestehenden Mechanismus `WaermequelleClass.SchemaSicherstellen()` /
`SpalteSicherstellen()` (nur `ADD COLUMN`). Keine Beziehungen, keine neuen Tabellen, kein
Migrationsskript.

## 1. Umfang

### Neue Dateien

| Datei | Inhalt |
|---|---|
| `Allgemein/Simulation/ErdreichTemperatur.cs` | Bodentyp-Katalog (13 Typen nach VDI 4640 Bl. 1, Tab. 1), Jahresgang-Analyse des Außentemperaturvektors, Kusuda-Profil für den Kollektor, konstante Sondentemperatur, Kennwerte für die Dialogvorschau, Selbsttest |
| `Allgemein/Simulation/VDI4640Pruefung.cs` | Auslegungsprüfung Stufe 1: Tabelle A2 vollständig (15 Klimazonen × 4 Bodenarten + Volllaststunden), Tabelle B2 als **Auszug**, lineare Interpolation mit Klemmung, Bodentyp→Bodenart-Mapping, Selbsttest |
| `Views/Simulation/Form_QuelleErdreich.cs` | Quellendialog nach Mockup 4.5 — programmatisch, ohne Designer und `.resx` (Muster `Form_QuellePufferspeicher`) |

### Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/Simulation/WaermequelleClass.cs` | `TYP_ERDREICH = "Erdreich"`; `TypAnzeige`/`TypWerte` um „Erdreich (VDI 4640)" **angehängt** (Index-Kopplung, Warnung als Kommentar hinterlegt); `SchemaSicherstellen` legt `WQ_Tiefe` DOUBLE, `WQ_Flaeche` DOUBLE, `WQ_Anzahl` LONG, `WQ_Bodentyp` TEXT(50), `WQ_Quellsystem` TEXT(50) sowie `Tab_Klimaregion.Klimazone_DIN4710` LONG DEFAULT 0 an; `Quelltemperatur()` um den Fall `TYP_ERDREICH` erweitert, samt Konsistenz-Check `MAX_KOLLEKTORTIEFE_M` gegen teilgeschriebene Feldsätze |
| `Views/Simulation/Form_Simulation_Config.cs` | `WaermequelleAnzeige()` um den Erdreich-Fall; neuer `case TYP_ERDREICH` in `WqCombo_SelectedIndexChanged()`; Hilfsmethoden `AussentemperaturLaden()` (einmalig gecacht), `KlimazoneDesProjekts()`, `KlimazoneSpeichern()`, `ErdreichAnzeige()` |
| `Controller/KlimaregionCtrl.cs` | Vorarbeit A: `Add()`/`Update()`/`Delete()` auf das reale Schema gezogen; Lesen/Schreiben von `Klimazone_DIN4710`; statische Helfer `GetKlimazone` / `SetKlimazone` |
| `Model/KlimaregionModel.cs` | Feld `Klimazone_DIN4710` |

`btn_Speichern_Click` in `Form_Simulation_Config.cs` wurde **nicht** berührt (dort liegen die
frischen B0-1/B0-11-Änderungen). Ebenso unberührt: `Form_Heizkessel*.cs`, `SimulationSPK.cs`,
`RecordSet.cs`, `WizardCtrl.cs` und die übrigen uncommitteten ID_Carrier-Änderungen.

## 2. Vorarbeit A — `KlimaregionCtrl`

Am Schema der Arbeitskopie `Referenzlaeufe\Arbeitskopie\Kenndaten.accdb` verifiziert (lesend,
ACE OLEDB 16.0). Reale Spalten von `Tab_Klimaregion`:

```
ID (AutoWert) · ID_Projekt · Bezeichner · Longitude · Latitude · Details
```

Die Schreibseite traf das Schema nicht durchgängig und wäre zur Laufzeit gescheitert — aber
nicht in jeder Spalte. Der reale Befund (adversariale Review vom 14.08.2026, V6):

* `Add()` schrieb `Longitude`, `Latitude` und `Details` bereits **korrekt**; falsch war nur
  `Name` (die Spalte heißt `Bezeichner`, `Name` gibt es nur in `Tab_Klimaregion_STAMM`).
  Zusätzlich fehlte die Pflichtspalte `ID_Projekt`.
* `Update()` war in allen fünf Bezeichnern falsch
  (`Name`, `Längengrad`, `Breitengrad`, `Beschreibung`, `WHERE ID_Klimaregion`).
* `Delete()` filterte über `WHERE Name = ?`.

Die frühere Formulierung „keine einzige Spalte korrekt" war eine Übertreibung und ist hier
wie im Klassenkommentar von `KlimaregionCtrl` richtiggestellt. Die produktive Leseseite
(`ReadAll`/`ReadSingle`, konsumiert u. a. von `SimulationSolarthermie.cs:47` und
`SimulationPV.cs:96`) war dagegen immer richtig.

| Methode | vorher | jetzt |
|---|---|---|
| `Add` | `INSERT … (Name, Longitude, Latitude, Details)` | `INSERT … (ID_Projekt, Bezeichner, Longitude, Latitude, Details, Klimazone_DIN4710)`; `idProjekt` ist **Pflicht** (siehe unten) |
| `Update` | `SET Name, Längengrad, Breitengrad, Beschreibung WHERE ID_Klimaregion = ?` | `SET Bezeichner, Longitude, Latitude, Details, Klimazone_DIN4710 WHERE ID = ?` |
| `Delete` | `WHERE Name = ?` | `WHERE Bezeichner = ?`, optional zusätzlich `AND ID_Projekt = ?` |

Aufrufer der drei Methoden gibt es im Produktivcode derzeit keine — der Fehler war deshalb
unbemerkt geblieben. Die Signaturen bleiben quellkompatibel (nur optionale Parameter ergänzt).

### 2.1 Pflicht-Fremdschlüssel `ID_Projekt` (Befund V1a der Review, behoben)

`Tab_Klimaregion.ID_Projekt` ist `NOT NULL` und über die **erzwungene** Beziehung
`Tab_ProjektTab_Klimaregion` an `Tab_Projekt` gebunden. Die erste Fassung ließ bei
`idProjekt = 0` — dem Default und damit dem Pfad jedes bestehenden Aufrufmusters —
`ID_Projekt` aus dem `INSERT` weg. Das ist keine „projektlose Zeile", sondern ein
Laufzeitfehler:

```
Der Datensatz kann nicht hinzugefügt oder geändert werden, da ein Datensatz
in der Tabelle 'Tab_Projekt' … in Beziehung stehen muss
```

`Add()` verlangt jetzt ein gültiges `idProjekt > 0` und bricht andernfalls mit `false` und
einer sprechenden Konsolenmeldung ab, statt die Ausnahme laufen zu lassen; der
XML-Kommentar behauptet keine projektlose Zeile mehr.

Zweiter Befund derselben Stelle (V1b): `Bezeichner` ist ebenfalls `NOT NULL`, `Add()` und
`Update()` setzten dafür aber `DBNull.Value`, sobald der Name leer war
(„Sie müssen einen Wert in das Feld 'Tab_Klimaregion.Bezeichner' eingeben"). Beide schreiben
jetzt `""`. `Details` bleibt `NULL`-fähig und wird weiter als `DBNull` geschrieben.

Drittens rufen `Add()` und `Update()` vor dem SQL
`WaermequelleClass.SpalteSicherstellen("Tab_Klimaregion", "Klimazone_DIN4710", "LONG")` auf.
Ohne das hingen beide daran, dass vorher einmal die Simulationskonfiguration oder ein
Simulationslauf `SchemaSicherstellen()` ausgelöst hat — auf einer frisch installierten
Datenbank wäre ein künftiger Aufrufer (z. B. der Wizard) an der fehlenden Spalte
gescheitert („latente Falle" der Review).

Neue Spalte `Tab_Klimaregion.Klimazone_DIN4710` (LONG, `DEFAULT 0` = unbestimmt), angelegt in
`WaermequelleClass.SchemaSicherstellen()` — der Punkt, den sowohl `SimulationControl` als auch
`Form_Simulation_Config` beim Öffnen durchlaufen. Bestehende Zeilen bleiben nach dem
`ADD COLUMN` NULL; die Leseseite behandelt NULL und fehlende Spalte gleichermaßen als 0.

## 3. Selbsttest (Kapitel B)

`ErdreichTemperatur.Selbsttest()` und `VDI4640Pruefung.Selbsttest()` wurden gegen die
Originaldateien ausgeführt (eigenes Konsolenprojekt im Scratchpad, das die beiden `.cs`-Dateien
per `<Compile Include>` einbindet).

Beide Methoden stehen seit der Review-Nacharbeit in einer **`#if DEBUG`-Klammer**. Damit ist
die frühere Aussage „kein Testcode im Produktivprojekt" auch wörtlich wahr: im
Release-Assembly kommt keine der beiden Methoden mehr vor (nachgewiesen — die Zeichenkette
`Selbsttest` findet sich in `bin\x86\Debug\…\WindowsFormsApplication1.dll` zweimal, in
`bin\x86\Release\…` null mal). Das Konsolenprojekt muss deshalb in **Debug** gebaut werden.

Die Selbsttests sichern seit der Nacharbeit auch wirklich zu, was ihre Kommentare behaupten
(Befund P6 der Review — vorher gaben sie den Katalog und die A2-Tabelle überwiegend nur
aus): zellweise Stichproben `SAND_FEUCHT a = 0,7368 mm²/s / d = 2,7199 m` und
`GNEIS 1,3810 / 3,7233` (Toleranz 1 mm, die Konzeptangaben sind gerundet; gerechnet werden
2,71967 und 3,72321), `A2 Zone 6/Sand = 16 / 31` und `Zone 12/Sandiger Ton = 42 / 56`, die
Monotoniekette über **alle vier** Bodenarten (`0 ≤ 1 ≤ 2 ≤ 3`, vorher übersprang sie die
Spalte Schluff), die Konsistenz `Leistung × Volllaststunden / 1000 ≈ Energie` über alle 60
Zellen (größte Abweichung 0,95 kWh/(m²·a)), die Mockup-Probe 25,9 / 35,6 samt der
405 m² im Hinweistext, die Bereichsmeldung der B2-Klemmung, das vollständige
Bodentyp→Bodenart-Mapping und das Festgestein-Flag im Ergebnis.

### 3.1 Bodentyp-Katalog — reproduziert die Konzepttabelle 13.1 exakt

```
Schluessel        lambda   rho*cp   a[mm2/s]   d[m]    A(1,5m)  A(4m)   A(10m)
TON_TROCKEN        0.5     1.55      0.32    1.80     43 %    11 %   0.4 %
TON_NASS           1.8     2.40      0.75    2.74     58 %    23 %   2.6 %
SAND_TROCKEN       0.4     1.45      0.28    1.66     41 %     9 %   0.2 %
SAND_FEUCHT        1.4     1.90      0.74    2.72     58 %    23 %   2.5 %
SAND_NASS          2.4     2.50      0.96    3.10     62 %    28 %   4.0 %
KIES_TROCKEN       0.4     1.45      0.28    1.66     41 %     9 %   0.2 %
KIES_NASS          1.8     2.40      0.75    2.74     58 %    23 %   2.6 %
MERGEL_LEHM        2.4     2.00      1.20    3.47     65 %    32 %   5.6 %
TONSTEIN           2.2     2.25      0.98    3.13     62 %    28 %   4.1 %
SANDSTEIN          2.8     2.20      1.27    3.57     66 %    33 %   6.1 %
KALKSTEIN          2.7     2.25      1.20    3.47     65 %    32 %   5.6 %
GRANIT             3.2     2.55      1.25    3.55     66 %    32 %   6.0 %
GNEIS              2.9     2.10      1.38    3.72     67 %    34 %   6.8 %
```

Alle 13 Zeilen stimmen in a, d und den drei Amplitudenanteilen mit Konzept 13.1 überein.
Der größte Amplitudenrest in 10 m Tiefe ist **6,8 %** (Gneis) — die Vorgabe „≤ ~7 %" ist
eingehalten, ebenso die Normaussage zur neutralen Zone in 10–20 m.

Einheiten: λ in W/(m·K), ρ·c_p in MJ/(m³·K), a = λ/(ρ·c_p) in m²/s. Für d = √(2a/ω) wird a
nach m²/h umgerechnet (·3600), damit ω = 2π/8760 h⁻¹ eingesetzt werden kann und d in Metern
herauskommt. Gegenprobe: SAND_FEUCHT a = 0,74 mm²/s → d = 2,72 m ✔

### 3.2 Unabhängige Referenz a = 4,17·10⁻⁷ m²/s

```
d              = 2.046 m                    (Konzept: 2,05 m)
Phase in 6,4 m = 4361 h = 181,7 d = 5,97 Monate   (Konzept: 182 d = 6,0 Monate)
```

Die Phasenverschiebung prüft Dämpfungstiefe und Phasenterm gemeinsam — „exakt reproduziert"
bestätigt.

### 3.3 Erdsonde

```
 40 m -> 11,00 °C   (mittlere Tiefe = 20 m, kein geothermischer Anteil)
 50 m -> 11,15 °C   (Konzept: 11,15 °C)
100 m -> 11,90 °C   (Konzept: 11,90 °C)
```

bei T_m = 9,5 °C, ΔT_Oberflaeche = 1,5 K, grad_geo = 0,03 K/m, Abzug 20 m.

### 3.4 Rückgewinnung des Jahresgangs

Synthetischer Jahresgang T_m = 9,5 °C, A = 9,0 K, t_min = 480 h:

```
T_m   = 9.500 °C      A = 9.000 K      t_min = 480,5 h
```

Die Regression läuft über die zwölf Monatsmittel; als Regressoren dient nicht der Wert in der
Monatsmitte, sondern der **exakte Mittelwert von cos bzw. sin über das Monatsintervall**. Damit
ist die Anpassung für einen reinen Sinus erwartungstreu — die naive Monatsmittelung
unterschätzt die Amplitude systematisch um rund 1 %.

Gegenprobe mit überlagertem Tagesgang (±4 K) und Rauschen (±1 K):

```
Regression ueber Monatsmittel : A = 9,01 K
(Max-Min)/2 der Stundenwerte  : A = 13,91 K   -> Ueberschaetzung um 54 %
```

Das belegt die Konzeptvorgabe 4.5, die Amplitude **nicht** aus den Stundenextrema zu bilden.

### 3.5 Kollektorprofil 1,5 m, Sand feucht

```
min 4,32 °C (Feb)   max 14,68 °C (Aug)   Mittel 9,50 °C
Amplitude 5,18 K, erwartet 5,18 K (= 9,0 K × 57,6 %)
```

Größenordnung und Monatslage decken sich mit dem Mockup 4.5
(„min 4,2 °C (Feb) · max 14,8 °C (Aug) · Mittel 9,6 °C").

### 3.6 Auslegungsprüfung

Tabelle A2: 15 Zonen × 4 Bodenarten, Bandbreite 5…42 W/m² — deckt sich mit der Konzeptangabe
(Zone 11/Sand bis Zone 12/sandiger Ton). Bodenart-Reihenfolge
(Sand ≤ Lehm ≤ Schluff ≤ sandiger Ton) in allen Zonen und für beide Größen monoton.

Mockup-Gegenprobe (Zone 6, Sand feucht, 250 m², 6 480 W, 8 900 kWh/a):

```
Entzugsleistung    6.480 W / 250 m² = 25,9 W/m²      Grenze 16 W/m²        !
Entzugsenergie     8.900 kWh/a = 35,6 kWh/(m²·a)     Grenze 31 kWh/(m²·a)  !
  Klimazone 6, Bodenart Sand: Kollektor ist zu klein bemessen.
  Erforderlich sind mindestens 405 m² (Zonen-Volllaststunden 1.950 h/a).
```

Grenzwerte und Istwerte entsprechen dem Mockup 4.5 Ziffer für Ziffer. Klimazone 0 liefert
„Klimazone nicht zugeordnet, Prüfung nicht möglich."

Tabelle B2 (Auszug): alle sechs kodierten Stützstellen werden exakt getroffen; Interpolation
zwischen λ-Stützstellen monoton, Klemmung außerhalb bestätigt (600 h/a, λ 0,2 → 37,5 W/m =
Randwert 1200 h / λ 1,0).

### 3.7 Plausibilitätsschranke des Außentemperaturvektors

```
8760 x 0,0 C         -> T_m   9,50 C  A  8,50 K  AusKlimadaten False
ab h 4000 genullt    -> T_m   9,50 C  A  8,50 K  AusKlimadaten False
konstant 12,0 C      -> T_m  12,00 C  A  0,00 K  AusKlimadaten True
T_m = -30 C          -> T_m   9,50 C  A  8,50 K  AusKlimadaten False
echter Jahresgang    -> T_m   9,50 C  A  9,00 K  AusKlimadaten True
```

## 4. Regression (Kapitel G3)

Der Nachweis ist nach der Review-Nacharbeit **wiederholt** worden (alle Fixes liegen im
Opt-in-Pfad `WQ_Typ = 'Erdreich'` bzw. in Methoden ohne Produktivaufrufer). Zahlen und
Wertezahl sind ziffernweise identisch zum ersten Lauf:

```
Werkzeug : Referenzlauf.exe (Paket B1)
Lauf     : Referenzlaeufe\2026-08-14_Paket3_Review   (8 von 8 Projekten OK, 36 s)
Vergleich: 2026-08-14_B0  ->  2026-08-14_Paket3_Review
Toleranz : relativ 1e-4 ab Betrag 1, sonst absolut 0,01

Projekt_1007: PASS (29 Dateien, 324209 Werte)
Projekt_1008: PASS (21 Dateien, 227832 Werte)
Projekt_1010: PASS (18 Dateien, 201539 Werte)
Projekt_1011: PASS (29 Dateien, 324231 Werte)
Projekt_1017: PASS (20 Dateien, 245377 Werte)
Projekt_1018: PASS (19 Dateien, 210342 Werte)
Projekt_1023: PASS (25 Dateien, 262902 Werte)
Projekt_1024: PASS (22 Dateien, 236615 Werte)

GESAMT: PASS (2.033.047 Werte innerhalb der Toleranz)
```

**PASS wie erwartet** — Erdreich ist opt-in: Ohne `WQ_Typ = 'Erdreich'` an einer Anlage läuft
kein Zeilenzweig des neuen Codes an, und der Luft-Wasser-Kurzschluss in `Quelltemperatur()`
bleibt unverändert.

Die sechs neuen Spalten sind während des Laufs in der **Arbeitskopie** entstanden und dort
nachgewiesen:

```
Tab_Energieanlagen : WQ_Tiefe [Double], WQ_Flaeche [Double], WQ_Anzahl [Int32],
                     WQ_Bodentyp [String], WQ_Quellsystem [String]
Tab_Klimaregion    : Klimazone_DIN4710 [Int32]
```

Die produktive `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` bleibt davon unberührt (der
Referenzlauf verifiziert den DB-Pfad vor jedem Kindprozess).

Der Ordner `Referenzlaeufe\2026-08-14_Paket3_Review` ist reine Nachweisablage und kann nach
Kenntnisnahme gelöscht werden; Referenz bleibt `2026-08-14_B0`. Die Zwischenablage
`2026-08-14_Paket3` des ersten Laufs wurde gelöscht. Das Vergleichsergebnis liegt jetzt als
`vergleich_protokoll.md` **im Laufordner** — `vergleich` schreibt von sich aus nur nach
stdout, der Nachweis lebte bisher allein in diesem Protokoll (Anmerkung V3 der Review).

## 5. Build und Encoding (Kapitel G1, G4)

Gebaut ausschließlich mit dem VS-MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    ..\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
```

Fehlerfrei, Exit-Code 0, keine neuen Warnungen (die sechs verbleibenden Warnungen stammen aus
`StromverbraucherStammCtrl`, `KlimaregionStammCtrl`, `WErzeugerModel` und `MDIMainForm` und
sind Bestand bzw. Parallelarbeit).

Zusätzlich wird seit der Review-Nacharbeit **auch Release gebaut**
(`-p:Configuration=Release -p:Platform=x86`, Exit-Code 0, dieselben sechs Warnungen) — das ist
die Gegenprobe zur `#if DEBUG`-Klammer um die beiden `Selbsttest()`-Methoden.

Encoding: Die drei neuen Dateien sind UTF-8 **mit** BOM. Die geänderten Bestandsdateien haben
ihre vorhandene Kodierung behalten (`Form_Simulation_Config.cs`, `KlimaregionCtrl.cs`,
`KlimaregionModel.cs` mit BOM; `WaermequelleClass.cs` ohne BOM — so lag sie vor). Der `git diff`
über die vier geänderten Dateien ist gültiges UTF-8 und enthält **null** Ersatzzeichen. Die
Ersatzzeichen im Gesamt-Diff des Projekts stammen aus `Views/BHKW/Form_BHKWEing.cs`, einer der
uncommitteten ID_Carrier-Änderungen einer Parallelsitzung, und wurden nicht angefasst.

## 6. Bewusste Abweichungen

1. **Lokalisierung hartkodiert.** Alle sichtbaren Texte in `Form_QuelleErdreich` und in den
   Prüfmeldungen sind deutsch hartkodiert — nach dem verifizierten Bestandsmuster
   (`Form_QuellePufferspeicher`, `Form_Quellprofil`: kein Designer, keine `.resx`, keine
   Satellitenressourcen). Das weicht vom Lokalisierungshinweis in Konzept 4.2 / 13.6 ab. Die
   durchgängige Umstellung des Simulationsbereichs auf `MyResource` gehört zu **Paket 9** und
   wird dort nachgezogen; die Texte liegen dafür an einer Stelle je Dialog beisammen.

2. **Tabelle B2 nur als Auszug.** Kodiert sind genau die Stützstellen, die Konzept 13.1
   wiedergibt: Volllaststunden 1200 / 1800 / 2400 h/a, Sondenzahl 1 und 5 (bei 2400 h/a: 1 und
   4), λ = 1,0 / 2,0 / 3,0 / 4,0 W/(m·K). Zwischenwerte entstehen durch lineare Interpolation,
   außerhalb wird geklemmt. **Die Vervollständigung gegen den Normtext ist offen** und im
   Klassenkommentar von `VDI4640Pruefung` als solche vermerkt. Tabelle A2 ist dagegen
   vollständig kodiert.

3. **Auslegungsprüfung nur im Dialog.** Konzept 4.5 verlangt zusätzlich einen Ausweis im
   Ergebnisbereich, damit die Prüfung den Anwender auch dann erreicht, wenn er den
   Quellendialog nicht mehr öffnet. Der Dialog zeigt bis dahin „(noch kein Simulationslauf)",
   weil die Anbindung der Ergebnisgrößen (maximale Entzugsleistung, Jahresentzugsarbeit,
   Volllaststunden) zur Ergebnis-Persistenz gehört → **Paket 7**. Die Rechenseite
   (`VDI4640Pruefung`) ist fertig und nimmt die Werte über ihre öffentliche API entgegen.

4. **Bodentyp → Bodenart, Zuordnungsregel.** Die 13 Untergrundtypen aus Blatt 1 werden auf die
   vier Bodenarten der Tabelle A1 abgebildet — Textur zuerst, λ als Feinabgleich:

   | Blatt-1-Typ | A1-Bodenart | Begründung |
   |---|---|---|
   | Sand trocken/feucht/nass, Kies trocken/nass | Sand | grobkörnig; A1 kennt keinen Kies. „Sand" führt die niedrigsten A2-Grenzwerte, die Zuordnung ist also konservativ. Bestätigt durch Mockup 4.5: „Sand, feucht" in Zone 6 ergibt dort 16 W/m² und 31 kWh/(m²·a) — exakt Zone 6, Spalte Sand |
   | Ton/Schluff trocken | **Sand** (korrigiert) | hier schlägt der λ-Feinabgleich die Textur: λ = 0,5 W/(m·K) ist der kleinste Wert des ganzen Blatt-1-Katalogs und liegt unter dem kleinsten A1-Rechenwert (Sand, 1,2). Die A1-Bodenarten sind gerade über den Wassergehalt definiert; „Schluff" steht dort für 35…40 Vol.-% und hätte in Zone 6 einen um 75 % höheren Entzug zugelassen (28 statt 16 W/m²) als für feuchten Sand. Die frühere Zuordnung auf „Schluff" war die einzige nicht konservative Stelle des Mappings (Befund P5 der Review) |
   | Ton/Schluff wassergesättigt | Sandiger Ton | bindig, λ = 1,8 = exakter A1-Rechenwert |
   | Geschiebemergel/-lehm | Lehm | Textur |
   | Tonstein, Sandstein, Kalkstein, Granit, Gneis | Sandiger Ton | A2 gilt für Lockergestein; Fels liegt mit λ 2,2…3,2 über allen A1-Klassen → höchste Klasse. Ein Flachkollektor im Fels ist untypisch; das Ergebnisobjekt trägt dafür seit der Nacharbeit das Feld `Ergebnis.FestgesteinNaeherung` (vorher stand der Vorbehalt nur im Dialog, obwohl der Klassenkommentar die Kennzeichnung im Ergebnis behauptete) |

5. **Klimazone am Ort der Region, nicht der Anlage.** Konzept 13.1 legt fest, dass die Zone
   eine Eigenschaft der Klimaregion ist. Der Dialog belegt sie aus
   `Tab_Klimaregion.Klimazone_DIN4710` vor; eine Änderung im Dialog wird an die Region
   zurückgeschrieben, nicht an `Tab_Energieanlagen`. Deshalb gibt es dafür keine der in 5.3
   gelisteten `WQ_*`-Spalten.

6. **Zonenbezeichnung im Dropdown.** Statt Regionsnamen („6 — Nordwestdeutschl." im Mockup)
   steht dort die Zonennummer mit den Jahresvolllaststunden der Zone („6 — 1.950 h/a"). Die
   Zuordnung Zonennummer → Landschaftsname steht nicht im Konzept und wurde bewusst nicht
   erfunden.

7. **`WQ_Anzahl` ohne `DEFAULT 1`.** Konzept Kapitel 12 listet die Spalte mit Default 1;
   angelegt wird sie ohne `DEFAULT` (Bestandszeilen bleiben `NULL`), und
   `Form_QuelleErdreich.cs:509` schreibt im Kollektorfall aktiv **0** hinein. Das ist bewusst:
   ein Erdkollektor hat keine Sonden, und „0 Sonden" ist die ehrlichere Angabe als „1". Alle
   Leser sind dagegen abgesichert (`Convert.ToInt32(…) > 0` bzw. `Math.Max(1, …)`), die
   Abweichung bleibt damit folgenlos. Sie ist die einzige der sechs neuen Spalten, deren
   Default-Angabe aus dem Konzept nicht umgesetzt wurde — `Klimazone_DIN4710` hat ihr
   `DEFAULT 0` bekommen.

8. **Plausibilitätsschranke statt reiner Längenprüfung.** `AnalysiereJahresgang` prüfte
   zunächst nur die Länge des Außentemperaturvektors. Ein 8760er-Array aus Nullen — real
   erreichbar, weil `Form_Simulation_Config.cs:416` `DBNull` auf `0f` abbildet und
   `SimulationWaermebedarf.Stundentemperatur_aus_DB` ein vorbelegtes Array nur so weit füllt,
   wie `Tab_Solar` Zeilen hat — lief damit als „echter" Jahresgang mit T_m = 0 °C durch, ohne
   Hinweis im Dialog (Befund P3 der Review). Jetzt gelten zusätzlich: Anteil exakter Nullen
   > 5 %, Jahresmittel außerhalb −10…+25 °C und ein nahezu konstanter Gang (A < 1 K) mit
   einem Mittel außerhalb 0…20 °C führen auf die Ersatzwerte 9,5 °C / 8,5 K mit
   `AusKlimadaten = false`, also mit dem Dialoghinweis „(ohne Klimadaten — Ersatzwerte)".
   Ein bewusst gesetzter Konstantvektor (z. B. 12 °C, A = 0) bleibt gültig.

## 7. Offene Punkte

1. **Klimazonen-Zuordnung der EPOS-Regionen** (Konzept 13.1, „vor der Umsetzung zu klären").
   Die Spalte `Klimazone_DIN4710` existiert jetzt, ist aber in allen Bestandszeilen NULL/0. Die
   vorhandenen Klimaregionen der Arbeitskopie („Berlin", „stuttgart", „Texas", …) lassen keinen
   automatischen Schluss auf die 15 DIN-4710-Zonen zu — eine Vorbelegung über die Zonenkarte
   (Bild A1) anhand von `Longitude`/`Latitude` ist möglich, braucht aber die Zonengeometrie.
   Solange die Zone 0 bleibt, meldet die Kollektorprüfung „Prüfung nicht möglich"; alles andere
   (Jahresgang, Simulation) funktioniert unabhängig davon. Bis zur Klärung ist die Zone im
   Dialog von Hand setzbar.

   **Nachtrag (Befund V6.3 der Review):** `Tab_Klimaregion` ist keine Stammtabelle, sondern
   die **Projektkopie** einer Region — 17 Zeilen in der Arbeitskopie, jede mit eigener
   `ID_Projekt`. Die Zonenspalte liegt genau dort. `Tab_Klimaregion_STAMM` hat **keine**
   Zonenspalte, folglich kann `KlimaregionStammCtrl.CopyRegionToProjekt()` (`:266`) sie beim
   Anlegen eines neuen Projekts nicht mitführen: die Zone startet in jedem neuen Projekt bei
   0 und geht bei jeder Kopie verloren. Die Konzeptzusage 13.1 „einmal je Region gepflegt
   statt je Projekt" ist damit **nicht erreicht** — sie wird erst mit einer Zonenspalte in
   `Tab_Klimaregion_STAMM` (plus Übernahme in `CopyRegionToProjekt`) eingelöst. Das ist eine
   echte Schemaänderung an einer `_STAMM`-Tabelle und gehört deshalb in die **Migration**;
   sie ist hier bewusst **zurückgestellt** (Paket 3 ändert das Schema ausschließlich additiv
   über `ADD COLUMN`). Bis dahin gilt: Zone je Projekt von Hand setzen.

2. **Tabelle B2 vervollständigen** — siehe Abweichung 2. Stufe 2 nach Konzept 13.1 ergänzt
   zusätzlich B3–B7 (mit Trinkwassererwärmung, mit Kühlung, andere Austrittstemperaturen),
   Tabelle A3 für Kapillarrohrmatten und die Rohrabstands-Empfehlung (0,2…0,65 m je Bodenart).

3. **Ergebnisanbindung der Auslegungsprüfung** → Paket 7, siehe Abweichung 3. Dazu gehört auch
   die zweite Warnbedingung aus Konzept 13.1 (Quelltemperatur minus Spreizung soll 0 °C nicht
   dauerhaft unterschreiten).

4. **Konzept-Rest O11.** Entzugsleistung und Regeneration des Erdreichs werden nicht
   modelliert (bewusste Vereinfachung nach Konzept 4.5). Wer Quellerschöpfung abbilden will,
   nutzt weiterhin den Quellentyp `Pufferspeicher` mit Regeneration. Die Vereinfachung ist im
   Klassenkommentar von `ErdreichTemperatur` dokumentiert und gehört in den Ergebnisausweis.

5. **Gleichartiger Bestandsfehler außerhalb des Auftrags:**
   `Views/Hauptformular/FormMain.cs`, `GetIDKlimaregion()` (~`:551`) liest über `RecordSet`
   `select * from Tab_Klimaregion where Name = '…'` und `rs.Read("ID_Klimaregion")` — beide
   Spalten existieren in `Tab_Klimaregion` nicht (richtig wären `Bezeichner` und `ID`). Das ist
   derselbe Fehlertyp wie in `KlimaregionCtrl`, liegt aber in einem Formular außerhalb des
   Paket-3-Umfangs und wurde nicht angefasst. Gehört zu den B0-Bestandsfehlern.

6. **Beziehungen und `FK_MAP`.** Konzept 5.3 verlangt für die neuen ID-Spalten erzwungene
   Access-Beziehungen und Einträge in `ProjektDuplizierenCtrl.FK_MAP`. Das betrifft die
   Puffer-FKs (`WS_ID_Puffer`, `WS_ID_Puffer2`, `WQ_ID_Puffer`), **nicht** die hier angelegten
   Erdreich-Spalten — die sind reine Skalare ohne Fremdschlüsselbezug und beim
   Variantenanlegen unkritisch. Mit der Migration nachzuholen bleibt es trotzdem für die
   Puffer-Spalten aus Paket 2/4.

7. **Bekannte Kleinpunkte** (aus der adversarialen Review, jeweils bewusst nicht behoben):

   * **Vorschau ≠ Simulation im Randfall.** Hat eine Klimaregion weniger als 8760
     `Tab_Solar`-Zeilen, liefert `Form_Simulation_Config.AussentemperaturLaden()` (`:410`)
     `null`, und der Dialog zeigt die Ersatzkurve 9,5 °C / 8,5 K mit dem Hinweis „(ohne
     Klimadaten — Ersatzwerte)". Die Engine dagegen lässt in
     `SimulationWaermebedarf.Stundentemperatur_aus_DB` (`:563-577`) den Vektor teilbefüllt
     (Rest 0 °C). Seit der Plausibilitätsschranke (Abweichung 8) rechnet
     `ErdreichTemperatur` in **beiden** Fällen mit denselben Ersatzwerten — die Erdreichquelle
     driftet also nicht mehr auseinander. Für die übrigen Verbraucher des Vektors
     (Wärmebedarf, Luft-Wasser-WP) bleibt der Randfall bestehen; das gehört zur Datenpflege
     der Klimaregion und nicht in Paket 3.
   * **Cache-Flag der Außentemperatur.** `AussentemperaturLaden()` setzt
     `_aussentempGeladen = true` auch dann, wenn kein Vektor gefunden wurde (`:405`). Wechselt
     der Anwender die Klimaregion innerhalb derselben Formularsitzung, bemerkt die Vorschau
     das nicht. Für eine reine Vorschau vertretbar, hier dokumentiert.
   * **Chart-Neuzeichnung.** `Form_QuelleErdreich.Aktualisieren()` hängt an `TextChanged`
     aller vier Eingabefelder und trägt je Tastendruck 2 × 8760 Punkte neu ein; zusätzlich
     wird `AnalysiereJahresgang` je Aktualisierung zweimal gerechnet (einmal in
     `JahresprofilKollektor`, einmal für das `AusKlimadaten`-Flag). Der DB-Zugriff ist wie
     gefordert gecacht, die Zeichnung nicht. Die Außentemperatur-Serie ändert sich nie und
     müsste nur einmal gefüllt werden — funktional harmlos, Optimierung offen.
   * **Fünf UPDATEs ohne Transaktion.** Der Dialog schreibt `WQ_Quellsystem`, `WQ_Tiefe`,
     `WQ_Flaeche`, `WQ_Anzahl` und `WQ_Bodentyp` einzeln über `WertSchreiben`, das Fehler
     schluckt. Gegen den gefährlichsten Teilzustand (Quellsystem „Kollektor" mit
     Sondenlänge in `WQ_Tiefe`) steht jetzt ein Konsistenz-Check in
     `WaermequelleClass.Quelltemperatur()`: `WQ_Tiefe > 10 m` im Kollektorfall wird als Sonde
     gerechnet und protokolliert (reale Verlegetiefen liegen bei 1…2 m, der Dialog begrenzt
     auf 10 m). Eine echte Transaktion über alle fünf Felder bleibt offen.
   * **B2-Klemmung auf der Sondenzahl-Achse.** Sie ist dort **nicht** konservativ: die
     zulässige spezifische Entzugsleistung sinkt mit wachsender Sondenzahl, ein 20-Sonden-Feld
     bekäme sonst kommentarlos den 5-Sonden-Grenzwert. Das Ergebnis trägt jetzt das Feld
     `Ergebnis.AusserhalbTabelle` und im Hinweistext „außerhalb des kodierten Tabellenbereichs
     (B2-Auszug)". Die eigentliche Abhilfe ist die Vervollständigung der Tabelle
     (offener Punkt 2).

## 8. Reproduktion

```powershell
# 1. Build (beide Konfigurationen - Release ist die Gegenprobe zur #if-DEBUG-Klammer)
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Release -p:Platform=x86

# 2. Selbsttest (Konsolenprojekt mit <Compile Include> auf die beiden Quelldateien,
#    zwingend in Debug - im Release sind die Methoden ausgeklammert)
#    ErdreichTemperatur.Selbsttest()  und  VDI4640Pruefung.Selbsttest()

# 3. Regression
$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
& $exe lauf --ziel C:\Waermeplan\WP_Plan\Referenzlaeufe\<neuer_Ordner>
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B0 `
                 C:\Waermeplan\WP_Plan\Referenzlaeufe\<neuer_Ordner>
```

## 9. Nacharbeit zur adversarialen Review (14.08.2026)

Die Review hat den Katalog, die Kusuda-Formel, die Ableitung von T_m/A/t_min, die Sondenformel,
Tabelle A2, den B2-Auszug, das Schema, `Quelltemperatur()`, beide Dialoge, Build, Encoding und
den Regressionsnachweis als **korrekt** bestätigt (jeweils nachgerechnet, nicht nur gelesen).
Beanstandet wurden sechs Punkte, alle nachgearbeitet:

| Befund | Was geändert wurde | Datei |
|---|---|---|
| P3 — Nullvektor lief als echter Jahresgang durch | Plausibilitätsschranke (Nullanteil > 5 %, T_m außerhalb −10…+25 °C, A < 1 K mit unplausiblem T_m) ⇒ Ersatzwerte, `AusKlimadaten = false` | `ErdreichTemperatur.cs` |
| P5 — `TON_TROCKEN` → „Schluff" war nicht konservativ | Zuordnung auf **„Sand"**, Begründung im Code und in Abweichung 4 konsistent zur Regel „Textur zuerst, λ als Feinabgleich" | `VDI4640Pruefung.cs` |
| B2 — Klemmung auf der Sondenzahl-Achse unbemerkt | `Ergebnis.AusserhalbTabelle` + Hinweistext „außerhalb des kodierten Tabellenbereichs (B2-Auszug)"; `AusserhalbB2Bereich()` prüft λ- und Sondenzahl-Achse | `VDI4640Pruefung.cs` |
| V1 — `Add()` scheiterte an der Pflicht-FK, `Bezeichner` NOT NULL als `DBNull` | `idProjekt > 0` ist Pflicht (sonst `false` + Log), leerer Name als `""`, `SpalteSicherstellen` in `Add()`/`Update()`, XML- und Klassenkommentar richtiggestellt | `KlimaregionCtrl.cs` |
| P6 — Selbsttests prüften weniger als behauptet | zellweise Stichproben, Monotoniekette `0 ≤ 1 ≤ 2 ≤ 3`, Tabellenkonsistenz, Bereichs- und Mapping-Asserts, Plausibilitätsproben; XML-Kommentare auf das tatsächlich Geprüfte gezogen; beide `Selbsttest()` in `#if DEBUG` | `ErdreichTemperatur.cs`, `VDI4640Pruefung.cs` |
| Beobachtungen — teilgeschriebene Feldsätze, Festgestein-Flag | Konsistenz-Check „Kollektor mit `WQ_Tiefe` > 10 m ⇒ Sonde"; `Ergebnis.FestgesteinNaeherung`, vom Dialog genutzt | `WaermequelleClass.cs`, `VDI4640Pruefung.cs`, `Form_QuelleErdreich.cs` |

Nachweis der Nacharbeit: Debug **und** Release fehlerfrei (je 0 Fehler, dieselben sechs
Bestandswarnungen), beide Selbsttests „alle Pruefungen bestanden", Regression
`2026-08-14_B0 → 2026-08-14_Paket3_Review` **PASS** über dieselben 2.033.047 Werte.
