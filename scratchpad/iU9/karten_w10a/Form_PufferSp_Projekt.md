# Feldkarte Form_PufferSp_Projekt

| Angabe | Wert |
|---|---|
| Maske | Form_PufferSp_Projekt |
| Datei | `WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_Projekt.Designer.cs` |
| Titel de | Pufferspeicher im Projekt |
| Titel en |  |
| ClientSize | 700 x 662 |
| Lokalisiert | nein |
| Zeilen der Karte | 22 |
| Steuerelemente | Label 15, TextBox 9, Button 5, ComboBox 3, GroupBox 3, ListBox 1, ListView 1 |
| Felder ohne Beschriftung | 2 |
| MessageBox | 9 |
| Aufrufer (ShowDialog) | 3: `WindowsFormsApplication1/Views/Simulation/Form_QuellePufferspeicher.cs:968`, `WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.Uebersicht.cs:342`, `WindowsFormsApplication1/Views/Simulation/Form_Waermesenke.cs:1790` |
| Öffner erreichbar | ja — Form_Start → btn_SimKonfig → Form_Simulation_Config → PufferVerwaltungOeffnen → Form_PufferSp_Projekt |

## Fenster

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_cbEntladeprio` | ComboBox | Entladepriorität: |  | Auswahl | Stil=DropDownList |  |  | Auswahlfeld | ☐ |
| 2 | `_lblEntladeInfo` | Label | Wird als {0}. von {1} {2} entladen. |  | Text |  |  |  | Text | ☐ |
| 3 | `_lblStatus` | Label |  |  | Text |  |  |  | Text | ☐ |
| 4 | `_btnUebernehmen` | Button | Übernehmen |  | Knopf |  |  | Click -> btnUebernehmen_Click | SpeichernLeiste | ☐ |
| 5 | `_btnSchliessen` | Button | Schließen |  | Knopf |  |  |  | SpeichernLeiste | ☐ |

### Pufferspeicher im Projekt (`_gbListe`, GroupBox)

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_lbProjekt` | ListBox |  |  | Auswahl |  |  | SelectedIndexChanged -> lbProjekt_SelectedIndexChanged | Auswahlfeld | ☐ |
| 2 | `_btnNeu` | Button | Neuer Pufferspeicher |  | Knopf |  |  | Click -> btnNeu_Click | Knopf (pruefen) | ☐ |
| 3 | `_btnEntfernen` | Button | Entfernen |  | Knopf |  |  | Click -> btnEntfernen_Click | Knopf (pruefen) | ☐ |
| 4 | `_btnKatalog` | Button | Katalog ansehen… |  | Knopf |  |  | Click -> btnKatalog_Click | Knopf (pruefen) | ☐ |

### Eigenschaften (`_gbDaten`, GroupBox)

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_cbKatalog` | ComboBox | Aus Katalog: |  | Auswahl | Stil=DropDownList |  | SelectedIndexChanged -> cbKatalog_SelectedIndexChanged | Auswahlfeld | ☐ |
| 2 | `_tbBezeichner` | TextBox | Bezeichner: |  | Text |  |  |  | Textfeld | ☐ |
| 3 | `_tbVolumen` | TextBox | Gesamtvolumen [l]: |  | Text |  |  | TextChanged -> Kapazitaet_Geaendert | Textfeld | ☐ |
| 4 | `_cbVerwendung` | ComboBox | Verwendung: |  | Auswahl | Stil=DropDownList |  | SelectedIndexChanged -> Daten_Geaendert | Auswahlfeld | ☐ |
| 5 | `_tbVerluste` | TextBox | Bereitschaftsverl. [kWh/24h]: |  | Zahl |  |  |  | Zahlenfeld | ☐ |
| 6 | `_tbVorlauf` | TextBox | Vorlauf [°C]: |  | Text |  |  | TextChanged -> Kapazitaet_Geaendert | Textfeld | ☐ |
| 7 | `_tbRuecklauf` | TextBox | Rücklauf [°C]: |  | Text |  |  | TextChanged -> Kapazitaet_Geaendert | Textfeld | ☐ |
| 8 | `_lblQmax` | Label | →  Q_max {0} kWh |  | Text |  |  |  | Text | ☐ |
| 9 | `_tbSchwelleEin` | TextBox | Einschaltschwelle [%]: |  | Text |  |  |  | Textfeld | ☐ |
| 10 | `_tbSchwelleAus` | TextBox | Abschaltschwelle [%]: |  | Text |  |  |  | Textfeld | ☐ |
| 11 | `_tbSchwelleNachrang` | TextBox | … nachrangig [%]: |  | Text |  |  |  | Textfeld | ☐ |
| 12 | `_tbSchwelleReserve` | TextBox | Mindestfüllstand/Notreserve [%]: |  | Text |  |  |  | Textfeld | ☐ |

### Ladereihenfolge dieses Speichers (aus den Erzeugerzuordnungen) (`_gbLaden`, GroupBox)

| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `_lvLaden` | ListView |  |  | Raster |  |  |  | Raster | ☐ |

## Ereignishandler in `Form_PufferSp_Projekt.cs`

| Handler | Zeile | Umfang |
|---|---|---|
| `Kapazitaet_Geaendert` | 1348 | 5 Zeilen |
| `Daten_Geaendert` | 1354 | 5 Zeilen |
| `lbProjekt_SelectedIndexChanged` | 1552 | 7 Zeilen |
| `btnNeu_Click` | 1560 | 5 Zeilen |
| `cbKatalog_SelectedIndexChanged` | 1566 | 26 Zeilen |
| `btnKatalog_Click` | 1593 | 11 Zeilen |
| `btnUebernehmen_Click` | 1605 | 80 Zeilen |
| `btnEntfernen_Click` | 1819 | 37 Zeilen |


