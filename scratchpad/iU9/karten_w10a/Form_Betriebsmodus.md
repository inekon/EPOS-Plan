# Feldkarte Form_Betriebsmodus

| Angabe | Wert |
|---|---|
| Maske | Form_Betriebsmodus |
| Datei | `WindowsFormsApplication1/Views/Simulation/Form_Betriebsmodus.Designer.cs` |
| Titel de | Betriebsmodus - {0} |
| Titel en |  |
| ClientSize | 520 x 300 |
| Lokalisiert | nein |
| Zeilen der Karte | 9 |
| Steuerelemente | Label 4, RadioButton 3, Button 2 |
| Felder ohne Beschriftung | 0 |
| MessageBox | 0 |
| Aufrufer (ShowDialog) | 1: `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.Uebersicht.cs:621` |
| Öffner erreichbar | ja — Form_Start → btn_SimKonfig → Form_Simulation_Config → BetriebsmodusBearbeiten → Form_Betriebsmodus |

## Fenster

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_lblKopf` | Label | Leistungssteuerung der Wärmepumpe: |  | Text |  |  |  | Text | ☐ |
| 2 | `_rbLaufzeit` | RadioButton | Laufzeitoptimiert - maximale Leistung |  | Auswahl |  |  |  | Auswahlfeld (Gruppe pruefen) | ☐ |
| 3 | `_lblLaufzeit` | Label | "Die Wärmepumpe fährt volle Leistung; die über den Bedarf hinaus\r\nerzeugte Wärme l" +     "ädt den Pufferspeicher. Lange Laufzeiten, wenig Takten." |  | Text |  |  |  | Text | ☐ |
| 4 | `_rbLeistung` | RadioButton | Leistungsoptimiert - nur den Bedarf decken |  | Auswahl |  |  |  | Auswahlfeld (Gruppe pruefen) | ☐ |
| 5 | `_lblLeistung` | Label | "Die Wärmepumpe moduliert exakt auf den Wärmebedarf und erzeugt\r\nkeinen Überschuss." +     " Der Speicher wird nicht gezielt beladen." |  | Text |  |  |  | Text | ☐ |
| 6 | `_rbPV` | RadioButton | PV-optimiert - Überschuss nur mit PV-Strom |  | Auswahl |  |  |  | Auswahlfeld (Gruppe pruefen) | ☐ |
| 7 | `_lblPV` | Label | "Bei verfügbarem PV-Strom fährt die Wärmepumpe erhöhte Leistung\r\n(begrenzt auf den " +     "PV-Überschuss) und lädt den Speicher; sonst\r\narbeitet sie leistungsoptimiert." |  | Text |  |  |  | Text | ☐ |
| 8 | `_btnOk` | Button | OK |  | Knopf |  |  |  | SpeichernLeiste | ☐ |
| 9 | `_btnAbbrechen` | Button | Abbrechen |  | Knopf |  |  |  | SpeichernLeiste | ☐ |


