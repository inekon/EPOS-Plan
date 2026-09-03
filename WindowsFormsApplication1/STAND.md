# STAND.md — datierte Stände des Anwendungsprojekts

Hier steht, was altert: Schemanummern, Etappen, Zahlen, Basen. Wer etwas hieraus verwendet, prüft
das Datum. Die Regeln stehen in [`CLAUDE.md`](CLAUDE.md); diese Datei wird nur bei Bedarf gelesen.

**Stand 03.09.2026**, Branch `ios_migration` (nach iU9 Welle 6).

## Datenhaltung

- Cutover Access → SQLite umgesetzt (Arbeitspakete S0–S8, Commit `6486c36`, 02.09.2026).
  Protokolle `../sql/S*_Protokoll_*.md`; Referenzläufe auf beiden Backends **byte-identisch**
  (S7: 10/10 Projekte, 234 CSV). Der SQLite-Stand liegt auf `ios_migration`, `sqlite` und
  `lokal_dirk`; **`main` (01.09.2026) steht noch auf der Access-Fassung** mit ACE OLEDB x64.
- `SchemaMigration.ZIEL_VERSION = 61` (Access-Freeze). **Reserviert, nicht umgesetzt:** 62 =
  Einheitenbruch Brennstoff-Stamm „m³" → „Nm³" (entschieden 30.08.2026, Konzept nur auf Zweig
  `claude/lucid-cori-a9a425`); 63 = B6 Modus § 9 Abs. 1 Nr. 3 (`Stromst_Befreiung_Modus`).
- Nummernhistorie: 48–54 Pufferspeicher-Konzept · 55 B2 Kessel-Temperaturmodus · 56/57 CO₂-Saat und
  Emissionsarten · 58 E6 Quellensaat · 59 Pflichtpositionen (H1) · 60 B2 · 61 B3a.

## Umstellungen

- **x64** seit 22.08.2026 (Paket P2; Rückweg Git-Tag `letzter-x86-stand`; offene Pakete P3–P5 in
  `../Konzept_Umstellung_64Bit_EPOS-Plan.md`). ODBC ist seit P1 vollständig entfernt.
