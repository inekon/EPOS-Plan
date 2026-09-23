# Analyse für die Umsetzung: Berichte und Export, VALERI-Etappen, Nutzungsdauer, Verlauf, Bandbreite

*Stand 19.09.2026 · Zweig `ios_migration_september` · Zielversion 94 · nur gelesen, nichts geändert.
Belege als `Datei:Zeile` bzw. `Papier Z. n`. „Gemessen" heißt: an der genannten Stelle gelesen oder
gezählt; „vermutet" ist eigens gekennzeichnet. Die Mockup-Prüfung von heute (Befundpapier,
Protokolle `00…06`) wird zitiert, nicht wiederholt.*

## 0 Ergebnis in fünf Sätzen

Von den fünf ValERI-Etappen ist nur **V-B** (Referenzwahl) fertig, **V-A, V-C, V-D und V-E fehlen
ganz** — im Kern gibt es weder eine „nachrichtlich"-Kennzeichnung noch eine IZF-Mehrdeutigkeits­warnung,
keine Deklarationszeilen und **repoweit keine einzige Excel-Formel** (0 Treffer auf
`FormulaA1`/`FormulaR1C1`/`RecalculateAllFormulas` über alle `.cs`), während der `ExcelBerichtGenerator`
mit 167 `.Value`-Zuweisungen den Parametersatz weiterhin nur fünfmal anfasst und keine seiner Zahlen in
eine Zelle bringt. Die Messung des Konzepts § 2.11.6 ist damit bestätigt und nur in drei Kleinigkeiten
nachzuziehen (1 412 statt 1 380 Zeilen; auch der **Word**-Generator ist ungedeckt, nicht nur der
Excel-Generator; das fünfte `Worksheets.Add` ist die Schrift-Messprobe, es bleiben vier Blattarten).
Die Nutzungsdauer ist weiter als das konsolidierte Konzept behauptet: **S1 und S2 sind gebaut**
(Statuszeile #357), also auch der Knopf „Nutzungsdauern vorbelegen…" und der Pflegeort der Positionsart
— zwei Sätze in § 2.13 (3) sind veraltet; offen bleiben von den fünf Stücken nur noch drei
(Entkopplung Ersatz/Restwert, geräteeigene Dauerspalten, Speicherflotte) plus der Hinweis im Bericht
und die plattformfreie Hülle. Der Verlauf rechnet ein Szenario je Lauf, vergibt Farben nach laufendem
Reihenindex und trägt Reihennamen ohne Szenario; für die Dreierreihe fehlt außerdem etwas, das das
Konzept noch nicht nennt: **`Reihe.Gestrichelt` ist ein `bool` und kennt nur zwei Stricharten, drei
Szenarien brauchen eine dritte**. Die eine Sichtbarkeitsregel hält — Seite, Word und Excel ziehen
nachweislich dieselbe `WirtschaftlichkeitZeilen.Sichtbare` und dieselben Zahlenformatpaare —,
die Abweichungen liegen im Drumherum (Word druckt nur Erwartet, Excel drei Blöcke, der
G7-Zeitraumhinweis fehlt in Excel ganz), und für die Anhang-D-Gegenprobe gilt die Vermutung des
Auftrags **nicht**: Normtext und VALERI-Vorlage liegen im Repositorium unter `Quellen/VALERI/`.

---

## 1 VALERI-Matrix

### 1.1 Etappen V-A … V-E

| Etappe | Stand | Codeort (gemessen) | Test |
|---|---|---|---|
| **V-A** Ausweis: „nachrichtlich", IZF-Warnung, Deklarationszeilen, Steigungsspalte | **fehlt** (zwei Teil-Ausweise stehen, siehe § 5.5) | keiner; `grep -i "nachrichtlich\|Vorzeichenwechsel\|Anhang C"` über `EPOS.Kern`, `EPOS.UI`, `EPOS.UI.Daten`, `WindowsFormsApplication1` → kein fachlicher Treffer | — |
| **V-B** Referenzwahl § 2.9 | **umgesetzt** | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/Referenzwahl.cs`; `WirtschaftlichkeitCtrl.cs:1682` (`BerechneVerlauf` mit `idReferenz`); Schemaschritt 92 | `EPOS.Kern.Tests/ReferenzprojektTests.cs` (16 Fälle), Statuszeile #358 |
| **V-C** ValERI-Ansicht, fünf Blöcke, Umschalter | **fehlt** | Protokoll `03/#76` (kein Umschalter, Ressourcen fehlen), `03/#77` (vier Abschnittsköpfe fehlen) | — |
| **V-D** XLSX-Formelbericht + Anhang-E-Checkliste + Anhang-D-Gegenprobe | **fehlt** | 0 Formeltreffer repoweit; kein Checklisten-Element (`03/§ 5 A`) | — |
| **V-E** Vollständige Szenarioabdeckung, Risiko, Degradation, n-Jahres-Zeitpunkte | **teilweise** — nur der Parametersatz W5‑B‑9/‑12 | `WirtschaftlichkeitDaten.cs:518-532` (`FuerSzenario` ersetzt genau vier Größen: i, p_E, p_B, p_I) | `SzenarioParameterTests` (17 Fälle) |

**Befund zu V-A, genauer.** `KapitalwertRechner.InternerZinsfuss` (`KapitalwertRechner.cs:770-798`)
liefert `null`, wenn `fLo * fHi > 0` — das ist ein Konvergenzabbruch, **keine Zählung der
Vorzeichenwechsel** und keine Warnung. Die vom Konzept verlangte Mehrdeutigkeitswarnung braucht einen
eigenen Zähler über die Differenzreihe (`fluss[t]`, dort schon gebildet) und eine Warnzeile; das ist
der kleinste Teil von V-A und rechnerisch folgenlos.

### 1.2 Gap-Tabelle V-G (konsolidiertes Konzept § 2.11.2)

| # | Stand | Beleg |
|---|---|---|
| V-G1 zwei Preisraten, nominal | **erfüllt**, inzwischen **drei** (p_E, p_B, p_I) — Deklaration „nominal gerechnet" fehlt | `WirtschaftlichkeitDaten.cs:530`, `:541-546` |
| V-G2 Degradation je Position | **fehlt**; als Vereinfachung offengelegt | `WirtschaftlichkeitEmpfehlung.cs` (`ValeriAusweis.Vereinfachungen`) |
| V-G3 Zeitpunkt „alle n Jahre" | **fehlt**; StartJahr und Ersatzkette bei n, 2n … vorhanden | `KapitalwertRechner.Ersatz` (aus `Rechne` herausgelöst, #357/U30) |
| V-G4 kein Restwertverfahren | **abweichend, bewusst**; Deklaration im Bericht fehlt | Restwert linear, `KapitalwertRechner.cs:758-760` |
| V-G5 Szenarien = alle Parameter | **teilweise**; Zeitraum, Trägerpreise, Erlössätze, Mengen bleiben gleich | `WirtschaftlichkeitDaten.cs:518-532` |
| V-G6 Sensitivität 7 Parameter, Steigung €/%, Diagramm | **teilweise**: fünf Zeilen, keine Steigungsspalte, kein Diagramm | § 5.4 |
| V-G7 Risiko | **fehlt** | kein Treffer |
| V-G8 IZF/Amortisation nur nachrichtlich | **fehlt** | § 1.1 |
| V-G9 Steuerdeklaration | **fehlt** als Zeile | kein Treffer auf `WIRT_DEKL`/„Steuern berücksichtigt" |
| V-G10 Bericht + editierbare XLSX mit Formeln | **fehlt** | § 3 |
| V-G11 nicht monetarisierbare Wirkungen | **teilweise umgesetzt** (Freitext) — Konzeptzeile veraltet | `WirtschaftlichkeitParameter.NichtMonetaer`; Excel `ExcelBerichtGenerator.cs:582-585`, Word `BausteineWirtschaftlichkeit.cs:145-150`; Kategorie und Beurteilung (Dauer × Wirkung) fehlen |
| V-G12 Anhang-E-Checkliste | **fehlt** | § 7 |

### 1.3 Szenarienkonzept § 9 (G7–G11), § 10 (p_I), § 11

| Nr. | Stand | Codeort | Test |
|---|---|---|---|
| **G7** Zeitraum gegen Nutzungsdauern | umgesetzt im Kern, **nur zwei von drei Ausgaben** | `WirtschaftlichkeitEmpfehlung.cs:263-317` (`NutzungsdauerAbgleich.Hinweis`); Word `BausteineWirtschaftlichkeit.cs:1051`, Windows-Seite `WirtschaftlichkeitSeiteGaben.cs:413` — **Excel ruft ihn nicht** | `ValeriLueckenTests.cs:395-440` |
| **G8** Bandbreite im Bericht | umgesetzt in Word und Excel; „Spanne" und Referenzzeile fehlen, auf der Seite gar nichts | Word `BausteineWirtschaftlichkeit.cs:943-948`, Excel `ExcelBerichtGenerator.cs:451-478`; Befund `03/#72` | — |
| **G9** Empfehlungsregel | umgesetzt, **eine** Regel für drei Ausgaben | `WirtschaftlichkeitEmpfehlung.cs:165-200`; Word `:939`/`:992`, Excel `:568`, Seite `WirtschaftlichkeitSeiteGaben.cs:712` | `ValeriLueckenTests.cs:274-383` |
| **G10** Eigenverbrauch abgeleitet | umgesetzt (Ausweissatz) | `WirtschaftlichkeitEmpfehlung.cs` (`ValeriAusweis`) | `ValeriLueckenTests` |
| **G11** Basisregel der Prozentzeilen | umgesetzt | `EPOS.Kern/Controller/InvestKaskade.cs:313` (`BetragImSzenario`), gezogen in `WirtschaftlichkeitCtrl.cs:5700` und `BetriebskostenCtrl.cs:248` | `ValeriLueckenTests.cs:172` |
| **p_I Teil a** Schema | umgesetzt, Schritt 72 | `SchemaKatalog.Schritt72_ValeriErgaenzung` | — |
| **p_I Teil b** Parametersatz, Dialog, Bericht | umgesetzt | `WirtschaftlichkeitDaten.cs:530` (`FuerSzenario`), `:544-546` (Nachweis) | `SzenarioParameterTests` |
| **§ 11 V-4** Hinweistext § 2.11.7 | **nicht umgesetzt** — keine Ressource, keine Fundstelle | Papier selbst: Szenarienkonzept Z. 548-572 | — |
| **§ 11 K-8/V-1** Umschalter | **nicht umgesetzt** | `03/#76`; Statuszeile „Nach #346 (a)" | — |
| **§ 11 V-G10** ganzer Bericht formelbasiert | **nicht umgesetzt** | § 3 | — |

### 1.4 Entscheidungsbedarf

| Frage | Stand | Quelle |
|---|---|---|
| V-1 Umschalter statt Aufklappabschnitte | **entschieden 18.09.2026** (Umschalter) | Konzept Z. 686 |
| V-2 nur ValERI-Blatt oder ganzer Bericht | **entschieden 18.09.2026, gekippt** → ganzer Bericht | Konzept Z. 688 |
| V-3 IZF/Amortisation mit Label behalten | **entschieden** (behalten mit Label) | Konzept Z. 690 |
| V-4 Szenarioabdeckung wann | **entschieden 18.09.2026** (danach, mit Hinweistext) | Konzept Z. 691 |
| § 7.3 G1 … G11 | **alle entschieden 09.09.2026** | Szenarienkonzept Z. 267-287 |
| ND-Q1 … ND-Q8 | **alle entschieden 14.09.2026** nach Empfehlung | Nutzungsdauer-Konzept Z. 236-238 |
| **Offen** | U39 (Rest von U8) und Q1–Q25 des Befundpapiers | Status „Nach #357 (b)", „Nach #369 (a)" |

**Keine der Etappen hat heute einen offenen Fachentscheid als Blocker.** Was offen ist, ist der
Zuschnitt (U39, Q1–Q25) — und die im Befundpapier § 4 gestellten Fragen.

---

## 2 Nutzungsdauer-Stufen

### 2.1 Stufenplan § 3 gegen den Code

| Stufe | Konzeptaussage | Gemessener Stand |
|---|---|---|
| **S1** Tabelle und Verwaltung | umgesetzt | **bestätigt**: `EPOS.Kern/Allgemein/Update/NutzungsdauerSchema.cs`, `EPOS.Kern/Controller/NutzungsdauerCtrl.cs`, `EPOS.UI/Dialoge/Kosten/NutzungsdauerDialog.razor`, plattformfreie Hülle `EPOS.UI.Daten/Kosten/NutzungsdauerHuelle.cs`, Windows-Fenster `WindowsFormsApplication1/Views/Kosten/NutzungsdauerFenster.cs`. Tests `EPOS.Kern.Tests/NutzungsdauerTests.cs` (15 Fälle), `EPOS.UI.Tests/Dialoge/NutzungsdauerDialogTests.cs` |
| **S2** Vorbelegung | umgesetzt | **bestätigt**: Knopf `EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor:310-314` mit Parametern `:483`, `:595-607`; Kern `KostenProjektPositionenCtrl.cs:918` (`NutzungsdauerVorbelegen`) und `:940` (`NutzungsdauerArtZuordnen`), Vorlagenübernahme `KostenVorlagenUebernahmeCtrl.cs:510/527`; Tafel `EPOS.Kern/Controller/ErsatzRestwertTafel.cs:96/223`. Tests `ErsatzRestwertTafelTests.cs` (13 Fälle). Statuszeile #357 |
| **S3** Instandsetzung und Wartung | nur nach Entscheid | **Spalten liegen schon** (`NutzungsdauerSchema.cs:165` `Instandsetzung_Prozent`, `:168` `Wartung_Prozent`, ND-Q6 (a)); Anzeige und `BetriebskostenCtrl`-Nutzung fehlen — Entscheid offen |

### 2.2 § 6 Umsetzung — Aufträge A und B

Beide als „umgesetzt" geführt und im Code belegt (oben). **Offen aus § 6 B**, mit Beleg: Die Hülle
der Kostenverwaltung liegt weiter in der Windows-Schale
(`WindowsFormsApplication1/Views/Kosten/KostenKomponenteHuelle.cs:490-540`, ruft `ErsatzRestwertTafel`),
Konzept § 3 letzter Absatz und Status „Nach #357 (a)".

### 2.3 Die fünf Stücke aus § 2.13 (3) / U39

| # | Stück | Stand | Beleg |
|---|---|---|---|
| 1 | Entkopplung Ersatz / Restwert | **fehlt** | `grep -i "ErsatzFuehren\|RestwertAnsetzen"` → 0 Treffer; Schalter bleibt allein die Dauer (`n ≥ 1`), Konzept § 3.1 Z. 1452 |
| 2 | Nachpflege des Bestands (Knopf) | **gebaut** — Konzeptsatz veraltet | `KostenKomponenteDialog.razor:310`; Restbefund: der Zwilling auf der **Betriebsseite** ist sichtbar und dauerhaft gesperrt (`03/§ 5 B`, Befund 26) |
| 3 | Pflegeort für die Positionsart | **gebaut** — Konzeptsatz veraltet | `KostenProjektPositionenCtrl.cs:940`, Klappliste im Zeileneditor (#357) |
| 4 | Geräteeigene Dauerspalten | **fehlt** (zweite Wahrheit besteht fort) | `Tab_BHKW` über `BhkwCtrl.cs:150/381`, `Tab_Heizkessel` über `HeizkesselCtrl.cs:91/220`; kein Wirtschaftlichkeitsrechner liest sie |
| 5 | Anschluss der Speicherflotte | **fehlt** | `SpeicherEngine/FlottenModel.cs:541` `ErsatzintervallJahre`, `:544` `RestwertEuro`; kein Bezug zu `Tab_Nutzungsdauer` |

**Der Hinweis „T über Vorgabe, k von n Positionen ohne Dauer"** ist **zur Hälfte** gebaut: als
`ErsatzRestwertTafel.Hinweis` (`ErsatzRestwertTafel.cs:223`) **im Kostendialog je Komponente**. Auf der
Ergebnisseite und im Wortbericht steht weiterhin nur `NutzungsdauerAbgleich.Hinweis` (G7), der
kürzeste und längste Dauer nennt, aber **nicht die Zahl der betragstragenden Positionen ohne Dauer**
(`WirtschaftlichkeitEmpfehlung.cs:285-317` — dort wird `p.Nutzungsdauer < 1.0` übersprungen und nicht
gezählt). `ErsatzRestwertTafel` ist `internal` **im selben Zusammenbau** wie die Berichtsbausteine —
die Ausgabe in Seite und Bericht ist damit ein kleiner Schritt, kein neues Rechenwerk.

**Plattformfrei** fehlt weiterhin die Hinweiszeile: einziger Aufrufer ist
`WirtschaftlichkeitSeiteGaben.cs:413` in der Windows-Schale.

### 2.4 § 4 Fragen

Alle acht (ND-Q1 … ND-Q8) sind am 14.09.2026 nach Empfehlung entschieden (Nutzungsdauer-Konzept
Z. 236-238). Umgesetzt sind sie in S1/S2; einzig ND-Q7 (b) — Vorbelegung der Gerätekataloge — hängt
ausdrücklich an S3 und deckt sich mit Stück 4 oben.

---

## 3 Formelbericht § 2.11.6

### 3.1 Die Messung des Konzepts, nachvollzogen

| Größe | Konzept (18.09.) | Heute gemessen (19.09.) | Urteil |
|---|---|---|---|
| Formeln im Generator | „keine einzige" | **0** Treffer auf `FormulaA1`, `FormulaR1C1`, `RecalculateAllFormulas`, `.Formula` über **alle** `.cs` des Repositoriums | bestätigt, sogar weiter als behauptet |
| Wertzellen | `.Value` mit Zahl oder Text | **167** `.Value = ` in `ExcelBerichtGenerator.cs` | bestätigt |
| benannte Bereiche, Excel-Tabellen, Diagramme, Bilder | keine | **0** Treffer auf `NamedRange`, `CreateTable`, `AddPicture`, `.Tables` | bestätigt |
| Zugriffe auf den Parametersatz | „nur fünfmal" | **genau fünf**: `p.Nachweis` `:368`, `p.SatzFuer` `:467`, `p.NichtMonetaer` `:582/585`, `p.Betrachtungszeitraum` `:663`, `p.IdKraftwerkspark` `:816` — **keine Zahl erreicht eine Zelle** | bestätigt |
| Blattarten | vier | **vier**: Übersicht `:184`, Vergleich `:260`, Wirtschaftlichkeit `:347`, Detail je Variante `:1192`. Das fünfte `Worksheets.Add("Probe")` `:126` ist die Wegwerf-Messprobe der Schriftwahl | bestätigt, Nebensatz nötig |
| Zeilenzahl | 1 380 | **1 412** (die Datei trägt seit #358 die Referenzwahl) | nachzuziehen |

### 3.2 Testabdeckung

**Kein Test ruft `ExcelBerichtGenerator.Erzeuge` oder `WordBerichtGenerator.Erzeuge`.** Die beiden
einzigen Nennungen sind Kommentare: `EPOS.Kern.Tests/ErgebnisNachweisPersistenzTests.cs:119` und
`KwkgSatzHerkunftTests.cs:230`. Damit ist der Konzeptsatz zu eng — **auch der Word-Generator ist
ungedeckt**, und die Statuszeile sagt dasselbe („Nach #346 (c): Keine Berichtsprobe für die
Kennzahlentabelle des Word-Berichts"). Mittelbar geprüft ist nur der Zeilen**katalog**
(`ErloesrubrikTests`, `KwkgPauschaleZeileTests`, `KwkgSatzHerkunftTests`,
`ErgebnisNachweisPersistenzTests` rufen alle `WirtschaftlichkeitZeilen.Sichtbare`), nicht die
Blattstruktur.

### 3.3 ClosedXML 0.105.1 — bekannt gegen zu prüfen

Gebunden in `Directory.Packages.props:36`, referenziert von `EPOS.Kern/EPOS.Kern.csproj:135` und
`WindowsFormsApplication1.csproj:147`; das Paket liegt lokal
(`~/.nuget/packages/closedxml/0.105.1`).

**Bekannt, weil im Repositorium gemessen**

* Die Mappe entsteht mit `new XLWorkbook()` und `wb.SaveAs(zielDatei)`
  (`ExcelBerichtGenerator.cs:160/176`); Zahlenformate laufen über `Style.NumberFormat.Format`.
* Der Generator nutzt von ClosedXML nur Zellwert, Stil, Autofilter, Freeze und Spaltenbreite —
  keine Formel-, Namens-, Tabellen- oder Diagramm-API.
* Die Schriftmessung ist bereits ein bekannter Stolperstein: `MessprobeLaeuft()` `:121-135` und
  `RueckfallSchrift()` `:141-151` fangen ab, dass ClosedXML Spaltenbreiten ohne installierte
  Schrift nicht messen kann (SixLabors.Fonts ist deshalb ausdrücklich direkt referenziert,
  `EPOS.Kern.csproj:143-146`).

**Bekannt als Bibliothekseigenschaft (allgemein, nicht hier belegt)**

* Formeln werden über `IXLCell.FormulaA1` bzw. `FormulaR1C1` gesetzt; ClosedXML führt seit den
  0.10x-Fassungen eine eigene Rechenmaschine und bietet `RecalculateAllFormulas()` sowie
  `XLWorkbook.CalculateMode`.

**Zu prüfen, bevor Stufe 0 beginnt** (auf diesem Rechner nicht belegbar, weil im Hauptbaum kein Bau
und kein Test laufen darf)

1. Ob 0.105.1 beim Speichern **nur `<f>`** ablegt oder auch ein `<v>` mitschreibt — davon hängt ab, ob
   eine frisch erzeugte Mappe schon Zahlen zeigt oder erst nach dem Öffnen in Excel.
2. Ob `RecalculateAllFormulas` die für Stufe 2 nötigen Funktionen (NBW/NPV, RMZ/PMT, IKV/IRR) trägt
   und was sie bei fehlendem Vorzeichenwechsel liefert (Zellfehler statt benanntem Leerwert wäre ein
   Bruch der Konzeptvorgabe).
3. Ob LibreOffice/Numbers dieselben Werte zeigen — die Norm verlangt eine **editierbare**
   Tabellenkalkulationsdatei, nicht „eine, die Excel rechnet".
4. Ob die deutschen Funktionsnamen (NBW, RMZ) oder die englischen abzulegen sind; ClosedXML erwartet
   im Allgemeinen die englischen, die Anzeige übersetzt Excel selbst — **vermutet**, zu bestätigen.

> **Nachtrag 23.09.2026 (#455, Etappe E8 Teil b):** Die vier Fragen sind gemessen. ClosedXML 0.105.1 legt zu einer
> Formel kein Ergebnis ab, auch nicht nach `RecalculateAllFormulas`; seine Rechenmaschine kennt `NPV` und `IRR` nicht
> (`#NAME?`), `PMT` rechnet sie; abgelegt werden die englischen Namen. Excel (Microsoft 365, Version 16) rechnet jede
> Formelzelle auf den eingetragenen Wert; LibreOffice war auf dem Rechner nicht prüfbar. Daraus die Regel „EPOS trägt
> die Werte ein, Excel rechnet neu" (Konzept § 2.11.6) und die Wache `FormelmappeClosedXmlBefundTests`. Befund,
> Entscheid und Berichtigung (E8b/8: `PMT` rechnet doch):
> [`E8b_Formelmappe_AnhangE_D_Protokoll.md`](../E8b_Formelmappe_AnhangE_D_Protokoll.md), Abschnitt „Der
> ClosedXML-Befund und der Entscheid".

### 3.4 Was Stufe 0 als Erstes braucht

Eine **Wache über die Blattstruktur**, und zwar vor dem Parameterblock. Zuschnitt:

* ein Kern-Fall, der `ExcelBerichtGenerator.Erzeuge` in eine temporäre Datei schreibt und pinnt:
  Blattzahl und Blattnamen, je Blatt die Ankerzeilen (Titel, Kopfzeile der Kennzahlentabelle,
  Szenario-Blocküberschriften, Kopf der Mehrjahrestabelle) und die Werte einer festen Ankerzeile;
* dazu das Hausmuster des Kerns: `[Collection("Testdatenbank")]`, `TestDatenbank`-Arbeitskopie und
  Kulturpinnung auf `de-DE` (`EPOS.Kern/CLAUDE.md` § Nachweis) — sonst schlägt der Fall auf dem
  en-US-Windows-Läufer aus dem falschen Grund fehl;
* derselbe Fall ist zugleich die Gegenprobe jeder Stufe („die Mappe zeigt vor und nach der Stufe
  dieselben Werte"), weil er Werte und nicht Formeln vergleicht;
* ein zweiter, kleiner Fall für den **Word**-Generator (Kennzahlentabelle), weil die
  Sichtbarkeitsregel beide Ausgaben gemeinsam trägt und ein einseitiger Wächter sie auseinanderlaufen
  ließe.

Ohne diese Wache ist Stufe 0 nicht abnehmbar: Die Formelfassung gegen die Wertfassung zu halten
setzt voraus, dass die Wertfassung überhaupt festgehalten ist.

---

## 4 Verlauf mit drei Szenarien (§ 2.13 (5), Punkt 9j)

### 4.1 Was heute geschieht

| Baustein | Gemessenes Verhalten | Beleg |
|---|---|---|
| `WirtschaftlichkeitCtrl.BerechneVerlauf` | **ein** Szenario je Lauf; Szenario wird als Zeichenkette genommen, `FuerSzenario(...).Kopie()` gebildet, Horizont überschrieben; zwei Reihenlisten (absolut, Differenz gegen die gewählte Referenz) | `WirtschaftlichkeitCtrl.cs:1668`, `:1682-1760` |
| `WirtschaftlichkeitVerlauf` | trägt genau **ein** `Szenario`-Feld und zwei Listen | `WirtschaftlichkeitDaten.cs:636-645` |
| `ChartRenderer.VerlaufsReihen` | Farbe **nach laufendem Reihenindex** `C_SERIEN[i++ % 8]`; Reihenname = `s.Anzeige`, also nur der Projektname; dritter Parameter `stammGestrichelt` (Vorgabe `false`) | `ChartRenderer.cs:574-591`, Palette `:553-562` (acht Farben) |
| `ChartRenderer.KapitalwertVerlauf` | festes Maß **1240 × 620**; Zeichenrechteck 110/80/(W−150)/400; Legende bei `110, H−104, W−30` **mit Umbruch**, Fußnote bei `H−28` → Platz für **zwei** Legendenzeilen; liest `Reihe.Gestrichelt` für Linie und Legendenfeld; kein Flächenband | `:600`, `:684-692`, `:703-704`; Legende `:4019-4047` |
| Wortbericht | zeichnet **beide** Bilder; das absolute mit gestrichelter Stammlinie (U18), im Dokument 620 × 310 | `BausteineWirtschaftlichkeit.cs:245-273`, `:271` |
| Dialog | `KapitalwertVerlaufDialog.razor` zeigt beide Bilder, Jahre 2…60, Szenario-Klappliste | `EPOS.UI/Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufDialog.razor:45-62` |
| Tabellenbericht | Mehrjahresblock „Jahr, je Projekt eine Spalte, dann die Δ-Spalten", **nur Erwartet** | `ExcelBerichtGenerator.cs:662-700` (`:663` fest `ERWARTET`) |

### 4.2 Was die Dreierreihe zusätzlich braucht

1. **Drei Läufe oder ein Sammelmodell.** `BerechneVerlauf` dreimal zu rufen ist der kleinere Eingriff;
   die Berichtsdaten werden ohnehin einmal gesammelt, teuer ist nur die Zahlungsbildrechnung
   (`RechneBild` je Variante).
2. **Reihenbildung Variante × Szenario.** Heute sind Farbe und Name beide an der Reihe und beide
   falsch für den Zweck: dasselbe Projekt in drei Szenarien bekäme drei beliebige Farben und dreimal
   denselben Namen. Nötig ist eine Überladung, die die Farbe am **Projekt** und die Strichart am
   **Szenario** festmacht.
3. **Eine dritte Strichart — im Konzept noch nicht genannt.** `Reihe.Gestrichelt` ist ein `bool`
   (`ChartRenderer.cs:106`, Segment `:75`) und trägt genau zwei Zustände (voll / 8-5-Strichel,
   `:688`, `:4034`). Drei Szenarien brauchen drei unterscheidbare Stricharten; entweder wird das Feld
   zu einer Aufzählung (`Voll` / `Strich` / `Punkt`) oder es kommt ein zweites Feld dazu. **Beide Wege
   müssen die Vorgabe byte-gleich halten** — das ist die Kernregel für neue Renderer-Parameter
   (`EPOS.Kern/CLAUDE.md` § Bericht).
4. **Zweigeteilte Legende und größeres Bildmaß.** Bei n Varianten + 3 Szenarien passen n + 3
   Einträge in zwei Zeilen nur bis etwa n = 4; darüber wächst die Legende in den Fußnotenbereich.
   `Legende` liefert ihre Höhe bereits zurück (`:4047`), und `LegendenHoehe` (`:4066/:4087`) ist die
   prüfbare Fassung derselben Regel — der Umbau ist also messbar, ohne Bildpunkte zu lesen.
5. **Spaltengruppen je Szenario im Tabellenbericht**, ein zusätzliches Bild im Wortbericht.

### 4.3 Plattformfreiheit

**Rechnen und Zeichnen liegen bereits plattformfrei** — `WirtschaftlichkeitCtrl.BerechneVerlauf` und
`ChartRenderer` im Kern (SkiaSharp, ohne Windows-API). **Nicht plattformfrei ist die Ablauffolge:**

* Die Verlaufs-Hülle (sammeln → rechnen → zwei Bilder) steht in
  `WindowsFormsApplication1/Views/Wirtschaftlichkeit/KapitalwertVerlaufHuelle.cs:149`, `:188-204`.
  `EPOS.UI/Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufBilder.cs` ist nur der Transportsatz.
* In `EPOS.UI.Daten` gibt es **keine** Wirtschaftlichkeits-Hülle — der Ordner führt nur
  `Allgemein`, `Assistent`, `Bedarf`, `Kosten`, `Projekt`, `Pufferspeicher`, `Simulation`,
  `Stromspeicher`.
* Dasselbe gilt für die Zeilenliste der Seite: sie entsteht in
  `WindowsFormsApplication1/Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs:618`, weshalb
  kein bunit-Fall sie greifen kann (Status „Nach #346 (b)").

### 4.4 ChartProben

Alles Verlangte ist vorhanden und läuft in der CI (`kern.yml`, `CLAUDE.md:166`):

| Probe | Ort |
|---|---|
| `kapitalwert_differenz` | `Proben/ChartProben/Program.cs:147` |
| `kapitalwert_absolut` | `:154` |
| `kapitalwert_absolut_legende` (gestrichelte Stammlinie) | `:164` |
| Gegenprobe `kapitalwert_verlauf_gestrichelt_wirkt` | `:1119` |
| Gegenprobe `kapitalwert_verlauf_legende_nennt_die_version` | `:1129` |

Die Bilder liegen **nicht** im Repositorium — `Proben/ChartProben/` führt nur `Program.cs`, `.csproj`
und `.sln`; die Dateien entstehen beim Lauf im Zielordner. Für die Dreierreihe kommen zwei Proben
hinzu: ein Bild mit drei Stricharten und eine Gegenprobe „die dritte Strichart wirkt" nach demselben
Muster wie `kapitalwert_verlauf_gestrichelt_wirkt`.

---

## 5 Bandbreite und Empfehlung

### 5.1 G8 — Bandbreite im Bericht

Die Tafel steht im Wortbericht (`BausteineWirtschaftlichkeit.cs:923-995`) mit **sechs** Spalten:
Variante · ΔKW Worst · ΔKW Erwartet · ΔKW Best · Amortisation · Einstufung (`:943-948`). Darunter die
Annahmen je Szenario (`SchreibeSzenarioAnnahmen`) und der Vorschlagssatz. Excel führt dieselbe
Auskunft als drei Blöcke mit Annahmenzeile je Blocküberschrift (`ExcelBerichtGenerator.cs:451-478`)
und den Vorschlag darunter (`:568`).

**Es fehlen** die Spalte „Spanne" und die Referenzzeile — der Stamm ist ausdrücklich ausgeschlossen
(`:926`). Auf der Seite gibt es die Tafel gar nicht. Beides ist als `03/#72` belegt und dort
vollständig beschrieben; hier nur die Einordnung: Die **Spanne** ist eine reine Ableitung
(Best − Worst) und braucht kein Rechenwerk, die **Referenzzeile** braucht die Entscheidung, welche
Zahl in ihren ΔKW-Spalten steht (0 oder Gedankenstrich) — das ist ein Anzeigeentscheid, kein
Rechenentscheid.

### 5.2 G9 — Empfehlungsregel

`WirtschaftlichkeitEmpfehlung` (`WirtschaftlichkeitEmpfehlung.cs:116`) bildet die Regel genau wie
§ 9.1: `Einstufungen` `:125-160` sammelt je Variante die drei ΔKW, `Stufen` `:165-185` urteilt
(ΔKW_Erwartet ≤ 0 → nicht; ohne Bandbreite → empfohlen mit Zusatz; sonst empfohlen nur bei
Worst > 0 **und** Best > 0), `Vorschlag` `:190-200` nimmt die höchste Erwartet-Differenz zuerst unter
den empfohlenen, dann unter den bedingten. Drei Ausgaben ziehen dieselbe Regel (Word, Excel, Seite,
siehe § 1.3). Abdeckung: `ValeriLueckenTests.cs:274-383`.

**Ein Nachzug ist fällig.** Maßstab ist `KapitalwertDiff`; seit § 2.9 / #358 rechnet diese Größe gegen
die **gewählte Referenz**, nicht mehr fest gegen den Stamm. Der Ressourcentext
`WIRT_EMPF_KEINE` sagt aber weiterhin „Keine Variante ist gegenüber dem **Stammprojekt**
wirtschaftlich; Weiterbetrieb (Referenzfall)" (`EPOS.Kern/MyResource/Resource.resx:16084`), und
Szenarienkonzept § 9.1 formuliert ebenso. Bei gewählter Variantenreferenz ist der Satz falsch — die
Referenz muss beim Namen genannt werden, wie `Referenzwahl.Deklarationszeile` es im Paarvergleich
schon tut.

### 5.3 G7 — Betrachtungszeitraum gegen die Nutzungsdauern

Umgesetzt, aber **nur in zwei von drei Ausgaben**: Wortbericht `BausteineWirtschaftlichkeit.cs:1051`
und Windows-Seite `WirtschaftlichkeitSeiteGaben.cs:413`; im Excel-Blatt kommt
`NutzungsdauerAbgleich.Hinweis` nicht vor. Inhaltlich zählt er kürzeste und längste **gepflegte**
Dauer, das früheste Ersatzjahr und wählt zwischen Restwert-, Ersatz- und Deckungsgleich-Satz
(`WirtschaftlichkeitEmpfehlung.cs:285-317`); die Zahl der Positionen **ohne** Dauer fehlt (§ 2.3).

### 5.4 Sensitivität

Fünf Zeilen, wie das Konzept sagt: vier feste Ausschläge (`WirtschaftlichkeitCtrl.cs:238-241`:
Zins ± 1 %-Pkt, p_E ± 1 %-Pkt, Investition ± 10 %, Energiekosten ± 10 %) plus „KWKG-Bonus entfällt"
(`:5188-5192`), gebildet in `BaueSensitivitaet` `:5146-5197`, nur für Erwartet (`:1640-1645`).
Ausgabe: Word `BausteineWirtschaftlichkeit.cs:866-898` (Spalten −Δ · Basis · +Δ, im Kopfkommentar noch
als „4 Parameterzeilen" beschrieben, obwohl die fünfte Zeile mitläuft), Excel `:723-760`.
**Es fehlen** die Steigungsspalte €/%, das Liniendiagramm, die T-Variation und die Endzahlungen
(V-G6).

### 5.5 Deklarationszeilen und IZF-Warnung (V-A)

**Fehlen beide.** Gebaut sind nur die zwei Ausweissätze aus § 9.4: `ValeriAusweis.Vereinfachungen()`
(G1/G3/G5) und der G10-Satz, beide in `WirtschaftlichkeitEmpfehlung.cs` unter `ValeriAusweis`. Die
vier von der Norm verlangten Deklarationen (nominal · Steuern · Restwert · Risiko) und die
IZF-Mehrdeutigkeitswarnung haben weder Ressource noch Codeort. Vorhanden ist außerdem
`Referenzwahl.Deklarationszeile` (`ExcelBerichtGenerator.cs:437-443`,
`BausteineWirtschaftlichkeit.cs:690-695`) — das ist jedoch die **Benennung der Referenz** in Sicht 2
(VG‑Q4), nicht die Normdeklaration; die Namensähnlichkeit ist eine Verwechslungsfalle für die
Umsetzung.

---

## 6 Eine Sichtbarkeitsregel für drei Ausgaben (§ 6.1 B7)

### 6.1 Die Regel hält

Alle drei Ausgaben rufen dieselbe Kette `WirtschaftlichkeitZeilen.Sichtbare(Kennzahlen(...), alle)`:

| Ausgabe | Stelle |
|---|---|
| Wortbericht | `BausteineWirtschaftlichkeit.cs:664` |
| Excel-Blatt | `ExcelBerichtGenerator.cs:428` |
| Seite (Windows-Gabe) | `WirtschaftlichkeitSeiteGaben.cs:618` |
| *(zusätzlich)* BHKW-Vorschau | `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor:1155` |

Die Regel selbst steht einmal (`WirtschaftlichkeitZeilen.cs:877-910`) und räumt auch leere
Blocküberschriften und Summen ohne Summanden weg. **Zeilenmenge und Reihenfolge sind damit
identisch**, weil alle drei aus demselben `Kennzahlen`-Katalog schöpfen und keine zweite Filterung
mehr führen (die früheren Zweitfilter sind an allen drei Stellen als entfernt kommentiert).

### 6.2 Nachkommastellen

Jede Zeile trägt **zwei** Formatfelder, die paarweise gesetzt werden: `WirtZeile.Format` für Word und
Seite, `WirtZeile.ExcelFormat` für Excel (`WirtschaftlichkeitZeilen.cs:37/40`). Gemessene Paare:
Vorgabe `N0` / `#,##0`; Arbeitswert `:479` `N2` / `#,##0.00`; Amortisation `:619` und IZF `:625`
`N1` / `#,##0.0`; Gestehungskosten `:631` `N3` / `#,##0.000`. **Keine Abweichung im Kennzahlengrid.**
Das Risiko liegt in der Kopplung: Die beiden Felder sind nur durch Disziplin verbunden, ein Wächter
(„zu jedem `Format` das passende `ExcelFormat`") existiert nicht — das wäre ein billiger Zusatzfall
für die Blattstruktur-Wache aus § 3.4.

Der offene Punkt **„Nach #342"** (Status Z. 402) betrifft **nicht** `WirtZeile`, sondern die
KWKG-Satzfelder: Das Dialogfeld führt vier Stellen, Excel-Bericht (`#,##0.00`), Textbaustein
(`k.F(…, 2)`), die Herleitungszeile (`N2`) und die `N2`-Texte des `KwkgSatzRechner` zeigen zwei.
Empfehlung der Statuszeile: als **eine** Entscheidung in einem Zug auf vier Stellen. Für den
Berichtsteil heißt das: Rechner-Herleitung, Excel-Zahlformat und Dialogzeile gemeinsam ändern, sonst
entstehen drei Wahrheiten über dieselbe Zahl.

### 6.3 Abweichungen, die bleiben

| Abweichung | Stand | Beleg |
|---|---|---|
| Excel lässt die **Referenzzelle** einer Differenzkennzahl leer, Word und Seite schreiben „—" | **bewusst** (Divergenz D5: Wertspalten bleiben numerisch für Filter und Diagramme) | `ExcelBerichtGenerator.cs:526-530` |
| **Szenarioabdeckung**: Word druckt die Kennzahlentabelle nur für **Erwartet**, Excel druckt **drei Blöcke**, die Seite zeigt das gewählte Szenario | unentschieden, nirgends als Entscheid vermerkt | Word `BausteineWirtschaftlichkeit.cs:117`, Excel `:451` |
| **G7-Zeitraumhinweis** fehlt im Excel-Blatt | Lücke | § 5.3 |
| **Hinweis- und Fehlgrundzeilen** hängt die Seite als eigene Matrixzeilen an; der Wortbericht sammelt sie am Kapitelende | unterschiedliche Form, gleiche Auskunft | Seite `WirtschaftlichkeitSeiteGaben.cs:637-645`; Word verweist auf „Hinweise am Kapitelende" `BausteineWirtschaftlichkeit.cs:262` |
| Die Zeilenliste der Seite entsteht in der **Windows-Schale** (Datenbankzugriff), nicht im Kern — kein bunit-Fall möglich | offen, Kandidat für einen Kern-Controller | Status „Nach #346 (b)" |

---

## 7 Anhang-E-Checkliste und Anhang-D-Gegenprobe (V-D)

**Nichts davon existiert.** `grep -i "Anhang D|Anhang E|Fallstudie|Checkliste"` über `.cs`, `.razor`
und `.resx` liefert ausschließlich Fremdtreffer (Access-Checkliste in `SchemaMigration.cs`,
Formularkarte im `Werkzeuge`-Zweig). In den Papieren steht die Checkliste nur als Vorhaben
(Konzept Z. 603, 655, 683, 1131; Rechenweg 08 Z. 172-174). Der Knopf „Anhang-E-Checkliste…" ist im
Mockup gezeichnet und hat weder Element noch Ressourcenschlüssel noch U-Nummer (`03/§ 5 A`).

**Die Annahme des Auftrags, die Anhang-D-Daten lägen nicht im Repositorium, trifft nicht zu.** Es gibt
`Quellen/VALERI/` mit zwei Dateien: dem Normtext `DIN EN 17463 - DIN.pdf` (3,6 MB, echte PDF, kein
LFS-Zeiger) und `VALERI_Vorlage_V7.xlsx` (336 kB, echte Mappe). Beide sind im Repositorium und werden
im Aufräumkonzept ausdrücklich als dorthin verschoben geführt
(`Dokumentation/aktuell/Konzept_Repository_Aufraeumen_EPOS-Plan.md:68-70`); `KapitalwertRechner.cs:9`
nennt `VALERI_Vorlage_V7` als Prüfvorbild. *(Gelesen habe ich nur die Dateiköpfe — der Inhalt des
Normtexts ist urheberrechtlich geschützt und war für diese Feststellung nicht nötig.)*

**Was die Gegenprobe braucht** (Vorschlag, nicht gemessen):

1. **Die Zahlen aus Anhang D als Prüfvorrichtung**, nicht als Projekt: Die Fallstudie ist ein reines
   Zahlungsstromproblem (90 kW_th, T = 18 a). Sie gehört deshalb gegen
   `KapitalwertRechner.Rechne` — mit von Hand gesetzter `ProjektEingabe` —, **nicht** gegen die
   volle Kette, denn EPOS bezieht seine Mengen aus dem Stundenlauf, den die Norm nicht liefert. Das
   ist zugleich der billigste Weg und der einzige, der die Abweichung eindeutig dem Rechenkern
   zuordnet.
2. **Drei Sollwerte** (NPV, Worst, Best) und eine Toleranz, die die Rundung der Norm aufnimmt.
3. **Die dokumentierte Falle mitnehmen:** Zwei Zeilen der Sensitivitätstafel D.6 tragen im Normtext
   Werte des Pumpenbeispiels (Konzept Z. 657-660) — sie gehören ausdrücklich **nicht** in die
   Prüfvorrichtung, sonst ist der Fall dauerhaft rot aus dem falschen Grund.
4. **Die Anhang-E-Checkliste** braucht nur einen Textkatalog (15 Punkte, Note 1–5) plus eine
   Berichtsseite; sie rechnet nichts und ist die einzige V-D-Teilaufgabe ohne technische
   Vorbedingung.
5. `VALERI_Vorlage_V7.xlsx` ist zugleich das natürliche Muster für das **Anhang-A-Raster** der
   Stufen 0 und 1 — und die Mappe, deren Formelweise (`=E44*1,3`) das Szenarienkonzept bewusst
   **nicht** übernommen hat (§ 7.3 Schlussabsatz). Beim Formelbericht ist also zu trennen:
   Raster übernehmen, Szenariologik nicht.

---

## 8 Umsetzungsliste dieses Teils

| # | Punkt | Größe | Rechenwirkung | Nachweis | Abhängigkeit | Plattform |
|---|---|---|---|---|---|---|
| B1 | **Wache über die Blattstruktur** (Excel + Word), § 3.4 | M | keine | Berichtsprobe (neu), Kern-Tests | — | plattformfrei |
| B2 | Stufe 0: Parameterblock aus echten Zellen, absolute Bezüge | M | keine | Berichtsprobe vorher/nachher | B1 | plattformfrei |
| B3 | Stufe 1: Mehrjahrestabelle als Formeln (Energie, Netto, Barwert, Kumuliert, Betrieb zweiteilig) | L | keine (Werte müssen gleich bleiben) | Berichtsprobe, A/B gegen die Wertfassung | B2 | plattformfrei |
| B4 | Stufe 2: NBW/RMZ, IZF und Amortisation über eine Differenzreihe je Variante | L | keine | Berichtsprobe, Kern-Fall gegen `KapitalwertRechner` | B3 | plattformfrei |
| B5 | Stufe 3: Betriebskostenblock Menge × Satz, Delta-Block des Vergleichsblatts | S | keine | Berichtsprobe | B3 | plattformfrei |
| V1 | **V-A**: „nachrichtlich"-Label, vier Deklarationszeilen, IZF-Mehrdeutigkeitswarnung, Steigungsspalte €/% | M | keine (reiner Ausweis) | Kern-Tests (`ValeriLueckenTests` erweitern), Berichtsprobe | — | Kern; Anzeige in allen drei Ausgaben |
| V2 | G8 vervollständigen: Spalte „Spanne", Referenzzeile, Tafel auf die Seite (`03/#72`) | S | keine | Kern-Test, Berichtsprobe, bunit | V1 (gemeinsamer Zeilenkopf) | Kern + Seite |
| V3 | G9-Nachzug: Vorschlagssatz nennt die **gewählte** Referenz statt „Stammprojekt" | S | keine | Kern-Test mit Variantenreferenz | V-B (steht) | Kern (Ressourcen de/en) |
| V4 | G7 auch im Excel-Blatt; Hinweis um „k von n Positionen ohne Dauer" erweitern | S | keine | Kern-Test, Berichtsprobe | N1 | Kern |
| V5 | **V-C**: Umschalter und fünf Blöcke | L | keine | bunit, Sichtprüfung | V1, V2 | `EPOS.UI` + plattformfreie Hülle |
| V6 | **V-E**: vollständige Szenariospalten (Rahmen, Trägerpreise, Erlössätze, Mengenfaktor) | L | **ja**, je Pflege | A/B je Projekt, Referenzlauf, Schemaschritt | V5 (Hinweistext entfällt damit) | Kern + Dialoge |
| V7 | Hinweistext § 2.11.7 bis dahin | S | keine | bunit, Ressourcen de/en | — | Kern-Ressource, Seite |
| N1 | Hinweiszeile („T über Vorgabe, k von n ohne Dauer") auf Seite **und** Bericht, plattformfreie Hülle | M | keine | Kern-Test, Berichtsprobe, bunit | `ErsatzRestwertTafel` (steht) | Hülle nach `EPOS.UI.Daten` |
| N2 | U39 (a): Entkopplung Ersatz / Restwert je Position oder Technik | M | **ja**, je Pflege | Kern-Fall gegen `KapitalwertRechner`, Referenzlauf | Anwenderentscheid | Kern + Kostendialog |
| N3 | U39 (b): geräteeigene Dauerspalten auflösen oder anschließen | M | **ja**, falls angeschlossen | Referenzlauf, Kern-Fall | N2 | Kern |
| N4 | U39 (c): Speicherflotte an `Tab_Nutzungsdauer` | M | **ja**, falls angeschlossen | Kern-Fall, Referenzlauf | N3 | Kern + SpeicherEngine |
| N5 | Gesperrter Zwilling „Nutzungsdauern vorbelegen…" auf der Betriebsseite (`03/§ 5 B`, Befund 26) | S | keine | bunit | — | `EPOS.UI` |
| K1 | **Verlauf**: dritte Strichart (`Reihe.Gestrichelt` → Aufzählung), Vorgabe byte-gleich | S | keine | **ChartProben** (Bild + Gegenprobe) | — | Kern |
| K2 | `VerlaufsReihen`-Überladung: Farbe = Variante, Strichart = Szenario, Name mit Szenario | S | keine | ChartProben | K1 | Kern |
| K3 | Dreierlauf (`BerechneVerlauf` dreimal oder Sammelmodell) | M | keine | Kern-Test, A/B gegen den Einzellauf | K2 | Kern |
| K4 | Bildmaß und zweigeteilte Legende | S | keine | ChartProben, `LegendenHoehe`-Fall | K2 | Kern |
| K5 | Verlaufs-Hülle plattformfrei nach `EPOS.UI.Daten`; Knopf „Verlauf…" entfällt | M | keine | bunit, Sichtprüfung | K3, V5 | Umzug aus der Windows-Schale |
| K6 | Spaltengruppen je Szenario im Tabellenbericht, zweites Bild im Wortbericht | M | keine | Berichtsprobe | K3, B1 | Kern |
| A1 | Anhang-E-Checkliste als Berichtsseite (15 Punkte, Note 1–5) | S | keine | Berichtsprobe | B1 | Kern |
| A2 | Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne`, Daten aus `Quellen/VALERI/` | M | keine | Kern-Fall mit Sollwerten | — | plattformfrei |
| S1 | Wächter „zu jedem `Format` das passende `ExcelFormat`" | S | keine | Kern-Test | B1 | Kern |
| S2 | „Nach #342": vier Nachkommastellen in Rechner-Herleitung, Excel und Dialogzeile in einem Zug | S | keine (Anzeige), aber Nachrechenbarkeit | Kern-Tests, Berichtsprobe | Anwenderentscheid | Kern + Dialog |

**Reihenfolge-Vorschlag:** B1 zuerst (ohne die Wache ist nichts am Bericht abnehmbar), dann V1/V2/V3/V4
als eine Ausweiswelle, dann K1–K4 (klein, in sich geschlossen, ChartProben tragen den Nachweis),
dann N1 und die U39-Entscheide, dann B2–B5, zuletzt V5/V6 und K5/K6.

### 8.1 Zu berichtigende Konzeptstellen

| Papier | Zeile | alt | neu |
|---|---|---|---|
| Konzept konsolidiert | 968-970 (§ 2.13 (3) Punkt 2) | „der … Knopf ‚Nutzungsdauern vorbelegen' für bestehende Positionen ist **nicht gebaut**" | gebaut seit #357 (`KostenKomponenteDialog.razor:310`); offen bleibt allein der gesperrte Zwilling auf der Betriebsseite (`03/§ 5 B`, Befund 26) |
| Konzept konsolidiert | 971-972 (Punkt 3) | „`NutzungsdauerID` wird ausschließlich bei der Vorlagenübernahme gesetzt, **kein Dialog** lässt sie wählen" | seit #357 Klappliste im Zeileneditor über `KostenProjektPositionenCtrl.NutzungsdauerArtZuordnen` |
| Konzept konsolidiert | 654 (V-G11) | „fehlt" | Freitext umgesetzt (W5‑B‑12/G6); **es fehlen Kategorie und Beurteilung** nach Dauer × Wirkung |
| Konzept konsolidiert | 741-742 (§ 2.11.6) | „eine Klasse, **1 380 Zeilen**, vier Blattarten" | 1 412 Zeilen (19.09.2026); „vier Blattarten" bleibt richtig — das fünfte `Worksheets.Add("Probe")` ist die Schrift-Messprobe |
| Konzept konsolidiert | 794 (§ 2.11.6) | „Im Bestand deckt **kein Test den Excel-Generator** ab" | kein Test deckt **Excel und Word** ab; die Wache gehört über beide Blattstrukturen |
| Konzept konsolidiert | 1002-1003 (§ 2.13 (5)) | „Platz für die zweigeteilte Legende und ein passendes Bildmaß (das Lesen von `Gestrichelt` … steht)" | ergänzen: `Reihe.Gestrichelt` ist ein `bool` (`ChartRenderer.cs:106`) und trägt **zwei** Stricharten — drei Szenarien brauchen eine dritte |
| Konzept konsolidiert | 999-1000 (§ 2.13 (5)) | „heute vergibt `VerlaufsReihen` Farben nach laufendem Index" | bestätigt; ergänzen: die Palette hat **acht** Farben (`ChartRenderer.cs:553-562`), mit Farbe = Variante reicht sie bis acht Varianten |
| Szenarienkonzept | 322-337 (§ 9.1) | „Maßstab ist die Kapitalwertdifferenz **zum Stamm**" | zur gewählten **Referenz** (§ 2.9 seit #358); Ressource `WIRT_EMPF_KEINE` nennt weiterhin das Stammprojekt und ist nachzuziehen |
| Szenarienkonzept | 240 (§ 7.1, Zeile „Referenzfall") | „Stammprojekt; Varianten werden als Differenz zum Stamm bewertet" | wählbare Referenz je Vergleichsgruppe |
| Rechenweg 08 | 39 | nennt die Knöpfe „Anhang-E-Checkliste…", „XLSX mit Formeln exportieren…" ohne Stand | als **Mockup-Knöpfe ohne Element und ohne Ressourcenschlüssel** kennzeichnen (`03/§ 5 A`) |

### 8.2 Widersprüchliche Etappenbezeichnungen

Das ist der gefährlichste Punkt dieses Teils, weil er zu stillen Doppelarbeiten führt.

1. **Zwei Etappenreihen für dieselben Arbeiten.** Das konsolidierte Konzept führt **V-A…V-E**
   (Z. 678-684), das Szenarienkonzept **W5‑B‑9 bis W5‑B‑12** (Z. 30, 226, 308, 387). Sie überlappen
   ohne Zuordnung: V-B entspricht der Etappe „VG" der Statuszeile #358 (in **keinem** der beiden
   Schemata benannt), V-E enthält, was W5‑B‑9 und W5‑B‑12 bereits geliefert haben, und V-D deckt sich
   mit V-G10 aus § 11. **Vorschlag:** in § 2.11.4 eine Spalte „entspricht / bereits geliefert durch"
   ergänzen und in Szenarienkonzept § 11 die Rückverweise auf V-A…V-E setzen.
2. **Kollidierende Lückennummern.** Konsolidiert nummeriert **V-G1…V-G12**, das Szenarienkonzept
   **G1…G11** — und dieselben Buchstaben meinen Verschiedenes: V-G2 = Degradation gegen G2 =
   Preisänderung je Kostenart; V-G6 = Sensitivität gegen G6 = nicht monetisierbare Wirkungen; V-G11 =
   nicht monetisierbare Wirkungen gegen G11 = investitionsgekoppelte Betriebskosten. Genau hier
   entstand auch der veraltete Eintrag V-G11 „fehlt", obwohl G6 längst umgesetzt ist.
   **Vorschlag:** eine Umsetzungstafel „V-Gn ↔ Gn" an den Anfang von § 2.11.2 — oder, sauberer, das
   Szenarienkonzept auf die V-G-Nummern umstellen, weil das konsolidierte Papier die führende Fassung
   ist.
3. **Zwei Stufenbegriffe für den Formelbericht.** § 2.11.6 zählt Stufen **0–3**, § 11 des
   Szenarienkonzepts spricht von „Stufe 0 ist ein Parameterblock" — gleichbedeutend, aber die
   Stufen 1–3 fehlen dort. Ein Satzverweis genügt.

---

## Nicht geprüft

* **Kein Bau, kein Test, kein Referenzlauf** — die Auftragsregel verbietet beides im Hauptbaum. Alle
  Aussagen über Testabdeckung stützen sich auf Dateiinhalte und Fallzählungen, nicht auf grüne Läufe.
* **Das Laufzeitverhalten von ClosedXML 0.105.1** (zwischengespeicherte Werte, `RecalculateAllFormulas`,
  Funktionsumfang, fremde Tabellenkalkulationen) — als „zu prüfen" gekennzeichnet, § 3.3.
* **Der Inhalt von `Quellen/VALERI/DIN EN 17463 - DIN.pdf` und `VALERI_Vorlage_V7.xlsx`** — nur
  Existenz, Größe und Dateityp gemessen; die Sollwerte der Anhang-D-Fallstudie habe ich aus dem
  Konzept übernommen, nicht aus der Norm nachgeschlagen.
* **Die Mockups selbst** — sie sind heute geprüft (Befundpapier, Protokolle `00…06`); ich habe nur
  daraus zitiert.
* **Die Blazor-Seite `WirtschaftlichkeitSeite.razor` im Einzelnen** — ihre Zeilen kommen aus der
  Windows-Gabe, deshalb wurde diese gemessen; die Darstellungsseite selbst ist über `03/#71…#79`
  bereits erfasst.
* **`AbweichungsErmittler`, `KomponentenVergleich`, `ZeitreihenExtraktor`, `ProjektDetails`,
  `SpeicherBetriebsbild`, `PeakShavingBild`** — Berichtsbausteine außerhalb dieses Teils.
* **Die iOS-Schale (`EPOS.iOS`)** — nur die Frage „liegt die Logik plattformfrei" wurde beantwortet,
  nicht, ob ein Umzug dort baut.
* **Die englischen Ressourcentexte** — nur die deutschen Werte der genannten Schlüssel wurden gelesen.

**Nebenbefund am Arbeitsbaum, nicht von mir verursacht:** `Dokumentation/aktuell/Status_iOS_Migration.md`
steht als `UU` (unaufgelöste Zusammenführung) und trägt **sechs Konfliktmarken** in zwei Blöcken
(Z. 289-299 und Z. 438-450). Die von mir zitierten Statuszeilen liegen außerhalb dieser Blöcke —
nur die Zeile **#369** (Z. 290) steht im oberen Block auf der `Updated upstream`-Seite; ich habe sie
nicht nach Zeilennummer zitiert. Wer die Datei fortschreibt, löst den Konflikt zuerst auf, sonst
entstehen doppelte Statusnummern. Ich habe im Repositorium nichts geändert (nur `sed`, `grep`, `wc`,
`ls`, `find`, `od`); `AGENT_LAEUFT` blieb unberührt.
