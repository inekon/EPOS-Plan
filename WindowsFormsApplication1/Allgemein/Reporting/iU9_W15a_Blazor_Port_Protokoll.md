# iU9 Welle 15a — Projekt: Auswahl, Löschen, Speichern unter, Export/Import, Assistentenkopf

> Umsetzungsprotokoll zur Vermessung `iU9_W15a_Vermessung.md` (2 088 Zeilen, Stand `fe22915`) und
> zur Arbeitsanweisung `iU9_W15a_Arbeitsanweisung.md`. Basis der Umsetzung: `f7e2758`
> (Merge der Welle 14c auf `ios_migration`). Form: `iU9_W14c_Blazor_Port_Protokoll.md`.

## 0 — Was die Welle getan hat

**Sechs Bauteile → ein Baustein, drei Dialoge, eine Assistentenseite, vier Hüllen.**

| Gefallen | Zeilen (`.cs` / Designer) | Nachfolge |
|---|---|---|
| `Form_ProjektAuswahl` | 99 / 93 | `EPOS.UI/Dialoge/Projekt/ProjektWahlDialog.razor` (Zweck `Oeffnen`) |
| `Form_ProjektDelete` | 55 / 85 | **dieselbe** Komponente (Zweck `Loeschen`) |
| `Form_ProjektSpeichernUnter` | 268 / 205 | `EPOS.UI/Dialoge/Projekt/ProjektKopieDialog.razor` |
| `Form_ProjektExportImport` | 320 / — (K4) | `EPOS.UI/Dialoge/Projekt/ProjektTransferDialog.razor` |
| `Wizard_Projekt` | 104 / 193 | `EPOS.UI/Seiten/Assistent/ProjektKopfSeite.razor` |
| **`ProjektAuswahl` (uc)** | **408 / 107 — BLEIBT bis W16** | Baustein `EPOS.UI/Bausteine/ProjektListe.razor` steht daneben |

**Vier Hüllen:** `Views/Projekt/ProjektWahlHuelle.cs`, `Views/Projekt/ProjektKopieHuelle.cs`,
`Views/Projekt/ProjektTransferHuelle.cs`, `Views/Wizard/ProjektKopfHuelle.cs`.

**Der Hebel der Welle:** Der Bestand führte **vier Projektlisten nebeneinander** (Befund
W15a‑B52) — `ProjektAuswahl` (ListView, drei Spalten, Suche, Sortierung, 408 Z.),
`Form_ProjektSpeichernUnter.listView_Projekt` (ListView, zwei Spalten), `Form_ProjektDelete.comboBox_Projekte`
(ComboBox über eine Erweiterungsmethode) und `Form_ProjektExportImport.cbProjekt` (ComboBox mit
eigener Schleife); dazu als fünfte die fertige Razor-Seite `Seiten/Projektliste` (iOS-Einstieg).
Sie sind jetzt **ein** Baustein. Damit ist „Eine Projektauswahl für alle"
(`Konzept_Projektdialoge_Vereinheitlichung.md:177`) eingelöst.

## 1 — Commits

| Commit | Schritt | Inhalt |
|---|---|---|
| `7d8c93a` | W15a.0e (1/2) | `SchemaStand.Zielversion` im Kern; `SchemaMigration.ZIEL_VERSION` reicht weiter; `ProjektExportImportCtrl` verliert seinen unbenutzten `using System.Windows.Forms` |
| `ea9ab71` | W15a.0j | `ProjekttransferTests` (P1–P5) **vor** dem Umzug — und der Befund W15a‑B55, den sie finden |
| `45c6b45` | W15a.0e (2/2) | `git mv` des Controllers in den Kern; P1–P5 danach erneut grün |
| `5255c75` | W15a.0a–0g, 0k | `ProjektAngaben.cs`, die vier Kern-Wege, `ProjektpflegeTests` (P7–P9) |
| `6207e03` | W15a.0h/0i | `IProjektQuelle.TransferDaten()`, 83 Textschlüssel in beiden Sprachen |
| `8777ebf` | W15a.1 / W15a.7 | Baustein `ProjektListe`; `Seiten/Projektliste` baut darauf auf |
| `1b6d2be` | W15a.2 / W15a.3 | `ProjektWahlDialog`, Hülle, Sprungtabelle, `MenueCtrl.ProjektDelete`, `Form_Start` |
| `a1ae656` | W15a.4 | `ProjektKopieDialog` und Hülle |
| `c4e9575` | W15a.5 | `ProjektTransferDialog`, Hülle, `Dateiwahl.Speichern` |
| `2a8f43c` | W15a.6 | `ProjektKopfSeite`, Hülle, `AssistentSeiten`, die sechs `WizardParent`-Stellen |
| `62cdf63` | W15a.9 | die zwei Testanker, vier Schwellen, `Erreichbarkeit_2026-09-03.md`, `LIESMICH.md` |
| (dieser) | W15a.10 | Protokoll und die drei `CLAUDE.md` |

## 2 — Feldkartenabgleich

Die Karten sind **vor** dem Port gezogen worden
(`dotnet run --project Werkzeuge/Formularkarte -c Release -- --alle WindowsFormsApplication1
--ziel <scratch> --erreichbarkeit`, 04.09.2026, 17 Masken / 18 Designer).

### 2.1 `Form_ProjektDelete` (3 Kartenzeilen)

