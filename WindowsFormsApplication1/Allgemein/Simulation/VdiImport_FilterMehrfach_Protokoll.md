# Suchfilter und Mehrfachauswahl in allen VDI-3805-Importen

Stand 17.08.2026. Nicht committet.

Anwenderanforderung vom 17.08.2026, „beim Laden der VDI-Dateien — für alle Importe aus
VDI-Dateien":

1. **Filter bei der Auswahl ergänzen** — Live-Suchfeld über der Auswahlliste, Groß/Klein egal,
   leeres Feld zeigt alles.
2. **Mehrfachselektion und Laden mehrerer Einträge in einem Vorgang** — mit Statusrückmeldung
   „n von m", je Eintrag saubere Fehlerbehandlung statt Abbruch des Gesamtvorgangs.

Betroffen sind die vier Dialoge unter Administration → Daten & Import:
Wärmepumpe, Solarkollektoren, Pufferspeicher, Heizkessel.

---

## 1. Bestandsanalyse

Alle vier Dialoge sind aus derselben Vorlage entstanden und deshalb strukturgleich:

| Dialog | Liste | Zahlenfilter (Bestand) | Übernahme schreibt nach | Rückfrage bei Duplikat |
|---|---|---|---|---|
| `Form_WP_einlesen` | `ListBox Liste_WP` | `num_LeistungVon/-Bis` | `Tab_WP_STAMM` + Kenndaten (`WPStammCtrl`, `RecordSet`) | Hinweis „Daten bereits eingelesen!", Abbruch |
| `Form_SolarKollektoren_einlesen` | `ListBox Liste_Kollektoren` | `num_AperturVon/-Bis` | `Tab_Solarkollektoren_STAMM` (`SolarkollektorenStammCtrl.InsertFrom`) | Hinweis, Abbruch |
| `Form_PufferSp_einlesen` | `ListBox Liste_PufferSp` | `num_VolumenVon/-Bis` | `Tab_Pufferspeicher_STAMM` (`PufferSpStammCtrl.Exists`/`InsertFrom`) | Hinweis (`MyResource`), Abbruch |
| `Form_Heizkessel_einlesen` | `ListBox Liste_Heizkessel` | `num_LeistungVon/-Bis` | `Tab_Heizkessel_STAMM` (formeigenes `Insert` in Transaktion) | Hinweis, Abbruch |

Gemeinsamer Ablauf im Bestand: `btn_VDI3805_Click` liest die Datei über den jeweiligen
Importer (`ctrl.Import`), `FuelleListe()` baut die `ListBox` aus `ctrl._list` unter dem
Zahlenfilter neu auf, `Liste_WP_SelectedIndexChanged` überträgt den angeklickten Eintrag in
die Detail-Textboxen, `btn_Uebernehmen_Click` schreibt **genau einen** Datensatz.

Drei Befunde, die die Umsetzung geprägt haben:

* **Quelle der Übernahme ist nicht überall dieselbe.** Wärmepumpe und Solarkollektoren lesen
  direkt aus `ctrl._list[index]`; **Pufferspeicher und Heizkessel lesen die Detail-Textboxen**
  (`InitDatensatzUpdate()`), Heizkessel zusätzlich die Felder `szBrennstoffIndex`, `szCO2`,
  `szNOx`, `szCO` aus dem Selektionsereignis. Die Textboxen sind editierbar, ein Anwender kann
  vor dem Speichern also korrigieren. Beides musste erhalten bleiben.
* **Es gibt keine Ja/Nein-Rückfrage.** Ein bereits vorhandener Bezeichner erzeugt nur einen
  Hinweis und bricht ab; überschrieben wird nie. Eine „Für alle"-Mechanik war damit nicht
  anzulegen — beim Mehrfachladen werden Duplikate gezählt und übersprungen.
* **`Liste_WP` (Wärmepumpe) hatte keine Zeilen-Zuordnung.** Der Dialog suchte den Datensatz
  über `Liste_WP.Text` im Namensvergleich, während die drei anderen bereits eine Liste
  `_anzeigeIndex` (Zeile → Index in `ctrl._list`) führen. Bei gleichnamigen Einträgen traf der
  Namensvergleich den falschen Datensatz.

## 2. Umsetzung

### 2.1 Gemeinsame Logik

Neu: `WindowsFormsApplication1\Allgemein\Import\VDI 3805\VdiAuswahlFilter.cs`

