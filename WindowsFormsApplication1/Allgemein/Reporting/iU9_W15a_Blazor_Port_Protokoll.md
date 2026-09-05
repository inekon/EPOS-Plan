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
| **P9c** mehrdeutiger Name | Arbeitskopie OHNE den eindeutigen Index `Projektname`, ein zweites Projekt desselben Namens: `LoeschenMitVorarbeiten` meldet `Mehrdeutig` mit Anzahl 2, **beide Zeilen stehen noch**, keine Vorarbeit ist gelaufen (Entscheid W15a‑O‑3) | grün |
| **P9d** mit Freigabe | derselbe Stand mit `mehrdeutigZugelassen: true`: **beide** Projekte fallen, alle Vorarbeiten sind gelaufen | grün |
| **P9e** Variante, mehrdeutig | dieselbe Arbeitskopie ohne den Index, ein zweites Projekt mit dem Namen der Variante 1023 („Wöhler ‑ Test1"): `VariantenCtrl.LoescheVariante` meldet `Mehrdeutig` mit Anzahl 2, **beide Zeilen stehen noch**, die `Tab_Variante`-Verknüpfung auch (Entscheid W15a‑O‑4) | grün |
| **P9f** mit Freigabe | derselbe Stand mit `mehrdeutigZugelassen: true`: **beide** Projekte fallen, dazu Verknüpfung und `Tab_Energieanlagen` der Variante | grün |
| **P9g** Stammprojekt | ein Stamm fällt über diesen Weg nicht — `LoeschStand.KeineVariante`, nichts angefasst (unverändert, nur als Befund statt als `false` + `out`) | grün |
| + 5 Fälle | `NamenListe` (Zahl, Sortierung, Kunde/Beschreibung/Datum), `IdVonName` (auch mit Apostroph), `AnzahlGleicherNamen` (Zählung, leerer Name, Apostroph), `ProjektCtrl.Kopf` (neun Felder, leerer Zweig, geratener Name), `KlimaregionStammCtrl` (Projektkopie und STAMM-Rückfall) | grün |

**17 Fälle, 17 grün** (11 aus der Welle, 3 aus dem Entscheid W15a‑O‑3 und 3 aus dem Entscheid
W15a‑O‑4, beide vom 04.09.2026).

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
| `Dialoge/ProjektWahlDialogTests.cs` | 15 | beide Zwecke, Meldung ohne Auswahl, Rückfrage mit Vorgabe „Nein", Doppelklick, Esc/Enter, Vorauswahl der Kachel, Hilfeknopf — dazu **vier Fälle zum Entscheid W15a‑O‑3**: die zweite Rückfrage bei mehrdeutigem Namen (auch sie mit Vorgabe „Nein"), „Nein" lässt den Dialog stehen, „Ja" meldet die Freigabe mit, ein eindeutiger Name fragt nicht nach |
| `Dialoge/ProjektKopieDialogTests.cs` | 11 | Felder, Vorbelegung aus der Quelle, die drei Prüfungen, **die Gegenprobe zur Präfixsuche**, Fortschritt, Fehlerpolitik der Verwaltungsfelder, Doppelklick |
| `Dialoge/ProjektTransferDialogTests.cs` | 15 | zwei Blätter, Variantenhaken „alle an", Paketvorschau, drei Konfliktmodi, beide Rückfragen, „kein Delegat = kein Schalter", Bericht |
| `Seiten/ProjektKopfSeiteTests.cs` | 8 | neun Felder, gesperrte Datumsfelder, beide Betriebsarten, Schreiben AN ORT UND STELLE, Klimaregion über Id und über den Namen |
| `Seiten/ProjektlisteTests.cs` | 5 | **unverändert** (R‑W15a‑13) |
| `Seiten/AppWurzelTests.cs` | 7 | **unverändert** |
| `Seiten/UebersichtSeiteTests.cs` | +3 | **aus dem Entscheid W15a‑O‑4**: die zweite Rückfrage der Variantenlöschung bei mehrdeutigem Projektnamen (Vorgabe „Nein" am betonten Knopf, Name und Anzahl im Text), „Nein" löscht nichts, „Ja" gibt alle Gleichnamigen frei, ein eindeutiger Name fragt nicht nach. Die Klasse pinnt dafür die Sprache selbst auf `de-DE` |

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
> Liste** mehr. **Anwenderfrage E‑5 gilt damit nur für „Löschen"**.
>
> **Entschieden am 04.09.2026** (W15a‑O‑2): Der Anwender hat die Empfehlung
> angenommen — **der Transferdialog behält sein Auswahlfeld**, im Export gibt es keine
> volle Projektliste. Die Begründung dieses Abschnitts gilt damit als Entscheid:
> Variantenliste und Exportknopf stehen darunter, die Wahl ist ein Einzeiler ohne Kunden-
> oder Datumsbezug, und die Datenquelle bleibt `ProjektCtrl.NamenListe`.

## 5 — Anwenderfragen

| Nr | Frage | Entscheid dieser Welle |
|---|---|---|
| **E‑1** | Bleibt das ❌ auf „Abbrechen"? | **nein**, gestrichen (A‑1) |
| **E‑2** | Duplizierlauf abbrechbar (Kern-Parameter)? | **ja** — `Duplizieren(…, CancellationToken)`; der Abbruch wird ZWISCHEN den Tabellen geprüft und rollt zurück. Kein toter Knopf |
| **E‑3** | Doppelklick: sofort duplizieren oder nur markieren? | **nur markieren** (A‑6) |
| **E‑4** | Datumsformat: `de-DE` fest oder Programmsprache? | **Programmsprache** (A‑9) |
| **E‑5** | Klapplisten durch die volle Projektliste ersetzen? | **„Löschen" ja, „Export" nein** — Begründung oben. **Für „Export" entschieden: nein** (Anwender, 04.09.2026, W15a‑O‑2) |
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
| B49 | `Delete` löscht über den NAMEN | **unverändert im Schreibweg, mit Vorprüfung** — der Löschweg ist bitgleich (`WHERE Projektname=?` in `Delete` und den drei Vorarbeiten). **Entschieden am 04.09.2026** (W15a‑O‑3): `LoeschenMitVorarbeiten` zählt VOR dem ersten Schritt `SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = ?`; bei mehr als einem Treffer meldet es den neuen Befund `LoeschStand.Mehrdeutig` mit der Anzahl und fasst nichts an. Der Löschdialog fragt daraufhin mit Vorgabe „Nein" nach (`PROJ_MSG_NAME_MEHRDEUTIG`), und erst `mehrdeutigZugelassen: true` lässt den Weg wie zuvor laufen |
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
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 436 + 75 neue, dazu 7 aus dem Entscheid W15a‑O‑3 und 6 aus dem Entscheid W15a‑O‑4 | **3 524** (KiKern 450, SpeicherEngine 337, EPOS.UI.Tests 1 933, EPOS.Kern.Tests 804) |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | gleich | grün |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 124 | **124** |
| Stapellauf `--alle … --erreichbarkeit` | 13 Masken / 14 Designer, 13 / 0 / 0 / 0 | **13 / 14, 13 ja** |
| `SqlDialektPruefer` | 0 Fundstellen | **0** (1 235 Texte, 184 dynamisch, 1 051 in Ordnung — der eine neue Text ist die Zählung aus W15a‑O‑3; W15a‑O‑4 bringt keinen weiteren, es ist dieselbe Zählung) |
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
| **W15a‑O‑2** | E‑5 für den Transferdialog: Auswahlfeld statt Projektliste (Begründung § 4). **Entschieden 04.09.2026: Empfehlung angenommen, Auswahlfeld bleibt** — keine volle Projektliste im Export; die Datenquelle bleibt `ProjektCtrl.NamenListe` |
| **W15a‑O‑3** | B49: `ProjektCtrl.Delete` löscht über den NAMEN; zwei Projekte gleichen Namens würden beide gelöscht. **Entschieden 04.09.2026** — der Anwender wörtlich: „Projektname darf nicht gleich sein, daher löschen. Rückfragen in diesem Fall." **Deutung:** Projektnamen SIND eindeutig — `Tab_Projekt` trägt seit der SQLite-Migration den eindeutigen Index `Projektname` (`CREATE UNIQUE INDEX "Projektname" ON "Tab_Projekt" ("Projektname")`, nachgesehen im `sqlite_master` von `Referenzlaeufe/Kenndaten_Test.sqlite`), und „Speichern unter" prüft über `ProjektDuplizierenCtrl.PruefeNamen`. **Das Löschen über den Namen bleibt deshalb bitgleich.** Für einen Altbestand OHNE diesen Index wird VOR dem Löschen nachgefragt, statt still beide Projekte mitzunehmen: `LoeschenMitVorarbeiten` zählt zuerst, meldet `LoeschStand.Mehrdeutig` mit der Anzahl und fasst nichts an; der Löschdialog stellt die Rückfrage mit Vorgabe „Nein" hinter der unveränderten Sicherheitsabfrage (A‑7), und erst `mehrdeutigZugelassen: true` lässt alle fallen. Proben: P9c/P9d in `ProjektpflegeTests` (Arbeitskopie ohne Index) und vier bunit-Fälle in `ProjektWahlDialogTests` |
| **W15a‑O‑4** | `VariantenCtrl.LoescheVariante` rief `new ProjektCtrl().Delete(projektname)` direkt und kannte die Vorprüfung aus O‑3 nicht — der letzte ihrer drei Schritte läuft damit über den NAMEN. **Entschieden 04.09.2026 (Empfehlung angenommen): Die Variantenlöschung geht über dieselbe Vorprüfung und dieselbe Rückfrage.** Umsetzung wie O‑3, nur an der zweiten Stelle: `LoescheVariante` zählt vor dem ersten Schritt über `ProjektCtrl.AnzahlGleicherNamen`, meldet bei mehr als einem Treffer `LoeschStand.Mehrdeutig` mit der Anzahl und fasst nichts an; mit `mehrdeutigZugelassen: true` läuft sie bitgleich wie zuvor. Sie liefert dafür denselben `LoeschBefund` wie `LoeschenMitVorarbeiten` statt `bool` + `out fehler` (neu darin `KeineVariante` und `Loeschfehler`, die nur dieser Weg meldet). **Einziger Aufrufer** ist `UebersichtSeiteGaben` hinter der Razor-Seite `UebersichtSeite`; die stellt hinter der unveränderten Löschfrage die zweite Rückfrage mit Vorgabe „Nein" und **denselben Textschlüsseln** wie der `ProjektWahlDialog` (`PROJ_MSG_NAME_MEHRDEUTIG`, `PROJ_MSG_NAME_MEHRDEUTIG_TITEL`) — es ist dieselbe Frage, deshalb kein neuer Text. Proben: P9e/P9f/P9g in `ProjektpflegeTests` und drei bunit-Fälle in `UebersichtSeiteTests`. Commit `5104ea3` |
| **W16** | `ProjektAuswahl` (uc) löschen (§ 7); T1 streichen oder auf ein Prüfmuster umziehen, T2 streichen (§ 8) |

---

## 13 — Windows-Abnahme 05.09.2026 (Befund W15a‑B‑1)

### 13.1 Befund W15a‑B‑1 — „Geändert Datum nicht ersichtlich"

**Beobachtung.** In „Projekt Speichern unter" (`ProjektKopieDialog`, Fenster
940 × 660) zeigt die Projektliste links die Spalten **Wahl / Projektname / Kunde /
Geändert**. Die dritte ist rechts abgeschnitten; der Anwender sieht das
Änderungsdatum nicht.