| # | Steuerelement | Typ | de / en | Nachfolge in `ProjektWahlDialog` (Zweck `Loeschen`) |
|---|---|---|---|---|
| 1 | `comboBox_Projekte` | ComboBox | „Projekt:" / „Project:" | **A‑12:** der Baustein `ProjektListe` — Liste mit Suche, Kunde und Änderungsdatum statt einer Klappliste |
| 2 | `btn_OK` | Button | „OK" / „OK" | `SpeichernLeiste`, Beschriftung „Löschen" (`PRJ_DEL_BTN_LOESCHEN`) |
| 3 | `btn_Abbrechen` | Button | „Abbrechen" / „Cancel" | `SpeichernLeiste` |
| — | (kein Hilfeknopf, B8) | — | — | **neu**: `InfoKnopf` mit `Form_ProjektDelete.btn_Help`, Eintrag in `help_mapping.txt` ergänzt |

Vier Handler abgeglichen: `Load` (füllt die Liste → `ProjektCtrl.NamenListe`), `btn_OK_Click`
(**ohne** Leerprüfung, B3 → der Dialog meldet jetzt `Text_Select`), `btn_Abbrechen_Click`,
`comboBox_Projekte_SelectedIndexChanged` (**verkettetes SQL**, B1 → `ProjektCtrl.IdVonName`;
die Id kommt jetzt ohnehin mit der Zeile).

### 2.2 `Form_ProjektSpeichernUnter` (15 Kartenzeilen + 2 im Panel)

| # | Steuerelement | de / en | Nachfolge in `ProjektKopieDialog` |
|---|---|---|---|
| 1 | `button_Open` | „OK" / (leer) | `SpeichernLeiste` |
| 2 | `button_Abbrechen` | **„Abbrechen ❌" / „Cancel ❌"** | `SpeichernLeiste`, **A‑1: ohne ❌** |
| 3 | `label1` | „Projektauswahl:" | `Gruppenkopf` |
| 4 | `listView_Projekt` | (2 Laufzeitspalten) | `ProjektListe` |
| 5 | `label2` | „Neuer Projektname:" | `Textfeld`-Beschriftung |
| 6 | `textBox_NeuerProjektName` | — | `Textfeld` |
| 7 | `label_Beschreibung` | „Beschreibung:" | `Textfeld`-Beschriftung |
| 8 | `textBox_Beschreibung` | mehrzeilig | `Textfeld` `Mehrzeilig`, 4 Zeilen |
| 9 | `label_Kunde` | „Kunde:" | `Textfeld`-Beschriftung |
| 10 | `textBox_Kunde` | — | `Textfeld` |
| 11 | `label_Bearbeiter` | „Bearbeiter:" | `Textfeld`-Beschriftung |
| 12 | `textBox_Bearbeiter` | — | `Textfeld` |
| 13 | `btn_Help` | — | `InfoKnopf` |
| P1 | `lbl_Fortschritt` | — | `Fortschritt.Text` |
| P2 | `progressBar_Duplizieren` | — | `Fortschritt.Anteil` |

**Nachtrag von Hand (R‑W15a‑6):** Die **zwei ListView-Spalten** entstehen im `.ctor:38–41`
(`MyResource.Resource.Text_Name`, `Text_Beschreibung`) und stehen in keiner Karte (B9). Sie sind
in der neuen Liste die Spalten „Projektname" und „Kunde"/„Geändert" — die Beschreibung wird
weiterhin DURCHSUCHT, aber nicht mehr als Spalte gezeigt (sie war im Vorläufer die zweite Spalte;
die drei Spalten der `ProjektAuswahl`-Sicht sind die gemeinsame Form, A‑12).

### 2.3 `Form_ProjektAuswahl` (4 Kartenzeilen) + `ProjektAuswahl` (3)

| # | Steuerelement | de / en | Nachfolge |
|---|---|---|---|
| 1 | `ucAuswahl` (`ProjektAuswahl`) | — | Baustein `ProjektListe` |
| 2 | `btn_OK` | „OK" | `SpeichernLeiste` |
| 3 | `btn_Abbrechen` | „Abbrechen" / „Cancel" | `SpeichernLeiste` |
| 4 | `btn_Help` | — | `InfoKnopf` |
| uc 1 | `textBox_Suche` | „Suchen:" / „Search:" | `ProjektListe.SucheText` |
| uc 2 | `listView_Projekte` | 3 `ColumnHeader` | die Tabelle des Bausteins |
| uc 3 | `label_Anzahl` | **„{0} von {1} Projekten"** | `ProjektListe.AnzahlFormat` (B20 — jetzt ein Parameter, kein getarnter Steuerelementtext) |

### 2.4 `Form_ProjektExportImport` — die Handkarte (R‑W15a‑6, B24)

Die Maske hatte **keinen Designer**; die Karte ist von Hand aus `BaueUi():46–126` geschrieben.
**23 Steuerelemente, alle Texte deutsche Literale im Quelltext.**

