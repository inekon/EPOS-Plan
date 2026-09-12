# D2 — Fußzeilen-Norm und DashboardForm-Korrektur

**Auftrag:** 28.08.2026, Folgepaket zum [D-Check](DCheck_Dialoge_Protokoll.md) (Abschnitt 2.2,
Prio 1 und Prio 2).
**Stand vor dem Paket:** Branch `Pufferspeicher`, HEAD `7b591e4` (code-identisch zu `babab27`;
die beiden Zwischencommits `7a3deff`/`7b591e4` enthalten ausschließlich Dokumentation und
Referenz-CSV — `git diff --name-only babab27 7b591e4` liefert keine einzige `.cs`, `.resx` oder
`.csproj`).
**Kein Commit, kein Push, kein Branchwechsel.** Die produktive `Kenndaten.accdb` wurde
ausschließlich gelesen.

**Keine Designer- und keine `.resx`-Datei angefasst.** Alle Korrekturen laufen zur Laufzeit im
Konstruktor-Nachlauf bzw. im `Load`. Keine neuen sichtbaren Texte, deshalb keine Ressourcenpflege.

---

## 1. Die Norm

Neue Klasse **`Allgemein/GrafikTools/FusszeilenNorm.cs`** (Namespace `WindowsFormsApplication1`).

| | |
|---|---|
| **Knopfgröße** | **110 × 30 px** |
| **Mindestbreite** | Textmaß + 24 px Luft, wenn das mehr als 110 px ergibt |
| **Randabstand** | **12 px** rechts und unten |
| **Knopfabstand** | **10 px** waagerecht |
| **Reihenfolge (von rechts)** | Primäraktion (OK / Speichern / Übernehmen), links davon Abbrechen / Beenden / Schließen, weiter links die übrigen Aktionen der Zeile |
| **Anker** | `Bottom`+`Right` für die ganze Reihe |

**Woher die Zahlen kommen.** 110 × 30 ist die Größe des jüngsten Dialogs
(`Form_QuellePufferspeicher`); die Höhe 30 ist mit drei Dialogen (`Form_Simulation_Config`,
`Form_PufferSp_Projekt`, `Form_QuellePufferspeicher`) zugleich die häufigste im Bestand. Rand 12
und Abstand 10 stammen aus `Form_Simulation_Config.FusszeilePlatzieren()` — der einzigen
Fußzeile, die vor diesem Paket schon unten rechts verankert war. Diese Methode ist damit die
Vorlage der Norm und ruft sie jetzt selbst auf; der Rand geht dort von 19 auf 12.

### Schnittstelle

| Aufruf | Wirkung |
|---|---|
| `Einhaengen(Form, params Button[])` | Merkt das Entwurfsmaß, richtet im `Load` aus und noch einmal nach `Shown` |
| `Anwenden(Form, params Button[])` | Sofort ausrichten; idempotent, rechnet absolut |
| `BezugSetzen(Form, Size)` | Bezugsrahmen ausdrücklich setzen (für Masken, die zur Laufzeit wachsen) |
| `AnkerLoesen(params Button[])` | Anker vorübergehend abnehmen, bevor der Dialog selbst umräumt |
| `ZeileMitziehen(params Control[])` | Nur `Bottom`+`Left` setzen — für Arbeitsknöpfe derselben Zeile, die nicht genormt werden |