**Ursache — zwei Regeln, die sich gegenseitig aufheben.**

1. Der Dialog stellt Liste und Formular **nebeneinander**:
   `.epos-projektkopie-raster` ist ein Raster `minmax(0,3fr) minmax(0,2fr)`, und der
   Umbruch auf eine Spalte kam erst bei **780 px**. Im 940‑px‑Fenster stand das
   Formular (Neuer Projektname, Beschreibung, Kunde, Bearbeiter) also neben der
   Liste und ließ ihr drei Fünftel — rund **535 px** für vier Spalten.
2. Die Hausregel `.epos-raster td { white-space: nowrap }` hält jede Zelle in EINER
   Zeile. Sie ist richtig für ein Raster mit kurzen Bezeichnern („Namen brechen
   nicht in drei Zeilen") und falsch für eine Liste mit Projektnamen: Ein langer
   Name macht die Tabelle breiter, als die Spalte ist. Die Hülle rollt dann
   waagerecht (`overflow-x: auto`) — und die dritte Spalte liegt hinter dem
   Rollbalken. Der Anwender suchte auch keinen: **Die Tabelle sah vollständig
   aus.**

**Behebung — an beiden Enden.**

* **Der Umbruch kommt früher.** `@media (max-width: 780px)` wird
  `@media (max-width: 1100px)`: Die Liste steht dann über die **volle Breite**, das
  Formular darunter. Das trägt bei 1 024 px Fensterbreite, im 940‑px‑Dialog **und**
  auf dem iPad hochkant (768 × 1024). Auf einem breiten Schirm bleiben die zwei
  Spalten — dort ist der Platz da.
* **Die Spalten brechen um, statt die Tabelle zu treiben.** Jede Spalte der
  Auswahl trägt jetzt ihre Stilklasse: `epos-projektliste-name` und
  `…-kunde` bekommen `white-space: normal` und `overflow-wrap: anywhere`,
  `…-geaendert` bleibt `nowrap` mit fester Breite (7,5 rem). Das Datum ist die
  kürzeste Spalte und die einzige, deren Umbruch nichts brächte.

**Das Datumsformat bleibt, wie es war.** `Zelle(…, SPALTE_GEAENDERT)` liefert
`ToShortDateString()` — auf Deutsch also bereits `dd.MM.yyyy` („01.03.2026"), auf
Englisch die dortige Kurzform. Ein fest verdrahtetes `dd.MM.yyyy` wäre in der
englischen Oberfläche falsch; der Fall
`Das_Aenderungsdatum_steht_kurz_und_leer_wenn_keines_da_ist` hält die deutsche
Schreibweise fest.

**Die zwei Geschwister mitgeprüft.**

| Dialog | Liste | Befund |
|---|---|---|
| `ProjektWahlDialog` (Öffnen / Löschen) | `ProjektListe` über die **volle** Fensterbreite (760 px), kein Formular daneben | war nicht betroffen; die Umbruchregeln machen lange Namen dort trotzdem lesbar, statt waagerecht zu rollen |
| `ProjektTransferDialog` (Export / Import) | **keine** `ProjektListe` — ein `Auswahlfeld` mit den Projektnamen (Entscheid W15a‑O‑2, 04.09.2026) | nicht betroffen |

**Wachen.** `EPOS.UI.Tests/Bausteine/ProjektListeTests`:
`Jede_Spalte_der_Auswahl_traegt_ihre_Stilklasse` (Markup),
`Name_und_Kunde_brechen_um_das_Datum_nicht` und
`Speichern_unter_stapelt_Liste_und_Formular_bis_1100_Pixel` (die Regeln im
Stilblatt — eine bunit-Probe sieht sie nicht, Lehre W6‑B‑1).

**Abnahmepunkt A‑W15a‑B‑1.** „Projekt Speichern unter" im Vorgabemaß und bei
1 024 px Breite: Projektname, Kunde **und** Geändert sind vollständig lesbar, die
Liste rollt nicht waagerecht, das Formular steht darunter. Ein sehr langer
Projektname bricht um, statt die Spalten zu verschieben. Dasselbe in „Projekt
öffnen" und „Projekt löschen".

## 14 — Anwenderwunsch 05.09.2026 (W15a‑E‑1): Varianten in den Projektlisten

> **„Projekt öffnen: Es sollte wie zuvor kenntlich sein, welches Variantenprojekte
> sind."**

### 14.1 Das Bildschirmfoto

Projektassistent, Seite 0 in Betriebsart BEARBEITEN, linke Spalte „Bestehendes
Projekt auswählen". Die Liste zeigt zwei Spalten — **Wahl** und **Projektname ▲** —,
darunter „24 von 24 Projekten" und den Knopf „Projekt öffnen". Drei aufeinander
folgende Zeilen lesen sich gleich:

```
Booster-Kette mit Kombi-Spe…
Booster-Kette mit Kombi-Spe…
Booster-Kette mit Kombi-Spe…
```

In `Referenzlaeufe/Kenndaten_Test.sqlite` sind das die Projekte 1042
„Booster-Kette mit Kombi-Speicher", 1043 „… (2)" (eine Kopie aus „Speichern
unter") und 1044 „… ‑ Schichtspeicher" (die **Variante** von 1042,
`Tab_Variante`-Zeile 8). Unterscheidbar waren sie nur an dem Teil des Namens, den
der waagerechte Rollbalken abschnitt.

### 14.2 Das Vorbild — wie es „zuvor" war

**Als eigene Spalte gab es die Variante nie.** Weder das gelöschte UserControl
`ProjektAuswahl` (`git show d6e2433^:WindowsFormsApplication1/Views/Projekt/ProjektAuswahl.cs`,
418 Zeilen) noch die gelöschte Maske `Form_ProjektAuswahl`
(`git show 1b6d2be^:…/Form_ProjektAuswahl.cs`, 99 Zeilen) enthalten das Wort
„Variante" auch nur einmal. Kenntlich war eine Variante **am NAMEN**:

* `VariantenCtrl.AnlegenAusStamm` (`EPOS.Kern/Controller/VariantenCtrl.cs` :124)
  bildet den Projektnamen der Kopie als **`"<Stamm> - <Bezeichner>"`**, bei
  Namensgleichheit mit einem Zähler dahinter.
* `Form_Start.FuelleVariantenCombo`
  (`git show 428443f^:…/Views/Hauptformular/Form_Start.cs` :2087‑2143) zeigte in
  der Klappliste des Projektkopfes **genau diese Zeichenkette** — ausdrücklich
  „ohne Vorsatz »Stamm: «", weil das Feld an der Stelle des früheren blauen
  Projekttextes steht „und deshalb genau dessen Format" trägt.
* Die Reihenfolge dort kam aus `VariantenCtrl.LadeGruppe` (:40‑65): **der Stamm als
  erste Zeile**, danach seine Varianten `ORDER BY Variantenname`.

Das Vorbild ist also: *der volle Name*, und *die Gruppe beieinander, Stamm zuerst,
Varianten nach Bezeichner.*

### 14.3 Warum es nicht mehr trug

Der Name allein trägt nur, solange man ihn ganz sieht. Das Assistentenband ist
**280 px** breit (`.epos-assistent-band { width: 280px }`), und ausgerechnet der
abgeschnittene Teil (` - <Bezeichner>`) ist der, der die Variante ausmacht.

Dazu kommt ein zweiter, älterer Fehler: Die Umbruchregel aus **Befund W15a‑B‑1**
(§ 13) stand seit dem Vormittag im Stilblatt und **wirkte nicht**.
`.epos-raster td` hat die Spezifität (0,1,1), `.epos-projektliste-name` nur
(0,1,0) — die Hausregel `white-space: nowrap` gewann jedes Mal. In „Speichern
unter" fiel das nicht auf, weil dort § 13 zusätzlich den Umbruch des Rasters auf
1 100 px vorzog und die Liste damit die volle Breite bekam; im 280‑px‑Band gab es
diesen Ausweg nicht.

### 14.4 Die Umsetzung

**Kern — die Herkunft reist in der Zeile mit.**

* `ProjektKopfZeile` (`EPOS.Kern/Model/ProjektAngaben.cs`) trägt drei Felder mehr:
  `StammId` (0 = keine Variante), `Bezeichner` und `StammName`, dazu die
  abgeleitete Frage `IstVariante`.
* `ProjektCtrl.NamenListe` liest sie in **EINER** Abfrage mit zwei LEFT JOINs
  (`Tab_Projekt` → `Tab_Variante` → `Tab_Projekt` als Stamm), nicht mit einer
  zweiten Abfrage je Zeile: Die Liste wird bei jedem Suchtastendruck neu
  gezeichnet. Die Klammerung im FROM ist die von `VariantenCtrl.EntferneWaisen`
  (Jet verlangt sie bei zwei JOINs, SQLite nimmt sie klaglos an).
* **Ohne `Tab_Variante` läuft die alte Abfrage.** Die Tabelle legt
  `StelleVariantentabelleSicher` erst beim ersten Anlegen einer Variante an; ein
  LEFT JOIN auf eine fehlende Tabelle bräche die **ganze** Abfrage, und der
  Anwender sähe eine leere Projektliste. `VariantentabelleLesbar()` fragt vorher —
  still über `StilleDb`, wie jede Selbstheilungsauskunft des Hauses.

**Baustein `ProjektListe` — drei Mittel, alle drei aus dem Vorbild.**

1. **Der Name bricht um.** Die Regel aus § 13 bekommt den Tabellenselektor davor
   (`.epos-projektliste-raster .epos-projektliste-name`, (0,2,0)) und schlägt die
   Hausregel damit — eine Klasse mehr, keine Wichtigkeitsmarke.
2. **Die Gruppe steht beieinander.** `Gruppiert(…)` ordnet die **Stämme** nach der
   gewählten Sortierspalte und hängt jede Variante unmittelbar unter ihren Stamm,
   dort nach **Bezeichner** — die Ordnung von `LadeGruppe`. Auch absteigend, denn
   eine Gruppe ist keine Reihenfolge, sondern eine Zugehörigkeit. Fällt der Stamm
   durch den Suchfilter, steht die Variante selbst oben; sonst wäre sie nach einer
   Suche unauffindbar. Ein Sicherheitsnetz hängt ans Ende, was eine ringförmige
   Verweiskette sonst verschlucken würde — eine Liste darf eine Zeile nicht
   **verlieren**, auch nicht bei kaputten Daten.
3. **Die Auskunft steht da, wo Platz ist.** Im Spaltensatz `Auswahl` als Spalte
   **„Art"** zwischen Name und Kunde (»Stamm« / »Variante« mit dem Bezeichner
   darunter); in der schmalen Namenssicht des Assistenten und im iOS-Einstieg als
   **leise Zeile** „Variante von &lt;Stamm&gt;" unter dem Namen. Beides zugleich
   wäre dieselbe Auskunft zweimal, deshalb schließen sie einander aus.
   Zusätzlich ist jede Variantenzeile **eingerückt** und trägt eine senkrechte
   Linie zum Stamm hin — dieselbe Lesart wie die Einrückung der `Baumansicht`.

**Drei Entscheidungen, die begründet sein wollen.**

* **Die Artspalte erscheint nur, wenn die Liste überhaupt eine Variante führt.**
  Eine in allen 24 Zeilen leere Spalte nimmt dem Namen Platz weg und sagt nichts.
  Nebenwirkung: In einer Datenbank ohne Varianten sieht die Liste aus wie zuvor.
* **Ein Projekt ohne Varianten trägt in der Artspalte NICHTS** — es ist weder Stamm
  noch Variante, und ein Wort dafür hatte der Bestand nicht. »Stamm« steht nur an
  einem Projekt, an dem wirklich eine Variante hängt.
* **Die Suche greift über den Bezeichner.** Er hat nirgends eine eigene Spalte;
  wer ihn nicht durchsucht, macht ihn unauffindbar — dieselbe Lehre wie die
  unsichtbare Beschreibung (Befund W15a‑B22).

**Texte.** Vier neue Schlüssel in beiden `MyResource`-Katalogen:
`PRJ_LIST_SP_ART` (Art / Type), `PRJ_LIST_ART_STAMM` (Stamm / Base),
`PRJ_LIST_ART_VARIANTE` (Variante / Variant), `PRJ_LIST_VARIANTE_VON`
(„Variante von {0}" / „Variant of {0}"). Ohne Stammnamen — der Stamm ist gelöscht
— bleibt das bloße Wort stehen: „Variante von " ohne Namen wäre ein angefangener
Satz.

### 14.5 Wo die Kennzeichnung überall gilt

| Ort | Spaltensatz | Kennzeichnung |
|---|---|---|
| Assistent Seite 0, linkes Band (`AssistentSeite`) | `NurName` | Einrückung + leise Zeile „Variante von …" |
| „Projekt öffnen" / „Projekt löschen" (`ProjektWahlDialog`) | `Auswahl` | Artspalte + Einrückung |
| „Projekt Speichern unter" (`ProjektKopieDialog`) | `Auswahl` | Artspalte + Einrückung |
| iOS-Einstieg (`Seiten/Projektliste`) | `Einstieg` | Einrückung + leise Zeile |
| „Export / Import" (`ProjektTransferDialog`) | — | keine `ProjektListe`, ein `Auswahlfeld` (Entscheid W15a‑O‑2) — unverändert |
| Startseite, Klappliste des Projektkopfes (`Startseite.Varianten`) | — | **schon gekennzeichnet und unverändert**: Sie führt nur die Gruppe des offenen Projekts, Stamm zuerst, und zeigt den vollen Namen „&lt;Stamm&gt; ‑ &lt;Bezeichner&gt;" — genau `FuelleVariantenCombo` |

### 14.6 Wachen

`EPOS.Kern.Tests/ProjektpflegeTests.Die_Namensliste_nennt_zu_jeder_Variante_ihren_Stamm`
hält jede Zeile gegen `Tab_Variante` selbst (nur lesend).
`EPOS.UI.Tests/Bausteine/ProjektListeTests` führt neun Fälle: Artspalte mit
Bezeichner, leere Art am gewöhnlichen Projekt, keine Artspalte ohne Varianten,
Gruppierung nach Bezeichner (auch unter Datumssortierung), Einrückung und leise
Zeile im schmalen Band, kein Doppel aus Spalte und Zeile, Suche über den
Bezeichner, Variante ohne ihren Stamm, Variante ohne Stammnamen — dazu
`Die_Umbruchregel_schlaegt_die_Hausregel_des_Rasters` und
`Die_Variantenzeile_ist_im_Stilblatt_eingerueckt`, die die **Regeln** prüfen (eine
bunit-Probe sieht ein Stilblatt nicht, Lehre W6‑B‑1).

### 14.7 Abnahmepunkt A‑W15a‑E‑1

„Projekt öffnen": Über den drei „Booster-Kette…"-Zeilen steht die Spalte **Art**;
1044 trägt dort »Variante« mit dem Bezeichner „Schichtspeicher", 1042 »Stamm«,
1043 nichts. 1044 steht **unmittelbar unter** 1042 und ist eingerückt. Der
Projektname ist in jeder Zeile **vollständig** lesbar, die Liste rollt nicht
waagerecht. Die Suche nach „Schichtspeicher" findet 1044. Dasselbe in „Projekt
löschen" und „Speichern unter".

Assistent, Seite 0: In der 280 px breiten Spalte steht der volle Projektname
(umgebrochen, nicht abgeschnitten); unter jeder Variante steht leise „Variante von
Booster-Kette mit Kombi-Speicher", und die Zeile ist eingerückt. Dasselbe auf dem
iPad hochkant.
