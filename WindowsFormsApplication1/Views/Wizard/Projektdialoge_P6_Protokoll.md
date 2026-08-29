# Projektdialoge vereinheitlichen — Umsetzungsprotokoll Paket P6 (Abnahme und Doku-Angleich)

Stand: 29.08.2026. Grundlage:
[`Konzept_Projektdialoge_Vereinheitlichung.md`](../../../Konzept_Projektdialoge_Vereinheitlichung.md)
§ 5/P6, § 6 (Prüfliste) und § 8 (Fallstricke). Vorgänger:
[`Projektdialoge_P1P3_Protokoll.md`](../Hauptformular/Projektdialoge_P1P3_Protokoll.md) und
[`Projektdialoge_P4P5_Protokoll.md`](Projektdialoge_P4P5_Protokoll.md) (HEAD bei Beginn: `9a57069`).

**Umfang von P6:** Hilfe-Anbindung der in P1–P5 neuen und umgebauten Masken nachziehen,
die Prüfliste § 6 belegen, soweit sie ohne Oberflächenbedienung belegbar ist, und die vier
Wiki-Seiten samt Vertragstabelle an die neue Wirklichkeit angleichen (D1–D10). **Am
Rechenkern, an der Simulation und an den Fachdialogen wurde nichts geändert.**

---

## 1. Was sich geändert hat

### Geänderte Dateien (vier)

| Datei | Änderung |
|---|---|
| `Views/Projekt/Form_ProjektAuswahl.Designer.cs` | `btn_Help` angelegt, verdrahtet und als erstes Control eingehängt (ZOrder 0) |
| `Views/Projekt/Form_ProjektAuswahl.resx` | zehn `btn_Help`-Schlüssel; Kopfstreifen freigeräumt (ClientSize/MinimumSize +34 px, `ucAuswahl` und Knopfreihe nach unten); ZOrder der drei Altcontrols um 1 verschoben |
| `Allgemein/Hilfe/help_mapping.txt` | neue Zeile `Form_ProjektAuswahl.btn_Help = Projektverwaltung` samt Kommentar im Abschnitt „--- Projekt ---" |
| `Allgemein/KI/HilfeKontext.cs` | `{ "Form_ProjektAuswahl", B_PROJEKT }` alphabetisch in `BEREICH_JE_TYP` |

**Nicht angefasst:** `Form_ProjektAuswahl.de-DE.resx`/`.en-US.resx` (führen nur Texte — der
Infoknopf trägt keinen), `MyResource/Resource*`, `DbWerte.cs`, `SchemaMigration.cs`,
`WikiWissen.cs`, `BhkwPlan.cs` (fremd geändert, siehe § 7), alle Simulationsklassen.
Kein Git-Schreibbefehl, kein Schreibzugriff auf `Kenndaten.accdb`.

### Wiki (nur Entwürfe, kein schreibender Zugriff)

Fünf Seiten als Importdatei — Ablage und Einzelheiten in § 6.

---

## 2. Teil A — Inventur der Hilfe-Anbindung

### 2.1 Befund je Maske aus P1–P5

| Maske | Herkunft | Infoknopf | Zeile in `help_mapping.txt` | `HilfeKontext` | Befund |
|---|---|---|---|---|---|
| `Form_Start` | P2 umgebaut (6 Kacheln) | **6 Knöpfe** (`btn_Help`, `…_Kurzanleitung`, `…_Waermebedarf`, `…_Strombedarf` im Designer; `…_Energieerzeuger`, `…_Simulation` über `InfoKnopf.Anbringen` in `Form_Start.cs:47/48`) | 6 Zeilen | `Hauptfenster` | **hat überlebt** — der Kachelumbau hat `btn_Help_Kurzanleitung` auf `tabPage1` nicht berührt (nur seine ZOrder wurde nachgezogen, P1P3-Protokoll 3.1) |
| `WizardParent` | P4 umgebaut | `btn_Help` im Designer, in `pnlLeft` bei 14/162, 28×28 | `WizardParent.btn_Help = Kurzanleitung` | `Assistent (Wizard)` | **hat überlebt** — die Spaltenverbreiterung 219 → 300 px und der Austausch `listBox_Projekte` → `ucProjektAuswahl` haben den Knopf nicht verschoben. Er sitzt in der Lücke zwischen Logo (y 14…154) und `label_Projekt` (y 204) |
| `Form_ProjektSpeichernUnter` | P2/P3 berührt | `btn_Help` im Designer bei 496/17 | `= Projektverwaltung` | `Projektverwaltung` | **hat überlebt** — P3 hat nur den Load-Handler umbenannt und den en-US-Titel richtiggestellt |
| **`Form_ProjektAuswahl`** | **P3 neu** | **fehlte** | **fehlte** | fiel auf die Kennungsstufe zurück | **mit P6 ergänzt** (alle drei) |
| `ProjektAuswahl` (UserControl) | P3 neu, P4 erweitert | keiner | keine | — | **bewusst ohne**: Der Baustein steckt in `Form_ProjektAuswahl` und in `WizardParent`; beide Hüllen tragen ihren eigenen Knopf. Ein dritter im UserControl stünde in der Seitenspalte des Assistenten direkt neben dem vorhandenen |
| `AktionsKarte` | P1 neu | keiner | keine | — | **bewusst ohne**: eine Kachel, keine Maske |
| `Wizard_Komponenten` | P5 umgebaut | keiner (auch vorher nicht) | keine (auch vorher nicht) | `Assistent (Wizard)` | **unverändert** — Altstand; die Seite läuft im Rahmen, dessen `btn_Help` oben links daneben steht. Kein Regressionsbefund, aber ein offener Punkt (§ 7, O2) |

