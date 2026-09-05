# iU9 Welle 6 — Erzeuger-Eingabemasken I — Portprotokoll

> Umsetzung 03.09.2026 auf `ios_migration`, Basis `740c73e` (nach W4), zusammengeführt
> mit Welle 5 (`ddaea70`). Vorbild in Aufbau und Tiefe: das Protokoll der Welle 4 im
> selben Ordner. Regeln: Wellenplan Abschnitt F, `EPOS.UI/CLAUDE.md`,
> `EPOS.Kern/CLAUDE.md`.

---

## 1. Auftrag und Ergebnis

**Sieben WinForms-Masken der Erzeugerkacheln** (Heizkessel, BHKW, Photovoltaik,
Stromspeicher, Pufferspeicher) sind Razor-Komponenten in `EPOS.UI/Dialoge/Erzeuger/`;
ihre WinForms-Fassungen sind im selben Commit gelöscht (Regel M1). Zusammen **4 202
Zeilen** Oberflächencode, 55 `MessageBox`-Aufrufe und 214 Kartenzeilen.

| Maske | Zeilen | Komponente | Hülle |
|---|---|---|---|
| `Form_Heizkessel_Bearbeiten` | 714 | `HeizkesselKatalogDialog` | `Views/Heizkessel/HeizkesselHuelle.cs` |
| `Form_DBBHKW` | 712 | `BhkwKatalogDialog` | `Views/BHKW/BhkwHuelle.cs` |
| `Form_Heizkessel` | 767 | `HeizkesselDialog` | dieselbe Datei |
| `Form_BHKWEing` | 940 | `BhkwDialog` | dieselbe Datei |
| `Form_PV` | 338 | `PhotovoltaikDialog` | `Views/Photovoltaik/PhotovoltaikHuelle.cs` |
| `Form_Stromspeicher` | 354 | `StromspeicherDialog` | `Views/Stromspeicher/StromspeicherHuelle.cs` |
| `Form_PufferSp` | 377 | `PufferspeicherDialog` | `Views/Pufferspeicher/PufferspeicherHuelle.cs` |

**Vier davon sind zugleich Assistentenseiten** (PV, Stromspeicher, Heizkessel, BHKW) —
die ersten, die als Razor-Komponente im Assistentenrahmen laufen.

### Commits

| Hash | Betreff |
|---|---|
| `c825649` | iU9-W6.0a/b: Trägervariante anlegen, Gruppenvarianten und Umhängen im Kern |
| `68f3634` | iU9-W6.0c: Katalogfilter und Detailblöcke der Erzeugerdialoge in den Kern |
| `9991259` | iU9-W6.0d: vier Sprungziele für die Katalogverwaltungen der Erzeuger |
| `ca73a39` | iU9-W6.0f: `KostenKnoepfeLeiste` — der KD6-Kostenblock als Razor-Teilstück |
| `8fc101e` | iU9-W6.0e: Assistentenseite als Blazor-Hülle und ihre Schnittstelle |
| `bd9151f` | iU9-W6.1: `HeizkesselKatalogDialog` — der Katalogeditor als Razor-Komponente |
| `dd11c2b` | iU9-W6.2: `BhkwKatalogDialog` — der BHKW-Katalogeditor als Razor-Komponente |
| `448d4c5` | iU9-W6.3: `HeizkesselDialog` — der Projektdialog als Razor-Komponente |
| `1bb2c19` | iU9-W6.4a: `BhkwDialog` — die Komponente des BHKW-Projektdialogs |
| `7e8e341` | Merge iU9 Welle 5 (Berichte- und Kostenseiten) in Welle 6 |
| `ef28099` | iU9-W6.4b: `BhkwDialog` verdrahtet — Hülle, Aufrufer, Assistentenseite |
| `329a1be` | iU9-W6.5: `PhotovoltaikDialog` — der PV-Projektdialog als Razor-Komponente |
| `fa670fc` | iU9-W6.6: `StromspeicherDialog` — der Speicherdialog als Razor-Komponente |
| `6e2a2f5` | iU9-W6.7: `PufferspeicherDialog` — die letzte der sieben Masken |

---

## 2. Bauweise

### 2.1 Ein Muster für fünf Masken

Die fünf Projektdialoge teilen einen Aufbau: links „ausgewählt im Projekt", rechts
„aus Datenbank", dazwischen ◀ und ▶, unten ein Detailblock. Dafür gibt es **eine**
Datenform, `EPOS.UI/Dialoge/Erzeuger/ErzeugerAuswahlDaten.cs`:

| Typ | Wofür |
|---|---|
| `ErzeugerZeile` | eine Zeile der linken Liste — `Schluessel` (die Zeile), `GeraetId` (das Gerät), dazu je nach Art Vorlauf/Rücklauf, Grenzleistung, Neigung/Azimut/Anzahl |
| `KatalogZeile` | eine Zeile der rechten Liste — Id, Bezeichner, optional eine zweite, mehrzeilige Spalte |
| `ErzeugerDetail` | der Detailblock: Name, Beschreibung, eine Liste (Beschriftung, Wert) und optional ein Ja/Nein-Merkmal |
| `TraegerVorbereitung` | was der Kern beisteuert, bevor der Trägerdialog erscheint |
| `AufnahmeErgebnis` | die neue Zeile, die Meldung und ob sie eine Warnung ist |

**`Schluessel` und `GeraetId` sind getrennt, und das ist die Fachlage.** Zwei gleiche
Kessel im Projekt teilen sich EINE Kopie in `Tab_Heizkessel`; daran hängt die Regel,
dass „▶" die Projektkopie nur entfernt, wenn keine zweite Zeile mehr darauf verweist.
Der Vorläufer brauchte dafür `ListViewItem.Tag` (Heizkessel, BHKW) oder eine
Parallelliste (`_linkeListe` im Pufferspeicher, Befund 4).

### 2.2 Die Datenseite liegt im Kern

| Was | Wo | Herkunft |
|---|---|---|
| `EnergietraegerVarianteCtrl.Anlegen` | `EPOS.Kern/Controller/` | die 185 Zeilen `CreateNewEnergyCarrier`, die ZWEIMAL wortgleich in der Oberfläche standen (`Form_Heizkessel.cs:305`, `Form_BHKWEing.cs:536`) |
| `…VariantenDerGruppe`, `…TraegerUmhaengen` | dieselbe Datei | `ApplySelectedKessel`:546 / `ApplySelectedBHKW`:381 und die beiden `cmbBrennstoffArt_SelectedIndexChanged` |
| `HeizkesselStammCtrl.Filtern`, `IdZu`, `LEISTUNG_SQL`, `Ueberschreiben`, `Anlegen` | `EPOS.Kern/Controller/` | `SetFilter`:646, `btn_Ueberschreiben_Click`:401, `Insert`:447 |
| `BHKWStammCtrl.Filtern`, `IdZu`, `Ueberschreiben`, `Anlegen`, `Namen` | dito | `BuildFilter`:156, `btn_Speichern_Unter_Click`:403, `btn_Speichern_Click`:483 |
| `PhotovoltaikStammCtrl.Hersteller`, `Filtern`, `Detail`, `BezeichnerZu` | dito | `Form_PV_Load`:69, `SetFilter`:215, die drei `RecordSet`-Blöcke |
| `PufferSpStammCtrl.VOLUMEN_SQL`, `VolumenTexte`, `Hersteller`, `Filtern`, `Detail` | dito | `Views/Pufferspeicher/PufferSpFilter.cs` (die Tabellen) und `SetFilter`:300 |
| `HeizkesselCtrl.ProjektDetail`/`KatalogDetail`, `BHKWCtrl.ProjektDetail`/`StammDetail`, `PufferSpCtrl.Detail` | dito | die Detailblöcke der fünf Masken |
| `EmissionsVorgaben` | `EPOS.Kern/Allgemein/` (neu) | `btn_CO2_Click` ×2 und `btn_Eintragen_Click` — dreimal dieselben Zahlen im Oberflächencode |

**`Update()` ist zweigeteilt** (Heizkessel): `UpdateMitGrund` liefert den
Ablehnungsgrund ZURÜCK, `Update` zeigt ihn wie bisher über `Meldung.*`. Eine
Razor-Komponente hat keine `MessageBox` — es bleibt bei genau einer Regel, aber mit
zwei Arten, sie zu erfahren.

