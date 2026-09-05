# iU9 Welle 16a — Der Assistent: Wizard_Stromlastgang, Wizard_Komponenten, WizardParent, ProjektAuswahl — Portprotokoll

> Teilwelle **W16a** des Pakets iU9 (Welle 16 = der Rahmen K5 in drei Teilwellen).
> Grundlage: `iU9_W16_Vermessung.md` (1 907 Zeilen, Stand `4101740`) und die
> Arbeitsanweisung `iU9_W16_Arbeitsanweisung.md`, Abschnitt „W16a — Der Assistent".
> Basis: `975ead5` (Statusblock W15c, Git-Tag `vor-W16`).
> **W16b (Startseite) und W16c (Hauptfenster) sind eigene Teilwellen** mit eigenem
> Gate und eigenem Merge.

---

## 0 — Was die Teilwelle getan hat

**Vier Masken sind verschwunden — der ganze Projektassistent bis auf seine Daten.**
Zusammen 1 694 Zeilen `.cs`, 988 Zeilen Designer und 2 `MessageBox`:

| Maske | `.cs` | Designer | Nachfolge |
|---|---|---|---|
| `Wizard_Stromlastgang` | 108 | 120 | **keine neue Komponente** — `StromganglinieDialog` aus W12 (Befund W12‑O‑3) |
| `Wizard_Komponenten` | 216 | 298 | `EPOS.UI/Dialoge/Bedarf/KomponentenauswahlDialog.razor` (S4) |
| `WizardParent` | 962 | 162 | Baustein `Assistent` + Seite `Seiten/Assistent/AssistentSeite` (S3) |
| `ProjektAuswahl` (uc) | 408 | 408 | der Baustein `ProjektListe` aus W15a (die iZ5-Ausnahme ist eingelöst) |

Dazu gelöscht: `WizardSeite.cs`, `AssistentSeiten.cs`, `IAssistentErzeugerSeite.cs`,
`IAssistentListenSeite.cs`, `Allgemein/IAssistentRahmen.cs` und
`Allgemein/Blazor/BlazorAssistentSeite.cs` — **21 Dateien insgesamt**.

Neu im Kern: `KomponentenBestandCtrl` (K1, verschoben), `AssistentCtrl` (K3) und der
umgezogene `WizardCtrl`. Neu in `EPOS.UI`: der Baustein `Assistent` mit
`AssistentSchritt`, die Seite `AssistentSeite`, der Dialog
`KomponentenauswahlDialog` mit `KomponentenZeile` und der Aufzählungstyp
`Kachelstand`. Neu in der Anwendung: `AssistentHuelle` und
`KomponentenauswahlHuelle`.

**`Views/Wizard` und `Views/Projekt` führen seither KEINE Designer-Maske mehr.**

---

## 1 — Commits

| # | Commit | Inhalt |
|---|---|---|
| W16a.0 | `d10b7b9` | `KomponentenBestand` → `KomponentenBestandCtrl` im Kern (K1), Nachweis **N6** |
| W16a.1 | `76f4279` | `Wizard_Stromlastgang` fällt — Assistentenseite 6 ist `StromganglinieDialog` |
| W16a.2 | `1cce87e` | `Kachel` bekommt `Zustand` und `Aktiv` (Befund W16‑B7) |
| W16a.3 | `1de17f5` | `Wizard_Komponenten` → `KomponentenauswahlDialog` (S4) |
| W16a.4 | `d6641db` | `AssistentCtrl` im Kern (K3) — und `WizardCtrl` gleich mit |
| W16a.5 | (dieser) | Rahmen als Razor; `WizardParent`, `ProjektAuswahl`, `BlazorAssistentSeite` fallen |

---

## 2 — Die sechzehn Annahmen A1 … A16 (Risiko R‑W16‑1)

Die Vermessung stand auf dem Stand nach W12; acht Wellen lagen dazwischen. Jede
Annahme ist vor Wellenbeginn nachgemessen worden.

