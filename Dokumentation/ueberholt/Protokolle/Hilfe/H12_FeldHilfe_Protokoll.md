# H12 — Feldgenaue Hilfe (Umsetzungsprotokoll, 29.08.2026)

Ausgangsstand `20f4ea0` (Branch `Pufferspeicher`). Vorgänger: `H1H2_Umsetzung_Protokoll.md`
(Anker-Durchlass A3, Abschaltlogik § 14), `H7_InfoButtons_Protokoll.md` (`InfoKnopf`,
Namensabsicherung), `H11_Sammelpaket_Protokoll.md` (Tiefensuchen-Grenzregel,
Beschreibungs-Popup).

**Ergebnis in einem Satz:** Einzelne Eingabebereiche der großen Dialoge haben jetzt eigene
Hilfe — `help_mapping.txt` wächst von 99 auf **175** Zeilen (**76 Feldzuordnungen** auf
**70 Sprungmarken** über 10 Rubrikseiten); dafür war **ein** Eingriff im Extender nötig, weil
die Abschaltlogik der Infobuttons sonst ganze Eingabegruppen lahmlegt. Build 0 Fehler /
5 bekannte Warnungen; `dev\h12probe` **34/34 grün**, h7/h9/h10/h11 grün.

---

## 1. Aufgabe 1 — Mechanik-Befund

### 1.1 Die Frage

Trägt der HelpExtender-Pfad heute schon Nicht-Button-Steuerelemente? Und greift die
btn_Help-spezifische Abschalt-/Graulogik dabei fälschlich auf Felder durch?

### 1.2 Der Befund — zwei Hälften

**Die Verkabelung trägt bereits vollständig.** `HelpExtender.SetHelpKey` (`HelpCatalog.cs:1015`)
kennt keinen Typvorbehalt: Es hängt `MouseEnter`, `MouseLeave`, `Click` und `Disposed` an
**jedes** `Control`. `ZuordnungenAnwenden` sucht über `FindControlRecursive` ebenfalls jedes
Control. Eine GroupBox mit Mapping-Zeile bekommt damit ohne jede Codeänderung
Hover-Popup (flüchtig) und Klick-Anheften — gemessen, nicht vermutet (§ 5, Teil M).

**Die Abschaltlogik dagegen greift durch — und zwar zerstörerisch.** Zwei Stellen rufen
`SteuerelementAbschalten` für **jedes** registrierte Steuerelement, nicht nur für Infobuttons:

| Stelle | Auslöser |
|---|---|
| `ZuordnungenPruefen` (`:1386`) | nach dem Ladelauf, wenn `ZielAufloesen` leer bleibt |
| `EintragHolen` (`:1674`) | beim Überfahren/Klicken, wenn der geladene Katalog nichts liefert |

