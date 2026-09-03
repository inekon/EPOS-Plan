# Feldkarte Form_QuellePufferspeicher

| Angabe | Wert |
|---|---|
| Maske | Form_QuellePufferspeicher |
| Datei | `WindowsFormsApplication1/Views/Simulation/Form_QuellePufferspeicher.Designer.cs` |
| Titel de | Wärmequelle Pufferspeicher |
| Titel en |  |
| ClientSize | 620 x 508 |
| Lokalisiert | nein |
| Zeilen der Karte | 14 |
| Steuerelemente | Label 9, Button 3, TextBox 3, CheckBox 1, GroupBox 1, ListBox 1 |
| Felder ohne Beschriftung | 1 |
| MessageBox | 5 |
| Aufrufer (ShowDialog) | 1: `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.Uebersicht.cs:892` |
| Öffner erreichbar | ja — Form_Start → btn_SimKonfig → Form_Simulation_Config → _wqCombo → Form_QuellePufferspeicher |

## Fenster

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_lblKopf` | Label | Pufferspeicher als Wärmequelle auswählen: |  | Text |  |  |  | Text | ☐ |
| 2 | `_lbSpeicher` | ListBox |  |  | Auswahl |  |  | SelectedIndexChanged -> lbSpeicher_SelectedIndexChanged | Auswahlfeld | ☐ |
| 3 | `_lblDaten` | Label |  |  | Text |  |  |  | Text | ☐ |
| 4 | `_lblHinweisArt` | Label | "Die Wärmepumpe entzieht dem Speicher je Stunde die Verdampferwärme (Wärmeproduktio" +     "n − Stromaufnahme).\r\n\r\nIst der Speicher leer, wird die Leistung der Wärmepumpe be" +     "grenzt; die Regeneration lädt den Speicher laufend nach." |  | Text |  |  |  | Text | ☐ |
| 5 | `_lblLeer` | Label |  |  | Text |  |  |  | Text | ☐ |
| 6 | `_btnPufferAnlegen` | Button | Pufferspeicher anlegen… |  | Knopf |  |  | Click -> btnPufferAnlegen_Click | Knopf (pruefen) | ☐ |
| 7 | `_lblKaskade` | Label | "Kaskade (Heizkessel): Der Kessel bezieht seine Eintrittstemperatur aus dem gewählt" +     "en Pufferspeicher statt aus dem Systemrücklauf.\r\n\r\nAnteil = (Vorlauf des Puffers " +     "− Rücklauf des Kessels) / (Vorlauf des Kessels − Rücklauf des Kessels)\r\n\r\nUm dies" +     "en Anteil der Nutzwärme sinkt der Brennstoffbedarf; die Entnahme ist zugleich ein" +     "e Entladung des Speichers. Liefert der Puffer weniger, springt Brennstoff für den" +     " Fehlbetrag ein. Der Kessel rechnet nach dem Erzeuger, der den Puffer lädt." |  | Text | verborgen |  |  | Text | ☐ |
| 8 | `_btnOk` | Button | OK |  | Knopf |  |  | Click -> btnOk_Click | SpeichernLeiste | ☐ |
| 9 | `_btnAbbruch` | Button | Abbrechen |  | Knopf |  |  |  | SpeichernLeiste | ☐ |

### Parameter der Wärmequelle (`_gbParameter`, GroupBox)

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_tbTemperatur` | TextBox | Quelltemperatur [°C]: |  | Zahl |  |  | TextChanged -> tbTemperatur_TextChanged | Zahlenfeld | ☐ |
| 2 | `_lblKapazitaet` | Label |  |  | Text |  |  |  | Text | ☐ |
| 3 | `_tbSpreizung` | TextBox | nutzbare Spreizung [K]: |  | Zahl |  |  | TextChanged -> tbSpreizung_TextChanged | Zahlenfeld | ☐ |
| 4 | `_tbRegeneration` | TextBox | Regeneration [kW]: |  | Zahl |  |  |  | Zahlenfeld | ☐ |
| 5 | `_cbUnbegrenzt` | CheckBox | Quelle unbegrenzt verfügbar (nur Temperatur maßgeblich) |  | Schalter |  |  |  | Schalter | ☐ |

## Ereignishandler in `Form_QuellePufferspeicher.cs`

| Handler | Zeile | Umfang |
|---|---|---|
| `lbSpeicher_SelectedIndexChanged` | 892 | 6 Zeilen |
| `tbTemperatur_TextChanged` | 899 | 6 Zeilen |
| `tbSpreizung_TextChanged` | 955 | 4 Zeilen |
| `btnPufferAnlegen_Click` | 966 | 17 Zeilen |
| `btnOk_Click` | 984 | 72 Zeilen |


