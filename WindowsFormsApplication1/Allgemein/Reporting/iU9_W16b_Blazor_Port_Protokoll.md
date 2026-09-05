# iU9 Welle 16b — Die Startseite: Form_Start, FormMain, AktionsKarte, Form_Hinweis — Portprotokoll

> Teilwelle **W16b** des Pakets iU9 (Welle 16 = der Rahmen K5 in drei Teilwellen).
> Grundlage: `iU9_W16_Vermessung.md` (1 907 Zeilen, Stand `4101740`) und die
> Arbeitsanweisung `iU9_W16_Arbeitsanweisung.md`, Abschnitt „W16b — Die Startseite".
> Basis: `84d7c16` (Statusblock W16a).
> **W16c (Hauptfenster) ist die dritte Teilwelle** mit eigenem Gate und eigenem Merge.

---

## 0 — Was die Teilwelle getan hat

**Die Wurzel der Anwendung aus Anwendersicht ist Razor — und der Altzweig ist weg.**
Zusammen **34 gelöschte Dateien**, 13 019 gelöschte gegen 5 549 neue Zeilen:

| Bauteil | `.cs` | Designer | `.resx` | Nachfolge |
|---|---|---|---|---|
| `Form_Start` | 2 339 (+ 1 864 `.bak`) | 1 381 | 4 900 | `EPOS.UI/Seiten/Start/Startseite.razor` + 5 Reiterkomponenten (S1) |
| `FormMain` | 1 345 | 634 | 1 854 | **keine** — Anwenderentscheid E‑7 (K6‑a) |
| `Form_StromTest` | 80 | 167 | 120 | **keine** — ein Prüfstand im Auslieferungsstand (Befund W16‑B31) |
| die 12 `*KontextMenuCtrl` | 2 381 | – | – | **keine** — einziger Erzeuger war `FormMain` (Befund W16‑B28) |
| `StromTestClass` | 101 | – | – | **keine** |
| `AktionsKarte` | 323 | 91 | 120 | der Baustein `Kachel` (seit W16a.2 mit `Zustand`/`Aktiv`) |
| `Form_Hinweis` | 34 | 103 | 943 | `Warnbanner.Verfaellt` (gebaut in W15b.1) |
| `FormStartProjektKontext` | 78 | – | – | `EPOS.Kern/Controller/ProjektKontextCtrl` (K2) |

Neu im Kern: **`ProjektKontextCtrl`** (K2), **`StartseiteCtrl`** (K4) und
**`BedarfsZustand`** (E‑5). Neu in `EPOS.UI`: die Seite `Startseite` mit
`ProjektReiter`, `WaermebedarfReiter`, `StrombedarfReiter`, `ErzeugerReiter`,
`SimulationReiter`, dazu `StartKachel`, `Zusammenfassung`, `Kachelschluessel` und
`Reiterschluessel`. Neu in der Anwendung: **`StartseiteHuelle`** (753 Z.).

**`WindowsFormsApplication1` führt seither ZWEI Masken** — `MDIMainForm` (die Hülle,
fällt in W16c auf 120–160 Zeilen zurück) und `Form_HelpPopup` (bleibt bis iU11,
Entscheid W15b‑E‑2) — **und NULL Inline-SQL** (Befund W16‑B34 eingelöst).

---

## 1 — Commits

| # | Commit | Inhalt |
|---|---|---|
| W16b.0 | `b10cfc1` | `ProjektKontextCtrl` (K2) und `StartseiteCtrl` (K4) im Kern, Nachweis **N7** |
| W16b.1 | `55a3f0e` | **E‑7 / K6‑a**: `FormMain`, `Form_StromTest`, `StromTestClass` und die zwölf `*KontextMenuCtrl` fallen |
| W16b.2 | `1d701d7` | `Startseite.razor` mit fünf Reiterkomponenten (S1), 78 neue Texte, Nachweis **N3** |
| W16b.3/.5 | `428443f` | `StartseiteHuelle`, `MDIMainForm_Load`, `Dienste.Projekt` → Kern; `Form_Start`, `AktionsKarte`, `Form_Hinweis` fallen; E‑9 |
| W16b.4 | `a7e0a2d` | **E‑5**: Konfiguration als freie Ansicht, Ergebnis als `Ueberlagerung`, `BedarfsZustand`; K6 |
| — | `b12edbb` | Der Projektwechsel-Nachweis zu **R‑W16‑4** (Vermessung § 16.3) |

