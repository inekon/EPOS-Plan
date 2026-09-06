# iU9 Welle 14a — Erzeuger-Admin: Portprotokoll

**Sieben WinForms-Masken → drei Razor-Komponenten und sieben Hüllen**, jede WinForms-Fassung
im selben Commit gelöscht (Regel M1). Stand 04.09.2026, Basis `01c9933` (nach W13).

| Maske | `.cs` | Designer | MessageBox | Nachfolge |
|---|---|---|---|---|
| `Form_Heizkessel_Admin` | 365 | 306 | 3 (+5) | `KatalogBrowserDialog`, Ausprägung Heizkessel |
| `Form_BHKWAdmin` | 465 | 517 | 5 (+5) | dieselbe, Ausprägung BHKW |
| `Form_SolarKollektorenAdmin` | 188 | 305 | 3 | dieselbe, Ausprägung Solarkollektoren |
| `Form_PufferSp_Admin` | 213 | 280 | 2 | dieselbe, Ausprägung Pufferspeicher (+ `NurLesen`) |
| `Form_PufferSp_Bearbeiten` | 354 | 257 | 11 (+1) | `PufferSpKatalogDialog` |
| `Form_AdminStromspeicher` | 505 | 274 | 9 (+11) | `ModulKatalogDialog`, Ausprägung Stromspeicher |
| `Form_AdminPV` | 297 | 430 | 6 (+10) | dieselbe, Ausprägung Photovoltaik |
| **Summe** | **2 387** | **2 369** | **39 (+32)** | **3 Komponenten** |

**Der Befund der Vermessung hat sich bestätigt:** Vier der sieben Masken sind Behälter um
Editoren, die seit W6/W7 schon Razor sind. Nur der Pufferspeicher hatte noch einen
WinForms-Editor — den fehlenden vierten der Editorfamilie.

---

## 1 Commits

| Commit | Schritt | Inhalt |
|---|---|---|
| `5fdbb4b` | **W14a.0i** | `EPOS.Kern.Tests/KatalogVerwaltungTests.cs` — der Nachweis VOR der ersten Maske |
| `5c4c83c` | **W14a.0b** | Die Heizkessel-Brennstoffkette berichtigt (Befund W14‑B2), mit Vorher/Nachher-Zählung |
| `ea15d01` | **W14a.0a** | `KatalogBrowserProfil` — die vier Ausprägungen als Daten |
| `1975707` | **W14a.0c/0d** | Kataloglisten, Detailblöcke, Speicherwege, Speichertyp-Abbildung im Kern |
| `ca89f5f` | **W14a.0e/0f** | `SpeichernAus` für drei Kataloge, `ModulKatalogProfil`, zwei Vorgabewerte |
| `ef3d7e2` | **W14a.0g** | Textkatalog: 97 neue Schlüssel in beiden Sprachen |
| `479336c` | **W14a.0h** | Der PV-Menüpunkt geht über `MenueCtrl` wie die zehn anderen |
| `c278a62` | **W14a.2** | `PufferSpKatalogDialog` — der fehlende vierte Katalogeditor |
| `0afe8ad` | **W14a.1** | `KatalogBrowserDialog` — vier Admin-Masken werden EINE Komponente |
| `1b2e2f4` | **W14a.3/4/5** | `ModulKatalogDialog`, die letzten Sprungziele, `SpeichernLeiste`, `KiAufrufKnopf` |
| (dieser) | **W14a.6/7/8** | Ressourcen, Formularkarte, Protokoll und drei `CLAUDE.md` |

---

## 2 Der Nachweis entsteht zuerst (R‑W14‑1)

**Befund W14‑B77:** Kein Referenzlauf, keine ChartProbe und kein Kern-Test berührte die sieben
Masken fachlich. Die drei Testanker hingen an ihrer ERREICHBARKEIT und an der SCHREIBWEISE
eines Dateinamens, nicht an ihrem Verhalten.

`EPOS.Kern.Tests/KatalogVerwaltungTests.cs` steht deshalb im **ersten** Commit der Welle, vor
jeder portierten Zeile — 50 Fälle über eine Arbeitskopie von `Kenndaten_Test.sqlite`, nur
lesend und mit EINER Kopie je Klasse (Regel seit W11a):

| Gruppe | Was eingefroren ist |
|---|---|
| Heizkessel | sechs Leistungsstufen (62/53/9/0/0/0), dreizehn Brennstoffgruppen, Sortierung |
| BHKW | neun Leistungsstufen (79/14/9/8/10/18/6/6/8), dreizehn Brennstoffgruppen, die vier Werte der Zweitspalte |
| Pufferspeicher | sechs Volumenstufen (13/0/1/5/5/2), die drei Hersteller |
| Photovoltaik | die vier Hersteller |
| `Exists` | fünf Kataloge, bejahend und verneinend |
| Satzzahlen | 63 / 79 / 7 / 13 / 6 / 5 |
| Profile | Feldzahlen 8/8/8/6 und 13/13, Speicherweg, Filterart, `leerErlaubt`, Vorbelegungen |
| Detailblöcke | jedes Profilfeld beantwortet, Formatierung je Katalog |
| Speichertyp | DB-Werte, die drei eingefrorenen englischen Altwerte, beide Wege |
| Schreibwege | die Ablehnungsgründe — alle **vor** dem Schreiben, die Arbeitskopie bleibt unberührt |
| Textkatalog | jeder Schlüssel beider Profile in de-DE UND en-US |

---

## 3 Die Vorher/Nachher-Zählung der Brennstoffkette (W14a.0b, R‑W14‑7)

`HeizkesselStammCtrl.Filtern` trug die Kette des mit W6.3 gelöschten `Form_Heizkessel`: Sie
kannte `"Sonstige" → Brennstoff=23`, während `Tab_BrennstoffKategorien` „Sonstige
Energieträger" führt. Der Eintrag traf also **nie**, und die drei wirklich vorhandenen Gruppen
standen gar nicht in der Kette — wer sie wählte, bekam KEINE Einengung und sah den ganzen
Katalog. Die richtige Kette stand in `Form_Heizkessel_Admin.SetFilter` (:118‑120).

Gemessen auf `Referenzlaeufe/Kenndaten_Test.sqlite`, Leistungsstufe „Alle", 63 Katalogsätze
(62 mit `Ptherm`):

| Brennstoffgruppe | vorher | nachher |
|---|---:|---:|
| Alle | 62 | 62 |
| Gas | 52 | 52 |
| Öl | 3 | 3 |
| Koks | 0 | 0 |
| Kohle | 0 | 0 |
| Holz | 0 | 0 |
| Pellets | 0 | 0 |
| Strom | 8 | 8 |
| Rapsöl | 0 | 0 |
| Tierische Fette | 0 | 0 |
| **Fernwärme** | **62** | **0** |
| **Sonstige Energieträger** | **62** | **0** |
| **Wasserstoff** | **62** | **0** |

Zehn Gruppen unverändert, drei berichtigt. Der Testkatalog führt keinen Kessel mit Brennstoff
23, 24 oder 25 — die Null ist die richtige Antwort und kein leerer Filter.

**Die Änderung betrifft ZWEI Dialoge**: die neue Katalogverwaltung und den bereits portierten
Projektdialog `HeizkesselDialog` (W6.3), der dieselbe Methode benutzt. Beide gehören in die
Windows-Abnahme (§ 9). Der Kern-Kommentar „W6‑O‑1" ist damit geschlossen.

---

## 4 Feldkartenabgleich je Ausprägung (R‑W14‑4)

Die sieben Feldkarten sind vor dem Port gezogen worden
(`dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`). Der Abgleich läuft je
AUSPRÄGUNG, nicht je Komponente — Muster W8/W13.

### 4.1 `KatalogBrowserDialog`, Ausprägung **Heizkessel** (Karte 19 Zeilen)

| Kartenzeile | Nachfolge | ☑ |
|---|---|---|
| `listBox_Kessel_DB` „Kessel aus Datenbank" | `Raster` + `Zeilenwahl`, einwertig | ☑ |
| `comboBox_Brennstoffart` „Filtern nach Brennstoffart:" | `Auswahlfeld`, Index = Steuerwert | ☑ |
| `comboBox_Leistung` „Filtern nach Leistung:" | `Auswahlfeld`, sechs Stufen aus `HZK_STUFE_*` | ☑ |
| `textBox_Kesselname` „Name:" | `Textfeld`, nur lesend (Schlüssel des UPDATE) | ☑ |
| `textBox_Kesselbeschreibung` „Beschreibung:" | `Textfeld` mehrzeilig, editierbar | ☑ |
| `textBox_Brennstoff` „Brennstoff:" | `Textfeld`, nur lesend (Nachschlag) | ☑ |
| `textBox_Kesselleistung` „Leistung:" + `label13` „kW" | `Zahlenfeld`, `F2`, editierbar | ☑ |
| `textBox_Investitionskosten` „Investitionskosten:" + `label4` „€" | `Zahlenfeld`, `F2`, editierbar | ☑ |
| `checkBox_Brennwert` „Brennwertkessel" | `Schalter`, editierbar | ☑ |
| `textBox_Vorlauf` „Vorlauf:" + `label47` „°C" | `Ganzzahlfeld`, editierbar | ☑ |
| `textBox_Ruecklauf` „Rücklauf:" + `label46` „°C" | `Ganzzahlfeld`, editierbar | ☑ |
| `btn_Bearbeiten` „Kessel Bearbeiten…" | Knopf „Bearbeiten…" (gemeinsamer Text, A‑5) | ☑ |
| `btn_Neu` „Neuer Kessel…" | Knopf „Neu…" (A‑5) | ☑ |
| `btn_Loeschen` „Kessel Löschen" | Knopf „Löschen" (A‑5) | ☑ |
| `btn_OK` „OK" | Knopf „OK" — schreibt offene Änderungen und schließt (A‑1) | ☑ |
| `SpeichernLeiste` (Laufzeit) | Knopf „Speichern", ohne Schließen | ☑ |
| `btn_Abbrechen_Click` (toter Handler, W14‑B5) | entfällt | ☑ |

### 4.2 Ausprägung **BHKW** (Karte 20 Zeilen)

| Kartenzeile | Nachfolge | ☑ |
|---|---|---|
| `dataGridView1` „Module in Datenbank:" (2 Spalten) | `Raster`, zweite Spalte mit dem vierzeiligen Text | ☑ |
| Zebrastreifen, `DividerHeight`, `WrapMode` | CSS | ☑ |
| ReadOnly-Sätze grau (`:202`) | Klasse `epos-katalogbrowser-geschuetzt` | ☑ |
| `comboBox_Brennstoff` „Filtern nach Brennstoffart" | `Auswahlfeld` | ☑ |
| `comboBox_Leistung` „Filtern nach Leistung" | `Auswahlfeld`, NEUN Stufen über den Index (A‑3) | ☑ |
| `textBox_Name` „Modul-Name:" | `Textfeld`, nur lesend | ☑ |
| `textBox_Firma` „Hersteller:" | `Textfeld`, editierbar | ☑ |
| `textBox_Beschreibung` „Beschreibung:" | `Textfeld` mehrzeilig, nur lesend (Designer) | ☑ |
| `textBox_Leistung_th` „thermische Leistung:" + „kWth" | `Zahlenfeld`, editierbar | ☑ |
| `textBox_Leistung_el` „elektrische Leistung:" + „kWel" | `Zahlenfeld`, editierbar | ☑ |
| `textBox_M_GrenzL` „Untere Grenzleistung…" + „%" | `Zahlenfeld`, editierbar | ☑ |
| `textBox_Vorlauf` / `textBox_Ruecklauf` + „°C" | `Ganzzahlfeld`, editierbar | ☑ |
| `groupBox2` „Info markiertes BHKW" | `Gruppenkopf` | ☑ |
| vier Knöpfe | wie Heizkessel | ☑ |
| `ADM_SCHUTZ_FRAGE` beim Überschreiben (`:418`) | `Rueckfrage` | ☑ |
| `dataGridView1_Click` (tot, W14‑B9) | entfällt | ☑ |
| 26 deutsche Literale (W14‑B11) | 26 Ressourcenschlüssel de + en | ☑ |

### 4.3 Ausprägung **Solarkollektoren** (Karte 19 Zeilen)

| Kartenzeile | Nachfolge | ☑ |
|---|---|---|
| `dataGridView1` „Auswahl in DB:" (2 Spalten) | `Raster`, dreizeiliger Zweitspaltentext | ☑ |
| `label_Type` „Eingabe der Solarkollektoren" | `Gruppenkopf` des Detailblocks | ☑ |
| `textBox_Name` „Name:" | `Textfeld`, nur lesend | ☑ |
| `textBox_Kollektortype` „Kollektor:" | `Textfeld`, nur lesend | ☑ |
| `textBox_Firma` „Hersteller:" | `Textfeld`, nur lesend | ☑ |
| `textBox_Beschreibung` „Beschreibung:" | `Textfeld` mehrzeilig, nur lesend | ☑ |
| `textBox_Kollektor_A` „Kollektorfläche:" + „m²" | `Textfeld`, **bleibt leer** (W14a‑B78, E‑11) | ☑ |
| `textBox_Modul_A` „Aperturfläche:" + „m²" | `Textfeld`, zeigt die Aperturfläche (W14‑B15, E‑2) | ☑ |
| `textBox_Vorlauf` / `textBox_Ruecklauf` + „°C" | `Textfeld`, nur lesend | ☑ |
| drei Knöpfe „…in DB…" | die drei gemeinsamen Knopftexte (A‑5) | ☑ |
| `btn_OK` „OK" / `btn_Abbrechen` „Abbrechen" | ein „OK"; der zweite Knopf entfällt | ☑ |
| KEINE Filterleiste | keine | ☑ |
| vier tote Stellen (W14‑B17) | entfallen | ☑ |

### 4.4 Ausprägung **Pufferspeicher** (Karte 16 Zeilen) und `NurLesen`