| # | Bereich | Steuerelement | Text (deutsch, Literal) | Nachfolge |
|---|---|---|---|---|
| 1 | Fenster | `tabs` (`TabControl`) | — | `Reiter` |
| 2 | Export | `TabPage` | „Exportieren" | `Reiterblatt` `EXPORT` |
| 3 | Export | `Label` | „Projekt:" | `Auswahlfeld`-Beschriftung |
| 4 | Export | `cbProjekt` (`ComboBox`, `DropDownList`) | — | `Auswahlfeld` |
| 5 | Export | `Label` | „Varianten mitexportieren:" | `Mehrfachauswahl`-Beschriftung |
| 6 | Export | `clbVarianten` (`CheckedListBox`, `CheckOnClick`) | — | `Mehrfachauswahl`, **alle vorbelegt an** (TF2) |
| 7 | Export | `btnExport` | „Exportieren…" | Knopf `epos-transfer-export` |
| 8 | Import | `TabPage` | „Importieren" | `Reiterblatt` `IMPORT` |
| 9 | Import | `btnDatei` | „Datei wählen…" | `Dateiwahl.KnopfText` |
| 10 | Import | `txtDatei` (`ReadOnly`) | — | `Dateiwahl.Pfad` |
| 11 | Import | `lblInfo` (`DimGray`, 3 Zeilen) | Paketvorschau | `Warnbanner` (Stufe Hinweis / Warnung) |
| 12 | Import | `Label` | „Zielname (leer = aus Datei):" | `Textfeld`-Beschriftung |
| 13 | Import | `txtZielname` | — | `Textfeld` |
| 14 | Import | `Label` | „Falls dieser Name bereits existiert:" | `Optionsgruppe`-Beschriftung |
| 15 | Import | `rbNeuerName` (**`Checked`**) | „Unter neuem Namen importieren" | `Optionsgruppe`, Vorbelegung |
| 16 | Import | `rbUeberschreiben` | „Vorhandenes Projekt überschreiben" | `Optionsgruppe` |
| 17 | Import | `rbAbbrechen` | „Abbrechen" | `Optionsgruppe` |
| 18 | Import | `chkSicherung` (**`Checked`**) | „Sicherungskopie der Datenbank vor dem Import anlegen" | `Schalter` — **nur mit Delegat** (A‑10) |
| 19 | Import | `btnImport` (`Enabled = false`) | „Importieren…" | Knopf `epos-transfer-import`, gesperrt ohne Datei |
| 20 | Fuß | `pb` (`ProgressBar`) | — | `Fortschritt` |
| 21 | Fuß | `lblStatus` (`DimGray`) | — | `Warnbanner` bzw. `Fortschritt.Text` |
| 22 | Fuß | `btnSchliessen` (`CancelButton`) | „Schließen" | `SpeichernLeiste` `OkText`, ohne Abbrechen |
| 23 | Fenster | `InfoKnopf.Anbringen(this)` | — | `InfoKnopf` |

**Der Wellenplan zählt „3 TabPage" — es sind zwei** (B26); die dritte Zeile der Inventarliste
ist die gemeinsame Fußzeile.

### 2.5 `Wizard_Projekt` (10 Kartenzeilen)

| # | Steuerelement | de / en | Nachfolge in `ProjektKopfSeite` |
|---|---|---|---|
| 1 | `textBox_Name` | „Projektname" / „Project name" | `Textfeld`, `NurLesen` = `!NameAenderbar` |
| 2 | `textBox_Beschreibung` | „Beschreibung" / „Description" | `Textfeld` `Mehrzeilig`, 5 Zeilen |
| 3 | `textBox_Kunde` | „Kunde" / **„customer"** (klein, B43) | `Textfeld` — englisch jetzt „Customer" |
| 4 | `textBox_Bearbeiter` | „Bearbeiter" / „Editor" | `Textfeld` |
| 5 | `pictureBox1` | Zierbild | **entfällt** |
| 6 | `label6` | „Geben Sie hier die administrativen Projektdaten ein:" | `Herleitungszeile` |
| 7 | `textBox_Aenderungsdatum` | „Änderungsdatum" (gesperrt) | `Textfeld` `NurLesen`, **A‑9** |
| 8 | `label7` (`Dock=Top`) | „Projektkonfiguration" | `Gruppenkopf` |
| 9 | `comboBox_Klima` | „Klimaregion" / „Climate region" | `Auswahlfeld` |
| 10 | `textBox_Erstelldatum` | „Erstelldatum" (gesperrt) | `Textfeld` `NurLesen`, **A‑9** |

## 3 — Die Proben

### 3.1 P1–P5 — Projekttransfer (`EPOS.Kern.Tests/ProjekttransferTests.cs`)

Sie sind **vor** dem Umzug geschrieben worden (R‑W15a‑2). Jede Probe auf einer eigenen
Arbeitskopie der 77‑MB-Testdatenbank.

| Probe | Was sie prüft | vor dem Umzug | nach dem Umzug |
|---|---|---|---|
| **P1** Determinismus | Projekt 1030 zweimal exportieren; alle ZIP-Einträge außer `manifest.json` byteweise gleich, das Manifest ohne `exportedUtc` gleich | grün | grün |
| **P2** Rundreise-Zählung | je Pakettabelle: Zeilen im Paket == `COUNT(*)` im Ziel; Quelle == Ziel (Ausnahme `Tab_ProjektWerte` wegen des T6-Filters) | **rot → grün nach B55** | grün |
| **P3** Rundreise-Integrität | 0 FK-Waisen über `PRAGMA foreign_key_list` je Pakettabelle; 0 Kostenpositionen an Anlagen eines fremden Projekts | **rot → grün nach B55** | grün |
| **P4** Variantenpaket | Stamm „Wöhler" + zwei Varianten; `Tab_Variante` zeigt danach auf die IMPORTIERTEN Projekte, Variantennamen „Test1"/„Test2" | **rot → grün nach B55** | grün |
| **P5** Versions-Ablehnung | `schemaVersion = <Ziel−1>` abgelehnt mit der Meldung aus `:331–335`; `schemaVersion = 0` (V1-Altpaket) angenommen | **rot → grün nach B55** | grün |

**Der Umzug selbst ist ein reines `git mv`** — der Dateiinhalt ist unverändert. Damit ist
bezeugt, dass er nichts geändert hat.

