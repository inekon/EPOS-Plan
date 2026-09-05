# H7 — Info-Buttons auf allen Hauptdialogen (Umsetzungsprotokoll, 29.08.2026)

Grundlage: `Inventar.md` und `Entscheidungen.md` der H7-Orchestrierung, Soll-Zuordnung
`mapping_soll.txt` (73 Zeilen, wörtlich übernommen). Vorbild für die Knopfklasse:
`Allgemein\KI\KiAufrufKnopf.cs`. Zuordnungs- und Abschaltlogik: `H1H2_Umsetzung_Protokoll.md`
§ 14.

**Ergebnis in einem Satz:** 73 neue Info-Buttons in 72 Masken, angebracht über die neue
Klasse `Allgemein\Hilfe\InfoKnopf.cs`; `help_mapping.txt` wächst von 26 auf 99 Zuordnungen,
`help_cache.json` von 23 auf 32 Startbestandsseiten; Build 0 Fehler / 5 bekannte Warnungen;
Harnesse `..\dev\h7probe\` meldet **ALLES GRUEN**.

---

## 1. Was neu ist

| Datei | Art | Inhalt |
|---|---|---|
| `Allgemein\Hilfe\InfoKnopf.cs` | **neu** (UTF-8) | `InfoKnopf.Anbringen(...)` — die eine Stelle, an der ein Infoknopf entsteht |
| `Allgemein\Hilfe\help_mapping.txt` | geändert | Kommentarkopf um den H7-Absatz ergänzt, 73 Zeilen aus `mapping_soll.txt` **unverändert** angehängt → 99 Zuordnungen |
| `Allgemein\Hilfe\help_cache.json` | geändert | 9 neue Einträge → 32 |
| `Allgemein\KI\WikiWissen.cs` | geändert | Bereichstabelle `SEITE_JE_BEREICH`: 2 neue Zeilen, 2 geänderte Ziele |
| `Allgemein\KI\HilfeKontext.cs` | geändert | `BEREICH_JE_TYP`: 6 Formulare nachgetragen |
| 72 Maskendateien unter `Views\` | geändert | je 1 Aufruf im Konstruktor (2 Masken: je 2 Konstruktoren) |

Nicht angefasst: `SchemaMigration`, `DbWerte`, beide `CLAUDE.md`, `KiKern`, `KiSchreibschutz`,
sämtliche `.Designer.cs` und `.resx` (nachgewiesen: `git diff --name-only` enthält keine
solche Datei). Kein Git-Schreibkommando ausgeführt.

**Fremde Änderung im Arbeitsbaum:** `Views\Help\Form_HelpPopup.cs` (Bildschirmrand-Korrektur
des Hilfe-Popups, 15:49 Uhr) stammt aus einer parallel laufenden Sitzung, nicht aus H7.

---

## 2. `InfoKnopf.Anbringen` — Vertrag

```csharp
public static Button Anbringen(Control wurzel, string name = "btn_Help",
                               int abstandRechts = 12, int abstandOben = 12,
                               Control ziel = null,
                               int breite = 28, int hoehe = 28)