| Kartenzeile | Nachfolge | ☑ |
|---|---|---|
| `listBox_PufferSp_DB` „Pufferspeicher aus Datenbank" | `Raster` + `Zeilenwahl` | ☑ |
| `comboBox_Hersteller` „Filtern nach Hersteller:" | `Auswahlfeld`, „Alle" voran | ☑ |
| `comboBox_Volumen` „Filtern nach Volumen:" | `Auswahlfeld`, `VOLUMEN_SQL` bitgleich | ☑ |
| `textBox_Name` „Name:" | `Textfeld`, nur lesend | ☑ |
| `textBox_Hersteller` „Hersteller:" | `Textfeld`, nur lesend | ☑ |
| `textBox_Typ` „Speichertyp:" | `Textfeld`, nur lesend | ☑ |
| `textBox_Versluste` „Bereitschaftsverluste:" + „kWh/d" | `Textfeld`, nur lesend, ROH | ☑ |
| `textBox_Volumen` „Gesamtvolumen:" + „l" | `Textfeld`, nur lesend, ROH | ☑ |
| `textBox_Investitionskosten` „Investitionskosten:" + „€" | `Textfeld`, nur lesend, ROH | ☑ |
| drei Knöpfe + „OK" | wie oben | ☑ |
| **`m_bReadOnly`** (`:39‑44`) | **Parameter `NurLesen`** — drei Knöpfe gesperrt, Liste und Detail sichtbar | ☑ |
| `label1_Click` (leer verdrahtet, W14‑B29) | entfällt | ☑ |

### 4.5 `PufferSpKatalogDialog` (Karte 13 Zeilen)

| Kartenzeile | Nachfolge | ☑ |
|---|---|---|
| `groupBox1` (ohne Titel) | `Gruppenkopf` „Bezeichnung" | ☑ |
| `textBox_Name` „Name:" / en „Boiler name:" | `Textfeld`, nur lesend im Modus Bearbeiten; en **„Storage name:"** (W14‑B24) | ☑ |
| `textBox_Hersteller` „Hersteller:" | `Textfeld` | ☑ |
| `comboBox_Speichertyp` „Speichertyp:" (3 Einträge) | `Auswahlfeld`, INDEX als Steuerwert (L0‑1) | ☑ |
| `groupBox2` „Technische Daten" | `Gruppenkopf` | ☑ |
| `textBox_Verluste` „Betriebsbereitschaftsverluste:" + „kWh/d" | `Zahlenfeld` | ☑ |
| `textBox_Volumen` „Gesamtvolumen:" + „l" | `Ganzzahlfeld`, Feldname aus dem Katalog (W14‑B20) | ☑ |
| `groupBox3` „Eingabedaten zur Berechnung der Kosten" | `Gruppenkopf` | ☑ |
| `textBox_Investitionskosten` „Investitionskosten:" + „€" | `Zahlenfeld` | ☑ |
| `btn_Ueberschreiben` / `btn_Speichern_Unter` / `btn_Speichern` | drei Knöpfe, Enabled je Modus bitgleich | ☑ |
| `btn_Abbrechen` | Knopf „Abbrechen" | ☑ |
| `NamensDialogHuelle` (zweites Fenster) | `Ueberlagerung` mit `NamensDialog` | ☑ |
| `KiAufrufKnopf.Anbringen` (`:63`) | **entfällt** (A‑2 aus W6, E‑10) | ☑ |

### 4.6 `ModulKatalogDialog`, Ausprägung **Stromspeicher** (Karte 20 Zeilen + **6 von Hand**)

Die sechs AP3-Felder baute der Vorläufer zur LAUFZEIT (`:367‑461`); die Feldkarte sieht sie
nicht (R‑W14‑10). Sie sind hier von Hand aus dem Quelltext nachgetragen.

| Kartenzeile | Nachfolge | ☑ |
|---|---|---|
| `listBox_Stromspeicher` | `Raster` + `Zeilenwahl` | ☑ |
| `textBox_Bezeichner` „Bezeichner" | `Textfeld`, gesperrt | ☑ |
| `textBox_Typ` + `label3` „Typ" | `Textfeld`, Pflicht | ☑ |
| `textBox_Energie` + `label2`/`label8` | `Zahlenfeld`, Einheit **kWh** (berichtigt, A‑9) | ☑ |
| `textBox_Leistung` + „kW" | `Zahlenfeld`, Pflicht | ☑ |
| `textBox_Ladezustand` + „%" | `Zahlenfeld`, Pflicht | ☑ |
| `textBox_Degradation` + „%" | `Zahlenfeld`, Pflicht | ☑ |
| `textBox_Modulkosten` + `label11` | `Zahlenfeld`, Einheit **€/kWh** (berichtigt, A‑9) | ☑ |
| **AP3 (Laufzeit) `SP_GRUPPE_GERAETETECHNIK`** | zweiter `Gruppenkopf` | ☑ |
| **AP3 `textBox_WirkungsgradRT`** + „-" | `Zahlenfeld`, leer erlaubt | ☑ |
| **AP3 `textBox_Zyklen`** + „-" | `Ganzzahlfeld`, leer erlaubt | ☑ |
| **AP3 `textBox_Verschleisskosten`** + `SP_EINHEIT_ZYKLUSKOSTEN` | `Zahlenfeld`, leer erlaubt | ☑ |
| **AP3 `textBox_Leistungskosten`** + „€/kW" | `Zahlenfeld`, leer erlaubt | ☑ |
| **AP3 `textBox_InvestitionFix`** + „€" | `Zahlenfeld`, leer erlaubt | ☑ |
| **AP3 `textBox_Standby`** + „W" | `Zahlenfeld`, leer erlaubt | ☑ |
| `btn_Speichern` / `btn_Neu` / `btn_Loeschen` / `btn_OK` „Beenden" | vier Knöpfe | ☑ |
| `btn_Beenden` „OK" (zweiter Schließknopf) | entfällt — ein „Beenden" | ☑ |
| `textBox_Typ_Validating` (modal, W14‑B45) | Bannerprüfung beim Speichern (A‑10) | ☑ |
| `ClientSize`-Vergrößerung auf 1 036 px (W14‑B43) | entfällt — CSS | ☑ |
| `GetTextBox_Energie`, `btn_Abbruch_Click` (tot, W14‑B46) | entfallen | ☑ |

### 4.7 Ausprägung **Photovoltaik** (Karte 29 Zeilen)

| Kartenzeile | Nachfolge | ☑ |
|---|---|---|
| `listBox_PV` | `Raster` + `Zeilenwahl` | ☑ |
| `textBox_Bezeichner` „Bezeichner:" | `Textfeld`, gesperrt | ☑ |
| `textBox_Firma` „Hersteller:" | `Textfeld` | ☑ |
| `textBox_Beschreibung` „Beschreibung:" | `Textfeld` mehrzeilig | ☑ |
| `textBox_Leistung` „Nennleistung (Pmax):" + „W" | `Zahlenfeld`, **Pflicht** (bitgleich) | ☑ |
| `textBox_Wirkungsgrad` „Wirkungsgrad:" + „%" | `Zahlenfeld`, leer erlaubt | ☑ |
| `textBox_UMpp` / `textBox_ULeerlauf` + „V" | `Zahlenfeld`, leer erlaubt | ☑ |
| `textBox_IMpp` / `textBox_IKurzschluss` + „A" | `Zahlenfeld`, leer erlaubt | ☑ |
| `textBox_TempKoeff` + „%/K" | `Zahlenfeld`, leer erlaubt | ☑ |
| `textBox_Laenge` / `textBox_Breite` + „m" | `Zahlenfeld`, leer erlaubt | ☑ |
| `textBox_Modulkosten` „Modulkosten:" + „€" | `Zahlenfeld`, leer erlaubt | ☑ |
| `btn_Speichern` / `btn_Neu` / `btn_Loeschen` / `btn_OK` | vier Knöpfe | ☑ |
| `btn_Beenden` (zweiter Schließknopf) | entfällt | ☑ |
| Löschen OHNE Rückfrage (W14‑B35) | `Rueckfrage` (A‑7) | ☑ |
| `m_bItemBearbeiten`, `SetControls`, `list_pvmodel` (tot, W14‑B31) | entfallen | ☑ |
| 28 Texte ohne Englisch (W14‑B37) | 29 Schlüssel de + en | ☑ |

---

## 5 Abweichungen (A‑Zeilen)

Jede Abweichung ist eine **Verhaltensänderung ohne automatisches Netz** (R‑W14‑5) und hat
deshalb einen eigenen Punkt in der Windows-Abnahme (§ 9).