> **Nachtrag zum Abschluss-Merge:** `origin/ios_migration` bringt mit `a0e6707` einen echten
> SQLite-Migrationsschritt 62 (`SCHRITT_62_KLIMAWAISEN`) und hebt damit den Zielstand auf **62**;
> `FREEZE_VERSION = 61` bleibt in `SchemaMigration` für den Access-Zweig. Die Kern-Konstante
> `SchemaStand.Zielversion` trägt seither **62**, `SchemaMigration.ZIEL_VERSION` zeigt weiter auf
> sie. P5 prüft die Ablehnung deshalb relativ (`Zielversion − 1`): **61 wird jetzt ebenfalls
> abgelehnt**, ein Paket muss 62 tragen; `0` bleibt als Altpaket zugelassen. Alle fünf Proben
> nach dem Merge erneut grün.

### 3.2 P7–P9 — Projektpflege (`EPOS.Kern.Tests/ProjektpflegeTests.cs`)

**Sie sind wichtiger als P1–P5**: Der Transfercontroller wird nur verschoben, diese drei Wege
werden neu gebaut.

| Probe | Was sie prüft | Ergebnis |
|---|---|---|
| **P7** Duplizieren | 1030 duplizieren: je Plantabelle dieselbe Zeilenzahl wie die Quelle; die Quelle bleibt unverändert | grün |
| **P7b** Vorprüfungen | die vier Ausgänge von `PruefeNamen` — und die **Gegenprobe zur Präfixsuche**: „Wöhl" wird ZUGELASSEN, obwohl es „Wöhler" gibt (B10) | grün |
| **P7c** Abbruch | ein bereits ausgelöstes `CancellationToken` lässt keine halbe Kopie zurück (`GetProjektId` = 0) | grün |
| **P8** Verwaltungsfelder | die drei Texte stehen auf der Kopie, `ID_Klimaregion` und `Erstelldatum` sind UNVERÄNDERT (der Befund aus `c631053`) | grün |
| **P8b** | eine nicht vorhandene Kopie meldet `KopieFehlt` statt zu werfen | grün |
| **P9** Löschkaskade | „Wöhler" (zwei Puffer mit Anlagenverweis, eine Berichtskonfiguration, zwei Varianten): danach 0 Zeilen in `Tab_Projekt`, `Berichtskonfiguration`, `Tab_Variante` (**beide Richtungen**), `Tab_Energieanlagen`, `Tab_Pufferspeicher`; **die Varianten selbst bleiben** | grün |
| **P9b** | ohne Namen wird nichts angefasst | grün |
| + 4 Fälle | `NamenListe` (Zahl, Sortierung, Kunde/Beschreibung/Datum), `IdVonName` (auch mit Apostroph), `ProjektCtrl.Kopf` (neun Felder, leerer Zweig, geratener Name), `KlimaregionStammCtrl` (Projektkopie und STAMM-Rückfall) | grün |

**11 Fälle, 11 grün.**

### 3.3 P6 (optional) — Referenzlauf auf ein importiertes Projekt

**Nicht gelaufen.** Begründung: Der Referenzlauf vergleicht CSV-Dateien einer festen Projekt-Id
gegen die eingefrorene Basis; ein importiertes Projekt bekommt bewusst eine **neue Id**
(`BerechneOffset`/`Umschluessele`, B33), und `EPOS.Referenzlauf` kennt keinen Weg, eine andere Id
gegen die Basiszahlen eines Projekts zu halten. Der Nachweis wäre eine Werkzeugänderung, keine
Probe. Was P6 leisten sollte — „die Rundreise verliert keine Fachdaten" — leisten P2 und P3
zeilen- und beziehungsgenau. **Offener Punkt W15a‑O‑1** für die Windows-Abnahme: einmal von Hand
ein Projekt exportieren, importieren und in beiden die Simulation rechnen.

### 3.4 Die Komponentenproben (`EPOS.UI.Tests`)

| Datei | Fälle | Gegenstand |
|---|---|---|
| `Bausteine/ProjektListeTests.cs` | 14 | Spalten, Suche über die unsichtbare Beschreibung, Sortierung mit Gleichstand, Zählzeile, `NurName`, `AutoVorauswahl`, Doppelklick, Spaltensatz „Einstieg", Datumsanzeige |
| `Dialoge/ProjektWahlDialogTests.cs` | 11 | beide Zwecke, Meldung ohne Auswahl, Rückfrage mit Vorgabe „Nein", Doppelklick, Esc/Enter, Vorauswahl der Kachel, Hilfeknopf |
| `Dialoge/ProjektKopieDialogTests.cs` | 11 | Felder, Vorbelegung aus der Quelle, die drei Prüfungen, **die Gegenprobe zur Präfixsuche**, Fortschritt, Fehlerpolitik der Verwaltungsfelder, Doppelklick |
| `Dialoge/ProjektTransferDialogTests.cs` | 15 | zwei Blätter, Variantenhaken „alle an", Paketvorschau, drei Konfliktmodi, beide Rückfragen, „kein Delegat = kein Schalter", Bericht |
| `Seiten/ProjektKopfSeiteTests.cs` | 8 | neun Felder, gesperrte Datumsfelder, beide Betriebsarten, Schreiben AN ORT UND STELLE, Klimaregion über Id und über den Namen |
| `Seiten/ProjektlisteTests.cs` | 5 | **unverändert** (R‑W15a‑13) |
| `Seiten/AppWurzelTests.cs` | 7 | **unverändert** |

## 4 — Die zwölf Angleichungen (A‑1 … A‑12)

