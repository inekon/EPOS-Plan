# iU9 Welle 14c — Gesetze, Klimadaten, Einstellungen, Dubletten: Portprotokoll

**Fünf WinForms-Masken → fünf Razor-Komponenten in vier Fenstern und vier Hüllen**, jede
WinForms-Fassung im selben Commit gelöscht (Regel M1). Stand 04.09.2026, Basis `4e77221`
(nach W14a und W14b).

| Maske | `.cs` | Designer | MessageBox | Nachfolge |
|---|---|---|---|---|
| `Form_Gesetzesparameter` | 403 | 207 | 3 | `GesetzeskatalogDialog` |
| `Form_GesetzparameterZeile` | 258 | 224 | 3 | `GesetzeskatalogZeileDialog` (Überlagerung) |
| `Form_KatalogDubletten` | 800 | — | 9 | `KatalogDublettenDialog` |
| `Form_AdminSettings` | 320 | 491 | 4 | `EinstellungenDialog` |
| `Form_Klimadaten` | 417 | 503 | 7 (+2) | `KlimaregionDialog` |
| **Summe** | **2 198** | **1 425** | **26 (+2)** | **5 Komponenten, 4 Fenster** |

**Der Befund der Vermessung hat sich bestätigt:** Vier der fünf Fachteile lagen schon im
Kern — `GesetzKatalog` (1 123 Z.), `DublettenPruefung`/`KatalogBereinigung`/`KatalogRegistry`
und `SolarPVGISCalculator`. Die Kern-Vorarbeit war **Zuschnitt, kein neuer Rechenweg**; die
einzigen echten Umzüge waren `KlimaregionStammCtrl`, `KlimaImportAblauf` und
`EinstellungenCtrl`.

**Drei Dinge nimmt die Welle mit, die keine Maske sind:** die **letzten zwei ablösbaren
`Sprungziel`e**, **alle sechs WFO1000** der Mappe und den **letzten MS-Chart-Nutzer**
(`ChartManager`, 560 Z.). Neu ist der Baustein `Baumansicht` für den einzigen `TreeView`
des Bestands.

---

## 1 Commits

| Commit | Schritt | Inhalt |
|---|---|---|
| `8ee59d7` | **W14c.0k** | `EPOS.Kern.Tests/KatalogpflegeTests.cs` — der Nachweis VOR der ersten Maske (69 Fälle) |
| `1576b76` | **W14c.0a/0b/0c** | Steuerwertlisten, `Pruefe`/`Existiert`, `GesetzZeile` im Gesetzeskatalog |
| `701a042` | **W14c.0l** | 80 Textschlüssel in beiden Sprachen; `GESETZ_BTN_UEBERNEHMEN` gelöscht |
| `ec1fcdd` | **W14c.1/2/3** | Gesetzeskatalog und Zeilendialog; die zwei Sprungziele fallen |
| `edc3221` | **W14c.0f/0g/0h/4/5** | `DublettenBefundText`, `SatzUmbenennen`, `DublettenBaum`, Baustein `Baumansicht`, Dublettensuche |
| `fabbc46` | **W14c.0i/6** | `EinstellungenCtrl` im Kern, `EinstellungenDialog` |
| `0bd2f25` | **W14c.0d/0e/0j/7/8** | `KlimaregionStammCtrl` in den Kern, `KlimaImportAblauf`, `MinimumNull`, `KlimaregionDialog`; `ChartManager` und `RoundedPanel` gelöscht |
| `2f8a0b3` | **W14c.9** | Die acht Testanker, fünf Schwellen, vier `CLAUDE.md` |
| (dieser) | **W14c.10** | Portprotokoll |

---

## 2 Der Nachweis entsteht zuerst (R‑W14c‑1)

**Befund W14c‑B62:** Für `GesetzKatalog`, `DublettenPruefung`, `KatalogBereinigung`,
`KatalogRegistry`, `KlimaregionStammCtrl`, `SolardatenCtrl`, `PVGIS_EPW_Downloader` und
`SolarCalculator` gab es **keinen einzigen Test**. Der Referenzlauf sieht nichts davon — er
rechnet einen bestehenden PROJEKTstand nach, diese Masken pflegen STAMMdaten.

`EPOS.Kern.Tests/KatalogpflegeTests.cs` steht deshalb im **ersten** Commit der Welle, vor jeder
portierten Zeile, und ist mit jeder Vorarbeit gewachsen — **104 Fälle** über eine Arbeitskopie
von `Kenndaten_Test.sqlite`, nur lesend und mit EINER Kopie je Klasse (Regel seit W11a); die
sechs schreibenden Fälle legen sich ihre eigene an.

| Gruppe | Was eingefroren ist |
|---|---|
| Registry | 19 Kataloge in Reihenfolge, die zwei Datenblöcke der Klimaregion, die vier Kataloge mit Verwendungsprüfung, die 19 Anzeigenamen |
| Scan | Satzzahl / Namensgruppen / Inhaltsgruppen je Katalog (51/63/13/7/6/79/5/277/32/16/13/41/40/32/20/3/1/4/12), `VergebeneNamen`, `NormalisiereName` |
| Gesetzeskatalog | 222 Zeilen, neun Klassen ohne `SYSTEM`, die Zeilenzahl je Klasse (14/35/38/30/15/52/29/7/1), Sortierung, Stichtagsregel, leeres Wertfeld = NULL, Vorbelegung 221 / Generation 6 |
| Steuerwerte | acht Klassen im Vorrat, 15 Einheiten, 3 Statuswerte samt DB-Schreibweisen, `WertText` mit `"0.####"` |
| Prüfung | die drei Regeln in ihrer Reihenfolge, `Existiert` je Klasse, die eigene Id zählt nicht mit |
| Baum | Registry-Reihenfolge, nur gescannte Kataloge, Wurzel auch ohne Dubletten, Wurzel/Ast offen — Gruppe zu |
| Befundtext | Namensspalte zuerst, dann die Vergleichsspalten; Gruppe: erster Satz gegen jeden weiteren |
| Klimaimport | die acht Tagtyp-Werte, die Halbe-Globalstrahlung-Regel, vier Fassaden + Sonnenwinkel, der ganze Ablauf gegen die **eingefrorene TMY-Antwort**, Dublette, Abbruch |
| Sonnenrechnung | `CalculateHourly` (Nacht = 0, Mittag Süd > Nord, Winkel 16…18° am 1. Januar in Stuttgart), `GetDailyAverages` (Maximum, nicht Mittel) |
| Einstellungen | die vier Vorgabepfade und ihre Regeln, `Lesen` füllt alle neun, der Ordnerfehler meldet sich |
| Schreibwege | Anlegen/Ändern/Löschen einer Gesetzeszeile, `SatzUmbenennen`, die Löschkaskade (8 760 + 365 gehen mit), die Leerkopien- und die Leerwert-Regel |

**Kein Netz im Test** (R‑W14c‑5): Die TMY-Antwort kommt aus
`Referenzlaeufe/Importproben/pvgis_tmy_stuttgart_72h.json` — 72 Stunden, deterministisch
gerechnet, PVGIS-Form — und läuft über **denselben Leser wie die echte Antwort**
(`PVGIS_EPW_Downloader.AusJson`, aus `GetTMY` herausgezogen).
**Grenze, ehrlich benannt:** Die Probe ist synthetisch, nicht ein mitgeschnittener echter
PVGIS-Lauf. Sie sichert den ABLAUF und die Rechnung, nicht das Format eines künftigen
PVGIS-Servers — dafür ist der Windows-Abnahmepunkt 6 da.

