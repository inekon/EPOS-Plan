# iU9 Welle 10a — Simulationskonfiguration I: die sieben Dialoge — Portprotokoll

> Umsetzung 03.09.2026 im Arbeitsbaum `agent-aa75b5c4740e1c9bd`, Basis
> `04fc474` (nach dem Merge der Welle 9). Vorbild in Aufbau und Tiefe: die
> Protokolle der Wellen 8 und 9 im selben Ordner. Regeln: Arbeitsanweisung
> `iU9_W10a_Arbeitsanweisung.md` Abschnitt F, Vermessung
> `iU9_W10_Vermessung.md` §1–§7, `EPOS.UI/CLAUDE.md`, `EPOS.Kern/CLAUDE.md`.

---

## 1. Auftrag und Ergebnis

**Sieben WinForms-Masken** — alle Dialoge, die `Form_Simulation_Config` öffnet —
sind **sieben** Razor-Komponenten in `EPOS.UI/Dialoge/Simulation/`; ihre
WinForms-Fassungen sind im selben Commit gelöscht (Regel M1). Zusammen **7 803
Zeilen** Oberflächencode, **30 `MessageBox`** und **69 Kartenzeilen** — dazu das
Steuerelement `KlimazonenKarte` (326 Z.), das mit seiner einzigen Maske fällt.

**`Form_Simulation_Config` selbst bleibt WinForms** und ist mit **W10b** an der
Reihe; sie ist nach dieser Welle die letzte Designer-Maske im Ordner
`Views/Simulation`, die noch Dialoge öffnet.

| Komponente | ersetzt | Zeilen | Hülle |
|---|---|---|---|
| `BetriebsmodusDialog` | `Form_Betriebsmodus` (144) | 144 | `Views/Simulation/BetriebsmodusHuelle.cs` |
| `KlimazonenkarteDialog` | `Form_Klimazonenkarte` (96) + `KlimazonenKarte` (326) | 422 | keine — Überlagerung im Erdreichdialog |
| `QuelleErdreichDialog` | `Form_QuelleErdreich` (1 273) | 1 273 | `Views/Simulation/QuelleErdreichHuelle.cs` |
| `PufferSpProjektDialog` | `Form_PufferSp_Projekt` (2 067) | 2 067 | `Views/Pufferspeicher/PufferSpProjektHuelle.cs` |
| `QuellePufferspeicherDialog` | `Form_QuellePufferspeicher` (1 089) | 1 089 | `Views/Simulation/QuellePufferspeicherHuelle.cs` |
| `QuellprofilDialog` | `Form_Quellprofil` (1 084) | 1 084 | `Views/Simulation/QuellprofilHuelle.cs` |
| `WaermesenkeDialog` | `Form_Waermesenke` (2 050) | 2 050 | `Views/Simulation/WaermesenkeHuelle.cs` |

**Neu im Kern**: `WaermesenkeClass.SenkeAnzeige`/`SENKE_LEER`,
`ProjektPuffer.NutzbareKapazitaetKWh`, `PufferSpStammCtrl.Katalogzeilen`,
`VDI4640Pruefung.Sondenmeter`/`Volllaststunden`,
`ErdreichAuswertung.ErdreichLaufErgebnis`/`ErgebnisZuordnen`, die drei
Serialisierungswege in `QuellprofilCtrl`, das Renderer-Bild
`ChartRenderer.Jahresgang` und die erzeugte Datei
`Allgemein/Simulation/KlimazonenPfade.cs`.
**Ein neuer Baustein** (`Bildkarte`), **ein neuer Kleindialog** (`WertAbfrage`),
**ein neues Renderer-Bild**, **ein neues Sprungziel**.

### Commits

| Hash | Betreff |
|---|---|
| `352f349` | iU9-W10a.0a: `SenkeAnzeige` und `IstPufferZiel` in den Kern |
| `cbfccb1` | iU9-W10a.0b: Rechnung, SQL und Zerlegung der sieben Masken in den Kern |
| `7aae643` | iU9-W10a.0c: Sprungziel `PufferSpAdminNurLesen` |
| `ef513e6` | iU9-W10a.0d: `ChartRenderer.Jahresgang` — das zweireihige Jahresbild |
| `53240c4` | iU9-W10a.0e: Baustein `Bildkarte`, `KlimazonenPfade` und das Kartenbild |
| `6fe8656` | iU9-W10a.0f: `WertAbfrage` — die kleine Zahlenabfrage als Überlagerung |
| `18ac6e1` | iU9-W10a.1: `BetriebsmodusDialog`, `Form_Betriebsmodus` gelöscht |
| `b34a6d3` | iU9-W10a.2: `KlimazonenkarteDialog` auf dem Baustein `Bildkarte` |
| `033d0b9` | iU9-W10a.3: `QuelleErdreichDialog` — drei WinForms-Dateien gelöscht |
| `781e463` | iU9-W10a.4: `PufferSpProjektDialog` — die größte Maske der Welle gelöscht |
| `a6d15e5` | iU9-W10a.5: `QuellePufferspeicherDialog` mit der Pufferverwaltung als Überlagerung |
| `82aad99` | iU9-W10a.6: `QuellprofilDialog` mit virtualisiertem Werteraster |
| `97ff674` | iU9-W10a.7: `WaermesenkeDialog` — die letzte Maske der Welle gelöscht |
| `630a56b` | iU9-W10a.8/9: Ressourcen abgeschlossen, Formularkarte auf den Stand nach W10a |