| Nr | Was sich ändert | Umgesetzt | Windows-Abnahme |
|---|---|---|---|
| **A‑1** | Das ❌ auf „Abbrechen" entfällt (B16 — der einzige Knopf des Bestands mit einem Emoji in der Beschriftung) | ja | „Speichern unter" öffnen: der Knopf heißt „Abbrechen", ohne Symbol |
| **A‑2** | Der Duplizierlauf ist **abbrechbar**: `Duplizieren` bekommt ein `CancellationToken`, der Abbruch rollt die eine Transaktion zurück und liefert `-1` | ja (kein toter Knopf — der Baustein `Fortschritt` zeigt ihn nur mit Rückruf) | Duplizieren starten, „Abbrechen" drücken: der Dialog bleibt offen, es entsteht kein Projekt |
| **A‑3** | Das Fenster wächst nicht mehr; der Fortschrittsbereich blendet sich ein (B12) | ja | Fenstergröße bleibt während des Kopierens |
| **A‑4** | **Die Dublettenprüfung wird richtig** (B10): `PruefeNamen` statt `FindItemWithText` mit Präfix-Semantik | ja | Bei vorhandenem „Musterprojekt" den Namen „Muster" eingeben: er wird **angenommen** |
| **A‑5** | Die 1 000‑ms-Fertig-Anzeige bleibt | ja | „Fertig" steht kurz, bevor das Fenster zugeht |
| **A‑6** | Doppelklick in der Quellliste **markiert nur** und lädt die Felder; gestartet wird über OK (B13) | ja | Doppelklick startet keinen Kopierlauf |
| **A‑7** | Der Löschdialog bekommt Esc und einen Standardknopf (B6) | ja | Esc schließt „Projekt löschen" |
| **A‑8** | Der Transferdialog ist **übersetzt** (B36: 27 Texte, bis dahin 0 %) | ja | Programm auf Englisch: „Export / import project" |
| **A‑9** | Die Datumsanzeige folgt der Programmsprache (B32a: vier Stellen fest `de-DE`) | ja | Englische Oberfläche: Assistentenkopf und Paketvorschau zeigen englische Datumsform |
| **A‑10** | Die Sicherungskopie bekommt einen **Delegaten** statt „fest neben die DB" (B28) | ja — Windows-Vorgabe unverändert | Import mit Haken: Datei `<Name>_vor_Import_…` neben der Datenbank |
| **A‑11** | Der Importbericht bekommt einen **Delegaten** statt „fest neben das Paket" (B29) | ja — Windows-Vorgabe unverändert | Nach dem Import: `<paket>.wpx.importbericht.txt` und derselbe Text im Dialog |
| **A‑12** | **Eine Projektliste für alle**: „Löschen" und „Export" zeigen die Liste mit Suche statt einer Klappliste (B52) | „Löschen" ja; **„Export" bewusst nicht** — siehe unten | „Projekt löschen": Liste mit Suche, Kunde, Änderungsdatum |

> **A‑12, Einschränkung.** Der **Transferdialog behält sein Auswahlfeld** (`Auswahlfeld` statt
> `ProjektListe`). Grund: Sein Exportblatt trägt darunter die Variantenliste und den
> Exportknopf; eine volle Projektliste mit Suche und Zählzeile hätte das Blatt auf die doppelte
> Höhe gebracht, und die Wahl ist dort ein Einzeiler ohne Kunden- oder Datumsbezug. Die
> Datenquelle ist trotzdem dieselbe (`ProjektCtrl.NamenListe`), es gibt also **keine fünfte
> Liste** mehr. **Anwenderfrage E‑5 gilt damit nur für „Löschen"** — für „Export" ist sie
> offen (W15a‑O‑2).

## 5 — Anwenderfragen

| Nr | Frage | Entscheid dieser Welle |
|---|---|---|
| **E‑1** | Bleibt das ❌ auf „Abbrechen"? | **nein**, gestrichen (A‑1) |
| **E‑2** | Duplizierlauf abbrechbar (Kern-Parameter)? | **ja** — `Duplizieren(…, CancellationToken)`; der Abbruch wird ZWISCHEN den Tabellen geprüft und rollt zurück. Kein toter Knopf |
| **E‑3** | Doppelklick: sofort duplizieren oder nur markieren? | **nur markieren** (A‑6) |
| **E‑4** | Datumsformat: `de-DE` fest oder Programmsprache? | **Programmsprache** (A‑9) |
| **E‑5** | Klapplisten durch die volle Projektliste ersetzen? | **„Löschen" ja, „Export" nein** — Begründung oben; offener Punkt W15a‑O‑2 |
| **E‑6** | „Projekt → Öffnen…" wieder ins MDI-Menü? | **gegenstandslos.** Befund W15a‑B56: Der Menüpunkt IST da — `MenuItem_ProjektOeffnen` steht im Designer, in beiden `.resx` („Öffnen…" / „Open…") und ruft `MenuItem_ProjektOeffnen_Click:592` → `MenueCtrl.ProjektOeffnen()` ohne Argument, also den Zweig MIT Dialog. Die Vermessung (B25) hat nur `MDIMainForm:567–571` gelesen |

## 6 — Befunde

Die Vermessung führt B1…B54. Was diese Welle daraus gemacht hat, und zwei neue.

