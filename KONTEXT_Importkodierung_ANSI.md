# Kontext: ANSI-Kodierung der Herstellerdaten-Importe

Stand: 15.08.2026

## Ursache

Die Herstellerdateien (VDI 3805, PVsyst-PAN) sind ANSI/Windows-1252 kodiert. Unter .NET 8 ist
`Encoding.Default` **UTF-8** (nicht mehr die ANSI-Codepage wie im .NET Framework); auch
`File.OpenText` liest UTF-8. Jedes Umlaut-Byte (z. B. `0xE4` für „ä") ist in UTF-8 ungültig und
wird beim Dekodieren durch U+FFFD (Ersatzzeichen „�") ersetzt — der Name landet dauerhaft
beschädigt in `Kenndaten.accdb`.

Die korrekte Kodierung liefert zentral `Allgemein/Import/AnsiEncoding.cs` (Windows-1252,
Rückfall ISO-8859-1, weil Codepage 1252 unter .NET Core ohne `CodePagesEncodingProvider` nicht
verfügbar ist). Neue Importe, die ANSI-Dateien lesen, müssen `AnsiEncoding.Get()` verwenden.

## Korrigierte Fundstellen

| Datei | vorher |
|---|---|
| `Allgemein/Import/VDI 3805/Heizkesselmport.cs` | bereits korrigiert (private Kopie, jetzt auf `AnsiEncoding` umgestellt) |
| `Allgemein/Import/VDI 3805/PufferSpImport.cs` | `Encoding.Default` |
| `Allgemein/Import/VDI 3805/Solarkollektorenlmport.cs` | `Encoding.Default` |
| `Allgemein/Import/VDI 3805/WaermepumpenImport.cs` | `File.OpenText` (= UTF-8) |
| `Views/Photovoltaik/Form_CECImport.cs` (PAN-Import) | `Encoding.Default` |

## Geprüft, nicht betroffen

- `Allgemein/Import/CEC/CECDataService.cs` — Quelle sind UTF-8-CSVs (GitHub/NREL), der lokale
  Cache wird UTF-8 geschrieben und gelesen; konsistent.
- `Allgemein/Import/CsvReader.cs` — arbeitet auf einem `TextReader`, die Kodierung bestimmt der
  Aufrufer.
- `Allgemein/Import/IniFileParser.cs` — liest UTF-8 (`File.ReadLines` ohne Encoding), hat aber
  **keinen Aufrufer** (toter Code, nur noch im `.csproj.netfx-backup` referenziert). Bei einer
  Wiederverwendung Kodierung explizit festlegen.

## Beschädigte Bestandsdaten (nicht bereinigt)

Aus Importen vor der Korrektur des Kesselimports stehen in `Kenndaten.accdb` real beschädigte
Bezeichner (das Zeichen ist U+FFFD im Datenbestand, keine Anzeigefrage):

- `Tab_Heizkessel.Bezeichner`: „Vitocrossal 200 CM2 raumluftabh�ngig"
- `Tab_Energieanlagen.Bezeichner` (Projekte 1018 und 1026): derselbe Wert

**Entscheidung:** Die Werte bleiben unverändert, bis der Anwender die Anlage neu importiert —
der Neuimport schreibt den korrekten Namen. Eine Bereinigung per Skript wäre ein Schreibzugriff
auf die Produktiv-DB und wird nur nach separater Abstimmung gemacht (vorher: `Kenndaten.laccdb`
prüfen, datierte Kopie nach `DB-Backup/`).