**Abweichung von der Schrittfolge der Arbeitsanweisung.** W10a.2 legt die
Komponente `KlimazonenkarteDialog` samt Tests an, **löscht** aber
`Form_Klimazonenkarte` und `KlimazonenKarte` noch nicht — das geschieht in
W10a.3 zusammen mit `Form_QuelleErdreich`. Grund: Die Karte hat genau einen
Aufrufer, und der ist der Erdreichdialog; sie wird dort zur **Überlagerung**.
Eine eigene Hülle mit Fensterweg hätte einen Schritt lang existiert und wäre im
nächsten wieder verschwunden. Regel M1 („im selben Commit löschen") ist damit
gewahrt: Der Commit, der die WinForms-Fassung entfernt, ist derjenige, der ihren
Ersatz an die Aufrufstelle hängt.

---

## 2. Die beiden Vorabproben

Die Arbeitsanweisung verlangt zwei Messungen **vor** dem Bau, weil sie die
Bauweise bestimmen.

### R‑W10a‑2 — läuft der Simulationslauf in einem fremden Faden?

```
EPOS.Kern.Tests/SimulationslaufAusFremdemFadenTests.cs
  Simulationslauf_laeuft_in_Task_Run_ohne_Fehler          598 ms, gruen
  Simulationslauf_zweimal_hintereinander_bleibt_gruen     598 ms, gruen
→ SimulationRunner.Simuliere(1030, out fehler) == true, fehler leer
```

**Antwort: ja.** `SqliteDatenzugriff` öffnet je Aufruf eine eigene Verbindung,
und im Rechenweg steht kein `[ThreadStatic]`. **Folge für den Bau:** Der
Erdreichdialog stößt den Lauf **asynchron** an (`await Simulieren(...)` auf
einem `Task.Run` in der Hülle) und zeigt für seine Dauer einen sichtbaren
Wartezustand (Abweichung A‑5). Der Rückweg — synchron nach `await Task.Yield()`
— wird nicht gebraucht.

### R‑W10a‑3 — wie groß ist das Kartenbild?

```
Zonenkarte_Klimazonen.png   3 390 x 3 510 Punkte, 1 356 742 Bytes = 1,29 MiB
Schwelle der Arbeitsanweisung: 2 MiB
```

**Unter der Schwelle — nicht verkleinert.** Das Bild geht unverändert
(`R100`, byte-gleich) von `WindowsFormsApplication1/Ressourcen/` nach
`EPOS.UI/wwwroot/bilder/` und wird von der WebView als statische Datei geladen
statt als eingebettete Ressource. Maße vorher = Maße nachher.

---

## 3. Bauweise

### 3.1 Der Kern zuerst — sechs Vorabschritte

Fünf der sieben Masken trugen Rechnung, SQL oder Zerlegung im Formular. Die
Welle beginnt deshalb mit sechs Schritten, die **keine** Oberfläche anfassen:

| Schritt | Was in den Kern ging | Befund |
|---|---|---|
| W10a.0a | `WaermesenkeClass.SenkeAnzeige` + `SENKE_LEER`; `IstPufferZiel` zusammengeführt | W10‑B22, W10‑B23 |
| W10a.0b | `ProjektPuffer.NutzbareKapazitaetKWh`, `PufferSpStammCtrl.Katalogzeilen`, `VDI4640Pruefung.Sondenmeter`/`Volllaststunden`, `ErdreichAuswertung.ErdreichLaufErgebnis`/`ErgebnisZuordnen`, `QuellprofilCtrl.MonatswerteParsen`/`MonatswerteText`/`WochenwerteParsen` | W10‑B8, B12, B21, B27 |
| W10a.0c | Sprungziel `PufferSpAdminNurLesen` | W10‑B28 |
| W10a.0d | `ChartRenderer.Jahresgang` (1 304 × 440, zwei Reihen, Monate 0…12, vorzeichenfähige y‑Achse) | — |
| W10a.0e | Baustein `Bildkarte`, erzeugte `KlimazonenPfade.cs`, das Kartenbild nach `EPOS.UI/wwwroot/bilder/` | W10‑B3, B5 |
| W10a.0f | `WertAbfrage` — die kleine Zahlenabfrage als Überlagerung | W10‑B18 |

**`SenkeAnzeige` musste zuerst.** Sie war eine **statische** Methode auf einem
Formular mit **drei fremden Aufrufern** (`Uebersicht.cs`:503/509,
`SchemaModell.cs`:577). Solange sie dort stand, hätte das Löschen der Maske drei
weitere Dateien mitgerissen. Jetzt steht sie in `WaermesenkeClass` — dort, wo
`IstPufferZiel` und `KurzformZuZiel` schon standen.

### 3.2 `KlimazonenPfade` — aus einer SVG wird eine `.cs`

Der Vorläufer las die eingebettete `Zonenkarte_Klimazonen.svg` **zur Laufzeit**
mit einem Regex-Parser und einer hart kodierten Füllfarbe `#15181C`
(`KlimazonenKarte.cs`:123, Befund W10‑B5). Das ist in `EPOS.UI` nicht nachbaubar:
Eine Razor-Bibliothek hat keine eingebetteten Ressourcen der Windows-Anwendung,
und ein Regex-Parser im Browser wäre dieselbe Zerbrechlichkeit an einer neuen
Stelle.

Stattdessen erzeugt `Werkzeuge/KlimazonenPfade/erzeugen.py` **einmalig** die
Datei `EPOS.Kern/Allgemein/Simulation/KlimazonenPfade.cs` (406 915 Bytes):
15 Zonen, `VIEWBOX_BREITE = 1303.65`, `VIEWBOX_HOEHE = 1349.50`, dazu
`Pfad(int zone)` und `Alle()`. Der Generator bleibt im Baum, damit eine neue
Karte denselben Weg nehmen kann; das Ergebnis ist Quelltext und damit prüfbar,
vergleichbar und plattformfrei.

### 3.3 Eine Maske, drei Rollen — `PufferSpProjektDialog`

Die Pufferverwaltung hatte im Bestand **drei** Aufrufer
(`QuellePufferspeicher.cs`:968, `Uebersicht.cs`:342, `Waermesenke.cs`:1790).
In der Razor-Fassung ist sie **einmal** gebaut und erscheint

* als eigenes Fenster (aus der Simulationskonfiguration, über die Hülle),
* als **Überlagerung** im Quellendialog (W10a.5),
* als **Überlagerung** im Senkendialog (W10a.7).

Alle drei bekommen denselben `PufferSpProjektDienste`-Satz mit **16 Delegaten**;
die Hülle baut ihn einmal. Zwei WebViews entstehen dabei nie (Risiko R2).

### 3.4 Der Simulationslauf verlässt den Oberflächenfaden

`Form_QuelleErdreich.btnSimulation_Click`:1039 startete einen **vollständigen
Jahreslauf synchron im UI-Faden** (Befund W10‑B9) — die Maske stand für die
Dauer. In der WebView wäre daraus ein eingefrorener Dialog geworden. Die Hülle
startet ihn jetzt in `Task.Run` (Probe R‑W10a‑2), die Komponente zeigt für seine
Dauer eine Wartefläche und sperrt den Knopf. Der Rest ist wörtlich: Die
**Prüfung** rechnet weiter mit den **angezeigten** Eingaben, der **Lauf** mit dem
Datenbankstand (Befund W10‑B10, bewusst nicht angeglichen).

### 3.5 Das Klassen-Set ist die führende Wahrheit

Im Vorläufer sagten **zwei** Bedienelemente dasselbe: die drei Häkchen
Heizung/Brauchwasser/Prozess **und** die Klappliste „Verwendung". Weil beide
bedienbar waren, musste `_klassenSetSpiegelt` sie gegenseitig nachziehen. In der
Razor-Fassung steht die Mehrfachauswahl allein, und der abgeleitete Altwert
erscheint darunter als **Herleitungszeile** (`PSP_HERLEITUNG_VERWENDUNG`). Das
gegenseitige Nachziehen entfällt ersatzlos; die Pflichtprüfung sitzt weiterhin im
„Übernehmen", nicht im Klicken — wer von {Heizung} auf {Brauchwasser} umstellt,
muss durch das leere Set hindurch.

### 3.6 8 760 Zeilen virtualisiert

Das Werteraster des Quellprofils hing an einer `DataTable` mit 8 760 Zeilen
(Befund W10‑B20). In Blazor steht dort ein `QuickGrid` über `Virtualize`; die
Tests laufen deshalb mit `JSRuntimeMode.Loose`. Der **Altweg-Reiter**
„Wochenwerte" (Befund W10‑B17) bleibt erhalten — 24 Stundenfelder je Wochentag,
nur lesend, sichtbar nur bei vorhandenem Wochengang, mit einer Herleitungszeile,
die sagt, dass er nicht mehr wirkt.

---

## 4. Feldkarten-Abgleich

Die **fünf** Karten der Masken mit Designer wurden vor Wellenbeginn neu gezogen
(Stand nach W9). `Form_Quellprofil` und `Form_Waermesenke` haben **keinen
Designer** (Befund W10‑B38) — sie bauen ihre Oberfläche im Quelltext auf; für sie
ist der Abgleich der Quelltext selbst.

| Maske | Karte | Komponente | Anmerkung |
|---|---|---|---|
| Form_Betriebsmodus | 9 Zeilen, 4 Label, 3 RadioButton, 2 Button, 520 × 300 | 1 Optionsgruppe mit 3 Einträgen **und 3 Beschreibungen**, SpeichernLeiste, InfoKnopf | +1 = der Hilfeeinstieg (A‑1). Die drei Erläuterungstexte werden `Beschreibungen` statt eigener Label |
| Form_Klimazonenkarte | 4 Zeilen, 2 Button, 1 `KlimazonenKarte`, 1 Label, 700 × 760 | `Bildkarte` (15 Flächen), Statuszeile, SpeichernLeiste, InfoKnopf | +1 = A‑2. Das eine Steuerelement wird der Baustein |
| Form_QuelleErdreich | 20 Zeilen, 14 Label, 5 TextBox, 4 Button, 3 GroupBox, 2 ComboBox, 2 RadioButton, 700 × 748 | 2 Klapplisten, 6 Zahlenfelder (**je Zweig eigene**, A‑4), 1 Optionsgruppe, 3 Gruppen, Jahresgangbild, Prüfblock | 5 TextBox → 6 Felder: `_tbLaenge`/`_tbTiefe` sind je Zweig getrennt (A‑4) |
| Form_QuellePufferspeicher | 14 Zeilen, 9 Label, 3 Button, 3 TextBox, 1 CheckBox, 1 GroupBox, 1 ListBox, 620 × 508 (+2 Laufzeitblöcke) | Zeilenwahl, 3 Zahlenfelder, 1 Schalter, **2 Warnbanner** (B16), Anschlusshöhe und Temperaturbezug im Markup | die 11 Laufzeitfelder (B14) stehen jetzt im Markup; das eine Feld ohne Beschriftung bekommt eine |
| Form_PufferSp_Projekt | 22 Zeilen, 15 Label, 9 TextBox, 5 Button, 3 ComboBox, 3 GroupBox, 1 ListBox, 1 ListView, 700 × 662 (+20 Laufzeitfelder) | 2 Raster, Mehrfachauswahl (Klassen-Set), 3 Gruppen, 9 Zahlenfelder, 2 Rückfragen, SpeichernLeiste **nur „Schließen"** (B29) | die Klappliste „Verwendung" entfällt (§ 3.5); die 2 Felder ohne Beschriftung bekommen eine |
| Form_Quellprofil | kein Designer; 1 084 Z., 7 MessageBox, 45 Textschlüssel | 3 Betriebsarten, Reiter, virtualisiertes Raster, Altweg-Reiter | Abgleich gegen den Quelltext |
| Form_Waermesenke | kein Designer; 2 050 Z., 5 MessageBox, 63 Textschlüssel, 35 Steuerelemente | 4 Gruppen: Senkenliste, Senkenzeile, Parallelverbund, Ladeverhalten | Abgleich gegen den Quelltext |

**Beschriftungen aus dem Designer, nicht aus der Karte** — wie in Welle 9. Die
drei Felder ohne Beschriftung (2 + 1) sind Anzeigelabel, deren Text zur Laufzeit
gesetzt wurde; sie bekommen in der Razor-Fassung eine eigene Beschriftung, weil
ein Formularraster keine stillschweigende Spaltenzuordnung kennt.

---

## 5. Abweichungen (mit Begründung)

| # | Was | Warum |
|---|---|---|
| **A‑1** | `BetriebsmodusDialog` bekommt einen Hilfeeinstieg (Befund W10‑B2) | Der Vorläufer hatte als einziger keinen — Erdreich (:206), Quellprofil (:148), Wärmesenke (:275) und PufferSp_Projekt (:170) rufen alle `InfoKnopf.Anbringen`. Ein Dialog ohne Hilfe ist in dieser Reihe die Ausnahme, nicht die Regel. `help_mapping.txt` bekommt die Zielzeile |
| **A‑2** | `KlimazonenkarteDialog` ebenso (Befund W10‑B2) | dieselbe Begründung; Ziel ist die Wikiseite „Wärmequelle Erdreich" |
| **A‑3** | Bodentyp und Klimazone sind **schlüssel**gekoppelt statt über den Listenindex (Befund W10‑B6) | `AktuellerBodentyp`:911‑916 und `AktuelleZone`:918‑921 lasen `SelectedIndex` und rechneten ihn in einen Katalogschlüssel um. Wächst der Katalog um einen Eintrag in der Mitte, zeigt der Dialog stumm den falschen Boden |
| **A‑4** | Erdkollektor und Erdsonde haben **getrennte** Modellfelder (Befund W10‑B11) | `rbQuellsystem_CheckedChanged`:750‑758 schrieb beim Umschalten die Vorgaben `"90"` bzw. `"1,5"` in die Felder des **inaktiven** Zweigs — wer zwischen beiden hin- und herschaltete, verlor seine Eingaben. Jetzt behält jeder Zweig seine Werte |
| **A‑5** | Der Simulationslauf läuft **asynchron** mit sichtbarem Wartezustand (Befund W10‑B9) | § 3.4. Probe R‑W10a‑2 hat gezeigt, dass der Lauf im fremden Faden fehlerfrei durchgeht |
| **A‑6** | Zwei Hinweise, zwei Warnstufen im Quellendialog Pufferspeicher (Befund W10‑B16) | Ein `_lblLeer` trug zwei ganz verschiedene Aussagen — „Das Projekt enthält noch keinen Pufferspeicher" und „Quelle bisher nur über den Namen …". Wer die zweite las, sah die erste nicht mehr |
| **A‑7** | Beide Zwillingsknöpfe des Quellprofils melden (Befund W10‑B18) | „alle Monate" meldete bei ungültiger Eingabe, „alle Werte" kehrte **stumm** zurück (:477 gegen :929) — dieselbe Handlung, zwei Verhaltensweisen. Beide fragen jetzt über `WertAbfrage`, und dort bleibt OK gesperrt, solange keine Zahl dasteht: Der Unterschied verschwindet an der Wurzel |
| **A‑8** | Eine unlesbare Rasterzelle wird **angezeigt** (Befund W10‑B19) | `GridUebernehmen`:854‑856 verschluckte den Konvertierungsfehler mit `catch { }`; die Zelle behielt still ihren alten Wert. Jetzt färbt das Zahlenfeld wie überall |
| **A‑9** | Die Ersatzwerte −1/−2/−3 sind weg (Befund W10‑B24) | Der Vorläufer kodierte **Eingabefehler in Datenfeldern des Modells** (Ladegrenze −1 = unlesbar, −2 = außerhalb; bei der Einspeisehöhe begann dieselbe Reihe erst bei −2, weil −1 schon „nicht gesetzt" hieß). „Nicht gesetzt" ist jetzt `null`, der Eingabefehler bleibt im Formularzustand — gemeldet beim OK, mit demselben Wortlaut und derselben Nennung des Rangs |
| **A‑10** | 30 `MessageBox` werden `Warnbanner`, `Rueckfrage` oder Meldungstext | Wie A‑10 aus Welle 9: Bestätigungen bleiben Bestätigungen, Ablehnungen bleiben als Banner stehen und lassen den Dialog offen |
| **A‑11** | Die grünen Statuszeilen von `Form_PufferSp_Projekt` werden die **neue** Warnstufe `Erfolg` | Der Vorläufer setzte `label.ForeColor = Color.Green`. Eine Farbe allein ist keine Aussage (Hochkontrast, Farbsinn); die Stufe trägt zusätzlich ein Zeichen |
| **A‑12** | Die Pufferverwaltung erscheint bei Quelle **und** Senke als **Überlagerung** statt als zweites Fenster | Risiko R2: zwei WebViews übereinander. Dasselbe Muster wie in W4 und W9 |
| **A‑13** | „Katalog ansehen" bekommt ein **eigenes** Sprungziel (Befund W10‑B28) | `btnKatalog_Click`:1596 setzte `m_bReadOnly = true`, `WinFormsNavigation.cs`:152‑153 kannte das Kennzeichen nicht. Mit `Masken.PufferSpAdmin` wäre aus dem Nachschlagen unversehens das Bearbeiten des Auslieferungskatalogs geworden |
| **A‑14** | `Form_QuellePufferspeicher` bekommt seine fehlende `help_mapping.txt`-Zeile (Befund W10‑B13) | Sie stand im `HilfeKontext`, aber nicht in der Zuordnung — der Infoknopf lief ins Leere |
| **A‑15** | Die Bildkarte ist **mit der Tastatur bedienbar** | Der Vorläufer war ein reines Mausziel (`MouseMove`/`MouseClick` auf einem `Control`). Die 15 Flächen tragen jetzt `tabindex`, Enter und Leertaste wählen; die Zonenliste im Erdreichdialog bleibt als zweiter Weg bestehen (Befund W10‑B4) |
| **A‑16** | Kein KI-Aufrufknopf, kein `SchriftAngleichen`, keine Pixelarithmetik, keine Laufzeit-Steuerelemente | Wie A‑14 aus den Wellen 8 und 9: Der KI-Einstieg hat in `EPOS.UI` noch keinen Baustein (W15b); die übrigen sind WinForms-Layoutkorrekturen, die Hülle und CSS erledigen. Die 11 + 20 Laufzeitfelder (Befunde W10‑B14/B31) entfallen vollständig |
| **A‑17** | Der Kommentar in `Form_Simulation_Config.Karten.cs`:611 nennt jetzt **neun** Aufrufstellen (Befund W10‑B40) | Er sprach von acht; gezählt sind neun |

---

## 6. Texte

**Sechs neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`,
`Resource.en-US.resx` und — von Hand, weil hier kein Visual Studio läuft —
`Resource.Designer.cs`. Alle drei Dateien geprüft: **3 828 de = 3 828 en**,
0 Dubletten, 0 Schlüssel nur in einer Sprache.

| Schlüssel | Wofür |
|---|---|
| `SIMQ_ERDREICH_GB_STANDORT` | Gruppentitel „Standort und Boden" |
| `SIMQ_ERDREICH_BILD_PLATZHALTER` | „Noch kein Diagramm" vor dem ersten Lauf |
| `SIMQ_ERDREICH_SIM_LAEUFT` | der Wartezustand des Simulationslaufs (A‑5) |
| `PSP_HERLEITUNG_VERWENDUNG` | der aus dem Klassen-Set abgeleitete Altwert (§ 3.5) |
| `SIM_HERLEITUNG_SPEICHERLISTE` | warum das Speicher-Dropdown ALLE Projektpuffer zeigt (Befund W10‑B26) |
| `SIM_HERLEITUNG_VERBUNDLISTE` | warum die Verbundliste kürzer ist (derselbe Befund) |

**Nur sechs, weil die sieben Masken ihre Texte schon im Katalog hatten.** Keine
von ihnen führte eine eigene `.resx`; ihre Anzeigetexte standen seit jeher unter
`SIM_*` (298), `SIMQ_*` (192) und `PSP_*` (158). Die sechs Hüllen greifen auf
**266 verschiedene Schlüssel** zu — 108 `SIMQ_*`, 84 `PSP_*`, 63 `SIM_*`, dazu
`CHART_*`, `KANAL_*`, `SIMWARN_*` und `ALLG_*`.

**Die 30 `MessageBox` sind Warnbanner, Rückfragen und Meldungstexte geworden**
(A‑10) — Wortlaut, Reihenfolge und Knopfbelegung wörtlich; nur der Träger ist
ein anderer. Ihre Verteilung: Erdreich 4, Quellprofil 7, Quellpufferspeicher 5,
Wärmesenke 5, PufferSp_Projekt 9; Betriebsmodus und Klimazonenkarte hatten
keine.

**Nicht übersetzt sind die Steuerwerte:** `WaermequelleClass.MODUS_*`
(Betriebsmodus), die Kanalschlüssel, die Bodentyp- und Zonenschlüssel und die
Verwendungswerte der Pufferspeicher. Sie stehen in der Datenbank und werden
beim nächsten Öffnen wieder mit der Liste verglichen — Drei-Schichten-Regel,
Persistenzschicht.

**`help_mapping.txt`** bekommt **drei** Zeilen: `Form_Betriebsmodus.btn_Help`
und `Form_Klimazonenkarte.btn_Help` sind neu (A‑1/A‑2),
`Form_QuellePufferspeicher.btn_Help` schließt die Lücke aus Befund W10‑B13
(A‑14). Die vorhandene Zeile `Form_QuelleErdreich.btn_Help` bleibt: Der
Schlüssel benennt die Wikiseite, nicht die Klasse.

**`Allgemein/KI/HilfeKontext.cs`:** die fünf Einträge der gelöschten Masken
entfernt, jeweils im Commit ihrer Maske (Regel F10).

---

## 7. WinForms-Seite

**Gelöscht** (13 Dateien + 2 Ressourcen):

```
Views/Simulation/Form_Betriebsmodus.{cs,Designer.cs}
Views/Simulation/Form_Klimazonenkarte.{cs,Designer.cs}
Views/Simulation/Form_QuelleErdreich.{cs,Designer.cs}
Views/Simulation/Form_QuellePufferspeicher.{cs,Designer.cs}
Views/Pufferspeicher/Form_PufferSp_Projekt.{cs,Designer.cs}
Views/Simulation/Form_Quellprofil.cs                (ohne Designer)
Views/Simulation/Form_Waermesenke.cs                (ohne Designer)
Allgemein/GrafikTools/KlimazonenKarte.cs            (nur diese eine Maske nutzte es)
Ressourcen/Zonenkarte_Klimazonen.png                → EPOS.UI/wwwroot/bilder/
Ressourcen/Zonenkarte_Klimazonen.svg                → Werkzeuge/KlimazonenPfade/
```

**Keine `.resx` dabei** — keine der sieben Masken hatte eine eigene; ihre Texte
lagen von Anfang an im gemeinsamen Katalog.

Der `EmbeddedResource`-Block der `.csproj` entfällt mit dem Steuerelement, das
ihn brauchte. Das Kartenbild geht **byte-gleich** (`R100`) nach
`EPOS.UI/wwwroot/bilder/`, die SVG als Eingang des Generators nach
`Werkzeuge/KlimazonenPfade/`.

**Neu auf der Windows-Seite** (6): `Views/Simulation/BetriebsmodusHuelle.cs`,
`Views/Simulation/QuelleErdreichHuelle.cs`,
`Views/Simulation/QuellePufferspeicherHuelle.cs`,
`Views/Simulation/QuellprofilHuelle.cs`,
`Views/Simulation/WaermesenkeHuelle.cs`,
`Views/Pufferspeicher/PufferSpProjektHuelle.cs`.

**Aufrufer umgestellt:** `Form_Simulation_Config.Uebersicht.cs` — **fünf**
Aufrufstellen (`:342` Pufferverwaltung, `:621` Betriebsmodus, `:741`
Wärmesenke, `:892` Quellpufferspeicher, `:1033` Quellprofil, `:1094`
Erdreich). Zwei weitere Aufrufwege verschwinden **mit** ihren Masken:
`Form_QuelleErdreich`:1081 → Klimazonenkarte wird eine Überlagerung,
`Form_QuellePufferspeicher`:968 und `Form_Waermesenke`:1790 → die
Pufferverwaltung wird zweimal eine Überlagerung (A‑12).

**Ein toter Rückfallweg gestrichen** (Befund W10‑B7):
`Form_QuelleErdreich.ProjektErmitteln`:1102‑1108 fiel auf
`this.Owner as Form_Simulation_Config` zurück, wenn `m_ID_Projekt` leer war —
der Owner war immer gesetzt, der Zweig lief nie.

**Eine Doppelung entdoppelt** (Befund W10‑B8): `ErgebnisUebernehmen`:1155‑1188
war Zeile für Zeile die Zuordnung aus `Uebersicht.cs`:1130‑1162. Beide Stellen
rufen jetzt `ErdreichAuswertung.ErgebnisZuordnen`.

**Keine Typverwendung ist übrig:**

```
grep -rn "(new|typeof|:)\s*(Form_Betriebsmodus|Form_Klimazonenkarte|
    Form_QuelleErdreich|Form_QuellePufferspeicher|Form_PufferSp_Projekt|
    Form_Quellprofil|Form_Waermesenke|KlimazonenKarte)\b" --include=*.cs .
→ 0 Treffer im Code
```

Restfundstellen der alten Namen sind ausschließlich (a) `HilfeSchluessel`- und
`help_mapping`-Zeichenketten, (b) Kommentare, die die Herkunft nennen, und
(c) der Erreichbarkeitsbericht `Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md`
(ein datierter Messstand, kein Code).

---

## 8. Nachweise

### 8.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64
→ 0 Fehler, 17 Warnungen
```

**20 → 17.** Die drei entfallenen sind WFO1000-Fundstellen der gelöschten
Designer-Masken (14 → 11); die übrige Aufteilung ist unverändert: 2 CS0108,
2 CS0109, 1 WFO0003, 1 CA2255. **Keine neue Warnung.**

```
dotnet build WP-Plan.Kern.slnf -c Release
→ 0 Fehler, 3 Warnungen (2 CS0108, 1 CA2255 — Bestand)
```

### 8.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ KiKern.Tests         450 gruen
  SpeicherEngine.Tests  337 gruen
  EPOS.UI.Tests       1 269 gruen   (+165 aus Welle 10a)
  EPOS.Kern.Tests       228 gruen   (+53 aus Welle 10a)
  zusammen            2 284 gruen, 0 rot
```

**Beide Sprachen, drei Kulturen.** Die Regel seit Welle 8 verlangt einen zweiten
Lauf mit englischer Umgebung, weil der Windows-Läufer en-US ist:

```
LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 dotnet test WP-Plan.Kern.slnf -c Release
→ dieselben 2 284 gruen, 0 rot
```

Nachgemessen, dass die Umgebung wirklich ankommt: ohne `LANG` läuft der Container
unter der **invarianten** Kultur, mit `LANG=en_US.UTF-8` unter **en-US**. Beide
Läufe sind grün — die Kulturklammer (`CultureInfo.CurrentCulture`/`CurrentUICulture`
auf de‑DE für die Dauer des Falls) sitzt in jedem neuen Testkonstruktor.

**165 neue bunit-Fälle**: `WertAbfrage` 7, `Bildkarte` 8, `BetriebsmodusDialog` 9,
`KlimazonenkarteDialog` 11, `QuellePufferspeicherDialog` 22, `QuellprofilDialog` 22,
`WaermesenkeDialog` 23, `PufferSpProjektDialog` 27, `QuelleErdreichDialog` 28
(Zahl der `[Fact]`/`[Theory]`-Methoden; `[Theory]`-Fälle zählen mehrfach), dazu
der Zählwert in `SprungzielTests` (8 → 9 Ziele) und die neue
`Beschreibungen`-Prüfung in `OptionsgruppeTests`. Jeder Satz prüft den
Feldbestand (Zahl UND Beschriftungen), die Vorbelegung, die Prüfregeln, die
Rückrufe und die Tastatur.

**53 neue Kern-Fälle**: `WaermesenkeAnzeigeTests` 7 (die fünf Zweige von
`SenkeAnzeige` samt `SENKE_LEER` und der zusammengeführten `IstPufferZiel`),
`SimulationsdialogeKernTests` 23 (Kapazitätsformel, Katalogzeilen gegen
`Kenndaten_Test.sqlite`, Sondenmeter, Volllaststunden je Zone, die
Monats- und Wochenserialisierung hin und zurück, `ErgebnisZuordnen`),
`KlimazonenPfadeTests` 6 (15 Zonen, ViewBox, jeder Pfad geschlossen und
parsbar), `SimulationslaufAusFremdemFadenTests` 2 (Probe R‑W10a‑2).

### 8.3 Formularkarte

```
dotnet test Werkzeuge/Formularkarte.Tests -c Release
→ 123 gruen
```

Drei Zähler bewegt: **53** Designer-Dateien (58), **50** Masken (55),
**49 von 50** erreichbar (54 von 55). **Keine Test-Anker umgehängt** — die
beiden Zeugen, die Welle 9 gesetzt hat (`Form_Stromganglinie` für „erreichbar",
`Form_PufferSp_Bearbeiten` für „unklar"), stehen beide erst in späteren Wellen
an und sind unberührt.

### 8.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1 --erreichbarkeit
→ Designer-Dateien 51, davon Masken 50 (55 nach W9), lokalisiert 29,
  Kartenzeilen 911, erreichbar 49, unerreichbar 0, verwaist 0, unklar 1
```

**50 = 55 − 5.** Nicht − 7: `Form_Quellprofil` und `Form_Waermesenke` haben nie
einen Designer gehabt und standen deshalb nie in dieser Liste (Befund W10‑B38).
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md` ist neu erzeugt und um
einen Abschnitt „Stand nach iU9‑W10a" ergänzt; seine Zählung stand noch auf dem
Stand nach W4 (89 von 91).

### 8.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 240 SQL-Texte geprueft: 0 Fundstellen, 171 dynamisch, 1 069 in Ordnung
python3 … --selbsttest
→ 32 Anweisungen, 0 Abweichungen
```

Gezogen nach **jedem** Schritt, der SQL angefasst hat. Die Zahl bleibt bei 1 240
(1 241 nach W9): Das inline-SQL des Pufferdialogs (Befund W10‑B27) ist nicht
verschwunden, sondern in `PufferSpStammCtrl.Katalogzeilen` umgezogen — es wird
dort weiter geprüft.

### 8.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 16 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

**15 → 16.** Neu ist `jahresgang_erdreich` (1 304 × 440, 97,558 % Deckung,
2 109 Farben, deterministisch) — das zweireihige Jahresbild der Quelltemperatur
aus W10a.0d.

### 8.7 Referenzlauf

**Pflicht in dieser Welle**, weil Senkenliste, Ladeordnung, Pufferparameter und
Quellprofile unmittelbarer Simulationseingang sind und sieben Kerndateien
angefasst wurden.

```
dotnet run --project EPOS.Referenzlauf -c Release -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w10a
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/w10a
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

**Byte-gleich, nicht nur innerhalb der Toleranz.**

---

## 9. Grenzen

* **Am Gerät ungeprüft.** Alles hier ist ohne Windows entstanden. Die
  Abnahmeliste in § 10 ist der Prüfplan.
* **Die Bildkarte ist der erste Baustein, der von einer Bilddatei abhängt.**
  Lädt `wwwroot/bilder/Zonenkarte_Klimazonen.png` nicht (falsche Basis-URL,
  fehlendes `wwwroot` im Veröffentlichungsordner — genau die Falle, die den
  Razor-SDK in der `.csproj` nötig macht), bleiben die 15 Flächen sichtbar und
  wählbar, aber ohne Hintergrund. Das ist der Abnahmepunkt 3.
* **Der Simulationslauf im Erdreichdialog ist der zweite `Task.Run` mit
  Datenbankzugriff** (nach `KapitalwertVerlaufHuelle`, W1.6). Probe R‑W10a‑2
  belegt ihn gegen die Testdatenbank; wie er sich gegen eine **geöffnete**
  Anwenderdatenbank verhält, zeigt erst das Gerät (Abnahmepunkt 5).
* **`Form_Simulation_Config` ist jetzt ein WinForms-Wirt mit sieben
  Blazor-Kindern.** Jeder Aufruf baut eine eigene, modale WebView. Ob das auf
  einem älteren Gerät flüssig bleibt, ist Abnahmepunkt 1.
* **Sechs Bestandsbefunde bleiben stehen** (§ 11) — sie sind Fachentscheidungen,
  keine Portfragen.

---

## 10. Abnahmeliste Windows (iZ5) für diese sieben Masken

Je Dialog: öffnet mittig, kein weißes Aufblitzen, ziehbar und maximierbar,
Tabellen ohne Umbruch, de **und** en (`HKCU\Software\wp-plan\Language`),
Hochkontrast, 125 % und 150 % scharf, Maus **und** Finger (44 px), Tab-Zyklus
bleibt im Dialog, Esc schließt, Infoknopf zeigt die Wikiseite.

| # | Aufrufweg | Was besonders zu prüfen ist |
|---|---|---|
| 1 | Simulationskonfiguration → **Erzeugerkarte → Betriebsmodus** | Nur für Wärmepumpen (die Vorprüfung bleibt beim Aufrufer); ein leerer oder unbekannter `BM_Typ` steht auf „laufzeitoptimiert"; unter jedem Wahlknopf steht seine Erläuterung; **Enter bestätigt** (reiner Entscheidungsdialog); der PV-Hinweis kommt weiterhin vom Aufrufer, wenn keine PV-Anlage in der Simulation steht |
| 2 | Simulationskonfiguration → **Wärmequelle Erdreich** | Bodentyp und Klimazone über den **Schlüssel** (A‑3): Katalogeintrag in der Mitte einfügen und prüfen, dass die Anzeige stimmt; zwischen Erdkollektor und Erdsonde hin- und herschalten — **die Eingaben des anderen Zweigs bleiben stehen** (A‑4) |
| 3 | In 2 → **„…" neben der Klimazone** | Die Karte erscheint als **Überlagerung**, nicht als zweites Fenster; das Kartenbild ist da (§ 9); Zeigen färbt, Klicken wählt, Doppelklick übernimmt; **Tab und Enter** erreichen jede der 15 Zonen (A‑15); „nicht zugeordnet" ist auf der Karte nicht wählbar, in der Liste dahinter schon (Befund W10‑B4); OK ohne Auswahl ändert nichts |
| 4 | In 2 → **„Simulation"** | Der Knopf sperrt, die Wartefläche erscheint, das Fenster bleibt bedienbar und **friert nicht ein** (A‑5); danach stehen Prüfergebnis, Vorbehalt und Frosthinweis; das Jahresgangbild zeigt **zwei** Reihen (Quell- und Außentemperatur) |
| 5 | Wie 4, aber mit **großem Projekt** | Laufzeit und Speicher messen: Der Lauf ist ein vollständiger Jahresgang in einem fremden Faden. Fällt das durch, wäre der Rückweg der synchrone Lauf nach `await Task.Yield()` |
| 6 | Simulationskonfiguration → **Pufferspeicher (Verwaltung)** | **Kein Abbrechen** (Befund W10‑B29) — Anlegen, Ändern und Entfernen wirken sofort, Esc nimmt nichts zurück; das **Klassen-Set** ist die Pflichtangabe, die abgeleitete Verwendung steht als leise Zeile darunter (§ 3.5); die Wechsel-Rückfrage kommt beim Übernehmen, nicht beim Klicken; die grünen Statuszeilen sind jetzt Erfolgsbanner **mit Zeichen** (A‑11) |
| 7 | In 6 → **„Katalog ansehen"** | Springt in die Pufferspeicher-Verwaltung **nur zum Ansehen** (A‑13) — dort darf nichts schreibbar sein; zurück steht der Projektdialog unverändert |
| 8 | In 6 → Schichtung ab **zwei Schichten** | Die Schichtfelder erscheinen erst dann (die Sichtbarkeitsregel bleibt, die 20 Laufzeitfelder sind weg); die Kapazität rechnet nach `V × 1,16 × ΔT / 1000` und bleibt leer, wenn Volumen ≤ 0 oder Vorlauf ≤ Rücklauf |
| 9 | Simulationskonfiguration → **Wärmequelle Pufferspeicher** (Wärmepumpe) | Parameterblock sichtbar; Haken „unbegrenzt verfügbar" **und** gewählter Puffer ergibt eine Warnung samt der Temperatur, die dann gälte — der Dialog **verwirft nichts**; die Pufferliste ist ungefiltert |
| 10 | Dasselbe für einen **Heizkessel** | Kein Parameterblock, dafür Kaskadenhinweis und Temperaturbezug; **Quelltemperatur, Spreizung, Regeneration und „unbegrenzt" bleiben unangetastet** (Befund W10‑B15) — nach dem Speichern prüfen, dass die WP-Vorgaben noch dastehen |
| 11 | In 9/10 → **„Pufferspeicher verwalten"** | Erscheint als Überlagerung (A‑12); nach dem Schließen steht die Liste neu, die Auswahl bleibt |
| 12 | In 9 ohne Puffer im Projekt / mit Altbezeichner | **Zwei** verschiedene Banner mit eigener Stufe (A‑6) — beide dürfen gleichzeitig stehen |
| 13 | Simulationskonfiguration → **Quellprofil** | Betriebsart wechseln: Der Reiter wechselt mit, und **das vorderste Blatt ist das der neuen Betriebsart**; „alle Monate" und „alle Werte" fragen beide über dieselbe Abfrage und melden beide (A‑7) |
| 14 | In 13 → Betriebsart **Stunde**, CSV einlesen | 8 760 Zeilen im virtualisierten Raster — flüssig scrollen; eine unlesbare Zelle **färbt** (A‑8); Min/Max/Mittel stimmen |
| 15 | In 13 mit einem Wochengang an der Anlage | Der Altweg-Reiter „Wochenwerte" erscheint, ist **nur lesend**, und die Herleitungszeile sagt, dass er nicht mehr wirkt (Befund W10‑B17) |
| 16 | Simulationskonfiguration → **Wärmesenken** | Rangfolge tauschen: Die **PV-Sonderpriorität wandert nicht mit** (nur Rang 1 kennt sie); ein Feld, das nicht zum Ziel passt, wird beim Zielwechsel **gelöscht** und wirkt nicht heimlich weiter |
| 17 | In 16 → **Ladegrenze / Anschlusshöhe** ungültig | Der Fehler steht im **Formular**, nicht im Modell (A‑9): Meldung beim OK, mit dem alten Wortlaut und der Nennung des Rangs; nach dem Korrigieren speichert OK ohne Rest |
| 18 | In 16 → **Parallelverbund** | Nur Speicher **derselben Verwendung** stehen zur Wahl, das Speicher-Dropdown darüber zeigt **alle** — die beiden Herleitungszeilen erklären den Unterschied (Befund W10‑B26); der Verbund hängt weiter an Rang 1 (W10‑B25) |
| 19 | **Schema-Ansicht** und **Erzeugerkarten** der Simulationskonfiguration | Sie zeigen die Senken über `WaermesenkeClass.SenkeAnzeige` (§ 3.1) — nach jeder Senkenänderung prüfen, dass Karte und Schema denselben Text zeigen wie der Dialog |
| 20 | **Sprache umstellen auf en** und 1–19 stichprobenartig wiederholen | Alle 266 Textschlüssel liegen in beiden Sprachen; die Steuerwerte (Modus, Kanal, Boden, Zone, Verwendung) dürfen sich **nicht** mit übersetzen |

---

## 11. Offene Punkte

| # | Was | Vorschlag |
|---|---|---|
| **W10‑B10 / W10a‑O‑1** | Die Auslegungsprüfung rechnet mit den **angezeigten** Eingaben, der Simulationslauf mit dem **Datenbankstand** (`Form_QuelleErdreich`:961‑974 gegen :1039). Wer etwas ändert und sofort „Simulation" drückt, sieht eine Prüfung zu den neuen und ein Ergebnis zu den alten Werten | Wörtlich übernommen (Regel F3). **Entscheid des Anwenders:** Soll der Knopf vorher speichern, oder soll die Prüfung auf den Datenbankstand umgestellt werden? |
| **W10‑B25 / W10a‑O‑2** | Der Parallelverbund hängt **konstruktiv an Rang 1**: `Z_AnlagePufferVerbund` führt keine `ID_Senke`, ein Verbund auf Rang 2 ist im Schema nicht abbildbar | Wörtlich übernommen und im Dialog dokumentiert. **Entscheid des Anwenders:** Soll die Tabelle eine `ID_Senke` bekommen? Das wäre ein Migrationsschritt, keine Maskenfrage |
| **W10‑B26 / W10a‑O‑3** | Das Speicher-Dropdown zeigt **alle** Projektpuffer, die Verbundliste filtert nach Verwendung — zwei Listen desselben Dialogs mit verschiedenen Regeln | Wörtlich übernommen, aber erstmals **erklärt**: zwei Herleitungszeilen (`SIM_HERLEITUNG_SPEICHERLISTE`, `SIM_HERLEITUNG_VERBUNDLISTE`). **Frage an den Anwender:** Soll das Dropdown ebenfalls filtern? Dann fiele die Meldung „Klassen-Set führt den Kanal nicht" weg |
| **W10‑B29 / W10a‑O‑4** | Die Pufferverwaltung hat **kein Abbrechen** — jede Handlung wirkt sofort | Wörtlich übernommen; die `SpeichernLeiste` trägt nur „Schließen", damit kein Knopf etwas verspricht, was er nicht hält. **Entscheid des Anwenders:** Soll der Dialog auf „Sammeln und beim Schließen schreiben" umgebaut werden? |
| **W10‑B39 / W10a‑O‑5** | `Form_Simulation_Config` hat **keine `de-DE.resx`** (nur `.resx` + `.en-US.resx`), und der Ordner `Views/Simulation` steht nicht in der Lückenliste von `CLAUDE.md` | Erledigt sich mit **W10b**, wenn die Maske selbst geht. Bis dahin unverändert |
| **W10a‑O‑6** | Der KI-Aufrufknopf fehlt in allen sieben Dialogen (A‑16) | Mit W15b, wenn `Gespraechsverlauf` steht — wie W6‑O‑6, W7‑O‑6, W8‑O‑6 und W9‑O‑6 |
| **W10a‑O‑7** | Die Karte als **Überlagerung** statt als Fenster ändert die Fenstergröße: Der Erdreichdialog muss die 1 304 × 1 350 der Karte tragen | In der Komponente auf `max-height` gestellt und scrollbar; am Gerät bei 150 % prüfen (Abnahmepunkt 3) |

### Neue Befunde dieser Welle

* **W10a‑B41 (neu):** Die WinForms-Klimazonenkarte **konnte die ausgelieferte SVG
  nie lesen.** `KlimazonenKarte.PfadParsen` zerlegte den Pfadtext mit
  `Split(' ')` und erwartete den Befehlsbuchstaben getrennt von der ersten
  Koordinate; die Datei schreibt ihn **angeklebt** (`M315.30,1201.71`).
  `float.Parse("M315.30")` warf, und der umschließende `catch { _daten = null; }`
  verschluckte es — die Maske zeigte immer nur ihre Ladefehlerzeile. Der
  Generator `erzeugen.py` nimmt deshalb einen richtigen Tokenizer
  (`[MLZmlz]|[-+]?[0-9]*\.?[0-9]+…`). **Die Blazor-Fassung stellt die Funktion
  zum ersten Mal her** — sie ist damit kein Nachbau, sondern die erste
  funktionierende Ausgabe dieser Maske.

### Entfallene Befunde

* **W10‑B7:** der tote Rückfallweg `Owner as Form_Simulation_Config` — ersatzlos.
* **W10‑B14 / W10‑B31:** 11 + 20 Steuerelemente, die nur zur Laufzeit entstanden,
  weil die Designer-Datei nicht von Hand bearbeitet werden darf. Der ganze Block
  entfällt; die **fachlichen** Sichtbarkeitsregeln bleiben.
* **W10‑B30:** `Verwendung` war Ein- **und** Ausgabefeld, wurde aber von keinem
  der drei Aufrufer zurückgelesen. Sie geht nur noch hinein.
* **W10‑B37:** die halbierten Bildschirmkoordinaten (`p1.Y /= 2; p1.X /= 2;`)
  der Aufrufer — die Hülle setzt kein `Location`.

---

## 12. Geänderte und neue Dateien

**Neu in `EPOS.UI`** (14): `Bausteine/Bildkarte.razor`;
`Dialoge/Simulation/WertAbfrage.razor`, `BetriebsmodusDialog.razor`,
`KlimazonenkarteDialog.razor`, `QuelleErdreichDialog.razor`,
`QuelleErdreichDaten.cs`, `PufferSpProjektDialog.razor`,
`PufferSpProjektDaten.cs`, `QuellePufferspeicherDialog.razor`,
`QuellePufferspeicherDaten.cs`, `QuellprofilDialog.razor`,
`QuellprofilDaten.cs`, `WaermesenkeDialog.razor`, `WaermesenkeDaten.cs`.
**Geändert in `EPOS.UI`** (5): `Bausteine/WarnStufe.cs` (neue Stufe `Erfolg`),
`Bausteine/Warnbanner.razor`, `Bausteine/Optionsgruppe.razor` (neuer Parameter
`Beschreibungen`), `Dialoge/Allgemein/Sprungziel.cs`, `wwwroot/epos-ui.css`;
dazu `wwwroot/bilder/Zonenkarte_Klimazonen.png` (verschoben, byte-gleich).

**Neu im Kern** (1): `Allgemein/Simulation/KlimazonenPfade.cs` (erzeugt).
**Geändert im Kern** (7 + Ressourcen): `Allgemein/Simulation/WaermesenkeClass.cs`,
`Allgemein/Simulation/VDI4640Pruefung.cs`,
`Allgemein/Simulation/ErdreichAuswertung.cs`,
`Allgemein/Update/ProjektPuffer.cs`, `Controller/PufferSpStammCtrl.cs`,
`Controller/QuellprofilCtrl.cs`, `Allgemein/Bericht/ChartRenderer.cs`; dazu die
drei Ressourcendateien.

**Neu in der Anwendung** (6): die sechs Hüllen.
**Geändert in der Anwendung** (6): `Allgemein/Blazor/Sprungbruecke.cs`,
`Allgemein/KI/HilfeKontext.cs`, `Allgemein/Hilfe/help_mapping.txt`,
`Allgemein/Simulation/SchemaModell.cs`,
`Views/Simulation/Form_Simulation_Config.Uebersicht.cs`,
`Views/Simulation/Form_Simulation_Config.Karten.cs`,
`WindowsFormsApplication1.csproj`.

**Neu in den Werkzeugen** (2): `Werkzeuge/KlimazonenPfade/erzeugen.py` und die
Eingangsdatei `Zonenkarte_Klimazonen.svg` (verschoben).

**Neu in den Tests** (13): neun Klassen in `EPOS.UI.Tests/` (acht unter
`Dialoge/`, eine unter `Bausteine/`), vier in `EPOS.Kern.Tests/`.
**Geändert in den Tests** (5): `EPOS.Kern.Tests/ChartRendererTests.cs`,
`EPOS.UI.Tests/Dialoge/SprungzielTests.cs`,
`EPOS.UI.Tests/Bausteine/OptionsgruppeTests.cs`,
`Werkzeuge/Formularkarte.Tests/StapelTests.cs`,
`Werkzeuge/Formularkarte.Tests/ErreichbarkeitTests.cs`;
dazu `Proben/ChartProben/Program.cs` (das 16. Bild) und
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md`.