### 2.3 Die Assistentenseite (W6.0e)

`BlazorAssistentSeite<TKomponente>` ist das Gegenstück zu `BlazorDialogForm`: randlos,
`TopLevel = false`-tauglich, dieselbe `BlazorWebView` mit denselben
`CreationProperties` — also derselbe Browserprozess wie die Dialoge.

Drei Entscheidungen mit Grund:

* **Verzögert gebaut.** `AssistentSeiten.Erzeugen` baut alle dreizehn Seiten auf
  einmal. Vier WebViews im Voraus wären vier Browserprozesse für Seiten, die der
  Anwender vielleicht nie sieht (Risiko R‑W6‑1). Die WebView entsteht erst in
  `Bestuecken`.
* **Beim Wiederbesuch wird die Wurzelkomponente getauscht**, nicht die WebView — das
  kostet einen Neuaufbau der Komponente, aber keinen neuen Browserprozess. Weigert
  sich die Sammlung, wird die WebView als Ganzes ersetzt.
* **Das Wunschmaß ist gesetzt, nicht gemessen.** `WizardParent.LoadNewForm`
  vergrößert das Fenster nach `PreferredSize`; eine Form mit gedockter WebView meldet
  dafür nichts Brauchbares.

`IAssistentErzeugerSeite` ersetzt die je zwei Zeilen mit hartem Typumbruch, die
`WizardParent` für jede Erzeugerseite führte. Damit entfällt für diese vier Masken
auch `WizardParent.Aktiver`: Der Rahmen reicht die Liste herein, statt dass die Maske
ihn sich sucht.

### 2.4 Unterdialoge in Überlagerungen

Sechs Unterdialoge stehen im SELBEN Fenster (Risiko R2):

