# iU9 Welle 14b — Bedarfs-Admin: Portprotokoll

> Stand 04.09.2026. Vermessung: `iU9_W14ab_Vermessung.md` § 8–11, § 12.2, § 13, § 14.3, § 15.
> Vorgänger: [`iU9_W13_Blazor_Port_Protokoll.md`](iU9_W13_Blazor_Port_Protokoll.md).
> Basis: `29aecbc` (`ios_migration` nach W13), dazu der Einheiten-Merge `fbea453`
> (Anwenderentscheid W8‑O‑5/W9‑O‑3) und der Formularkarte-Fix `9cccfc1`.

---

## 1. Auftrag und Ergebnis

**Vier ruhende Verwaltungsmasken des Bedarfs — 670 Zeilen `.cs`, 937 Zeilen Designer,
11 `MessageBox`, 0 `MyResource` — werden ZWEI Razor-Komponenten und ZWEI Hüllen.**

| Vorher (WinForms) | Zeilen | Nachher (Razor) |
|---|---|---|
| `Views/Stromverbraucher/Form_Stromverbraucher_Admin` | 177 / 219 | `EPOS.UI/Dialoge/Bedarf/BedarfAdminDialog`, Ausprägung `Stromverbraucher` |
| `Views/Prozesswärme/Form_Prozesswaerme_Admin` | 177 / 220 | dieselbe Komponente, Ausprägung `Prozesswaerme` |
| `Views/Brauchwasser/Form_Brauchwasser_Admin` | 163 / 331 | dieselbe Komponente, Ausprägung `Brauchwasser` |
| `Views/Solarthermie/Form_Solarganglinie_Admin` | 153 / 167 | `EPOS.UI/Dialoge/Solarthermie/SolarganglinieAdminDialog` |

Hüllen: `Views/Bedarf/BedarfAdminHuelle.cs` (**eine** für drei Maskenschlüssel) und
`Views/Solarthermie/SolarganglinieAdminHuelle.cs`.

**Mit der Welle fallen außerdem:** `EPOS.Kern/Allgemein/ToolsClass.cs` (56 Z., letzter Nutzer),
`Sprungziel.SolarganglinieAdmin` samt dem `case`-Zweig der `Sprungbruecke`, und 18 Dateien der
vier Masken.

**Neu im Kern:** `Controller/BedarfsVorschauCtrl.cs`; erweitert sind `BedarfStammCtrl`
(`Bezeichner`, `Kopf`, `Loeschen`, dazu der Aufzählungstyp `BedarfLoeschErgebnis`) und
`SolarganglinieStammCtrl` (`Exists`, `HatProjektzuordnung`).

### Commits (11, auf `29aecbc` + `fbea453`/`9cccfc1`)

| # | Commit | Inhalt |
|---|---|---|
| 1 | `c06a8f1` | **W14b.0** — der Nebenbaum-Filter des Stapellaufs misst ab der Suchwurzel |
| 2 | `da992bc` | **W14b.0i** — der eingefrorene Nachweis (27 Fälle), Probe `solarganglinie_8760.txt` |
| 3 | `f64b7a0` | **W14b.0a** — `BedarfStammCtrl.Bezeichner`/`Kopf`/`Loeschen` (+3 Fälle) |
| 4 | `eb0b009` | **W14b.0b** — `BedarfsVorschauCtrl` (+4 Fälle) |
| 5 | `527b5b6` | **W14b.0c/0d** — `SolarganglinieStammCtrl.Exists`/`HatProjektzuordnung` (+3 Fälle) |
| 6 | `8cfed9a` | **W14b.0e** — Textkatalog, 46 Schlüssel de + en |
| 7 | `31fb5c2` | **W14b.1** — `BedarfAdminDialog`, drei Masken gelöscht |
| 8 | `57b47cd` | **W14b.2/3** — `SolarganglinieAdminDialog`, Maske und Sprungziel gelöscht |
| 9 | `13e88a6` | **W14b.4** — `ToolsClass` gelöscht |
| 10 | `cdab154` | **W14b.5** — Ressourcen und KI-Bereichstabelle nachgezogen |
| 11 | `cbc9662` | **W14b.6** — Formularkarte: Zeuge umgehängt, vier Schwellen |

---

## 2. Die drei Entscheidungen der Welle

### 2.1 Der „Ergebnisse"-Knopf gab es nie (Befund W14‑B78)