| # | Annahme | Befund |
|---|---|---|
| A1 | Stapellauf 10 Masken / 11 Designer | **ABWEICHUNG.** Gemessen: **11 Masken**, und die Designerzahl hängt am Suchbaum — **12** über `WindowsFormsApplication1`, **14** über die Repowurzel (die zwei generierten des Kerns kommen dazu). Die Arbeitsanweisung nennt 11/14, das ist der Repowurzel-Lauf. Nach W16a: **7 Masken / 8 Designer** (`WindowsFormsApplication1`) bzw. 10 über die Repowurzel — der Sollwert der Anweisung ist auf die Zahl getroffen |
| A2 | „ja"-Zeuge steht auf `Form_StromTest` | erfüllt (er steht seit W14c auf `MDIMainForm`, der Wurzel selbst — noch stabiler); W16a fasst ihn nicht an |
| A3 | Maskenschlüssel-Zeuge auf `FormMain`/`Masken.ProjektDetail` | erfüllt, unberührt (T1 folgt mit W16b/W16c) |
| A4 | Erreichbarkeit 10 / 0 / 0 / 0 | gemessen **11 / 0 / 0 / 0**; nach W16a **7 / 0 / 0 / 0** |
| A5 | Kleinschreibungs-Zeuge auf `WizardParent.designer.cs` | erfüllt — und mit W16a.5 ins Prüfmuster umgezogen (E‑9) |
| A6 | `Sprungbruecke` mit einem Zweig | erfüllt; **nicht angefasst** (iF22) |
| A7 | `EPOS.UI/Bausteine/` bei 25 `.razor` | gemessen 25 vor der Welle, **27** danach (`Assistent`, `Kachelstand` ist eine `.cs`) |
| A8 | `Form_Hinweis` gelöscht | **ABWEICHUNG, wie vorhergesehen** (Risiko R‑W16‑7): W15b hat sie ausdrücklich stehen gelassen (Entscheid W15b‑E‑1b), ihre drei Aufrufer liegen in `Form_Start`. **W16b nimmt sie mit** — W16a berührt sie nicht |
| A9 | `Form_HelpPopup` bleibt | erfüllt, unberührt |
| A10 | 11 von 13 Assistentenseiten Razor | erfüllt; nach W16a **13 von 13** |
| A11 | `ProjektAuswahl` (uc) noch da | erfüllt; mit W16a.5 gefallen |
| A12 | `EPOS.UI.Tests` über 1 600 Fälle | gemessen 2 091 vor der Welle, **2 139** danach |
| A13 | iOS-Lauf grün, `AppWurzel` mit fünf Ansichten | erfüllt; `AppWurzel` führt nach W16a **sechs** (neu: `ASSISTENT`) |
| A14 | Beide Simulationsseiten bis W16 modal | erfüllt, unberührt — **E‑5 gehört zu W16b** |
| A15 | `Dienste.Projekt` ist `FormStartProjektKontext` | erfüllt, unberührt — K2 gehört zu W16b |
| A16 | Referenzlauf und die 13 Projekte unverändert | erfüllt; byte-gleich (§ 8) |

---

## 3 — Feldkartenabgleich

Die Feldkarte ist vor der Umstellung gezogen worden
(`dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`).

### `Wizard_Komponenten` (24 Kartenzeilen, 1 `MessageBox`)

| Kartenzeile | Nachfolger | ☑ |
|---|---|---|
| `label1` „Projekt-Erstellungskonfiguration" | `Gruppenkopf Titel` = `KOMPAUSW_KOPF` | ☑ |
| `label2` (Erläuterungsabsatz) | `Herleitungszeile Text` = `KOMPAUSW_HINWEIS` | ☑ |
| `label3` „Wärmeerzeuger bzw. Energieerzeuger Komponenten auswählen:" | `h2.epos-untergruppe` = `KOMPAUSW_AUSWAHL` | ☑ |
| `karte_Gebaeude` … `karte_Puffer` (13 `AktionsKarte`) | 13 `Kachel` in `Kachelraster Mindestbreite="250"` | ☑ |
| `pictureBox1` (Zierrat) | entfällt | ☑ |
| `panel_Textvorlagen` mit 7 Vorlage-Label | sieben `MyResource`-Schlüssel `KOMPAUSW_*` | ☑ |
| `karte_Geklickt` (29 Z.) | `BeiKachel` + `Rueckfrage` mit `VorgabeNein` | ☑ |

**Zwei der sieben Vorlagetexte entfallen ersatzlos** (`label_TextNeuFrage`,
`label_TextNeuTitel`): Sie waren Reste eines entfallenen Knopfes „Neues Projekt…"
und hatten im ganzen Bestand keinen Aufrufer (Befund W16‑B13).

### `Wizard_Stromlastgang` (6 Kartenzeilen)

