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
| `SIM_CHK_SORTIERT` | sortiert | sorted | NavigatorWaerme.cs (`checkBox_Sortiert`, programmatisch) | **neu (Sichttest „nur vorhandene Komponenten").** Umschalter Jahresganglinie ↔ Jahresdauerlinie. Wortgleich mit der Designer-Checkbox `checkBox_WP_sortiert` in `Form_Simulation_Detail` (dort über die Satelliten-.resx der Form übersetzt: „sortiert"/„sorted"); die Checkbox in NavigatorWaerme entsteht programmatisch und braucht deshalb einen Katalogschlüssel. |
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

## Nachträge aus der Nacharbeit zu den Paket-9-Reviews (15.08.2026)

Die Nacharbeit hat **41 bereits übersetzte, aber nicht verdrahtete Schlüssel angeschlossen**
(Befund N1), den `ChartManager` in den Lokalisierungsumfang genommen (N2) und den Katalog
bereinigt. Bestand jetzt **545 Schlüssel** (541 + 4 neu − 2 tot + 2 wieder aufgenommen).

### Neu (4)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `CHART_TOOLTIP_EINHEIT` | Einheit: {0} | Unit: {0} | ChartManager.cs (`ChartMouseWheel2.HandleMouseMove`) | **neu.** Mouseover-Text der Zahlenachse; stand als Literal im Quelltext. Formatangabe `{0:0}` bleibt im Quelltext. |
| `CHART_TOOLTIP_WERT` | Wert: {0} {1} | Value: {0} {1} | ChartManager.cs (`ChartMouseWheel2.HandleMouseMove`) | **neu.** Wie oben; `{0}` = Zahlwert (Format `N2` im Quelltext), `{1}` = Einheit (sprachneutral, aus `szToolTipUnit`). |
| `SIM_GRUPPE_HAUPTSENKE` | Hauptsenke | Main sink | Form_Waermesenke.cs:113, Form_PufferSp_Projekt.cs:634 | **neu.** Beschriftung in Sentence case. `SIM_ROLLE_HAUPTSENKE` („main sink") bleibt die klein geschriebene **Satzform** für den Einsatz als Platzhalter in Meldungen. |
| `SIM_SPALTE_ZWEITSENKE` | Zweitsenke | Secondary sink | Form_Simulation_Config.Uebersicht.cs:225, Form_PufferSp_Projekt.cs:633 | **neu.** Wie oben, für Spaltenkopf und Tabellenzelle; `SIM_ROLLE_ZWEITSENKE` bleibt die Satzform. |

> Damit sind zwei in Etappe 1 zusammengeführte Schlüssel **wieder aufgenommen** (Tabelle
> „Zusammengeführte Schlüssel" am Ende dieser Datei). Die Zusammenführung war eine reine
> Wortlaut-Gleichheit im Deutschen — im Englischen trennt sich Beschriftung („Main sink")
> von der Satzform („… assign a buffer storage to the main sink"), und genau dafür braucht es
> zwei Schlüssel.

### Entfernt (2, repo-weit ohne Referenz)

| Schlüssel | DE | EN |
|---|---|---|
| `Text_Ausgewaehlt` | ausgewählt | selected |
| `Text_nicht_geoffnet` | nicht geöffnet | not opened |

Vor dem Entfernen repo-weit gesucht (alle Dateitypen, ohne `bin/`, `obj/`, Vollkopien):
**0 Fundstellen** außerhalb von `Resource.resx`, `Resource.en-US.resx` und
`Resource.Designer.cs`.

### Englische Werte berichtigt (2)

| Schlüssel | EN alt | EN neu | Grund |
|---|---|---|---|
| `Text_Hinweis` | Hint | Note | Glossar Kapitel 8: „Hinweis → Note (MessageBox-Titel)". Alle drei Fundstellen (`Form_Start.cs:820, 1176, 1475`) verwenden den Schlüssel als **Titel** eines `Form_Hinweis` — genau der Glossarfall. |
| `SIM_SPALTE_PRIO` | Prio | prio | Angleichung an die in Protokoll 5.5 festgehaltene Entscheidung „Prio / WP-Prio → prio / HP prio" und an das bereits so ausgelieferte `SIM_SPALTE_WPPRIO` („HP prio"). |

### Fundstellen jetzt verdrahtet (41 Schlüssel, Befund N1)

Diese Schlüssel lagen seit L2 mit deutschem und englischem Wert im Katalog, im Quelltext stand
aber weiter das Literal. Sie sind jetzt angeschlossen; die Zeilennummern der Tabellen unten
sind dadurch verschoben, die Zuordnung Schlüssel → Datei bleibt.

| Datei | Schlüssel | Anzahl |
|---|---|---:|
| `ErdreichTemperatur.cs` | `SIMQ_BODENTYP_*` (13) — über das neue Feld `Bodenkennwerte.AnzeigeSchluessel`, aufgelöst in der Eigenschaft `Untergrund` | 13 |
| `ErdreichTemperatur.cs` | `SIMQ_PROFIL_KENNWERTE_ZEILE` in `Kennwerte.Zeile()` | 1 |
| `VDI4640Pruefung.cs` | `SIMQ_PRUEFZEILE_FORMAT`, `SIMQ_PRUEFZEILE_ENTZUGSLEISTUNG` (2 Fundstellen), `SIMQ_PRUEFZEILE_ENTZUGSENERGIE`, `SIMQ_VDI4640_KLIMAZONE_FEHLT`, `_KEINE_KOLLEKTORFLAECHE`, `_KEINE_SONDENLAENGE`, `_KEINE_VOLLLASTSTUNDEN`, `_GRUNDLAGE_KOLLEKTOR`, `_GRUNDLAGE_SONDE`, `_KOLLEKTOR_OK`, `_KOLLEKTOR_ZU_KLEIN`, `_SONDE_OK`, `_SONDENFELD_ZU_KLEIN`, `_AUSSERHALB_TABELLE` | 14 |
| `ErdreichAuswertung.cs` | `SIMQ_ERDREICH_KURZTEXT_KOPF`, `SIMQ_ERDREICH_ENTZUG_KURZTEXT`, `SIMQ_INKL_SPEICHERLADUNG`, `SIMQ_SPITZE_AUS_SUMMENGANGLINIE`, `SIMQ_VDI4640_EINGEHALTEN`, `SIMQ_VDI4640_GRENZWERT_UEBERSCHRITTEN`, `SIMQ_VDI4640_PRUEFUNG_NICHT_MOEGLICH`, `SIMQ_FROSTTEXT`, `SIMQ_FROST_NORMBASIS`, `SIMQ_ANLAGE_ERSATZNAME`, `SIMQ_ERDREICH_UNWIRKSAM_LUFT_WASSER`, `SIMQ_ENTZUG_NICHT_JE_MODUL_TRENNBAR`, `SIMQ_ENTZUG_ANTEILIG_GESCHAETZT` | 13 |

**Zwei Zeichenketten bleiben an diesen Stellen bewusst deutsch** — beide sind nach der
Drei-Schichten-Regel keine reine Anzeige:

| Stelle | Text | Grund |
|---|---|---|
| `VDI4640Pruefung.Bodenarten` (`BODENART_SAND`, `_SANDIGER_TON`, `_LEHM`, `_SCHLUFF`) | „Sand", „Sandiger Ton", „Lehm", „Schluff" | **Steuerwert.** `BodenartIndex()` sucht darüber die Spalte der Tabelle A2. Der Wert erscheint als Platzhalter `{1}` in `SIMQ_VDI4640_GRUNDLAGE_KOLLEKTOR` und als `{4}` in `SIMQ_ERDREICH_BODENKENNWERTE`; die englische Meldung mischt dort die Sprachen. Auflösung wäre eine eigene Anzeigefunktion — Folgepaket. |
| `ErdreichTemperatur.MONATSKUERZEL` | „Jan"…„Dez" | **Monatsname.** Der Katalog nimmt Monats- und Wochentagsnamen ausdrücklich aus (Lesehinweis oben); `CultureInfo` liefert im Deutschen „Mrz" statt „Mär" und würde die deutsche Anzeige verändern. Siehe Protokoll, Abschnitt 25.7. |

## Nachträge aus dem Sichttest — Ergebnisdiagramm der Heizkessel-Seite (15.08.2026)

Die Heizkessel-Seite der Detailansicht hat ein Diagramm „Wärmelast Jahresganglinie" mit
Umschalter „sortiert" und CSV-Ausgabe bekommen — aufgebaut wie die Wärmepumpen-Seite. Für die
**Anzeige** war kein neuer Schlüssel nötig (Titel, Achsen und Legenden decken die
bestehenden `CHART_*`-Schlüssel ab, siehe Mehrfachnutzung unten); die vier neuen Schlüssel
gehören ausschließlich zur CSV-Ausgabe und ihrer Meldung.

**Zum Fenstertitel:** `Form_Simulation_Detail.resx` (`$this.Text`) trug den Tippfehler
„Detailierte Simulation"; berichtigt zu „**Detaillierte** Simulation". Der englische Wert in
der Satelliten-.resx („Detailed simulation") war bereits richtig, eine `de-DE`-Fassung des
Titels gibt es nicht — die neutrale .resx ist die deutsche Anzeige.

### Neu (4)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `CHART_CSV_RESTWAERME` | Restwärme [kW] | Residual heat [kW] | Form_Simulation_Detail.cs (`btn_CsvExportKessel_Click`) | **neu.** Spaltenkopf der CSV-Ausgabe. Nicht zu verwechseln mit `CHART_SEGMENT_RESTWAERME` („Restwärme", Legenden- und Segmenttext ohne Einheit). |
| `CHART_DATEI_HEIZKESSEL` | Heizkessel_Projekt_{0}.csv | Boiler_project_{0}.csv | Form_Simulation_Detail.cs (`btn_CsvExportKessel_Click`) | **neu.** Vorschlagsname der Exportdatei, Muster `CHART_DATEI_WAERMEPUMPE`; `{0}` = Projekt-ID. |
| `SIM_MSG_KEINE_DATEN_HEIZKESSEL` | Keine Simulationsdaten vorhanden!\nBitte zuerst die Simulation mit Heizkessel durchführen. | No simulation data available!\nPlease run the simulation with the boiler first. | Form_Simulation_Detail.cs (`btn_CsvExportKessel_Click`) | **neu.** Wortgleich zu `SIM_MSG_KEINE_DATEN_WAERMEPUMPE`, nur mit dem Erzeuger ausgetauscht. |
| `SIM_TOOLTIP_CSV_HEIZKESSEL` | Heizkessel-Simulation als CSV exportieren\n(Zeitstempel, Außentemperatur, Wärmebedarf, Heizkessel, Restwärme) | Export boiler simulation as CSV\n(time stamp, outdoor temperature, heat demand, boiler, residual heat) | Form_Simulation_Detail.cs (`InitCsvExportButtons`) | **neu.** Muster `SIM_TOOLTIP_CSV_WAERMEPUMPE`; die Klammer nennt die Spalten der Datei. |

**Mehrfachnutzung bestehender Schlüssel** — das neue Diagramm kommt ohne eigene Anzeigetexte aus:

| Schlüssel | zusätzliche Verwendung |
|---|---|
| `CHART_TITEL_WAERMELAST_JAHRESGANGLINIE` | Titel des Kessel-Diagramms (`chart_Kessel`) |
| `CHART_ACHSE_WAERMELAST` | Y-Achse ebenda |
| `CHART_ACHSE_JAHRESSTUNDEN`, `CHART_ACHSE_MONATE` | X-Achse ebenda, je nach Stellung von „sortiert" |
| ~~`CHART_LEGENDE_WAERMEBEDARF`~~ | Bedarfsfläche ebenda (Stufeneingang der Kessel) — **abgelöst am 16.08.2026** durch `CHART_LEGENDE_WAERMEBEDARF_GESAMT`, siehe Nachtrag am Dateiende |
| ~~`CHART_LEGENDE_WAERMEPRODUKTION`~~ | Produktionssäulen ebenda — **abgelöst am 16.08.2026** durch `CHART_LEGENDE_WAERMEPRODUKTION_HEIZKESSEL` |
| `CHART_SEGMENT_RESTWAERME` | Linie „Restwärme" ebenda |
| `SIM_CHK_SORTIERT` | Umschalter `checkBox_Kessel_sortiert` (programmatisch, wie in NavigatorWaerme) |
| `SIM_BTN_CSV_EXPORT` | Beschriftung des dritten Export-Knopfes |
| ~~`CHART_CSV_WAERMEBEDARF`~~, `CHART_CSV_HEIZKESSEL` | Spaltenköpfe derselben CSV-Ausgabe; die Bedarfsspalte heißt seit 16.08.2026 `CHART_CSV_WAERMEBEDARF_KESSELSTUFE`, daneben steht neu `CHART_CSV_WAERMEBEDARF_GESAMT` |

## Nachträge aus Etappe E0 — Quellpuffer-Dialog (15.08.2026)

`Form_QuellePufferspeicher` listet seit Etappe E0 die **Projekt**-Puffer statt der
STAMM-Speicher und liefert die Puffer-ID zurück (Konzept `Konzept_KonfigUI_Hydraulik`,
Abschnitt 4). Dafür braucht der Dialog vier neue Texte: zwei Listenformate, ein
Detailformat und den Hinweis für ein Projekt ohne Pufferspeicher. Alle vier sind in beiden
`.resx` und in `Resource.Designer.cs` nachgezogen. Wie bei den übrigen Nachträgen stehen sie
nur hier und nicht in der Etappe-1-Inventarliste weiter unten (die bleibt der Stand von L2);
die SIMQ-Gruppe umfasst damit 142 Schlüssel.

### Neu (4)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `SIMQ_PUFFER_LISTE_EINTRAG` | {0} — {1}, {2} l, {3}/{4} °C | {0} — {1}, {2} l, {3}/{4} °C | Form_QuellePufferspeicher.cs (`SpeicherItem.ToString`) | **neu.** Ein Listeneintrag der Auswahl: Bezeichner, Verwendung, Volumen, Vorlauf/Rücklauf. In beiden Sprachen gleich — der Eintrag besteht aus Werten, Einheiten und Symbolen. |
| `SIMQ_PUFFER_LISTE_OHNE_TEMP` | {0} — {1}, {2} l | {0} — {1}, {2} l | Form_QuellePufferspeicher.cs (`SpeicherItem.ToString`) | **neu.** Kurzform für Puffer ohne gepflegtes Temperaturpaar; „0/0 °C" wäre eine Angabe, die es nicht gibt. |
| `SIMQ_PUFFER_DATEN_PROJEKT` | Verwendung: {0}\nGesamtvolumen: {1} l\nBereitschaftsverluste: {2} kWh/24h\nVorlauf/Rücklauf: {3} | Use: {0}\nTotal volume: {1} l\nStandby losses: {2} kWh/24h\nFlow/return: {3} | Form_QuellePufferspeicher.cs (`ZeigeSpeicherDaten`) | **neu.** Tritt an die Stelle von `SIMQ_PUFFER_DATEN` (Stammdaten: Speichertyp/Volumen/Verluste). Der Projekt-Puffer kennt keinen „Speichertyp", dafür Verwendung und Temperaturpaar. `{3}` ist bereits gesetzt („55/45 °C" bzw. „-"). |
| `SIMQ_PUFFER_HINWEIS_KEIN_PROJEKTPUFFER` | Das Projekt enthält noch keinen Pufferspeicher. Über „Pufferspeicher anlegen…" einen anlegen. | The project does not contain a buffer storage yet. Use "Create buffer storage…" to add one. | Form_QuellePufferspeicher.cs (`PufferListeLaden`, `btnOk_Click`) | **neu.** Leerer Zustand mit Handlungsanweisung, Muster `Form_Waermesenke`. Ersetzt die frühere Meldung über fehlende STAMM-Daten, die dem Anwender nicht sagte, was zu tun ist. |

**Mehrfachnutzung bestehender Schlüssel:**

| Schlüssel | zusätzliche Verwendung |
|---|---|
| `PSP_BTN_PUFFER_ANLEGEN` | Absprungknopf in `Form_QuellePufferspeicher` (bisher nur `Form_Waermesenke`) |
| `SIMQ_PUFFER_HINWEIS_KEIN_PROJEKTPUFFER` | zweite Fundstelle: Meldung beim OK ohne Auswahl im leeren Projekt |

**Ohne Referenz, aber belassen:** `SIMQ_PUFFER_MSG_KEINE_SPEICHER` („Es sind keine
Pufferspeicher in den Stammdaten vorhanden!") — die Meldung hing an der STAMM-Liste und hat
mit E0 keine Fundstelle mehr. Der Schlüssel bleibt bis zur Abnahme stehen; entfernt wird er
zusammen mit der übrigen Altlast in D2.

**Ohne Referenz durch Etappe D1:** `PSP_SPALTE_ZUORDNUNG_ALT` (Spaltenkopf „Zuordnung (alt)")
und `PSP_TIP_ZUORDNUNG_ALTMODELL` (ihr Mouseover-Hinweis) — die Spalte ist entfallen. Auch
diese beiden bleiben bis zur Abnahme stehen, weil die Alt-Zuordnung selbst noch besteht.

## Nachtrag Startseite — zurückgestellte Form_Start-Texte (15.08.2026)

Die elf beim EN-Sichttest zurückgestellten hartkodierten Texte in
`Views/Hauptformular/Form_Start.cs` (MessageBoxen und die Technologieliste auf dem Blatt
Simulation) sind auf den Katalog umgestellt. Sechs Schlüssel sind neu, der Rest ist
Mehrfachnutzung; alle sechs sind in beiden `.resx` und in `Resource.Designer.cs` nachgezogen.
Sie gehören zur `Text_Form_Start_*`-Altfamilie außerhalb der Etappe-1-Inventarliste.

### Neu (6)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `Text_Form_Start_ProjektGeloescht` | Das zuletzt geöffnete Projekt ist gelöscht! | The last opened project has been deleted! | Form_Start.cs (`pBox_ProjektZuletzt_Click`) | **neu.** |
| `Text_Form_Start_KlimaregionNichtGesetzt` | Die Klimaregion ist nicht gesetzt! Bitte setzen Sie die Klimaregion im Projekt! | The climate region is not set! Please set the climate region in the project! | Form_Start.cs (`tabPage5_Enter`) | **neu.** |
| `Text_Form_Start_KlimaregionAuswaehlen` | Bitte eine Klimaregion auswählen. | Please select a climate region. | Form_Start.cs (`btn_Speichern_Click`) | **neu.** Nicht dasselbe wie `SIM_MSG_KLIMAREGION_WAEHLEN` („Klimaregion auswählen!"). |
| `Text_Form_Start_KlimaregionNichtGefunden` | Die gewählte Klimaregion wurde nicht gefunden. | The selected climate region was not found. | Form_Start.cs (`btn_Speichern_Click`) | **neu.** |
| `Text_Form_Start_KlimaregionNichtUebernommen` | Die Klimaregion konnte nicht in das Projekt übernommen werden. | The climate region could not be applied to the project. | Form_Start.cs (`btn_Speichern_Click`) | **neu.** |
| `Text_Form_Start_KlimaregionGespeichert` | Klimaregion gespeichert. | Climate region saved. | Form_Start.cs (`btn_Speichern_Click`) | **neu.** |

**Mehrfachnutzung bestehender Schlüssel:**

| Schlüssel | zusätzliche Verwendung |
|---|---|
| `SIM_ERZEUGERNAME_HEIZKESSEL` / `SIM_ERZEUGERNAME_WAERMEPUMPE` / `SIM_STROMSPEICHER` / `SIM_ERZEUGERNAME_BHKW` | Technologieliste `label_Komponenten` auf dem Blatt Simulation (`tabPage5_Enter`). Reine Anzeige — die gleichlautenden DB-Werte in `DbWerte.cs` bleiben deutsch. |
| `Text_Hinweis` | Titel der vier Hinweis-MessageBoxen in `btn_Speichern_Click` |
| `SIM_TITEL_FEHLER` | Titel der Fehler-MessageBox in `btn_Speichern_Click` |
| `Text_Form_Start_MessageBox1` | „Projekt fehlt"-Meldung in `btn_Speichern_Click` (bisher „Bitte zuerst ein Projekt auswählen." hartkodiert — durch die Wiederverwendung endet der Satz jetzt auf „!") |

**Berichtigt:** `Text_Select` folgt jetzt Kapitel 8/11 des Glossars (Sentence case):
DE „bitte auswählen!" → „Bitte auswählen!", EN „please select!" → „Please select!".
Alle drei Fundstellen in `Form_Start.cs` setzen bzw. vergleichen über die Ressource,
der Wertwechsel ist deshalb gefahrlos.

## Nachtrag aus dem K-3-Folgeschritt — Bivalenztemperatur-Hinweis (15.08.2026)

Die K-3-Protokollmeldung „Bivalenztemperatur 0 °C" aus
`SimulationWaermepumpe.AlternativHinweisPruefen` — bisher als `KATALOG-KANDIDAT
(Lokalisierung)` markierter deutscher Festtext — ist in `MyResource` aufgenommen und
verdrahtet (beide `.resx` und `Resource.Designer.cs`). Im selben Zug nennt der
Meldungsschluss das Eingabefeld bei seinem neuen Namen: Die Beschriftung in
`Wizard_WPItem` heißt seit dem K-3-Folgeschritt „Bivalenztemperatur" (de-DE und neutral;
en-US „Bivalent temperature", Begriff aus EN 14825), nicht mehr „Abschalttemperatur".

### Neu (1)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `SIMENG_WP_BIVALENZTEMPERATUR_VORBELEGUNG` | Die Anlage '{0}' rechnet bivalent-alternativ mit einer Bivalenztemperatur von 0 °C — dem Vorbelegungswert des Eingabefelds. Unterhalb von 0 °C bleibt die Wärmepumpe aus und der zweite Wärmeerzeuger übernimmt allein. Ist das nicht beabsichtigt, die Bivalenztemperatur der Anlage pflegen. | The unit '{0}' calculates in bivalent-alternative mode with a bivalent temperature of 0 °C — the default value of the input field. Below 0 °C the heat pump remains off and the second heat generator takes over alone. If this is not intended, maintain the unit's bivalent temperature. | SimulationWaermepumpe.cs (`AlternativHinweisPruefen`) | **neu.** K-3-Hinweis (Protokoll `K3_BivalenzTemperatur_Protokoll.md`, Abschnitt 3); der Modul-Präfix kommt weiterhin aus `SIMENG_PRAEFIX_WAERMEPUMPE`, `{0}` = Anlagen-Bezeichner. |

**Weiterhin offener Kandidat desselben Moduls:** die `WarnungEinmal`-Meldung
„Quellspeicher … wird von mehreren Modulen benutzt" in
`SimulationWaermepumpe.QuellspeicherZusammenfuehren` steht unverändert als deutscher
Festtext im Code (wie die Quellspeicher-Hinweise in `WaermequelleClass.PufferZeile`).

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
| `SIM_KASKADE_SCHALTER` | Zweikanalige Kaskade | Two-channel cascade | Form_Simulation_Config.Uebersicht.cs:352 |
| `SIM_KASKADE_TOOLTIP` | Rechnet Heiz- und Warmwasserbedarf als getrennte Kanäle und löst die\nSpeicherladung aus der Erzeugerkaskade heraus.\n\nDas ÄNDERT die Ergebnisse: Anlagen mit Pufferspeicher als Senke laden\ndiesen, statt den Bedarf direkt zu decken; gedeckt wird aus dem Speicher.\nWas sich im Einzelnen ändert, steht im Umsetzungsprotokoll zu Paket 4\n(Teil 7, Dokumentierte Ergebnisaenderungen). Ohne Haken rechnet die\nbisherige, einkanalige Kaskade unverändert weiter.\n\nDer Haken wird automatisch gesetzt, sobald die Konfiguration Warmwasser\nund Heizwärme getrennt führt - dann wäre der einkanalige Weg blind für\nBrauchwasser-/Kombi-Senken und Quellbezüge. | Calculates space heating and domestic hot water demand as separate channels and\nseparates the storage charging from the generator cascade.\n\nThis CHANGES the results: units with a buffer storage as sink charge\nit instead of covering the demand directly; the demand is covered from the storage.\nWhat changes in detail is described in the implementation log for package 4\n(part 7, Documented result changes). Without the tick, the\nprevious single-channel cascade continues to calculate unchanged.\n\nThe tick is set automatically as soon as the configuration handles domestic\nhot water and space heating separately - the single-channel path would then\nbe blind to DHW/combi storage sinks and source references. | Form_Simulation_Config.Uebersicht.cs:363 |
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
| `SIM_MSG_BRAUCHWASSER_UEBERGANG` | Hinweis: Die Brauchwasser-/Kombi-Senke wird erst mit aktivierter zweikanaliger Kaskade wirksam (Schalter im Konfigurationsdialog).\nSie wird gespeichert und angezeigt, geht in die Simulation aber noch nicht ein. | Note: The DHW/combi sink only becomes effective once the two-channel cascade is enabled (switch in the configuration dialog).\nIt is saved and displayed, but does not yet enter the simulation. | Form_Waermesenke.cs:832 |
| `SIM_MSG_BRAUCHWASSER_WP_ZUSATZ` | Die bisherige Pufferspeicher-Zuordnung dieser Wärmepumpe wird dabei entfernt; ohne zweikanalige Kaskade rechnet die Simulation dann ohne Speicher. | The previous buffer storage assignment of this heat pump is removed in the process; without the two-channel cascade the simulation then calculates without storage. | Form_Waermesenke.cs:839 |
| `SIM_MSG_ERGEBNIS_GESPEICHERT` | Simulationsergebnis gespeichert. | Simulation result saved. | Form_Simulation_Detail.cs:1397 |
| `SIM_MSG_ERGEBNIS_NICHT_GESPEICHERT` | Das Ergebnis konnte nicht gespeichert werden. | The result could not be saved. | Form_Simulation_Detail.cs:1399 |
| `SIM_MSG_KASKADE_ABWAHL` | Die Konfiguration führt Warmwasser und Heizwärme getrennt. Ohne zweikanalige\nKaskade gehen Brauchwasser-/Kombi-Senken und Quellbezüge nicht in die Simulation ein.\n\nTrotzdem deaktivieren? | The configuration handles domestic hot water and space heating separately. Without\nthe two-channel cascade, DHW/combi storage sinks and source references are not\nincluded in the simulation.\n\nDeactivate anyway? | Form_Simulation_Config.Uebersicht.cs:191 |
| `SIM_MSG_KASKADE_AUTOMATISCH` | Die zweikanalige Kaskade wurde für dieses Projekt automatisch aktiviert, da\nWarmwasser und Heizwärme getrennt geführt werden. | The two-channel cascade has been activated automatically for this project because\ndomestic hot water and space heating are handled separately. | Form_Simulation_Config.Uebersicht.cs:280 |
| `SIM_MSG_KASKADE_FRAGE` | Die Konfiguration führt Warmwasser und Heizwärme getrennt.\n\nSoll die zweikanalige Kaskade für dieses Projekt eingeschaltet werden? | The configuration handles domestic hot water and space heating separately.\n\nShould the two-channel cascade be switched on for this project? | Form_Simulation_Config.Uebersicht.cs:297 |
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
| `SIM_TITEL_KASKADE` | Zweikanalige Kaskade | Two-channel cascade | Form_Simulation_Config.Uebersicht.cs:192 |
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
| `SIM_SPALTE_ZWEITSENKE` | ~~`SIM_ROLLE_ZWEITSENKE`~~ — **Zusammenführung zurückgenommen**, siehe Nacharbeit oben |
| `SIMQ_QUELLE_PUFFERSPEICHER` | `SIMQ_TYP_PUFFERSPEICHER` |
| `SIM_GB_HAUPTSENKE` | ~~`SIM_ROLLE_HAUPTSENKE`~~ — die Beschriftung heißt jetzt `SIM_GRUPPE_HAUPTSENKE`, siehe Nacharbeit oben |
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

## Nachtrag Etappen D2/D3 (Kartenansicht der Simulationskonfiguration)

**Konzept_KonfigUI_Hydraulik, Abschnitte 3 / 3a / 6.** Die Erzeugerübersicht und die
Pufferzeile der Fußzeile sind durch zwei Kartenspalten ersetzt
(`ErzeugerKarte`, `SpeicherKarte`). Dabei kamen **39 Schlüssel** dazu, einer wurde
umformuliert, einer entfiel. Alle sind in beiden `.resx` und in `Resource.Designer.cs`
nachgezogen; Bestand jetzt **590 Schlüssel**.

### Neu

| Schlüssel | DE | EN | Fundstelle |
|---|---|---|---|
| `SIM_KARTEN_KOPF_ERZEUGER` | Komponenten der Simulation | Simulation components | Form_Simulation_Config.Karten.cs (Spaltenkopf links) |
| `SIM_KARTE_TITEL` | {0} · {1} | {0} · {1} | Karten.cs (Kopfzeile „Erzeuger · Anlage") |
| `SIM_KARTE_QUELLE` | Quelle: {0} | Source: {0} | Karten.cs (Chip, blau) |
| `SIM_KARTE_QUELLE_KASKADE` | Quelle: {0} · Kaskade | Source: {0} · cascade | Karten.cs (Chip, blau gestrichelt) |
| `SIM_KARTE_SENKE` | Senke: {0} | Sink: {0} | Karten.cs (Chip, koralle) |
| `SIM_KARTE_ZWEITSENKE` | Zweitsenke: {0} | Secondary sink: {0} | Karten.cs (Chip) |
| `SIM_KARTE_WPPRIO` | WP-Prio {0} | HP prio {0} | Karten.cs (Chip, nur Wärmepumpe) |
| `SIM_KARTE_TEMPERATURPAAR` | {0} / {1} °C | {0} / {1} °C | Karten.cs (Erzeuger- und Speicherkarte) |
| `SIM_KARTE_OHNE_ANLAGE` | keine Anlage im Projekt | no unit in the project | Karten.cs |
| `SIM_KARTE_VERFUEGBAR` | nicht in der Simulation | not in the simulation | Karten.cs (verfügbare Karte) |
| `SIM_KARTE_AUFNEHMEN` | + aufnehmen | + include | ErzeugerKarte.cs (Schalter) |
| `SIM_KARTE_KEINE_ERZEUGER` | Kein Wärmeerzeuger ausgewählt. | No heat generator selected. | Karten.cs |
| `SIM_KARTE_ANLAGE_HINZU` | + Anlage hinzufügen: Anlagen werden über die Projektseite angelegt. | + Add unit: units are created on the project page. | Karten.cs (Fußzeile der Spalte) |
| `SIM_KARTE_TIP_HOCH` | In der Kaskade einen Rang nach vorn | Move one rank forward in the cascade | ErzeugerKarte.cs (▲) |
| `SIM_KARTE_TIP_RUNTER` | In der Kaskade einen Rang nach hinten | Move one rank back in the cascade | ErzeugerKarte.cs (▼) |
| `SIM_KARTE_TIP_BEARBEITEN` | Wärmesenke bearbeiten (auch per Doppelklick auf die Karte) | Edit heat sink (double-click on the card works as well) | ErzeugerKarte.cs (✎) |
| `SIM_KARTE_TIP_AUFNEHMEN` | In die Simulation aufnehmen — …\n… letzter Rang der Kaskade (mit ▲ nach vorn). | Include in the simulation — …\n… last cascade rank (move it forward with ▲). | ErzeugerKarte.cs (+) |
| `SIM_KARTE_TIP_ENTFERNEN` | Aus der Simulation nehmen — die Komponente bleibt im Projekt,\nwird aber nicht mehr gerechnet. | Remove from the simulation — the component stays in the project\nbut is no longer computed. | ErzeugerKarte.cs (×) |
| `SIM_KARTE_TIP_KASKADE` | Kaskade: Dieser Erzeuger bezieht seine Eintrittstemperatur aus dem Pufferspeicher\nund hebt sie weiter an. … | Cascade: this generator draws its inlet temperature from the buffer storage\nand raises it further. … | Karten.cs (Hinweis am Kaskaden-Quellchip) |
| `SIM_KARTE_TIP_TEMPERATUR_WARNUNG` | Der Vorlauf des Erzeugers ({0} °C) liegt unter dem Vorlauf des Speichers „{1}" ({2} °C).\n… | The generator flow temperature ({0} °C) is below the flow temperature of storage "{1}" ({2} °C).\n… | Karten.cs (Warnregel Konzept Abschnitt 5) |
| `PSP_KARTEN_KOPF_SPEICHER` | Speicher im Projekt | Storage in the project | Karten.cs (Spaltenkopf rechts) |
| `PSP_KARTE_BILANZ` | {0} Lader · {1} Abnehmer | {0} chargers · {1} consumers | SpeicherKarte.cs (Kurzbilanz) |
| `PSP_KARTE_VOLUMEN` | {0} l | {0} l | Karten.cs |
| `PSP_KARTE_LADER` | Lader: {0} | Chargers: {0} | Karten.cs (Detailzeile) |
| `PSP_KARTE_LADER_KEINE` | Lader: keiner | Chargers: none | Karten.cs |
| `PSP_KARTE_VERSORGT` | Versorgt: {0} | Supplies: {0} | Karten.cs |
| `PSP_KARTE_QUELLE_FUER` | Quelle für: {0} | Source for: {0} | Karten.cs (Invariante S-1: nur Erzeuger) |
| `PSP_KARTE_KASKADE` | (Kaskade) | (cascade) | Karten.cs |
| `PSP_KARTE_PV_RANG` | PV-Rang {0} | PV rank {0} | Karten.cs (Ladereihenfolge, Konzept 3.5) |
| `PSP_KARTE_ENTLADEPRIO` | Entladeprio: {0} | Discharge prio: {0} | Karten.cs |
| `PSP_KARTE_SCHWELLEN` | Schwellen {0} / {1} / {2} % | Thresholds {0} / {1} / {2} % | Karten.cs (unter dem Schwellenband) |
| `PSP_KARTE_TEMP_HERKUNFT` | Temperaturen: {0} | Temperatures: {0} | Karten.cs |
| `PSP_KARTE_TEMP_EIGEN` | eigene Werte am Speicher | own values at the storage | Karten.cs (Vorrangkette Stufe 1) |
| `PSP_KARTE_TEMP_ZUORDNUNG` | aus der Zuordnungszeile | from the assignment row | Karten.cs (Stufe 2) |
| `PSP_KARTE_TEMP_SYSTEM` | Systemvorgabe des Projekts | project system default | Karten.cs (Stufe 3) |
| `PSP_KARTE_TEMP_KEINE` | nicht gepflegt | not maintained | Karten.cs |
| `PSP_KARTE_KEIN_SPEICHER` | Für dieses Projekt ist noch kein Pufferspeicher angelegt. | No buffer storage has been created for this project yet. | Karten.cs |
| `PSP_KARTE_TIP_BEARBEITEN` | Pufferspeicher bearbeiten (Verwaltung öffnen) | Edit buffer storage (open management) | SpeicherKarte.cs (✎) |

### Entfallen

| Schlüssel | Grund |
|---|---|
| `SIM_KARTEN_KOPF_STROM` | Die Gruppenüberschriften „Wärmeerzeuger", „Stromerzeuger", „Energiespeicher" kommen aus den vorhandenen Designer-Beschriftungen `label1`/`label2`/`label3` von `Form_Simulation_Config` (dort schon in beiden Sprachen gepflegt). Ein eigener Katalogschlüssel wäre eine zweite Wahrheit über denselben Text. |

### Ohne Fundstelle seit D2/D3

Diese Schlüssel gehörten zur abgelösten ListView-Übersicht bzw. zur Pufferzeile der
Fußzeile. Sie bleiben im Katalog stehen — der Wortbestand ist unverändert richtig, und
Etappe D4 („Schema"-Ansicht) braucht mehrere davon voraussichtlich wieder:

`SIM_UEBERSICHT_TITEL`, `SIM_SPALTE_PRIO`, `SIM_SPALTE_WPPRIO`, `SIM_SPALTE_MODUS`,
`SIMQ_SPALTE_QUELLE`, `SIM_TIP_UEBERSICHT_STANDARD`, `SIM_TIP_WPPRIO_NICHT_WP`,
`SIMQ_TIP_QUELLE_NICHT_WP`, `SIM_TIP_BETRIEBSMODUS_NICHT_WP`, `PSP_FUSSZEILE_LISTE`,
`PSP_FUSSZEILE_KEINER`.

`SIM_SPALTE_ANLAGE`, `SIM_SPALTE_SENKE`, `SIM_SPALTE_ZWEITSENKE` und
`PSP_FUSSZEILE_OHNE_PROJEKT` haben weiterhin Fundstellen (Senkendialog,
Puffer-Verwaltung, Speicherspalte ohne Projekt).

## Nachtrag Etappe D5a (Kombispeicher — Nacharbeit nach den Reviews)

**Konzept_KonfigUI_Hydraulik, Anforderungen 4/7, Entscheidung K-1.** Die vierte
Senkenoption „Pufferspeicher Kombi" und die Verwendung `Kombi` in der Puffer-Verwaltung
standen bis zur Nacharbeit als deutsche **Festtexte** im Code — die Ressourcendateien waren
während D5a vom parallel laufenden D2/D3-Paket belegt (Befund I-K1-2 des Integrations-
Reviews: sichtbarer Sprachbruch auf englischer Oberfläche). Alle fünf sind jetzt im Katalog,
in beiden `.resx` und in `Resource.Designer.cs`; Bestand jetzt **595 Schlüssel**.

### Neu (5)

| Schlüssel | DE | EN | Fundstelle |
|---|---|---|---|
| `SIM_ZIEL_PUFFERSPEICHER_KOMBI` | Pufferspeicher Kombi (Heizung + Warmwasser) | Buffer storage combined (heating + DHW) | `WaermesenkeClass.ZielAnzeige`; dritter Eintrag von `_cbZiel2` in `Form_Waermesenke.cs` |
| `SIM_PUFFER_KOMBI_KURZ` | Puffer Kombi | Buffer combined | `WaermesenkeClass.KurzformZuZiel` (Haupt-/Zweitsenkenanzeige) |
| `PSP_VERWENDUNG_KOMBI_ANZEIGE` | Kombi (Heizung + Warmwasser) | Combined (heating + DHW) | `WaermesenkeClass.VerwendungAnzeige`; dritte Option des Verwendungs-Dropdowns in `Form_PufferSp_Projekt.cs` |
| `SIM_RB_PUFFER_KOMBI` | Puffer Kombi (Heizung + Warmwasser) | Buffer combined (heating + DHW) | vierter Radiobutton der Hauptsenke, `Form_Waermesenke.cs` |
| `SIM_LBL_HINWEIS_KOMBI` | Ein Kombispeicher deckt Heizung und Warmwasser aus einem gemeinsamen Wärmevorrat. Reicht er in einer Stunde nicht für beides, wird zuerst Warmwasser bedient. | A combined storage covers heating and DHW from one common heat reservoir. If it is not sufficient for both within an hour, DHW is served first. | Hinweislabel des Senkendialogs, hinter `SIM_LBL_HINWEIS_PUFFER` |

**Wortwahl nach dem Glossar:** „Pufferspeicher" → *buffer storage*, „Brauchwasser/Warmwasser"
→ *DHW* (Kürzel zulässig, Kapitel 11), „Heizung" → *heating*. „Kombi" ist im Englischen
*combined* — nicht *combi*, das im britischen Sprachgebrauch die Kombitherme meint und damit
ein Gerät statt einer hydraulischen Verwendung bezeichnet.

### Nicht lokalisiert — und warum

| Wert | Grund |
|---|---|
| `"Kombi"` (`DbWerte.PSP_VERWENDUNG_KOMBI`) | **Persistenzwert** in `Tab_Pufferspeicher.Verwendung`, deutsch und eingefroren (Drei-Schichten-Regel). Er wird in SQL verglichen und steht in `Tab_ErgebnisPufferspeicher`. |
| `"PufferKombi"` (`DbWerte.WS_ZIEL_PUFFER_KOMBI`) | **Persistenzwert** in `Tab_Energieanlagen.WS_Ziel`/`WS_Ziel2`, ebenso eingefroren. |
| Meldungen der Kessel-Kaskade, des Zyklus-Guards, des Kurzschluss-Guards und der Altpfad-Hinweise | **Protokollkanal der Engine.** Sie laufen wie in Paket 8 vorgesehen als deutsche Klartexte über `SimulationProtokoll`; die Umstellung des gesamten Kanals ist als eigener Schritt vorgemerkt (`SIMENG_*`-Familie). Bis dahin bleiben sie Festtexte — ausdrücklich als offener Punkt geführt, nicht übersehen. |

## Nachtrag Etappe D5b (Dialog-Freischaltung der Quellenwahl)

**Konzept_KonfigUI_Hydraulik, Abschnitte 4 und 7.** Der Heizkessel darf seine Wärmequelle
jetzt in der Kartenansicht wählen (Kaskade), die beiden Dialogprüfungen — Kurzschluss und
Ring — melden im Dialog statt erst im Lauf, und der Kombispeicher zeigt seine
Entladeposition in **beiden** Kanälen. Dazu kamen **11 Schlüssel**; einer verlor seine
Fundstelle. Alle sind in beiden `.resx` und in `Resource.Designer.cs` nachgezogen; der
Bestand steht jetzt bei **617 `<data>`-Einträgen** je Datei (davon vier Nicht-Text-Einträge
der Vorlage: `Name1`, `Color1`, `Bitmap1`, `Icon1`).

### Neu (11)

| Schlüssel | DE | EN | Fundstelle |
|---|---|---|---|
| `PSP_ENTLADE_POSITION_KANAL_HEIZUNG` | Heizkanal: als {0}. von {1} entladen. | Heating circuit: discharged as no. {0} of {1}. | `Form_PufferSp_Projekt.KombiPositionstext` (Zeile 1 beim Kombispeicher) |
| `PSP_ENTLADE_POSITION_KANAL_WARMWASSER` | Warmwasserkanal: als {0}. von {1} entladen. | DHW circuit: discharged as no. {0} of {1}. | dito (Zeile 2) |
| `SIMQ_QUELLE_SYSTEMRUECKLAUF` | Systemrücklauf | System return | `WaermequelleClass.TypAnzeigeFuer` (Kessel, Eintrag 1); Quellen-Chip des Kessels in `Form_Simulation_Config.Karten.QuellenChip` |
| `SIMQ_TIP_QUELLE_KESSEL` | Wärmequelle des Heizkessels (Doppelklick zum Ändern) … | Heat source of the boiler (double-click to change) … | Mouseover des Kessel-Quellenchips, `…Karten.QuellenChip` |
| `SIMQ_MSG_QUELLE_ART` | Eine Wärmequelle wählen können nur die Wärmepumpe und der Heizkessel. … | Only the heat pump and the boiler can select a heat source. … | `Form_Simulation_Config.WaermequelleBearbeiten` (Sperre für BHKW/Solarthermie) |
| `SIMQ_PUFFER_HINWEIS_KASKADE` | Kaskade (Heizkessel): … Anteil = (Vorlauf des Puffers − Rücklauf des Kessels) / … | Cascade (boiler): … Share = (storage flow − boiler return) / … | `Form_QuellePufferspeicher.ArtAnwenden` — steht beim Kessel an der Stelle der Verdampfer-Rubrik |
| `SIMQ_PUFFER_HINWEIS_KASKADE_KURZ` | Der Heizkessel hebt von der Puffertemperatur auf sein eigenes Vorlaufniveau an … | The boiler raises the temperature from the storage level to its own flow level … | dito, rechte Spalte (ersetzt beim Kessel `SIMQ_PUFFER_HINWEIS_QUELLWAERME`) |
| `SIMQ_PUFFER_HINWEIS_ALTBEZEICHNER` | Quelle bisher nur über den Namen „{0}". Mit OK wird der markierte Speicher fest zugeordnet. | Source stored by name "{0}" only. OK assigns the storage selected above. | `Form_QuellePufferspeicher.PufferListeLaden` (E0: nicht aufgelöster Altbestand) |
| `SIM_QUELLE_GLEICH_EIGENE_SENKE` | Der Pufferspeicher „{0}" ist bereits Wärmesenke dieser Anlage ({1}). … | The buffer storage "{0}" is already a heat sink of this unit ({1}). … | `WaermesenkeClass.KurzschlussMeldung` |
| `SIM_QUELLE_KASKADE_RING` | Die Quellbezüge der Pufferspeicher bilden einen RING: … Beteiligt: {0} … | The source references of the buffer storages form a RING: … Involved: {0} … | `WaermesenkeClass.RingMeldung` |
| `SIM_QUELLE_BETEILIGT` | {0} (Quelle: {1}) | {0} (source: {1}) | `WaermesenkeClass.RingBeteiligte` (Aufzählungsglied der Ringmeldung) |

**Wortwahl nach dem Glossar:** „Pufferspeicher" → *buffer storage*, „Heizkanal/
Warmwasserkanal" → *heating circuit* / *DHW circuit* (Kürzel DHW zulässig, Kapitel 11),
„Systemrücklauf" → *system return*, „Kaskade" → *cascade*, „Anlage" → *unit*. Die
Kanalwörter sind bewusst NICHT die Speicherwörter aus `PSP_KANALWORT_*`: Dort steht, wie
viele **Speicher** der Kanal hat („von 2 Heizungsspeichern"), hier, in **welchem Kanal**
die Position gilt — ein Kombispeicher ist weder ein Heizungs- noch ein
Brauchwasserspeicher.

### Ohne Fundstelle seit D5b

| Schlüssel | Grund |
|---|---|
| `SIMQ_MSG_QUELLE_NUR_WP` | Der Text („Eine Wärmequelle hat nur die Wärmepumpe. Heizkessel, BHKW und Solarthermie erzeugen ihre Wärme selbst …") ist mit der Kessel-Freischaltung **inhaltlich falsch geworden**. Statt ihn umzuschreiben — der Schlüsselname trüge die Aussage weiter — steht die neue Sperre unter `SIMQ_MSG_QUELLE_ART`. Der alte Schlüssel bleibt im Katalog stehen (Wortbestand für BHKW/Solarthermie unverändert richtig), hat aber keine Fundstelle mehr. |

### Nicht lokalisiert — und warum

| Wert | Grund |
|---|---|
| `""` (`DbWerte.WQ_TYP_OHNE`) | **Persistenzwert** in `Tab_Energieanlagen.WQ_Typ` — der leere Spaltenwert, den jede Anlage trägt, die nie einen Quellendialog gesehen hat. Er ist der Steuerwert des ersten Kessel-Eintrags; angezeigt wird er über `SIMQ_QUELLE_SYSTEMRUECKLAUF`. |
| Meldungen des Zyklus- und des Kurzschluss-Guards **in der Engine** | unverändert Protokollkanal (siehe D5a-Nachtrag). Die beiden neuen Texte oben sind die **Dialog**-Fassungen; die Engine-Fassungen bleiben deutsche Klartexte über `SimulationProtokoll`, bis die `SIMENG_*`-Familie kommt. |

## Nachtrag Etappe D4 (Ansicht „Schema")

**Konzept_KonfigUI_Hydraulik, Abschnitte 3 und 6.** Die Konfigurationsseite bekommt eine
zweite, synchronisierte Ansicht — das gezeichnete Hydraulikschema mit Kaskadenband und
Legende — und die Kessel-Kaskade wird als Ergebnisgröße sichtbar. Dazu kamen **23
Schlüssel**; der Bestand steht damit bei **640 `<data>`-Einträgen** je Datei (davon vier
Nicht-Text-Einträge der Vorlage: `Name1`, `Color1`, `Bitmap1`, `Icon1`).

### Neu (23)

| Schlüssel | DE | EN | Fundstelle |
|---|---|---|---|
| `SIM_ANSICHT_LABEL` | Ansicht: | View: | `Form_Simulation_Config.Schema.SchemaAufbauen` (Beschriftung vor dem Umschalter) |
| `SIM_ANSICHT_LISTE` | Liste | List | dito, Schalter mit Steuerwert `LISTE` |
| `SIM_ANSICHT_SCHEMA` | Schema | Schematic | dito, Schalter mit Steuerwert `SCHEMA` |
| `SIM_SCHEMA_SPALTE_QUELLE` | Wärmequelle | Heat source | `SchemaAnsicht.SpaltenkoepfeZeichnen` (Spalte 0) |
| `SIM_SCHEMA_SPALTE_ERZEUGER` | Erzeuger | Generators | dito (Spalte 1) |
| `SIM_SCHEMA_SPALTE_SPEICHER` | Speicher | Storage | dito (Spalte 2) |
| `SIM_SCHEMA_SPALTE_ABNEHMER` | Abnehmer | Consumers | dito (Spalte 3) |
| `SIM_SCHEMA_ABNEHMER_WARMWASSER` | Warmwasser | Domestic hot water | `SchemaModell` — Abnehmerknoten und zweites Badge des Kombispeichers |
| `SIM_SCHEMA_TIP_ABNEHMER` | Abnehmer der Wärme — er wird unmittelbar von einem Erzeuger oder aus einem Pufferspeicher versorgt. | Heat consumer — supplied directly by a generator or from a buffer storage tank. | Mouseover der beiden Abnehmerknoten |
| `SIM_SCHEMA_QUELLE_SOLARSTRAHLUNG` | Solarstrahlung | Solar irradiation | `SchemaModell.Quelltext` (Solarthermie) |
| `SIM_SCHEMA_QUELLE_BRENNSTOFF` | Brennstoff | Fuel | `SchemaModell.Quelltext` (BHKW) |
| `SIM_SCHEMA_WARNUNG` | Vorlauf unter dem Puffer-Sollwert | Flow below the buffer set point | `SchemaAnsicht.KnotenZeichnen` — das amber Band am Erzeugerkasten (Warnregel Konzept 5) |
| `SIM_SCHEMA_KETTE_KOPF` | Kaskadenkette | Cascade chain | `SchemaAnsicht.BandZeichnen` (Überschrift des Pillen-Bands) |
| `SIM_SCHEMA_KEINE_KETTE` | Keine Kaskade im Projekt — kein Erzeuger bezieht seine Wärme aus einem Pufferspeicher. | No cascade in this project — no generator draws its heat from a buffer storage tank. | dito, wenn das Projekt keine Kette führt |
| `SIM_SCHEMA_LEER` | Für dieses Projekt ist noch keine Hydraulik konfiguriert. | No hydraulic configuration has been set up for this project yet. | `SchemaAnsicht.OnPaint` bei leerem Modell |
| `SIM_SCHEMA_LEGENDE_LADUNG` | Ladung (Kreis = wirksame Priorität) | Charging (circle = effective priority) | `SchemaAnsicht.LegendeZeichnen` |
| `SIM_SCHEMA_LEGENDE_VERSORGUNG` | Versorgung / Entladung | Supply / discharge | dito |
| `SIM_SCHEMA_LEGENDE_QUELLE` | Quellseite | Source side | dito |
| `SIM_SCHEMA_LEGENDE_KASKADE` | Kaskade: Puffer speist den Vorlauf des nachgeschalteten Erzeugers | Cascade: buffer feeds the flow temperature of the downstream generator | dito |
| `SIM_KESSEL_QUELLWAERME` | Quellwärme aus Kaskade: | Source heat from cascade: | `Form_Simulation_Detail.InitKesselQuellwaerme` (Beschriftung der neuen Ergebniszeile) |
| `SIM_KESSEL_QUELLWAERME_EINHEIT` | MWh | MWh | dito, Einheit rechts vom Feld |
| `SIM_KESSEL_QUELLWAERME_TIP` | Wärme, die die Spitzenkessel in der Kaskade aus ihrem Quellpuffer bezogen haben. … | Heat that the peak-load boilers have drawn from their source buffer in the cascade. … | Mouseover derselben Zeile |
| `PSP_VOLLZYKLEN_KOMBI_TIP` | Kombispeicher: Heizung und Warmwasser werden aus EINEM Wärmevorrat gedeckt. … | Combined storage tank: heating and domestic hot water are covered from ONE heat reservoir. … | `Form_Simulation_Detail.PufferspeicherErgebnisAnzeigen` (Zeilenhinweis am markierten Vollzyklen-Wert) |

**Wortwahl nach dem Glossar:** „Pufferspeicher" → *buffer storage*, „Warmwasser" → *domestic
hot water* (in der Spaltenüberschrift ausgeschrieben, weil der Kasten breit genug ist und
die Abkürzung DHW dort ohne Kontext stünde), „Kaskade" → *cascade*, „Vorlauf" → *flow*,
„Ladung/Entladung" → *charging/discharge*. `SIM_ANSICHT_SCHEMA` ist im Englischen
*Schematic* und nicht *Scheme* — *scheme* meint im Englischen einen Plan oder eine
Systematik, nicht die Zeichnung einer Anlage. `MWh` ist in beiden Sprachen gleich; das ist
die einzige Zeile, an der die Sprachgleichheitsprobe absichtlich keinen Unterschied sieht.

### Nicht lokalisiert — und warum

| Wert | Grund |
|---|---|
| `"LISTE"` / `"SCHEMA"` (`Form_Simulation_Config.ANSICHT_*`) | **Steuerwerte** der Ansichtsumschaltung, sprachneutral und ASCII (Drei-Schichten-Regel, Schicht „Schlüssel"). Sie stehen am `Tag` der beiden Schalter; die Beschriftung kommt aus `SIM_ANSICHT_LISTE`/`SIM_ANSICHT_SCHEMA`. |
| `"QUELLE_…"`, `"ERZEUGER_…"`, `"SPEICHER_…"`, `"ABNEHMER_HEIZKREIS"`, `"ABNEHMER_WARMWASSER"` (`SchemaModell`) | **Knotenschlüssel** der Zeichnung — sie tragen die Auswahl zwischen Liste und Schema und dürfen deshalb nie ein Anzeigetext sein. Sprachneutral, ASCII, mit angehängter Datenbank-ID. |
| `"Quellwaerme"` (`SchemaKatalog.SPALTE_KESSEL_QUELLWAERME`) | **Persistenzwert** — Spaltenname in `Tab_ErgebnisHeizkessel`. Wie alle Spaltennamen umlautfrei und eingefroren. |
| `" *"` an der Vollzyklen-Zelle | typografische Marke ohne Wortbestand, wie die Glyphen `▲▼✎×` der Kartenansicht. Die Erklärung dazu steht im lokalisierten `PSP_VOLLZYKLEN_KOMBI_TIP`. |

## Nachtrag Sichttest Kessel-Diagramm — Bezugsgröße und Überlappung (16.08.2026)

Das Diagramm „Wärmelast Jahresganglinie" der Heizkessel-Seite zeigt als Bedarf jetzt den
**Gesamtwärmebedarf des Projekts** statt des Stufeneingangs der Kessel, und der Bedarf liegt
als Linie ÜBER den Produktionssäulen statt als Fläche dahinter. Beide Legendentexte werden
damit mehrdeutig — „Wärmebedarf" könnte weiterhin den Stufeneingang meinen, „Wärmeproduktion"
die Gesamtproduktion aller Erzeuger. Sie bekommen deshalb **eigene, präzisierte Schlüssel**;
die bisherigen (`CHART_LEGENDE_WAERMEBEDARF`, `CHART_LEGENDE_WAERMEPRODUKTION`) bleiben
unverändert für die übrigen Diagramme in Gebrauch. Die CSV-Ausgabe derselben Seite führt seit
diesem Nachtrag beide Bedarfsgrößen nebeneinander und braucht dafür zwei getrennte Spaltenköpfe.

Die **Seriennamen bleiben unverändert** (`WAERMEBEDARF`, `WAERMEPRODUKTION`, `RESTWAERME`) —
sie sind Steuerwerte der Schicht 2 und dürfen sich nicht mit dem Anzeigetext bewegen.

### Neu (4)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `CHART_LEGENDE_WAERMEBEDARF_GESAMT` | Wärmebedarf gesamt | Total heat demand | Form_Simulation_Detail.cs (`KesselSerienAufbauen`, Serie `WAERMEBEDARF`) | **neu.** Sagt, dass die rote Linie den PROJEKTbedarf zeigt und nicht den bei den Kesseln anliegenden Rest. Nicht zu verwechseln mit `CHART_LEGENDE_GESAMT` („Gesamt", Summenlinie der Erzeuger in NavigatorWaerme). |
| `CHART_LEGENDE_WAERMEPRODUKTION_HEIZKESSEL` | Wärmeproduktion Heizkessel | Boiler heat generation | Form_Simulation_Detail.cs (`KesselSerienAufbauen`, Serie `WAERMEPRODUKTION`) | **neu.** Benennt den Erzeuger ausdrücklich: die Säulen sind die Summe über alle Kessel des Projekts, nicht die Gesamtproduktion. Wortwahl nach Glossar (`Heizkessel` → *boiler*). |
| `CHART_CSV_WAERMEBEDARF_GESAMT` | Wärmebedarf gesamt [kW] | Total heat demand [kW] | Form_Simulation_Detail.cs (`btn_CsvExportKessel_Click`) | **neu.** Spaltenkopf der Bedarfslinie des Diagramms. |
| `CHART_CSV_WAERMEBEDARF_KESSELSTUFE` | Wärmebedarf Kesselstufe [kW] | Heat demand at boiler stage [kW] | Form_Simulation_Detail.cs (`btn_CsvExportKessel_Click`) | **neu.** Spaltenkopf des Stufeneingangs, der bis hierher unter `CHART_CSV_WAERMEBEDARF` („Wärmebedarf [kW]") lief. Der alte Schlüssel bleibt für den Wärmepumpen-Export in Gebrauch; hier stünden sonst zwei Spalten „Wärmebedarf" nebeneinander. |

### Geändert (1)

| Schlüssel | DE neu | EN neu | Grund |
|---|---|---|---|
| `SIM_TOOLTIP_CSV_HEIZKESSEL` | Heizkessel-Simulation als CSV exportieren\n(Zeitstempel, Außentemperatur, **Wärmebedarf gesamt, Wärmebedarf Kesselstufe**, Heizkessel, Restwärme) | Export boiler simulation as CSV\n(time stamp, outdoor temperature, **total heat demand, heat demand at boiler stage**, boiler, residual heat) | Die Klammer nennt die Spalten der Datei; die Datei hat eine Spalte mehr bekommen. |

## Nachtrag Kostenfixes D1–D6 — Kategorie in Summen und Überschriften (18.08.2026)

Die Komponententabelle der Seite „Berichte & Kosten → Kosten" und die Gesamtsumme der
Kostenverwaltung lasen bisher die gespeicherte Abfrage `Abfrage_KostenKomponenten`, die
**nicht** nach `KategorieID` filtert. Investitions-, Betriebs- und Energiepositionen derselben
Komponente landeten in einer Zahl (Projekt 1024: Kachel „Investition" 12.001,00 € gegen
Tabellenzeile „Gesamt" 12.100,00 €). Beide Ansichten lesen jetzt kategoriegetrennt über
`Form_Kosten.LiesKomponentenSummen`. Damit ändert sich die **Bedeutung** der beiden
Tabellenüberschriften: Sie zeigen ausschließlich Kategorie 1 und sagen das jetzt auch.
Die Gesamtsumme der Kostenverwaltung nennt zusätzlich die Kategorie, weil Investitions- und
Betriebskosten verschiedene Bezugsgrößen haben (€ gegenüber €/a).

Die Reiterbeschriftungen von `Form_Kosten` („Investitionskosten"/„Betriebskosten"/
„Energiekosten") bleiben unangetastete Designer-Literale und dienen dort weiterhin als
Steuerwert (`tabMain_SelectedIndexChanged`) — ein Altbestand, der mit diesen Fixes nicht
angefasst wurde.

### Neu (1)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `KOSTEN_LBL_PROJEKT_GESAMT` | PROJEKT GESAMT ({0}): {1} € | PROJECT TOTAL ({0}): {1} € | Form_Kosten.cs (`Gesamtkosten`, `label_Gesamt`) | **neu.** Ersetzt das deutsche Literal `"PROJEKT GESAMT: …"`. `{0}` ist die Kategorie (Reitertext), `{1}` der bereits formatierte Betrag — ohne die Kategorie wäre nicht erkennbar, welche Kostenart die Zahl summiert. |

### Geändert (2)

| Schlüssel | DE neu | EN neu | Grund |
|---|---|---|---|
| `BK_KOSTEN_LBL_KOMPONENTEN` | Investition je Komponente | Investment per component | Die Tabelle zeigt seit dem Kategoriefilter nur noch Kategorie 1. „Kosten je Komponente" hätte weiterhin die Gesamtkosten aller Kategorien versprochen. |
| `BK_KOSTEN_SP_SUMME` | Investition [€] | Investment [€] | Dieselbe Präzisierung im Spaltenkopf; die Summenzeile darunter (`BK_KOSTEN_SUMME`, „Gesamt") muss zur Kachel „Investition" passen. |

## Nachtrag Kostenübernahme — Technik-Planwerte, Nebenkosten, Abweichungen (18.08.2026)

Umsetzung der vier Nutzerentscheidungen aus
[`../Reporting/Kostenuebernahme_Protokoll.md`](../Reporting/Kostenuebernahme_Protokoll.md):
Der Technik-Planwert wird je Anlage zur Wahl gestellt, Nebenkosten entstehen als eigene Zeilen,
Betriebskosten werden erst nach einem Simulationslauf vorbelegt, und Abweichungen zwischen
erfasster Position und Technik werden angezeigt statt still überschrieben.

**Drei-Schichten-Zuordnung dieser Etappe.** Die vier Nebenkostenbezeichnungen („Montage",
„Lieferung", „Schallschutzhaube", „Abgasreinigung") gehen als `Tab_Kostenfaktor.Bezeichnung`
in die Datenbank und werden in SQL damit verglichen — sie sind **Persistenzwerte** und stehen
deshalb in `Allgemein/DbWerte.cs` (`KOSTENPOSTEN_*`), nicht hier. Ebenso die Gruppe „Allgemein"
(`KOSTEN_GRUPPE_ALLGEMEIN`) und die Einheit „€" (`KOSTEN_EINHEIT_EURO`), die bis dahin als
Literale in `Form_Kosten` standen. Die Kostenbasen sind **Steuerwerte** und sprachneutral
(`MODULPREIS`, `SPEZIFISCH`, `KEINE` in `TechnikPlanwertCtrl`); die Auswahlliste des Dialogs
zeigt dazu die Anzeigetexte `KOSTEN_PLANWERT_BASIS_*`.

### Neu (30)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `KOSTEN_BTN_PLANWERT` | 🔄 Planwert übernehmen… | 🔄 Apply engineering value… | Form_Kosten.cs (`UpdateDetailPanel`, Gruppenkopf) | **neu.** Ersetzt das deutsche Literal „🔄 Planwert übernehmen...". |
| `KOSTEN_PLANWERT_TITEL` | Technik-Planwert übernehmen — {0} | Apply engineering value — {0} | Form_PlanwertUebernahme.cs (`Aufbauen`) | **neu.** `{0}` ist die Komponente. |
| `KOSTEN_PLANWERT_KOPF` | Je Anlage festlegen, welcher Wert als Investition gilt. Die Nebenkosten entstehen als eigene Zeilen. | Choose per unit which value counts as the investment. Ancillary costs are created as separate rows. | Form_PlanwertUebernahme.cs | **neu.** Sagt die beiden Regeln der Maske in einem Satz. |
| `KOSTEN_PLANWERT_SP_ANLAGE` | Anlage | Unit | Form_PlanwertUebernahme.cs | **neu.** Spaltenkopf. |
| `KOSTEN_PLANWERT_SP_BASIS` | Kostenbasis | Cost basis | Form_PlanwertUebernahme.cs | **neu.** Spaltenkopf der Auswahlspalte. |
| `KOSTEN_PLANWERT_SP_BETRAG` | Betrag [€] | Amount [€] | Form_PlanwertUebernahme.cs | **neu.** Spaltenkopf. |
| `KOSTEN_PLANWERT_SP_HERLEITUNG` | Herkunft | Origin | Form_PlanwertUebernahme.cs | **neu.** Spaltenkopf; die Spalte zeigt Feldname bzw. Rechenweg. |
| `KOSTEN_PLANWERT_BASIS_MODUL` | Modulpreis | Module price | TechnikPlanwertCtrl.cs (`BasisName`) | **neu.** Anzeigetext zum Steuerwert `MODULPREIS`. |
| `KOSTEN_PLANWERT_BASIS_SPEZ` | spezifischer Preis × Baugröße | specific price × size | TechnikPlanwertCtrl.cs (`BasisName`) | **neu.** Anzeigetext zum Steuerwert `SPEZIFISCH`; bewusst gewerkneutral, weil die Baugröße beim BHKW kWel und beim Speicher kWh/kW ist. |
| `KOSTEN_PLANWERT_BASIS_KEINE` | nicht ansetzen | do not apply | TechnikPlanwertCtrl.cs (`BasisName`) | **neu.** Anzeigetext zum Steuerwert `KEINE` — die Anlage trägt nichts zur Hauptposition bei. |
| `KOSTEN_PLANWERT_HERL_FELD` | Feld {0} | field {0} | TechnikPlanwertCtrl.cs (`BasenFuellen`) | **neu.** `{0}` ist der SPALTENNAME der Datenbank und bleibt deshalb unübersetzt. |
| `KOSTEN_PLANWERT_HERL_BHKW` | {0} €/kWel × {1} kWel | {0} €/kWel × {1} kWel | TechnikPlanwertCtrl.cs (`BasenFuellen`) | **neu.** Rein numerische Herleitung; `kWel` ist eine Einheit und in beiden Sprachen gleich. |
| `KOSTEN_PLANWERT_HERL_SPEICHER` | {0} €/kWh × {1} kWh + {2} €/kW × {3} kW + {4} € | (gleich) | TechnikPlanwertCtrl.cs (`BasenFuellen`) | **neu.** Dieselbe Begründung. |
| `KOSTEN_PLANWERT_NEBENKOSTEN` | Nebenkosten — je Posten eine eigene Zeile: | Ancillary costs — one row per item: | Form_PlanwertUebernahme.cs (`SummeAktualisieren`) | **neu.** Die Posten selbst sind Persistenzwerte aus `DbWerte` und bleiben deutsch. |
| `KOSTEN_PLANWERT_SUMME` | Hauptposition: {0} € | Main item: {0} € | Form_PlanwertUebernahme.cs | **neu.** `{0}` ist bereits formatiert. |
| `KOSTEN_PLANWERT_BTN_OK` | Übernehmen | Apply | Form_PlanwertUebernahme.cs | **neu.** |
| `KOSTEN_PLANWERT_BTN_ABBRUCH` | Abbrechen | Cancel | Form_PlanwertUebernahme.cs | **neu.** |
| `KOSTEN_PLANWERT_LEER` | Für „{0}" ist in der Technik dieses Projekts kein Kostenwert gepflegt. | No cost value is maintained in this project's engineering data for "{0}". | Form_Kosten.cs (`btnTest_KostenUebernahme_Click`) | **neu.** Ersetzt die frühere Ja/Nein-Rückfrage „Es wurden 0,00 € in der Technik gefunden. Trotzdem übernehmen?", die eine 0 in die Position schreiben wollte. |
| `KOSTEN_PLANWERT_UEBERNOMMEN` | „{0}": Hauptposition auf {1} € gesetzt, {2} Nebenkostenzeile(n) abgeglichen. | "{0}": main item set to {1} €, {2} ancillary row(s) reconciled. | Form_Kosten.cs (`btnTest_KostenUebernahme_Click`) | **neu.** Ersetzt das Literal „Der Wert für '…' wurde erfolgreich auf … € aktualisiert."; nennt zusätzlich die Nebenzeilen. |
| `KOSTEN_ABWEICHUNG` | Weicht vom Technik-Planwert ab: erfasst {0} €, Technik {1} €. Über „Planwert übernehmen…" angleichen. | Differs from the engineering value: recorded {0} €, engineering {1} €. Use "Apply engineering value…" to align. | Form_Kosten.cs (`HinweiszeileAnlegen`), UcBkKosten.cs (Zellen-Tooltip) | **neu.** Nennt **beide** Werte und den Weg zum Angleichen — überschrieben wird nie. |
| `KOSTEN_ABWEICHUNG_AUSWAHL` | Für diese Komponente stehen zwei Kostenbasen zur Wahl. Über „Planwert übernehmen…" entscheiden. | Two cost bases are available for this component. Decide via "Apply engineering value…". | Form_Kosten.cs (`HinweiszeileAnlegen`) | **neu.** Der Fall „noch nichts gewählt" ist keine Abweichung im Zahlenvergleich, sondern eine offene Entscheidung. |
| `KOSTEN_BETRIEB_OHNE_ERGEBNIS` | Vorbelegung der Betriebskosten erst nach einem Simulationslauf verfügbar. | Operating cost defaults are available only after a simulation run. | TechnikPlanwertCtrl.cs (`LiesBetriebsplanwert`) | **neu.** Nutzerentscheidung 3: ohne Lauf keine Zahl, sondern ein Grund. |
| `KOSTEN_BETRIEB_OHNE_WARTUNGSFELD` | Für dieses Gewerk sind keine Wartungsangaben hinterlegt — keine Vorbelegung. | No maintenance data is stored for this trade — no default value. | TechnikPlanwertCtrl.cs | **neu.** Gilt für WP, PV, Solarthermie, Pufferspeicher, Stromspeicher. |
| `KOSTEN_BETRIEB_OHNE_MENGE` | Der Simulationslauf weist für dieses Gewerk keine Jahresmenge aus — keine Vorbelegung. | The simulation run reports no annual quantity for this trade — no default value. | TechnikPlanwertCtrl.cs | **neu.** Lauf vorhanden, aber Erzeugung 0. |
| `KOSTEN_BETRIEB_NICHT_ZUORDENBAR` | Die Wartungssätze lassen sich den Modulen des Laufs nicht eindeutig zuordnen — keine Vorbelegung. | Maintenance rates cannot be matched unambiguously to the modules of the run — no default value. | TechnikPlanwertCtrl.cs | **neu.** Lieber kein Wert als ein geratener. |
| `KOSTEN_BETRIEB_KESSEL_UNKLAR` | Die Einheit von Tab_Heizkessel.Wartungskosten ist nicht belegt — keine Vorbelegung (offene Rückfrage). | The unit of Tab_Heizkessel.Wartungskosten is not documented — no default value (open question). | TechnikPlanwertCtrl.cs | **neu.** Der Spaltenname steht bewusst im Text: er ist die Kennung, unter der die Rückfrage im Protokoll geführt wird. |
| `KOSTEN_BETRIEB_HERLEITUNG` | Vorbelegt: {0} €/kWhel × {1} kWhel aus dem Lauf vom {2}. | Default: {0} €/kWhel × {1} kWhel from the run of {2}. | TechnikPlanwertCtrl.cs | **neu.** Macht die Zahl nachvollziehbar und nennt den Lauf, aus dem sie stammt. |
| `BK_KOSTEN_SP_TECHNIK` | Technik-Planwert | Engineering value | UcBkKosten.cs (`LadeKomponenten`) | **neu.** Dritte Spalte der Komponententabelle neben „Investition [€]". |
| `BK_KOSTEN_ABWEICHUNG` | ⚠ {0} Komponente(n) weichen vom Technik-Planwert ab | ⚠ {0} component(s) differ from the engineering value | UcBkKosten.cs (`Aktualisiere`, Statuszeile) | **neu.** Ergänzt `BK_KOSTEN_STATUS`, wenn mindestens eine Komponente abweicht. |
| `BK_KOMP_HINW_KOSTEN` | Die Kostenposition „{0}" wurde nicht verändert und weicht jetzt vom Technik-Planwert ab — in der Kostenverwaltung über „Planwert übernehmen…" angleichen. | Cost item "{0}" was left unchanged and now differs from the engineering value — align it in cost management via "Apply engineering value…". | KomponentenUebernahmeCtrl.cs (`KostenabweichungMelden`) | **neu.** Reiht sich in die Hinweise der Komponenten-Übernahme ein (`BK_KOMP_HINW_*`): der Bestandsaustausch lässt die Kostenposition absichtlich stehen, sagt es jetzt aber. |

## Nachtrag Kessel-Wartungseinheit und Stückzahl bei PV/Solarthermie (18.08.2026)

Umsetzung der beiden Nutzerentscheidungen aus
[`../Reporting/Kostenuebernahme_Protokoll.md`](../Reporting/Kostenuebernahme_Protokoll.md),
Abschnitt „Nachtrag": Die Bezugsgröße der Kessel-Wartungskosten ist wählbar statt fest
verdrahtet, und der Investitions-Planwert von Photovoltaik und Solarthermie ist Modulpreis ×
Stückzahl.

**Drei-Schichten-Zuordnung dieser Etappe.** Die drei Einheiten treten in allen drei Schichten
auf und dürfen nicht verwechselt werden:

| Schicht | Wo | Werte |
|---|---|---|
| **Persistenz** | `Tab_Heizkessel.Wartungskosten_Einheit`, `Tab_Heizkessel_STAMM.Wartungskosten_Einheit`; Konstanten in `Allgemein/DbWerte.cs:177/185/194` | `€/a`, `€/kWh`, `%/a` — deutsch/eingefroren, in SQL verglichen (Migrationsschritt 15b) |
| **Schlüssel** | Steuerwerte der Auswahlliste, `Controller/TechnikPlanwertCtrl.cs:60-70` | `EUR_JAHR`, `EUR_KWH`, `PROZENT_INV` — sprachneutral, ASCII |
| **Anzeige** | `KESSEL_WARTUNG_EINH_*`, ausgegeben über `TechnikPlanwertCtrl.WartungName` | s. Tabelle unten |

Die Umrechnung läuft ausschließlich über `TechnikPlanwertCtrl.WartungSchluessel`
(Persistenz → Schlüssel, `TechnikPlanwertCtrl.cs:773`) und `WartungDbWert`
(Schlüssel → Persistenz, `:784`). In der ComboBox von `Form_Heizkessel_Bearbeiten` steht als
Item der Typ `EinheitItem` (`Form_Heizkessel_Bearbeiten.cs:214`): Er **trägt** den Schlüssel und
**zeigt** den lokalisierten Namen — kein Anzeigetext ist je Steuerwert. Verifiziert: Auf
englischer Oberfläche liefert `WartungDbWert("PROZENT_INV")` weiterhin `%/a`, und
`WartungSchluessel("€/kWh")` weiterhin `EUR_KWH`.

### Neu (11)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `KESSEL_WARTUNG_LBL` | Wartungskosten | Maintenance costs | Form_Heizkessel_Bearbeiten.cs (`WartungsfeldAufbauen`, `EingabenPruefen`) | **neu.** Beschriftung des neuen Feldes; **ohne Doppelpunkt**, weil derselbe Text auch als Feldname in der Prüfmeldung von `Program.ZahlPruefen` erscheint. Den Doppelpunkt hängt die Maske an. |
| `KESSEL_WARTUNG_EINHEIT_LBL` | Einheit | Unit | Form_Heizkessel_Bearbeiten.cs (`WartungsfeldAufbauen`) | **neu.** Beschriftung der Einheitenauswahl, ebenfalls ohne Doppelpunkt. |
| `KESSEL_WARTUNG_EINH_JAHR` | €/a Jahresbetrag | €/a per year | TechnikPlanwertCtrl.cs (`WartungName`) | **neu.** Anzeigetext zum Steuerwert `EUR_JAHR`. Das Einheitenzeichen bleibt in beiden Sprachen gleich, nur die Erläuterung ist übersetzt. |
| `KESSEL_WARTUNG_EINH_ARBEIT` | €/kWh Wärmemenge | €/kWh of heat | TechnikPlanwertCtrl.cs (`WartungName`) | **neu.** Anzeigetext zum Steuerwert `EUR_KWH`. |
| `KESSEL_WARTUNG_EINH_PROZENT` | %/a der Investition | %/a of investment | TechnikPlanwertCtrl.cs (`WartungName`) | **neu.** Anzeigetext zum Steuerwert `PROZENT_INV`. |
| `KOSTEN_PLANWERT_HERL_MENGE` | {0} €/Modul × {1} Module | {0} €/module × {1} modules | TechnikPlanwertCtrl.cs (`Stueckpreis`) | **neu.** Macht die Multiplikation in der Herkunftsspalte des Übernahmedialogs sichtbar, damit der Anwender die Rechnung nachvollziehen kann („468,89 €/Modul × 20 Module"). Reiht sich in `KOSTEN_PLANWERT_HERL_BHKW`/`…_SPEICHER` ein. |
| `KOSTEN_BETRIEB_EINHEIT_GEMISCHT` | Die Kessel dieses Projekts führen unterschiedliche Einheiten für die Wartungskosten — keine Vorbelegung. | The boilers of this project use different units for their maintenance costs — no default value. | TechnikPlanwertCtrl.cs (`KesselPlanwert`) | **neu.** Wärmemenge und Investitionsposition sind Gewerkgrößen, keine Gerätegrößen; bei gemischten Einheiten gibt es keinen rechenbaren Gesamtwert. Lieber kein Wert als ein geratener. |
| `KOSTEN_BETRIEB_OHNE_INVESTITION` | Die Wartungskosten sind als Anteil der Investition angegeben, die Investitionsposition ist aber noch nicht erfasst — keine Vorbelegung. | The maintenance costs are given as a share of the investment, but no investment item has been recorded yet — no default value. | TechnikPlanwertCtrl.cs (`KesselPlanwert`) | **neu.** Eigene Bezugsgröße, eigener Grund: `%/a` braucht keinen Simulationslauf, sondern eine erfasste Investition. |
| `KOSTEN_BETRIEB_HERL_KESSEL_JAHR` | Vorbelegt: {0} €/a — fester Jahresbetrag aus der Kesseltechnik. | Default: {0} €/a — fixed annual amount from the boiler data. | TechnikPlanwertCtrl.cs (`KesselPlanwert`) | **neu.** Herleitung je Einheit statt einer Sammelmeldung — der Anwender soll erkennen, WELCHE Einheit gerechnet wurde. |
| `KOSTEN_BETRIEB_HERL_KESSEL_ARBEIT` | Vorbelegt: {0} €/kWh × {1} kWh Wärme aus dem Lauf vom {2}. | Default: {0} €/kWh × {1} kWh of heat from the run of {2}. | TechnikPlanwertCtrl.cs (`KesselPlanwert`) | **neu.** Nennt zusätzlich den Lauf, aus dem die Wärmemenge stammt — Muster `KOSTEN_BETRIEB_HERLEITUNG` (BHKW). |
| `KOSTEN_BETRIEB_HERL_KESSEL_PROZENT` | Vorbelegt: {0} %/a von {1} € Investition = {2} €/a. | Default: {0} %/a of {1} € investment = {2} €/a. | TechnikPlanwertCtrl.cs (`KesselPlanwert`) | **neu.** Nennt Satz, Bezugsgröße und Ergebnis, weil sich der Betrag hier aus einer ANDEREN Kostenposition ableitet. |

### Entfallen (1)

| Schlüssel | Grund |
|---|---|
| `KOSTEN_BETRIEB_KESSEL_UNKLAR` | Der Text lautete „Die Einheit von Tab_Heizkessel.Wartungskosten ist nicht belegt — keine Vorbelegung (offene Rückfrage)." Die Rückfrage ist mit der Entscheidung vom 18.08.2026 beantwortet: Die Einheit ist wählbar und in jeder Zeile gesetzt. Der Schlüssel ist aus `Resource.resx`, `Resource.en-US.resx` und `Resource.Designer.cs` entfernt; an seine Stelle treten die drei `KOSTEN_BETRIEB_HERL_KESSEL_*` und die beiden neuen Gründe. |

**Kein neuer Persistenzwert außerhalb von `DbWerte`.** Die Stückzahlspalten (`PV_Leistung`,
`Kollektormodulanzahl`) und die Gerätetabellen stehen als Spaltennamen in der Landkarte
`TechnikPlanwertCtrl.Plaene` — Spaltennamen sind keine Datenwerte und gehören deshalb weiterhin
nicht in `DbWerte` (dieselbe Abgrenzung wie bei `Tab_WP.Heizung`, siehe Kopf von `DbWerte.cs`).

## Nachtrag Anlagenzeilen-Eindeutigkeit — eine Zeile je Projekt und Gerät (18.08.2026)

Umsetzung der Nutzerentscheidung „Prüfung und Index"; Befundlage, Leitgedanke und Verifikation in
[`../Update/Anlagenzeilen_Eindeutigkeit_Protokoll.md`](../Update/Anlagenzeilen_Eindeutigkeit_Protokoll.md).

**Drei-Schichten-Zuordnung dieser Etappe.**

| Schicht | Wo | Werte |
|---|---|---|
| **Persistenz** | keine neuen DB-Werte. Tabellennamen aus `SchemaKatalog`, Spaltennamen als Konstanten in `Allgemein/Update/AnlagenEindeutigkeit.cs:71-74` | `ID_WP`, `ID_Kessel`, `ID_BHKW`, `ID_PUFFER` — Spalten**namen**, keine Datenwerte (Abgrenzung wie im Kopf von `DbWerte.cs`) |
| **Schlüssel** | dieselben Spaltennamen als Steuerwerte in `WizardCtrl.Verweis`/`VerweisSetzen` (`Controller/WizardCtrl.cs:929/939`) und im Indexnamen `idx_Anlage_<Spalte>` | sprachneutral, ASCII |
| **Anzeige** | die fünf `ANL_*` unten, ausgegeben über `AnlagenEindeutigkeit.Fragen`/`Melden` (`:195`/`:212`) | s. Tabelle |

**Nicht lokalisiert und warum.** `GeraeteSperre.Gewerk` (`AnlagenEindeutigkeit.cs:79-88`:
„Wärmepumpe", „Heizkessel", „BHKW", „Pufferspeicher") ist reiner **Protokolltext** des
Migrationsberichts — dieselbe Kategorie wie die übrigen Zeilen in `SchemaMigration`, die
durchgehend deutsch bleiben. Ebenso die `Console.WriteLine`-Diagnosen des Schreibwegs.

### Neu (5)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `ANL_DUBLETTE_TITEL` | Gerät bereits im Projekt | Device already in the project | AnlagenEindeutigkeit.cs (`Aufnehmen`, `ZweitesGeraetBestaetigen`, `FeldHinweisPruefen`, `SpeichervarianteBenennen`) | **neu.** Ein Titel für alle vier Meldungen dieses Pakets — der Anwender soll sie als dieselbe Sache erkennen. |
| `ANL_DUBLETTE_FRAGE` | Das Gerät „{0}" ist bereits im Projekt.\n\nAls zweites, baugleiches Gerät aufnehmen? Dann wird eine eigene Gerätekopie angelegt.\n\n„Nein" verwirft die Aufnahme. | The device “{0}” is already part of this project.\n\nAdd it as a second, identical device? A separate device copy will then be created.\n\n“No” discards the entry. | AnlagenEindeutigkeit.cs (`Aufnehmen:333`, `ZweitesGeraetBestaetigen:238`), Form_PufferSp.cs (`btn_PufferSp_Hinzu_Click:112`) | **neu.** Die eine Rückfrage — ausdrücklich mit BEIDEN Folgen im Text, weil „Nein" eine Zeile verwirft und das sonst unsichtbar bliebe. Dialog und Schreibweg benutzen denselben Schlüssel; zwei Wortlaute für dieselbe Frage wären der Anfang zweier Wahrheiten. |
| `ANL_DUBLETTE_KOPIE_FEHLER` | Für „{0}" konnte keine eigene Gerätekopie angelegt werden. Die Anlage wurde nicht aufgenommen; Einzelheiten stehen im Protokoll. | No separate device copy could be created for “{0}”. The item was not added; details are in the log. | AnlagenEindeutigkeit.cs (`Aufnehmen`) | **neu.** Nennt die FOLGE („wurde nicht aufgenommen"), nicht nur den Fehler — ohne Kopie gäbe es nur noch die Dublette oder gar nichts, und der Anwender muss wissen, welches von beidem eingetreten ist. |
| `ANL_FELD_HINWEIS` | „{0}" ist mit derselben Neigung ({1}°), demselben Azimut ({2}°) und derselben Modulanzahl ({3}) bereits im Projekt.\n\nMehrere Felder desselben Modultyps sind zulässig — bitte prüfen, ob das so gewollt ist. | “{0}” is already in the project with the same tilt ({1}°), the same azimuth ({2}°) and the same module count ({3}).\n\nSeveral arrays of the same module type are allowed — please check whether this is intended. | AnlagenEindeutigkeit.cs (`FeldHinweisPruefen:609`) | **neu.** PV und Solarthermie sind NICHT gesperrt. Der zweite Satz steht deshalb ausdrücklich im Text: Der Hinweis ist eine Rückversicherung, keine Fehlermeldung. Die drei Zahlen nennen genau die Kriterien, die zum Treffer geführt haben. |
| `ANL_SP_NAME_ANGEPASST` | Der Name „{0}" ist im Projekt bereits vergeben. Die Speichervariante wurde in „{1}" umbenannt. | The name “{0}” is already used in this project. The storage variant was renamed to “{1}”. | AnlagenEindeutigkeit.cs (`SpeichervarianteBenennen:650`) | **neu.** Gegenstück zu `VAR_MSG_NAME_VERGEBEN`, das im Kontextmenü die Eingabe zurückweisen kann. Auf dem Wizard-Weg steht der Aufruf hinter einem bereits ausgeführten DELETE — dort wird umbenannt statt abgebrochen, und der Text sagt beide Namen. |

## Nachtrag Etappe E1 — Katalog gesetzlicher Parameter (18.08.2026)

Umsetzung der Etappe E1 aus
[`../Reporting/Konzept_BHKW_Kosten_Erloese.md`](../Reporting/Konzept_BHKW_Kosten_Erloese.md);
Befundlage, Seed-Liste und Verifikation in
[`../Reporting/W4_E1_Gesetzesparameter_Protokoll.md`](../Reporting/W4_E1_Gesetzesparameter_Protokoll.md).

**Drei-Schichten-Zuordnung dieser Etappe.**

| Schicht | Wo | Werte |
|---|---|---|
| **Persistenz** | `Allgemein/DbWerte.cs`, Block `GESETZ_*` — 161 Konstanten: 8 Klassen, 3 Status, 15 Einheiten und 135 Schlüssel | Alles, was als Zeichenkette in `Tab_Gesetzesparameter` steht: `KWKG`, `EF_NACHWEIS`, `GESICHERT`, `EUR/1000l`, `KWKG_VBH_JAHRESDECKEL` … Nach der Auslieferung **eingefroren** — ein umbenannter Schlüssel macht jede gepflegte Bestandszeile unauffindbar. |
| **Schlüssel** | dieselben Konstanten, verwendet als Steuerwert in `Form_Gesetzesparameter.KlasseItem.Wert` und in der Einheiten- und Statusauswahl des Zeilendialogs | sprachneutral, ASCII |
| **Anzeige** | die 36 `GESETZ_*` unten, ausgegeben aus `Form_Gesetzesparameter`, `Form_GesetzparameterZeile` und `MDIMainForm.InitGesetzeMenue` | s. Tabelle |

**Kein Anzeigetext ist Steuerwert.** Die Klassenauswahl der Maske trägt den DB-Wert im
Item (`KlasseItem.Wert`) und zeigt den lokalisierten Namen (`KlasseItem.Anzeige`) —
dasselbe Muster wie `EinheitItem` in `Form_Heizkessel_Bearbeiten`. Verifiziert im
Reflection-Harness (Probe D9): Auf englischer Oberfläche steht in der Auswahl
„CO₂ price", der gespeicherte Wert bleibt `CO2_PREIS`.

**Bewusst nicht lokalisiert.** Die Spalte `Quelle` jeder Katalogzeile („KWKG 2025 § 7
Abs. 1 — eingespeister KWK-Strom") ist ein **Datenwert**, kein Anzeigetext: Sie steht in
der Datenbank, ist vom Anwender pflegbar und benennt eine deutsche Rechtsnorm. Ebenso
bleiben die Statuswerte `GESICHERT` / `VORLAEUFIG` / `PROGNOSE` in der Liste als Rohwert
stehen — sie sind sprachneutrale ASCII-Schlüssel und werden erst dort übersetzt, wo sie
als Fließtext erscheinen (Bericht, Etappe E7).

### Neu (36)

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `GESETZ_MENUE` | Gesetzliche Parameter… | Statutory parameters… | MDIMainForm.cs (`InitGesetzeMenue`) | **neu.** Menüeintrag unter Administration, programmatisch eingehängt wie Lizenz und Peak-Shaving. |
| `GESETZ_TITEL` | Gesetzliche Parameter | Statutory parameters | Form_Gesetzesparameter.cs (Fenstertitel, Titel der Meldungen) | **neu.** |
| `GESETZ_LBL_KLASSE` | Bereich | Area | Form_Gesetzesparameter.cs, Form_GesetzparameterZeile | **neu.** „Bereich" statt „Klasse": Der Anwender wählt ein Themenfeld, `Klasse` ist der Spaltenname. |
| `GESETZ_LBL_HINWEIS` | Eine Gesetzesänderung ist eine neue Jahreszeile, kein Ändern der alten. Nur so liefert eine heute gerechnete Variante in einigen Jahren noch dieselben Zahlen. | An amendment is a new year row, not a change to the old one. Only then will a calculation made today still produce the same figures years from now. | Form_Gesetzesparameter.cs (Kopfzeile) | **neu.** Die Kernregel steht sichtbar auf der Maske, nicht nur in der Rückfrage — sie erklärt, warum die Liste mit den Jahren wächst. |
| `GESETZ_LBL_WERT_LEER` | leer = der Satz ist entfallen (nicht 0) | empty = the rate has been abolished (not 0) | Form_GesetzparameterZeile | **neu.** Der Unterschied zwischen „kein Wert" und „Wert 0" ist der Kern von L12; der Klammerzusatz macht ihn am Feld sichtbar. |
| `GESETZ_SP_SCHLUESSEL` | Schlüssel | Key | Form_Gesetzesparameter.cs (Spalte), Form_GesetzparameterZeile | **neu.** |
| `GESETZ_SP_JAHRVON` | Gültig ab | Valid from | dieselben | **neu.** |
| `GESETZ_SP_WERT` | Wert | Value | dieselben | **neu.** |
| `GESETZ_SP_EINHEIT` | Einheit | Unit | dieselben | **neu.** |
| `GESETZ_SP_STATUS` | Status | Status | dieselben | **neu.** In beiden Sprachen wortgleich; im Harness ausdrücklich als erlaubte Ausnahme geführt (Probe D6). |
| `GESETZ_SP_QUELLE` | Quelle | Source | dieselben | **neu.** |
| `GESETZ_BTN_NEU` | Neu… | New… | Form_Gesetzesparameter.cs | **neu.** Auslassungspunkte, weil ein Dialog folgt. |
| `GESETZ_BTN_AENDERN` | Ändern… | Edit… | Form_Gesetzesparameter.cs | **neu.** |
| `GESETZ_BTN_LOESCHEN` | Löschen | Delete | Form_Gesetzesparameter.cs | **neu.** |
| `GESETZ_BTN_SCHLIESSEN` | Schließen | Close | Form_Gesetzesparameter.cs | **neu.** |
| `GESETZ_BTN_UEBERNEHMEN` | Übernehmen | Apply | Form_GesetzparameterZeile | **neu.** |
| `GESETZ_BTN_ABBRECHEN` | Abbrechen | Cancel | Form_GesetzparameterZeile | **neu.** |
| `GESETZ_DLG_TITEL_NEU` | Neuer gesetzlicher Parameter | New statutory parameter | Form_GesetzparameterZeile | **neu.** |
| `GESETZ_DLG_TITEL_AENDERN` | Gesetzlichen Parameter ändern | Edit statutory parameter | Form_GesetzparameterZeile | **neu.** |
| `GESETZ_FRAGE_TITEL` | Gesetzesänderung oder Berichtigung? | Amendment or correction? | Form_Gesetzesparameter.cs (`btnAendern_Click`) | **neu.** Der Titel stellt die Frage, die der Anwender wirklich beantwortet — nicht „Wirklich ändern?". |
| `GESETZ_FRAGE_NEUE_ZEILE` | Die Zeile „{0}" gilt ab {1} und liegt damit in der Vergangenheit.\n\nEine Gesetzesänderung gehört in eine NEUE Jahreszeile; die alte bleibt stehen, damit ältere Rechnungen nachvollziehbar bleiben.\n\n„Ja" legt eine neue Zeile an. „Nein" ändert die bestehende Zeile — das ist nur für Tippfehler gedacht. | The row “{0}” is valid from {1} and therefore lies in the past.\n\nAn amendment belongs in a NEW year row; the old one stays in place so that earlier calculations remain traceable.\n\n“Yes” creates a new row. “No” changes the existing row — that is meant for typing errors only. | Form_Gesetzesparameter.cs (`btnAendern_Click`) | **neu.** Nennt **beide** Folgen ausdrücklich, weil „Nein" eine Altrechnung unreproduzierbar macht und das sonst unsichtbar bliebe. Vorgabeknopf ist „Ja". |
| `GESETZ_LOESCHEN_TITEL` | Gesetzlichen Parameter löschen | Delete statutory parameter | Form_Gesetzesparameter.cs (`btnLoeschen_Click`) | **neu.** |
| `GESETZ_FRAGE_LOESCHEN` | Die Zeile „{0}" (gültig ab {1}) wirklich löschen?\n\nDanach fehlt der Wert in jeder Rechnung, die dieses Jahr betrifft. | Really delete the row “{0}” (valid from {1})?\n\nAfter that the value will be missing from every calculation concerning that year. | Form_Gesetzesparameter.cs (`btnLoeschen_Click`) | **neu.** Der zweite Satz nennt die Folge: Die Lesefassade liefert danach `null`, nicht 0 — die Rechnung fällt aus, sie wird nicht billiger. Vorgabeknopf ist „Nein". |
| `GESETZ_MSG_SCHLUESSEL_FEHLT` | Bitte einen Schlüssel angeben. | Please enter a key. | Form_Gesetzesparameter.cs (`PruefeNeu`), Form_GesetzparameterZeile | **neu.** |
| `GESETZ_MSG_JAHR_UNGUELTIG` | „Gültig ab" muss eine Jahreszahl zwischen 1990 und 2100 sein. | “Valid from” must be a year between 1990 and 2100. | dieselben | **neu.** Nennt den Wertebereich, statt nur „ungültig" zu sagen. |
| `GESETZ_MSG_WERT_UNGUELTIG` | Der Wert ist keine gültige Zahl. Für einen entfallenen Satz das Feld leer lassen. | The value is not a valid number. Leave the field empty for a rate that has been abolished. | Form_GesetzparameterZeile (`btnOk_Click`) | **neu.** Der zweite Satz verhindert die naheliegende Fehlbedienung „dann trage ich eben 0 ein". |
| `GESETZ_MSG_DOPPELT` | Für den Schlüssel „{0}" gibt es bereits eine Zeile ab {1}. | The key “{0}” already has a row valid from {1}. | Form_Gesetzesparameter.cs (`PruefeNeu`) | **neu.** Schlüssel und Jahr sind zusammen eindeutig; zwei Zeilen für dasselbe Jahr machten den Lookup von der Zeilenreihenfolge abhängig. |
| `GESETZ_MSG_SPEICHERN_FEHLER` | Die Zeile konnte nicht gespeichert werden. | The row could not be saved. | Form_Gesetzesparameter.cs (drei Stellen) | **neu.** |
| `GESETZ_KLASSE_ANZ_KWKG` | KWK-Gesetz | CHP Act | Form_Gesetzesparameter.cs (`KlasseAnzeige`) | **neu.** Anzeigename zum Steuerwert `KWKG`. |
| `GESETZ_KLASSE_ANZ_STROMSTEUER` | Stromsteuer | Electricity tax | dieselbe | **neu.** Zu `STROMSTEUER`. |
| `GESETZ_KLASSE_ANZ_ENERGIESTEUER` | Energiesteuer | Energy tax | dieselbe | **neu.** Zu `ENERGIESTEUER`. |
| `GESETZ_KLASSE_ANZ_CO2_PREIS` | CO₂-Preis | CO₂ price | dieselbe | **neu.** Zu `CO2_PREIS`. Der Schlüssel bleibt ASCII, die Anzeige bekommt den Index. |
| `GESETZ_KLASSE_ANZ_EF_NACHWEIS` | Emissionsfaktoren — gesetzlicher Nachweis | Emission factors — statutory proof | dieselbe | **neu.** Zu `EF_NACHWEIS`. Der Zusatz ist Absicht: Die Maske muss auf einen Blick zeigen, dass das **nicht** die reale Bilanz ist (L11). |
| `GESETZ_KLASSE_ANZ_EF_BILANZ` | Emissionsfaktoren — reale Bilanz | Emission factors — real balance | dieselbe | **neu.** Zu `EF_BILANZ`. Gegenstück zum vorigen. |
| `GESETZ_KLASSE_ANZ_PEF_NACHWEIS` | Primärenergiefaktoren — gesetzlicher Nachweis | Primary energy factors — statutory proof | dieselbe | **neu.** Zu `PEF_NACHWEIS`. |
| `GESETZ_KLASSE_ANZ_UMSATZSTEUER` | Umsatzsteuer | VAT | dieselbe | **neu.** Zu `UMSATZSTEUER`. |

## Nachtrag Etappe E2 — Vollbenutzungsstunden des BHKW (18.08.2026)

Umsetzung der Etappe E2 aus
[`../Reporting/Konzept_BHKW_Kosten_Erloese.md`](../Reporting/Konzept_BHKW_Kosten_Erloese.md),
Leitentscheidung L6; Befund, Wirkung und Verifikation in
[`../Reporting/W4_E2_Vollbenutzungsstunden_Protokoll.md`](../Reporting/W4_E2_Vollbenutzungsstunden_Protokoll.md).

Drei neue Schlüssel im Katalog `MyResource/Resource.resx` (+ `.en-US.resx` + Designer).
Zwei davon **ersetzen zur Laufzeit** die Beschriftungen `label13` und `label108` der
BHKW-Ergebnisseite: Die Entwurfstexte lauteten „Betriebsstunden gesamt" und
„Betriebsstunden Durchschnitt", die Felder zeigen aber Summe beziehungsweise Mittel der
**thermischen Vollbenutzungsstunden** je Modul. Der Ersatz erfolgt in
`Form_Simulation_Detail.InitBhkwVbhZeile` — Designer und `.resx` der Form bleiben
unangetastet, dasselbe Muster wie bei den Speicher-Kennzahlzeilen.

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `SIM_BHKW_VBH_EL` | Vollbenutzungsstunden elektrisch: | Full-load hours, electric: | `Form_Simulation_Detail.InitBhkwVbhZeile` (Beschriftung der neuen Ergebniszeile) | **neu.** Die Größe, an der der KWK-Zuschlag hängt. |
| `SIM_BHKW_VBH_TH_SUMME` | Vbh thermisch, Summe Module | Thermal FLH, sum of modules | dieselbe (ersetzt `label13`) | **neu.** „Betriebsstunden gesamt" war falsch: Die Zahl ist eine Summe von Vollbenutzungsstunden und kann 8.760 h überschreiten. |
| `SIM_BHKW_VBH_TH_MITTEL` | Vbh thermisch, Mittel Module | Thermal FLH, module average | dieselbe (ersetzt `label108`) | **neu.** Wie oben, für den Mittelwert. |

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `"VbhThermisch"`, `"VbhElektrisch"` (`SchemaKatalog.SPALTE_MODUL_VBH_*`, `SPALTE_BHKW_VBH_ELEKTRISCH`) | **Persistenzwerte** — Spaltennamen in `Tab_ErgebnisBHKWModul` bzw. `Tab_ErgebnisBHKW`. Wie alle Spaltennamen umlautfrei und eingefroren. |
| `"KWKGVbhElektrisch"` (`WirtschaftlichkeitCtrl.SPALTE_KWKG_VBH_EL`) | dito, Spalte in `Tab_ErgebnisWirtschaftlichkeit`. |
| `"—"` im Feld der neuen Zeile, wenn keine elektrische Leistung gepflegt ist | typografische Marke ohne Wortbestand, wie die Glyphen der Kartenansicht. |

## Nachtrag zu Etappe E2 — 500-kW-Grenze je Anlage (19.08.2026)

Nutzerentscheidung vom 19.08.2026; Begründung, Wirkung und Verifikation im Abschnitt
„Nachtrag: 500-kW-Grenze je Anlage" von
[`../Reporting/W4_E2_Vollbenutzungsstunden_Protokoll.md`](../Reporting/W4_E2_Vollbenutzungsstunden_Protokoll.md).

Drei neue Schlüssel im Katalog `MyResource/Resource.resx` (+ `.en-US.resx` + Designer) —
die **erste** Lokalisierung im Bereich Wirtschaftlichkeit und damit ein neues
Schlüsselpräfix `WIRT_`. Sie ersetzen die bisherige deutsche Literal-Meldung des
500-kW-Guards, die nur die Projektsumme nannte.

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `WIRT_KWKG_ANLAGE_UEBER_GRENZE` | KWKG: Über der Ausschreibungsgrenze von {0} kW und deshalb ohne Zuschlag: {1} (der Weg über eine Ausschreibung nach § 8a KWKG/KWKAusV ist nicht abgebildet). Die übrigen Anlagen mit zusammen {2} kW rechnen weiter. | CHP Act: above the tendering threshold of {0} kW and therefore without bonus: {1} (the tendering route under section 8a KWKG/KWKAusV is not modelled). The remaining units totalling {2} kW continue to be calculated. | `WirtschaftlichkeitCtrl.BaueKwkgReihe` | **neu.** Nennt die betroffene Anlage und sagt ausdrücklich, dass die übrigen weiterrechnen — der Kern der Korrektur. |
| `WIRT_KWKG_ALLE_UEBER_GRENZE` | KWKG: Jede BHKW-Anlage des Projekts liegt über der Ausschreibungsgrenze von {0} kW ({1}) — der Zuschlag wäre nur über eine Ausschreibung nach § 8a KWKG/KWKAusV zu erlangen; Bonus = 0. | CHP Act: every CHP unit of the project exceeds the tendering threshold of {0} kW ({1}) — the bonus could only be obtained through tendering under section 8a KWKG/KWKAusV; bonus = 0. | dieselbe | **neu.** Löst die Altmeldung ab; Ergebnis wie bisher, Begründung jetzt die richtige. |
| `WIRT_KWKG_LEISTUNG_JE_ANLAGE_UNKLAR` | KWKG: Σ installierte BHKW-Leistung {0} kW über der Ausschreibungsgrenze von {1} kW; die Leistung je Anlage ließ sich nicht ermitteln, deshalb greift die Grenze ersatzweise auf die Projektsumme; Bonus = 0. | CHP Act: total installed CHP capacity {0} kW exceeds the tendering threshold of {1} kW; the capacity of the individual units could not be determined, so the threshold falls back to the project total; bonus = 0. | dieselbe | **neu.** Der Rückfallzweig ohne zuordenbare Anlagenzeilen. Er ist konservativ, und der Anwender muss wissen, dass hier ersatzweise die Projektsumme geprüft wurde. |

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `"KWKG_AUSSCHREIBUNG_GRENZE_KW"` (`DbWerte.GESETZ_KWKG_AUSSCHREIBUNG_GRENZE`) | **Persistenzwert** — Schlüssel in `Tab_Gesetzesparameter`, sprachneutral und eingefroren wie die 135 Schlüssel aus E1. Der Anzeigename der Klasse `KWKG` steht getrennt als `GESETZ_KLASSE_ANZ_KWKG`. |
| `Bezeichner + " (" + Pel + " kW)"` in `WirtschaftlichkeitCtrl.Anlagenauswahl` | Der Bezeichner ist ein **Datenwert** des Anwenders, die Klammer trägt nur das Einheitenzeichen `kW` — kein Wortbestand, in beiden Sprachen gleich. Dieselbe Ausnahme wie bei den typografischen Marken. |
| Die übrigen KWKG-Hinweise derselben Methode (Stichtag, Realisierungsfrist, Heizöl) | deutsche Literale des Bestands. Sie gehören zusammen mit den vierzehn deutschen Zeilentiteln des Wirtschaftlichkeitsreiters in **einem** Vorgang umgestellt (Begründung in Abschnitt 3.5 des E2-Protokolls); die drei neuen Texte laufen schon jetzt über den Katalog, weil sie neu entstehen. |

## Nachtrag 2 zu Etappe E2 — Heizöl-Ausschluss je Anlage (19.08.2026)

Nutzerentscheidung vom 19.08.2026; Begründung, Wirkung und Verifikation im Abschnitt
„Nachtrag 2: Heizöl-Ausschluss je Anlage" von
[`../Reporting/W4_E2_Vollbenutzungsstunden_Protokoll.md`](../Reporting/W4_E2_Vollbenutzungsstunden_Protokoll.md).

Sechs neue Schlüssel im Katalog `MyResource/Resource.resx` (+ `.en-US.resx` + Designer).
Sie ersetzen die **beiden** deutschen Literal-Meldungen des Heizöl-Guards, die weder die
betroffene Anlage nannten noch unterschieden, ob überhaupt eine installierte Anlage
betroffen ist. Die drei Schlüssel des Nachtrags 1 (`WIRT_KWKG_*_UEBER_GRENZE`,
`WIRT_KWKG_LEISTUNG_JE_ANLAGE_UNKLAR`) bleiben **wortgleich unverändert**.

| Schlüssel | DE | EN | Fundstellen | Grund |
|---|---|---|---|---|
| `WIRT_KWKG_ANLAGE_HEIZOEL` | KWKG: Mit Heizöl betrieben und deshalb ohne Zuschlag: {0} (KWKG 2025, Neuanlagen nur noch mit Erdgas; Näherung: gilt auch für Bio-Blends). Die übrigen Anlagen mit zusammen {1} kW rechnen weiter. | CHP Act: fired with heating oil and therefore without bonus: {0} (KWKG 2025, new units only with natural gas; approximation: applies to bio blends as well). The remaining units totalling {1} kW continue to be calculated. | `WirtschaftlichkeitCtrl.BaueKwkgReihe` | **neu.** Der Kern der Korrektur: Die Öl-Anlage wird benannt, und es steht ausdrücklich da, dass die übrigen weiterrechnen. |
| `WIRT_KWKG_ALLE_HEIZOEL` | KWKG: Jede BHKW-Anlage des Projekts wird mit Heizöl betrieben ({0}) — als Neuanlage nicht mehr förderfähig (KWKG 2025, nur noch Erdgas; Näherung: gilt auch für Bio-Blends); Bonus = 0. | CHP Act: every CHP unit of the project is fired with heating oil ({0}) — no longer eligible as a new unit (KWKG 2025, natural gas only; approximation: applies to bio blends as well); bonus = 0. | dieselbe | **neu.** Löst die Altmeldung ab; Ergebnis wie bisher, Begründung jetzt anlagenscharf. |
| `WIRT_KWKG_KEINE_FOERDERFAEHIG` | KWKG: Keine BHKW-Anlage des Projekts ist zuschlagsberechtigt — über der Ausschreibungsgrenze von {0} kW: {1}; mit Heizöl betrieben: {2}; Bonus = 0. | CHP Act: no CHP unit of the project is eligible for the bonus — above the tendering threshold of {0} kW: {1}; fired with heating oil: {2}; bonus = 0. | dieselbe | **neu.** Der gemischte Fall: Ein Teil der Anlagen fällt wegen der Größe heraus, der Rest wegen Heizöl. Ohne eigenen Text stünde in einer der beiden Einzelmeldungen eine leere Aufzählung. |
| `WIRT_KWKG_HEIZOEL_OHNE_IBN` | KWKG: Öl-BHKW ohne Inbetriebnahmedatum: {0} — als Neuanlage wäre der Zuschlag für diese Anlagen ausgeschlossen (KWKG 2025); Datum im Parameterdialog pflegen. | CHP Act: oil-fired CHP units without commissioning date: {0} — as new units their bonus would be excluded (KWKG 2025); please enter the date in the parameter dialogue. | dieselbe | **neu.** Ersetzt das gleichlautende deutsche Literal des Altstands und nennt jetzt die Anlagen. |
| `WIRT_KWKG_HEIZOEL_JE_ANLAGE_UNKLAR` | KWKG: Das Projekt führt ein Öl-BHKW; welche Anlage damit betrieben wird, ließ sich nicht ermitteln, deshalb greift der Heizöl-Ausschluss ersatzweise auf alle Geräte des Projekts (KWKG 2025, Neuanlagen nur noch mit Erdgas); Bonus = 0. | CHP Act: the project contains an oil-fired CHP unit; which unit is fired with oil could not be determined, so the heating-oil exclusion falls back to all devices of the project (KWKG 2025, new units only with natural gas); bonus = 0. | dieselbe | **neu.** Der Rückfallzweig ohne zuordenbare Anlagenzeilen — das Gegenstück zu `WIRT_KWKG_LEISTUNG_JE_ANLAGE_UNKLAR`. Er ist konservativ, und der Anwender muss wissen, dass hier ersatzweise die Gerätezeilen geprüft wurden. |
| `WIRT_KWKG_HEIZOEL_OHNE_IBN_UNKLAR` | KWKG: Das Projekt führt ein Öl-BHKW, aber kein Inbetriebnahmedatum — als Neuanlage wäre der Zuschlag ausgeschlossen (KWKG 2025). Welche Anlage mit Öl betrieben wird, ließ sich nicht ermitteln; Datum im Parameterdialog pflegen. | CHP Act: the project contains an oil-fired CHP unit but no commissioning date — as a new unit the bonus would be excluded (KWKG 2025). Which unit is fired with oil could not be determined; please enter the date in the parameter dialogue. | dieselbe | **neu.** Derselbe Rückfallzweig, wenn zusätzlich das Inbetriebnahmedatum fehlt — dann wird nichts ausgeschlossen, aber gewarnt. |

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `WirtschaftlichkeitCtrl.BRENNSTOFF_KATEGORIE_OEL` = 2 | **Persistenzwert** — `Tab_BrennstoffKategorien.ID` der Kategorie „Öl", in SQL verglichen und eingefroren. Er steht **nicht** in `DbWerte`, weil dort ausschließlich die deutschen Zeichen*ketten* der Datenbank gesammelt sind; die Klasse führt keine numerischen Schlüssel. Der Anzeigename der Kategorie („Öl") kommt aus `Tab_BrennstoffKategorien.Gruppe` und ist ein Datenwert, kein Ressourcentext. |
| `"LIQUID_FUEL"`, `"ELECTRICITY"` u. a. (`energy_carrier.pricing_model`) | dito Persistenzwerte, hier sogar sprachneutral-englisch. Bewusst **nicht** als Merkmal des Heizöl-Ausschlusses verwendet — sie fassen Kategorie 2 (Öl) und Kategorie 8 (Rapsöl) zusammen; Begründung im Protokoll, Abschnitt N2-3. |
| `Bezeichner + " (" + Pel + " kW)"` in `WirtschaftlichkeitCtrl.Anlagenauswahl` | unverändert aus Nachtrag 1: Der Bezeichner ist ein **Datenwert** des Anwenders, die Klammer trägt nur das Einheitenzeichen `kW`. |
| Die übrigen KWKG-Hinweise derselben Methode (Stichtag, Realisierungsfrist, fehlender KWKG-Satz, Vbh nicht bestimmbar) | deutsche Literale des Bestands. Sie gehören zusammen mit den vierzehn deutschen Zeilentiteln des Wirtschaftlichkeitsreiters in **einem** Vorgang umgestellt (Begründung in Abschnitt 3.5 des E2-Protokolls); die neuen Texte laufen schon jetzt über den Katalog, weil sie neu entstehen. |

## Nachtrag zu Etappe E3 — Kostenarten und Betriebskosten-Dialog (19.08.2026)

Umsetzung der Etappe **E3** aus `Konzept_BHKW_Kosten_Erloese.md`; Begründung, Wirkung und
Verifikation im
[`../Reporting/W4_E3_Kostenarten_Betriebskosten_Protokoll.md`](../Reporting/W4_E3_Kostenarten_Betriebskosten_Protokoll.md).

**43 neue Schlüssel** in `MyResource/Resource.resx` (+ `.en-US.resx` + `Resource.Designer.cs`).
Zwei Präfixe: `KOSTEN_*` für die beiden Ergänzungen in der Kostenverwaltung, `VDI_*` für die
neue Maske `Form_Betriebskosten`.

| Schlüssel | DE | EN | Fundstellen |
|---|---|---|---|
| `KOSTEN_BTN_VDI2067` | ⚙ Betriebskosten VDI 2067… | ⚙ Operating costs VDI 2067… | `Form_Kosten.UpdateDetailPanel` — Knopf in der Hauptgruppe des BHKW auf dem Reiter „Betriebskosten" |
| `KOSTEN_BEMESSUNG_HERLEITUNG` | Abgeleitet: {0} {1} × {2} {3} | Derived: {0} {1} × {2} {3} | `Form_Kosten.LoadKostenFaktoren` — Hinweis am gesperrten Betragsfeld einer abgeleiteten Position |
| `VDI_TITEL` | Betriebskosten nach VDI 2067 | Operating costs to VDI 2067 | `Form_Betriebskosten.Aufbauen` |
| `VDI_HINWEIS` | Kopfzeile: netto verbindlich, Brutto abgeleitet, Satz hat Vorrang | dito englisch | dieselbe |
| `VDI_SP_POSITION` · `VDI_SP_BEMESSUNG` · `VDI_SP_SATZ` · `VDI_SP_NETTO` · `VDI_SP_BRUTTO` · `VDI_SP_BEZUG` | die sechs Spaltenköpfe | dito | dieselbe |
| `VDI_POS_ANZ_*` (11 Schlüssel) | Anzeigenamen der elf Positionen | dito | `Form_Betriebskosten.PositionName` |
| `VDI_BEM_ANZ_*` (5 Schlüssel) | Anzeigenamen der fünf Bemessungsarten | dito | `Form_Betriebskosten.BemessungName` |
| `VDI_BEZUG_*` (6 Schlüssel) | Anzeigenamen der Bezugsgrößen | dito | `Form_Betriebskosten.BezugName` |
| `VDI_BEZUG_FEHLT` | nicht ermittelbar (Simulationslauf oder Investitionsposition fehlt) | cannot be determined (…) | `Form_Betriebskosten.Bezugstext` |
| `VDI_EMPFEHLUNG` | VDI 2067: {0}–{1} % | dito | dieselbe |
| `VDI_ERSETZT` | Durch die Satzangabe ersetzt — der Betrag wird berechnet … | Replaced by the rate … | `Form_Betriebskosten.ZeileNachziehen` (Hinweis am gesperrten Feld) |
| `VDI_VBH_NAEHERUNG` | Näherung: „Vollbenutzungsstunden" sind Wärme geteilt durch Leistung … | Approximation: … | `Form_Betriebskosten.Aufbauen`, Fußhinweis |
| `VDI_HINWEIS_INSTANDHALTUNG` | Wartung und Instandhaltung BHKW sind zwei EIGENE Positionen … | Maintenance and repairs … | dieselbe |
| `VDI_SUMME_NETTO` · `VDI_SUMME_BRUTTO` | Summenzeilen | dito | `Form_Betriebskosten.SummenNachziehen` |
| `VDI_UST_FEHLT` | Umsatzsteuersatz nicht im Katalog gepflegt — kein Bruttobetrag | VAT rate not maintained … | dieselbe |
| `VDI_BTN_OK` · `VDI_BTN_ABBRUCH` | Übernehmen · Abbrechen | Apply · Cancel | `Form_Betriebskosten.Aufbauen` |
| `VDI_GESPEICHERT` | {0} Betriebskostenpositionen nach VDI 2067 gespeichert. | {0} operating cost items to VDI 2067 saved. | `Form_Kosten.btnBetriebskostenVdi_Click` |

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `"KAPITALGEBUNDEN"`, `"BEDARFSGEBUNDEN"`, `"BETRIEBSGEBUNDEN"`, `"SONSTIGE"` (`DbWerte.KOSTENART_*`) | **Persistenzwerte** — Inhalt von `Tab_ProjektWerte.Kostenart`, in SQL verglichen und nach der Auslieferung eingefroren. ASCII und Großbuchstaben wie die 135 Katalogschlüssel aus E1. Die Anzeige läuft nicht über diese Werte: Die Kostenart hat in E3 keine sichtbare Oberfläche, sie gliedert erst den Bericht der Etappe E7. |
| `"BETRAG"`, `"PROZENT_INVESTITION"`, `"EUR_PRO_H"`, `"EUR_PRO_KWH"`, `"PROZENT_BRENNSTOFFKOSTEN"` (`DbWerte.BEMESSUNG_*`) | dito — Inhalt von `Tab_ProjektWerte.Bemessung`. Der Anzeigename steht getrennt als `VDI_BEM_ANZ_*`; die ComboBox trägt `Form_Betriebskosten.BemessungItem`, das den **Wert** hält und den **Namen** anzeigt (Muster `Form_Gesetzesparameter.KlasseItem`). Kein Anzeigetext ist je Steuerwert. |
| Die elf Positionsbezeichnungen (`DbWerte.VDI_POS_*`, z. B. `"Wartung BHKW"`) | **Persistenzwerte** — sie stehen als `Tab_Kostenfaktor.Bezeichnung` in der Datenbank, werden in SQL damit verglichen und ordnen der Position im Code ihre Bezugsgröße zu. Deutsch und eingefroren wie die vier Nebenkostenposten aus der Kostenübernahme (`KOSTENPOSTEN_MONTAGE` & Co.). Der Anzeigetext kommt getrennt aus `VDI_POS_ANZ_*`. |
| `"Betriebskosten VDI 2067"` (`DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI`) | dito — Wert in `Tab_ProjektWerte.Gruppe` und `Tab_KostenGruppenKatalog.GruppenName`, wie `KOSTEN_GRUPPE_ALLGEMEIN`. |
| `"INVEST_BHKW"`, `"VBH_BHKW"` & Co. (`BetriebskostenCtrl.BEZUG_*`) | **Schlüssel**, nicht Anzeige: sprachneutral und ASCII, stehen nirgends in der Datenbank und nirgends auf dem Bildschirm. Der Anzeigename ist `VDI_BEZUG_*`. |
| `"%"`, `"€/h"`, `"€/kWh"`, `"€"`, `"h/a"`, `"kWh/a"` (`BetriebskostenCtrl.SatzEinheit` / `MengenEinheit`) | reine **Einheitenzeichen ohne Wortbestand**, in beiden Sprachen gleich — dieselbe Ausnahme wie bei den typografischen Marken. |

---

## Nachtrag zu Etappe E4 — Energiesteuer- und Stromsteuergutschrift (19.08.2026)

Umsetzung der Etappe **E4** aus `Konzept_BHKW_Kosten_Erloese.md`; Begründung, Wirkung und
Verifikation im
[`../Reporting/W4_E4_Steuergutschriften_Protokoll.md`](../Reporting/W4_E4_Steuergutschriften_Protokoll.md).

**27 neue Schlüssel** in `MyResource/Resource.resx` (+ `.en-US.resx` + `Resource.Designer.cs`).
Drei Präfixe: `STEUER_*` für die Begründungen und die Herkunftszeile, `WIRT_ZEILE_*` für die
neuen Zeilen der Kennzahlentabelle, `WIRT_REIHE_*` für die Anzeigenamen der benannten
Erlösreihen.

### Neu (27)

| Schlüssel | DE | EN | Fundstellen |
|---|---|---|---|
| `STEUER_ENERGIEST_NICHT_GEWAEHLT` | Energiesteuer: keine Entlastung gewählt — § 53 oder § 53a im Parameterdialog festlegen | Energy tax: no relief selected … | `SteuerGutschriftRechner.Energiesteuer` |
| `STEUER_ENERGIEST_TRAEGER_UNKLAR` | {0} — dem Energieträger ist kein Steuersatz zugeordnet | {0} — no tax rate assigned … | dieselbe |
| `STEUER_ENERGIEST_SATZ_FEHLT` | {0} — der Katalogsatz {1} ist nicht gepflegt | {0} — catalogue rate {1} not maintained | dieselbe |
| `STEUER_ENERGIEST_MENGE_UNKLAR` | {0} — der auf die Stromerzeugung entfallende Brennstoff ist 0 | {0} — fuel attributable to power generation is 0 | dieselbe |
| `STEUER_ENERGIEST_EINHEIT_UNKLAR` | {0} — nicht in die gesetzliche Einheit {1} umrechenbar (…für Kilogramm fehlt die Dichte) | {0} — cannot be converted into the statutory unit {1} … | `SteuerGutschriftRechner.EinheitGrund` |
| `STEUER_ENERGIEST_53A_NUTZUNGSGRAD` | § 53a: Jahresnutzungsgrad {0} % unter der Schwelle von {1} % | § 53a: annual utilisation rate {0} % below … | `SteuerGutschriftRechner.NutzungsgradErfuellt` |
| `STEUER_ENERGIEST_53A_NUTZUNGSGRAD_FEHLT` | § 53a: kein Jahresnutzungsgrad erfasst (Schwelle {0} %) | § 53a: no annual utilisation rate recorded … | dieselbe |
| `STEUER_ENERGIEST_HO` | Erdgasmenge brennwertbezogen bemessen — Ho/Hi = {0} | natural gas quantity assessed on gross calorific value — Hs/Hi = {0} | `SteuerGutschriftRechner.MengeInGesetzlicherEinheit` (Herkunftszeile) |
| `STEUER_ENERGIEST_HO_FEHLT` | {0} — kein Brennwert gepflegt, rund 10 % zu niedrig | {0} — no gross calorific value maintained … | dieselbe |
| `STEUER_STROMST_HOCHEFFIZIENZ` | § 9 Abs. 1 Nr. 3: Hocheffizienz nicht nachgewiesen | § 9 (1) no. 3: high efficiency not evidenced | `SteuerGutschriftRechner.StromsteuerBefreiung` |
| `STEUER_STROMST_RAEUMLICH` | § 9 Abs. 1 Nr. 3: räumlicher Zusammenhang (bis {0} km) nicht bestätigt | § 9 (1) no. 3: spatial connection … not confirmed | dieselbe |
| `STEUER_STROMST_EIGEN_UNKLAR` | § 9 Abs. 1 Nr. 3: KWK-Eigenverbrauch nicht bestimmbar (keine Stundenreihen) | § 9 (1) no. 3: CHP self-consumption cannot be determined … | dieselbe |
| `STEUER_STROMST_LEISTUNG` | § 9 Abs. 1 Nr. 3: über {0} kW je Anlage: {1}; übrige {2} kW rechnen weiter | § 9 (1) no. 3: above {0} kW per plant … | dieselbe |
| `STEUER_STROMST_CO2` | § 9 Abs. 1 Nr. 3: über dem CO₂-Grenzwert von {0} g je kWh Energieertrag: {1} | § 9 (1) no. 3: above the CO₂ limit of {0} g … | dieselbe |
| `STEUER_STROMST_CO2_UNKLAR` | § 9 Abs. 1 Nr. 3: direkte CO₂-Emissionen nicht bestimmbar ({0}) | § 9 (1) no. 3: direct CO₂ emissions cannot be determined ({0}) | dieselbe |
| `STEUER_STROMST_9B_UNTERNEHMENSART` | § 9b: weder produzierendes Gewerbe noch Land- und Forstwirtschaft | § 9b: neither manufacturing industry nor agriculture … | `SteuerGutschriftRechner.StromsteuerEntlastung` |
| `STEUER_STROMST_9B_SOCKEL` | § 9b: {0} € erreichen den Sockelbetrag von {1} € nicht | § 9b: relief of {0} € does not reach the {1} € base amount | dieselbe |
| `STEUER_SATZ_FEHLT` | Steuer: der Katalogsatz {0} ist nicht gepflegt | Tax: catalogue rate {0} is not maintained | beide Steuerzweige |
| `STEUER_HERKUNFT_FORMAT` | {0} = {1} {2}, gültig ab {3} ({4}) — {5} | {0} = {1} {2}, valid from {3} ({4}) — {5} | `SteuerGutschriftRechner.Herkunft` |
| `WIRT_ZEILE_ENERGIESTEUER` | Energiesteuer-Gutschrift Jahr 1 [€/a] | Energy tax credit year 1 [€/a] | `UcWirtschaftlichkeit.ZeigeErgebnisse`, `BausteineWirtschaftlichkeit.SchreibeVergleich`, `ExcelBerichtGenerator` |
| `WIRT_ZEILE_STROMST_BEFREIUNG` | Stromsteuer-Befreiung Jahr 1 [€/a] | Electricity tax exemption year 1 [€/a] | dieselben drei |
| `WIRT_ZEILE_STROMST_ENTLASTUNG` | Stromsteuer-Entlastung Jahr 1 [€/a] | Electricity tax relief year 1 [€/a] | dieselben drei |
| `WIRT_ZEILE_STEUER_HERKUNFT` | Herkunft der Steuersätze | Source of the tax rates | `UcWirtschaftlichkeit.ZeigeErgebnisse` |
| `WIRT_REIHE_KWKG` | KWK-Zuschlag | CHP bonus | Anzeigename zu `KapitalwertRechner.ErloesReihe.KWKG` (Ausgabe folgt mit E7) |
| `WIRT_REIHE_ENERGIESTEUER` | Energiesteuer-Gutschrift | Energy tax credit | dito `…ErloesReihe.ENERGIESTEUER` |
| `WIRT_REIHE_STROMSTEUER_BEFREIUNG` | Stromsteuer-Befreiung | Electricity tax exemption | dito `…ErloesReihe.STROMSTEUER_BEFREIUNG` |
| `WIRT_REIHE_STROMSTEUER_ENTLASTUNG` | Stromsteuer-Entlastung | Electricity tax relief | dito `…ErloesReihe.STROMSTEUER_ENTLASTUNG` |
| `WIRT_ZEILE_VERMIEDEN_ARBEIT` | Vermiedene Kosten — Arbeit [€/a] | Avoided cost — energy charge [€/a] | `UcWirtschaftlichkeit.ZeigeErgebnisse`, `BausteineWirtschaftlichkeit.SchreibeVergleich`, `ExcelBerichtGenerator` (Etappe E5) |
| `WIRT_ZEILE_VERMIEDEN_LEISTUNG` | Vermiedene Kosten — Leistung [€/a] | Avoided cost — demand charge [€/a] | dieselben drei |
| `WIRT_ZEILE_VERMIEDEN_GESAMT` | Vermiedene Kosten gesamt [€/a] | Avoided cost total [€/a] | dieselben drei |
| `WIRT_ZEILE_AUFSCHLAG` | Aufschläge auf den Strombezug [€/a] | Surcharges on grid supply [€/a] | dieselben drei |

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `"KEIN_PROD_GEWERBE"`, `"PROD_GEWERBE"`, `"LAND_FORSTWIRTSCHAFT"` (`DbWerte.UNTERNEHMENSART_*`) | **Persistenzwerte** — Inhalt von `Tab_ProjektWirtschaftlichkeit.Unternehmensart`, in SQL verglichen, nach der Auslieferung eingefroren. Die ComboBox trägt `Form_WirtschaftlichkeitParameter.Steuerwahl`, das den **Wert** hält und den **Namen** anzeigt (Muster `Form_Betriebskosten.BemessungItem`). |
| `"KEINE"`, `"PARAGRAF_53"`, `"PARAGRAF_53A"` (`DbWerte.ENERGIESTEUER_WAHL_*`) | dito — Inhalt von `…Energiesteuer_Wahl`, Vorbelegung durch Migrationsschritt 20b. |
| `"VOLLER_BRENNSTOFF"`, `"ENERGETISCH"` (`DbWerte.AUFTEILUNG_*`) | dito — Inhalt von `…Aufteilung_Methode`. |
| `"KWKG_ZUSCHLAG"`, `"ENERGIESTEUER_GUTSCHRIFT"`, `"STROMSTEUER_BEFREIUNG"`, `"STROMSTEUER_ENTLASTUNG"` (`KapitalwertRechner.ErloesReihe.*`) | **Schlüssel**, nicht Anzeige: sprachneutral und ASCII, stehen nirgends in der Datenbank und nirgends auf dem Bildschirm. Der Anzeigename ist `WIRT_REIHE_*`. |
| `"ZONEN"`, `"ROLLEN"` (`DbWerte.TARIF_MODUS_*`) | **Persistenzwerte** (Etappe E5) — Inhalt von `Tab_ProjektTarif.Tarif_Modus`, in SQL verglichen, Vorbelegung durch Migrationsschritt 21b, nach der Auslieferung eingefroren. Die ComboBox in `Form_Tarifstruktur` trägt eine `Wahl`-Klasse, die den **Wert** hält und den **Namen** anzeigt (Muster `Form_WirtschaftlichkeitParameter.Steuerwahl`). |
| `"MONATLICH"`, `"STAFFEL"`, `"JAHRESHOECHSTLAST"` (`DbWerte.LEISTUNGSMODELL_*`) | dito — Inhalt von `…Bezug_Leistungsmodell` und `…Rest_Leistungsmodell`. Längster Wert 17 Zeichen ⇒ TEXT(24). |
| Die Beschriftungen von `Form_Tarifstruktur` (beide Modellblöcke, Staffelraster, Fußhinweis) | Der Dialog ist wie `Form_WirtschaftlichkeitParameter` **vollständig** unlokalisiert und im Code aufgebaut; er gehört als Ganzes umgestellt, nicht in Teilen. |
| Die Beschriftungen des Blocks „BHKW — Energie- und Stromsteuer" im Parameterdialog | `Form_WirtschaftlichkeitParameter` ist **vollständig** unlokalisiert (alle Gruppen, alle Zeilen, der Fußhinweis) und im Code aufgebaut. Vier lokalisierte Zeilen darin wären keine Lokalisierung, sondern eine Inkonsistenz mehr — der Dialog gehört als Ganzes umgestellt. |
| Katalogschlüssel in den Meldungen (`ENERGIEST_ERDGAS` & Co.) | **Schlüssel der Persistenzschicht** aus Etappe E1. Sie stehen bewusst im Klartext in der Meldung, damit der Anwender die Zeile in Administration → „Gesetzliche Parameter" wiederfindet. |

---

## Nachtrag zu Etappe E6 — KWK-Zuschlag je BHKW-Modul (19.08.2026)

`WirtschaftlichkeitCtrl.BaueKwkgReihe` rechnet den Zuschlag ab E6 **je Anlage** und summiert
jahresweise. Dabei entstehen drei neue Meldungen im Ergebnis und sechs Bausteine der
**Herleitung**, die der neue Dialog „KWK-Zuschlag je BHKW-Modul" zeigt und die zugleich in den
Ergebnishinweis wandern können.

Die neun Schlüssel der Nachträge 1 und 2 zu E2 (`WIRT_KWKG_*_UEBER_GRENZE`,
`WIRT_KWKG_*_HEIZOEL*`, `WIRT_KWKG_KEINE_FOERDERFAEHIG`, `WIRT_KWKG_LEISTUNG_JE_ANLAGE_UNKLAR`)
bleiben **wortgleich unverändert**.

### Neu (9)

| Schlüssel | Deutsch | Englisch | Fundstelle |
|---|---|---|---|
| `WIRT_KWKG_JE_MODUL` | KWKG: Zuschlag je BHKW-Modul gerechnet — {0}. | CHP Act: bonus calculated per CHP unit — {0}. | `WirtschaftlichkeitCtrl.ReiheJeAnlage` — erscheint **nur** bei mehr als einer Anlage oder bei mindestens einer eigenen Anlagenangabe; ein Einmodulprojekt ohne eigene Werte bekommt keine neue Meldung. |
| `WIRT_KWKG_ANLAGE_STICHTAG` | KWKG: {0} — Bestellung/Genehmigung nach dem {1} … für diese Anlage kein Zuschlag. | CHP Act: {0} — order/permit dated after {1} … no bonus for this unit. | `WirtschaftlichkeitCtrl.Anlagenauswahl` — § 6 KWKG **je Anlage**. Die Projektmeldung des Altstands bleibt daneben bestehen und gilt, solange keine Anlage ein eigenes Datum trägt. |
| `WIRT_KWKG_ANLAGE_FRIST` | KWKG: {0} — Inbetriebnahme nach Ablauf der Realisierungsfrist … | CHP Act: {0} — commissioning after the realisation deadline … | dieselbe |
| `WIRT_KWKG_HERLEITUNG_TRANCHEN` | {0} kW nach Leistungsanteilen: {1} → Mischsatz {2} ct/kWh ({3}, Stand {4}). | {0} kW by capacity tranches: {1} → blended rate {2} ct/kWh ({3}, as of {4}). | `KwkgSatzRechner.Mischsatz` — die Herleitung zeigt die **Tranchen**, nicht eine Klasse. |
| `WIRT_KWKG_HERLEITUNG_PAUSCHAL` | {0} kW und damit bis {1} kW, neue Anlage → {2} ct/kWh ({3}, Stand {4}) … | {0} kW and thus up to {1} kW, new unit → {2} ct/kWh ({3}, as of {4}) … | `KwkgSatzRechner.Pauschal` — § 7 Abs. 3a geht Abs. 1 und 2 vor. |
| `WIRT_KWKG_HERLEITUNG_KEIN_EIGENFALL` | Kein Tatbestand des § 6 Abs. 3 KWKG 2025 erfasst … | None of the cases of section 6 (3) KWKG 2025 recorded … | `KwkgSatzRechner.Vorschlag` — der **Regelfall**: Eigenstrom bekommt nicht generell einen Zuschlag. |
| `WIRT_KWKG_HERLEITUNG_N1_ZU_GROSS` | Der Tatbestand des § 6 Abs. 3 Nr. 1 gilt nur bis {0} kW; diese Anlage hat {1} kW … | The case of section 6 (3) no. 1 applies only up to {0} kW; this unit has {1} kW … | dieselbe |
| `WIRT_KWKG_HERLEITUNG_SATZ_FEHLT` | Der Satz „{0}" ist im Katalog „Gesetzliche Parameter" nicht gepflegt — kein Vorschlag. | The rate “{0}” is not maintained in the “Statutory parameters” catalogue — no proposal. | `KwkgSatzRechner` — nie ein geratener Ersatzwert (Regel wie `GesetzKatalog.Wert`). |
| `WIRT_KWKG_HERLEITUNG_OHNE_LEISTUNG` | Ohne gepflegte elektrische Nennleistung lässt sich kein Zuschlagssatz vorschlagen. | Without a maintained electrical rated capacity no bonus rate can be proposed. | dieselbe |

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `"NEUANLAGE"`, `"MODERNISIERT"`, `"NACHGERUESTET"` (`DbWerte.KWKG_ANLAGENART_*`) | **Persistenzwerte** — Inhalt von `Tab_Energieanlagen.KWKG_Anlagenart`, in SQL verglichen, nach der Auslieferung eingefroren. Die ComboBox in `Form_KwkgModule` trägt eine `Steuerwahl`-Klasse, die den **Wert** hält und den **Namen** anzeigt. Längster Wert 13 Zeichen ⇒ TEXT(24). |
| `"KEINER"`, `"NR1_BIS100KW"`, `"NR2_KUNDENANLAGE"`, `"NR3_STROMINTENSIV"` (`DbWerte.KWKG_EIGENFALL_*`) | dito — Inhalt von `…KWKG_Eigenstromfall`. Längster Wert 17 Zeichen ⇒ TEXT(24). |
| `"KATALOG_GENERATION"`, `"SYSTEM"` (`DbWerte.GESETZ_KATALOG_GENERATION`, `…KLASSE_SYSTEM`) | **Verwaltungszeile** der generationsweisen Nachsaat in `Tab_Gesetzesparameter` — Schlüssel der Persistenzschicht, nie auf dem Bildschirm; die Pflegemaske blendet die Klasse aus. |
| Die Normbezeichnungen der Herleitung (`§ 7 Abs. 1 KWKG 2025`, `§ 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 2 KWKG 2025` …) | Paragrafenzeichen, Zahlen und die amtliche Kurzbezeichnung des Gesetzes — kein übersetzbarer Wortbestand. Sie stehen als Textbaustein im Code und werden in den lokalisierten Rahmen `{3}` eingesetzt. Dasselbe gilt für die Klammer `„(50 kW, 7.476 h/a, 4,00/8,00 ct/kWh, 30.000 h)"` der Modulaufzählung — nur Zahlen und Einheitenzeichen (Muster `KwkgAnlagenauswahl.Klartext`). |
| Die Beschriftungen von `Form_KwkgModule` | Der Dialog folgt seinem Aufrufer `Form_WirtschaftlichkeitParameter`, und der ist **vollständig** unlokalisiert und im Code aufgebaut. Einzelne lokalisierte Zeilen darin wären keine Lokalisierung, sondern eine Inkonsistenz mehr. Die **Herleitungstexte** kommen dagegen aus `MyResource`, weil dieselben Texte auch im Ergebnis erscheinen. |

---

## Nachtrag zu Etappe E7 — Bericht und Mehrjahrestabelle (19.08.2026)

Etappe E7 stellt die **Kennzahlentabelle der Wirtschaftlichkeit vollständig** auf `MyResource`
um und legt die Texte der vier neuen Berichtsblöcke an. **81 Schlüssel** kommen hinzu, **sieben**
vorhandene ändern ihren Wert. Die Umstellung ist bewusst **vollständig** und nicht in Teilen —
der bisherige Mischzustand (fünfzehn deutsche Literale zwischen sieben lokalisierten Zeilen) war
das Ergebnis genau solcher Teilumstellungen.

### Geänderte Werte (7)

| Schlüssel | vorher | nachher | Grund |
|---|---|---|---|
| `WIRT_ZEILE_ENERGIESTEUER` | Energiesteuer-Gutschrift **Jahr 1** [€/a] | Energiesteuer-Gutschrift [€/a] | Der Zeitbezug gehört in den Tabellenkopf (`WIRT_ZEILE_JAHR1`), nicht in vier von zweiundzwanzig Zeilentiteln. Erst dadurch passt derselbe Schlüssel in Kennzahlen- **und** Mehrjahrestabelle. |
| `WIRT_ZEILE_STROMST_BEFREIUNG` | … **Jahr 1** … | ohne „Jahr 1" | dito |
| `WIRT_ZEILE_STROMST_ENTLASTUNG` | … **Jahr 1** … | ohne „Jahr 1" | dito |
| `WIRT_ZEILE_VERMIEDEN_ARBEIT` | Vermiedene Kosten **—** Arbeit [€/a] | Vermiedene Kosten**,** Arbeit [€/a] **(Ausweis)** | Komma statt Halbgeviertstrich (einheitliche Untergliederung); der Zusatz „(Ausweis)" ist keine Kosmetik — ohne ihn liest ein Prüfer die Zeile als addierbaren Erlös (E5-Protokoll, Übergabepunkt 6). |
| `WIRT_ZEILE_VERMIEDEN_LEISTUNG` | dito | dito | dito |
| `WIRT_ZEILE_VERMIEDEN_GESAMT` | Vermiedene Kosten gesamt [€/a] | Vermiedene Kosten, gesamt [€/a] (Ausweis) | dito |
| `WIRT_ZEILE_AUFSCHLAG` | Aufschläge auf den Strombezug [€/a] | … **(in Energiekosten enthalten)** | E5-Protokoll, Übergabepunkt 7: Wer Energiekosten und Aufschlag addiert, zählt doppelt. |

### Neu (81)

| Gruppe | Schlüssel | Inhalt |
|---|---|---|
| Kennzahlzeilen (20) | `WIRT_ZEILE_INVESTITION`, `…BETRIEBSKOSTEN`, `…ENERGIEKOSTEN`, `…STROMKOSTEN_BEZUG`, `…STROMKOSTEN_RESTSTROM`, `…CO2_BEHG`, `…EINSPEISEERLOES`, `…EINSPEISEERLOES_PV`, `…EINSPEISEERLOES_KWK`, `…KWKG`, `…VBH_ELEKTRISCH`, `…RESTWERT`, `…NETTOBARWERT`, `…KAPITALWERT_DIFF`, `…ANNUITAET`, `…AMORTISATION`, `…IRR`, `…GESTEHUNGSKOSTEN`, `…STAMM_REFERENZ`, `…JAHR1` | Die bisher deutschen Literale der Kennzahlentabelle. **Zwei Schlüssel für eine Zeile:** Der Titel der Stromkostenzeile hängt am Tarifmodus — im Rollenmodell trägt sie den **Reststrom**betrag (Kosten *mit* Anlage) und steht direkt neben den vermiedenen Kosten, die sich auf den Bezug *ohne* Anlage beziehen. |
| Mehrjahrestabelle (16) | `WIRT_MJ_TITEL`, `…HINWEIS`, `…JAHR`, `…INVEST_ERSATZ`, `…BETRIEB`, `…ENERGIE`, `…BEHG`, `…EINSPEISUNG`, `…NETTO`, `…BARWERT`, `…KUMULIERT`, `…RESTWERT_T`, `…PROBE`, `…ENTFAELLT`, `…NACHWEIS_TITEL`, `…NACHWEIS_HINWEIS` | Spaltenköpfe und Erläuterung des neuen Blocks. Die vier **Reihen**namen kommen aus den seit E4 vorhandenen, bis E7 toten `WIRT_REIHE_*`. |
| Betriebskostenblock (11) | `WIRT_BK_TITEL`, `…HINWEIS`, `…SP_POSITION`, `…SP_GRUPPE`, `…SP_BEMESSUNG`, `…SP_HERLEITUNG`, `…SP_BETRAG`, `…SUMME`, `…SZENARIOWERT`, `…ABWEICHUNG`, `…OHNE_SPALTEN` | Gliederung nach `Kostenart` (Zweck der E3-Spalte). |
| Kostenarten und Bemessungsarten (10) | `KOSTENART_KAPITALGEBUNDEN`, `…BETRIEBSGEBUNDEN`, `…BEDARFSGEBUNDEN`, `…SONSTIGE`, `…OHNE`; `BEMESSUNG_BETRAG`, `…PROZENT_INVESTITION`, `…EUR_PRO_H`, `…EUR_PRO_KWH`, `…PROZENT_BRENNSTOFFKOSTEN` | **Anzeigetexte** der gleichnamigen Steuerwerte aus `DbWerte`. Der Namensgleichklang ist Absicht und folgt dem Hinweis in `DbWerte.cs`; die Steuerwerte selbst bleiben deutsch, ASCII und eingefroren. |
| KWKG-Modultabelle (16) | `WIRT_KWKG_MODUL_TITEL`, `…MODUL_HINWEIS`, `WIRT_KWKG_SP_*` (9), `…SATZ_QUELLE_ANLAGE`, `…SATZ_QUELLE_PROJEKT`, `…DECKEL_STAFFEL`, `…ERSCHOEPFT_NIE`, `…HERLEITUNG_ZEILE` | Tabelle statt Aufzählung (E6-Protokoll, Übergabepunkt 1). |
| Nachweise und Matrix (8) | `WIRT_NACHWEIS_TITEL`, `…TARIF`, `…LAUFHINWEISE`, `WIRT_ERGEBNIS_VERALTET`, `WIRT_MATRIX_BEDARF`, `…BEDARF_HINWEIS` und die beiden Nachweisköpfe | Die Blöcke, die bisher nur in **einer** der beiden Ausgaben standen. |

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `"INVESTITION"`, `"BETRIEBSKOSTEN"`, … (`WirtZeile.Schluessel`) und `"INVEST_ERSATZ"`, `"NETTO"`, `"BARWERT"`, `"KUMULIERT"` (`MehrjahresSpalte.Schluessel`) | **Schlüssel**, nicht Anzeige: sprachneutral, ASCII, nur zum Wiederfinden einer Spalte im Renderer (Abschlusszeile der Mehrjahrestabelle). Sie stehen nirgends in der Datenbank und nirgends auf dem Bildschirm. |
| `Tab_Kostenfaktor.Bezeichnung` und `Tab_ProjektWerte.Gruppe` im Betriebskostenblock | **Datenwerte** des Anwenders („BHKW", „Wartung BHKW"). Sie stehen so in der Datenbank und werden nicht übersetzt — auch nicht im englischen Bericht. |
| Die Einheitenzeichen der Herleitung (`h/a`, `kWh/a`, `€/h`, `€/kWh`, `%`) | `BetriebskostenCtrl.SatzEinheit` / `…MengenEinheit` — reine Einheitenzeichen ohne Wortbestand, dieselbe Ausnahme wie bisher. |
| `„Jahres-Bezugsspitze: … (Basis der Leistungspreis-Staffel)"` und die Zonennamen `Winter HT` … | **Bestand**, von E7 nicht angefasst: Die Gesamtlokalisierung des Berichtsmoduls (82 + 18 Literale) ist ausdrücklich außerhalb dieser Etappe. E7 stellt die **Kennzahlentabelle vollständig** um und legt die **neuen** Blöcke von Anfang an lokalisiert an. |

---

## Nachtrag zu den Leitentscheidungen L12 und L13 — Bilanzierung (19.08.2026)

Die Nacharbeit zu den Abnahmebefunden A3 und A4 legt **30 Schlüssel** neu an; **kein vorhandener
Wert ändert sich**. Alle drei Dateien (`Resource.resx`, `Resource.en-US.resx`,
`Resource.Designer.cs`) führen jeden Schlüssel genau einmal.

| Gruppe | Schlüssel | Inhalt |
|---|---|---|
| Ausweis (13) | `BILANZ_AUSWEIS`, `BILANZ_METHODE_STROMGUTSCHRIFT`, `…_OHNE_GUTSCHRIFT`, `…_SUBSTITUTION`, `BILANZ_HERKUNFT_KATALOG`, `…_KATALOG_LEER`, `…_WAHL`, `BILANZ_BIOMASSE_NULL`, `…_VERBRENNUNG`, `BILANZ_NACHWEIS_JA`, `…_NEIN`, `BILANZ_JAHR_RUECKFALL`, `BILANZ_OHNE_WERT` | Die eine Zeile, die in Reiter, Word und Excel sagt, nach welchem Rechtsstand und mit welcher Konvention gerechnet wurde (`BilanzKonvention.Ausweis`). |
| Berichtshinweise (3) | `BILANZ_HINWEIS_DIN`, `BILANZ_HINWEIS_SUBSTITUTION`, `BILANZ_HINWEIS_BIOMASSE` | Die drei Sätze, die eine methodische Wahl als solche kennzeichnen — Wegfall der Gutschrift gegen DIN EN 15316-4-5, Herkunft des Substitutionsfaktors, Widerspruch der Regelwerke zur Biomasse. |
| Bilanzzeilen (2) | `BILANZ_ZEILE_BIOGEN`, `BILANZ_ZEILE_GUTSCHRIFT` | Die beiden Teilbeträge, die aus einer Wahl stammen und in den Summen der Emissionsbilanz stecken. |
| Parameterdialog (12) | `BILANZ_DLG_GRUPPE`, `…_JAHR`, `…_METHODE`, `…_METHODE_KATALOG`, `…_METHODE_GUTSCHRIFT`, `…_METHODE_OHNE`, `…_METHODE_SUBSTITUTION`, `…_BIOMASSE`, `…_BIOMASSE_NULL`, `…_BIOMASSE_VERBRENNUNG`, `…_NACHWEIS`, `…_HINWEIS` | Der neue Block in `Form_WirtschaftlichkeitParameter`. |

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `KATALOG`, `STROMGUTSCHRIFT`, `OHNE_GUTSCHRIFT`, `SUBSTITUTION`, `NULLANSATZ`, `VERBRENNUNG`, `NACHWEIS_JA`, `NACHWEIS_NEIN` | **Persistenzwerte** in `Tab_ProjektWirtschaftlichkeit`, in SQL damit verglichen: ASCII, eingefroren, in `DbWerte.cs`. Die Anzeigetexte dazu sind die `BILANZ_DLG_*`-Schlüssel oben. |
| Die 23 deutschen Literale der übrigen Zeilen von `Form_WirtschaftlichkeitParameter` | **Bestand** (Befund A6, offener Punkt 11): Die Gesamtlokalisierung der drei Dialoge aus E4 bis E6 ist ausdrücklich außerhalb dieser Nacharbeit. Der neue Block ist von Anfang an lokalisiert angelegt. |

---

## Nachtrag Lizenzverwaltung — `LIZ_*` (21.08.2026)

Die Designer-Umstellung von `Views/Admin/Form_LizenzVerwaltung.cs` holt zugleich deren
Lokalisierung nach: Die Maske war bis dahin **zu 100 % hart deutsch**. **31 Schlüssel** kommen
neu hinzu, **kein vorhandener Wert ändert sich**. Alle drei Dateien (`Resource.resx`,
`Resource.en-US.resx`, `Resource.Designer.cs`) führen jeden Schlüssel genau einmal; das Formular
selbst hat weiterhin **keine eigene `.resx`** — die Texte setzt `TexteSetzen()` nach
`InitializeComponent()`, im Designer stehen nur Platzhalter (Hausmuster
`Form_SpotpreisImport`).

Präfix `LIZ_`: Vor dieser Etappe gab es weder `LIZ_*` noch `LIZENZ_*` im Katalog.

| Gruppe | Schlüssel | Inhalt |
|---|---|---|
| Rahmen und Gruppen (5) | `LIZ_TITEL`, `LIZ_GRP_STATUS`, `LIZ_GRP_AKTIVIEREN`, `LIZ_GRP_AKTIONEN`, `LIZ_BTN_SCHLIESSEN` | Fenstertitel und die drei GroupBoxen. `LIZ_TITEL` ist **nur das Wort „Lizenz"** — der vollständige Titel `Lizenz — EPOS-Plan` entsteht in `TexteSetzen()`, weil `MDIMainForm.PRODUKTNAME` eine Anwendungskonstante und kein Übersetzungsgut ist. |
| Statusblock (3) | `LIZ_LINK_PORTAL`, `LIZ_DETAIL`, `LIZ_DETAIL_KEINE` | Der Verweis auf das Lizenzportal und die Detailzeile darüber. `LIZ_DETAIL` ist **zweizeilig mit vier Platzhaltern** (`{0}` LizenzId, `{1}` Firma, `{2}` Benutzer, `{3}` Gerätename) und ersetzt die bisherige Verkettung mit `Environment.NewLine`. |
| Aktivierungsblock (5) | `LIZ_LBL_SCHLUESSEL`, `LIZ_LBL_EMAIL`, `LIZ_BTN_AKTIVIEREN`, `LIZ_BTN_LIC`, `LIZ_HINWEIS_AKTIVIERUNG` | Eingabezeilen und der zweizeilige Datenschutzhinweis unter den Feldern. |
| Weitere Aktionen (2) | `LIZ_BTN_TRIAL`, `LIZ_BTN_FREIGEBEN` | Testversion anfordern und Gerät von der Lizenz lösen. |
| Fußzeile und Ablaufmeldungen (4) | `LIZ_HINWEIS_LIC_GELADEN`, `LIZ_STATUS_AKTIVIERUNG`, `LIZ_STATUS_TRIAL`, `LIZ_STATUS_FREIGABE` | Die Zeile links unten. Die drei `…_STATUS_*` gehen als Argument an `BedienungSperren(true, …)`. |
| Dateidialog (2) | `LIZ_DLG_LIC_TITEL`, `LIZ_DLG_LIC_FILTER` | Titel und Filterzeichenkette des `OpenFileDialog` für `.lic`-Dateien. Der Filter behält seine Pipe-Syntax und die Produktbezeichnung. |
| Meldungen (10) | `LIZ_MSG_EINGABE_FEHLT`, `LIZ_MSG_EMAIL_UNGUELTIG`, `LIZ_MSG_AKTIVIERT`, `LIZ_MSG_AKTIVIERUNG_FEHLER`, `LIZ_MSG_LIC_OHNE_SCHLUESSEL`, `LIZ_MSG_TRIAL_EMAIL`, `LIZ_MSG_TRIAL_OK`, `LIZ_MSG_TRIAL_FEHLER`, `LIZ_MSG_FREIGEBEN_FRAGE`, `LIZ_MSG_SERVER_NICHT_ERREICHBAR` | Sämtliche `MessageBox`-Texte der Maske. `LIZ_MSG_EMAIL_UNGUELTIG` trägt `{0}` für die geprüfte Adresse, `LIZ_MSG_FREIGEBEN_FRAGE` enthält die **Leerzeile** zwischen Frage und Folgesatz als echten Umbruch. |

**Zeilenumbrüche.** Die drei mehrzeiligen Werte (`LIZ_DETAIL`, `LIZ_HINWEIS_AKTIVIERUNG`,
`LIZ_MSG_FREIGEBEN_FRAGE`) stehen als **echte Umbrüche im `<value>`**, nicht als `\n`-Escape.
Zur Laufzeit liefert die Ressource damit `CRLF` — bei den Meldungen exakt das, was vorher
`Environment.NewLine` erzeugt hat; beim Label-Hinweis stand vorher ein einzelnes `\n`, was
optisch dasselbe Ergebnis hat.

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `MDIMainForm.PRODUKTNAME` (`EPOS-Plan`) und `LizenzManager.PORTAL_URL` | **Anwendungskonstanten**, keine Anzeigetexte: Produktname und Portaladresse sind in beiden Sprachen gleich. |
| `LizenzManager.StatusText()` | **Bestand außerhalb dieser Etappe.** Die Statuszeile oben und der Zusatz in der Erfolgsmeldung kommen aus dem Lizenzmodul; dessen Lokalisierung (`Allgemein/Lizenz/`) ist eine eigene Baustelle. |
| `LizenzServerAntwort.Meldung` | **Servertext.** Er kommt vom Lizenzserver und wird unverändert angezeigt; die Ressource ist nur der Rückfall, wenn keine Meldung geliefert wurde. |
| `Debug.WriteLine("Link konnte nicht geöffnet werden: …")` in `LinkOeffnen` | **Ablaufverfolgung**, keine Anzeige — dieselbe Ausnahme wie für `Console.WriteLine`- und `Exception`-Texte. |

---

## Nachtrag Berichtsseite — `BK_BER_*` (21.08.2026)

Die Designer-Umstellung von `Views/Bericht/UcBericht.cs` holt zugleich dessen Lokalisierung nach:
Die Seite war bis auf den Knopf „Projektvergleich + Bericht (alt)" (`BK_BTN_VERGLEICH_ALT`)
**vollständig hart deutsch**. **37 Schlüssel** kommen neu hinzu, **sieben vorhandene werden
mitbenutzt**, **kein vorhandener Wert ändert sich**. Alle drei Dateien (`Resource.resx`,
`Resource.en-US.resx`, `Resource.Designer.cs`) führen jeden Schlüssel genau einmal; das Control hat
weiterhin **keine eigene `.resx`** — die Texte setzt `TexteSetzen()` nach `InitializeComponent()`,
im Designer stehen nur Platzhalter (Hausmuster `Form_SpotpreisImport`).

**Präfix `BK_BER_` statt eines eigenen `BER_`.** `UcBericht` ist die Seite „Bericht" des Reiters
„Berichte &amp; Kosten" und trug mit `BK_BTN_VERGLEICH_ALT` schon vor dieser Etappe einen `BK_*`-
Schlüssel. Die `BK_*`-Gruppe führt bereits seitenweise Unterpräfixe — `BK_KOSTEN_*` für `UcBkKosten`,
`BK_UEB_*` für den Übernahme-Dialog, `BK_KOMP_*` für die Komponenten-Übernahme; die Nabe selbst
benutzt die flachen `BK_NAV_*`, `BK_KOPF_*`, `BK_SP_*`, `BK_ART_*`, `BK_MSG_*`. `BK_BER_*` reiht sich
genau dort ein. Ein eigenständiges `BER_*` hätte die vier Seiten desselben Reiters auf zwei
Katalogfamilien verteilt.

| Gruppe | Schlüssel | Inhalt |
|---|---|---|
| Seitentitel (1) | `BK_BER_TITEL` | Titelzeile des Dialog-Wrappers bzw. Seitenüberschrift, mit `{0}` für den Stammprojektnamen. Ersetzt die bisherige Verkettung in der Eigenschaft `Titel`. |
| Variantenliste (5) | `BK_BER_LBL_VARIANTEN`, `BK_BER_SP_SIMULATION`, `BK_BER_BTN_ALLE`, `BK_BER_BTN_KEINE`, `BK_BER_MSG_STAMM_REFERENZ` | Überschrift, vierter Spaltenkopf, die beiden Auswahlknöpfe und der Hinweis, dass die Stammzeile angehakt bleibt. `BK_BER_SP_SIMULATION` ist **„Simulation"** und damit nicht dasselbe wie `BK_SP_SIMSTAND` („Simulationsstand") der Übersichtsseite. |
| Bausteine (2) | `BK_BER_LBL_BAUSTEINE`, `BK_BER_LBL_RECHNEN` | Überschrift der Baustein-Checkliste und der graue Rechenhinweis darunter (Hinweis statt Option, Nutzeranforderung 15.08.2026). |
| Ausgabe und Ziel (6) | `BK_BER_LBL_AUSGABE`, `BK_BER_RB_WORD`, `BK_BER_RB_EXCEL`, `BK_BER_RB_BEIDE`, `BK_BER_LBL_ZIEL`, `BK_BER_BTN_DURCHSUCHEN` | Die drei Auswahlknöpfe des Ausgabeformats sowie Zielordnerzeile und „Durchsuchen…". **Nur die Beschriftungen** — die Steuerwerte bleiben Persistenz (siehe unten). |
| Schaltflächen (3) | `BK_BER_BTN_ERSTELLEN`, `BK_BER_BTN_SCHLIESSEN`, `BK_BER_BTN_ABBRECHEN` | „Erstellen" und der Doppelknopf rechts unten: `SetBusy` schaltet ihn während eines Laufs von „Schließen" auf „Abbrechen" um. |
| Statuszeile (5) | `BK_BER_STATUS_ERSTELLT`, `BK_BER_STATUS_WORD`, `BK_BER_STATUS_EXCEL`, `BK_BER_STATUS_ABGEBROCHEN`, `BK_BER_STATUS_FEHLER` | Alles, was durch `Melde()` in `lblStatus` läuft. `BK_BER_STATUS_ERSTELLT` trägt `{0}` für den Dateipfad und wird von beiden Wegen (regulär und „Vergleich (alt)") benutzt. |
| Meldungen und Fragen (10) | `BK_BER_MSG_WIRTSCHAFT_HINWEIS`, `BK_BER_MSG_HINWEISE`, `BK_BER_MSG_VERGLEICH_FERTIG`, `BK_BER_MSG_ERSTELLT_KOPF`, `BK_BER_MSG_LAUFFEHLER`, `BK_BER_FRAGE_START`, `BK_BER_FRAGE_OEFFNEN`, `BK_BER_FRAGE_OEFFNEN_WORD`, `BK_BER_FRAGE_OEFFNEN_BERICHT`, `BK_BER_DLG_ZIELORDNER` | Sämtliche `MessageBox`-Inhalte plus die Beschreibung des Ordnerdialogs. `BK_BER_FRAGE_START` trägt `{0}` für die Anzahl der Projekte, `BK_BER_MSG_LAUFFEHLER` `{0}` für die Ausnahmemeldung. `BK_BER_MSG_HINWEISE` („Hinweise:") steht in beiden Meldungen als eigener Baustein, weil der Aufzählungspunkt `• ` und die Umbrüche im Code bleiben. |
| Dateidialog (1) | `BK_BER_DLG_FILTER_WORD` | Filterzeichenkette des `SaveFileDialog` im Bestandsweg; behält ihre Pipe-Syntax. |
| Fenstertitel der Meldungen (4) | `BK_BER_TITEL_ERSTELLEN`, `BK_BER_TITEL_VERGLEICH`, `BK_BER_TITEL_FEHLER`, `BK_BER_TITEL_FEHLER_VERGLEICH` | Die vier Titelzeilen der `MessageBox`-Aufrufe. „Fehler" und „Fehler beim Erstellen des Berichts" sind zwei verschiedene Titel und bleiben deshalb zwei Schlüssel. |

**Mitbenutzte Schlüssel** (der Katalog führt gleiche deutsche Texte innerhalb einer Gruppe unter
einem Schlüssel — Etappe 1, Abschnitt 5.1). `UcBericht` zeigt dieselbe Variantenliste wie
`UcBkUebersicht` desselben Reiters und übernimmt deren Schlüssel unverändert:

| Schlüssel | Verwendung in `UcBericht` |
|---|---|
| `BK_SP_ART`, `BK_SP_BEZEICHNER`, `BK_SP_PROJEKTNAME` | die ersten drei Spaltenköpfe von `lvVarianten` |
| `BK_ART_STAMM`, `BK_ART_VARIANTE`, `BK_ART_STAMMPROJEKT` | die Zellwerte der Spalten „Art" und „Bezeichner" in `LadeDaten` |
| `BK_BTN_VERGLEICH_ALT` | unverändert; wandert nur aus `InitializeComponent` nach `TexteSetzen()` |

**Zeilenumbrüche.** Der einzige mehrzeilige Wert (`BK_BER_FRAGE_START`) steht als **echter Umbruch
im `<value>`**, nicht als `\n`-Escape; zur Laufzeit liefert die Ressource `CRLF` — exakt das, was
vorher die Literale `"\r\n\r\n"` erzeugt haben. Alle übrigen Meldungen setzen ihre Umbrüche
weiterhin im Code zusammen, damit Aufzählungspunkte und Pfadzeilen unverändert bleiben.

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `"Word"`, `"Excel"`, `"Beide"` in `LeseKonfigurationAusUi` und `LadeDaten` | **Persistenzwerte**: `BerichtsKonfiguration.Ausgabe` wird als JSON in `Berichtskonfiguration.KonfigJson` abgelegt und dort wieder verglichen. Deutsch und eingefroren; lokalisiert sind nur die drei Knopfbeschriftungen `BK_BER_RB_*`. |
| `"Projektvergleich_" + … + ".docx"` (Dateinamensvorschlag) | **Dateiname, kein Anzeigetext** — dieselbe Behandlung wie der Namensstamm `"_Bericht_"` in `BerichtCtrl.ErzeugeWord/ErzeugeExcel`. |
| `"({0}/{1}) {2}"` im Fortschrittsmelder | **Gerüst ohne Wortbestandteil**; der Text kommt aus `BerichtsDatenSammler.Fortschritt`. |
| `BerichtsKonfiguration.AlleBausteine[].Titel` (acht Bausteinnamen) | **Bestand außerhalb dieser Etappe.** Die Titel stehen im Modell (`Allgemein/Bericht/BerichtsKonfiguration.cs`) und werden auch vom Word- und Excel-Erzeuger benutzt; ihre Lokalisierung gehört zum Berichtsmodul, nicht zur Maske. |

---

## Nachtrag KI-Einstellungen — `KI_EINST_*` (21.08.2026)

Der Einstellungsdialog des KI-Assistenten stand bis dahin als `new Form()` mitten in
`Form_KiChat.EinstellungenOeffnen()` und war **zu 100 % hart deutsch** (bis auf die Checkbox
`KI_AKT_WEGB_EINSTELLUNG`). Mit dem Umzug nach `Views/Help/Form_KiEinstellungen.cs` +
`.Designer.cs` kommen **14 Schlüssel** neu hinzu, **kein vorhandener Wert ändert sich**. Alle drei
Dateien führen jeden Schlüssel genau einmal; der Dialog hat **keine eigene `.resx`** — die Texte
setzt `TexteSetzen()`, die wertabhängigen `WerteUebernehmen()`.

Präfix `KI_EINST_`: Die `KI_*`-Familie führt bereits `KI_AKT_*` (Aktionsbetrieb), `KI_KERN_*`
(Kern/Prüfungen), `KI_REG_*` (Werkzeugkatalog) und `KI_VORSCHAU_*`; `KI_EINST_*` ist die Maske.
Der Block steht in beiden `.resx` unmittelbar hinter der `KI_AKT_*`-Gruppe.

| Gruppe | Schlüssel | Inhalt |
|---|---|---|
| Rahmen (3) | `KI_EINST_TITEL`, `KI_EINST_BTN_OK`, `KI_EINST_BTN_ABBRECHEN` | Fenstertitel und die beiden Schaltflächen unten rechts. |
| Eingabezeilen (4) | `KI_EINST_LBL_SCHLUESSEL`, `KI_EINST_LBL_TAGESLIMIT`, `KI_EINST_LIMIT_FEST`, `KI_EINST_TIP_TAGESLIMIT` | API-Schlüsselzeile und Tageslimit. `KI_EINST_LIMIT_FEST` trägt `{0}` für den Zahlenwert aus `KiChatService.Tageslimit`; der zugehörige Kurzhinweis ist der ToolTip daneben. |
| Modell (3) | `KI_EINST_BTN_MODELL`, `KI_EINST_HINWEIS_MODELL`, `KI_EINST_HINWEIS_MODELL_NEU` | „Modell neu erkennen" und die **erste Zeile** des Hinweisblocks in ihren beiden Fassungen (beim Öffnen und nach dem Zurücksetzen), jeweils mit `{0}` für `KiChatService.MODELL`. |
| Hinweisabsätze (2) | `KI_EINST_HINWEIS_DATEN`, `KI_EINST_HINWEIS_KONTINGENT` | Der Datenschutz- und der Kontingentabsatz — unverändert im Wortlaut, nur getrennt abgelegt. |
| Rückmeldung im Verlauf (2) | `KI_EINST_MSG_GESPEICHERT`, `KI_EINST_MSG_GESPEICHERT_OHNE_SCHLUESSEL` | Die beiden Zeilen, die `Form_KiChat` nach OK in den Verlauf schreibt. Sie bleiben beim Aufrufer, weil dort auch das Speichern bleibt. |

**Warum der Hinweisblock in vier statt einem Schlüssel liegt.** Bisher stand der ganze Block in
einem Literal, und „Modell neu erkennen" tauschte die erste Zeile über
`hinweis.Text.Substring(hinweis.Text.IndexOf("\n\n"))` aus. Als **ein** mehrzeiliger
Ressourcenwert wäre daraus ein Laufzeitfehler geworden: Mehrzeilige `<value>` liefern `CRLF`, die
Suche nach `"\n\n"` liefe ins Leere und `Substring(-1)` wirft. Deshalb stehen Modellzeile,
Datenschutz- und Kontingentabsatz **einzeln und einzeilig** im Katalog; `HinweisSetzen()` fügt sie
mit demselben `"\n\n"` zusammen, das vorher im Literal stand. Die angezeigte Zeichenkette ist
dadurch **byteweise identisch** mit der bisherigen.

**Nicht lokalisiert — und warum**

| Wert | Grund |
|---|---|
| `KiChatService.MODELL` (z. B. `gemini-2.5-flash-lite`) | **Anbieterbezeichnung**, kein Übersetzungsgut; geht als `{0}` in die Modellzeile. |
| `KI_AKT_WEGB_EINSTELLUNG` | **Bestand** — die Checkbox war schon vor dieser Etappe lokalisiert und behält ihren Schlüssel. |
| Registrierungs- und Ablagenamen in `KiChatService` | **Persistenz**, keine Anzeige. |
---

## Nachtrag Paket S2 — Warnkriterienkatalog (27.08.2026)

Paket S2 setzt den **Warnkriterienkatalog W1–W5** aus Konzept 6.2 (Entscheidung F6) um. Neu sind
**14 Schlüssel**, **einer entfällt** (K2-O5); Bestand danach **2571 Schlüssel** in beiden `.resx`
und in `Resource.Designer.cs`.

Neues Präfix `SIMWARN_*` — die Texte des Katalogs. Sie erscheinen an **drei** Stellen mit
demselben Wortlaut: im Senkendialog beim Speichern, als Mouseover des Warn-Chips auf der
Erzeugerkarte und als Zeile im Laufprotokoll. Genau deshalb liegen sie im Katalog und nicht an
einer der drei Stellen.

### Neu (14)

| Schlüssel | DE | EN | Fundstellen |
|---|---|---|---|
| `SIMWARN_W1_ZIEL_AUSSERHALB_SET` | Anlage „{0}" (Rang {1}): Der Speicher „{2}" wird als {3} geladen, sein Klassen-Set lautet aber {4}. Der Kanal {5} fehlt — … | Unit "{0}" (rank {1}): … | `Warnkriterien.ZeilePruefen` |
| `SIMWARN_W2_BAUFORM_WIDERSPRUCH` | Speicher „{0}": Die Bauform „{1}" ist auf Warmwasser ausgelegt, das Klassen-Set lautet aber {2}. … | Storage "{0}": design type "{1}" … | `Warnkriterien.SpeicherPruefen` |
| `SIMWARN_W3_VORLAUF_ZU_NIEDRIG` | Anlage „{0}": Der Erzeuger-Vorlauf {1} °C liegt unter dem wirksamen Vorlauf {2} °C des Zielspeichers „{3}". … | Unit "{0}": the generator flow temperature … | `Warnkriterien.ZeilePruefen` |
| `SIMWARN_W5_QUELLE_OHNE_LADER` | Anlage „{0}": Der Speicher „{1}" ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen. … | Unit "{0}": storage "{1}" is its heat source … | `Warnkriterien.QuelleOhneLaderPruefen` |
| `SIMWARN_HART_RING` | Die Quellbezüge der Pufferspeicher bilden einen RING: {0}. … | The buffer-storage source references form a LOOP: {0}. … | `Warnkriterien.RingPruefen` |
| `SIMWARN_HART_LEERES_SET` | Speicher „{0}": Das Klassen-Set ist leer — kein Kanal entlädt ihn. … | Storage "{0}": the class set is empty … | `Warnkriterien.SpeicherPruefen` |
| `SIMWARN_TRENNER` | `␠+␠` | `␠+␠` | `Warnkriterien.Verbinden` — der Verbinder zwischen zwei Kanalnamen („Heizung + Brauchwasser"). Eigener Schlüssel, weil er zwischen **übersetzten** Wörtern steht. |
| `SIMWARN_DIALOG_KOPF` | Die Zuordnung ist zulässig und wird gespeichert, gilt aber als unplausibel: | The assignment is permitted and will be saved, but it is considered implausible: | `Form_Waermesenke.btnOk_Click` |
| `SIMWARN_KARTE_CHIP` | Konfiguration prüfen | check configuration | `Form_Simulation_Config.Karten.WarnChip` |
| `SIMWARN_KARTE_CHIP_TIP` | Der Warnkriterienkatalog (Konzept 6.2) meldet zu dieser Anlage: | The warning-criteria catalogue (concept 6.2) reports for this unit: | wie oben |
| `PSP_KLASSENSET_LEER` | ohne Nutzung | no usage | `Warnkriterien.KlassenSetAnzeige` — die Anzeige des LEEREN Sets. |
| `SIM_PUFFERGRUPPE_KOPF` | `— {0} —` | `— {0} —` | `Form_Waermesenke.FuelleCombo` — Gruppenkopf der nach Klassen-Set gruppierten Speicherauswahl. |
| `PSP_MELDUNG_KLASSENSETWECHSEL` | Die Nutzung des Pufferspeichers „{0}" wird von {1} auf {2} umgestellt. … | The usage of buffer storage "{0}" is being changed … | `Form_PufferSp_Projekt.KlassenSetWechselBestaetigt` |
| `PSP_TITEL_KLASSENSET_AENDERN` | Nutzung ändern | Change usage | wie oben |

### Entfallen (1)

| Schlüssel | Grund |
|---|---|
| `PSP_FEHLER_VERWENDUNG_PFLICHT` | **Ticket K2-O5.** „Die Verwendung ist ein Pflichtfeld: Heizung oder Brauchwasser (Konzept 5.1)." Repo-weit ohne Fundstelle — der Dialog `Form_PufferSp_Projekt` prüft seit Paket K2 das **Klassen-Set** (mindestens ein Häkchen) und nicht mehr die Verwendungs-ComboBox, und der Text nennt nur zwei der drei Klassen. Aus `Resource.resx`, `Resource.en-US.resx` und `Resource.Designer.cs` entfernt. |

### Wiederverwendet statt neu

| Fall | Schlüssel | Grund |
|---|---|---|
| Kriterium `HART_KURZSCHLUSS` | `SIM_PUFFER_QUELLE_UND_SENKE` | Der Katalog übernimmt den Guard aus `Form_Waermesenke.ListePruefen`. Ein eigener Text hätte denselben Sachverhalt zweimal formuliert — und die Dialogmeldung hätte sich für den Anwender ohne Grund geändert. |
| Dritter Abnehmerknoten im Schema | `KANAL_PROZESS_ANZEIGE` | „Prozesswärme" steht bereits als Kanalname im Katalog; ein zweiter Schlüssel für dasselbe Wort wäre eine Gabelung beim Übersetzen. |
| Prozess-Badge der Speicherknoten | `KANAL_PROZESS_ANZEIGE` | wie oben. |

### Ohne Fundstelle seit S2 (nicht entfernt)

Drei Schlüssel der abgelösten **Verwendungs-Sperre** stehen ohne Fundstelle da. Sie werden hier
nur vermerkt und **nicht** entfernt: Die Alt-Verwendung selbst wird erst mit Paket A1
(Schritt 51) stillgelegt, und der Aufräumschnitt gehört in dasselbe Paket.

| Schlüssel | Bis wann benutzt |
|---|---|
| `SIM_PUFFER_VERWENDUNG_PASST_NICHT` | `WaermesenkeClass.PufferPasst` — die dritte Prüfung ist mit S2 entfallen (Konzept 6.2). |
| `PSP_MELDUNG_VERWENDUNGSWECHSEL` | `Form_PufferSp_Projekt.VerwendungswechselBestaetigt` — abgelöst durch `PSP_MELDUNG_KLASSENSETWECHSEL` (K2-O8). |
| `PSP_TITEL_VERWENDUNG_AENDERN` | wie oben, abgelöst durch `PSP_TITEL_KLASSENSET_AENDERN`. |

---

## Nachtrag Paket A1 — Abriss der WS_-Spiegelung und der Alt-Zuordnung (27.08.2026)

Paket A1 (Dialogteil) reißt die `WS_*`-Spiegelung, die Alt-Zuordnung `Z_ProjektPufferSp` und den
Schalter des einkanaligen Altpfads ab. Damit fallen **35 Schlüssel** ohne Ersatz weg, **einer kommt
hinzu** (`SIM_PUFFER_PROZESS_KURZ`). Bestand danach **2533 Schlüssel** in beiden `.resx` und in
`Resource.Designer.cs` (DE und EN deckungsgleich, je Schlüssel eine Designer-Eigenschaft).

> Zählweise: gezählt sind die `data`-KNOTEN der `.resx` (XML), nicht die Zeilen mit `<data name=`.
> Der Kopfkommentar jeder `.resx` enthält vier Beispielzeilen dieser Form; ein Zeilen-`grep` zählt
> sie mit und liegt deshalb um vier zu hoch — daher die 2571 im S2-Nachtrag gegenüber den hier
> ausgewiesenen 2567 vor A1. Der Wert stimmt in beiden Sprachen und mit `Resource.Designer.cs` überein.

Jeder Schlüssel wurde vor dem Entfernen zweifach geprüft: repo-weit ohne Fundstelle im
aktuellen Stand **und** — für die 32 Schlüssel unter „durch A1 verwaist" — mit genau einer
Fundstelle im Stand vor A1 (`git grep … HEAD`), also nachweislich durch DIESEN Abriss verwaist.

### Entfallen: die drei Alt-Schlüssel der Verwendungs-Sperre (S2-O4, 3)

Sie standen seit S2 ohne Fundstelle da und wurden dort bewusst stehen gelassen, weil die
Alt-Verwendung erst mit Schritt 51 stillgelegt wird. Das ist jetzt geschehen.

| Schlüssel | Bis wann benutzt |
|---|---|
| `SIM_PUFFER_VERWENDUNG_PASST_NICHT` | `WaermesenkeClass.PufferPasst` — dritte Prüfung, entfallen mit S2 |
| `PSP_MELDUNG_VERWENDUNGSWECHSEL` | `Form_PufferSp_Projekt` — abgelöst durch `PSP_MELDUNG_KLASSENSETWECHSEL` |
| `PSP_TITEL_VERWENDUNG_AENDERN` | wie oben, abgelöst durch `PSP_TITEL_KLASSENSET_AENDERN` |

### Entfallen: Schalter „Zweikanalige Kaskade" und seine Automatiken (8)

Der Schalter war das Feature-Flag des einkanaligen Rechenwegs. Schritt 51 setzt
`Tab_Einstellungen.Kaskade_Zweikanalig` in Bestandsdaten auf WAHR und nimmt es aus der Weiche —
ein Schalter ohne Weiche wäre eine Zusage ohne Wirkung. Mit ihm gehen die Rückfrage vor der
Abwahl, die Meldung nach dem automatischen Einschalten und die beiden Statuszeilen.

| Schlüssel | Letzte Fundstelle |
|---|---|
| `SIM_KASKADE_SCHALTER` | `Form_Simulation_Config.Uebersicht.InitKaskadeSchalter` |
| `SIM_KASKADE_TOOLTIP` | wie oben (Mouseover) |
| `SIM_MSG_KASKADE_ABWAHL` | `checkBox_KaskadeZweikanalig_CheckedChanged` (Abwahl-Guard) |
| `SIM_MSG_KASKADE_AUTOMATISCH` | `KaskadeAutomatikNachAenderung` |
| `SIM_MSG_KASKADE_FRAGE` | `KaskadeAutomatikBeimSpeichern` |
| `SIM_STATUS_KASKADE_EIN` | Statuszeile beider Automatiken |
| `SIM_STATUS_KASKADE_AUS` | Statuszeile des Schalters |
| `SIM_TITEL_KASKADE` | Fenstertitel aller drei Meldungen |

### Entfallen: Übergangshinweis des Senkendialogs (2)

Er sagte, dass eine Brauchwasser-/Kombi-Senke ohne die zweikanalige Kaskade zwar gespeichert
wird, aber nicht mitrechnet. Der einkanalige Rechenweg ist abgerissen; die Aussage hat keinen
Gegenstand mehr.

| Schlüssel | Letzte Fundstelle |
|---|---|
| `SIM_MSG_BRAUCHWASSER_UEBERGANG` | `Form_Waermesenke.BrauchwasserUebergangsHinweis` |
| `SIM_MSG_BRAUCHWASSER_WP_ZUSATZ` | wie oben (Zusatz nur an der Wärmepumpe) |

### Entfallen: Alt-Zuordnung `Z_ProjektPufferSp` in der Oberfläche (21)

Die unsichtbare Zuordnungstabelle des Konfigurationsdialogs mit ihrem Zelleditor, der Dialog
`Form_KonfigPufferspeicher` (Konzept 10: entfällt), der Schwellendialog
`SpeicherregelungBearbeiten` und die Temperaturstufe „aus der Zuordnung" der Speicherkarte.

| Schlüssel | Letzte Fundstelle |
|---|---|
| `PSP_SPALTE_WAERMEERZEUGER`, `PSP_SPALTE_VORLAUF`, `PSP_SPALTE_RUECKLAUF` | Spaltenköpfe der Alt-Tabelle (`Form_Simulation_Config`-Konstruktor) |
| `PSP_TIP_ZUORDNUNG_ERZEUGER`, `…_SPEICHER`, `…_VORLAUF`, `…_RUECKLAUF`, `…_STAMMDATEN`, `…_STANDARD` | `listView1_MouseMove` — Mouseover je Spalte |
| `PSP_TITEL_TEMPERATUR_PRUEFEN` | Zelleditor der Alt-Tabelle (Paarprüfung B4-2) |
| `PSP_STATUS_ZUORDNUNG_FEHLGESCHLAGEN` | `btn_Speichern_Click` — Delete/Insert-Zyklus auf `Z_ProjektPufferSp` |
| `PSP_TITEL_SPEICHERREGELUNG`, `PSP_MSG_WP_OHNE_SPEICHER`, `PSP_MSG_SCHWELLEN_BEREICH`, `PSP_SPEICHERREGELUNG_FENSTERTITEL`, `…_KOPF`, `…_EINSCHALT`, `…_ABSCHALT`, `…_HINWEIS`, `PSP_STATUS_SPEICHERREGELUNG_GESPEICHERT` | `SpeicherregelungBearbeiten` — die Hysterese-Schwellen der Alt-Zuordnung; seit Etappe D1 ohne Aufrufer. Am Puffer selbst sind sie in `Form_PufferSp_Projekt` gepflegt |
| `PSP_KARTE_TEMP_ZUORDNUNG` | `Form_Simulation_Config.Karten.TemperaturHerkunft` — die mittlere Stufe der Temperatur-Vorrangkette. Schritt 51 hat die Werte einmalig an `Tab_Pufferspeicher` übergeben |

### Entfallen: Senkendialog auf EINEM Speicherweg (1)

| Schlüssel | Letzte Fundstelle |
|---|---|
| `SIM_ROLLE_HAUPTSENKE` | `WaermesenkeClass.Pruefen`/`KurzschlussMeldung` — beide prüfen seit A1 über ALLE Ränge und benennen die Rolle deshalb als RANG (`SIM_ROLLE_RANG`). `SIM_ROLLE_ZWEITSENKE` bleibt: Die Schemakante trennt weiter „erste" von „weitere" Ladung |

### Neu (1)

| Schlüssel | DE | EN | Fundstelle |
|---|---|---|---|
| `SIM_PUFFER_PROZESS_KURZ` | Puffer Prozessw. | Buffer process | `WaermesenkeClass.KurzformZuZiel` — die vierte Kurzform. Bis A1 kannte die Kurzformfamilie nur Heizung, Brauchwasser und Kombi, weil die Altspalten das S1-Ziel `PufferProzess` gar nicht ausdrücken konnten. Jetzt beschriftet dieselbe Funktion jede Senkenzeile, und ohne diesen Schlüssel stünde an einer Prozess-Puffersenke der lange Name der Auswahlliste. |

### Erweitert statt neu

| Fall | Schlüssel | Grund |
|---|---|---|
| Fehlerzeile beim Speichern | `SIM_STATUS_SENKE_FEHLER` | BLEIBT. Die Meldung hing am Rückgabewert von `WaermesenkeClass.Schreiben`; diese Schreibstelle ist entfallen, die AUSSAGE nicht. `Form_Waermesenke.ListeSpeichern` gibt den Erfolg jetzt über `SpeichernOk` an den Aufrufer weiter, der dieselbe Zeile zeigt. |
| Senkenanzeige aller Ränge | `SIM_HEIZKREIS_BEIDES`, `SIM_HEIZKREIS_NUR_HEIZWAERME`, `SIM_HEIZKREIS_NUR_WARMWASSER`, `SIM_PUFFER_HEIZUNG_KURZ`, `SIM_PUFFER_BRAUCHWASSER_KURZ`, `SIM_PUFFER_KOMBI_KURZ` | `Form_Waermesenke.SenkeAnzeige` ist mit A1 die EINE Anzeigefunktion für eine Senkenzeile. Sie hat die Kurzform des Ladeziels UND die Bedarfsart-Feinsteuerung des Heizkreises von `WaermesenkeClass.HauptsenkeAnzeige` übernommen; beide Altfassungen (`HauptsenkeAnzeige`, `ZweitsenkeAnzeige`) sind entfallen. An Karte, Übersicht und Schemaknoten steht damit Wort für Wort derselbe Text wie vorher. |

## Nachtrag Paket E1 — Ergebnis je Kanal (27.08.2026)

Paket E1 macht Bedarf und Deckung je Bedarfsart sichtbar (Konzept 4.4) und gibt dem
Prozess-Abnehmer im Schema eine eigene Kantenfarbe (Befund S2-O7). **Drei Schlüssel kommen
hinzu, keiner fällt weg.** Bestand danach **2538 Schlüssel** in beiden `.resx` und in
`Resource.Designer.cs` (DE und EN deckungsgleich, je Schlüssel eine Designer-Eigenschaft;
Zählweise wie im A1-Nachtrag: `data`-Knoten der XML).

| Schlüssel | DE | EN | Fundstelle |
|---|---|---|---|
| `SIM_SCHEMA_LEGENDE_PROZESS` | Prozessversorgung | Process supply | `SchemaAnsicht.LegendeZeichnen` — der fünfte Legendeneintrag zur neuen Kantenart `SchemaModell.Kantenart.Prozess` (violett #7E57A6). Bis E1 trug die Prozesskante die gemeinsame Versorgungsfarbe und war im Bild nicht von Heizkreis und Warmwasser zu unterscheiden. |
| `SIM_LABEL_BEDARF_JE_KANAL` | Wärmebedarf je Bedarfsart | Heat demand by demand type | `Form_Simulation_Detail.InitBedarfKanalzeilen` — Überschrift des Blocks unter „Gesamter Wärmebedarf" auf der Bedarfsseite. Die drei Zeilenbeschriftungen darunter nutzen die vorhandenen `KANAL_*_ANZEIGE` aus Paket K1. |
| `SIM_SPALTE_DECKUNG_KANAL` | Deckung {0} [MWh/a] | Coverage {0} [MWh/a] | `NavigatorUebersicht` (Konstruktor) — Kopf der drei neuen Spalten der Ergebnistabelle. Der Platzhalter nimmt den Kanalnamen aus `KANAL_*_ANZEIGE` auf; drei getrennte Schlüssel wären dreimal derselbe Satzbau. |

**Bewusst KEIN neuer Schlüssel** für die Kanalnamen selbst: `KANAL_HEIZUNG_ANZEIGE`,
`KANAL_BRAUCHWASSER_ANZEIGE` und `KANAL_PROZESS_ANZEIGE` (Paket K1) sind der eine Katalogeintrag
je Kanal und werden von E1 an drei weiteren Stellen wiederverwendet.

**Bericht:** Die sieben neuen Berichtstexte („davon Heizung", „Deckungsgrade je Bedarfsart" …)
stehen NICHT in `MyResource`, sondern im Wörterbuch `BerichtTexte._en` bzw. als
`LabelDe`/`LabelEn` im `KennzahlenKatalog` — das ist die Zweisprachigkeit des Berichtsmoduls
(Konzept Eckpunkt 10), und E1 folgt ihr, statt eine zweite danebenzustellen.