Die Vermessung nennt für die drei Drillinge **sieben Knöpfe**, darunter „Ergebnisse"
(`btn_ErgebnisseVerbrauch_Click`), und leitet daraus zwei offene Entscheide ab: **E‑7 /
W14‑B50** (der Knopf zeigt beim Brauchwasser eine andere Sicht als „Grafik") und **E‑8 /
W14‑B58** („Ergebnisse" ohne vorheriges „Grafik" zeigt Nullen).

**Beim Ziehen der vier Feldkarten fiel auf: Den Knopf gibt es in keinem der drei Designer.**
`grep "Ergebnis"` über die drei `.designer.cs` liefert null Treffer, ebenso über die `.resx`.
Der Handler stand in allen drei Masken, verdrahtet war er nirgends — er ist der Rest des
gleichnamigen Knopfes aus `Form_Prozesswaerme` (der PROJEKTmaske, W9.5), aus der die drei
Verwaltungen kopiert wurden.

**Damit sind E‑7 und E‑8 gegenstandslos**: Beide beschreiben das Verhalten eines Knopfes, den
niemand drücken konnte. Der Dialog baut ihn nicht nach (A‑4). Die sieben Knöpfe der Feldkarte
sind: „Grafik", „…neu", „…löschen", „Typ ändern", „…ändern", „OK", „Abbrechen".

### 2.2 Der Brauchwasser-Teiler ist kein Fehler mehr, sondern eine Einheit (E‑6)

`Form_Brauchwasser_Admin:83` rechnete `Waermebedarf_Brauchwasser = brauchwasserwerte.Sum()` —
**ohne** den Teiler 1000, den beide Zwillinge haben (Befund W14‑B49). Die Beschriftung daneben
nannte „MWth".

Der Anwenderentscheid **W8‑O‑5 vom 04.09.2026** (Merge `fbea453`) hat den Fall geschlossen,
bevor diese Welle ihn anfassen musste: Die Ergebnishülle nennt seither je Kennzahl die
EINHEIT, IN DER IHR WERT VORLIEGT (`Energieeinheit`), und die Anzeige rechnet um.
`Waermebedarf_Brauchwasser` ist als **kWh** deklariert, alles andere als MWh.

**Folge für W14b.0b:** `BedarfsVorschauCtrl` teilt beim Brauchwasser NICHT. Ein Teiler hier
würde die Zahl ein zweites Mal teilen. Der Test friert das ein: Die Summe der Stundenreihe
(742,9008) ist exakt das Tausendfache der Katalog-Jahressumme (0,7429 MWh).

### 2.3 Ein Löschweg und ein Rückfragetext für alle vier

Der Bestand hatte **vier verschiedene Löschwege** in vier Masken: ohne Leerprüfung
(Brauchwasser), mit `try/catch` und Erfolgsmeldung (Prozesswärme), schlicht (Stromverbraucher),
ganz ohne Rückfrage (Solarganglinie) — und denselben Satz „Soll X wirklich gelöscht werden ?"
in **drei Schreibweisen**. Die vier Komponenten teilen sich jetzt:

* Leerprüfung → Meldung (Text wörtlich je Ausprägung, das Brauchwasser bekommt sie neu),
* Sperren (Projektzuordnung, `ReadOnly`) → Warnbanner, ohne Rückfrage,
* `Rueckfrage` mit `PSP_MELDUNG_WIRKLICH_LOESCHEN` + `PSP_TITEL_LOESCHEN`,
* Auswertung des Rückgabewerts → Erfolgs- oder Warnbanner.

**Keine `MessageBox` bleibt übrig.** Die elf des Bestands werden acht `Warnbanner` und drei
`Rueckfrage`n.

---

## 3. Bauweise

### 3.1 Drei Masken, eine Komponente, drei Ausprägungen

Die Vermessung § 12.2 nennt dreizehn Unterschiede zwischen den Drillingen. **Vier davon sind
echte Ausprägung** — `BedarfsArt`, Simulationsklasse, Engine-Methode, Teiler —, und die drei
letzten liegen ohnehin hinter `BedarfsArt`. Alles andere ist Nachzug oder Zufall.

`BedarfsArt` liegt seit W8 im Kern, weil ihn beide Seiten brauchen; `TypStammDialog`,
`TypProfilDialog` und `BedarfErgebnisDialog` tragen ihn schon. Die Komponente bekommt ihn als
`[Parameter] Art` und daran die Hilfeadresse; alles Übrige — Texte, Format, Delegaten — liefert
die Hülle je Ausprägung.

### 3.2 Fünf von sieben Knöpfen führten schon in Blazor

`TypStammHuelle.Bearbeiten`/`Neu`/`ProfilOeffnen` und `BedarfErgebnisHuelle.Zeigen` sind seit
W8 Razor-Hüllen und öffneten bisher ein ZWEITES modales Fenster über der WinForms-Maske. Sie
werden **Überlagerungen** derselben Komponente (Risiko R2) — vier Stück, dazu die
Namensabfrage:

| Überlagerung | Parametersatz aus |
|---|---|
| `TypStammDialog` (Bearbeiten und Neu) | `TypStammHuelle.Gaben(art, …, KatalogModus)` |
| `TypProfilDialog` | `TypStammHuelle.ProfilGaben(art)` |
| `BedarfErgebnisDialog` | `BedarfErgebnisHuelle.Gaben(…)` nach `BedarfsVorschauCtrl.Rechnen` |
| `NamensDialog` | inline, vor `Exists` |

Dasselbe Muster hat W9.5 für die Projektblätter gebaut; beide Wirte teilen sich damit die
Hüllen der Welle 8.

### 3.3 Was in den Kern gezogen ist

| Was | Wohin | Warum |
|---|---|---|
| `SetControls` (Liste) | `BedarfStammCtrl.Bezeichner(art)` | stand dreimal wortgleich; Prozesswärme füllte aus `m_szProzessname`, die anderen aus `m_szBezeichner` — dieselbe DB-Spalte |
| `SetProzessInfo` | `BedarfStammCtrl.Kopf(art, name)` | dito; `null` = Satz fehlt, die Felder bleiben stehen |
| `Prozesssumme` | war schon `BedarfStammCtrl.Jahressumme` (W8.0b) | Befund W14‑B53: die Maske rechnete sie ein zweites Mal |
| die drei `Delete`-Aufrufe | `BedarfStammCtrl.Loeschen(art, name)` → `BedarfLoeschErgebnis` | die `Delete` der Stammcontroller MELDEN die ReadOnly-Sperre über `Meldung.Hinweis` — ein modaler Kasten über der WebView |
| die drei `btn_Simulation_Click` | `BedarfsVorschauCtrl.Rechnen(art, idProjekt, bezeichner)` | vier Unterschiede, alle hinter `BedarfsArt` |
| `listBox_Extern.FindString` | `SolarganglinieStammCtrl.Exists(name)` | eine PRÄFIXsuche in der ANZEIGE (B70) |
| inline-SQL `:79` | `SolarganglinieStammCtrl.HatProjektzuordnung(name)` | Zeichenkettenverkettung über den Anwendertext (B12) |
| `ToolsClass.OpenText` | `GanglinienTextDatei.Lies(pfad, mitKopfzeile: true)` | war schon mit W13.0h gebaut (R‑W14‑8 vermieden) |

**W14b.0c war ohne Arbeit erledigt:** W13.0h hat `GanglinienTextDatei` von vornherein MIT dem
Kopfzeilenschalter gebaut — der Solarganglinien-Weg ist ein Aufrufwechsel, keine zweite Klasse.

### 3.4 Lesen und Schreiben laufen nebenher

Der Solarganglinien-Import liest 8 760 Zeilen und schreibt sie in EINER Transaktion. In einer
WebView ist der Renderfaden derselbe Faden; die Kette läuft deshalb in `Task.Run` der Hülle und
meldet an den Baustein `Fortschritt`. Der Vorläufer setzte dafür `Cursors.WaitCursor` — ohne
`try/finally` (Befund W14‑B71).

---

## 4. Feldkarten-Abgleich

Die vier Karten sind **vor** dem Port gezogen worden
(`dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`).

### 4.1 `Form_Stromverbraucher_Admin` → Ausprägung `Stromverbraucher` (13 Zeilen)

| # | Steuerelement | Nachfolge | ☑ |
|---|---|---|---|
| 1 | `btn_Prozess_DBneu` „Verbraucher in DB neu" | Knopf „Neu" → Namensabfrage → `Exists` → `TypStammDialog` | ☑ |
| 2 | `btn_Prozess_loeschen` „Verbraucher in DB löschen" | Knopf „Löschen" → Leerprüfung → `Rueckfrage` | ☑ |
| 3 | `btn_OK` „OK" | `SpeichernLeiste` | ☑ |
| 4 | `btn_ProzTypeDBedit` „Typ in DB ändern" | Knopf → `TypProfilDialog`-Überlagerung | ☑ |
| 5 | `btn_Prozess_DBedit` „Verbraucher in DB ändern" | Knopf → `TypStammDialog`-Überlagerung | ☑ |
| 6 | `textBox_Name` „Name:" (nur lesen) | `Textfeld`, `NurLesen` | ☑ |
| 7 | `textBox_Jahres_Verbrauch` „jährlicher Strombedarf:" | `Textfeld`, `NurLesen`, Format `"F2"` | ☑ |
| 8 | `textBox_Beschreibung` „Beschreibung:" (mehrzeilig) | `Textfeld`, `Mehrzeilig`, 3 Zeilen | ☑ |
| 9 | `textBox_Type` „Typ:" | `Textfeld`, `NurLesen` | ☑ |
| 10 | `Label11` „MWh" | `epos-einheit` rechts neben der Jahressumme | ☑ |
| 11 | `listBox_Verbraucher_DB` „Datenbank Stromverbraucher" | `Raster` + `Zeilenwahl` | ☑ |
| 12 | `btn_Abbrechen` „Abbrechen" | `SpeichernLeiste` | ☑ |
| 13 | `btn_Simulation` „Grafik" | Knopf → `BedarfsVorschauCtrl` → `BedarfErgebnisDialog` | ☑ |

### 4.2 `Form_Prozesswaerme_Admin` → Ausprägung `Prozesswaerme` (13 Zeilen)

Zeichengleich mit § 4.1 bis auf die Texte („Neuer Prozess", „Prozess löschen", „Typ ändern",
„Prozess ändern", „jährlicher Prozesswärmebedarf:", „Datenbank Prozesswärme:", „MWth") und das
Jahressummen-Format (**ohne Formatangabe**). Zusätzlich behoben: Die Liste war mit `Click` UND
`SelectedIndexChanged` auf dieselbe Arbeit verdrahtet (Befund W14‑B52, in der Vermessung nur
für das Brauchwasser vermerkt — sie gilt für beide).

### 4.3 `Form_Brauchwasser_Admin` → Ausprägung `Brauchwasser` (14 Zeilen)

Dieselben 13 Elemente, dazu `Label24` „Datenbank Brauchwasserprofile" als eigene Beschriftung
(die Liste selbst hatte keine — „Felder ohne Beschriftung: 1"). Sie wird der `Gruppenkopf`.
**Die Maske war als einzige der vier gar nicht lokalisiert** (`ApplyResources` 0, keine
Satellitendateien, 13 deutsche Designer-Literale, Befund W14‑B54); alle 13 Texte sind mit
W14b.0e in beiden Sprachen entstanden. Der Tippfehler „Pofiltyp ändern…" (B55) heißt jetzt
„Profiltyp ändern…".

### 4.4 `Form_Solarganglinie_Admin` → `SolarganglinieAdminDialog` (11 Zeilen)

| # | Steuerelement | Nachfolge | ☑ |
|---|---|---|---|
| 1 | `listBox_Extern` „Ganglinien in DB" | `Raster` + `Zeilenwahl`, zwei Spalten (Bezeichner, Beschreibung) | ☑ |
| 2 | `btn_Hilfe` „Hilfe" — **ohne Handler** (B74) | `InfoKnopf`, Hilfeziel `Solarthermie` | ☑ |
| 3 | `btn_OK` „OK" | Schlussknopf; liefert jetzt OK (A‑7) | ☑ |
| 4 | `label6` „Datei Basis Ordner:" — **`Visible = False`** | sichtbar (A‑6) | ☑ |
| 5 | `textBox_Ordner` — **`Visible = False`, gesperrt** | `Textfeld`, `NurLesen`, sichtbar (A‑6) | ☑ |
| 6 | `btn_Loeschen` „Ganglinie Löschen" | Knopf → Sperren → `Rueckfrage` (A‑5) | ☑ |
| 7 | `textBox_Name` (mehrzeilig, nur lesen) | Pfadfeld der `Dateiwahl` | ☑ |
| 8 | `btn_Oeffnen` „Datei bearbeiten…" | Knopf → `Dienste.Datei.MitSystemOeffnen` mit dem VOLLEN Pfad (B67) | ☑ |
| 9 | `btn_Datei` „Datei Auswählen…" | Knopf der `Dateiwahl`, Filter `"(*.txt)|*.txt"` | ☑ |
| 10 | `btn_Einlesen` „Datei Einlesen…" | Knopf → `Task.Run` mit `Fortschritt` | ☑ |
| 11 | `label1` „Stundenwerte über 1 Jahr als Textdatei" | `Herleitungszeile` | ☑ |
| — | `groupBox1` „Ganglinie aus Datei Einlesen" | `Gruppenkopf` | ☑ |

**Zwei U+200B im englischen `label1`** des Vorläufers („Hourly values ​​over …") sind beim
Übertragen entfallen — der neue Schlüssel `SGAD_LBL_STUNDENWERTE` trägt normale Leerzeichen.

---

## 5. Abweichungen (A‑Zeilen)

| Nr | Was | Warum | Windows-Abnahme |
|---|---|---|---|
| **A‑1** | Leerprüfung vor dem Löschen **auch beim Brauchwasser** | Es fragte bei leerer Liste „Soll  wirklich gelöscht werden ?" (B51) | „Löschen" bei leerer Liste je Ausprägung |
| **A‑2** | EIN Löschsatz mit Platzhalter (`PSP_MELDUNG_WIRKLICH_LOESCHEN`) | Derselbe Satz stand in drei Schreibweisen da (B64) | Rückfragetext je Ausprägung |
| **A‑3** | Fehlschlag und ReadOnly-Sperre als **Warnbanner im Dialog**; die Erfolgsmeldung der Prozesswärme wird eine Bannerzeile | Fünf `MessageBox` in einem Handler (B59); ein modaler Kasten über der WebView | Löschen eines Auslieferungssatzes (Brauchwasser „Haushalt-3") |
| **A‑4** | Der Knopf „Ergebnisse" entfällt | Er war in KEINEM Designer verdrahtet (B78) | Die sieben Knöpfe je Ausprägung zählen |
| **A‑5** | Solarganglinie: **Rückfrage** vor dem Löschen, Rückgabewert ausgewertet | Sie löschte ohne Rückfrage und prüfte nichts (B68) | Löschen mit „Nein" und mit „Ja" |
| **A‑6** | Der Ganglinienordner ist **sichtbar** | `textBox_Ordner`/`label6` waren `Visible = False` (B79, neu) | Der Pfad steht im Dialog |
| **A‑7** | „OK" der Solarganglinie liefert OK | Sie setzte ein Feld `result`, das niemand las (B4) | — (folgenlos, kein Aufrufer wertet aus) |

### Wörtlich trotz Befund

| Befund | Wie behandelt |
|---|---|
| **B57** — drei Formate der Jahressumme (`"F3"` / ohne / `"F2"`) | **wörtlich je Ausprägung** in `BedarfAdminHuelle.JahressummeText`. Die Vereinheitlichung auf `"F2"` ändert die angezeigte Zahl → **Anwenderfrage W14b‑O‑1** |
| **B66** — zwei Ordner für dasselbe Feld | **wörtlich**: `Settings.VDI3805Path\Solarthermie` gewinnt, der Konstruktorwert `ApplicationPath_User\Solarthermie` war tot (`SetControls` überschrieb ihn bei jedem Aufrufer sofort) |
| die Dateikopie | **wörtlich**: Trägt der Ordner schon eine gleichnamige Datei, wird DIESE weiterverwendet — derselbe offene Punkt wie W13‑O‑1 und W12‑O‑2 |
| die Reiter- und `mitBrauchwasser`-Argumente je Knopf | **wörtlich**: Brauchwasser `true`/Reiter 2, Prozesswärme `false`/Reiter 1, Strom die Stromüberladung mit Reiter 1 |
| die Reihenfolge Namensabfrage → `Exists` → `TypStammDialog` | **wörtlich** |
| die zwölf Monatswerte, Engine-Methode und Teiler je Art | **bitgleich**, eingefroren in `BedarfVerwaltungTests` |

### Ersatzlos entfallen

`m_bAdmin` (öffentlich, nie gesetzt oder gelesen, in allen drei Drillingen) · `m_ID_Projekt` und
`m_szProjekt` (B6) · `list_pwmodel` — beim Brauchwasser sogar mit dem PROZESS-Typ (B48) ·
`model` und `ctrl` (B60) · die neun unbenutzten lokalen Objekte der drei `SetControls` (B56) ·
`Z_ProjektStromverbraucherModel ctrl` (Name sagt Controller, Typ ist Model, B63) ·
`btn_ErgebnisseVerbrauch_Click` ×3 (B78) · bei der Solarganglinie `result` (B4), `DateiListe`
(B65), `btn_Abbrechen_Click` ohne Knopf und `GetDateiInfo` (B75) · die Handler- und
Steuerelementnamen „Prozess…" in der Stromverbrauchermaske (B62).

---

## 6. Texte (W14b.0e)

**46 neue Schlüssel, je deutsch UND englisch** — `BADM_*` (34) und `SGAD_*` (12).

* **9 gemeinsam** für die Drillinge: Name/Beschreibung/Typ, „Grafik", die beiden
  Einheitenkürzel (MWh / MWth) und die drei Löschmeldungen.
* **8 je Ausprägung**: Titel, Listenbeschriftung, Jahressummen-Beschriftung, die vier Knöpfe
  und die Leerprüfung.
* **12 für die Solarganglinie**: Titel, Listenbeschriftung, Gruppenkopf, Hinweiszeile, zwei
  Feldbeschriftungen, vier Knöpfe und drei Meldungen.

**Zum ersten Mal Englisch bekommt die ganze Maske `Form_Brauchwasser_Admin`** (B54) und die
drei Basis-Texte ohne Englisch je Drilling (B61). Vorlage sind die englischen Texte der
Zwillinge: „Delete process in DB" → „Delete profile".

**Wiederverwendet statt verdoppelt:** `PSP_MELDUNG_WIRKLICH_LOESCHEN`, `PSP_TITEL_LOESCHEN`,
`BTYP_MSG_NAME_BELEGT`, `BTYP_MSG_NAME_LEER`, `ALLG_BTN_OK`/`ABBRECHEN`/`JA`/`NEIN`,
`KFAK_SP_WAHL`, `WBAD_SPALTE_BEZEICHNER`, `WBAD_DATEIFILTER`, `WBAD_MSG_PROJEKTZUORDNUNG`,
`WBAD_MSG_ABLAGE`, `WBAD_MSG_GELOESCHT`, `WBAD_MSG_GESPEICHERT`.

---

## 7. Formularkarte (W14b.6)

**Der Testanker in einem eigenen Schritt mit Vorher/Nachher-Lauf** (R‑W14‑2).

Vorher: 119 von 123 grün — rot waren genau die vier Zahlenzeugen.

| Stelle | vorher | nachher |
|---|---|---|
| `StapelTests:32` Kleinschreibungs-Zeuge | `Form_Brauchwasser_Admin.designer.cs` | **`WizardParent.designer.cs`** |
| `StapelTests:55` Designer-Dateien | ≥ 33 | ≥ 29 |
| `StapelTests:100` Masken | ≥ 32 | ≥ 28 |
| `StapelTests:132` lokalisiert | ≥ 20 | ≥ 17 |
| `ErreichbarkeitTests:259` erreichbar | ≥ 31 | ≥ 27 |

**Nach W14a und W14b bleiben genau ZWEI kleingeschriebene Designer im Bestand** —
`WizardParent` und `Wizard_Komponenten`, beide Welle 16. Der Zeuge hält damit so lange wie
möglich. Der Großschreibungs-Zeuge `Form_Klimadaten` bleibt (Welle 14c).

Nachgezogen: `Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md` (neuer Abschnitt „Stand
nach iU9‑W14b", Zählung 28/27, die vier Tabellenzeilen entfernt) und
`WindowsFormsApplication1/CLAUDE.md`.

**Ein Fund am Werkzeug selbst (W14b.0):** Der Nebenbaum-Filter aus `9cccfc1` (`.claude`,
`.git`) verglich den ABSOLUTEN Pfad. Läuft der Stapellauf in einem Git-Nebenbaum, liegt dessen
Wurzel aber selbst unter `.claude/worktrees/` — der Filter warf dann den gesamten Bestand
hinaus und meldete null Masken (7 von 123 rot). Gemessen wird jetzt der Pfad RELATIV zur
Suchwurzel; für einen Lauf über die Repowurzel ändert das nichts.

---

## 8. Nachweise

### 8.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64
→ Build succeeded. 0 Fehler, 12 Warnungen
```

Die zwölf sind die bekannten: 6 × WFO1000, 2 × CS0108, 2 × CS0109, 1 × WFO0003, 1 × CA2255.
**Keine neue** — die vier Masken trugen keine.

### 8.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ EPOS.Kern.Tests       622 gruen
  EPOS.UI.Tests       1 678 gruen
  SpeicherEngine.Tests  337 gruen
  KiKern.Tests          450 gruen
  = 3 087 gruen, 0 rot

dasselbe mit LANG=en_US.UTF-8
→ 3 087 gruen, 0 rot
```

Basis vor der Welle (nach dem Einheiten-Merge): **3 008**, also **79 Fälle mehr**:

| Datei | Fälle | Gegenstand |
|---|---|---|
| `EPOS.Kern.Tests/BedarfVerwaltungTests.cs` | 37 | der eingefrorene Nachweis (§ 8.3) |
| `EPOS.UI.Tests/Dialoge/BedarfAdminDialogTests.cs` | 43 | W14b.1 — Feldbestand je AUSPRÄGUNG, Löschwege, „Neu", „Grafik" |
| `EPOS.UI.Tests/Dialoge/SolarganglinieAdminDialogTests.cs` | 21 | W14b.2 |
| `EPOS.UI.Tests/Dialoge/SolarganglinieDialogTests.cs` | ±0 | zwei Fälle vom Sprung auf die Überlagerung umgestellt |
| übrige | −22 | die 22 Fälle der drei gelöschten Masken hatte es nie gegeben; die Differenz ist der Sammelzähler |

**Beide neuen bunit-Klassen pinnen die Sprache SELBST im Konstruktor** (`DeutscheOberflaeche()`
— Kultur, UI-Kultur und die beiden `DefaultThread*`-Kulturen auf `de-DE`), Regel seit W8. In
`EPOS.Kern.Tests` pinnt der eine Formatfall (`Die_drei_Anzeigeformate_…`) im Rumpf und stellt im
`finally` zurück.

`TestDatenbank` wird nur lesend je Klasse geteilt (Regel seit W11a); die beiden schreibenden
Fälle legen sich ihre eigene Arbeitskopie an.

### 8.3 Der eingefrorene Nachweis — die Basis der Welle

`EPOS.Kern.Tests/BedarfVerwaltungTests.cs`, **vor der ersten portierten Zeile** angelegt
(R‑W14‑1). Für die vier Masken gab es kein Netz: kein Referenzlauf (sie pflegen Kataloge, die
der Lauf über die Projektzuordnung liest), keine ChartProbe (null Grafiken), keinen Kern-Test
(Befund W14‑B77).

| Was | Eingefroren auf |
|---|---|
| Jahressumme je Art | drei Proben je Art, sechs Stellen — z. B. „EFH Wohnen, 1 Person" 0,7429 · „CONT" 365,0 · „Berger-Fertigung" 5 136,0 |
| die ZWÖLF Monatswerte | eine Probe je Art, Wert für Wert |
| die drei Anzeigeformate | `"0,743"` / `"365"` / `"365,00"` (B57) |
| Liste und Kopf | Satzzahlen 16 / 32 / 41, erster Bezeichner, Typ und Beschreibung |
| `Exists`, `IstReadOnly` | sechs ReadOnly-Sätze im Brauchwasser, keiner in den beiden anderen |
| Vorrechnung Brauchwasser | 8 760 Werte, Summe **742,9008 kWh** — OHNE Teiler (B49), exakt das Tausendfache der Katalogsumme |
| Vorrechnung Prozesswärme | Summe 365 000 kWh, `/1000` = 365 MWh; Monatswerte 31/28/…/31 |
| Vorrechnung Strom | Summe 365 000, `/1000` = 365; `Array.Copy` in 35 040 Viertelstundenplätze; `Maximaler_Strombedarf` = **41,666668** |
| unbekannter Bezeichner | leere Reihe in allen drei Zweigen — und beim Strom **kein** `null`: Die Null-Prüfung `:99` greift dort gar nicht |
| Solarganglinien-Katalog | 1 Satz „Tsol1", Beschreibung, `GetStammId` 1 bzw. 0 |
| `GanglinienTextDatei` mit Kopfzeile | Beschreibung + **8 760** Werte; dieselbe Datei OHNE Schalter → **8 761** |
| die drei Gegenproben | `IMP_TXT_TRENNZEICHEN` (`;`, `,`) und `IMP_TXT_LEERZEILE` |

**Die Erwartungswerte des Parsers sind gegen `ToolsClass.OpenText` gemessen, VOR dessen
Löschung** — mit dem Ergebnis: `;` und `,` am Zeilenende lieferten `false` **mit eigenem
Dialog**, eine LEERZEILE warf eine `ArgumentOutOfRangeException` mitten im Parser (B72 /
W13‑B11).

Neue Probe: `Referenzlaeufe/Importproben/solarganglinie_8760.txt` (Kopfzeile + 8 760 Werte,
68 KB) — sie liegt wie die zwanzig aus W13 als `-text` in `.gitattributes`.

### 8.4 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 240 SQL-Texte geprueft: 0 Fundstellen, 173 dynamisch, 1 067 in Ordnung
```

1 241 → 1 240: Die zwei neuen Anweisungen (`COUNT(*)` der Projektzuordnung, `SELECT ID` für
`Exists`) kommen dazu, das verkettete inline-SQL der Maske und die drei SQL-Texte der
`ToolsClass` fallen weg.

### 8.5 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 30 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

**Unverändert 30** — die Welle fasst den Renderer nicht an.

### 8.6 Referenzlauf

```
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w14b
→ Erfolgreich: 3 von 3

vergleich <Basis 1030/1007/1017> artifacts/reflauf/w14b
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt gegen Referenzlaeufe/2026-08-30_B3-Kaskade
→ BYTE-GLEICH: Projekt_1007, Projekt_1017, Projekt_1030
```

**Byte-gleich, nicht nur innerhalb der Toleranz.** Ein Unterschied wäre hier ein Fehler: Die
Welle fasst den Rechenweg nicht an — `BedarfsVorschauCtrl` ist eine ANZEIGE-Vorrechnung, kein
Simulationsweg.

### 8.7 Formularkarte

```
dotnet test Werkzeuge/Formularkarte.Tests -c Release
→ 123 gruen (auch unter LANG=en_US.UTF-8)

dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1
→ 28 Masken, 17 lokalisiert, 27 erreichbar, 0 nein, 0 verwaist, 1 unklar
```

### 8.8 Keine Typverwendung ist übrig

`git grep` über `*.cs` und `*.razor` nach den vier Klassennamen und nach `ToolsClass` findet
**nur noch Kommentare, Maskenschlüssel und Hilfeadressen** — keinen Aufruf, kein `new`, keine
Typreferenz.

Die `Masken.*`-Konstanten behalten ihre Werte (`"Form_Brauchwasser_Admin"` …): Sie sind
sprachneutrale ASCII-**Schlüssel** und nicht der Name einer Klasse — dieselbe Praxis wie seit
W12. Ebenso die vier Zeilen in `help_mapping.txt` (`:202`, `:238`, `:248`, `:259`): Sie sind die
Adresse des HILFETEXTES.

### 8.9 Die beiden Wächter

```
git grep -nE '\bProgram\.[A-Za-z]' -- 'EPOS.Kern/*.cs' …        → leer
git grep -nE 'System\.Windows\.Forms|…|OleDb' -- 'EPOS.Kern/*.cs' → leer
```

---

## 9. Grenzen

* **Der Referenzlauf sieht diese Welle nicht.** Er rechnet einen bestehenden Projektstand nach;
  die vier Masken pflegen Kataloge. Dafür sind die 37 eingefrorenen Fälle da — und die
  A‑Zeilen stehen als Windows-Abnahmepunkte in § 10.
* **`BedarfsProfileHuelle.Rechenstand` (W9.5) bleibt unberührt.** Die Projektblätter rechnen
  ihre Vorschau anders als die Verwaltungen: Beim Strom fehlt dort `Strombedarf_Gebaeude_gesamt`
  ganz, bei der Prozesswärme der Teiler. Das ist ein eigener Gegenstand (**offener Punkt
  W14b‑O‑2**) und keine Nebenarbeit dieser Welle.
* **Der Dialog ist nicht am Gerät gelaufen.** Alles Geprüfte ist headless geprüft; die
  Abnahmeliste in § 10 ist die zweite Hälfte des Nachweises.

---

## 10. Abnahmeliste Windows (iZ5)

**Je Ausprägung des `BedarfAdminDialog`** (Menü → Stromverbraucher / Prozesswärme /
Brauchwasser):

1. Die Liste steht, die **erste Zeile ist gewählt**, Jahressumme, Name, Beschreibung und Typ
   zeigen den ersten Satz.
2. Eine andere Zeile wählen → alle vier Felder wechseln.
3. Die Jahressumme trägt ihr Format: **drei** Nachkommastellen (Brauchwasser), **keine**
   (Prozesswärme), **zwei** (Stromverbraucher) — und die Einheit rechts daneben MWth / MWth /
   MWh. *(Anwenderfrage W14b‑O‑1: vereinheitlichen?)*
4. „…ändern" öffnet den Stammkopf **als Überlagerung im selben Fenster**, nicht als zweites
   Fenster; nach „Beenden" ist die Liste neu geladen.
5. „…neu" → Namensabfrage → ein **belegter** Name meldet „Name existiert bereits!" und der
   Stammkopf bleibt zu; ein freier Name öffnet ihn im Modus Neu.
6. „Typ ändern" öffnet das Wochen-Stundenprofil als Überlagerung.
7. **„Löschen" bei leerer Auswahl** meldet den Text der Ausprägung und fragt NICHT (A‑1).
8. „Löschen" mit Auswahl fragt „Soll *Name* wirklich gelöscht werden ?" (A‑2); „Nein" lässt den
   Satz stehen.
9. **Brauchwasser: „Haushalt-3" löschen** → Warnbanner „schreibgeschützt", der Satz bleibt, KEIN
   modaler Kasten (A‑3).
10. „Grafik" rechnet vor und zeigt den Ergebnisdialog als Überlagerung — Brauchwasser mit der
    dritten Sicht und Reiter „Grafik", die beiden anderen ohne und mit Reiter „monatlich".
11. **Die Zahl „Wärmebedarf Brauchwasser" ist bei der Vorgabe MWh zeichengleich zum alten
    Stand** (Entscheid W8‑O‑5); Umschalten auf kWh multipliziert sie mit 1000.
12. **Es gibt KEINEN Knopf „Ergebnisse"** (A‑4) — sieben Knöpfe je Ausprägung.
13. „OK" schließt mit OK, „Abbrechen" mit Abbruch; Esc schließt nur, wenn keine Überlagerung
    offen ist.
14. Englische Oberfläche: **alle** Texte sind übersetzt, auch die des Brauchwassers samt
    Fenstertitel (B54).
15. 125 % DPI: Der Dialog ist scharf, die Knopfleiste bricht nicht um.

**`SolarganglinieAdminDialog`** (Menü → Solarthermie Ganglinie):

16. Die Liste zeigt Bezeichner **und Beschreibung**; der Ordnerpfad steht sichtbar da (A‑6).
17. „Datei Auswählen…" startet im Ordner `…\VDI-3805-Daten\Solarthermie` mit dem Filter
    `(*.txt)`; die gewählte Datei wird dorthin **kopiert**, und das Pfadfeld zeigt danach die
    KOPIE.
18. Eine **fehlgeschlagene** Kopie (Ordner schreibgeschützt) meldet als Warnbanner und der
    Import läuft mit dem Original weiter (B69).
19. „Datei bearbeiten…" öffnet die Datei mit der Systemanwendung — **einmal** der volle Pfad,
    nicht verdoppelt (B67).
20. „Datei Einlesen…" liest **Kopfzeile + 8 760 Werte**, zeigt den Fortschritt und meldet
    „…wurde mit 8760 Werten eingelesen"; die Liste ist danach neu geladen und die Beschreibung
    steht in der Zeile.
21. Dieselbe Datei ein zweites Mal einlesen → „Solarganglinie ist bereits in Datenbank
    vorhanden!"; eine Datei, deren Name mit einem vorhandenen BEGINNT (z. B. `Tsol1_2026.txt`
    bei vorhandenem `Tsol1`), wird **eingelesen** (B70).
22. „Ganglinie Löschen" mit **Projektzuordnung** → Warnbanner, keine Rückfrage.
23. „Ganglinie Löschen" ohne Zuordnung → Rückfrage mit Namen (A‑5), „Ja" löscht, „Nein" nicht.
24. **Aus dem Projektdialog Solarthermieganglinien:** „Bearbeiten…" öffnet die Verwaltung
    **als Überlagerung** — die Liste ist gefüllt (B73), nach dem Schließen ist der Katalog des
    Projektdialogs neu gezogen.
25. Englische Oberfläche und 125 % DPI wie oben.

---

## 11. Offene Punkte

| Nr | Punkt |
|---|---|
| **W14b‑O‑1** | **Anwenderfrage:** Die Jahressumme trägt drei Formate — `"F3"` (Brauchwasser), ohne (Prozesswärme), `"F2"` (Stromverbraucher). Empfehlung: `"F2"` für alle drei. Sie ändert die angezeigte Zahl und ist deshalb nicht nebenbei zu entscheiden (B57). |
| **W14b‑O‑2** | `BedarfsProfileHuelle.Rechenstand` (W9.5) rechnet die Vorschau der PROJEKTblätter anders als `BedarfsVorschauCtrl` die der Verwaltungen: Beim Strom bleibt `Strombedarf_Gebaeude_gesamt` dort 0, bei der Prozesswärme fehlt der Teiler. Beide Wege in EINEN zusammenzuführen ist ein eigener Schritt mit eigenem Nachweis. |
| **W14b‑O‑3** | Die verlustfreie Ablage verwendet eine schon vorhandene gleichnamige Datei weiter, statt die neue zu nehmen — wörtlich übernommen, derselbe offene Punkt wie W13‑O‑1 und W12‑O‑2. |
| **E‑6** | **erledigt** mit W8‑O‑5 (04.09.2026): Einheit am Wert, MWh als Vorgabe, kWh wählbar. |
| **E‑7 / E‑8** | **gegenstandslos** (B78): Der Knopf, den sie betreffen, war in keinem Designer verdrahtet. |

---

## 12. Geänderte und neue Dateien

**Neu**

```
EPOS.Kern/Controller/BedarfsVorschauCtrl.cs
EPOS.Kern.Tests/BedarfVerwaltungTests.cs
EPOS.UI/Dialoge/Bedarf/BedarfAdminDialog.razor
EPOS.UI/Dialoge/Bedarf/BedarfAdminDaten.cs
EPOS.UI/Dialoge/Solarthermie/SolarganglinieAdminDialog.razor
EPOS.UI/Dialoge/Solarthermie/SolarganglinieAdminDaten.cs
EPOS.UI.Tests/Dialoge/BedarfAdminDialogTests.cs
EPOS.UI.Tests/Dialoge/SolarganglinieAdminDialogTests.cs
WindowsFormsApplication1/Views/Bedarf/BedarfAdminHuelle.cs
WindowsFormsApplication1/Views/Solarthermie/SolarganglinieAdminHuelle.cs
Referenzlaeufe/Importproben/solarganglinie_8760.txt
WindowsFormsApplication1/Allgemein/Reporting/iU9_W14b_Blazor_Port_Protokoll.md
```

**Gelöscht (19)**

```
EPOS.Kern/Allgemein/ToolsClass.cs
WindowsFormsApplication1/Views/Brauchwasser/Form_Brauchwasser_Admin.{cs,designer.cs,resx}
WindowsFormsApplication1/Views/Prozesswärme/Form_Prozesswaerme_Admin.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
WindowsFormsApplication1/Views/Stromverbraucher/Form_Stromverbraucher_Admin.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
WindowsFormsApplication1/Views/Solarthermie/Form_Solarganglinie_Admin.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
```

**Geändert**

```
EPOS.Kern/Controller/BedarfStammCtrl.cs, SolarganglinieStammCtrl.cs
EPOS.Kern/Allgemein/Import/GanglinienDatei.cs
EPOS.Kern/MyResource/Resource.resx, Resource.en-US.resx, Resource.Designer.cs
EPOS.UI/Dialoge/Allgemein/Sprungziel.cs
EPOS.UI/Dialoge/Solarthermie/SolarganglinieDialog.razor
EPOS.UI.Tests/Dialoge/SolarganglinieDialogTests.cs, SprungzielTests.cs
WindowsFormsApplication1/Dienste/WinFormsNavigation.cs
WindowsFormsApplication1/Allgemein/Blazor/Sprungbruecke.cs
WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs
WindowsFormsApplication1/Views/Solarthermie/SolarganglinieHuelle.cs
Werkzeuge/Formularkarte/Stapel.cs, Erreichbarkeit_2026-09-03.md
Werkzeuge/Formularkarte.Tests/StapelTests.cs, ErreichbarkeitTests.cs
EPOS.UI/CLAUDE.md, EPOS.Kern/CLAUDE.md, WindowsFormsApplication1/CLAUDE.md
```