---

## 3 Feldkartenabgleich je Maske

Die Karten sind vor dem Port gezogen worden
(`dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`);
`Form_KatalogDubletten` hat keinen Designer, ihre Karte ist von Hand aus
`BaueOberflaeche:70–185` geschrieben (R‑W14c‑10).

### 3.1 `Form_Gesetzesparameter` → `GesetzeskatalogDialog` (Karte 7 Zeilen)

| # | Feld der Karte | Nachfolge | ☑ |
|---|---|---|---|
| 1 | `lblHinweis` (die Kernregel, blau) | `Warnbanner` Stufe Hinweis | ☑ |
| 2 | `cbKlasse` | `Auswahlfeld` „Bereich", gefüllt aus `GesetzKatalog.Klassen()` | ☑ |
| 3 | `lvZeilen` (6 Spalten, `MultiSelect=false`, Doppelklick = Ändern) | `Raster` + `Zeilenwahl`, sechs `PropertyColumn` | ☑ |
| 4 | `btnNeu` | Knopf; öffnet den Zeilendialog als Überlagerung | ☑ |
| 5 | `btnAendern` | Knopf, **an der Auswahl** statt an der Listenlänge (A‑13) | ☑ |
| 6 | `btnLoeschen` | Knopf, dito; Rückfrage mit `VorgabeNein` (A‑1) | ☑ |
| 7 | `btnSchliessen` | Knopf, **liefert jetzt OK** (B11) | ☑ |
| — | `InfoKnopf.Anbringen` | `<InfoKnopf Schluessel="Form_Gesetzesparameter.btn_Help" />` | ☑ |