Vollständig abgedeckt von `StromganglinieDialog` (W12) — zwei Listen, „◀"/„▶",
„Bearbeiten…". Der Feldkartenabgleich dazu steht im **W12-Protokoll**; hier kommt
nur der Rückweg dazu (§ 4, A‑1).

### `WizardParent` (7 Kartenzeilen, 2 Abschnitte)

| Kartenzeile | Nachfolger | ☑ |
|---|---|---|
| `btn_Help` (Bild, kein Handler) | `InfoKnopf Schluessel="AssistentSeite.btn_Help"` | ☑ |
| `button_ProjektOeffnen` „Projekt öffnen" | Knopf im linken Band, `WIZ_BTN_PROJEKT_OEFFNEN` | ☑ |
| `label_Projekt` „Bestehendes Projekt auswählen" | `h2` im Band, `WIZ_LBL_PROJEKT` | ☑ |
| `ucProjektAuswahl` (`NurNamensspalte=true`, `AutomatischeVorauswahl=false`) | `ProjektListe NurName="true" AutoVorauswahl="false"` | ☑ |
| `pnlContent` | `.epos-assistent-inhalt` | ☑ |
| `btnBack` „◀ Zurück" / `btnNext` „Weiter ▶" / `btnCancel` „Abbrechen" | die drei Fußknöpfe des Bausteins `Assistent` | ☑ |
| Fenstertitel „Projektassistent" | `WIZ_TITEL` (Titel der `BlazorDialogForm`) | ☑ |

---

## 4 — Die Angleichungen (A‑1 … A‑8)

