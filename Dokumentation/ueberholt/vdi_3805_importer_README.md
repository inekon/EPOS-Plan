# VDI 3805 Blatt 22 – Wärmepumpen Scraper & Export

## Beschreibung

C# .NET 8 Konsolenprogramm zum Auslesen aller VDI 3805 Wärmepumpen-Produktdaten von:
**https://catalogue.bim4hvac.eu/BDH/Default.aspx**

Export ausschließlich als normkonforme **VDI 3805 Blatt 22 PART-XML (.vdi)** mit allen
für den Wärmepumpen-Import relevanten Daten.

## Voraussetzungen

- .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8)
- Keine externen NuGet-Pakete erforderlich (nur BCL)

## Build & Start

```bash
dotnet build
dotnet run
```

## Bedienung

Das Programm führt interaktiv durch folgende Schritte:

1. **Katalog laden** – lädt die Hersteller-Liste von catalogue.bim4hvac.eu
   - Bei fehlender Internetverbindung: automatischer Fallback auf eingebettete Demo-Katalogliste
2. **Produktart filtern** – z.B. nur „Wärmepumpen" (VDI 3805 Blatt 22)
3. **Hersteller auswählen** – alle oder bestimmte (z.B. `1 3 5`)
4. **Länder-Filter** – z.B. nur „Deutschland"
5. **Download & Parse** – lädt .vdi-Dateien herunter und parst sie
   - Demo-Datensätze als Fallback (12 Geräte, 8 Hersteller)
6. **Export** – speichert als VDI 3805 Blatt 22 PART-XML

## Projektstruktur

```
VdiScraper/
├── Program.cs                      Einstiegspunkt
├── ConsoleApp.cs                   Interaktiver Dialog + Demo-Daten
├── VdiScraper.csproj               .NET 8 Projekt
├── Models/
│   └── Vdi3805Models.cs            Alle Datenmodelle (SA 100..740)
├── Services/
│   ├── KatalogParser.cs            HTTP-Parser für BIM4HVAC-Katalog
│   └── VdiXmlParser.cs             PART-XML Einleser für .vdi-Dateien
└── Export/
    └── Vdi3805Exporter.cs          VDI 3805 Blatt 22 XML-Exporter
```

## VDI 3805 Blatt 22 Export-Format (PART-XML)

```xml
<VDI3805 Richtlinie="VDI 3805 Blatt 22" Ausgabe="2019-03">
  <PART parttype="100">  <!-- SA 100: Hersteller -->
    <PART parttype="110">  <!-- SA 110: Produktgruppe Wärmepumpen -->
      <PART parttype="700">  <!-- SA 700: Grunddaten pro Gerät -->
        <PART parttype="710"/>  <!-- SA 710: Luft/Wasser-COP-Tabelle -->
        <PART parttype="720"/>  <!-- SA 720: Betriebspunkte EN 14511 -->
        <PART parttype="730"/>  <!-- SA 730: Geometrie -->
      </PART>
    </PART>
  </PART>
</VDI3805>
```

## Exportierte Schlüsselfelder (SA 700 Grunddaten)

| attr | Beschreibung                      | Einheit |
|------|-----------------------------------|---------|
| 4    | Produktname                       | –       |
| 5    | Heizleistung                      | kW      |
| 6    | Leistungszahl (COP)               | –       |
| 7    | Elektr. Aufnahmeleistung WP       | kW      |
| 10   | WP-Typ (1=Luft/W, 2=Sole/W, 3=W/W)| –      |
| 13   | Kältemittel                       | –       |
| 15   | Schallleistungspegel              | dB(A)   |
| 17   | Max. Vorlauftemperatur            | °C      |
| 21   | COP A7/W35 (EN 14511)             | –       |
| 22   | COP A2/W35 (EN 14511)             | –       |
| 23   | COP A-7/W35 (EN 14511)            | –       |
| 25   | ErP-Effizienzklasse               | –       |
| 26   | SCOP (Seasonal COP)               | –       |

## Demo-Datensätze (bei fehlender Verbindung)

12 Geräte von 8 Herstellern: DAIKIN, Bosch/Junkers, Hoval, Buderus, Kermi,
Stiebel Eltron, Vaillant, Viessmann – alle mit vollständigen EN 14511-Betriebspunkten
(A-7/W35, A2/W35, A7/W35, A-7/W45, A2/W45, A7/W45, A-7/W55, A2/W55, A7/W55).
