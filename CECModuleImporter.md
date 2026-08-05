# CEC PV-Modul Import-Tool – Installationsanleitung

## Voraussetzungen

| Werkzeug | Version | Download |
|---|---|---|
| Visual Studio | 2022 (Community/Pro/Enterprise) | https://visualstudio.microsoft.com |
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download |
| Workload | **.NET Desktop Development** (WinForms) | Im VS Installer auswählen |

---

## Projektstruktur

```
CECModuleImporter/
├── CECModuleImporter.csproj   ← Projektdatei mit NuGet-Abhängigkeiten
├── Program.cs                 ← Einstiegspunkt
├── Models/
│   └── PVModule.cs            ← Datenmodell (alle CEC-Parameter)
├── Services/
│   └── CECDataService.cs      ← Datenladen, Filtern, Excel-Export
└── UI/
    └── MainForm.cs            ← WinForms-Hauptfenster
```

---

## NuGet-Pakete (werden automatisch wiederhergestellt)

| Paket | Version | Zweck |
|---|---|---|
| **CsvHelper** | 33.x | CSV-Parsing der CEC-Datei |
| **Newtonsoft.Json** | 13.x | JSON-Hilfsfunktionen |
| **Microsoft.Data.Sqlite** | 8.x | Optionaler lokaler Cache |
| **ClosedXML** | 0.102.x | Excel-Export (.xlsx) |

---

## Erste Schritte in Visual Studio

1. **Projekt öffnen:**  
   `Datei → Öffnen → Projekt/Projektmappe` → `CECModuleImporter.csproj`

2. **NuGet-Pakete wiederherstellen:**  
   Automatisch beim ersten Build, oder manuell:  
   `Extras → NuGet-Paket-Manager → Pakete für Projektmappe wiederherstellen`

3. **Starten:**  
   `F5` (Debug) oder `Strg+F5` (ohne Debugger)

---

## Bedienung

### Daten laden
| Schaltfläche | Funktion |
|---|---|
| **Aus CEC-Datenbank laden** | Lädt automatisch vom NREL/pvlib-Repository (Internet erforderlich). Danach lokaler Cache (30 Tage). |
| **CSV-Datei öffnen** | Öffnet eine lokale CEC-Moduldatei (z. B. `sam-library-cec-modules-*.csv`) |

**Bezugsquelle der offiziellen CSV:**  
https://github.com/pvlib/pvlib-python/tree/main/pvlib/data

### Filterparameter

| Filter | Beschreibung |
|---|---|
| **Hersteller** | Herstellerauswahl (aus Modulname abgeleitet) |
| **Baujahr** | Vierstellige Jahreszahl aus dem Modulnamen |
| **Technologie** | z. B. `monoSi`, `polySi`, `CdTe`, `CIGS` |
| **Leistung [W]** | Min/Max STC-Nennleistung |
| **Effizienz [%]** | Min/Max Wirkungsgrad |
| **Bifaziale Module** | Checkbox für bifaziale Filterung |

### Detailansicht (rechte Seite)
Nach Klick auf ein Modul in der Liste:

| Tab | Inhalt |
|---|---|
| **Übersicht** | Name, Hersteller, Technologie, Fläche, Abmessungen |
| **Elektrisch** | I_sc, V_oc, I_mp, V_mp, Temperaturkoeffizienten |
| **Diodenmodell** | a_ref, I_L_ref, I_o_ref, R_s, R_sh, E_g (1-Dioden-Modell) |
| **Thermisch** | NOCT, TK Leistung/Strom/Spannung |

### Excel-Export
Klick auf **Excel exportieren** → speichert alle gefilterten Module als `.xlsx`

---

## Parameter-Erklärungen (CEC-Modell)

| Parameter | Einheit | Bedeutung |
|---|---|---|
| STC | W | Leistung bei Standardtestbedingungen (1000 W/m², 25°C, AM1.5) |
| PTC | W | Leistung bei PV USA Test Conditions (1000 W/m², 20°C, 1 m/s Wind) |
| I_sc_ref | A | Kurzschlussstrom bei STC |
| V_oc_ref | V | Leerlaufspannung bei STC |
| I_mp_ref | A | Strom im Maximalleistungspunkt (MPP) |
| V_mp_ref | V | Spannung im MPP |
| alpha_sc | A/°C | Temperaturkoeffizient des Kurzschlussstroms |
| beta_oc | V/°C | Temperaturkoeffizient der Leerlaufspannung |
| gamma_r | %/°C | Temperaturkoeffizient der Leistung |
| a_ref | — | Modifizierter Idealitätsfaktor × k × T_ref |
| I_L_ref | A | Photostrom bei STC |
| I_o_ref | A | Diodensättigungsstrom bei STC |
| R_s | Ω | Serienwiderstand |
| R_sh_ref | Ω | Parallelwiderstand bei STC |
| T_NOCT | °C | Nominal Operating Cell Temperature |
| EgRef | eV | Bandlückenenergie des Halbleiters |

---

## Bekannte Einschränkungen

- Das Baujahr wird aus dem Modulnamen per Regex extrahiert (nicht immer vorhanden).  
- Nicht alle Felder sind in jeder CEC-Dateiversion vorhanden (fehlende Felder = 0).  
- Excel-Export erfordert Write-Zugriff auf den Zielordner.