> **W16b.3 und W16b.5 sind EIN Commit** — mit Begründung. Die Anweisung trennt sie
> („.3 hängt die Seite ein, .5 löscht `Form_Start`"); dazwischen läge aber ein Stand,
> in dem `MDIMainForm_Load` die Maske nicht mehr baut, sechs andere Stellen sie aber
> noch rufen (`Program.startfrm` wäre `null`) — ein Commit, der übersetzt und nicht
> läuft. Beides zusammen ist der kleinste Schritt, der beides erfüllt.

---

## 2 — Was vor der Teilwelle nachgemessen wurde

| Maß | Sollwert der Anweisung | Gemessen (Basis `84d7c16`) |
|---|---|---|
| Stapellauf `--alle WindowsFormsApplication1` | 7 Masken / 8 Designer, 7 / 0 / 0 / 0 | **erfüllt** (3 lokalisiert) |
| `dotnet test WP-Plan.Kern.slnf` | 3 923 | **3 923** (450 / 337 / 997 / 2 139) |
| `dotnet test Werkzeuge/Formularkarte.Tests` | 123 | **123** |
| Build-Warnungen | 6 | **6** |
| `ChartProben` | 32 | **32** |
| SQL-Texte | 1 234 | **1 234**, 0 Fundstellen |

**`git grep` vor dem Löschen** (Auflage E‑7): `FormMain` hat außerhalb seiner eigenen
fünf Dateien genau **sechs** Codestellen — `WinFormsNavigation` (3), `Program.mainfrm`,
`ErreichbarkeitTests`, `HilfeKontext`; alles Übrige sind Kommentare und Protokolle.
Die **zwölf `*KontextMenuCtrl` haben genau EINEN Erzeuger**, `FormMain` (Befund
W16‑B28 bestätigt, keine Ausnahme). `Gewerke`/`OeffneGewerk` werden ausschließlich
von ihnen und von `WinFormsNavigation` gerufen (Befund W16‑B27 bestätigt).
`ProjektInFormMainLaden` und `ProjektDetailZeigen` haben je einen Aufrufer.

---

## 3 — Feldkartenabgleich `Form_Start` (108 Kartenzeilen)

Die Feldkarte ist vor der Umstellung gezogen worden
(`dotnet run --project Werkzeuge/Formularkarte -- Form_Start.Designer.cs`) und liegt
seither als **Prüfmuster** unter `Werkzeuge/Formularkarte.Tests/Pruefmuster/Hauptformular/`.

### Kopfband (Fenster, `panelKlima`, `panelVariante`)

| Kartenzeile | Nachfolger | ☑ |
|---|---|---|
| `label20` „Energieplanungs-Software" | `START_GATTUNG` im Kopfband | ☑ |
| `label11` „Projekt:" + `textBox_ProjektOpen` | `START_LBL_PROJEKT` + das Auswahlfeld (der Text stand seit dem Projektkopfumbau ohnehin dort) | ☑ |
| `comboBox_Varianten` (`Stamm / Variante:`) | dasselbe Auswahlfeld — `label4` war schon im Bestand zur Laufzeit entfernt | ☑ |
| `label_ProjektStatus` ✔/⚠ | `.epos-startseite-status--offen/--keins` | ☑ |
| `label12` „Klimaregion auswählen:" + `comboBox_Klima` | `START_LBL_KLIMA` + Auswahlfeld | ☑ |
| `label21` „Region auswählen" | Leereintrag des Auswahlfeldes bei offenem Projekt | ☑ |
| `label22` „Gilt für alle Berechnungen im Projekt" | `.epos-startseite-klimahinweis` | ☑ |
| `btn_Speichern` (Bildknopf ohne Text) | Knopf „Speichern" (`WIZ_BTN_SPEICHERN`) | ☑ |
| `btn_Weiter` / `btn_Zurueck` | die zwei Fußknöpfe | ☑ |
| `btn_Help` (Fensterhilfe) | **nach W16c** — sie gehört zum Fenster, nicht zur Seite (§ 11) | ☐ |
| `pictureBox1…4`, `pictureBox2`, `label_Haus` (Zierrat) | entfällt | ☑ |
| `tabControl_Wizard` (`OwnerDrawFixed`) + 6 `TabPage` | `Reiter` + 6 `Reiterblatt` | ☑ |

### Reiter 1 „Projekt" (6 `AktionsKarte`)

| Karte | Nachfolger | ☑ |
|---|---|---|
| `karte_ProjektNeu`, `…Oeffnen`, `…Zuletzt`, `…SpeichernUnter`, `karte_Delete` | fünf `Kachel` im `Kachelraster` | ☑ |
| `karte_ProjektDetails` | **entfällt** (E‑7: einziger Weg in `FormMain`, Befund W16‑B20) | ☑ |
| `label3` / `label1` | `START_P_KOPF` / `START_P_TEXT` | ☑ |
| `btn_Help_Kurzanleitung` | `InfoKnopf Schluessel="Form_Start.btn_Help_Kurzanleitung"` | ☑ |

### Reiter 2 „Wärmebedarf" (4 Bildkacheln)

| Kartenzeile | Nachfolger | ☑ |
|---|---|---|
| `pBox_Gebaude` + `label_pBox_Gebaude` + `label2_pBox_Gebaude` | **eine** `Kachel` | ☑ |
| `pBox_WBedarfDaten`, `pBox_Prozess`, `pBox_Brauchwasser` (je Trio) | je **eine** `Kachel` | ☑ |
| `pBox_WBHinweis` + `label33` „Hinweis" + `label32` | ein `Warnbanner` der Stufe Hinweis | ☑ |
| `label25` / `label24` | `START_W_KOPF` / `START_W_TEXT` | ☑ |
| `btn_Help_Waermebedarf` | `InfoKnopf` mit demselben Schlüssel | ☑ |
| `Label2` (ohne Text, ohne Handler) | entfällt | ☑ |

### Reiter 3 „Strombedarf" (3 Bildkacheln)

| Kartenzeile | Nachfolger | ☑ |
|---|---|---|
| `pBox_StdLastProfil`, `pBox_StromProfilEigenes`, `pBox_StromMessdaten` (je Trio) | drei `Kachel` | ☑ |
| `label27` / `label26` | `START_S_KOPF` / `START_S_TEXT` | ☑ |
| `btn_Help_Strombedarf` | `InfoKnopf` mit demselben Schlüssel | ☑ |
| `Label5`, `Label6` (ohne Text) | entfallen | ☑ |

### Reiter 4 „Energieerzeuger" (7 Bildkacheln)

| Kartenzeile | Nachfolger | ☑ |
|---|---|---|
| `pBox_WP`, `pBox_Heizkessel`, `pBox_Solarthermie`, `pBox_BHKW`, `pBox_PV`, `pBox_Stromspeicher`, `pBox_Pufferspeicher` (je Trio) | sieben `Kachel` | ☑ |
| `radioButton_KollektorProfil` / `radioButton_Ganglinie` | zwei Auswahlknöpfe UNTER der Solarthermiekachel (`role="radiogroup"`) | ☑ |
| `label29` / `label28` | `START_E_KOPF` / `START_E_TEXT` | ☑ |
| `label58` „Tipp" / `label59` | ein `Warnbanner` der Stufe Hinweis | ☑ |
| `btn_Help_Energieerzeuger` (programmatisch angebracht) | `InfoKnopf` im Markup — `InfoKnopf.Anbringen` entfällt | ☑ |
| `Label7`, `Label8`, `pictureBox3` (Zierrat) | entfallen | ☑ |

### Reiter 5 „Simulation"

| Kartenzeile | Nachfolger | ☑ |
|---|---|---|
| `btn_SimKonfig` „Simulation Konfiguration…" | Knopf, zählt als 21. Kachel | ☑ |
| `pBox_DetailSim` + 2 Label | **eine** `Kachel` | ☑ |
| `pBox_Optimierung` + `label_pBox_Optimierung` + `label2_pBox_Optinierung` | **wird gar nicht gebaut** (H11: leerer Handler, zur Laufzeit ausgeblendet) | ☑ |
| `pictureBox_Zusammenfassung` + `label70` + vier Paare | die `<dl>` der Projektzusammenfassung | ☑ |
| `label31` / `label30` | `START_SIM_KOPF` / `START_SIM_TEXT` | ☑ |
| `btn_Help_Simulation` (programmatisch) | `InfoKnopf` im Markup | ☑ |
| `Label9`, `Label10` (ohne Text) | entfallen | ☑ |

### Reiter 6 „Berichte && Kosten"

Unverändert die `BerichteKostenSeite` aus W5.6 — sie wechselt nur den Wirt (von der
`BlazorSeite` in `tabPage6` in ein `Reiterblatt` derselben WebView). ☑

**Ersatzlos entfallen und nicht vergessen** (rund 700 der 2 300 Zeilen): die 13
`Paint`-Handler, die 14 Weiterleitungshandler `label46_Click` … `label74_Click`, das
Wörterbuch `_clickEvents` mit 24 Einträgen samt `CentralControl_Click` (Befund
W16‑B19), `tabControl_Wizard_DrawItem`, `SetzeDropDownBreite`, `ProjektkopfAufbauen`,
`KopfNameZeigen`/`KopfNameWaehlen`/`KopfEinzeltextZeigen`, `panelKlima_Paint`,
`panelVariante_Paint`, `RundesRechteck`, `OptimierungskachelVerbergen`,
`LiesProjektname` und **`UpdateWizardSymbole`**.

---

## 4 — Die Angleichungen (A‑1 … A‑10)

| # | Was | Wie | Warum |
|---|---|---|---|
| **A‑1** | Die 13 `Paint`-Handler des Statusanstrichs (je ~45 Z. `GraphicsPath`, Halbdeckkraft, farbiger Balken) | zwei CSS-Klassen am Statuspunkt der `Kachel` | `System.Drawing` gibt es in `EPOS.UI` nicht; die Aussage („im Projekt / nicht im Projekt") ist dieselbe |
| **A‑2** | Drei Bindemuster für denselben Kachelklick (Wörterbuch + 14 Weiterleitungen + 6 `Geklickt`) | **ein** `@onclick` je Kachel mit einem sprachneutralen Schlüssel | Befund W16‑B19; ~90 Zeilen Klebstoff und 24 Wörterbucheinträge fallen |
| **A‑3** | `UpdateWizardSymbole` (13 Bits, 7 Inline-SQL, 6 `ReadAllFilter`) | `KomponentenBestandCtrl.Bitmaske` | Entscheid E‑3; N6 belegt die Bitgleichheit für alle 13 Referenzprojekte |
| **A‑4** | Die Reitersperre: gesperrte `TabPage` + `Form_Hinweis` beim Klickversuch | `Reiterblatt Bedienbar` + ein DAUERHAFTES `Warnbanner` mit denselben zwei Sätzen | Ein gesperrter Reiterknopf lässt sich gar nicht anklicken — der Hinweis muss vorher dastehen, nicht danach |
| **A‑5** | `Form_Hinweis` (3 s Selbstverfall, `PointToScreen` über der auslösenden Kachel) | `Warnbanner` mit `Verfaellt="3 s"` | Entscheid W15b‑E‑1b; ein Banner steht IN der Seite und braucht keinen Bildschirmpunkt. Der Lebenszeit-Kunstgriff des Vorläufers („bewusst OHNE `using`") entfällt mit ihm |
| **A‑6** | Die fünf `MessageBox` des Klimaspeicherwegs | **ein** `Warnbanner`, Stufe aus dem Ausgang (`KlimaStand`) | Die Schlüssel sind unverändert; nur der Träger ist ein anderer |
| **A‑7** | Die zwei Auswahlknöpfe der Solarthermie trugen ZUGLEICH den Status (grün bei Kollektoren ODER Ganglinien, `:1313-1325`) | **entfällt** — die Farbe sagt der Statuspunkt der Kachel (Bit 512) | Eine Aussage an zwei Stellen ist eine Wahrheit zu viel; die WEICHE (Profil/Ganglinie) bleibt vollständig erhalten |
| **A‑8** | `IProjektKontext.Vorhanden` war `Program.startfrm != null` | `true` — der Kern-Träger existiert, sobald `Program.Main` ihn eingelegt hat | Dieselbe Antwort wie `EPOS.iOS/Dienste/IosProjektKontext`. Ohne Oberfläche steht weiter `LeererProjektKontext` mit `false`, die Fallgabelung von `KiAktionenProjekt` bleibt unverändert |
| **A‑9** | „Öffnen…" und „Zuletzt geöffnet" im Menü bauten `FormMain` | sie setzen das Projekt AKTIV (`MenueCtrl.ProjektAktivSetzen`) | E‑7: Das Detailformular gibt es nicht mehr; die Kacheln taten schon vorher genau das |
| **A‑10** | Die Projektzusammenfassung rechnete ihre Rechtsbündigkeit in drei `Left`-Zuweisungen aus | CSS-Raster | Eine WebView hat keine gerechneten Pixel |

**Nicht angeglichen, obwohl es naheläge:** die 21 Kachelwege (Liste lesen → Hülle →
zurückschreiben) sind Anweisung für Anweisung übernommen, einschließlich der Stellen,
an denen der Bestand das Änderungsdatum fortschreibt und der Stellen, an denen er es
NICHT tut; die Reihenfolge der Reiter; die 17 Meldungsschlüssel; die Regel, dass ein
Variantenwechsel sich NICHT als „zuletzt geöffnet" merkt.

---

## 5 — Anwenderfragen

| # | Frage | Stand |
|---|---|---|
| **E‑7** | `FormMain` und `Form_StromTest` stilllegen? | **Vorläufig ja, umgesetzt.** 3 907 Zeilen `.cs` und 6 682 Zeilen insgesamt sind gefallen. **Was der Anwender verliert:** die Gewerksübersicht **in Listenform** und das **Drag & Drop zwischen den zwölf Listen** — das gab es an keiner anderen Stelle des Programms (§ 7 f der Vermessung). **Was die Startseite an seiner Stelle bietet:** dieselben zwölf Gewerke als Kacheln mit einem STATUSPUNKT je Gewerk (grün = im Projekt, grau = nicht) — die Übersicht „was steckt in diesem Projekt" ist damit auf EINEN Blick sichtbar statt in zwölf Listen verteilt; jede Kachel führt in denselben Bearbeitungsdialog, den das Kontextmenü der Liste öffnete; das Anlegen, Ändern und Löschen einer Anlage steht in diesen Dialogen vollständig. **Nicht ersetzt** ist das VERSCHIEBEN eines Katalogeintrags von einer Liste in eine andere per Maus. |
| **E‑5** | Simulationskonfiguration/-ergebnis modal? | **Vorläufig ja, umgesetzt.** Die Konfiguration ist eine freie Ansicht (sie löst die Startseite in derselben WebView ab), das Ergebnis eine `Ueberlagerung` derselben Seite — modal bleibt es, weil der Lauf beim Öffnen von selbst startet. Die zwei Bedarfsobjekte gehören jetzt dem PROJEKT (`BedarfsZustand`), nicht mehr einem Fenster; **damit ist der Grund für beide Fenster weg** (Befund W11‑B3). Die Entscheide R‑W10b‑1 und R‑W11‑1 sind erfüllt; ihr Nachtrag im Umsetzungskonzept ist Sache der Orchestrierung. |
| **E‑2** | `Masken` und `Seitenschluessel` zusammenlegen? | **Vorbereitet, nicht vollzogen** (K7 ist W16c). `Masken.ProjektDetail` und `Ansichten.ProjektDetail` sind mit E‑7 gefallen; `Masken` führt damit noch **24** Schlüssel, von denen genau einer (`Assistent`) einen zusammengesetzten Ablauf trägt — und der führt seit W16a in eine Razor-Hülle. |
| **E‑1** | `AppWurzel` als gemeinsame Wurzel? | **Soweit W16b sie berührt: ja.** `IProjektQuelle.Startkacheln(int)` steht mit Standardumsetzung (K6); der Zweig in `AppWurzel` und `Seitenschluessel.STARTSEITE` kommen mit W16c (K7). |
| **E‑9** | Wohin mit den Zeugen? | **Umgesetzt.** `Form_Start.Designer.cs` und seine drei `.resx` (6 281 Z., 108 Kartenzeilen) sind eingefroren nach `Pruefmuster/Hauptformular/` gewandert; die **fünf letzten Typzeugen** des Stapellaufs (`Label`, `TextBox`, `Button`, `ComboBox`, `TabPage`) hängen seither dort. Der **Maskenschlüssel-Zeuge ist gestrichen** (offener Punkt W16b‑O‑1), der „ja"-Zeuge steht unverändert an `MDIMainForm`. |
| **W16a‑E‑1 (aus W16a)** | Wird der Assistent mit der Razor-Startseite eine freie Ansicht? | **W16b hat es nicht getan.** Der Assistent bleibt eine modale `BlazorDialogForm`: Seine beiden Aufrufer (`MenueCtrl.ProjektNeu`/`…Bearbeiten`) werten aus, ob gespeichert wurde, und `MDIMainForm` zieht danach den Projektkontext nach — anders als bei den Simulationsseiten hängt daran ein SCHREIBWEG. **Technisch möglich wäre es jetzt** (die Startseite kann Ansichten wechseln, siehe E‑5); es bräuchte denselben Umbau der zwei Aufrufer. **Entschieden 04.09.2026: ja — der Assistent wird in iU11 zusammen mit der Transaktion W16a‑O‑1 eine freie Ansicht.** |
| **W16b‑E‑1 (neu)** | **Der Reiter „Simulation" springt jetzt SICHTBAR zurück.** Ohne gesetzte Klimaregion holte der Vorläufer den Anwender mit `tabControl_Wizard.SelectedIndex = 0` auf Reiter 1 und zeigte eine `MessageBox`; die Seite tut dasselbe, aber die Meldung steht als Banner OBEN und verschwindet nicht von selbst | Wortlaut und Sprungziel sind unverändert. **Bestätigt 04.09.2026.** |
| **W16b‑E‑2 (neu)** | **Ein Variantenwechsel im Kopfband frischt den Reiter „Berichte & Kosten" mit** | Das tat der Vorläufer schon (`VariantenAnzeigeAktualisieren` im Nachzug); neu ist, dass er es auch dann tut, wenn der Reiter noch gar nicht aufgebaut wurde — die Hülle hält ihn jetzt von Anfang an. Kein Verhaltensunterschied für den Anwender, aber ein Ladevorgang mehr beim ersten Wechsel. **Bestätigt 04.09.2026.** |
| **W16b‑E‑3 (neu, 05.09.2026)** | **Bilder der Kacheln und Symbole der Aktionskarten wie im Vorläufer** — Anwenderwunsch 05.09.2026 („Icons fehlen", zum ersten Bildschirmfoto der gestylten Startseite) | **Umgesetzt.** Die 21 Kacheln tragen wieder ihr Sinnbild aus `Form_Start`: die fünf Aktionskarten ihr `*_Symbol.png` (Herkunft `karte_*.KartenBild`, Designer :238–273), die sechzehn Bildkacheln das JPG ihrer `pictureBox` (Herkunft `pBox_*.BackgroundImage`). Die 20 Dateien sind **dieselben** und unverändert (`git mv`) nach `EPOS.UI/wwwroot/bilder/start/` gewandert und aus `Properties/Resources.resx`/`.Designer.cs` ausgetragen — sie hatten seit W16b keinen Nutzer mehr. Die Zuordnung steht als Tabelle `EPOS.UI/Seiten/Start/Kachelbilder.cs`, nicht in `StartKachel`: Das Bild hängt am SCHLÜSSEL, nicht an den Daten, sonst müssten es die Windows-Hülle und der iOS-Weg getrennt setzen. Zugeschnitten wird im **Stilblatt** (`object-fit: none` + `object-position`), nicht in der Datei — die Kachel-JPG sind die GANZE Kachel des Vorläufers, eine weiße Karte von rund 554 × 260 mit dem Sinnbild oben links. **Zwei Abweichungen**, beide in der Tabelle begründet: `pBox_Heizkessel` trug sein Bild eingebettet in `Form_Start.resx` (die Bytes sind Byte für Byte `PHeizkessel.jpg`, 8 066 B, 551 × 215 — nachgemessen, nicht geraten), und `btn_SimKonfig` war ein Knopf OHNE Bild; als 21. Kachel bekommt er `PSchnellSim.jpg`, das einzige Kachelbild des Bestands ohne eigene Kachel |
| **W16b‑E‑4 (neu, 05.09.2026)** | **Die Gattungszeile der Startseite steht nur ohne Kopfleiste** | **Umgesetzt.** „Energieplanungs-Software" stand ein zweites Mal frei links neben dem Projektfeld — unter dem Kopfband des Hauptfensters, das PRODUKTNAME, GATTUNG und CLAIM ohnehin nennt. Die Seite führt dafür `KopfbandZeigen` (Vorgabe `true`); `AppWurzel` setzt es auf `false`, sobald sie eine `Kopfleiste` gezeichnet hat — das Attribut steht nach dem Parametersatz und gewinnt deshalb gegen ihn. **Auf iOS ist die Kopfleiste leer** — dort bleibt die Zeile die einzige Nennung des Produkts und steht unverändert. Im Bestand war sie `label20` (Segoe UI Semibold 26 pt fett, weiß auf `#6876DF`) auf dem Titelband `pictureBox2`, also eine EIGENE Zeile über den zwei Kopfkästen und nicht neben ihnen |
| **W16b‑E‑5 (neu, 05.09.2026)** | **Design und Farbgebung an die WinForms-Fassung vor W16 anlehnen** — Anwenderwunsch 05.09.2026 („Design und Farbgebung kann verbessert werden, angelehnt an winforms Version vor-W16"), zum zweiten Bildschirmfoto nach W16b‑E‑3 | **Umgesetzt — ausschließlich im Stilblatt.** W16b‑E‑3 hat die BILDER zurückgeholt und die Anordnung angeglichen; was fehlte, war die FARBE. Erhoben wurden alle Farb-, Schrift- und Flächenwerte von `Form_Start` aus `84d7c16` (Designer, die drei `.resx` und die Laufzeitfarben in `Form_Start.cs`); die vollständige Gegenüberstellung steht unten als Tabelle „Vorbild → Umsetzung". Die Angleichung läuft über **sieben neue Token** in `:root`, die NUR `Seiten/Start/*` benutzen darf (`--epos-start-*`): Die Startmaske hatte eine eigene Handschrift — größere Schrift, kühlere Rahmen, eine gefüllte Reiterzunge, graue Knöpfe —, und wer die über den gemeinsamen Farbsatz durchreichte, färbte sechzig Dialoge mit um. Das Sichtbarste sind vier Stücke: die **Reiterleiste** steht wieder auf ihrem eigenen grauen Grund und die aktive Zunge ist **gefüllt mit weißer Schrift**, die zwei **Kopfkästen** tragen das kühle Blaugrau des Vorläufers statt des warmen Hausbeige, jede **Erläuterung** (Reiter wie Kachel) steht in DimGray halbfett statt in Fließtextgrau, und die zwei **Fußknöpfe** sind hellgrau wie im Bestand. **Kein Markup ist geändert**, keine feste Pixelkoordinate hinzugekommen. **Drei Farben des Vorläufers sind bewusst NICHT übernommen** — sie tragen den Hauskontrast von 4,5:1 nicht (Tabelle unten, Spalte „warum nicht"); die Rechnung dazu steht als Fall in `StartseiteAnmutungTests` und fällt rot aus, sobald jemand ein Token aufhellt |


### Die Farb- und Schrifttabelle zu W16b‑E‑5 (Vorbild → Umsetzung)

> Erhoben aus `84d7c16`: `Views/Hauptformular/Form_Start.Designer.cs` (1 381 Z.),
> `Form_Start.resx` (41 Schrifteinträge) und `Form_Start.cs` (Laufzeitfarben,
> Reiterzeichnung, die zwei `*_Paint` der Kopfkästen). „bis `830c903`" ist der Stand,
> den der Anwender am 05.09.2026 gesehen hat.

| Element | Vorbild (Wert, Fundstelle) | bis `830c903` | jetzt |
|---|---|---|---|
| Fensterhintergrund | `Color.White` (Designer :1203) | `body` auf `--epos-karte-flaeche` (#ffffff) | unverändert |
| **Reiterleiste, Grund** | `SystemColors.Control` (#f0f0f0) — die Zungen standen auf einer eigenen Fläche | keine Fläche, nur eine Linie | `--epos-start-leiste-flaeche` #f0f0f0, 4 px Polster oben |
| **Reiterzunge, aktiv** | `e.DrawBackground()` = `SystemColors.Highlight`, Text `0xffffff` (`Form_Start` :131–141) | durchsichtig, Text `--epos-quelle-text`, 3-px-Unterstrich | **gefüllt** `--epos-start-reiter-aktiv` #005aa0, Text weiß, Unterstrich in derselben Farbe |
| Reiterzunge, inaktiv | Text `0x000000` (:139) | `--epos-text-leise` #5f5e5a | `--epos-text` #2c2c2a |
| Reiterzunge, gesperrt | `TabPage.Enabled = false` (:80) | `--epos-text-sehr-leise` | unverändert (eigene Regel, damit die vierklassige Regel darüber sie nicht schlägt) |
| Reiterschrift | Segoe UI Semibold 12 pt fett = 16 px (`.resx` :3275) | 16 px / 600 | unverändert |
| **Reiterblatt** | der weiße Körper unter der Leiste | keine Fläche, kein Rahmen | weiß, Rahmen `--epos-start-kasten-rahmen`, oben ohne Fuge, 10 px Polster |
| **Kopfkästen, Rahmen** | `Pen(Color.FromArgb(180, 190, 205), 1.5f)` auf Rundeck 8 (`panelKlima_Paint`/`panelVariante_Paint`, :2273–2292) | `--epos-rahmen-leise` #d9d7cf, Radius 6 | `--epos-start-kasten-rahmen` #b4becd, Radius 8 (Strichbreite bleibt 1 px) |
| Beschriftungen der Kopfkästen | `label12`/`label11` Segoe UI 12 pt = 16 px | 13 px fett, #0f1f3d | 16 px / 600, `--epos-text` |
| `label11`-Fläche | weiß auf #6876df (Designer :1078–1079) | — | **nicht übernommen**: 3,76:1 — genug für die 26-px-Gattungszeile (große Schrift, 3:1), zu wenig für 16 px (4,5:1) |
| Statuszeichen ✔/⚠ | Microsoft Sans Serif 18 pt = 24 px; `Color.Green` bzw. 192,0,0 (:188, :1211) | 16 px | 20 px, Farben unverändert |
| Gattungszeile | `label20` Segoe UI Semibold 26 pt fett, weiß auf #6876df (:1085) | ebenso | unverändert — sie steht seit W16b‑E‑4 nur ohne Kopfleiste, also auf iOS |
| Reiterüberschrift | `label3/25/27/29/31` Segoe UI Semibold 16 pt fett = 21 px, `ControlText` | 21 px / 700, `--epos-karte-titel` | unverändert |
| **Reitererläuterung** | `label1/24/26/28/30` Segoe UI Semibold 12 pt **fett** in `Color.DimGray` | 16 px, normal, `--epos-text-leise` #5f5e5a | 16 px / 600, `--epos-start-text-leise` #696969 |
| Hinweis-/Tippkasten | `label32/33`, `label58/59`: Fläche 239,246,255, Text 35,66,159 | `--epos-karte-flaeche-hover` #eff6ff, `--epos-karte-titel` | unverändert — die Fläche ist **wertgleich** (KartenStil hat sie von dort) |
| Kachel, Fläche und Rahmen | die weiße Karte im JPG; `KartenStil.KARTE_RAHMEN` #d1d5db | `--epos-karte-flaeche`/`--epos-karte-rahmen` | unverändert |
| Kachel, überfahren | `KARTE_RAHMEN_HOVER` #3b82f6 (2 px), `KARTE_FLAECHE_HOVER` #eff6ff | ebenso | unverändert |
| Kacheltitel | `label_pBox_*`, geerbt Segoe UI Semibold 12 pt fett = 16 px, schwarz auf Weiß | 16 px / 700, `--epos-karte-titel` | unverändert |
| **Kachelerläuterung** | `label2_pBox_*`, dieselbe Schrift in `Color.DimGray` (Designer :315, :331, :345, :360 …) | 13 px, normal, `--epos-karte-text` #5a6270 | 16 px / 600, `--epos-start-text-leise` #696969 |
| Statuspunkt | `Color.FromArgb(90, 0, 255, 0)` | `--epos-karte-status` (**wertgleich**) | unverändert |
| Statusanstrich der GANZEN Kachel | halbdeckendes Grün plus blaues Rundeck 0,150,230 (13 `*_Paint`) | ein Statuspunkt (Angleichung **A‑1**) | **nicht zurückgeholt** — A‑1 ist eine getroffene Angleichung, und die Aussage ist dieselbe |
| **Zusammenfassung, Fläche** | 249,250,252 (Designer :903, :911, :919, :927, :935) | weiß | `--epos-start-zusammenfassung` #f9fafc |
| Zusammenfassung, Beschriftung | Segoe UI Semibold 13 pt fett in `DimGray` | 700, `--epos-karte-titel` | 600, `--epos-start-text-leise` |
| Zusammenfassung, Wert | Segoe UI 12,75 pt fett in 128,128,255 | `--epos-text` | `--epos-marke` #005aa0 — **Farbe nicht übernommen**: 128,128,255 trägt auf #f9fafc nur 3,12:1 |
| **Knöpfe „◀ Zurück"/„Weiter ▶"/„Simulation Konfiguration…"** | `Color.LightGray` zur Laufzeit (`Form_Start_Load` :85–88), Rundeck 6 aus `MakeSmoothButton`, Segoe UI Semibold 12 pt fett, 132×35 | `--epos-flaeche` #f5f4ef | `--epos-start-knopf-flaeche` #d3d3d3, Rahmen `--epos-start-kasten-rahmen` |
| Knopf gesperrt | Windows graut die Fläche mit aus | nur die Textfarbe | zusätzlich Fläche `--epos-flaeche` — sonst sähe „Zurück" auf Reiter 1 bedienbar aus |

**Was bewusst NICHT angeglichen wurde**

| Was | Warum |
|---|---|
| Die drei Farben mit zu wenig Kontrast (`label11`-Fläche, die Zusammenfassungswerte in 128,128,255, die Versionsfarbe 150,156,162 aus W16c) | Die Hausschwelle ist 4,5:1 für Fließtext. Alle drei liegen darunter (3,76 / 3,12 / 2,77). Der Ersatz bleibt in derselben Farbfamilie und ist lesbar; `StartseiteAnmutungTests.Jede_neue_Paarung_haelt_den_Hauskontrast` rechnet es aus dem Stilblatt nach |
| Der Statusanstrich der ganzen Kachel (A‑1) | Eine getroffene Angleichung der Teilwelle. Der Statuspunkt sagt dasselbe, und die 13 `Paint`-Handler wären wieder `System.Drawing` |
| Die Kachelbeschriftung MITTIG (im Bestand saß Titel und Erläuterung zentriert über dem Bild) | Die Anordnung „Sinnbild links, Titel daneben, Erläuterung darunter" ist der Stand von **W16b‑E‑3** und vom Anwender abgenommen; sie umzustellen wäre ein Rückgang, kein Anschluss |
| Die 29-px-Höhe der Menüleiste und die 65-px-Höhe der Kopfkästen | Berührungsziele bleiben bei 44 px (Hausregel M2/iL4) — dieselbe Wurzel trägt auf dem iPad ein Menü, das mit dem Finger bedient wird |
| Die Rahmenbreite 1,5 px der Kopfkästen | Ein halber Bildpunkt ist auf einem Bildschirm ohne Skalierung ein unscharfer Strich; der Farbeindruck hängt an der FARBE, und die ist übernommen |
| `Microsoft Sans Serif` (drei Beschriftungen des Bestands) | Die Schriftfamilie steht einmal in `--epos-schrift` (Segoe UI mit Rückfall für iOS). Zwei Familien in einer Maske waren im Bestand ein Versehen, keine Aussage |

---

## 6 — Befunde

| # | Befund | Folge |
|---|---|---|
| **W16b‑B1** | **Drei deutsche Literale standen IM CODE der Startmaske**, nicht in ihren drei `.resx`: der Platzhalter des Klimafeldes (`comboBox_Klima.SetPlaceholder("Bitte zuerst ein Projekt auswählen.")`) und die zwei Statuszeichen ✔/⚠ | Sie bekommen mit `START_KLIMA_PLATZHALTER`, `START_STATUS_OFFEN` und `START_STATUS_KEINS` zum ersten Mal eine englische Fassung |
| **W16b‑B2** | **Die Klimazone des Projektkontexts wird an ZWEI Stellen verschieden gelesen.** Windows nahm die PROJEKTKOPIE (`Tab_Klimaregion.Bezeichner` über `Tab_Projekt.ID_Klimaregion`), `EPOS.iOS/Dienste/IosProjektKontext` nimmt den STAMMNAMEN (`Tab_Klimaregion_STAMM.Name` über dieselbe Id) | `ProjektKontextCtrl` übernimmt den **Windows-Weg wörtlich** (`StartseiteCtrl.ProjektKlimaregion`) — er ist der, den `IProjektKontext.Klimazone` bisher herausgab. Die iOS-Fassung sollte mit iU11 auf denselben Weg gezogen werden; solange sie eine eigene Umsetzung führt, sind es zwei Wahrheiten (offener Punkt W16b‑O‑3) |
| **W16b‑B3** | **`Form_Start.GetKlimaregion(int)` hatte nach K6‑a keinen Aufrufer mehr.** Ihr einziger Leser war `WinFormsNavigation.ProjektDetailZeigen`, und der ist mit `FormMain` gefallen | Sie steht trotzdem als `StartseiteCtrl.KlimaregionName(int)` im Kern: Die Anweisung nennt die vier SQL ausdrücklich, und der Wert wird für die iOS-Angleichung (B2) gebraucht |
| **W16b‑B4** | **Die Kachel „Eigenes Profil" trug im Bestand keinen Statuspunkt** — sie führt in die STAMMDATEN (`TypStammHuelle.ProfilOeffnen`) und nicht in das Projekt | Wörtlich übernommen: 20 der 21 Kacheln zeigen einen Bestand, diese eine nicht |
| **W16b‑B5** | **`Form_Start.btn_Help` ist die Hilfe des FENSTERS, nicht der Seite.** Die Feldkarte weist ihn oberhalb des Reiterwerks aus, und `help_mapping.txt:90` nennt ihn „Programmablauf" | Er bleibt als Schlüssel stehen und wandert mit W16c an das Hauptfenster; die fünf Reiter-Infoknöpfe tragen ihre Schlüssel WÖRTLICH weiter |
| **W16b‑B6** | **`SimulationErgebnisHuelle` benutzte ihr eigenes modales Fenster als BESITZER** für einen Unterdialog und für die `Sprungbruecke` (`_fenster`) | Mit E‑5 gibt es dieses Fenster nicht mehr; der Besitzer kommt als `Func<Form>` herein und ist das Hauptfenster |
| **W16b‑B7** | **`ProjektTransferDialogTests.Schliessen_meldet_ob_ein_Import_gelungen_ist` ist flatterhaft** — im Gesamtlauf einmal rot, einzeln und im Wiederholungslauf grün | Dasselbe Muster wie `28312c1` (Fortschrittsmeldungen aus einem Hintergrundlauf nach dem Lauf). Nicht von dieser Welle verursacht; offener Punkt W16b‑O‑2 |
| **W16b‑B8** | **Die Sollzahl des Stapellaufs geht nicht auf.** Die Anweisung nennt „1 Maske / 2 Designer"; gemessen sind es nach W16b **2 Masken / 3 Designer-Dateien** | Nachgerechnet vom heutigen Stand: 7 Masken − 5 gelöschte Maskendesigner = 2 (`MDIMainForm`, `Form_HelpPopup`); 8 Designer-Dateien − 5 = 3 (die dritte ist `Properties/Resources.Designer.cs`, ohne `InitializeComponent`). Die Sollzahl der Anweisung ist die von **nach W16c**, wenn `MDIMainForm` auf die Hülle zurückgebaut ist |

### Nachsatz zu W16b‑B2 — die Messung vom 04.09.2026 (Entscheid W16b‑O‑3)

Vor der Angleichung an die iOS-Lösung ist **nachgemessen** worden, was die beiden Wege auf
`Referenzlaeufe/Kenndaten_Test.sqlite` überhaupt herausgeben — nur lesend, Skript
`messung_klimazone.py` (Arbeitsstand, nicht im Repo). **Das Ergebnis dreht die Erwartung um:**

| Projekt | `ID_Klimaregion` | `Tab_Klimaregion_STAMM.Name` (iOS-Weg) | `Tab_Klimaregion.Bezeichner` (Windows-Weg) |
|---|---|---|---|
| 1007 Laurentiuskirche | 1007001 | *kein Stammsatz* | `stuttgart` |
| 1008 Heinestr 15 | 1008001 | *kein Stammsatz* | `stuttgart` |
| **1011** | — | — | — (Projekt steht nicht in dieser DB) |
| 1017 WP_PV-Speicher | 1017001 | *kein Stammsatz* | `stuttgart` |
| 1018 BHKW Test München | 1018047 | *kein Stammsatz* | `München` |
| **1021** | — | — | — (Projekt steht nicht in dieser DB) |
| 1023 Wöhler - Test1 | 1020033 | *kein Stammsatz* | `stuttgart` |
| 1024 Wöhler - Test2 | 1020034 | *kein Stammsatz* | `stuttgart` |
| 1030 Referenz BHKW-Kaskade | 1020040 | *kein Stammsatz* | `München` |
| 1039 Mehrgebäude | 1020049 | *kein Stammsatz* | `stuttgart` |
| 1040 zwei Puffer je Kanal | 1020050 | *kein Stammsatz* | `stuttgart` |
| 1041 Prozesswärme mit eigenem Puffer | 1020051 | *kein Stammsatz* | `stuttgart` |
| 1042 Booster-Kette mit Kombi-Speicher | 1020052 | *kein Stammsatz* | `stuttgart` |

**Die beiden Wege lesen nicht dieselbe Id, sondern ZWEI VERSCHIEDENE SCHLÜSSELRÄUME.**
`Tab_Projekt.ID_Klimaregion` trägt die Id der **Projektkopie** (`Tab_Klimaregion.ID`) — so
schreibt es `StartseiteCtrl.KlimaregionSpeichern` ausdrücklich („am Projekt wird die Id der
PROJEKT-Kopie gespeichert, nicht die STAMM-Id"). Der iOS-Weg hält denselben Zahlenwert gegen
`Tab_Klimaregion_STAMM.ID_Klimaregion`. In der Testdatenbank laufen die Stamm-Ids von **1 bis
50** (32 Zeilen), die Kopie-Ids von **1 006 017 bis 1 020 054** (22 Zeilen); die
**Überschneidung der beiden Räume ist 0**.

Daraus folgt für die Messung:

- **11 von 11** vorhandenen Referenzprojekten haben zu ihrer `ID_Klimaregion` **keinen**
  Stammeintrag. Der iOS-Weg gibt für sie **alle** die leere Zeichenkette heraus; ein Vergleich
  „gleich/ungleich" kommt gar nicht erst zustande (0 gleich, 0 ungleich).
- Über **alle 23** Projekte der Datenbank: **22 ohne Stammsatz** zur Id, **1 ohne Projektkopie**
  zur Id. Das eine ist Projekt **19 „Wöhler WP"** mit `ID_Klimaregion = 1` — ein Altbestand ohne
  Projektkopie, dessen Id zufällig auf den Stammsatz 1 (`stuttgart`) trifft. Es ist der einzige
  Satz der ganzen Datenbank, für den der iOS-Weg überhaupt antwortet, und er tut es aus einer
  **Schlüsselraum-Kollision**, nicht aus einer Beziehung.
- Der Kopie-`Bezeichner` hängt am Stamm **über den TEXT**: `Tab_Klimaregion` führt gar keine
  Stammspalte (`ID`, `ID_Projekt`, `Bezeichner`, `Longitude`, `Latitude`, `Details`,
  `Klimazone_DIN4710`). Die Gegenprobe „Bezeichner → `Tab_Klimaregion_STAMM.Name`" trifft für
  **alle elf** Projekte und liefert **denselben Text** (`stuttgart` → `stuttgart`, `München` →
  `München`). Ein „Stammname", der sich von der Projektkopie unterscheidet, existiert im
  Bestand also nicht.
- **Der Klimadaten-Import legt keine Kopie-Waisen an.** `KlimaImportAblauf` schreibt
  ausschließlich in die drei STAMM-Tabellen (`Tab_Klimaregion_STAMM`, `Tab_Klimadaten_STAMM`,
  `Tab_Solar_STAMM`); die Projektkopie entsteht erst beim Speichern der Region am Projekt
  (`KlimaregionStammCtrl.CopyRegionToProjekt`). Schema-Schritt 62 `KlimaWaisenBereinigung`
  betrifft eine andere Waise — Datenblockzeilen ohne **Kopfsatz im Stamm** —, nicht die
  Projektkopie.

**Folge für die Umsetzung — die Messung hat den Entscheid geschärft.** Der Entscheid lautete
„nehme iOS-Lösung", weil zwei Fassungen desselben Wertes nebeneinander standen. Die Messung zeigt,
dass die iOS-Fassung **kein zweiter Weg war, sondern ein Fehler**: Sie hielt die Id der
Projektkopie gegen den Stammschlüssel, antwortete deshalb für jedes Projekt des Bestands leer und
hätte nur dort überhaupt geantwortet, wo die beiden Schlüsselräume zufällig kollidieren. Ein
„Stamm zuerst, Kopie als Rückfall" hätte diesen Fehler als Sonderfall konserviert.

**Umgesetzt ist daher (Anwender, 04.09.2026):** `StartseiteCtrl.ProjektKlimazone` liest
**ausschließlich die Projektkopie** — wörtlich der bisherige Windows-Weg (`Form_Start:379-400`),
parametriert —, **ohne Stammabfrage und ohne Rückfall**. Die Stammabfrage `:356` (zuletzt
`KlimaregionName(int)`) ist ersatzlos gefallen; sie hatte seit K6‑a keinen Aufrufer und stand nur
noch für diese Angleichung im Kern (Befund W16b‑B3). **Die Vereinheitlichung bleibt:**
`IosProjektKontext` führt keine eigene Abfrage mehr, sondern reicht an `ProjektKontextCtrl` durch —
eine Klasse, eine Antwort. Der offene Punkt **W16b‑O‑6** (Kollisionsgefahr in einer frischen
Datenbank) ist damit **gegenstandslos**: Es gibt keinen Stammzweig mehr, der kollidieren könnte.

---

## 7 — Texte

**78 neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx` **und**
`Resource.en-US.resx` — **maschinell** aus `Form_Start.resx` (neutral = deutsch),
`Form_Start.de-DE.resx` und `Form_Start.en-US.resx` übernommen, nicht abgetippt
(dieselbe Regel wie R‑W16‑8 für die Menütabelle; das Skript steht im Protokoll der
Welle als `w16b_texte.py`):

| Gruppe | Zahl | Bemerkung |
|---|---|---|
| `START_REITER_*` | 6 | die sechs Reitertitel; das WinForms-`&&` von „Berichte && Kosten" ist zu einem `&` geworden (in Razor gibt es keine Tastenkürzel-Verdopplung) |
| `START_K_*` | 42 | Titel und Beschreibung der 20 beschrifteten Kacheln, davon die sechs Kartentexte aus `de-DE` (dort allein gepflegt, Befund W16‑B21) |
| `START_P_*`, `START_W_*`, `START_S_*`, `START_E_*`, `START_SIM_*` | 21 | Kopfzeilen, Erläuterungen, Hinweis- und Tippkasten, die vier Zeilen der Zusammenfassung, die zwei Auswahlknöpfe |
| `START_GATTUNG`, `START_LBL_*`, `START_KLIMA_*`, `START_BTN_*`, `START_STATUS_*` | 9 | Kopfband und Fußleiste |

Die **17 vorhandenen `MyResource`-Schlüssel** von `Form_Start` sind wörtlich
übernommen: `Text_Select`, `Text_Hinweis`, `Text_Projekt`, `Text_Geoeffnet`, die
sieben `Text_Form_Start_*` und die fünf `SIM_*`.

---

## 8 — Gate

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 6 Warnungen | **0 / 6** (Vollneubau) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 923 + neue | **3 968** (KiKern 450, SpeicherEngine 337, EPOS.Kern.Tests **1 021**, EPOS.UI.Tests **2 160**) — **+45** |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | gleich | **grün** |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 123 | **121**, auch unter `en_US` (T1 gestrichen, die Wurzel-Theorie hat einen Fall weniger) |
| Stapellauf `--alle WindowsFormsApplication1` | **1 Maske / 2 Designer** | **2 Masken / 3 Designer**, 1 lokalisiert, **2 ja / 0 nein / 0 verwaist / 0 unklar** — Abweichung erklärt in Befund W16b‑B8 |
| `SqlDialektPruefer` | 0 Fundstellen | **0 von 1 200** (−34 mit den gelöschten Dateien); **`WindowsFormsApplication1` hat null Inline-SQL** |
| `ChartProben` | 32 unverändert | **32 Bilder, 0 Verstöße** |
| Referenzlauf 1030 / 1007 / 1017 gegen `2026-08-30_B3-Kaskade` | byte-gleich | **PASS, 815 043 Werte** (1007: 324 219, 1017: 254 154, 1030: 236 670); `diff -rq` **byte-gleich in allen drei** |
| **Referenzlauf mit Projektwechsel** (§ 16.3) | beide gegen die Basis | **PASS**, siehe unten |
| Wächter `Program.*` im Kern und in den Kernkandidaten | leer | **leer** |
| Wächter `System.Windows.Forms`/`System.Drawing`/`MessageBox.`/`Registry.`/`ProtectedData`/`OleDb` im Kern | leer | **leer** |
| `git grep` auf die gefallenen Klassen | nur Kommentare, Protokolle und das eingefrorene Prüfmuster | erfüllt |

**Nach dem Merge von `origin/ios_migration`** (`d4a7632` — der einundzwanzigste
iOS-Lauf auf dem Stand nach W16a, die Nachweisliste und ein neuer Abschnitt
„Compact instructions" in der Wurzel-`CLAUDE.md`) ist das ganze Gate ein zweites Mal
gelaufen: Build **0 / 6** (Vollneubau), **3 968** grün und ebenso unter `en_US`,
Formularkarte **121**, Stapellauf **2 / 3** mit 2 / 0 / 0 / 0, SQL **0 von 1 200**,
ChartProben **32**, Referenzlauf **byte-gleich in allen drei Projekten** (815 043
Werte), beide Wächter leer. **Der Merge lief ohne Konflikt** — die andere Seite hat
nur Konzept- und Nachweisdateien angefasst, keine Quelldatei.

### R‑W16‑4 — der Projektwechsel

Der Nachweis läuft **zweigleisig**, weil der Referenzlauf allein die Frage nicht
beantwortet: Er RECHNET einen bestehenden Projektstand nach, er wechselt kein Projekt.

1. **Der Referenzlauf in beiden Reihenfolgen**: `--projekte 1030,1007` und
   `--projekte 1007,1030`, je in EINEM Prozess. Beide Ergebnisse sind byte-gleich zur
   Basis **und untereinander** (`diff -rq`, je 29 bzw. 22 Dateien). Ein Kontext, der
   zwischen zwei Läufen hängen bliebe, machte die Reihenfolge sichtbar.
2. **Der Kern-Fall** `ProjektKontextCtrlTests.Ein_Projektwechsel_schreibt_in_kein_falsches_Projekt`:
   1030 öffnen, auf 1007 wechseln, zurück auf 1030 — danach stehen **beide** Projekte
   inhaltlich unverändert (Zählstand der sieben Zuordnungstabellen, Bitmaske des Kerns,
   Anlagenbezeichner je Typ), und `Tab_Applikation` folgt jedem Schritt. Auf einer
   EIGENEN Arbeitskopie, mit `Dienste.Projekt` für die Dauer des Falls auf dem
   Kern-Controller.

---

## 9 — Windows-Abnahme

| # | Was | Erwartung |
|---|---|---|
| 1 | Programm starten | Die Startseite steht in der Hauptfläche: Kopfband mit dem Klimakasten **links** und dem Projektkasten **rechts** — Projektauswahl (Platzhalter „Bitte auswählen!") mit rotem ⚠ **davor**, Klimafeld mit dem Globussinnbild davor; darunter das Hinweisbanner „Bitte zuerst ein Projekt auswählen! Projekt öffnen oder zuletzt geöffnet"; sechs Reiter, davon fünf gesperrt. **Die Zeile „Energieplanungs-Software" steht NICHT mehr in der Startseite** (W16b‑E‑4) — sie steht einmal, im Kopfband des Hauptfensters |
| 2 | **Alle 21 Kacheln durchklicken** | Reiter 1 fünf, Reiter 2 vier, Reiter 3 drei, Reiter 4 sieben, Reiter 5 ein Knopf und eine Kachel. Jede führt in denselben Dialog wie vorher |
| 2a | **Die 21 Sinnbilder** (W16b‑E‑3) | JEDE Kachel zeigt links ihr Sinnbild, den Titel daneben und die Erläuterung darunter — dieselben Bilder wie im Vorläufer. Keine Kachel bleibt leer, kein Bild ist verrutscht (der Ausschnitt ist CSS): auf Reiter 1 die fünf Logo-Quadrate der Aktionskarten, auf Reiter 4 sieben Erzeuger-Sinnbilder, davon **Stromspeicher und Pufferspeicher aus den zwei flachen JPG**. Auf Reiter 5 trägt auch der Knopf „Simulation Konfiguration…" ein (kleines) Sinnbild |
| 2b | **Drei Kacheln je Reihe** | Das Raster legt bei voller Fensterbreite drei Kacheln nebeneinander (404 px, die Kachelbreite des Designers); auf einem schmalen Fenster bricht es um, ohne waagerecht überzulaufen |
| 2c | **Die Farbgebung** (W16b‑E‑5) | Die Reiterleiste steht auf einem **grauen Grund**, der gewählte Reiter ist ein **gefüllter blauer Block mit weißer Schrift** (#005aa0), die übrigen tragen schwarze Schrift, die gesperrten graue. Darunter hängt das Reiterblatt als **weißer Körper mit einem dünnen blaugrauen Rahmen**, ohne Fuge zur Leiste |
| 2d | **Die zwei Kopfkästen** (W16b‑E‑5) | Ihr Rahmen ist ein **kühles Blaugrau** (#b4becd) mit 8 px Rundung — nicht mehr das warme Beigegrau. Die zwei Beschriftungen („Klimaregion auswählen:", „Projekt:") stehen **größer als die Dialogschrift** (16 px), das Statuszeichen ⚠/✔ ist das größte Zeichen der Zeile |
| 2e | **Die Erläuterungen** (W16b‑E‑5) | Jede Erläuterung — unter der Reiterüberschrift **und** unter jedem Kacheltitel — steht in **halbfettem Grau** (DimGray) und in derselben Größe wie die Überschriftenzeile; sie sieht nicht mehr wie Fließtext aus |
| 2f | **Die Knöpfe** (W16b‑E‑5) | „◀ Zurück", „Weiter ▶" und „Simulation Konfiguration…" sind **hellgrau gefüllt** wie im Bestand. Ein gesperrter Knopf („Zurück" auf Reiter 1) ist an der Fläche als gesperrt zu erkennen, nicht nur an der Schrift |
| 2g | **Reiter „Simulation": der Zusammenfassungskasten** (W16b‑E‑5) | Er steht auf einer **eigenen, fast weißen Fläche** (#f9fafc) mit demselben blaugrauen Rahmen; die vier Beschriftungen sind grau, die vier Werte **blau** (#005aa0) |
| 3 | Ein Projekt öffnen („Zuletzt geöffnet") | Grünes ✔, Klimaregion gefüllt, alle sechs Reiter frei, das Hinweisbanner verschwindet, ein Erfolgsbanner „Projekt … geöffnet!" steht **drei Sekunden** |
| 4 | **Projektwechsel im Kopfband** | Auswahlfeld auf eine andere Variante: Statuspunkte, Klimaregion, Zusammenfassung und der Reiter „Berichte & Kosten" folgen; „zuletzt geöffnet" ändert sich dabei NICHT |
| 5 | Reiter „Energieerzeuger" | Sieben Kacheln mit Statuspunkt; unter der Solarthermiekachel die zwei Auswahlknöpfe. „Profil" öffnet den Kollektordialog, „Ganglinie" den Gangliniendialog |
| 6 | Klimaregion wählen und speichern | EIN grünes Banner „Klimaregion gespeichert."; ohne Projekt oder ohne Region ein rotes mit dem jeweiligen Satz |
| 7 | Reiter „Simulation" ohne Klimaregion | Die Seite springt auf Reiter 1 zurück und zeigt „Die Klimaregion ist nicht gesetzt! …" |
| 8 | Reiter „Simulation" mit Klimaregion | Projektzusammenfassung mit Name, Wärmebedarf, Strombedarf und den gewählten Technologien |
| 9 | „Simulation Konfiguration…" | Die Konfiguration **löst die Startseite ab** (kein zweites Fenster); „Schließen" bringt die Startseite zurück |
| 10 | Kachel „Simulation" | Das Ergebnis erscheint als **Überlagerung** über der Startseite; der Lauf startet von selbst |
| 11 | Menü „Projekt → Öffnen…" und „→ zuletzt geöffnet" | Das Projekt wird AKTIV (kein Detailformular mehr) |
| 12 | Menü „Projekte → Varianten und Bericht…" | Der Reiter „Berichte & Kosten" kommt nach vorn, Seite „Übersicht" |
| 13 | Assistent → „Projekt öffnen" | Der Assistent schließt, das Projekt ist aktiv, die Startseite meldet es drei Sekunden lang |
| 14 | Sprachwechsel auf Englisch | Alle 78 neuen Texte englisch, einschließlich der drei bisherigen Literale (B1) |
| 15 | **DPI 100 / 125 / 150 %** | Die Startseite sitzt in der DpiUnaware-`MDIMainForm` und wird ab 125 % **bitmapskaliert** — das ist der bekannte Stand (Risiko R‑W16‑2). iF21 kommt mit W16c; die Abnahme hier hält fest, WIE unscharf es ist |
| 16 | Ein Projekt löschen, das gerade offen ist | Statuszeichen zurück auf ⚠, Auswahlfeld auf den Platzhalter, Klimafeld leer, Reiter 2–6 wieder gesperrt |

---

## 10 — Was W16c von hier erbt

| Was | Zustand |
|---|---|
| **`MDIMainForm`** | Angefasst wurden GENAU die Stellen, die W16b brauchte: `MDIMainForm_Load` hängt eine `BlazorSeite<Startseite>` statt `new Form_Start()` ein (zwei neue Felder `_startseite`/`_startbild`), `MenuItem_Neu_Click` und `MenuItem_ProjektBearbeiten_Click` rufen `Program.projektkontext.Setzen`, `MenuItem_AlsVariante_Click` liest `Dienste.Projekt`, `MenuItem_VariantenBericht_Click` ruft `StartseiteHuelle.Aktuelle`. Alles Übrige — die 45 Menüpunkte, die acht `Init*`, das Kopfband, `KeyPreview`/F1, der Sprachwechsel — ist **unberührt** |
| **Menüpunkte** | „Projektdetail" gibt es nicht mehr; „Öffnen…" und „zuletzt geöffnet" setzen das Projekt aktiv (A‑9). `MenueCtrl` führt seither 24 Methoden statt 26 |
| **Der „ja"-Zeuge** | steht unverändert an `MDIMainForm` (der Wurzel selbst, Pfadlänge 1). **W16c muss ihn umhängen**, sobald `MDIMainForm` auf die Hülle zurückgebaut ist |
| **Der Maskenschlüssel-Zeuge** | **gestrichen** (W16b‑O‑1). Nach dieser Teilwelle gibt es keinen `Masken.*`-Schlüssel mit einer WinForms-Maske dahinter. W16c kann ihn mit E‑9 am Prüfmuster zurückholen — dafür bräuchte `Pruefmuster/` einen Auszug der Sprungtabelle |
| **Die Typzeugen** | **alle elf** hängen jetzt am Prüfmuster (E‑9); `Pruefmuster/Hauptformular/Form_Start.Designer.cs` trägt die letzten fünf |
| **`Erreichbarkeit.Wurzelmasken`** | führt nur noch `MDIMainForm`. `Form_HelpPopup` hängt weiter daran und meldet „ja" — **Befund W16‑B3 ist damit erledigt**, ohne dass `Program.Main` eine dritte Wurzel werden musste |
| **`AppWurzel`** | unberührt. `IProjektQuelle.Startkacheln(int)` steht mit Standardumsetzung (K6); `Seitenschluessel.STARTSEITE` und der Zweig in `AppWurzel` sind K7 und gehören zu W16c |
| **`Masken`** | führt noch 24 Schlüssel; `ProjektDetail` und `Ansichten.ProjektDetail` sind gefallen |
| **Die Schwellen der Formularkarte** | stehen auf den Endwerten von W16b (2 Masken, 5 Designer über die Repowurzel, 1 lokalisiert, 2 erreichbar). W16c setzt sie nach E‑8a auf N1/N2 |
| **`Program.startfrm`** | **gibt es nicht mehr.** `Program` hält noch `mdifrm`, `projektkontext`, `menuectrl` und `wizardctrl` |

---

## 11 — Offene Punkte

| # | Punkt |
|---|---|
| **W16b‑O‑1** | **Der Maskenschlüssel-Zeuge ist gestrichen** (`ErreichbarkeitTests.DieSprungtabelleLoestDieMaskenschluesselAuf`). Er prüfte, dass der Graph einen `Masken.*`-Schlüssel bis zur Maske auflöst; nach E‑7 gibt es keine solche Kette mehr. Rückholbar in W16c über einen Auszug der Sprungtabelle im Prüfmuster |
| **W16b‑O‑2** | **`ProjektTransferDialogTests.Schliessen_meldet_ob_ein_Import_gelungen_ist` ist flatterhaft** (Befund W16b‑B7) — nicht von dieser Welle verursacht, aber im Gesamtlauf einmal rot |
| **W16b‑O‑3** | ~~**`IosProjektKontext` liest die Klimazone anders als der Kern** (Befund W16b‑B2). Er sollte auf `ProjektKontextCtrl` gezogen werden — dieselbe Klasse, dieselbe Antwort; das ist iU11~~ — **Anwenderentscheid 04.09.2026 „iOS-Lösung"; die Messung zeigte, dass die iOS-Abfrage den falschen Schlüsselraum las — umgesetzt als EINE Wahrheit im Kern (Projektkopie), iOS läuft über den Kern** (Commit `b94dbb5`; Vorstufen `f9ac47e` Messung, `32d5f79` Kern, `140309b` iOS, `8819e5d` Doku). `StartseiteCtrl.ProjektKlimazone` löst `ProjektKlimaregion` ab und liest **nur** `Tab_Klimaregion.Bezeichner` über `Tab_Projekt.ID_Klimaregion` — parametriert, wörtlich `Form_Start:379-400`, **ohne Stammabfrage und ohne Rückfall**. Die Stammabfrage `:356` (zuletzt `KlimaregionName(int)`, Befund W16b‑B3) ist **ersatzlos gefallen**: Sie hatte keinen Aufrufer und stand nur für diese Angleichung im Kern. `IosProjektKontext` ist eine **dünne Weiterleitung** auf `ProjektKontextCtrl` (nur das `try/catch` um `Uebernehmen` bleibt iOS-eigen). Nachweis N7 von 12 auf **15 Fälle** |
| **W16b‑O‑4** | **`Form_Start.btn_Help` (die Fensterhilfe) hat noch keinen neuen Ort.** Der Schlüssel steht in `help_mapping.txt`; W16c hängt ihn an das Hauptfenster (Befund W16b‑B5) |
| **W16b‑O‑5** | **Der Assistent ist weiterhin modal** (W16a‑E‑1 / W16a‑O‑3). Die Startseite kann seit E‑5 Ansichten wechseln; der Umbau bräuchte dieselbe Behandlung der zwei Aufrufer wie bei den Simulationsseiten |
| **W16b‑O‑6** | ~~**Der Stammzweig aus W16b‑O‑3 kann in einer FRISCHEN Datenbank die falsche Region nennen.** `Tab_Klimaregion.ID` ist ein `INTEGER PRIMARY KEY AUTOINCREMENT` und beginnt in einem Neustand bei 1 — genau dort, wo auch die Stamm-Ids liegen~~ — **gegenstandslos** (04.09.2026, mit `b94dbb5`): **Es gibt keinen Stammzweig mehr.** `ProjektKlimazone` liest ausschließlich die Projektkopie, und die Stammabfrage ist aus dem Kern gefallen; eine Kollision der beiden Schlüsselräume kann die Anzeige damit nicht mehr erreichen. Der Zeuge dafür steht als N7-Fall `Ohne_Projektkopie_ist_die_Klimazone_leer` (Projekt 19 „Wöhler WP" trifft mit `ID_Klimaregion = 1` einen Stammsatz und meldet trotzdem `""`); kehrt die Stammabfrage zurück, wird er rot. **Bestehen bleibt der Schemabefund dahinter**: `Tab_Klimaregion` führt keine Stammspalte, sondern hängt über den TEXT am Stamm — entgegen der Regel „bei neuen Beziehungen IDs verwenden". Das zu ändern ist ein Migrationsschritt und gehört nicht in diese Welle |