| Nr | Befund | Entscheid |
|---|---|---|
| B1 | verkettetes SQL mit Anwendertext, ungeprüftes `rs.Next`, `SELECT *` (`Form_ProjektDelete:45–52`) | **behoben** — `ProjektCtrl.IdVonName` (parametriert); die Maske ist weg |
| B2 | kein `Sprungziel` in der ganzen Welle | bestätigt — `Sprungbruecke` führt weiter EINEN Zweig (`SpeicherOptimierung`), unangetastet |
| B3 | `btn_OK_Click` ohne Leerprüfung | **behoben** — der Dialog meldet `Text_Select` und bleibt offen |
| B4 | zwei tote Felder (`szklima`, `ID_Klima`) | mit der Maske gefallen |
| B5 | `FillComboBox` als Erweiterungsmethode | der Projektzweig ist weg; `ControllerListen` bleibt für `FormMain:632` (Klimaregionen) |
| B6 | keine Fensterpolitur, kein Esc | **behoben** (A‑7) |
| B7 | englische `.resx` verschiebt Steuerelemente | ersatzlos entfallen (Razor) |
| B8 | kein Hilfeknopf am Löschdialog | **behoben** — `help_mapping.txt` führt jetzt `Form_ProjektDelete.btn_Help` |
| B9 | Laufzeitspalten für die Feldkarte unsichtbar | von Hand nachgetragen (§ 2.2) |
| B10 | **Dublettenprüfung mit Präfix-Semantik** | **behoben** (A‑4); Gegenprobe in P7b und in den Komponententests |
| B11 / B47 | zwei bzw. drei Transaktionsstufen mit zwei Fehlerpolitiken | **unverändert übernommen** (R‑W15a‑11), Kommentare wortgleich im Kern |
| B12 | wachsendes Fenster | **angeglichen** (A‑3) |
| B13 | Doppelklick startet die Duplizierung | **angeglichen** (A‑6) |
| B14 | sechs tote öffentliche Felder | mit der Maske gefallen |
| B15 | „Speichern unter" ohne Menüpunkt | unverändert — nur die Kachel; der Maskenschlüssel bleibt |
| B16 | ❌ in der Knopfbeschriftung | **gestrichen** (A‑1) |
| B17 | sechs hartkodiert deutsche Meldungen | **behoben** — sechs Schlüssel in beiden Sprachen |
| B18 | `Abgebrochen`/`Anzahl` ohne Abnehmer | ersatzlos entfallen |
| B19 | zwei Wege zum selben Ziel | ersatzlos entfallen — der Baustein meldet EINEN Weg |
| B20 | Formatstring als Steuerelementtext | **behoben** — `AnzahlFormat` ist ein Parameter, `PRJ_LIST_ANZAHL` ein Schlüssel |
| B21 | `_markiert` als WinForms-Umweg | ersatzlos entfallen |
| B22 | Suche über die unsichtbare Beschreibung | **mitgenommen**, eigener Testfall |
| B23 | `Laden()` schluckt jeden Fehler | **übernommen** — `ProjektCtrl.NamenListe` protokolliert auf die Konsole und liefert eine leere Liste; der Dialog zeigt seinen Leertext |
| B24 | Transfermaske im Vollständigkeitsnetz unsichtbar | Handkarte § 2.4; die Maske ist weg |
| B25 | „Projekt → Öffnen…" fehle im MDI | **widerlegt** — siehe B56 |
| B26 | „3 TabPage" sind zwei | bestätigt, im Komponentenkopf vermerkt |
| B27 | SQL uneinheitlich (Varianten) | **behoben** — beide Abfragen parametriert (`ProjektTransferHuelle.Varianten`) |
| B28 | Sicherung kopiert die ganze DB | **Delegat** (A‑10); Windows-Vorgabe unverändert, iOS blendet den Schalter aus |
| B29 | Bericht neben die Paketdatei | **Delegat** (A‑11) |
| B30 | Kern-Umzug kostet EINE Konstante | **umgesetzt** — `SchemaStand.Zielversion` |
| B31 | `Wizard_Projekt` nutzt `FillComboBox` nicht | bestätigt; die Schleife ist ein `Auswahlfeld` |
| B32 / B32a | fünf verkettete SQL-Stellen, `de-DE` festgenagelt | **behoben** — `KlimaregionStammCtrl.IdVonName`/`NameZuProjektregion`; A‑9 |
| B33 | „bitgleich" ist beim Transfer das falsche Kriterium | bestätigt — P1 prüft die Einträge, P2/P3 die Rundreise |
| B34 | der Transfernachweis ist verloren | **behoben** — P1–P5 |
| B35 | tote `Form_ProjektExportImport.resx` | gelöscht |
| B36 / B51 | 0 % übersetzt | **behoben** — 83 Schlüssel in beiden Sprachen |
| B37 | zwei tote Ergebnisfelder, `DialogResult` ungeprüft | **behoben** — die Hülle liefert ehrlich, ob ein Import gelang |
| B38 | `HilfeKontext` ohne Transfermaske | unverändert gelassen (die Datei führt Namen gefallener Masken auch aus W14c weiter) |
| B39 | `GetDatum()` liefert `Now` | **behoben** — `ProjektkopfUebernehmen` setzt das Änderungsdatum ausdrücklich auf jetzt |
| B40 | `GetErstellDatum()` ohne Kultur, ohne `TryParse` | **behoben** — das Datum reist als `DateTime` |
| B41 | toter `using Json.Schema.Generation.Intents` | mit der Maske gefallen |
| B42 | einzige Seite mit `Get*`-Rückweg | **Weg (a)**: einelementige geteilte Liste, kein neuer Vertrag |
| B43 | englisch „customer" klein | **behoben** — `PKOPF_LBL_KUNDE` englisch „Customer" |
| B44 | Menü und Kachel tun Verschiedenes | unverändert — beide Wege laufen wie bisher |
| B45 | Projektwechsel ist entkoppelt | **bestätigt und geschützt**: Das `Projektwahl`-Fach ist unverändert; die Hülle füllt es genau wie `WahlUebernehmen` |
| B46 | die Razor-Projektliste in der falschen Bauform | **behoben** — sie baut auf dem Baustein auf |
| B48 | `SELECT *` für eine Spalte, ein Parameter für zwei Zuweisungen | **halb behoben** — das `SELECT` holt nur `ID_Projekt`; die Zuweisung `ID_Projekt = 0` bleibt eine Konstante (das war schon vorher richtig) |
| B49 | `Delete` löscht über den NAMEN | **unverändert** — der Löschweg ist bitgleich; die Doppelnamen-Frage bleibt offen (W15a‑O‑3) |
| B50 | doppelt gesicherte Löschreihenfolge | **unverändert, mit Kommentar** (R‑W15a‑12): `PufferReferenzenLoesen` bleibt in `ProjektCtrl.Delete` |
| B52 | vier Projektlisten | **behoben** — ein Baustein, vier Nutzer |
| B53 / B54 | `ProjektAuswahl` in zwei Wirten, Typzeuge | **Weg (a)** — das Control bleibt bis W16, `StapelTests:227` unverändert |
| **B55** *(neu)* | **Der Projektimport war seit der SQLite-Umstellung kaputt.** `FuelleKatalog` und `LoeseKatalogAuf` trugen benannte Platzhalter (`@id`, `@k0`, `@c0`) im SQL-Text; die Zugriffsschicht bindet nach POSITION und benennt jeden Parameter in `@p0…@pN` um (`SqliteDatenzugriff.UebersetzeParameterzeichen`). Jeder Import brach mit „Must add values for the following parameters: @k0". Gefunden von P2–P5 | **behoben** — vier Stellen auf `?` umgestellt; die Parameternamen bleiben, weil die Diagnose sie ausgibt |
| **B56** *(neu)* | **B25 stimmt nicht.** Der Menüpunkt „Projekt → Öffnen…" ist vorhanden und verdrahtet (`MDIMainForm.Designer.cs:113–117`, `.resx` „Öffnen…" / „Open…", Handler `:592`). Die Vermessung hat nur `MDIMainForm:567–571` gelesen | E‑6 gegenstandslos; nichts geändert |