**Die Klasse sucht sich keine Knöpfe.** Verschoben wird ausschließlich, was der Aufrufer
übergibt. Eine Heuristik über alle `Button` eines Formulars würde Aktionsknöpfe INNERHALB der
Maske mitreißen — `Form_Prozesswaerme.btn_neuerWert` („Übernehmen", 17/53) ist der Beleg.
Text, `DialogResult`, `TabIndex` und Ereignisverdrahtung bleiben unangetastet: die Norm ist reine
Geometrie.

### Drei Messfallen, die den Bau bestimmt haben

1. **`Control.Visible` taugt nicht als Filter.** Solange das Formular nicht angezeigt ist, meldet
   JEDES Kind `Visible = false` (die Eigenschaft liefert die wirksame Sichtbarkeit der ganzen
   Kette). Der erste Entwurf filterte darauf — und lief bei jedem Aufruf aus einer Aufbaumethode
   heraus wirkungslos durch (`Form_Simulation_Config` blieb unverändert bei 103 × 30 / Rand 19).
2. **`Button.GetPreferredSize` misst nicht den Text.** Es liefert im Wesentlichen die vorhandene
   Größe plus Innenabstand — derselbe Befund, an dem der D-Check im ersten Lauf 22 Fehlmeldungen
   erzeugte („typisch +2 px bei Schaltflächen"). Als Mindestbreite wäre das eine
   Selbstbestätigung: `btn_Speichern` (136 px) wollte prompt 152 px und legte sich auf `btn_Neu`
   und `btn_Loeschen`. Gemessen wird deshalb mit `TextRenderer.MeasureText`.
3. **Der Bezugsrahmen darf weder das Client- noch das Anzeigerechteck sein.**
   * *Client allein:* Windows klemmt jedes Fenster beim Anzeigen auf Bildschirmgröße. Bei
     `Form_WP` ist der Ausschnitt 713 px hoch, der Entwurf 741 — die Reihe landete 28 px zu hoch
     und überdeckte das Kennlinien-Register.
   * *Anzeigerechteck allein:* Es wird aus den Kindelementen hochgerechnet, und die
     Fußzeilenknöpfe SIND Kindelemente. Beim Aufklappen von `Form_PufferSp_Projekt` wanderte die
     Reihe dadurch in mehreren Runden von 784 auf 1312 px. Ein fremdes Steuerelement außerhalb
     des Entwurfs (die Meldungszeile der Detailansicht, siehe 4.1) blähte den Rahmen zusätzlich
     auf 1876 px Breite auf.
   * *Gewählt:* das Größere aus aktueller Client-Fläche und gemerktem Entwurfsmaß. Das
     Entwurfsmaß wird beim Einhängen genommen und bis zum `Load` bei jeder Vergrößerung
     nachgezogen (`ClientSizeChanged`/`Layout`) — bei `Form_WP` läuft die Schriftskalierung erst
     nach dem Konstruktor und macht aus 877 × 642 die tatsächlichen 1023 × 741. Ab `Load` endet
     die Beobachtung: dann kommen nur noch Klemmung und Bildlauf, die verkleinern würden.

---

## 2. Fußzeilen — vorher und nachher

Alle Angaben in Entwurfskoordinaten. „Abstand" = rechts / unten zum Bezugsrahmen.
Quelle nachher: `dev\dcheck_fusszeilen.tsv` (Messlauf 28.08.2026 11:34).

### 2.1 Vorher (D-Check, Abschnitt 3)

| Dialog | Knöpfe (links → rechts) | Größen | Anker | Abstand |
|---|---|---|---|---|
| `Form_Simulation_Config` | Konfiguration speichern, **Beenden** | 193×30, 103×30 | Bottom, Right | 19 / 12 |
| `Form_Waermesenke` | **OK**, Abbrechen | 85×23, 85×23 | Top, Left | 12 / 23 |
| `Form_PufferSp_Projekt` | Übernehmen, Schließen | 130×30, 130×30 | Top, Left | 20 / 10 |
| `Form_QuellePufferspeicher` | **OK**, Abbrechen | 110×30, 110×30 | Top, Left | 12 / 12 |
| `Form_Quellprofil` | **OK**, Abbrechen | 85×23, 85×23 | Top, Left | 12 / 17 |
| `Form_Simulation_Detail` | Simulation starten, Konfiguration …, Ergebnis speichern, **Beenden** | 185×38, 185×38, 185×38, 101×38 | Top, Left | 54 / 8 |
| `Form_Waermebedarf` | Abbrechen, **OK** | 98×33, 98×33 | Top, Left | 17 / 11 |
| `Form_PufferSp` | Abbrechen, **OK** | 106×34, 106×34 | Top, Left | 11 / 9 |
| `Form_Prozesswaerme` | monatlicher Verlauf, Simulation, Abbrechen, **OK** | 144×31, 119×31, 105×31, 105×31 | Top, Left | 10 / 9 |
| `Form_WP` (beide Modi) | Speichern, Neu, Löschen, **OK** (`btn_Beenden`) | 136×35, 111×35, 111×35, 111×35 | Top, Left | 39 / 14 |

Sieben Knopfgrößen von 85×23 bis 136×35; Bestätigungsknopf dreimal links, viermal rechts; ein
einziger Dialog unten rechts verankert.

### 2.2 Nachher

| Dialog | Knöpfe (links → rechts) | Größen | Anker | Abstand |
|---|---|---|---|---|
| `Form_Simulation_Config` | Konfiguration speichern, **Beenden** | **175×30**, **110×30** | **Bottom, Right** | **12 / 12** |
| `Form_Waermesenke` | Abbrechen, **OK** | **110×30**, **110×30** | **Bottom, Right** | **12 / 12** |
| `Form_PufferSp_Projekt` (zu) | Schließen, **Übernehmen** | **110×30**, **110×30** | **Bottom, Right** | **12 / 12** |
| `Form_PufferSp_Projekt` (auf, N = 5) | Schließen, **Übernehmen** | **110×30**, **110×30** | **Bottom, Right** | **12 / 12** |
| `Form_QuellePufferspeicher` | Abbrechen, **OK** | **110×30**, **110×30** | **Bottom, Right** | **12 / 12** |
| `Form_Quellprofil` | Abbrechen, **OK** | **110×30**, **110×30** | **Bottom, Right** | **12 / 12** |
| `Form_Simulation_Detail` | Simulation starten, Konfiguration …, Ergebnis speichern, **Beenden** | 185×38, 185×38, 185×38, **110×30** | Bottom, Left / Bottom, Left / Top, Left / **Bottom, Right** | **12 / 12** |
| `Form_Waermebedarf` | Abbrechen, **OK** | **110×30**, **110×30** | **Bottom, Right** | **12 / 12** |
| `Form_PufferSp` | Abbrechen, **OK** | **110×30**, **110×30** | **Bottom, Right** | **12 / 12** |
| `Form_Prozesswaerme` | monatlicher Verlauf, Simulation, Abbrechen, **OK** | 144×31, 119×31, **110×30**, **110×30** | Bottom, Left / Bottom, Left / **Bottom, Right** ×2 | **12 / 12** |
| `Form_WP` (beide Modi) | Speichern, Neu, Löschen, **OK** (`btn_Beenden`) | **110×30** ×4 | **Bottom, Right** ×4 | **12 / 12** |

**Eine Knopfgröße (110 × 30), eine Reihenfolge, ein Rand (12/12), ein Anker.** Einzige Abweichung
nach oben: `btn_Speichern` in `Form_Simulation_Config` — die Beschriftung „Konfiguration
speichern" braucht 175 px, das ist die Mindestbreite nach Textmaß und damit Teil der Norm (vorher
193 px).

### 2.3 Warum nicht jeder Knopf der Zeile genormt wurde

Drei Dialoge führen auf derselben Zeile **Arbeitsknöpfe**, die keine Fußzeilenaktion sind. Sie
behalten Lage und Größe — ihre Beschriftung braucht die Breite — und bekommen nur den unteren
Anker (`ZeileMitziehen`), damit die Zeile beim Aufziehen des Fensters als Ganzes mitwandert:

* `Form_Simulation_Detail`: `btn_Simulation` („Simulation starten ▶", 185 px) und
  `btn_Konfiguration` („Konfiguration …", 185 px).
* `Form_Prozesswaerme`: `btn_ErgebnisseVerbrauch` („monatlicher Verlauf") und `btn_Simulation`.

`Form_WP` ist der Gegenfall: dort besteht die ganze Zeile aus Satzverwaltung (Speichern, Neu,
Löschen) plus Abschluss. Nur den Abschlussknopf zu normen ginge nicht — „Speichern" käme dann auf
„Neu" und „Löschen" zu liegen (im Zwischenstand gemessen: Schnitt 111 × 23 px). Die Norm nimmt
deshalb alle vier und erhält dabei die bisherige Reihenfolge von links nach rechts.

### 2.4 Ausgelassene Sonderfälle

| Fall | Warum |
|---|---|
| `DashboardForm` | Eingebettet (`TopLevel = false`, `FormBorderStyle.None`), hat gar keine Fußzeile. Der Messlauf bestätigt es: „keine Fusszeilenknoepfe gefunden" |
| Assistentenbetrieb von `Form_PufferSp`, `Form_Prozesswaerme`, `Form_Waermebedarf` | `SetControls(..., bWizard: true)` blendet die ganze Fußzeile aus und setzt `FormBorderStyle.None`. Die Norm läuft mit, bleibt aber ohne sichtbare Wirkung — und weil BEIDE Knöpfe verschwinden, entsteht auch keine Lücke in der Reihe |
| `Form_Prozesswaerme.btn_neuerWert` („Übernehmen", 17/53) | Aktionsknopf INNERHALB der Maske, keine Fußzeile. Der Textfilter des Messlaufs fängt ihn mit, die Norm nicht |
| `Form_Simulation_Detail.btn_ErgebnisSpeichern` (692/777) | Arbeitsknopf in der Fußzeilenzeile, aber im mittleren Feld; die Meldungszeile des Laufs richtet sich an ihm aus (siehe 4.1). Weder verschoben noch verankert |
| `btn_Help` (Infoknopf) in fünf Dialogen | Steht oben rechts, gehört nicht zur Fußzeile |
| Wizard-Seiten ohne eigene Fußzeile | Kein eigener Aufruf nötig — sie erben die Knopfleiste des Assistenten |

---

## 3. DashboardForm (Autarkie-Analyse) — Befundabgleich

Alle sechs Prio-1-Befunde des D-Checks sind erledigt. Umgesetzt in
`Views/Simulation/DashboardForm.cs`, Methode `LayoutEinpassen()`, gerufen aus dem Konstruktor, aus
`OnLoad`, aus `OnFontChanged` und am ENDE von `UpdateSimulationData()` — erst dort stehen die
Texte, deren Breite gemessen wird.

| D-Check-Befund | Klasse | Nachher |
|---|---|---|
| `lblSpeicherInfo` „Theoretischer Speicher (PV) (kWh):" braucht 242 px, hat 165 | c | **weg** — Breite wird gemessen (Entwurfsmaß als Untergrenze, Platz bis zum rechten Rand als Obergrenze) |
| `lblNutzungsgradST` braucht 198 px, hat 159 | c | **weg** — dieselbe Messung |
| `lblSTDeckung` („nicht benötigt") braucht 102 px, hat 100 | c | **weg** — die Wertbeschriftung übernimmt die Breite ihres Balkens (180 px) |
| `lblTest` ↔ `numSpeicherKWh`, Schnitt 120 × 2 px | a | **weg** — der Speicherblock ist eine Kette: `lblSpeicherInfo` → `numSpeicherKWh` (Unterkante + 2) → `lblTest` (Unterkante + 6) |
| `pbPV` ↔ `lblPVAutarkie`, Schnitt 100 × 3 px | a | **weg** — Beschriftung auf Schrifthöhe, Balken auf `Unterkante + 2` |
| `pbST` ↔ `lblSTDeckung`, Schnitt 100 × 3 px | a | **weg** — dieselbe Regel |

**Warum an `OnFontChanged`.** Das Formular wird von `TabNavigationManager` als Kind in ein fremdes
Panel gehängt; erbt es dort eine andere Schrift, verschieben sich alle gemessenen Maße. Dasselbe
Muster wie bei der Bedarfsseite der Detailansicht (D-Check, D8).

**Mitgenommen (kein D-Check-Befund, aber dieselbe Ursache):** Ohne Photovoltaik rückt die
Solarthermie-Kachel in die linke Spalte (`PraesenzAnwenden`) — dort steht die CO₂-Zeile. Die jetzt
breitere Nutzungsgradzeile weicht in diesem Fall nach unten aus.

**Kein Designer-Umbau nötig.** Alle sechs Befunde ließen sich mit gemessenen Breiten und
Ketten-Positionen heilen; es bleibt kein offener Punkt an diesem Formular.

Belege: `dev\dcheck_bilder\Form_Simulation_Detail_SEITE_Ergebnis_Nav1.png` (vorher, drei
abgeschnittene Texte und drei Überlappungen) gegen
`…_Nav1_D2.png` (nachher).

---

## 4. Beifang

### 4.1 Meldungszeile der Detailansicht (nicht im Auftrag, aber beim Messen aufgefallen)

`LaufmeldungenLabelSicherstellen()` stand die Zeile 440 px breit „hinter dem letzten Knopf
derselben Fußzeile" ab. Der D-Check-Kommentar hielt fest: „btn_Beenden am rechten Rand bleibt
außen vor" — die Bedingung traf aber nicht zu. `btn_ErgebnisSpeichern` (692/777, 185 px breit)
schiebt die Zeile zuerst auf x = 893, und damit fiel `btn_Beenden` (1319) in das Fenster
`links + 440`. Ergebnis war eine Meldungszeile bei **x = 1441, also 400 px rechts außerhalb des
Entwurfs** (Client 1474), die über `FensterEinpassung.InhaltsMass` den Bildlaufbereich auf
**1876 px** aufblähte. Der Befund war im D-Check nicht sichtbar, weil Beschneidung (Klasse b) bei
rollenden Flächen nicht gemeldet wird.

`btn_Beenden` ist jetzt ausdrücklich ausgenommen, zusätzlich jeder Knopf mit rechtem Anker. Der
Bildlaufbereich der Detailansicht geht damit von 1876 auf **1462 px** zurück.

### 4.2 Leeres `tabControl3` (D-Check Prio 2)

**Unbenutzt — Beweis:** Es trägt nur `tabPage1` und `tabPage2`, beide mit Größe 0 × 0 und ohne ein
einziges Kindelement (`Form_Simulation_Detail.Designer.cs`, Zeilen 1720–1738); außerhalb dieses
Blocks nennt keine Zeile Anwendungscode die drei Namen (`grep` über `Views/`, `Allgemein/`,
`Controller/`). Entfernt wird es NICHT — das wäre ein Designer-Eingriff —, es wird im Konstruktor
auf `Visible = false` gestellt. Der 8 × 8-px-Überlappungsbefund auf
`tabControl_Einstellungen_MapSplit` ist damit weg.

### 4.3 Arial-Ausreißer `lblCO2` (D-Check Prio 4)

`Arial 10 fett` auf einer Formularschrift `Segoe UI 9` war der einzige echte Fremdschrift-Fall der
Anwendung. Er läuft jetzt auf `new Font(this.Font, FontStyle.Bold)` — fett bleibt fett, die
Familie folgt dem Formular, und `OnFontChanged` zieht nach. Die Klasse-e-Zählung geht von 27 auf
26 Schriftgruppen zurück; die restlichen 26 sind Größenabweichungen und bleiben dem Sammelpaket
„Schriftbild vereinheitlichen" vorbehalten.

---

## 5. Verifikation

| Prüfung | Ergebnis |
|---|---|
| Rebuild der Solution (`WP-Plan.sln`, Debug × x64, eigener `OutDir` im Scratch) | **0 Fehler**, 5 Warnungen — alle fünf unverändert aus dem Bestand (`KlimaregionStammCtrl`, `StromverbraucherStammCtrl`, `WErzeugerModel`, `MDIMainForm`) |
| Harness-Lauf (`dev\harness_dcheck`, 14 Fälle × 2 Läufe) | **Klasse a: 7 → 3, Klasse b: 0 → 0, Klasse c: 3 → 0, Klasse f: 0 → 0.** Die verbliebenen drei sind die 1-px-Berührungen aus D-Check Prio 3 (unverändert, ausdrücklich nicht im Auftrag). **Keine neuen Befunde in a/b/c** |
| Fußzeilentabelle neu erzeugt | `dev\dcheck_fusszeilen.tsv` — alle behandelten Dialoge 110 × 30, Reihenfolge nach Norm, Anker `Bottom, Right`, Abstand 12/12 (Abschnitt 2.2) |
| Größenprobe | Zwei Dialoge auf vier Fenstergrößen gezogen (Entwurf, 1280×800, kleiner als der Entwurf, aufgezogen): Fußzeile bleibt in **allen acht** Messungen bei 12/12 und 110×30 (Abschnitt 5.1) |
| PNG-Belege | 40 Bilder in `dev\dcheck_bilder\` mit Suffix `_D2` |
| Byte-Gate A/B gegen den unveränderten HEAD | **9 von 9 Projekten PASS, 226 von 226 CSV byte-gleich** (Abschnitt 5.2) |
| Produktive `Kenndaten.accdb` | MD5 `6E15CC7DF5F3B913CD97E4738D2B332F` **vor und nach** jedem Lauf identisch, mtime 28.08.2026 11:22 (Abschnitt 5.3) |

### 5.1 Größenprobe

Neuer Harness-Modus `Groessenprobe`: Dialog anzeigen, `f.Size` setzen, Abstand der Fußzeile zur
rechten unteren Ecke des Anzeigerechtecks messen.

```
[PROBE] Form_Simulation_Config Fenster 1136x659  -> btn_OK 12/12 110x30 Bottom, Right | btn_Speichern 132/12 175x30
[PROBE] Form_Simulation_Config Fenster 1280x800  -> btn_OK 12/12 110x30 Bottom, Right | btn_Speichern 132/12 175x30
[PROBE] Form_Simulation_Config Fenster 1136x659  -> btn_OK 12/12 110x30 Bottom, Right | btn_Speichern 132/12 175x30
[PROBE] Form_Simulation_Config Fenster 1300x820  -> btn_OK 12/12 110x30 Bottom, Right | btn_Speichern 132/12 175x30
[PROBE] Form_PufferSp          Fenster  790x535  -> btn_OK 12/12 110x30 Bottom, Right | btn_Abbrechen 132/12 110x30
[PROBE] Form_PufferSp          Fenster 1280x800  -> btn_OK 12/12 110x30 Bottom, Right | btn_Abbrechen 132/12 110x30
[PROBE] Form_PufferSp          Fenster 1000x640  -> btn_OK 12/12 110x30 Bottom, Right | btn_Abbrechen 132/12 110x30
[PROBE] Form_PufferSp          Fenster 1300x820  -> btn_OK 12/12 110x30 Bottom, Right | btn_Abbrechen 132/12 110x30
```

Die angeforderten 1600×950 klemmt Windows auf 1300×820 (Prüfrechner 1280×800) — die Reihe bleibt
auch dort in der Ecke. `Form_Simulation_Config` fällt bei 1000×640 auf seine Mindestgröße
1136×659 zurück, ebenfalls mit unveränderter Fußzeile.

Zusätzlich läuft der komplette Messlauf ein zweites Mal mit erzwungener Arbeitsfläche 1280×800
(Laufmarke `1280`); dort greifen Klemmung, veränderbare Berandung und Bildlauf. Die
Fußzeilenzeilen beider Läufe sind bis auf zwei Ausnahmen identisch:

* `Form_PufferSp_Projekt` aufgeklappt — der Bezugsrahmen der Norm ist die ungeklemmte Sollhöhe
  976, der Bildlaufbereich der Einpassung endet bei 964; die Knöpfe stehen bei y = 934, also
  12 px über der Entwurfsunterkante. Der Bildlaufbereich ist mit 964 px praktisch derselbe wie
  vor dem Paket (966).
* `Form_Simulation_Detail` — 1 px Unterschied in der X-Koordinate zwischen beiden Läufen
  (Rundung beim Umrechnen aus einer gerollten Fläche).

### 5.2 Byte-Gate

A = `git archive HEAD` nach `dev\ab_head\`, dort gebaut (`Referenzlauf`, Debug × x64, eigener
`OutDir`) und gelaufen (`lauf --ziel dev\ab_csv_A`); B = Arbeitsstand, gebaut in den Scratch und
gelaufen (`--ziel dev\ab_csv_B`). A/B statt Vergleich gegen einen eingefrorenen Referenzstand,
weil die produktive Quelle durch den arbeitenden Anwender driftet.

```
Projekt_1011: PASS (29 Dateien, 324241 Werte)      Projekt_1030: PASS (22 Dateien, 236667 Werte)
Projekt_1017: PASS (21 Dateien, 254152 Werte)      Projekt_1038: PASS (18 Dateien, 201546 Werte)
Projekt_1021: PASS (21 Dateien, 227854 Werte)      Projekt_1040: PASS (30 Dateien, 306763 Werte)
Projekt_1023: PASS (25 Dateien, 262935 Werte)      Projekt_1042: PASS (34 Dateien, 341836 Werte)
Projekt_1024: PASS (26 Dateien, 271715 Werte)
GESAMT: PASS (2427709 Werte innerhalb der Toleranz)
```

Zusätzlich zum Toleranzvergleich ein reiner Byte-Vergleich über alle Ergebnisdateien:
**226 CSV auf jeder Seite, 226 byte-gleich, 0 Abweichungen, 0 fehlend.** Die Zusage „UI-only" ist
damit belegt: keine einzige Ergebniszahl hat sich geändert.

*Anmerkung zum ersten A-Lauf:* Er meldete „8 von 9" — Projekt 1023 lief in den 300-s-Timeout.
Die Wiederholung derselben unveränderten A-Seite gegen dieselbe Quelle (MD5 identisch) lieferte
9 von 9 in 11 Sekunden. Der Timeout war ein Einmaleffekt beim Anlegen der Arbeitskopie, während
die Anwendung des Anwenders die Datenbank geöffnet hielt; er hat nichts mit dem Paket zu tun.

### 5.3 Produktive Datenbank

`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde nur gelesen. MD5 vor dem A-Lauf, zwischen A und
B und nach dem B-Lauf jeweils `6E15CC7DF5F3B913CD97E4738D2B332F`, mtime 28.08.2026 11:22:07.

Der Wert weicht von dem des D-Checks ab (`F7A422…`, mtime 09:05) — die Datenbank wurde zwischen
den beiden Paketen **vom laufenden Programm des Anwenders** geschrieben, nicht von diesem Paket.
Sämtliche Werkzeuge dieses Pakets arbeiten auf Kopien: der Messlauf auf `dev\db\Kenndaten.accdb`,
der Referenzlauf auf einer eigenen Arbeitskopie (`DbUmgebung.ArbeitskopieAnlegen`).

### 5.4 Kodierung

Zwei der geänderten Dateien sind Windows-1252, nicht UTF-8: `Form_Waermebedarf.cs` und
`Form_Prozesswaerme.cs`. Beide wurden über den iconv-Hinweg bearbeitet und mit Rundprobe
zurückgeschrieben (kein U+FFFD, Rückkonvertierung byte-identisch zur editierten Fassung). Nach dem
Paket: alle zwölf Dateien mit unveränderter Kodierung, durchgehend CRLF, kein Ersatzzeichen.
Der eingefügte Text dieser beiden Dateien ist bewusst reines ASCII.

---

## 6. Beste Belegbilder

| Bild | Zeigt |
|---|---|
| `dev\dcheck_bilder\Form_Simulation_Detail_SEITE_Ergebnis_Nav1_D2.png` | Autarkie-Analyse eingebettet: „Theoretischer Speicher (PV) (kWh):", „nicht benötigt" und „Therm. Nutzungsgrad: 0,0 %" jetzt vollständig, Balken unter ihrer Beschriftung. Vorher-Bild ohne Suffix daneben |
| `dev\dcheck_bilder\Form_WP_Pflege_Start_D2.png` | Die vierteilige Fußzeile Speichern / Neu / Löschen / OK in einer Größe und Flucht am rechten Rand — vorher drei Größen und 39 px Abstand |
| `dev\dcheck_bilder\Form_Waermebedarf_Start_D2.png` | Zwei Knöpfe in Normgröße, OK rechts von Abbrechen, 12/12 in der Ecke |
| `dev\dcheck_bilder\DashboardForm_ohneWaerme_Start_D2.png` | Der längste Fall der Solarthermie-Kachel („nicht benötigt") als eigenständiges Fenster |

---

## 7. Offene Punkte

| Prio | Punkt | Warum offen |
|---|---|---|
| **2** | `Form_WP` misst 1039 × 780 und passt damit nicht auf eine 1280×800-Arbeitsfläche. `FensterEinpassung` setzt dort KEINEN Bildlauf, weil sie ihr Entwurfsmaß erst im `Load` misst — dann hat Windows bereits auf 713 px Client geklemmt. Die untersten 28 px (die Fußzeile) sind auf einem solchen Schirm nicht erreichbar | Bestand, nicht durch dieses Paket entstanden (vorher waren es 21 px). Die saubere Lösung wäre, `FensterEinpassung` das Entwurfsmaß vor der Klemmung mitzugeben — dieselbe Mechanik, die `FusszeilenNorm` jetzt hat. Eigener Auftrag |
| **3** | Drei 1-px-Berührungen auf `Form_Simulation_Detail`, Register „Simulation" (`textBox_MaxStrombedarf`/`label20`, `textBox_MaxWaermelast`/`label1`, `label14`/`label1`) | D-Check Prio 3, Designer, optisch folgenlos |
| **3** | `Form_Simulation_Config`: Der Kartenbereich ist 19 px vom Rand eingerückt, die Fußzeile jetzt 12 px | Bewusst: EIN Randmaß für alle Dialoge war die Vorgabe. Anzugleichen wäre der Kartenrand, nicht die Fußzeile |
| **3** | `Form_WP.btn_Beenden` trägt die Beschriftung „OK" | Name und Aufschrift widersprechen sich. Eine Umbenennung ist ein Designer-Eingriff und berührt `KiDialoge.Waermepumpe()` |
| **4** | 26 Fremdschrift-Gruppen (Klasse e) | D-Check Prio 4, Sammelpaket „Schriftbild vereinheitlichen" |
| **4** | `Form_PufferSp_Projekt`, `Form_Waermesenke`, `Form_Simulation_Detail`: Bildlauf-Ratchet — `AutoScrollMinSize` wächst und schrumpft nie wieder | D-Check P1-O8, unverändert |

---

## 8. Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein/GrafikTools/FusszeilenNorm.cs` | **neu** — die Norm |
| `Views/Simulation/Form_Simulation_Config.Karten.cs` | `FusszeilePlatzieren()` ruft die Norm statt eigener Rechnung |
| `Views/Simulation/Form_Waermesenke.cs` | Knopfgröße aus der Norm, `Einhaengen` + `Anwenden` am Ende des Fußzeilenaufbaus, Fensterhöhe mit Normrand |
| `Views/Simulation/Form_Quellprofil.cs` | dasselbe, nach `HinweiszeileEinpassen()` |
| `Views/Pufferspeicher/Form_PufferSp_Projekt.cs` | `Einhaengen` im Konstruktor; `SchichtSichtbarkeitSetzen` löst vor dem Umräumen den Anker und richtet nach der Einpassung mit `BezugSetzen(_schichtSollHoehe)` aus |
| `Views/Simulation/Form_QuellePufferspeicher.cs` | `Einhaengen` im Konstruktor |
| `Views/Wärmebedarf/Form_Waermebedarf.cs` | `Einhaengen` im Konstruktor (CP1252) |
| `Views/Pufferspeicher/Form_PufferSp.cs` | `Einhaengen` im Konstruktor |
| `Views/Prozesswärme/Form_Prozesswaerme.cs` | `Einhaengen` + `ZeileMitziehen` im Konstruktor (CP1252) |
| `Views/Wärmepumpe/Form_WP.cs` | neue `FusszeileNormen()` aus BEIDEN Konstruktoren, vier Knöpfe |
| `Views/Simulation/Form_Simulation_Detail.cs` | `Einhaengen(btn_Beenden)` + `ZeileMitziehen`, `tabControl3` unsichtbar, `LaufmeldungenLabelSicherstellen` nimmt rechts verankerte Knöpfe aus |
| `Views/Simulation/DashboardForm.cs` | `LayoutEinpassen()`, `KachelOrdnen()`, `BreiteMessen()`, `OnLoad`, `OnFontChanged`, Entwurfsmaße, `lblCO2`-Schrift |
| `Allgemein/Simulation/D2_FusszeilenNorm_Protokoll.md` | dieses Protokoll |

Werkzeugseitig (unter `dev/`, gitignored): `dev\harness_dcheck\Program.cs` um die Messung gegen
das Anzeigerechteck, zwei `DashboardForm`-Fälle, die Größenprobe und die TSV-Ausgabe erweitert.
