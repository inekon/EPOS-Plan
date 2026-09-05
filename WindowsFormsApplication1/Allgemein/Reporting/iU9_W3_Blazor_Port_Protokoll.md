# iU9 Welle 3 — Port der Energieträger-Kleindialoge (Umsetzungsprotokoll)

> Muster: [`iU9_W2_Blazor_Port_Protokoll.md`](iU9_W2_Blazor_Port_Protokoll.md) und
> [`iU9_W1_Blazor_Port_Protokoll.md`](iU9_W1_Blazor_Port_Protokoll.md) — Feldkarten-Abgleich
> je Maske, Abweichungsliste A‑n, Entscheidungen, Windows-Abnahmepunkte.
>
> Basis `95cf8be` (Branch `ios_migration`), Arbeitsstand 03.09.2026.
> Plan: Wellenplan iU9, Abschnitt C Zeile W3, E Priorität 4–6 und 12, F, G (R2/R8/R9).

---

## 1. Auftrag und Ergebnis

**Vier WinForms-Masken → vier Razor-Komponenten**, jede WinForms-Fassung im selben Schritt
gelöscht (Regel M1). Dazu **zwei neue Bausteine**, **drei erweiterte Standards**, **vier
Hüllen mit Datenseite** und **eine neue Methode im Kern-Renderer** — die erste Erweiterung
des `ChartRenderer` seit seiner Portierung nach SkiaSharp.

Alle vier Masken hängen am Energieträger: `Form_Energietraeger` öffnet zwei davon direkt,
`ucFuelSettings` (in `Form_Energietraeger` gehostet) die anderen beiden.