* `VdiAuswahlFilter.Passt(suchtext, params felder)` — leerer Suchtext lässt alles durch,
  Vergleich `CurrentCultureIgnoreCase`; mehrere durch Leerzeichen getrennte Begriffe wirken als
  UND (jeder Begriff muss in mindestens einem Feld vorkommen). Gefiltert wird über die in der
  Liste sichtbare Bezeichnung **und** die Firma.
* `VdiAuswahlFilter.LadeMeldung(gespeichert, markiert, übersprungen, fehler)` — baut die
  Statusrückmeldung „n von m Einträgen geladen." und ergänzt die Zeilen
  „Bereits eingelesen (übersprungen): x" bzw. „Fehlgeschlagen: y" nur, wenn es sie gibt.
* `VdiAuswahlFilter.QuellIndizes(markierteZeilen, anzeigeIndex)` — bildet die markierten
  Listenzeilen auf die echten Indizes von `ctrl._list` ab und ignoriert Zeilen außerhalb der
  Zuordnung.
* `enum VdiUebernahmeErgebnis { Gespeichert, Duplikat, Fehler }` — Ergebnis der Übernahme eines
  einzelnen Eintrags.

Die Datei ist UI-frei und damit ohne Formular prüfbar (siehe Abschnitt 3).

### 2.2 Muster je Dialog (identisch in allen vier)

* **Designer/resx:** ein `Label lbl_Filter` („Filter:") und eine `TextBox txt_Filter` in der
  Zeile über der Liste; `txt_Filter.TextChanged += Suchfilter_TextChanged`. Die ListBox wird auf
  `SelectionMode.MultiExtended` gestellt und um die Höhe der neuen Zeile nach unten geschoben,
  ihre Unterkante bleibt unverändert. Lokalisierung über die **formeigene** `.resx`
  (`ApplyResources`-Muster) wie beim Zahlenfilter — dort steht der Text ebenfalls nur in der
  neutralen Variante.
* **`FuelleListe()`:** zusätzlich zum Zahlenfilter der Aufruf von `VdiAuswahlFilter.Passt`.
  Vor dem Neuaufbau werden die markierten Quellindizes gesichert und danach für alle weiterhin
  **sichtbaren** Einträge wiederhergestellt. Markierungen ausgefilterter Einträge werden
  verworfen — bewusst, weil Selektion und Laden auf den sichtbaren Einträgen arbeiten und ein
  unsichtbar mitgeschleppter Datensatz sonst still mitgeschrieben würde. Anschließend werden die
  Detailfelder auf die verbleibende Markierung nachgezogen, damit sie bei Pufferspeicher und
  Heizkessel (Textboxen = Quelle) nie auf einen ausgefilterten Eintrag zeigen.
* **`_listeWirdGefuellt`:** Sperre, damit das programmgesteuerte Wiederherstellen der Markierung
  nicht über `SelectedIndexChanged` die Detailfelder auf einen Zwischenstand setzt.
* **`ZeigeDetails(int i)`:** der Rumpf des bisherigen `SelectedIndexChanged` als eigene Methode,
  damit das Mehrfachladen je Eintrag denselben Weg nimmt wie ein Anwenderklick.
* **`btn_Uebernehmen_Click`:** ermittelt die markierten Einträge.
  Bei **0** die bestehende Meldung „Bitte … selektieren!" (Wärmepumpe: stiller Rücksprung wie
  im Bestand). Bei **1** exakt das Bestandsverhalten inklusive Meldungstexten, Dialogergebnis
  und Schließen — die Detailfelder werden dabei **nicht** neu besetzt, eine Korrektur von Hand
  bleibt also erhalten. Bei **mehr als einem** läuft eine Schleife über `ZeigeDetails(i)` und
  `UebernehmeEintrag(...)`, zählt Erfolg/Duplikat/Fehler, zeigt am Ende
  `VdiAuswahlFilter.LadeMeldung(...)` und schließt den Dialog nur, wenn mindestens ein Datensatz
  geschrieben wurde.
* **`UebernehmeEintrag(...)`:** der unveränderte Bestandsrumpf der Einzelübernahme — dieselben
  SQL-Anweisungen, dieselbe Transaktion, dieselben Controller. Geändert ist nur, dass er das
  Ergebnis zurückgibt statt die MessageBox zu zeigen; über Meldung und Schließen entscheidet der
  Aufrufer. Es gibt damit weiterhin genau **einen** Schreibpfad in jede STAMM-Tabelle.

### 2.3 Dateien und Stellen

