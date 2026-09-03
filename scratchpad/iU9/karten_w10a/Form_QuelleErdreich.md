# Feldkarte Form_QuelleErdreich

| Angabe | Wert |
|---|---|
| Maske | Form_QuelleErdreich |
| Datei | `WindowsFormsApplication1/Views/Simulation/Form_QuelleErdreich.Designer.cs` |
| Titel de | Wärmequelle Erdreich |
| Titel en |  |
| ClientSize | 700 x 748 |
| Lokalisiert | nein |
| Zeilen der Karte | 20 |
| Steuerelemente | Label 14, TextBox 5, Button 4, GroupBox 3, ComboBox 2, RadioButton 2 |
| Felder ohne Beschriftung | 0 |
| MessageBox | 4 |
| Aufrufer (ShowDialog) | 1: `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.Uebersicht.cs:1094` |
| Öffner erreichbar | ja — Form_Start → btn_SimKonfig → Form_Simulation_Config → _wqCombo → Form_QuelleErdreich |

## Fenster

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_cbBoden` | ComboBox | Bodentyp: |  | Auswahl | Stil=DropDownList |  | SelectedIndexChanged -> auswahl_SelectedIndexChanged | Auswahlfeld | ☐ |
| 2 | `_lblBodentypHinweis` | Label | (Katalog VDI 4640 Blatt 1, Entwurf 2021-12) |  | Text |  |  |  | Text | ☐ |
| 3 | `_lblBoden` | Label |  |  | Text |  |  |  | Text | ☐ |
| 4 | `_btnKarte` | Button | … |  | Knopf |  |  | Click -> btnKarte_Click | Knopf (pruefen) | ☐ |
| 5 | `_cbZone` | ComboBox | Klimazone: |  | Auswahl | Stil=DropDownList |  | SelectedIndexChanged -> auswahl_SelectedIndexChanged | Auswahlfeld | ☐ |
| 6 | `_lblKlimazoneHinweis` | Label | (DIN 4710, Vorbelegung aus der Klimaregion) |  | Text |  |  |  | Text | ☐ |
| 7 | `_tbSpreizung` | TextBox | Nutzbare Spreizung [K]: |  | Zahl |  |  | TextChanged -> eingabe_TextChanged | Zahlenfeld | ☐ |
| 8 | `_lblSpreizungHinweis` | Label | "(Quelleintritt minus Quellaustritt; Warnung, wenn Quelltemperatur − Spreizung daue" +     "rhaft unter 0 °C liegt)" |  | Text |  |  |  | Text | ☐ |
| 9 | `_btnOk` | Button | OK |  | Knopf |  |  | Click -> btnOk_Click | SpeichernLeiste | ☐ |
| 10 | `_btnAbbruch` | Button | Abbrechen |  | Knopf |  |  |  | SpeichernLeiste | ☐ |

### Quellsystem (`_gbSystem`, GroupBox)

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_tbTiefe` | TextBox | Verlegetiefe [m]: |  | Zahl |  |  | TextChanged -> eingabe_TextChanged | Zahlenfeld | ☐ |
| 2 | `_tbFlaeche` | TextBox | Fläche [m²]: |  | Zahl |  |  | TextChanged -> eingabe_TextChanged | Zahlenfeld | ☐ |
| 3 | `_rbKollektor` | RadioButton | Erdkollektor |  | Auswahl | vorbelegt an |  | CheckedChanged -> rbQuellsystem_CheckedChanged | Auswahlfeld (Gruppe pruefen) | ☐ |
| 4 | `_tbLaenge` | TextBox | Länge je Sonde [m]: |  | Zahl |  |  | TextChanged -> eingabe_TextChanged | Zahlenfeld | ☐ |
| 5 | `_tbAnzahl` | TextBox | Anzahl Sonden: |  | Zahl |  |  | TextChanged -> eingabe_TextChanged | Zahlenfeld | ☐ |
| 6 | `_rbSonde` | RadioButton | Erdsonde |  | Auswahl |  |  | CheckedChanged -> rbQuellsystem_CheckedChanged | Auswahlfeld (Gruppe pruefen) | ☐ |

### Vorschau: Jahresgang der Quelltemperatur (`_gbVorschau`, GroupBox)

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_lblKennwerte` | Label |  |  | Text |  |  |  | Text | ☐ |

### Auslegungsprüfung nach VDI 4640 Blatt 2 (nach der Simulation) (`_gbPruefung`, GroupBox)

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_lblPruefung` | Label |  |  | Text |  |  |  | Text | ☐ |
| 2 | `_btnSimulation` | Button | Simulation |  | Knopf |  |  | Click -> btnSimulation_Click | Knopf (pruefen) | ☐ |
| 3 | `_lblAenderung` | Label |  |  | Text |  |  |  | Text | ☐ |

## Ereignishandler in `Form_QuelleErdreich.cs`

| Handler | Zeile | Umfang |
|---|---|---|
| `rbQuellsystem_CheckedChanged` | 707 | 5 Zeilen |
| `eingabe_TextChanged` | 717 | 4 Zeilen |
| `auswahl_SelectedIndexChanged` | 723 | 4 Zeilen |
| `btnSimulation_Click` | 1014 | 56 Zeilen |
| `btnKarte_Click` | 1079 | 9 Zeilen |
| `btnOk_Click` | 1194 | 71 Zeilen |


