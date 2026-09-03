# Feldkarte Form_Klimazonenkarte

| Angabe | Wert |
|---|---|
| Maske | Form_Klimazonenkarte |
| Datei | `WindowsFormsApplication1/Views/Simulation/Form_Klimazonenkarte.Designer.cs` |
| Titel de | Klimazonen nach DIN 4710 |
| Titel en |  |
| ClientSize | 700 x 760 |
| Lokalisiert | nein |
| Zeilen der Karte | 4 |
| Steuerelemente | Button 2, KlimazonenKarte 1, Label 1 |
| Felder ohne Beschriftung | 0 |
| MessageBox | 0 |
| Aufrufer (ShowDialog) | 1: `WindowsFormsApplication1/Views/Simulation/Form_QuelleErdreich.cs:1081` |
| Öffner erreichbar | ja — Form_Start → btn_SimKonfig → Form_Simulation_Config → _wqCombo → Form_QuelleErdreich → _btnKarte → Form_Klimazonenkarte |

## Fenster

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_karte` | sonstig (KlimazonenKarte) |  |  | - |  | 0 | ZoneGewaehlt -> karte_ZoneGewaehlt, ZoneUebernommen -> karte_ZoneUebernommen | pruefen | ☐ |
| 2 | `_btnOk` | Button | OK |  | Knopf |  | 1 |  | SpeichernLeiste | ☐ |
| 3 | `_btnAbbruch` | Button | Abbrechen |  | Knopf |  | 2 |  | SpeichernLeiste | ☐ |
| 4 | `_lblGewaehlt` | Label | Noch keine Zone gewählt — eine Zonenfläche auf der Karte anklicken. |  | Text |  |  |  | Text | ☐ |

## Ereignishandler in `Form_Klimazonenkarte.cs`

| Handler | Zeile | Umfang |
|---|---|---|
| `karte_ZoneGewaehlt` | 84 | 4 Zeilen |
| `karte_ZoneUebernommen` | 90 | 5 Zeilen |