Zeilenangaben sind die Anfangszeilen der jeweiligen Methode bzw. Anweisung.

| Datei | Stellen |
|---|---|
| `Allgemein\Import\VDI 3805\VdiAuswahlFilter.cs` | neu (`Passt`, `LadeMeldung`, `QuellIndizes`, `enum VdiUebernahmeErgebnis`) |
| `Views\Wärmepumpe\Form_WP_einlesen.cs` | 21/26 Felder, 62 `SelectedIndexChanged`, 72 `ZeigeDetails`, 94 `Suchfilter_TextChanged`, 99 `FuelleListe`, 138 `MarkierteQuellIndizes`, 143 `btn_Uebernehmen_Click`, 201 `UebernehmeEintrag` |
| `Views\Wärmepumpe\Form_WP_einlesen.designer.cs` | 64-65 Instanzen, 93 `SelectionMode`, 286-299 Control-Blöcke, 336-337 `Controls.Add`, 380-381 Felder |
| `Views\Wärmepumpe\Form_WP_einlesen.resx` | `Liste_WP.Location/Size`, Blöcke `lbl_Filter`/`txt_Filter` |
| `Views\Solarthermie\Form_SolarKollektoren_einlesen.cs` | 13/18 Felder, 54 `SelectedIndexChanged`, 64 `ZeigeDetails`, 86 `Suchfilter_TextChanged`, 91 `FuelleListe`, 131 `MarkierteQuellIndizes`, 136 `btn_Uebernehmen_Click`, 219 `UebernehmeEintrag`, 245 `InitDatensatzUpdate(int index)` |
| `Views\Solarthermie\Form_SolarKollektoren_einlesen.designer.cs` | 67-68, 97, 332-345, 367-368, 416-417 |
| `Views\Solarthermie\Form_SolarKollektoren_einlesen.resx` | `Liste_Kollektoren.Location/Size`, neue Blöcke |
| `Views\Pufferspeicher\Form_PufferSp_einlesen.cs` | 13/18 Felder, 56 `SelectedIndexChanged`, 66 `ZeigeDetails`, 83 `Suchfilter_TextChanged`, 88 `FuelleListe`, 127 `MarkierteQuellIndizes`, 132 `btn_Uebernehmen_Click`, 219 `UebernehmeEintrag` |
| `Views\Pufferspeicher\Form_PufferSp_einlesen.designer.cs` | 52-53, 81, 198-211, 236-237, 268-269 |
| `Views\Pufferspeicher\Form_PufferSp_einlesen.resx` | `Liste_PufferSp.Location/Size`, neue Blöcke |
| `Views\Heizkessel\Form_Heizkessel_einlesen.cs` | 20/25 Felder, 39 `Suchfilter_TextChanged`, 46 `FuelleListe`, 86 `MarkierteQuellIndizes`, 124 `SelectedIndexChanged`, 133 `ZeigeDetails`, 150 `btn_Uebernehmen_Click`, 226 `UebernehmeEintrag` |
| `Views\Heizkessel\Form_Heizkessel_einlesen.designer.cs` | 57-58, 86, 241-254, 284-285, 321-322 |
| `Views\Heizkessel\Form_Heizkessel_einlesen.resx` | `Liste_Heizkessel.Location/Size`, neue Blöcke |

Nicht angefasst: `MyResource\Resource.resx`, `MyResource\Resource.Designer.cs`, die
`.de-DE`/`.en-US`-Varianten der vier Formulare (Vorbild ist der Zahlenfilter, dessen Texte
ebenfalls nur in der neutralen `.resx` stehen), sämtliche Importer und Controller.

Zwei benannte Abweichungen vom reinen „nur ergänzen":

* `Form_PufferSp_einlesen`: der lokale Stamm-Controller heißt jetzt `pspctrl`. Im Bestand hieß er
  `ctrl` und verdeckte damit das Feld `ctrl` (der Importer) — in der herausgezogenen Methode wird
  beides gebraucht.
* `Form_WP_einlesen`: der Dialog führt jetzt wie die drei anderen `_anzeigeIndex` statt des
  Namensvergleichs über `Liste_WP.Text`. `RecordSet` bleibt im herausgezogenen Bestandsrumpf
  stehen (Bestandsaufruf); neuer Code verwendet es nicht.

## 3. Verifikation

### 3.1 Build