| Nr | Abweichung | Grund |
|---|---|---|
| **A‑1** | „OK" liefert jetzt OK | W14‑B4, Entscheid E‑1: Drei der vier Browser setzten kein `DialogResult` und lieferten über `MitOk` IMMER `false`. Kein Aufrufer wertete es aus — folgenlos, aber jetzt richtig. |
| **A‑2** | EIN Löschtext für alle sieben Masken (`PSP_MELDUNG_WIRKLICH_LOESCHEN` mit Namen) | W14‑B16/B7, Entscheid E‑4: Der Solarkollektor-Browser hatte einen eigenen Wortlaut OHNE Namen, Heizkessel und BHKW denselben Satz hartkodiert deutsch, obwohl der Textkatalog ihn führt. |
| **A‑3** | Die achte BHKW-Leistungsstufe trifft | W14‑B10: Der Vorläufer füllte die Klappliste aus `LeistungText` („größer 1200 kW") und verglich gegen „über 1.200 kW" — die Stufe traf NIE und zeigte still ALLE Leistungen (79 statt 8). Der Kern entscheidet über den INDEX (A‑6 aus W6). |
| **A‑4** | `Exists`-Vorabtest in allen vier Browsern und beim Stromspeicher | W14‑B27: BHKW und Solarkollektoren legten ohne Vorabtest an, der Pufferspeicher prüfte mit inline-SQL, obwohl `PufferSpStammCtrl.Exists` im Kern liegt. |
| **A‑5** | Drei gemeinsame Knopftexte statt zwölf eigener | Damit fallen auch die Fehlübersetzungen „Extinguish the boiler" und „Cauldron from database". |
| **A‑6** | Eine Fehleingabe gibt den Speichern-Knopf frei | Sonst bliebe er gesperrt und die Prüfmeldung erschiene nie. Der Vorläufer setzte `m_bGeaendert` in `TextChanged`, ohne den Text anzusehen. |
| **A‑7** | Löschen mit Rückfrage auch bei der Photovoltaik | W14‑B35, Entscheid E‑3: Sie war die EINZIGE der elf Masken, die kommentarlos löschte. |
| **A‑8** | Der Löschgrund kommt durch | W14‑B42: Der Stromspeicher deutete JEDE Ausnahme als „Es besteht eine Projektzuordnung!" — auch eine gesperrte Datei oder einen fehlenden Schreibzugriff. |
| **A‑9** | Die drei berichtigten Einheiten stehen gleich richtig im Profil | W14‑B40: `EinheitenBeschriftungKorrigieren` schrieb sie zur Laufzeit über die Designer-Werte; die englischen `.resx`-Werte `label11 = "€"` und `label12 = "Module costs"` waren dadurch teils tot. |
| **A‑10** | Keine modale Prüfung beim Feldverlassen mehr | W14‑B45: `textBox_Typ_Validating` war der LETZTE Rest des vor `ab5bf32` überall abgeschafften Musters. Geprüft wird am Speichern-Knopf, gemeldet im Banner. |
| **A‑11** | Der Kontextmenüweg des Stromspeichers öffnet den Katalog wie jeder andere Weg | W14‑B39, Entscheid E‑5: `StromspeicherKontextMenuCtrl` füllte `list_spmodel` mit einer Anlagenzeile und schrieb sie nach OK zurück — die Maske hat die Liste NIE verändert. Ein Leerlauf mit Datenbankzugriff weniger. |
| **A‑12** | Die Fehlschläge melden statt zu schweigen | W14‑B22 (Überschreiben schloss ohne Meldung), W14‑B33 (`UpdateFrom` ohne `else`), W14‑B47 (stiller `return`). |
| **A‑13** | Vier Übersetzungsfehler berichtigt | „Perfomance:" → „Performance:", „Apertur area:" → „Aperture area:", „Cauldron from database" → „Boilers from database", „Boiler name:" im PUFFERSPEICHER-Editor → „Storage name:" (W14‑B24). |
| **A‑14** | Die Herstellerliste des Pufferspeicherfilters kommt sortiert aus der Datenbank | Der Vorläufer baute sie über `ReadAll` und `FindStringExact` in der Oberfläche zusammen; die Reihenfolge hing an der Katalogsortierung. |
| **A‑15** | Die Speicherliste kommt sortiert | `Form_Stromspeicher_Load` las `SELECT Bezeichner FROM …` OHNE `ORDER BY`; die Liste stand in Einfügereihenfolge. |
| **A‑16** | Der Speichertyp ist eine Auswahl statt eines Freitextfeldes | Ein Katalogsatz mit unbekanntem Wert verliert ihn NICHT: Die Hülle hängt den Rohwert als vierten Auswahleintrag an. |

**Bitgleich geblieben:** sämtliche Filterprädikate und -grenzen (`Ptherm <50`, `>=50 and <200`,
`>=200 and <500`, `>=500 and <1000`, `>=1000`; die acht BHKW-Stufen; `VOLUMEN_SQL`), die
Brennstoff-IDs 1…25, die Speichertyp-DB-Werte samt den drei eingefrorenen englischen Altwerten,
die drei Speicherwege des Editors und ihre Enabled-Zustände, die ReadOnly-Regeln, die
Dublettenklammer, die Vorbelegungen bei „Neu" (PV zwei leer + zehn Nullen, Stromspeicher
dreizehn mit `eta_RT = 0,90`, `c_ver = 0,025` und „Lithium-Ionen"), die `leerErlaubt`-Regel je
Feld, die Reihenfolge Prüfung → Schreiben → Liste neu → Markierung zurück, die Meldung ohne
Auswahl nur dort, wo der Vorläufer sie hatte, und `NurLesen`.

---

## 6 Befunde W14‑B1 … B77 mit Entscheid

| Nr | Befund | Entscheid |
|---|---|---|
| B1 | Drei `Load`-Handler tragen den Namen einer gelöschten Maske | **fällt** mit den Masken |
| B2 | Zwei Brennstoffketten für denselben Katalog | **berichtigt** (W14a.0b, § 3, A‑1 der Vermessung) |
| B3 | Zwei Menüpunkte für Heizkessel und Pufferspeicher | **wörtlich** — beide bleiben; **Anwenderfrage E‑9** |
| B4 | Fünf Masken setzen kein `DialogResult` | **angeglichen** (A‑1) |
| B5 | Neun tote Stellen in vier Masken | **fallen** |
| B6 | `m_ID_Projekt` in fünf Masken unbenutzt | **fällt** |
| B7 | Vier Meldungstexte hartkodiert deutsch, obwohl der Katalog sie führt | **angeglichen** (A‑2) |
| B8 | `Form_BHKWEing_Load` baut die Liste zweimal | **fällt** |
| B9 | `dataGridView1_Click` nicht verdrahtet | **fällt** |
| B10 | Die achte BHKW-Leistungsstufe trifft nie | **angeglichen** (A‑3) |
| B11 | `Form_BHKWAdmin` gar nicht lokalisiert (26 Literale) | **26 Schlüssel de + en** (W14a.0g) |
| B12 | Sieben inline-SQL-Stellen per Textverkettung | **in die Controller mit `DbParam`** (W14a.0c) |
| B13 | Drei unbenutzte öffentliche Felder | **fallen** |
| B14 | Methodenname mit Umlaut | **fällt** |
| B15 | `textBox_Modul_A` doppelt belegt, Modulfläche geht verloren | **wörtlich**; **Anwenderfrage E‑2** |
| B16 | Abweichender Löschtext ohne Namen | **angeglichen** (A‑2) |
| B17 | Fünf tote Stellen in 188 Zeilen | **fallen** |
| B18 | `SetDBList`-Parameter nie belegt | **entfällt ersatzlos** |
| B19 | `KiAufrufKnopf` verliert seinen letzten Nutzer | **gelöscht**; **Anwenderfrage E‑10** |
| B20 | Feldname „Gesamtvolumen" hartkodiert | **aus dem Textkatalog** (`PSPK_FELD_VOLUMEN`) |
| B21 | `double.TryParse` ohne Kultur neben geprüftem Volumen | **eine Zahlregel für alle drei Felder** |
| B22 | „Überschreiben" meldet den Fehlschlag nicht | **behoben** (A‑12) |
| B23 | Kein `help_mapping`-Eintrag für den Katalogeditor | **Zeile ergänzt** |
| B24 | Englisch „Boiler name:" im Pufferspeicher | **berichtigt** → „Storage name:" (A‑13) |
| B25 | Der letzte „unklar"-Zustand des Bestands | **fällt**; die Regel bleibt am Prüfmuster prüfbar (§ 7) |
| B26 | Fenster vor der Leerprüfung angelegt | **fällt** |
| B27 | inline-`Exists` neben vorhandenem `PufferSpStammCtrl.Exists` | **Controller** (A‑4) |
| B28 | `PufferSpFilter.cs` verliert seinen einzigen Nutzer | **gelöscht** |
| B29 | `label1_Click` leer verdrahtet | **fällt** |
| B31 | `m_bItemBearbeiten`/`SetControls`/`list_pvmodel` ohne Aufrufer | **fallen** |
| B32 | `Console.WriteLine` ohne `$` | **fällt** |
| B33 | `UpdateFrom` ohne `else` | **behoben** (A‑12) |
| B34 | `InitControls` setzt ein Feld anders als die zwölf anderen | **fällt** — die Vorbelegungen stehen im Profil |
| B35 | `Form_AdminPV` löscht ohne Rückfrage | **angeglichen** (A‑7) |
| B36 | Tote Kette `Masken.PvAdmin` → `MenueCtrl.PV()` | **belebt** (W14a.0h) |
| B37 | 28 von 29 Texten ohne Englisch | **29 Schlüssel de + en** |
| B38 | `btn_Abbruch_Click` tot | **fällt** |
| B39 | `list_spmodel` unverändert zurückgeschrieben | **entfällt** (A‑11, E‑5) |
| B40 | Englische Labels vom Code überschrieben | **im Profil richtig** (A‑9) |
| B41 | `"Lithium-Ionen"` nicht in `DbWerte` | **`DbWerte.SP_TYP_LITHIUM_IONEN`** (W14a.0f) |
| B42 | Jede Ausnahme gilt als Projektzuordnung | **behoben** (A‑8) |
| B43 | `ClientSize` 614 gegen 1 036 zur Laufzeit | **entfällt** — CSS |
| B44 | Zwei Vorgabewerte an zwei Orten | **`StromspeicherModel.C_VER_VORGABE`** (W14a.0f) |
| B45 | Modale Prüfung beim Feldverlassen | **behoben** (A‑10) |
| B46 | `GetTextBox_Energie`, `btn_Abbruch_Click` tot | **fallen** |
| B47 | `Update` bei `false`: stiller `return` | **behoben** (A‑12) |
| B76 | `SpeichernLeiste.cs` verliert beide Nutzer | **gelöscht** |
| B77 | Kein Nachweis für die sieben Masken | **`KatalogVerwaltungTests`** entsteht zuerst (§ 2) |

**Neu in dieser Welle:**

| Nr | Befund | Entscheid |
|---|---|---|
| **W14a‑B78** | `Form_SolarKollektorenAdmin` hat ZWEI Flächenfelder: `textBox_Kollektor_A` („Kollektorfläche:" / „Collector area:") und `textBox_Modul_A` („Aperturfläche:"). Das erste wird im ganzen Bestand NIE gefüllt und bleibt leer; das zweite bekommt in `:117` die Modulfläche und in `:118` sofort danach die Aperturfläche. Die Vermessung hat die beiden Felder als eines gelesen. | **wörtlich**: beide Felder bleiben, das erste leer. **Anwenderfrage E‑11** |
| **W14a‑B79** | `Werkzeuge/Formularkarte.Tests` war beim Wellenbeginn ROT: `DieHaelfteDerMaskenIstLokalisiert` verlangte 21 bei gemessenen 20, und `DieHaeufigstenTypenSindAbgedeckt` fand `NumericUpDown` nicht mehr. | **behoben durch den Merge** von `origin/ios_migration` (`9cccfc1`, `2a53d36`) — der Typzeuge prüft seither „Bestand ODER Prüfmuster", und der Nebenbaumfilter misst ab der Suchwurzel. |

---

## 7 Die zwei Testanker (R‑W14‑2)

### 7.1 `ErreichbarkeitTests.DieSprungtabelleLoestDieMaskenschluesselAuf`

| | vorher | nachher |
|---|---|---|
| Zeuge | `Form_AdminStromspeicher` | **`Form_ProjektSpeichernUnter`** |
| Schlüssel | `Masken.StromspeicherAdmin` | `Masken.ProjektSpeichernUnter` |
| fällt mit | W14a | **W15a** |

Die Kette der Zeugen steht als Kommentar im Test: `Form_WP` (bis W7.10) →
`Form_AdminStromspeicher` (bis W14a) → `Form_ProjektSpeichernUnter`. Von den fünf
Maskenschlüsseln, hinter denen nach W14 noch eine WinForms-Maske steht, ist sie der kürzeste Weg.

### 7.2 `EinDauerhaftGesperrterKnopfMachtDenWegUnklarStattJa`

Der Fall lief bis W14a gegen den echten Bestand — `Form_PufferSp_Bearbeiten` war die EINE
„unklar"-Maske des Programms. Er läuft jetzt gegen das **Prüfmuster**
(`Werkzeuge/Formularkarte.Tests/Pruefmuster/Pufferspeicher/`):

* `Form_PufferSp_Bearbeiten.{cs,designer.cs,resx,de-DE.resx,en-US.resx}` — verschoben, unverändert
* `Form_PufferSp_Admin.Auszug.cs` — gekürzt auf den `m_bReadOnly`-Zweig und die zwei Öffner
* `Form_PufferSp_Admin.{Designer.cs,resx}` — für die Kartenlesung
* `MDIMainForm.Auszug.cs` — die WURZEL; ohne sie wäre im Prüfmusterbaum jede Maske „nein",
  und der Unterschied zwischen „nein" und „unklar" nicht mehr prüfbar

Dazu ein neuer Fall `DerBestandFuehrtKeineUngeklaerteMaskeMehr`: 0 nein / 0 verwaist /
0 unklar, und `Erreichbar(Ja) == Masken`.

### 7.3 Der DataGridView-Typzeuge (Nachtrag der Orchestrierung)

W14a löscht die letzten `DataGridView`-Masken. `Form_SolarKollektorenAdmin` ist deshalb
ebenfalls **verschoben statt gelöscht** — nach `Pruefmuster/Solarthermie/`, wo
`StapelTests.DieHaeufigstenTypenSindAbgedeckt` sie über `PruefmusterTypen()` findet.

---

## 8 Gate

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, ≤ 12 Warnungen | **0 Fehler, 12 Warnungen** |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 027 nach dem Merge | **3 144** (450 + 337 + 635 + 1 722) |
| dieselben unter `LANG=en_US.UTF-8` | grün | **grün** |
| `dotnet test Werkzeuge/Formularkarte.Tests` | 123 | **124** (ein Fall dazu: § 7.2) |
| dieselben unter `LANG=en_US.UTF-8` | grün | **grün** |
| `python3 Werkzeuge/SqlDialektPruefer/pruefer.py` | 0 Fundstellen | **0** (1 233 SQL-Texte) |
| `dotnet run --project Proben/ChartProben -c Release` | 30 | **30, 0 Verstöße** |
| Referenzlauf 1030/1007/1017 | byte-gleich | **PASS, 815 043 Werte; `diff -rq` byte-gleich in allen drei** |
| Stapellauf `--alle` | 25 Masken, 0 unklar | **25 Masken, 28 Designer-Dateien, 14 lokalisiert, 25 von 25 erreichbar, 0/0/0** |
| Wächter iU5 (`Program.*`) | leer | **leer** |
| Wächter Plattform (`System.Windows.Forms` …) | leer | **leer** |
| `git grep` auf die sieben Klassennamen, `PufferSpFilter`, `SpeichernLeiste`, `KiAufrufKnopf` | 0 außerhalb Protokoll/Prüfmuster | **0** |

**Nach dem Abschluss-Merge von `origin/ios_migration` (Welle W14b, `c9855b1`)** — die Zahlen
der Tabelle sind die der Welle allein, die des gemeinsamen Standes stehen hier:

| Prüfung | Ergebnis nach dem Merge |
|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | **0 Fehler, 12 Warnungen** |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **3 227** (450 + 337 + 1 764 + 676) |
| `dotnet test Werkzeuge/Formularkarte.Tests` | **124** |
| Stapellauf `--alle` | **21 Masken, 24 Designer-Dateien, 11 lokalisiert, 21 von 21 erreichbar, 0/0/0** |
| Referenzlauf 1030/1007/1017 | **PASS, byte-gleich** |

---

## 9 Windows-Abnahme

Am Windows-Gerät zu prüfen — was kein automatisches Netz sieht.

| # | Punkt |
|---|---|
| 1 | **Heizkessel-Brennstoffkette (beide Dialoge!)**: In der Katalogverwaltung UND im Projektdialog `HeizkesselDialog` die Filter „Fernwärme", „Sonstige Energieträger" und „Wasserstoff" wählen — die Liste zeigt nur noch Kessel dieser Brennstoffe (in der Auslieferungsdatenbank ggf. keinen), nicht mehr den ganzen Katalog. |
| 2 | **`NurLesen`**: Aus dem Pufferspeicher-Projektdialog „Katalog ansehen" — „Neu…", „Bearbeiten…" und „Löschen" sind gesperrt, Liste und Detailblock stehen. Über das Menü dagegen sind alle drei frei. |
| 3 | **AP3-Gruppe des Stromspeichers**: Alle sechs Gerätefelder sind sichtbar und beschriftet; die Fenstergröße stimmt ohne Laufzeitvergrößerung. |
| 4 | **Die drei Speicherwege des Katalogeditors**: „Überschreiben", „Speichern unter" (Namensabfrage als Überlagerung) und „Speichern" im Modus Neu; die Enabled-Zustände je Modus. |
| 5 | **Kontextmenüweg Stromspeicher**: Rechtsklick auf eine Speicherzeile → „Bearbeiten" öffnet den Modulkatalog; nach „Beenden" ist das Änderungsdatum gesetzt und die Gewerksliste aufgefrischt. |
| 6 | **BHKW-Schreibschutz**: Ein Auslieferungssatz wird grau gezeichnet; „Speichern" fragt nach, „Nein" schreibt nicht, „Ja" schreibt. Löschen wird abgelehnt und sagt warum. |
| 7 | **Löschen mit Rückfrage in allen sieben Ausprägungen**, mit dem Namen im Text. |
| 8 | **„OK"/„Beenden" liefert OK** (A‑1) — und schreibt beim Heizkessel und BHKW offene Änderungen zurück. |
| 9 | **Beide Sprachen**: Oberfläche auf Englisch umstellen, alle drei Komponenten öffnen — keine deutschen Reste, „Storage name:" statt „Boiler name:". |
| 10 | **125 % Skalierung**: Alle drei Komponenten öffnen; die Felder sind vollständig sichtbar, kein Text abgeschnitten. |
| 11 | **Solarkollektoren**: Die Felder „Kollektorfläche" (leer) und „Aperturfläche" (Wert) — der Zustand, den E‑2/E‑11 zur Entscheidung stellen. |
| 12 | **PV-Menüpunkt**: „Photovoltaik → Module bearbeiten" öffnet den Modulkatalog (jetzt über `MenueCtrl`). |
| 13 | **Der KI-Knopf ist weg** — in allen Masken (E‑10). |

---

## 10 Anwenderfragen

| Nr | Frage | Stand |
|---|---|---|
| **E‑2** | Das Feld „Aperturfläche" der Solarkollektorenverwaltung zeigt die Aperturfläche, obwohl der Code vorher die MODULFLÄCHE liest und sofort überschreibt. Soll es dabei bleiben? | wörtlich übernommen |
| **E‑9** | Für Heizkessel und Pufferspeicher gibt es je ZWEI Menüpunkte, die dieselbe Maske öffnen. Zusammenlegen oder beide behalten? | beide behalten |
| **E‑10** | Mit `Form_PufferSp_Bearbeiten` verliert `KiAufrufKnopf` seinen letzten Nutzer; der KI-Einstieg aus einer Maske verschwindet, bis W15b den `Gespraechsverlauf` baut. | Datei gelöscht (Regel M1) |
| **E‑11** (neu) | Das Feld „Kollektorfläche" der Solarkollektorenverwaltung wird im ganzen Bestand NIE gefüllt und bleibt leer. Soll es die Modulfläche zeigen (die heute gelesen und verworfen wird) oder ganz entfallen? | wörtlich übernommen: es bleibt und bleibt leer |
| **E‑1, E‑3, E‑4, E‑5** | `DialogResult`, Löschrückfrage bei PV, ein Löschtext für alle, Kontextmenüweg des Stromspeichers | wie in der Vermessung empfohlen umgesetzt (A‑1, A‑7, A‑2, A‑11) |

---

## 11 Was die Welle abgeräumt hat

| Datei | Zeilen | Grund |
|---|---|---|
| `Views/Heizkessel/Form_Heizkessel_Admin.*` | 365 + 306 | Ausprägung Heizkessel |
| `Views/BHKW/Form_BHKWAdmin.*` | 465 + 517 | Ausprägung BHKW |
| `Views/Solarthermie/Form_SolarKollektorenAdmin.*` | 188 + 305 | Ausprägung Solarkollektoren — **verschoben** nach `Pruefmuster/Solarthermie/` |
| `Views/Pufferspeicher/Form_PufferSp_Admin.*` | 213 + 280 | Ausprägung Pufferspeicher — Designer und `.resx` **kopiert** nach `Pruefmuster/Pufferspeicher/` |
| `Views/Pufferspeicher/Form_PufferSp_Bearbeiten.*` | 354 + 257 | `PufferSpKatalogDialog` — **verschoben** nach `Pruefmuster/Pufferspeicher/` |
| `Views/Photovoltaik/Form_AdminPV.*` | 297 + 430 | Ausprägung Photovoltaik |
| `Views/Stromspeicher/Form_AdminStromspeicher.*` | 505 + 274 | Ausprägung Stromspeicher |
| `Views/Pufferspeicher/PufferSpFilter.cs` | 96 | letzter Nutzer weg (B28) |
| `Allgemein/SpeichernLeiste.cs` | 128 | beide Nutzer weg (B76) |
| `Allgemein/KI/KiAufrufKnopf.cs` | 270 | letzter Nutzer weg (B19, E‑10) |
| fünf `Sprungziel`-Konstanten und -Zweige | ~47 | die Ziele sind selbst Blazor |


## Anwenderentscheid #76 (05.09.2026) — ein Schema für alle Projekt↔DB-Auswahldialoge

**Der Entscheid.** Nach der Windows-Abnahme (PDF „iOS_Migration_Probleme", S. 2, 6–8)
hat der Anwender festgelegt: *Alle* Dialoge, in denen links „im Projekt ausgewählt" und
rechts „aus der Datenbank/Katalog" mit Pfeilknöpfen dazwischen stehen, folgen dem alten
**BHKW-PLAN-Schema NEBENEINANDER** — Projektliste links, Katalogliste rechts, die zwei
Pfeilknöpfe in einer schmalen Mittelspalte. Auf **schmalem Schirm** (iPad hochkant,
schmales Fenster) bricht das Paar automatisch **untereinander** um; dann gilt das
Schema, das der Gebäudedialog seit Welle 9 hatte (Projektliste oben, Pfeile dazwischen,
Katalog unten). Listen sind in beiden Fällen höhenbegrenzt mit Rollbalken (Befund
W9‑B‑2, `.epos-raster-huelle` / `--epos-listenhoehe`).

**Ein Baustein statt elf Markups.** `EPOS.UI/Bausteine/Zweispaltenauswahl.razor` trägt
drei benannte Bereiche — `Links` (Projekt), `Mitte` (die zwei Knöpfe), `Rechts`
(Katalog) — dazu die Überschriften, die vier Texte der Knöpfe, ihre Sperrzustände und
Rückrufe sowie `NurRechts` für die Verwaltungsbetriebsart. Der Stilblock
„Zweispaltenauswahl" in `EPOS.UI/wwwroot/epos-ui.css` steht direkt hinter dem alten
Block AUSWAHLPAAR; die alte Klasse `.epos-auswahlpfeile` ist entfallen,
`.epos-auswahlpaar`/`.epos-auswahlspalte` bleiben für die fünf Masken **ohne** Pfeile
(`GebaeudetypDialog`, `TypProfilDialog`, `KennlinienEditorDialog`,
`WaermepumpeAnlageDialog`, `WaermepumpeStammDialog`).

**Das Zeichen hängt an der Anordnung, nicht am Text.** Ein Pfeil im Ressourcentext kann
nicht wissen, wie die Listen gerade stehen. Jeder Knopf trägt deshalb **beide** Zeichen
im Markup (`aria-hidden`, damit eine Sprachausgabe den Satz liest und nicht das
Dreieck), und das Stilblatt zeigt je Breite genau eines: nebeneinander **◀/▶** (die
Zeile wandert nach links ins Projekt bzw. nach rechts in den Katalog zurück),
untereinander **▲/▼**. Kein JavaScript.

> **Zur Pfeilrichtung.** Der Entscheidtext nennt in der Klammer „▶ In das Projekt
> übernehmen, ◀ Aus dem Projekt entfernen". Umgesetzt ist es **umgekehrt** — ◀
> übernimmt, ▶ entfernt —, weil derselbe Satz die Projektliste ausdrücklich **links**
> verortet und weil das Vorbild es so hält: `Form_Gebaeude.resx` `btn_Hinzu` = „◀",
> `btn_Entfernen` = „▶"; `Form_Heizkessel.resx` `btn_Kessel_Hinzu` = „◀",
> `btn_Kessel_Entfernen` = „▶". Bei Projektliste links zeigt „übernehmen" nach links.
> Soll es doch andersherum sein, sind es zwei Zeichen in
> `Bausteine/Zweispaltenauswahl.razor` — sonst nichts.

**Der Umbruch ist eine Medienabfrage, kein `flex-wrap`.** Nur so weiß das Stilblatt,
welches Zeichen gerade gilt; bei `flex-wrap` käme die Reihe um, ohne dass eine Regel es
merkt, und die Pfeile zeigten ins Leere. Die Umbruchbreite steht als Token
`--epos-zweispalten-umbruch` (900 px) **und** — weil eine Medienabfrage kein Token
lesen kann — ein zweites Mal in der Abfrage; die Wache
`ZweispaltenauswahlTests.Die_Umbruchbreite_steht_als_Token` hält beide Werte
gegeneinander. Die Breite der Mittelspalte ist `--epos-zweispalten-mitte` (10 rem; im
Bestand 63 px bei `Form_Gebaeude`, 88 px bei `Form_Heizkessel` — hier etwas mehr, weil
die Knöpfe seit Befund W9‑B‑3 ihre Aufgabe im Klartext tragen).

**Texte.** Neu in beiden Sprachkatalogen und im `Resource.Designer.cs`:
`AUSWAHL_BTN_UEBERNEHMEN`, `AUSWAHL_BTN_UEBERNEHMEN_HINWEIS`, `AUSWAHL_BTN_ENTFERNEN`,
`AUSWAHL_BTN_ENTFERNEN_HINWEIS`, `AUSWAHL_GRP_PFEILE` (der Name der Knopfgruppe für die
Sprachausgabe). Aus `GEB_BTN_UEBERNEHMEN` / `GEB_BTN_ENTFERNEN` sind die Zeichen
**▲/▼ entfernt**; die acht nebeneinander stehenden Dialoge nehmen weiter
`HZK_TIP_HINZU` / `HZK_TIP_ENTFERNEN` — jetzt als **Beschriftung** statt nur als
Kurztext.

**Tastaturweg und Sprachausgabe.** Die drei Bereiche stehen in der Reihenfolge links –
Mitte – rechts im Markup; der Tabulator läuft damit von der Projektliste über die zwei
Knöpfe in den Katalog. Jede Spalte ist eine `role="group"` mit ihrer Überschrift als
`aria-label`, die Knopfgruppe ebenso.

### Der Katalogbrowser ist NICHT betroffen — und warum das die Prüfung war

Der Auftrag zum Entscheid #76 nannte den `KatalogBrowserDialog` (vier Ausprägungen:
Heizkessel, BHKW, Solarkollektoren, Pufferspeicher) unter den Kandidaten, weil er
„heute schon nebeneinander steht, aber mit eigenem Markup". Die Prüfung ergibt:
**Er gehört nicht zu diesem Muster.**

Der Entscheid gilt für Dialoge mit **zwei Listen und Pfeilknöpfen dazwischen** — links
das Projekt, rechts die Datenbank, und die Knöpfe schieben eine Zeile von der einen in
die andere. Der Katalogbrowser hat **eine** Liste (den Katalog) und daneben einen
**Detailblock**; es gibt kein Projekt, keine zweite Liste und keinen Pfeil. Seine
Knöpfe heißen „Neu…", „Bearbeiten…", „Löschen" und „OK". Ein Umbau auf
`Zweispaltenauswahl` hätte ihm eine Mittelspalte mit zwei Knöpfen gegeben, die nichts
zu tun haben. Er bleibt auf `epos-katalogbrowser`.

Dieselbe Prüfung fällt für `WaermepumpenDialog` (`Form_WPAuswahl`, eine Liste),
`WaermepumpenKatalogDialog`, `SolarganglinieAdminDialog` und `StromganglinieAdminDialog`
ebenso aus. Die **elf** Dialoge, für die der Entscheid gilt, stehen in
`ZweispaltenauswahlTests.Dialoge` — wer einen zwölften baut, trägt ihn dort ein, und die
Wache `Keine_Komponente_baut_die_Pfeilspalte_noch_selbst` verhindert, dass daneben eine
zweite Fassung entsteht.

Die vier Projektdialoge der Erzeuger, die der Katalogbrowser als Sprungziel bedient
(`HeizkesselDialog`, `BhkwDialog`, `PufferspeicherDialog`, `PhotovoltaikDialog`), sind
umgestellt — dokumentiert im Protokoll der Welle 6.

### Wachen

`EPOS.UI.Tests/Bausteine/ZweispaltenauswahlTests` (14 Fälle) prüft drei Ebenen: den
**Baustein** (Reihenfolge der drei Bereiche = Tastaturweg, `aria`-Beschriftungen, beide
Zeichen je Knopf mit `aria-hidden`, Klartext, Kurztext, Sperrzustände, Rückrufe,
`NurRechts`), die **Regel im Stilblatt** (nebeneinander ist die Vorgabe, kein
`flex-wrap`, Token gegen Medienabfrage, je Anordnung genau ein Zeichen) und den
**Bestand** (alle elf Projekt↔DB-Dialoge nehmen den Baustein; keine Komponente baut die
Pfeilspalte noch selbst). Eine bunit-Probe sieht eine Stilregel nicht — Lehre W6‑B‑1.

### Abnahmepunkte A‑#76

1. **Breit** (Fenster ≥ 900 px): Projektliste **links**, Katalog **rechts**, die zwei
   Knöpfe in einer schmalen Spalte dazwischen; die Zeichen sind ◀ (übernehmen) und ▶
   (entfernen).
2. **Schmal** (Fenster < 900 px, iPad hochkant): Projektliste **oben**, Knöpfe
   darunter nebeneinander, Katalog **unten**; die Zeichen sind ▲ und ▼.
3. **Listen begrenzt**: Beide Listen rollen in ihrem Rahmen, der Spaltenkopf bleibt
   stehen; Filter, Detailblock und Schlussleiste bleiben erreichbar, ohne die ganze
   Seite zu rollen.
4. **Knöpfe**: Beide tragen ihren Satz im Klartext — auf Deutsch **und** auf Englisch —
   und einen Kurztext, der die Herkunft der Zeile nennt. Jeder bleibt gesperrt, solange
   in der jeweils anderen Liste nichts markiert ist.

---

## Anwenderwunsch 05.09.2026 (**W14a‑E‑6**) — Katalogbrowser und Modulkatalog nutzen die Fläche

**Der Befund.** Bildschirmfoto „Administration Solarkollektoren" (`KatalogBrowserDialog`
in der Ausprägung `Solarkollektoren`): *„Admin-Menüs sind nicht an Größe Bildschirm
angepasst."* Zu sehen waren untereinander die Überschrift, der Balken „Auswahl in DB:",
eine Liste in ihrem **eigenen** kleinen Rollrahmen — und darunter, nur über den
**Seiten**rollbalken erreichbar, der Balken „Eingabe der Solarkollektoren" mit den
Feldern. Dazu eine Kopfzeile, die **„Name | Name | Eigenschaften"** las.

**Die Vorbilder standen NEBENEINANDER.** Nachgemessen an den Designern und `.resx` der
sechs gelöschten Masken (`git show 0afe8ad^:…`, `git show 1b2e2f4^:…`):

| Ausprägung | Vorbild | Fenster | Liste | Eingabe |
|---|---|---|---|---|
| `Heizkessel` | `Form_Heizkessel_Admin` | 726 × 383 | `listBox_Kessel_DB` (12, 30) 302 × 157 **links** | Felder ab x 448 **rechts** |
| `Bhkw` | `Form_BHKWAdmin` | 856 × 517 | `dataGridView1` (12, 25) 403 × 369 **links** | `groupBox2` (438, 28) 405 × 421 **rechts** |
| `Solarkollektoren` | `Form_SolarKollektorenAdmin` | 825 × 494 | `dataGridView1` (26, 59) 359 × 302 **links** | Felder ab x 402 **rechts** |
| `Pufferspeicher` | `Form_PufferSp_Admin` | 721 × 330 | `listBox_PufferSp_DB` (13, 30) 299 × 191 **links** | Felder ab x 346 **rechts** |
| `Photovoltaik` | `Form_AdminPV` | 607 × 489 | `listBox_PV` (12, 9) 211 × 259 **links** | Felder ab x 253 **rechts** |
| `Stromspeicher` | `Form_AdminStromspeicher` | 614 × 367 | `listBox_Stromspeicher` (22, 22) 201 × 293 **links** | Felder ab x 240 **rechts** |

**Die Umsetzung — ein Baustein, kein CSS je Dialog.**
`EPOS.UI/Bausteine/Katalograhmen.razor` trägt zwei benannte Bereiche, `Liste` und
`Eingabe`, dazu `Gestapelt` für die Masken, deren Vorbild schon untereinander stand.
`KatalogBrowserDialog` und `ModulKatalogDialog` füllen ihn; ihre Wurzel trägt zusätzlich
`epos-katalog-dialog`. Der Stilblock „Katalogdialoge" in `EPOS.UI/wwwroot/epos-ui.css`
steht am Ende des Blattes und hält drei Regeln:

* **Die Wurzel nutzt die Höhe** — `max-width: none` (die 1160‑px‑Bremse von
  `.epos-dialog` ist für eine Liste neben einem Feldblock falsch) und `height: 100dvh`.
* **Die Liste nimmt die verbleibende Höhe** — `flex: 1 1 auto; max-height: none;
  min-height: 9rem`. **Hier und nur hier** fällt die Höchsthöhe aus Befund W9‑B‑2:
  Sie war die Antwort auf eine Liste, die den Detailblock nach unten schob; im
  Katalograhmen schiebt nichts mehr, weil der Eingabeblock **daneben** steht und die
  Schlussleiste außerhalb. Überall sonst gilt `--epos-listenhoehe` unverändert weiter
  (Wache: `ListenrahmenTests`).
* **Der Eingabeblock rollt selbst**, nie die Seite (`overflow-y: auto`).

**Die Filter stehen in der Listenspalte.** Im Vorbild saßen `comboBox_Brennstoffart` und
`comboBox_Leistung` **unter** der Liste (Heizkessel: Liste bis y 187, Klapplisten bei
y 214). Hier stehen sie darüber — die Hausregel aus Anwenderentscheid #76 („Filter
gehören über die Katalogliste, dort steht die Liste, auf die sie wirken") und ohne der
Liste Höhe zu nehmen.

**Die Umbruchbreite ist 900 px, nicht 1100.** Der Inhalt der WebView rechnet in
CSS-Pixeln, das Fenstermaß (iU8‑E‑1) in Gerätepixeln: Bei 150 % Skalierung sind
1632 Gerätepixel nur **1088** CSS-Pixel. Ein Umbruch bei 1100 px träfe genau den
Anwender, der den Befund gemeldet hat. 900 px ist außerdem der Wert, bei dem schon der
Baustein `Zweispaltenauswahl` (`--epos-zweispalten-umbruch`) und der Dublettenbaum
umbrechen — und die Vorbilder waren 607 bis 856 px breit und standen dabei
nebeneinander.

**Die Kopfzeile heißt wieder „Wahl".** Die Ursache saß in den **zwei Hüllen**, nicht in
den Komponenten: `KatalogBrowserHuelle:93` gab `["SpalteWahlText"] = profil.SpalteName`
und `ModulKatalogHuelle:72` `= profil.Listenbeschriftung` — beide Male die Beschriftung
der **Nachbarspalte**. Beide lesen jetzt `Resource.KFAK_SP_WAHL` (de „Wahl", en
„Select"), denselben Schlüssel wie die acht übrigen Katalogdialoge. Kein neuer Text.

**Wachen.** `EPOS.UI.Tests/Bausteine/KatalograhmenTests` (Markup **und** Stilblatt —
Lehre W6‑B‑1) und `EPOS.UI.Tests/KatalogdialogTests` (Wurzelklasse, Rahmen,
Eingabeblock im DOM, Kopfzeile „Wahl" in allen sechs Ausprägungen).

**Abnahmepunkte am Gerät** (100 / 125 / 150 %):

1. „Administration Solarkollektoren" füllt rund 85 % der Breite und 90 % der Höhe.
2. Liste **links**, Eingabeblock **rechts**; beide ohne Seitenrollbalken sichtbar.
3. Die Liste ist so hoch wie das Fenster es zulässt und rollt **in sich**.
4. Das Fenster schmal ziehen (< 900 CSS-px): Liste oben, Eingabe unten — nichts
   verschwindet.
5. Die Kopfzeile der Liste liest „Wahl | Name | Eigenschaften" (Solarkollektoren, BHKW)
   bzw. „Wahl | Name" (Heizkessel, Pufferspeicher, PV, Stromspeicher).
6. Dasselbe für „BHKW Verwaltung", „Administration Heizkessel", „Administration
   Pufferspeicher", „Administration PV-Module" und „Administration Stromspeicher".

## Windows-Abnahme 05.09.2026 — Kompaktes Formularraster (iU8‑E‑2, W14a‑E‑7)

**Der Wortlaut.** „Verbessere die Darstellung der Dialoge, insbesondere der Parameter auf der
rechten Seite: kompakter, übersichtlicher, …" — dazu die Erweiterung desselben Tages:
„Genauso für andere Dialoge prüfen." Das Bildschirmfoto zeigt
`EPOS.UI/Dialoge/Erzeuger/ModulKatalogDialog.razor` in der Ausprägung PV, im `Katalograhmen`
aus **W14a‑E‑6** vom Nachmittag: links die Liste „Module in Datenbank", rechts der Block
„Moduldaten".

**Der Befund, gemessen am Bild.** Jedes Feld nahm die **volle Breite**, die Beschriftung stand
**darüber**, die Zahlenfelder waren so breit wie die Textfelder, und die Einheit stand am
**rechten Rand des Blocks** — bei „Nennleistung (Pmax)" rund 40 Zeichen vom Wert entfernt. Der
Block war damit doppelt so hoch wie der Dialog und rollte, obwohl daneben eine halbe Fensterbreite
leer stand.

**Die Vorbilder taten es anders — gemessen, nicht geschätzt.** Aus `Form_AdminPV.resx`
(607 × 489, gelöscht mit iU9‑W14a.3, aus der Historie gezogen):

| Was | Vorbild | Neu |
|---|---|---|
| Beschriftungsspalte | Label bei x = 253, Feld bei x = 431 → **178 px** | `--epos-beschriftung-breite: 12rem` (192 px) |
| Zahlenfeld | **62 px** (`textBox_Leistung`, `_Wirkungsgrad`, `_UMpp`, `_ULeerlauf`, `_IMpp`, `_IKurzschluss`, `_TempKoeff`, `_Laenge`, `_Breite`), Modulkosten 85 px | `--epos-kurzfeld-breite: 8em` (104 px bei 13 px Schrift — das Feld hält weiter `--epos-touchziel`) |
| Einheit | Feldende x = 493, Einheit bei x = 497/498 → **4 px dahinter** | in derselben `.epos-feld-zeile`, Abstand 6 px |
| Textfeld | 250 px (`textBox_Bezeichner`, `_Firma`) | die Feldspalte |
| Beschreibung | 250 × **48 px** — zwei Zeilen | `Mehrzeilig`, über beide Spalten |

Dieselben Verhältnisse in `Form_Heizkessel_Admin` (726 × 383), `Form_BHKWAdmin` (856 × 517),
`Form_AdminStromspeicher` (614 × 367) und `Form_PufferSp_Admin` (721 × 330).

**Die Regel in drei Sätzen.** Ein Parameterblock steht im Baustein **`Formularraster`**; darin
steht die Beschriftung **neben** dem Feld in einer festen Spalte von 12 rem, und die Felder ordnen
sich über `repeat(auto-fill, minmax(--epos-formularspalte, 1fr))` von selbst in **eine oder zwei
Spalten** — gemessen an der Breite des Rasters, nicht des Fensters, weshalb die rechte Spalte eines
`Katalograhmen` genauso richtig liegt wie ein freistehender Dialog. Ein Feld sagt **selbst**, wie
lang es ist: `Zahlenfeld` und `Ganzzahlfeld` tragen `epos-feld--kurz` (kurze Breite, Einheit
unmittelbar dahinter), ein mehrzeiliges `Textfeld` trägt `epos-feld--breit` (über beide Spalten) —
deshalb braucht **kein Dialog eine eigene Regel**, er hängt seine vorhandenen Feldbausteine nur in
den Raster. Unter 900 CSS-Pixeln (`--epos-zweispalten-umbruch`, dieselbe Breite wie
`Zweispaltenauswahl` und `Katalograhmen`) fällt die Beschriftung wieder über das Feld und der
Raster auf eine Spalte.

**Was NICHT passiert ist.** Kein Token wurde umgefärbt, keine Schriftgröße geändert,
`--epos-touchziel` (44 px) bleibt die Mindesthöhe jedes Feldes — die Kompaktheit kommt aus der
**Anordnung**, nicht aus kleineren Bedienelementen. Und die Regel greift **nur** innerhalb von
`.epos-formularraster`: Ein Feld irgendwo sonst im Haus behält seine Form (Beschriftung darüber).
Ohne diese Grenze hätte die Umstellung eines Bausteins die 92 Dateien mit Feldern auf einmal
verschoben — dieselbe Vorsicht wie bei den sieben `--epos-start-*`-Token (W16b‑E‑5).

**Zwei neue Bausteine.** `Bausteine/Formularraster.razor` (das Raster, `Einspaltig` als benannter
Rückweg für Blöcke, die eine Reihenfolge tragen oder neben einem Diagramm stehen) und
`Bausteine/Formulargruppe.razor` (die leise Zwischenüberschrift; ihr Wirt trägt
`display: contents`, damit die Felder **direkte** Rasterkinder bleiben — ein zwischengeschobener
Kasten hätte alle Felder einer Gruppe in EINE Rasterzelle gesetzt und die Beschriftungsspalten
zweier Gruppen auseinanderlaufen lassen). Die Gruppen kommen aus den Dialogen; der Baustein
liefert nur das Raster.

**Umgestellt (8 Dateien).**

| Datei | Welle | Felder | Wie |
|---|---|---|---|
| `Dialoge/Erzeuger/ModulKatalogDialog.razor` | W14a | 13 (PV) / 11 (Stromspeicher), über `Feldmarkup` | beide Gruppen im Raster — **der gemeldete Dialog** |
| `Dialoge/Erzeuger/KatalogBrowserDialog.razor` | W14a | je Profil (Heizkessel, BHKW, Solarkollektoren, Pufferspeicher) | Detailblock im Raster |
| `Dialoge/Erzeuger/PufferSpKatalogDialog.razor` | W6 | 6 | drei Gruppenköpfe, je ein Raster; das handgebaute `epos-feldpaar` entfällt |
| `Dialoge/Bedarf/BedarfAdminDialog.razor` | W8 | 4 | Anzeigeblock im Raster |
| `Dialoge/Bedarf/WaermebedarfAdminDialog.razor` | W8 | 2 | Einleseblock im Raster (`Einspaltig` — ein Pfadfeld braucht die Breite) |
| `Dialoge/Erzeuger/HeizkesselDialog.razor` | W6 | 9 | **Stichprobe**: Detailblock im Raster, `epos-feldpaar` entfällt |
| `Dialoge/Bedarf/GebaeudeDialog.razor` | W9 | 8 | **Stichprobe**: Detailblock im Raster |
| `Dialoge/Admin/EinstellungenDialog.razor` | W14c | 10 | **Stichprobe**: Reiter „Datenbank" und „Web" im Raster (`Einspaltig`) |

Die drei Stichproben belegen die Absicht: Es war **je eine Zeile** — `<Formularraster>` um den
vorhandenen Feldlauf, kein Feld angefasst, kein Text geändert, keine Regel je Dialog.

### Bestandsaufnahme aller Dialoge und Seiten mit Parameterblöcken

Maschinell gezählt (`Zahlenfeld`, `Ganzzahlfeld`, `Textfeld`, `Auswahlfeld`, `Datumsfeld`,
`Schalter`, `Dateiwahl`) über `EPOS.UI/Dialoge/**` und `EPOS.UI/Seiten/**`:
**92 Dateien, 624 Feldbausteine**, davon **8 Dateien im Formularraster**. Die restlichen **84**
teilen sich in zwei Klassen:

**A — durch reines Einhängen umstellbar** (Feldlauf in einem `Gruppenkopf`/`KindInhalt`, ggf. mit
`epos-feldpaar`, das dabei entfällt). **41 Dateien**, die dicksten zuerst:
`GebaeudeKatalogDialog` (41 Felder, W9), `BhkwKatalogDialog` (27, W6),
`TarifstrukturDialog` (27, W2), `HeizkesselKatalogDialog` (21, W6),
`PeakShavingDialog` (19, W12), `SolarkollektorKatalogDialog` (14, W7),
`PhotovoltaikVerguetungDialog` (13, W3), `WirtschaftlichkeitParameterDialog` (13, W3),
`WaermepumpeStammDialog` (10, W7), `GebaeudeWohnflaecheDialog` (9, W9),
`SolarkollektorenDialog` (8, W7), `BedarfsProfileDialog` (7, W8),
`GesetzeskatalogZeileDialog` (7, W3), `QuelleErdreichDialog` (7, W10b),
`QuellprofilDialog` (7, W10b), `ProjektKopfSeite` (7, W16a),
`CaseEingabeDialog` (6, W5), `StromAufschlaege` (5, W4),
`VorlagenPositionDialog` (5, W5), `ProjektTransferDialog` (5, W15a),
`TypStammDialog` (4, W8), `SpotpreisImportDialog` (4, W4),
`VorlagenUebernahmeDialog` (4, W5), `ProjektKopieDialog` (4, W15a),
`KostenprofilDialog` (4, W4), `VorlagenZeile` (4, W5) und 15 weitere mit 1–3 Feldern.

**B — Handarbeit nötig** (eigene Anordnung: Felder in Tabellenzellen, in einem `Zeilenraster`, in
einer `TemplateColumn`, an einem Diagramm oder in einem gerechneten Reiterband). **43 Dateien**,
darunter `BhkwWirtschaftlichkeitDialog` (29, Zeilenraster), `PvModulImportDialog` (28, Raster und
Reiter), `ParameterReiter` (23, Simulationsseite mit eigenem CSS), `PufferSpProjektDialog` (20),
`EnergietraegerEinstellungen` (18), `WaermepumpeAnlageDialog` (17),
`WaermepumpenKatalogDialog` (12), `BhkwDialog` (10), `EmissionskatalogDialog` (10),
`WaermesenkeDialog` (9), `GanglinieImportOptionenDialog` (9), `KennlinienEditorDialog` (7).
Bei ihnen ist zuerst zu entscheiden, ob der Block **überhaupt** ein Formularblock ist — eine
Bearbeitungszeile in einer Tabelle ist keiner, und der Raster darf dort nicht hinein.

**Vorschlag für die Restumstellung — drei Pakete** (Aufgabe #91):

| Paket | Umfang | Inhalt |
|---|---|---|
| **P1 — Erzeuger und Kataloge (W6/W7/W14a)** | 8 Dateien, ~110 Felder | `BhkwKatalogDialog`, `HeizkesselKatalogDialog`, `SolarkollektorKatalogDialog`, `WaermepumpeStammDialog`, `SolarkollektorenDialog`, `PufferspeicherDialog`, `StromspeicherDialog`, `PhotovoltaikDialog` — dieselbe Bauart wie die schon umgestellten Katalogdialoge, überwiegend Klasse A |
| **P2 — Kosten und Wirtschaftlichkeit (W1–W5)** | 12 Dateien, ~100 Felder | `TarifstrukturDialog`, `PhotovoltaikVerguetungDialog`, `WirtschaftlichkeitParameterDialog`, `GesetzeskatalogZeileDialog`, `CaseEingabeDialog`, `StromAufschlaege`, `VorlagenPositionDialog`, `VorlagenUebernahmeDialog`, `KostenprofilDialog`, `SpotpreisImportDialog`, `EnergietraegerDialog`, `ErtragBonus` — reine Parameterblöcke, aber **mit** den programmatisch gebauten Untergruppen des Vorläufers (`Form_Tarifstruktur.Gruppe`); hier wird `Formulargruppe` zum ersten Mal wirklich gebraucht |
| **P3 — Bedarf, Simulation und Projekt (W8–W12, W15a, W16a)** | 10 Dateien, ~130 Felder | `GebaeudeKatalogDialog`, `GebaeudeWohnflaecheDialog`, `BedarfsProfileDialog`, `TypStammDialog`, `PeakShavingDialog`, `QuelleErdreichDialog`, `QuellprofilDialog`, `ProjektKopfSeite`, `ProjektTransferDialog`, `ProjektKopieDialog` — teils Klasse B (Reiterbänder, Tabellen), je Datei zuerst entscheiden, welche Blöcke Formularblöcke sind |

**Wachen.** `EPOS.UI.Tests/Bausteine/FormularrasterTests` — 14 Fälle: Markup von Raster und
Gruppe, die Selbstmeldung der Felder (kurz / breit / weder-noch, samt der Gegenprobe, dass ein
`Datumsfeld` **nicht** kurz ist — ein `input type="date"` schneidet unter rund 130 px sein
Kalendersymbol ab), und fünf Stilblattfälle nach dem Muster `StartseiteTests.Stilblock`
(Beschriftungsspalte, Kurzfeld, Zwei-Spalten-Regel ohne Prozente, durchsichtige Gruppe, Umbruch
bei 900 px). Dazu ein Fall, der **jede Selektorzeile** des neuen Blocks daraufhin liest, dass sie
`.epos-formularraster` nennt — die Grenze der Regel ist damit selbst bewacht. In
`EPOS.UI.Tests/KatalogdialogTests` zwei weitere Fälle: der Eingabeblock der vier Katalogdialoge
trägt den Raster, und die Zahlenfelder des Modulkatalogs sind kurze Felder mit der Einheit in
ihrer Feldzeile.

**Testzahlen.** `EPOS.UI.Tests` **2530** grün (de‑DE und `LANG=en_US.UTF-8`); nach dem Merge
von `origin/ios_migration` (Stromganglinien-Import, W12) **2546** grün in beiden Sprachen.
`Werkzeuge/Formularkarte` **122** grün. `StilblattTests`, `ParametersatzTests`,
`KatalograhmenTests` und `KatalogdialogTests` unverändert grün — die Klammerbilanz des
Stilblatts hält auch nach dem Merge, der 14 Zeilen in einen anderen Block geschrieben hat.

**Abnahmepunkte am Gerät** (100 / 125 / 150 %):

1. „Administration Photovoltaik Module" öffnen: Im Block „Moduldaten" steht jede Beschriftung
   **links neben** ihrem Feld, nicht darüber.
2. „Nennleistung (Pmax)", „Wirkungsgrad", „Spannung im MPP": Das Eingabefeld ist **kurz**, und
   das „W" / „%" / „V" steht **unmittelbar dahinter**.
3. Der Block „Moduldaten" ist **ohne Rollen** ganz zu sehen; bei breitem Fenster stehen **zwei**
   Feldpaare nebeneinander.
4. „Beschreibung" ist zweizeilig und geht über **beide** Spalten.
5. Das Fenster schmal ziehen (< 900 CSS-px): Die Beschriftung fällt wieder **über** das Feld,
   nichts wird abgeschnitten.
6. Dasselbe in „Administration Stromspeicher", „Administration Heizkessel", „BHKW Verwaltung",
   „Administration Solarkollektoren", „Administration Pufferspeicher".
7. Die drei Stichproben: Heizkessel-Projektdialog (Detailblock unter der Zweispaltenauswahl),
   „Gebäude" (Detailblock „Verbrauch"), „Einstellungen" (Reiter „Datenbank" und „Web").
8. Ein Feld **außerhalb** eines Katalogdialogs — z. B. „Tarifstruktur" — sieht **unverändert**
   aus; dort steht die Beschriftung weiter über dem Feld, bis Paket P2 läuft.

## Windows-Abnahme 05.09.2026 — Formularraster, Paket P1 (iU8‑E‑2)

Paket **P1 — Erzeuger und Kataloge** aus der Pakettabelle weiter oben ist gelaufen. Die
beiden W14a-Dialoge (`ModulKatalogDialog`, `KatalogBrowserDialog`) trugen den Raster schon
seit dem Vortagsschritt; **neu** sind die zwölf Dialoge der Wellen **W6** und **W7** — die
Tabellen dazu stehen in `iU9_W6_Blazor_Port_Protokoll.md` und
`iU9_W7_Blazor_Port_Protokoll.md`, je Datei nur die eigenen. Hier steht, was **hausweit**
dazugekommen ist.

**Elf Dateien umgestellt, eine bewusst nicht.**

| Welle | Umgestellt | Felder im Raster |
|---|---|---|
| W6 | `HeizkesselKatalogDialog`, `BhkwKatalogDialog`, `BhkwDialog`, `PhotovoltaikDialog`, `StromspeicherDialog`, `PufferspeicherDialog` | 65 |
| W7 | `KennlinienEditorDialog`, `WaermepumpeStammDialog`, `WaermepumpeAnlageDialog`, `SolarkollektorKatalogDialog`, `SolarkollektorenDialog` | 52 |

Damit stehen **19** der 92 Dateien im Formularraster (8 aus dem Vorschritt, 11 aus P1); in
`EPOS.UI/Dialoge/Erzeuger/`, `…/Solarthermie/` und `…/Waermepumpe/` gibt es **kein**
handgebautes `epos-feldpaar` mehr. Die verbliebenen fünfzehn Stellen liegen in Bedarf,
Kosten und Strom — Sache der Pakete P2 und P3.

**Eine Regel kam dazu — Unterblock „Formularraster — Paket P1" am Ende von `epos-ui.css`:**

```
.epos-formularraster > .epos-herleitung { grid-column: 1 / -1; }
```

Ein Parameterblock ist selten NUR eine Reihe Felder: Zwischen den Feldern stehen Sätze, die
den Rechenweg nennen — „der abgeleitete Wert je kWel", der Rücklaufvorschlag der Wärmepumpe,
der Hinweis zur Spitzenlast. Eine `Herleitungszeile` ist **kein** Feld und darf deshalb nicht
in einer Rasterzelle **neben** einem Feld landen; sie gehört über die volle Breite, genau wie
`.epos-untergruppe` eine Regel weiter oben. Ohne diese Zeile hätten allein `BhkwKatalogDialog`
und `WaermepumpeAnlageDialog` ihre Blöcke in fünf statt zwei Raster zerlegen müssen — und
jede Zerlegung ist eine Stelle, an der die Beschriftungsspalten später auseinanderlaufen. Die
Regel nennt `.epos-formularraster` und greift damit nirgends sonst: Eine Herleitungszeile
außerhalb eines Rasters bleibt unverändert. Der Wächterfall
`Ausserhalb_des_Rasters_bleibt_ein_Feld_unveraendert` liest den Unterblock mit und hält das
fest.

**Der `Einspaltig`-Rückweg ist zum ersten Mal fachlich in Gebrauch** — im Block „Spitzenlast"
von `WaermepumpeAnlageDialog`: eine Regel, die sich von oben nach unten aufblättert. Zwei
Spalten stellten den Schalter „Sperrzeit" neben den Schalter „Heizstab" und rissen die
Reihenfolge auseinander. Einspaltig heißt nicht „wie vorher": Die Beschriftung steht auch
dort neben dem Feld.

**Zwei Blöcke bleiben bewusst, wie sie sind** (Klasse‑B‑Entscheid, Begründung in den
Wellenprotokollen):

* die **Filterzeile** von `WaermepumpenKatalogDialog` (`epos-zahlenraster`, zwölf Filter über
  einer Liste) — Werkzeuge der Liste, keine Parameter eines Geräts; eine 12‑rem-Spalte je
  Filter machte das Band doppelt so breit und die Liste kürzer;
* der **`Zeilenraster`** und die **Neuzeile** von `KennlinienEditorDialog` — Bearbeitungszeilen
  einer Tabelle. Ebenso bleiben die Listenfilter über den Kataloglisten (BHKW, PV,
  Pufferspeicher) und die Summenzeile unter der BHKW-Projektliste außerhalb des Rasters.

**Grenzfall, nicht angefasst:** `PufferSpProjektDialog` steht in der P1-Liste der Aufgabe,
trägt im Kopf aber die Welle **iU9‑W10a.4** und fällt damit in den Wellenbereich von Paket
**P3** (W8–W16a). Er ist hier bewusst **nicht** umgestellt, damit ihn nicht zwei Pakete
gleichzeitig anfassen; sein Block „Eigenschaften" ist mit der neuen Herleitungszeilen-Regel
jetzt in **einem** Raster darstellbar.

**Testzahlen.** `EPOS.UI.Tests` **2557** grün (de‑DE und `LANG=en_US.UTF-8`) — elf neue
Fälle, je einer in der vorhandenen Testdatei des umgestellten Dialogs.
`Werkzeuge/Formularkarte` **122** grün. `FormularrasterTests`, `ParametersatzTests`,
`StilblattTests` und `KatalogdialogTests` unverändert grün; die Klammerbilanz des Stilblatts
hält (757 / 757).

**Abnahmepunkte am Gerät** (100 / 125 / 150 %) stehen je Welle in `iU9_W6_…` (fünf Punkte)
und `iU9_W7_…` (sieben Punkte). Übergreifend gilt: Ein Feld **außerhalb** dieser neunzehn
Dateien — z. B. „Tarifstruktur" (P2) oder „Gebäude-Katalog" (P3) — sieht **unverändert** aus.

### Nachtrag 05.09.2026 — `Textfeld.Kurz` und `ErzeugerDetail.IstZahl`

Zweite Meldung des Anwenders am selben Tag, mit Bildschirmfoto „Verwaltung BHKW": „Stelle
diesen Dialog kompakter dar (insbesondere Daten zum BHKW-Modul unten). Prüfe das Gleiche
mit anderen Komponenten zur Darstellung der Komponentendaten."

Der Befund betrifft nicht das Raster, sondern die **Selbstmeldung eines Feldes**. Der
Komponentenblock der sechs Erzeuger-Projektmasken zeichnet seine Werte als NUR LESBARE
`Textfeld` — die Paare (Beschriftung, Wert) kommen fertig formatiert aus der Hülle. Ein
`Textfeld` konnte sich bis dahin nur als **lang** melden (`Mehrzeilig` →
`epos-feld--breit`), nie als kurz; also nahm „80" hinter „thermische Leistung [kWth]:" die
volle Feldspalte.

**Zwei hausweite Ergänzungen, keine neue Datenform:**

* **`Textfeld.Kurz`** setzt `epos-feld--kurz` — dieselbe Klasse, dieselbe Regel im
  Stilblatt wie beim `Zahlenfeld`, kein neues CSS. Sie greift wie alles andere **nur** im
  Raster; ein Textfeld ohne `Kurz` bleibt unverändert, ein **mehrzeiliges** bleibt lang,
  auch wenn jemand beides setzt (die Länge hängt am Bauteil, nicht am Aufrufer).
* **`ErzeugerDetail.IstZahl(wert)`** entscheidet, WELCHES Anzeigefeld kurz ist — an EINER
  Stelle für alle sechs Masken, die diesen Block zeichnen. Die Probe hängt am **Wert**:
  Die Feldnamen kommen je Erzeugerart anders herein (`Hersteller:`, `Nennweite:`,
  `Aperturfläche:`), eine Zahl bleibt eine Zahl. Beide Kulturen werden gefragt, weil die
  Hülle in der laufenden Kultur formatiert; rät die Probe falsch, ändert sich nur die
  **Breite** eines Anzeigefeldes. Die Alternative — ein drittes Element im Tupel
  `(Feld, Wert)` — hätte sechs Windows-Hüllen und ihre Feldkartenproben angefasst, um
  dieselbe Auskunft zu transportieren.

Betroffen sind neun Dateien: die sechs Detailblöcke (`BhkwDialog`, `HeizkesselDialog`,
`PhotovoltaikDialog`, `StromspeicherDialog`, `PufferspeicherDialog`,
`SolarkollektorenDialog`), dazu vier einzeln benannte Anzeigewerte — die BHKW-Summenzeile,
die PV-Gesamtleistung, die Gesamt-Aperturfläche der Solarthermie und Baujahr/Nennleistung
der Wärmepumpenanlage. Die Aufteilung je Block steht in den Wellenprotokollen W6 und W7.

**Wachen.** Vier Fälle in `FormularrasterTests` (Kurz meldet sich, Gegenprobe ohne Kurz,
mehrzeilig bleibt lang, und `IstZahl` mit acht Werten) und ein Fall in `BhkwDialogTests`,
der am Beispiel des Anwenders prüft, dass die beiden Leistungen kurz sind, der Hersteller
nicht und die Beschreibung breit. **Testzahlen nach dem Nachtrag:** `EPOS.UI.Tests`
**2569** grün in beiden Sprachen, `Werkzeuge/Formularkarte` **122** grün.

**Vermerkt, nicht gemacht:** Die Spalte „Eigenschaften" der BHKW-Katalogliste bricht in
vier Zeilen um. Sie kommt nicht aus `Zweispaltenauswahl` und aus keinem Listenprofil,
sondern wird in `Views/BHKW/BhkwHuelle.KatalogZeilen` aus `\n`-getrennten Stücken gebaut —
ein eigener Umbau in der Windows-Hülle, von `EPOS.UI.Tests` aus nicht prüfbar. Begründung
im Protokoll W6.

---

## Anwenderwunsch W14a‑E‑8 (06.09.2026) — Parameterübersicht mit Verwendungskennzeichnung

> „Für alle Menüs mit Anlagendaten: Erstelle einen Bearbeiten-Dialog zusätzlich im
> Bearbeiten-Menü (optionale Anzeige), in dem 1. alle verfügbaren Parameter und
> Eigenschaften angezeigt werden und 2. alle verwendeten Parameter gekennzeichnet sind.
> Alle verwendeten Parameter und Eigenschaften sollen direkt unter Bearbeiten angezeigt
> werden → prüfe."

Gemeint sind die sieben Katalogverwaltungen unter **Administration**: Heizkessel, BHKW,
Wärmepumpe, Solarkollektoren, PV-Module, Stromspeicher und Pufferspeicher. Vorbild für
die optionale Anzeige ist der Aufklapper „Alle Modulparameter anzeigen" aus **W6‑E‑1**
im PV-Projektdialog.

### 1 Eine Wahrheit im Kern: `ParameterVerwendung`

`EPOS.Kern/Allgemein/Katalog/ParameterVerwendung.cs` nennt für **jede** Spalte der sieben
Stammtabellen ihre Verwendung und **belegt jede Einstufung mit Datei und Zeile**. Die
Beschriftungen holt der Katalog über denselben Übersetzer und dieselben
Ressourcenschlüssel wie `KatalogBrowserProfil` und `ModulKatalogProfil` — ein zweiter
Text liefe beim ersten Fachwechsel auseinander.

Die fünf Stufen sind trennscharf definiert:

| Stufe | Wer liest den Wert |
|---|---|
| `Simulation` | `EPOS.Kern/Allgemein/Simulation/**`, `SpeicherEngine`, `Controller/StromspeicherSimCtrl` |
| `Wirtschaftlichkeit` | `Allgemein/Wirtschaftlichkeit/**`, `Bericht/KostenEmissionRechner`, `Controller/TechnikPlanwertCtrl` |
| `Bericht` | `Allgemein/Bericht/AbweichungsErmittler.Felder` — der einzige Ort, der Gerätespalten namentlich in den Bericht trägt |
| `Dialog` | nur Anzeige oder Pflege in einer Maske |
| `Keine` | kein Leser: weder Rechenweg, noch Bericht, noch Maske |

**Die Zählung je Anlagenart** (128 Spalten; eine Spalte kann mehrere Stufen tragen,
`Keine` steht allein):

| Anlagenart | Stammtabelle | Spalten | Simulation | Wirtschaftl. | Bericht | Dialog | Keine |
|---|---|---:|---:|---:|---:|---:|---:|
| Heizkessel | `Tab_Heizkessel_STAMM` | 23 | 9 | 7 | 6 | 9 | 0 |
| BHKW | `Tab_BHKW_STAMM` | 27 | 12 | 11 | 8 | 5 | 0 |
| Wärmepumpe | `Tab_WP_STAMM` | 20 | 5 | 1 | 8 | 4 | **5** |
| Solarkollektoren | `Tab_Solarkollektoren_STAMM` | 16 | 7 | 2 | 3 | 7 | 0 |
| Photovoltaik | `Tab_PV_STAMM` | 19 | 9 | 2 | 5 | 8 | 0 |
| Stromspeicher | `Tab_Stromspeicher_STAMM` | 15 | 9 | 8 | 4 | 1 | 0 |
| Pufferspeicher | `Tab_Pufferspeicher_STAMM` | 8 | 5 | 2 | 3 | 2 | 0 |
| **Summe** | | **128** | **56** | **33** | **37** | **36** | **5** |

**Die gerechneten Spalten im Wortlaut** (Simulation oder Wirtschaftlichkeit; `ID` ist der
Fremdschlüssel aus `Tab_Energieanlagen`, über den der Rechenweg das Gerät findet):

| Anlagenart | gerechnet | Spalten |
|---|---:|---|
| Heizkessel | 12 | `ID`, `Bezeichner`, `Ptherm`, `Brennstoff`, `Wirkungsgrad_Gas`, `Wirkungsgrad_Öl`, `Investitionskosten`, `Wartungskosten`, `Wartungskosten_Einheit`, `Betriebsbereitschaftverlust`, `Vorlauf`, `Ruecklauf` |
| BHKW | 18 | `ID`, `Bezeichner`, `Ptherm`, `Pel`, `Brennstoff`, `Wirkungsgrad`, `Grenzleistung`, `Wartungskosten_kwhel`, `Kosten_Modul`, `Kosten_Montage`, `Kosten_Lieferung`, `Kosten_Schallschutzhaube`, `Kosten_Abgasreinigung`, `CO2`, `SO2`, `NOX`, `CO`, `Staub` |
| Wärmepumpe | 6 | `ID`, `Bezeichner`, `Typ`, `Nennleistung`, `Heizung`, `Modulkosten` |
| Solarkollektoren | 8 | `ID`, `Bezeichner`, `Aperturflaeche`, `h0`, `k1`, `k2`, `Kdir`, `Investitionskosten` |
| Photovoltaik | 10 | `ID`, `Bezeichner`, `Leistung`, `Wirkungsgrad`, `gamma_PMP`, `T_NOCT`, `Laenge`, `Breite`, `Modulkosten`, `Technologie` |
| Stromspeicher | 13 | `ID`, `Bezeichner`, `Leistung`, `Energie`, `Ladezustand`, `Degradation`, `Modulkosten`, `Wirkungsgrad_RT`, `Zyklen_Zugesichert`, `Verschleisskosten`, `Leistungskosten`, `Investition_Fix`, `Standby_Verbrauch` |
| Pufferspeicher | 6 | `ID`, `Bezeichner`, `Speichertyp`, `Bereitschaftsverluste`, `Gesamtvolumen`, `Investitionskosten` |

### 2 Drei Befunde, die beim Vermessen herausfielen

**W14a‑E‑8‑B1 — Die Emissionsspalten des Heizkessels rechnen nicht mit.** Der
Katalogeditor pflegt `CO2`, `SO2`, `NOx`, `CO` und `Staub` in g/MWh; der Rechenweg liest
sie **nie**. `SimulationSPK.Kesseldaten_Einlesen` (:151‑158) holt die Emissionsfaktoren
zum Brennstoff aus `Tab_Brennstoff_Stamm` — fünf gepflegte Zahlen ohne Wirkung. Beim
BHKW ist es umgekehrt: Dort liest `SimulationBHKW.Moduldaten_Einlesen` (:315‑319) genau
diese fünf Spalten des Geräts. Zwei Masken, die gleich aussehen, und ein Unterschied,
den bis hierher nichts anzeigte. Die Übersicht zeigt ihn jetzt: „nur Anzeige" beim
Kessel, „Simulation" beim BHKW.

**W14a‑E‑8‑B2 — Fünf Spalten der Wärmepumpe hat kein Leser.** `Laenge`, `Breite`, `Hoehe`,
`Gewicht` und `Raum` stehen in `Tab_WP_STAMM`, werden vom VDI‑3805‑Import gefüllt, von
`WPCtrl` ins Projekt kopiert — und im ganzen Bestand nirgends gelesen. Sie sind die
einzigen fünf Spalten aller sieben Kataloge mit der Stufe `Keine`.

**W14a‑E‑8‑B3 — `Investition_kwel` des BHKW ist eine Dublette ohne Leser.** Seit dem
Nutzerentscheid vom 22.08.2026 wird der Wert aus den fünf Kostenposten ABGELEITET
(`BHKWKosten.JeKWel`) und schreibgeschützt angezeigt; die Kostenplanung rechnet mit
`Kosten_Modul` und den vier Nebenposten (`TechnikPlanwertCtrl.cs:317‑325`). Er bleibt —
als Anzeigewert ist er richtig —, trägt aber die Stufe „nur Anzeige".

### 3 Die optionale Anzeige: `Parameteruebersicht`

`EPOS.UI/Bausteine/Parameteruebersicht.razor` — **ein** Baustein für alle sieben
Ausprägungen in **drei** Wirten:

| Wirt | Ausprägungen |
|---|---|
| `Dialoge/Erzeuger/KatalogBrowserDialog` | Heizkessel, BHKW, Solarkollektoren, Pufferspeicher |
| `Dialoge/Erzeuger/ModulKatalogDialog` | Photovoltaik, Stromspeicher |
| `Dialoge/Waermepumpe/WaermepumpeStammDialog` | Wärmepumpe |

Der Block steht **unter dem Bearbeiten-Formular**, im Eingabeteil des `Katalograhmen`,
und ist **zugeklappt** — der Dialog sieht aus wie vorher, bis der Anwender ihn öffnet;
der Zustand hält die Dialogsitzung durch, auch über einen Satzwechsel hinweg (Regel aus
W6‑E‑1). Aufgeklappt steht je Katalogeintrag eine Zeile: **Anzeigetext**, **Wert mit
Einheit** (Halbgeviertstrich „–" bei NULL) und die **Kennzeichnung der Verwendung**.

**Vier Entscheidungen dahinter:**

* **Der Knopf ist der aus W6‑E‑1** (`.epos-modulparameter-knopf`, `aria-expanded`,
  Pfeil voran) — eine Form für denselben Zweck, keine zweite Stilregel.
* **Eine Tabelle statt eines Formularrasters.** W6‑E‑1 zeigt zwei Angaben je Zeile und
  kommt mit lesenden Standardfeldern aus. Hier sind es drei, und die Kennzeichnung muss
  sich über alle Zeilen hinweg vergleichen lassen („welche Werte rechnen eigentlich
  mit?"). Die Tabelle steht in `.epos-raster-huelle` und erbt damit die Höchsthöhe aus
  **W9‑B‑2** samt stehendem Spaltenkopf — 27 Zeilen (BHKW) dürfen den Eingabeblock nicht
  ins Endlose schieben.
* **Die Kennzeichnung trägt Text**, nicht nur Farbe: Jedes Chip nennt seine Stufe im
  Klartext, das Sinnbild davor ist `aria-hidden`, „nicht verwendet" ist gestrichelt und
  nicht rot (es ist kein Fehler, sondern eine Auskunft), und `forced-colors` ist
  abgesichert.
* **Die neun Texte füllt der Baustein selbst** aus `MyResource` (Bauart `LizenzTexte`,
  W15c‑O‑2). Drei Wirte mit je neun Parametern wären siebenundzwanzig Stellen, an denen
  ein Schlüssel fehlen kann.

**Die Datenseite** ist `EPOS.Kern/Controller/ParameterUebersichtCtrl` — EIN Lesevorgang je
Satz (`SELECT * FROM [<Stammtabelle>] WHERE Bezeichner = ? ORDER BY ID`), gepaart mit dem
Verwendungskatalog. `SELECT *` mit Bedacht: Der Spaltensatz wächst mit den
Migrationsschritten (die Testdatenbank steht auf 61, das Programm auf 64), eine
namentliche Liste ließe die Übersicht auf einer älteren Datenbank mit „no such column"
scheitern — dieselbe Überlegung wie in `StromspeicherSimCtrl` (:1046) und
`ProjektDetails` (:118). Sieben Detail-Lader um je zehn Spalten zu erweitern hätte
dieselbe Erweiterung siebenmal zu pflegen bedeutet. **SQL-Prüfer: 1213 Texte, 0
Fundstellen.**

**Nicht gemacht:** `PufferSpKatalogDialog` bekommt keine zweite Übersicht. Er ist der
Editor, den der Pufferspeicher-Browser als Überlagerung öffnet — und der Browser trägt
den Aufklapper bereits. Zwei Kopien derselben Auskunft im selben Fenster wären keine
zweite Auskunft.

### 4 Teil 3 der Prüfung: „steht jeder verwendete Parameter im Bearbeiten-Formular?"

Verglichen wird je Anlagenart die Menge der **gerechneten** Spalten (Simulation oder
Wirtschaftlichkeit) gegen die Menge der Spalten, die der Speicherweg der Verwaltung
zurückschreibt. `ID` und `ReadOnly` bleiben außen vor: Eine Eingabemöglichkeit wäre dort
ein Fehler, kein Fortschritt.

| Anlagenart | gerechnet (ohne `ID`) | im Formular | Lücke |
|---|---:|---:|---|
| Heizkessel | 11 | 21 | — |
| BHKW | 17 | 24 | — |
| Wärmepumpe | 5 | 10 | **`Modulkosten`** |
| Solarkollektoren | 7 | 14 | — |
| Photovoltaik | 9 | 15 | — |
| Stromspeicher | 12 | 13 | — |
| Pufferspeicher | 5 | 6 | — |

**Eine einzige Lücke** — und sie ist keine Nachlässigkeit, sondern ein Widerspruch
zwischen zwei Entscheiden:

> **W14a‑O‑1 — Entschieden 06.09.2026 (Empfehlung angenommen), umgesetzt in `83e8490`**
> — `Tab_WP_STAMM.Modulkosten` geht in die Kostenplanung
> (`TechnikPlanwertCtrl.BasenFuellen`, Fall `ERZEUGER_WAERMEPUMPE`, :345: Basis
> „Modulpreis"), ist in der Wärmepumpenverwaltung aber **nicht zu pflegen**: Entscheid
> **Ä19** der Welle 7 nahm das Feld aus der Maske („Gerätekosten laufen über die
> Kostenverwaltung"), der Wert läuft seither nur verborgen im Datensatz mit und wird
> allein vom VDI‑3805‑Import gefüllt. Bei einer Neuanlage steht er auf 0, und die
> Kostenübernahme schlägt dann **keinen** Planwert vor.
> **Vorschlag:** Das Feld im Stammdialog wieder zeigen — als **nur lesenden** Wert mit
> einer Herleitungszeile „aus dem Herstellerimport; Gerätekosten werden in der
> Kostenverwaltung gepflegt". Damit bleibt Ä19 gewahrt (niemand tippt hier Kosten ein),
> und der Anwender sieht, woher der Planwert kommt bzw. warum keiner kommt. Die
> Alternative — das Feld editierbar machen — kehrt Ä19 um und gehört deshalb dem
> Anwender, nicht dem Umbau.
> *Nicht eigenmächtig gebaut* (Regel: unklare Semantik → offener Punkt mit Vorschlag).

**Was gebaut wurde** (`83e8490`) — der Vorschlag wortgetreu, nicht mehr:

* **Ein Lesewert, kein Feld.** Im `Formularraster` des Stammdatenblocks steht hinter
  „Kühlleistung" eine Rasterzelle aus `.epos-feld-text` und `.epos-feld-zeile` mit einem
  `<span class="epos-lesewert">` und der Einheit dahinter — **kein `<input>`**, auch
  kein gesperrtes. Ein `disabled`-Feld sagte „hier könntest du tippen, nur jetzt nicht"
  und verspräche eine Pflege, die es nicht gibt; ein `readonly`-Feld bliebe im
  Tabulatorweg. Die neue Stilklasse `.epos-lesewert` ist der allgemeine Fall dafür
  (Tabellenziffern, kein Rahmen, kein Fokusring) und steht in `epos-ui.css` neben
  `.epos-eingabe`/`.epos-einheit`, wo sie hingehört.
* **Die Einheit ist Euro, nicht €/kW.** `TechnikPlanwertCtrl.BasenFuellen` nimmt den
  Wert im Fall `ERZEUGER_WAERMEPUMPE` **unverändert** als Basis „Modulpreis" — anders
  als bei Photovoltaik (Preis je Modul × Stückzahl) und Stromspeicher (€/kWh × kWh)
  wird hier nichts multipliziert. Es ist ein Betrag **je Gerät**; der
  Verwendungskatalog führt für diese Spalte dieselbe Einheit „€".
* **Die Herleitungszeile steht immer da.** Ihre zweite Hälfte ist die Auskunft aus Ä19:
  „aus dem Herstellerimport; **Gerätekosten werden in der Kostenverwaltung gepflegt**" —
  wer hier Kosten pflegen will, findet den Weg dorthin, statt ein fehlendes Feld zu
  suchen. Englisch: „from the manufacturer import; equipment costs are maintained in
  cost management" (`WPS_HERL_MODULKOSTEN`).
* **0 und NULL sind dasselbe, und beides heißt „kein Planwert".** Die Spalte ist
  `INTEGER`, ein NULL kommt als 0 im Datensatz an (`WPStammCtrl`:677), und
  `TechnikPlanwertCtrl.Basis` verwirft Beträge ≤ 0 („0 gilt als ungepflegt und erzeugt
  nie eine Scheinauswahl"). Die Maske zeigt deshalb den **Halbgeviertstrich ohne
  Einheit** und darunter eine zweite leise Zeile „kein Planwert aus dem
  Herstellerimport" / „no planned value from the manufacturer import"
  (`WPS_HINWEIS_MODULKOSTEN_LEER`). Genau der Fall der Neuanlage, um den es ging.
* **Eine Zahl, zwei Wege — dieselben Ziffern.** Der Anzeigetext ist die rohe Zahl in der
  Kultur des Anwenders ohne Tausenderpunkt, wortgleich zu `ParameterUebersichtCtrl.Anzeige`;
  die **Beschriftung** kommt aus `MODK_LBL_MODULKOSTEN`, also aus demselben Schlüssel,
  den `ParameterVerwendung.Waermepumpe` führt. Wer Raster und Aufklapper
  nebeneinanderlegt, liest denselben Namen und denselben Wert. **Ein Unterschied bleibt
  mit Absicht:** Beim Wert 0 schreibt der Aufklapper „0" — er zeigt die Spalte roh —,
  das Raster den Strich samt Grund; dort geht es nicht um den Zellinhalt, sondern um den
  Planwert, und der ist bei 0 keiner.
* **Ä19 ist unberührt geblieben**, ebenso Rechenweg, SQL und Speicherweg: Der Wert wird
  im `Speichern` der Hülle wie bisher nur durchgereicht (Abweichung **A‑14** bleibt —
  es gibt nichts zu prüfen, was der Anwender eingegeben hätte). Die
  **Teil‑3‑Lücke bleibt deshalb bestehen und der Kern-Fall
  `Genau_eine_gerechnete_Spalte_fehlt_im_Bearbeiten_Formular` unverändert**: Er zählt
  die Spalten, die das Formular ZURÜCKSCHREIBT, und ein Lesewert ist keine Pflege.
  Angepasst sind nur die Erläuterungen — im Verwendungskatalog, in `WaermepumpeStammDaten`
  und in den beiden Doc-Kommentaren der Kern-Probe.
* **Sieben neue Fälle** in `EPOS.UI.Tests/Dialoge/WaermepumpeStammDialogTests`: Wert mit
  Einheit im Raster, **kein `input`/`textarea`/`select`/`button`** in der Zelle (und
  weiterhin genau fünf `input` im Block), Herleitungszeile deutsch und englisch, 0 → „–"
  ohne Einheit samt zweiter Zeile, die Neuanlage, und der Vergleich mit dem Aufklapper.
* **Nachweise** — `EPOS.UI` und `EPOS.Kern` 0 Fehler und **keine neue Warnung** (die
  fünf bekannten des Kerns, eine bekannte des Windows-Baus); `EPOS.UI.Tests` **2 731**,
  `EPOS.Kern.Tests` **1 344**, beide grün unter `de_DE.UTF-8` **und** `en_US.UTF-8`;
  Designer-Prüfung „abweichend 0" (4 895 Einträge, +2); SQL-Prüfer 1 213 Texte /
  **0 Fundstellen** (Kontrolle — es ist kein SQL angefasst).

**Zwei Beobachtungen ohne Lücke, aber mit Aussage:**

* `Tab_WP_STAMM.maxPtherm` und `Kuehlleistung` stehen im **Bericht**
  (`AbweichungsErmittler.cs:80/81`), sind in der Verwaltung aber nicht editierbar
  (`maxPtherm` läuft verborgen mit, `Kuehlleistung` steht `Aktiv="false"` da). Keine
  Rechenlücke — die Übersicht sagt es jetzt aber.
* `Tab_PV_STAMM.alpha_SC` und `beta_OC` sind die einzigen zwei Spalten, die ein
  Katalogeditor nicht pflegen kann; sie kommen ausschließlich aus dem CEC-/PAN-Import.
  Da sie **nur Anzeige** sind (das erweiterte PV-Modell rechnet mit `Leistung`,
  `gamma_PMP`, `T_NOCT` und `Technologie`), ist das keine Lücke im Sinne von Teil 3.

### 5 Nachweise

* **Kern** — `EPOS.Kern.Tests/ParameterVerwendungTests`, **42 Fälle**. Geprüft wird nicht
  gegen eine zweite Liste im Testcode, sondern gegen die **Datenbank**:
  `DataRepository.SpaltenVonTabelle` gegen den Katalog, je Anlagenart *keine vergessene
  und keine erfundene Spalte* und dieselbe Reihenfolge; dazu die **Belegpflicht** (jede
  gerechnete Spalte nennt eine Fundstelle), die Stichproben aus Rechenweg und
  Kostenseite, die drei Befunde aus § 2 und der **Teil‑3‑Vergleich**
  (`Genau_eine_gerechnete_Spalte_fehlt_im_Bearbeiten_Formular`) samt Gegenprobe, dass
  keine Formularliste eine Spalte nennt, die es nicht gibt.
* **Oberfläche** — `EPOS.UI.Tests/Bausteine/ParameteruebersichtTests`, **17 Fälle**:
  Aufklapper zu/auf/zu, Zeilenzahl = Katalogeinträge je Ausprägung (4 + 2 + 1),
  Kennzeichnung als Text mit `aria-hidden`-Zeichen, „nicht verwendet" fünfmal bei der
  Wärmepumpe, „–" bei NULL, Einheit hinter dem Wert, englische Kultur, leere Liste, und
  **ohne Delegat kein Aufklapper**.
* **Testzahlen** — der Wunsch bringt **+42** Kern- und **+17** Oberflächenfälle; nach der
  Zusammenführung mit `ios_migration` (iF30, Lizenz-Schreibnaht) steht die Mappe bei
  `EPOS.Kern.Tests` **1 344**, `EPOS.UI.Tests` **2 721**, `KiKern.Tests` 469,
  `SpeicherEngine.Tests` 337, `Werkzeuge/Formularkarte` 122 — grün unter
  `de_DE.UTF-8` **und** `en_US.UTF-8`. Beide Kern-Wächter leer, Windows-Bau
  (`-p:Platform=x64`) 0 Fehler, SQL-Prüfer 1 213 Texte / 0 Fundstellen,
  Stilblatt-Wache grün (Klammerbilanz, kein Nesting), Designer-Prüfung
  „abweichend 0".
* **Rechenweg unberührt** — es ist keine Zeile in `Allgemein/Simulation/**` oder
  `Allgemein/Wirtschaftlichkeit/**` angefasst; der Verwendungskatalog LIEST diese
  Dateien nur als Fundstellenangabe.

### Abnahmepunkte A‑W14a‑E‑8 (100 / 125 / 150 %)

1. **Administration → Wärmebedarf und Heizung → Kessel**: Unter dem Eingabeblock steht
   die Zeile „▸ Alle Parameter und ihre Verwendung anzeigen". Draufklicken: 23 Zeilen,
   Kopf „Parameter | Wert | Verwendung".
2. In derselben Tabelle: `Ptherm` trägt drei Kennzeichen (Simulation, Wirtschaftlichkeit,
   Bericht), `CO2` nur „nur Anzeige", `Nutzungsdauer` ebenfalls „nur Anzeige".
3. Einen anderen Kessel in der Liste wählen: Die Werte wechseln, der Block **bleibt
   offen**. Erneut klicken schließt ihn.
4. Dasselbe in den fünf übrigen Verwaltungen — die Menüwege nach der Umordnung
   **W16c‑E‑6** vom 06.09.2026: *Wärmebedarf & Heizung →* **BHKW** (27 Zeilen; `CO2`
   trägt hier „Simulation" — der Unterschied zum Kessel aus § 2) und
   **Solarkollektoren** (16), *Energiesysteme →* **Photovoltaik** (19) und
   **Pufferspeicher** (8), *Strombedarf & Stromspeicher →* **Stromspeicher** (15).
5. **Administration → Wärmebedarf und Heizung → Wärmepumpe**: 20 Zeilen; `Laenge`,
   `Breite`, `Hoehe`, `Gewicht` und `Raum` tragen „nicht verwendet" (gestrichelter
   Rahmen), `Modulkosten` trägt „Wirtschaftlichkeit" — der entschiedene Punkt
   W14a‑O‑1, Punkt 9.
6. Eine Anlage ohne gepflegte Werte wählen (oder „Neu…" drücken): Die leeren Spalten
   zeigen „–" und nicht „0".
7. Fenster schmal ziehen (< 900 CSS-px): Die Tabelle rollt in sich, die Seite nicht.
8. Auf **English** umschalten: „Show all parameters and how they are used",
   „Parameter | Value | Used for", „Simulation / Economics / Report / display only /
   not used".
9. **W14a‑O‑1 am Gerät** — *Administration → Wärmebedarf und Heizung → Wärmepumpe*:
   Im Block „Wärmepumpe" steht unter „Kühlleistung" die Zeile **„Modulkosten"** mit dem
   Wert und „€" dahinter, darunter leise „aus dem Herstellerimport; Gerätekosten werden
   in der Kostenverwaltung gepflegt". **In das Feld lässt sich nicht klicken und nicht
   tabben** — es ist Text, kein Eingabefeld. Ein Gerät ohne gepflegten Wert wählen
   (oder **„Neu"** drücken): Dort steht **„–"** ohne Einheit und eine zweite leise
   Zeile „kein Planwert aus dem Herstellerimport". Den Aufklapper öffnen: Die Zeile
   `Modulkosten` zeigt bei einem gepflegten Gerät **dieselbe Zahl** wie das Raster
   (beim Wert 0 dort „0", im Raster der Strich — so gewollt). „Speichern" drücken und
   den Satz erneut wählen: Der Wert steht unverändert da. Auf **English**: „Module
   costs", „from the manufacturer import; equipment costs are maintained in cost
   management", „no planned value from the manufacturer import".