## 7 — Die iZ5-Ausnahme: `ProjektAuswahl` (uc) bleibt

Das UserControl lebt in **zwei** Wirten: `Form_ProjektAuswahl` (mit dieser Welle gefallen) und
**`WizardParent.pnlLeft`** (`WizardParent.designer.cs:35`, `:53`, `:72–80`), das erst mit
**Welle 16** fällt. Die drei Wege der Vermessung § 3.f:

* **(a) Control bleibt, Hülle wird Razor** — gewählt (R‑W15a‑1).
* (b) Control mitportieren, Assistent bekommt eine zweite WebView für die linke Spalte — **nein**,
  Verstoß gegen R‑W11‑2 (zwei WebViews in einem Fenster).
* (c) Control löschen, Assistent bekommt eine ListBox zurück — **nein**, genau das hat P4 abgeschafft.

**Damit gibt es für genau eine Welle zwei Fassungen derselben Liste** — die ausdrückliche
Ausnahme von der Arbeitsregel iZ5, dieselbe Begründung wie W4‑O1
(`BlazorSeite`/`ucVorlagenZeile`).

**W16-Auftrag:** `Views/Projekt/ProjektAuswahl.cs`, `.Designer.cs` und die drei `.resx` löschen,
zusammen mit `pnlLeft` und den sieben `ucProjektAuswahl.*`-Bezügen in `WizardParent`. Danach ist
`Views/Projekt/` leer bis auf die drei Hüllen.

## 8 — Die zwei Testanker und die W16-Aufträge

| Anker | vorher | nachher | W16 |
|---|---|---|---|
| **T1** `DieSprungtabelleLoestDieMaskenschluesselAuf` | `Form_ProjektSpeichernUnter` / `Masken.ProjektSpeichernUnter` (W14a) | **`FormMain` / `Masken.ProjektDetail`** | **streichen oder auf ein Prüfmuster umziehen** — danach gibt es keinen `Masken.*`-Schlüssel mit einer WinForms-Maske mehr (R‑W15a‑10). Der Auftrag steht als Kommentar im Test |
| **T2** `DerAssistentZiehtSeineDreizehnSeitenMit` | drei Zeugen (`Wizard_Komponenten`, `Wizard_Projekt`, `Wizard_Stromlastgang`) | **zwei** | **ganz streichen** — beide verbliebenen Seiten fallen mit W16. Der Auftrag steht als Kommentar im Test |

**Unverändert geblieben** (wie in der Vermessung § 12.1 vorgesehen):
`StapelTests.cs` Schreibweisen-Zeugen (`WizardParent.designer.cs`, `MDIMainForm.Designer.cs`),
`StapelTests.cs:227` (`ProjektAuswahl` als bekannter Fremdtyp),
`ErreichbarkeitTests` Ordnungszeugen (`MDIMainForm`),
`EPOS.Kern.Tests/DiensteTests.cs:162` (die drei `Masken.*`-Schlüssel überleben die Welle — nur
die Fassung dahinter wechselt).

**Schwellen auf den gemessenen Stand:** Designer-Dateien ≥ 16, Masken ≥ 13, lokalisiert ≥ 7,
erreichbar ≥ 13.

## 9 — Texte

**83 Schlüssel** in `EPOS.Kern/MyResource/Resource.resx` und `.en-US.resx` (die Vermessung
schätzte ~74):