```

Eigenschaften wortgleich zu den 20 Designer-Vorbildern (`Form_WP.Designer.cs` als Muster):
28×28, `BackgroundImage = Properties.Resources.help_icon`, `BackgroundImageLayout = Zoom`,
`FlatStyle = Flat` mit `FlatAppearance.BorderSize = 0`, `Cursor = Hand`, `TabStop = false`,
`BackColor = Transparent`, `UseVisualStyleBackColor = false`.
Reihenfolge: `Controls.Add` → `Anchor = Top|Right` → `BringToFront()`.
**Kein Click-Handler** — verkabelt wird über den Namen durch `HilfeAutomatik` →
`HelpExtender.RegisterBaum`.

Vier Eigenschaften der Klasse gehen über die Auftragsvorgabe hinaus; sie sind das Ergebnis
der Messungen und unten je einzeln begründet:

### 2.1 Namensabsicherung (`NamenSicherstellen`)

`HilfeAutomatik.WurzelErfassen` steigt ohne `Control.Name` aus („ohne Namen keine
Zuordnung"), `RegisterBaum` wird dann nie gerufen — der Knopf sähe bedienbar aus und täte
beim Anklicken nichts. **Sechs der versorgten Masken bauen ihre Oberfläche vollständig im
Code auf und hatten deshalb nie einen Namen:** `Form_ProjektExportImport`,
`Form_Quellprofil`, `Form_Waermesenke`, `Form_SpeicherOptimierung`, `Form_Lizenz`,
`Form_KatalogDubletten`. `Anbringen` setzt den Namen deshalb auf den Typnamen, wenn er leer
ist — genau der Name, den auch der Designer vergeben hätte und unter dem die Zuordnung ihre
Zeile führt. Ein vorhandener Name bleibt unangetastet.

### 2.2 Ausweichregel (`FreiesOben`) — zwei Runden

Die Messung (siehe § 6) zeigte: bei **rund 40 der 70 Masken ist die obere rechte Ecke
belegt** — von einem Kopfband (`Form_WPAuswahl.label_Type` 0/0, 780×37), einer senkrechten
Knopfleiste (`Form_Heizkessel_Bearbeiten` und `Form_DBBHKW`: vier Knöpfe von y 19 bis 172),
einem Eingabefeld (`Form_AdminPV.textBox_Bezeichner`) oder einer Liste. Eine Tabelle mit 40
handgemessenen Ausnahmen wurde verworfen: sie altert mit jeder Designer-Änderung.

Stattdessen sucht `Anbringen` vom Wunschplatz aus den nächstgelegenen freien Platz im
senkrechten Streifen des Knopfes, in beide Richtungen zugleich, bei gleichem Abstand gewinnt
der obere; höchstens 200 Bildpunkte weit (das reicht an der längsten Knopfleiste des
Bestands vorbei).

1. **Streng** — jedes frei gesetzte Geschwister ist Hindernis. Ergibt den vollständig freien
   Platz; auf Masken mit Kopfband ist das genau der Platz, den der Bestand von Hand gewählt
   hat (`Form_WP`: `btn_Help` bei y 31, unter `label_Type`).
2. **Nachgiebig** — nur wenn Runde 1 nichts findet: jetzt zählen allein **bedienbare**
   Geschwister (`ButtonBase`, `TextBoxBase`, `ListControl`, `ListView`, `DataGridView`,
   `TreeView`, `UpDownBase`, `TrackBar`, `DateTimePicker`, `MonthCalendar`, `ScrollBar`).
   Rahmen, Registerwerke, Bilder und Beschriftungen dürfen überlagert werden — getroffen
   wird ihre obere rechte Ecke, also Rahmenkante, leeres Ende der Registerleiste oder
   auslaufender Text.
3. **Rückfall** auf den Wunschplatz; `BringToFront` hält den Knopf dort bedienbar.

`Dock.Fill` zählt nie als Hindernis (die füllende Inhaltsfläche belegt den Client-Bereich
vollständig, sonst gäbe es auf keiner gedockten Maske einen freien Platz). **Ein Kopfband
`Dock.Top` dagegen schon** — der Bestand geht ihm aus dem Weg, und es gibt einen sichtbaren
Grund: der durchsichtige Knopfhintergrund zeigt die Farbe seines *Elternelements*, nicht die
des Geschwisters darunter; auf einem dunkelblauen Band säße er sonst als heller Fleck. Wo
der Knopf ins Band gehört, wird das Band ausdrücklich als `ziel` übergeben.

`Control.Visible` wird bewusst **nicht** geprüft: im Konstruktor liefert die Eigenschaft für
jedes Kind `false`, weil sie den Elternzustand einrechnet — eine Prüfung darauf hätte alle
Hindernisse verschluckt.

### 2.3 Höhenanpassung in flachen Kopfbändern (`WunschOben`)

Kopfbänder sind 30 bis 74 Bildpunkte hoch. Passt der Knopf mit dem Regelabstand nicht mit
Luft nach unten hinein, sitzt er senkrecht mittig. Auf einer Maske greift die Regel nie.

### 2.4 Idempotenz (`Vorhandenen`)

Gesucht wird über alle Ebenen, aber nur im eigenen Zuständigkeitsbereich: an einem
eingebetteten `Form`/`UserControl` bricht die Suche ab (dieselbe Grenze zieht
`HelpExtender.UnterPraefixeAnwenden`). Eine flache Suche genügte nicht — wo der Knopf in
einem Kopfband sitzt, ist er kein direktes Kind der Maske.

---

## 3. Fundstellen — 75 Aufrufe in 72 Dateien (73 Knöpfe)

`Form_CaseEingabe` und `Wizard_WPItem` haben je zwei Konstruktoren; beide sind versorgt, der
zweite Durchlauf ist folgenlos (§ 2.4). Spalte „Kod" = Kodierung der Datei.

| Maske | Datei : Zeile | Kod | Aufruf |
|---|---|---|---|
| Form_Start | `Views/Hauptformular/Form_Start.cs:48` | UTF-8+BOM | `Anbringen(tabPage4, "btn_Help_Energieerzeuger", 18, 20, breite: 51, hoehe: 39)` |
| Form_Start | `Views/Hauptformular/Form_Start.cs:49` | UTF-8+BOM | `Anbringen(tabPage5, "btn_Help_Simulation", 18, 20, breite: 51, hoehe: 39)` |
| UcBerichteKosten | `Views/BerichteKosten/UcBerichteKosten.cs:82` | UTF-8+BOM | `Anbringen(this, breite: 24, hoehe: 24, ziel: lblKopf)` |
| Form_BHKWAdmin | `Views/BHKW/Form_BHKWAdmin.cs:21` | UTF-8+BOM | Regelfall |
| Form_DBBHKW | `Views/BHKW/Form_DBBHKW.cs:34` | **CP1252** | Regelfall |
| Form_Brauchwasser_Admin | `Views/Brauchwasser/Form_Brauchwasser_Admin.cs:21` | **CP1252** | Regelfall |
| Form_EingBrauchwasserTyp | `Views/Brauchwasser/Form_EingBrauchwasserTyp.cs:19` | **CP1252** | Regelfall |
| Form_ErgBrauchwasserwaerme | `Views/Brauchwasser/Form_ErgBrauchwasserwaerme.cs:27` | **CP1252** | Regelfall |
| Form_EingGebTyp | `Views/Gebäude/Form_EingGebTyp.cs:28` | **CP1252** | Regelfall |
| Form_Gebaeude1 | `Views/Gebäude/Form_Gebaeude1.cs:17` | **CP1252** | Regelfall |
| Form_Gebaeude2 | `Views/Gebäude/Form_Gebaeude2.cs:16` | **CP1252** | Regelfall |
| Form_Heizkessel_Admin | `Views/Heizkessel/Form_Heizkessel_Admin.cs:15` | UTF-8+BOM | Regelfall |
| Form_Heizkessel_Bearbeiten | `Views/Heizkessel/Form_Heizkessel_Bearbeiten.cs:38` | UTF-8+BOM | `Anbringen(this, abstandRechts: 60, abstandOben: 176)` |
| ~~Form_Heizkessel_einlesen~~ | mit **iU9‑W13.1** abgelöst → `KatalogImportDialog` (Ausprägung Heizkessel) | UTF‑8+BOM (seit iU1‑P1.12, nicht CP1252 — Befund W13‑B13) | Infoknopf jetzt als Razor-Baustein |
| Form_KiEinstellungen | `Views/Help/Form_KiEinstellungen.cs:48` | UTF-8 | Regelfall |
| Form_Betriebskosten | `Views/Kosten/Form_Betriebskosten.cs:81` | UTF-8 | Regelfall |
| Form_CaseEingabe | `Views/Kosten/Form_CaseEingabe.cs:20` und `:42` | UTF-8+BOM | Regelfall, 2 Konstruktoren |
| Form_Energietraeger | `Views/Kosten/Form_Energietraeger.cs:44` | UTF-8+BOM | `Anbringen(this, abstandRechts: 200, ziel: pnlKopf)` |
| Form_KostenAdmin | `Views/Kosten/Form_KostenAdmin.cs:17` | UTF-8+BOM | Regelfall |
| Form_KostenKomponente | `Views/Kosten/Form_KostenKomponente.cs:48` | UTF-8 | `Anbringen(this, ziel: pnlKopf)` |
| Form_Kostenprofil | `Views/Kosten/Form_Kostenprofil.cs:98` | UTF-8+BOM | Regelfall |
| Form_LeistungspreisReihe | `Views/Kosten/Form_LeistungspreisReihe.cs:39` | UTF-8+BOM | `Anbringen(this, ziel: pnlKopf)` |
| Form_SpotpreisImport | `Views/Kosten/Form_SpotpreisImport.cs:56` | UTF-8+BOM | Regelfall |
| Form_VorlagenUebernahme | `Views/Kosten/Form_VorlagenUebernahme.cs:38` | UTF-8 | `Anbringen(this, ziel: pnlKopf)` |
| Form_Emissionskatalog | `Views/Kosten/Form_Emissionskatalog.cs:59` | UTF-8+BOM | `Anbringen(this, ziel: pnlKopf)` |
| Form_PhotovoltaikVerguetung | `Views/Wirtschaftlichkeit/Form_PhotovoltaikVerguetung.cs:56` | UTF-8+BOM | `Anbringen(this, abstandRechts: 175, ziel: pnlKopf)` |
| Form_Tarifstruktur | `Views/Wirtschaftlichkeit/Form_Tarifstruktur.cs:95` | UTF-8 | Regelfall |
| Form_AdminPV | `Views/Photovoltaik/Form_AdminPV.cs:19` | UTF-8+BOM | Regelfall |
| ~~Main_PV_Test~~ | mit **iU9‑W13.3** abgelöst → `PvModulImportDialog` | UTF‑8+BOM | Das Kopfband `_headerPanel` (der einzige `ziel:`-Sonderfall der Welle) entfällt mit dem `HeaderGradientPanel` ersatzlos — der Infoknopf sitzt jetzt im Dialogkopf wie überall |
| Form_ProjektExportImport | `Views/Projekt/Form_ProjektExportImport.cs:43` | UTF-8+BOM | Regelfall |
| FormMain | `Views/Hauptformular/FormMain.cs:75` | UTF-8+BOM | Regelfall |
| Form_EingProzTyp | `Views/Prozesswärme/Form_EingProzTyp.cs:20` | **CP1252** | Regelfall |
| Form_Prozesswaerme_Admin | `Views/Prozesswärme/Form_Prozesswaerme_Admin.cs:22` | **CP1252** | Regelfall |
| Form_ErgProzesswaerme | `Views/Prozesswärme/Form_ErgProzesswaerme.cs:23` | **CP1252** | Regelfall |
| Form_PufferSp_Admin | `Views/Pufferspeicher/Form_PufferSp_Admin.cs:22` | UTF-8+BOM | Regelfall |
| Form_PufferSp_Projekt | `Views/Pufferspeicher/Form_PufferSp_Projekt.cs:170` | UTF-8+BOM | Regelfall |
| ~~Form_PufferSp_einlesen~~ | mit **iU9‑W13.1** abgelöst → `KatalogImportDialog` (Ausprägung Pufferspeicher) | UTF‑8+BOM | Infoknopf jetzt als Razor-Baustein |
| Form_Quellprofil | `Views/Simulation/Form_Quellprofil.cs:148` | UTF-8+BOM | Regelfall |
| Form_Waermesenke | `Views/Simulation/Form_Waermesenke.cs:275` | UTF-8+BOM | Regelfall |
| Form_Simulation_Detail | `Views/Simulation/Form_Simulation_Detail.cs:275` | UTF-8+BOM | Regelfall |
| Form_QuelleErdreich | `Views/Simulation/Form_QuelleErdreich.cs:206` | UTF-8+BOM | Regelfall |
| Form_SolarDB | `Views/Solarthermie/Form_SolarDB.cs:18` | **CP1252** | Regelfall |
| Form_SolarKollektorenAdmin | `Views/Solarthermie/Form_SolarKollektorenAdmin.cs:23` | **CP1252** | Regelfall |
| ~~Form_SolarKollektoren_einlesen~~ | mit **iU9‑W13.1** abgelöst → `KatalogImportDialog` (Ausprägung Solarkollektoren) | UTF‑8+BOM (seit iU1‑P1.12, nicht CP1252 — Befund W13‑B13) | Infoknopf jetzt als Razor-Baustein |
| Form_Solarganglinie | `Views/Solarthermie/Form_Solarganglinie.cs:25` | UTF-8 | Regelfall |
| Form_Solarganglinie_Admin | `Views/Solarthermie/Form_Solarganglinie_Admin.cs:21` | **CP1252** | Regelfall |
| Form_GanglinieImportOptionen | `Views/Stromverbraucher/Form_GanglinieImportOptionen.cs:89` | UTF-8+BOM | Regelfall |
| Form_Stromganglinie | `Views/Stromverbraucher/Form_Stromganglinie.cs:25` | UTF-8 | Regelfall |
| Form_Stromganglinie_Admin | `Views/Stromverbraucher/Form_Stromganglinie_Admin.cs:28` | **CP1252** | Regelfall |
| Form_EingDBStromverbraucher | `Views/Stromverbraucher/Form_EingDBStromverbraucher.cs:25` | **CP1252** | Regelfall |
| Form_EingStromTyp | `Views/Stromverbraucher/Form_EingStromTyp.cs:21` | **CP1252** | Regelfall |
| Form_ErgStromverbraucher | `Views/Stromverbraucher/Form_ErgStromverbraucher.cs:23` | **CP1252** | Regelfall |
| Form_Stromverbraucher_Admin | `Views/Stromverbraucher/Form_Stromverbraucher_Admin.cs:23` | **CP1252** | Regelfall |
| Form_AdminStromspeicher | `Views/Stromspeicher/Form_AdminStromspeicher.cs:21` | UTF-8+BOM | Regelfall |
| Form_PeakShaving | `Views/Stromspeicher/Form_PeakShaving.cs:93` | UTF-8+BOM | Regelfall |
| Form_SpeicherOptimierung | `Views/Stromspeicher/Form_SpeicherOptimierung.cs:157` | UTF-8+BOM | Regelfall |
| Form_SpeicherVariantenVergleich | `Views/Stromspeicher/Form_SpeicherVariantenVergleich.cs:161` | UTF-8+BOM | Regelfall |
| UcBkUebersicht | `Views/BerichteKosten/UcBkUebersicht.cs:126` | UTF-8+BOM | Regelfall |
| UcBkKosten | `Views/BerichteKosten/UcBkKosten.cs:99` | UTF-8+BOM | Regelfall |
| UcBericht | `Views/Bericht/UcBericht.cs:89` | UTF-8+BOM | Regelfall |
| ~~Form_AdminWaermeeinlesen~~ | mit **iU9‑W13.2** abgelöst → `WaermebedarfAdminDialog` | UTF‑8+BOM (seit iU1‑P1.12, nicht CP1252 — Befund W13‑B13) | Infoknopf jetzt als Razor-Baustein |
| Wizard_WPItem | `Views/Wizard/Wizard_WPItem.cs:46` und `:75` | UTF-8+BOM | Regelfall, 2 Konstruktoren |
| Form_WPAuswahl | `Views/Wärmepumpe/Form_WPAuswahl.cs:21` | UTF-8+BOM | Regelfall |
| Form_WpFilterAuswahl | `Views/Wärmepumpe/Form_WPFilterAuswahl.cs:20` | UTF-8+BOM | Regelfall (Datei ≠ Klasse) |
| ~~Form_WP_einlesen~~ | mit **iU9‑W13.1** abgelöst → `KatalogImportDialog` (Ausprägung Wärmepumpe) | reines ASCII ohne BOM | Infoknopf jetzt als Razor-Baustein; der Designer liegt als Prüfmuster unter `Werkzeuge/Formularkarte.Tests/Pruefmuster/Wärmepumpe/` |
| Kenndaten | `Views/Wärmepumpe/Kenndaten.cs:22` | UTF-8+BOM | Regelfall |
| Form_KwkgModule | `Views/Wirtschaftlichkeit/Form_KwkgModule.cs:61` | UTF-8 | Regelfall |
| Form_WirtschaftlichkeitParameter | `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs:64` | UTF-8 | Regelfall |
| Form_WirtschaftlichkeitVerlauf | `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitVerlauf.cs:64` | UTF-8 | Regelfall |
| Form_Lizenz | `Views/Help/Form_Lizenz.cs:73` | UTF-8 | Regelfall |
| Form_LizenzVerwaltung | `Views/Admin/Form_LizenzVerwaltung.cs:39` | UTF-8 | Regelfall |
| Form_Gesetzesparameter | `Views/Admin/Form_Gesetzesparameter.cs:64` | UTF-8+BOM | Regelfall |
| Form_KatalogDubletten | `Views/Admin/Form_KatalogDubletten.cs:61` | UTF-8 | Regelfall |

„Regelfall" = `InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt`

**Ankerpunkt der Einfügung:** in der Regel die Zeile `InitializeComponent();`. Sieben Masken
bauen ihre Oberfläche im Code auf und haben keine; dort steht der Aufruf hinter dem
Aufbauaufruf des Konstruktors: `Form_Betriebskosten` (`ZeilenAufbauen();`),
`Form_ProjektExportImport` (`LadeProjekte();`), `Form_Quellprofil`, `Form_Waermesenke`,
`Form_Lizenz`, `Form_KatalogDubletten` (je `BaueOberflaeche();`), `Form_SpeicherOptimierung`
(`AufbauSteuerelemente();`).

---

## 4. Kodierung — der größte Fallstrick dieses Auftrags

**21 der 72 Maskendateien sind CP1252**, 51 sind UTF-8 (37 mit BOM, 14 ohne). Die CP1252-Dateien
stehen oben fett; gruppiert: `Views\BHKW` (1), `Views\Brauchwasser` (3), `Views\Gebäude` (3),
`Views\Heizkessel` (1), `Views\Prozesswärme` (3), `Views\Solarthermie` (4),
`Views\Stromverbraucher` (5), `Views\Wärmebedarf` (1).

Vorgehen und Beweis:

1. **Strikte UTF-8-Probe je Datei** vor jeder Änderung
   (`UTF8Encoding(false, throwOnInvalidBytes: true)`); schlägt sie fehl → CP1252, und die
   Datei wird ausschließlich über `Encoding.GetEncoding(1252)` gelesen und geschrieben.
2. **Der eingefügte Text ist reines ASCII** — Bezeichner, Kommentare, alles ohne Umlaute
   (Konvention von `KiAufrufKnopf.cs` und `HilfeAutomatik.cs`). Damit sind die eingefügten
   Bytes in beiden Kodierungen identisch; ein Kodierungsfehler kann gar nicht erst entstehen.
3. **Rundprobe je Datei nach dem Schreiben:** Kodierungsart, BOM-Vorhandensein und Anzahl der
   Nicht-ASCII-Bytes werden gegen den Vorzustand verglichen. Ergebnis über alle 72 Dateien:
   **0 Abweichungen** (21 × CP1252, 51 × UTF-8, BOM-Zustand unverändert, Umlautbytes
   unverändert).
4. Zeilenenden werden je Ankerzeile übernommen; `Views\Kosten\Form_KostenAdmin.cs` führt LF
   und behält LF.

`help_mapping.txt` bleibt UTF-8 **mit** BOM und CRLF, `help_cache.json` UTF-8 **ohne** BOM,
CRLF, reines ASCII (Umlaute als `\uXXXX`) — beides wie im Vorzustand, jeweils nach dem
Schreiben nachgemessen. `HilfeKontext.cs` und `WikiWissen.cs` sind UTF-8 ohne BOM; nach der
Änderung 17 bzw. 13 Umlaute und **0** Ersatzzeichen U+FFFD.

---

## 5. Sonderfälle

| Maske | Was und warum |
|---|---|
| `Form_Start` Reiter 4/5 | Programmatisch auf `tabPage4`/`tabPage5`, 51×39, `abstandRechts 18`, `abstandOben 20` — deckungsgleich zu `btn_Help_Strombedarf` auf Reiter 3 (Location 1196/26 bei Seitenbreite 1265). Die Startmaske führt ihre Koordinaten je Sprache in eigenen `.resx`; von Hand wäre jede zu pflegen. Zur TabPage-Vierseitenanker-Falle: der Knopf wird **nach** `InitializeComponent()` eingehängt, nur `Top\|Right` verankert und behält damit seine Größe; selbst wenn die Seite zu diesem Zeitpunkt noch auf einer vorläufigen Größe stünde, korrigierte die Verankerung die Lage beim ersten Layout (gemessen, § 6). |
| Reiter 6 → `UcBerichteKosten` | Der Knopf gehört nicht auf `tabPage6` — `UcBerichteKosten` dockt `Fill` und läge darüber. Er sitzt 24×24 **in** `lblKopf` (Dock Top, 30 hoch); `lblKopf` ist zugleich sein Elternelement, nur so zeigt der durchsichtige Hintergrund die Farbe der Kopfzeile. Gemessen nach dem ersten Layout: 1021/3. |
| `Form_Heizkessel_Bearbeiten` | Die Maske führt oben rechts eine **senkrechte Knopfleiste** (x 616..721 von y 19 bis 168), der KI-Aufrufknopf sitzt deshalb laut `KiDialoge.cs` **darunter** (`AbstandRechts 8, AbstandOben 176`) — nicht oben rechts. Die Kollisionsregel „Infoknopf links neben den KI-Knopf" ergibt damit `abstandRechts: 60, abstandOben: 176`. Ein Knopf bei `abstandRechts: 60` auf Regelhöhe läge mitten auf `btn_Ueberschreiben`. |
| Kopfband trägt den Knopf | `Form_KostenKomponente`, `Form_LeistungspreisReihe`, `Form_VorlagenUebernahme`, `Form_Emissionskatalog` (je `ziel: pnlKopf`) und `Main_PV_Test` (`ziel: _headerPanel`) — deren Kopfbänder sind rechts frei. **Alle fünf sind inzwischen Razor-Komponenten**; dort sitzt der Infoknopf im Dialogkopf, und die Frage nach einem freien Kopfband stellt sich nicht mehr. |
| Kopfband **belegt** | `Form_Energietraeger`: `lblKontext` ist rechts verankert und endet 12 px vor dem Rand → `abstandRechts: 200` setzt den Knopf links daneben (x 856..884). `Form_PhotovoltaikVerguetung`: `chkAktiv` rechts verankert bei x 756..901 → `abstandRechts: 175` (x 711..739). |
| Zwei Konstruktoren | `Form_CaseEingabe`, `Wizard_WPItem` — beide versorgt, zweiter Aufruf folgenlos. |
| Datei ≠ Klasse | `Form_WPFilterAuswahl.cs` → `Form_WpFilterAuswahl`, `Form_CECImport.cs` → `Main_PV_Test`. Beide über die Klasse zugeordnet, wie in `mapping_soll.txt`. **Beide sind abgelöst** (W7.1 bzw. W13.3); mit `Main_PV_Test` fällt auch der tote `HilfeKontext`-Eintrag `Form_CECImport`, den der Dateiname erzeugt hatte (Befund W13‑B37). |
| `Form_Gebaeude2` | Ohne Fenstertitel — Knopf trotzdem angebracht (Zuordnung läuft über `Control.Name`, nicht über den Titel). |
| Sechs namenlose Masken | siehe § 2.1. |
| `.cs.bak` und `Form_Simulation_Detail - Kopie.cs` | Nicht angefasst (Letztere ist per `.csproj` vom Build ausgeschlossen). |

### 5.1 `WikiWissen.SEITE_JE_BEREICH`

| Bereich | vorher | nachher |
|---|---|---|
| `B_BERICHT` „Bericht" | *(fehlte — keine Seite)* | **Berichte und Kosten** |
| `B_LIZENZ` „Lizenz" | *(fehlte — keine Seite)* | **Lizenz** |
| `B_SIM_DETAIL` „Detaillierte Simulation" | Simulation | **Simulationsergebnisse** |
| `B_QUELLE_ERDREICH` | Wärmepumpe | **Wärmequelle Erdreich** |

`Bericht` zeigt auf `Berichte und Kosten`, weil der Bereich den ganzen Reiter umfasst
(Übersicht, Kosten, Wirtschaftlichkeit, Bericht) und nicht nur dessen letzte Seite. Ohne
Eintrag bleibt jetzt nur noch `Unbekannter Bereich`.

### 5.2 `HilfeKontext.BEREICH_JE_TYP` — 6 Nachträge

| Formular | Bereich | Begründung |
|---|---|---|
| `Form_AdminWaermeeinlesen` | `B_WAERMEBEDARF` | H7-Zielseite Wärmebedarf |
| `Form_KwkgModule` | `B_WIRTSCHAFT` | H7-Zielseite Wirtschaftlichkeit |
| `Form_AlsVariante` | `B_VARIANTEN` | legt aus dem Projekt eine Variante an |
| `Form_StromTest` | `B_STROMSPEICHER` | Entwicklermaske hinter dem Knopf „SP" auf `FormMain`; ordnet dem Projekt einen Stromspeicher zu |
| `Main_PV_Test` | `B_PHOTOVOLTAIK` | **Befund:** die Tabelle führte `Form_CECImport` — das ist der DATEIname, nachgeschlagen wird der TYPname. Der alte Eintrag griff nie und bleibt stehen, der neue trifft. |
| `Kenndaten` | `B_WAERMEPUMPE` | Kennfeld einer Wärmepumpe, aufgerufen aus `Form_WP` |

Kein neuer Bereich in der Positivliste — alle sechs treffen bestehende Konstanten.

---

## 6. Harnesse `..\dev\h7probe\` (gitignored)

Läuft gegen die gebaute Assembly in `..\dev\build_h7\`; Vorbilder `dev\h1probe`
(Katalogebene) und `dev\h2probe_buttons` (echte Verkabelung). Jede Maske wird auf einem
eigenen STA-**Hintergrund**faden mit 8-Sekunden-Schranke gebaut, damit ein Konstruktor, der
in einer Meldung hängen bliebe, den Lauf nicht anhält.

### Ergebnis — `ALLES GRUEN`, ExitCode 0

| Prüfung | Ergebnis |
|---|---|
| **A** Startbestand `help_cache.json` | **32** Unterseiten |
| **A** `help_mapping.txt` (eingebettete Ressource) | **99** Zuordnungen |
| **A** alle Ziele lösen im Katalog auf | **99/99**, 32 verschiedene Zielseiten |
| **A** keine doppelte linke Seite | ja |
| **B** Masken instanziierbar | **56 von 72** |
| **B** Infoknöpfe gefunden | **57 von 73** (= alle Knöpfe der 56 baubaren Masken) |
| **B** nach `HelpExtender.RegisterBaum` aktiv | **57 von 57** — kein grauer Knopf |
| **B** verdeckt ein bedienbares Geschwister | **0 von 57** |
| **B** Anker hält den Abstand zum rechten Rand (Maske um 120 px verbreitert) | **57 von 57** |
| **C** zweiter `Anbringen`-Aufruf erzeugt zweiten Knopf | **nein**, 56/56 |

**Die 16 nicht instanziierbaren Masken** sind eine Grenze der Prüfumgebung, kein Befund:
15 scheitern an `PlatformNotSupportedException: System.Data.OleDb is not supported on this
platform` (der Konstruktor liest die Datenbank; die Wegwerf-Assembly bringt den
OLE-DB-Anbieter nicht mit — bewusst, damit die Probe die Produktivdatenbank nicht anfasst),
eine an `ScottPlot.Fonts`: `Form_BHKWAdmin`, `Form_Heizkessel_Admin`,
`Form_Heizkessel_Bearbeiten`, `Form_Betriebskosten`, `Form_KostenAdmin`,
`Form_KostenKomponente`, `FormMain`, `Form_Solarganglinie`, `Form_Stromganglinie`,
`Form_PeakShaving`, `Form_SpeicherOptimierung` (ScottPlot), `Form_EingDBStromverbraucher`,
`Wizard_WPItem`, `Form_WpFilterAuswahl`, `Kenndaten`, `Form_Simulation_Detail`.
Für sie ist der Anbringungsaufruf durch den Compile-Beweis und die Fundstellenliste belegt,
die Zuordnung durch Teil A — offen bleibt allein die gemessene Lage (§ 8).

### Gemessene Lagen (Auszug, Maßeinheit Bildpunkte nach dem ersten Layout)

| Maske | Elternelement | Lage | Bemerkung |
|---|---|---|---|
| `Form_Start` (beide Reiter) | TabPage | 1196/20, 51×39 | deckungsgleich zu Reiter 3 |
| `UcBerichteKosten` | Label `lblKopf` | 1021/3, 24×24 | in der Kopfzeile |
| `Form_DBBHKW` | Formular | 751/**172** | Ausweichregel: unter der senkrechten Knopfleiste |
| `Form_AdminPV` | Formular | 567/**123** | unter den drei Eingabefeldern |
| `Form_SolarKollektorenAdmin` | Formular | 785/**31** | unter `label_Type` — derselbe Platz, den `Form_WP` von Hand hat |
| `Form_WPAuswahl` | Formular | 740/**37** | unter `label_Type` |
| `Form_QuelleErdreich` | Formular | 660/**130** | unter `_gbSystem` |
| `Form_Stromganglinie_Admin` | Formular | 624/**207** | unter `groupBox1`, über `btn_OK` |
| `Form_WirtschaftlichkeitVerlauf` | Formular | 858/**40** | unter `btnSchliessen`, auf der oberen Kante von `picDiff` |
| `Form_Lizenz` | Formular | 880/**58** | unter dem Kopfband |
| `Form_ErgBrauchwasserwaerme` u. a. | Formular | 523/12 | auf dem leeren rechten Ende der Registerleiste |
| 34 weitere | Formular / Kopfband | Regelplatz 12/12 bzw. mittig im Band | ohne jede Überlappung |

---

## 7. Build

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x64 `
  -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_h7\