| Wirt | Unterdialog | Vorläufer |
|---|---|---|
| `HeizkesselKatalogDialog` | `NamensDialog` („Speichern unter") | `NamensDialogHuelle` als Fenster |
| `BhkwKatalogDialog` | `NamensDialog`, `Rueckfrage` (Schreibschutz) | dito, `MessageBox.YesNo` |
| `HeizkesselDialog` | `EnergietraegerVarianteDialog`, `HeizkesselKatalogDialog` | zwei eigene Fenster |
| `BhkwDialog` | `EnergietraegerVarianteDialog`, `BhkwKatalogDialog`, `NamensDialog` | drei eigene Fenster |

Die vier **Katalogverwaltungen** (Heizkessel-Admin, PV-Admin, Stromspeicher-Admin,
PufferSp-Admin) bleiben bis Welle 14 WinForms und gehen deshalb über die
**Sprungbrücke** — vier neue `Sprungziel`-Schlüssel (W6.0d).

### 2.5 Zwei neue Fähigkeiten der Standardfelder

`Zahlenfeld` und `Ganzzahlfeld` führen seit W6.1 `Feldname` und `FehlerZustand`. Der
WinForms-Bestand prüft beim Speicherknopf jedes Zahlenfeld einzeln
(`Program.ZahlPruefen(feld, "Thermische Leistung", …)`) und nennt in der Meldung genau
das Feld, an dem es hängt. Jetzt färbt das Feld während der Eingabe wie bisher und
meldet SEINEN NAMEN an den Dialog — so bleibt die Regel erhalten, ohne dass ein
Dialog fünfzehn `@ref` auf seine Felder halten muss.

---

## 3. Feldkarten-Abgleich

Die sieben Karten wurden am 03.09.2026 neu gezogen (Stand nach W4) und liegen unter
`scratchpad/iU9/karten_w6/`. Abgeglichen ist je Maske der **Feldbestand nach Zahl und
Beschriftung** (bunit-Test „Der Feldbestand …").

| Maske | Karte | Komponente | Anmerkung |
|---|---|---|---|
| Heizkessel_Bearbeiten | 42 Zeilen, 5 Gruppen, 17 TextBox | 5 Gruppen, 18 Felder | +1 = das Laufzeitfeld `tb_Wartungskosten` (`WartungsfeldAufbauen`:146), das die Karte nicht kennt |
| DBBHKW | 58 Zeilen, 5 Gruppen, 24 TextBox | 5 Gruppen, 25 Felder | +1 = `comboBox_Name`, im Port ein Textfeld (A‑3) |
| Heizkessel | 24 Zeilen, Fenster 17 + Gruppe „Modul" 7 | 2 Listen, 2 Filter, 7 Detailfelder | die zwei ListView/ListBox werden Raster mit `Zeilenwahl` |
| BHKWEing | 31 Zeilen, Fenster 23 + Panel 8 | dito plus Summe und Grenzleistung | das `DataGridView` wird ein Raster mit zwei Spalten |
| PV | 22 Zeilen, Fenster 13 + Panel1 4 + Panel2 5 | 2 Listen, 1 Filter, 3 Anlagen-, 4 Modulfelder | **Karte falsch** — siehe A‑15 |
| Stromspeicher | 16 Zeilen, Fenster 9 + Gruppe 7 | 2 Listen, 7 Detailfelder | zwei Beschriftungen aus dem Ressourcenkatalog |
| PufferSp | 21 Zeilen | 2 Listen, 2 Filter, 6 Detailfelder | die zwei Parallellisten entfallen |

**Laufzeitfelder von Hand ergänzt** (die Karte liest nur `InitializeComponent`):
`tb_Wartungskosten` + `cb_WartungEinheit` im Heizkessel-Katalogeditor, die
`KostenKnoepfe.Leiste` in vier Masken.

---

## 4. Abweichungen (mit Begründung)

| # | Was | Warum |
|---|---|---|
| **A‑1** | Die Kostenleiste öffnet ihre beiden Ziele als **zweites modales Fenster** aus dem Rückruf | Beide Ziele (`KostenKomponenteHuelle`, `EnergietraegerHuelle`) sind selbst Blazor-Hüllen. Die Verschmelzung zur `Ueberlagerung` bräuchte deren 790‑Zeilen-Datenseite als Delegatensatz — nicht in W6. Wie W4‑O3 für die PV-Vergütung hingenommen; Rückfall nachgelagert |
| **A‑2** | Kein KI-Aufrufknopf (`KiAufrufKnopf.Anbringen`), kein `FensterEinpassung`, kein `SchriftAngleichen`, kein `Form_PV_Paint` | Der KI-Einstieg hat in `EPOS.UI` noch keinen Baustein (`Gespraechsverlauf` kommt in W15b). Die drei anderen sind WinForms-Layoutkorrekturen; das erledigen die Hülle (`AnBildschirmGeklemmt`) und das CSS |
| **A‑2b** | `EnergietraegerVarianteCtrl.Anlegen` nimmt drei Einzelwerte statt des Ergebnis-Records der Komponente | Der Kern referenziert `EPOS.UI` nicht und darf es nicht. Die Hülle packt den Record aus |
| **A‑3** | Der BHKW-**Modulname** ist im Modus „Bearbeiten" nur lesbar | `BHKWStammCtrl.Update` filtert per `Bezeichner`; ein hier geänderter Name träfe keinen Satz und meldete trotzdem Erfolg — `ExecuteSQL` gilt auch bei null betroffenen Zeilen als gelungen. Umbenannt wird über „Speichern unter" |
| **A‑4** | „Löschen" im Heizkessel-Projektdialog fragt jetzt nach | Der Knopf löscht aus dem KATALOG und wirkt für alle Projekte; der Vorläufer tat das ohne Rückfrage. Dieselbe Lage wie B0‑8 beim Pufferspeicher, wo die Rückfrage in Paket 9 ergänzt wurde |
| **A‑5** | „▶" im PV-Dialog entfernt die ZEILE, nicht ihren Listenindex | `list_pvmodel.RemoveAt(listBox_Auswahl.SelectedIndex)`: Im Assistenten führt dieselbe Liste ALLE Erzeugertypen, die ListBox aber nur die PV-Zeilen — der Index passte dort nicht, und entfernt wurde eine fremde Anlage |
| **A‑6** | Die achte BHKW-Leistungsstufe ist erreichbar | Der Bestand füllte die Liste aus `LeistungText` („größer 1200 kW") und verglich in `BuildFilter` gegen „über 1.200 kW" — die Stufe traf nie und zeigte still alle Leistungen. Über den Index greift sie |
| **A‑7** | Herstellernamen kommen in PV und Pufferspeicher als `DbParam` | Der PV-Bestand verdoppelte das Hochkomma nicht; ein Herstellername mit Apostroph zerriss dort das Prädikat |
| **A‑8** | `PhotovoltaikStammCtrl.Filtern` sortiert nach Bezeichner | Der Bestand sortierte beim Filtern NICHT, während die Erstbefüllung sortiert kam — die Liste sprang beim ersten Filtern in eine andere Reihenfolge |
| **A‑9** | Katalogzeilen führen ihren Primärschlüssel; gelöscht und geladen wird darüber | `Tab_Heizkessel_STAMM` hat auf `Bezeichner` keinen eindeutigen Index (21 Zeilen, 16 auf acht Dubletten). Der alte Weg über den Namen traf bei einer Dublette alle Namensvettern (dieselbe Klasse wie V0‑9) |
| **A‑10** | `HeizkesselStammCtrl.Anlegen` unterscheidet „Name existiert bereits" und „Datenbankfehler" | Der Vorläufer meldete beides als „Name existiert bereits oder Datenbankfehler!" |
| **A‑11** | `Form_PufferSp_Admin` nimmt `Form_PufferSp_Bearbeiten.MODE_NEU` | Dort stand `Form_Heizkessel_Bearbeiten.MODE_NEU` — die Konstante einer FREMDEN Maske, die zufällig denselben Wert trug |
| **A‑12** | Der BHKW-Energieträger bleibt **0‑basiert** | `InitDatensatzUpdate` setzt `m_Brennstoff = SelectedIndex` ohne `+ 1`, anders als der Heizkessel. Regel F3 — wörtlich übernommen |
| **A‑13** | `Form_SolarKollektoren(Admin)` nimmt `Form_SolarDB.MODE_EDIT` | Dort stand `Form_DBBHKW.MODE_EDIT`, wieder die Konstante einer fremden Maske |
| **A‑14** | Die linke Liste zeigt die Zeilen des ÜBERGEBENEN Typs | `SetControls` filterte hart auf `KESSEL_TYP`, auch wenn der Aufrufer `REF_KESSEL_TYP` meinte — siehe Befund W6‑O‑3 |
| **A‑15** | Die drei PV-Panel-Beschriftungen kommen aus dem **Designer**, nicht aus der Feldkarte | Die Karte ordnet „Azimut [°]" dem Feld `textBox_AnlagenLeistung` und „10" dem Feld `textBox_Azimut` zu. Die Designer-Koordinaten sagen: `label3` „Neigung [°]:" über `textBox_Neigung` (8,10 → 93,8), `label6` „Azimut [°]:" über `textBox_Azimut` (9,36 → 93,35), `label7` „Anzahl Module:" über `textBox_AnlagenLeistung` (177,10 → 180,34). Ein Test hält die drei fest (R‑W6‑7) |
| **A‑16** | Der PV-Katalogsatz wird über seine Id gelöscht | wie A‑9 |
| **A‑17** | „▶" im Stromspeicher trifft genau die GEWÄHLTE Zeile | Der Vorläufer nahm die erste Zeile gleichen Namens — bei zwei gleichen Speichern also nicht zwingend die markierte |
| **A‑18** | Die Stromspeicherzeile führt einen eigenen Schlüssel | Der Vorläufer legte die Modelle ohne `ID` an; ohne eindeutigen Schlüssel wären zwei gleiche Speicher für die Hülle ununterscheidbar |
| **A‑19** | Nach dem Sprung in eine Katalogverwaltung wird die Liste IMMER neu gezogen | Der Anwender kann dort etwas geändert und mit Abbrechen geschlossen haben |
| **A‑20** | 55 `MessageBox` werden `Warnbanner`, `Rueckfrage` oder Meldungstext eines Ergebnis-Records | Bestätigungen („Datensatz gespeichert") schließen den Dialog wie bisher; Ablehnungen bleiben als Banner stehen und lassen ihn offen (Folgepaket zu `ab5bf32`) |

---

## 5. Texte

**196 Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` und —
von Hand, weil hier kein Visual Studio läuft — `Resource.Designer.cs`. Alle drei
Dateien geprüft: kein Schlüssel fehlt in einer von ihnen.

| Präfix | Zahl | Wofür |
|---|---|---|
| `ETVAR_*` | 5 | die vier Ausgänge des Trägeranlegens plus der Speicherfehler |
| `HZKK_*` | 48 | Heizkessel-Katalogeditor |
| `HZK_*` | 28 | Heizkessel-Projektdialog samt der sechs Leistungsstufen |
| `BHKWK_*` | 52 | BHKW-Katalogeditor |
| `BHKWV_*` | 23 | BHKW-Projektdialog |
| `PVD_*` | 17 | Photovoltaik |
| `SPD_*` | 9 | Stromspeicher |
| `PSPD_*` | 12 | Pufferspeicher |
| `ALLG_BTN_JA/_NEIN` | 2 | die beiden Antworten der `Rueckfrage` |

**Wiederverwendet statt neu angelegt:** `KESSEL_WARTUNG_LBL`,
`KESSEL_WARTUNG_EINHEIT_LBL`, `ADM_MEHRDEUTIG_TEXT/_TITEL`, `ADM_BTN_SPEICHERN`,
`BHKW_SUMME_LBL`, `BHKW_INVEST_UNBESTIMMT`, `BHKW_INVEST_HINWEIS_*`,
`SP_LABEL_ENERGIE`, `SP_LABEL_MODULKOSTEN`, `PSP_FILTER_*`, `PSP_MELDUNG_*`,
`PSP_TITEL_KATALOG_LOESCHUNG`, `ANL_DUBLETTE_FRAGE/_TITEL`, `KDLG_KNOPF_*`,
`KAUSW_*`, `KFAK_SP_WAHL`, `ALLG_BTN_OK/_ABBRECHEN`.

**Fünf der sieben Masken waren lokalisiert** (`.en-US.resx`: Heizkessel 21 Texte,
Heizkessel_Bearbeiten 33, Stromspeicher 12, PufferSp 15, BHKWEing 0 — nur Maße). Alle
Texte sind in den neuen Schlüsseln aufgegangen; die Zahl der lokalisierten Masken
sinkt dadurch von 59 auf **54**.

**Zugriff** über `Resource.ResourceManager.GetString` mit deutschem Rückfall im Code
(B5b‑O4) — die Hülle setzt die Texte, die Komponente trägt den deutschen Literaltext
als Parametervorgabe.

**`help_mapping.txt` bleibt unverändert.** Die sieben Zeilen `Form_X.btn_Help` gelten
weiter — der Schlüssel benennt die Wikiseite, nicht die Klasse.

**`Allgemein/KI/HilfeKontext.cs`:** die sieben Einträge der gelöschten Masken
entfernt, jeweils im Commit ihrer Maske (Regel F10).

---

## 6. WinForms-Seite

**Gelöscht** (31 Dateien):

```
Views/Heizkessel/Form_Heizkessel_Bearbeiten.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Heizkessel/Form_Heizkessel.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}
Views/BHKW/Form_DBBHKW.{cs,designer.cs,resx}
Views/BHKW/Form_BHKWEing.{cs,designer.cs,resx,en-US.resx}
Views/Photovoltaik/Form_PV.{cs,Designer.cs,resx}
Views/Stromspeicher/Form_Stromspeicher.{cs,designer.cs,resx,de-DE.resx,en-US.resx}
Views/Pufferspeicher/Form_PufferSp.{cs,Designer.cs,resx,de-DE.resx,en-US.resx}
```

**Neu** auf der Windows-Seite: `Views/Heizkessel/HeizkesselHuelle.cs`,
`Views/BHKW/BhkwHuelle.cs`, `Views/Photovoltaik/PhotovoltaikHuelle.cs`,
`Views/Stromspeicher/StromspeicherHuelle.cs`,
`Views/Pufferspeicher/PufferspeicherHuelle.cs`,
`Allgemein/Blazor/BlazorAssistentSeite.cs`, `Views/Wizard/IAssistentErzeugerSeite.cs`.

**Umgebaut:** `Views/Pufferspeicher/PufferSpFilter.cs` ist eine dünne
ComboBox-Fassade geworden — die sechs Prädikate und ihre Texte liegen im Kern, weil
`Form_PufferSp_Admin` sie bis Welle 14 unverändert braucht.

**Aufrufer umgestellt:** `Form_Start` (fünf `pBox_*_Click`), fünf `*KontextMenuCtrl`
(dort auch fünf unbenutzte `new Form_X()` im Löschen-Handler gestrichen),
`Form_Heizkessel_Admin`, `Form_BHKWAdmin`, `AssistentSeiten` (vier Zeilen und die
`_typen`-Liste), `WizardParent` (vier Zweige weg, dafür einer über die Schnittstelle).

**Keine Typverwendung ist übrig:**

```
git grep -nE "(new|typeof|:)\s*(Form_Heizkessel|Form_Heizkessel_Bearbeiten|Form_DBBHKW|
    Form_BHKWEing|Form_PV|Form_Stromspeicher|Form_PufferSp)\b" -- '*.cs'
→ 0 Treffer (ohne Kommentare)
```

Restfundstellen der alten Namen sind ausschließlich (a) `HilfeSchluessel`-Zeichenketten,
(b) Kommentare, die die Herkunft nennen, und (c) `Form_BHKWAdmin.Form_BHKWEing_Load` —
ein Handlername in einer FREMDEN Maske, der nie auf die gelöschte Klasse zeigte.

---

## 7. Nachweise

### 7.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 -t:Rebuild
→ 0 Fehler, 20 Warnungen
```

Die Warnzahl ist gegenüber der Basis 740c73e (22) um zwei gesunken und liegt gleichauf
mit dem Stand nach Welle 5 — die WFO1000-Fundstellen der gelöschten Designer fallen
mit ihnen weg. `BlazorAssistentSeite.Modelle` trägt `Browsable(false)` und
`DesignerSerializationVisibility.Hidden`, sonst zählte der WinForms-Analysator die
Laufzeitgabe als weitere Fundstelle.

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ KiKern.Tests        450 gruen
  SpeicherEngine.Tests 337 gruen
  EPOS.UI.Tests        785 gruen   (+ 91 aus Welle 6)
  EPOS.Kern.Tests       64 gruen   (+ 27 aus Welle 6)
  zusammen           1 636 gruen, 0 rot
```

**91 neue bunit-Fälle** je Komponente: `KostenKnoepfeLeiste` 8,
`HeizkesselKatalogDialog` 18, `BhkwKatalogDialog` 19, `HeizkesselDialog` 20,
`BhkwDialog` 17, `PhotovoltaikDialog` 13, `StromspeicherDialog` 11,
`PufferspeicherDialog` 13, `SprungzielTests` +4. Jeder Satz prüft Feldbestand (Zahl
UND Beschriftungen), Vorbelegung, Prüfregeln, Rückrufe und Tastatur; die Kultur ist
auf de‑DE gepinnt.

**27 neue Kern-Fälle** in zwei Klassen, die erstmals eine DATENBANK brauchen:
`EnergietraegerVarianteCtrlTests` (10) und `KatalogFilterTests` (17). Sie legen je
eine Arbeitskopie von `Referenzlaeufe/Kenndaten_Test.sqlite` an und biegen
`DataRepository.PfadUeberschreibung` darauf um (`TestDatenbank`) — dasselbe Vorgehen
wie `EPOS.Referenzlauf`, damit die Vergleichsbasis unberührt bleibt. Beide tragen
`[Collection("Testdatenbank")]`: `PfadUeberschreibung` ist statisch, und xunit fährt
Testklassen sonst nebeneinander.

### 7.3 Formularkarte

```
dotnet test Werkzeuge/Formularkarte/Formularkarte.sln -c Release
→ 123 gruen
```

Vier Anker hingen an gelöschten Masken und zeigen jetzt auf länger haltbare:

| Test | vorher | jetzt | hält bis |
|---|---|---|---|
| Zeuge „große Schreibweise" | `Form_Heizkessel.Designer.cs` | `Form_Klimadaten.Designer.cs` | W14c |
| Zeuge „kleine Schreibweise" | `Form_BHKWEing.designer.cs` | `Form_Brauchwasser_Admin.designer.cs` | W14b |
| Stapellauf und Übersicht | `Views/Heizkessel` | `Views/Klimadaten` | W14c |
| Erreichbarkeit über die Startseite | `Form_Heizkessel` | `Form_Gebaeude` | W9 |

Kein neues Prüfmuster nötig: Keiner der vier Anker braucht eine gelöschte Maske als
Analysegegenstand, und `Pruefmuster/Stromspeicher` trägt `Form_StromspeicherItemNeu`
(aus W2), nicht `Form_Stromspeicher`.

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1 --erreichbarkeit
→ Masken 81 (88 nach W5), lokalisiert 54 (59), erreichbar 79, unerreichbar 0, verwaist 0
```

**81 = 88 − 7.** Die Zahl der lokalisierten Masken sinkt erstmals seit Welle 2 mit,
weil Welle 6 fünf lokalisierte Masken umstellt.

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 283 SQL-Texte geprueft: 0 Fundstellen, 170 dynamisch, 1 113 in Ordnung
python3 … --selbsttest
→ 32 Anweisungen, 0 Abweichungen
```

**Eine Nachbesserung am Prüfer** (Befund W6‑O‑4): Mit der Löschung von `Form_PufferSp`
verschwand die zweite Vereinbarung des Namens `felder`
(`const string felder = "SELECT Bezeichner, Hersteller, …"`). Damit galt der Kurzname
als eindeutig, und der Prüfer löste die LOKALEN Variablen gleichen Namens in
`WirtschaftlichkeitCtrl` (Z. 4942 und 5413) gegen die Konstante aus
`EnergieEinheitenPruefung` auf — zwei falsche Fundstellen in Code, den diese Welle
nicht anfasst. `_konstante` bekommt deshalb eine Sperrliste: Ein Name, der in
derselben Datei als gewöhnliche `string`- oder `var`-Variable vereinbart ist, wird
nicht mehr über den Kurznamen aus einer fremden Klasse aufgelöst. Fünf Anweisungen
gelten dadurch als nicht auflösbar statt falsch aufgelöst (1 288 → 1 283 geprüft).

### 7.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 10 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

Unverändert — Welle 6 fasst den Renderer nicht an.

### 7.7 Referenzlauf

**Pflicht in dieser Welle**, weil fünf Kern-Controller angefasst werden
(`EnergietraegerVarianteCtrl`, `HeizkesselStammCtrl`, `HeizkesselCtrl`,
`BHKWStammCtrl`, `BHKWCtrl`, `PhotovoltaikStammCtrl`, `PufferSpStammCtrl`,
`PufferSpCtrl`).

```
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w6
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/w6
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

**Byte-gleich, nicht nur innerhalb der Toleranz.** Das ist der Nachweis, dass die
Verlagerung der SQL-Anweisungen in den Kern zeichengleich war.

### 7.8 Veröffentlichung

```
dotnet publish WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Release -p:Platform=x64
→ wwwroot/index.html, wwwroot/_framework/blazor.webview.js,
  wwwroot/_content/EPOS.UI/epos-ui.css — vollstaendig
```

---

## 8. Grenzen

* **Am Gerät ungeprüft.** Alles hier ist ohne Windows entstanden. Die
  Abnahmeliste in § 9 ist der Prüfplan.
* **Vier WebViews im Assistenten** (R‑W6‑1). Die verzögerte Bauweise ist die
  Gegenmaßnahme, gemessen ist sie nicht: Seitenwechsel unter 1 s, kein Aufblitzen,
  Speicher der Browserprozesse — das entscheidet das Gerät.
* **Die Sprungbrücke aus einem Erzeugerdialog** (R‑W6‑2) hängt am selben offenen
  Abnahmepunkt W2‑7 wie seit Welle 2.
* **Die Kostenleiste öffnet ein zweites Blazor-Fenster** (A‑1, R‑W6‑3). Rückfall
  nachgelagert; die Verschmelzung nach W16.
* **Zwei Bestandsbefunde bleiben stehen** (W6‑O‑1 und W6‑O‑2, § 10) — sie sind
  Fachentscheidungen, keine Portfragen.

---

## 9. Abnahmeliste Windows (iZ5) für diese sieben Masken

Je Dialog: öffnen mittig, kein weißes Aufblitzen, ziehbar und maximierbar, Tabellen
ohne Umbruch, de **und** en (`HKCU\Software\wp-plan\Language`), Hochkontrast, 125 %
und 150 % scharf, Maus **und** Finger (44 px), Tab-Zyklus bleibt im Dialog, Esc
schließt, Enter schließt NICHT, Infoknopf zeigt die Wikiseite.

| # | Aufrufweg | Was besonders zu prüfen ist |
|---|---|---|
| 1 | Startbild → Kachel **Heizkessel** | Filter (10 Gruppen × 6 Stufen), ◀ öffnet die Trägerwahl in der Überlagerung, Abbruch fügt nichts hinzu, ▶ auf zwei Zeilen mit demselben Kessel entfernt die Projektkopie erst beim zweiten Mal, Trägerwechsel schreibt sofort, „Bearbeiten…" zeigt den Katalogeditor IM Fenster, „Administration…" öffnet die WinForms-Maske DARÜBER |
| 2 | Startbild → Kachel **BHKW** | wie 1, dazu: zweite Spalte „Eigenschaften" vierzeilig, Summe der Ptherm nach jedem ◀/▶, „Neu…" fragt erst den Namen und zeigt dann den Editor in derselben Überlagerung, „Löschen" auf einem Auslieferungssatz nennt den Grund |
| 3 | Startbild → Kachel **Photovoltaik** | Anlagenblock erscheint nur bei Projektzeile, gestrichelter Rahmen, Gesamtleistung folgt der Modulzahl, „Modul Bearbeiten…" springt in `Form_AdminPV` |
| 4 | Startbild → Kachel **Stromspeicher** | zweimal derselbe Speicher = zwei Zeilen, ▶ trifft die markierte, „Energie (Kapazität) [kWh]" und „Modulkosten [€/kWh]" stehen richtig da |
| 5 | Startbild → Kachel **Pufferspeicher** | ◀ auf ein bereits gewähltes Gerät fragt nach, „Nein" fügt nichts hinzu, „Ja" legt eine zweite Zeile an und der Speicherweg fragt NICHT erneut; Projektzeile zeigt die Kopie (ggf. anderer Name) |
| 6 | Übersichtslisten → Kontextmenü **„Hinzufügen/Bearbeiten"** | fünfmal derselbe Dialog; beim Heizkessel auch auf der REF-Liste (dort war die linke Liste bisher leer, A‑14) |
| 7 | Assistent → Seiten 9–12 (PV, Speicher, Kessel, BHKW) | Seitenwechsel unter 1 s, kein Aufblitzen, Rückkehr zeigt den aktuellen Listenstand, keine OK/Abbrechen-Leiste, keine Kostenleiste, Speicher der Browserprozesse im Auge behalten (R‑W6‑1) |
| 8 | Menü **Kataloge → Heizkessel-Admin → Bearbeiten** | Katalogeditor als eigenes Fenster, Mehrdeutigkeitshinweis bei doppeltem Namen, „Speichern unter" fragt den Namen in der Überlagerung |
| 9 | Menü **Kataloge → BHKW-Admin → Bearbeiten / Neu** | Summe der fünf Posten rechnet live, Investition folgt, „unbestimmt" bei Pel = 0, Rückfrage beim Auslieferungssatz |
| 10 | In 1, 2, 8 und 9: **Kostenleiste** | „Investitionskosten…", „Betriebskosten…", „Energiekosten…" öffnen ein zweites Fenster und kehren sauber zurück (A‑1, R‑W6‑3) |

---

## 10. Offene Punkte

| # | Was | Vorschlag |
|---|---|---|
| **W6‑O‑1** | Die Gruppen→`Brennstoff`-Ketten von Heizkessel und BHKW sind **uneinheitlich**. Der Heizkessel kennt „Sonstige", `Tab_BrennstoffKategorien` führt aber „Sonstige Energieträger" — der Eintrag trifft nie; außerdem ist „Sonstige" auf `Brennstoff=23` abgebildet, und 23 ist im Katalog **Fernwärme**. Die Gruppen „Fernwärme" und „Wasserstoff" fehlen der Heizkesselkette ganz. Das BHKW bildet alle zwölf Gruppen richtig ab, nennt „Tierische Fette" aber zweimal | Wörtlich übernommen (Regel F3). Künftig über `Tab_Brennstoff_Stamm.ID_Kategorie` wie `EnergietraegerVarianteCtrl.KategorieZu` — dann gibt es die Kette gar nicht mehr. **Entscheid des Anwenders** |
| **W6‑O‑2** | Die Filterstufe „Alle" heißt `Ptherm Like '%'` und lässt einen Katalogsatz **ohne** Ptherm herausfallen (NULL bleibt NULL) — in der Testdatenbank genau ein Kessel („Test", ID 252). Der Pufferspeicher hat dafür seit Paket 9 die Absicherung `(Gesamtvolumen IS NULL OR Gesamtvolumen Like '%')` | Wörtlich übernommen; die Absicherung ist eine Zeile. **Entscheid des Anwenders** |
| **W6‑O‑3** | `Form_Heizkessel.SetControls` filterte hart auf `KESSEL_TYP`, auch wenn der Aufrufer `REF_KESSEL_TYP` meinte — im Referenzfall blieb die linke Liste leer, obwohl Zeilen vorhanden waren. Die Daten gingen dabei nicht verloren (die Modellliste blieb vollständig), sichtbar und entfernbar waren sie aber nicht | Mit A‑14 behoben. Am Gerät auf der REF-Liste nachsehen |
| **W6‑O‑4** | Der SQL-Dialektprüfer löste einen LOKALEN Variablennamen gegen eine gleichnamige Konstante einer fremden Klasse auf, sobald der Kurzname im Bestand eindeutig wurde | Mit § 7.5 behoben. Die fünf jetzt nicht mehr auflösbaren Anweisungen sind echte Laufzeit-SQL; sie zählen wie die übrigen 170 dynamischen |
| **W6‑O‑5** | Die Kostenleiste öffnet ein zweites Blazor-Fenster (A‑1) | Verschmelzung zur `Ueberlagerung` nach W16, wenn `KostenKomponenteHuelle` ihre Datenseite als Delegatensatz liefert |
| **W6‑O‑6** | Der KI-Aufrufknopf fehlt in Heizkessel-Katalogeditor und PV-Dialog (A‑2) | Mit W15b, wenn `Gespraechsverlauf` steht |

---

## 11. Geänderte und neue Dateien

**Neu in `EPOS.UI`** (10): `Dialoge/Erzeuger/ErzeugerAuswahlDaten.cs`,
`HeizkesselKatalogDaten.cs`, `HeizkesselKatalogDialog.razor`, `BhkwKatalogDaten.cs`,
`BhkwKatalogDialog.razor`, `HeizkesselDialog.razor`, `BhkwDialog.razor`,
`PhotovoltaikDialog.razor`, `StromspeicherDialog.razor`, `PufferspeicherDialog.razor`;
dazu `Dialoge/Kosten/KostenKnoepfeLeiste.razor`.

**Geändert in `EPOS.UI`**: `Standards/Zahlenfeld.razor`, `Standards/Ganzzahlfeld.razor`
(Feldname und Fehlerzustand), `Dialoge/Allgemein/Sprungziel.cs` (vier Ziele),
`wwwroot/epos-ui.css` (Kostenleiste, Auswahlpaar, Anlagenblock, mehrzeilige Zelle).

**Neu im Kern** (1): `Allgemein/EmissionsVorgaben.cs`.
**Geändert im Kern** (8 Controller): `EnergietraegerVarianteCtrl`,
`HeizkesselStammCtrl`, `HeizkesselCtrl`, `BHKWStammCtrl`, `BHKWCtrl`,
`PhotovoltaikStammCtrl`, `PufferSpStammCtrl`, `PufferSpCtrl`; dazu die drei
Ressourcendateien.

**Neu in der Anwendung** (7): fünf Hüllen, `Allgemein/Blazor/BlazorAssistentSeite.cs`,
`Views/Wizard/IAssistentErzeugerSeite.cs`.

**Neu in den Tests** (10): acht bunit-Klassen in `EPOS.UI.Tests/Dialoge/`,
`EPOS.Kern.Tests/TestDatenbank.cs`, `EnergietraegerVarianteCtrlTests.cs`,
`KatalogFilterTests.cs`.

---

## 12. Windows-Abnahme 04.09.2026 — Befund

| # | Befund | Ursache | Behebung |
|---|---|---|---|
| **W6‑B‑1** | **Das Hauptfenster erscheint als ungestyltes HTML.** Die Menüköpfe stehen untereinander als Standardknöpfe des Browsers, Kopfband und Kacheln haben keine Gestaltung. Die Anwendung war damit unbenutzbar — nicht falsch gerechnet, sondern nicht angezogen. | **Eine fehlende geschweifte Klammer im Stilblatt.** Der Regel `.epos-mehrzeilig { white-space: pre-line;` (Z. 1384 f.) fehlte das schließende `}`. Chromium liest die folgenden Regeln dann nicht als Nachbarn, sondern als **verschachtelte** Regeln (CSS Nesting, in Chromium seit Version 112; ein Selektor, der wie `.epos-reiter` mit einem Punkt beginnt, braucht dafür kein `&`): Aus `.epos-reiter { … }` wird `.epos-mehrzeilig .epos-reiter { … }`. Sie greifen also weiter — nur eben ausschließlich **innerhalb** eines `.epos-mehrzeilig`-Elements, und das ist im Hauptfenster keines. Der Browser meldet dabei **nichts**: verschachteltes CSS ist gültiges CSS. Klammerbilanz 619 zu 618. | Die eine Zeile `}` nach `white-space: pre-line;` (Z. 1386). Bilanz wieder 619 zu 619. Wache: **`EPOS.UI.Tests/StilblattTests.cs`** (§ 12.3). |

### 12.1 Woher die Klammer verschwand — nicht aus W6.4a

Der naheliegende Verdacht trifft nicht zu. `1bb2c19` (**W6.4a**, 03.09.2026,
16:45 UTC) hat die Regel eingeführt, und zwar **heil**: Sie war damals die
**letzte** Regel des Blatts, das Blatt hatte 149 zu 149 Klammern, und der
Strukturprüfer von § 12.3 findet an diesem Stand null Befunde.

Verloren ging das `}` im Merge **`7e8e341`** („Merge iU9 Welle 5 (Berichte- und
Kostenseiten) in Welle 6", 03.09.2026) — und der Weg dahin ist der klassische:

| | Stand | Was am Dateiende steht |
|---|---|---|
| Basis | `740c73e` | 1 140 Zeilen, endet mit `overflow: auto;` `}` |
| Elternteil 1 | `1bb2c19` (W6.4a) | hängt **daran** die Regel `.epos-mehrzeilig` samt `}` an |
| Elternteil 2 | `ddaea70` (Welle 5) | hängt **an dieselbe Stelle** den Block „Reiterleiste" an |
| Ergebnis | `7e8e341` | beide Anbauten hintereinander — **ohne** das `}` des ersten |

Beide Zweige haben am selben Dateiende angebaut; beim Auflösen dieses
Anbau/Anbau-Konflikts blieb die eine Zeile liegen. Ab `7e8e341` steht die
Bilanz auf 193 zu 192 und wandert von dort unverändert durch **jeden**
Folgestand bis `e1ed87b` — 619 zu 618.

### 12.2 Wirkung, und warum es einen Tag lang niemandem auffiel

**Was messbar ist.** Das Blatt führt bei `e1ed87b` **569** Blöcke der obersten
Ebene. **155** davon stehen **vor** dem Bruch und waren nie betroffen; die
übrigen **414** — alles ab Z. 1386 — waren nur noch innerhalb von
`.epos-mehrzeilig` wirksam. Der Schnitt fällt genau zwischen zwei Bauarten:

| Vor dem Bruch (wirksam) | Nach dem Bruch (tot) |
|---|---|
| `.epos-dialog` (Z. 135), `.epos-knopf` (Z. 271), `.epos-kachel` (Z. 338), `.epos-feld` (Z. 492), `.epos-raster` (Z. 632) | `.epos-reiter` (Z. 1393), `.epos-kachelraster` (Z. 1454), `.epos-zellenaktionen` (Z. 1672), `.epos-startseite` (Z. 3779), `.epos-menueband` (Z. 3958) |

Das ist der Grund, warum die **Dialoge** der Wellen 6 bis 15 in der Abnahme
richtig aussahen: Rahmen, Knopf, Feld, Kachel und Tabellenraster — alles, was
ein modaler Dialog braucht — stammt aus dem heilen ersten Drittel. Was hinter
dem Bruch liegt, ist fast durchweg **Seiten**gestaltung: Reiterleiste,
Kachelraster, Startseite, Menüband. Und Seiten gibt es erst seit W10b, die
Startseite seit W16b, das Hauptfenster seit W16c.

Dazu ein zweiter belegbarer Punkt: Der Stilblattteil, mit dem **W5‑B‑1** am
04.09.2026 behoben wurde (`acc19a3`, `.epos-zellenaktionen` und
`.epos-zellenaktionen-inhalt`, Z. 1672 ff.), steht **selbst hinter dem Bruch**.
Er war bis zur Klammerkorrektur ebenso wirkungslos wie die Regel, die er
ersetzt hat — die Kostenseite hätte die Aktionsspalte also auch nach der
Behebung nicht so gezeigt, wie § 12 der Welle 5 sie beschreibt. Ob das am Gerät
nachgesehen wurde, geht aus den Protokollen nicht hervor.

**Was Vermutung bleibt.** Warum in den gut 28 Stunden zwischen `7e8e341`
(03.09., 16:49 UTC) und der Abnahme am Abend des 04.09. niemand eine Seite in
WebView2 gesehen hat, lässt sich hier nicht
belegen — in der Arbeitsumgebung ist kein Browser erreichbar (dieselbe Grenze,
die schon § 12 der Welle 5 nennt), und die bunit-Fälle rechnen keine
Stilblätter aus. Wahrscheinlich ist die einfache Erklärung: Die Abnahme vom
04.09.2026 war der **erste** Lauf, in dem ein Gerät das Hauptfenster überhaupt
gezeichnet hat. Der Start davor ist an **W16c‑B12** gescheitert (fehlender
`[Parameter] Zustand`, `TargetInvocationException` an `Program.Main:332`); erst
`73b6e58` hat ihn repariert. Der erste Blick auf das gezeichnete Fenster und
der Befund fallen damit zusammen — was den Befund erklärt, aber nicht die
vorangegangenen Wellen entlastet.

### 12.3 Die Wache: `StilblattTests`

Eine bunit-Probe kann diesen Fehler grundsätzlich nicht sehen — das Markup war
die ganze Zeit richtig, und bunit rechnet kein CSS aus. Deshalb liest die Wache
das Stilblatt selbst und prüft seine **Struktur**. Sie steht in
`EPOS.UI.Tests/StilblattTests.cs` und geht denselben Weg zum Blatt wie die
Regressionswache zu W5‑B‑1
(`Seiten/KostenSeiteTests.Die_Aktionszelle_traegt_im_Stilblatt_kein_display_flex`),
die den **Inhalt** einer einzelnen Regel prüft; die neue prüft den **Bau** des
ganzen Blatts.

Der Prüfer ist ein eigener kleiner Strukturparser (kein CSS-Verständnis, nur
Blockzählung): Kommentare `/* … */`, Zeichenketten `"…"`/`'…'` und `url(…)`
werden übersprungen, über die geöffneten Blöcke läuft ein Stapel mit
**Zeilennummer und Selektor** mit — damit jede Meldung sagen kann, wo man
nachzusehen hat.

| Fall | Was er verlangt |
|---|---|
| `Jede_geoeffnete_Klammer_wird_geschlossen` | Jede `{` wird geschlossen, keine `}` ist überzählig. Die Meldung nennt Zeile **und** Selektor des offen gebliebenen Blocks |
| `Keine_Stilregel_steht_in_einer_Stilregel` | Innerhalb eines Blocks, dessen Selektor nicht mit `@` beginnt, beginnt kein weiterer Block. Unter einer At-Regel (`@media`, `@supports`, `@keyframes`, `@font-face`, `@layer`) ist ein Block normal und erlaubt |
| `Kein_kaufmaennisches_Und_als_Nesting_Selektor` | Kein `&` außerhalb von Kommentaren und Zeichenketten. **Das Haus benutzt kein CSS-Nesting** — wo verschachtelt aussieht, ist eine Klammer verlorengegangen |
| `Die_Wache_findet_die_fehlende_Klammer_von_epos_mehrzeilig` | Die Gegenprobe: Sie entfernt die Klammer in einer Kopie des **echten** Blatts wieder und verlangt beide Meldungen — den offenen Block (Z. 1384, `.epos-mehrzeilig`) und die erste Regel, die dadurch in ihm landet (Z. 1392, `.epos-reiter`). Die Zeilennummern stehen **nicht fest** im Test, sie werden im Text gesucht; sonst bräche der Fall bei jeder Regel, die jemand weiter oben einfügt |
| `Das_Hausblatt_liegt_unter_der_Wache` | Der Pfadweg greift nicht ins Leere. Ohne diesen Fall wären die drei Theorien bei einem misslungenen Aufstieg **leer und trotzdem grün** |

Die ersten drei laufen als `[Theory]` über **alle** `.css` unter
`EPOS.UI/wwwroot` — heute ist das eine Datei, ein zweites Blatt stünde ohne
weiteres Zutun mit unter der Wache.

### 12.4 Bestandsaufnahme

Der Prüfer über `e1ed87b` (mit gesetzter Klammer) findet **keinen weiteren
Fehler**:

| Geprüft | Ergebnis |
|---|---|
| Stilblätter unter `EPOS.UI/wwwroot` | **eines** — `epos-ui.css` (4 123 Zeilen). Daneben liegen nur `epos-verlauf.js`, `help_icon.png` und `bilder/` |
| Klammerbilanz | 619 zu 619, kein offener Block, keine überzählige `}` |
| Verschachtelung | 569 Blöcke der obersten Ebene, **0** Stilregeln in Stilregeln; die 27 At-Regeln (25 `@media`, 2 `@keyframes`) sind richtig geschachtelt |
| `&`-Selektoren | **0**. Das Zeichen kommt dreimal vor, jedes Mal in einem **Kommentar** (Z. 1485 und 1603 „Berichte & Kosten", Z. 2701 `&nbsp;`) — der Prüfer sieht Kommentare nicht |
| `url(…)` | kommt im Blatt nicht vor; der Prüfer überspringt es trotzdem, damit ein späteres `url(data:…)` ihn nicht aus dem Tritt bringt |
| Einbindung | `WindowsFormsApplication1/wwwroot/index.html` und `EPOS.iOS/wwwroot/index.html` verweisen **beide** auf `_content/EPOS.UI/epos-ui.css` und je ein gebündeltes `*.styles.css` des Wirts (`EPOS_Plan.styles.css` bzw. `EPOS.iOS.styles.css`). Dasselbe Blatt trägt also beide Schalen — ein Bruch darin trifft Windows und iOS gleichermaßen |

### 12.5 Gate der Behebung

| Nachweis | Ergebnis |
|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | **0 Fehler**, 6 Warnungen (unverändert; keine aus der neuen Datei) |
| `dotnet test EPOS.UI.Tests -c Release --no-build` | **2 233 grün** (2 228 + 5), auch unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` |
| Gegenprobe am echten Blatt | Klammer entfernt → `Jede_geoeffnete_Klammer_wird_geschlossen` **rot** mit „Zeile 1384: Block nicht geschlossen — `.epos-mehrzeilig`", `Keine_Stilregel_steht_in_einer_Stilregel` **rot** mit 414 Meldungen. Klammer gesetzt → grün |

**Grenze des Nachweises.** Dass das Fenster jetzt richtig aussieht, ist auf
Linux nicht zu zeigen — kein WebView2, kein Browser. Belegt sind die
**Struktur** des Blatts und die Wache dagegen; die Sichtprüfung bleibt beim
Anwender und gehört zu Abnahmepunkt 0 der Welle 16c („Start").


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

### Vorbild → Umsetzung je Dialog

| Dialog | Vorbild (Geometrie im `.resx`) | Umsetzung |
|---|---|---|
| `Erzeuger/HeizkesselDialog` | `Form_Heizkessel`: Projektliste x=12 **w=316**, `btn_Kessel_Hinzu` „◀" x=342 **w=88**, `btn_Kessel_Entfernen` „▶", Katalog x=443 **w=313**; Detailblock „Modul" darunter über die linke Hälfte, die zwei Filterklapplisten rechts | Stand schon nebeneinander (`epos-auswahlpaar`), jetzt `Zweispaltenauswahl`. Die zwei Filter bleiben **über** der Katalogliste in der rechten Spalte, „Bearbeiten/Löschen/Verwaltung" darunter, der Detailblock unter dem Paar über die volle Breite |
| `Erzeuger/BhkwDialog` | `Form_BHKWEing`: dasselbe Muster, links zusätzlich die Summenzeile („Module in Datenbank" rechts) | `Zweispaltenauswahl`; die Summenzeile bleibt **in** der linken Spalte unter der Projektliste — sie gehört zur Projektliste, nicht zum Katalog |
| `Erzeuger/PhotovoltaikDialog` | `Form_PV`: zwei Listen, ein Herstellerfilter rechts | `Zweispaltenauswahl`, Filter über der Katalogliste |
| `Erzeuger/PufferspeicherDialog` | `Form_PufferSp`: zwei Listen, zwei Filter rechts | `Zweispaltenauswahl`, beide Filter über der Katalogliste |
| `Erzeuger/StromspeicherDialog` | `Form_Stromspeicher`: zwei Listen ohne Filter | `Zweispaltenauswahl` |

**Was sich für den Anwender ändert.** Auf breitem Schirm nichts an der Anordnung — die
fünf Dialoge standen schon so. Neu ist zweierlei: Die zwei Knöpfe tragen jetzt ihre
Aufgabe im **Klartext** statt nur „◀"/„▶" (Befund W9‑B‑3 galt hier genauso, war aber
hier nicht gemeldet), und auf schmalem Schirm stapeln sich die Listen sauber
untereinander, statt sich auf 260 px zusammenzuquetschen.

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

## Windows-Abnahme 05.09.2026 — Formularraster, Paket P1 (iU8‑E‑2)

Der Anwender am 05.09.2026: „Darstellung der Dialoge kompakter und übersichtlicher —
Parameterblöcke rechts. Genauso für andere Dialoge prüfen." Die hausweite Regel dazu steht
im Protokoll **W14a**, Abschnitt „Kompaktes Formularraster"; hier stehen nur die **sechs
Dialoge dieser Welle**. Umgestellt wird durch **Einhängen**: `<Formularraster>` um den
vorhandenen Feldlauf, kein Feld umbenannt, kein Text geändert, keine Regel je Dialog. Die
von Hand gebauten `epos-feldpaar`-Wirte entfallen dabei — der Raster stellt zwei Feldpaare
je Zeile selbst, sobald die Spalte breit genug ist. In `EPOS.UI/Dialoge/Erzeuger/` steht
danach **kein** `epos-feldpaar` mehr.

| Datei | Felder | Gruppen (Raster) | Einspaltig | Klasse‑B‑Entscheid |
|---|---|---|---|---|
| `Dialoge/Erzeuger/HeizkesselKatalogDialog.razor` (W6.1) | 21 | 4 — Bezeichnung, Technik, Kosten, Emissionen | nein | Klasse A, reines Einhängen |
| `Dialoge/Erzeuger/BhkwKatalogDialog.razor` (W6.2) | 26 | 4 — Bezeichnung, Technik, Kosten, Emissionen | nein | Klasse A. Die **Herleitungszeile** zum abgeleiteten Wert je kWel steht MITTEN im Kostenraster und spannt über beide Spalten (neue Regel, s. u.) — sonst hätte die Gruppe in zwei Raster zerfallen müssen |
| `Dialoge/Erzeuger/BhkwDialog.razor` (W6.4) | 7 | 1 — Detailblock | nein | **B:** Der Detailblock ist ein Formularblock (dieselbe Bauart wie im Heizkessel-Projektdialog) → Raster. Die zwei **Filterfelder** über der Katalogliste und die **Summenzeile** unter der Projektliste bleiben: Das sind Listenwerkzeuge, keine Parameter eines Geräts |
| `Dialoge/Erzeuger/PhotovoltaikDialog.razor` (W6.5) | 7 | 2 — Anlagenblock, Detailblock | nein | **B:** Der **gestrichelte Rahmen** des Anlagenblocks BLEIBT — er sagt „diese Werte gehören zur markierten Anlage" (`Form_PV_Paint`, vier `DrawLine`-Aufrufe). Der Raster steht DARIN und ordnet nur die drei Felder. Der Herstellerfilter über der Katalogliste bleibt |
| `Dialoge/Erzeuger/StromspeicherDialog.razor` (W6.6) | 2 | 1 — Detailblock | nein | Klasse A |
| `Dialoge/Erzeuger/PufferspeicherDialog.razor` (W6.7) | 2 | 1 — Detailblock | nein | Klasse A. Die zwei Filter (Hersteller, Volumenstufe) über der Katalogliste bleiben |

Die Feldzahl zählt Feldbausteine im Quelltext; die Detailblöcke der vier Projektdialoge
zeichnen über `@foreach` mehr Zeilen, als hier stehen.

**Eine Zeile im Stilblatt kam dazu** (Unterblock „Formularraster — Paket P1"):
`.epos-formularraster > .epos-herleitung { grid-column: 1 / -1; }`. Eine Herleitungszeile
ist kein Feld und darf nicht in einer Rasterzelle **neben** einem Feld landen. Ohne sie
müsste jeder Dialog mit Zwischensatz seinen Block in zwei oder drei Raster zerlegen — und
jede Zerlegung ist eine Stelle, an der die Beschriftungsspalten später auseinanderlaufen.
Die Regel nennt `.epos-formularraster`, greift also nirgends sonst: Eine Herleitungszeile
außerhalb eines Rasters bleibt unverändert.

### Wachen

Je Dialog ein bunit-Fall in der vorhandenen Testdatei (`Der_Eingabeblock_…` bzw.
`Der_Detailblock_steht_im_Formularraster`): Der Block trägt `epos-formularraster` und es
steht ein Feld darin; wo der Block ein Zahlenfeld mit Einheit führt, zusätzlich, dass es
sich als **kurzes** Feld meldet und die Einheit in **derselben** Feldzeile steht.

### Abnahmepunkte am Gerät (100 / 125 / 150 %)

1. „Administration Heizkessel" und „BHKW Verwaltung": In jedem Block steht die Beschriftung
   **links neben** ihrem Feld, das Zahlenfeld ist **kurz**, die Einheit **unmittelbar
   dahinter**; bei breitem Fenster stehen zwei Feldpaare nebeneinander.
2. „BHKW Verwaltung", Gruppe Kosten: Der Satz zum abgeleiteten Wert je kWel steht über die
   **volle Breite** zwischen den Kostenposten und Raumbedarf/Wartung — nicht neben einem Feld.
3. Photovoltaik (Projektmaske) mit markierter Anlage: Der **gestrichelte Rahmen** ist noch da,
   die drei Werte darin stehen mit Beschriftung daneben.
4. BHKW (Projektmaske): Der Detailblock unter der Zweispaltenauswahl ist kompakt; die zwei
   Filter über der Katalogliste und die Summenzeile unter der Projektliste sehen aus wie
   vorher.
5. Fenster schmal ziehen (< 900 CSS-px): Die Beschriftung fällt in allen sechs Masken wieder
   **über** das Feld, nichts wird abgeschnitten.

### Nachtrag 05.09.2026 — der KOMPONENTENBLOCK (Anwenderfoto „Verwaltung BHKW")

Der Anwender, mit Bildschirmfoto der Maske „Verwaltung BHKW": „Stelle diesen Dialog
kompakter dar (insbesondere Daten zum BHKW-Modul unten). Prüfe das Gleiche mit anderen
Komponenten zur Darstellung der Komponentendaten."

**Der Befund.** Der Detailblock stand nach dem Einhängen schon mit Beschriftung neben dem
Feld — aber jedes Anzeigefeld nahm die **volle Feldspalte**, auch „80" hinter „thermische
Leistung [kWth]:". Der Block braucht damit fünf Zeilen, wo drei genügen. Der Grund ist die
Datenform: Die Werte kommen als Paare (Beschriftung, Wert) aus der Hülle
(`BhkwHuelle.DetailZu`) und werden als **nur lesbare `Textfeld`** gezeichnet — ein
`Textfeld` meldete sich dem Raster bis dahin nie als kurz.

**Die Lösung — zwei Zeilen, keine neue Datenform.** `Textfeld` bekommt `Kurz` (setzt
`epos-feld--kurz`, die Gegenrichtung zu `Mehrzeilig` → `epos-feld--breit`), und **welches**
Anzeigefeld kurz ist, entscheidet `ErzeugerDetail.IstZahl(wert)` — an EINER Stelle für
alle sechs Erzeuger-Projektmasken, die denselben Detailblock zeichnen. Die Probe hängt am
**Wert**, nicht an der Beschriftung: Die Feldnamen kommen je Erzeugerart anders herein,
eine Zahl bleibt eine Zahl. Beide Kulturen werden gefragt; rät die Probe falsch, ändert
sich nur die **Breite** eines Anzeigefeldes. Die Beschriftung führt die Einheit ohnehin
schon („[kWth]"), es entsteht kein neuer Text.

**So ist der Block „Modul" der Maske „Verwaltung BHKW" jetzt aufgeteilt** — zwei Spalten,
sobald der Dialog breit genug ist:

| Zeile | links | rechts |
|---|---|---|
| 1 | Modul-Name (Feldspalte) | Hersteller (Feldspalte) |
| 2 | thermische Leistung **[kWth]** (kurz) | elektrische Leistung **[kWel]** (kurz) |
| 3 | Beschreibung — **über beide Spalten**, zweizeilig | |
| 4 | Brennstoff (Auswahlfeld) | Untere Grenzleistung **%** (kurz) |
| 5 | Vorlauf **°C** (kurz) | Rücklauf **°C** (kurz) |

Die Zeilen 4 und 5 stehen nur bei einer **Projektzeile**; bei einem Katalogsatz endet der
Block nach der Beschreibung — das war schon vorher so. Die **Summenzeile** „Summe aller
ausgewählten Module [kWth]" unter der linken Liste bleibt links (Regel #76) und steht
jetzt in einem einspaltigen Raster mit `Kurz` — Beschriftung neben dem Wert, der Wert kurz.
Der **Hinweisbalken** oben („Die Energieträgervariante … ist diesem Projekt bereits
zugeordnet") ist ein `Warnbanner` und bleibt unverändert: eine Zeile, die nur erscheint,
wenn es etwas zu sagen gibt.

**Dieselbe Behandlung für die anderen Komponentendaten-Blöcke:**

| Dialog | Der Komponentenblock jetzt |
|---|---|
| `HeizkesselDialog` (W6.3) | Name und die Detailfelder im Raster (seit dem Vorschritt); die **Zahlen** darunter jetzt kurz. Träger, Vorlauf, Rücklauf folgen als Auswahlfeld und zwei kurze Felder |
| `PhotovoltaikDialog` (W6.5) | Name, die Detailfelder (Zahlen kurz), Beschreibung breit, **Gesamtleistung kurz**; der gestrichelte Anlagenrahmen darüber trägt Neigung, Azimut und Modulzahl in seinem eigenen Raster |
| `StromspeicherDialog` (W6.6) | Name und die Detailfelder im Raster, Zahlen kurz |
| `PufferspeicherDialog` (W6.7) | Name und die Detailfelder im Raster, Zahlen kurz |

**Nicht angefasst — die Spalte „Eigenschaften" der rechten Katalogliste.** Sie bricht in
vier Zeilen um (Hersteller / Brennstoff / Ptherm / Pel). Die Zeile kommt **nicht** aus dem
Baustein `Zweispaltenauswahl` und auch nicht aus einem Listenprofil, sondern wird in
`WindowsFormsApplication1/Views/BHKW/BhkwHuelle.KatalogZeilen` aus vier `\n`-getrennten
Stücken zusammengesetzt und in `<span class="epos-mehrzeilig">` (`white-space: pre-line`)
gezeichnet — ausdrücklich so gebaut, „genau wie im `DataGridView` des Vorläufers". Sie auf
eine Zeile zu ziehen („2‑G Energietechnik GmbH · Stadtgas · 290 kWth / 250 kWel") ist ein
eigener Umbau in der WINDOWS-Hülle samt ihrem Feldkartenabgleich; er ist von hier aus auch
nicht prüfbar, weil `EPOS.UI.Tests` das WinForms-Projekt nicht übersetzt. **Vermerkt, nicht
gemacht.**