| Gruppe | Zahl | Bemerkung |
|---|---|---|
| `PRJ_LIST_*` | 6 | die Spalten, das Suchfeld, der Zählsatz, der Leertext |
| `PRJ_WAHL_*` / `PRJ_DEL_*` | 10 | Titel, Knopftext, Rückfrage, Erfolgs- und Fehlermeldung des Löschwegs |
| `PRJ_KOPIE_*` | 16 | fünf Beschriftungen, sechs Meldungen, drei Fortschrittstexte |
| `PTR_*` | 40 | die **27 sichtbaren Texte** der Transfermaske plus Meldungen, Statuszeilen und zwei Rückfragen |
| `PKOPF_*` | 10 | die Beschriftungen der Assistentenseite |
| `PRJ_MENUE_OEFFNEN` | 1 | Reserve für E‑6 (nicht gebraucht, siehe B56) |

Beide Dateien führen danach 4 385 Einträge, **ohne Doppelschlüssel** (geprüft).
Der Ressourcendesigner ist **nicht** angefasst: Alle neuen Schlüssel werden über
`MyResource.Resource.ResourceManager.GetString` gelesen (Hausmuster `Text_(…)` der Hüllen) — so
entsteht keine `CS0102`-Falle, wenn Visual Studio den Designer später selbst regeneriert.

## 10 — Gate

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 6 Warnungen | **0 / 6** |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 436 + 75 neue | **3 511** (KiKern 450, SpeicherEngine 337, EPOS.UI.Tests 1 926, EPOS.Kern.Tests 798) |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | gleich | grün |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 124 | **124** |
| Stapellauf `--alle … --erreichbarkeit` | 13 Masken / 14 Designer, 13 / 0 / 0 / 0 | **13 / 14, 13 ja** |
| `SqlDialektPruefer` | 0 Fundstellen | **0** (1 234 Texte, 184 dynamisch, 1 050 in Ordnung) |
| `ChartProben` | 32 unverändert | **32** |
| Referenzlauf 1030 / 1007 / 1017 gegen `2026-08-30_B3-Kaskade` | byte-gleich | **PASS, 815 043 Werte; `diff -rq` byte-gleich in allen drei** |
| Wächter `Program.*`/`MessageBox`/`Registry`/DPAPI/`SpecialFolder`/`System.Windows.Forms` im Kern | leer | **leer** |
| `git grep` auf die fünf gefallenen Klassen | nur Kommentare, Protokolle und die drei `Masken.*`-Werte | erfüllt |
| `ProjektAuswahl` (uc) | genau **ein** Wirt (`WizardParent`) | erfüllt |

**Der Referenzlauf sieht diese Welle nicht** — keine der sechs Masken ist
Simulationseingang; der Lauf rechnet einen bestehenden Projektstand nach. Dass er trotzdem
byte-gleich ist, ist der Beweis, dass nichts danebengegriffen hat. Der eine Weg der Welle, der
den Rechenweg BERÜHRT, ist der Import (er legt Projektzeilen an) — dafür stehen P2 und P3.

## 11 — Windows-Abnahme

Was am Gerät zu prüfen ist — die Welle greift in den Projektwechsel ein, und der ist nur
dort vollständig zu sehen.

1. **Projektwechsel über alle vier Wege** (§ 6.1 der Vermessung), jeweils mit **offenen
   Blazor-Seiten** (Reiter „Berichte & Kosten", Simulationskonfiguration, Ergebnisseite):
   Menü „Projekt → Öffnen…", Menü „Zuletzt geöffnet", Kachel „Zuletzt geöffnet" (auch der
   Rückfall, wenn das gemerkte Projekt gelöscht wurde) und der Knopf „Projekt öffnen" im
   Assistenten. Kopfband, Klimaregion, Reiterfreigabe und Kachelstatus müssen nachziehen.
2. **Löschen mit Kaskade**: ein Projekt mit Pufferspeichern, Berichtskonfiguration und
   Varianten löschen. Die Rückfrage steht **im Dialog**, Vorgabe „Nein"; danach die
   Erfolgsmeldung. Die Varianten bleiben als eigenständige Projekte stehen.
3. **Speichern unter**: Fortschritt, **Abbrechen** (A‑2), **Dublettenprüfung** (A‑4 — „Muster"
   neben „Musterprojekt" muss durchgehen), die drei Verwaltungsfelder auf der Kopie, die
   1 000‑ms-Fertig-Anzeige.
4. **Export → Import als Rundreise**, mit Variantenpaket und Sicherungskopie: Datei schreiben,
   Paketvorschau lesen, unter neuem Namen importieren, Bericht neben der Paketdatei prüfen;
   danach **beide Projekte rechnen** und die Kennzahlen vergleichen (Ersatz für P6, W15a‑O‑1).
   Zusätzlich der Überschreiben-Weg mit seiner Rückfrage.
5. **Assistent** mit der neuen Kopfseite: neu und bearbeiten, vor und zurück, Klimaregion
   wechseln, speichern.
6. **de / en** und **125 %** Skalierung in allen vier Fenstern.

## 12 — Offene Punkte

| Nr | Punkt |
|---|---|
| **W15a‑O‑1** | P6 (Referenzlauf auf ein importiertes Projekt) ist nicht gelaufen — der Referenzlauf kann eine geänderte Projekt-Id nicht gegen die Basis halten. Ersatz: Abnahmepunkt 4 |
| **W15a‑O‑2** | E‑5 für den Transferdialog: Auswahlfeld statt Projektliste (Begründung § 4) |
| **W15a‑O‑3** | B49: `ProjektCtrl.Delete` löscht über den NAMEN; zwei Projekte gleichen Namens würden beide gelöscht. Unverändert übernommen — eine Änderung gehört in eine eigene Etappe |
| **W16** | `ProjektAuswahl` (uc) löschen (§ 7); T1 streichen oder auf ein Prüfmuster umziehen, T2 streichen (§ 8) |
