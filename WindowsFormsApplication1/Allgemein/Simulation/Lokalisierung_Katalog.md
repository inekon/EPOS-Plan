# Ressourcenkatalog Simulationsbereich — Zuordnungstabelle

**Paket 9 „Lokalisierung", Teilpaket L2.** Erzeugt am 15.08.2026.

Diese Tabelle ist die **Arbeitsgrundlage für Etappe 2**: Jede Zeile nennt den Ressourcenschlüssel,
den heutigen deutschen Text, die englische Entsprechung und alle Fundstellen im Code, an denen die
hartkodierte Zeichenkette durch `MyResource.Resource.<Schlüssel>` zu ersetzen ist.

Die Schlüssel liegen in `MyResource/Resource.resx` (neutral = deutsch) und
`MyResource/Resource.en-US.resx` (englisch) und sind über `MyResource/Resource.Designer.cs`
stark typisiert erreichbar. Übersetzungsgrundlage ist
[`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md).

## Lesehinweise

- `\n` steht für einen Zeilenumbruch im Text.
- `{0}`, `{1}` … sind Platzhalter. **Achtung:** Die **Formatangaben** des Quelltexts
  (`{0:N0}`, `{0:0.0}`, `{0:F1}` …) sind in dieser Tabelle auf die bloße Nummer normalisiert.
  Beim Umbau in Etappe 2 ist die Formatangabe aus der jeweiligen Fundstelle zu übernehmen —
  sonst ändert sich die Zahlendarstellung.
- Namensschema: `SIM_*` Simulation allgemein · `SIMQ_*` Wärmequellen · `PSP_*` Pufferspeicher ·
  `SIMENG_*` Engine- und Protokollmeldungen · `CHART_*` Chart-, Achsen-, Legenden- und
  CSV-Beschriftungen.
- **Nicht** in dieser Tabelle: DB-Persistenzwerte (die stehen in
  [`../DbWerte.cs`](../DbWerte.cs) und bleiben deutsch), Monats- und Wochentagsnamen
  (kommen in L3 über `CultureInfo`), reine Einheiten und Symbole, Chart-Serien**namen**, die als
  Zugriffsschlüssel dienen, sowie `Console.WriteLine`- und `Exception`-Texte.

## Nachträge aus Etappe 2 (L3–L6)

Beim Umbau kamen zwei Schlüssel dazu und zwei Einträge wurden berichtigt. Beides ist in
beiden `.resx` und in `Resource.Designer.cs` nachgezogen; Bestand jetzt **530 Schlüssel**.

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `CHART_LEGENDE_GESAMT` | Gesamt | Total | NavigatorWaerme.cs (Serie `GESAMT`) | **neu.** Der Legendentext hing bis L6 am Seriennamen; mit der Umstellung auf technische Schlüssel braucht er einen eigenen Eintrag. |
| `CHART_LEGENDE_WAERMEBEDARF` | Wärmebedarf | Heat demand | NavigatorWaerme.cs (Serie `WAERMEBEDARF`) | **neu.** Wie oben. Nicht zu verwechseln mit `CHART_CSV_WAERMEBEDARF` („Wärmebedarf [kW]", CSV-Kopf). |

| Schlüssel | Berichtigung |
|---|---|
| `SIMENG_STROMPROFILE_DIAGNOSE` | Der Text trägt jetzt **zwei** Platzhalter: `…nicht berechnet werden{0} - {1}`. `{0}` nimmt den optionalen Zusatz `SIMENG_STROMPROFIL_ZULETZT_BEARBEITET` auf, `{1}` die Ausnahmemeldung. Mit nur einem Platzhalter wäre der Zusatz beim Umbau verlorengegangen; die deutsche Ausgabe ist unverändert. |
| `SIM_BHKW_MODUL_STANDARD` | Die Fundstelle **`SimulationRunner.cs:499` ist keine Anzeige**, sondern ein Persistenzwert: `ErgebnisBHKWModulModel.Modul` wird nach `Tab_ErgebnisBHKWModul.Modul` geschrieben und von der Referenzlauf-Suite als Skalar exportiert. Sie bleibt hartkodiert deutsch (Kommentar an der Stelle). Der Schlüssel gilt nur noch für `Form_Simulation_Detail.cs:2010`. |

## Nachträge aus Etappe 2b (Rest-Simulationsbereich)

Beim Umbau von `Form_Simulation_Detail`, den drei Navigatoren, `DashboardForm`,
`Form_Waermesenke`, `Form_QuelleErdreich` und `TabNavigationManager` kamen **elf**
Schlüssel dazu; alle sind in beiden `.resx` und in `Resource.Designer.cs` nachgezogen.
Bestand jetzt **541 Schlüssel**.

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `CHART_LEGENDE_HEIZWAERMEBEDARF` | Heizwärmebedarf | Space heating demand | Form_Simulation_Detail.cs (Serie `HEIZWAERMEBEDARF`, 3×) | **neu.** Der Legendentext hing am Seriennamen; mit der Umstellung auf technische Schlüssel braucht er einen eigenen Eintrag. |
| `CHART_LEGENDE_WARMWASSERBEDARF` | Warmwasserbedarf | DHW demand | Form_Simulation_Detail.cs (Serie `WARMWASSERBEDARF`, 3×) | **neu.** Wie oben. |
| `CHART_LEGENDE_WAERMEPRODUKTION` | Wärmeproduktion | Heat generation | Form_Simulation_Detail.cs (Serie `WAERMEPRODUKTION`, 6×) | **neu.** Wie oben. Nicht zu verwechseln mit `SIM_SPALTE_WAERMEPRODUKTION` („Wärmeprod. [MWh/a]"). |
| `CHART_LEGENDE_UEBERSCHUSS` | Überschuss | Surplus | Form_Simulation_Detail.cs (Serie `UEBERSCHUSS`) | **neu.** Wie oben. |
| `CHART_LEGENDE_PROFIL_LASTGANG` | Profil/Lastgang | Profile/load curve | NavigatorStrom.cs (Serie `PROFIL_LASTGANG`, Checkbox) | **neu.** Wie oben; zugleich Designer-Checkbox. |
| `CHART_TITEL_STROMVERLAUF_JAHRESGANGLINIE` | Stromverlauf Jahresganglinie␠ | Electricity profile, annual load profile␠ | NavigatorStrom.cs (`chart7.Titles[0]`) | **neu.** Entwurfszeit-Titel; **abschließendes Leerzeichen** wie im Bestand, über `xml:space="preserve"` erhalten. |
| `SIM_BTN_WAERMEBEDARF_UEBERSICHT` | Wärmebedarf Übersicht... | Heat demand overview... | NavigatorUebersicht.cs (`bt_WaermebedarfUebersicht`) | **neu.** Designer-Knopf. |
| `SIM_CHK_WAERMEBEDARF_EINBLENDEN` | Wärmebedarf einblenden | Show heat demand | NavigatorWaerme.cs (`checkBox_Waermebedarf`) | **neu.** Designer-Checkbox. |
| `SIM_DASH_GRUPPE_PV` | Photovoltaik Autarkie | Photovoltaic self-sufficiency | DashboardForm.cs (`groupPV`) | **neu.** Designer-Gruppe. |
| `SIM_DASH_GRUPPE_ST` | Solarthermie Deckung | Solar thermal coverage | DashboardForm.cs (`groupST`) | **neu.** Designer-Gruppe. |
| `SIM_DASH_SPEICHER_INFO` | Theoretischer Speicher (PV) (kWh): | Theoretical storage (PV) (kWh): | DashboardForm.cs (`lblSpeicherInfo`) | **neu.** Designer-Label. |

**Mehrfachnutzung bestehender Schlüssel** (der Katalog führt gleiche deutsche Texte unter
einem Schlüssel — Etappe 1, Abschnitt 5.1). Diese Schlüssel haben in Etappe 2b weitere
Fundstellen bekommen:

| Schlüssel | zusätzliche Verwendung |
|---|---|
| `CHART_ACHSE_STROMBEDARF` | Legendentext der Serien `STROMBEDARF` (Form_Simulation_Detail, Diagramme 6 und 9) |
| `PSP_CHECKBOX_SPEICHERFUELLSTAND` | Legendentext der Serie `SPEICHERFUELLSTAND` (Form_Simulation_Detail, Diagramm 9) |
| `CHART_LEGENDE_WAERMEBEDARF` | Legendentext der Serien `WAERMEBEDARF` (Form_Simulation_Detail, Diagramme 4, 8, 10) |
| `CHART_SEGMENT_HEIZSTAB` | Legendentext der Serien `HEIZSTAB` (4×) und Zeile der Ergebnistabelle in NavigatorUebersicht |
| `CHART_LEGENDE_GESAMT`, `SIM_ERZEUGERNAME_*`, `SIM_PHOTOVOLTAIK` | Designer-Checkboxen von NavigatorStrom und NavigatorWaerme |

**Berichtigungen der Fundstellenangaben:**

| Schlüssel | Berichtigung |
|---|---|
| `SIM_ROLLE_HAUPTSENKE`, `SIM_ROLLE_ZWEITSENKE` | Die Fundstellen in `WaermesenkeClass` sind die **Parameter** von `PufferPasst(...)`; sie wandern als Platzhalter `{0}` in `SIM_KEIN_PUFFER_GEWAEHLT`, `SIM_PUFFER_FREMDES_PROJEKT` und `SIM_PUFFER_VERWENDUNG_PASST_NICHT`. |
| `SIM_KEIN_PUFFER_GEWAEHLT`, `SIM_PUFFER_FREMDES_PROJEKT`, `SIM_PUFFER_VERWENDUNG_PASST_NICHT` | Die Verwendungs-Platzhalter werden **vor dem Einsetzen** über `WaermesenkeClass.VerwendungAnzeige(...)` übersetzt. Damit ist der in Etappe 1, Abschnitt 5.5 angemeldete Vorbehalt („die englische Meldung mischt die Sprachen") erledigt. |
| `SIM_BETRIEBSART_WAERMEGEFUEHRT/_STROMGEFUEHRT/_OHNE_EINSPEISUNG` | Diese drei dienen in `Form_Simulation_Detail` als **Suchbegriff** für den Fettdruck im Erklärtext `richTextBox_Info`. Dieser Text liegt in der neutralen Formular-`.resx` und ist **nicht** übersetzt — auf englischer Oberfläche findet die Suche ihn nicht und der Fettdruck entfällt (kein Fehler). Siehe Protokoll, Abschnitt 21, Punkt 2. |

## CHART — 54 Schlüssel

| Schlüssel | DE | EN | Fundstellen |
|---|---|---|---|
| `CHART_ACHSE_ENERGIEBEDARF_DECKUNG` | Energie-Bedarf & Deckung (kWh) | Energy demand & coverage (kWh) | DashboardForm.cs:79 |
| `CHART_ACHSE_JAHRESSTUNDEN` | Jahresstunden | Hours of the year | Form_Simulation_Detail.cs:1635, Form_Simulation_Detail.cs:1659, Form_Simulation_Detail.cs:1874, Form_Simulation_Detail.cs:1965, Form_Simulation_Detail.cs:2113, Form_Simulation_Detail.cs:2144 |
| `CHART_ACHSE_LEISTUNG` | Leistung | Power | Form_Simulation_Detail.cs:1919, NavigatorStrom.cs:109 |
| `CHART_ACHSE_LEISTUNG_SPEICHERINHALT` | Leistung [kW] / Speicherinhalt [kWh] | Power [kW] / storage content [kWh] | NavigatorWaerme.cs:187 |
| `CHART_ACHSE_MONAT` | Monat | Month | Form_Simulation_Detail.cs:2075, DashboardForm.cs:87, Form_QuelleErdreich.cs:280, Form_Quellprofil.cs:340 |
| `CHART_ACHSE_MONATE` | Monate | Months | Form_Simulation_Detail.cs:1918, NavigatorStrom.cs:108, NavigatorWaerme.cs:186 |
| `CHART_ACHSE_QUELLTEMPERATUR` | Quelltemperatur [°C] | Source temperature [°C] | Form_QuelleErdreich.cs:281, Form_Quellprofil.cs:341 |
| `CHART_ACHSE_SPEICHER_KWH` | Speicher [kWh] | Storage [kWh] | Form_Simulation_Detail.cs:2401 |
| `CHART_ACHSE_STROMBEDARF` | Strombedarf | Electricity demand | Form_Simulation_Detail.cs:1660 |
| `CHART_ACHSE_TEMPERATUR` | Temperatur [°C] | Temperature [°C] | Form_Simulation_Detail.cs:1783 |
| `CHART_ACHSE_WAERMEBEDARF_KWH` | Wärmebedarf [kWh] | Heat demand [kWh] | NavigatorWaerme.cs:442 |
| `CHART_ACHSE_WAERMELAST` | Wärmelast | Heat load | Form_Simulation_Detail.cs:1636, Form_Simulation_Detail.cs:1875, Form_Simulation_Detail.cs:1966 |
| `CHART_CSV_BHKW` | BHKW [kW] | CHP [kW] | NavigatorStrom.cs:65, NavigatorWaerme.cs:127 |
| `CHART_CSV_GESAMT` | Gesamt [kW] | Total [kW] | NavigatorStrom.cs:59, NavigatorWaerme.cs:122 |
| `CHART_CSV_HEIZKESSEL` | Heizkessel [kW] | Boiler [kW] | NavigatorStrom.cs:62, NavigatorWaerme.cs:125 |
| `CHART_CSV_HEIZSTAB` | Heizstab [kW] | Immersion heater [kW] | Form_Simulation_Detail.cs:468, NavigatorStrom.cs:61, NavigatorWaerme.cs:124 |
| `CHART_CSV_PROFIL_LASTGANG` | Profil/Lastgang [kW] | Profile/load curve [kW] | NavigatorStrom.cs:63 |
| `CHART_CSV_PV` | PV [kW] | PV [kW] | NavigatorStrom.cs:64 |
| `CHART_CSV_SOLARTHERMIE` | Solarthermie [kW] | Solar thermal [kW] | NavigatorWaerme.cs:126 |
| `CHART_CSV_SPEICHER_ENTLADUNG` | {0} Entladung [kWh] | {0} discharging [kWh] | Form_Simulation_Detail.cs:480 |
| `CHART_CSV_SPEICHER_INHALT` | {0} Speicherinhalt [kWh] | {0} storage content [kWh] | Form_Simulation_Detail.cs:481 |
| `CHART_CSV_SPEICHER_LADUNG` | {0} Ladung [kWh] | {0} charging [kWh] | Form_Simulation_Detail.cs:479 |
| `CHART_CSV_SPEICHERFUELLSTAND` | Speicherfüllstand {0} [kWh] | Storage level {0} [kWh] | NavigatorWaerme.cs:136 |
| `CHART_CSV_STROMBEDARF` | Strombedarf [kW] | Electricity demand [kW] | Form_Simulation_Detail.cs:447 |
| `CHART_CSV_STROMBEDARF_WP` | Strombedarf WP [kW] | Electricity demand HP [kW] | Form_Simulation_Detail.cs:470 |
| `CHART_CSV_WAERMEBEDARF` | Wärmebedarf [kW] | Heat demand [kW] | Form_Simulation_Detail.cs:467 |
| `CHART_CSV_WAERMELAST` | Wärmelast [kW] | Heat load [kW] | Form_Simulation_Detail.cs:445 |
| `CHART_CSV_WAERMEPRODUKTION_WP` | Wärmeproduktion WP [kW] | Heat generation HP [kW] | Form_Simulation_Detail.cs:469 |
| `CHART_CSV_WAERMEPUMPE` | Wärmepumpe [kW] | Heat pump [kW] | NavigatorStrom.cs:60, NavigatorWaerme.cs:123 |
| `CHART_DATEI_ENERGIEBEDARF` | Energiebedarf_Projekt_{0}.csv | Energy_demand_project_{0}.csv | Form_Simulation_Detail.cs:449 |
| `CHART_DATEI_STROMBEDARF` | Strombedarf.csv | Electricity_demand.csv | NavigatorStrom.cs:71 |
| `CHART_DATEI_WAERMEPRODUKTION` | Waermeproduktion.csv | Heat_generation.csv | NavigatorWaerme.cs:140 |
| `CHART_DATEI_WAERMEPUMPE` | Waermepumpe_Projekt_{0}.csv | Heat_pump_project_{0}.csv | Form_Simulation_Detail.cs:484 |
| `CHART_KACHEL_STROMBEDARFSDECKUNG` | Strombedarfsdeckung [%] | Electricity demand coverage [%] | NavigatorUebersicht.cs:219 |
| `CHART_KACHEL_WAERMEBEDARFSDECKUNG` | Wärmebedarfsdeckung [%] | Heat demand coverage [%] | NavigatorUebersicht.cs:201 |
| `CHART_LEGENDE_AUTARKIELUECKE` | Autarkie-Lücke (Netz) | Self-sufficiency gap (grid) | DashboardForm.cs:74 |
| `CHART_LEGENDE_EIGENVERBRAUCH_DIREKT` | Eigenverbrauch (Direkt) | Self-consumption (direct) | DashboardForm.cs:56 |
| `CHART_LEGENDE_EIGENVERBRAUCH_SPEICHER` | Eigenverbrauch (Speicher) | Self-consumption (storage) | DashboardForm.cs:66 |
| `CHART_LEGENDE_GESAMT` | Gesamt | Total | NavigatorWaerme.cs:198 |
| `CHART_LEGENDE_WAERMEBEDARF` | Wärmebedarf | Heat demand | NavigatorWaerme.cs:196 |
| `CHART_LEGENDE_WAERMEBEDARFSDECKUNG` | Wärmebedarfsdeckung | Heat demand coverage | Form_Simulation_Detail.cs:116 |
| `CHART_SEGMENT_HEIZSTAB` | Heizstab | Immersion heater | Form_Simulation_Detail.cs:1423, NavigatorUebersicht.cs:212, NavigatorUebersicht.cs:67 |
| `CHART_SEGMENT_REST` | Rest | Residual | Form_Simulation_Detail.cs:1429 |
| `CHART_SEGMENT_RESTSTROM` | Reststrom | Residual electricity | NavigatorUebersicht.cs:246 |
| `CHART_SEGMENT_RESTWAERME` | Restwärme | Residual heat | NavigatorUebersicht.cs:212 |
| `CHART_SEGMENT_SPITZENKESSEL` | Spitzenkessel | Peak-load boiler | NavigatorUebersicht.cs:212 |
| `CHART_SERIE_AUSSENTEMPERATUR` | Außentemperatur | Outdoor temperature | Form_QuelleErdreich.cs:302 |
| `CHART_SERIE_QUELLTEMPERATUR` | Quelltemperatur | Source temperature | Form_QuelleErdreich.cs:293, Form_Quellprofil.cs:352 |
| `CHART_TITEL_LEISTUNG_UEBER_AUSSENTEMPERATUR` | Leistung über Außentemperatur | Power versus outdoor temperature | Form_Simulation_Detail.cs:1782 |
| `CHART_TITEL_STROMBEDARF_JAHRESGANGLINIE` | Strombedarf Jahresganglinie | Electricity demand, annual load profile | Form_Simulation_Detail.cs:1662 |
| `CHART_TITEL_STROMBEDARF_PV_JAHRESGANGLINIE` | Strombedarf, Photovoltaik Jahresganglinie | Electricity demand, photovoltaics, annual load profile | Form_Simulation_Detail.cs:1921 |
| `CHART_TITEL_STROMBEDARF_STROMVERBRAUCH_JAHRESGANGLINIE` | Strombedarf, Stromverbrauch Jahresganglinie | Electricity demand, electricity consumption, annual load profile | NavigatorStrom.cs:111 |
| `CHART_TITEL_WAERMELAST_JAHRESGANGLINIE` | Wärmelast Jahresganglinie | Heat load, annual load profile | Form_Simulation_Detail.cs:1638, Form_Simulation_Detail.cs:1877, Form_Simulation_Detail.cs:1968 |
| `CHART_TITEL_WAERMEPRODUKTION_JAHRESGANGLINIE` | Wärmeproduktion Jahresganglinie | Heat generation, annual load profile | NavigatorWaerme.cs:189 |

## PSP — 123 Schlüssel

| Schlüssel | DE | EN | Fundstellen |
|---|---|---|---|
| `PSP_ANZEIGE_QMAX` | →  Q_max {0} kWh | →  Q_max {0} kWh | Form_PufferSp_Projekt.cs:528 |
| `PSP_AUSWAHL_ALLE_SPEICHER` | Alle Speicher | All storage units | NavigatorWaerme.cs:241 |
| `PSP_BEZEICHNER_ERSATZ` | Speicher | Storage | SimulationPufferspeicher.cs:668, Form_Simulation_Detail.cs:317 |
| `PSP_BTN_ANLEGEN` | Anlegen | Create | Form_PufferSp_Projekt.cs:421 |
| `PSP_BTN_ENTFERNEN` | Entfernen | Remove | Form_PufferSp_Projekt.cs:119 |
| `PSP_BTN_KATALOG_ANSEHEN` | Katalog ansehen… | View catalogue… | Form_PufferSp_Projekt.cs:123 |
| `PSP_BTN_NEUER_PUFFERSPEICHER` | Neuer Pufferspeicher | New buffer storage | Form_PufferSp_Projekt.cs:115 |
| `PSP_BTN_PUFFER_ANLEGEN` | Pufferspeicher anlegen… | Create buffer storage… | Form_Waermesenke.cs:330 |
| `PSP_BTN_PUFFER_VERWALTEN` | Pufferspeicher anlegen / verwalten… | Create / manage buffer storage… | Form_Simulation_Config.Uebersicht.cs:318 |
| `PSP_BTN_SCHLIESSEN` | Schließen | Close | Form_PufferSp_Projekt.cs:278 |
| `PSP_BTN_UEBERNEHMEN` | Übernehmen | Apply | Form_PufferSp_Projekt.cs:268, Form_PufferSp_Projekt.cs:461 |
| `PSP_CHECKBOX_SPEICHERFUELLSTAND` | Speicherfüllstand | Storage level | NavigatorWaerme.cs:65 |
| `PSP_ENTLADE_POSITION` | Wird als {0}. von {1} {2} entladen. | Discharged as no. {0} of {1} {2}. | Form_PufferSp_Projekt.cs:584 |
| `PSP_FEHLER_BEZEICHNER_FEHLT` | Bitte einen Bezeichner eintragen oder einen Katalogeintrag wählen. | Please enter an identifier or select a catalogue entry. | Form_PufferSp_Projekt.cs:909 |
| `PSP_FEHLER_EIN_KLEINER_AUS` | Die Einschaltschwelle muss kleiner als die Abschaltschwelle sein. | The switch-on threshold must be lower than the switch-off threshold. | Form_PufferSp_Projekt.cs:957 |
| `PSP_FEHLER_NACHRANG_UEBER_AUS` | Die Abschaltschwelle für nachrangige Erzeuger darf die Abschaltschwelle nicht überschreiten - sie ist die Reservezone für den Vorrang (Konzept 3.4). | The switch-off threshold for lower-priority heat generators must not exceed the switch-off threshold - it is the reserve zone for the priority (concept 3.4). | Form_PufferSp_Projekt.cs:963 |
| `PSP_FEHLER_NACHRANG_UNTER_EIN` | Die Abschaltschwelle für nachrangige Erzeuger muss über der Einschaltschwelle liegen. | The switch-off threshold for lower-priority heat generators must be above the switch-on threshold. | Form_PufferSp_Projekt.cs:970 |
| `PSP_FEHLER_SCHWELLE_BEREICH` | Die {0} muss zwischen 0 und 100 % liegen. | The {0} must be between 0 and 100 %. | Form_PufferSp_Projekt.cs:992 |
| `PSP_FEHLER_SCHWELLE_ZAHL` | Die {0} muss eine Zahl sein [%]. | The {0} must be a number [%]. | Form_PufferSp_Projekt.cs:986 |
| `PSP_FEHLER_VERLUSTE` | Die Bereitschaftsverluste müssen eine Zahl ≥ 0 sein [kWh/24h]. | The standby losses must be a number ≥ 0 [kWh/24h]. | Form_PufferSp_Projekt.cs:931 |
| `PSP_FEHLER_VERWENDUNG_PFLICHT` | Die Verwendung ist ein Pflichtfeld: Heizung oder Brauchwasser (Konzept 5.1). | The use is a mandatory field: heating or domestic hot water (concept 5.1). | Form_PufferSp_Projekt.cs:915 |
| `PSP_FEHLER_VOLUMEN` | Bitte ein Gesamtvolumen in Litern eintragen (ganze Zahl größer 0). | Please enter a total volume in litres (whole number greater than 0). | Form_PufferSp_Projekt.cs:922 |
| `PSP_FILTER_100_BIS_200L` | >100 bis 200 l | >100 to 200 l | Form_PufferSp.cs:67, Form_PufferSp.cs:190, Form_PufferSp_Admin.cs:37, Form_PufferSp_Admin.cs:66 |
| `PSP_FILTER_200_BIS_500L` | >200 bis 500 l | >200 to 500 l | Form_PufferSp.cs:68, Form_PufferSp.cs:191, Form_PufferSp_Admin.cs:38, Form_PufferSp_Admin.cs:67 |
| `PSP_FILTER_500_BIS_1000L` | >500 bis 1.000 l | >500 to 1.000 l | Form_PufferSp.cs:69, Form_PufferSp.cs:192, Form_PufferSp_Admin.cs:39, Form_PufferSp_Admin.cs:68 |
| `PSP_FILTER_ALLE` | Alle | All | Form_PufferSp.cs:65, Form_PufferSp.cs:71, Form_PufferSp.cs:72, Form_PufferSp.cs:188, Form_PufferSp.cs:195, Form_PufferSp_Admin.cs:35, Form_PufferSp_Admin.cs:41, Form_PufferSp_Admin.cs:42, Form_PufferSp_Admin.cs:64, Form_PufferSp_Admin.cs:71 |
| `PSP_FILTER_BIS_100L` | bis 100 l | Up to 100 l | Form_PufferSp.cs:66, Form_PufferSp.cs:189, Form_PufferSp_Admin.cs:36, Form_PufferSp_Admin.cs:65 |
| `PSP_FILTER_UEBER_1000L` | über 1.000 l | Over 1.000 l | Form_PufferSp.cs:70, Form_PufferSp.cs:193, Form_PufferSp_Admin.cs:40, Form_PufferSp_Admin.cs:69 |
| `PSP_FUSSZEILE_KEINER` | Pufferspeicher im Projekt: keiner angelegt | Buffer storage in the project: none created | Form_Simulation_Config.Uebersicht.cs:594 |
| `PSP_FUSSZEILE_LISTE` | Pufferspeicher im Projekt:  {0} | Buffer storage in the project:  {0} | Form_Simulation_Config.Uebersicht.cs:603 |
| `PSP_FUSSZEILE_OHNE_PROJEKT` | Pufferspeicher im Projekt: - | Buffer storage in the project: - | Form_Simulation_Config.Uebersicht.cs:587 |
| `PSP_GRUPPE_EIGENSCHAFTEN` | Eigenschaften | Properties | Form_PufferSp_Projekt.cs:130 |
| `PSP_GRUPPE_LADEREIHENFOLGE` | Ladereihenfolge dieses Speichers (aus den Erzeugerzuordnungen) | Charging order of this storage (from the generator assignments) | Form_PufferSp_Projekt.cs:207 |
| `PSP_KANALWORT_BRAUCHWASSERSPEICHER` | Brauchwasserspeicher | DHW storage unit | Form_PufferSp_Projekt.cs:600 |
| `PSP_KANALWORT_BRAUCHWASSERSPEICHER_PLURAL` | Brauchwasserspeichern | DHW storage units | Form_PufferSp_Projekt.cs:603 |
| `PSP_KANALWORT_HEIZUNGSSPEICHER` | Heizungsspeicher | heating storage unit | Form_PufferSp_Projekt.cs:601 |
| `PSP_KANALWORT_HEIZUNGSSPEICHER_PLURAL` | Heizungsspeichern | heating storage units | Form_PufferSp_Projekt.cs:603 |
| `PSP_KATALOG_FREIE_EINGABE` | (freie Eingabe) | (free entry) | Form_PufferSp_Projekt.cs:358 |
| `PSP_LABEL_ABSCHALTSCHWELLE` | Abschaltschwelle [%]: | Switch-off threshold [%]: | Form_PufferSp_Projekt.cs:196 |
| `PSP_LABEL_AUS_KATALOG` | Aus Katalog: | From catalogue: | Form_PufferSp_Projekt.cs:136 |
| `PSP_LABEL_BEREITSCHAFTSVERLUSTE` | Bereitschaftsverl. [kWh/24h]: | Standby losses [kWh/24h]: | Form_PufferSp_Projekt.cs:169 |
| `PSP_LABEL_BEZEICHNER` | Bezeichner: | Identifier: | Form_PufferSp_Projekt.cs:146 |
| `PSP_LABEL_EINSCHALTSCHWELLE` | Einschaltschwelle [%]: | Switch-on threshold [%]: | Form_PufferSp_Projekt.cs:192 |
| `PSP_LABEL_ENTLADEPRIORITAET` | Entladepriorität: | Discharging priority: | Form_PufferSp_Projekt.cs:234 |
| `PSP_LABEL_GESAMTVOLUMEN` | Gesamtvolumen [l]: | Total volume [l]: | Form_PufferSp_Projekt.cs:164 |
| `PSP_LABEL_RUECKLAUF` | Rücklauf [°C]: | Return [°C]: | Form_PufferSp_Projekt.cs:178 |
| `PSP_LABEL_SCHWELLE_NACHRANGIG` | … nachrangig [%]: | … lower priority [%]: | Form_PufferSp_Projekt.cs:200 |
| `PSP_LABEL_VERWENDUNG` | Verwendung: | Use: | Form_PufferSp_Projekt.cs:150 |
| `PSP_LABEL_VOLUMEN_PENDELSPEICHER` | Volumen Pendelspeicher [l] | Buffer storage volume [l] | Form_Simulation_Detail.cs:2623 |
| `PSP_LABEL_VORLAUF` | Vorlauf [°C]: | Flow [°C]: | Form_PufferSp_Projekt.cs:173 |
| `PSP_LADEN_KEINE_ANLAGE` | (keine Anlage lädt diesen Speicher) | (no unit charges this storage) | Form_PufferSp_Projekt.cs:545 |
| `PSP_LADEN_NOCH_NICHT_ANGELEGT` | (der Speicher ist noch nicht angelegt) | (the storage has not been created yet) | Form_PufferSp_Projekt.cs:537 |
| `PSP_LADEPRIO_MANUELL` | {0} (manuell) | {0} (manual) | Form_PufferSp_Projekt.cs:558 |
| `PSP_LISTE_EINTRAG` | {0}  -  {1}, {2} l | {0}  -  {1}, {2} l | Form_PufferSp_Projekt.cs:372 |
| `PSP_LISTE_VERWENDUNG_FEHLT` |   (Verwendung nicht gepflegt) |   (use not specified) | Form_PufferSp_Projekt.cs:374 |
| `PSP_MELDUNG_AENDERN_FEHLGESCHLAGEN` | Der Pufferspeicher konnte nicht geändert werden. | The buffer storage could not be changed. | Form_PufferSp_Projekt.cs:736 |
| `PSP_MELDUNG_ANLEGEN_FEHLGESCHLAGEN` | Der Pufferspeicher konnte nicht angelegt werden. | The buffer storage could not be created. | Form_PufferSp_Projekt.cs:715 |
| `PSP_MELDUNG_BEZEICHNER_UNGUELTIG` | Bitte einen gültigen Bezeichner eingeben! | Please enter a valid identifier! | Form_PufferSp_Bearbeiten.cs:99 |
| `PSP_MELDUNG_DATEN_BEREITS_EINGELESEN` | Daten bereits eingelesen! | Data already imported! | Form_PufferSp_einlesen.cs:101 |
| `PSP_MELDUNG_DATENSATZ_GESPEICHERT` | Datensatz gespeichert | Record saved | Form_PufferSp_Bearbeiten.cs:117, Form_PufferSp_Bearbeiten.cs:164, Form_PufferSp_Bearbeiten.cs:195, Form_PufferSp_einlesen.cs:108 |
| `PSP_MELDUNG_ENTFERNEN_BESTAETIGEN` | Den Pufferspeicher „{0}" aus dem Projekt entfernen?\nDie Anlagenzeile im Projektbaum wird mit entfernt. | Remove the buffer storage "{0}" from the project?\nThe unit row in the project tree will be removed as well. | Form_PufferSp_Projekt.cs:814 |
| `PSP_MELDUNG_ENTFERNEN_BLOCKIERT` | Der Pufferspeicher „{0}" kann nicht entfernt werden - er ist noch zugeordnet:\n\n  • {1}\n\nBitte zuerst die Wärmequelle bzw. Wärmesenke dieser Anlagen ändern. | The buffer storage "{0}" cannot be removed - it is still assigned:\n\n  • {1}\n\nPlease change the heat source or heat sink of these units first. | Form_PufferSp_Projekt.cs:804 |
| `PSP_MELDUNG_ENTFERNEN_FEHLGESCHLAGEN` | Der Pufferspeicher konnte nicht entfernt werden. | The buffer storage could not be removed. | Form_PufferSp_Projekt.cs:822 |
| `PSP_MELDUNG_FEHLER_AUFGETRETEN` | Ein Fehler ist aufgetreten: {0} | An error has occurred: {0} | Form_PufferSp_Bearbeiten.cs:129, Form_PufferSp_Bearbeiten.cs:176, Form_PufferSp_Bearbeiten.cs:206, Form_PufferSp_einlesen.cs:120 |
| `PSP_MELDUNG_KATALOG_LOESCHEN` | Der Pufferspeicher '{0}' wird aus dem Katalog\n(Stammdaten) gelöscht und steht danach in keinem Projekt mehr zur Auswahl.\n\nWirklich aus den Stammdaten löschen? | The buffer storage '{0}' will be deleted from the catalogue\n(master data) and will then no longer be available for selection in any project.\n\nReally delete it from the master data? | Form_PufferSp.cs:237 |
| `PSP_MELDUNG_MODUL_WAEHLEN` | Bitte ein Modul auswählen! | Please select a module! | Form_PufferSp.cs:231 |
| `PSP_MELDUNG_NAME_EXISTIERT` | Name existiert bereits! | Name already exists! | Form_PufferSp_Admin.cs:194, Form_PufferSp_Bearbeiten.cs:106, Form_PufferSp_Bearbeiten.cs:159 |
| `PSP_MELDUNG_PUFFER_SELEKTIEREN` | Bitte einen Pufferspeicher selektieren! | Please select a buffer storage! | Form_PufferSp_einlesen.cs:92 |
| `PSP_MELDUNG_SPEICHERN_FEHLER` | Fehler beim Speichern des Datensatzes! | Error saving the record! | Form_PufferSp_Bearbeiten.cs:122, Form_PufferSp_Bearbeiten.cs:169, Form_PufferSp_einlesen.cs:113 |
| `PSP_MELDUNG_VERWENDUNGSWECHSEL` | Die Verwendung des Pufferspeichers „{0}" wird von „{1}" auf „{2}" umgestellt.\n\nDer Speicher ist zugeordnet:\n  • {3}\n\nDiese Zuordnungen passen danach nicht mehr zur Verwendung und müssen im Wärmesenken-Dialog neu gesetzt werden.\nVerwendung trotzdem ändern? | The use of the buffer storage "{0}" is being changed from "{1}" to "{2}".\n\nThe storage is assigned to:\n  • {3}\n\nThese assignments will then no longer match the use and must be set again in the heat sink dialogue.\nChange the use anyway? | Form_PufferSp_Projekt.cs:782 |
| `PSP_MELDUNG_WIRKLICH_LOESCHEN` | Soll {0} wirklich gelöscht werden ? | Really delete {0} ? | Form_PufferSp_Admin.cs:98 |
| `PSP_MSG_SCHWELLEN_BEREICH` | Die Werte müssen zwischen 0 und 100 % liegen und\ndie Einschaltschwelle muss kleiner als die Abschaltschwelle sein! | The values must be between 0 and 100 % and\nthe switch-on threshold must be smaller than the switch-off threshold! | Form_Simulation_Config.cs:309 |
| `PSP_MSG_WP_OHNE_SPEICHER` | Der Wärmepumpe ist kein Pufferspeicher zugeordnet.\nDie Zuordnung erfolgt in der Tabelle 'Pufferspeicher Zuordnung'. | No buffer storage is assigned to the heat pump.\nThe assignment is made in the 'Buffer storage assignment' table. | Form_Simulation_Config.cs:233 |
| `PSP_MSG_ZAHLENWERTE` | Bitte gültige Zahlenwerte eintragen! | Please enter valid numeric values! | Form_Simulation_Config.cs:303, Form_QuellePufferspeicher.cs:266 |
| `PSP_NAME_ABSCHALTSCHWELLE` | Abschaltschwelle | switch-off threshold | Form_PufferSp_Projekt.cs:951 |
| `PSP_NAME_ABSCHALTSCHWELLE_NACHRANG` | Abschaltschwelle für nachrangige Erzeuger | switch-off threshold for lower-priority heat generators | Form_PufferSp_Projekt.cs:952 |
| `PSP_NAME_EINSCHALTSCHWELLE` | Einschaltschwelle | switch-on threshold | Form_PufferSp_Projekt.cs:950 |
| `PSP_OBERGRENZE_EIGEN` | {0} % (eigene) | {0} % (own) | Form_PufferSp_Projekt.cs:559 |
| `PSP_PRIO_AUTOMATISCH` | automatisch | automatic | Form_PufferSp_Projekt.cs:381 |
| `PSP_PRIO_AUTOMATISCH_WERT` | automatisch ({0}) | automatic ({0}) | Form_PufferSp_Projekt.cs:614 |
| `PSP_PROJEKT_FENSTERTITEL` | Pufferspeicher im Projekt | Buffer storage in the project | Form_PufferSp_Projekt.cs:95, Form_PufferSp_Projekt.cs:105 |
| `PSP_ROLLE_QUELLSPEICHER` | Quellspeicher | Source storage | SimulationPufferspeicher.cs:657 |
| `PSP_ROLLE_SENKENSPEICHER` | Senkenspeicher | Sink storage | SimulationPufferspeicher.cs:657 |
| `PSP_RUBRIK_LABEL` | Pufferspeicher: | Buffer storage: | Form_Simulation_Config.cs:389, Form_Waermesenke.cs:276 |
| `PSP_SPALTE_ENTLADUNG` | Entladung [kWh/a] | Discharging [kWh/a] | Form_Simulation_Detail.cs:321 |
| `PSP_SPALTE_FUELLSTAND_ENDE` | Füllstand Ende [kWh] | Storage level at end [kWh] | Form_Simulation_Detail.cs:324 |
| `PSP_SPALTE_KAPAZITAET` | Kapazität [kWh] | Capacity [kWh] | Form_Simulation_Detail.cs:319 |
| `PSP_SPALTE_LADEPRIO` | Ladeprio | Charging prio | Form_PufferSp_Projekt.cs:227 |
| `PSP_SPALTE_LADUNG` | Ladung [kWh/a] | Charging [kWh/a] | Form_Simulation_Detail.cs:320 |
| `PSP_SPALTE_LAEDT_BIS` | lädt bis | Charges up to | Form_PufferSp_Projekt.cs:228 |
| `PSP_SPALTE_ROLLE` | Rolle | Role | Form_Simulation_Detail.cs:318 |
| `PSP_SPALTE_RUECKLAUF` | Rücklauf [°C] | Return [°C] | Form_Simulation_Config.cs:57 |
| `PSP_SPALTE_VERLUSTE` | Verluste [kWh/a] | Losses [kWh/a] | Form_Simulation_Detail.cs:322 |
| `PSP_SPALTE_VOLLZYKLEN` | Vollzyklen | Full cycles | Form_Simulation_Detail.cs:323 |
| `PSP_SPALTE_VORLAUF` | Vorlauf [°C] | Flow [°C] | Form_Simulation_Config.cs:56 |
| `PSP_SPALTE_WAERMEERZEUGER` | Wärmeerzeuger | Heat generator | Form_Simulation_Config.cs:54 |
| `PSP_SPALTE_ZUORDNUNG_ALT` | Zuordnung (alt) | Assignment (old) | Form_Simulation_Config.Uebersicht.cs:227 |
| `PSP_SPEICHERREGELUNG_ABSCHALT` | Abschaltschwelle [% der Kapazität]: | Switch-off threshold [% of capacity]: | Form_Simulation_Config.cs:268 |
| `PSP_SPEICHERREGELUNG_EINSCHALT` | Einschaltschwelle [% der Kapazität]: | Switch-on threshold [% of capacity]: | Form_Simulation_Config.cs:265 |
| `PSP_SPEICHERREGELUNG_FENSTERTITEL` | Speicherregelung - {0} | Storage control - {0} | Form_Simulation_Config.cs:250 |
| `PSP_SPEICHERREGELUNG_HINWEIS` | Unterschreitet der Speicherfüllstand die Einschaltschwelle, läuft die Wärmepumpe an und lädt bis zur Abschaltschwelle durch. Dazwischen bleibt sie aus und der Bedarf wird aus dem Speicher gedeckt.\n\nDie Abschaltschwelle sollte unter 100 % liegen, da die Bereitschaftsverluste den Füllstand laufend absenken. | If the storage charge level falls below the switch-on threshold, the heat pump starts up and charges through to the switch-off threshold. In between it stays off and the demand is covered from the storage.\n\nThe switch-off threshold should be below 100 %, because the standby losses continuously lower the charge level. | Form_Simulation_Config.cs:276 |
| `PSP_SPEICHERREGELUNG_KOPF` | Ein- und Abschaltschwelle des Pufferspeichers | Switch-on and switch-off threshold of the buffer storage | Form_Simulation_Config.cs:259 |
| `PSP_STATUS_AENDERUNGEN_UEBERNOMMEN` | Änderungen übernommen. | Changes applied. | Form_PufferSp_Projekt.cs:743 |
| `PSP_STATUS_ANGELEGT` | Pufferspeicher angelegt. | Buffer storage created. | Form_PufferSp_Projekt.cs:723 |
| `PSP_STATUS_ENTFERNT` | Pufferspeicher entfernt. | Buffer storage removed. | Form_PufferSp_Projekt.cs:828 |
| `PSP_STATUS_SPEICHERREGELUNG_GESPEICHERT` | ✔ Speicherregelung gespeichert ({0} % / {1} %) | ✔ Storage control saved ({0} % / {1} %) | Form_Simulation_Config.cs:320 |
| `PSP_STATUS_ZUORDNUNG_FEHLGESCHLAGEN` | ⚠ {0} Pufferspeicher-Zuordnung(en) konnten nicht gespeichert werden | ⚠ {0} buffer storage assignment(s) could not be saved | Form_Simulation_Config.cs:1113 |
| `PSP_TIP_ZUORDNUNG_ALTMODELL` | Zuordnung im Altmodell (Doppelklick öffnet die Speicherregelung)\nDiese Spalte zeigt die Zuordnung aus Z_ProjektPufferSp, die die\nSimulation bis zur Umstellung der Engine noch auswertet. Sie wird\naus der Wärmesenke der Wärmepumpe automatisch nachgeführt.\nEin- und Abschaltschwelle in % der nutzbaren Kapazität. | Assignment in the old model (double-click opens the storage control)\nThis column shows the assignment from Z_ProjektPufferSp, which the\nsimulation still evaluates until the engine is converted. It is\nupdated automatically from the heat sink of the heat pump.\nSwitch-on and switch-off threshold in % of the usable capacity. | Form_Simulation_Config.Uebersicht.cs:1100 |
| `PSP_TIP_ZUORDNUNG_ERZEUGER` | Wärmeerzeuger, dem dieser Pufferspeicher zugeordnet ist.\nZuordnungen werden über 'Hinzufügen...' angelegt und über\n'Löschen' entfernt. | Heat generator to which this buffer storage is assigned.\nAssignments are created via 'Add...' and removed via\n'Delete'. | Form_Simulation_Config.cs:184 |
| `PSP_TIP_ZUORDNUNG_RUECKLAUF` | Rücklauftemperatur [°C] (Doppelklick zum Ändern)\nUntere Temperatur des Speichers. Je größer die Spreizung zum\nVorlauf, desto mehr Energie kann der Speicher aufnehmen. | Return temperature [°C] (double-click to change)\nLower temperature of the storage. The larger the temperature spread to\nthe flow, the more energy the storage can take up. | Form_Simulation_Config.cs:203 |
| `PSP_TIP_ZUORDNUNG_SPEICHER` | Pufferspeicher (Doppelklick zum Ändern)\nAuswahl aus den Stammdaten. Volumen und Bereitschaftsverluste\nstammen aus dem Speicher-Datensatz und bestimmen zusammen mit\nVor- und Rücklauf die nutzbare Kapazität. | Buffer storage (double-click to change)\nSelection from the master data. Volume and standby losses\ncome from the storage record and, together with\nflow and return, determine the usable capacity. | Form_Simulation_Config.cs:190 |
| `PSP_TIP_ZUORDNUNG_STAMMDATEN` | Doppelklick öffnet die Pufferspeicher-Stammdaten (nur Ansicht). | Double-click opens the buffer storage master data (view only). | Form_Simulation_Config.cs:209 |
| `PSP_TIP_ZUORDNUNG_STANDARD` | Pufferspeicher-Zuordnung: Doppelklick auf Pufferspeicher,\nVorlauf oder Rücklauf zum Bearbeiten. | Buffer storage assignment: double-click on buffer storage,\nflow or return to edit. | Form_Simulation_Config.cs:213 |
| `PSP_TIP_ZUORDNUNG_VORLAUF` | Vorlauftemperatur [°C] (Doppelklick zum Ändern)\nObere Temperatur des Speichers. Die nutzbare Kapazität ergibt\nsich aus: Volumen × 1,16 Wh/(l·K) × (Vorlauf − Rücklauf). | Flow temperature [°C] (double-click to change)\nUpper temperature of the storage. The usable capacity results\nfrom: volume × 1,16 Wh/(l·K) × (flow − return). | Form_Simulation_Config.cs:197 |
| `PSP_TITEL_KATALOG_LOESCHUNG` | Katalog-Löschung | Catalogue deletion | Form_PufferSp.cs:240 |
| `PSP_TITEL_LOESCHEN` | Löschen | Delete | Form_PufferSp_Admin.cs:98 |
| `PSP_TITEL_PUFFER_ENTFERNEN` | Pufferspeicher entfernen | Remove buffer storage | Form_PufferSp_Projekt.cs:809, Form_PufferSp_Projekt.cs:817 |
| `PSP_TITEL_SPEICHERREGELUNG` | Speicherregelung | Storage control | Form_Simulation_Config.cs:235, Form_Simulation_Config.cs:303, Form_Simulation_Config.cs:311 |
| `PSP_TITEL_TEMPERATUR_PRUEFEN` | Temperatur prüfen | Check temperature | Form_Simulation_Config.cs:667 |
| `PSP_TITEL_VERWENDUNG_AENDERN` | Verwendung ändern | Change use | Form_PufferSp_Projekt.cs:791 |
| `PSP_TITEL_ZUORDNUNG` | Pufferspeicher-Zuordnung | Buffer storage assignment | Form_KonfigPufferspeicher.cs:50 |
| `PSP_VERWENDUNG_BRAUCHWASSER_ANZEIGE` | Brauchwasser | Domestic hot water | Form_PufferSp_Projekt.cs:159 |
| `PSP_VERWENDUNG_HEIZUNG_ANZEIGE` | Heizung | Heating | Form_PufferSp_Projekt.cs:159 |

## SIM — 169 Schlüssel

| Schlüssel | DE | EN | Fundstellen |
|---|---|---|---|
| `SIM_ANZEIGE_CO2_ERSPARNIS` | {0} kg CO2 / Jahr gespart | {0} kg CO2 / year saved | DashboardForm.cs:152 |
| `SIM_ANZEIGE_NICHT_BENOETIGT` | nicht benötigt | not required | DashboardForm.cs:148 |
| `SIM_ANZEIGE_SPEICHERNUTZEN` | Speichernutzen: {0} kWh/Jahr | Storage benefit: {0} kWh/year | DashboardForm.cs:158 |
| `SIM_ANZEIGE_THERM_NUTZUNGSGRAD` | Therm. Nutzungsgrad: {0} % | Therm. utilisation ratio: {0} % | DashboardForm.cs:151 |
| `SIM_BEDARF_BEIDES` | Beides (Warmwasser zuerst) | Both (domestic hot water first) | Form_Waermesenke.cs:139 |
| `SIM_BEDARF_HEIZWAERME` | nur Heizwärme | space heating only | Form_Waermesenke.cs:139 |
| `SIM_BEDARF_WARMWASSER` | nur Warmwasser | domestic hot water only | Form_Waermesenke.cs:139 |
| `SIM_BETRIEBSART_OHNE_EINSPEISUNG` | Ohne Einspeisung (Zero-Export) | Without feed-in (zero export) | Form_Simulation_Detail.cs:1007 |
| `SIM_BETRIEBSART_STROMGEFUEHRT` | Stromgeführt (Wirtschaftlich) | Electricity-led (economic) | Form_Simulation_Detail.cs:1006 |
| `SIM_BETRIEBSART_WAERMEGEFUEHRT` | Wärmegeführt (Standard) | Heat-led (standard) | Form_Simulation_Detail.cs:1005 |
| `SIM_BETRIEBSMODUS_FENSTERTITEL` | Betriebsmodus - {0} | Operating mode - {0} | Form_Simulation_Config.Uebersicht.cs:929 |
| `SIM_BETRIEBSMODUS_KOPF` | Leistungssteuerung der Wärmepumpe: | Output control of the heat pump: | Form_Simulation_Config.Uebersicht.cs:938 |
| `SIM_BHKW_MODUL_STANDARD` | Standard BHKW | Standard CHP unit | SimulationRunner.cs:499, Form_Simulation_Detail.cs:2010 |
| `SIM_BM_RB_LAUFZEIT` | Laufzeitoptimiert - maximale Leistung | Runtime-optimised - maximum output | Form_Simulation_Config.Uebersicht.cs:946 |
| `SIM_BM_RB_LEISTUNG` | Leistungsoptimiert - nur den Bedarf decken | Output-optimised - cover the demand only | Form_Simulation_Config.Uebersicht.cs:961 |
| `SIM_BM_RB_PV` | PV-optimiert - Überschuss nur mit PV-Strom | PV-optimised - surplus only with PV electricity | Form_Simulation_Config.Uebersicht.cs:976 |
| `SIM_BM_TEXT_LAUFZEIT` | Die Wärmepumpe fährt volle Leistung; die über den Bedarf hinaus\nerzeugte Wärme lädt den Pufferspeicher. Lange Laufzeiten, wenig Takten. | The heat pump runs at full output; the heat generated beyond the\ndemand charges the buffer storage. Long runtimes, few starts. | Form_Simulation_Config.Uebersicht.cs:952 |
| `SIM_BM_TEXT_LEISTUNG` | Die Wärmepumpe moduliert exakt auf den Wärmebedarf und erzeugt\nkeinen Überschuss. Der Speicher wird nicht gezielt beladen. | The heat pump modulates exactly to the heat demand and generates\nno surplus. The storage is not charged deliberately. | Form_Simulation_Config.Uebersicht.cs:967 |
| `SIM_BM_TEXT_PV` | Bei verfügbarem PV-Strom fährt die Wärmepumpe erhöhte Leistung\n(begrenzt auf den PV-Überschuss) und lädt den Speicher; sonst\narbeitet sie leistungsoptimiert. | With available PV electricity the heat pump runs at increased output\n(limited to the PV surplus) and charges the storage; otherwise\nit works output-optimised. | Form_Simulation_Config.Uebersicht.cs:982 |
| `SIM_BTN_ABBRECHEN` | Abbrechen | Cancel | Form_Simulation_Config.cs:284, Form_Simulation_Config.Uebersicht.cs:998, Form_Simulation_Config.Uebersicht.cs:1503, Form_Waermesenke.cs:354, Form_QuelleErdreich.cs:354, Form_Quellprofil.cs:170, Form_QuellePufferspeicher.cs:155 |
| `SIM_BTN_CSV_EXPORT` | CSV Export | CSV export | Form_Simulation_Detail.cs:254, Form_Simulation_Detail.cs:272, Form_Simulation_Detail.cs:440, Form_Simulation_Detail.cs:462, NavigatorStrom.cs:35, NavigatorStrom.cs:53, NavigatorWaerme.cs:96, NavigatorWaerme.cs:116 |
| `SIM_BTN_OK` | OK | OK | Form_Simulation_Config.cs:283, Form_Simulation_Config.Uebersicht.cs:997, Form_Simulation_Config.Uebersicht.cs:1502, Form_Waermesenke.cs:347, Form_QuelleErdreich.cs:347, Form_Quellprofil.cs:163, Form_QuellePufferspeicher.cs:148 |
| `SIM_CHK_LADEGRENZE` | eigene Ladeobergrenze: | own charging upper limit: | Form_Waermesenke.cs:217 |
| `SIM_CHK_LADEGRENZE2` | Ladeobergrenze: | Charging upper limit: | Form_Waermesenke.cs:299 |
| `SIM_CHK_ZWEITSENKE` | Zweitsenke (nimmt nur Überschuss bzw. verbleibendes Ladepotenzial auf) | Secondary sink (takes only surplus or remaining charging potential) | Form_Waermesenke.cs:251 |
| `SIM_ENTLADEEINTRAG_AUTOMATISCH` | {0} (Prio {1}, automatisch) | {0} (prio {1}, automatic) | Ladeordnung.cs:497 |
| `SIM_ENTLADEEINTRAG_MANUELL` | {0} (Prio {1}, manuell) | {0} (prio {1}, manual) | Ladeordnung.cs:497 |
| `SIM_ERGEBNIS` | Ergebnis | Result | Form_Simulation_Detail.cs:673, Form_Simulation_Detail.cs:1397 |
| `SIM_ERZEUGERNAME_ALLGEMEIN` | Erzeuger | Heat generator | Ladeordnung.cs:110, Form_Simulation_Config.Uebersicht.cs:220, Form_PufferSp_Projekt.cs:225 |
| `SIM_ERZEUGERNAME_BHKW` | BHKW | CHP unit | Ladeordnung.cs:109, Form_Simulation_Detail.cs:156, Form_Simulation_Detail.cs:641, NavigatorUebersicht.cs:70, Form_Simulation_Detail.cs:1427, NavigatorUebersicht.cs:212, NavigatorUebersicht.cs:246 |
| `SIM_ERZEUGERNAME_HEIZKESSEL` | Heizkessel | Boiler | Ladeordnung.cs:108, Form_Simulation_Detail.cs:123, Form_Simulation_Detail.cs:634, Form_Simulation_Detail.cs:1425 |
| `SIM_ERZEUGERNAME_SOLARTHERMIE` | Solarthermie | Solar thermal | Ladeordnung.cs:107, Form_Simulation_Detail.cs:648, NavigatorUebersicht.cs:212 |
| `SIM_ERZEUGERNAME_WAERMEPUMPE` | Wärmepumpe | Heat pump | Ladeordnung.cs:106, Form_Simulation_Detail.cs:627, NavigatorUebersicht.cs:66, Form_Simulation_Detail.cs:1421, NavigatorUebersicht.cs:212 |
| `SIM_EXTRAPOLATION_SCHALTER` | Extrapolation der WP-Kennlinie erlauben | Allow extrapolation of the heat pump characteristic curve | Form_Simulation_Config.Uebersicht.cs:452 |
| `SIM_EXTRAPOLATION_TOOLTIP` | Unterschreitet die Quelltemperatur die niedrigste Stützstelle der\nWärmepumpen-Kennlinie, wird die Kennlinie linear verlängert.\n\nMit Haken (Vorbelegung): Es wird extrapoliert, und der Lauf vermerkt das\nals Hinweis. Das entspricht genau dem bisherigen Verhalten - die Engine\nhat bis Paket 8 an dieser Stelle nachgefragt.\n\nOhne Haken: Die Simulation bricht ab und nennt die betroffene Anlage.\nSinnvoll, wenn extrapolierte Kennwerte nicht in ein Ergebnis einfließen\nsollen; die Kennlinie ist dann um tiefere Stützstellen zu ergänzen. | If the source temperature falls below the lowest data point of the\nheat pump characteristic curve, the curve is extended linearly.\n\nWith the tick (default): extrapolation takes place and the run records this\nas a note. This corresponds exactly to the previous behaviour - up to package 8\nthe engine asked at this point.\n\nWithout the tick: the simulation aborts and names the affected unit.\nUseful if extrapolated characteristic values should not enter a result;\nthe curve then has to be supplemented with lower data points. | Form_Simulation_Config.Uebersicht.cs:458 |
| `SIM_GB_LADEVERHALTEN` | Ladeverhalten am Pufferspeicher | Charging behaviour at the buffer storage | Form_Waermesenke.cs:192 |
| `SIM_HEIZKREIS` | Heizkreis | Heating circuit | WaermesenkeClass.cs:69, WaermesenkeClass.cs:692 |
| `SIM_HEIZKREIS_BEIDES` | Heizkreis (beides) | Heating circuit (both) | WaermesenkeClass.cs:707 |
| `SIM_HEIZKREIS_NUR_HEIZWAERME` | Heizkreis (nur Heizwärme) | Heating circuit (space heating only) | WaermesenkeClass.cs:706 |
| `SIM_HEIZKREIS_NUR_WARMWASSER` | Heizkreis (nur Warmwasser) | Heating circuit (DHW only) | WaermesenkeClass.cs:705 |
| `SIM_KACHEL_RESTSTROMBEDARF` | Reststrombedarf | Residual electricity demand | NavigatorUebersicht.cs:263 |
| `SIM_KACHEL_RESTWAERMEBEDARF` | Restwärmebedarf | Residual heat demand | NavigatorUebersicht.cs:268 |
| `SIM_KACHEL_SIMULATIONSERGEBNISSE` | Simulationsergebnisse im Detail | Simulation results in detail | NavigatorUebersicht.cs:282 |
| `SIM_KASKADE_SCHALTER` | Zweikanalige Kaskade (Vorschau) | Two-channel cascade (preview) | Form_Simulation_Config.Uebersicht.cs:352 |
| `SIM_KASKADE_TOOLTIP` | Rechnet Heiz- und Warmwasserbedarf als getrennte Kanäle und löst die\nSpeicherladung aus der Erzeugerkaskade heraus (Vorschau).\n\nDas ÄNDERT die Ergebnisse: Anlagen mit Pufferspeicher als Senke laden\ndiesen, statt den Bedarf direkt zu decken; gedeckt wird aus dem Speicher.\nWas sich im Einzelnen ändert, steht im Umsetzungsprotokoll zu Paket 4\n(Teil 7, Dokumentierte Ergebnisaenderungen). Ohne Haken rechnet die\nbisherige, einkanalige Kaskade unverändert weiter. | Calculates space heating and domestic hot water demand as separate channels and\nseparates the storage charging from the generator cascade (preview).\n\nThis CHANGES the results: units with a buffer storage as sink charge\nit instead of covering the demand directly; the demand is covered from the storage.\nWhat changes in detail is described in the implementation log for package 4\n(part 7, Documented result changes). Without the tick, the\nprevious single-channel cascade continues to calculate unchanged. | Form_Simulation_Config.Uebersicht.cs:363 |
| `SIM_KEIN_BRAUCHWASSERBEDARF` | Hinweis: Dem Projekt ist kein Brauchwasserbedarf zugeordnet.\nEin Brauchwasserspeicher wird dann zwar geladen, aber nie entladen. | Note: no domestic hot water demand is assigned to the project.\nA DHW storage is then charged but never discharged. | WaermesenkeClass.cs:669 |
| `SIM_KEIN_PUFFER_GEWAEHLT` | Für die {0} „{1}" ist kein Pufferspeicher gewählt.\n\nIm Projekt muss ein Pufferspeicher mit der Verwendung „{2}" angelegt sein. | No buffer storage is selected for the {0} "{1}".\n\nThe project must contain a buffer storage with the use "{2}". | WaermesenkeClass.cs:591 |
| `SIM_KEINE_SENKENDATEN` | Keine Senkendaten übergeben. | No heat sink data supplied. | WaermesenkeClass.cs:516 |
| `SIM_LABEL_GASVERBRAUCH` | Gasverbrauch (Hu): | Gas consumption (NCV): | Form_Simulation_Detail.cs:2800 |
| `SIM_LABEL_HOLZVERBRAUCH` | Holzverbrauch: | Wood consumption: | Form_Simulation_Detail.cs:2812 |
| `SIM_LABEL_KOHLE` | Kohle: | Coal: | Form_Simulation_Detail.cs:2839 |
| `SIM_LABEL_KOKS` | Koks: | Coke: | Form_Simulation_Detail.cs:2834 |
| `SIM_LABEL_OELVERBRAUCH` | Ölverbrauch: | Oil consumption: | Form_Simulation_Detail.cs:2806 |
| `SIM_LABEL_PELLETS` | Pellets: | Pellets: | Form_Simulation_Detail.cs:2818 |
| `SIM_LABEL_RAPSOEL` | Rapsöl: | Rapeseed oil: | Form_Simulation_Detail.cs:2824 |
| `SIM_LABEL_SONSTIGE` | Sonstigel: | Other: | Form_Simulation_Detail.cs:2844 |
| `SIM_LABEL_TIERISCHE_FETTE` | Tierische Fette: | Animal fats: | Form_Simulation_Detail.cs:2829 |
| `SIM_LADEEINTRAG_ANZEIGE` | {0} ({1}, Prio {2}) | {0} ({1}, prio {2}) | Ladeordnung.cs:180 |
| `SIM_LAUFMELDUNG_EINER` | 1 Hinweis zum Lauf (anklicken) | 1 note on the run (click) | Form_Simulation_Detail.cs:1318 |
| `SIM_LAUFMELDUNG_MEHRERE` | {0} Hinweise zum Lauf (anklicken) | {0} notes on the run (click) | Form_Simulation_Detail.cs:1319 |
| `SIM_LBL_BEDARF_HINWEIS` | (nur beim Heizkreis wirksam) | (effective only for the heating circuit) | Form_Waermesenke.cs:144 |
| `SIM_LBL_BEDARFSART` | Bedarfsart: | Demand type: | Form_Waermesenke.cs:127 |
| `SIM_LBL_HINWEIS_PUFFER` | Für Puffer-Senken muss der Speicher im Projekt angelegt sein (mit passender Verwendung Heizung bzw. Brauchwasser). | For buffer sinks the storage must be created in the project (with matching use heating or DHW). | Form_Waermesenke.cs:323 |
| `SIM_LBL_LADEGRENZE_EINHEIT` | % des Speichers  (sonst gilt die Abschaltschwelle des Speichers) | % of the storage  (otherwise the switch-off threshold of the storage applies) | Form_Waermesenke.cs:225 |
| `SIM_LBL_LADEPRIO` | Ladepriorität: | Charging priority: | Form_Waermesenke.cs:198, Form_Waermesenke.cs:285 |
| `SIM_LBL_PV_UEBERSCHUSS` | Bei PV-Überschuss: | With PV surplus: | Form_Waermesenke.cs:230 |
| `SIM_LBL_ZIEL2` | Ziel: | Target: | Form_Waermesenke.cs:266 |
| `SIM_MENUE_ENERGIEBEDARF` | Energiebedarf | Energy demand | Form_Simulation_Detail.cs:608 |
| `SIM_MODUS_LAUFZEIT` | laufzeitoptimiert | runtime-optimised | Form_Simulation_Config.Uebersicht.cs:901 |
| `SIM_MODUS_LEISTUNG` | leistungsoptimiert | output-optimised | Form_Simulation_Config.Uebersicht.cs:899 |
| `SIM_MODUS_PV` | PV-optimiert | PV-optimised | Form_Simulation_Config.Uebersicht.cs:900 |
| `SIM_MSG_BRAUCHWASSER_UEBERGANG` | Hinweis: Die Brauchwasser-Senke wird erst mit dem Engine-Umbau (Paket 4) wirksam.\nSie wird gespeichert und angezeigt, geht in die Simulation aber noch nicht ein. | Note: The DHW sink only becomes effective with the engine conversion (package 4).\nIt is saved and displayed, but does not yet enter the simulation. | Form_Waermesenke.cs:698 |
| `SIM_MSG_BRAUCHWASSER_WP_ZUSATZ` | Die bisherige Pufferspeicher-Zuordnung dieser Wärmepumpe wird dabei entfernt; bis Paket 4 rechnet die Simulation dann ohne Speicher. | The previous buffer storage assignment of this heat pump is removed in the process; until package 4 the simulation then calculates without storage. | Form_Waermesenke.cs:707 |
| `SIM_MSG_ERGEBNIS_GESPEICHERT` | Simulationsergebnis gespeichert. | Simulation result saved. | Form_Simulation_Detail.cs:1397 |
| `SIM_MSG_ERGEBNIS_NICHT_GESPEICHERT` | Das Ergebnis konnte nicht gespeichert werden. | The result could not be saved. | Form_Simulation_Detail.cs:1399 |
| `SIM_MSG_KEIN_BRENNSTOFF` | Kein Brennstoff für dieses BHKW definiert. | No fuel defined for this CHP unit. | Form_Simulation_Detail.cs:2852 |
| `SIM_MSG_KEIN_PROJEKT` | Kein Projekt geladen. | No project loaded. | Form_Simulation_Detail.cs:1367 |
| `SIM_MSG_KEIN_VOLLSTAENDIGES_ERGEBNIS` | Es liegt kein vollständiges Simulationsergebnis vor.\n\nBitte zuerst die Simulation ausführen. Ein abgebrochener oder noch nicht gerechneter Lauf wird nicht gespeichert - das bisher gespeicherte Ergebnis des Projekts bleibt dadurch erhalten. | There is no complete simulation result.\n\nPlease run the simulation first. An aborted run or one that has not yet been calculated is not saved - the result stored so far for the project is thereby retained. | Form_Simulation_Detail.cs:1382 |
| `SIM_MSG_KEINE_DATEN_ENERGIEBEDARF` | Keine Simulationsdaten vorhanden!\nBitte zuerst den Energiebedarf berechnen. | No simulation data available!\nPlease calculate the energy demand first. | Form_Simulation_Detail.cs:439 |
| `SIM_MSG_KEINE_DATEN_SIMULATION` | Keine Simulationsdaten vorhanden!\nBitte zuerst die Simulation durchführen. | No simulation data available!\nPlease run the simulation first. | NavigatorStrom.cs:52, NavigatorWaerme.cs:115 |
| `SIM_MSG_KEINE_DATEN_WAERMEPUMPE` | Keine Simulationsdaten vorhanden!\nBitte zuerst die Simulation mit Wärmepumpe durchführen. | No simulation data available!\nPlease run the simulation with the heat pump first. | Form_Simulation_Detail.cs:461 |
| `SIM_MSG_KLIMAREGION_WAEHLEN` | Klimaregion auswählen! | Select climate region! | Form_Simulation_Detail.cs:1445 |
| `SIM_MSG_KONFIGURATION_FEHLT` | Bitte zuerst die Konfiguration festlegen. | Please define the configuration first. | Form_Simulation_Detail.cs:1151 |
| `SIM_MSG_LADEGRENZE_BEREICH` | Die Ladeobergrenze der {0} muss zwischen 0 und 100 % liegen. | The charging upper limit of the {0} must be between 0 and 100 %. | Form_Waermesenke.cs:780 |
| `SIM_MSG_LADEGRENZE_ZAHL` | Die Ladeobergrenze der {0} muss eine Zahl sein. | The charging upper limit of the {0} must be a number. | Form_Waermesenke.cs:774 |
| `SIM_MSG_MODUS_NUR_WP` | Der Betriebsmodus (Leistungssteuerung) ist heute nur für Wärmepumpen wirksam.\n\nAnlage: {0}\nFür Heizkessel, BHKW und Solarthermie ergibt sich das Verhalten aus der\nKaskadenstellung und der Wärmesenke. | The operating mode (output control) is currently effective only for heat pumps.\n\nUnit: {0}\nFor boilers, CHP units and solar thermal the behaviour results from the\nposition in the cascade and the heat sink. | Form_Simulation_Config.Uebersicht.cs:920 |
| `SIM_MSG_NETZVERLUSTE_ZU_GROSS` | die Netzverluste dürfen nicht größer als 100 % sein! | The network losses must not be greater than 100 %! | Form_Simulation_Detail.cs:1437 |
| `SIM_MSG_PUFFER_ANLEGEN_FRAGE` | {0}\n\nJetzt einen Pufferspeicher im Projekt anlegen? | {0}\n\nCreate a buffer storage in the project now? | Form_Waermesenke.cs:638 |
| `SIM_MSG_PV_AUSWAHL` | Hinweis: Für den PV-optimierten Betrieb muss im Bereich 'Stromerzeuger' die Photovoltaik ausgewählt sein.\nOhne PV-Anlage verhält sich die Wärmepumpe leistungsoptimiert. | Note: For PV-optimised operation, photovoltaics must be selected in the 'Electricity generator' area.\nWithout a PV system the heat pump behaves output-optimised. | Form_Simulation_Config.Uebersicht.cs:1019 |
| `SIM_MSG_WEITERE_FEHLERMELDUNGEN` | Weitere Fehlermeldungen des Laufs: | Further error messages from the run: | Form_Simulation_Detail.cs:1266, Form_Simulation_Detail.cs:1478 |
| `SIM_MSG_WPPRIO_NUR_WP` | Die WP-Priorität regelt die Reihenfolge der Wärmepumpen untereinander.\nFür {0} ist sie ohne Bedeutung. | The HP priority governs the order of the heat pumps among themselves.\nFor {0} it has no meaning. | Form_Simulation_Config.Uebersicht.cs:1164 |
| `SIM_NAV_AUTARKIE_ANALYSE` | ℹ️ \nAutarkie\nAnalyse | ℹ️ \nSelf-sufficiency\nanalysis | TabNavigationManager.cs:61 |
| `SIM_NAV_STROMPRODUKTION_CHART` | ⚡ \nStrom\nProduktion\n Chart | ⚡ \nElectricity\ngeneration\n chart | TabNavigationManager.cs:63 |
| `SIM_NAV_UEBERSICHT` | 🏠 \nÜbersicht | 🏠 \nOverview | TabNavigationManager.cs:60 |
| `SIM_NAV_WAERMEPRODUKTION_CHART` | 🔥 \nWärme\nProduktion\nChart | 🔥 \nHeat\ngeneration\nchart | TabNavigationManager.cs:62 |
| `SIM_PHOTOVOLTAIK` | Photovoltaik | Photovoltaics | Form_Simulation_Detail.cs:205, Form_Simulation_Detail.cs:659, NavigatorUebersicht.cs:246 |
| `SIM_POSITION_BIS` | bis {0} % | up to {0} % | Form_Waermesenke.cs:561 |
| `SIM_POSITION_LAEDT_ALS` | Lädt als {0}. von {1} | Charges as no. {0} of {1} | Form_Waermesenke.cs:559 |
| `SIM_PRIO_UNVERAENDERT` | unverändert (reguläre Priorität) | unchanged (regular priority) | Form_Waermesenke.cs:469 |
| `SIM_PRIO_VORGABE` | nach Vorgabe ({0} - {1}) | as default ({0} - {1}) | Form_Waermesenke.cs:470 |
| `SIM_PUFFER_BRAUCHWASSER_KURZ` | Puffer Brauchw. | Buffer DHW | WaermesenkeClass.cs:698, WaermesenkeClass.cs:727 |
| `SIM_PUFFER_FREMDES_PROJEKT` | Der für die {0} gewählte Pufferspeicher gehört nicht zu diesem Projekt oder wurde entfernt.\n\nBitte einen Projekt-Pufferspeicher mit der Verwendung „{1}" anlegen. | The buffer storage selected for the {0} does not belong to this project or has been removed.\n\nPlease create a project buffer storage with the use "{1}". | WaermesenkeClass.cs:601 |
| `SIM_PUFFER_HEIZUNG_KURZ` | Puffer Heizung | Buffer heating | WaermesenkeClass.cs:698, WaermesenkeClass.cs:727 |
| `SIM_PUFFER_MIT_VOLUMEN` | {0} ({1} l) | {0} ({1} l) | WaermesenkeClass.cs:150 |
| `SIM_PUFFER_QUELLE_UND_SENKE` | Der Pufferspeicher „{0}" ist bereits die WÄRMEQUELLE dieser Anlage.\nDerselbe Speicher kann nicht zugleich Quelle und Senke sein (Kurzschluss); bitte einen anderen Speicher wählen. | The buffer storage "{0}" is already the HEAT SOURCE of this unit.\nThe same storage cannot be source and sink at the same time (short circuit); please select a different storage. | WaermesenkeClass.cs:570 |
| `SIM_PUFFER_VERWENDUNG_PASST_NICHT` | Der Pufferspeicher „{0}" hat die Verwendung „{1}", die {2} verlangt aber „{3}".\n\nBitte einen passenden Speicher wählen oder die Verwendung in der Pufferspeicher-Verwaltung ändern. | The buffer storage "{0}" has the use "{1}", but the {2} requires "{3}".\n\nPlease select a suitable storage or change the use in the buffer storage management. | WaermesenkeClass.cs:609 |
| `SIM_RB_HEIZKREIS` | Heizkreis (direkte Deckung des Bedarfs) | Heating circuit (direct coverage of the demand) | Form_Waermesenke.cs:119 |
| `SIM_ROLLE_HAUPTSENKE` | Hauptsenke | main sink | WaermesenkeClass.cs:524, Form_Waermesenke.cs:111, Form_Waermesenke.cs:745, Form_PufferSp_Projekt.cs:557 |
| `SIM_ROLLE_ZWEITSENKE` | Zweitsenke | secondary sink | WaermesenkeClass.cs:537, Form_Simulation_Config.Uebersicht.cs:225, Form_Waermesenke.cs:756, Form_PufferSp_Projekt.cs:557 |
| `SIM_SENKE_TITEL` | Wärmesenke | Heat sink | Form_Waermesenke.cs:101, Form_Waermesenke.cs:625, Form_Waermesenke.cs:648, Form_Waermesenke.cs:665 |
| `SIM_SENKE_TITEL_ANLAGE` | Wärmesenke - {0} | Heat sink - {0} | Form_Waermesenke.cs:372 |
| `SIM_SOLARTHERMIE_ANLAGE` | Solarthermie-Anlage | Solar thermal system | NavigatorUebersicht.cs:68 |
| `SIM_SPALTE_ANLAGE` | Anlage | Unit | Form_Simulation_Config.Uebersicht.cs:221, Form_PufferSp_Projekt.cs:224 |
| `SIM_SPALTE_ANZAHL` | Anzahl | Quantity | Form_Simulation_Detail.cs:179, Form_Simulation_Detail.cs:208 |
| `SIM_SPALTE_BETRIEBSSTUNDEN` | Betriebsstunden [h/a] | Operating hours [h/a] | Form_Simulation_Detail.cs:1716 |
| `SIM_SPALTE_BRENNSTOFFE` | Gas/Biogas/Rapsöl/Holz... [MWh/a] | Gas/biogas/rapeseed oil/wood... [MWh/a] | Form_Simulation_Detail.cs:125 |
| `SIM_SPALTE_ENERGIE_ERZEUGER` | Energie-Erzeuger | Energy generator | NavigatorUebersicht.cs:44 |
| `SIM_SPALTE_ERGEBNIS_MWH` | Ergebnis [MWh/a] | Result [MWh/a] | NavigatorUebersicht.cs:52 |
| `SIM_SPALTE_FLAECHE` | Fläche [m²] | Area [m²] | Form_Simulation_Detail.cs:178, Form_Simulation_Detail.cs:207 |
| `SIM_SPALTE_HEIZSTAB` | Heizstab [MWh/a] | Immersion heater [MWh/a] | Form_Simulation_Detail.cs:1715 |
| `SIM_SPALTE_JAHRESNUTZUNGSGRAD` | Jahresnutzungsgrad [%] | Annual utilisation ratio [%] | Form_Simulation_Detail.cs:127 |
| `SIM_SPALTE_LEISTUNG` | Leistung [kW] | Power [kW] | Form_Simulation_Detail.cs:1712, Form_Simulation_Detail.cs:1784 |
| `SIM_SPALTE_MODUL` | Modul | Module | Form_Simulation_Detail.cs:1711 |
| `SIM_SPALTE_MODUS` | Modus | Mode | Form_Simulation_Config.Uebersicht.cs:226 |
| `SIM_SPALTE_NAME` | Name | Name | Form_Simulation_Detail.cs:124, Form_Simulation_Detail.cs:157, Form_Simulation_Detail.cs:177, Form_Simulation_Detail.cs:206 |
| `SIM_SPALTE_OEL` | Öl [MWh/a] | Oil [MWh/a] | Form_Simulation_Detail.cs:126 |
| `SIM_SPALTE_PRIO` | Prio | Prio | Form_Simulation_Config.Uebersicht.cs:219 |
| `SIM_SPALTE_SENKE` | Senke | Sink | Form_Simulation_Config.Uebersicht.cs:224, Form_PufferSp_Projekt.cs:226 |
| `SIM_SPALTE_SOLARKOLLEKTOR` | Solarkollektor | Solar collector | Form_Simulation_Detail.cs:176 |
| `SIM_SPALTE_STROMPRODUKTION` | Stromprod. [MWh/a] | Electricity generation [MWh/a] | Form_Simulation_Detail.cs:159, Form_Simulation_Detail.cs:209 |
| `SIM_SPALTE_STROMVERBRAUCH` | Stromverbr. [MWh/a] | Electricity consumption [MWh/a] | Form_Simulation_Detail.cs:1714 |
| `SIM_SPALTE_UEBERSCHUSS` | Überschuß [MWh/a] | Surplus [MWh/a] | Form_Simulation_Detail.cs:181 |
| `SIM_SPALTE_WAERMEPRODUKTION` | Wärmeprod. [MWh/a] | Heat generation [MWh/a] | Form_Simulation_Detail.cs:158, Form_Simulation_Detail.cs:180, Form_Simulation_Detail.cs:1713 |
| `SIM_SPALTE_WPPRIO` | WP-Prio | HP prio | Form_Simulation_Config.Uebersicht.cs:222 |
| `SIM_STATUS_EINSTELLUNG_FEHLER` | Die Einstellung konnte nicht gespeichert werden. | The setting could not be saved. | Form_Simulation_Config.Uebersicht.cs:427, Form_Simulation_Config.Uebersicht.cs:577 |
| `SIM_STATUS_EXTRAPOLATION_AUS` | Extrapolation der WP-Kennlinie abgewählt - der Lauf bricht ab, wenn die Quelltemperatur die Kennlinie unterschreitet. | Extrapolation of the heat pump characteristic curve deselected - the run aborts if the source temperature falls below the curve. | Form_Simulation_Config.Uebersicht.cs:567 |
| `SIM_STATUS_EXTRAPOLATION_EIN` | Extrapolation der WP-Kennlinie erlaubt - der Lauf vermerkt sie als Hinweis. | Extrapolation of the heat pump characteristic curve allowed - the run records it as a note. | Form_Simulation_Config.Uebersicht.cs:566 |
| `SIM_STATUS_KASKADE_AUS` | Zweikanalige Kaskade abgewählt - es rechnet wieder die einkanalige Kaskade. | Two-channel cascade deselected - the single-channel cascade calculates again. | Form_Simulation_Config.Uebersicht.cs:415 |
| `SIM_STATUS_KASKADE_EIN` | Zweikanalige Kaskade eingeschaltet - der nächste Lauf rechnet damit und liefert andere Ergebnisse. | Two-channel cascade switched on - the next run calculates with it and delivers different results. | Form_Simulation_Config.Uebersicht.cs:413 |
| `SIM_STATUS_KONFIG_GESPEICHERT` | ✔ Konfiguration erfolgreich gespeichert | ✔ Configuration saved successfully | Form_Simulation_Config.cs:982 |
| `SIM_STATUS_SENKE_FEHLER` | ⚠ Die Wärmesenke konnte nicht vollständig gespeichert werden | ⚠ The heat sink could not be saved completely | Form_Simulation_Config.Uebersicht.cs:1237 |
| `SIM_STATUS_SENKE_GESPEICHERT` | ✔ Wärmesenke gespeichert ({0}) | ✔ Heat sink saved ({0}) | Form_Simulation_Config.Uebersicht.cs:1240 |
| `SIM_STROMSPEICHER` | Stromspeicher | Electricity storage | Form_Simulation_Detail.cs:667 |
| `SIM_TABELLE_HEIZKESSEL` | HeizKessel | Boiler | NavigatorUebersicht.cs:69 |
| `SIM_TIP_BETRIEBSMODUS` | Betriebsmodus (Doppelklick zum Ändern)\n• laufzeitoptimiert - volle Leistung, Überschuss lädt den Speicher\n• leistungsoptimiert - moduliert exakt auf den Wärmebedarf\n• PV-optimiert - erhöhte Leistung nur bei verfügbarem PV-Strom,\n  sonst leistungsoptimiert | Operating mode (double-click to change)\n• runtime-optimised - full output, surplus charges the storage\n• output-optimised - modulates exactly to the heat demand\n• PV-optimised - increased output only with available PV electricity,\n  otherwise output-optimised | Form_Simulation_Config.Uebersicht.cs:1091 |
| `SIM_TIP_BETRIEBSMODUS_NICHT_WP` | Der Betriebsmodus ist heute nur für Wärmepumpen wirksam. | The operating mode is currently effective only for heat pumps. | Form_Simulation_Config.Uebersicht.cs:1096 |
| `SIM_TIP_SENKE` | Wärmesenke (Doppelklick zum Ändern)\nWohin gibt dieser Erzeuger seine Wärme ab?\n• Heizkreis - deckt den Bedarf der Stunde unmittelbar\n  (Bedarfsart Warmwasser / Heizwärme / beides)\n• Pufferspeicher Heizung bzw. Brauchwasser - lädt einen\n  Projekt-Pufferspeicher; dort werden auch Ladepriorität und\n  Ladeobergrenze gepflegt. | Heat sink (double-click to change)\nWhere does this generator release its heat?\n• Heating circuit - covers the demand of the hour directly\n  (demand type domestic hot water / space heating / both)\n• Buffer storage heating or DHW - charges a\n  project buffer storage; charging priority and\n  charging upper limit are maintained there as well. | Form_Simulation_Config.Uebersicht.cs:1073 |
| `SIM_TIP_UEBERSICHT_STANDARD` | Anlage: {0}\nDoppelklick auf Wärmesenke, Zweitsenke oder - bei Wärmepumpen -\nWP-Prio, Wärmequelle und Betriebsmodus zum Bearbeiten. | Unit: {0}\nDouble-click on heat sink, secondary sink or - for heat pumps -\nHP prio, heat source and operating mode to edit. | Form_Simulation_Config.Uebersicht.cs:1108 |
| `SIM_TIP_WPPRIO` | WP-Priorität (Doppelklick zum Ändern)\nEinsatz-Reihenfolge der Wärmepumpen: 1 = wird zuerst eingesetzt,\ndie nächste deckt jeweils den verbleibenden Bedarf der Stunde. | HP priority (double-click to change)\nOrder of use of the heat pumps: 1 = is used first,\nthe next one covers the remaining demand of the hour. | Form_Simulation_Config.Uebersicht.cs:1056 |
| `SIM_TIP_WPPRIO_NICHT_WP` | WP-Priorität gilt nur für Wärmepumpen. | HP priority applies only to heat pumps. | Form_Simulation_Config.Uebersicht.cs:1059 |
| `SIM_TIP_ZWEITSENKE` | Zweitsenke (Doppelklick zum Ändern)\nOptionaler zweiter Pufferspeicher, der NUR Überschuss bzw.\nverbleibendes Ladepotenzial aufnimmt - nie Pflichtbedarf.\n„–" bedeutet: keine Zweitsenke. | Secondary sink (double-click to change)\nOptional second buffer storage that takes ONLY surplus or\nremaining charging potential - never mandatory demand.\n'–' means: no secondary sink. | Form_Simulation_Config.Uebersicht.cs:1083 |
| `SIM_TITEL_BETRIEBSMODUS` | Betriebsmodus | Operating mode | Form_Simulation_Config.Uebersicht.cs:924 |
| `SIM_TITEL_BETRIEBSMODUS_PV` | Betriebsmodus PV | Operating mode PV | Form_Simulation_Config.Uebersicht.cs:1022 |
| `SIM_TITEL_ERGEBNIS_SPEICHERN` | Ergebnis speichern | Save result | Form_Simulation_Detail.cs:1387 |
| `SIM_TITEL_FEHLER` | Fehler | Error | Form_Simulation_Detail.cs:1151, Form_Simulation_Detail.cs:1399 |
| `SIM_TITEL_HINWEIS` | Hinweis | Note | Form_Simulation_Detail.cs:1367 |
| `SIM_TITEL_MELDUNGEN_LAUF` | Meldungen des Simulationslaufs | Messages from the simulation run | Form_Simulation_Detail.cs:1351 |
| `SIM_TITEL_SENKE_PUFFER_FEHLT` | Wärmesenke - Pufferspeicher fehlt | Heat sink - buffer storage missing | Form_Waermesenke.cs:640 |
| `SIM_TITEL_SIMULATION_ABGEBROCHEN` | Simulation abgebrochen | Simulation aborted | Form_Simulation_Detail.cs:1269, Form_Simulation_Detail.cs:1479 |
| `SIM_TITEL_SIMULATION_NICHT_VERFUEGBAR` | Simulation nicht verfügbar | Simulation not available | Form_Simulation_Detail.cs:1109, Form_Simulation_Config.cs:855 |
| `SIM_TITEL_WPPRIO` | WP-Priorität | HP priority | Form_Simulation_Config.Uebersicht.cs:1167 |
| `SIM_TOOLTIP_CSV_BEDARF` | Wärmelast und Strombedarf als CSV exportieren\n(Zeitstempel, Außentemperatur, Werte) | Export heat load and electricity demand as CSV\n(time stamp, outdoor temperature, values) | Form_Simulation_Detail.cs:265 |
| `SIM_TOOLTIP_CSV_WAERMEPUMPE` | Wärmepumpen-Simulation als CSV exportieren\n(Zeitstempel, Außentemperatur, Wärmebedarf, Heizstab, Wärmeproduktion, Strombedarf) | Export heat pump simulation as CSV\n(time stamp, outdoor temperature, heat demand, immersion heater, heat generation, electricity demand) | Form_Simulation_Detail.cs:282 |
| `SIM_UEBERSICHT_TITEL` | Übersicht Wärmeerzeuger | Heat generator overview | Form_Simulation_Config.Uebersicht.cs:194 |
| `SIM_WPPRIO_DIALOG_TEXT` | Einsatz-Reihenfolge der Wärmepumpe\n'{0}'\n(1 = wird zuerst eingesetzt): | Order of use of the heat pump\n'{0}'\n(1 = is used first): | Form_Simulation_Config.Uebersicht.cs:1172 |
| `SIM_WPPRIO_DIALOG_TITEL` | Wärmepumpen-Priorität | Heat pump priority | Form_Simulation_Config.Uebersicht.cs:1171 |
| `SIM_ZIEL_PUFFERSPEICHER_BRAUCHWASSER` | Pufferspeicher Brauchwasser | Buffer storage DHW | WaermesenkeClass.cs:68, Form_Waermesenke.cs:167, Form_Waermesenke.cs:273 |
| `SIM_ZIEL_PUFFERSPEICHER_HEIZUNG` | Pufferspeicher Heizung | Buffer storage heating | WaermesenkeClass.cs:66, Form_Waermesenke.cs:152, Form_Waermesenke.cs:273 |
| `SIM_ZWEITSENKE_GLEICH_HAUPTSENKE` | Die Zweitsenke muss sich von der Hauptsenke unterscheiden.\nBeide zeigen auf {0} „{1}". | The secondary sink must differ from the main sink.\nBoth point to {0} "{1}". | WaermesenkeClass.cs:557 |

## SIMENG — 29 Schlüssel

| Schlüssel | DE | EN | Fundstellen |
|---|---|---|---|
| `SIMENG_BHKW_MAX_UEBERSCHRITTEN` | BHKW: Im Projekt sind {0} BHKW hinterlegt, die Simulation unterstützt maximal {1}. Der Lauf wurde abgebrochen, damit kein Ergebnis ohne die übrigen Module entsteht. | CHP unit: {0} CHP units are stored in the project, the simulation supports a maximum of {1}. The run was aborted so that no result is produced without the remaining modules. | SimulationBHKW.cs:1189 |
| `SIMENG_BRAUCHWASSER_TYP_UNDEFINIERT` | Brauchwasser: Der Typ des Eintrags '{0}' ist nicht definiert. Die Rechnung wurde abgebrochen; ihr Anteil bleibt 0. | Domestic hot water: the type of the entry '{0}' is not defined. The calculation was aborted; its share remains 0. | SimulationWaermebedarf.cs:849 |
| `SIMENG_DB_ZUGRIFF_WAEHREND_LAUF` | Datenbankzugriff während des Laufs: {0} | Database access during the run: {0} | SimulationRunner.cs:76, SimulationControl.cs:241 |
| `SIMENG_ERGEBNIS_NICHT_GESPEICHERT` | Das Simulationsergebnis konnte nicht gespeichert werden. | The simulation result could not be saved. | SimulationRunner.cs:738 |
| `SIMENG_KEINE_KLIMAREGION` | Für Projekt {0} ist keine Klimaregion gesetzt. | No climate region is set for project {0}. | SimulationRunner.cs:134 |
| `SIMENG_KEINE_KONFIGURATION` | Für Projekt {0} ist keine Konfiguration (Tab_Einstellungen) hinterlegt. | No configuration (Tab_Einstellungen) is stored for project {0}. | SimulationRunner.cs:116 |
| `SIMENG_KESSEL_MAX_UEBERSCHRITTEN` | Heizkessel: Im Projekt sind {0} Kessel hinterlegt, die Simulation unterstützt maximal {1}. Es werden nur die ersten {2} Kessel berücksichtigt. | Boiler: {0} boilers are stored in the project, the simulation supports a maximum of {1}. Only the first {2} boilers are taken into account. | SimulationSPK.cs:131, SimulationSPK.cs:499 |
| `SIMENG_KESSEL_NICHT_HINTERLEGT` | Der Heizkessel '{0}' ist im Projekt nicht hinterlegt. Die Kessel-Simulation wurde abgebrochen. | The boiler '{0}' is not stored in the project. The boiler simulation was aborted. | SimulationSPK.cs:195 |
| `SIMENG_LADEORDNUNG_ART_NICHT_IN_SPEICHERSTUFE` | Ladeordnung: Anlage {0} ({1}) lädt laut Konfiguration den Speicher {2} ({3}). Diese Erzeugerart rechnet in diesem Lauf nicht in der Speicherstufe; die Anlage rechnet als Vektorstufe wie eine Heizkreis-Anlage. | Charging order: unit {0} ({1}) is configured to charge the storage {2} ({3}). This generator type is not part of the storage stage in this run; the unit is calculated as a vector stage, like a heating-circuit unit. | SimulationControl.cs:1704 |
| `SIMENG_LADEPRIO_VORBELEGUNG_NACHGEZOGEN` | Ladeprioritäten: {0} Feld(er) ohne Vorgabe auf 0 gesetzt (Konzept 3.4, Vorbelegung wie Migrationsregel R5). | Charging priorities: {0} field(s) without a default value set to 0 (concept 3.4, default value as in migration rule R5). | SimulationControl.cs:291 |
| `SIMENG_LISTE_FEHLER` | Fehler: {0} | Error: {0} | SimulationProtokoll.cs:224 |
| `SIMENG_LISTE_HINWEIS` | Hinweis: {0} | Note: {0} | SimulationProtokoll.cs:227 |
| `SIMENG_LISTE_WARNUNG` | Warnung: {0} | Warning: {0} | SimulationProtokoll.cs:225 |
| `SIMENG_NETZVERLUSTE_UEBER_100` | Die Netzverluste dürfen nicht größer als 100 % sein. | The network losses must not exceed 100 %. | SimulationRunner.cs:124 |
| `SIMENG_PENDELSPEICHER_NICHT_LESBAR` | BHKW-Pendelspeicher: Die Puffer-Zeile {0} des Projekts {1} ließ sich nicht lesen oder gehört zu einem anderen Projekt. Der Lauf wurde abgebrochen, damit das BHKW nicht stillschweigend ohne Speicher rechnet. | CHP buffer storage: the buffer storage record {0} of project {1} could not be read or belongs to a different project. The run was aborted so that the CHP unit does not silently calculate without storage. | SimulationControl.cs:1271 |
| `SIMENG_PENDELSPEICHER_ZEILE_FEHLT` | BHKW-Pendelspeicher: Für Projekt {0} ist ein Volumen von {1} l bekannt, aber es gibt keine Puffer-Zeile „{2}". Der Lauf wurde abgebrochen, damit das BHKW nicht stillschweigend ohne Speicher rechnet. | CHP buffer storage: for project {0} a volume of {1} l is known, but there is no buffer storage record "{2}". The run was aborted so that the CHP unit does not silently calculate without storage. | SimulationControl.cs:1256 |
| `SIMENG_PRAEFIX_HEIZKESSEL` | Heizkessel:  | Boiler:  | SimulationSPK.cs:198 |
| `SIMENG_PRAEFIX_STROMBEDARF` | Strombedarf:  | Electricity demand:  | SimulationStrombedarf.cs:86 |
| `SIMENG_PRAEFIX_WAERMEPUMPE` | Wärmepumpe:  | Heat pump:  | SimulationWaermepumpe.cs:304, SimulationWaermepumpe.cs:1537 |
| `SIMENG_PROZESSWAERME_TYP_UNDEFINIERT` | Prozesswärme: Der Typ des Prozesses '{0}' ist nicht definiert. Die Prozesswärme-Rechnung wurde abgebrochen; ihr Anteil bleibt 0. | Process heat: the type of the process '{0}' is not defined. The process heat calculation was aborted; its share remains 0. | SimulationWaermebedarf.cs:737 |
| `SIMENG_SIMULATION_ABGEBROCHEN` | Simulation abgebrochen: {0} | Simulation aborted: {0} | SimulationControl.cs:262 |
| `SIMENG_SPEICHERN_DES_ERGEBNISSES` | Speichern des Ergebnisses: {0} | Saving the result: {0} | SimulationRunner.cs:733 |
| `SIMENG_STROMPROFIL_ZULETZT_BEARBEITET` |  (zuletzt bearbeitet: Stromprofil '{0}') |  (last processed: electricity profile '{0}') | SimulationStrombedarf.cs:241 |
| `SIMENG_STROMPROFILE_DIAGNOSE` | Strombedarf: Die Stromprofile konnten nicht berechnet werden{0} - {1} | Electricity demand: the electricity profiles could not be calculated{0} - {1} | SimulationStrombedarf.cs:240, SimulationStrombedarf.cs:243 |
| `SIMENG_STROMPROFILE_NICHT_BERECHENBAR` | Die Stromprofile des Projekts konnten nicht berechnet werden. Die Simulation wurde abgebrochen. | The electricity profiles of the project could not be calculated. The simulation was aborted. | SimulationStrombedarf.cs:84 |
| `SIMENG_TAGESVERTEILUNG_FEHLT` | Wärmebedarf: Zum Tagesverteilungstyp „{0}“ sind keine Daten hinterlegt. Die Bedarfsrechnung wurde an dieser Stelle abgebrochen; das Ergebnis ist unvollständig. | Heat demand: no data is stored for the daily distribution type "{0}". The demand calculation was aborted at this point; the result is incomplete. | SimulationWaermebedarf.cs:175 |
| `SIMENG_WP_EXTRAPOLATION_HINWEIS` | Wärmepumpe '{0}': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie ({1} °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“). | Heat pump '{0}': the source temperature falls below the lowest data point of the performance curve ({1} °C). Extrapolation is applied (project setting "Allow extrapolation of the performance curve"). | SimulationWaermepumpe.cs:1545 |
| `SIMENG_WP_EXTRAPOLATION_VERBOTEN` | Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie der Wärmepumpe '{0}' ({1} °C). Die Projekteinstellung „Extrapolation der Kennlinie erlauben“ ist abgewählt, deshalb wurde die Simulation abgebrochen. Entweder die Kennlinie um tiefere Stützstellen ergänzen oder die Einstellung setzen. | The source temperature falls below the lowest data point of the performance curve of the heat pump '{0}' ({1} °C). The project setting "Allow extrapolation of the performance curve" is deselected, therefore the simulation was aborted. Either add lower data points to the performance curve or set the option. | SimulationWaermepumpe.cs:1532 |
| `SIMENG_WP_KEINE_KENNDATEN` | Für die Wärmepumpe '{0}' sind keine Kenndaten (Kennlinie) für Vorlauf {1} °C vorhanden. Die Simulation wurde abgebrochen. | For the heat pump '{0}' there is no performance data (performance curve) for a flow temperature of {1} °C. The simulation was aborted. | SimulationWaermepumpe.cs:301 |

## SIMQ — 138 Schlüssel

| Schlüssel | DE | EN | Fundstellen |
|---|---|---|---|
| `SIMQ_ANLAGE_ERSATZNAME` | Anlage {0} | Unit {0} | ErdreichAuswertung.cs:321 |
| `SIMQ_BODENTYP_GNEIS` | Gneis | Gneiss | ErdreichTemperatur.cs:175 |
| `SIMQ_BODENTYP_GRANIT` | Granit | Granite | ErdreichTemperatur.cs:174 |
| `SIMQ_BODENTYP_KALKSTEIN` | Kalkstein | Limestone | ErdreichTemperatur.cs:173 |
| `SIMQ_BODENTYP_KIES_NASS` | Kies/Steine, wassergesättigt | Gravel/stones, water-saturated | ErdreichTemperatur.cs:169 |
| `SIMQ_BODENTYP_KIES_TROCKEN` | Kies/Steine, trocken | Gravel/stones, dry | ErdreichTemperatur.cs:168 |
| `SIMQ_BODENTYP_MERGEL_LEHM` | Geschiebemergel/-lehm | Glacial till/boulder clay | ErdreichTemperatur.cs:170 |
| `SIMQ_BODENTYP_SAND_FEUCHT` | Sand, feucht | Sand, moist | ErdreichTemperatur.cs:166 |
| `SIMQ_BODENTYP_SAND_NASS` | Sand, wassergesättigt | Sand, water-saturated | ErdreichTemperatur.cs:167 |
| `SIMQ_BODENTYP_SAND_TROCKEN` | Sand, trocken | Sand, dry | ErdreichTemperatur.cs:165 |
| `SIMQ_BODENTYP_SANDSTEIN` | Sandstein | Sandstone | ErdreichTemperatur.cs:172 |
| `SIMQ_BODENTYP_TON_NASS` | Ton/Schluff, wassergesättigt | Clay/silt, water-saturated | ErdreichTemperatur.cs:164 |
| `SIMQ_BODENTYP_TON_TROCKEN` | Ton/Schluff, trocken | Clay/silt, dry | ErdreichTemperatur.cs:163 |
| `SIMQ_BODENTYP_TONSTEIN` | Ton-/Schluffstein | Claystone/siltstone | ErdreichTemperatur.cs:171 |
| `SIMQ_CSV_DATEIDIALOG_TITEL` | Quelltemperatur-Profil auswählen | Select source temperature profile | Form_Simulation_Config.Uebersicht.cs:1387 |
| `SIMQ_CSV_DATEIFILTER` | CSV Dateien (*.csv)\|*.csv\|Alle Dateien (*.*)\|*.* | CSV files (*.csv)\|*.csv\|All files (*.*)\|*.* | Form_Simulation_Config.Uebersicht.cs:1388 |
| `SIMQ_CSV_FEHLER` | Die Datei konnte nicht gelesen werden oder enthält keine 8760 Stundenwerte!\n\n{0} | The file could not be read or does not contain 8760 hourly values!\n\n{0} | Form_Simulation_Config.Uebersicht.cs:1393 |
| `SIMQ_CSV_FEHLER_TITEL` | CSV-Datei ungültig | CSV file invalid | Form_Simulation_Config.Uebersicht.cs:1394 |
| `SIMQ_CSV_FORMAT_HINWEIS` | Erwartetes CSV-Format für das Quelltemperatur-Profil:\n\n- 8760 Zeilen = Stundenwerte für ein Jahr (01.01. 00:00 bis 31.12. 23:00)\n- je Zeile ein Temperaturwert in °C (Dezimal-Komma oder -Punkt)\n- optional mit Zeitstempel: "Zeitstempel;Temperatur" (Semikolon-getrennt,\n  es wird der letzte Zahlenwert der Zeile verwendet)\n- eine Kopfzeile wird automatisch erkannt und übersprungen | Expected CSV format for the source temperature profile:\n\n- 8760 lines = hourly values for one year (01.01. 00:00 to 31.12. 23:00)\n- one temperature value in °C per line (decimal comma or point)\n- optionally with a time stamp: "Time stamp;Temperature" (semicolon-separated,\n  the last numeric value of the line is used)\n- a header line is detected and skipped automatically | WaermequelleClass.cs:89 |
| `SIMQ_CSV_FRAGE_DATEI` | {0}\n\nJetzt Datei auswählen? | {0}\n\nSelect file now? | Form_Simulation_Config.Uebersicht.cs:1382 |
| `SIMQ_CSV_TITEL` | Quelltemperatur aus CSV-Datei | Source temperature from CSV file | Form_Simulation_Config.Uebersicht.cs:1383 |
| `SIMQ_ENTZUG_ANTEILIG_GESCHAETZT` | maximale Entzugsleistung anteilig aus der Summenganglinie aller Wärmepumpen-Module geschätzt. | maximum extraction rate estimated proportionally from the aggregated load profile of all heat pump modules. | ErdreichAuswertung.cs:366 |
| `SIMQ_ENTZUG_NICHT_JE_MODUL_TRENNBAR` | maximale Entzugsleistung nicht je Modul trennbar (mehrere Wärmepumpen mit unterschiedlichen Quellen, Stundenganglinie liegt nur global vor). | maximum extraction rate cannot be separated per module (several heat pumps with different heat sources, the hourly load profile is only available globally). | ErdreichAuswertung.cs:343 |
| `SIMQ_ERDKOLLEKTOR_ANZEIGE` | Erdreich Kollektor {0} m | Ground collector {0} m | Form_Simulation_Config.Uebersicht.cs:802 |
| `SIMQ_ERDREICH_ANZAHL_SONDEN` | Anzahl Sonden: | Number of BHE: | Form_QuelleErdreich.cs:178 |
| `SIMQ_ERDREICH_BODENKENNWERTE` | λ = {0} W/(m·K)   ρ·c_p = {1} MJ/(m³·K)   a = {2} mm²/s   Dämpfungstiefe d = {3} m   Bodenart nach Tabelle A1: {4} | λ = {0} W/(m·K)   ρ·c_p = {1} MJ/(m³·K)   a = {2} mm²/s   Damping depth d = {3} m   Soil type according to Table A1: {4} | Form_QuelleErdreich.cs:452 |
| `SIMQ_ERDREICH_BODENTYP` | Bodentyp: | Soil type: | Form_QuelleErdreich.cs:194 |
| `SIMQ_ERDREICH_BODENTYP_HINWEIS` | (Katalog VDI 4640 Blatt 1, Entwurf 2021-12) | (Catalogue VDI 4640 Part 1, draft 2021-12) | Form_QuelleErdreich.cs:205 |
| `SIMQ_ERDREICH_ENTZUG_KURZTEXT` | Entzug {0} kWh/a{1}, Spitze {2} W, {3} h/a.  | Extraction {0} kWh/a{1}, peak {2} W, {3} h/a.  | ErdreichAuswertung.cs:167 |
| `SIMQ_ERDREICH_FLAECHE` | Fläche [m²]: | Area [m²]: | Form_QuelleErdreich.cs:173 |
| `SIMQ_ERDREICH_GB_PRUEFUNG` | Auslegungsprüfung nach VDI 4640 Blatt 2 (nach der Simulation) | Design check according to VDI 4640 Part 2 (after the simulation) | Form_QuelleErdreich.cs:329 |
| `SIMQ_ERDREICH_GB_QUELLSYSTEM` | Quellsystem | Source system | Form_QuelleErdreich.cs:149 |
| `SIMQ_ERDREICH_GB_VORSCHAU` | Vorschau: Jahresgang der Quelltemperatur | Preview: annual variation of the source temperature | Form_QuelleErdreich.cs:268 |
| `SIMQ_ERDREICH_HINWEIS_FESTGESTEIN` | \n  Hinweis: Festgestein wird auf die höchste Bodenart der Tabelle A1 abgebildet — nur Orientierung. | \n  Note: Rock is mapped to the highest soil type of Table A1 — for orientation only. | Form_QuelleErdreich.cs:517 |
| `SIMQ_ERDREICH_HINWEIS_VORBEHALT` | \n  Hinweis: {0} | \n  Note: {0} | Form_QuelleErdreich.cs:519 |
| `SIMQ_ERDREICH_KEINE_PRUEFUNG` | Auslegungsprüfung nicht möglich:\n\n{0} | Design check not possible:\n\n{0} | Form_Simulation_Config.Uebersicht.cs:1449 |
| `SIMQ_ERDREICH_KLIMAZONE` | Klimazone: | Climate zone: | Form_QuelleErdreich.cs:218 |
| `SIMQ_ERDREICH_KLIMAZONE_HINWEIS` | (DIN 4710, Vorbelegung aus der Klimaregion) | (DIN 4710, default from the climate region) | Form_QuelleErdreich.cs:234 |
| `SIMQ_ERDREICH_KURZTEXT_KOPF` | Erdreich {0}:  | Ground {0}:  | ErdreichAuswertung.cs:158 |
| `SIMQ_ERDREICH_LAENGE_SONDE` | Länge je Sonde [m]: | Length per BHE [m]: | Form_QuelleErdreich.cs:176 |
| `SIMQ_ERDREICH_MSG_ANZAHL_MIN` | Es muss mindestens eine Sonde vorhanden sein! | At least one BHE must be present! | Form_QuelleErdreich.cs:595 |
| `SIMQ_ERDREICH_MSG_FLAECHE` | Bitte die Kollektorfläche eintragen — sie ist Eingangsgröße\nder Auslegungsprüfung nach VDI 4640 Blatt 2. | Please enter the collector area — it is an input variable\nfor the design check according to VDI 4640 Part 2. | Form_QuelleErdreich.cs:570 |
| `SIMQ_ERDREICH_MSG_LAENGE_NULL` | Die Sondenlänge muss größer als 0 m sein! | The BHE length must be greater than 0 m! | Form_QuelleErdreich.cs:590 |
| `SIMQ_ERDREICH_MSG_SPREIZUNG` | Bitte eine nutzbare Spreizung größer als 0 K eintragen!\nSie ist Eingangsgröße der Frostprüfung der Quelle. | Please enter a usable temperature spread greater than 0 K!\nIt is an input variable for the frost check of the source. | Form_QuelleErdreich.cs:608 |
| `SIMQ_ERDREICH_MSG_TIEFE_MAX` | Ein Erdkollektor wird nicht tiefer als 10 m verlegt.\nFür größere Tiefen das Quellsystem 'Erdsonde' wählen. | A horizontal ground collector is not installed deeper than 10 m.\nFor greater depths select the source system 'borehole heat exchanger'. | Form_QuelleErdreich.cs:564 |
| `SIMQ_ERDREICH_MSG_TIEFE_NULL` | Die Verlegetiefe muss größer als 0 m sein! | The installation depth must be greater than 0 m! | Form_QuelleErdreich.cs:559 |
| `SIMQ_ERDREICH_MSG_ZAHL_KOLLEKTOR` | Bitte gültige Zahlenwerte für Verlegetiefe und Fläche eintragen! | Please enter valid numeric values for installation depth and area! | Form_QuelleErdreich.cs:554 |
| `SIMQ_ERDREICH_MSG_ZAHL_SONDE` | Bitte gültige Zahlenwerte für Sondenlänge und Anzahl eintragen! | Please enter valid numeric values for BHE length and number! | Form_QuelleErdreich.cs:585 |
| `SIMQ_ERDREICH_OHNE_KLIMADATEN` |    (ohne Klimadaten — Ersatzwerte 9,5 °C / 8,5 K) |    (without climate data — fallback values 9,5 °C / 8,5 K) | Form_QuelleErdreich.cs:478 |
| `SIMQ_ERDREICH_PRUEFUNG_KEIN_LAUF` | (noch kein Simulationslauf)\n\nDie Prüfung braucht maximale Entzugsleistung, Jahresentzugsarbeit und\nJahresvolllaststunden aus einem Simulationslauf. | (no simulation run yet)\n\nThe check requires maximum extraction rate, annual extracted energy and\nannual full-load hours from a simulation run. | Form_QuelleErdreich.cs:491 |
| `SIMQ_ERDREICH_RB_KOLLEKTOR` | Erdkollektor | Horizontal ground collector | Form_QuelleErdreich.cs:157 |
| `SIMQ_ERDREICH_RB_SONDE` | Erdsonde | Borehole heat exchanger | Form_QuelleErdreich.cs:164 |
| `SIMQ_ERDREICH_SPEICHERLADUNG` | Entzugsarbeit und Spitze enthalten die Wärme, mit der die Wärmepumpe den Pufferspeicher lädt. | Extracted energy and peak include the heat with which the heat pump charges the buffer storage. | Form_Simulation_Config.Uebersicht.cs:1458 |
| `SIMQ_ERDREICH_SPREIZUNG` | Nutzbare Spreizung [K]: | Usable temperature spread [K]: | Form_QuelleErdreich.cs:243 |
| `SIMQ_ERDREICH_SPREIZUNG_HINWEIS` | (Quelleintritt minus Quellaustritt; Warnung, wenn Quelltemperatur − Spreizung dauerhaft unter 0 °C liegt) | (Source inlet minus source outlet; warning if source temperature − temperature spread is permanently below 0 °C) | Form_QuelleErdreich.cs:252 |
| `SIMQ_ERDREICH_TITEL` | Wärmequelle Erdreich | Ground heat source | Form_QuelleErdreich.cs:137, Form_QuelleErdreich.cs:545 |
| `SIMQ_ERDREICH_TITEL_MIT_WP` | Wärmequelle Erdreich — {0} | Ground heat source — {0} | Form_QuelleErdreich.cs:379 |
| `SIMQ_ERDREICH_UNWIRKSAM_LUFT_WASSER` | Die Wärmepumpe ist eine Luft-Wasser-Anlage — die Erdreich-Konfiguration bleibt in der Simulation unwirksam (gerechnet wird mit der Außenluft). Für eine Erdreich-Quelle eine Sole-Wasser- oder Wasser-Wasser-Wärmepumpe wählen. | The heat pump is an air-to-water unit — the ground configuration has no effect in the simulation (outdoor air is used). For a ground heat source, select a brine-to-water or water-to-water heat pump. | ErdreichAuswertung.cs:329 |
| `SIMQ_ERDREICH_VERLEGETIEFE` | Verlegetiefe [m]: | Installation depth [m]: | Form_QuelleErdreich.cs:171 |
| `SIMQ_ERDREICH_WIRKUNGSLOS` | Diese Konfiguration bleibt wirkungslos:\n\n{0} | This configuration remains without effect:\n\n{0} | Form_Simulation_Config.Uebersicht.cs:1447 |
| `SIMQ_ERDREICH_ZONE_NICHT_ZUGEORDNET` | 0 — nicht zugeordnet | 0 — not assigned | Form_QuelleErdreich.cs:225 |
| `SIMQ_ERDSONDE_ANZEIGE` | Erdsonde {0}×{1} m | Borehole heat exchanger {0}×{1} m | Form_Simulation_Config.Uebersicht.cs:798 |
| `SIMQ_FROST_NORMBASIS` | VDI 4640 Bl. 2 bemisst gegen −5 °C Soleaustritt | VDI 4640 part 2 is dimensioned against a brine outlet of −5 °C | ErdreichAuswertung.cs:87 |
| `SIMQ_FROSTTEXT` | Hinweis: Quelltemperatur − Spreizung liegt in {0} von {1} Betriebsstunden unter 0 °C ({2}; die Auslegungsprüfung bleibt davon unberührt). | Note: source temperature − temperature spread is below 0 °C in {0} of {1} operating hours ({2}; the design check is not affected by this). | ErdreichAuswertung.cs:191 |
| `SIMQ_INKL_SPEICHERLADUNG` |  (inkl. Speicherladung) |  (incl. storage charging) | ErdreichAuswertung.cs:168 |
| `SIMQ_KONSTANT_DIALOG_TEXT` | Quelltemperatur der Wärmepumpe\n'{0}' [°C]: | Source temperature of the heat pump\n'{0}' [°C]: | Form_Simulation_Config.Uebersicht.cs:1325 |
| `SIMQ_KONSTANT_DIALOG_TITEL` | Konstante Quelltemperatur | Constant source temperature | Form_Simulation_Config.Uebersicht.cs:1324 |
| `SIMQ_MSG_LUFT_WASSER` | Für Luft-Wasser-Wärmepumpen ist die Wärmequelle immer die Außenluft\n(Außentemperatur der gewählten Klimaregion).\n\nWP-Typ: {0} | For air-to-water heat pumps the heat source is always the outdoor air\n(outdoor temperature of the selected climate region).\n\nHP type: {0} | Form_Simulation_Config.Uebersicht.cs:1198 |
| `SIMQ_MSG_QUELLE_NUR_WP` | Eine Wärmequelle hat nur die Wärmepumpe.\n\nHeizkessel, BHKW und Solarthermie erzeugen ihre Wärme selbst; ihre\nEinsatzgrenzen stehen in den jeweiligen Eingabemasken. | Only the heat pump has a heat source.\n\nBoilers, CHP units and solar thermal generate their heat themselves; their\noperating limits are in the respective input forms. | Form_Simulation_Config.Uebersicht.cs:1188 |
| `SIMQ_PROFIL_KENNWERTE_ZEILE` | min {0} °C ({1})  ·  max {2} °C ({3})  ·  Mittel {4} °C | min {0} °C ({1})  ·  max {2} °C ({3})  ·  mean {4} °C | ErdreichTemperatur.cs:457 |
| `SIMQ_PRUEFZEILE_ENTZUGSENERGIE` | Entzugsenergie | Extraction energy | VDI4640Pruefung.cs:386 |
| `SIMQ_PRUEFZEILE_ENTZUGSLEISTUNG` | Entzugsleistung | Extraction rate | VDI4640Pruefung.cs:377, VDI4640Pruefung.cs:466 |
| `SIMQ_PRUEFZEILE_FORMAT` | {0} {1}   Grenze {2} {3}{4} | {0} {1}   Limit {2} {3}{4} | VDI4640Pruefung.cs:270 |
| `SIMQ_PUFFER_CB_UNBEGRENZT` | Quelle unbegrenzt verfügbar (nur Temperatur maßgeblich) | Source available without limit (only temperature relevant) | Form_QuellePufferspeicher.cs:120 |
| `SIMQ_PUFFER_DATEN` | Speichertyp: {0}\nGesamtvolumen: {1} l\nBereitschaftsverluste: {2} kWh/24h | Storage type: {0}\nTotal volume: {1} l\nStandby losses: {2} kWh/24h | Form_QuellePufferspeicher.cs:214 |
| `SIMQ_PUFFER_GB_PARAMETER` | Parameter der Wärmequelle | Heat source parameters | Form_QuellePufferspeicher.cs:93 |
| `SIMQ_PUFFER_HINWEIS_QUELLWAERME` | Die Wärmepumpe entzieht dem Speicher je Stunde die Verdampferwärme (Wärmeproduktion − Stromaufnahme).\n\nIst der Speicher leer, wird die Leistung der Wärmepumpe begrenzt; die Regeneration lädt den Speicher laufend nach. | The heat pump extracts the evaporator heat (heat generated − electricity input) from the storage every hour.\n\nIf the storage is empty, the output of the heat pump is limited; the regeneration recharges the storage continuously. | Form_QuellePufferspeicher.cs:139 |
| `SIMQ_PUFFER_KAPAZITAET` | nutzbare Kapazität:\n{0} kWh | Usable capacity:\n{0} kWh | Form_QuellePufferspeicher.cs:248 |
| `SIMQ_PUFFER_KOPF` | Pufferspeicher als Wärmequelle auswählen: | Select buffer storage as heat source: | Form_QuellePufferspeicher.cs:66 |
| `SIMQ_PUFFER_MSG_AUSWAHL` | Bitte einen Pufferspeicher auswählen! | Please select a buffer storage! | Form_QuellePufferspeicher.cs:255 |
| `SIMQ_PUFFER_MSG_KEINE_SPEICHER` | Es sind keine Pufferspeicher in den Stammdaten vorhanden! | There is no buffer storage in the master data! | Form_QuellePufferspeicher.cs:189 |
| `SIMQ_PUFFER_MSG_SPREIZUNG` | Die nutzbare Spreizung muss größer als 0 K sein! | The usable temperature spread must be greater than 0 K! | Form_QuellePufferspeicher.cs:274 |
| `SIMQ_PUFFER_QUELLTEMPERATUR` | Quelltemperatur [°C]: | Source temperature [°C]: | Form_QuellePufferspeicher.cs:99 |
| `SIMQ_PUFFER_REGENERATION` | Regeneration [kW]: | Regeneration [kW]: | Form_QuellePufferspeicher.cs:107 |
| `SIMQ_PUFFER_SPREIZUNG` | nutzbare Spreizung [K]: | Usable temperature spread [K]: | Form_QuellePufferspeicher.cs:103 |
| `SIMQ_PUFFER_TITEL` | Wärmequelle Pufferspeicher | Buffer storage heat source | Form_QuellePufferspeicher.cs:57, Form_QuellePufferspeicher.cs:190, Form_QuellePufferspeicher.cs:255, Form_QuellePufferspeicher.cs:266, Form_QuellePufferspeicher.cs:275 |
| `SIMQ_PUFFER_TITEL_MIT_WP` | Wärmequelle Pufferspeicher - {0} | Buffer storage heat source - {0} | Form_QuellePufferspeicher.cs:174 |
| `SIMQ_QUELLE_AUSSENLUFT` | Außenluft | Outdoor air | Form_Simulation_Config.Uebersicht.cs:765, Form_Simulation_Config.Uebersicht.cs:778 |
| `SIMQ_QUELLE_CSVPROFIL` | CSV-Profil | CSV profile | Form_Simulation_Config.Uebersicht.cs:776 |
| `SIMQ_QUELLE_KONSTANT` | Konstant ({0} °C) | Constant ({0} °C) | Form_Simulation_Config.Uebersicht.cs:769 |
| `SIMQ_QUELLE_PUFFER_NAME` | Puffer: {0} | Buffer: {0} | Form_Simulation_Config.Uebersicht.cs:773 |
| `SIMQ_QUELLE_QUELLPROFIL` | Quellprofil | Source profile | Form_Simulation_Config.Uebersicht.cs:775, Form_Quellprofil.cs:239, Form_Quellprofil.cs:398, Form_Quellprofil.cs:426, Form_Quellprofil.cs:445, Form_Quellprofil.cs:458 |
| `SIMQ_QUELLPROFIL_BTN_ALLE_MONATE` | Alle Monate auf Januarwert setzen | Set all months to the January value | Form_Quellprofil.cs:230 |
| `SIMQ_QUELLPROFIL_BTN_ALLE_TAGE` | auf alle Tage übertragen | Apply to all days | Form_Quellprofil.cs:304 |
| `SIMQ_QUELLPROFIL_BTN_TAG_EINFUEGEN` | Tag einfügen | Paste day | Form_Quellprofil.cs:303 |
| `SIMQ_QUELLPROFIL_BTN_TAG_KOPIEREN` | Tag kopieren | Copy day | Form_Quellprofil.cs:302 |
| `SIMQ_QUELLPROFIL_BTN_UEBERNEHMEN` | Änderungen Übernehmen | Apply changes | Form_Quellprofil.cs:305 |
| `SIMQ_QUELLPROFIL_HINWEIS_ABWEICHUNG` | Hinweis: 0 = keine Abweichung (Quelltemperatur entspricht dem Monatswert). | Note: 0 = no deviation (source temperature equals the monthly value). | Form_Quellprofil.cs:321 |
| `SIMQ_QUELLPROFIL_INFO` | Quelltemperatur = Monatswert [°C] + Wochenwert [K].\nDie Monatswerte geben den Jahresgang vor, die Wochenwerte den Tages-/Wochengang. | Source temperature = monthly value [°C] + weekly value [K].\nThe monthly values define the annual variation, the weekly values the daily/weekly variation. | Form_Quellprofil.cs:144 |
| `SIMQ_QUELLPROFIL_KOPF_MONAT` | Monats-Mitteltemperatur der Wärmequelle [°C] | Monthly mean temperature of the heat source [°C] | Form_Quellprofil.cs:189 |
| `SIMQ_QUELLPROFIL_KOPF_WOCHE` | Abweichung vom Monatswert je Stunde [K] | Deviation from the monthly value per hour [K] | Form_Quellprofil.cs:256 |
| `SIMQ_QUELLPROFIL_LBL_WOCHENTAG` | Auswahl Wochentag | Weekday selection | Form_Quellprofil.cs:290 |
| `SIMQ_QUELLPROFIL_MSG_ALLE_TAGE` | Der Tagesgang wurde auf alle Wochentage übertragen. | The daily variation has been applied to all weekdays. | Form_Quellprofil.cs:445 |
| `SIMQ_QUELLPROFIL_MSG_ERST_KOPIEREN` | Bitte zuerst einen Tag kopieren! | Please copy a day first! | Form_Quellprofil.cs:426 |
| `SIMQ_QUELLPROFIL_MSG_JANUAR` | Bitte im Feld Januar eine gültige Zahl eintragen! | Please enter a valid number in the January field! | Form_Quellprofil.cs:239 |
| `SIMQ_QUELLPROFIL_MSG_MONAT_UNGUELTIG` | {0}: '{1}' ist keine gültige Zahl! | {0}: '{1}' is not a valid number! | Form_Quellprofil.cs:457 |
| `SIMQ_QUELLPROFIL_MSG_STUNDE_UNGUELTIG` | Stunde {0}: '{1}' ist keine gültige Zahl! | Hour {0}: '{1}' is not a valid number! | Form_Quellprofil.cs:397 |
| `SIMQ_QUELLPROFIL_TAB_GRAFIK` | Grafik | Chart | Form_Quellprofil.cs:332 |
| `SIMQ_QUELLPROFIL_TAB_MONATSWERTE` | Monatswerte | Monthly values | Form_Quellprofil.cs:185 |
| `SIMQ_QUELLPROFIL_TAB_WOCHENWERTE` | Wochenwerte | Weekly values | Form_Quellprofil.cs:252 |
| `SIMQ_QUELLPROFIL_TITEL` | Quellprofil Wärmequelle | Source profile of the heat source | Form_Quellprofil.cs:132 |
| `SIMQ_QUELLPROFIL_TITEL_MIT_WP` | Quellprofil Wärmequelle - {0} | Source profile of the heat source - {0} | Form_Quellprofil.cs:116 |
| `SIMQ_SPALTE_QUELLE` | Quelle | Source | Form_Simulation_Config.Uebersicht.cs:223 |
| `SIMQ_SPITZE_AUS_SUMMENGANGLINIE` |  (Spitze anteilig aus der Summenganglinie) |  (peak apportioned from the aggregated load profile) | ErdreichAuswertung.cs:175 |
| `SIMQ_TIP_QUELLE` | Wärmequelle (Doppelklick zum Ändern)\nLuft-Wasser: immer Außenluft aus den Klimadaten.\nSole-/Wasser-Wasser: Erdreich, Konstante Temperatur, Pufferspeicher,\nQuellprofil (Monats- und Wochenwerte) oder CSV-Datei. | Heat source (double-click to change)\nAir-to-water: always outdoor air from the climate data.\nBrine-/water-to-water: ground, constant temperature, buffer storage,\nsource profile (monthly and weekly values) or CSV file. | Form_Simulation_Config.Uebersicht.cs:1064 |
| `SIMQ_TIP_QUELLE_NICHT_WP` | Eine Wärmequelle hat nur die Wärmepumpe.\nHeizkessel, BHKW und Solarthermie erzeugen die Wärme selbst. | Only the heat pump has a heat source.\nBoilers, CHP units and solar thermal generate the heat themselves. | Form_Simulation_Config.Uebersicht.cs:1068 |
| `SIMQ_TITEL_WAERMEQUELLE` | Wärmequelle | Heat source | Form_Simulation_Config.Uebersicht.cs:1192, Form_Simulation_Config.Uebersicht.cs:1201 |
| `SIMQ_TYP_AUSSENLUFT` | Außenluft (Klimadaten) | Outdoor air (climate data) | WaermequelleClass.cs:72 |
| `SIMQ_TYP_CSV_DATEI` | CSV-Datei (Stundenwerte) | CSV file (hourly values) | WaermequelleClass.cs:76 |
| `SIMQ_TYP_ERDREICH` | Erdreich (VDI 4640) | Ground (VDI 4640) | WaermequelleClass.cs:77 |
| `SIMQ_TYP_KONSTANTE_TEMPERATUR` | Konstante Temperatur | Constant temperature | WaermequelleClass.cs:73 |
| `SIMQ_TYP_PUFFERSPEICHER` | Pufferspeicher | Buffer storage | WaermequelleClass.cs:74, Form_Simulation_Config.cs:55, Form_Simulation_Config.Uebersicht.cs:773, Form_PufferSp_Projekt.cs:696, Form_PufferSp_Projekt.cs:716, Form_PufferSp_Projekt.cs:737, Form_PufferSp_Projekt.cs:823 |
| `SIMQ_TYP_QUELLPROFIL` | Quellprofil (Monatswerte) | Source profile (monthly values) | WaermequelleClass.cs:75 |
| `SIMQ_VDI4640_AUSSERHALB_TABELLE` |  Achtung: Sondenzahl bzw. λ liegen außerhalb des kodierten Tabellenbereichs (B2-Auszug); der Grenzwert wurde auf die Randstützstelle geklemmt. Auf der Sondenzahl-Achse ist das nicht konservativ - größere Sondenfelder brauchen kleinere spezifische Entzugsleistungen, als der Randwert zulässt. |  Caution: the number of boreholes or λ lies outside the coded table range (B2 extract); the limit was clamped to the boundary data point. On the borehole-number axis this is not conservative - larger borehole fields require lower specific extraction rates than the boundary value allows. | VDI4640Pruefung.cs:484 |
| `SIMQ_VDI4640_EINGEHALTEN` | VDI 4640: eingehalten. | VDI 4640: complied with. | ErdreichAuswertung.cs:173 |
| `SIMQ_VDI4640_GRENZWERT_UEBERSCHRITTEN` | VDI 4640: Grenzwert überschritten — Quelle zu klein bemessen! | VDI 4640: limit exceeded — heat source is undersized! | ErdreichAuswertung.cs:172 |
| `SIMQ_VDI4640_GRUNDLAGE_KOLLEKTOR` | Klimazone {0}, Bodenart {1} | Climate zone {0}, soil type {1} | VDI4640Pruefung.cs:396 |
| `SIMQ_VDI4640_GRUNDLAGE_SONDE` | λ = {0} W/(m·K), {1} Sonde(n), {2} h/a | λ = {0} W/(m·K), {1} borehole(s), {2} h/a | VDI4640Pruefung.cs:476 |
| `SIMQ_VDI4640_KEINE_KOLLEKTORFLAECHE` | Keine Kollektorfläche angegeben, Prüfung nicht möglich. | No collector area specified, check not possible. | VDI4640Pruefung.cs:361 |
| `SIMQ_VDI4640_KEINE_SONDENLAENGE` | Keine Sondenlänge angegeben, Prüfung nicht möglich. | No borehole length specified, check not possible. | VDI4640Pruefung.cs:449 |
| `SIMQ_VDI4640_KEINE_VOLLLASTSTUNDEN` | Keine Jahresvolllaststunden bekannt, Prüfung nicht möglich. | Annual full-load hours not known, check not possible. | VDI4640Pruefung.cs:455 |
| `SIMQ_VDI4640_KLIMAZONE_FEHLT` | Klimazone nicht zugeordnet, Prüfung nicht möglich. | Climate zone not assigned, check not possible. | VDI4640Pruefung.cs:355 |
| `SIMQ_VDI4640_KOLLEKTOR_OK` | Auslegung liegt innerhalb der Grenzwerte der Tabelle A2. | The design is within the limits of table A2. | VDI4640Pruefung.cs:401 |
| `SIMQ_VDI4640_KOLLEKTOR_ZU_KLEIN` | Kollektor ist zu klein bemessen. Erforderlich sind mindestens {0} m² (Zonen-Volllaststunden {1} h/a). | The collector is undersized. At least {0} m² are required (zone full-load hours {1} h/a). | VDI4640Pruefung.cs:398 |
| `SIMQ_VDI4640_PRUEFUNG_NICHT_MOEGLICH` | Auslegungsprüfung nach VDI 4640 nicht möglich — {0} | Design check to VDI 4640 not possible — {0} | ErdreichAuswertung.cs:164 |
| `SIMQ_VDI4640_SONDE_OK` | Auslegung liegt innerhalb der Grenzwerte der Tabelle B2 (Auszug). | The design is within the limits of table B2 (extract). | VDI4640Pruefung.cs:481 |
| `SIMQ_VDI4640_SONDENFELD_ZU_KLEIN` | Sondenfeld ist zu klein bemessen. Erforderlich sind mindestens {0} Sondenmeter. | The borehole field is undersized. At least {0} borehole metres are required. | VDI4640Pruefung.cs:479 |
| `SIMQ_WPTYP_NICHT_GEPFLEGT` | (nicht gepflegt) | (not maintained) | Form_Simulation_Config.Uebersicht.cs:1200 |

## Zusammengeführte Schlüssel

Diese Schlüssel wurden bei der Zusammenführung der Teilkataloge aufgegeben, weil ihr
deutscher Text bereits unter einem anderen Schlüssel geführt wird:

| aufgegeben | gilt jetzt |
|---|---|
| `SIM_HEIZKESSEL` | `SIM_ERZEUGERNAME_HEIZKESSEL` |
| `SIM_BHKW` | `SIM_ERZEUGERNAME_BHKW` |
| `PSP_SPALTE_SPEICHER` | `PSP_BEZEICHNER_ERSATZ` |
| `SIM_WAERMEPUMPE` | `SIM_ERZEUGERNAME_WAERMEPUMPE` |
| `SIM_SOLARTHERMIE` | `SIM_ERZEUGERNAME_SOLARTHERMIE` |
| `CHART_SEGMENT_WAERMEPUMPE` | `SIM_ERZEUGERNAME_WAERMEPUMPE` |
| `CHART_SEGMENT_HEIZKESSEL` | `SIM_ERZEUGERNAME_HEIZKESSEL` |
| `CHART_SEGMENT_BHKW` | `SIM_ERZEUGERNAME_BHKW` |
| `CHART_ACHSE_LEISTUNG_KW` | `SIM_SPALTE_LEISTUNG` |
| `SIM_BHKW_STANDARDNAME` | `SIM_BHKW_MODUL_STANDARD` |
| `SIM_HEIZSTAB` | `CHART_SEGMENT_HEIZSTAB` |
| `CHART_SEGMENT_SOLARTHERMIE` | `SIM_ERZEUGERNAME_SOLARTHERMIE` |
| `CHART_SEGMENT_PHOTOVOLTAIK` | `SIM_PHOTOVOLTAIK` |
| `PSP_SPALTE_PUFFERSPEICHER` | `SIMQ_TYP_PUFFERSPEICHER` |
| `SIM_TITEL_NICHT_VERFUEGBAR` | `SIM_TITEL_SIMULATION_NICHT_VERFUEGBAR` |
| `SIM_SPALTE_ERZEUGER` | `SIM_ERZEUGERNAME_ALLGEMEIN` |
| `SIM_SPALTE_ZWEITSENKE` | `SIM_ROLLE_ZWEITSENKE` |
| `SIMQ_QUELLE_PUFFERSPEICHER` | `SIMQ_TYP_PUFFERSPEICHER` |
| `SIM_GB_HAUPTSENKE` | `SIM_ROLLE_HAUPTSENKE` |
| `SIM_ZIEL_PUFFER_HEIZUNG` | `SIM_ZIEL_PUFFERSPEICHER_HEIZUNG` |
| `SIM_ZIEL_PUFFER_BRAUCHWASSER` | `SIM_ZIEL_PUFFERSPEICHER_BRAUCHWASSER` |
| `SIM_LBL_PUFFER2` | `PSP_RUBRIK_LABEL` |
| `SIMQ_BTN_OK` | `SIM_BTN_OK` |
| `SIMQ_BTN_ABBRECHEN` | `SIM_BTN_ABBRECHEN` |
| `SIMQ_QUELLPROFIL_MSG_TITEL` | `SIMQ_QUELLE_QUELLPROFIL` |
| `SIMQ_PUFFER_MSG_ZAHLEN` | `PSP_MSG_ZAHLENWERTE` |
| `PSP_GRUPPE_PUFFER_IM_PROJEKT` | `PSP_PROJEKT_FENSTERTITEL` |
| `PSP_SPALTE_ANLAGE` | `SIM_SPALTE_ANLAGE` |
| `PSP_SPALTE_ERZEUGER` | `SIM_ERZEUGERNAME_ALLGEMEIN` |
| `PSP_SPALTE_SENKE` | `SIM_SPALTE_SENKE` |
| `PSP_SENKE_ZWEITSENKE` | `SIM_ROLLE_ZWEITSENKE` |
| `PSP_SENKE_HAUPTSENKE` | `SIM_ROLLE_HAUPTSENKE` |
| `PSP_TITEL_PUFFERSPEICHER` | `SIMQ_TYP_PUFFERSPEICHER` |
