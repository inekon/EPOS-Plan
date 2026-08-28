# D-Check — Dialoge auf Überlappungen, Buttons und einheitliches Design

**Auftrag:** 28.08.2026, nach Abschluss der Konzeptumsetzung Brauchwasser/Heizung/Pufferspeicher.
**Stand vor dem Paket:** Branch `Pufferspeicher`, HEAD `3491d50` (Paket E2, Nachtrag).
**Kein Commit, kein Push, kein Branchwechsel.** Die produktive `Kenndaten.accdb` wurde
ausschließlich gelesen (MD5 `f7a4227759…` vor und nach dem Paket unverändert).

---

## 1. Was geprüft wurde und womit

Ein Wegwerf-Prüfprogramm misst jeden Dialog **am laufenden Objekt** statt am Quelltext: Es baut
das Formular gegen eine Wegwerf-Datenbank auf, erzwingt das Fensterhandle, schaltet **jede**
Seite durch und erfasst rekursiv Name, Typ, Lage, Sichtbarkeit, Anker, Dock und Schrift aller
Steuerelemente.

| | |
|---|---|
| Programm | `dev\harness_dcheck\` (Konsolenprojekt, Projektreferenz auf die App, **nicht** in `WP-Plan.sln`) |
| Datenbank | `dev\db\Kenndaten.accdb` — Kopie über `Referenzlauf.exe migration`, DB-Pfad der App per Reflection umgebogen und hart gegengeprüft |
| Projekt | **1042 „Booster-Kette mit Kombi-Speicher"** (datenreich: Kaskade, drei Puffer, Kombi-Speicher) |
| Bericht | `dev\dcheck_befunde.txt` (Messlauf-Rohbefund) |
| Bilder | `dev\dcheck_bilder\` — 38 PNG, siehe Abschnitt 6 |

> **Warum `dev\` in der Repo-WURZEL und nicht unter `WindowsFormsApplication1\dev\`:**
> `WindowsFormsApplication1.csproj` sammelt `**\*.cs` ein und schließt nur `.claude\**` aus.
> Eine `.cs`-Datei unterhalb des Anwendungsordners bricht deshalb sofort den Build der App
> (`CS0017: mehrere Einstiegspunkte`). Alle bisherigen Harnesse liegen aus demselben Grund unter
> `WP_Plan\dev\`; `dev/` ist in `.gitignore` (Zeile 380) auf jeder Ebene ausgeschlossen.

### Geprüfte Dialoge (12 Fälle)

`Form_Simulation_Config` (Kartenliste **und** Schema-Ansicht) · `Form_Waermesenke` ·
`Form_PufferSp_Projekt` **zugeklappt und aufgeklappt (N = 5)** · `Form_QuellePufferspeicher` ·
`Form_Quellprofil` · `Form_Simulation_Detail` (Grundzustand, alle 3 Hauptregister, alle 5
Parameter-Unterseiten, **alle 7 Seitenleisten-Seiten** Energiebedarf/Wärmepumpe/Heizkessel/
Solarthermie/Photovoltaik/Stromspeicher/Ergebnis und im Ergebnis **alle 4 Navigator-Seiten**) ·
`Form_Waermebedarf` · `Form_PufferSp` · `Form_Prozesswaerme` · `Form_WP` in **beiden**
Konstruktormodi (parameterlos = Pflege, mit Name = Ansicht).

### Befundklassen

| Klasse | Kriterium |
|---|---|
| **a** | **Überlappung** — sichtbare Geschwister mit echtem Rechteckschnitt > 4 px² |
| **b** | **Beschneidung** — Steuerelement ragt > 2 px über den Client-Bereich des Elternteils hinaus (ohne Bildlauf) |
| **c** | **Abschnitt** — Text passt bei vorhandener Breite auch umgebrochen nicht in die Höhe |
| **d** | **Fenstermaß** — Entwurfsmaß passt nicht in eine 1280×800-Arbeitsfläche |
| **e** | **Fremdschrift** — Schrift weicht von der Formularschrift ab |
| **f** | **Tabreihenfolge** — TabIndex-Doppelbelegung unter fokussierbaren Geschwistern |

### Zwei Messregeln, die den Befund erst tragfähig machen

1. **Abschnitt (c) wird mit Umbruch gemessen.** `GetPreferredSize(new Size(Breite, 0))` gegen die
   vorhandene Höhe. Ein reiner Breitenvergleich meldet jede Schaltfläche, weil
   `GetPreferredSize` den Innenabstand aufschlägt — im ersten Lauf 22 Fehlbefunde, darunter jedes
   „OK". Zusätzlich muss der Text **einzeilig** zu breit sein; `AutoEllipsis`-Labels kürzen
   sichtbar mit „…" und gelten als gewollt.
2. **Beschneidung (b) misst gegen das Entwurfsmaß.** Der Prüfrechner hat 1280×800; **Windows
   klemmt jedes neu erzeugte Fenster auf Bildschirmgröße**, auch wenn `FensterEinpassung` nichts
   tut (nachgewiesen: `Form_Waermesenke` meldet vor dem Anzeigen 620×825, danach 620×781 — bei
   erzwungener Prüf-Arbeitsfläche 1920×1040 und unveränderter `FixedDialog`-Berandung). Ohne
   diese Korrektur meldete der Lauf vier Fußzeilenknöpfe als „außerhalb des Formulars", die in
   Wahrheit sauber im Entwurf liegen.

---

## 2. Befundtabelle

**Gesamt nach den Fixes: 43 Befunde** — 7 × a, 0 × b, 3 × c, 6 × d, 27 × e, 0 × f;
dazu 11 als **gewollt** klassifizierte Überlagerungen (Abschnitt 4).
**Vor den Fixes: 30 Befunde in a/b/c** (a 22, b 5, c 22 im ersten, ungefilterten Lauf).

### 2.1 Gefixt

| # | Dialog / Seite | Befund | Klasse | Stelle |
|---|---|---|---|---|
| **D1** | `Form_Simulation_Config`, Speicherkarte | Der Chipstreifen lief **58 × 17 px unter das Bilanzfeld**. Ursache: `AutoSize` + `GrowAndShrink` misst ohne Breitenvorgabe, legt alle Chips in eine Zeile und überschreibt die zugewiesene Breite (gemessen 142 statt 76 px) | a | `Views/Simulation/SpeicherKarte.cs`, `Neuordnen()` |
| **D2** | `Form_Simulation_Detail`, Fußzeile | Die Meldungszeile des Laufs lag **177 × 24 px auf `btn_Konfiguration`** — sie richtete sich starr an `btn_Simulation.Right + 16` aus | a | `Form_Simulation_Detail.cs`, `LaufmeldungenLabelSicherstellen()` + neue `FusszeileNachbarn()` |
| **D3** | `Form_Simulation_Detail`, Seite Heizkessel | Die D4-Zeile „Quellwärme aus Kaskade" lag **deckungsgleich auf der Zeile „Wärmeproduktion Spitzenkessel"** (97 × 23 px im Feld, 139 × 19 px in der Beschriftung, 43 × 15 px in der Einheit). `tb_Gasspitze.Bottom + 9` ist auf der umgeräumten Seite nicht frei | a | `Form_Simulation_Detail.cs`, `InitKesselQuellwaerme()` + neue `FreieZeile()` |
| **D4** | `Form_Simulation_Detail`, Seite Heizkessel | „Quellwärme aus Kaskade:" brauchte 164 px, die übernommene Nachbarbreite gab 139 px — links beschnitten | c | dieselbe Methode |
| **D5** | `Form_Simulation_Detail`, Parameterseite Stromspeicher | Die 700 px breite Fußzeile lag **220 × 24 px unter `button_SpOptimierung`** | a | `Form_Simulation_Detail.cs`, `InitStromspeicherParameter()` |
| **D6** | `Form_Simulation_Detail`, Parameterseite Stromspeicher | Die Preiszeile ist zweizeilig (39 px) und war mit 34 px angelegt | c | dieselbe Methode |
| **D7** | `Form_Simulation_Detail`, Seite Stromspeicher | Die untere Kachelreihe des Kernblocks stand **8 px über den Blockrand** — die Rasterzeile trug den Außenabstand des Blocks nicht mit | b | `Form_Simulation_Detail.cs`, `SpKernblockEinpassen()` |
| **D8** | `Form_Simulation_Detail`, Bedarfsseite | „Brauchwasser" (106 px) und „Prozesswärme" (111 px) waren auf 92 px beschnitten | c | `Form_Simulation_Detail.cs`, `InitBedarfKanalzeilen()`, neue `BedarfKanalBeschriftungEinpassen()`, `LabelBreiteMessen()`/`BreiteMessen()` |
| **D9** | `Form_WP` (**beide** Modi) | Der KI-Aufrufknopf lag **26 × 24 px auf `btn_Help`** | a | `Allgemein/KI/Dialoge/KiDialoge.cs`, `Waermepumpe()` |
| **D10** | `Form_Quellprofil` | Die Hinweiszeile braucht drei Textzeilen (45 px), angelegt waren 32 px — die dritte fehlte | c | `Views/Simulation/Form_Quellprofil.cs`, neue `HinweiszeileEinpassen()` |
| **D11** | `Form_QuellePufferspeicher` | Die Hinweiszeile zur Anschlusshöhe braucht zwei Zeilen (30 px), angelegt waren 28 px | c | `Views/Simulation/Form_QuellePufferspeicher.cs` |

**Drei wiederkehrende Ursachen**, die über die Einzelfälle hinaus gelten:

* **Selbstmessende Behälter brauchen `MaximumSize`, nicht `Width`** (D1). Derselbe Fehler war in
  `ErzeugerKarte` schon behoben und dort ausführlich begründet — `SpeicherKarte` war nicht
  nachgezogen.
* **„Eine Zeile unter X" ist keine Zusicherung** (D2, D3, D5). Wo eine Ergebniszeile
  programmatisch nachgetragen wird, muss der Platz **gemessen** werden. `FreieZeile()` tut das
  jetzt allgemein; sie prüft bewusst **nicht** auf `Visible`, weil Kinder einer noch nicht
  gewählten `TabPage` im Konstruktor `Visible = false` melden.
* **Textbreiten dürfen nicht im Konstruktor festgeschrieben werden** (D4, D8, D10, D11). Drei
  Fallen greifen ineinander: `TextRenderer.MeasureText` misst kleiner als das Label zeichnet;
  die Schriftskalierung des Formulars läuft **nach** dem Konstruktor; und die Seitenleiste der
  Detailansicht **leiht** die Steuerelemente in ein Panel mit **anderer Schrift** aus
  (`listViewQuellen_SelectedIndexChanged`). D8 ist deshalb an `FontChanged` gehängt.

### 2.2 Offen — nur Befund, priorisiert

| Prio | Dialog / Seite | Befund | Klasse | Warum nicht angefasst |
|---|---|---|---|---|
| **1** | `Form_Simulation_Detail` → Ergebnis → „Autarkie-Analyse" (`DashboardForm`) | `lblSpeicherInfo` „Theoretischer Speicher (PV) (kWh):" braucht 242 px, hat 165 → Text abgeschnitten; ebenso `lblNutzungsgradST` (198/159) und `lblSTDeckung` (102/100) | c | Designer-Layout von `DashboardForm` |
| **1** | dieselbe Seite | `lblTest` ↔ `numSpeicherKWh` überlappen 120 × 2 px; `pbPV` ↔ `lblPVAutarkie` und `pbST` ↔ `lblSTDeckung` je 100 × 3 px | a | Designer-Layout |
| **2** | **alle Dialoge** | **Fußzeilen sind uneinheitlich** — Reihenfolge, Größe und Anker (Abschnitt 3) | Design | Geschmacks-/Gestaltungsfrage über Altdialoge hinweg |
| **2** | `Form_Simulation_Detail`, Register „Parameter" | `tabControl3` — ein **8 × 8 px großes, leeres TabControl** liegt auf `tabControl_Einstellungen_MapSplit` | a | Designer; sieht nach Rest aus, Entfernen ist ein Designer-Eingriff |
| **3** | `Form_Simulation_Detail`, Register „Simulation" | `textBox_MaxStrombedarf`/`label20` (85 × 1 px), `textBox_MaxWaermelast`/`label1` (70 × 1 px), `label14`/`label1` (24 × 1 px) — Unterkante der Beschriftung berührt die Oberkante des Feldes | a | Designer, 1 px, optisch folgenlos |
| **3** | 6 Dialoge | **Entwurfsmaß größer als eine 1280×800-Arbeitsfläche**: `Form_Simulation_Detail` 1507×877, `Form_PufferSp_Projekt` 733×882 (zu **und** auf), `Form_Waermesenke` 653×881, `Form_WP` 1056×780 (beide Modi). `FensterEinpassung` fängt alle sechs mit Bildlauf ab | d | Bekannt und abgefangen (P1-O8, P2-O5); ein echter Umbau wäre eine Neugestaltung |
| **4** | 6 Dialoge, 27 Schriftgruppen | **Fremdschriften** — allein `Form_Simulation_Detail` mischt 13 Varianten (Segoe UI 7,75/8/8,25/9,75/10/10,5/12, Segoe UI Semibold 8/9,75/12, **Arial 10 fett** bei `lblCO2`) auf einer Formularschrift Segoe UI 9. `Form_PufferSp` und `Form_Prozesswaerme` setzen Feldschriften auf 8 pt herunter | e | Gestaltungsfrage; ein Sammelpaket „Schriftbild vereinheitlichen" wäre der richtige Rahmen. **`lblCO2` (Arial) ist der einzige echte Ausreißer** und wäre auch einzeln zu heilen |

**Ohne Befund:** Klasse **f** (keine TabIndex-Doppelbelegung in Eingabegruppen) und Klasse **b**
(nach D7 kein Steuerelement mehr über seinem Elternrand).

### 2.3 Nachprüfung der offenen Punkte aus den Vorpaketen

| Punkt | Ergebnis |
|---|---|
| **P2-O6** — Einheitentext der Ladeobergrenze ragt 16 px über die Gruppe in `Form_Waermesenke` | **Erledigt bestätigt.** Der Messlauf findet in `Form_Waermesenke` keinen Befund der Klassen a/b/c. Paket L (Abschnitt 3.2) hat den Punkt geschlossen |
| **P1-O8** — `Form_PufferSp_Projekt` zugeklappt 826 px, Scroll-Ratchet | **Bestätigt, unverändert offen** (Klasse d). Zugeklappt misst der Client 700 × 826, aufgeklappt (N = 5) 700 × 976; als Fenster sind das 733 × 882 bzw. 733 × 1032 gegen eine Arbeitsfläche von 1280 × 752. Der Ratchet ist messbar: `AutoScrollMinSize` wächst beim Aufklappen von 700 × 816 auf 700 × 966 und wird nie wieder kleiner. Die Fußzeilenknöpfe liegen in **beiden** Zuständen sauber im Entwurf (10 px Abstand nach unten) |
| **E2-Beifang** — CSV-Export-Button der Bedarfsseite verdeckte den E1-Kanalblock | **Erledigt bestätigt.** `btn_CsvExportBedarf` erscheint in keinem Überlappungsbefund |

---

## 3. Fußzeilen-Vergleichstabelle

Alle Angaben in Entwurfskoordinaten (Client-Maß in der letzten Spalte), Reihenfolge von links
nach rechts.

| Dialog | Knöpfe (links → rechts) | Größen | Anker | Abstand rechts / unten | Entwurf |
|---|---|---|---|---|---|
| `Form_Simulation_Config` | Beenden | 103×30 | **Bottom, Right** | 19 / 12 | 1120×620 |
| `Form_Waermesenke` | **OK**, Abbrechen | 85×23, 85×23 | Top, Left | 12 / 23 | 620×825 |
| `Form_PufferSp_Projekt` | Übernehmen, Schließen | 130×30, 130×30 | Top, Left | 20 / 10 | 700×826 |
| `Form_QuellePufferspeicher` | **OK**, Abbrechen | 110×30, 110×30 | Top, Left | 12 / 12 | 620×566 |
| `Form_Quellprofil` | **OK**, Abbrechen | 85×23, 85×23 | Top, Left | 12 / 17 | 700×625 |
| `Form_Simulation_Detail` | Beenden | 101×38 | Top, Left | 59 / 50 | 1474×821 |
| `Form_Waermebedarf` | Abbrechen, **OK** | 98×33, 98×33 | Top, Left | 17 / 11 | 828×443 |
| `Form_PufferSp` | Abbrechen, **OK** | 106×34, 106×34 | Top, Left | 11 / 9 | 774×496 |
| `Form_Prozesswaerme` | Abbrechen, **OK** | 105×31, 105×31 | Top, Left | 10 / 9 | 796×584 |
| `Form_WP` (beide Modi) | Speichern, **OK** (`btn_Beenden`) | 136×35, 111×35 | Top, Left | 39 / 28 | 1023×741 |

**Vier Uneinheitlichkeiten:**

1. **Bestätigungsknopf mal links, mal rechts.** `Form_Waermesenke`, `Form_QuellePufferspeicher`
   und `Form_Quellprofil` stellen **OK links neben Abbrechen**; `Form_Waermebedarf`,
   `Form_PufferSp`, `Form_Prozesswaerme` und `Form_WP` stellen ihn **rechts**. Beides ist in
   sich schlüssig, zusammen ist es eine Stolperfalle.
2. **Sieben verschiedene Knopfgrößen** von 85×23 bis 136×35.
3. **Nur `Form_Simulation_Config` verankert die Fußzeile unten rechts.** Alle anderen stehen auf
   `Top, Left` — und das ist nicht folgenlos: `FensterEinpassung` stellt jeden zu großen Dialog
   auf eine **veränderbare** Berandung um; zieht der Anwender das Fenster dann auf, bleiben die
   Knöpfe oben links kleben, statt in der Ecke zu bleiben.
4. **`Form_WP.btn_Beenden` trägt die Beschriftung „OK".** Name und Aufschrift widersprechen sich;
   der Katalogeintrag `KiDialoge.Waermepumpe()` hält den Widerspruch bereits fest.

*Nicht in der Wertung:* `Form_Prozesswaerme.btn_neuerWert` („Übernehmen", 30/294) ist ein
Aktionsknopf **innerhalb** der Maske, keine Fußzeile — der Textfilter fängt ihn mit.

---

## 4. Als gewollt klassifizierte Überlagerungen (11)

Sie werden gemessen, aber **nicht** als Befund gemeldet:

* **6 × Bedienschalter auf einem Diagramm** — `checkBox_Sortiert`/`chart1`,
  `checkBox_StromSortiert`/`chart2`, `checkBox_WP_sortiert`/`chart3`,
  `checkBox_Kessel_sortiert`/`chart_Kessel`, `checkBox_Ueberschuss` und
  `checkBox_Speicherzustand` auf `chart_PV`. Regel: das kleine Element liegt **vollständig**
  innerhalb der Diagrammfläche.
* **3 × `AutoEllipsis`** — die Namen auf den Speicherkarten („Puffer 3000Ltr",
  „Puffer 3000Ltr (2)", „Stora B 1000-6 ER 1 B") werden bewusst auf die halbe freie Kopfbreite
  begrenzt und sichtbar mit „…" gekürzt, damit lange Herstellerbezeichner die Chips nicht
  wegdrücken.
* **1 × gemeinsame Kante** — `btn_AnsichtListe`/`btn_AnsichtSchema` teilen sich 1 px.
* **1 × `TabListMapper`** — `tabControl_Einstellungen` sitzt 29 px über seinem Panel, damit die
  Reiterleiste verschwindet und die `ListView`-Navigation an ihre Stelle tritt.

---

## 5. Verifikation

| Prüfung | Ergebnis |
|---|---|
| Build der Solution (`WP-Plan.sln`, Debug × x64, `-p:OutDir=dev\slnout\`) | **0 Fehler** |
| Harness-Zweitlauf | Alle 11 direkt gefixten Befunde verschwunden, **keine neuen** |
| Byte-Gate A/B gegen den unveränderten HEAD | **9 von 9 Projekten PASS auf beiden Seiten, 226 von 226 CSV byte-gleich** |
| Produktive `Kenndaten.accdb` | mtime 28.08.2026 09:05 und MD5 `f7a422775915976127f2da6b1e024adf` **vor und nach** dem Paket identisch |

**Zum Byte-Gate:** A = `git archive HEAD` nach `dev\ab_head\`, dort gebaut und gelaufen
(`--ziel dev\ab_csv_A`); B = Arbeitsstand, gelaufen nach `dev\ab_csv_B`. A/B statt Vergleich
gegen einen eingefrorenen Referenzstand, weil die produktive Quelle durch den arbeitenden
Anwender driftet — beide Läufe lagen 35 Sekunden auseinander und die Quelle war dazwischen
nachweislich unverändert. Die Zusage „UI-only" ist damit belegt: **keine einzige Ergebniszahl
hat sich geändert.**

---

## 6. Bildverzeichnis

`dev\dcheck_bilder\` — 38 PNG, je Dialog und Seite, aufgenommen **nach** den Fixes.

| Bild | Zeigt |
|---|---|
| `Form_Simulation_Detail_SEITE_tabPage_Heizkessel.png` | Die Zeile „Quellwärme aus Kaskade" steht jetzt vollständig und auf eigener Zeile (D3 + D4) |
| `Form_WP_Pflege_Start.png` | Der KI-Knopf steht links neben dem Infoknopf statt darauf (D9) |
| `Form_Simulation_Detail_SEITE_Ergebnis_Nav1.png` | **Offener Befund Prio 1**: die abgeschnittenen Beschriftungen und Überlappungen der Autarkie-Analyse |
| `Form_Simulation_Config_Start.png` | Speicherkarten mit sauber umbrechendem Chipstreifen (D1) |
| `Form_Simulation_Config_Schema.png` | Schema-Ansicht, ohne Befund |
| `Form_PufferSp_Projekt_zu_Start.png` / `Form_PufferSp_Projekt_auf_Start.png` | Pufferdialog zugeklappt und mit N = 5 aufgeklappt (P1-O8) |
| `Form_Simulation_Detail_SEITE_*` | Die sieben Seitenleisten-Seiten |
| `Form_Simulation_Detail_SEITE_Ergebnis_Nav0…3.png` | Die vier Navigator-Seiten des Ergebnisses |

---

## 7. Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Views/Simulation/SpeicherKarte.cs` | D1 — `MaximumSize` für den Chipstreifen |
| `Views/Simulation/Form_Simulation_Detail.cs` | D2–D8 — `FusszeileNachbarn()`, `FreieZeile()`, `LabelBreiteMessen()`, `BedarfKanalBeschriftungEinpassen()` neu; `BreiteMessen()`, `InitKesselQuellwaerme()`, `InitBedarfKanalzeilen()`, `BedarfKanalzeilenFuellen()`, `InitStromspeicherParameter()`, `SpKernblockEinpassen()`, `LaufmeldungenLabelSicherstellen()` geändert |
| `Views/Simulation/Form_Quellprofil.cs` | D10 — `HinweiszeileEinpassen()` neu |
| `Views/Simulation/Form_QuellePufferspeicher.cs` | D11 — Hinweiszeile wird gemessen statt festgeschrieben |
| `Allgemein/KI/Dialoge/KiDialoge.cs` | D9 — `KiKnopfposition` für `Form_WP` auf 60/34 |
| `Allgemein/Simulation/DCheck_Dialoge_Protokoll.md` | dieses Protokoll |

**Keine Designer- oder `.resx`-Datei angefasst.** Alle Fixes liegen in programmatisch
aufgebautem Layout aus den Paketen K1…E2 oder in einem Katalogeintrag. Keine neuen sichtbaren
Texte, deshalb keine Ressourcenpflege nötig.