### 2.2 Der neue Infoknopf auf `Form_ProjektAuswahl`

**Muster:** `Form_ProjektSpeichernUnter` (Aussehen, `TabStop = false`, Einhängen als
erstes Control) kombiniert mit dem Serialisierungsmuster der Nachbardateien. Die Maske ist
`Localizable = True` — Geometrie gehört deshalb in die `.resx`, nicht in den Designer;
genau so führt es `Form_Start` bei `btn_Help_Kurzanleitung`:

```csharp
//
// btn_Help
//
btn_Help.BackColor = System.Drawing.Color.Transparent;
btn_Help.BackgroundImage = Properties.Resources.help_icon;
resources.ApplyResources(btn_Help, "btn_Help");
btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
btn_Help.FlatAppearance.BorderSize = 0;
btn_Help.Name = "btn_Help";
btn_Help.TabStop = false;
btn_Help.UseVisualStyleBackColor = false;
```

`Controls.Add(btn_Help);` steht **vor** den drei übrigen — der zuerst eingehängte Knopf
liegt auf Index 0 und damit oben auf (dasselbe Muster wie `Form_ProjektSpeichernUnter` und
`Form_Start`). Die ZOrder-Angaben in der `.resx` sind entsprechend nachgezogen:
`btn_Help` 0, `btn_Abbrechen` 1, `btn_OK` 2, `ucAuswahl` 3.

**Kollisionsregel oben rechts — warum die Maske dafür wachsen musste.** Die obere rechte
Ecke war vollständig belegt: `ucAuswahl` lag bei 12/12 auf 540×330, und dort sitzt in
seinem Inneren das **Suchfeld** (`textBox_Suche`, 69/11, 459×23, verankert Top-Left-Right)
— also ein `TextBoxBase` und damit nach `InfoKnopf.Bedienbar` ausdrücklich nichts, was
verdeckt werden darf. `InfoKnopf.Anbringen` hätte den Knopf trotzdem dort abgelegt: In der
strengen Runde findet er innerhalb von 200 px keinen freien Platz, und in der nachgiebigen
zählt `ucAuswahl` als UserControl nicht als Hindernis. Der Knopf wäre auf dem rechten Ende
des Suchfelds gelandet.

Stattdessen ist ein **Kopfstreifen von 34 px** entstanden — dieselbe Anordnung wie in
`Form_ProjektSpeichernUnter`, wo `btn_Help` bei y 17 über der Liste (y ab 51) steht:

| Schlüssel | vorher | nachher |
|---|---|---|
| `$this.ClientSize` | 564, 394 | **564, 428** |
| `$this.MinimumSize` | 480, 340 | **480, 374** |
| `ucAuswahl.Location` | 12, 12 | **12, 46** |
| `btn_OK.Location` | 370, 352 | **370, 386** |
| `btn_Abbrechen.Location` | 464, 352 | **464, 386** |
| `btn_Help` | — | **524, 12 · 28×28 · Anchor `Top, Right`** |

Größe der Liste, alle Anker und die Knopfbreiten sind unverändert; der Knopf hat 12 px
Abstand nach oben und nach rechts. Der Anker ist gesetzt, weil die Maske eine
`MinimumSize` führt und damit größenveränderlich ist (Prüfstand B misst das, siehe § 4.2).

### 2.3 `help_mapping.txt`

Neu im Abschnitt „--- Projekt ---", mit demselben Spaltenraster wie die Nachbarzeilen
(`=` in Spalte 37):

```
# P6: Die Huellform "Projekt oeffnen" (Views\Projekt\Form_ProjektAuswahl.cs) ist
# seit P3 der Weg hinter Menue "Projekt -> Oeffnen..." und hinter der Kachel
# "Zuletzt geoeffnet" - dieselbe Projektliste, dieselbe Seite wie beim
# Duplizieren daneben.
Form_ProjektAuswahl.btn_Help        = Projektverwaltung
```