**Ersatzlos entfallen:** `_ = lvZeilen.Handle` (die WinForms-Eigenheit „ohne Handle greift die
ListView-Auswahl nicht"), `ZeilenAnzahl`, `Auswahl`, `Waehle` und die drei Testdelegaten.

### 3.2 `Form_GesetzparameterZeile` → `GesetzeskatalogZeileDialog` (Karte 10 Zeilen)

| # | Feld der Karte | Nachfolge | ☑ |
|---|---|---|---|
| 1 | `tbSchluessel` (`CharacterCasing=Upper`) | `Textfeld`, `NurLesen` beim Ändern | ☑ |
| 2 | `cbKlasse` | `Auswahlfeld`, gesperrt beim Ändern | ☑ |
| 3 | `tbJahr` | `Ganzzahlfeld`, Min 1990 / Max 2100 | ☑ |
| 4 | `tbWert` | `Zahlenfeld`, **leer erlaubt** | ☑ |
| 5 | `lblWertLeer` („leer = der Satz ist entfallen (nicht 0)") | `Herleitungszeile` | ☑ |
| 6 | `cbEinheit` | `Auswahlfeld` aus `GesetzKatalog.Einheiten()` | ☑ |
| 7 | `cbStatus` | `Auswahlfeld` aus `Statuswerte()` | ☑ |
| 8 | `tbQuelle` (`MaxLength=120`) | `Textfeld`, `Hoechstlaenge=120` | ☑ |
| 9 | `btnOk` (`SIM_BTN_OK`) | `SpeichernLeiste`, OK | ☑ |
| 10 | `btnAbbruch` | `SpeichernLeiste`, Abbrechen | ☑ |
| — | **kein Hilfeknopf** (B3) | **wörtlich übernommen** — kein `InfoKnopf` | ☑ |

### 3.3 `Form_KatalogDubletten` → `KatalogDublettenDialog` (Karte VON HAND, 10 + 4 Felder)

| # | Feld aus `BaueOberflaeche` | Nachfolge | ☑ |
|---|---|---|---|
| 1 | `_cbKatalog` (`DropDownList`, „(alle Kataloge)" + 19) | `Auswahlfeld` | ☑ |
| 2 | `_btnPruefen` | Knopf; Scan in `Task.Run` (A‑15) | ☑ |
| 3 | `_lblStatus` (blau) | `.epos-status` in der Kontextleiste | ☑ |
| 4 | `_tree` (`HideSelection=false`) | **`Baumansicht`** | ☑ |
| 5 | `_tbDetails` (mehrzeilig, nur lesend, Festbreite) | `Textfeld` `Mehrzeilig`/`NurLesen`/`Festbreite` | ☑ |
| 6 | `_btnBereinigen` | Knopf; an der Auswahl bzw. am einzigen gescannten Katalog | ☑ |
| 7 | `_btnLoeschen` | Knopf; nur am Satz | ☑ |
| 8 | `_btnUmbenennen` | Knopf; nur am Satz | ☑ |
| 9 | `_btnProtokoll` | Knopf; **meldet bei leerem Protokoll** (B47) | ☑ |
| 10 | `_tbProtokoll` | `Textfeld` `Mehrzeilig`/`NurLesen`/`Festbreite` | ☑ |
| — | **kein Schließen-Knopf** (B38) | **neu**: „Schließen" (A‑14) | ☑ |
| 11–14 | der handgebaute Umbenennen-Dialog (`Label`, `TextBox`, 2 × `Button`) | `NamensDialog` als Überlagerung **mit `Pruefung`** (B46) | ☑ |
| — | `InfoKnopf.Anbringen`, `FensterEinpassung.Einhaengen` | `InfoKnopf`; die Fenstereinpassung entfällt ersatzlos | ☑ |

### 3.4 `Form_AdminSettings` → `EinstellungenDialog` (Karte 28 Zeilen)

| Rubrik | Felder der Karte | Nachfolge | ☑ |
|---|---|---|---|
| `listBox_Rubriken` (4 Einträge) | — | **`Reiter` mit vier `Reiterblatt`, senkrecht** (A‑16/A‑17) | ☑ |
| VDI Datensätze | `lbl_VDIPath`, `txt_VDIPath`, `btn_VDIPathBrowse` | `Dateiwahl` mit `OrdnerWaehler` | ☑ |
| Datenbank | `txt_DBExportPath`, `txt_DBImportPath`, `txt_DBPath` (je mit Knopf), `txt_DBName` | 3 × `Dateiwahl` + `Textfeld`; dazu der Neustart-Hinweis (B52) | ☑ |
| Web-Schnittstellen | `txt_PVGISUrl`, `txt_OnlineDokuUrl`, `txt_GEOCodUrl` | 3 × `Textfeld` | ☑ |
| Anwendung | `txt_AllgemeinPath` + Knopf | `Dateiwahl` | ☑ |
| Anwendung | **`chk_KiAus` + `lbl_KiAus` (ZUR LAUFZEIT gebaut, R‑W14c‑6)** | `Gruppenkopf` + `Schalter` + `Herleitungszeile`, dazu `Warnbanner` bei Maschinenriegel | ☑ |
| Fuß | `btn_Speichern`, `btn_Abbrechen` | `SpeichernLeiste` | ☑ |
| Fuß | `btn_Standardwerte` | Knopf mit `Rueckfrage` (`VorgabeNein`) | ☑ |
| Fuß | `btn_Help` | `InfoKnopf` | ☑ |

### 3.5 `Form_Klimadaten` → `KlimaregionDialog` (Karte 17 Zeilen, drei Ebenen tief)

| # | Feld der Karte | Nachfolge | ☑ |
|---|---|---|---|
| 1 | `label1` (Kopftext) | `Herleitungszeile` — **Tippfehler berichtigt** (B35) | ☑ |
| 2 | `comboBox_Ort` (`DropDown`, freie Eingabe) | `<input list>` mit `<datalist>`; **Vorschläge, keine Startbedingung** (E‑7) | ☑ |
| 3 | `textBox_Longitude` | `Zahlenfeld`, `Feldname` „Longitude" (B27) | ☑ |
| 4 | `textBox_Latitude` | `Zahlenfeld`, `Feldname` „Latitude" | ☑ |
| 5 | `textBox_Bezeichnung` | `Textfeld` | ☑ |
| 6 | `label9` („oder"), `label10` (Gruppentext) | Text bzw. `Gruppenkopf` | ☑ |
| 7 | `listBoxKlimreg` | `Raster` + `Zeilenwahl` | ☑ |
| 8 | `btn_Delete` | Knopf mit `Rueckfrage` (A‑7) und Kaskade (A‑8) | ☑ |
| 9 | `btn_Import` | Knopf; `Task.Run` mit `Fortschritt` und Abbrechen (A‑4) | ☑ |
| 10 | `pBar_Import` (Maximum 9 125 im Designer, 7 im Code — B25) | `Fortschritt`, sieben Schritte | ☑ |
| 11 | `textBox_Display` | `Textfeld` `Mehrzeilig`/`NurLesen` | ☑ |
| 12 | `tabControl1` + `tabPage1`/`tabPage2` | `Reiter` + 2 × `Reiterblatt` | ☑ |
| 13 | `chart1` / `chart2` | 2 × `ChartBild` aus `ChartRenderer.Jahresgang` | ☑ |
| 14 | `btn_Beenden` | Knopf | ☑ |
| 15 | `btn_Help` (im DESIGNER, ohne Handler) | `InfoKnopf` | ☑ |
| — | `label3` (tot: 0 × 28, kein Text) | ersatzlos | ☑ |
| — | `panel_KlimaGraph_Paint` (blauer Akzentbalken) | CSS | ☑ |
| — | `IncrPBar`, `AxisScrollBarClicked` (beide tot, B24) | ersatzlos | ☑ |

---

## 4 Abweichungen (A‑Zeilen)

**Siebzehn Angleichungen, zwei hingenommene Abweichungen.**

| Nr | Was sich ändert | Warum | Windows-Abnahme |
|---|---|---|---|
| **A‑1** | `Rueckfrage` bekommt `VorgabeNein`; sechs Fragen betonen „Nein" | Bei „Satz endgültig löschen?" ist ein hervorgehobenes „Ja" ein Rückschritt (`MessageBoxDefaultButton.Button2` des Bestands) | 1, 4, 7 |
| **A‑2** | Die zwei Klimadiagramme tragen eine **Legende** | `Jahresgang` zeichnet sie immer; bei EINER Reihe ist das eine Zeile — **hingenommen** | 6 |
| **A‑3/E‑4** | Die Sonnenwinkel-Achse beginnt weiter bei 0 | über `MinimumNull` (W14c.0j) — ohne den Schalter sähe das Bild sichtbar anders aus | 6 |
| **A‑4** | Der Klimaimport lässt sich **abbrechen** | Ein Import mit Netzabruf und 9 125 Zeilen ohne Abbruch ist eine Zumutung | 6 |
| **A‑5** | **Kein Mausrad-Zoom** mehr in den Klimadiagrammen | dieselbe Abweichung wie W8‑A‑1; ein PNG bleibt ein Bild — **hingenommen** (offener Punkt W3‑O2) | 6 |
| **A‑6/E‑2** | Die x-Achse heißt **„Monat"** statt „Jahresstunden" | Das ist, was gezeichnet wird (B37) | 6 |
| **A‑7** | Klimaregion löschen **fragt** | 9 126 Zeilen ohne Nachfrage zu löschen ist der Ausreißer | 7 |
| **A‑8** | Klimaregion löschen **räumt die Datenblöcke ab** | über `KatalogBereinigung.SatzLoeschen`; der alte Weg liess Waisen stehen (B23) | 7 |
| **A‑9** | Die Dublettenprüfung des Imports fragt die **Datenbank** und **meldet** | die Präfixsuche in der Anzeige traf „Berlin" auch bei „Berlin_2024" und kehrte still zurück (B26) | 6 |
| **A‑10** | **Ein** PVGIS-Abruf statt vier | drei wurden geholt und weggeworfen (B28); kein gespeichertes Byte ändert sich | 6 |
| **A‑11** | Die Tageswerte tragen den `Listbezeichner` | im Handeingabe-Zweig stand dort ein leerer Name (B31) | 6 |
| **A‑12** | „Standardwerte" setzt den DB-**Namen** ins **Namensfeld** | der einzige echte Rechenfehler der Welle (B53) | 8 |
| **A‑13** | „Ändern"/„Löschen" hängen an der **Auswahl** | ein Knopf, der nichts tut, ist eine Behauptung, die nicht stimmt (B10) | 1 |
| **A‑14** | Der Dublettendialog bekommt einen **Schließen-Knopf** | jede andere Maske hat einen (B38) | 4 |
| **A‑15** | Der Dublettenscan läuft **im Hintergrund mit Fortschritt** | 19 Kataloge im Bedienfaden sind ein eingefrorenes Fenster (B41) | 4 |
| **A‑16** | Die vier Rubriken sind **Reiter mit Schlüsseln** statt Panels über den Index | Voraussetzung der Lokalisierung (B50/B51) | 8 |
| **A‑17** | Die Panelnamen sind beim Neubau richtig benannt | „Datenbank" zeigte `panel_Export` (B50) | 8 |

### Wörtlich trotz Befund

| Befund | Was bleibt, wie es war |
|---|---|
| **B3** | `GesetzeskatalogZeileDialog` hat **keinen** Hilfeknopf — als einziger der fünf |
| **B5** | Der Zeilendialog bietet die **acht** Klassen des Vorrats an, die Liste der Wirtsmaske führt **neun** (mit `EEG`). Beide Listen stehen jetzt an EINER Stelle, aber sie bleiben verschieden — `EEG`-Zeilen pflegt der Vergütungsdialog |
| **B8** | Die Id wird nur durchgereicht; beim Anlegen ist sie 0 |
| **B16** | `initChart` ist ersatzlos entfallen — es überschrieb ohnehin nur, was `ChartManager.Init` danach wieder setzte |
| **B30** | Die Klasse heißt weiter `AccessRepository`, obwohl die Datenhaltung SQLite ist — ein Umbenennen wäre ein eigener Schritt |
| **B39** | `KlasseItem`/`KatalogItem` sind in beiden Fällen eine `(Wert, Anzeige)`-Liste am `Auswahlfeld` geworden — der Träger fällt weg, die Trennung bleibt |

### Ersatzlos entfallen

`ZeilenAnzahl`, `Auswahl`, `Waehle`, `_ = lvZeilen.Handle` (B9/B10), die drei Testdelegaten
(B14), `IncrPBar` und `AxisScrollBarClicked` (B24), `RecordSet rs` im Löschhandler (B20),
`label3` der Klimadaten, `GetConfiguredOrDefaultPath(szPath)` (B54), `FensterEinpassung` im
Dublettendialog und `GESETZ_BTN_UEBERNEHMEN` (B4).

---

## 5 Befunde W14c‑B1 … B64 mit Entscheid

| Nr | Entscheid |
|---|---|
| **B1** | keine der fünf Masken steht in `Masken.cs` — **unverändert**: Die vier Hüllen werden unmittelbar gerufen, kein Maskenschlüssel entsteht |
| **B2** | `Form_GesetzparameterZeile` ohne `.resx` — gegenstandslos, die Komponente hat keine |
| **B3** | kein Hilfeknopf — **wörtlich übernommen** |
| **B4** | `GESETZ_BTN_UEBERNEHMEN` unbenutzt — **gelöscht** (W14c.0l) |
| **B5** | zwei Klassenlisten — **beide im Kern** (`KlassenVorrat` / `Klassen`), der Unterschied ist jetzt beschrieben statt zufällig |
| **B6** | `Einheiten()` `internal` ohne zweiten Aufrufer — jetzt `public` im Katalog, mit zwei Aufrufern |
| **B7** | dieselbe Prüfung zweimal — **`GesetzKatalog.Pruefe`, einmal** |
| **B8** | `_id` nur durchgereicht — **wörtlich** |
| **B9** | `ZeilenAnzahl` ohne Leser — **entfallen** (als Prüfhilfe der Komponente wieder da, jetzt MIT Test) |
| **B10** | Knöpfe an der Listenlänge — **A‑13** |
| **B11** | kein `DialogResult` — **„Schließen" liefert OK** |
| **B12** | Dublettenprüfung lädt den Katalog neu — **SQL-Zählung** |
| **B13** | beide Sprungquellen sind Razor — **W14c.3: zwei Überlagerungen** |
| **B14** | fünf WFO1000 für vier ungenutzte Eigenschaften — **alle fünf gefallen**, ersetzt durch 25 bunit-Fälle |
| **B15** | Ortsliste fehlt, `Load` wirft — **E‑7: Vorschlagsliste, nie ein Absturz** |
| **B16** | `initChart` wird überschrieben — entfallen |
| **B17** | `listBoxWP_SelectedIndexChanged` auf `listBoxKlimreg` — der Handler heißt jetzt `Waehlen` |
| **B18/B18b** | inline-SQL zweimal — **`ReadByName` und `ReadAllStamm` mit `DbParam`** |
| **B19** | `Max()` ohne Leerprüfung — **Meldung statt Ausnahme** |
| **B20** | `RecordSet` unbenutzt — entfallen |
| **B21** | zwei ReadOnly-Texte — **einer**; der unerreichbare im Controller ist weg, der Rückgabewert sagt es |
| **B22** | `chart2` bleibt stehen — **beide Bilder gehen weg** |
| **B23** | Löschen ohne Kaskade und ohne Rückfrage — **A‑7/A‑8** |
| **B24** | `IncrPBar`, `AxisScrollBarClicked` tot — entfallen |
| **B25** | `Maximum` 9 125 gegen 7 — der Fortschritt zählt sieben Schritte, die tote Zahl ist weg |
| **B26** | Präfix-Dublettenprüfung, stiller Abbruch — **A‑9** |
| **B27** | Feldnamen hartkodiert — **`KLIMA_FELD_LONGITUDE`/`_LATITUDE`** |
| **B28** | drei von vier Abrufen umsonst — **A‑10** |
| **B29** | `SolarCalculator.sonnenwinkel` statisch — **Wert statt Feld**; die Bitgleichheit ist geprüft (der Winkel hängt nicht an der Fassade) |
| **B30** | `AccessRepository` bei SQLite — **wörtlich**, ein Umbenennen ist ein eigener Schritt |
| **B31** | `comboBox_Ort.Text` statt `Listbezeichner` — **A‑11** |
| **B32** | kein `finally` für den Balken — **`try/finally`** |
| **B33** | `KlimaregionStammCtrl` mit WinForms — **in den Kern, `Bezeichner()` statt zweier Füller** |
| **B34** | `ShowDialog()` ohne Besitzer und ohne `using` — **beide Hüllen mit Besitzer und `using`** |
| **B35** | Tippfehler im Kopftext — **berichtigt** |
| **B36** | 0 `MyResource` — **46 `KLIMA_*` in beiden Sprachen** |
| **B37** | Designer gegen Quelltext — **A‑6/E‑2**; der Quelltext gewinnt, die Achse heißt „Monat" |
| **B38** | kein Schließen-Knopf — **A‑14** |
| **B39** | `KatalogItem` neben `KlasseItem` — beide entfallen |
| **B40** | 19er-Liste zweimal — **`KatalogRegistry.Anzeige`** |
| **B41** | Scan im Bedienfaden — **A‑15** |
| **B42** | `DataRow` in der Anzeigeschicht — **`DublettenBefundText`** |
| **B43** | Protokolltexte hartkodiert — **drei `ADM_DUBLETTEN_PROT_*`** |
| **B44** | leerer `catch` → „nicht verwendet" — **`VerwendungZaehlen` mit Grund**, der Dialog hält an |
| **B45** | inline-`UPDATE` in der Maske — **`KatalogBereinigung.SatzUmbenennen`** |
| **B46** | `NamensDialog` nachgebaut — **der Baustein, mit `Pruefung`** |
| **B47** | leeres Protokoll meldet nichts — **Banner** |
| **B48** | Ereignisse an zwei Orten — gegenstandslos, das Markup verdrahtet alles an einer Stelle |
| **B49** | leerer `catch` beim KI-Schalter — **er meldet sich** |
| **B50/B51** | Panelnamen gegen Rubriknamen, Umschaltung über den Index — **A‑16/A‑17** |
| **B52** | DB-Pfad im laufenden Betrieb — **Neustart-Hinweis** |
| **B53** | `txt_DBPath.Text = …DBName` — **A‑12, behoben** |
| **B54** | `szPath` unbenutzt — **Parameter entfallen** |
| **B55** | `SpecialFolder` in der Maske — **`Dienste.Pfade`** im `EinstellungenCtrl` |
| **B56** | drei Orte für dieselben Vorgabewerte — **einer**: die Designer-Werte sind mit der Maske gefallen; `Settings.settings` bleibt führend (`…/api/tmy`, `https://wiki.epos-plan.de`) |
| **B57** | kein schreibender Weg zu `Properties.Settings` — **`EinstellungenCtrl`** |
| **B58** | 0 eigene `MyResource` — **23 `ADM_SET_*`** |
| **B59** | `MajorGrid.Interval = 10` gegen gerechnetes Intervall — gegenstandslos, `Jahresgang` zeichnet Beschriftung und Raster aus DERSELBEN Rechnung |
| **B60** | `RoundedPanel` ohne Nutzer — **gelöscht** |
| **B61** | `Form_KatalogDubletten` im Netz unsichtbar — **Feldkarte von Hand + 23 bunit-Fälle** |
| **B62** | kein Kern-Test für acht Typen — **`KatalogpflegeTests`, 104 Fälle, VOR der ersten Maske** |
| **B63** | `MenuItem_Einstellungen` trägt vier Einstiege — **der Menüpunkt bleibt unverändert stehen**, nur sein Click-Ereignis zeigt auf die Hülle; die Reihenfolge des Administrationsmenüs ist damit unberührt |
| **B64** | Wellenplan nennt falsche Zahlen — gemessen: 6 WFO1000 (nicht 10), 7 Klimadaten-Anker (nicht 6) |

---

## 6 Die WFO1000-Bilanz

| Fundstelle | Art | Stand nach W14c |
|---|---|---|
| `Form_Gesetzesparameter.cs:44` `FrageNeueZeile` | Testdelegat | **weg** (Maske gelöscht) |
| `Form_Gesetzesparameter.cs:47` `FrageLoeschen` | Testdelegat | **weg** |
| `Form_Gesetzesparameter.cs:50` `ZeileBearbeiten` | Testdelegat | **weg** |
| `Form_Gesetzesparameter.cs:168` `GewaehlteKlasse` | benutzt (Sprungbrücke) | **weg** — die Vorwahl ist ein Parameter der Komponente |
| `Form_Gesetzesparameter.cs:394` `Meldung` | Meldungskanal | **weg** — `Warnbanner` |
| `RoundedPanel.cs:10` `CornerRadius` | ohne Nutzer | **weg** (Datei gelöscht) |

**WFO1000 steht bei NULL.** Die Warnzahl der Mappe fällt von **12 auf 6**:
2 CS0108, 2 CS0109, 1 WFO0003, 1 CA2255 — alle sechs sind Altbestand des Kerns bzw. des
`app.manifest`, keine davon neu.

Die `.editorconfig`-Herabstufung (`dotnet_diagnostic.WFO1000.severity = warning`) **bleibt
stehen**: Sie wird gebraucht, sobald wieder eine WinForms-Maske eine serialisierbare
Eigenschaft trägt — sie zu streichen, wäre eine Entscheidung für eine Folgewelle.

---

## 7 Die acht Testanker (R‑W14c‑2)

**Vorher-Lauf: 115 von 124 grün.** Rot waren genau die neun Fälle, die an
`Form_Klimadaten`, ihrem Ordner oder `Form_AdminSettings` hingen.

### 7.1 Prüfmuster statt Umhängen — fünf Anker auf einen Streich

`Form_Klimadaten.{cs,Designer.cs,resx}` ist **verschoben** nach
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Klimadaten/` (Muster W2/W4/W7/W13/W14a).
Sie war die **einzige** Maske des Bestands, deren `btn_Help` im DESIGNER stand statt über
`InfoKnopf.Anbringen` — zwei der fünf Fälle prüfen genau das; ein Umhängen hätte sie
inhaltlich verändert.

| Anker | Datei | vorher | nachher |
|---|---|---|---|
| 1 | `AbschnittTests.Klimadaten_GehtDreiStufenTief` | `Repowurzel.Designer` | `Repowurzel.Pruefmuster` |
| 2 | `AbschnittTests.Klimadaten_HilfeknopfWirdInfoKnopf` | `Abschnitte(…)` | `Musterabschnitte(…)` |
| 3 | `RazorSchreiberTests.OhneHilfeknopfImDesignerKeinInfoKnopf` | `Skelett(…)` | `Musterskelett(…)` |
| 6 | `StapelTests.StapellaufSchreibtKarteUndSkelettJeMaske` | `Designer("Klimadaten")` | `Pruefmuster("Klimadaten")` |
| 7 | `ErreichbarkeitTests.OhneSchalterWirdDieErreichbarkeitNichtGerechnet` | dto. | dto. |

Als Nebenwirkung bleibt der **`Chart`-Typzeuge** erhalten: `DieHaeufigstenTypenSindAbgedeckt`
erwartet ihn seit W14a im Bestand ODER im Prüfmuster — jetzt steht er dort.

### 7.2 Drei Anker wandern auf `MDIMainForm`

| Anker | Datei | vorher | nachher |
|---|---|---|---|
| 4 | Großschreibungs-Zeuge (`StapelTests:31`) | `Form_Klimadaten.Designer.cs` | **`MDIMainForm.Designer.cs`** |
| 5 | Übersichts-Zeuge (`StapelTests:137`) | `Form_Klimadaten` | **`MDIMainForm`** |
| 8 | „ja"-Zeuge (`ErreichbarkeitTests`) | `Form_AdminSettings` | **`MDIMainForm`** |

`MDIMainForm` ist die **Wurzel** des Erreichbarkeitsgraphen (Pfadlänge 1) und fällt als
**allerletzte** Maske überhaupt (Welle 16): Der Anker kann nicht mehr unerreichbar werden und
muss nicht noch einmal umziehen. `Form_ProjektSpeichernUnter` wäre der zweitbeste gewesen —
sie fällt schon mit W15a und trägt seit W14a den Maskenschlüssel-Zeugen; zwei Anker auf einer
Maske sind unnötig.

Die Zeugenkette ist damit vollständig dokumentiert:
`Form_Heizkessel` → `Form_Gebaeude` → `Form_Stromganglinie` (bis W12) →
`Form_AdminSettings` (bis W14c) → **`MDIMainForm`**.

### 7.3 Fünf Schwellen auf den gemessenen Stand (R‑W14c‑9)

| Stelle | vorher | nachher | Bemerkung |
|---|---|---|---|
| `StapelTests` Designer-Dateien | ≥ 24 | **≥ 20** | 18 unter `WindowsFormsApplication1` + 2 generierte des Kerns |
| `StapelTests` Masken | ≥ 21 | **≥ 17** | |
| `StapelTests` lokalisiert | ≥ 11 | **≥ 11** | keine der fünf Masken war lokalisiert |
| `ErreichbarkeitTests` erreichbar | ≥ 21 | **≥ 17** | |
| `ErreichbarkeitTests` Befund-Zeuge | `Form_AdminSettings` | **`MDIMainForm`** | |

> **Abweichung von der Vermessung, benannt:** § 10.4 nannte für die Designer-Dateien „18". Der
> Test zählt aber über die **Repowurzel** (`Stapel.Dateien(Repowurzel.Pfad)`), nicht über
> `WindowsFormsApplication1` — dort kommen `Resource.Designer.cs` und `Settings.Designer.cs`
> des Kerns dazu. Gemessen sind es 20; die Regel „die Schwellen gelten für den gemessenen
> Stand" (R‑W14c‑9) hat Vorrang.

**Nachher-Lauf: 124 von 124 grün**, auch unter `LANG=en_US.UTF-8`.
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md` ist neu erzeugt (neuer Abschnitt
„Stand nach iU9‑W14c", Kopfzahlen und beide Tabellen).

---

## 8 Gate

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, **6** Warnungen | **0 Fehler, 6 Warnungen** |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 227 | **3 430** (450 + 337 + 780 + 1 863) |
| dieselben unter `LANG=en_US.UTF-8` | grün | **grün** |
| `dotnet test Werkzeuge/Formularkarte.Tests` | 124 | **124** |
| dieselben unter `LANG=en_US.UTF-8` | grün | **grün** |
| `python3 Werkzeuge/SqlDialektPruefer/pruefer.py` | 0 Fundstellen | **0** (1 232 SQL-Texte) |
| `dotnet run --project Proben/ChartProben -c Release` | 32 | **32 Bilder, 0 Verstöße** |
| Referenzlauf 1030/1007/1017 | byte-gleich | **PASS, 815 043 Werte; `diff -rq` byte-gleich in allen drei** |
| Stapellauf `--alle` | 17 Masken, 0 unklar | **17 Masken, 18 Designer-Dateien, 11 lokalisiert, 17 von 17 erreichbar, 0/0/0** |
| Wächter iU5 (`Program.*`) | leer | **leer** |
| Wächter Plattform (`System.Windows.Forms` …) | leer | **leer** |
| `git grep` auf die fünf Klassennamen, `ChartManager`, `RoundedPanel`, `Sprungziel.Gesetzesparameter` | 0 außerhalb Protokoll/Prüfmuster | **0** — es bleiben Kommentare, `HilfeKontext`-Schlüssel und `help_mapping`-Adressen (dieselbe Praxis wie seit W12) |

**Nach dem Abschluss-Merge von `origin/ios_migration`** (`809fe41`, der iOS-Statusblock und
die Nachweisliste — nur zwei Dokumente, keine Quelldatei) ist das Gate unverändert:
0 Fehler / 6 Warnungen, 3 430 Tests, 124 Formularkarte-Fälle, 32 ChartProben, Referenzlauf
byte-gleich, Stapellauf 17/18/11 und 17/0/0/0.

**Der Referenzlauf sieht diese Welle nicht** — keine der fünf Masken ist Simulationseingang;
`Tab_Gesetzesparameter` liest die Wirtschaftlichkeit, `Tab_Solar_STAMM` wird beim Projektanlegen
kopiert, und der Lauf rechnet auf den PROJEKTtabellen. Dass er trotzdem byte-gleich ist, ist der
Beweis, dass nichts danebengegriffen hat.

---

## 9 Windows-Abnahme

Am Windows-Gerät zu prüfen — was kein automatisches Netz sieht.

| # | Punkt |
|---|---|
| 1 | **Gesetzeskatalog aus dem Menü**: Bereich umschalten, Zeile wählen — „Ändern"/„Löschen" sind erst DANN frei (A‑13). Eine Zeile aus einem VERGANGENEN Jahr ändern: Die Rückfrage kommt mit drei Knöpfen; „Ja" legt eine neue Zeile an, „Nein" ändert die alte, „Abbrechen" tut nichts. Eine Zeile aus dem laufenden Jahr fragt NICHT. |
| 2 | **Der Katalog aus BEIDEN Razor-Aufrufern als Überlagerung**: (a) Kostendialog → Reiter „Ertrag/Bonus" → „Gesetzesparameter…" — der Katalog erscheint IM Fenster, nach dem Schließen sind die Sätze des Reiterblatts neu gelesen; (b) Wirtschaftlichkeits-Parameter → der Knopf in der Emissionsgruppe — der Katalog steht auf **CO₂-Preis** vorgewählt. **Esc-Ebenen**: Mit offenem Katalog schließt Esc NUR den Katalog, der Wirt bleibt stehen. |
| 3 | **Zeilendialog**: Beim Ändern sind Schlüssel und Klasse gesperrt. Ein leeres Wertfeld speichert NULL, nicht 0 — die Zeile zeigt danach eine LEERE Wertspalte. Ein doppelter Schlüssel mit gleichem Jahr hält den Dialog offen und sagt warum. |
| 4 | **Dublettensuche**: „(alle Kataloge)" prüfen — der Fortschritt läuft, das Fenster bleibt bedienbar (A‑15). Der Baum steht mit Wurzel und Ast OFFEN, die Gruppen ZU. **Per Tastatur**: Tab in den Baum, ↓↑ wandern über Ebenen hinweg, → klappt auf und steigt ab, ← klappt zu und steigt auf, Pos1/Ende springen, Enter wählt — und löst KEINE Aktion aus. Das Dreieck klappt um, ohne zu wählen. Ein Auslieferungssatz trägt das Abzeichen „[Auslieferung]" und lässt sich weder löschen noch umbenennen. |
| 5 | **Umbenennen**: Ein bereits vergebener Name hält die Namensabfrage offen; die eigene Schreibweise („Berlin " → „Berlin") ist erlaubt. „Protokoll speichern…" bei LEEREM Protokoll meldet sich, statt nichts zu tun. |
| 6 | **Klimaimport ECHT gegen PVGIS** (der einzige Netzzugriff): Eine Region über den Ortsnamen importieren, eine zweite über Longitude/Latitude/Bezeichnung. **Vorher/nachher zahlengleich**: `SELECT COUNT(*)` auf `Tab_Solar_STAMM` und `Tab_Klimadaten_STAMM` je neuer Region muss 8 760 bzw. 365 ergeben, und die Werte müssen zu einem Import des alten Standes passen (A‑10 — ein Abruf statt vier ändert kein Byte). Der Fortschritt zählt sieben Schritte, **Abbrechen** hält an, und danach steht KEINE halbe Region in der Datenbank. Ein bereits vergebener Name meldet sich (A‑9). Die zwei Bilder gegen den Bestand halten: dieselbe Kurve, x-Achse „Monat" (A‑6), Sonnenwinkel-Achse ab 0 (A‑3), Legende neu (A‑2), kein Mausrad-Zoom (A‑5). |
| 7 | **Klimaregion löschen**: Die Rückfrage kommt (A‑7), „Nein" ist hervorgehoben (A‑1). Nach „Ja" sind Kopfsatz, 8 760 Stunden- und 365 Tageswerte weg (A‑8) — `SELECT COUNT(*)` prüfen. Ein Auslieferungssatz bleibt stehen und sagt warum. |
| 8 | **Einstellungen**: Alle vier Rubriken durchklicken; „Durchsuchen…" setzt den Pfad. Speichern legt die fünf Ordner an und meldet sich; ein unmöglicher Pfad hält den Dialog offen. „Standardwerte" fragt, füllt und speichert NICHT — **der DB-Name steht danach im Namensfeld, nicht im Pfadfeld** (A‑12). Der Neustart-Hinweis steht in der Rubrik „Datenbank". |
| 9 | **KI-Schalter**: Umlegen, speichern, Maske neu öffnen — der Stand steht. Mit gesetztem `HKLM\Software\wp-plan\KiDeaktiviert` ist der Schalter gesperrt und der Grund steht daneben. |
| 10 | **Das Administrationsmenü in seiner Reihenfolge** (B63): „Einstellungen…", darunter „Gesetzliche Parameter…", darunter „Katalog-Dubletten prüfen…", darunter „Lizenz…". |
| 11 | **Beide Sprachen**: Oberfläche auf Englisch umstellen, alle vier Fenster öffnen — keine deutschen Reste. Besonders bei Klimadaten und Einstellungen: Sie waren VOR dieser Welle vollständig deutsch, samt Fenstertitel. |
| 12 | **125 % Skalierung**: Alle vier Fenster öffnen; die Felder sind vollständig sichtbar, der Baum rollt, kein Text abgeschnitten. |
| 13 | **Die Ortsliste**: Ohne `<BenutzerLokal>\Ortsliste\Ortsnamen.txt` öffnet der Dialog und das Ortsfeld ist leer, aber beschreibbar (E‑7) — der Vorläufer öffnete GAR NICHT. Mit Datei stehen die Vorschläge. |
| 14 | **Schemaschritt 62 auf dem Windows-Gerät** (E‑6, nachgereicht): Der erste Start nach dem Update hebt den Stand von 61 auf 62 und schreibt in `migration_protokoll.txt` je Tabelle „Waisen vorher n, nachher 0"; ein zweiter Start meldet „bereits erledigt". `Proben/ZugriffsschichtProben` (Fälle 13 und 14) laufen dort — sie brauchen die WindowsDesktop-Laufzeit und sind auf Linux nur baubar, nicht lauffähig. Danach: **ein Projektpaket mit Schemastand 61 wird abgewiesen** (Regel B2) — das ist so gewollt und im Nachtrag zu E‑6 benannt. |

---

## 10 Anwenderfragen

| Nr | Frage | Stand |
|---|---|---|
| **E‑1** | **Der Gesetzeskatalog erscheint künftig als Bereich IM Dialog** statt als eigenes Fenster — aus dem Kostendialog und aus dem Wirtschaftlichkeits-Parameterdialog. Ist das recht? | umgesetzt (Risiko R2 lässt keine zweite Wahl); der Weg über das Menü bleibt ein eigenes Fenster |
| **E‑2** | **Die x-Achse der beiden Klimadiagramme heißt künftig „Monat"** — heute steht „Jahresstunden" über einer Monatsachse | umgesetzt (A‑6) |
| **E‑3** | Die Komponente heißt **`KlimaregionDialog`**, nicht `KlimadatenDialog`. Betrifft auch den Menüpunkt: „Klimadaten" oder „Klimaregionen"? | **entschieden** (Anwender, 04.09.2026): „Klimaregion ist eigentlich für Deutschland gedacht, Klimadaten für den Download weltweit mit TMY-Daten." Der **Menütext bleibt „Klimadaten"**, die Komponente heißt **wieder `KlimadatenDialog`** — siehe den Nachtrag unten |
| **E‑4** | **Die y-Achse des Sonnenwinkels** — am kleinsten Wert oder bei 0? | **bei 0**, über `MinimumNull` (W14c.0j); das Bild bleibt wie im Bestand |
| **E‑5** | **Auf iOS gibt es die fünf „Durchsuchen…"-Knöpfe der Einstellungen nicht** (`OrdnerWaehlen` liefert dort immer `""`). Reicht das — oder braucht iOS gar keine Pfadeinstellungen? | **entschieden** (Anwender, 04.09.2026: „Empfehlung"): **Ohne `OrdnerWaehler` sind die fünf Pfade fest** — nur lesende Felder mit dem Wert aus `EinstellungenCtrl` (auf iOS die Sandbox-Pfade), kein Knopf, darüber der Hinweis `ADM_SET_HINT_PFADE_FEST` („Die Ordner sind auf dieser Plattform fest vorgegeben." / „Folders are fixed on this platform."); „Speichern" schreibt die übrigen Werte unverändert und gibt die fünf Pfade so zurück, wie der Kern sie vorgegeben hat — auch nach „Standardwerte". Mit Wähler (Windows) bleibt alles wie bisher. Fünf neue bunit-Fälle |
| **E‑6** | **Löschen einer Klimaregion räumt künftig die 8 760 + 365 Datenzeilen ab.** Sollen vorhandene Altwaisen mit einer einmaligen Bereinigung mitgehen? | **entschieden** (Anwender, 04.09.2026): „Altbereinigung ausführen." Umgesetzt als **Schemaschritt 62** (`SCHRITT_62_KLIMAWAISEN`, der ERSTE Eintrag in `SCHRITTE_SQLITE`) — zwei `DELETE` aus `KlimaWaisenBereinigung` im Kern; auf `Kenndaten_Test.sqlite` ein **No-op** (0 Waisen). Siehe den Nachtrag unten |
| **E‑7** | **`Ortsnamen.txt` fehlt in Auslieferung und Repo.** Was soll die Ortsauswahl anbieten? | **(c) umgesetzt**: vorhanden → Vorschlagsliste, fehlt → leere Liste, nie ein Absturz; das Feld erlaubt immer freie Eingabe. Ob die Datei künftig ausgeliefert oder aus `Tab_Klimaregion_STAMM` gefüllt wird — offen |
| **E‑8** | **WebView2-Bezug**: Mit dieser Welle sind die letzten vier Admin-Masken Blazor. Ohne die WebView2-Laufzeit bleiben Gesetzeskatalog, Dublettensuche, Einstellungen und Klimadaten LEER — die Anwendung startet, aber die Verwaltung ist unbedienbar. Das Setup installiert die Laufzeit nach (`Setup/EPOS-Plan.iss`, `WebView2Vorhanden`); auf einem Rechner ohne Internet muss sie vorher da sein | Hinweis, keine Änderung |

### Nachtrag zu E‑3: die zwei Begriffe (04.09.2026)

Der Anwender trennt sie fachlich, nicht technisch:

| Begriff | Was gemeint ist | Was den Namen trägt |
|---|---|---|
| **Klimaregion** | die **deutschen** Klimaregionen — die Klimazonenkarte und der Projektbezug über `ID_Klimaregion` | `KlimazonenkarteDialog` (W10a), `KlimaregionStammCtrl`, `KlimaregionCtrl`, `Tab_Klimaregion_STAMM` — **alle unverändert** |
| **Klimadaten** | der **weltweite** Download von TMY-Daten (PVGIS) in den Stammkatalog | der Menüpunkt „Klimadaten", `KlimadatenDialog`, `KlimadatenHuelle`, `KlimadatenDialogTests` |

Umbenannt sind deshalb genau drei Dateien samt ihren Klassen —
`EPOS.UI/Dialoge/Klimadaten/KlimadatenDialog.razor` (Ordner und `@namespace`
`EPOS.UI.Dialoge.Klimadaten` bleiben), `WindowsFormsApplication1/Views/Admin/KlimadatenHuelle.cs`
und `EPOS.UI.Tests/Dialoge/KlimadatenDialogTests.cs` — dazu der Aufruf in `MDIMainForm`
und die Nennungen in `EPOS.UI/CLAUDE.md` und `WindowsFormsApplication1/CLAUDE.md`.
**Nicht angefasst:** `KlimaregionStammCtrl`, `KlimaImportAblauf`, `Tab_Klimaregion_STAMM`,
`KlimazonenkarteDialog`, der `HilfeKontext`-Schlüssel `Form_Klimadaten.btn_Help` (er trug nie
den Klassennamen) und die CSS-Klassen `epos-klimaregion*` (sie haben keine Regel im
Stilblatt und dienen nur als Prüfanker).
Die Abschnitte § 1, § 3.5 und § 12 dieses Protokolls nennen die Komponente weiter
`KlimaregionDialog` — sie sind der **datierte Bericht des Portstands**, nicht der aktuelle
Namensstand.

### Die Zählung zu E‑6 (verlangt in § 11.6)

```sql
SELECT COUNT(*) FROM Tab_Solar_STAMM
 WHERE ID_Klimaregion NOT IN (SELECT ID_Klimaregion FROM Tab_Klimaregion_STAMM);   -- 0
SELECT COUNT(*) FROM Tab_Klimadaten_STAMM
 WHERE ID_Klimaregion NOT IN (SELECT ID_Klimaregion FROM Tab_Klimaregion_STAMM);   -- 0
```

Auf `Referenzlaeufe/Kenndaten_Test.sqlite`: **32 Regionen, 280 320 Stundenwerte
(= 32 × 8 760), 11 680 Tageswerte (= 32 × 365), NULL Waisen.** Die Zahlen gehen exakt auf —
der Auslieferungsstand trägt keinen Datenblock ohne Kopfsatz. Der Fall steht als
`DerBestandFuehrtKeineVerwaistenKlimadaten` in `KatalogpflegeTests` und würde rot, sobald sich
das ändert.

### Nachtrag zu E‑6: die Altbereinigung als Schemaschritt 62 (04.09.2026)

Der Anwender hat sie angeordnet („Altbereinigung ausführen"), obwohl die Zählung oben auf dem
Auslieferungsstand null ergibt — die Anwenderdatenbanken sind der Grund.

| Stelle | Was dort steht |
|---|---|
| `EPOS.Kern/Allgemein/Katalog/KlimaWaisenBereinigung.cs` | **die zwei Anweisungen**, `ZaehlungZu`/`LoeschungZu` je Datenblocktabelle — die EINE Wahrheit für Schritt und Nachweis |
| `SchemaMigration.SCHRITT_62_KLIMAWAISEN` + `Schritt_62_KlimaWaisen` | der Schritt; **erster Eintrag in `SCHRITTE_SQLITE`** — angelegt in der Reihenfolge des E6‑Vorfalls: erst Konstante, Methode und Eintrag, DANN das Ziel |
| `SchemaMigration.ZIEL_VERSION` | **61 → 62** |
| `SchemaMigration.FREEZE_VERSION` | **neu, 61** — siehe unten |
| `SqliteDml` / `SqliteZahl` | zwei Helfer neben `SqliteDdl`, gleiche Bauart, über `DataRepository` statt über `Lauf.Conn`; `SqliteDdl` meldet „angelegt", ein `DELETE` meldet „ausgefuehrt" |

**`FREEZE_VERSION` musste dazukommen, und das ist der eigentliche Eingriff.** Bis hierher waren
Freeze-Stand und Zielstand DIESELBE Zahl, und beide Zweige lasen `ZIEL_VERSION`:
`SchritteAbarbeitenSqlite` wies alles unterhalb davon als „nicht auf Freeze-Stand" ab, und der
Access-Zweig prüfte am Ende `StandNachher >= ZIEL_VERSION`. Ein blosses Hochsetzen auf 62 hätte
deshalb **jede frisch migrierte Datei (Stand 61) abgewiesen, statt Schritt 62 auf ihr zu fahren**,
und `HebeAltbestand` hätte einen Fehlschlag gemeldet, obwohl der eingefrorene Zweig alles getan
hat, was er kann. Seither: `FREEZE_VERSION` = 61 (was der `EposSqliteMigrator` liefert, wird nie
wieder angehoben), `ZIEL_VERSION` = 62 (was das Programm erwartet).

**Befund am Rande, benannt:** Beide Datenblocktabellen tragen seit der SQLite-Umstellung einen
Fremdschlüssel auf `Tab_Klimaregion_STAMM` mit `ON DELETE CASCADE`, und `DataRepository` setzt je
Verbindung `PRAGMA foreign_keys = ON`. **Über die Zugriffsschicht kann seither gar keine Waise
mehr entstehen** — die Kaskade räumt beim Löschen des Kopfsatzes selbst ab. Der Schritt ist damit
für Bestände da, die ihre Waisen aus der Access-Zeit mitbringen oder deren Datei einmal ohne
Fremdschlüssel geschrieben wurde; er ist ein Netz, kein Alltagsweg. Der Kern-Test legt seine
Waisen deshalb an der Zugriffsschicht vorbei an (`Foreign Keys=False` auf einer eigenen
Verbindung).

**Nachweis** (`EPOS.Kern.Tests/KatalogpflegeTests.cs`, 104 → 106 Fälle, eigene Arbeitskopie):

| Fall | Was er festhält |
|---|---|
| `DieAltbereinigungRaeumtWaisenAbUndLaesstDenBestandStehen` | je Tabelle eine künstliche Waise (`ID_Klimaregion = 999999`) → nach den zwei Anweisungen 0 Waisen, Bestand unverändert 32 / 280 320 / 11 680; **zweiter Lauf ändert nichts** |
| `DieAltbereinigungIstAufDemAuslieferungsstandEinNoOp` | die zwei Anweisungen auf dem unberührten Stand — jede Zahl steht danach wie vorher |

**`Proben/ZugriffsschichtProben` ist nachgezogen, aber hier nicht lauffähig:** Das Projekt ist
`net10.0-windows` und braucht die WindowsDesktop-Laufzeit; es **baut** auf Linux, es **läuft**
dort nicht. Fall 13 erwartet jetzt Schritt 62, die Waisenzeilen „vorher 0, nachher 0" und
Schemastand 62 (statt „Stand bleibt 61"), dazu einen zweiten Lauf mit „bereits erledigt"; der
Wegwerf-Schritt des Test-Seams in Fall 14 ist von 62 auf **63** gerückt, weil 62 jetzt wirklich
existiert. **Beides ist Windows-Abnahmepunkt 14** (siehe § 9).

**Paketfolge, ausdrücklich benannt:** Wie bei jedem Schemaschritt hebt sich der Stand, und
`ProjektExportImportCtrl` (Regel B2, `:329`) weist ein Projektpaket ab, dessen `schemaVersion`
nicht dem eigenen Zielstand entspricht. **Pakete mit Schemastand 61 werden nach dem Update also
abgewiesen** — beide Rechner sind auf denselben Programmstand zu bringen und das Projekt neu zu
exportieren. Das ist die stehende Regel jedes Schemaschritts, keine Besonderheit dieses einen.

---

## 11 Grenzen

* **Der Referenzlauf sieht diese Welle nicht.** Er rechnet einen bestehenden Projektstand nach;
  diese Masken pflegen Stammdaten. Dafür sind die 104 eingefrorenen Fälle da — und die
  A‑Zeilen stehen als Windows-Abnahmepunkte in § 9.
* **Die TMY-Probe ist synthetisch.** Sie hat PVGIS-Form und läuft über denselben Leser, ist aber
  kein Mitschnitt eines echten Abrufs: Ein Formatwechsel bei PVGIS fiele hier nicht auf. Der
  Abnahmepunkt 6 fährt den Import deshalb einmal echt.
* **`Sprungbruecke.cs` bleibt bis W16** (R‑W14c‑11). Sie führt danach EINEN Zweig
  (`SpeicherOptimierung`) und `Sprungziel.cs` EINE Konstante. **Das ist ein Entscheid, kein
  Rest:** `Form_SpeicherOptimierung` bleibt nach iF22 bewusst WinForms — sie ist der einzige
  Ort des Programms, an dem ScottPlot läuft (Heatmap und Schnittkurve der Rastersuche). Wer die
  zwei Dateien jetzt „aufräumt", bricht sie.
* **`Views/Admin` führt noch eine Maske**: `Form_LizenzVerwaltung` (Welle 15c).
* **Die `.editorconfig`-Herabstufung für WFO1000 bleibt**, obwohl die Zahl bei null steht —
  streichen kann man sie, wenn keine WinForms-Maske mehr da ist.

---

## 12 Was die Welle abgeräumt hat

| Datei | Zeilen | Grund |
|---|---|---|
| `Views/Admin/Form_Gesetzesparameter.*` | 403 + 207 + resx | `GesetzeskatalogDialog` |
| `Views/Admin/Form_GesetzparameterZeile.*` | 258 + 224 | `GesetzeskatalogZeileDialog` (Überlagerung) |
| `Views/Admin/Form_KatalogDubletten.*` | 800 + resx | `KatalogDublettenDialog` |
| `Views/Admin/Form_AdminSettings.*` | 320 + 491 + resx | `EinstellungenDialog` |
| `Views/Klimadaten/Form_Klimadaten.*` | 417 + 503 + resx | `KlimaregionDialog` — **verschoben** nach `Pruefmuster/Klimadaten/` |
| `Allgemein/GrafikTools/ChartManager.cs` | 560 | letzter Nutzer weg — **die MS-Chart-Bindung endet** |
| `Allgemein/GrafikTools/RoundedPanel.cs` | ~70 | ohne Nutzer (B60), sechste WFO1000-Fundstelle |
| `Controller/KlimaregionStammCtrl.cs` | 352 | **in den Kern gezogen** (B33) |
| zwei `Sprungziel`-Konstanten und -Zweige | ~22 | die Ziele sind selbst Blazor |
| `GESETZ_BTN_UEBERNEHMEN` | 1 Schlüssel × 2 Sprachen | ohne Nutzer (B4) |
| `Views/Klimadaten/` | — | der Ordner ist leer und weg |