```

**0 Fehler.** Warnungen unverändert **5**, alle in nicht berührtem Code:
`Controller\KlimaregionStammCtrl.cs:22,24` und `:23,48` (CS0109),
`Controller\StromverbraucherStammCtrl.cs:25,44` (CS0108),
`Model\WErzeugerModel.cs:6,20` (CS0108), `MDIMainForm.cs:489,28` (CS1998).
Zwischenbauten nach der Klasse, nach den 72 Dateien und nach jeder Regeländerung.

---

## 8. Offene Punkte — am laufenden Programm zu prüfen

1. **Die neun Wiki-Seiten fehlen noch.** `Programm Dokumentation/Energieerzeuger`,
   `…/Berichte und Kosten`, `…/Bericht`, `…/Simulationsergebnisse`,
   `…/Wärmequelle Erdreich`, `…/Lizenz`, `…/Gesetzesparameter`, `…/Katalogpflege`,
   `…/Emissionen` werden **parallel erstellt und separat importiert**. Bis dahin bleiben die
   **10** darauf zeigenden Buttons im Onlinebetrieb grau — `ZuordnungenPruefen` schaltet ab,
   was der Katalog nicht kennt (§ 14.1 des H1H2-Protokolls). Das sind
   `Form_Start.btn_Help_Energieerzeuger`, `UcBerichteKosten.btn_Help`, `UcBericht.btn_Help`,
   `Form_Simulation_Detail.btn_Help`, `Form_QuelleErdreich.btn_Help`,
   `Form_Lizenz.btn_Help`, `Form_LizenzVerwaltung.btn_Help`,
   `Form_Gesetzesparameter.btn_Help`, `Form_KatalogDubletten.btn_Help`,
   `Form_Emissionskatalog.btn_Help`. **Offline gegen den neuen Startbestand sind sie bereits
   grün** (Harnesse Teil A: 99/99). Nach dem Import ist nichts nachzuziehen — der Ladelauf
   ersetzt den Startbestand, und `HilfeAutomatik.NachKatalogErneuern` weckt bereits
   abgeschaltete Knöpfe wieder auf.
2. **Restdatei `help_mapping.txt` neben der EXE.** Liegt eine dort, übersteuert sie je Zeile
   (seit dem Fix vom 29.08.). Vor der Sichtprüfung ausschließen, dass in
   `bin\x64\Debug\net8.0-windows\` wieder eine liegt.
3. **Sichtprüfung der 16 nicht instanziierbaren Masken** (Liste in § 6) — Lage des Knopfes
   in der laufenden Anwendung. Erwartung nach der Messung der Geschwistergeometrie:
   `Form_Heizkessel_Bearbeiten` 60/176 (links neben dem KI-Knopf),
   `Form_KostenKomponente` im Kopfband, die übrigen auf dem Regelplatz bzw. unter dem
   jeweiligen Kopfband.
4. **Doppelte Infoknöpfe auf Wizard-Seiten** sind laut Entscheidung 6 akzeptiert
   (`WizardParent.btn_Help` plus der Knopf des eingebetteten Formulars).
5. **`Form_Variantentest`** behält seinen bestehenden Knopf; die Maske ist über die
   Oberfläche weiterhin unerreichbar (Inventar, Nebenbefunde).
6. **`Form_CECImport`-Eintrag in `HilfeKontext`** ist tot (Dateiname statt Klassenname) und
   bleibt bewusst stehen; ein Aufräumen wäre eine eigene Kleinigkeit.
7. **Skalierte Oberflächen:** Alle Messungen entstanden nach dem ersten Layout inklusive
   `AutoScaleMode.Font`. Der Anker hält den rechten Abstand (57/57 nachgemessen); die
   senkrechte Lage der ausgewichenen Knöpfe skaliert mit den Geschwistern mit, weil der
   Knopf vor dem Skalieren eingehängt wird.