Kommentar bewusst **ohne Umlaute** — der Bestand schreibt seine Kommentare durchgängig in
`ae/oe/ue` (die Umlaute stehen nur in den Zielen). Die Datei bleibt UTF-8 **mit** BOM,
CRLF; geschrieben über PowerShell mit `UTF8Encoding($true)`, nie über das Bash-Werkzeug.
Zeilenzahl: **175 → 176** Zuordnungen.

### 2.4 `HilfeKontext`

```csharp
// P6 nachgetragen: die Huellform "Projekt oeffnen" aus Paket P3. Ohne
// Eintrag griff erst die Kennungsstufe ("projekt" im Typnamen) - das
// Ergebnis war zwar dasselbe, aber unbeabsichtigt.
{ "Form_ProjektAuswahl",         B_PROJEKT },
```

Der Eintrag steht alphabetisch **vor** `Form_ProjektDelete`. `BEREICH_JE_TYP` führt damit
125 Einträge. Der Befund dahinter: Vor P6 lieferte `BereichFuer("Form_ProjektAuswahl", …)`
zwar auch schon `Projektverwaltung`, aber erst über `BEREICH_JE_KENNUNG` (Regel
`"projekt"`) — also zufällig richtig statt gepflegt.

### 2.5 E5-Konsistenz: `B_ASSISTENT` bleibt „Assistent (Wizard)" — Begründung

Der Auftrag sah vor, `HilfeKontext.B_ASSISTENT` von `"Assistent (Wizard)"` auf
`"Projektassistent"` zu ändern, **sofern der String nur ein KI-Kontextbegriff ist**.

**Er ist es nicht.** Der Wert dient an einer zweiten Stelle als **Vergleichswert**:

* `Allgemein/KI/WikiWissen.cs:399` führt ihn als **Schlüssel** in
  `SEITE_JE_BEREICH` (`Dictionary<string,string>`, `OrdinalIgnoreCase`):
  `{ "Assistent (Wizard)", "Kurzanleitung" }`. Über diese Tabelle findet
  `WikiWissen.KontextSeite()` die Rubrik-Unterseite zum aktuellen Bereich.

Gemessen (Prüfstand C, § 4.2):

| Aufruf | Ergebnis |
|---|---|
| `WikiWissen.KontextSeite("Bereich: " + BereichFuer("WizardParent", ""))` | `Kurzanleitung` |
| `WikiWissen.KontextSeite("Bereich: Projektassistent")` | **`""` — keine Seite** |