`MSBuild WindowsFormsApplication1.csproj -p:Configuration=Debug -p:Platform=x86` —
**0 Fehler, 6 Warnungen** (Bestandswarnungen, unverändert zur Baseline). Dieser Build lief um
17:48 über den Endstand aller hier beschriebenen Dateien (letzte Schreibzeit 17:48:22) und ist
die Grundlage des Smoke-Laufs um 17:49.

Ein späterer Build-Versuch um 17:54 scheiterte an `Allgemein\Update\SchemaMigration.cs(446)`
(`CS0103: Schritt_14_Parallelverbund`). Die Datei gehört zu einem parallel laufenden Vorgang,
wurde um 17:54:35 fremd geändert und ist hier tabu — der Fehler betrifft keine der oben
genannten Dateien.

### 3.2 Headless-Smoke

Im Repo und unter `C:\Waermeplan` bzw. `C:\ProgramData\EPOS_PLAN` liegt **keine
VDI-Beispieldatei** (`**/*.vdi` ohne Treffer). Der Smoke setzt deshalb an der Stelle an, an der
ein echter Dateiimport endet: Wegwerf-Harness `%TEMP%\wpk8` (`VdiImportHarness.csproj`,
ProjectReference auf das App-Projekt, x86, Muster wie `%TEMP%\wpk5`) besetzt `ctrl._list` per
Reflection mit sechs synthetischen Einträgen und fährt danach genau den Anwenderweg:
`FuelleListe` → Filtertext → Markierung → `btn_Uebernehmen.PerformClick()`. MessageBoxen werden
vom `DialogWaechter` (aus `Referenzlauf`) weggeklickt und protokolliert. Geschrieben wird
ausschließlich in die DB-Arbeitskopie `%TEMP%\wpk8\db\Kenndaten.accdb`; der Lauf bricht ab, wenn
`DataRepository.GetDBPath()` nicht darauf zeigt.

Testdaten: `HRN Alpha Kompakt 6` / `HRN Alpha Kompakt 8` (Viessmann),
`HRN Alpha Split 12` / `HRN Beta Kompakt 6` (Vaillant), `HRN Beta Split 20` /
`HRN Gamma Kompakt 10` (Bosch).

Ergebnis (identisch für alle vier Dialoge):

| Prüfung | Ergebnis |
|---|---|
| `SelectionMode` | `MultiExtended` |
| Filterfeld innerhalb des Formulars, oberhalb der Liste | ja |
| Filter `''` | 6 Zeilen |
| Filter `alpha` / `ALPHA` | 3 Zeilen (Groß/Klein egal) |
| Filter `viessmann` | 2 Zeilen (Treffer über die Firma) |
| Filter `alpha split` | 1 Zeile (UND über zwei Begriffe) |
| Filter `xyz` | 0 Zeilen |
| zwei markiert, dann auf `alpha` gefiltert | 1 Markierung bleibt, die unsichtbare ist verworfen |
| Detailfeld nach dem Umfiltern | folgt der verbleibenden Markierung (`HRN Alpha Kompakt 6`) |
| 3 markiert → Übernehmen | STAMM-Tabelle **+3**, Meldung „3 von 3 Einträgen geladen." |
| dieselben 3 ein zweites Mal | STAMM-Tabelle **+0**, Meldung „0 von 3 Einträgen geladen. / Bereits eingelesen (übersprungen): 3" |
| 1 markiert → Übernehmen | STAMM-Tabelle **+1**, Bestandsmeldung unverändert |

Zeilenzahlen der Arbeitskopie: `Tab_WP_STAMM` 51 → 54 → 54 → 55,
`Tab_Solarkollektoren_STAMM` 7 → 10 → 10 → 11, `Tab_Pufferspeicher_STAMM` 4 → 7 → 7 → 8,
`Tab_Heizkessel_STAMM` 21 → 24 → 24 → 25.

Meldungstexte der Einzelübernahme, vom Dialogwächter mitgelesen und damit als unverändert
belegt: „Daten gespeichert!" (Wärmepumpe), „Datensatz gespeichert" (Solarkollektoren,
Pufferspeicher über `MyResource`), „Datensatz erfolgreich neu angelegt." (Heizkessel).

Reine Logikprüfung von `VdiAuswahlFilter.Passt` (leer, `null`, Treffer Name, Treffer Firma,
Groß/Klein, UND-Verknüpfung, Fehltreffer) und `LadeMeldung` (3/3, 1/3 mit Duplikat und Fehler,
0/3): alle bestanden.