| # | Was | Wie | Warum |
|---|---|---|---|
| **A‑1** | Der Rückweg der Stromlastgangseite | Im Dialogbetrieb schreibt die Hülle nach dem Schließen zurück; als Assistentenseite meldet die Komponente **jede** Änderung über den neuen Rückruf `Geaendert` | Der Assistent schließt nicht — er blättert. Dasselbe Muster wie `GebaeudeDialog.Geaendert` (W9.2) |
| **A‑2** | Der Kachelanstrich (13 `Paint`-Handler mit `GraphicsPath`) | zwei CSS-Klassen (`epos-kachel-statuspunkt--aus`) | `System.Drawing` gibt es in `EPOS.UI` nicht; das Bild bleibt gleich |
| **A‑3** | `Cursors.Default` auf Karte UND jedem Kind (14 Zeilen) | `Kachel.Aktiv` → `<button disabled>` | Mehr als eine Optik: Der Knopf fällt aus der Tabreihenfolge und meldet sich einer Sprachausgabe als gesperrt |
| **A‑4** | `label1` — neutral und de‑DE wichen ab (Befund W16‑B12) | die **de‑DE**-Fassung („Projekt-Erstellungskonfiguration") | Sie ist die gepflegte |
| **A‑5** | `LoadNewForm` (32 Z. gerechnete Fenstergröße) | CSS-Raster | Eine WebView hat keine Wunschgröße |
| **A‑6** | `Next`/`Back`/`GetNextUpIndex`/`GetNextDownIndex`/`lastIndex` (≈ 190 Z.) | `NaechsteAktive(richtung)` + `LetzteAktive` | Die Seiten sind eine Liste mit `Aktiv`-Kennzeichen, kein Formularwechsel |
| **A‑7** | `WizardParent.Aktiver` / `IAssistentRahmen` | der Wirt reicht seinen Zustand als Delegat herein | Die Richtung stimmt damit wieder: Der Wirt kennt seine Seiten, nicht umgekehrt |
| **A‑8** | Die Fehlermeldung des Speicherwegs | ein `Warnbanner` statt einer `MessageBox`; Titel und Satz in EINER Zeile | Der Baustein kennt keinen eigenen Titel; die `MessageBox` trug ihn als **Fenster**titel |

**Nicht angeglichen, obwohl es naheläge:** Die Reihenfolge der 23 Schreibschritte,
die zwei Pflichtprüfungen, die Rückfrage beim Abwählen, die dreizehn Kacheltitel und
die Seitenreihenfolge sind **wörtlich** übernommen.

---

## 5 — Anwenderfragen

| # | Frage | Stand |
|---|---|---|
| **E‑3** | Die Bitmaske zusammenlegen? | **Vorläufig ja, und der Nachweis steht.** `KomponentenBestandCtrl` liegt im Kern; **N6** hält `Bitmaske(id)` gegen den eingefrorenen `Form_Start.status`-Wert für **alle dreizehn** Referenzprojekte — **keine Abweichung**. Damit ist die Gleichheit erstmals erzwungen statt nur behauptet. `Form_Start.UpdateWizardSymbole` fällt mit W16b |
| **E‑4** | Bekommt `SpeichernAusfuehren` eine Transaktion? | **Halb umgesetzt.** Die **Meldung** ist da: statt 17 stiller `return` nennt `AssistentErgebnis` den fehlgeschlagenen Schritt, und der Assistent bleibt stehen. Die **Transaktion** ist es NICHT: Ein `DbVorgang` über den ganzen Lauf setzte voraus, dass alle 23 Schreibmethoden von `WizardCtrl` (1 737 Z.) ihn hereingereicht bekommen statt jede ihre eigene gepoolte Verbindung zu öffnen — ein Umbau des SCHREIBWEGS, den Risiko R‑W16‑6 ohne einen Feld-für-Feld-Vergleich am Windows-Gerät untersagt. **Frage an den Anwender: eigener Schritt mit eigenem Nachweis, oder bleibt es bei der Meldung?** |
| **E‑5** | Simulationskonfiguration/-ergebnis modal? | **W16a berührt es nicht** — beide Seiten hängen an `Form_Start` (W16b) |
| **E‑9** | Wohin mit den Zeugen? | **Vorläufig umgesetzt für den Kleinschreibungs-Zeugen**: `Wizard_Komponenten` ist eingefroren nach `Pruefmuster/Wizard/` gewandert (5 Dateien), der Test zählt ihn von dort. Der „ja"- und der Maskenschlüssel-Zeuge bleiben bis W16b/W16c am Bestand |
| **W16a‑E‑1 (neu)** | **Der Assistent ist unter Windows MODAL geblieben.** `BlazorDialogForm<AssistentSeite>`, weil beide Aufrufer (`MenueCtrl.ProjektNeu`/`…Bearbeiten`) auswerten, ob gespeichert wurde, und `Form_Start`/`MDIMainForm` danach den Projektkontext nachziehen | Dieselbe Begründung wie R‑W10b‑1/R‑W11‑1. **Mit W16b/W16c könnte er eine freie Ansicht in derselben WebView werden** — soll er? |
| **W16a‑E‑2 (neu)** | **Der Fehlerfall des NEU-Zweigs verhält sich anders als früher.** Schlug `Add_Projekt` fehl, SCHLOSS sich der Assistent bisher kommentarlos; jetzt bleibt er stehen und meldet | Die Eingaben gehen damit nicht mehr verloren. Bestätigen? |

---

## 6 — Befunde

| # | Befund | Folge |
|---|---|---|
| **W16a‑B1** | **„nur Anzeige" ist KEIN dritter Kachelzustand.** Die Vermessung schlug `Aus`/`An`/`NurAnzeige` vor (§ 12.2); der Bestand führt aber ZWEI unabhängige Achsen — die Farbe hängt allein am Bestand, die Anklickbarkeit allein daran, ob die Komponente eine Assistentenseite hat. Brauchwasser ist „nur Anzeige" UND grün oder grau | `Kachelstand` hat zwei Werte, die zweite Achse trägt `Kachel.Aktiv` |
| **W16a‑B2** | **Der Schreibweg konnte gar nicht in den Kern.** `WizardCtrl` (1 737 Z.) enthielt keine Zeile Oberfläche; seine einzige WinForms-Kante war `public WizardParent parentform` mit genau EINEM Schreiber (`WinFormsNavigation:258`) und KEINEM Leser | Feld gestrichen, Klasse verschoben; `GeraeteWaisen.Aufraeumen` läuft über den vorhandenen Haken |
| **W16a‑B3** | **Befund W16‑B18 war zu klein gefasst.** Nicht nur `LoadWBedarfFromDB` ließ sein `RecordSet` offen — `LoadZGeb` ebenfalls | Beide schließen jetzt |
| **W16a‑B4** | **Befund W16‑B35 ist überholt.** Der Baustein `Rueckfrage` HAT einen Parameter `VorgabeNein` — er ist mit iU9‑W14c (A‑1) dazugekommen | Die Rückfrage des Komponentenschritts nutzt ihn; die Wirkung ist damit nicht nur gleich, sondern gleich gebaut |
| **W16a‑B5** | **K5 (`StromganglinieStammCtrl.IdNach`) ist gegenstandslos.** Das konkatenierte SQL fällt mit der Datei, und die parametrisierte Fassung liegt seit W12.0g im Kern (`FindeStamm`, `GetStammId`) | Keine dritte Methode desselben Inhalts |
| **W16a‑B6** | **`AktionsKarte` kann in W16a nicht fallen.** Sechs ihrer neunzehn Instanzen stehen auf `Form_Start.tabPage1` | Sie fällt mit `Form_Start` in W16b — Abweichung von der Löschliste der Arbeitsanweisung, mit Beleg |
| **W16a‑B7** | **`WizardCtrl.speichern` ist ein totes Feld** — dreimal geschrieben, nie gelesen | Wörtlich mitgenommen (harmlos); Streichung wäre ein eigener Schritt |
| **W16a‑B8** | **Die Designerzahl des Stapellaufs hängt am Suchbaum**: 12 über `WindowsFormsApplication1`, 14 über die Repowurzel (die zwei generierten des Kerns). Die Vermessung und die Anweisung nennen beide Zahlen ohne den Unterschied zu benennen | In diesem Protokoll steht durchgängig der `WindowsFormsApplication1`-Lauf |

---

## 7 — Texte

**36 neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx` **und**
`Resource.en-US.resx`:

| Gruppe | Zahl | Bemerkung |
|---|---|---|
| `KOMPAUSW_*` | 24 | drei Kopftexte, drei Satzbausteine, Frage samt Titel, Ja/Nein, Fenstertitel und die **dreizehn** Kachelbeschriftungen |
| `WIZ_KLIMA_*`, `WIZ_NAME_*` | 4 | **Befund W16‑B17**: die vier deutschen Literale der zwei Pflichtprüfungen — die einzigen unlokalisierten Texte einer sonst zweisprachigen Maske |
| `WIZ_SPEICHERN_FEHLER*` | 2 | die EINE Fehlermeldung des Speicherwegs (E‑4), mit `{0}` = Schritt |
| `WIZ_TITEL`, `WIZ_BTN_*`, `WIZ_LBL_PROJEKT` | 6 | die Beschriftungen aus `WizardParent.*.resx`, wörtlich |

`WIZ_BTN_SPEICHERN` gab es bereits (P4).

---

## 8 — Gate

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 6 Warnungen | **0 / 6** (Vollneubau) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 833 + neue | **3 923** (KiKern 450, SpeicherEngine 337, EPOS.Kern.Tests **997**, EPOS.UI.Tests **2 139**) — **+90** |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | gleich | **grün** |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 124 | **123** (der Assistenten-Zeuge ist gestrichen, T2), auch unter `en_US` |
| Stapellauf `--alle WindowsFormsApplication1` | **7 Masken / 8 Designer**, 7 / 0 / 0 / 0 | **7 / 8, 3 lokalisiert, 7 ja / 0 nein / 0 verwaist / 0 unklar** |
| `SqlDialektPruefer` | 0 Fundstellen | **0 von 1 234** |
| `ChartProben` | 32 unverändert | **32 Bilder, 0 Verstöße** |
| Referenzlauf 1030 / 1007 / 1017 gegen `2026-08-30_B3-Kaskade` | byte-gleich | **PASS, 815 043 Werte** (1007: 324 219, 1017: 254 154, 1030: 236 670); `diff -rq` **byte-gleich in allen drei** |
| Wächter `Program.*` im Kern und in den Kernkandidaten | leer | **leer** |
| Wächter `System.Windows.Forms`/`System.Drawing`/`MessageBox.`/`Registry.`/`ProtectedData`/`OleDb` im Kern | leer | **leer** |
| `git grep` auf die gefallenen Klassen | nur Kommentare, Protokolle und das eingefrorene Prüfmuster | erfüllt |

**Nach dem Merge von `origin/ios_migration`** (`3c7e0d6` — die W15c-Entscheide O‑1/O‑2,
der Git-Tag `vor-W16` in der Konzeptnotiz und eine Korrektur am Transferdialog) ist das
ganze Gate ein zweites Mal gelaufen: Build **0 / 6**, **3 923** grün und ebenso unter
`en_US`, Formularkarte **123**, Stapellauf **7 / 8** mit 7 / 0 / 0 / 0, SQL **0 von
1 234**, ChartProben **32**, Referenzlauf **byte-gleich in allen drei Projekten**
(815 043 Werte), beide Wächter leer. **Der Merge lief ohne Konflikt** — die andere
Seite hat weder `EPOS.UI/Seiten/` noch die beiden `Resource.resx` angefasst.

### R‑W16‑6 — der `projekt`-Vergleich

Der Modus `EPOS.Referenzlauf projekt` rechnet, er **schreibt kein Projekt**; ein
Assistentenlauf lässt sich damit auf Linux nicht auslösen. Der Nachweis steht
deshalb als **zwei Kern-Fälle** in `EPOS.Kern.Tests/AssistentCtrlTests.cs`, jeder auf
einer EIGENEN Arbeitskopie:

* `Ein_Bearbeiten_Lauf_ohne_Aenderung_laesst_das_Projekt_stehen` — Projekt 1030 wird
  geladen, unverändert gespeichert; Zählstand der sieben Tabellen, Bitmaske,
  Anlagenbezeichner je Typ und die Kopffelder sind danach gleich.
* `Ein_Neu_Lauf_legt_dasselbe_an_was_er_bekommen_hat` — derselbe Stand wird über den
  NEU-Zweig unter neuem Namen angelegt; Zählstand und Anlagenbezeichner stimmen mit
  der Vorlage überein (ohne die Pufferzeilen, die der Assistent nach FR‑1 nicht
  mitnimmt, und ohne Brauchwasser, für das er keine Seite führt).

**Windows-Abnahmepunkt (Rezept):** `Referenzlauf.exe projekt <id> <ordner>` für ein
über den Assistenten NEU angelegtes und ein BEARBEITETES Projekt, je einmal vor und
nach der Welle, danach `Referenzlauf.exe vergleich <vorher> <nachher>`.

---

## 9 — Windows-Abnahme

| # | Was | Erwartung |
|---|---|---|
| 1 | Menü „Projekt → Neu" | Der Assistent geht modal auf, Schritt 0 zeigt dreizehn Kacheln, alle grau, kein linkes Band |
| 2 | Menü „Projekt → Bearbeiten" | Linkes Band mit Projektliste (nur Namensspalte), „Weiter" gesperrt, bis ein Projekt markiert ist |
| 3 | Ein Projekt markieren | Die dreizehn Kacheln füllen sich aus dem Bestand; grün/grau wie auf der Startseite |
| 4 | Eine BELEGTE Komponente abwählen | Rückfrage mit Klartext („… wird aus dem Projekt genommen. Beim Speichern werden N Einträge gelöscht: …"), **„Nein" hervorgehoben**, Enter nicht belegt, Esc = Nein |
| 5 | Brauchwasser- und Pufferkachel anklicken | Nichts geschieht; beide zeigen ihren Bestand mit „ · nur Anzeige" |
| 6 | Alle dreizehn Seiten durchklicken | Jede Seite baut auf, „Weiter" überspringt abgewählte, auf der letzten aktiven steht „Speichern" |
| 7 | Speichern ohne Klimazone / ohne Namen | EIN Warnbanner mit dem wörtlichen Satz, der Assistent bleibt stehen |
| 8 | Speichern eines vollständigen Laufs | Fenster schließt, Startmaske zeigt das Projekt |
| 9 | „Projekt öffnen" im linken Band | Assistent schließt, Projekt ist aktiv, Startmaske meldet den Wechsel kurz |
| 10 | Sprachwechsel auf Englisch | Alle 36 neuen Texte englisch |
| 11 | DPI 100 / 125 / 150 % | Der Assistent läuft in `BlazorDialogForm` und damit in der `DpiInsel` — scharf |

---

## 10 — Was W16b von hier erbt

| Was | Zustand |
|---|---|
| **Der Rückweg an die Startseite** | `AssistentHuelle.Oeffnen` ruft nach dem modalen Lauf `Program.startfrm.HinweisProjektGeoeffnet()`, wenn „Projekt öffnen" gedrückt wurde (`_hinweisFaellig`). **W16b macht daraus einen Rückruf an die Razor-Startseite** — die Stelle ist die einzige, an der die Hülle `Program.startfrm` anfasst |
| **`IProjektQuelle.AssistentGaben(betriebsart, id)`** | angelegt, mit Standardumsetzung `null`. `AppWurzel` hat den Zweig, `Seitenschluessel.Assistent` den Wert. **`IosProjektQuelle` setzt sie noch nicht um** — der iOS-Lauf zeigt dort die Statuszeile „Der Projektassistent steht auf diesem Gerät noch nicht zur Verfügung." |
| **`Form_Start.UpdateWizardSymbole`** | steht unverändert. Es rechnet dieselben dreizehn Bits wie `KomponentenBestandCtrl` (N6 belegt die Gleichheit); **W16b löscht es ersatzlos** und liest `bestand.Bitmaske` |
| **`AktionsKarte`** | bleibt mit sechs Instanzen auf `Form_Start.tabPage1`; **W16b löscht die drei Dateien** |
| **`Form_Hinweis`** | unberührt (W15b‑E‑1b); **W16b nimmt sie mit** |
| **`Masken.Assistent`** | zeigt jetzt auf `AssistentHuelle.Oeffnen` statt auf `WizardParent`. Der Schlüssel bleibt; K7 legt ihn in W16c mit `Seitenschluessel` zusammen |
| **`MenueCtrl.ProjektNeu`/`ProjektBearbeiten`** | rufen `AssistentCtrl.BETRIEBSART_*` statt `WizardParent.WIZARD_MODE_*` |
| **Die Prüfmuster** | `Pruefmuster/Wizard/` führt jetzt `Wizard_WPItem` UND `Wizard_Komponenten`. W16c legt `Form_Start.Designer.cs` und `MDIMainForm.Designer.cs` dazu (E‑9) |

---

## 11 — Offene Punkte

| # | Punkt |
|---|---|
| **W16a‑O‑1** | **Die Transaktion des Speicherwegs** (E‑4, zweite Hälfte). Umfang: 23 Methoden in `WizardCtrl` auf einen hereingereichten `DbVorgang`; Nachweis: der Windows-`projekt`-Vergleich aus § 8 |
| **W16a‑O‑2** | **`WizardCtrl.speichern`** ist ein totes Feld (B7). Streichen, sobald jemand die Klasse ohnehin anfasst |
| **W16a‑O‑3** | **Der Assistent ist modal.** Sobald die Startseite Razor ist (W16b), könnte er eine freie Ansicht derselben WebView werden — dieselbe Frage wie R‑W10b‑1/R‑W11‑1 |
| **W16a‑O‑4** | **`IosProjektQuelle.AssistentGaben`** ist nicht umgesetzt; der Assistent ist auf iOS damit angekündigt, aber nicht bedienbar (iU11) |

---

## 12 — Windows-Abnahme 05.09.2026 (Befunde W16a‑B‑1 und W16a‑B‑2)

Der Anwender hat den Stand `d3abd94` am Gerät gefahren. Zwei der fünf Meldungen
betreffen diese Teilwelle.

### 12.1 Befund W16a‑B‑1 — „Optionen Profil und Ganglinie sollte eher im Solarthermie Feld sein"

**Beobachtung.** Auf dem Reiter „Energieerzeuger" steht unter der Karte
„Solarthermie" ein Kasten mit den zwei Optionen **Profil** und **Ganglinie**. Er
steht neben den gerahmten Nachbarkacheln im Kachelraster und liest sich damit wie
ein Zusatz zum ganzen Reiter, nicht wie die Weiche **dieser** Karte.

**Wo es steht.** Die Meldung nennt die „Komponentenauswahl"; die Optionsgruppe
gehört tatsächlich zum **Erzeugerreiter der Startseite**
(`EPOS.UI/Seiten/Start/ErzeugerReiter.razor`, W16b.2). Der
`KomponentenauswahlDialog` dieser Teilwelle führt keine Optionsgruppe — er zeigt
dreizehn Kacheln und sonst nichts. Der Befund wird hier geführt, weil der Anwender
ihn hier gemeldet hat; die Änderung liegt in `Seiten/Start/`.

**Warum die zwei Knöpfe überhaupt bei der Kachel stehen.** Sie sind eine **Weiche**,
keine Anzeige: `pBox_Solarthermie_Click` prüft `radioButton_KollektorProfil.Checked`
und öffnet danach **entweder** den Kollektor- **oder** den Gangliniendialog
(`Form_Start` :1262‑1307). Sie gehören also genau zu dieser einen Kachel — und
sollen das auch zeigen.

**Ursache.** Zwei Dinge zusammen. Erstens trug **jede** der sieben Kacheln den Wirt
`epos-startkachel-mit-wahl`, obwohl nur eine eine Wahl hat; der Klassenname sagte
etwas, das für sechs Kacheln nicht stimmte. Zweitens war dieser Wirt ein reiner
Stapelkasten (`display: flex; gap: 6px`) **ohne** Rahmen: Die Kachel behielt ihren
eigenen, die Optionsgruppe stand mit 6 px Abstand darunter im Freien.

**Behebung.**

* **Markup:** Den Wirt bekommt **nur noch** die Solarthermiekachel; die übrigen
  sechs stehen wie auf jedem anderen Reiter unmittelbar im `Kachelraster`.
* **Rahmen:** Der Kartenrahmen (`--epos-karte-rahmen`, `--epos-karte-flaeche`,
  `--epos-ecke`) liegt jetzt am **Wirt**; die Kachel darin gibt ihren eigenen ab
  (`border: 0; background: none`). Die Optionsgruppe sitzt darunter im selben
  Rahmen, bündig unter dem Kacheltext, abgesetzt durch eine feine Linie.
* **Warum nicht über das Markup:** Eine Kachel **ist** ein `<button>` — damit sie
  Tastatur, Enter/Leertaste und Sprachausgabe von selbst kann. Ein `<button>` darf
  keine Auswahlknöpfe enthalten; ein Klick darauf löste sonst die Kachel aus. Der
  Rahmen muss deshalb um beide herum, nicht um eines von beiden.
* **Klickziel und Tastaturweg bleiben, wie sie waren:** Anklickbar ist weiterhin
  genau der Kachelteil (er behält seine Hoverfarbe); die Reihenfolge ist Kachel →
  Profil → Ganglinie, und die zwei Optionen teilen sich wie bisher einen
  `name`, sind also **ein** Tabulatorhalt mit Pfeiltasten darin.

**Wachen.** `EPOS.UI.Tests/Seiten/StartseiteTests`:
`Die_Solarweiche_steht_im_Rahmen_ihrer_Karte` (genau EINE Karte führt die Weiche,
sie steht IN ihr, die Reihenfolge stimmt) und
`Der_Kartenrahmen_der_Solarweiche_liegt_am_Wirt` (die Regel im Stilblatt — eine
bunit-Probe sieht sie nicht, Lehre W6‑B‑1). Der Fall
`Die_Solarweiche_meldet_ihre_Stellung` bleibt unverändert grün.

**Abnahmepunkt A‑W16a‑B‑1.** Reiter „Energieerzeuger": Die Solarthermiekarte trägt
Bild, Titel, Erläuterung, Statuspunkt **und** die zwei Optionen in EINEM Rahmen;
die sechs Nachbarkacheln sehen unverändert aus. Ein Klick auf die Optionen öffnet
nichts, ein Klick auf den Kachelteil öffnet je nach Stellung den Kollektor- oder
den Gangliniendialog.

### 12.2 Befund W16a‑B‑2 — der Parametersatz einer Assistentenseite

Der zweite Teil des Befundes **W9‑B‑1** („Im Projekt gespeichertes Gebäude wird
nicht angezeigt bzw. in der Liste selektiert") liegt in dieser Teilwelle:
`AssistentSeite.SchritteBauen` zog den Parametersatz der **stehenden** Seite bei
jedem `OnParametersSet` neu — also bei jedem Neuzeichnen des Wirtes —, obwohl der
Kopfkommentar der Seite seit W16a.5 „bei JEDEM **Betreten** neu erfragt" sagt. Die
elf Hüllen bauen in ihrer `Gaben`-Methode aber jedesmal eine **neue** Anzeigeliste
aus ihrer Fachliste auf; der lebenden Komponente wurde die Liste damit unter den
Füßen ausgetauscht.

Die Seite merkt sich den Inhalt seither (`_inhalt` / `_inhaltSchritt` /
`_inhaltQuelle`) und erfragt ihn nur beim **Schrittwechsel**, bei einem
**Projektwechsel** im linken Band und bei einem **anderen Gabendelegaten**. Das
entspricht dem Vorbild: `WizardParent.Next` bestückte die Seite, `WizardParent.Back`
gar nicht.

Vollständige Herleitung, Behebung und Abnahmepunkt: **W9‑Protokoll § 12.1**.
Wachen: `EPOS.UI.Tests/Seiten/AssistentTests` (drei Fälle).