| # | Maske (Zeilen) | Komponente | Hülle | Aufrufer nach dem Umbau |
|---|---|---|---|---|
| W3.1 | `Form_LeistungspreisReihe` (167 + 431) | `EPOS.UI/Dialoge/Kosten/LeistungspreisReiheDialog.razor` | `Views/Kosten/LeistungspreisReiheHuelle.cs` | `Views/Kosten/ucFuelSettings.cs:218` (`btnSaisonSaetze`) |
| W3.2 | `Form_SpotpreisImport` (219 + 184) | `EPOS.UI/Dialoge/Kosten/SpotpreisImportDialog.razor` | `Views/Kosten/SpotpreisImportHuelle.cs` | `Views/Kosten/Form_Energietraeger.cs:250` (Karte „Spotmarktpreise") |
| W3.3 | `Form_Emissionskatalog` (767 + 346) | `EPOS.UI/Dialoge/Kosten/EmissionskatalogDialog.razor` | `Views/Kosten/EmissionskatalogHuelle.cs` | `ucFuelSettings.cs:1421` (Rückgabemodus), `:1441` (Verwaltungsmodus) |
| W3.4 | `Form_Kostenprofil` (483 + 296 + `.resx`) | `EPOS.UI/Dialoge/Kosten/KostenprofilDialog.razor` | `Views/Kosten/KostenprofilHuelle.cs` | `Form_Energietraeger.cs:232` (Karte „Kostenprofil") |

**Commits** (ein Commit je Nummer, Reihenfolge des Plans):

```
afd599d  iU9-W3.0  Bausteine: Dateiwahl, Textfeld mehrzeilig, Zeilenwahl, Raster bearbeitbar
624ce28  iU9-W3.1  LeistungspreisReiheDialog + Huelle
b2a9511  iU9-W3.2  SpotpreisImportDialog + Daten + Huelle
15417a8  iU9-W3.3  EmissionskatalogDialog + Daten + Huelle
cb700f0  iU9-W3.4  KostenprofilDialog + Huelle + ChartRenderer.Kostenprofil
5a25c1d  iU9-W3.5  Ressourcen-Sammelnachtrag (67 Schluessel, de + en + Designer)
4ea688c  iU9-W3.6  Formularkarte-Tests (fuenftes Pruefmuster, neue Zaehler)
(dieses Protokoll)  iU9-W3.7
```

---

## 2. Bauweise

### 2.1 Die zwei neuen Bausteine (W3.0)

**`EPOS.UI/Standards/Dateiwahl.razor`** (Bausteinlücke 4). Pfadfeld und Knopf
„Durchsuchen…" — die Zeile, die sechs Importmasken des Bestands wortgleich bauen. Die
Komponente **öffnet nichts**: Was ein Dateiwähler ist, weiß nur die Plattform. Er kommt als
`Func<string, Task<string?>> Waehlen` herein, unter Windows aus `Dienste.Datei.DateiOeffnen`.
Ohne Delegat bleibt der Knopf weg — dieselbe Regel wie A‑18 aus Welle 2. Das Pfadfeld ist
wie im Bestand nur lesbar (`_tbPfad.ReadOnly = true`).

**`EPOS.UI/Bausteine/Zeilenwahl.razor`** (Bausteinlücke 6, zweiter Teil). Der runde
Wahlknopf einer Rasterzeile. Er stand bisher **zweimal wortgleich im Markup** — im Dialog
„BHKW-Wirtschaftlichkeit" (A‑6 aus B5b) und im Kostenfaktor-Katalog (A‑14 aus W1); mit dem
Emissionskatalog wäre er zum vierten Mal entstanden. Optik unverändert
(`epos-anlagenwahl`, 44 px rund), gewählt zusätzlich `epos-knopf--primaer` und
`aria-pressed="true"`.

### 2.2 Die drei erweiterten Standards

| Standard | Neu | Wozu |
|---|---|---|
| `Textfeld` | `Mehrzeilig`, `Zeilen`, `NurLesen`, `Festbreite` | die MultiLine-`TextBox` des Bestands. `NurLesen` lässt den Inhalt markierbar (anders als ein gesperrtes Feld) — genau das braucht ein Protokoll; `Festbreite` gibt ihm die Schreibmaschinenschrift, in der `_tbProtokoll` seine Spalten baute (Consolas 9 pt). `Festbreite` ist in W3.2 nachgetragen, weil erst die Maske den Bedarf zeigte |
| `Raster` | `Bearbeitbar` | Bedienelemente in Zellen stehen in `TemplateColumn`s (QuickGrid kann es nicht selbst). Die Klasse nimmt der Zelle nur die senkrechte Polsterung, damit ein 44‑px‑Feld die Zeile nicht auf 60 px treibt |
| CSS | `epos-zahlenraster` | Feld an Feld in mitwachsenden Spalten — die Wertegitter der beiden Preismasken (12 Monatssätze, 12 Monats- und 24 Stundenwerte). In WinForms waren das gerechnete Pixelspalten (`m / 6` und `m % 6`) |

Dazu `epos-dateiwahl`, `epos-eingabe--mehrzeilig`, `epos-eingabe--festbreite`,
`epos-raster--bearbeitbar`, `epos-editorblock` und `epos-wert-geltend`.

### 2.3 Der neue Chart im Kern (W3.4)

`ChartRenderer.Kostenprofil(titel, stundenwerte, einheit, achseMonat)` samt Palettenfarbe
`C_PROFIL` — die **erste** Erweiterung des Renderers seit der SkiaSharp-Portierung (iU7).

| Merkmal | Vorläufer (`Form_Kostenprofil.ChartKonfigurieren`) | Renderer |
|---|---|---|
| Bildmaß | Chart 648 × 390 | **1296 × 780** = doppelte Zielauflösung, wie alle Bilder der Datei |
| Linienfarbe | `Color.FromArgb(180, Color.DarkGreen)`, Stärke 2 | `C_PROFIL` = `SKColor(0x00,0x64,0x00,180)`, Stärke 2 |
| x‑Achse | 0…12, `Interval = 1`, gepunktetes Raster | dasselbe, dazu der Achsenname rechts |
| y‑Achse | Automatik | **vorzeichenfähige** Skala mit „schönen" Stufen wie beim Kapitalwert-Verlauf, Nulllinie gestrichelt |
| Punkte | 8 760, `x = i * 12 / 8760` | dieselbe Abbildung, jeder n‑te Wert (mehr Punkte als Pixel zeichnen dasselbe Bild langsamer) |

**Warum vorzeichenfähig.** Ein Wochenwert ist eine *Abweichung* und darf den Monatswert
unter null ziehen. Eine bei 0 beginnende Achse hätte die Linie abgeschnitten.

### 2.4 Die vier Hüllen

Muster durchweg `BhkwWirtschaftlichkeitHuelle` bzw. `KapitalwertVerlaufHuelle`: laden mit
denselben Controllern und in derselben Reihenfolge wie zuvor der Maskenkonstruktor,
schreiben über Rückrufe, lassen Langläufer auf einem eigenen Faden laufen.

| Hülle | Lädt / rechnet | Delegaten |
|---|---|---|
| `LeistungspreisReiheHuelle` | `PreisreiheCtrl.ReadTraegerReihe` + `ReadWerte`; entscheidet die **Ebene** (Projekt- oder Stammreihe) | `Uebernehmen` (Jahr, 12 Werte), `Loeschen` |
| `SpotpreisImportHuelle` | `SpotpreisImportCtrl.Pruefe` und `Speichere`, beide in `Task.Run`; hält den geprüften `Lauf` zwischen den Schritten | `Waehlen` (→ `Dienste.Datei`), `Pruefen`, `Speichern` (mit Fortschrittsmelder) |
| `EmissionskatalogHuelle` | `EmissionskatalogCtrl` (9 Methoden) und `EmissionenCtrl.VorgabeLesen`/`-Schreiben`; wandelt Fachmodell ↔ Anzeigezeile | `ArtenLaden`, `WerteLaden`, `AuswahlSetzen`, `ArtAnlegen`/`-Aendern`/`-Loeschen`, `WertAnlegen`/`-Aendern`/`-Loeschen`/`-Uebernehmen`, `Rueckfrage` |
| `KostenprofilHuelle` | `KostenprofilCtrl`, das `";"`-Ablageformat, `PreisModell.AusMonatsUndWochenwerten` + `ChartRenderer.Kostenprofil` in `Task.Run` | `Vorschau`, `Speichern` |

**Kein neuer Controller, keine neue SQL-Zeile.** Alle vier Masken kannten schon vorher keine
SQL-Anweisung — die Datenseite lag in `PreisreiheCtrl`, `SpotpreisImportCtrl`,
`EmissionskatalogCtrl`/`EmissionenCtrl` und `KostenprofilCtrl`, alle vier seit Paket iU4 im
Kern. Der SQL-Dialektprüfer bestätigt es: 1 303 Texte wie vorher, 0 Fundstellen.

### 2.5 Die beiden Untereditoren des Emissionskatalogs

`Form_Emissionskatalog` baute für „Neu…" und „Bearbeiten…" je ein `Form` **zur Laufzeit** —
zwei kleine Fenster über dem Dialog. Ein zweites Blazor-Fenster über dem ersten wäre eine
zweite WebView (Risiko R2 des Wellenplans), und den Baustein `Ueberlagerung` gibt es erst in
Welle 4. Beide Editoren sind deshalb **eingerückte Blöcke** im selben Fenster
(`epos-editorblock`) — dieselbe Entscheidung wie A‑13 in Welle 1, nur eine Stufe größer.
Solange ein Block steht, ruht der Rest des Dialogs (`EditorOffen`), und Esc schließt zuerst
ihn.

---

## 3. Feldkarten-Abgleich

Der Abgleich ist **als Test ausgeführt** (`EPOS.UI.Tests/Dialoge/*Tests.cs`), nicht als
einmalige Messung: Je Dialog prüft ein Test den Feldbestand, ein zweiter die
Beschriftungen. Fällt ein Feld weg, wird der Test rot. Die Karten wurden vor Beginn frisch
gezogen (`Werkzeuge/Formularkarte`, Stand `95cf8be`).

| Maske | Soll (Feldkarte) | Ist (Komponente) | Deckung |
|---|---|---|---|
| `Form_LeistungspreisReihe` | 20 Zeilen: Kontext, Einheit, Hinweis, Jahr (2000…2100), 12 Monatssätze (0…100 000, 2 Nachkommastellen), 3 Knöpfe, Kopftitel | `Ganzzahlfeld` · 12 `Zahlenfeld` im `epos-zahlenraster` · `Kontextzeile` · `Herleitungszeile` · 3 Knöpfe · `InfoKnopf` | **20/20** |
| `Form_SpotpreisImport` | 9 Zeilen: Info, Pfad + Wählknopf, Bezeichnung, Stammschalter, Protokoll, Status, Übernehmen, Abbrechen | `Herleitungszeile` · `Dateiwahl` · `Textfeld` · `Schalter` · `Textfeld` mehrzeilig/nur lesend/festbreit · Status als `Kohaerenzzeile`/`Warnbanner` · 2 Knöpfe | **9/9** |
| `Form_Emissionskatalog` | 17 Zeilen: Kopftitel, Kontext, Modusgruppe (2 Optionen + Ortsvermerk), Artenraster + 3 Knöpfe, Werteraster + 4 Knöpfe, Hinweis, OK, Abbrechen | `Optionsgruppe` · 2 `Raster` (bearbeitbar, mit `Zeilenwahl`) · 7 Knöpfe · 2 `Herleitungszeile` · `SpeichernLeiste`-Ersatz | **17/17** + die beiden Editoren |
| `Form_Kostenprofil` | 14 Kartenzeilen **+ 36 Laufzeitfelder** | Bezeichner · 12 + 24 `Zahlenfeld` · `Auswahlfeld` Wochentag · 7 Knöpfe · `ChartBild` · 2 `Herleitungszeile` | **14/14 + 36** |

**Die 36 Laufzeitfelder von `Form_Kostenprofil`** stehen in keiner Feldkarte — der Designer
kennt sie nicht, sie entstanden in `MonatsRasterBauen` und `StundenRasterBauen` (Regel F1
verlangt, sie von Hand nachzutragen):

| Feld im Vorläufer | Herkunft | Ziel in der Komponente |
|---|---|---|
| `_tbMonat[0…11]` | `MonatsRasterBauen`, Vorgabe 25,0 ct/kWh | 12 `Zahlenfeld` mit Monatsnamen aus der Kultur |
| `_tbStunde[0…23]` | `StundenRasterBauen`, Vorgabe 0,0 | 24 `Zahlenfeld` „1." … „24." des gewählten Wochentags |

**Die beiden Untereditoren von `Form_Emissionskatalog`** stehen ebenfalls in keiner Karte
(zur Laufzeit gebaute `Form`s):

| Feld im Vorläufer | Ziel |
|---|---|
| Arteneditor: Kürzel, Name, Einheit, GWP, Quelle + Pflichthinweis | `epos-editorblock` mit `Textfeld` ×3, `Auswahlfeld`, `Zahlenfeld`, `Herleitungszeile` |
| Werteeditor: Bezeichnung, Wert + Einheit, „bereits CO₂e?", „Vorlage für alle Träger" | `epos-editorblock` mit `Textfeld`, `Zahlenfeld`, `Schalter` ×2 |

**Kein Feld einer Karte fehlt.**

---

## 4. Abweichungen (mit Begründung)

| # | Abweichung | Begründung |
|---|---|---|
| **A‑1** | W3.1: Die Meldung „alle zwölf Sätze sind 0" ist ein **Hinweisbanner** statt einer MessageBox | Hausregel `EPOS.UI/CLAUDE.md`; der Text ist derselbe (`KDLG_LPR_ALLES_NULL`) |
| **A‑2** | W3.1: Ein gescheitertes Speichern oder Löschen zeigt **zusätzlich** ein Fehlerbanner | `PreisreiheCtrl` meldet über `DataRepository.FehlerMelden` weiter selbst; ohne die zweite Zeile bliebe der Dialog danach wortlos stehen — der Vorläufer schloss in diesem Fall einfach nicht |
| **A‑3** | W3.1/W3.4: Ein geleertes Zahlenfeld **behält** seinen geladenen Wert | A‑7 aus Welle 2. Eine `NumericUpDown` konnte nicht leer sein, ein Eingabefeld schon; so kann ein versehentlich geleertes Feld keine Null in die Datenbank tragen |
| **A‑4** | W3.1: Die zwölf Felder stehen in einem **mitwachsenden Gitter** statt in zwei gerechneten Pixelspalten | `epos-zahlenraster`: Unter 320 px steht eine Spalte, sonst so viele wie passen (M2). Die Reihenfolge Januar…Dezember bleibt |
| **A‑5** | Alle: Eine ungültige Zahl **färbt** das Feld, statt zu melden | Hausregel; übernommen aus W1 (dort A‑8) |
| **A‑6** | W3.2: Prüfen und Schreiben laufen auf einem **eigenen Faden** (`Task.Run`) | Der Vorläufer setzte den Sanduhrzeiger und blockierte den Oberflächenfaden; in einer WebView stünde damit auch der Dialog still. Der Ablauf bleibt derselbe — erst prüfen, dann und nur bei Erfolg schreiben. Während des Laufs sperren Dateiwahl, Schalter und „Übernehmen" |
| **A‑7** | W3.2: Der Dateiwähler kommt aus **`Dienste.Datei`** statt aus einem eigenen `OpenFileDialog` | Bausteinregel (`Dateiwahl`). Folge: Er erscheint ohne ausdrückliches Besitzerfenster (`WindowsDateiDienst` ruft `ShowDialog()` ohne Eltern) — Windows-Abnahmepunkt W3‑6 |
| **A‑8** | W3.2: Die Statuszeile ist grün eine **`Kohaerenzzeile`**, rot ein **Warnbanner** | Der Vorläufer färbte ein Label (`DarkGreen`/`Firebrick`). Die Texte sind dieselben; die Unterscheidung liegt jetzt nicht mehr allein in der Farbe, sondern auch im Zeichen (✓ bzw. ⚠) — das trägt auch im Hochkontrastmodus |
| **A‑9** | W3.2: Das Protokollfeld trägt `Festbreite` | `_tbProtokoll` stand in Consolas 9 pt; ein Protokoll baut seine Spalten aus Leerzeichen |
| **A‑10** | W3.3: Die beiden Untereditoren sind **eingerückte Blöcke** im selben Fenster | Risiko R2 (zweite WebView), Baustein `Ueberlagerung` erst in Welle 4. Siehe § 2.5 |
| **A‑11** | W3.3: Beide Listen haben eine **Wahlspalte** (`Zeilenwahl`) | Ein `Raster` markiert die Zeile nicht selbst; dasselbe Muster wie A‑6 aus B5b und A‑14 aus W1 |
| **A‑12** | W3.3: Das Auswahlhäkchen sitzt als **`Schalter` in der Zelle** (`Raster.Bearbeitbar`) | Ersatz für die `DataGridViewCheckBoxColumn`. Die Sperre der Pflichtart bleibt (`Aktiv="false"`), die Sofortwirkung des Häkchens ebenfalls — `CurrentCellDirtyStateChanged` brauchte es dafür; ein `Schalter` meldet ohnehin sofort |
| **A‑13** | W3.3: Alle vier MessageBox-Stellen werden **Banner**; die zwei echten Ja/Nein-Rückfragen bleiben ein modaler Dialog über `Dienste.Dialog` | Hausregel; einen `Rueckfrage`-Baustein gibt es bis Welle 4 nicht (Bausteinlücke 8, wie A‑16 in W1) |
| **A‑14** | W3.3: **Enter ist nicht belegt** — der Vorläufer hatte `AcceptButton = btnOk` | In einem Dialog, dessen Knöpfe fast alle sofort schreiben, wäre ein versehentliches Enter kein Bestätigen, sondern ein Zufall (A‑7 aus B5b) |
| **A‑15** | W3.3: Der Kurztext „ausgelieferter Katalogwert" hängt an **jeder unveränderlichen Zeile** | Der Vorläufer setzte ihn über eine zweite, gleichbedeutende Bedingung; die Regel `DarfAendern` ist dieselbe und steht jetzt an einer Stelle |
| **A‑16** | W3.3: **Abbrechen trägt die beiden Änderungsmerker mit** | **Befund.** `ucFuelSettings.KatalogFuerZeile` las `ArtenGeaendert`/`WerteGeaendert` schon in der WinForms-Fassung **unabhängig vom `DialogResult`** — was geschrieben wurde, ist geschrieben. Ein `null`-Ergebnis bei Abbrechen hätte diese Information verloren. Der Modus-Schalter geht dagegen nur über OK mit; auch das ist der Bestand (`Beenden` lief nur dort) |
| **A‑17** | W3.4: **Drei Abschnitte statt drei Reitern** | Einen Reiter-Baustein gibt es erst in Welle 5 (Bausteinlücke 10). Der Vorläufer zeichnete die Vorschau ohnehin bei jedem Reiterwechsel neu; hier steht dafür ein Knopf „Vorschau aktualisieren", und „Tag einfügen", „Für alle Tage" und „Stundenwerte übernehmen" zeichnen wie bisher mit |
| **A‑18** | W3.4: Die drei „ungültig"-Meldungen (Januar, Monat, Stunde) **entfallen** | Ein `Zahlenfeld` färbt statt zu melden (A‑5) und behält beim Leeren seinen Wert (A‑3) — ein ungültiger Wert erreicht den Speicherweg gar nicht mehr. Die drei Ressourcenschlüssel bleiben stehen; sie werden nur nicht mehr gebraucht |
| **A‑19** | W3.4: „Stundenwerte für diesen Tag übernehmen" **zeichnet nur noch die Vorschau neu** | Die Werte stehen bereits in der Wochenmatrix: Jedes `Zahlenfeld` meldet seine Eingabe sofort. Der Vorläufer brauchte den Knopf, weil er die Textfelder erst beim Tagwechsel auslas |
| **A‑20** | W3.4: Die Vorschau rechnet auf einem **eigenen Faden**; das **Zoomen entfällt** | 8 760 Stützstellen rechnen und zeichnen würde die WebView sonst anhalten. `CursorX.IsUserEnabled` und `ScaleView.Zoomable` des WinForms-Chart haben in einem PNG kein Gegenstück — ein zoombares Diagramm braucht eine JS-Schicht (offener Punkt W3‑O2) |
| **A‑21** | W3.1/W3.2/W3.4: **Enter unbelegt, Esc schließt** | Wo ein Knopf sofort schreibt, ist ein versehentliches Enter kein Bestätigen (A‑7 aus B5b). In W3.3 kommt dazu, dass Esc zuerst einen offenen Editorblock schließt |
| **A‑22** | W3.3: Der Spaltenkurztext „Ausgewählte Arten erscheinen als Feld…" steht jetzt als **Herleitungszeile unter der Liste** | Ein `Raster` hat keinen Spaltenkurztext. Der Satz geht damit nicht verloren; der Spaltenkopf heißt „im Tab" (`EMK_SP_AUSWAHL_KOPF`) |

**Ein Befund am Rand:** `Form_Emissionskatalog` rief für seinen OK-Knopf
`T("KDLG_BTN_OK", "OK")` — den Schlüssel gibt es im Bestand **nicht**, gezeigt wurde immer
der deutsche Rückfall. Der Dialog nimmt jetzt den Haustext `ALLG_BTN_OK`.

---

## 5. Texte

**67 neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` und —
von Hand, weil hier kein Visual Studio läuft — `Resource.Designer.cs` (alphabetisch zwischen
den Nachbarn, im Muster der erzeugten Datei; die Änderung ist in allen drei Dateien rein
additiv):

| Präfix | Zahl | Dialog |
|---|---|---|
| `LPR_*` | 3 | Leistungspreis-Reihe (Gitterüberschrift, zwei Fehlermeldungen) |
| `SPOT_*` | 1 | Spotpreis-Import (Prüfstatus) |
| `EMK_*` | 60 | Emissionsfaktor-Katalog — **vollständig** |
| `KPROF_*` | 3 | Kostenprofil (Vorschauknopf, Bildtext, Platzhalter) |

**Befund zum Emissionskatalog.** Er war die einzige der vier Masken **ohne einen einzigen
eigenen Schlüssel**: Alle 60 Texte standen als deutsche Literale im Code, die `T()`-Aufrufe
fielen also ausnahmslos auf den Rückfall zurück. **Englisch gab es den Dialog nie.** Er ist
jetzt vollständig übersetzt.

**Wiederverwendet statt neu angelegt:** die zehn vorhandenen `KDLG_LPR_*`, alle 28
`PREIS_IMPORT_*` und 22 `PREIS_PROFIL_*`, `CHART_ACHSE_MONAT`,
`PREIS_CHART_SERIE_KOSTENPROFIL`, `KPROF_KARTE_*`/`KPROF_STATUS_*`, `ALLG_BTN_OK`,
`SIM_BTN_OK`, `SIM_BTN_ABBRECHEN`, `PVW_ABBRECHEN`.

**Zugriff** über `Resource.ResourceManager.GetString` mit deutschem Rückfall im Code
(B5b‑O4) — die Hülle setzt die Texte, die Komponente trägt den deutschen Literaltext als
Parametervorgabe. Bei den beiden Masken, die schon durchgängig `MyResource` benutzten
(`Form_SpotpreisImport`, `Form_Kostenprofil`), greift die Hülle direkt auf die erzeugten
Eigenschaften zu — dort gibt es nichts zu sichern.

**Keine Übersetzung ist verloren gegangen.** Keine der vier Masken war lokalisiert
(`ApplyResources`); nur `Form_Kostenprofil` führte überhaupt eine `.resx`, und die trägt
ausschließlich die Designer-Standardeinträge. Die Zahl der lokalisierten Masken bleibt
deshalb bei 59.

**`help_mapping.txt` bleibt unverändert.** Die vier Zeilen
`Form_LeistungspreisReihe.btn_Help`, `Form_SpotpreisImport.btn_Help`,
`Form_Emissionskatalog.btn_Help` und `Form_Kostenprofil.btn_Help` gelten weiter — der
Schlüssel benennt die Wikiseite, nicht die Klasse (dasselbe Vorgehen wie seit iU8‑9).

**`Allgemein/KI/HilfeKontext.cs`:** die drei Einträge der gelöschten Masken entfernt
(`Form_Kostenprofil`, `Form_SpotpreisImport`, `Form_LeistungspreisReihe`) — jeweils im
Commit ihrer Maske (Regel F10). `Form_Emissionskatalog` stand dort nie.

---

## 6. WinForms-Seite

**Gelöscht** (9 Dateien):

```
Views/Kosten/Form_LeistungspreisReihe.{cs,Designer.cs}
Views/Kosten/Form_SpotpreisImport.{cs,Designer.cs}
Views/Kosten/Form_Emissionskatalog.{cs,Designer.cs}
Views/Kosten/Form_Kostenprofil.{cs,Designer.cs,resx}
```

**Kopiert** (3 Dateien) — `Form_Kostenprofil.*` nach
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/`: An ihr hängen neun Testbezüge des
Werkzeugs; sie ist der einzige Beleg für die Abschnittsbildung (§ 7.3).

**Neu** auf der Windows-Seite: `Views/Kosten/LeistungspreisReiheHuelle.cs`,
`SpotpreisImportHuelle.cs`, `EmissionskatalogHuelle.cs`, `KostenprofilHuelle.cs`.

**Keine Typverwendung ist übrig:**

```
git grep -nE "(new|typeof|:)\s*Form_(LeistungspreisReihe|SpotpreisImport|Emissionskatalog|Kostenprofil)\b" \
    -- 'WindowsFormsApplication1/*.cs' 'EPOS.UI/*.razor' 'EPOS.Kern/*.cs'
→ 0 Treffer
```

Restfundstellen der alten Namen sind ausschließlich (a) `HilfeSchluessel`-Zeichenketten
(`"Form_X.btn_Help"` — Schlüssel des Hilfekatalogs, § 5), (b) Kommentare, die die Herkunft
nennen, und (c) die Prüfmusterbezüge der Formularkarte-Tests. Zwei Kommentarverweise auf
gelöschte Masken wurden auf lebende umgehängt (`InfoKnopf.cs` — Kopfbandhöhe,
`UcWirtschaftlichkeit.cs` — `AutoScaleMode`-Muster); der Klassenkommentar von
`EmissionskatalogCtrl` nennt jetzt die Razor-Nachfolge.

---

## 7. Nachweise

### 7.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental
→ 0 Fehler, 26 Warnungen
```

Basis (`95cf8be`): 28. **WFO1000 sinkt von 22 auf 20** — die beiden Fundstellen der
gelöschten Masken sind weg; der Rest ist unverändert (2 × CS0108, 2 × CS0109,
1 × WFO0003, 1 × CA2255).

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ EPOS.Kern.Tests       37 grün   (35 vorher, 2 neu)
  KiKern.Tests         450 grün
  SpeicherEngine.Tests 337 grün
  EPOS.UI.Tests        393 grün   (288 vorher, 105 neu)
  ────────────────────────────────
  1 217 grün, 0 rot    (1 110 vorher)
```

Die 107 neuen Tests:

| Datei | Tests | Prüft |
|---|---|---|
| `Standards/DateiwahlTests.cs` | 8 | kein Knopf ohne Wähler, Knopftext, gewählter Pfad, Filterdurchreichung, Abbruch lässt den Pfad stehen, Vorgabe nur lesbar, beschreibbares Feld, Sperre |
| `Bausteine/ZeilenwahlTests.cs` | 5 | ungewählt/gewählt (Zeichen, `aria-pressed`, Klasse), Klickmeldung, Kurztext, Sperre |
| `Standards/FelderTests.cs` | +4 | Textfeld mehrzeilig (Zeilenzahl, Klasse, Inhalt), Eingabe, `NurLesen` mehrzeilig und einzeilig |
| `Standards/RasterTests.cs` | +1 | bearbeitbare Zellen: `Schalter` und `Zahlenfeld` in `TemplateColumn`s melden an die Zeile |
| `Dialoge/LeistungspreisReiheDialogTests.cs` | 17 | Feldbestand (13 Zahlenfelder, 3 Knöpfe), Monatsnamen, Kontext/Einheit/Hinweis, Vorbelegung, kurze Werteliste, Löschsperre beidseitig, Nullsummenregel, Übernahme mit Jahr und 12 Werten, geänderter Monatswert, geleertes Jahresfeld, Speicher- und Löschfehler, Löschen, Abbrechen/Esc, Enter unbelegt, Hilfeschlüssel |
| `Dialoge/SpotpreisImportDialogTests.cs` | 16 | Feldbestand (9 Zeilen), Protokollfeld (mehrzeilig/nur lesbar/festbreit), Stammschalter vorbelegt, Übernehmen anfangs gesperrt, Prüfung nach Dateiwahl samt Bezeichnervorgabe, eigener Bezeichner bleibt, unbrauchbare Datei, Ausnahme wird Protokoll, Abbruch prüft nicht, Übernahme mit Ziel, Ziel ohne Stammschalter, Schreibfehler, Fortschritt, Abbrechen/Esc, Enter, Hilfeschlüssel |
| `Dialoge/EmissionskatalogDialogTests.cs` | 33 | Feldbestand (Kopf, Modus, zwei Raster, Schlussleiste), Artenliste, gesperrtes Pflichthäkchen, Vorwahl nach Kürzel, Übernehmen nur mit Träger, Artwechsel lädt Werte, Häkchen setzen und verweigern, Arteneditor (anlegen, leeres Kürzel, Pflichtart, ausgelieferte Art, Fehlschlag), Ruhen des Dialogs, Esc-Reihenfolge, Löschen mit Rückfrage/Nein, „abwählen statt löschen", Pflichtart ohne Ausweg, Wertesperren, Wert bearbeiten und anlegen, Wert ohne Zahl, Vorlagenschalter, Übernahme in beiden Modi, Eintrag ohne Zahlenwert, OK/Abbrechen mit Modus und Merkern, Enter, Hilfeschlüssel |
| `Dialoge/KostenprofilDialogTests.cs` | 21 | drei Abschnitte, 12 + 24 Laufzeitfelder, Monats- und Stundennamen, Wochentage ab Montag, sieben Knöpfe, Vorbelegung, Tagwechsel, Januar-für-alle, Einfügen ohne Kopie, Kopieren/Einfügen, „Für alle Tage", geleertes Feld, Vorschau beim Öffnen, Werteübergabe, Aktualisieren, Tagübernahme, OK mit getrimmtem Bezeichner, Schreibfehler, Abbrechen/Esc, Enter, Hilfeschlüssel |
| `EPOS.Kern.Tests/ChartRendererTests.cs` | +2 | Kostenprofil: Determinismus und Maß 1296 × 780 mit negativem Abschnitt; leere Reihe liefert ein Bild statt `null` |

### 7.3 Formularkarte

```
dotnet build Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 0 Fehler, 0 Warnungen
dotnet test  Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 121 grün (120 vorher)
```

Neun Testbezüge hingen an `Form_Kostenprofil` (Risiko R8). Sie ist als **fünftes
Prüfmuster** eingefroren — der einzige Beleg für die **Abschnittsbildung**: ein `TabControl`
mit drei Reitern, darin ein `Chart`, eine `ListBox` mit eigenem Label und Beschriftungen,
die nur innerhalb ihres Abschnitts wirken. `PruefmusterTests` führt sie als vierte
`Theory`-Zeile; daher 121 statt 120.

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -c Release -- --alle WindowsFormsApplication1 --erreichbarkeit
```

| Kennzahl | nach W1 | nach W0 | nach W2 | **nach W3** |
|---|---:|---:|---:|---:|
| Designer-Dateien (Repo) | 114 | 108 | 105 | **101** |
| davon Masken | 111 | 105 | 102 | **98** |
| lokalisiert | 62 | 61 | 59 | **59** |
| Kartenzeilen | 2 322 | 2 231 | 2 188 | **2 128** |
| Felder ohne Beschriftung | 172 | 168 | 168 | **165** |
| Öffner erreichbar („ja") | 104 | 103 | 100 | **96** |
| unerreichbar / verwaist / unklar | 4/1/2 | 0/0/2 | 0/0/2 | **0/0/2** |

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 303 SQL-Texte geprüft: 0 Fundstellen, 149 dynamisch, 1 154 in Ordnung
```

Unverändert zu W2 — die Welle hat keine SQL-Anweisung angefasst. Alle vier Masken riefen
schon vorher ausschließlich Kern-Controller (Hausmuster Ä9).

### 7.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 10 Bilder geprueft, 0 Verstoesse.  ERGEBNIS: alle gruen.
```

Das zehnte Bild ist `kostenprofil` (1296 × 780). Geprüft wird die über Weiß **gemischte**
Linienfarbe 75/146/75, weil `C_PROFIL` halbtransparent ist — dasselbe Thema wie bei den
halbtransparenten Speichertemperaturen, dort ist die untere Schicht deshalb gar nicht
geprüft. Die Probendaten laufen bewusst ins Negative, damit die gestrichelte Nulllinie
mitgeprüft wird.

### 7.7 Referenzlauf

```
dotnet run --project EPOS.Referenzlauf -c Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
    --projekte 1030,1007,1017 --ziel <ordner>
dotnet run --project EPOS.Referenzlauf -c Release -- vergleich Referenzlaeufe/2026-08-30_B3-Kaskade <ordner>
```

| Projekt | Ergebnis |
|---|---|
| 1007 | **PASS** (29 Dateien, 324 219 Werte) |
| 1017 | **PASS** (21 Dateien, 254 154 Werte) |
| 1030 | **PASS** (22 Dateien, 236 670 Werte) |

`diff -rq` gegen die Basis meldet für diese drei Ordner **keinen** Unterschied; repoweit
bleibt nur `protokoll.txt` (das Laufprotokoll) und die zehn nicht gerechneten Projekte. Der
Lauf ist **Pflicht**, weil der Kern angefasst wurde (`ChartRenderer.Kostenprofil`,
`C_PROFIL`) — der Nachweis bestätigt, dass die neue Methode additiv ist und keine
bestehende berührt.

### 7.8 Veröffentlichung

```
dotnet publish WindowsFormsApplication1 -c Release -r win-x64 --self-contained -p:Platform=x64 -o <ordner>
```

`wwwroot` vollständig: `index.html`, `_framework/blazor.webview.js`,
`_framework/blazor.modules.json`, `_content/EPOS.UI/{epos-ui.css,help_icon.png}` (samt
`.br`/`.gz`), `_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js`. Die
sieben neuen CSS-Klassen (`epos-zahlenraster`, `epos-editorblock`, `epos-dateiwahl`,
`epos-eingabe--mehrzeilig`, `epos-eingabe--festbreite`, `epos-raster--bearbeitbar`,
`epos-wert-geltend`) sind in der ausgelieferten `epos-ui.css` enthalten.

---

## 8. Grenzen

* **Keine Windows-Sicht.** Alles hier ist auf Linux gemessen: Build, Tests, Referenzlauf,
  ChartProben, Veröffentlichung. Ob die Dialoge in der WebView2 richtig aussehen — und ob
  der Dateiwähler aus `Dienste.Datei` sauber über dem Blazor-Dialog erscheint —, sagt erst
  die Abnahme (§ 9).
* **Der Emissionskatalog ist der größte Dialog der Welle** (946 Zeilen Razor). Er trägt
  zwei Listen, zwei Editoren und vierzehn Knöpfe in einem Fenster; die WinForms-Fassung
  verteilte das auf drei. Ob das in einem Fenster noch übersichtlich ist, ist eine
  Sichtfrage (Abnahmepunkt W3‑9).
* **Das Kostenprofil zeigt drei Abschnitte statt drei Reitern** (A‑17), bis Welle 5 den
  Baustein `Reiter` bringt.
* **Das Diagramm ist ein PNG** und damit nicht mehr zoombar (A‑20).
* **Die Rückfrage vor dem Löschen ist noch ein WinForms-Fenster** (A‑13), bis Welle 4 den
  Baustein `Rueckfrage` bringt.

---

## 9. Abnahmeliste Windows (iZ5) für diese vier Dialoge

Weg zu den Dialogen: **Menü → Energieträger** (`Form_Energietraeger`) — dort die Karten
„Kostenprofil" (W3.4, nur im Projektkontext beim Stromträger) und „Spotmarktpreise" (W3.2);
in der Trägerkarte darunter (`ucFuelSettings`) der Knopf „Saisonale Sätze…" (W3.1) und in
der Emissionszeile „Katalog…" bzw. „Emissionsarten & Katalog verwalten…" (W3.3).

| # | Punkt | W3.1 | W3.2 | W3.3 | W3.4 |
|---|---|:--:|:--:|:--:|:--:|
| 1 | Öffnet mittig, kein weißes Aufblitzen | ☐ | ☐ | ☐ | ☐ |
| 2 | Fenster ziehbar **und** maximierbar | ☐ | ☐ | ☐ | ☐ |
| 3 | Tabellen ohne Umbruch (Befund 03.09.) | – | – | ☐ | – |
| 4 | Deutsch **und** Englisch (`HKCU\Software\wp-plan\Language`) | ☐ | ☐ | ☐ | ☐ |
| 5 | Hochkontrast: Warnbanner, Statuszeile und Fehleingabe bleiben unterscheidbar | ☐ | ☐ | ☐ | ☐ |
| 6 | 125 % und 150 % scharf (DPI-Insel greift) | ☐ | ☐ | ☐ | ☐ |
| 7 | Maus **und** Finger (44 px), Optionsgruppe mit den Pfeiltasten | ☐ | ☐ | ☐ | ☐ |
| 8 | Tab-Zyklus bleibt im Dialog, Esc schließt | ☐ | ☐ | ☐ | ☐ |
| 9 | Infoknopf zeigt die Wikiseite („Kosten" bzw. „Emissionen") | ☐ | ☐ | ☐ | ☐ |

**Fachliche Proben:**

| # | Probe |
|---|---|
| **W3‑1** | W3.1: Im **Projektkontext** „Saisonale Sätze…" öffnen — die Kontextzeile sagt „Projektreihe"; ist nur eine Stammreihe gepflegt, stehen deren Werte drin und der Hinweis nennt ihr Jahr; „Reihe löschen" ist dann **gesperrt** |
| **W3‑2** | W3.1: Zwölf Nullen eingeben und „Übernehmen" — der Dialog meldet und schreibt nicht; ein Wert ≠ 0 legt die Reihe an, ein zweiter Lauf im selben Jahr **ersetzt** sie (andere Jahre bleiben als Historie) |
| **W3‑3** | W3.2: „Datei wählen…" — **der Dateiwähler erscheint über dem Blazor-Dialog** (A‑7, Prüfpunkt); nach der Wahl steht das Protokoll da, „Übernehmen" ist frei, die Bezeichnung trägt den Dateinamen |
| **W3‑4** | W3.2: Eine unbrauchbare Datei wählen — das Protokoll nennt den Grund, die Statuszeile ist rot, „Übernehmen" bleibt gesperrt |
| **W3‑5** | W3.2: „Übernehmen" mit gesetztem Stammschalter — die Zeile zählt sichtbar hoch (8 760 Werte), danach schließt der Dialog und die Karte „Spotmarktpreise" nennt die neue Reihe |
| **W3‑6** | W3.2: Dateiwähler mit **Abbrechen** verlassen — der Dialog sagt nichts und behält den alten Pfad |
| **W3‑7** | W3.3: Aus einer Emissionszeile „Katalog…" — die Art ist vorgewählt, „Übernehmen" **schließt** und trägt den Wert in die Zeile ein (nicht in die Datenbank); „Speichern" des Trägerdialogs schreibt ihn |
| **W3‑8** | W3.3: „Emissionsarten & Katalog verwalten…" — „Übernehmen" schreibt **sofort** und der Dialog bleibt offen; das Häkchen einer ausgelieferten Art lässt sich abwählen, „Löschen" bietet stattdessen das Abwählen an; bei CO₂ bleibt es beim Hinweis |
| **W3‑9** | W3.3: „Neu…" öffnet den **eingerückten Block**, nicht ein zweites Fenster; solange er steht, sind die übrigen Knöpfe grau; Esc schließt zuerst ihn und erst beim zweiten Mal den Dialog |
| **W3‑10** | W3.3: Modus auf „CO₂-Äquivalent" stellen und mit **OK** schließen — die globale Vorgabe steht um; mit **Abbrechen** bleibt sie, aber angelegte Arten und Werte sind trotzdem da |
| **W3‑11** | W3.4: Monatswerte ändern und „Vorschau aktualisieren" — die Linie folgt; „Januar-Wert für alle Monate" ebnet sie ein |
| **W3‑12** | W3.4: Wochentag wechseln — die 24 Felder zeigen den neuen Tag; „Tag kopieren" auf Montag, Wechsel auf Samstag, „Tag einfügen" überträgt den Gang; „Für alle Tage" meldet und zeichnet neu |
| **W3‑13** | W3.4: Ein Wochenwert, der den Monatswert unter null zieht — die Vorschau zeigt die **gestrichelte Nulllinie** und schneidet die Linie nicht ab |
| **W3‑14** | W3.4: „OK" schreibt das Profil; nach dem Schließen nennt die Karte „Kostenprofil" den Bezeichner |

---

## 10. Offene Punkte

| # | Punkt |
|---|---|
| **W3‑O1** | **A‑7 am Gerät prüfen** (W3‑3). `WindowsDateiDienst.DateiOeffnen` ruft `ShowDialog()` **ohne** Besitzerfenster. Erscheint der Wähler hinter dem Blazor-Dialog, bekommt `IDateiDienst` einen Besitzerparameter — eine Zeile je Seite, aber eine Schnittstellenänderung, die auch die sechs Importmasken der Welle 13 betrifft |
| **W3‑O2** | **A‑20:** Das Kostenprofil ist nicht mehr zoombar. Das WinForms-Chart erlaubte Ziehen und Aufziehen in der Zeitachse. Braucht der Anwender das für ein Jahresprofil, führt der Weg über eine JS-Schicht in `EPOS.UI` (dieselbe, die W1‑O4 für `SelectAll()` erwägt) — oder über eine Ausschnittswahl (Monat/Woche) im Dialog, die der Renderer bedient |
| **W3‑O3** | **A‑17 sichtprüfen:** Drei Abschnitte untereinander statt dreier Reiter machen das Kostenprofil zu einem langen Fenster (36 Felder + Diagramm). Wenn der Anwender die Reiterform vermisst, bringt Welle 5 den Baustein `Reiter`, und der Dialog bekommt ihn nachträglich |
| **W3‑O4** | **A‑16 dem Anwender vorlegen:** Der Emissionskatalog gibt seine Änderungsmerker auch bei „Abbrechen" zurück (so las der Aufrufer es schon in WinForms). Der Modus-Schalter dagegen wirkt nur über OK. Diese Zweiteilung ist der Bestand, aber sie ist erklärungsbedürftig — soll „Abbrechen" im Katalog künftig „Schließen" heißen? |
| **W3‑O5** | **A‑18:** Die drei „ungültig"-Meldungen des Kostenprofils (`PREIS_PROFIL_MSG_JANUAR`, `_MONAT_UNGUELTIG`, `_STUNDE_UNGUELTIG`) haben keinen Aufrufer mehr. Sie bleiben in der Ressource stehen, bis geklärt ist, ob ein späterer Reiter-Umbau sie wieder braucht |
| **W3‑O6** | Der Emissionskatalog zeigt beide Listen untereinander; die WinForms-Fassung stellte sie nebeneinander (920 px breit). Auf einem breiten Bildschirm bleibt rechts Platz. Ob eine zweispaltige Anordnung ab einer Mindestbreite lohnt, entscheidet die Sichtabnahme — im CSS wäre es eine Medienabfrage |
| **W3‑O7** | `Zeilenwahl` ersetzt den Wahlknopf im Emissionskatalog. `BhkwWirtschaftlichkeitDialog` und `KostenfaktorKatalogDialog` tragen ihn weiterhin als eigenes Markup; sie ziehen beim nächsten Anfassen nach (kein eigener Commit wert, solange die Optik dieselbe ist) |

---

## 11. Geänderte und neue Dateien

```
NEU
  EPOS.UI/Standards/Dateiwahl.razor                                   92 Zeilen
  EPOS.UI/Bausteine/Zeilenwahl.razor                                  39
  EPOS.UI/Dialoge/Kosten/LeistungspreisReiheDialog.razor             278
  EPOS.UI/Dialoge/Kosten/SpotpreisImportDialog.razor                 297
  EPOS.UI/Dialoge/Kosten/SpotpreisImportDaten.cs                      21
  EPOS.UI/Dialoge/Kosten/EmissionskatalogDialog.razor                946
  EPOS.UI/Dialoge/Kosten/EmissionskatalogDaten.cs                     87
  EPOS.UI/Dialoge/Kosten/KostenprofilDialog.razor                    479
  WindowsFormsApplication1/Views/Kosten/LeistungspreisReiheHuelle.cs 187
  WindowsFormsApplication1/Views/Kosten/SpotpreisImportHuelle.cs     142
  WindowsFormsApplication1/Views/Kosten/EmissionskatalogHuelle.cs    406
  WindowsFormsApplication1/Views/Kosten/KostenprofilHuelle.cs        224
  EPOS.UI.Tests/Standards/DateiwahlTests.cs                          105  (8 Tests)
  EPOS.UI.Tests/Bausteine/ZeilenwahlTests.cs                          65  (5)
  EPOS.UI.Tests/Dialoge/LeistungspreisReiheDialogTests.cs            299  (17)
  EPOS.UI.Tests/Dialoge/SpotpreisImportDialogTests.cs                298  (16)
  EPOS.UI.Tests/Dialoge/EmissionskatalogDialogTests.cs               617  (33)
  EPOS.UI.Tests/Dialoge/KostenprofilDialogTests.cs                   393  (21)
  Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/Form_Kostenprofil.{cs,Designer.cs,resx}
  WindowsFormsApplication1/Allgemein/Reporting/iU9_W3_Blazor_Port_Protokoll.md  dieses Protokoll

GEÄNDERT
  EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs        + Kostenprofil, C_PROFIL
  EPOS.Kern.Tests/ChartRendererTests.cs               + 2 Tests
  Proben/ChartProben/Program.cs                       + zehntes Bild und seine Probendaten
  EPOS.UI/Standards/Textfeld.razor                    + Mehrzeilig, Zeilen, NurLesen, Festbreite
  EPOS.UI/Standards/Raster.razor                      + Bearbeitbar
  EPOS.UI/wwwroot/epos-ui.css                         + 7 Klassen
  EPOS.Kern/Controller/EmissionskatalogCtrl.cs        Klassenkommentar (Nachfolge)
  EPOS.Kern/MyResource/Resource.resx                  + 67 Schlüssel
  EPOS.Kern/MyResource/Resource.en-US.resx            + 67
  EPOS.Kern/MyResource/Resource.Designer.cs           + 67 (von Hand)
  WindowsFormsApplication1/Views/Kosten/ucFuelSettings.cs         3 Aufrufstellen
  WindowsFormsApplication1/Views/Kosten/Form_Energietraeger.cs    2
  WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs          − 3 Einträge
  WindowsFormsApplication1/Allgemein/Hilfe/InfoKnopf.cs           1 Kommentarverweis
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs  1
  EPOS.UI.Tests/Standards/{Felder,Raster}Tests.cs                + 5 Tests
  Werkzeuge/Formularkarte.Tests/{Abschnitt,FeldkarteSchreiber,RazorSchreiber,Pruefmuster,
      Stapel,Erreichbarkeit}Tests.cs
  Werkzeuge/Formularkarte/{LIESMICH.md,Erreichbarkeit_2026-09-03.md}

GELÖSCHT
  9 Dateien der vier WinForms-Masken (Regel M1) — Liste in § 6
```