`SteuerelementAbschalten` setzt `Enabled = false`. Bei einem Infobutton ist das gewollt (F3,
„ein grauer Button ist ehrlicher als ein toter"). Bei einer **GroupBox** nimmt es in WinForms
**jedem Steuerelement darin** die Bedienbarkeit mit: `Control.Enabled` liefert im Getter erst
`false`, sobald ein Vorfahr gesperrt ist. Eine umbenannte, noch fehlende oder offline nicht
auflösbare Wiki-Seite hätte also den halben Dialog stillgelegt — ein Eingabefeld, in das man
nicht mehr tippen kann, wegen einer Hilfeseite.

Nur `InfobuttonsOhneZuordnungAbschalten` (`:1330`) war schon sauber: `InfobuttonsSammeln`
sammelt ausschließlich Namen mit dem Präfix `btn_Help`. Ein Feld **ohne** Zeile bleibt also
ohnehin unberührt — nachgemessen (Prüfung „gbOhne … völlig unberührt").

### 1.3 Rot-Nachweis, vor dem Eingriff

`dev\h12probe` gegen `dev\build_h12_vorher\` (Übersetzungsstand `20f4ea0`, unverändert):

```
   --- unaufgeloestes Feld: darf NIE abgeschaltet werden ---
  FEHL gbFehlt bleibt bedienbar                  -> Enabled=False
  FEHL Eingabefeld IN gbFehlt bleibt bedienbar   -> Enabled=False
  FEHL gbFehlt ist auch nach dem Nachfragen bedienbar -> Enabled=False
```
`Protokoll: dev\h12probe\lauf_rot.txt`, ExitCode 1.

Im selben Lauf bereits grün — der Beleg, dass die Verkabelung **nicht** ausgebaut werden
musste: Schlüsselvergabe, alle drei Ereignisbindungen, Popup beim Überfahren, Anker in der
Öffnungsadresse, Anheften beim Klick.

### 1.4 Der Eingriff — kleinstmöglich

`Allgemein\Hilfe\HelpCatalog.cs`, drei Stellen, keine geänderte Signatur:

| Neu / geändert | Inhalt |
|---|---|
| `IstInfobutton(Control)` (neu, statisch) | trägt der Name das Präfix `btn_Help`? Die eine Trennlinie. `InfobuttonsSammeln` nutzt sie jetzt ebenfalls statt der doppelten Prüfung |
| `Wirkung(Control)` (neu, statisch) | nur für die Protokollzeile: „abgeschaltet" bzw. „Feldhilfe bleibt still (H12)" |
| `SteuerelementAbschalten` | steigt für ein Nicht-Infobutton **sofort aus**, mit einer `Debug`-Zeile. `Enabled` und `Cursor` werden nicht angefasst, es entsteht kein Eintrag in `_abgeschaltet` |

`SteuerelementWiederEinschalten` brauchte keine Änderung: ohne Eintrag in `_abgeschaltet`
tut es ohnehin nichts.

**Wirkung für ein Feld, dessen Ziel der Katalog nicht kennt:** Es bleibt **still** — kein
Popup (`EintragHolen` liefert `null`, beide Ereignisbehandlungen steigen aus), aber auch
**kein Eingriff** in Bedienbarkeit oder Aussehen. Genau die Zusage des Auftrags.

**Buttons unverändert:** Beide Button-Fälle bleiben gemessen gleich — ohne Zeile abgeschaltet,
mit unbekanntem Ziel abgeschaltet, jeweils mit `Cursor = Default`.

---

## 2. Aufgabe 2 — Der Feldkatalog

### 2.1 Vier Aufnahmeregeln (im Kommentarkopf der Datei festgehalten)

1. **Zielseite ist immer die Rubrikseite des Dialogs** — dieselbe, die sein `btn_Help` öffnet.
   Eine Feldzeile verschiebt nie das Kapitel, sie verfeinert nur die Stelle darin.
2. **Nur nicht bedienbare Steuerelemente**: GroupBox, Panel/FlowLayoutPanel/TableLayoutPanel,
   Beschriftung. Der Extender hängt sich auf `Click`; ein angeheftetes Popup auf einem Knopf,
   einem Kontrollkästchen oder einem Eingabefeld stünde der Bedienung im Weg. Ein Feld wird
   deshalb über **seine Beschriftung** angesprochen, nicht über das Eingabefeld selbst.
   *Gegenprobe durchgeführt:* keines der 76 zugeordneten Steuerelemente trägt eine eigene
   `Click`/`MouseDown`/`DoubleClick`-Verdrahtung (Grep je Maske).
3. **Nur sichtbare Steuerelemente.**
4. **Ein Anker darf mehrfach vorkommen** — drei Schwellenbeschriftungen zeigen bewusst auf
   denselben Stichpunkt, die beiden Ferien-Gruppen ebenso.

### 2.2 Die 76 Zuordnungen je Dialog

| Dialog | Zeilen | Zielseite | Fundstelle der Namen |
|---|---:|---|---|
| `Form_PufferSp_Projekt` | 13 | Pufferspeicher | `Views\Pufferspeicher\Form_PufferSp_Projekt.Designer.cs` (`_gbListe:117`, `_gbDaten:305`, `_lblVorlauf:195`, `_lblRuecklauf:209`, `_lblEinschaltschwelle:231`, `_lblAbschaltschwelle:244`, `_lblSchwelleNachrangig:257`, `_lblMindestfuellstand:270`, `_gbLaden:367`, `_lblEntladeprio:375`) + `…Form_PufferSp_Projekt.cs` (`_lblKlassenSet:428`, `_gbSchichtung:662`, `_lblEntnahmeKopf` über `SchichtLabel:772`) |
| `Wizard_WPItem` | 8 | Wärmepumpe | `Views\Wizard\Wizard_WPItem.Designer.cs` (`label_WP:384`, `label31:401`, `groupBox2:446`, `groupBox3:197`, `label_Betriebsart:256`, `label_Abschalttemperatur:251`, `label7:186`, `label24:396`) |
| `Form_Klimadaten` | 8 | Klimadaten | `Views\Klimadaten\Form_Klimadaten.Designer.cs` (`panel2:404`, `label4:200`, `label6:238`, `label7:247`, `label8:266`, `label1:133`, `panel1:286`, `panel_KlimaGraph:296`) |
| `Form_QuelleErdreich` | 7 | Wärmequelle Erdreich | `Views\Simulation\Form_QuelleErdreich.Designer.cs` (`_gbSystem:151`, `_lblBodentyp:159`, `_lblKlimazone:189`, `_lblSpreizung:211`, `_lblKennwerte:236`, `_gbVorschau:243`, `_gbPruefung:277`) |
| `Form_Gebaeude2` | 6 | Gebäude | `Views\Gebäude\Form_Gebaeude2.designer.cs` (`groupBox1:147`, `groupBox2:249`, `groupBox3:294`, `groupBox5:347`, `groupBox6:445`, `groupBox7:534`) |
| `Form_Brauchwasser` | 5 | Brauchwasser | `Views\Brauchwasser\Form_Brauchwasser.designer.cs` (`Label24:122`, `Label1:231`, `Label12:141`, `Label19:196`, `groupBox1:241`) |
| `Form_Stromverbraucher` | 5 | Stromverbraucher | `Views\Stromverbraucher\Form_Stromverbraucher.designer.cs` (`Label24:121`, `Label1:242`, `Label12:140`, `Label19:199`, `groupBox1:258`) |
| `Form_Kosten` | 5 | Kosten | `Views\Kosten\Form_Kosten.Designer.cs` (`panel3:107`, `flpContainer:146`, `panel4:202`, `flpContainer_Betriebskosten:264`, `pnlFooter:435`) |
| `Form_Simulation_Config` | 4 | Simulation | `…Form_Simulation_Config.Designer.cs` (`label11:129`), `…Karten.cs` (`flow_Erzeuger`/`flow_Speicher` über `Kartenspalte:331`, gesetzt `:291/:292`), `…Schema.cs` (`label_Ansicht:65`) |
| `Form_Waermesenke` | 4 | Simulation | `Views\Simulation\Form_Waermesenke.cs` — **Namen mit H12 neu gesetzt**, siehe § 2.3 |
| `Form_Gebaeude` | 4 | Gebäude | `Views\Gebäude\Form_Gebaeude.designer.cs` (`groupBox1:98`, `groupBox2:147`, `label_ListProjektGebaeude:200`, `label_ListGebaeudeDB:205`) |
| `UcBericht` | 4 | Bericht | `Views\Bericht\UcBericht.Designer.cs` (`lblVarianten:132`, `lblBausteine:201`, `lblAusgabe:229`, `lblZiel:262`) |
| `Form_Gebaeude1` | 3 | Gebäude | `Views\Gebäude\Form_Gebaeude1.designer.cs` (`groupBox1:188`, `groupBox2:368`, `groupBox3:461`) |
| **Summe** | **76** | 10 Seiten | |

### 2.3 Der einzige Namensnachtrag: `Form_Waermesenke`

Die Maske baut ihre Oberfläche vollständig im Code auf und vergab **keinen einzigen**
`Control.Name` — nicht einmal ihren eigenen (den setzt seit H7 `InfoKnopf.NamenSicherstellen`).
Ohne Namen gibt es keine Anschrift für die Zuordnung.

Nachgetragen wurden vier Zeilen im jeweiligen Objektinitialisierer von `BaueOberflaeche()` —
genau das Muster, das `InfoKnopf` für die Maske selbst vormacht, reines ASCII:

| Gruppe | neuer `Name` | Beschriftung |
|---|---|---|
| lokale Variable `gbListe` | `_gbListe` | „Wärmesenken in Reihenfolge der Belieferung" |
| `_gbZeile` | `_gbZeile` | „Gewählte Senke" |
| `_gbVerbund` | `_gbVerbund` | „Parallelverbund (mehrere Speicher als ein Vorrat)" |
| `_gbLaden` | `_gbLaden` | „Ladeverhalten am Pufferspeicher" |

Keine weitere Datei brauchte einen Namensnachtrag.

### 2.4 Bewusst **nicht** aufgenommen — Befunde

| Was | Grund |
|---|---|
| **`Form_Simulation_Config.panel_Schema`** (`SchemaAnsicht : Panel`) | Die Zeichenfläche ist bedienbar: `OnMouseDown` wählt einen Knoten aus, `OnMouseMove` setzt einen eigenen Knotenhinweis und wechselt den Zeiger. Ein angeheftetes Hilfepopup läge der Bedienung im Weg (Regel 2). Der im Kommentarkopf seit H2 angedachte Anker `Simulation#hydraulikschema` entfällt damit vorerst; die Zeile steht als erklärter Kommentar in der Datei |
| **`UcBkKosten`** | Baut seine Oberfläche im Code auf und setzt an **keinem** Kind einen Namen — einziger `Control.Name` in der Datei ist `this.Name = "UcBkKosten"` (`:234`). Ohne Namen keine Anschrift. Mit einem Namensnachtrag (`pnlKopf`, `pnlKacheln`, `pnlListen`, `gridKomponenten`, `gridTraeger`, `lblStatus`) nachholbar |
| **`ErzeugerKarte` / `SpeicherKarte`** | `UserControl` ohne Namen — und zugleich die **Grenze** der Tiefensuche (H11). Damit sind **Quellprofil, Wärmequelle, Betriebsmodus und die Speicher-Schwellen auf den Karten** heute nicht feldgenau ansprechbar; sie sind je Anlage/Speicher mehrfach vorhanden, ein Name müsste ID-abhängig sein (`karte_Erzeuger_<ID>`) |
| **`ucKostenZeile`** (`Form_Kosten.cs:944`) | dito: `UserControl` ohne Namen, trägt nur `.Tag` |
| `Form_Simulation_Config.groupBox_Tools` / `groupBox_PufferSp` / `label12` / `label21` | `AltSteuerelementeStilllegen` (`…Karten.cs:222`) blendet sie dauerhaft aus |
| `Form_Kosten.tabEnergie` samt Inhalt, `panel2`/`panel5`/`panel9` + zugehörige Labels | zur Laufzeit entfernt bzw. `Dispose()` (`Form_Kosten.cs:162`, `:484-486`) |
| `Wizard_WPItem.groupBox1`, `textBox_Modulkosten`, `label32`, `label33` | unsichtbar bzw. zur Laufzeit entfernt (`Wizard_WPItem.cs:63/:86`, `:400-410`) |
| Knöpfe, Kontrollkästchen, Eingabefelder, Listen | Regel 2 |

---

## 3. Aufgabe 3 — Ankernamen

70 Sprungmarken, alle ASCII-klein aus `a-z 0-9 -`, je Rubrikseite eindeutig, maschinell
geprüft. Verteilung:

| Seite | Anker | Seite | Anker |
|---|---:|---|---:|
| Gebäude | 12 | Wärmequelle Erdreich | 7 |
| Pufferspeicher | 10 | Brauchwasser | 5 |
| Simulation | 8 | Stromverbraucher | 5 |
| Wärmepumpe | 8 | Kosten | 4 |
| Klimadaten | 7 | Bericht | 4 |

**Maschinenlesbare Liste** (steuert den späteren Wiki-Sammelimport):

```
C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan-WP-Plan-WindowsFormsApplication1\
  beb58fc8-af5b-4767-b81b-4941a52f3a8a\scratchpad\h12\anker_soll.txt
```

Format je Zeile, TAB-getrennt: `Seite <TAB> anker <TAB> Stichpunkt-Wortlaut-Anfang`.
Der dritte Teil nennt den Stichpunkt, vor dem `{{Anker|…}}` zu setzen ist — und ist zugleich
der Überschriftenvorschlag, falls es den Stichpunkt im Abschnitt „Eingaben" noch nicht gibt.
Die Datei ist gegen `help_mapping.txt` gegengeprüft: 70 = 70, keine Zeile auf einer Seite
ohne Gegenstück.

**Bis der Import läuft, ist nichts kaputt:** Eine Zeile `Seite#anker` löst schon heute auf
Seitenebene auf (der Anker wird vor der Katalogauflösung abgetrennt, A3). Der Verweis öffnet
die richtige Seite, der Sprung landet oben — akzeptiert.

---

## 4. Aufgabe 4 — `help_mapping.txt`

99 → **175** Zeilen. **Die 99 Bestandszeilen sind unangetastet** (maschinell geprüft: genau
99 Zeilen ohne `#` im Ziel, alle Ziele lösen auf, keine doppelte linke Seite). Der neue Block
steht am Dateiende hinter einem Kommentarkopf „Feldgenaue Hilfe (H12)", der die vier
Aufnahmeregeln, die Stillheits-Zusage und die Nicht-Aufnahmen begründet.

Kodierung unverändert **UTF-8 mit BOM, CRLF** — 439 von 439 Zeilen mit CR, 0 × `U+FFFD`.
Die Umlaute in den Zielen (`Wärmepumpe`, `Gebäude`, `Wärmequelle Erdreich`) sind intakt.

---

## 5. Aufgabe 5 — Prüfstand `dev\h12probe\`

Wegwerf-Projekt (gitignored, außerhalb von `WindowsFormsApplication1\`). Übersetzt gegen
`build_h12_vorher`, **geladen** wird zur Laufzeit der Ordner aus `args[0]` — derselbe
Prüfstand läuft damit rot gegen den alten und grün gegen den neuen Stand.

### 5.1 Ergebnis — `ALLES GRUEN`, ExitCode 0, **34 Prüfungen**, 0 `FEHL`

| Teil | Prüfung | Ergebnis |
|---|---|---|
| **M** | GroupBox mit Zeile: Schlüssel, `MouseEnter`/`MouseLeave`/`Click` verkabelt | ja |
| M | Popup beim Überfahren, Adresse **mit** Anker, flüchtig | `…/Simulation#h12-anker-eins` |
| M | zweites Feld (Label) trägt seinen **eigenen** Anker | `…/Klimadaten#h12-anker-zwei` |
| M | Klick heftet an | ja |
| M | **unaufgelöstes Feld** bleibt bedienbar, Zeiger unverändert, Kind bedienbar | **rot vor dem Eingriff** |
| M | unaufgelöstes Feld bleibt still (kein Eintrag, kein Popup) | ja |
| M | Feld ohne Zeile bleibt völlig unberührt | ja |
| M | `btn_Help*` ohne Zeile / mit unbekanntem Ziel weiterhin abgeschaltet | ja, `Cursor = Default` |
| **Z** | Zeilen gesamt / Bestand / H12 | **175 / 99 / 76** |
| Z | alle Ziele lösen im Livekatalog auf | **175/175** (32 Rubrikseiten) |
| Z | keine doppelte linke Seite | ja |
| Z | alle Anker ASCII-klein `a-z 0-9 -` | ja |
| **U** | jede H12-Zeile ergibt eine Öffnungsadresse **mit** Anker, Katalogeintrag bleibt ankerfrei | **76/76** |
| U | EN-Proxykette hält den Anker **hinter** der Query | **76/76** |
| **F** | die echten Masken: Feld gefunden, `Enabled`, Schlüssel gesetzt | **59/59** in 10 baubaren Masken |
| F | die 3 nicht baubaren Masken auf Typebene belegt | **17/17** |
| F | jede Feldzuordnung belegt (live oder Typebene) | **76/76** |
| F | `btn_Help` jeder gebauten Maske weiterhin aktiv mit richtigem Ziel | ja |

Beispieladressen aus dem Lauf:
```
DE  https://wiki.epos-plan.de/wiki/Programm_Dokumentation/Simulation#erzeuger-und-speicher
EN  https://wiki-epos--plan-de.translate.goog/wiki/Programm_Dokumentation/Simulation
    ?_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en#erzeuger-und-speicher
```

`Form_Kosten`, `Wizard_WPItem` und `Form_Gebaeude` sind im Prüfstand nicht baubar
(`PlatformNotSupportedException: System.Data.OleDb is not supported on this platform` — dem
Prüfprozess fehlt bewusst der OLE-DB-Anbieter, damit er die Produktivdatenbank nicht
anfasst). Für ihre 17 Zeilen wird ersatzweise auf **Typebene** belegt, dass zu jedem
zugeordneten Namen ein gleichnamiges `Control`-Feld existiert — der Designer erzeugt Feld und
`Control.Name` immer im Paar. Protokoll: `dev\h12probe\lauf_gruen.txt`,
Rot-Nachweis `dev\h12probe\lauf_rot.txt`.

### 5.2 Regress der Vorgängerpakete

`dev\build_h7`, `_h9`, `_h10`, `_h11` wurden mit den frischen Dateien aus `dev\build_h12`
überschrieben; die Prüfstände messen damit den **neuen** Stand.

| Prüfstand | ExitCode | OK | FEHL | Ergebnis |
|---|---|---|---|---|
| `h7probe` | 0 | 9 + Sammelzeilen | 0 | `ALLES GRUEN` |
| `h9probe` | 0 | 59 | 0 | `ALLES GRUEN` |
| `h10probe` | 0 | 61 | 0 | `ALLES GRUEN` |
| `h11probe` | 0 | 34 | 0 | `ALLES GRUEN` |

`h7probe` im Einzelnen: **175/175** Zuordnungen lösen auf, keine doppelte linke Seite;
56/72 Masken instanziierbar, 57/73 Knöpfe gefunden, **57 aktiv**, 0 verdecken ein
Bedienelement, Anker 57/57, kein zweiter Knopf bei erneutem Anbringen (56/56). **Die
Feldzuordnungen haben also keinen einzigen Infobutton gebrochen.**

In `h7probe` und `h11probe` wurde je **eine Sollzahl** von 99 auf 175 gezogen — beides
Wegwerf-Prüfstände unter `dev\`, keine Produktivdatei.

---

## 6. Aufgabe 6 — Build

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  C:\Waermeplan\WP_Plan\WP-Plan.sln -t:Rebuild -p:Configuration=Debug -p:Platform=x64 `
  -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_h12\
```

**Vollständiger Rebuild: 0 Fehler.** Warnungen **5** — unverändert dieselben wie vor dem
Paket, alle in nicht berührtem Code:

| Warnung | Fundstelle |
|---|---|
| CS0108 | `Model\WErzeugerModel.cs(6,20)` |
| CS0108 | `Controller\StromverbraucherStammCtrl.cs(25,44)` |
| CS0109 | `Controller\KlimaregionStammCtrl.cs(22,24)` |
| CS0109 | `Controller\KlimaregionStammCtrl.cs(23,48)` |
| CS1998 | `MDIMainForm.cs(489,28)` |

---

## 7. Kodierungsbehandlung je Datei

Vor **und** nach jeder Änderung geprüft (BOM-Bytes, CR-Anteil, Suche nach `U+FFFD`):

| Datei | vorher | nachher | Werkzeug |
|---|---|---|---|
| `Allgemein\Hilfe\HelpCatalog.cs` | UTF-8 +BOM, CRLF | UTF-8 +BOM, 1803/1803 CRLF, 0 × U+FFFD | Edit |
| `Allgemein\Hilfe\help_mapping.txt` | UTF-8 +BOM, CRLF | UTF-8 +BOM, 439/439 CRLF, 0 × U+FFFD | Edit (nicht Write — Write hätte den BOM verloren) |
| `Views\Simulation\Form_Waermesenke.cs` | UTF-8 +BOM, CRLF | UTF-8 +BOM, 2051/2051 CRLF, 0 × U+FFFD, 12 Umlaute unverändert | Edit |

**Keine** der drei Dateien ist CP1252 — das CP1252-Rezept kam nicht zum Einsatz. Der
eingefügte Programmtext ist durchgehend reines ASCII; nur `help_mapping.txt` trägt Umlaute,
und zwar in den bereits vorhandenen Zielnamen.

**Keine `.Designer.cs`, keine `.resx` angefasst** — `git status` zeigt genau drei geänderte
Dateien (§ 9). Kein Git-Schreibkommando ausgeführt.

---

## 8. Offene Prüfpunkte an der laufenden Oberfläche

Maschinell nicht belegbar, beim nächsten Programmlauf mit Blick zu prüfen:

1. **Trefffläche der Bereichshilfe.** Ein Popup erscheint, sobald die Maus die **eigene**
   Fläche des Bereichs berührt — beim Übergang auf ein Kind feuert `MouseLeave` des
   Bereichs. Auf dicht gefüllten Gruppen bleibt als Fläche nur der Rahmen und der Zwischenraum.
   Zu sehen: Fühlt sich das ruhig an, oder flackert das Popup beim Queren einer Gruppe?
   (`Control_MouseLeave` hat eine 500-ms-Verzögerung und prüft, ob der Zeiger im Popup steht —
   das dämpft, ist aber nicht gemessen.)
2. **Angeheftetes Popup und Bedienung.** Klick auf eine GroupBox heftet an; das Popup bleibt
   bis Esc oder Klick daneben. Zu prüfen, dass es nirgends dauerhaft ein Eingabefeld verdeckt.
3. **Die drei nicht baubaren Masken** (`Form_Kosten`, `Wizard_WPItem`, `Form_Gebaeude`):
   Zeigen die 17 Zuordnungen dort wirklich auf das gemeinte Feld?
4. **`Form_Waermesenke`** — die vier neuen Namen wirken erst im laufenden Dialog; im
   Prüfstand 4/4 gefunden und bedienbar, aber die Maske selbst wurde nicht bedient.
5. **Bis zum Wiki-Import** landet jeder Sprung oben auf der Seite. Nach dem Import je Seite
   einmal gegenprüfen, dass die Sprungmarke an der gemeinten Stelle sitzt.
6. **Restdatei `help_mapping.txt` neben der EXE** — liegt dort eine, übersteuert sie je Zeile
   (H1/H2 § 14). Vor der Sichtprüfung ausschließen.

---

## 9. Geänderte Dateien

**Produktiv (3):**

```
WindowsFormsApplication1/Allgemein/Hilfe/HelpCatalog.cs        IstInfobutton/Wirkung, Abschaltsperre
WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt      99 -> 175 Zeilen (76 Feldzuordnungen)
WindowsFormsApplication1/Views/Simulation/Form_Waermesenke.cs  4 Namen fuer die vier Gruppen
```

**Prüfstand (unter `dev\`, gitignored):** `dev/h12probe/` (`h12probe.csproj`, `Probe.cs`,
`lauf_rot.txt`, `lauf_gruen.txt`); je eine Sollzahl in `dev/h7probe/Probe.cs` und
`dev/h11probe/Probe.cs`.

**Nicht angefasst:** `SchemaMigration`, `DbWerte`, `KiKern`, `KiSchreibschutz`, beide
`CLAUDE.md`, sämtliche `.Designer.cs` und `.resx`, `help_cache.json`, `InfoKnopf.cs`,
`HilfeAutomatik.cs`, `DokuUebersetzung.cs`, `Form_HelpPopup.cs`.
Die untrackten `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` und
`KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` stammen aus parallel laufenden Sitzungen.

---

## Nachtrag 29.08.2026 (abends) — Hover nur noch mit eigenem Text

Anwenderbefund (Screenshot `Form_Gebaeude` im Projektassistenten): Die Bereichshilfe
zeigte beim Überfahren der gemappten Flächen (GroupBoxen, Beschriftungen) das Popup
mit dem **Seitentext** des Kapitels — für jede Fläche denselben, da es zu den Ankern
keine eigenen Texte gibt. Ergebnis: Das Popup stand beim Bewegen über den Dialog
„ständig" im Weg. Anwenderregel: *Das Mouseover soll nur erscheinen, wenn dazu auch
ein eigener Text steht.*

Umsetzung in `HelpExtender.Control_MouseEnter` (HelpCatalog.cs), zwei Wächter vor
dem Anzeigen:

1. **Bereichshilfe wird hover-still:** Ist das Steuerelement kein Infobutton und
   zielt der Schlüssel auf einen Anker (`…#…`), erscheint kein flüchtiges Popup —
   einen ankerspezifischen Text kennt der Katalog nicht. Der **Klick** auf die
   Fläche öffnet die Hilfe weiterhin (angeheftetes Popup, `Control_Click`
   unverändert).
2. **Ohne Kurzbeschreibung kein Hover-Popup:** Auch Infobuttons zeigen das
   flüchtige Popup nur, wenn die H11-Kurzbeschreibung der Zielseite vorliegt
   (nicht leer). Vor dem Eintreffen des Nachladelaufs bzw. für Seiten ohne
   Beschreibung bleibt der Hover still; der Klick funktioniert immer.

Wiederbelebung der Bereichs-Hover ist vorgezeichnet: Sobald der Katalog einmal
ankerspezifische Texte führt (Abschnittsauszüge je `{{Anker}}`), kann Wächter 1
auf „Ankertext vorhanden?" umgestellt werden.

Kompilierbeweis: Projektbau nach `dev\build_hover\`, 0 Fehler (29.08.2026).