- **Assembly `EPOS_Plan`** seit 29.08.2026 (Stufe 0 — nur der Ausgabename; Stufe 1 = Namespace
  bräuchte ein eigenes Konzept). Eine alte `WindowsFormsApplication1.exe` kann als Leiche in `bin\`
  liegen; `user.config` wandert mit dem Namen (Bestand war leer).
- **iOS-Migration, Pakete iU0 und iU1 erledigt 02.09.2026** (`../Umsetzung_iU0_iU1_Nachweise.md`):
  `net8.0` → `net10.0-windows` (`../global.json` 10.0.400, `../Directory.Build.props`); die
  COM-Referenzen `Microsoft.Office.Interop.Excel` und `VBIDE` sind entfernt (iU1-P1.1, Excel-Import
  über ClosedXML) — seither baut `dotnet build`; Setup auf `dotnet publish` (iU1-P1.10, `ce2dc9e`);
  WFO1000 per `../.editorconfig` auf `warning` (60 Fundstellen laut iU1, Schwerpunkt
  `Form_Gesetzesparameter`, `Form_Kosten_VarAuswahl`, Karten-Controls des Kostenmoduls; Beweisbau
  02.09.: 39 Warnungen, 0 Fehler); das Projekt `CSExeCOMServer` ist aus dem Repo entfernt (iU0-P0.1,
  Historie `git show 922228a:CSExeCOMServer/`).
- **Datenzugriff-Brücke:** `OleDbParameter → DbParam` implizit, `DbParam.Von(…)` für Arrays; hält
  432 Altaufrufe unter `Views/` lauffähig bis iU9. `RecordSet` hat 47 echte Nutzer (iR8).
- Die Altkopien `..\WindowsFormsApplication1 - Kopie` und `..\mit_Puffer_KI_Lösungsversuch` sind
  seit 29.08.2026 entsorgt.
- **Maskenumstellung nach Blazor (Paket iU9), Stand nach Welle 6 (03.09.2026):** Der Stapellauf
  der Formularkarte zählt **81 Masken** (88 nach W5, 91 nach W4, 98 nach W3, 102 nach W2,
  105 nach W0, 111 nach W1, 118 davor), davon **54 lokalisiert** und **79 über einen Öffner
  erreichbar** — 0 × „nein", 0 × „verwaist". Die Warnzahl der Mappe steht bei **20**
  (14 WFO1000, 2 CS0108, 2 CS0109, 1 WFO0003, 1 CA2255). Welle 6 hat die sieben
  Erzeugereingaben der Startkacheln umgestellt (4 202 Zeilen) und dabei erstmals
  **Assistentenseiten** auf Razor gehoben (`BlazorAssistentSeite<T>`, vier von dreizehn
  Seiten). Portprotokoll `Allgemein/Reporting/iU9_W6_Blazor_Port_Protokoll.md`.

## Simulation (`Allgemein/Simulation/`)

- Konzept Brauchwasser/Heizung/Pufferspeicher **vollständig umgesetzt 27./28.08.2026**:
  Dreikanalbilanz, Senkentabelle, Warnkriterien W1–W6 + harte Guards, Altpfad-Abriss,
  Schichtspeicher (N = 1…10, SOC führend), Booster (Quelltemperatur stundengekoppelt, Lesepunkt
  `Tab_Einstellungen.Booster_Lesepunkt`, Default „Davor" = Stundenanfang, Paket B2),
  Kessel-Temperaturbezug `Tab_Energieanlagen.WQ_TemperaturModus` („Berechnet" = Senkenspeicher →
  Katalog → 70/50, Default; „Fest" = Vorgabe), Quellprofile. Protokolle `V0_…` bis
  `L_Aufraeumen_Protokoll.md` + Nachträge `E2_…`, `DCheck_…`.
- Referenzläufe: aktuelle Basis `../Referenzlaeufe/2026-08-30_B3-Kaskade/` — 13 Projekte, 332 CSV.
- Die auskommentierten `CSExeCOMServer.SimpleObject`-Zeilen in den Simulationsklassen sind Altbestand
  und können weg.

## Hilfe und KI (Umsetzung 29.08.2026)

- `WikiHelpCatalog` lädt die Rubrik über die Action-API (`allpages` + `apprefix`), Basis-URL
  `Settings.WordPressUrl`, Not-Rückfall `Program.WIKI_STANDARD`; `help_mapping.txt`/`help_cache.json`
  mit Kurznamen der Rubrik-Unterseiten, optional `#anker`; EN über translate.goog. Protokoll
  `Allgemein/Hilfe/H1H2_Umsetzung_Protokoll.md`.
- `WikiWissen`: Wiki-Suche + Klartext-Auszüge, 24-h-Cache `%APPDATA%\wp-plan\wiki-wissen\`, speist
  die „Hilfeabschnitte" des Prompts; Chatfenster ohne KI = Online-Doku-Suche. Protokoll
  `Allgemein/KI/H4H5_Umsetzung_Protokoll.md`. Der Registry-Altwert des API-Keys wird einmalig
  migriert und gelöscht.

## Wirtschaftlichkeit

Führend `../Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` (02.09.2026) mit
`../Wirtschaftlichkeit_Kosten/` (Mockups, Rechenwege). Umgesetzt: Pflichtpositionen H1 (Schritt 59),
B1–B4, BK1–BK3, HB1; offen B5–B7 (B5 wartet auf Mockup-Abnahme), ValERI-Etappen V-A…V-E. Die
Fußleiste von `UcWirtschaftlichkeit` ist voll (Lücke K8).

## Lokalisierung und Kodierung (gemessen 02.09.2026)

- Views-Ordner ohne `de-DE.resx`: Admin, BHKW, Bericht, BerichteKosten, Brauchwasser,
  GemeinsameBausteine, Help, Import, Klimadaten, Photovoltaik, Varianten, Wirtschaftlichkeit.
- Kodierung: alle 573 `.cs` sind UTF-8 (iU1-P1.12 hat 68 cp1252-Dateien umkodiert); 455 mit BOM,
  118 ohne — unschädlich, je Datei beibehalten.
