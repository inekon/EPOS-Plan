# Status der iOS-Migration (EPOS-Plan)

**Stand 12.09.2026.** Diese Datei beantwortet eine einzige Frage: **was ist wann mit welchem
Ergebnis umgesetzt worden — und was ist offen.** Das *Konzept* (was die Pakete sind, welche Regeln
gelten, welche Risiken bestehen) steht unverändert in
[`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md).

**Die Fortschreibungsregel.** Je Schritt **eine Zeile hier**. Der ausführliche Block — Befund,
Umsetzung, Nachweis, Zahlen — kommt ins Protokoll unter
[`../ueberholt/Protokolle/`](../ueberholt/Protokolle) und wird von hier aus verwiesen, nie in diese
Datei hineinkopiert. Die vollständigen Statusblöcke bis zum 12.09.2026 (sie standen bis dahin im
Umsetzungskonzept) liegen in
[`../ueberholt/Protokolle/Statusbloecke/Umsetzungskonzept_iOS_Statusbloecke_bis_2026-09-12.md`](../ueberholt/Protokolle/Statusbloecke/Umsetzungskonzept_iOS_Statusbloecke_bis_2026-09-12.md).

**Keine Commit-Kennungen in dieser Datei.** Mit Auftrag #244 ist die Git-Geschichte am 12.09.2026
umgeschrieben worden; **jede Kennung, die vor diesem Tag notiert wurde, ist eine alte Kennung** und
zeigt im heutigen Zweig auf nichts mehr. Die Zuordnung alt → neu steht in
[`../ueberholt/Geschichte/commit-map_2026-09-12.txt`](../ueberholt/Geschichte/commit-map_2026-09-12.txt).
Datumsangaben gelten weiter, Kennungen nicht — deshalb führt diese Datei nur Daten.

**Lesart der Stände.** „**umgesetzt**" heißt: gebaut, getestet und gegen die Referenzbasis gefahren.
Wo zusätzlich ein Windows-Nachweis oder eine Sichtabnahme des Anwenders aussteht, steht
„**teilweise**" — die Arbeit ist getan, die Abnahme nicht. „**offen**" heißt: nicht begonnen.

---

## 1 Pakete iU0–iU13

| Paket | Stand | letzter Schritt | in einem Satz | Nachweis |
|---|---|---|---|---|
| **iU0** Klärung, Sicherung, Rückbau | umgesetzt | 02.09.2026 | Chart- und Grid-Masken ausgezählt (18/19), `CSExeCOMServer` und die netfx-Sicherung aus dem Repo, Referenzbasis eingefroren | [`../ueberholt/Umsetzung_iU0_iU1_Nachweise.md`](../ueberholt/Umsetzung_iU0_iU1_Nachweise.md) |
| **iU1** .NET 10, Windows, CI | teilweise | 02.09.2026 | Projektmappe baut und testet ohne Visual Studio, CI Kern + Windows grün (iZ1 hier erreicht) | dieselbe Nachweisliste; offen: Vollreferenzlauf 332/332 auf Windows |
| **iU2** Mac und Apple-Konto | offen | — | nicht begonnen: Hardware, Xcode, Apple Developer Program | — |
| **iU3** Machbarkeits-Spike | umgesetzt | 02.09.2026 | **Gate iZ3 bestanden** — Projekt 1030 byte-gleich auf x64-Linux **und** arm64-macOS | Entscheidungsregister § 2.2/2.3 |
| **iU4** `EPOS.Kern` herauslösen | teilweise | 03.09.2026 | Kern liegt physisch und plattformfrei (268 Dateien, CA1416 = 0), macOS-Lauf grün | offen: Vollreferenzlauf 332/332 auf Windows (iZ4) |
| **iU5** Statics kappen, Dienste | teilweise | 03.09.2026 | Wächter `Program.*` im Kernsatz = 0 Treffer (iZ5a), Referenzläufe unverändert byte-gleich | offen: Bedienprobe auf Windows (Bericht, Katalogimport, Lizenz, KI-Chat, 12 Gewerke, Sprachwechsel) |
| **iU6** Datenzugriff plattformfrei | teilweise | 06.09.2026 | `EPOS.Kern` nennt `System.Data.OleDb` nicht mehr, CA1416 87 → 0; Nachtrag iU6‑O‑1 (Ladeweg) am 06.09. | offen: Erststart-Migration aus `.accdb`, Solar-/Pufferspeicherdialoge, die 36 `RecordSet`-Views |
| **iU7** Charts und Berichte | teilweise | 03.09.2026 | `ChartRenderer` und Berichtsausgabe liegen im Kern (SkiaSharp), ChartProben grün auf ubuntu und macos | offen: Sichtvergleich der Berichte am Gerät (der Bildvergleich alt/neu entfällt, iF23) |
| **iU8** `EPOS.UI`, erster Dialog | teilweise | 06.09.2026 | **Modell-C-Stichtag iZ5 erreicht** — der erste Blazor-Dialog läuft produktiv, die WinForms-Fassung ist gelöscht; Fensteranteil (iU8‑E‑1) und hausweite Formularregel (iU8‑E‑2, Restumstellung #91) nachgezogen | [`../ueberholt/Umsetzung_iU8_Nachweise.md`](../ueberholt/Umsetzung_iU8_Nachweise.md) |
| **iU9** Masken in Wellen | teilweise | 12.09.2026 | **W0 bis W16c umgesetzt, Meilenstein M9 abgeschlossen** (04.09.); seither Abnahmebefunde und Aufträge des Anwenders, zuletzt am 12.09. Eine Designer-Maske bleibt (`Form_HelpPopup`, fällt mit iU11) | § 2 dieser Datei, [`../ueberholt/Umsetzung_iU9_Nachweise.md`](../ueberholt/Umsetzung_iU9_Nachweise.md) |
| **iU10** iOS-Hülle `EPOS.iOS` | teilweise | 06.09.2026 | Sieben der acht Schritte umgesetzt, seither je Welle im CI geprüft (30. `ios.yml`-Lauf am 06.09. grün, Referenzvergleich byte-gleich); **iU10‑9** (iL5-Wizard in `EPOS.UI/Seiten/`, `IosNavigation`) offen | [`Umsetzung_iU10_Nachweise.md`](Umsetzung_iU10_Nachweise.md) |
| **iU11** Kataloge, Importe, Feinschliff | offen | — | nicht begonnen; Umfang nach iF2 | — |
| **iU12** Absicherung und Betrieb | offen | — | nicht begonnen | — |
| **iU13** TestFlight und Vertriebsweg | offen | — | nicht begonnen | — |

**Meilensteine.** iZ1, iZ3, iZ4, iZ5a und iZ5 sind am 02./03.09.2026 erreicht — iZ1, iZ4 und iZ5
allerdings „hier", also auf Linux; ihre Windows-Abnahme steht aus. iZ2, iZ6 und iZ7 hängen an iU2
bzw. iU10/iU13.

---

## 2 Wellen des Pakets iU9 (W0–W16c)

Alle Wellen sind am 03./04.09.2026 umgesetzt worden; jede WinForms-Fassung ist im selben Schritt
gelöscht worden (Regel M1). Die Spalte „Protokoll" verweist auf den ausführlichen Bericht.

| Welle | umgesetzt | in einem Satz | Protokoll |
|---|---|---|---|
| **W0** | 03.09.2026 | Die K6-Liste ist abgetragen: neun unerreichbare Masken stillgelegt statt portiert (iF29), 25 Dateien / 10 625 Zeilen gelöscht | [`../ueberholt/Erreichbarkeit_2026-09-03.md`](../ueberholt/Erreichbarkeit_2026-09-03.md) |
| **W1** | 03.09.2026 | Sieben Masken der Kostenvorlagen und der Wirtschaftlichkeit → sechs Razor-Komponenten; Baustein `Optionsgruppe` | [`iU9_W1`](../ueberholt/Protokolle/Reporting/iU9_W1_Blazor_Port_Protokoll.md) |
| **W2** | 03.09.2026 | Sechs Masken → vier Komponenten; die **Sprungbrücke** (Schlüssel → `Form`) entsteht | [`iU9_W2`](../ueberholt/Protokolle/Reporting/iU9_W2_Blazor_Port_Protokoll.md) |
| **W3** | 03.09.2026 | Vier Masken am Energieträger → vier Komponenten; Bausteine `Dateiwahl`, `Zeilenwahl`, `Textfeld`, `Raster` | [`iU9_W3`](../ueberholt/Protokolle/Reporting/iU9_W3_Blazor_Port_Protokoll.md) |
| **W4** | 03.09.2026 | Sieben Masken; die beiden Hosts der Kostenseite (5 216 Zeilen) fallen; Bausteine `Ueberlagerung`, `Rueckfrage`, `Zeilenraster` | [`iU9_W4`](../ueberholt/Protokolle/Reporting/iU9_W4_Blazor_Port_Protokoll.md) |
| **W5** | 03.09.2026 | Erste Welle mit **Seiten** statt Dialogen — der Reiter „Berichte & Kosten" ist Blazor, in einer WebView | [`iU9_W5`](../ueberholt/Protokolle/Reporting/iU9_W5_Blazor_Port_Protokoll.md) |
| **W6** | 03.09.2026 | Sieben Erzeugerkacheln → sieben Komponenten, vier davon zugleich Assistentenseiten | [`iU9_W6`](../ueberholt/Protokolle/Reporting/iU9_W6_Blazor_Port_Protokoll.md) |
| **W7** | 03.09.2026 | Acht Masken Wärmepumpe und Solarthermie; `ChartRenderer.Kennlinien`, Katalogfilter im Kern | [`iU9_W7`](../ueberholt/Protokolle/Reporting/iU9_W7_Blazor_Port_Protokoll.md) |
| **W8** | 03.09.2026 | Zehn Masken der Bedarfstypen → vier Komponenten über die Ausprägung `BedarfsArt`; drei Bedarfsbilder im Renderer | [`iU9_W8`](../ueberholt/Protokolle/Reporting/iU9_W8_Blazor_Port_Protokoll.md) |
| **W9** | 03.09.2026 | Acht Masken der Bedarfskacheln → fünf Komponenten; **alle elf Kacheln des Startbilds sind Blazor**, zehn der dreizehn Assistentenseiten | [`iU9_W9`](../ueberholt/Protokolle/Reporting/iU9_W9_Blazor_Port_Protokoll.md) |
| **W10a** | 03.09.2026 | Die sieben Dialoge der Simulationskonfiguration → sieben Komponenten; Bausteine `Bildkarte` und `Jahresgang` | [`iU9_W10a`](../ueberholt/Protokolle/Reporting/iU9_W10a_Blazor_Port_Protokoll.md) |
| **W10b** | 04.09.2026 | `Form_Simulation_Config` (4 558 Z.) → Seite `SimulationKonfigSeite` mit SVG-Hydraulikschema; `SchemaModell`/`SchemaLayout` im Kern | [`iU9_W10b`](../ueberholt/Protokolle/Reporting/iU9_W10b_Blazor_Port_Protokoll.md) |
| **W11a** | 04.09.2026 | Ergebnisrechnung als sieben DTOs im Kern, **nebenläufiger Simulationslauf** mit Fortschritt und Abbruch, sieben Ergebnisbilder, Baustein `Fortschritt` | [`iU9_W11a`](../ueberholt/Protokolle/Reporting/iU9_W11a_Kern_Protokoll.md) |
| **W11b** | 04.09.2026 | Ergebnisseite der Simulation (zehn Blätter, Variantenvergleich); **`Form_Simulation_Detail` mit 7 766 Zeilen ist gelöscht**. In diesem Block stand bis 12.09. auch die laufende Tagesarbeit (§ 3) | [`iU9_W11b`](../ueberholt/Protokolle/Reporting/iU9_W11b_Blazor_Port_Protokoll.md) |
| **W12** | 04.09.2026 | Sechs Masken der Ganglinien; die AP5-Importkette stand zweimal wörtlich im Bestand und ist jetzt **ein** Kern-Ablauf | [`iU9_W12`](../ueberholt/Protokolle/Reporting/iU9_W12_Blazor_Port_Protokoll.md) |
| **W13** | 04.09.2026 | Sechs Masken der Katalog-Importe → drei Komponenten (`KatalogImportDialog` mit vier Ausprägungen, transaktional); zwanzig eingefrorene Importproben | [`iU9_W13`](../ueberholt/Protokolle/Reporting/iU9_W13_Blazor_Port_Protokoll.md) |
| **W14a** | 04.09.2026 | Sieben Masken der Erzeuger-Admin → drei Komponenten; Heizkessel-Brennstoffkette berichtigt; **Erreichbarkeit 0 nein / 0 verwaist / 0 unklar** | [`iU9_W14a`](../ueberholt/Protokolle/Reporting/iU9_W14a_Blazor_Port_Protokoll.md) |
| **W14b** | 04.09.2026 | Vier Masken der Bedarfs-Admin → zwei Komponenten (`BedarfsArt` in drei Ausprägungen); `ToolsClass` fällt | [`iU9_W14b`](../ueberholt/Protokolle/Reporting/iU9_W14b_Blazor_Port_Protokoll.md) |
| **W14c** | 04.09.2026 | Fünf Masken der Verwaltung; Baustein `Baumansicht`; **`ChartManager` fällt — die MS-Chart-Bindung endet**, WFO1000 6 → 0, Schemaschritt 62 | [`iU9_W14c`](../ueberholt/Protokolle/Reporting/iU9_W14c_Blazor_Port_Protokoll.md) |
| **W15a** | 04.09.2026 | Das Projekt: Baustein `ProjektListe` (vier Projektlisten werden eine), Transfer und Kopie; **der seit der SQLite-Umstellung kaputte Projektimport ist repariert** (B55) | [`iU9_W15a`](../ueberholt/Protokolle/Reporting/iU9_W15a_Blazor_Port_Protokoll.md) |
| **W15b** | 04.09.2026 | Hilfe und KI: `KiChatService` (1 751 Z.) in den Kern hinter die Naht `IKiAusfuehrung`, Bausteine `Gespraechsverlauf` und `KiKnopf` | [`iU9_W15b`](../ueberholt/Protokolle/Reporting/iU9_W15b_Blazor_Port_Protokoll.md) |
| **W15c** | 04.09.2026 | Lizenz und Erststart: drei Masken, `LizenzManager.Bewerten` im Kern, **die ersten Lizenztests überhaupt** (+79 Kern-, +67 bunit-Fälle) | [`iU9_W15c`](../ueberholt/Protokolle/Reporting/iU9_W15c_Blazor_Port_Protokoll.md) |
| **W16a** | 04.09.2026 | Der Projektassistent: Baustein `Assistent`, Seite `AssistentSeite` (13 Seiten); `WizardParent` und die Wizard-Masken fallen (26 Dateien); **Nachweis N6** | [`iU9_W16a`](../ueberholt/Protokolle/Reporting/iU9_W16a_Blazor_Port_Protokoll.md) |
| **W16b** | 04.09.2026 | Die Startseite: `Form_Start` → Seite `Startseite` mit sechs Reitern und 21 Kacheln; der Altzweig `FormMain` fällt ohne Nachfolge (34 Dateien, 13 019 gegen 5 549 Zeilen); **Nachweis N7** | [`iU9_W16b`](../ueberholt/Protokolle/Reporting/iU9_W16b_Blazor_Port_Protokoll.md) |
| **W16c** | 04.09.2026 | Das Hauptfenster: Baustein `Menueband` (54 Punkte aus dem Designer erzeugt, **Nachweis N4**), Seite `Hauptfenster`, `MDIMainForm` 873 → 129 Zeilen; **die Mischphase M9 endet** | [`iU9_W16c`](../ueberholt/Protokolle/Reporting/iU9_W16c_Blazor_Port_Protokoll.md) |

**Windows-Abnahme der Wellen.** Der Anwender hat ab dem 04.09.2026 abgenommen; die Befunde und
Wünsche (W*‑B‑*, W*‑E‑*) sind in denselben Blöcken nachgetragen und umgesetzt worden. Offen
markierte Wellen siehe § 4.

---

## 3 Aufträge des Anwenders

Nummerierte Aufträge, soweit sie in den Statusblöcken benannt sind. Alle Daten 2026.

| Nr. | Datum | Gegenstand | Ergebnis |
|---|---|---|---|
| **#76** | 05.09. | Katalogdialoge: altes Schema nebeneinander, Umbruch auf schmalem Schirm | Empfehlung umgesetzt — Baustein `Zweispaltenauswahl` in allen betroffenen Dialogen |
| **#91** | 05./06.09. | Restumstellung auf die hausweite Formularregel (iU8‑E‑2) | in drei Paketen abgeschlossen: 41 weitere Dateien, alle `epos-feldpaar` gefallen |
| **#155** | 09.09. | Befund des Wiki-Agenten (Erststart, Sicherungspunkt) | Auslöser für #157 und #158 |
| **#157** | 09.09. | Erststart ohne Altbestand | umgesetzt; drei Folgeentscheide → #160, #161, #162 |
| **#158** | 09.09. | Sicherungspunkt SQLite-fest | umgesetzt |
| **#160** | 09.09. | Auslieferungsvorlage als Werkzeug (#157‑E‑2) | umgesetzt |
| **#161** | 09.09. | Deinstallations-Rückfrage (#157‑E‑3) | umgesetzt |
| **#162** | 09.09. | Erststart aus der Auslieferungsvorlage (#157‑E‑1) | umgesetzt |
| **#164** | 10.09. | Build-Skript am Anwenderort (Inno Setup unter `C:\Waermeplan\WP_Plan\Setup`) | umgesetzt |
| **#166** | 10.09. | `BetriebskostenBaugroesseTests` (Anwenderbefund H4c) | auf Linux grün; deckte #167 auf |
| **#167** | 10.09. | Kultur-Leck in `EPOS.Kern.Tests` | geschlossen; Restpunkt → #168 |
| **#168** | 11.09. | EINE Kulturvorrichtung für `EPOS.UI.Tests` (#167‑O‑1) | umgesetzt |
| **#169** | 10.09. | `KlimadatenDialogTests` deterministisch (W16b‑O‑2) | umgesetzt |
| **#170** | 11.09. | Mehrspeicherkonzept vom Windows-Rechner nach `ios_migration_september` integriert | umgesetzt; Nacharbeiten → #170c, #172, #173 |
| **#171** | 11.09. | 291 Fundstellen aus der Integration | im Zuge von #170 abgearbeitet |
| **#172/#173** | 11.09. | tote Links, xUnit-Analysewarnungen | behoben |
| **#174** | 11.09. | Prüfprojekt 1046 und Referenzbasis R7 (SP‑O‑8) | angelegt; R7 ist seither die Bezugsbasis |
| **#178** | 11.09. | Schemaschritt 74: `Tab_SpeicherAuslegung` STRICT | umgesetzt |
| **#183** | 11.09. | Stromspeicher-Dialoge Paket P1 (SD‑Q3/Q4/Q5) | umgesetzt |
| **#184** | 11.09. | Stromspeicher-Dialoge Paket P2 (SD‑Q6/Q7) | umgesetzt |
| **#185** | 11.09. | Projektlauf mit aktivierter Flotte bricht an fehlenden Parametern ab | behoben |
| **#186** | 11.09. | Abnahmeliste: Kostenverwaltung und Gruppenkopf | behoben |
| **#187** | 11.09. | Abnahmeliste: doppelter Dialogtitel in der Überlagerung | behoben |
| **#188/#189** | 11.09. | Schema kürzt lange Bezeichner; zweiter Punkt der Abnahmeliste | behoben |
| **#190** | 11.09. | Erzeuger ohne Kaskadenplatz (HK‑E‑1a) | umgesetzt; Designer neu erzeugt |
| **#192** | 11.09. | Stromspeicher-Dialoge Paket P3 (Ansicht) | umgesetzt |
| **#193** | 11.09. | Stromspeicher-Dialoge Paket P4 (Größen-Sicht) | umgesetzt |
| **#195** | 11.09. | SQL-Dialektprüfer: Namensauflösung in drei Stufen | umgesetzt |
| **#196** | 11.09. | Größen-Sicht (P4) in der Ansicht (P3), iOS-Stilblatt | umgesetzt; 17 neue Fälle |
| **#199** | 11.09. | Assistent im Dialog, Stufe S1 (Wege 1 + 2) | umgesetzt |
| **#200** | 11.09. | Assistent im Dialog, Stufe S2 („Feldzustand mitgeben") | umgesetzt |
| **#201** | 11.09. | Assistent im Dialog, Stufe S3 | umgesetzt; Restpunkte → #211, #214 |
| **#203** | 11.09. | Bedienungsseiten mit Repo-Quelle, Wiki-Upload (Revisionen 537–539) | umgesetzt |
| **#206** | 11.09. | Stromspeicher-Auslegung P5: ein Modus statt zwei | umgesetzt |
| **#207** | 11.09. | Simulationsablauf Stufe S1 (SIM‑Q1 bis Q6) | umgesetzt; Abnahme → #216 |
| **#208** | 11.09. | Simulationsablauf Stufe S2: iOS erreicht die Simulation | umgesetzt |
| **#210** | 11.09. | Speicherflotte im Projektlauf: zwei Einheiten bleiben zwei | behoben |
| **#211** | 11.09. | tote Markdown-Links, Schreibschutz-Kennzeichen für `feld_setzen` | behoben |
| **#212** | 11.09. | Stromspeicher-Import „blinkt" bei 6 654 Zeilen | drei Zeichenkosten beseitigt; Restfall → #235 |
| **#213** | 11.09. | Stromspeicher-Reiter: Leistungsverteilung erst ab zwei Einheiten | umgesetzt |
| **#214** | 11.09. | Fortschrittsbalken und Abbrechen bei Rechenaktionen des Assistenten | umgesetzt |
| **#215** | 11.09. | Paket P7: adaptive Lastspitzenkappung, die kausale Ratsche | umgesetzt |
| **#216** | 11.09. | Windows-Abnahme der Simulationsansicht (#207, SIM‑E‑1) | abgenommen |
| **#217** | 11.09. | „Projekt löschen" (Kachel) stürzte ab | behoben |
| **#218** | 11.09. | Hilfe-Pille mit EPOS-Marke (Varianten C + D) | umgesetzt |
| **#219** | 11.09. | Hilfe-Assistent: Eingabe ohne Tastatur, toter Verweis „Online-Dokumentation" | behoben |
| **#220** | 11.09. | Startseiten-Reiter „Simulation": die Kachel rechnet an Ort und Stelle | umgesetzt; Layout → #233 |
| **#221** | 11.09. | EINE Hilfe-Pille je Bildschirm (KI‑D‑E‑1) | umgesetzt |
| **#222** | 11.09. | Simulationsergebnis: EINE Übersicht als Dashboard Wärme \| Strom (SIM‑E‑3) | umgesetzt |
| **#224** | 11.09. | Stromspeicher-Auslegung: Station „4 Optimierung" als Seite, Feinraster | umgesetzt |
| **#225** | 11.09. | Ablaufleiste der Stromspeicher-Auslegung bündig, toter Anzahl-Schritt fällt | umgesetzt |
| **#226** | 11.09. | Größen-Sicht: Rasterkarte und Schnitte folgen der Größenkopplung | umgesetzt |
| **#227** | 11.09. | Hilfe-Assistent: Startzeile „Aktuellen Dialog erklären" mit Kontext | umgesetzt |
| **#228** | 11.09. | Hilfe-Assistent öffnet wieder aus Hauptmenü und F1 | behoben |
| **#229** | 11.09. | Die EPOS-Marke als Programmsymbol | umgesetzt; Windows-Abnahme 12.09. „ok" |
| **#230** | 11.09. | Windows-CI rot seit Lauf 262: dreizehn Kern-Testfälle prüfen deutsche Texte | behoben; Messbefund dazu in #231/#232 berichtigt |
| **#231/#232/#232b** | 12.09. | Race der prozessweiten Kulturpinnung | CI ohne parallele Sammlungen, `SpeicherEngine/Kulturweitergabe`, Kultur threadgebunden statt prozessweit |
| **#233** | 12.09. | Startseiten-Reiter „Simulation" als Bedienblock (Layout, Lesbarkeit) | umgesetzt: linker Bedienblock 360 px, Ergebnisspalte rechts, Kontrast 6,8 → 13,4 : 1 |
| **#234** | 12.09. | „Wärme Produktion Chart" zu Beginn leer, Achseneinheiten fehlen, zweite Achse falsch belegt | behoben: Vorbelegung, Sitzungsgedächtnis, Achsentitel, zweite Achse nur für den Speicherinhalt |
| **#235** | 12.09. | Auswahlliste flackert bei großen Datenlisten (Restfall aus #212) | im echten Browser gemessen (Playwright) und behoben |
| **#236** | 12.09. | Strombedarf in der Übersicht (Reiter „Simulation") falsch dargestellt | behoben: die Übersicht zeichnete ein Nullobjekt; der Kern rechnete richtig |
| **#237** | 12.09. | Variante aus einem bestehenden Projekt anlegen | umgesetzt: Kontrollkästchen + Projektliste im Dialog, Zielname aus einer Kernregel |
| **#238** | 12.09. | Variantenname doppelt im Auswahlfeld (Berichte & Kosten › Übersicht) | behoben; Überlappung mit korrigiert |
| **#239** | 12.09. | „Stromspeicher hinzufügen" nur als Duplikat, angelegte Speicher fehlen in der Auswahl | behoben: ein Weg im Kern für Katalogsatz, Projektanlage und Vorbelegung |
| **#240** | 12.09. | Sieben Kleinpunkte ohne eigenen Auftrag in einem Schritt | umgesetzt; ChartProben 59 → 61 Bilder |
| **#241** | 12.09. | Alle Markdown-Papiere nach `Dokumentation/`, getrennt in `aktuell/` und `ueberholt/` | umgesetzt: 303 Dateien, Index `Dokumentation/LIESMICH.md`, Wache `DokumentationLinkWacheTests` |
| **#242** | 12.09. | Repository aufräumen (`.work/`, `DB-Backup/`, `.bak`, Spike-Ordner) | umgesetzt: 31 Dateien / 70,2 MB entfernt, Wache `RepositoryOrdnungWacheTests`, Aufräumkonzept |
| **#243** | 12.09. | Git LFS für VDI-Archive und Testdatenbank (AUF‑Q2), Fremdquellen nach `Quellen/` (AUF‑Q3) | umgesetzt: 69 LFS-Dateien (164 MiB), Zeiger-Schutz an drei Öffnungsstellen, Workflows mit Zwischenlager |
| **#244** | 12.09. | Git-Geschichte umschreiben (AUF‑Q1, Variante 2 mit Access-Altbeständen) | umgesetzt: Pack 554 → 358 MiB, Bäume unverändert, **alle Commit-Kennungen sind neu** (Karte `commit-map_2026-09-12.txt`) |
| **#245** | 12.09. | Fokus springt beim Tippen aus der Suchraum-Tabelle der Station „4 Optimierung“ | behoben: `@key` auf Wertidentität statt Objektreferenz, Hausregel in `EPOS.UI/CLAUDE.md`, drei bunit-Wachen |
| **#246** | 12.09. | Konzept Stromspeicher-Dialoge Kapitel 8: zwei Suchmethoden „Größe suchen“ und „Stückzahl suchen“ (SD‑E‑10), Mockup v2 | umgesetzt im Konzept; Entscheid SD‑E‑10 getroffen |
| **#247** | 12.09. | Station „4 Optimierung“ mit drei Suchoptionen, Karte je Einheit, Übernahme der Einheiten ins Projekt | umgesetzt (Engine, Kern, Oberfläche, Wiki); Windows-Abnahme steht aus |
| **#248** | 13.09. | Kleinpunkte ohne Auftrag: K1 Kommentare zur Zeilenhöhe der Katalog- und Projektliste auf die gemessenen Werte, K2 Fassungsnummer statt JSON-Tiefenkopie der Flotte je Tastendruck | umgesetzt; drei neue bunit-Wachen; Windows-Abnahme steht aus |
| **W16a‑O‑4** | 13.09. | `IosProjektQuelle.AssistentGaben`: der Projektassistent wird auf dem iPad bedienbar | umgesetzt: Parametersatz plattformfrei als `AssistentAnsichtQuelle` in `EPOS.UI.Daten`, Naht `AssistentPlattformwege` für die elf Seitenhüllen mit Fensterbesitzer (iU11), zwei Einstiege in der Projektliste, benannte Ablehnung statt leerer Schritte; Entscheid **W16a‑O‑4‑Q1** (13.09., Weg a): auf iOS wird IMMER gemerkt, der Windows-Weg bleibt unverändert; iOS-Teil ungebaut, Windows-Abnahme und iPad-Abnahme stehen aus |

---

## 4 Offen

**Aus den Paketen (Windows-Abnahme und Nachweise).**

- **Vollreferenzlauf 332/332** auf Windows über alle 13 Projekte — er schließt iZ1 **und** iZ4.
- **Bedienprobe iU5** auf Windows: Bericht, Katalogimport, Lizenzaktivierung, KI-Chat, 12 Gewerke,
  Sprachumschaltung de↔en in der laufenden Anwendung.
- **iU6:** Erststart-Migration aus einem `.accdb`-Bestand, Solar- und Pufferspeicherdialoge, die 36
  `RecordSet`-Views.
- **iU7:** Sichtvergleich der Berichte am Gerät (der Modus `bildvergleich` ist auf Anweisung
  gelöscht, iF23).
- **iU8 / iZ5:** Dialogabnahme mit Maus **und** Finger, de/en, Hochkontrast, 125 %/150 %, Enter/Esc;
  Setup mit und ohne WebView2; VS-2026-Designer unter dem Razor-SDK.
- **iU10‑9:** der iL5-Wizard in `EPOS.UI/Seiten/` und `IosNavigation` vollständig — der einzige noch
  offene Schritt des iOS-Pakets. Die Gerätebefunde selbst gehören zu iU13.
- **iU2, iU11, iU12, iU13:** nicht begonnen.

**Aus den Wellen.** Die Blöcke führen für W7, W9, W10a, W10b, W11a, W14c, W15a und W15b je eine
Zeile „Windows-Abnahme steht aus" mit benannten Prüfpunkten (Überlagerungsebenen mit Esc und
Fokusfalle, Kartenbild in der veröffentlichten `wwwroot`, Klimaimport einmal echt gegen PVGIS,
Projektwechsel über alle vier Wege, Menü und F1 des Hilfe-Assistenten, eine echte Modellfrage).
Ein Teil davon ist mit den Abnahmen vom 05./06.09. erledigt worden; **eine geschlossene
Gesamtabnahme je Welle liegt nicht vor.**

**Aus dem Bestand.**

- Eine Designer-Maske lebt noch: `Form_HelpPopup` — sie fällt mit iU11.
- Die Sprungbrücke führt einen Zweig (`SpeicherOptimierung`, iF22).
- Die Entscheidungen iF3, iF7, iF8 und iF16 sind mit iU8 **umgesetzt, förmlich aber nicht
  beschieden** — siehe
  [`Entscheidungsregister_iOS_EPOS-Plan.md`](Entscheidungsregister_iOS_EPOS-Plan.md) § 1.
- **Nach #243:** einmal `git lfs install` auf dem Windows-Rechner vor dem nächsten Pull.
- **Nach #244:** jeder Rechner klont neu; aus einem alten Klon wird nie wieder gepusht.
- **Nach #247:** Windows-Abnahme der Station „4 Optimierung“ und des Knopfs „Ausgewählte Einheiten in Projekt übernehmen“.
- **Nach #248:** Windows-Abnahme der Kleinpunkte K1 (Listenhöhen) und K2 (Fassungsnummer).
- **Nach W16a‑O‑4:** iPad-Abnahme des Projektassistenten (zwei Schritte bedienbar, elf benannt abgelehnt, Rückfrage beim Verlassen) und Windows-Gegenprobe, dass die zwei Startkacheln weiterhin merken und die zwei Menüwege weiterhin nicht.