Eine Umbenennung **allein** in `HilfeKontext.cs` nähme dem Hilfe-Assistenten also still die
Kontextseite. Nach der Auftragsregel („dient er als Steuerwert/Vergleichswert: NICHT
ändern") bleibt der Wert deshalb stehen. Er ist reiner Innentext: Er wird **nirgends
persistiert** (kein Vorkommen in `DbWerte.cs`, in keinem SQL, in keiner `.resx`), taucht in
keiner Oberfläche auf und ist damit nicht von der Drei-Schichten-Regel betroffen — die
Sichtbegriffe der Oberfläche stehen längst auf „Projektassistent" (P4).

Der saubere Weg wäre eine **Zweizeilenänderung im selben Zug** (`HilfeKontext.cs:34` und
`WikiWissen.cs:399`); sie steht als offener Punkt O1 in § 7 und braucht eine ausdrückliche
Freigabe.

### 2.6 `help_cache.json`

Die Zielseite **`Projektverwaltung` ist im mitgelieferten Startbestand bereits enthalten**
(`/wiki/programm_dokumentation/projektverwaltung/`). Der Bestand bleibt bei **32** Seiten —
ein neuer Schlüssel in `help_mapping.txt` braucht keinen eigenen Cache-Eintrag, nur seine
Zielseite muss der Katalog kennen. **Nichts zu tun** (Prüfstand F).

---

## 3. Teil B — Prüfliste § 6, Stand nach P6

| # | Prüfung | Stand | Beleg |
|---|---|---|---|
| 1 | Jede geänderte/neue Form im VS-Designer öffnen | **offen (nur am Rechner des Nutzers)** | § 5 |
| 2 | Alle sechs Projekt-Kacheln klicken (DE + EN) | offen (UI) | Ziele und Texte maschinell belegt: `p13probe` 93/93 |
| 3 | Menü „Projekt → Öffnen…" | offen (UI) | Ladeweg maschinell belegt: `p13probe` Teil D |
| 4 | „Speichern unter…" | offen (UI) | `p13probe` Teil D |
| 5 | Assistent Neu: kompletter Durchlauf | offen (UI) | Beschriftungen je Kultur: `p45probe` Teil B |
| 6 | Assistent Bearbeiten: Projektwahl links | offen (UI) | `p45probe` Teil B |
| 7 | Komponenten abwählen (E3) | offen (UI) | Löschweg gegen Wegwerf-DB: `p45probe` Teil F |
| 8 | Komponentenstand dreifach vergleichen | offen (UI) | Bitmaskengleichheit für **jedes** Projekt: `p45probe` Teil E |
| 9 | Brauchwasser/Pufferspeicher im Assistenten | offen (UI) | 13 Kacheln vorhanden: `p45probe` Teil E |
| 10 | Logo-Klick im Assistenten | offen (UI) | 0 Click-Abonnenten: `p45probe` Teil B |
| 11 | **Info-Buttons/Bereichshilfe der umgebauten Masken** | **belegt** | `p6probe` Teile A–F, § 4.2 |
| 12 | **CP1252-Stichprobe** | **belegt** | § 3.1 |
| 13 | **Referenzlauf** | **belegt** | § 4.4 |
| — | **Build** | **belegt** | § 4.5 |

### 3.1 Prüfung 12 — CP1252

P6 hat **keine** der sechs CP1252-Dateien angefasst. `git status` führt außer den vier
Dateien aus § 1 nur `Allgemein/BhkwPlan.cs` (fremde Änderung, § 7). Damit sind
`Form_Waermebedarf.cs`, `Form_Prozesswaerme.cs`, `Form_Stromverbraucher.cs`,
`Form_SolarKollektoren.cs`, `SectionPanel.cs` und `ChartManagerNeu.cs` diff-sauber.
Die vier geänderten Dateien sind sämtlich UTF-8 (zwei mit BOM: `.resx` und
`help_mapping.txt`; zwei ohne: die beiden `.cs`) — Kodierung und BOM-Zustand je Datei
vor dem Schreiben geprüft und danach mit einer Umlaut-Rundprobe bestätigt
(`$this.Text` liest „Projekt öffnen" zurück, `Form_Waermebedarf.btn_Help = Wärmebedarf`
steht unverändert in der Zuordnung).

---

## 4. Beweise

### 4.1 Werkzeug

`dev\p6probe` (Wegwerf, gitignored) — SDK-Konsolenprojekt `net8.0-windows`, x64, geladen
wird gegen `dev\build_p6`.

**Zwei Fallen beim Aufsetzen, beide echt zugeschlagen:**

1. **`<Private>true</Private>` an der App-Referenz geht nicht.** Wird
   `WindowsFormsApplication1.dll` in den Probenordner kopiert, lädt der
   `AssemblyLoadContext.Resolving`-Haken danach `System.Text.Json` **zweimal** und die
   Probe stirbt mit `FileLoadException 0x80131621` (Version 10.0.0.0). Alle fünf
   bestehenden Proben stehen deshalb auf `<Private>false</Private>` — der Programmstand
   liegt genau einmal, nämlich in `build_p6`. Zusätzlich braucht die Probe
   `<PackageReference Include="System.Text.Json" Version="10.0.3" />` (Muster `h7probe`),
   sonst versucht der Haken die 10.0 aus dem Build-Ordner zu laden.
2. **`WizardParent` hat zwei Konstruktoren, und der parameterlose ruft
   `InitializeComponent()` NICHT auf** (`WizardParent.cs:102`). Der Regelweg „Konstruktor
   mit den wenigsten Parametern" liefert dort eine Maske ohne Oberfläche — `Name` leer,
   kein `btn_Help`. Die Probe geht deshalb denselben Sonderweg wie `p45probe`:
   `new WizardParent(AssistentSeiten.Erzeugen())`.

### 4.2 Prüfstand `dev\p6probe` — 53 Prüfungen, alle grün

Lauf: `dev\p6probe\lauf_final.txt`.

| Teil | Prüfungen | Inhalt |
|---|---:|---|
| A Zuordnung | 11 | eingebettete `help_mapping.txt` (also der **ausgelieferte** Stand aus der DLL) gegen den eingebetteten Katalog-Startbestand: **176/176 Ziele lösen auf**, 32 verschiedene Zielseiten, keine doppelte linke Seite; die sieben Schlüssel der Projektdialoge einzeln geprüft |
| B Extender | 7 | sechs Masken gebaut, elf Knöpfe gefunden, `HelpExtender.RegisterBaum` gelaufen: **11/11 „grün" (Enabled)**; jeder Knopf hat eine Zeile; kein Knopf verdeckt ein bedienbares Geschwister; Anker hält den rechten Abstand; zweiter `InfoKnopf.Anbringen`-Aufruf erzeugt keinen zweiten Knopf (6/6) |
| C HilfeKontext | 11 | Bereich je Maskentyp (7 Masken), `BEREICH_JE_TYP` führt `Form_ProjektAuswahl` (125 Einträge), dazu der Nachweis aus § 2.5 |
| D Designer-Lint | 7 | Regelwerk unverändert aus `p13probe`/`p45probe`; `Form_ProjektAuswahl.Designer.cs` **OK** — 38 Anweisungen, 15 VS-Kommentare, 4 Felder |
| E Ressourcen | 14 | drei `.resx` wohlgeformt (43 / 2 / 2 Schlüssel); alle zehn `btn_Help`-Schlüssel; Anchor/Größe/Parent/ZOrder; Umlaut erhalten; Geometrie kollisionsfrei |
| F Cache | 3 | `Projektverwaltung` und `Kurzanleitung` im Startbestand, 32 Seiten unverändert |

**Die gemessenen Knopflagen** (nach einem Layoutdurchgang):

| Maske | Knopf | Elternelement | Lage | Ziel |
|---|---|---|---|---|
| `Form_Start` | `btn_Help` | Form | 23,130 · 51×39 | Programmablauf |
| `Form_Start` | `btn_Help_Kurzanleitung` | TabPage | 1187,19 · 51×39 | Kurzanleitung |
| `Form_Start` | `btn_Help_Waermebedarf` | TabPage | 1201,16 · 51×39 | Wärmebedarf |
| `Form_Start` | `btn_Help_Strombedarf` | TabPage | 1196,26 · 51×39 | Strombedarf |
| `Form_Start` | `btn_Help_Energieerzeuger` | TabPage | 1196,20 · 51×39 | Energieerzeuger |
| `Form_Start` | `btn_Help_Simulation` | TabPage | 1196,20 · 51×39 | Simulation |
| `WizardParent` | `btn_Help` | `pnlLeft` | 14,162 · 28×28 | Kurzanleitung |
| `Form_ProjektSpeichernUnter` | `btn_Help` | Form | 496,17 · 28×28 | Projektverwaltung |
| **`Form_ProjektAuswahl`** | **`btn_Help`** | **Form** | **524,12 · 28×28** | **Projektverwaltung** |
| `Form_ImportKonflikte` | `btn_Help` | Form | 210,380 · 28×28 | Projektverwaltung |
| `Form_ProjektExportImport` | `btn_Help` | Form | 440,12 · 28×28 | Projektverwaltung |

**Zwei Altbefunde, gemessen und bewusst nicht geändert** (sie liegen außerhalb von P6;
die Probe meldet sie unter „Altbefunde"):

* `Form_ProjektSpeichernUnter.btn_Help` trägt **keinen Anker**, die Maske ist aber
  größenveränderlich (kein `FormBorderStyle` gesetzt → `Sizable`). Beim Verbreiten um
  120 px wächst der Abstand zum rechten Rand von 20 auf 140 px — der Knopf bleibt links
  liegen. Eine Zeile `btn_Help.Anchor = Top | Right` behebt das; sie gehört zu den
  Altpunkten dieser Maske (P1P3-Protokoll O2/O3).
* `Form_ImportKonflikte.btn_Help` ist **absichtlich** `Bottom | Left` verankert und sitzt
  in der unteren Knopfleiste neben „Alle auslassen" (`Form_ImportKonflikte.cs:170–184`).
  Der Rechtsabstand ist dort kein Kriterium — **kein Befund**.

Die Probe liest die Produktivdatenbank nur **lesend** (der `WizardParent`-Konstruktor holt
über `ApplikationCtrl.ReadSingle()` das Anwendungs-Icon). Vor dem Lauf geprüft: keine
`Kenndaten.laccdb` vorhanden, die Anwendung lief also nicht. Kein Schreibzugriff.

### 4.3 Regressionsnetz — alle bestehenden Proben gegen `build_p6`

| Prüfstand | Lauf | Ergebnis |
|---|---|---|
| `p13probe` (Schnitt 1: Karten, Startmaske, ProjektAuswahl, MenueCtrl) | `dev\p13probe\lauf_p6.txt` | **ALLES GRUEN (93 Prüfungen)** |
| `p45probe` (Schnitt 2: Assistent, Kachelauswahl, E3-Löschweg) | `dev\p45probe\lauf_p6.txt` | **ALLES GRUEN (115 Prüfungen)** |
| `h7probe` (Info-/Hilfeknöpfe, 72 Masken) | `dev\h7probe\lauf_p6.txt` | **ALLES GRUEN** |
| `h11probe` (Sammelpaket H11) | `dev\h11probe\lauf_p6.txt` | **ALLES GRUEN** |
| `h12probe` (feldgenaue Hilfe) | `dev\h12probe\lauf_p6.txt` | **ALLES GRUEN** |

**Drei Proben tragen eine fest verdrahtete Zahl der Zuordnungszeilen** und liefen deshalb
im ersten Anlauf rot — ausschließlich wegen der **einen** neuen Zeile, nicht wegen eines
Fehlers:

| Prüfstand | Konstante | vorher | nachher |
|---|---|---:|---:|
| `h7probe` | Zusicherung im Prüftext | 175 | 176 |
| `h11probe` | `ZUORDNUNGEN_SOLL` | 175 | 176 |
| `h12probe` | `ZUORDNUNGEN_BESTAND` | 99 | 100 |

Diese drei Konstanten sind nachgezogen, dazu in `h7probe`, `h11probe` und `p45probe` der
Build-Ordner `build_p45` → `build_p6`. Von jeder betroffenen `Probe.cs` liegt eine
Sicherung `Probe.cs.p6bak` daneben; bei `h7probe`/`h11probe` wurde das UTF-8-BOM nach dem
Schreiben wiederhergestellt (Rundprobe im selben Lauf).

### 4.4 Prüfung 13 — Referenzlauf

**Aufbau.** `Referenzlauf.csproj` mit `-p:OutDir=C:\Waermeplan\WP_Plan\dev\refbin_p6\`
gebaut; weil `OutDir` als globale Eigenschaft auch für die referenzierte Anwendung gilt,
liegen Werkzeug und App-DLL im selben Ordner und sind zueinander konsistent — und `bin\`
bleibt unberührt (nachgeprüft: unveränderter Zeitstempel).

**Datenbestand.** `Referenzlauf.exe liste` zeigt den **13-Projekte-Bestand** (1039–1042
vorhanden, dazu neu 1043/1044). Nach `Referenzlaeufe\LIESMICH.md` ist dafür
`2026-08-29_Booster` die Basis.

**Lauf** (`dev\ref_p6`, feste Liste 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030,
1039, 1040, 1041, 1042): **13 von 13 erfolgreich, 332 CSV, Gesamtdauer 15 s.**

**Vergleich gegen `2026-08-29_Booster`:**

| Ergebnis | Projekte | Werte |
|---|---|---:|
| **PASS** | 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1039, 1040, 1041 | **2 979 816** |
| FAIL | 1030, 1042 | 236 667 bzw. 341 836 |

**Die beiden Abweichungen sind Datenänderungen des Anwenders, keine Codewirkung** — belegt
durch einen A/B-Lauf auf **derselben** Datenbank:

```
Referenzlauf.exe (Stand 28.08.2026 22:36, also VOR P4/P5/P6)  ->  dev\ref_altcode
Referenzlauf.exe (Stand P6, dev\refbin_p6)                    ->  dev\ref_p6
vergleich dev\ref_altcode dev\ref_p6_sub
  Projekt_1030: PASS (22 Dateien, 236659 Werte)
  Projekt_1042: PASS (34 Dateien, 341836 Werte)
  GESAMT: PASS (578495 Werte innerhalb der Toleranz)
```

Der alte und der neue Programmstand rechnen auf den heutigen Daten **identisch**. Die
Abweichungen zur Basis sind entsprechend kategorialer Natur — in 1030 fehlt das BHKW-Modul
„Agenitor 306(250kw.el) Gas" ganz (`Eintrag fehlt im Vergleichslauf`), in 1042 sind
Kessel- und Pufferzuordnung umgebaut (`Vitocrossal 200 CM2` → `ecoTEC plus VC 1206/5-5`,
Speicherreihenfolge und `Verwendung` vertauscht). Beides sind Zuordnungsänderungen in der
Datenbank, keine Zahlenverschiebung.

Zusammen also **3 558 311 verglichene Werte ohne eine einzige codebedingte Abweichung**.
Das ist zu erwarten: P6 hat keine Zeile im Simulationspfad angefasst.

Ablagen: `dev\ref_p6_lauf.txt`, `dev\ref_p6_vergleich.txt`, `dev\ref_altcode_lauf.txt`,
`dev\ref_ab_altcode_gegen_p6.txt`, `dev\refbin_p6_liste.txt`. **Die Referenzbasis wurde
nicht gewechselt** — `Referenzlaeufe\` ist unverändert (bis auf die `Arbeitskopie`, die
jeder `lauf` ohnehin neu anlegt).

### 4.5 Build

```
MSBuild WP-Plan.sln -t:Rebuild -p:Configuration=Debug -p:Platform=x64
        -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_p6\
```

→ **0 Fehler, 5 Warnungen** — dieselben fünf wie vor dem Paket
(CS0108 `WErzeugerModel.ID_Projekt`, 2× CS0109 `KlimaregionStammCtrl`,
CS0108 `StromverbraucherStammCtrl.items`, CS1998 `MDIMainForm.cs:489`).
Keine neue Warnung. Protokoll: `dev\build_p6.log`.

---

## 5. Offene UI-Prüfpunkte (nur am Rechner des Nutzers)

1. **`Views\Projekt\Form_ProjektAuswahl.cs` im VS-Designer öffnen** (Kern-Abnahmekriterium,
   Konzept § 6/1). Erwartung: öffnet ohne Fehler, der Infoknopf sitzt oben rechts über der
   Liste, der Kopfstreifen ist 34 px hoch, OK/Abbrechen sitzen unverändert unten rechts.
2. **Den Dialog einmal größer ziehen** — der Infoknopf muss oben rechts kleben bleiben
   (Anker `Top, Right`), die Liste mitwachsen.
3. **Infoknopf anklicken** (Menü „Projekt → Öffnen…" und Kachel „Zuletzt geöffnet"):
   Es muss die Wiki-Seite *Programm Dokumentation/Projektverwaltung* aufgehen. Bei
   englischer Oberfläche dieselbe Seite über den Übersetzungs-Proxy.
4. Die Prüfpunkte 2–10 der Konzept-Prüfliste (§ 3) stehen weiterhin offen; sie sind in den
   Protokollen zu P1–P3 und P4–P5 einzeln aufgeführt.

---

## 6. Teil C — Wiki-Angleich (Entwürfe, kein schreibender Zugriff)

**Ablage:**
`…\scratchpad\p6_wiki\` mit `import_p6.xml` (Importdatei), `Aenderungsuebersicht.md`
(Seite für Seite: was geändert, welche Anker erhalten), `ist\` (byte-genauer Live-Stand
vor der Änderung), `neu\` (Entwürfe), `seiten\` (dieselben, in Importreihenfolge),
`build_xml.ps1` (Erzeuger samt Gegenprobe).

**Grundlage byte-genau.** Für jede der fünf Seiten stimmt die Größe der verwendeten
Ist-Fassung exakt mit der von `api.php?…&rvprop=size` gemeldeten überein
(1 395 / 1 235 / 3 612 / 7 689 / 16 185 Bytes). Zwei Seiten kamen dabei aus den früheren,
erfolgreich importierten XML-Dateien im Scratchpad (`w3\import_w3.xml`,
`h7\rubrik_import_h7.xml`), zwei über `action=raw`.

**Die fünf Seiten:**

| Seite | Bytes | Kern der Änderung | erhaltene Anker |
|---|---:|---|---|
| `Programm Dokumentation/Kurzanleitung` | 1 395 → 3 208 | die sechs Karten des Reiters „Projekt" namentlich (D6/D7/D9), Kachelfeld statt Häkchen (D3/D4), Anzeigekacheln (D10), `Weiter ▶`/`◀ Zurück` (D1), Startmaske ehrlich (D8) | keine (die Seite führt keine) |
| `Programm Dokumentation/Projektverwaltung` | 1 235 → 2 243 | vier neue Punkte: Projektliste, Suchen, Öffnen (**dupliziert nichts**, D5), Zuletzt geöffnet; die sechs Altpunkte wortgleich | keine |
| `Projekt anlegen` | 3 612 → 4 829 | Kachelfeld statt Häkchenliste, falscher Satz zur Simulationskonfiguration entfernt (D4), „Öffnen" mit Liste/Suche/Geändert (D5), Bearbeiten = Assistent (D9), neue `{{Achtung}}`-Box für die E3-Rückfrage | **alle 4** (7 Namen) + `{{Navigation Programmablauf}}` + Fußzeile |
| `Erste Schritte` | 7 689 → 8 126 | zwei Absätze: „Einfache Handhabung" (D2/D3/D8) und „Schritt 2" (D3/D4) | **alle 14** |
| `Programm Dokumentation` (Vertragstabelle) | 16 185 → 16 354 | **eine** neue Tabellenzeile `Form_ProjektAuswahl.btn_Help`; Einleitungssatz 99 → 100 Aufrufstellen | **beide** |

**Anker-Gegenprobe.** Vor dem Schreiben geprüft, welche Zuordnungen auf diese Seiten
zeigen: `= Kurzanleitung` (2 Zeilen) und `= Projektverwaltung` (5 Zeilen, davon die neue) —
**keine einzige mit `#anker`**. Auf beiden Rubrikseiten gibt es folglich keine Sprungmarke
zu erhalten; die Anker der beiden Programmablauf-/Einstiegsseiten sind vollständig
übernommen. Maschinell gegengeprüft: Ankerliste und Kategorien je Seite sind zwischen
`ist\` und `neu\` **identisch**.

**Wo die Vertragstabelle steht.** Die Wiki-Suche hilft hier nicht — sowohl
`api.php?action=query&list=search` als auch `rest.php/v1/search/page` liefern **null
Treffer** (das Wiki führt keinen Suchindex). Gefunden über den Hinweis im Kopf von
`help_mapping.txt`: Rubrikseite `Programm Dokumentation`, Abschnitt „Hilfeseiten".

**Gegenprobe der Importdatei** (`build_xml.ps1`, Muster `w3\build_xml.ps1`): 5
`<page>`-Knoten, alle `bytes`-Angaben korrekt, Zeitstempel streng aufsteigend
(2026-08-29T21:21:38Z … :42Z), Rundprobe 5/5 byteweise gleich zur Quelldatei, UTF-8 ohne
BOM, LF, `&`/`<`/`>` maskiert, Kommentar je Seite
„P6: Doku-Angleich Projektdialoge (D1–D10)".
Das Skript ist **rein ASCII** gehalten (Gedankenstrich über `[char]0x2013`) — Windows
PowerShell 5.1 liest `.ps1` ohne BOM als ANSI und zerschriebe sonst die Umlaute.

---

## 7. Offene Punkte / bewusst nicht enthalten

| # | Punkt | Warum offen |
|---|---|---|
| O1 | **`B_ASSISTENT` → „Projektassistent"** | ~~Der Wert ist zugleich Schlüssel in `WikiWissen.SEITE_JE_BEREICH` (§ 2.5); Zweizeilenänderung, braucht Freigabe~~ — **erledigt** noch am 29.08.2026 durch die Hauptsitzung: beide Stellen synchron geändert (`HilfeKontext.cs` Konstante mit Kopplungs-Kommentar, `WikiWissen.cs` Eintrag alphabetisch zu „Projektassistent" umgesetzt), gedeckt durch Entscheid E5 |
| O2 | `Wizard_Komponenten` ohne eigenen Infoknopf | Altstand, auch vor P5 so. Der Rahmen `WizardParent` trägt den Knopf; eine eigene Zeile wäre eine Erweiterung der Zuordnung, keine Nachführung |
| O3 | `Form_ProjektSpeichernUnter.btn_Help` ohne Anker | Altbefund, mit P6 erstmals gemessen (§ 4.2). Einzeilig zu beheben, gehört aber zu den offenen Punkten dieser Maske (P1P3 O2/O3) |
| O4 | Wiki-Import selbst | ausdrücklich der Hauptsitzung vorbehalten; P6 liefert nur `import_p6.xml` |
| O5 | Prüfpunkte 1–10 der Konzept-Prüfliste | nur an der laufenden Oberfläche zu erledigen, § 5 |
| O6 | Menüpunkt „Speichern unter…" (O1 aus Schnitt 1, P3 aus Schnitt 2) | Menüerweiterung, weiterhin nicht Teil dieser Pakete |
| O7 | `Allgemein/BhkwPlan.cs` und `Allgemein/Wirtschaftlichkeit/EndenergieAufloeser.cs` sind **fremd geändert** | Beide standen während P6 aus Parallelsitzungen (Thema Wirtschaftlichkeit/H-Serie) im Arbeitsverzeichnis. P6 hat sie nicht angefasst; sie gehören nicht zu diesem Paket und dürfen beim Einchecken **nicht** mitgehen |
| O8 | **`build_p6` ist älter als die Umbenennung des Assemblys** | Während P6 lief, hat eine Parallelsitzung `27b8e08` „Umbenennung Stufe 0: Assembly/EXE/Prozess heisst EPOS_Plan" eingecheckt. Alle Proben dieses Pakets messen gegen `dev\build_p6`, das **vor** diesem Commit entstanden ist und die DLL noch als `WindowsFormsApplication1.dll` führt. Wer die Proben später wiederholt, muss den Assemblynamen in `p6probe.csproj` (und in den vier nachgezogenen Proben) auf den neuen Namen ziehen — an den Messungen selbst ändert das nichts |

---

## 8. Werkzeuge (Wegwerf, unter `dev\`)

| Pfad | Zweck |
|---|---|
| `dev\p6probe\p6probe.csproj`, `Probe.cs` | Prüfstand A–F (53 Prüfungen) |
| `dev\p6probe\lauf_final.txt` | der grüne Lauf |
| `dev\build_p6\` | Programmstand des Pakets (Verifikationsbuild, nie `bin\`) |
| `dev\refbin_p6\` | Referenzlauf-Werkzeug + App im selben Ordner (`OutDir`), für § 4.4 |
| `dev\ref_p6\`, `dev\ref_altcode\`, `dev\ref_p6_sub\` | Läufe und A/B-Teilmenge des Referenznetzes |
| `dev\p6_resx_vorher_Form_ProjektAuswahl.resx`, `dev\p6_help_mapping_vorher.txt` | Sicherungen vor dem Schreiben |
| `dev\{h7probe,h11probe,h12probe,p45probe}\Probe.cs.p6bak` | Sicherungen vor der Umstellung auf `build_p6` |