Die Produktiv-DB `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` wurde nur kopiert; ihr
Änderungszeitstempel (17:41:32) liegt vor beiden Harness-Läufen (17:47:32 und 17:49:44), eine
Sperrdatei ist nicht zurückgeblieben.

### 3.3 Encoding

Je Datei vorher gemessen und nachher nachgewiesen; das Repo ist gemischt:

| Datei | Encoding | Nachweis nach dem Edit |
|---|---|---|
| `Form_WP_einlesen.cs` | kein BOM, CRLF, rein ASCII | unverändert, 0 High-Bytes |
| `Form_WP_einlesen.designer.cs` | kein BOM, CRLF, cp1252 | unverändert (`F6 FC FC E4`) |
| `Form_WP_einlesen.resx` | BOM, CRLF, UTF-8 | unverändert, XML lädt |
| `Form_SolarKollektoren_einlesen.cs` | kein BOM, CRLF, cp1252 | unverändert (`E4 E4 DC E4 E4 E4 E4`) |
| `Form_SolarKollektoren_einlesen.designer.cs` | kein BOM, CRLF, cp1252 | unverändert |
| `Form_SolarKollektoren_einlesen.resx` | BOM, CRLF, UTF-8 | unverändert, XML lädt |
| `Form_PufferSp_einlesen.cs` | BOM, CRLF, UTF-8 | unverändert (`EF BB BF … C3 9C`) |
| `Form_PufferSp_einlesen.designer.cs` | BOM, CRLF, UTF-8 | unverändert |
| `Form_PufferSp_einlesen.resx` | BOM, CRLF, UTF-8 | unverändert, XML lädt |
| `Form_Heizkessel_einlesen.cs` | kein BOM, CRLF, cp1252 | unverändert (`FC F6 FC DC DC D6`) |
| `Form_Heizkessel_einlesen.designer.cs` | kein BOM, CRLF, cp1252 | unverändert |
| `Form_Heizkessel_einlesen.resx` | BOM, CRLF, UTF-8 | unverändert, XML lädt |
| `VdiAuswahlFilter.cs` (neu) | BOM, CRLF, UTF-8 | wie die Nachbardateien im Importer-Ordner |

Weil die vier Formular-Dateien in zwei verschiedenen Encodings vorliegen, stehen alle neuen
Kommentare dort **ohne Umlaute** (Hausstil des Heizkessel-Dialogs: „Fuellt", „Beruecksichtigung").
Die einzigen neuen sichtbaren Texte mit Umlauten liegen in der neuen, eindeutig
UTF-8-kodierten `VdiAuswahlFilter.cs` bzw. als `lbl_Filter.Text` in den `.resx`-Dateien.

## 4. Offene Punkte

* **Sammelmeldung nicht lokalisiert.** `VdiAuswahlFilter.LadeMeldung` liefert festen deutschen
  Text — das ist das Bestandsmuster von drei der vier Dialoge (dort sind alle MessageBox-Texte
  hart kodiert). Der Pufferspeicher-Dialog bezieht seine übrigen Meldungen dagegen aus
  `MyResource.Resource`; dort sollte die Sammelmeldung nachgezogen werden, sobald
  `MyResource\Resource.resx` wieder frei ist (aktuell parallel in Arbeit und tabu).
* **`lbl_Filter.Text` nur neutral.** Wie beim Zahlenfilter fehlt der Eintrag in den
  `.de-DE`/`.en-US`-Varianten; englische Oberfläche zeigt „Filter:".
* **Kein Test mit echter VDI-Datei.** Der Weg `btn_VDI3805_Click` → `ctrl.Import(datei)` ist
  unverändert, aber ohne Beispieldatei nicht gefahren. Sobald eine `.vdi` vorliegt, sollte der
  Import einmal von der Datei aus durchlaufen werden.
* **Bestandsbefund Heizkessel (nicht angefasst):** `szBrennstoffart` wird nirgends gesetzt,
  bleibt also immer leer. `InitDatensatzUpdate` legt den Wirkungsgrad deshalb stets auf
  `Wirkungsgrad_Gas`, auch bei Ölkesseln.
* **Bestandsbefund Wärmepumpe (nicht angefasst):** scheitert `InsertKenndatenStamm` mitten in
  der Kennlinienschleife, bleibt der bereits geschriebene STAMM-Satz ohne vollständige
  Kennlinien stehen — es gibt an dieser Stelle keine Transaktion. Beim Mehrfachladen zählt so
  ein Eintrag als „fehlgeschlagen".
