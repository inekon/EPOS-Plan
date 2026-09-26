# Konzept Berichtsvorlagen mit Platzhaltern — Word- und Excel-Bericht aus einer Vorlage (EPOS-Plan)

Stand 26.09.2026, Codestand aa198657 (Zweig claude/intelligent-bohr-hthrk8), Rev. 8 — BV-E6 umgesetzt (#544, Protokoll `../ueberholt/Protokolle/Bericht/BV_E6_Kennzeichnung_Protokoll.md`): gebauter Stand und Abweichungen der Kennzeichnung in der App eingearbeitet (Einleitung, 9.4–9.7, 13, 15.2, Anhang A); Rev. 7 — BV-E5 umgesetzt (#541, Protokoll `../ueberholt/Protokolle/Bericht/BV_E5_Tabellen_Bilder_Protokoll.md`): Entscheide BV-E5-1 bis BV-E5-5 und gebauter Stand eingearbeitet (Einleitung, 5.3, 5.4, 6.3, 6.4, 6.5, 10.2, 13, 14, 15.2, Anhang A, Anhang B); Rev. 6 — BV-E4 umgesetzt (#532, Protokoll `../ueberholt/Protokolle/Bericht/BV_E4_Bloecke_Protokoll.md`): Entscheide BV-E4-1 bis BV-E4-4 und gebauter Stand eingearbeitet (Einleitung, 9.5, 13, 14, 15.2, Anhang A); Rev. 5 — BV-E3 umgesetzt (#528, Protokoll `../ueberholt/Protokolle/Bericht/BV_E3_Wertesatz_Protokoll.md`): Wertesatz `BerichtsDaten.Wirtschaft`, Bedarf des Laufs und Regel der besten Variante eingearbeitet (2.4, 5.1, 8.5, 9.5, 12, 13, 15.2, Anhang A); Rev. 4 — BV-E2 umgesetzt (#520, Protokoll `../ueberholt/Protokolle/Bericht/BV_E2_Kapitel_Protokoll.md`): Entscheid BV-E2-1 und gebauter Stand eingearbeitet (4.8, 5.3, 6.3, 6.5, 10.2, 10.3, 11, 13, 14, 15.2, Anhang A, Anhang B.3); Rev. 3 — BV-E1 umgesetzt (#512, Protokoll `../ueberholt/Protokolle/Bericht/BV_E1_Vorlagenwahl_Protokoll.md`): Entscheid BV-E1-1 und gebauter Stand eingearbeitet (6.3, 8.4, 10.2, 10.3, 11, 13, 15.2, Anhang B.3); Rev. 2 — Entscheide vom 25.09.2026 eingearbeitet (Abschnitt 14), BV-E0 umgesetzt (#500, Protokoll `../ueberholt/Protokolle/Bericht/BV_E0_Grundlagen_Protokoll.md`); Kennungen BV-Q (Entscheidfragen), BV-E (Etappen), BV-P (Platzhalterklassen).

**Geltungsbereich.** Das Papier legt fest, wie EPOS-Plan den Word- und den Excel-Bericht künftig
aus einer Vorlage füllt, die der Anwender in Word bzw. Excel selbst pflegt, und wie er in der App
erkennt, welcher Platzhalter zu einem Feld, einer Tabelle oder einem Diagramm gehört. Anlass ist
der Anwenderauftrag vom 24.09.2026:

> Berichterstellung in Word/Excel nicht mehr komplett generieren, sondern Vorlage mit definierten
> Platzhaltern für Texte, Tabellen, Grafiken; Platzhalter in der App elegant erkennen.

Dazu der Nachtrag desselben Tages, mit einem Bildschirmfoto der heutigen Berichtsseite:

> Die Anforderungen für die Berichterstellung werden beibehalten: Simulation aller
> Berichtsrelevanter Varianten zuvor ausgeführt. Start in Bereich Berichte&Kosten

Am 25.09.2026 hat der Anwender die Entscheidfragen BV-Q1 bis BV-Q19 entschieden — nach Empfehlung, mit fünf
Änderungen (Abschnitt 14) — und BV-E0 gestartet. BV-E0 ist mit #500 umgesetzt, BV-E1 mit #512, BV-E2 mit #520,
BV-E3 mit #528, BV-E4 mit #532, BV-E5 mit #541, BV-E6 mit #544 (Abschnitt 13); für BV-E1 gilt der Entscheid BV-E1-1 (Auftraggeber, vom Anwender am 26.09.2026 bestätigt): Die Stilvorlage `Berichtsvorlage.docx` bleibt neben der
Standardvorlage `Berichtsvorlage_Standard.docx`, eine Übernahme der Altdatei gibt es nicht (6.3, 10.3). Für BV-E2 gilt der
Anwenderentscheid BV-E2-1 vom 26.09.2026: Das Logo der Kopfzeile ist ein Bildplatzhalter, gefüllt aus der Einstellung
`BerichtLogo` (6.5, 14). Für BV-E4 gelten die Anwenderentscheide BV-E4-1 bis BV-E4-4 vom 26.09.2026 (Leitversion,
Minimum und Maximum, Parameter in Prozent, Fassung der Standardvorlage; Abschnitt 14), für BV-E5 die Anwenderentscheide
BV-E5-1 bis BV-E5-5 vom 26.09.2026 (Bilder unter der Mindestbreite, keine Paarbilder, Deckung der Kapitel, Paarsicht im
Baukasten, Teilen auf iOS; Abschnitt 14).

Nicht Gegenstand sind Rechenweg, Simulation, Wirtschaftlichkeitsrechnung, Datenbankschema und
Referenzbasis; keine Etappe friert eine Basis neu ein.
**Mockup:** `Dokumentation/aktuell/Mockups/Berichtsvorlagen_Platzhaltermarke.html` (Abschnitt 9.8).


## 1 Die Frage und das Ziel

Heute erzeugt EPOS-Plan den Word-Bericht vollständig im Code: Es kopiert eine Vorlage, leert ihren
Rumpf und schreibt neun Bausteine in fester Folge hinein; die Excel-Mappe entsteht leer. Deckblatt,
Gliederung, Tabellenbild, Kopf- und Fußzeile kann der Anwender nicht gestalten. Die Frage: Wie wird
daraus ein Bericht, dessen Form dem Anwender gehört und dessen Inhalte EPOS-Plan liefert?

1. Der Anwender pflegt eine gewöhnliche `.docx`- oder `.xlsx`-Datei; Text, Gliederung, Logo, Kopf- und
   Fußzeile, Tabellen- und Seitenbild gehören ihm.
2. EPOS-Plan kennt definierte Platzhalter für Texte, Zahlen, Datumsangaben, Tabellen, Diagramme, Listen
   und ganze Kapitel und füllt sie.
3. In der App erkennt der Anwender ohne Störung, welcher Platzhalter zu einem Element gehört — wo es in
   der App ein Gegenstück gibt (Abschnitt 9).
4. **Die bestehenden Anforderungen bleiben:** Einstieg ist „Berichte & Kosten › Bericht“, und vor jeder
   Ausgabe werden alle gewählten Varianten simuliert und wirtschaftlich bewertet
   (`BerichtsDatenSammler.SammleFuerBericht`; Nutzeranforderung 15.08.2026,
   `EPOS.UI/Seiten/Berichte/BerichtSeite.razor:8-11`) — ohne Option.
5. Ohne eigene Vorlage entsteht der heutige Bericht, strukturgleich und belegt durch eine Messlatte.

**Leitsatz:** Jede Etappe ist für sich nutzbar und lässt den Standardweg unverändert.


## 2 Ausgangslage am Code

Abkürzungen: WBG = `EPOS.Kern/Allgemein/Bericht/WordBerichtGenerator.cs`, EBG = `…/ExcelBerichtGenerator.cs`,
BS/BP/BV/BW = `…/Bericht/Bausteine/BausteineStandard|Projekt|Vergleich|Wirtschaftlichkeit.cs`,
AE = `…/AnhangECheckliste.cs`, BD = `…/BerichtsDaten.cs`, BDS = `…/BerichtsDatenSammler.cs`,
KK = `…/KennzahlenKatalog.cs`, BT = `…/BerichtTexte.cs`, CR = `…/ChartRenderer.cs`,
WZ = `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs`,
BSG = `EPOS.UI.Daten/Bericht/BerichtSeiteGaben.cs`, WS = `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor`.

### 2.1 Word-Pipeline

| Schritt | Heute | Fundstelle |
|---|---|---|
| Lauf | Zeitreihen nach Häkchen; `SammleFuerBericht` unter Kulturweitergabe, rechnet immer frisch; erst Word, dann Excel | BSG:207-245; BDS:172-191, 446 ff. |
| Datei | `<Stamm>_Bericht_<yyyy-MM-dd>.docx` im Zielordner oder `Dienste.Pfade.Dokumente`, Ausweichen `_2 … _20` | `EPOS.Kern/Controller/BerichtCtrl.cs:29-54` |
| Vorlage | `FindeVorlage` nur relativ zu `AppDomain.BaseDirectory`; ohne Datei Ersatzstile ohne Gliederungsebene | WBG:116-127, 74, 139-167 |
| Rumpf | alle Kinder außer der letzten `sectPr` entfernt; `Fuege` setzt vor diese `sectPr` | WBG:78-80, 192-196 |
| Bausteine | neun in fester Folge, acht Häkchen; Anhang E am Schlüssel `B_WIRTSCHAFT` | WBG:96-111; AE:413 |
| Stile | feste IDs `Title`, `Subtitle`, `Heading1–3`, `Normal`, `Hinweis`, `Beschriftung`; `Title`, `Heading1–3` in der Vorlage doppelt (gemessen) | WBG:212, 220-232 |
| Tabellen | im Code, direkt formatiert (Kopf, Stammspalte, Rahmen, 9/7 pt), Blöcke zu drei Varianten, feste Breite `INHALT_B = 9355` (18 Fundstellen) | WBG:28-41, 186, 364-429, 453-461 |
| Bilder | SVG mit PNG-Rückfall über `svgBlip`, Größe fest in Pixeln (meist 620 px), `docPr/@id` ab 1 | WBG:48, 260, 306-360 |
| Felder | `updateFields` immer; TOC-Feld | WBG:85, 129-136, 247-256 |
| Kopf/Fuß | allein aus der Vorlage: „INEKON GmbH · Seite {PAGE} / {NUMPAGES}“ und Feld `DATE \@ "dd.MM.yyyy"` | `word/footer1.xml`, gemessen |
| Sprache | `BerichtTexte.Englisch` global an `Sprache.Nummer`; `T()` mit rund 141 Festtexten; Datumsmuster fest deutsch | BT:18-36; BS:33, BP:30-33, BW:89 |
| Datenzugriff | Wirtschaftlichkeitsbaustein lädt Parameter, Tarif, Strommatrix, Referenzkessel, Erzeuger und rechnet Verlauf und Emissionsbilanz; Anhang E lädt selbst | BW:29, 57, 72, 192, 204, 267, 992, 1319; AE:309-331 |

### 2.2 Excel und Diagramme

Die Mappe entsteht mit `new XLWorkbook()` (EBG:167; EBG:126 ist die Schriftprobe `MessprobeLaeuft`), ohne
Vorlage, Bilder, Diagramme und Excel-Tabellen. Übersicht und Vergleich immer; Wirtschaftlichkeit und
Checkliste nur mit `B_WIRTSCHAFT`, Verlauf nur mit Verlauf, je Stand ein Detailblatt nur mit `B_ERGEBNISSE`
(EBG:170-196). Die **Formelmappe** (`ExcelFormelmappe.cs`) baut das Wirtschaftlichkeitsblatt von Werten auf
Formeln um, rechnet jede Formel gegen und trägt die Ergebnisse per SDK nach (`Nachtragen`, :1299-1336);
`FullCalculationOnLoad` nur bei eigenen Formeln (EBG:201); Namen `Zins_i`, `Zeitraum_T`, `p_E`, `p_B`, `p_I`
mit Szenarioanhang (:36-48, 293-312), sonst harte A1-Bezüge. Mängel: Datum und Brennstoffmenge als Text
(EBG:235, :1764), Δ%-Format `±0,0` (EBG:349), Freeze der Titelzeile im Detailblatt (EBG:1774).

`ChartRenderer` hat 32 Zeichenmethoden auf dem `Zeichenmodell`; der Bericht nutzt 13 Bilder an festen
Aufrufstellen (BV:73-99, 162-224; BP:371-377; BW:290-418, 1223-1226). Farben aus `Farbpalette.Aktuell`
(Farbrollen), Schrift fest (`SvgSchreiber.cs:175`). In der App haben mehrere Bilder ein anderes Gegenstück
(Kuchen gegen `Ring`, Speicherverlauf gegen `Speicherbetrieb`).

### 2.3 Oberfläche, Ablage, Tests

| Thema | Befund | Fundstelle |
|---|---|---|
| Berichtsseite | links Variantenliste (Stamm fest; Art, Bezeichner, Projektname, Stromspeicher, Simulation), Alle/Keine, Ausgabe, Zielordner, Erstellen; rechts acht Häkchen; Hinweis „Jeder Bericht rechnet neu …“; rund dreißig Texte als Einzelparameter; KI-Sicht `BausteineLesen` | `BerichtSeite.razor:65-97, 178-222, 184, 317` |
| Zuordnung App ↔ Bericht | gibt es nicht; `DiagrammSvg.Kennung` ist ein Namensvorsatz der clipPath-Kennungen, kein Fachschlüssel | `DiagrammSvg.razor:196-201` |
| Wurzeln | Windows: jeder Dialog und jede Seite eine eigene `BlazorWebView` mit `Wurzel<T>` (rund 110 Stellen), ein gemeinsames Dienstverzeichnis; iOS: alles in `AppWurzel` | `BlazorDialogForm.cs:154`, `BlazorSeite.cs:145`, `BlazorDienste.cs:38` |
| Zwischenablage | schreibt die Hülle, nie EPOS.UI; keiner der neun Kern-Dienste kennt sie | `KiChatDialog.razor:948-957`, `Gespraechsverlauf.razor:128-135` |
| Vorlagendatei | `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx`, **zwei** Lieferwege (Ausgabeordner → Publish-Baum → Setup; MauiAsset); `Setup/EPOS-Plan.iss:296` ist nur ein Kommentar; `EPOS.Kern/CLAUDE.md:34` führt sie unter „Was mit Absicht NICHT hier liegt“ | `WindowsFormsApplication1.csproj:253-256`, `EPOS.iOS/EPOS.iOS.csproj:122-123` |
| Konfiguration | JSON in `Berichtskonfiguration` (Grundschema, Fremdschlüssel mit `CASCADE` seit Schritt 96); daneben Ad-hoc-DDL ohne Fremdschlüssel | `sql/schema/001_grundschema.sql:14-19`, `ProjektFremdschluessel.cs:111, :205`, `BerichtCtrl.cs:154-170` |
| Pfade, Einstellungen | `IPfade.Dokumente` = „Eigene Dokumente“ bzw. Sandbox-`Documents`; `IEinstellungen` unter Windows in HKCU | `IPfade.cs:61-62`, `RegistryEinstellungen.cs:43` |
| iOS-Dateien | `OrdnerWaehlen` immer `""`; `MitSystemOeffnen` = Teilen-Blatt; Dateifilter nur `.docx`/`.xlsx` | `IosDateiDienst.cs:87, 95-116`, `EPOS.iOS/Dienste/Dateifilter.cs:33-36` |
| Tests | kein Test mit Vorlage; Wachen `BerichtBlattstrukturWacheTests` (:191, :217, :1069, :1115, :1149), `WordBerichtSvgWacheTests` (Validator Office 2007–2021 :176, :323-340), `FormelmappeClosedXmlBefundTests` (:45, :72) | `EPOS.Kern.Tests/` |
| iOS-Bericht | nie gelaufen, iU11 offen | [Status_iOS_Migration.md](Status_iOS_Migration.md) Z. 42; [Umsetzung_iU10_Nachweise.md](Umsetzung_iU10_Nachweise.md) Z. 761-762 |

### 2.4 Befunde, die jede Vorlagenlösung zuerst trifft

Die Wirkungstafel wird mit `k.Body.Append(t)` statt `k.Fuege(t)` eingefügt (BW:678) und landete mit Vorlage
hinter der `sectPr` (in BV-E0 behoben). Zweiter Befund aus BV-E0: `SetzeUpdateFields` stellte
`w:updateFields` mit `PrependChild` vor `w:displayBackgroundShape` der Vorlage — in jeder Office-Fassung
ungültig, ohne Vorlage nie aufgefallen (behoben mit `AddChild` an der Schemastelle). Feste Stil-IDs, DATE-Feld, `docPr/@id` ab 1, feste Breite und Datenbankzugriffe der
Bausteine (2.1) treffen jede fremde Vorlage; die Abschnitte 5, 6 und 11 lösen sie. Die Datenbankzugriffe der
Bausteine sind mit BV-E3 gelöst: Die Schreiber lesen den Wertesatz des Sammlers (5.1).


## 3 Zielbild

Word- und Excel-Bericht sind gewöhnliche Office-Dateien, die der Anwender pflegt. Wo Projektdaten,
Kennzahlen, Tabellen, Diagramme oder Kapitel stehen sollen, schreibt er Platzhalter wie `{{projekt.kunde}}`.
Einstieg bleibt „Berichte & Kosten › Bericht“, wo er auch die Vorlage wählt. Wie heute werden vor jeder
Ausgabe alle gewählten Varianten simuliert und bewertet; die Vorlage wird davor geprüft und danach gefüllt,
und EPOS-Plan nennt jeden Platzhalter, der unbekannt oder leer blieb. Ein **Platzhalterkatalog im Kern** ist
die einzige fachliche Quelle für Engine, Prüfer, Baukasten, Wachen und die Beschreibungen in der App. Ohne
eigene Vorlage entsteht der heutige Bericht aus der mitgelieferten Standardvorlage, einer Datei der Auslieferung
wie heute (BV-Q19).

Wer eine Vorlage pflegt, schaltet in der App die **Platzhalteranzeige** ein: Jedes Element, das im Bericht
ein Gegenstück hat, trägt dann eine kleine Marke am Rand; beim Überfahren nennt sie den Schlüssel, ein Klick
oder Tipp kopiert ihn. Ausgeschaltet trägt kein Feld eine Marke; sichtbar bleibt nur ein ruhiger Umschalter
`{ }` in der Kopfzeile der Ansicht. Die Vorlage trägt Inhalt, Gliederung und Reihenfolge, Tabellenbild und
Bildrahmen; EPOS liefert Struktur und Werte; die Excel-Formelmappe bleibt ein erzeugtes Blatt.


## 4 Platzhaltermodell

### 4.1 Begriffe und Code-Namen

| Anwenderbegriff | Code | Bemerkung |
|---|---|---|
| Platzhalter, z. B. `{{projekt.kunde}}` | Katalogeintrag `Vorlagenfeld` | getippt oder als Inhaltssteuerelement |
| Blockanfang und -ende, `{{#je stand}}` … `{{/je}}` | `Vorlagenblock` | Wiederhol- oder Bedingungsbereich |
| Platzhalterkatalog | `Vorlagenfeldkatalog` | Kern, Daten wie `Menuetabelle.cs` |
| Platzhaltermarke (App) | Baustein `Vorlagenfeldknopf`, Parameter `Vorlagenfeld` | Symbol am Rand eines Elements |
| Platzhalteranzeige (App) | Zustandsdienst `Vorlagenfeldansicht` | Stellungen Aus, Marken, Schlüssel (Vorlagenmodus) |
| Beschreibungen | Ressourcen `VF_*` | de und en |

„Platzhalter“ (`Textfeld.razor:68`, `DiagrammSvg.razor:244`, `ProjektWahlPlatzhalterWacheTests`) und „Marke“
(`DiagrammSvg.razor:160, :625, :904`, `--epos-marke`) sind im Code belegt; „Platzhaltermarke“ steht nur in Texten.

### 4.2 Syntax in Word

| Form | Schreibweise | Regel |
|---|---|---|
| Einzelplatzhalter (Text, Zahl, Datum) | `{{schlüssel}}`, `{{schlüssel\|angabe}}` | an jeder zulässigen Stelle (4.3); zerlegte Runs nur im Speicher zusammengefügt; Format des Runs mit `{{` bleibt |
| Absatzplatzhalter (Tabelle, Liste, Kapitel) | `{{schlüssel}}` allein im Absatz | der Absatz wird durch den Inhalt ersetzt |
| Bildplatzhalter | beliebiges Bild, Schlüssel im Alternativtext (`wp:docPr/@descr`) | als Text allein im Absatz: Bild in Satzspiegelbreite; im Satz: Fehler (4.10) |
| Inhaltssteuerelement | SDT mit `w:tag` = Schlüssel | gleichrangig; für Blöcke und Bilder robuster; Füllregeln 6.6 |
| Wiederholblock | `{{#je stand}}`, `{{#je variante}}`, `{{#je gebaeude}}` … `{{/je}}` | eigene Absätze oder erste/letzte Zelle einer Musterzeile; höchstens zwei Ebenen; nie über Tabellengrenzen |
| Bedingung | `{{#wenn schalter}}`, `{{#wenn nicht schalter}}` … `{{/wenn}}` | immer ein Katalogschalter; keine Ausdruckssprache |

**Erkennen:** Groß- und Kleinschreibung, Leerzeichen in den Klammern und Umlaute spielen keine Rolle; Umlaute
werden dokumentiert gefaltet (ä → ae, ö → oe, ü → ue, ß → ss), gleich für Schlüssel, Block- und Formatwörter.
Abweichungen bekommen einen Prüferhinweis mit der Normalform. Wie Word nach `{{` ersetzt, misst BV-E0.

### 4.3 Erlaubte Orte in Word

Der Einfügeanker ist ein Tripel aus Elternelement, Bezugselement und OpenXML-Teil: eingefügt mit
`anker.Parent.InsertBefore`, Bildteile am Teil des Ankers statt pauschal am Hauptteil (heute WBG:311-320).

| Art | Rumpf | Tabellenzelle | Block-SDT | Kopf-/Fußzeile | Fuß-/Endnote | Textfeld |
|---|---|---|---|---|---|---|
| Text, Zahl, Datum (auch SDT im Satz) | ja | ja | ja | ja | ja | ja |
| Bild | ja | ja | ja | ja | nein | ja |
| Tabelle, Liste | ja | ja | ja | nein | nein | nein |
| Kapitel | ja | nein | ja | nein | nein | nein |
| Blockanfang und -ende | ja | Musterzeile | ja | nein | nein | nein |
| Schalter | nur in `{{#wenn …}}` | | | | | |

Ein Verstoß ist der Prüferfehler „Platzhalter passt nicht an diese Stelle“ mit Vorschlag. Kommentare werden
weder ersetzt noch geprüft (6.7).

### 4.4 Syntax in Excel

| Form | Schreibweise | Regel |
|---|---|---|
| Zellplatzhalter | `{{schlüssel}}` allein in der Zelle | typisierter Wert; im Satz wird Text ersetzt |
| Name | `EPOS.<schlüssel>`, mappenweit | Präfix, weil `CO2` oder `P1` sonst Zellbezüge wären; ohne Punkte (Messprobe BV-E0) `EPOS_<schlüssel>` mit `__` für den Punkt (5.5) |
| Bereichsplatzhalter | `{{tabelle.…}}` allein in einer Zelle; listentauglich auch Excel-Tabelle `EPOS_<name>` | 7.3 |
| Blattmarke | `{{blatt.<name>}}` in A1 eines leeren Blattes | 7.2 |
| Diagramm | kein Platzhalter: ein Excel-Diagramm der Vorlage auf einer Excel-Tabelle `EPOS_<name>`, einem festen Raster oder einem Namen `EPOS.reihe.*` | EPOS füllt die Zahlen; Bilder gibt es in Excel nicht (BV-Q11); 7.4 |

Excel kennt **keine Blocksyntax**; Stände erscheinen nur auf dem geklonten Musterblatt `blatt.detail` und als
Listenzeilen (4.7).

### 4.5 Schlüsselschema

- ASCII, klein, punktgegliedert, deutsche Wörter ohne Umlaut, Unterstrich nur einzeln im Glied; Muster der
  Wache `^[a-z][a-z0-9]*(_[a-z0-9]+)*(\.[a-z0-9]+(_[a-z0-9]+)*)*$`. Die 44 Kennzahlschlüssel (KK:329-507)
  genügen ihm unverändert.
- Zeilenschlüssel der Wirtschaftlichkeit klein (`kapitalwert_diff`), die Großschreibung (WZ:32) bleibt Alias;
  ohne Anhang gilt „Erwartet“, `.guenstig`/`.unguenstig` wählen die anderen Szenarien.
- Keine Positionsadressierung von Ständen (BV-Q10); `stand.anzeige` liefert beim Stamm den Stammprojektnamen
  statt „Stamm — Stamm“ (BD:403-406).
- Zur Laufzeit gebildete Zeilen (WZ:1237, 1262, 844) sind **Musterschlüssel mit Parameter**
  (`stand.wirtschaft.erl_<block>_k_<komponente>`); die Werte zählt der Katalog aus `KOMPONENTE_*` und
  `BLOCK_A/B` (WZ:143-153) auf.

| Bereich | Inhalt | Kontext |
|---|---|---|
| `bericht.` | Titel, Datum, Variantenliste, -anzahl, Emissionsmodus, Warnungen; Sammelanker `bericht.inhalt`; `bericht.programmversion` als Alias von `ersteller.version` | Bericht |
| `text.` | sprachabhängige Festtexte der Standardvorlage (`text.seite` …) aus MyResource | Bericht |
| `ersteller.` | Firma der Installation, Programmname und Programmfassung (`ersteller.firma`, `ersteller.programm`, `ersteller.version`; BV-Q8) | Installation |
| `projekt.`, `stamm.` | Stammdaten bzw. Ergebnisse des Stammprojekts | Stamm |
| `stand.` | laufender Stand in `je stand`/`je variante`; `stand.a`/`stand.b` im Paarvergleich | Stand |
| `gebaeude.` | nur in `je gebaeude` | Gebäude |
| `vergleich.`, `wirtschaft.` | über alle gewählten Stände, auch `wirtschaft.beste.*` | Gruppe |
| `kennzahl.<k>.beschriftung`, `.einheit` | Beschriftung und Einheit einer Kennzahl | Bericht |
| `tabelle.`, `bild.` | Tabellen, Diagramme; je Stand unter `stand.tabelle.`, `stand.bild.` | wie Bereich |
| `kapitel.`, `baustein.`, `hat.` | Kapitel; Häkchen der Berichtsseite; Datenschalter | Bericht bzw. Stand |
| `muster.`, `blatt.` | Mustertabelle der Tabellenrollen (6.4); erzeugte Excel-Blätter | – |

### 4.6 Platzhalterklassen

| Kennung | Art | Wirkung beim Füllen |
|---|---|---|
| BV-P1 | Text | Absatz- und Zeichenformat bleiben; Zeilenumbrüche werden in Word zu `w:br` |
| BV-P2 | Zahl | Format und Einheit aus dem Katalog; in Excel eine echte Zahl |
| BV-P3 | Datum | nach Kultur; in Excel ein echtes Datum |
| BV-P4 | Tabelle | Strukturtabelle aus `Berichtstabelle` (5.4) |
| BV-P5 | Bild | Diagramm aus dem Zeichenmodell, SVG mit PNG-Rückfall; in Excel kein Bild, sondern ein Excel-Diagramm auf dem Tabellenbereich (BV-Q11, 7.4) |
| BV-P6 | Liste | Aufzählung, etwa Warnungen |
| BV-P7 | Kapitel | ein heutiger Baustein, vollständig erzeugt |
| BV-P8 | Schalter | ja/nein, nur als Bedingung in `{{#wenn}}` |
| BV-P9 | Blatt | erzeugtes Excel-Blatt an der Stelle der Blattmarke |

### 4.7 Kontexte

| Kontext | gültig in Word | gültig in Excel |
|---|---|---|
| Bericht, Installation, Stamm, Gruppe | überall | überall |
| Stand (`stand.*`) | in `je stand`/`je variante`; außerhalb nur `stand.a`/`stand.b` | auf dem Musterblatt `blatt.detail`, als Listenzeile |
| Gebäude (`gebaeude.*`) | in `je gebaeude` | als Listenzeile |

`stand.a`/`stand.b` gelten in der Paarsicht (Sicht 2); in Sicht 1 ist `stand.b` zulässig, wenn genau eine
Variante gewählt ist, sonst meldet die Vorprüfung „Vorlage nutzt den Paarvergleich, gewählt ist Sicht 1“. Die
Sicht bleibt eine Wahl der Ergebnisansicht (BSG:38-44, :214-226). `hat.*` gilt im Block `je stand` für den
laufenden Stand, außerhalb für die Gruppe. Ein Verstoß ist ein Prüferfehler mit Vorschlag.

### 4.8 Formatangaben

Eine Schreibweise für alle Angaben: deutsche Wörter nach `|`, auch in Blockanfängen.

| Angabe | Wirkung | gilt für |
|---|---|---|
| `\|stellen 1` | Dezimalstellen | Zahl |
| `\|ohne einheit`, `\|mit einheit` | Einheit weglassen bzw. anhängen (Vorgabe: mit Einheit) | Zahl |
| `\|datum`, `\|datum lang`, `\|datum mit zeit` | Datumsform nach Kultur | Datum |
| `\|leer statt strich` | leerer Text statt „—“ | Zahl, Text, Datum |
| `\|mit grund` | Leerwert mit Grund: „— (Lauf fehlgeschlagen)“ | Zahl |
| `\|block 3` | Gruppen zu höchstens drei Varianten, Stammspalte je Gruppe | Tabelle, `{{#je variante}}` |
| `\|ohne titel`, `\|ebene n` | Kapitel ohne eigene Überschrift bzw. mit Überschriften ab Ebene n (1 bis 9; `\|ebene 2` eine Ebene tiefer) | Kapitel |

Ohne Angabe gelten `Kennzahl.Format` bzw. `WirtZeile.Format` samt Einheit; eine unbekannte oder unpassende
Angabe ist ein Prüferfehler mit Vorschlag.

### 4.9 Sprache und Kultur

- Die Kultur folgt der Oberflächensprache (`BerichtTexte.Englisch` global, BT:18-20); Einzelwerte folgen
  ihr, die Kapitel behalten ihre Datumsmuster, bis eine Etappe sie anfasst. **Die Syntax ist in beiden
  Sprachen deutsch**, die Beschreibungen in der App zweisprachig (BV-Q3).
- **Die Standardvorlage ist sprachneutral:** Festtexte sind Platzhalter (`text.seite`, `bericht.untertitel`,
  `wirtschaft.methodik`), übersetzt über `BerichtTexte.T` bzw. MyResource; der Standardweg warnt in keiner
  Sprache. Der Kurzbericht kommt je Sprache. Trägt eine eigene Vorlage abweichend `EPOS.Sprache` in
  `custom.xml`, fragt die Vorprüfung **vor** der Simulation zurück („Sprache der Vorlage setzen“).

### 4.10 Fehlverhalten

| Fall | Prüfer | Engine | Laufmeldung |
|---|---|---|---|
| unbekannter Schlüssel | Fehler mit Fundort und Vorschlag (Editierabstand, Aliasse) | Platzhalter bleibt, gelb hervorgehoben | genannt |
| Art passt nicht zur Stelle, Kontextverstoß, unbekannte Formatangabe | Fehler mit Vorschlag | wie unbekannt bzw. Katalogformat | genannt |
| kein Wert | – | Leerwert des Eintrags: Zahl und Datum „—“, nie 0 (WBG:467-469); Text „—“ oder leer; Excel leere Zelle | je Schlüssel zusammengefasst: „eff.jaz: leer bei 4 von 11 Ständen“ |
| leere Tabelle, Bild ohne Modell | – | Leertext bzw. Hinweisabsatz mit Grund | Bild genannt |
| Kapitel ohne Daten | – | entfällt wie heute, mit Kapitelkopf (5.3) | – |
| Ausnahme bei einem Einzelwert | – | „—“; der Bericht bricht nicht ab | Warnung |

Entfernt wird nie etwas still. Alle Texte, die die Engine in den Bericht schreibt (Leertexte, Hinweise, „nicht
im Bericht“ in Anhang E), stehen zweisprachig in MyResource oder der `BerichtTexte.T`-Liste; die Katalogwache
prüft sie.

### 4.11 Gültigkeit und Warnungen

Die Gültigkeitshinweise hängen heute am Kapitel: Rückfall auf den gespeicherten Stand (BW:39-54), „Ergebnis
veraltet“ (BW:107-117), Fehlergründe (BW:217-225), Warnzeilen je Zelle (BW:908-917), Strich mit Grund statt 0
(`ErgebnisansichtTests.cs:585`). Damit Vorlagen aus Einzelwerten sie behalten, gibt es die Listen
`bericht.warnungen`, `wirtschaft.warnungen`, `stand.wirtschaft.warnungen`, Einzelgründe `….grund`, die Angabe
`|mit grund`, die Schalter `stand.veraltet`, `stand.frisch`, `stand.hat_fehler`, `stand.hat_zeitreihen`,
`stand.ist_stamm` und den Text `stand.fehler`. Nutzt eine Vorlage `stand.wirtschaft.*`, `stand.kennzahl.*` oder
`wirtschaft.*` ohne Warnliste, warnt der Prüfer „Gültigkeitshinweise fehlen“. Die Laufmeldung nennt die
Warnungen immer.


## 5 Der Platzhalterkatalog im Kern

### 5.1 Ort, Eintrag, Wertesatz

`EPOS.Kern/Allgemein/Bericht/Vorlagen/Vorlagenfeldkatalog.cs`, Daten wie `Menuetabelle.cs`. Felder:
`Schluessel`, `Art` (BV-P), `Kontext`, `Quelle` (Funktion auf dem Wertesatz `Berichtswerte`), `Format`,
`Einheit`, `Leerwert`, `BeschreibungId` (nur handgepflegt), `Ableitung`, `BeispielId`, `Aliasse`, `Ausgaben`
(Word, Excel), `Bedarf` (Zeitreihen, Verlauf, Emissionsbilanz), `Seit` (Katalogfassung), bei Kapiteln `Deckt`.
`KATALOGFASSUNG` steigt mit jeder Etappe, die Einträge hinzufügt. Der Kern führt **keine Orte der Oberfläche**;
er kann Razor-Seiten und `EPOS.UI/Seiten/Seitenschluessel.cs` nicht referenzieren (9.6).

`Berichtswerte` entsteht nach `SammleFuerBericht`; beim Auflösen wird die Datenbank nicht berührt. **Gebaut mit
BV-E3 (#528):** Zugriffe und Nebenrechnungen des Wirtschaftlichkeitsbausteins, von Anhang E, des Tabellenberichts, der
Formelmappe und der Kälteerzeugertafel stehen im Wertesatz `BerichtsDaten.Wirtschaft` (`WirtschaftsBerichtswerte`,
`EPOS.Kern/Allgemein/Bericht/WirtschaftsBerichtswerte.cs`). Der Sammler ermittelt ihn einmal nach der
Wirtschaftlichkeitsrechnung (`Ermittle(daten, bedarf)`) über dieselben Rechenwege — jeder Teil ist genau der Aufruf,
den die Schreiber vorher selbst taten (`EPOS.Kern/CLAUDE.md`, „Eine Auskunft ruft den Rechenweg des Laufs“):
Ergebnisse, Parameter, Tarif, Bewertung, Wirkungen, Nachweiszeile der Parameter je Kultur, Bilanzkonvention, Erzeuger,
Aktualität, Zeilen der Kennzahltafel, KWKG-Lage samt Konsistenz-Gate des Verlaufs, Kapitalwertverlauf, Strommatrix,
Referenzkessel, Emissionsbilanz, Trägerpreiszeilen, Trägerpreise der Formelmappe (`Traegerpreissatz.Lies`, aus der
Formelmappe ausgegliedert) und Trägernamen. Word und Excel lesen denselben Satz, der Kapitalwertverlauf läuft einmal je
Lauf. Ein Baum ohne Sammler (Proben, Prüfstände) bekommt über `Von(daten)` einen Satz, der jeden Teil beim ersten Lesen
rechnet — der Weg vor BV-E3; liest ein Schreiber einen Teil, den der Sammler nicht gerechnet hat, rechnet der Teil nach
und steht in `Nachgeholt`. Dass die Schreiber nur den Satz lesen, hält die Wache
`BerichtSchreiberOhneDatenbankWacheTests` (12).

**Bedarf** (`Berichtsbedarf` mit den Flags `Zeitreihen`, `Verlauf`, `Emissionsbilanz` des Katalogs): Simulation und
Wirtschaftlichkeitsrechnung laufen immer, die drei nur, wenn der Bericht sie zeigt. Die Kapitel tragen den Bedarf ihres
Bausteins (`Berichtskapitel.Bedarf`: Ergebnisse Zeitreihen, Wirtschaftlichkeit Verlauf und Emissionsbilanz, die übrigen
keinen), `bericht.inhalt` alle drei, `stamm.kennzahl.kaelte.stunden` die Zeitreihen. `Vorgabe(konfig)` vereinigt über
die angehakten Kapitel — das Verhalten ohne Vorlage und der Bedarf der Mappe; `AusVorlage(befund, konfig)` vereinigt die
genutzten Schlüssel einer geprüften Vorlage, `kapitel.*` nur mit gesetztem Häkchen; Sammelanker, Vorlage ohne
Platzhalter, unlesbare Vorlage und Rückfall gelten wie die Vorgabe. **Zusatzregel:** Zeigt die Vorlage ein Kapitel am
Häkchen „Wirtschaftlichkeit“ (Wirtschaftlichkeit, Anhang E), erhebt der Lauf die Stundenreihen wie ohne Vorlage nach dem
Häkchen „Ergebnisse je Variante“ — die Zahlen der Wirtschaftlichkeit hängen an den Reihen; die Vorlage bestimmt, was der
Bericht zeigt, nicht, wie eine gezeigte Zahl entsteht. `BerichtCtrl.PruefeVorStart` legt den Bedarf der Vorlage in
`Startbefund.Bedarf`, `Berichtsbedarf.FuerLauf(konfig, start, weg, mitExcel)` bildet daraus den Bedarf des Laufs (mit
Mappe vereinigt mit der Vorgabe; Weg „Mit Standardvorlage“ und Rückfall: die Vorgabe), die Hülle reicht ihn an den
Sammler. Die Stundenreihen, die die Rechnung braucht (gepflegter Strom-Leistungspreis), ergänzt der Sammler unabhängig
vom Bedarf.

**Beste Variante:** Die Auswahl liegt im Kern (`BesteVariante.Waehle`,
`EPOS.Kern/Allgemein/Wirtschaftlichkeit/BesteVariante.cs`, Regel in 9.5); die Kacheln der Wirtschaftlichkeitsseite
rufen sie, `wirtschaft.beste.*` ruft sie ab BV-E4 — Kachel und Platzhalter rechnen gleich. Bis BV-E4 führt der Katalog
nur rein vorliegende Werte; BV-E3 fügt keine Einträge hinzu, die Katalogfassung bleibt 2. Neue `wirtschaft.*`-Schlüssel
tragen ihren Bedarf (BV-E4).

### 5.2 Erzeugte Einträge

- **Kennzahlen:** aus 44 Einträgen (KK:329-507) `stamm.kennzahl.<k>`, `stand.kennzahl.<k>`, `stand.delta.<k>`,
  `stand.delta_prozent.<k>` (nur `DeltaAnzeigen`), `kennzahl.<k>.beschriftung`, `.einheit`. Die Beschriftungen
  von `em.co2`, `em.co2_spez`, `kaelte.co2` nennen den Emissionsmodus des Laufs (KK:489-500, KK:324); die
  Beschriftung hat deshalb Kontext Bericht (aufgelöst mit `EmissionsAusweis.ModusAusVarianten`), dazu
  `bericht.emissionsmodus` und `hat.emissionsmodus_gwp`.
- **Wirtschaftlichkeit:** aus `WirtschaftlichkeitZeilen` (WZ:32) je Szenario `stamm.wirtschaft.<zeile>`,
  `stand.wirtschaft.<zeile>`, `wirtschaft.beste.<zeile>`; Parameter `wirtschaft.parameter.<name>` (`zins`,
  `zeitraum`, `p_e`, `p_b`, `p_i`, `risiko_*`), Szenarien `wirtschaft.szenario.<s>.name`, `.annahmen`,
  `.traegerpreise`, Tabelle `tabelle.wirtschaft.parameter`.
- Eine neue Kennzahl ist ohne Pflege ein Platzhalter; ihre Beschreibung kommt aus einem Muster (5.5).
- **Ersteller** (handgepflegt, nicht erzeugt; BV-Q8): `ersteller.firma` (Einstellung `BerichtFirma`, 10.3),
  `ersteller.programm` („EPOS-Plan“) und `ersteller.version` (Produktfassung wie heute auf dem Deckblatt,
  `ProduktFassung` BS:89-112); `bericht.programmversion` bleibt Alias von `ersteller.version`.

### 5.3 Kapitel und Einfügeanker

| Regel | Inhalt |
|---|---|
| Kapitel | jeder `IBerichtsBaustein` (`IBerichtsBaustein.cs:16`) wird `kapitel.<name>`; Anhang E wird `kapitel.anhang_e` mit dem Schalter „Wirtschaftlichkeit“ (AE:413); die Bausteine schreiben unverändert an den Einfügeanker (4.3), nach Behebung von BW:678. Die Zuordnung von Kapitel, Häkchen, Schalter `baustein.<name>` und Kapitelkopf `text.kapitel_<name>` steht an einer Stelle (`Berichtskapitel`, BV-E2) |
| Ort | allein im Absatz des Rumpfs oder als Inhaltssteuerelement auf Blockebene (4.3); je Kapitel gilt die erste Stelle (getippte Platzhalter vor Steuerelementen, je in Dokumentfolge), jede weitere bleibt gelb stehen, der Prüfer meldet „Kapitel … steht mehrfach“ (BV-E2) |
| Inhaltsbreite | `WordKontext.Inhaltsbreite` aus der `sectPr` des Ankerabschnitts (Seite minus Ränder und Spalten) ersetzt `INHALT_B`; Kapitelbilder höchstens min(620 px, Satzspiegel) |
| Sammelanker | `bericht.inhalt` setzt alle angehakten Kapitel in heutiger Folge ein (WBG:96-111), **ohne** die einzeln geführten |
| Entfall | ein Kapitel mit abgewähltem Häkchen oder ohne Daten liefert nichts; ein unmittelbar davor stehender Absatz im Format „EPOS Kapitelkopf“ entfällt mit — keine verwaiste Überschrift, auch vor BV-E4 |
| Kapitelformat | `\|ohne titel` unterdrückt die eigene Überschrift (die erste Überschrift 1 oder 2 des Bausteins); was der Baustein davor schreibt — der Seitenumbruch vor Anhang E —, kommt vor den Kapitelkopf. `\|ebene n` verschiebt die Überschriften um n − 1 Ebenen, höchstens bis 9; fehlende Überschriften 4 bis 9 legt die Engine über `w:name` an (BV-E2) |
| Deckt, Gedeckt | `bericht.inhalt` deckt alle `kapitel.*`, jedes Kapitel seinen Kapitelkopf, seinen Schalter, die Einzelschlüssel seines Bausteins (5.1) und die Tabellen und Bilder, die sein Baustein schreibt, samt `hat.tabelle.*`/`hat.bild.*` (Entscheid BV-E5-3; `hat.*` zählen in `Gedeckt` wie `baustein.*` nicht als Inhalt). `Vorlagenfeldkatalog.Gedeckt` rechnet daraus einen Fixpunkt: Ein Kapitel, das die Vorlage nicht führt, gilt als gedeckt, wenn sie jeden seiner Einzelschlüssel führt (ohne Schalter und Kapitelkopf) — so deckt ein **Deckblatt aus Platzhaltern** `kapitel.deckblatt`, und `bericht.inhalt` gilt, sobald jedes Kapitel gilt. Eine Vorlage mit Deckblattangaben im Rumpf trägt ihr Deckblatt selbst: Das Häkchen „Deckblatt“ steht ausgegraut (10.2), die Checkliste nennt die Stelle „Deckblatt“; die Regel greift schon bei einer einzelnen Deckblattangabe (offen, 15.2) |
| Stellen | die Überschrift vor jedem Anker — der Kapitelkopf unmittelbar davor, mit `\|ohne titel` sonst die nächste Überschrift davor, sonst die eigene Überschrift des Bausteins; über den Sammelanker die eigenen Überschriften — ist die Stelle der Anhang-E-Checkliste (11 Nr. 3); `BerichtCtrl.KapitelstellenDerVorlage` liefert sie ohne Füllen für die Überlagerung der Wirtschaftlichkeitsseite (10.2) |
| ein Codeweg | Kapitel bleiben Sammelplatzhalter; herausgelöste Tabellen nutzen denselben Tabellenbauer |

### 5.4 Tabellen und Bilder

`Berichtstabelle` (`EPOS.Kern/Allgemein/Bericht/Tabellen/`: Kopf, Spalten der Arten Fest, Stamm, Stand und Δ, Zeilen,
Zellen mit Wert, Format und Rolle Stamm, Gruppe, Summe, Warnung; `Bloecke(n)`, Breiten in DXA und in Prozent) entsteht
in den Bauwegen `Berichtstabellen` und wird vom Word-Renderer gelesen — Bausteine und Platzhalter lesen dieselbe Tabelle,
der Bausteinweg schreibt byte-gleich; der Excel-Renderer folgt mit BV-E8. Blockteilung (Vorgabe drei Varianten je Block)
und Δ-Spalte nur bei genau einer Variante (BV:257) bleiben; `|block n` stellt die Blockgröße ein. Das Merkmal
`Listentauglich` (feste Spaltenzahl, eindeutige Textköpfe, keine verbundenen Zellen) entscheidet über Excel-Tabellen
(7.3). `tabelle.varianten` führt sechs Spalten mit Stromspeicher (`SpeicherKontextText`, im Sammler erhoben;
BSG:126-129); das erzeugte Übersichtsblatt bleibt bei fünf. Die Katalogfassung 4 führt 37 Tabellen — 26 `tabelle.*` im
Kontext Gruppe oder Bericht, 11 `stand.tabelle.*` im Standblock —, `muster.tabelle` und je Tabelle einen Schalter
`hat.tabelle.<name>` (im Standblock für den Stand, außerhalb: irgendein Stand hat die Tabelle);
`tabelle.wirtschaft.parameter`, `tabelle.wirtschaft.verlauf` und `stand.tabelle.monatswerte` haben nur eine Excel-Quelle
und kommen mit BV-E7/E8. Bilder kommen aus der Modellfabrik `Berichtsbilder` im Kern, die Bausteine und Bildplatzhalter
gemeinsam rufen; Fassung 4 führt die 16 Bildschlüssel der dreizehn Berichtsbilder (7 `stand.bild.*`,
`stamm.bild.speichertemperaturen`, 4 `bild.vergleich.balken.<k>`, 4 `bild.wirtschaft.*`) und je Bild einen Schalter
`hat.bild.<name>`; Paarbilder (`stand.a/b.bild.*`) gibt es nicht (Entscheid BV-E5-2). In Excel wird aus einem Bild kein
Bild, sondern ein Excel-Diagramm auf dem Tabellenbereich mit denselben Zahlen (BV-Q11, 7.4). Vorgemerkt
(`Vorlagenfeldkatalog.VorgemerkteBilder`, `Seit` über der Fassung) sind alle App-Diagramme ohne Berichtsziel,
einschließlich Erzeugerstapel, Streuwolke, Temperaturverlauf, Jahresverlauf, GanglinieNormiert, MonatsStapel,
Stundenprofil und Peak-Shaving-Lastgang (Anhang A); der Prüfer nennt sie „erst in einer späteren Programmfassung“.

### 5.5 Zweisprachigkeit und Namen

- `designer_neu.py` nimmt nur C#-Bezeichner an (`Werkzeuge/ResourceDesigner/designer_neu.py:37`). Abbildung:
  `VF_` + Schlüssel in Großbuchstaben, Punkt → `__` (`projekt.kunde` → `VF_PROJEKT__KUNDE`); eindeutig, weil
  das Schlüsselmuster nur einzelne Unterstriche erlaubt. Dieselbe Abbildung liefert `EPOS_…` in Excel; eine
  Wache prüft Kollisionsfreiheit, auch gegen Excel-Tabellennamen (gemeinsamer Namensraum).
- Nur **handgepflegte** Einträge haben eine eigene Beschreibung; erzeugte nehmen ein Muster je Ableitung
  (`VF_MUSTER_STAND_KENNZAHL` = „Wert „{0}“ des laufenden Standes“, `{0}` = `Kennzahl.Label`). Die Wache „jede
  Beschreibung in beiden `.resx`“ gilt für handgepflegte Einträge und Muster.
- `BerichtTexte.T` bleibt für den Kapitelinhalt; die Bausteintitel (`BerichtsKonfiguration.cs:36-46`) wandern
  nach MyResource.

### 5.6 Katalogfassung und Stabilität

Die Schlüsselliste jeder `KATALOGFASSUNG` wird als Testdatei eingefroren; Wache: jeder einmal ausgelieferte
Schlüssel ist lebendig oder Alias. Die Vorlage trägt ihre Fassung in `custom.xml`; der Prüfer meldet eine
ältere Fassung **nur**, wenn ein genutzter Schlüssel seither Alias ist oder seine Bedeutung änderte, neue
Kapitel als Hinweis („Neues Kapitel X – in Ihrer Vorlage nicht enthalten“; genutzt heißt dabei auch gedeckt, 5.3).
Marke und Katalog zeigen nur Schlüssel mit `Seit` ≤ laufender Fassung. Fassung 1 kam mit BV-E1 (159 Schlüssel), Fassung 2
mit BV-E2 (184 Schlüssel: dazu neun Kapitel, acht Schalter, sieben Kapitelköpfe und das Logo); eingefroren in
`EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v1.txt` und `…_v2.txt`.


## 6 Word-Vorlagen

### 6.1 Rahmen

| Regel | Inhalt |
|---|---|
| Formate | `.docx`, `.dotx` (über `ChangeDocumentType`); `.docm`, `.dotm`, `.doc` abgelehnt mit „als .docx speichern“ |
| Inhalt bleibt | „Rumpf leeren“ (WBG:78-80) entfällt; mehrere Abschnitte erlaubt (Deckblatt ohne Kopfzeile, Querformat); ohne jeden Platzhalter Warnung und `{{bericht.inhalt}}` am Ende, nichts wird gelöscht |
| Nachverfolgte Änderungen | Prüferfehler „in Word alle Änderungen annehmen oder ablehnen“; kein eigener Annahme-Algorithmus (die einzige fertige Umsetzung ist archiviert, ihre Fortführungen verlangen SkiaSharp ≥ 4.152) |
| Externe Beziehungen | verknüpfte Bilder und Mappen entfernt und gemeldet; `attachedTemplate` still entfernt, nur als Hinweis |
| Sicherheit | Makros abgelehnt; Grenzen gegen Zip-Bomben (8.5) |

### 6.2 Formatvorlagen: Rollen statt IDs

Eine im deutschen Word angelegte Vorlage führt übersetzte Stil-IDs (`berschrift1`, `Titel`, `Untertitel`,
`Standard`), und `Beschriftung` ist dort die eingebaute Beschriftung. Mit festen IDs erschienen die Kapitel als
Standardtext, das TOC bliebe leer, die Anhang-E-Stelle fände keine Überschrift, „fehlende Stile anlegen“
erzeugte Doppelungen. Deshalb kennt `WordKontext` **Rollen** (Titel, Untertitel, Überschrift 1–3, Standard,
Beschriftung, Hinweis, Abstand, Kapitelkopf, Tabelle) und löst sie je Dokument auf: eingebaute Vorlagen über
`w:name` ohne Rücksicht auf Groß- und Kleinschreibung (`title`, `subtitle`, `heading 1`–`3`, `caption`), den
Standardabsatz über `w:default="1"`. Nur echt fehlende Stile legt die Engine an, mit eindeutigem Namen und ID
(`EPOS Hinweis`, `EPOS Abstand`, `EPOS Kapitelkopf`, `EPOS Tabelle`), mit Hinweis; `Beschriftung(" ")` als
Abstand wird die Rolle Abstand. Der Prüfer warnt, wenn Überschriftenstile fehlen oder keine Gliederungsebene
tragen. Die Standardvorlage wird um die Doppelungen bereinigt; BV-E0 misst eine im deutschen Word 365 angelegte
Vorlage. Die Rollenauflösung gehört zu BV-E1, weil schon dort Kapitel über `{{bericht.inhalt}}` in fremde
Vorlagen gelangen.

### 6.3 Mitgelieferte Vorlagen

Die mitgelieferten Vorlagen sind **Dateien der Auslieferung wie heute** (BV-Q19 b, 8.4): unter Windows in
`{app}\Vorlagen` (Lieferweg über `WindowsFormsApplication1.csproj`), auf iOS im App-Bundle (MauiAsset); der Kern
findet sie über `Dienste.Pfade.Berichtsvorlagen`. Sie sind schreibgeschützt, werden mit jedem Update erneuert und
nur über eine Kopie geändert. Die Standard-Excel-Mappe entsteht im Code (7.1), der Baukasten aus dem Katalog.

1. **Standardvorlage `Berichtsvorlage_Standard.docx`:** der volle Aufbau der **Beispielvorlage aus dem bisherigen
   Bericht** (Anhang B.3), erzeugt vom Werkzeug `Werkzeuge/Berichtsvorlage` aus der bereinigten Stilvorlage mit
   `beispiel --standard` (BV-E2, umgesetzt #520): Stile bereinigt (6.2), sprachneutral (4.9), Deckblatt aus Platzhaltern
   auf eigener Seite, Inhaltsverzeichnis `{{kapitel.inhalt}}`, je Kapitel ein Kapitelkopf `{{text.kapitel_<name>}}` im
   Format „EPOS Kapitelkopf“ und darunter `{{kapitel.<name>|ohne titel}}`; in der Kopfzeile `{{ersteller.programm}}` und das
   Logo als Bildplatzhalter (`{{bild.ersteller.logo}}`, 6.5, Entscheid BV-E2-1), in der Fußzeile `{{ersteller.firma}}`
   statt „INEKON GmbH“, `{{bericht.datum}}` statt DATE und `{{text.seite}}` mit PAGE/NUMPAGES (BV-Q8); keine Kommentare;
   in `custom.xml` `EPOS.Katalogfassung` 4 und `EPOS.Vorlage` `standard` (Fassung 4 mit BV-E5, inhaltsgleich). In BV-E1 trug sie die Stufe mit dem Sammelanker
   (`beispiel --sammelanker`, 11 Nr. 2): im Rumpf nur `{{bericht.inhalt}}`; das Werkzeug erzeugt diese Stufe weiter auf
   Zuruf. Die **Beispielvorlage** `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Beispiel.docx`
   (BV-E0) hat denselben Aufbau und erläutert ihn in drei Word-Kommentaren; sie unterscheidet sich von der Standardvorlage
   nur noch durch die Kommentare und steht in keinem Lieferweg (Anschauung; sie bleibt vorerst neben dem Kurzbericht).
   **Daneben bleibt die Stilvorlage `Berichtsvorlage.docx` ausgeliefert** — Rückfall des Codes, wenn die Standardvorlage
   fehlt, und Quelle der Bereinigung, mit dem Logo in der Kopfzeile; sie wird nicht abgelöst, und eine Übernahme der
   Altdatei gibt es nicht (Entscheid BV-E1-1, 10.3).
2. **Kurzbericht** `Berichtsvorlage_Kurzbericht.docx` und `Berichtsvorlage_Kurzbericht_en.docx` (je Sprache, BV-E5):
   Lehrvorlage mit neun Erläuterungen als Kommentaren, neutrale Namen und Werte („Variante 1“, „Speicher 1, 100 kWh“);
   erzeugt vom Werkzeug `Werkzeuge/Berichtsvorlage` mit `kurzbericht --sprache de|en` aus der bereinigten Stilvorlage,
   `custom.xml` mit `EPOS.Katalogfassung` 4, `EPOS.Vorlage` `kurzbericht` und `EPOS.Sprache`; in beiden Lieferwegen
   (`WindowsFormsApplication1.csproj`, MauiAsset in `EPOS.iOS/EPOS.iOS.csproj`). **Nicht** direkt wählbar, nur als Kopie
   über „Neue Vorlage…“ mit „Kopie von: Kurzbericht“ in der Sprache der Oberfläche (10.2, Anhang B.1).
3. **Baukasten** (`WordBaukasten`, BV-E5): aus dem Katalog erzeugt, keine Datei der Auslieferung — jeder Eintrag mit
   Ausgabe Word und `Seit` ≤ Fassung genau einmal (Fassung 4: 1.304 Einträge), **nach Kontext gegliedert** — Bericht,
   Installation, Stamm, Gruppe; Stand- und Gebäudeeinträge in fertigen Blöcken `je stand` bzw. `je gebaeude`; Schalter in
   `#wenn`-Mustern; Tabellen und Kapitel in eigenen Absätzen; die Mustertabelle. Je Eintrag Beschreibung und
   Platzhalter, für Bilder ein neutrales Musterbild; Platzhalter mit `w:noProof` gegen die Rechtschreibprüfung. Die
   **Paarsicht steht nur als Muster** — drei Beispiele als Text ohne Klammern und die Regel (Entscheid BV-E5-4) —, so ist
   der Baukasten in jeder Sicht fehlerfrei füllbar. „Baukasten speichern…“ im Platzhalterkatalog schreibt ihn in der
   Sprache der Oberfläche; auf iOS öffnet die App danach das Teilen-Blatt (`Dienste.Datei.MitSystemOeffnen`, Entscheid
   BV-E5-5). Der Excel-Baukasten kommt mit BV-E7.

### 6.4 Tabellen mit variabler Variantenzahl

1. **Varianten als Zeilen:** Musterzeile mit `{{#je stand}}` in der ersten und `{{/je}}` in der letzten Zelle,
   je Stand geklont, robust bei jeder Zahl; `gridSpan`/`vMerge` darin sind ein Prüferfehler.
2. **Strukturtabelle `{{tabelle.…}}`:** EPOS baut Struktur, Blöcke, Δ-Spalte, Breite in Prozent. Die
   Tabellenformatvorlage „EPOS Tabelle“ (über `w:name`) steuert Rahmen, Kopfzeile, Schrift und Bänder — mehr
   kennen ihre bedingten Formate nicht. Die **Rollen** Stamm (je Block wiederholte Spalte), Gruppe und Summe
   (Zeilen mitten in der Tabelle) und Warnung (Einzelzellen, BW:908-917) liest EPOS aus einer **Mustertabelle**
   mit Alternativtext `{{muster.tabelle}}`, je Rolle eine Zelle mit Schattierung und Zeichenformat; sie wird
   entfernt. Fehlt beides, gilt die heutige Direktformatierung (WBG:30-41, 364-429).
3. **Blöcke je Variante:** `{{#je variante|block 3}}` um eine ganze Tabelle samt Überschrift.

Gebaut (BV-E5, `WordVorlagentabellen.cs`): `{{tabelle.…}}` allein im Absatz — im Rumpf, in einer Tabellenzelle als
geschachtelte Tabelle und im Block-Steuerelement — wird die Tabelle; Warnsätze der Tabelle stehen als Absatz im Format
„Hinweis“ darunter, eine Tabelle ohne Zeilen wird der Leertext „— (Grund)“. „EPOS Tabelle“ wird aus der Vorlage über
`w:name` genommen; fehlt sie, legt die Engine sie nur an, wenn die Vorlage eine Mustertabelle trägt (mit Hinweis in der
Laufmeldung), sonst gilt die Direktformatierung. Der Prüfer warnt bei einer Mustertabelle ohne Rolle
(`VF_PRUEF_MUSTER_OHNE_ROLLEN`) und meldet `muster.tabelle` als getippten Text als Fehler. Der Alternativtext der
Mustertabelle (`w:tblDescription`) ist erst ab Office 2010 gültig; der Validator mit dem Maß Office 2007 lässt genau
diesen Befund durch.

Spaltenwiederholung aus einer Musterspalte ist nicht geplant (höchstes Bruchrisiko, die Strukturtabelle deckt
den Bedarf).

### 6.5 Bilder

Breite aus `wp:extent`, Höhe aus dem Seitenverhältnis des Modells bis zur Rahmenhöhe, eingepasst; Lage,
Umbruch, Rahmen, Drehung bleiben, `a:srcRect` wird entfernt, `docPr/@id` werden neu vergeben. **Stufe 1**
skaliert das fertige Modell, Schrift schrumpft mit; der Prüfer warnt unter 80 % der Modellbreite
(`VF_PRUEF_BILDRAHMEN`, volle Prüfung). **Stufe 2** (BV-E5) zeichnet in Zielgröße: `Bildmass` ist ein optionaler
Parameter an den zwölf Modellfunktionen der Berichtsbilder — ohne Maß entsteht das Bild byte-gleich —; im Zielmaß bricht
die Legende um, die Zeichenfläche räumt ihr Platz, der Titel passt sich ein. **Mindestbreiten** in Modellpunkten:
allgemein 560, Brücke 1000, Spanne 900, Szenarien 760; ist der Rahmen schmaler, zeichnet das Modell in der Mindestbreite
und wird verkleinert (Stufe 1), mit einem Hinweis (Entscheid BV-E5-1). Die ChartProben halten beide Stufen mit 22
Maß- und 22 Schriftproben. Ein Bildschlüssel als Text allein im Absatz wird ein Bild in Satzspiegelbreite, im Satz ist er
ein Fehler; ohne Modell steht ein Hinweisabsatz mit Grund; `stand.bild.*` gilt nur im Standblock. SVG mit PNG-Rückfall bleibt Pflicht (DG-E3-8); `BildTeile` (WBG:306-360) wird in „Blip bauen“
und „einfügen oder ersetzen“ geteilt. Nach dem Füllen wird der Diagrammtitel Alternativtext (BV-Q14). Die
Gestaltung folgt weiter Farbrollen und fester Schrift (BV-Q17).

**Logo des Erstellers — in BV-E2 vorgezogen (Anwenderentscheid BV-E2-1 vom 26.09.2026, Lesart b; verworfen: Entfall des
Logos, der Lizenznehmer setzt es in seiner Kopie ein).** Das Logo der Kopfzeile bleibt in Beispiel- und Standardvorlage als
Platzhalterbild stehen: Alternativtext `{{bild.ersteller.logo}}`, neutrales graues Bild „Logo“ (150 × 82 px) an Ort, Größe
und Umbruch des bisherigen Logos. Beim Füllen setzt die Engine in jedem Teil (Rumpf, Kopf- und Fußzeilen) die Bilddatei der
Einstellung `BerichtLogo` ein (10.3; PNG oder JPEG bis 5 MB), mit ihrem Seitenverhältnis in Breite und Höhe des Rahmens
(`wp:extent`) eingepasst; der Alternativtext wird der Dateiname. Ohne Logo entfällt das Bild samt Lauf, ein danach leerer
Absatz auch, außer er steht allein in seinem Teil; eine fehlende, zu große oder unlesbare Datei nennt die Laufmeldung einmal
als Warnung. Der Prüfer meldet das Logo als getippten Text oder im Steuerelement als Fehler (mit dem Rat, ein Bild mit dem Schlüssel
im Alternativtext einzufügen), einen unbekannten Bildschlüssel als Fehler; das Platzhalterbild ohne eingestelltes Logo
ist kein Befund. Die Diagramme füllt die Engine wie oben beschrieben. Die
Stilvorlage `Berichtsvorlage.docx` (Rückfall) behält ihr Logo.

### 6.6 Inhaltssteuerelemente nach dem Füllen

Jedes getaggte SDT wird nach dem Füllen **ausgepackt**, der Inhalt bleibt (BV-Q14). So bleiben weder
`w:showingPlcHdr`, `w:temporary` noch `w:dataBinding` stehen, die Word als grauen Platzhaltertext zeigen oder
beim Öffnen überschreiben würde; geklonte Wiederholabschnitte brauchen keine eindeutigen `w:id`, `w15`-Elemente
bleiben nicht zurück. Ein SDT im Satz (SdtRun) ist nur für Text, Zahl, Datum zulässig. Der Rundlauf prüft gegen
Office 2007–2021 (`WordBerichtSvgWacheTests.cs:323-340`).

### 6.7 Felder, Kopf- und Fußzeile, Kommentare

- Das Inhaltsverzeichnis ist Vorlageninhalt; `kapitel.inhalt` erzeugt es wie heute. `updateFields` nur bei
  Feldern, die Word nicht selbst aktualisiert: TOC, PAGEREF, REF, SEQ, DOCPROPERTY (nicht PAGE, NUMPAGES).
  DATE und TIME bekommen den Hinweis „zeigt das Datum des Öffnens – {{bericht.datum}} verwenden?“.
- Ersetzt wird in allen Kopf- und Fußzeilen aller Abschnitte, Fuß- und Endnoten, Textfeldern und beiden
  Zweigen von `mc:AlternateContent`; Seitenfelder bleiben unberührt.
- **Kommentare** werden weder ersetzt noch geprüft; die Engine entfernt alle samt `commentRangeStart/End` und
  `commentReference` und nennt ihre Zahl in der Laufmeldung.

### 6.8 Vorlage prüfen

Der `Vorlagenpruefer` hat zwei Stufen derselben Regeln: **Schnellprüfung** (Platzhalter, Kontexte, Orte,
Formatangaben; Millisekunden) und **volle Prüfung** (zusätzlich Stile, Bildrahmen, Felder, Paketprobe). Er
läuft zu festen Zeitpunkten ohne Dateiüberwachung und ohne Hintergrundfaden (`EPOS.Kern/CLAUDE.md:74-76`,
`EPOS.UI/CLAUDE.md:282-297`): voll beim Hinzufügen und auf Knopfdruck, schnell beim Öffnen der Seite, beim
Vorlagenwechsel und vor jedem Start; eine geänderte Prüfsumme ersetzt die Meldungsliste. Die Vorlage wird mit
`FileShare.ReadWrite` **einmal** in den Speicher gelesen; genau diese Bytes prüft und füllt EPOS, auch wenn der
Anwender während der Simulation speichert. Liegt eine Sperrdatei `~$…` daneben, meldet die Prüfzeile „in Word
geöffnet – ungespeicherte Änderungen fehlen“. Jede Meldung hat einen menschlichen Fundort („Tabelle 3,
Zeile 2, Zelle beginnt mit ‚Wärme‘“), ein „Was tun“ und bei fachfremden Befunden eine `KiMeldungskennung`
(`EPOS.UI/CLAUDE.md:94-95`); Texte aus MyResource.

| Stufe | Meldung (Beispiel) | Was tun |
|---|---|---|
| Fehler | „Unbekannter Platzhalter {{projekt.kundename}}“ | Vorschlag `{{projekt.kunde}}` übernehmen |
| Fehler | „Wert je Variante steht außerhalb von {{#je stand}}“, „passt nicht an diese Stelle“ | Block ergänzen bzw. verschieben |
| Fehler | „Block nicht geschlossen“, „verbundene Zellen in der Wiederholzeile“ | `{{/je}}` ergänzen, Zellverbund aufheben |
| Fehler | „nachverfolgte Änderungen“, „Makros“, „kann nicht gelesen werden: …“, „zu groß“ | in Word annehmen; als `.docx` speichern; verkleinern |
| Warnung | „Überschriftenstile fehlen“, „Bildrahmen unter 80 %“, „veralteter Platzhalter“, „Gültigkeitshinweise fehlen“, „ohne Platzhalter“ | Formatvorlage nutzen, Rahmen verbreitern, Normalform übernehmen, Warnliste ergänzen |
| Hinweis | „Formatvorlage EPOS Hinweis ergänzt“, „Vorlagenverweis entfernt“, „Normalform: {{projekt.geaendert}}“ | – |

Katalogfassung, Sprache und Vorlagenart schreibt der Prüfer nur mit Zustimmung nach `custom.xml`; ist die Datei
in Word geöffnet, meldet er „Vorlage ist in Word geöffnet“.

Eine Probefüllung ohne Simulation gibt es nicht (BV-Q12 b): Geprüft wird die Vorlage, gefüllt wird nur der
Bericht; die Prüfliste bietet „Meldungen kopieren“ und „Schließen“ (9.7).


## 7 Excel-Vorlagen

### 7.1 Mappe, Schrift, Formate

| Regel | Inhalt |
|---|---|
| Mappe | `new XLWorkbook(stream)` statt `new XLWorkbook()` (EBG:167); `.xltx` über das SDK umgestellt, ob ClosedXML das trägt, misst BV-E0; `.xlsm` abgelehnt; Anwenderblätter nur an Platzhaltern angefasst; `AdjustToContents` (EBG:284, 365, 407, 1775) nie auf Vorlagenblättern |
| Schrift | erzeugte Blätter setzen Calibri 11 ausdrücklich, sonst erbten sie eine Vorlagenschrift außerhalb der Rückfallkette (EBG:69-70) und würden auf Linux und iOS falsch vermessen; die Standard-`.xlsx` entsteht im Code |
| Werte | EPOS schreibt `Value`; `Style` nur beim Zahlenformat einer Zelle mit Format „Standard“ — dann gilt das Kernformat, sonst das der Vorlage; Datumswerte sind echte Datumswerte (heute Text, EBG:235) |
| Prozent | Kennzahlen mit „%“ (KK:365, 429, 447) sind 0–100; bei Prozentformat der Zielzelle schreibt EPOS den Anteil und gibt einen Hinweis |
| Delta | `stand.delta*` ist in Vorlagen ein Wert; die Δ%-Formel bleibt dem erzeugten Vergleichsblatt (Bezugszellen, `ExcelFormelmappe.cs:1050`) |

### 7.2 Blattmarken und erzeugte Blätter

Blattmarken `blatt.uebersicht`, `.vergleich`, `.wirtschaftlichkeit`, `.verlauf`, `.detail`, `.checkliste`: EPOS
ersetzt das markierte Blatt und gibt ihm den heutigen Namen; ohne Marke hängt es an (BV-Q2); nie an
sprachabhängigen Blattnamen. **Marke ohne Inhalt** (EBG:170-196): Das Blatt wird entfernt; der Prüfer warnt vor
Bezügen des Anwenders darauf (`#BEZUG!`). **`blatt.detail` ist ein Musterblatt** mit `stand.*`-Zellplatzhaltern,
je Stand geklont (`CopyTo`), benannt wie heute (EBG:1704, 1912-1922) und an der Markenstelle eingefügt. Ein
gleichnamiges Anwenderblatt ist ein Prüferfehler (`Worksheets.Add` würfe). Die Standardvorlage trägt nur
Blattmarken in heutiger Folge; `BerichtBlattstrukturWacheTests.cs:191` bleibt grün; ein Test deckt
`B_WIRTSCHAFT` aus, leeren Verlauf und null Varianten ab.

### 7.3 Listen und Bereiche

Excel-Tabellen nur für listentaugliche Tabellen (5.4), Zelle für Zelle über `InsertRowsBelow`;
`ReplaceData(IEnumerable<T>)` liest per Reflection und bleibt ungenutzt; ob berechnete Spalten mitwachsen,
misst BV-E0 (`propagateExtraColumns`). Tabellen mit Stand-Spalten, Blöcken, Gruppenzeilen oder verbundenen
Köpfen (Verlauf, EBG:818, `VerlaufExcel.cs:205`) entstehen als **erzeugter Bereich** an einer Zellmarke; EPOS
fügt Zeilen ein, Inhalte darunter wandern mit, Spalten rechts müssen frei sein.

### 7.4 Formelmappe, Formeln des Anwenders, Diagramme, Paketschutz

- Die Formelmappe bleibt erzeugtes Blatt samt Gegenrechnung, `Nachtragen`, Zellregister: Geometrie
  datenabhängig, Formel oder Wert entscheidet die Gegenrechnung, NPV/IRR rechnet ClosedXML nicht
  (`FormelmappeClosedXmlBefundTests.cs:72`). Reserviert: `Zins_i`, `Zeitraum_T`, `p_E`, `p_B`, `p_I`,
  `Zins_Basis`, Risikonamen, Szenarioanhänge; Vorlagen, die sie führen, lehnt der Prüfer ab.
- Anwenderformeln sind erlaubt, bis zu Ergebnisnamen nur auf reservierte Parameternamen und eigene
  Zellplatzhalter. `FullCalculationOnLoad` setzt EPOS, **sobald die Vorlage irgendeine Formel trägt** (heute nur
  bei eigenen, EBG:201). Ob ClosedXML das `<v>` einer in Excel gespeicherten Vorlagenformel behält, misst BV-E0;
  der Prüfer weist darauf hin, dass Vorschauen solche Zellen leer oder veraltet zeigen.
- **Diagramme sind in Excel Excel-Diagramme aus den Zahlen der Tabellen, keine Bilder** (BV-Q11). In
  Anwendervorlagen zeigen die Diagramme des Anwenders auf Excel-Tabellen `EPOS_<name>`, auf Namen `EPOS.reihe.*`
  oder auf feste Raster (12, 365, 168, 8760); Tabellen und Namen führt EPOS nach, Namen über ihr
  `RefersTo`. Ob die Reihenbezüge eines Diagramms mit einer wachsenden Tabelle mitwandern oder EPOS sie über das SDK
  nachzieht, misst BV-E0. Die Diagramme überstehen das Laden (Probe P3 des Technikbefunds); ihren Zwischenspeicher
  trägt EPOS nicht nach — Excel zeigt die Zellwerte, und der Prüfer weist darauf hin, dass Vorschauen ohne
  Rechenwerk alte Werte zeigen können. Die erzeugten Blätter der Standardmappe bekommen Diagramme, die EPOS über
  das OpenXML SDK auf ihren Tabellenbereichen anlegt, weil ClosedXML keine Diagramme anlegen kann; die
  Machbarkeit misst eine Messprobe in BV-E0. PNG- und SVG-Bilder aus EPOS gibt es in Excel nicht (SVG ginge
  ohnehin nicht, Probe P4).
- Paketschutz: Probe-Laden und -Speichern, Vergleich nach Beziehungs- und Inhaltstyp mit Positivliste
  erwarteter Verluste (`calcChain`, Druckereinstellungen); verlorene Teile (Pivot, Formen, Steuerelemente, VBA)
  mit Namen; eine Ladeausnahme ist ein benannter Fehler. Anhang E nennt in Excel Blatt und Zelle.


## 8 Engine und Technik

### 8.1 Optionen und Bewertung

**A** — eigene Engine: Word auf OpenXML SDK 3.5.1 mit Run-Normalisierer und SDT, Excel auf ClosedXML 0.105.1
mit SDK-Nachlauf (`Directory.Packages.props:37-38`). **B** — DocxTemplater 2.9.7 für Word (Probe P1: Runs,
Kopfzeile, Zeilenschleife; P2: SVG ohne PNG-Rückfall; Ausdrücke über DynamicExpresso), Excel wie A. **C** —
OpenXmlPowerTools-Fortführungen (SkiaSharp ≥ 4.152 statt Pin 3.119, `Directory.Packages.props:54`),
ClosedXML.Report, EPPlus (Polyform Noncommercial), Templater (kommerziell), NPOI, MiniWord, MiniExcel.

| Kriterium | A | B | C |
|---|---|---|---|
| iOS: Reflection, Ausdrücke, AOT | keine neuen Risiken | DynamicExpresso auf dem Gerät ungeprüft; die CI prüft weder Linker noch AOT (`.github/workflows/ios.yml:119-122`) | Reflection, `dynamic`, System.Drawing |
| Paketdrift | kein neues Paket | ein Paket, passt zu OpenXml 3.5.1 | SkiaSharp 4.x oder zweiter Stapel |
| SVG mit PNG-Rückfall | Code vorhanden (WBG:306-360) | eigener Formatierer | offen |
| Fehler benannt, deutsch | ja | englisch; Ausnahme bei fehlendem Wert | offen |
| Ausdruckssprache in Anwenderhand | nein | ja | teils |
| Kapitel über Einfügeanker | direkt | nur mit eigenem Formatierer | nein |
| Aufwand (Technikbefund) | Word 15–18 PT, Excel 6–10 PT | Word S–M plus Formatierer, Gerätetest | M–L |

### 8.2 Empfehlung und Rückfall

**Option A, kein neues Paket.** Die Lizenzlage bleibt unverändert: MIT, dazu Apache-2.0 für das gepinnte
SixLabors.Fonts 1.0.1 (Abhängigkeit von ClosedXML, `EPOS.Kern.csproj:143-146`); die Pins von SkiaSharp 3.119
und SixLabors.Fonts bleiben. Die Engine steht hinter `IVorlagenFueller`; Notweg ist DocxTemplater 2.9.7 mit
eigenem SVG-Formatierer, Fehlerbehandlung „überspringen“ und Gerätetest für DynamicExpresso (iU13). Für
Excel-Vorlagen mit Teilen, die ClosedXML verliert, bleibt der Datenblattmodus (nur erzeugte Blätter und Namen).

### 8.3 Schichten

| Schicht | Ort | Inhalt |
|---|---|---|
| Kern | `EPOS.Kern/Allgemein/Bericht/Vorlagen/` | `Vorlagenfeldkatalog`, `Berichtswerte`, `Berichtstabelle`, `Vorlagenpruefer`, `WordVorlagenfueller`, `ExcelVorlagenfueller`, `Baukasten`; keine Vorlagendateien — die mitgelieferten findet er über `Dienste.Pfade.Berichtsvorlagen` (8.4) |
| Controller | `BerichtCtrl` (erweitert), `BerichtsvorlagenCtrl` (neu) | auflisten, hinzufügen, prüfen, ersetzen, entfernen, kopieren, Baukasten; Dateiseite über `Dienste.Datei`/`Dienste.Pfade` |
| Hülle | `BerichtSeiteGaben.cs`, `BerichtsvorlagenGaben` (neu) | Vorlagenliste mit stabilen Ids, Prüfstand, Befunde, Katalogzeilen; Naht `Berichtsvorlagenwege` |
| Oberfläche | `EPOS.UI` | Vorlagengruppe, Prüfzeile (`Herleitungszeile`), Überlagerungen „Prüfliste“ und „Platzhalterkatalog“, `Vorlagenfeldknopf`, Dienste `Vorlagenfeldansicht`, `IZwischenablage` |
| Werkzeug | `Werkzeuge/Berichtsvorlage` (neu, BV-E0) | erzeugt die Beispielvorlage aus dem bisherigen Bericht und daraus die Standardvorlage in der Stufe der Etappe (6.3) |

### 8.4 Plattformfreiheit und Nähte

Die Engine arbeitet auf `Stream`/`byte[]`. **Die mitgelieferten Vorlagen bleiben Dateien der Auslieferung wie
heute, keine eingebetteten Ressourcen des Kerns** (BV-Q19 b); `EPOS.Kern/CLAUDE.md:34` gilt weiter, die
Papierfortschreibungen, die eine Einbettung verlangt hätte, entfallen. `FindeVorlage` (WBG:116-127) und beide
Lieferwege (`WindowsFormsApplication1.csproj:253-256`, `EPOS.iOS/EPOS.iOS.csproj:122-123`) bleiben; BV-E1 nimmt die
Standardvorlage als eigene Datei neben der Stilvorlage auf — die Dateinamen führt der `BerichtsvorlagenCtrl` als
`DATEI_STANDARD` (`Berichtsvorlage_Standard.docx`) und `DATEI_RUECKFALL` (`Berichtsvorlage.docx`), Entscheid BV-E1-1 —,
BV-E5 den Kurzbericht je Sprache (6.3). Den Ort liefert ab BV-E1 die
neue Eigenschaft **`Berichtsvorlagen` von `IPfade`** statt `AppDomain.BaseDirectory` (Vorbild `Herstellerdaten`,
`Auslieferungsvorlage`): in `StandardPfade` `{app}\Vorlagen`, in `IosPfade` das App-Bundle — eine Änderung an
`EPOS.iOS/`, im Protokoll benannt (8.5); `FindeVorlage` sucht nur noch dort. Fehlt die Standardvorlage selbst, bleibt der
Code-Rückfall — der bisherige Weg mit der Stilvorlage, ohne sie mit den eingebauten Formaten (WBG:74, 139-167) —,
benannt in der Laufmeldung; eine Wache prüft, dass die Auslieferungsdateien vorliegen und in beiden
Lieferwegen stehen (12). **Von den neun Kern-Diensten wächst allein `IPfade` um diese Eigenschaft**, die Wächter
des Kerns bleiben leer. Neu sind in EPOS.UI `Vorlagenfeldansicht` und die Naht `IZwischenablage` mit Windows- und iOS-Adapter (9.6,
BV-Q18), in der Hülle `Berichtsvorlagenwege` (10.3). Was eine Plattform nicht kann, wird benannt abgelehnt.

### 8.5 iOS, Aufwand je Lauf, Sicherheit

- **iOS: ungeprüft, Nachweis offen (iU11/iU13).** Die CI baut den Simulator im Debug ohne Linker, der
  Prüfmodus erzeugt keinen Bericht; ungeprüft sind ClosedXML-Vermessung mit iOS-Schriften, `SkiaMaler.Png` im
  Bericht, Schreiben des OpenXML-Pakets. Vorgesehen ist eine **iOS-Probe** (Prüfmodus erzeugt Word und Excel
  für 1030 aus der Standardvorlage), nur nach Rückfrage. Änderungen an `EPOS.iOS/`: MauiAsset-Zeilen der
  mitgelieferten Vorlagen, `IosPfade.Berichtsvorlagen` (App-Bundle) und Dateifilter
  `org.openxmlformats.wordprocessingml.template` (BV-E1), `org.openxmlformats.spreadsheetml.template` (BV-E7),
  Registrierung von `Vorlagenfeldansicht` und Adapter `IZwischenablage` (BV-E6); jede im Protokoll benannt, ein
  iOS-Lauf nur nach Rückfrage.
- **Aufwand:** Simulation und Wirtschaftlichkeit laufen immer; Zeitreihen, Verlauf, Emissionsbilanz nach
  `Bedarf` — gebaut mit BV-E3 (`Berichtsbedarf`, 5.1), vorher nach den Häkchen (BSG:207-209). Das Füllen malt alle
  Bilder (mit sieben Varianten über 50, PNG doppelt aufgelöst plus SVG). BV-E0 misst den heutigen Lauf (1030 und
  sieben Varianten, Windows und Linux-CI); **Ziel: heute plus höchstens 10 %**. Anhang E wird in einem Durchgang
  gefüllt, die Checkliste zuletzt. **BV-E3 hält das Ziel** (Median dreier Läufe): Messlatte ohne Sammler 1030 Word
  628 → 622 ms, Excel 301 → 251 ms, Gruppe mit sieben Ständen Word 3.871 → 3.711 ms, Excel 611 → 584 ms; Betriebsweg
  mit Wertesatz, Word und Excel zusammen, 1030 475 → 396 ms, Gruppe 2.922 → 2.377 ms — der Kapitalwertverlauf läuft
  einmal statt zweimal.
- **Sicherheit:** Vor dem Öffnen summiert der Prüfer die unkomprimierten Größen per `ZipArchive` (Vorgabe
  100 MB unkomprimiert, 20 MB Datei) und setzt für Word `OpenSettings.MaxCharactersInPart`; das schützt auch
  vor Jetsam auf dem iPad (Umsetzung_iU10_Nachweise Z. 753-754).


## 9 Platzhalter in der App erkennen

### 9.1 Varianten

- **A — Randmarke immer sichtbar:** über hundert Marken für alle verfehlen „ohne Störung“.
- **B — Marke nur beim Überfahren am Rand** (Vorschlag des Anwenders, „Symbol, Mouse-over am Rand“): kein
  Überfahren auf dem iPad, schwer per Tastatur, stört leise auch alle, die nie eine Vorlage pflegen.
- **C — Platzhalteranzeige mit drei Stellungen:** Aus (Vorgabe, kein DOM); **Marken** (Symbol `{ }` in einer
  Randspur, Mouse-over mit Schlüssel, Klick oder Tipp öffnet die Aufklappung); **Schlüssel** — der
  Vorlagenmodus (Schlüssel als Chip, gestrichelter Rahmen, ein Klick kopiert).
- **D — Platzhalterkatalog mit Suche:** vollständig, auch ohne Gegenstück in der App; keine Erkennung am Ort.

### 9.2 Bewertung

| Kriterium | A | B | C | D |
|---|---|---|---|---|
| stört ausgeschaltet nicht | nein | kaum | ja | ja |
| am Ort erkennbar | ja | ja, mit Maus | ja | nein |
| iPad, Tastatur | laut oder zu klein | nein | ja (Tipp, Fokus) | ja |
| vollständig | nein | nein | nein | ja |
| beantwortet „Symbol, Mouse-over am Rand“ | teils | ja | ja (Stellung Marken) | nein |
| Aufwand | S | S | M | M |

### 9.3 Empfehlung

**C für die Erkennung am Ort, D als Rückgrat**; beide lesen denselben Katalog, „In der App zeigen“ verbindet
sie. Der Vorschlag des Anwenders ist die Stellung „Marken“; „Schlüssel“ ist die Arbeitsstellung des
Vorlagenautors, in der man nicht erst überfahren muss.

### 9.4 Bedienung

| Punkt | Regel |
|---|---|
| Umschalter | **ein Umschalter je Bildschirm** (`{ }` · Aus · Marken · Schlüssel, `title` „Platzhalter zeigen“) in der Kopfzeile jeder Ansicht mit Marken; bei „Berichte & Kosten“ in `epos-navigation-kopf` nach der `Kopfzeile`, unabhängig vom Ort der Hilfepille (`BerichteKostenSeite.razor:68-82`); nicht zusätzlich in der Vorlagengruppe (dort führt „Platzhalter…“ zu Katalog und Baukasten). Alle Umschalter binden an denselben Zustand; ausgeschaltet ist das ruhige Symbol die einzige sichtbare Spur (Teilfrage BV-Q9) — an jedem Ort steht dann nur ein verborgener Anker ohne Layout mit `data-vorlagenfeld` und `data-vorlagenfeldstufe`, an dem Wachen und Proben den Ort finden. Umschalter stehen in „Berichte & Kosten“ und in der Simulation (`BerichteKostenSeite.razor`, `SimulationSeite.razor`). Einschalten auch über „In der App zeigen“ im Katalog |
| Ausschalten | am Umschalter und in jeder Aufklappung („Platzhalter ausblenden“); keine eigene Leiste, kein ✕; in der Stellung „Schlüssel“ nennt eine leise Zeile die Zahl der Platzhalter — die verschiedenen Schlüssel der gezeichneten Marken, unter Windows über alle WebViews — und öffnet „Katalog…“ (ohne Baukasten); der Zustand gilt für die Sitzung |
| Aufklappung | Schlüssel, Art, Kontext, Beschreibung, Beispielausgabe samt Einheit und Leerwert („1.234 MWh/a“, leer „—“), Excel-Name, „in Excel nur als Listenzeile“ oder bei Bildern „in Excel als Diagramm auf dem Tabellenbereich“ (BV-Q11), Stufe „entspricht“ oder „ähnlich im Bericht“ mit Hinweis („im Bericht als Kuchendiagramm“), „Kopieren“, „Platzhalter ausblenden“ |
| Kopieren je Art | Text, Zahl, Datum `{{schlüssel}}`; Tabelle, Liste, Kapitel dasselbe mit „in einen eigenen Absatz“; Bild der Schlüssel mit „Bild einfügen, Alternativtext = Schlüssel“; `stand.*` samt Blockrahmen `{{#je stand}}` … `{{/je}}` |
| Kacheln, Diagramme | Marke oben rechts; am `DiagrammSvg` mit Abstand zur umbrechenden Zoomleiste (`epos-ui.css:1719-1724`), zugeordnet über `Vorlagenfeld`, nie über `Kennung` |
| Tabellen | eine Marke am Tabellenkopf, die Aufklappung listet Zeilen- und Spaltenschlüssel; keine Zeilenmarken (Aktionsknöpfe einer Zeile wären immer sichtbar, `EPOS.UI/CLAUDE.md:37-38`; bei elf Ständen überlagerten sich Trefferflächen) |
| In der App zeigen | springt zur Seite, schaltet „Marken“ ein, lässt das Element 2,5 s aufleuchten — nur, wenn das Ziel lesend erreichbar und darstellbar ist; nie in den Projektassistenten (Bearbeitungsmodus); ohne erreichbaren Ort ist der Knopf weich gesperrt und nennt den Grund (kein Gegenstück in der App, nur in einer Eingabemaske, nur im Projektassistenten). Der Sprung geht über `Dienste.Navigation` mit dem Reiter als erstem Argument: „Berichte & Kosten“ mit der Seite (`WIRTSCHAFT`, `KOSTEN`, `UEBERSICHT`), die Ergebnisblätter über die Ansicht `SIMULATION` mit der Marke `schritt=3;blatt=<Blatt>` — so springen Windows und iOS gleich |

### 9.5 Abdeckung und Kontextregel

**Regel: Eine Marke nennt nur einen Schlüssel, der genau den angezeigten Wert erzeugt.** Die Kacheln der
Wirtschaftlichkeitsseite zeigen die beste Variante, ohne Variante den Stamm; die Regel steht im Kern
(`BesteVariante.Waehle`, gebaut mit BV-E3, 5.1): Maßgeblich ist der Erwartungsfall; es zählen die gewählten Stände in
ihrer Reihenfolge, je Stand das erste Ergebnis; Kriterium ist die größte Kapitalwertdifferenz unter den Ergebnissen ohne den
Merker `IstStamm` — die Referenz nimmt nie teil, auch eine negative Differenz gewinnt —, bei Gleichstand der erste
Stand; ohne Variante mit Differenz steht das Ergebnis des Stamms, sonst keins. Derselben Regel folgt die Leitversion
der Zahlungsgliederung (BV-E4-1). Marken `wirtschaft.beste.anzeige`, `wirtschaft.beste.kapitalwert` usw.:
`wirtschaft.beste.kapitalwert` ist der Kachelwert — bei einer Variante ihre Kapitalwertdifferenz, im Stammfall der
Nettobarwert des Stamms, den die Karte „Kapitalwert ggü. Stamm“ dann zeigt —, `wirtschaft.beste.ist_stamm` sagt, welcher
Fall vorliegt; `wirtschaft.beste.kapitalwert_diff` ist im Stammfall leer mit dem Grund „nur Stammprojekt gerechnet“. Die
Kostenkacheln zeigen „Stamm oder Variante“
(`KostenSeiteGaben.cs:60`): beim Stamm `stamm.wirtschaft.investition`, bei einer Variante
`stand.wirtschaft.investition` mit Blockhinweis (für Investition und Betrieb gibt es keine Kennzahl), beide mit der Stufe
„ähnlich im Bericht“: Die Kostenerfassung zeigt Cent, der Bericht rechnet aus der Wirtschaftlichkeit in ganzen Euro. Die
Ergebnisansicht der Simulation ebenso: Stamm → `stamm.kennzahl.*`, Variante → `stand.kennzahl.*`; Deckungsgrad und JAZ
Kälte tragen „ähnlich“ mit dem Hinweis, dass der Bericht aus den gespeicherten Summen rechnet (1017: 98,4 gegen 98,5 %,
4,52 gegen 4,51; die Angleichung ist ein eigener Auftrag). Die Komponententabelle der Übersicht trägt
`tabelle.komponenten.matrix` (ähnlich: im Bericht die Komponentenmatrix des Kapitels „Projekt“ mit allen Merkmalen). Zeigt die App
ein ähnliches Element (Ring statt Kuchen, Speicherbetrieb statt Speicherverlauf, alle Geräte statt des ersten),
trägt die Marke die Stufe „ähnlich im Bericht“.

**Abdeckung:** Jede `Kennzahlkachel`, jedes `DiagrammSvg` und jede `Vergleichstabelle` unter
`EPOS.UI/Seiten/Berichte` und `EPOS.UI/Seiten/Simulation` trägt ein `Vorlagenfeld` oder steht als benannte
Ausnahme in der Abdeckungswache (`VorlagenfeldAbdeckungWacheTests`); einbezogen sind der Helfer `@Kennzahl(…)` (`UebersichtReiter.razor:147`), die
Direkttabelle `UebersichtSeite.razor:161-162`, WS:264, :446, :1631, :1665, `ProjektKopfSeite.razor:5` und
`SimulationErgebnisSeite.razor:154`. `Vergleichstabelle` in Administrationsdialogen trägt keine Marke. Benannte Ausnahmen
sind der Leerzustand ohne Lauf, die Deckung Wärme und Strom über alle Erzeuger im Dashboard, der Kältering, 13 Bedarfs-
und Erzeugerbilder (`bild.ergebnis.*` vorgemerkt, nicht in Fassung 4) und die Kacheln der Autarkieanalyse und des
Speicherlaufs ohne Katalogschlüssel; ohne Marke bleiben auch der Strombedarf mit Eigenverbrauch im Dashboard und die
Klimaregion. Die Wache `VorlagenfeldAnzeigewertWacheTests` prüft für 1030, 1019 und 1017, dass jede gesetzte Marke der
Hüllen in `Vorlagenfeldorte` steht und den aufgelösten Katalogwert zeigt (eine Abweichung nur im Format N2/N0 über
`|stellen 2`). Die Anhang-E-Überlagerung
(`AnhangEChecklisteKnopf.razor`, WS:1665) nennt die Stelle der gewählten Vorlage (Prüferlauf ohne Füllen aus
Katalog und `Deckt`), sonst mit dem Zusatz „bezogen auf die Standardvorlage“.

### 9.6 Technik

- **`Vorlagenfeldknopf`** nimmt ein neutrales DTO `Vorlagenfeldanzeige` (Schlüssel, Art, Kontext,
  Beschreibung, Beispielausgabe, Excel-Name, Stufe) aus einem Halter in EPOS.UI, den die Hülle aus dem Katalog
  füllt; der Baustein kennt keine Fachklasse (`EPOS.UI/CLAUDE.md:305-306`).
- Einen optionalen Parameter `Vorlagenfeld` (Schlüssel) bekommen `Kennzahlkachel` (`Kennzahlkachel.razor:44-56`),
  `DiagrammSvg`, `Vergleichstabelle` und `Textfeld`. Die Orte stehen in einer Tabelle `Vorlagenfeldorte` in
  EPOS.UI (Zeilen Schlüssel ↔ `Vorlagenfeldort(Ansicht, Reiter, Element, NurLesend)`, 53 Zeilen), die
  `VorlagenfeldorteWacheTests` gegen Katalog, Blätter und Parameter hält; `Vorlagenfeldzeige` wählt daraus das Sprungziel.
- **`Vorlagenfeldansicht`:** Zustand mit Ereignis wie `SeitenZustand`, aber als **Singleton im
  Dienstverzeichnis**: Unter Windows teilen alle `BlazorWebView`-Wurzeln ein Verzeichnis (`BlazorDienste.cs:38`),
  auf iOS die MAUI-Anwendung; jede Marke abonniert und zeichnet über `InvokeAsync`. Ein `CascadingValue` aus
  `AppWurzel` erreichte die Wurzeln der Windows-Dialoge nicht.
- **`IZwischenablage`** (`EPOS.UI/Dienste`): Windows-Adapter `WindowsZwischenablage` über die Zwischenablage der Schale
  (`BlazorDienste.cs`), iOS-Adapter `IosZwischenablage` über `Clipboard.Default.SetTextAsync` (`MauiProgram.cs`); ohne Adapter zeigt die Aufklappung den Text markiert. Die Marke meldet nur.

### 9.7 Hausregeln

| Regel | Inhalt |
|---|---|
| Element | `<button>` mit `aria-label` („Platzhalter projekt.kunde kopieren“ in der Stellung „Schlüssel“, „Platzhalter projekt.kunde: Angaben zeigen und kopieren“ in „Marken“) und `title` mit Schlüssel und Kurzbeschreibung — das Mouse-over der Stellung „Marken“, wie an der Hilfepille (`InfoKnopf.razor:61-62, 71-73`); Überfahren oder Fokus öffnet die Aufklappung, Klick oder Tipp heftet sie an |
| Trefferfläche | die Marke ist ein voller 44-px-Knopf (`--epos-touchziel`, `EPOS.UI/CLAUDE.md:35-37`) in einer eigenen Randspur bzw. der Titelzeile des Elements; ohne Pseudofläche überlagert sie weder Sortierung noch Kennzeichen noch Hilfepille; die 28 px der Hilfepille sind kein Vorbild |
| Gestalt | immer Symbol `{ }` und Text bzw. `title`, nie nur Farbe; nur vorhandene Tokens (`--epos-marke`, `--epos-rahmen-leise`, `--epos-flaeche`, `--epos-ecke`, `--epos-warn-*`); kein CSS-Nesting; eigener `forced-colors`-Block; „kopiert“ 1,5 s |
| Prüfzeile | eine `Herleitungszeile` (vorhandener Baustein) mit Symbol, Text und optionalem „anzeigen“, kein `Kennzeichen` — das ist das Schloss eines Auslieferungssatzes ohne Textknoten (`Kennzeichen.razor:15-22`) |
| Überlagerungen | „Prüfliste“ und „Platzhalterkatalog“ in vier Teilen (`*Daten`, `*Texte`, `Dialog.razor`, Hülle; `EPOS.UI/CLAUDE.md:342-352`), Schließkreuz, Esc, `InfoKnopf` mit Hilfeschlüssel und Wiki-Anker, bunit mit Rückweg und ohne Gaben; ohne Arbeitsstand, deshalb primäres „Schließen“ statt OK/Abbrechen (Prüfliste: „Meldungen kopieren“ · Füller · „Schließen“, keine Probefüllung nach BV-Q12; Katalog: „Baukasten speichern“ · Füller · „Schließen“) |
| Anmeldung, Texte | Vorlagenwahl, Anzeigestufe, Katalogsuche in `KiMaskenabdeckungWacheTests`; Ressourcen `VF_KNOPF_*`, `VF_ANZEIGE_*`, `VF_KATALOG_*`, `VF_PRUEF_*` de/en, danach `designer_neu.py schreiben` |

### 9.8 Mockup

`Dokumentation/aktuell/Mockups/Berichtsvorlagen_Platzhaltermarke.html` ist ein eigenständiges Schema mit den
Tokens aus `epos-ui.css` und neutralen Werten. Teil 1 zeigt eine Beispielansicht (Projektstammdaten mit
`projekt.*`, Kachel „JAZ Wärmepumpe“ des Stamms mit `stamm.kennzahl.eff.jaz`, Vergleichstabelle mit
`tabelle.vergleich.effizienz` und Blockhinweis für Werte je Stand, Diagrammrahmen mit
`bild.vergleich.balken.eff.jaz`) in den drei Zuständen:

1. **Marke aus:** keine Marke, kein Rahmen; in der Kopfzeile nur der ruhige Umschalter `{ }`.
2. **Marke an mit Mouse-over:** das Symbol `{ }` in der Randspur jedes Elements; Überfahren oder Fokus öffnet
   die Aufklappung mit Schlüssel und Kurzbeschreibung, Klick oder Tipp heftet sie an.
3. **Vorlagenmodus mit Chips und Kopieren:** jede Marke als Chip mit ihrem Schlüssel, gestrichelte Rahmen,
   „Kopieren“ mit Rückmeldung „kopiert“ und eine leise Zeile mit der Zahl der Platzhalter.

Teil 2 zeigt die Berichtsseite nach dem Umbau, „wie heute“ und „neu“ gekennzeichnet; Teile 3 bis 5 zeigen
Bedienregeln, einen Katalogauszug und die Festlegungen des Mockups (drei Stellungen, Randspur, keine Leiste,
ein Umschalter je Bildschirm, Schloss und Herleitungszeile, eine Marke je Tabelle), die dieses Papier
übernimmt. Teil 2 folgt 10.2: „Standard (EPOS-Plan)“ und eigene Vorlagen im Auswahlfeld,
„Neue Vorlage…“ als erster Knopf, der Kurzbericht nur als Kopie erreichbar. Mit dem Entscheid BV-Q9 (c) vom
25.09.2026 gilt das Mockup als angenommen.


## 10 Berichtsseite nach dem Umbau, Ablage, Auswahl und Auslieferung

### 10.1 Einstieg und was unverändert bleibt

- Einstieg „Berichte & Kosten › Bericht“ (`MENU_VARIANTEN_BERICHT`); links die Variantenliste (Stamm fest;
  Art, Bezeichner, Projektname, Stromspeicher, Simulation mit Stand bzw. „fehlt“; Alle, Keine); Ausgabe Word,
  Excel oder Beide; Zielordner mit Durchsuchen; Erstellen, Fortschritt, Abbrechen.
- Der Hinweis „Jeder Bericht rechnet neu: alle gewählten Varianten werden simuliert und wirtschaftlich
  bewertet.“ (`BerichtSeite.razor:184`) bleibt Hinweis, keine Option: **`SammleFuerBericht` läuft vor jeder
  Ausgabe**, für jede Vorlage und jeden Einstieg.
- „Berichtsbausteine“ mit acht Titeln und Vorbelegung, Hinweis `BK_BER_MSG_WIRTSCHAFT_HINWEIS` (:212);
  Dateiname `<Stamm>_Bericht_<Datum>` mit `_2 … _20` (die Vorlage steht in `custom.xml` als `EPOS.Vorlage`);
  Konfiguration je Stammprojekt, Laufmeldung, „öffnen?“; Vergleichssicht der Ergebnisansicht (BSG:38-44,
  :226); Einzelmappe „Kapitalwert-Verlauf“ ohne Vorlage (`KapitalwertVerlaufHuelle.cs:325`); Diagrammfarben
  und Schrift wie heute (BV-Q17).

### 10.2 Neu: Gruppe „Vorlage“ oben rechts, über den Häkchen

| Element | Inhalt |
|---|---|
| Auswahlfeld „Word-Vorlage“ | „Standard (EPOS-Plan)“ und eigene Vorlagen mit stabilen Ids aus der Hülle (`Auswahlfeld.razor:64-70`); eine gespeicherte, fehlende Vorlage bleibt als gesperrter Eintrag („nicht vorhanden – Standard verwendet“, `GesperrtHinweise` :111-122); bei der mitgelieferten Vorlage daneben das `Kennzeichen` (Schloss) |
| Knöpfe | „Neue Vorlage…“ (Kopie der Standardvorlage oder des Kurzberichts — der erste Schritt jedes Autors), „Hinzufügen…“, „Prüfen“, „Platzhalter…“ (Katalog, Baukasten) |
| Menü „…“ | Windows „In Word öffnen“, „Im Ordner zeigen“, „Ersetzen…“, „Entfernen“; iOS „Teilen…“, „Ersetzen…“, „Entfernen“; mitgeliefert nur „Schreibgeschützt öffnen“ mit dem Hinweis, dass Änderungen nicht gespeichert werden |
| Prüfzeile (`Herleitungszeile`) | „geprüft, 23 Platzhalter, keine Befunde“ oder „2 unbekannte Platzhalter – anzeigen“; unter Windows „Original geändert – übernehmen?“ (10.3; in BV-E1 nicht verdrahtet, den Vergleich hat `BerichtsvorlagenCtrl.OriginalGeaendert`) |
| Weitere Zeilen | aktive Vergleichssicht, wenn die Vorlage den Paarvergleich nutzt; „Excel-Vorlage“ ab BV-E7 (nur Excel oder Beide); der Umschalter der Platzhalteranzeige steht in der Kopfzeile der Seite, nicht in der Gruppe (9.4) |
| Texte | Bündel mit Ressourcenschlüsseln im Kommentar (`EPOS.UI/CLAUDE.md:22-24`); gebaut in BV-E1: `BerichtSeiteVorlagentexte` für die Gruppe, `PrueflisteTexte`, `PlatzhalterkatalogTexte` und `EinstellungenBerichtTexte` für Überlagerungen und Einstellungen, gehalten von einer Bündelwache |
| Häkchen (BV-Q1 c) | schalten Kapitelplatzhalter und `baustein.*`, keine Einzelplatzhalter; gerechnet wird ohnehin alles (BDS:172-193); ausgegraut „in dieser Vorlage nicht enthalten“ nur, wenn weder Word- noch Excel-Vorlage Kapitel bzw. Blatt führt; die Liste bleibt, sobald die Vorlage `kapitel.*` oder `baustein.*` nutzt, sonst „Den Inhalt bestimmt die Vorlage“; `B_*` bleiben; Excel bis BV-E7 wie heute (BV-Q2) |

**Ablauf von „Erstellen“:** (1) Die Schnellprüfung liest die Vorlage einmal in den Speicher. (2) Ohne Befund
kommt die heutige Startrückfrage `BK_BER_FRAGE_START` (:218); mit Fehlern, abweichender Sprache oder
unpassender Sicht ersetzt sie **eine** erweiterte Rückfrage (Baustein `Rueckfrage`) mit Zahl der Varianten,
Name der Vorlage, Befunden und drei Wegen „Mit meiner Vorlage“ (unbekannte Stellen gelb), „Mit
Standardvorlage“, „Abbrechen“ (BV-Q6). (3) `SammleFuerBericht` läuft unverändert. (4) Word, dann Excel werden
aus den gelesenen Bytes gefüllt. (5) Die Laufmeldung nennt Vorlage, Rückfälle, unbekannte und zusammengefasst
leere Platzhalter, entfernte Kommentare und alle Warnungen.

**Zweiter Einstieg:** „Bericht erzeugen“ auf der Wirtschaftlichkeitsseite (`ErzeugeFuerVergleich`,
BSG:303-327) erzwingt `B_WIRTSCHAFT` (BSG:314-315), nimmt die gespeicherte Vorlagenwahl und prüft vor. Führt die
Vorlage keinen Schlüssel der Wirtschaftlichkeit — geprüft über Katalogbereich und `Deckt`, also auch
`stand.wirtschaft.*`, `stand.bandbreite.*`, `bild.wirtschaft.*`, `tabelle.wirtschaft.*` —, bietet die Rückfrage
für diesen Lauf die Standardvorlage an und nennt die gewählte. Die KI-Sicht (`BausteineLesen`, :317) lernt in
BV-E1, die Vorlage zu lesen und zu setzen.

**Stand nach BV-E1 (#512).** Gebaut sind das Auswahlfeld mit Schloss und gesperrtem Eintrag, „Neue Vorlage…“ als Kopie
der Standardvorlage (der Kurzbericht kommt mit BV-E5), „Hinzufügen…“, „Prüfen“, „Platzhalter…“ mit dem Katalog (der
Baukasten kommt mit BV-E5), das Menü „…“ je Plattform, die Prüfzeile mit „anzeigen“ und Prüfliste, die erweiterte
Rückfrage mit drei Wegen, der Lauf, der genau die geprüften Bytes füllt, die Rubrik „Bericht“ der Einstellungen und das
KI-Feld `vorlage` der Berichtsseite. **Der zweite Einstieg fragt in BV-E1 nicht zurück:** Führt die Vorlage Platzhalter,
aber keinen der Wirtschaftlichkeit, entsteht dieser eine Bericht mit der Standardvorlage, und die Laufmeldung nennt es;
eine Vorlage ganz ohne Platzhalter bekommt den Bericht an ihr Ende. **Noch nicht gebaut:** die Zeile „Original geändert –
übernehmen?“ (den Vergleich hat der Kern), eine Bedienung der Vorgabe (`BerichtVorlageWord` wird gelesen, gesetzt nur über
`SetzeVorgabeWord` im Controller; die Wahl der Seite ist die Abweichung des Stammprojekts), die Katalogsuche des
Assistenten (das KI-Feld ist nicht angemeldet), die Häkchenregeln (BV-E2) und die Zeile „Excel-Vorlage“ (BV-E7).

**Stand nach BV-E2 (#520).** Die Häkchen folgen der gewählten Word-Vorlage: Die Hülle leitet aus der Schnellprüfung
(`Pruefbefund.Bausteine`, `HatKapitel`, `DeckblattAusPlatzhaltern`) den `Kapitelstand` ab, die Seite verbindet ihn mit dem
Ausgabeformat. Ein Eintrag, dessen Kapitel die Vorlage nicht führt, steht **ausgegraut und weich gesperrt** mit dem Grund
„in dieser Vorlage nicht enthalten“ — nur wenn Word entsteht und auch die Excel-Mappe ihn nicht führt: bei „Beide“ also
nur die reinen Word-Bausteine Deckblatt, Inhaltsverzeichnis und Anhang, bei „Excel“ keiner (die Mappe folgt den Häkchen bis
BV-E7 wie heute, BV-Q2). Trägt die Vorlage ihr **Deckblatt aus Platzhaltern** — wie die Standardvorlage —, steht
„Deckblatt“ ausgegraut mit dem Grund „Deckblatt kommt aus der Vorlage“. Das Häkchen bleibt gespeichert und wirkt wieder,
sobald eine Vorlage das Kapitel führt; ein Klick auf den Eintrag nennt den Grund in einem Banner mit Verfall, „Alle“ lässt
gesperrte Einträge unverändert. Führt die Vorlage nur Einzelplatzhalter und kein Kapitel, steht bei Ausgabe Word statt der
Liste die leise Zeile **„Den Inhalt bestimmt die Vorlage – sie führt einzelne Platzhalter, aber kein Kapitel.“**; eine
Vorlage mit Sammelanker, ohne Platzhalter oder eine unlesbare lässt alle Einträge frei. Die Häkchen tragen ihre Titel
zweisprachig aus `MyResource` (`BK_BER_BAUSTEIN_*`). Die Überlagerung **„Anhang-E-Checkliste…“** der
Wirtschaftlichkeitsseite nennt in der Spalte „Stelle“ die Kapitel der gewählten Vorlage des Stammprojekts
(`BerichtCtrl.KapitelstellenDerVorlage`, gefragt mit den Häkchen des zweiten Einstiegs, also mit „Wirtschaftlichkeit“),
ein nicht geführtes Kapitel als „nicht im Bericht“; eine leise Zeile darunter nennt den Bezug („Die Stellen im Bericht
sind bezogen auf die Standardvorlage.“ bzw. „… nennen die Kapitel der Vorlage „…““). Der zweite Einstieg fragt weiter nicht
zurück; dass eine Vorlage die Wirtschaftlichkeit führt, erkennt der Prüfer jetzt auch an einem einzeln geführten
`kapitel.wirtschaftlichkeit` (über `Gedeckt`, 5.3).
**Noch nicht gebaut:** wie nach BV-E1 „Original geändert – übernehmen?“, eine Bedienung der Vorgabe, die Katalogsuche des
Assistenten und die Zeile „Excel-Vorlage“ (BV-E7).

**Stand nach BV-E5 (#541).** „Neue Vorlage…“ fragt mit der Optionsgruppe **„Kopie von:“** nach dem Muster — Standardvorlage
oder Kurzbericht in der Sprache der Oberfläche (`BerichtsvorlagenCtrl.NeueVorlage(name, muster, englisch)`,
`BerichtsvorlagenGaben.Mustereintraege`/`NeueVorlageAusMuster`); der Kurzbericht ist nur so erreichbar, nicht im
Auswahlfeld. Der Platzhalterkatalog trägt den Knopf **„Baukasten speichern…“** (`BerichtsvorlagenCtrl.Baukasten`/
`SpeichereBaukasten`, Hülle `BaukastenSpeichern`): unter Windows die Datei im Vorlagenordner, auf iOS danach das
Teilen-Blatt (Entscheid BV-E5-5; scheitert es, nennt die Meldung den Pfad). Die Häkchen folgen `Deckt` der Kapitel mit
Tabellen und Bildern (Entscheid BV-E5-3, 5.3).

### 10.3 Ablage, Auswahl, Plattformen, Datenbank

| Thema | Regel |
|---|---|
| Mitgeliefert | Dateien der Auslieferung wie heute (BV-Q19 b): die Standardvorlage Word `Berichtsvorlage_Standard.docx` im vollen Aufbau der Beispielvorlage (Werkzeug `beispiel --standard`, 6.3; das Logo der Kopfzeile als Bildplatzhalter, 6.5; BV-E2), daneben die Stilvorlage `Berichtsvorlage.docx` (Entscheid BV-E1-1, Zeile „Setup, Stilvorlage“) und ab BV-E5 der Kurzbericht je Sprache, unter Windows in `{app}\Vorlagen` (`WindowsFormsApplication1.csproj`), auf iOS im App-Bundle (MauiAsset); Ort über `Dienste.Pfade.Berichtsvorlagen` (8.4); die Standard-Excel-Mappe entsteht im Code (7.1), der Baukasten aus dem Katalog; schreibgeschützt, mit jedem Update erneuert, ändern nur über eine Kopie |
| Vorlagenordner | extern und wählbar mit Vorgabe (BV-Q15): Einstellung `BerichtVorlagenordner` (`IEinstellungen`), Vorgabe `Dienste.Pfade.Dokumente` + `EPOS-Plan/Berichtsvorlagen`, gewählt im `EinstellungenDialog`, Abschnitt „Bericht“; unter Windows jeder Ordner, auch ein gemeinsamer Ordner des Büros; auf iOS fest die Sandbox, die Wahl wird benannt abgelehnt; die Vorlagenliste liest diesen Ordner; „Hinzufügen“ kopiert hierher und merkt Herkunftspfad und Prüfsumme (Ablagedatei `.berichtsvorlagen.json` im Vorlagenordner); bearbeitet wird am Ort; Updates fassen den Ordner nie an |
| Liste | nur `*.docx`, `*.dotx` (ab BV-E7 auch `*.xlsx`, `*.xltx`), ohne `~$` und versteckte Dateien; gleicher Name → „Ersetzen“ oder „Unter neuem Namen“ — in BV-E1 vergibt „Hinzufügen…“ einen freien Namen „Name (2)“ und nennt ihn, ersetzt wird über „Ersetzen…“ im Menü „…“; „Entfernen“ legt die Datei in den Unterordner `Entfernt` |
| Sicherung, Cloud | die Datenbanksicherung nimmt den Ordner nicht mit ([BETRIEB_SQLITE.md](BETRIEB_SQLITE.md), Abschnitt 3), das Wiki sagt es; liegt die Vorlage in einem synchronisierten Ordner nur online: „Vorlage nicht lokal verfügbar“; gemeinsame Vorlagen eines Büros: den Vorlagenordner auf einen gemeinsamen Ordner stellen (BV-Q15, Windows); ist er nicht erreichbar, gilt die Zeile „Abweichung“ |
| Vorgabe | `IEinstellungen` `BerichtVorlageWord`/`BerichtVorlageExcel` (Hausschreibweise, `IEinstellungen.cs:4-6`); unter Windows **je Windows-Anwender** (HKCU), auf iOS je App; in BV-E1 gelesen, aber noch ohne Bedienung (10.2) |
| Abweichung | je Stammprojekt in `KonfigJson` als Quelle und Dateiname (gebaut: `VorlageWordQuelle`, `VorlageWordDatei`; Excel ab BV-E7); eine Datei, keine Datenbankzeile, „Beziehungen über IDs“ unberührt; fehlt die Datei: Vorgabe, dann Standard, benannt |
| Windows | Naht `Berichtsvorlagenwege` in der Hülle für „Im Ordner zeigen“ (`IDateiDienst` kennt es nicht, `MitSystemOeffnen` liefert für Ordner `false`, `WindowsDateiDienst.cs:105-107`) und „In Word öffnen“ |
| iOS | Vorlage über `DateiOeffnenAsync` sofort in die Sandbox kopiert, kein externer Verweis; „In Word öffnen“ gibt es nicht (Teilen-Blatt = Kopie), der Menüpunkt heißt „Teilen…“; „Im Ordner zeigen“ benannt abgelehnt. **Pflegeweg** (BV-Q16, am Gerät zu messen): App „Dateien“ › Auf meinem iPad › EPOS-Plan › Berichtsvorlagen, in Word über „Durchsuchen“ öffnen und am Ort speichern (`Info.plist:38-40`); nach der Rückkehr vergleicht die Schnellprüfung die Prüfsumme; Rückweg „Ersetzen…“. Wiki: Word für iPad auf großen Geräten nur mit Abonnement, keine Inhaltssteuerelemente, kein Überfahren (Wissensstand, BV-E0) |
| Datenbank | **kein Schemaschritt**; neues JSON-Feld in `Berichtskonfiguration` (Fremdschlüssel, `CASCADE`); BV-E0 entfernt oder begründet die Ad-hoc-DDL (`BerichtCtrl.cs:154-170`) und prüft das Löschen per Hand (`ProjektCtrl.cs:440-447`) samt veralteter Kommentare dort und in `ProjektDuplizierenCtrl.cs:68-78`, im Protokoll |
| Duplizieren, Transfer | die Konfiguration steht in `AUSNAHME_TABELLEN` (`ProjektDuplizierenCtrl.cs:86-90`), der Transfer übernimmt diesen Plan (`ProjektExportImportCtrl.cs:414-416`): Vorlagenwahl und Vorlagen reisen nicht mit, beim Empfänger gilt dessen Vorgabe (BV-Q5) |
| Setup, Stilvorlage | **Entscheid BV-E1-1 (Auftraggeber 25.09.2026, vom Anwender bestätigt 26.09.2026): kein Übernahmeschritt und kein `[InstallDelete]`.** `Berichtsvorlage.docx` bleibt als Stilvorlage ausgeliefert — Rückfall des Codes, wenn die Standardvorlage fehlt, und Quelle der Bereinigung —, die Standardvorlage heißt `Berichtsvorlage_Standard.docx`; beide stehen in beiden Lieferwegen, der Kommentar zu `[Files]` in `Setup/EPOS-Plan.iss` nennt beide. Grund: Eine Datei unter `{app}` hat der Anwender nie gepflegt (schreibgeschützt, mit jedem Update erneuert), und ein Vergleich beim ersten Start nach dem Setup sähe nichts mehr, weil das Setup die Datei schon überschrieben hat. Setup-Lauf nach Rückfrage (Standardvorlage in `{app}\Vorlagen`) |
| Ersteller, Lizenz | `ersteller.firma` = Einstellung `BerichtFirma`, vorbelegt aus `LizenzToken.Firma` (`LizenzToken.cs:29`), bei `demo`/`person` (:33) evtl. leer (Leerwert „leer“); im `EinstellungenDialog`, neuer Abschnitt „Bericht“, mit Ressourcen, Hilfeschlüssel, KI-Anmeldung (BV-E1); `ersteller.programm` („EPOS-Plan“) und `ersteller.version` (Produktfassung) brauchen keine Einstellung; `projekt.bearbeiter` bleibt die Person (BV-Q8); derselbe Abschnitt „Bericht“ trägt den Vorlagenordner; Vorlagen für alle Lizenztypen (BV-Q13). **Logo** (Entscheid BV-E2-1, 6.5): `bild.ersteller.logo` = Einstellung `BerichtLogo`, der Pfad einer PNG- oder JPEG-Datei bis 5 MB, leer = kein Logo (`BerichtsvorlagenCtrl.EINSTELLUNG_LOGO`, `LogoPfad`, `SchreibeLogo`; `Ersteller()` lädt die Datei einmal je Lauf); im Abschnitt „Bericht“ das Feld „Logo“ mit „Durchsuchen“ und „Entfernen“, unter dem Feld „Datei nicht gefunden.“, wenn die Datei fehlt; „Standardwerte“ leert es; KI-Feld `bericht_logo` |


## 11 Migration vom Bausteinweg

1. **Messlatte zuerst (BV-E0):** Struktur des heutigen Berichts mit echter Vorlage für 1030 und synthetische
   Daten (Überschriftenfolge, Tabellenköpfe, Absatztexte, Bildstellen). Begründete Abweichungen:
   Stiletiketten nach Bereinigung, Lage der Wirkungstafel (Nr. 5), `{{bericht.datum}}` statt DATE, ab BV-E2 das
   Deckblatt der Standardvorlage aus Platzhaltern (Anhang B.3) — gebaut: Die Messlatten des Vorlagenwegs
   (`Bericht_Word_1030_Vorlage.txt`, `Bericht_Word_Gruppe_Vorlage.txt`) weichen nur im Deckblatt ab, ihr Kapitelteil ist
   zeilengleich zum alten Weg, der Kapitelkopf steht im Format „EPOS Kapitelkopf“ statt Überschrift 1.
2. **Standardvorlage in zwei Schritten:** aus der Beispielvorlage (6.3) mit `{{bericht.inhalt}}` als
   `Berichtsvorlage_Standard.docx` (BV-E1, umgesetzt), dann mit einzelnen Kapiteln (BV-E2, umgesetzt #520: `beispiel --standard`); beide treffen die Messlatte. **Rückfall:** gewählte Vorlage → (Rückfrage) →
   Standardvorlage, benannt.
3. **Anhang E:** `kapitel.anhang_e` am Häkchen „Wirtschaftlichkeit“; „Stelle im Bericht“ nennt die tatsächliche
   Überschrift vor dem Anker — dank Rollenauflösung (6.2) auch in deutschen Vorlagen —, in Excel Blatt und
   Zelle, in einem Durchgang (8.5); fehlt eine Stelle: „nicht im Bericht“ mit Prüferwarnung; mit der
   Standardvorlage hält `BerichtBlattstrukturWacheTests.cs:1115`. Der Word-Teil ist mit BV-E2 (#520) gebaut: Die Engine
   sammelt die Stellen vor dem Füllen (`Fuellergebnis.Kapitelstellen`), der Prüfer ohne Füllen (`Pruefbefund.Kapitelstellen`,
   Warnung „Anhang E ohne Stelle“), die Überlagerung der Wirtschaftlichkeitsseite fragt `BerichtCtrl.KapitelstellenDerVorlage`
   (10.2), `AnhangECheckliste.Punkte(lage, stellen)` baut die Spalte; Blatt und Zelle in Excel folgen mit BV-E8.
4. **Formelmappe** unverändert; **Warnungen** in Laufmeldung und `bericht.warnungen` (heute BS:176-180);
   **Konfiguration** `B_*` unverändert gelesen.
5. **BW:678 und `SetzeUpdateFields` vorab (BV-E0, erledigt):** `k.Body.Append(t)` wird `k.Fuege(t)`;
   `w:updateFields` kommt über `AddChild` an die Schemastelle der Einstellungen.
6. **Bekannte Mängel** nur, wo eine Etappe sie berührt, im Protokoll: feste Datumsmuster, „Stamm — Stamm“,
   Δ%-Format (EBG:349), Freeze (EBG:1774), Datum und Menge als Text (EBG:235, :1764), nur erstes Gerät je
   Gewerk (`ProjektDetails.cs:80`).


## 12 Tests und Nachweis

| Bereich | Nachweis |
|---|---|
| Vorlagenweg | Test mit echter Vorlage und `OpenXmlValidator` (Office 2007–2021), vor der Behebung von BW:678 rot; Strukturmesslatte bleibt Wache |
| Auslieferung, Werkzeug | Wache: jede mitgelieferte Vorlage liegt als Datei vor, steht in beiden Lieferwegen (`WindowsFormsApplication1.csproj`, MauiAsset in `EPOS.iOS/EPOS.iOS.csproj`) und wird über `Dienste.Pfade.Berichtsvorlagen` gefunden (BV-Q19 b); die Beispielvorlage aus `Werkzeuge/Berichtsvorlage` besteht den Validator (BV-E0) |
| Katalog | Schlüssel eindeutig und nach Muster; Ressourcen- und Excel-Namen kollisionsfrei; handgepflegte Beschreibungen und Muster in beiden `.resx`; Aliasse lebendig; jede Kennzahl und Wirtschaftlichkeitszeile mit Eintrag, Musterschlüssel gegen die Konstanten; reservierte Namen decken die Formelmappe; eingefrorene Schlüssellisten; Engine-Texte zweisprachig |
| Deckung je Ausgabe | jeder Schlüssel mit Ausgabe Word in der Word-Standardvorlage (direkt oder über `Deckt`), ebenso Excel; vorgemerkte Einträge ausgenommen |
| Rundlauf | „Der Baukasten füllt ohne Prüferfehler; kein `{{` bleibt übrig“; Validator grün; jede Bildstelle mit SVG und PNG (`WordBerichtSvgWacheTests.cs:205`); Kurzbericht mit 1030 |
| Schmutzige Vorlagen | unter `EPOS.Kern.Tests/Proben/Berichtsvorlagen/` (Hauskonvention, `RepositoryOrdnungWacheTests.cs:85-91`): zerlegte Runs, Platzhalter im fetten Wort, Textfelder, Kopfzeile nur Seite 1, Hyperlink, nachverfolgte Änderungen, Vorlage aus deutschem Word 365, XML der Tippprobe (Windows, Mac, iPad, LibreOffice), `.dotx`, `.docm`, Bild in der Kopfzeile, Kapitel im Block-SDT, SDT mit `w:dataBinding`/`w:showingPlcHdr`, Kommentare, Excel mit Standardschrift Aptos |
| Varianten, Bestand | 0, 1, 3, 7 Varianten und Paarsicht; verschachtelte Blöcke; Leerfälle; `BerichtBlattstrukturWacheTests`, `WordBerichtSvgWacheTests`, Formelmappentests, `FormelmappeClosedXmlBefundTests` auf dem Vorlagenweg; Probe P3 als Test |
| Excel | Paketvergleich je Testvorlage; Standardvorlage mit `B_WIRTSCHAFT` aus, leerem Verlauf, null Varianten; Diagramme (BV-E8): die über das SDK angelegten Diagramme der Standardmappe bestehen den Validator, die Diagramme einer Testvorlage auf `EPOS_<name>` und `EPOS.reihe.*` zeigen nach dem Füllen die neuen Bereiche |
| Oberfläche | bunit (aus = kein DOM, beide Wurzelarten, Kopieren über Prüfadapter und ohne Adapter, ausgegraute Häkchen, gesperrter Eintrag, Wahl des Vorlagenordners samt benannter Ablehnung, wo die Plattform keine Ordnerwahl hat); Abdeckungswache; „Anzeigewert = Katalogwert“ für 1030; `StilblattTests`, `SchliesskreuzWacheTests`, `KnopfleistenWacheTests`, `KiMaskenabdeckungWacheTests`; Browserprobe nach `Proben/Rasterprobe` (Trefferflächen bei 1280 px, zehn Varianten, iPad hoch); **Rasterprobe** selbst, weil Marken an `.epos-raster`-Tabellen ansetzen |
| Produktdaten | Kurzbericht und Baukasten neutral, geprüft wie `WikiProduktdatenWacheTests` |
| Schreiber ohne Datenbank | nach dem Sammler entstehen Word (bisheriger Weg und Vorlagenweg) und Excel mit werfendem `IDatenzugriff` und Datenbankpfad ins Leere: 0 Zugriffe, nichts nachgeholt (`BerichtWertesatzTests`, BV-E3); Word gegen Excel an der Kennzahltafel, die Proben mit und ohne Sammler gegen die eingefrorenen Messlatten; dauerhaft hält es die Wache `BerichtSchreiberOhneDatenbankWacheTests` am Programmtext der Schreiber (Bausteinordner, Anhang E, Tabellenbericht, Formelmappe, Verlaufsblatt, Vorlagenfüller): 28 Muster in drei Gruppen — Zugriffsschicht, Controller, Rechenwege mit Datenbank —, eine Positivliste mit genauer Anzahl und Grund, eine Gegenprobe je Muster; Regelzeile in `EPOS.Kern/CLAUDE.md`, Abschnitt „Bericht“ |
| ChartProben, Referenzlauf | ChartProben unberührt bis Bildgröße Stufe 2 (BV-E5); Referenzlauf GESAMT: PASS in BV-E3 — erbracht mit #528 (14 Projekte gegen R19, 4.610.207 Werte); nichts wird eingefroren |
| Gate | `kern.yml`; bei Hüllen Linux-Bau der Windows-Schale (`-p:EnableWindowsTargeting=true`); `designer_neu.py` nach neuen Ressourcen; `SqlDialektPruefer` nur bei neuem SQL (keines geplant) |
| Papiere | je Etappe Statuszeile in [Status_iOS_Migration.md](Status_iOS_Migration.md) und Protokollblock unter `Dokumentation/ueberholt/Protokolle/` (Merge → Gate → Statuszeile und Protokoll → Push); Indexzeile dieses Papiers in [../LIESMICH.md](../LIESMICH.md) |
| Wiki | neue Repo-Quelle `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki` mit Kopfkommentar, Abschnitt „Bericht“ in `Programm Dokumentation - Wirtschaftlichkeit.wiki`; Anker in `help_mapping` erhalten (`HelpMappingAnkerWacheTests.cs:35-65`); „Upload ausstehend“ in der Statusdatei; Logbuch nur für sichtbare Änderungen (BV-E1, E2, E4 bis E7), ein Satz je Version ([Konzept_Hilfesystem_Wikidokumentation.md](Konzept_Hilfesystem_Wikidokumentation.md), 13.3, 13.4) |


## 13 Etappen

Aufwand geschätzt in Personentagen; Grundlage ist der Technikbefund (Word 15–18 PT, Excel 6–10 PT) zuzüglich
Katalog, Oberfläche, Migration und Tests. BV-E6 kann ab BV-E2 parallel laufen; BV-E4 setzt E2 und E3 voraus,
BV-E7 setzt E3 voraus. BV-E0 (#500), BV-E1 (#512), BV-E2 (#520), BV-E3 (#528), BV-E4 (#532), BV-E5 (#541) und BV-E6
(#544) sind umgesetzt (Entscheide in Abschnitt 14, BV-E1-1 in 10.3, BV-E2-1 in 6.5); nach diesem Plan folgt BV-E7
(Excel-Rahmen), vom Anwender bereits angeordnet.

| Etappe | Ziel | Inhalt | Abnahme | Aufwand |
|---|---|---|---|---|
| **BV-E0 Grundlagen, Messlatte, Messproben, Beispielvorlage** | Fehlerquellen schließen, Maßstab festlegen, Vorlage zeigen | BW:678 beheben; Test mit echter Vorlage; Strukturmesslatte; doppelte Stile bereinigen; Ad-hoc-DDL und veraltete Kommentare (10.3); Laufzeit heute messen. Beispielvorlage aus dem bisherigen Bericht über `Werkzeuge/Berichtsvorlage` (6.3, Anhang B.3). Messproben als Tests: Excel-Namen mit Punkt, ClosedXML-Rundlauf (Diagramm samt Reihenbezügen auf einer wachsenden Tabelle, Tabelle, Name, berechnete Spalten), Excel-Diagramm über das OpenXML SDK anlegen (BV-Q11), `.xltx`, `<v>` einer in Excel gespeicherten Vorlagenformel, SDT und Alternativtext, Vorlage aus deutschem Word 365, Tippprobe. Geräteprobe iPad notieren (BV-Q16) | Gate grün; Validator grün mit Vorlage; Beispielvorlage: Validator grün, Anwender hat sie in Word gesehen; Messbefunde im Protokoll; Mockup angenommen (BV-Q9 c, 25.09.2026); kein Logbucheintrag | 4–5 — **umgesetzt 25.09.2026 (#500)**, Protokoll `../ueberholt/Protokolle/Bericht/BV_E0_Grundlagen_Protokoll.md`; offen: Tippprobe, echtes Word 365, iPad |
| **BV-E1 Vorlagenwahl und Textplatzhalter** | eigenes Deckblatt, Kopf- und Fußzeile; übriger Bericht wie heute | Katalog v1 (`bericht.*`, `text.*`, `ersteller.*` mit `ersteller.programm` und `ersteller.version`, `projekt.*`, `stamm.kennzahl.*`, `kennzahl.*`, `bericht.inhalt`); Engine mit Normalisierer, allen Teilen, Einfügeanker, Rollenauflösung, Inhaltsbreite, Kommentarentfernung; Standardvorlage `Berichtsvorlage_Standard.docx` aus der Beispielvorlage in der Stufe mit `{{bericht.inhalt}}` in beiden Lieferwegen, die Stilvorlage bleibt (6.3), `IPfade.Berichtsvorlagen` in `StandardPfade` und `IosPfade` statt `AppDomain.BaseDirectory` in `FindeVorlage` (8.4); kein Übernahmeschritt und kein `[InstallDelete]` (Entscheid BV-E1-1, 10.3); `BerichtsvorlagenCtrl`, `BerichtsvorlagenGaben`, `Berichtsvorlagenwege`; Vorlagenordner wählbar mit Vorgabe (Einstellung `BerichtVorlagenordner`, 10.3), „Neue Vorlage…“, „Hinzufügen…“, Vorgabe; Vorlagengruppe, Prüfzeile, `BerichtSeiteVorlagentexte`; Prüfer beider Stufen, Vorprüfung, erweiterte Rückfrage; Abschnitt „Bericht“ im Einstellungsdialog mit „Firma“ und „Vorlagenordner“; iOS-Dateifilter `.dotx`; KI-Anmeldung samt Eingabebilanz (`KiMaskenabdeckungWacheTests.cs:131, :168`) | Messlatte, Validator, schmutzige Vorlagen (auch deutsches Word) grün; Wache der Auslieferungsdateien grün (12); Katalogwachen und `designer_neu.py` prüfend grün; bunit grün; Schale baut auf Linux; Setup-Lauf nach Rückfrage (Standardvorlage in `{app}\Vorlagen`); Anwenderprobe mit `{{ersteller.firma}}` und gewähltem Vorlagenordner (das Logo der Kopfzeile folgt mit BV-E2); Änderungen an `EPOS.iOS/` (MauiAsset, `IosPfade.Berichtsvorlagen`, Dateifilter) im Protokoll | 6–8 — **umgesetzt 26.09.2026 (#512)**, Protokoll `../ueberholt/Protokolle/Bericht/BV_E1_Vorlagenwahl_Protokoll.md`; offen: Anwenderprobe, Setup-Lauf, iOS-Lauf, Wiki-Upload, Tippprobe |
| **BV-E2 Kapitel und Häkchen** | Vorlage bestimmt Reihenfolge und Umfang | `kapitel.*` einzeln, Standardvorlage im vollen Aufbau der Beispielvorlage (Anhang B.3); `\|ohne titel`, `\|ebene 2`, Entfall des Kapitelkopfs; Häkchen mit ausgegrauten Einträgen; Abweichung je Stammprojekt; `custom.xml` mit Fassung; `Deckt`; Bausteintitel nach MyResource; zweiter Einstieg; Stelle in der Anhang-E-Überlagerung; Logo der Kopfzeile als Bildplatzhalter mit der Einstellung `BerichtLogo` (BV-Q8, Entscheid BV-E2-1, 6.5) | Word-Wachen grün auf dem Vorlagenweg; Deckungswache Kapitel; bunit Häkchenliste; Linux-Bau der Schale; Anwendervorlage mit umgestellter Folge und abgewähltem Häkchen ohne verwaiste Überschrift | 3–4 — **umgesetzt 26.09.2026 (#520)**, Protokoll `../ueberholt/Protokolle/Bericht/BV_E2_Kapitel_Protokoll.md`; offen: Anwenderprobe, Regel „Deckblatt aus Platzhaltern“ schärfen, Setup- und iOS-Lauf nach Rückfrage, Wiki-Upload |
| **BV-E3 Reiner Wertesatz** | Auflösen ohne Datenbank, gleiche Zahlen in Word und Excel | Zugriffe und Nebenrechnungen nach `BerichtsDaten.Wirtschaft` über dieselben Rechenwege; beste Variante in den Kern; `Bedarf` steuert Zeitreihen, Verlauf, Emissionsbilanz | Messlatte unverändert; Wirtschaftlichkeits- und Anhang-E-Tests grün; Test mit werfendem `IDatenzugriff` beim Füllen; Word gegen Excel für alle `stand.wirtschaft.*`; Probe gegen heutigen Bausteinweg; Referenzlauf GESAMT: PASS; kein Logbucheintrag | 3–5 — **umgesetzt 26.09.2026 (#528)**, Protokoll `../ueberholt/Protokolle/Bericht/BV_E3_Wertesatz_Protokoll.md`; Word gegen Excel geprüft an der Kennzahltafel (die Schlüssel `stand.wirtschaft.*` kommen mit BV-E4); die offenen Punkte (Speichertemperaturbild, Leitversion, Stammfall von `wirtschaft.beste.*`, Bedarf der `wirtschaft.*`-Schlüssel) sind mit BV-E4 (#532) entschieden und gebaut |
| **BV-E4 Blöcke, Schalter, Standwerte** | Werte je Variante und bedingte Abschnitte | `je stand`, `je variante`, `je gebaeude`, `wenn`, `hat.*`, Standschalter (4.11); Zeilenwiederholung, SDT-Wiederholabschnitt mit Auspacken; `stand.*`-Werte, `stand.a/b`, `vergleich.*`, `wirtschaft.*` samt `beste`, Parametern, Szenarien; Warnlisten und Gültigkeitsregel | 0, 1, 3, 7 Varianten und Paarsicht; verschachtelte Blöcke; Leerfälle; Prüfer meldet offene Blöcke und Kontextverstöße | 4–6 — **umgesetzt 26.09.2026 (#532)**, Protokoll `../ueberholt/Protokolle/Bericht/BV_E4_Bloecke_Protokoll.md`; Katalogfassung 3, Blöcke in Absätzen, Musterzeilen und SDT, `\|block n`; Schalter je Tabelle und Bild sowie Standardvorlage auf der aktuellen Fassung mit BV-E5 (#541) gebaut; offen: Anwenderproben unter Windows, Kapitel im Wiederholblock, `hat.*` im Gruppenblock |
| **BV-E5 Tabellen und Bilder** | Kurzberichte ohne ganze Kapitel | `Berichtstabelle`, Strukturtabellen mit Tabellenformatvorlage und Mustertabelle, Breite in Prozent; Bildplatzhalter, Bildgröße Stufe 2; vollständiger Baukasten; Kurzbericht je Sprache als Datei in beiden Lieferwegen | Validator und SVG-Wache grün; ChartProben mit neuen Größen grün; Kurzbericht mit 1030; Leistungstest 60 Seiten innerhalb „heute + 10 %“ | 5–7 — **umgesetzt 26.09.2026 (#541)**, Protokoll `../ueberholt/Protokolle/Bericht/BV_E5_Tabellen_Bilder_Protokoll.md`; Katalogfassung 4 (37 Tabellen, 16 Bilder, `hat.tabelle.*`, `hat.bild.*`), Standardvorlage auf Fassung 4, Kurzbericht de/en, Baukasten; Leistung: Verhältnis Vorlagenweg zu Bausteinweg 1,040 (7 Stände, 31 Tabellen, 54 Bilder, rund 60 Seiten); ChartProben 218 Bilder, Messlatte 183 Hashes; offen: Anwenderprobe, iOS- und Setup-Lauf nach Rückfrage, Excel-Seite (BV-E7/E8), Wiki-Upload |
| **BV-E6 Kennzeichnung in der App** | Platzhalter am Ort erkennen | `Vorlagenfeldknopf`, `Vorlagenfeldansicht`, `IZwischenablage` mit Adaptern, Umschalter (9.4), Katalog mit „In der App zeigen“, Parameter an den Bausteinen, `Vorlagenfeldorte`, Ressourcen; nur Schlüssel mit `Seit` ≤ Fassung | bunit, Abdeckungswache, „Anzeigewert = Katalogwert“, Stilblatt- und Schließkreuz-Wache grün; Browserprobe 1280 px, zehn Varianten, iPad; Rasterprobe grün (Zeilenhöhe in allen Stellungen gleich); `EPOS.iOS/` benannt, iOS-Lauf nach Rückfrage; Anwenderabnahme | 5–7 — **umgesetzt 26.09.2026 (#544)**, Protokoll `../ueberholt/Protokolle/Bericht/BV_E6_Kennzeichnung_Protokoll.md`; 53 Orte in `Vorlagenfeldorte`, Abdeckungs-, Orts- und Anzeigewertwache, Markenprobe (zehn Varianten und die echten Wirte, drei Stellungen, 1.280 px) und Rasterprobe grün; offen: Anwenderabnahme unter Windows und auf dem iPad, iOS-Lauf nach Rückfrage, Angleichung Deckungsgrad und JAZ Kälte, `bild.ergebnis.*` (Fassung 5), Wiki-Upload |
| **BV-E7 Excel-Rahmen** | Excel-Mappe aus einer Vorlage | `ExcelVorlagenfueller`, Standard-`.xlsx` im Code, Blattmarken mit Entfall-Regel, Zellplatzhalter, Namen, reservierte Namen, Paketvergleich, ausdrückliche Schrift, `FullCalculationOnLoad` bei Vorlagenformeln, Zeile „Excel-Vorlage“, Dateifilter `.xltx` | ohne Vorlage alle Excel-Wachen unverändert grün (`BerichtBlattstrukturWacheTests.cs:191, :217`); mit Vorlage die Formelmappentests; Aptos-Vorlage | 4–6 |
| **BV-E8 Excel-Listen, Detailblatt, Diagramme** | Stände, Listen und Diagramme in Excel | Excel-Tabellen aus listentauglichen Tabellen, erzeugte Bereiche, Musterblatt `blatt.detail`; Excel-Diagramme aus den Zahlen der Tabellen (BV-Q11, 7.4): Diagramme der Anwendervorlage auf `EPOS_<name>`-Tabellen, festen Rastern oder Namen `EPOS.reihe.*`, die EPOS nachführt, Diagramme der erzeugten Blätter der Standardmappe über das OpenXML SDK; Anhang-E-Stelle als Blatt und Zelle; nur nach grünen Messproben | Diagramme der Probemappe zeigen in Excel die neuen Werte; erzeugte Blätter mit ihren Diagrammen, Validator grün; Detailblätter an der Markenstelle in Standfolge | 4–6 |
| **BV-E9 Ausbau auf Zuruf, Abschluss** | Einzelposten nach nachgewiesenem Bedarf | `.dotx` mit Schnellbausteinen, Ergebnisnamen, Sprache des Laufs aus der Vorlage (BV-Q7 b), Positionsadressierung (BV-Q10 b); zum Abschluss `git mv` dieses Papiers nach `ueberholt/` | je Posten benannter Test und Anwenderprobe; Gate grün | je Posten |


## 14 Entscheidfragen

**Entscheid des Anwenders vom 25.09.2026:** alle Fragen nach Empfehlung, mit fünf Änderungen (BV-Q8, Q11, Q12,
Q15, Q19); die Spalte „Entscheid 25.09.2026“ nennt die gewählte Lesart, bei den fünf mit dem Wortlaut. Der
Entscheid im Wortlaut:

> 14 Entscheidfragen BV-Q1-BV-Q19: alle umsetzen, mit folgenden Änderungen: BV-Q19: Vorlage nicht im Kern, liegt
> extern vor. Erstelle eine Vorlage aus dem bisherigen Bericht als Beispiel. BV-Q15: Vorlagenverzeichnis extern,
> wählbar mit default. BV-Q12: Probefüllung nicht sinnvoll. BV-Q11: Grafiken in excel generierten Grafiken aus
> Zahlen in Tabellen. BV-Q8: auch name und versionsnummer EPOS-Plan. Starte BV-E0.

| Nr. | Frage | Lesarten | Empfehlung | Entscheid 25.09.2026 |
|---|---|---|---|---|
| **BV-Q1** | Bausteinhäkchen | (a) feste Schalter für alle Kapitel; (b) entfallen, die Vorlage bestimmt den Inhalt allein; (c) Schalter für Kapitelplatzhalter und `baustein.*`, Kapitel ohne Platz in der Vorlage ausgegraut | **(c):** mit der Standardvorlage wie heute, Tests bleiben gültig, eigene Vorlagen verwirren nicht | **(c)** |
| **BV-Q2** | Häkchen in Excel | (a) wie heute; (b) wie in Word; (c) wirken auf die Blätter der Excel-Vorlage | **(a)** bis BV-E7, danach (c) | **(a)** bis BV-E7, danach (c) |
| **BV-Q3** | Syntax in Word | (a) `{{…}}` und Inhaltssteuerelemente gleichrangig; (b) nur Inhaltssteuerelemente; (c) nur `{{…}}`. Teilfrage: Syntaxwörter in beiden Sprachen deutsch? | **(a)**; Teilfrage ja | **(a)**; Teilfrage ja |
| **BV-Q4** | Ablage | (a) Vorlagenordner in den Dokumenten, Kopie mit Herkunft; (b) BLOB in der Datenbank; (c) Datenordner neben `Kenndaten.sqlite` | **(a):** sichtbar, am Ort pflegbar, auf iOS ohne Bookmarks | **(a)**; der Ordner ist wählbar (BV-Q15) |
| **BV-Q5** | Geltung der Wahl | (a) je Anwender; (b) je Stammprojekt; (c) Vorgabe je Anwender, Abweichung je Stammprojekt | **(c)**; Duplizieren und Transfer nehmen sie nicht mit | **(c)** |
| **BV-Q6** | Fehlerhafte Vorlage | (a) Abbruch; (b) stiller Rückfall; (c) Vorprüfung vor der Simulation, eine Rückfrage mit drei Wegen | **(c)** | **(c)** |
| **BV-Q7** | Sprache | (a) Oberflächensprache; Standardvorlage sprachneutral, Kurzbericht je Sprache, Rückfrage vor der Simulation bei abweichender Vorlage; (b) die Vorlage bestimmt die Sprache des Laufs (Übersteuerung von `BerichtTexte.Englisch` je Lauf); (c) nur sprachneutrale Vorlagen | **(a)**; (b) auf Zuruf in BV-E9 | **(a)**; (b) auf Zuruf in BV-E9 |
| **BV-Q8** | „Ersteller“ | (a) `projekt.bearbeiter`; (b) Lizenznehmer; (c) `ersteller.firma` als Einstellung aus der Lizenz, `projekt.bearbeiter` bleibt die Person. Teilfrage: Fußzeile der Standardvorlage „INEKON GmbH“ → `{{ersteller.firma}}`, DATE → `{{bericht.datum}}`? | **(c)**; Teilfrage ja | **(c) mit Programmname und Versionsnummer** („auch name und versionsnummer EPOS-Plan“): neu `ersteller.programm` („EPOS-Plan“) und `ersteller.version` (Produktfassung), `bericht.programmversion` bleibt Alias; Teilfrage ja; **Logo: Bildplatzhalter, BV-E2-1 (b)** (Anwenderentscheid 26.09.2026: `{{bild.ersteller.logo}}` aus der Einstellung `BerichtLogo`, verworfen (a) Entfall; 6.5) |
| **BV-Q9** | Kennzeichnung in der App | (a) Randmarke immer sichtbar; (b) Marke nur beim Überfahren am Rand; (c) Platzhalteranzeige mit drei Stellungen (Aus, Marken mit Mouse-over, Schlüssel) plus Katalog; (d) nur Katalog. Teilfrage: Umschalter je Bildschirm in der Kopfzeile, auch ausgeschaltet sichtbar (Mockup), oder nur in Vorlagengruppe und Katalog? | **(c)**; Teilfrage: Kopfzeile — ausschalten, wo man ist, um den Preis eines ruhigen Symbols | **(c)**; Teilfrage: Umschalter in der Kopfzeile; Mockup angenommen (9.8) |
| **BV-Q10** | Stände außerhalb von Blöcken | (a) nur `stamm.*`, Gruppe, `stand.a/b` (`stand.b` in Sicht 1 bei genau einer Variante); (b) nach Position `variante1…n`; (c) nach Projekt-ID | **(a)**; (b) nur bei nachgewiesenem Bedarf | **(a)**; (b) nur bei nachgewiesenem Bedarf (BV-E9) |
| **BV-Q11** | Excel-Umfang | (a) Rahmen, Blattmarken, Namen; (b) zusätzlich Excel-Tabellen, Detail-Musterblatt, eigene Diagramme; (c) zusätzlich PNG-Bilder | **(a)** in BV-E7, (b) in BV-E8, (c) auf Zuruf | **Excel-Diagramme aus den Zahlen der Tabellen** („Grafiken in excel generierten Grafiken aus Zahlen in Tabellen“): (a) in BV-E7; (b) in BV-E8 mit Diagrammen der Anwendervorlage auf `EPOS_<name>`-Tabellen und `EPOS.reihe.*` sowie Diagrammen der erzeugten Blätter über das OpenXML SDK (7.4); PNG-Bilder (c) entfallen |
| **BV-Q12** | Probefüllung ohne Simulation | (a) Muster über „Erstellen“; (b) keine; (c) „Vorlage testen“ in der Prüfliste: Beispielwerte aus `BeispielId`, Kapitel als graue Kästen, Wasserzeichen „Probe – kein Bericht“, eigener Dateiname, nie über „Erstellen“ | **(c)** ab BV-E1 für Text, ab BV-E5 für Tabellen und Bilder; es entsteht kein Bericht, jeder Bericht rechnet weiter neu | **(b)** („Probefüllung nicht sinnvoll“): keine Probefüllung, kein „Vorlage testen“ |
| **BV-Q13** | Edition | (a) eigene Vorlagen für alle Lizenztypen; (b) nur „firma“; (c) demo nur Standard | **(a)** | **(a)** |
| **BV-Q14** | Zustand nach dem Füllen | (a) SDT auspacken, Schlüssel entfernen; (b) SDT behalten und sperren; (c) auspacken, Schlüssel in `docPr/@title` oder Textmarke behalten (erneut füllbar) | **(a):** sauberer Kundenbericht, Word überschreibt nichts | **(a)** |
| **BV-Q15** | Gemeinsame Vorlagen eines Büros | (a) je Anwender, Weitergabe per Datei; (b) zusätzlich ein nur lesender Teamordner `BerichtVorlagenTeamordner` (nur Windows); (c) Vorlagenpaket exportieren und importieren | **(b)** unter Windows, **(a)** auf iOS | **Vorlagenordner extern, wählbar mit Vorgabe** („Vorlagenverzeichnis extern, wählbar mit default“): Einstellung `BerichtVorlagenordner`, Vorgabe Dokumente + `EPOS-Plan/Berichtsvorlagen`; unter Windows jeder Ordner, auch ein gemeinsamer des Büros; auf iOS fest die Sandbox; kein eigener Teamordner (10.3) |
| **BV-Q16** | Pflegeweg auf dem iPad | (a) Pflege nur unter Windows; (b) Rundweg „Teilen…“ und „Ersetzen…“; (c) Bearbeiten am Ort über die App „Dateien“ | **(c)** nach Geräteprobe (iU13), sonst (b) | **(c)** nach Geräteprobe (iU13), sonst (b) |
| **BV-Q17** | Gestaltung der Diagramme | (a) wie heute: Farbrollen der Einstellungen, feste Schrift; (b) Palette aus den Designfarben der Vorlage; (c) Berichtspalette je Vorlage in `custom.xml` | **(a)**; ChartProben bleiben unberührt | **(a)** |
| **BV-Q18** | Zwischenablage der Marken | (a) Naht `IZwischenablage` in EPOS.UI mit Windows- und iOS-Adapter; (b) rein clientseitiger JS-Klickhandler, Messung am iPad | **(a):** hausgemäß, auf dem iPad verlässlich; iOS-Änderung benannt | **(a)** |
| **BV-Q19** | Ort der Standardvorlagen | (a) eingebettete Ressourcen im Kern; (b) wie heute Datei neben der EXE bzw. MauiAsset, ergänzt um Excel | **(a):** plattformfrei, testbar; kehrt `EPOS.Kern/CLAUDE.md:34` bewusst um | **(b) extern** („Vorlage nicht im Kern, liegt extern vor“): Dateien der Auslieferung wie heute, Ort über `IPfade.Berichtsvorlagen`, die Standard-Excel-Mappe bleibt im Code (7.1); dazu die Beispielvorlage aus dem bisherigen Bericht („Erstelle eine Vorlage aus dem bisherigen Bericht als Beispiel“), ab BV-E1 Standardvorlage (6.3, 8.4, Anhang B.3) |


**Entscheide des Anwenders zu BV-E4 vom 26.09.2026** (gebaut mit #532):

| Nr. | Entscheid |
|---|---|
| **BV-E4-1** | Die Leitversion der Zahlungsgliederung (Differenzspalte, Brückenbild, „Was daraus im Lauf wird“) folgt der Regel der besten Variante (`BesteVariante.Waehle`, 9.5); eine Gruppe nur mit dem Stamm hat den Stamm als Leitversion. |
| **BV-E4-2** | Der Variantenvergleich führt `vergleich.minimum.<k>` und `vergleich.maximum.<k>` neutral neben `vergleich.spanne.<k>` statt eines Bestwerts; `vergleich.beste_variante` ist Alias von `wirtschaft.beste.anzeige`. |
| **BV-E4-3** | Die Parameter `wirtschaft.parameter.*` stehen in Prozent. |
| **BV-E4-4** | Die Standardvorlage bleibt bei Katalogfassung 2 bis BV-E5. |

Dazu entschieden: Das Speichertemperaturbild der Projektbeschreibung bleibt eine Beigabe, `kapitel.projekt` trägt keinen
Bedarf (5.1); der Stammfall von `wirtschaft.beste.*` ist mit `beste.ist_stamm` und dem Kachelwert `beste.kapitalwert`
gelöst (9.5).

**Entscheide des Anwenders zu BV-E5 vom 26.09.2026** (gebaut mit #541):

| Nr. | Entscheid |
|---|---|
| **BV-E5-1** | Ist ein Bildrahmen schmaler als die Mindestbreite des Bildes, zeichnet das Modell in der Mindestbreite und wird verkleinert (Stufe 1), mit Hinweis (6.5). |
| **BV-E5-2** | Tabellen und Bilder haben keine Paarzwillinge `stand.a/b.*` (5.4). |
| **BV-E5-3** | `Deckt` der Kapitel führt die Tabellen und Bilder, die ihr Baustein schreibt, samt `hat.tabelle.*`/`hat.bild.*`; `tabelle.varianten` gehört zu „Komponenten & Varianten“ (5.3). |
| **BV-E5-4** | Die Paarsicht steht im Baukasten nur als Muster (6.3). |
| **BV-E5-5** | Auf iOS teilt die App den gespeicherten Baukasten (6.3, 10.2). |

Dazu festgelegt: „EPOS Tabelle“ wird nur mit einer Mustertabelle angelegt (6.4); `hat.tabelle.*` außerhalb eines Blocks
bedeutet „irgendein Stand hat die Tabelle“ (5.4); die Beispielvorlage bleibt vorerst (6.3).


## 15 Risiken und offene Punkte

### 15.1 Risiken

| Risiko | Gegenmittel |
|---|---|
| Normalisierer übersieht Randfälle; Vorlagen aus deutschem Word | Prüfer vor jeder Rechnung, schmutzige Vorlagen, SDT als robuste Form, Rollenauflösung über `w:name`; ein Platzhalter bleibt lieber stehen, als dass Text verloren geht |
| Standardweg verändert sich; umbenannte Schlüssel brechen Vorlagen | Messlatte vor jedem Umbau, Wachen auf dem Vorlagenweg; Aliasse, eingefrorene Schlüssellisten |
| Marke verspricht einen anderen Wert; Einzelwerte ohne Gültigkeitshinweise | Regel „genau der angezeigte Wert“ mit Wache; Warnlisten, Prüferregel, Warnungen immer in der Laufmeldung |
| Bildrahmen und Modell passen nicht; ClosedXML verliert Teile und legt keine Diagramme an | einpassen, Warnung unter 80 %, Stufe 2; Paketvergleich, `.xlsm` gesperrt, Raster, Tabellen oder Namen, Diagramme über das OpenXML SDK nach Messprobe (BV-E0), Schrift ausdrücklich |
| iOS: Bericht nie gelaufen, AOT, Zwischenablage | kein Paket mit Reflection oder Ausdrücken; iOS-Probe nach Rückfrage; Adapter mit Rückfall |
| Fremde Vorlagen; veraltete Kopien; gemeinsamer Vorlagenordner | Makros ablehnen, Verknüpfungen entfernen, Grenze auf unkomprimierte Größe; Herkunft und Prüfsumme; ein nicht erreichbarer Ordner wie eine fehlende Datei (10.3) |
| Marken stören; halbfertige Stände über `GitHub_Sync.bat` | Anzeige aus als Vorgabe, eine Marke je Tabelle, Browser- und Rasterprobe; jede Etappe für sich nutzbar |

### 15.2 Offene Punkte

Die Entscheide BV-Q1 bis BV-Q19 sind am 25.09.2026 gefallen (Abschnitt 14); BV-E0 (#500), BV-E1 (#512), BV-E2 (#520),
BV-E3 (#528), BV-E4 (#532), BV-E5 (#541) und BV-E6 (#544) sind umgesetzt, für BV-E1 gilt der Entscheid BV-E1-1 (10.3), für BV-E2 der
Anwenderentscheid BV-E2-1 (Logo als Bildplatzhalter, 6.5), für BV-E4 die Anwenderentscheide BV-E4-1 bis BV-E4-4, für BV-E5
die Anwenderentscheide BV-E5-1 bis BV-E5-5 (Abschnitt 14). Offen sind aus
BV-E0 die Tippprobe, der Nachweis mit echtem Word 365 und Excel und die Geräteprobe iPad (iU13, BV-Q16); aus BV-E1 die
Anwenderprobe unter Windows, der Setup- und der iOS-Lauf (je nach Rückfrage), der Wiki-Upload der Seite
„Berichtsvorlagen“, die Zeile „Original geändert – übernehmen?“, eine Bedienung der Vorgabe (`BerichtVorlageWord`), die
Anmeldung der Katalogsuche beim Assistenten und der Abgleich der Kern-Vorprüfung für Vorlagen ohne Platzhalter; aus BV-E2
die Anwenderprobe (Häkchen, Deckblatt aus der Vorlage, Kapitelfolge, Logo, Stelle in der Anhang-E-Überlagerung), die
Schärfung der Regel „Deckblatt aus Platzhaltern“ — sie greift schon bei einer einzelnen Deckblattangabe im Rumpf, etwa nur
`{{bericht.datum}}` (5.3) —, die Bezugszeile der Überlagerung bei unlesbarer eigener Vorlage (sie nennt die eigene, die
Stellen stammen aus der Standardvorlage), die in der Hülle ungenutzte Logo-Prüfung des Kerns
(`BerichtsvorlagenCtrl.LogoVorhanden()`); aus BV-E3 ein Nachholen im Wertesatz, das die
Laufmeldung nicht nennt; aus BV-E4 die Anwenderproben mit Blockvorlagen (0, 1, 3, 7 Varianten, Paarsicht, Gebäude), ein
Kapitel in einem Wiederholblock (nur die erste Wiederholung füllt, der Prüfer meldet es noch nicht), `hat.*` im
Gruppenblock `|block n` (wertet über den ganzen Bericht), die neuen Schlüssel in Excel (BV-E7/E8, Parameter dort als
Anteil); aus BV-E5 die Anwenderprobe unter Windows (Kurzbericht, Baukasten, Tabellen mit Mustertabelle, Bilder in
schmalen Rahmen), der iOS-Lauf (Kurzbericht als MauiAsset) und der Setup-Lauf (zwei neue Dateien in `{app}\Vorlagen`),
je nach Rückfrage, die Excel-Seite der Tabellen und Bilder (listentaugliche Tabellen als Excel-Tabellen, Bilder als
Excel-Diagramme, die drei Tabellen mit reiner Excel-Quelle, Excel-Baukasten; BV-E7/E8) und der Wiki-Upload der Seite
„Berichtsvorlagen“; die Beispielvorlage bleibt vorerst neben dem Kurzbericht; aus BV-E6 die Anwenderabnahme unter
Windows (1.280 px, zehn Varianten) und auf dem iPad, der iOS-Lauf nach Rückfrage (`IosZwischenablage`, `MauiProgram`),
die Angleichung von Deckungsgrad und JAZ Kälte zwischen Dashboard und Katalog (eigener Auftrag mit Referenzlauf),
`bild.ergebnis.*` für die Erzeugerbilder (Fassung 5) und Schlüssel für die Kacheln der Autarkieanalyse und des
Speicherlaufs, „Katalog…“ der leisen Zeile mit Baukasten und der Wiki-Upload. Einzelheiten stehen in den
Protokollen `../ueberholt/Protokolle/Bericht/BV_E1_Vorlagenwahl_Protokoll.md`,
`../ueberholt/Protokolle/Bericht/BV_E2_Kapitel_Protokoll.md`, `../ueberholt/Protokolle/Bericht/BV_E3_Wertesatz_Protokoll.md`,
`../ueberholt/Protokolle/Bericht/BV_E4_Bloecke_Protokoll.md` (je Abschnitt 8) und
`../ueberholt/Protokolle/Bericht/BV_E5_Tabellen_Bilder_Protokoll.md` (Abschnitt 10),
`../ueberholt/Protokolle/Bericht/BV_E6_Kennzeichnung_Protokoll.md` (Abschnitt 7) und unter „Nach #512“, „Nach #520“,
„Nach #528“, „Nach #532“, „Nach #541“ und „Nach #544“ in der Statusdatei. Nach dem Plan (Abschnitt 13) folgt BV-E7
(Excel-Rahmen), vom Anwender bereits angeordnet.

### 15.3 Verworfene Lösungen mit Grund

| Lösung | Grund |
|---|---|
| Vorlagen als BLOB in der Datenbank | Ausgeben und Neu-Einlesen macht still veraltete Fassungen wahrscheinlich; nicht am Ort pflegbar |
| neue Konfigurationstabelle mit Schemaschritt | Fremdschlüssel und `CASCADE` bestehen (`ProjektFremdschluessel.cs:205`) |
| „Rumpf leeren“ bei Vorlagen ohne Platzhalter | löscht still Inhalt des Anwenders |
| Ablage unter `%ProgramData%` | dort schreibt nur das Setup; die Datenbanksicherung nähme sie nicht mit |
| Standardvorlagen als eingebettete Ressourcen im Kern | Anwenderentscheid 25.09.2026, BV-Q19 (b): die Vorlagen liegen extern als Dateien der Auslieferung (6.3, 8.4) |
| Parameter `Platzhalter`, Code-Name „Marke“; Zuordnung über `DiagrammSvg.Kennung` | kollidieren mit `Textfeld.razor:68`, `DiagrammSvg`, `--epos-marke`; `Kennung` ist je Instanz, teils dynamisch (WS:1637) |
| Zeilenmarken nur bei Überfahren oder Fokus; dauerhafte Leiste „Platzhalter sichtbar … ✕“ | verletzt „Aktionsknöpfe einer Zeile immer sichtbar“; Banner nur für Zustände, die man beheben muss, ✕ ist Abbrechen |
| `CascadingValue` aus `AppWurzel`; Prüfzeile als `Kennzeichen` | erreicht Windows-Dialoge nicht; `Kennzeichen` ist das Schloss ohne Textknoten |
| eigener Annahme-Algorithmus für Änderungen | Umfang ohne Nutzen; fertige Fortführungen verlangen SkiaSharp 4.x |
| Spaltenwiederholung, `ReplaceData(T)` | höchstes Bruchrisiko; Reflection gegen die iOS-Begründung |
| Kürzel `\|d`, `\|D`, `max=3`; Positionsadressierung von Anfang an | .NET-Kürzel bzw. zweite Schreibweise; bricht bei Umordnung der Varianten still |

## 16 Verwandte Papiere

- [Konzept_Diagramme_Interaktiv_EPOS-Plan.md](Konzept_Diagramme_Interaktiv_EPOS-Plan.md) (Zeichenmodell, SVG im Bericht, Farbrollen), [Konzept_Knopfleisten_Administration_EPOS-Plan.md](Konzept_Knopfleisten_Administration_EPOS-Plan.md) (Fußleisten), [Konzept_Hilfesystem_Wikidokumentation.md](Konzept_Hilfesystem_Wikidokumentation.md) (Wiki, Logbuch).
- [Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md](Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md), [Konzept_KI-Assistent_Aufgabensteuerung.md](Konzept_KI-Assistent_Aufgabensteuerung.md) (KI-Sicht, Maskenanmeldung).
- [Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md](Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md) (Szenarien, Anhang E), [Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md](Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md) (Emissionsmodus).
- [Konzept_Projekttransfer_EPOS-Plan.md](Konzept_Projekttransfer_EPOS-Plan.md), [Konzept_Setup_InnoSetup_EPOS-Plan.md](Konzept_Setup_InnoSetup_EPOS-Plan.md), [EPOS-Plan_Konzept_Lizenzierung.md](EPOS-Plan_Konzept_Lizenzierung.md), [BETRIEB_SQLITE.md](BETRIEB_SQLITE.md).
- [Umsetzungskonzept_iOS_EPOS-Plan.md](Umsetzungskonzept_iOS_EPOS-Plan.md), [Umsetzung_iU10_Nachweise.md](Umsetzung_iU10_Nachweise.md), [Status_iOS_Migration.md](Status_iOS_Migration.md) (iU11, iU13, iR-g).
- Geschichte, keine Regelquelle: [../ueberholt/Protokolle/Reporting/Konzept_Berichtserstellung_EPOS-Plan.md](../ueberholt/Protokolle/Reporting/Konzept_Berichtserstellung_EPOS-Plan.md), [../ueberholt/Protokolle/Reporting/E14_Formelmappe_je_Szenario_Protokoll.md](../ueberholt/Protokolle/Reporting/E14_Formelmappe_je_Szenario_Protokoll.md), [../ueberholt/Protokolle/Bericht/DG_E3_Rollout_Protokoll.md](../ueberholt/Protokolle/Bericht/DG_E3_Rollout_Protokoll.md).
- Hausregeln und Prüfstände: [../../EPOS.UI/CLAUDE.md](../../EPOS.UI/CLAUDE.md), [../../EPOS.Kern/CLAUDE.md](../../EPOS.Kern/CLAUDE.md), [../../Proben/ChartProben/LIESMICH.md](../../Proben/ChartProben/LIESMICH.md), [../../Proben/Rasterprobe/LIESMICH.md](../../Proben/Rasterprobe/LIESMICH.md).


## Anhang A Platzhalterkatalog

Abkürzungen wie Abschnitt 2, dazu BK = `BerichtsKonfiguration.cs`, PD = `ProjektDetails.cs`, VE = `VerlaufExcel.cs`,
SEH = `SimulationErgebnisHuelle.Bilder.cs`. „entspricht“/„ähnlich“: Stufe der Marke; „–“: keine Marke; am Ende die Etappe.
Gebaut ist die Katalogfassung 4 (BV-E5): 2.078 Schlüssel und 3 Aliasse, eingefroren in
`EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v4.txt` (die Liste der Fassung 3 bleibt daneben unverändert); gegenüber
Fassung 3 kommen 37 Tabellen, `muster.tabelle`, 37 `hat.tabelle.*`, 16 Bilder und 16 `hat.bild.*` hinzu. Ein Leerwert ist nie 0 und trägt immer einen Grund, Großschreibung
wirkt als Alias über die Normierung des Katalogs.

| Schlüssel | Art | Quelle im Kern | Ort in der App | Bemerkung |
|---|---|---|---|---|
| `bericht.inhalt` | Kapitel (Sammelanker) | WBG:96-111 | Häkchen der Berichtsseite | angehakte Kapitel ohne die einzeln geführten; E1 |
| `bericht.titel`, `bericht.untertitel` | Text | `Stammprojektname` BD:18; BS:18 über `BerichtTexte.T` | Seitentitel | Untertitel in der Standardvorlage Platzhalter; E1 |
| `bericht.datum` | Datum | `ErstelltAm` BD:19 | – | Kultur statt „dd.MM.yyyy“; Excel echtes Datum; E1 |
| `bericht.programmversion` | Text | wie `ersteller.version` | – | Alias von `ersteller.version` (BV-Q8); E1 |
| `bericht.varianten.liste`, `.anzahl` | Text, Zahl | `Anzeige` BS:20-22; `Varianten.Count - 1` | Variantenliste (entspricht) | Rückfall „— (nur Stammprojekt)“; E1 |
| `bericht.gebaeudemodell.ausweis` | Text | `GEB_PRODUKTAUSWEIS_VDI6007` BS:41-45 | – | nur bei `ProduktausweisNoetig` (BS:59); E1 |
| `bericht.emissionsmodus` | Text | `EmissionsAusweis.ModusAusVarianten`, KK:324 | – | E1 |
| `bericht.warnungen` | Liste | `daten.Warnungen` BD:25; BS:176-180 | Laufmeldung | E1 |
| `text.seite`, `text.<name>` | Text | MyResource | – | Festtexte der Standardvorlage; E1 |
| `text.kapitel_<name>` (`projekt`, `komponenten`, `ergebnisse`, `vergleich`, `wirtschaftlichkeit`, `anhang`, `anhang_e`) | Text | `Berichtskapitel.Ueberschrift`: die Überschrift, die der Baustein druckt (`BerichtTexte.T`), beim Anhang E `WIRT_AE_TITEL` | – | Kapitelkopf der Standardvorlage in der Sprache des Berichts; vom Kapitel gedeckt; E2 |
| `ersteller.firma` | Text | Einstellung `BerichtFirma`, vorbelegt `LizenzToken.Firma` (`LizenzToken.cs:29`) | Einstellungen › Bericht (entspricht) | leer bei demo/person möglich; E1 |
| `ersteller.programm` | Text | Produktname „EPOS-Plan“ | – | BV-Q8; Deckblatt der Standardvorlage; E1 |
| `ersteller.version` | Text | `ProduktFassung` BS:89-112 | – | Produktfassung wie heute auf dem Deckblatt (BV-Q8); E1 |
| `bild.ersteller.logo` | Bild | Einstellung `BerichtLogo` über `BerichtsvorlagenCtrl.Ersteller()` (`Erstellerangaben.Logo`, `Bildinhalt`) | Einstellungen › Bericht, Feld „Logo“ (entspricht) | nur als Alternativtext eines Bildes, in allen Teilen; ohne Logo entfällt das Bild; Kontext Installation; aus E5 vorgezogen (Entscheid BV-E2-1, 6.5); E2 |
| `projekt.name`, `.kunde`, `.bearbeiter` | Text | `m_szProjektname`, `m_szKunde`, `m_szBearbeiter` BP:24-25; `Model/ProjektModel.cs:10` | `ProjektKopfSeite.razor:68-95` (entspricht) | Kunde: Leerwert leer; Bearbeiter: die Person; E1 |
| `projekt.beschreibung` | Text (mehrzeilig) | `m_szBeschreibung` (`ProjektModel.cs:11`) | wie oben | heute nicht in Excel; E1 |
| `projekt.klimaregion` | Text | `Details.KlimaregionName` PD:64-66 | `ProjektKopfSeite.razor:5` (entspricht) | „—“; E1 |
| `projekt.angelegt`, `.geaendert`, `.simulationsstand` | Datum | BP:30-31; `SimulationsStand` BD:93 | Spalte „Simulation“ (entspricht) | E1 |
| `stamm.kennzahl.<k>` | Zahl | `Kennzahlen[k]` KK:513-525; 44 Schlüssel KK:329-507 | Ergebnisansicht beim Stamm (`UebersichtReiter.razor:147`; entspricht, `kaelte.deckungsgrad` und `kaelte.jaz` ähnlich) | `energie.*`, `eff.*`, `kaelte.*`, `em.*`, `ko.*`; E1 |
| `kennzahl.<k>.beschriftung`, `.einheit` | Text | `Label(englisch)`, `Einheit` KK:14-16, 31 | – | Kontext Bericht, nach Emissionsmodus (KK:489-500); E1 |
| `kapitel.deckblatt`, `.inhalt`, `.projekt`, `.komponenten` | Kapitel | BS:8-50, 116-126; BP:10-410, 416-550; Zuordnung `Berichtskapitel` | – | TOC WBG:247-256; Projekt entfällt ohne Stamm; allein im Absatz oder als Block-Steuerelement (5.3); das Deckblatt deckt die zehn Deckblattangaben, die Projektbeschreibung alle `projekt.*`; E2 |
| `kapitel.ergebnisse`, `.vergleich`, `.wirtschaftlichkeit`, `.anhang` | Kapitel | BV:13-105, 112-474; BW:20-1340; BS:130-182 | – | Bedarf Zeitreihen bzw. Verlauf und Emissionsbilanz; Ergebnisse und Vergleich decken `stamm.kennzahl.*` und `kennzahl.*`, der Anhang `bericht.warnungen`; E2 |
| `kapitel.anhang_e` | Kapitel | AE:411-461 | Überlagerung Anhang E, WS:1665 (ähnlich) | am Häkchen „Wirtschaftlichkeit“ (`B_WIRTSCHAFT`, AE:413), ohne eigenen Schalter; die Spalte „Stelle“ aus den Kapitelstellen der Vorlage (11 Nr. 3); E2 |
| `baustein.<name>` | Schalter | BK:36-46, JSON `B_*` | Häkchen der Berichtsseite (entspricht) | acht Schalter; wirken erst mit E4 in `{{#wenn}}`, außerhalb meldet der Prüfer einen Ortsfehler; E2 |
| `hat.varianten`, `.ergebnis`, `.zeitreihen`, `.kaelte`, `.wirtschaft`, `.fehler`, `.veraltet`, `.gebaeude`, `.emissionsbilanz`, `.sensitivitaet`, `.emissionsmodus_gwp` | Schalter | abgeleitet aus dem Wertesatz; `EmissionsAusweis` | – | 11 Schalter; im Standblock für den Stand, sonst der ganze Bericht (auch im Gruppenblock `\|block n`); E4 |
| `hat.tabelle.<name>`, `hat.bild.<name>` | Schalter | Tabelle bzw. Bildmodell des Wertesatzes vorhanden | – | je Tabelle (37) und je Bild (16); im Standblock für den Stand, außerhalb: irgendein Stand; in `Deckt` des Kapitels (BV-E5-3); E5 |
| `stand.anzeige`, `.rolle`, `.bezeichner`, `.projektname`, `.hinweis` | Text | `Anzeige` BD:403-406; EBG:246-255; BS:139-162 | Variantenliste (entspricht) | Stamm: Stammprojektname; E4 |
| `stand.beschreibung`, `.kunde`, `.bearbeiter`, `.stromspeicher` | Text | `VariantenDaten.Projekt` BD:87, BDS:515-520; `SpeicherKontextText` (BSG:126-129), im Sammler | Spalte Stromspeicher (entspricht) | E4 |
| `stand.simulationsstand` | Datum/Zeit | `SimulationsStand`, `FrischSimuliert` BV:36-38 | Spalte „Simulation“ (entspricht) | E4 |
| `stand.ist_stamm`, `.hat_fehler`, `.veraltet`, `.frisch`, `.hat_zeitreihen`; `stand.fehler` | Schalter; Text | `IstStamm`, `Fehler` BD:382, `ErgebnisVeraltet` BD:100, `FrischSimuliert` BD:96; BV:40-41 | – | E4 |
| `stand.a.*`, `stand.b.*` | Kontext | Vergleichssicht BSG:214-224 | Paarvergleich der Wirtschaftlichkeitsseite | Paarzwillinge der Standwerte, Kontext Gruppe; `stand.b` in Sicht 1 nur bei einer Variante, der Prüfer meldet die Paarsicht bei mehr als einer Variante; E4 |
| `stand.kennzahl.<k>` | Zahl | wie `stamm.kennzahl` | Ergebnisansicht einer Variante; Vergleichstabelle (entspricht, `kaelte.deckungsgrad` und `kaelte.jaz` ähnlich) | 44 Schlüssel; im Block; E4 |
| `stand.delta.<k>`, `stand.delta_prozent.<k>` | Zahl | WBG:472-491 | – | je 32 Schlüssel, nur `DeltaAnzeigen`; in Excel-Vorlagen ein Wert; E4 |
| `stamm.kaeltestrom.*` | – | – | – | nicht angelegt: Doppelung zu `stamm.kennzahl.kaelte.*` |
| `stamm.wirtschaft.<zeile>` | Zahl | `WirtZeile.Wert/ExcelWert/Format` WZ:32, 41, 295 | Kostenkacheln beim Stamm, `KostenSeite.razor:109` (ähnlich: Cent gegen ganze Euro) | 68 Zeilen × 3 Szenarien (`.guenstig`/`.unguenstig`) = 204; E4 |
| `stand.wirtschaft.<zeile>` (`nettobarwert`, `kapitalwert_diff`, `annuitaet`, `amortisation`, `irr`, `investition`, `betriebskosten`, `energiekosten`, `gestehungskosten` …) | Zahl | `WirtZeile.ExcelWert` aus `Wirtschaft.Zeilen(IdReferenzTafel)` | Kostenkacheln bei einer Variante (ähnlich: Cent gegen ganze Euro) | 68 Zeilen × 3 Szenarien; Amortisation, IRR teils Text; E4 |
| `stand.wirtschaft.erl_<block>_k_<komponente>`, `…_teil_…`, `vermieden_herleitung_<komponente>` | – | WZ:1237, 1262, 844; Konstanten WZ:143-153 | – | laufzeitgebildete Kopf-, Kohärenz- und Anlagenzeilen (`erl_kopf_a/b`, `erl_<b>_k_<k>`, `KOHAERENZ_n`, `ENERGIEKOSTEN_ANLAGE_<name>`) nicht angelegt: Namen und Anzahl hängen am Projekt |
| `stand.wirtschaft.<zeile>.grund` | Text | Fehlgrund der Zelle (`ErgebnisansichtTests.cs:585`) | – | 54 Schlüssel, nur im Erwartungsfall; E4 |
| `wirtschaft.warnungen`, `stand.wirtschaft.warnungen` | Liste | BW:39-54, 107-117, 217-225, 908-917; EBG:434-510 | Hinweise der Wirtschaftlichkeitsseite (ähnlich) | E4 |
| `wirtschaft.beste.anzeige`, `.ist_stamm`, `.kapitalwert`, `.nettobarwert`, `wirtschaft.beste.<zeile>` | Text, Schalter, Zahl | Regel `BesteVariante.Waehle` im Kern (9.5): Erwartungsfall, größte Kapitalwertdifferenz unter den Varianten, ohne Variante der Stamm | Kacheln WS:247 (entspricht) | `.kapitalwert` ist der Kachelwert (Variante: Kapitalwertdifferenz, Stammfall: Nettobarwert); `.kapitalwert_diff` im Stammfall leer mit Grund „nur Stammprojekt gerechnet“; E4 |
| `stand.bandbreite.unguenstig`, `.erwartet`, `.guenstig`, `.spanne`, `.amortisation`, `.einstufung` | Zahl/Text | `BandbreitenZeile` BW:1182-1212 | Szenarientafel WS:264 (entspricht) | E4 |
| `wirtschaft.referenzname`, `.rechenstand` | Text, Datum | BW:1172; `Zeitstempel` EBG:431 | Wirtschaftlichkeitsseite (entspricht) | E4 |
| `wirtschaft.methodik`, `wirtschaft.parameternachweis` | Text | BW:66-71; BW:76-89, EBG:420-432 | – | Methodik in der Standardvorlage Platzhalter; Nachweis datenbankfrei ab E3; E4 |
| `wirtschaft.parameter.<name>` (`zins`, `zins_basis`, `zeitraum`, `p_e`, `p_b`, `p_i`, `risiko_zuschlag`, `risiko_abzug`, `risiko_verlust`, `risiko_p`) | Zahl | `ExcelFormelmappe.cs:120-247`, `BerichtsDaten.Wirtschaft` | – | 10 × 3 Szenarien = 30; in Prozent (BV-E4-3), Excel als Anteil (E7); E4 |
| `wirtschaft.szenario.<s>.name`, `.annahmen`, `.traegerpreise` | Text | BW:150-159, 1267-1290; EBG:516-647 | – | 9 Schlüssel; E4 |
| `wirtschaft.vorschlag`, `.szenarioabdeckung` | Text | BW:1248, 1240; EBG:689, 1455-1462 | – | Vorschlag entfällt, wenn leer; E4 |
| `wirtschaft.valeri_hinweise`, `.hinweise`, `.deklarationen` | Liste | BW:1309-1339; EBG:709-750, 1087-1117 | – | E4 |
| `vergleich.spanne.<k>`, `vergleich.minimum.<k>`, `vergleich.maximum.<k>`; `vergleich.beste_variante` | Zahl; Text | `Kennzahlen` aller Stände, KK:513-525 | – | je 44; Minimum und Maximum neutral statt eines Bestwerts (BV-E4-2); `vergleich.beste_variante` ist Alias von `wirtschaft.beste.anzeige`; E4 |
| `gebaeude.name`, `.art`, `.baualtersklasse`, `.flaeche`, `.nutzer`, `.waermebedarf`, `.spez_waermeverbrauch`, `.ww_bedarf`, `.raumhoehe` | Text/Zahl | `Tab_Gebaeude` über `ProjektDetails.S/D` BP:39-50 | – | 18 Schlüssel mit den Ergebniswerten; nur in `je gebaeude`; E4 |
| `gebaeude.ergebnis.rechenweg`, `.heizwaerme`, `.spitze`, `.spitze_tagesmittel`, `.spitze_95`, `.kuehlenergie`, `.kuehlstunden`, `.raumtemperatur`, `.ueberhitzungsstunden` | Text/Zahl | `ErgebnisGebaeudeModel` BP:281-297 | Gebäude-Ergebnisreiter (ähnlich) | Kühlwerte nur auf dem VDI-6007-Weg; E4 |
| `muster.tabelle` | Mustertabelle | Alternativtext oder Titel einer Vorlagentabelle | – | Rollen Stamm, Gruppe, Summe, Warnung; legt „EPOS Tabelle“ an, wenn die Vorlage sie nicht führt; wird entfernt; E5 |
| `tabelle.varianten` | Tabelle | EBG:238-261 | Variantenliste (entspricht) | sechs Spalten mit Stromspeicher; listentauglich; E5 |
| `tabelle.komponenten.matrix` | Tabelle | BP:429-464; EBG:264-281 | Komponententabelle `UebersichtSeite.razor` (ähnlich) | Spalten je Stand; E5 |
| `tabelle.komponenten.kenndaten.<gewerk>` (`bhkw`, `photovoltaik`, `pufferspeicher`, `solarthermie`, `spitzenkessel`, `stromspeicher`, `waermepumpe`) | Tabelle | BP:467-513; PD:23 | Gegenüberstellung `UebersichtSeiteGaben.cs:463` (ähnlich) | nur das erste Gerät (PD:80); E5 |
| `stand.tabelle.abweichungen`, `tabelle.gebaeude.ergebnis`, `tabelle.kaelteerzeuger` | Tabelle | BP:529-546, BDS:428; BP:269-307; BP:193-221 | – | E5 |
| `tabelle.speichertemperaturen` | Tabelle | BP:341-361 | `SimulationErgebnisSeite.razor:154` (entspricht) | E5 |
| `stand.tabelle.kennzahlen` | Tabelle | BV:43-54 (Kernliste BV:19-26); EBG:1725-1743 | – | Word 14 Kennzahlen, Excel alle mit Wert; E5 |
| `tabelle.vergleich.<gruppe>` (`energiebilanz`, `effizienz`, `kaelte`, `emissionen`, `kosten`), `tabelle.vergleich` | Tabelle | BV:254-308, KK:51; EBG:289-366 | – | Δ-Spalte nur bei einer Variante; E5 |
| `tabelle.vergleich.delta_prozent` | Tabelle | BV:310-345 | – | ab zwei Varianten; E5 |
| `stand.tabelle.erzeuger`, `.brennstoffmengen` | Tabelle | BV:358-432, EBG:1778-1823; BV:435-466, EBG:1750-1768 | – | Menge künftig Zahl; E5 |
| `stand.tabelle.monatswerte` | Tabelle | EBG:1825-1879 | – | nicht gebaut: nur Excel-Quelle; E7/E8 |
| `tabelle.wirtschaft.kennzahlen` (+ `.guenstig`/`.unguenstig`) | Tabelle | BW:814-920; EBG:516-647 | `Vergleichstabelle` WS:1709 (entspricht) | Paarsicht A∣B; E5 |
| `tabelle.wirtschaft.parameter` | Tabelle | `ExcelFormelmappe.cs:120-247` | – | nicht gebaut: nur Excel-Quelle; E7/E8 |
| `stand.tabelle.kwkg_module`, `.betriebskosten` | Tabelle | BW:526-620, EBG:1483-1578; BW:681-792, EBG:1587-1684 | – | KWK 11 oder 16 Spalten; E5 |
| `stand.tabelle.mehrjahres` | Tabelle | BW:389-483 | Mehrjahrestafel WS:1631 (entspricht) | Ausgabe Word; in Excel erzeugt; E5 |
| `stand.tabelle.vermiedene_kosten`, `.sensitivitaet`, `.strommengen`, `.emissionsbilanz` | Tabelle | BW:486-517; BW:1061-1105, EBG:915-968; BW:928-971, EBG:970-1015; BW:974-1052, EBG:1017-1085 | – | Emissionsbilanz nur mit Kraftwerkspark; E5 |
| `tabelle.wirtschaft.szenarien` | Tabelle | BW:1131-1214; EBG:1396-1470 | Szenarientafel WS:264 (entspricht) | E5 |
| `tabelle.wirtschaft.nicht_monetaer` | Tabelle | BW:634-679; EBG:1328-1372 | `WirkungenListe` WS:446 (ähnlich) | BW:678 vorab behoben; E5 |
| `tabelle.wirtschaft.verlauf` | Tabelle | EBG:759-879; VE:66-163 | `KapitalwertVerlaufAbschnitt.razor` (entspricht) | verbundene Köpfe, nicht listentauglich; nicht gebaut: nur Excel-Quelle; E7/E8 |
| `tabelle.anhang.simulationsstaende` | Tabelle | BS:139-162 | – | E5 |
| `tabelle.anhang_e.checkliste` | Tabelle | AE:344-403, 416-460 | Überlagerung Anhang E, WS:1665 (entspricht) | Spalte „Stelle“ nach Vorlage; E5 |
| `stand.bild.waerme_jahresverlauf`, `.waerme_dauerlinie`, `.strombilanz_monate` | Bild | CR:387, 414, 444; BV:75-90 | – | 620×280, Zeitreihen; E5; Excel: Diagramm auf dem Tabellenbereich (E8) |
| `stand.bild.speicherverlauf`; `stamm.bild.speichertemperaturen` | Bild | CR:488, BV:93-96; CR:574, BP:371-377 | `Speicherbetrieb` SEH:724; `SimulationErgebnisSeite.razor:154` (ähnlich) | 620×260 bzw. 620×280; E5; Excel: Diagramm auf dem Tabellenbereich (E8) |
| `bild.vergleich.balken.<k>` (`energie.brennstoff`, `energie.netzbezug`, `energie.waermerest`, `eff.jaz`) | Bild | CR:320; BV:162-187 | – | Höhe nach Zahl der Stände; E5; Excel: Diagramm auf dem Tabellenbereich (E8) |
| `stand.bild.deckung_waerme`, `.deckung_strom` | Bild | `KuchenModell` CR:254; BV:220-223 | `Ring` SEH:320, 364, 389 (ähnlich) | 420×262; E5; Excel: Diagramm auf dem Tabellenbereich (E8) |
| `bild.wirtschaft.kapitalwert_szenarien`, `.barwerte_kumuliert` | Bild | CR:1296, BW:290-294; CR:894, BW:316-319 | Wirtschaftlichkeitsseite, `KapitalwertVerlaufAbschnitt.razor` (entspricht) | E5; Excel: Diagramm auf dem Tabellenbereich (E8) |
| `bild.wirtschaft.bruecke`, `.spanne`; `stand.bild.zahlungsstrom` | Bild | CR:2023, BW:349-354; CR:1707, BW:1223-1226; CR:2390, BW:415-418 | WS:1584, :1503, :1637 (entspricht) | Brücke nur Leitversion ≠ Referenz; E5; Excel: Diagramm auf dem Tabellenbereich (E8) |
| `blatt.uebersicht`, `.vergleich`, `.wirtschaftlichkeit`, `.verlauf`, `.checkliste` | Blatt | EBG:213-366, 382-1131; VE:66; AE:344 | – | `wirtschaftlichkeit` ist die Formelmappe; entfällt ohne Inhalt; E7 |
| `blatt.detail` | Blatt (Muster) | EBG:1702-1776 | – | je Stand geklont; E8 |
| `EPOS.reihe.<name>` | Name (Diagrammreihe) | Rasterreihen des Kerns | – | `RefersTo` von EPOS gesetzt; E8 |
| `Zins_i`, `Zeitraum_T`, `p_E`, `p_B`, `p_I` (+ `_Guenstig`/`_Unguenstig`), `Zins_Basis`, `Risiko_*` | reservierter Name | `ExcelFormelmappe.cs:36-48, 293-312` | – | Ausgabe, nicht füllbar; E7 |
| `bild.kosten.profil`, `bild.klimadaten.jahresgang`, `bild.waermequelle.jahresgang`, `bild.waermepumpe.kennlinie.cop`/`.leistung`, `bild.speicherflotte.optimierungsraster`/`.schnittkurve`/`.stueckzahlkurve`/`.jahresprojektion` | Bild (vorgemerkt) | CR:2634, 2844, 3083, 5765, 6228, 6432, 6695 | `KostenprofilHuelle.cs:79`, `KlimadatenHuelle.cs:180, 193`, `QuelleErdreichHuelle.cs:195`, `WaermepumpeStammHuelle.cs:282, 285`, `SpeicherFlottenAnzeigeCtrl*.cs` | nicht in v1; Excel: Diagramm auf dem Tabellenbereich |
| `bild.zapfprofil.tagesgang`/`.wochenprofil`/`.jahresgang`/`.dauerlinie`, `bild.gebaeude.raumtemperatur`, `bild.ergebnis.erzeugerstapel`/`.streuwolke`/`.temperaturverlauf`/`.jahresverlauf`/`.ganglinie_normiert`/`.monatsstapel`/`.stundenprofil`, `bild.peakshaving.lastgang` | Bild (vorgemerkt) | `ZapfprofilBilder` :184, 210, 232, 256; CR:5316, 4503, 4875, 5291, 4126, 4354, 5153, 3493; `PeakShavingBild.cs:71` | Zapfprofil-Dialoge; Gebäude-Ergebnisreiter; SEH:430, 500, 523, 445, 190, 781; `PeakShavingDialog.razor` | nicht in v1; Excel: Diagramm auf dem Tabellenbereich |


## Anhang B Beispielvorlage

### B.1 Word „Kurzbericht“ (je Sprache, Lehrvorlage)

Gebaut mit BV-E5: `Berichtsvorlage_Kurzbericht.docx` und `Berichtsvorlage_Kurzbericht_en.docx`, erzeugt vom Werkzeug
`Werkzeuge/Berichtsvorlage` mit `kurzbericht <quelle> <ziel> --sprache de|en` aus der bereinigten Stilvorlage, byte-gleich
wiederholbar (das Platzhalterbild der Bildrahmen entsteht aus festen Bytes). Überschriften, Beschriftungen und Sätze
sind fester Text in der Sprache der Datei; neun Erläuterungen stehen als Word-Kommentare derselben Sprache und werden
beim Füllen entfernt (6.7); `custom.xml` trägt `EPOS.Katalogfassung` 4, `EPOS.Vorlage` `kurzbericht` und `EPOS.Sprache`.
Der Rundlauf mit 1030 (de und en) füllt ohne Prüferfehler und ohne verbliebenes `{{`, der Validator ist grün.

| Nr. | Abschnitt | Inhalt und Platzhalter |
|---|---|---|
| 1 | Deckblatt (eigener Abschnitt ohne Kopf- und Fußzeile) | `{{bericht.titel}}` im Stil Titel, ein fester Untertitel; `{{projekt.kunde}}`, `{{projekt.bearbeiter}}`, `{{ersteller.firma}}`, `{{bericht.datum}}`; „Verglichen: {{bericht.varianten.liste}}“; „Erstellt mit {{ersteller.programm}} {{ersteller.version}}“; Abschnittswechsel |
| 2 | Kopf- und Fußzeile ab Abschnitt 2 | oben „{{projekt.name}} · {{bericht.titel}}“ und das Logo als Bildplatzhalter `{{bild.ersteller.logo}}`; unten Firma, Datum und „{{text.seite}} {PAGE} / {NUMPAGES}“ wie in der Standardvorlage |
| 3 | Überschrift 1 „Ausgangslage“ | Fließtext der Vorlage mit `{{projekt.beschreibung}}` und „Klimaregion: {{projekt.klimaregion}}“ |
| 4 | Überschrift 1 „Ergebnisse im Überblick“ | Tabelle Variante · Wärmebedarf · JAZ · CO₂ · Kapitalwertdifferenz; Musterzeile `{{#je stand}}{{stand.anzeige}}` · `{{stand.kennzahl.energie.waermebedarf\|ohne einheit}}` · `{{stand.kennzahl.eff.jaz\|stellen 1}}` · `{{stand.kennzahl.em.co2\|ohne einheit}}` · `{{stand.wirtschaft.kapitalwert_diff\|mit grund}}{{/je}}`; darunter `{{bericht.warnungen}}` |
| 5 | Überschrift 1 „Empfehlung“ | „Beste Variante: {{wirtschaft.beste.anzeige}} mit einer Kapitalwertdifferenz von {{wirtschaft.beste.kapitalwert_diff}}.“; `{{wirtschaft.vorschlag}}` |
| 6 | Überschrift 1 „Wirtschaftlichkeit“ | im Block `{{#wenn hat.bild.wirtschaft.spanne}}` ein Bildrahmen in voller Breite, Alternativtext `{{bild.wirtschaft.spanne}}`; Absatz `{{tabelle.wirtschaft.szenarien}}` (Strukturtabelle); `{{wirtschaft.warnungen}}` |
| 7 | Bedingter Abschnitt | `{{#wenn hat.kaelte}}` Überschrift 2 „Kühlung“, Satz mit `{{stamm.kennzahl.kaelte.jahresbedarf}}` und `{{stamm.kennzahl.kaelte.deckungsgrad}}` `{{/wenn}}` |
| 8 | Zwei Bilder nebeneinander | Überschrift 1 zur Deckung, darunter der Block `{{#je stand}}` mit Überschrift 2 `{{stand.anzeige}}` und einer zweispaltigen Tabelle ohne Rahmen: `{{stand.bild.deckung_waerme}}` und `{{stand.bild.deckung_strom}}` je 7,8 cm breit, über der Mindestbreite (Bildgröße Stufe 2, 6.5) |
| 9 | Anhang | Absatz „Anhang“ im Format „EPOS Kapitelkopf“, darunter `{{kapitel.anhang\|ohne titel\|ebene 2}}` |
| 10 | Mustertabelle am Ende | Alternativtext `{{muster.tabelle}}`, Zellen „Stamm“ (hellgrau), „Gruppe“ (fett auf Blaugrau), „Summe“ (fett mit Linie), „Warnung“ (rot); wird entfernt |

**Abweichungen vom ersten Entwurf:** Die zwei Bilder stehen im Block `je stand` statt `je variante|block 1` — die
Deckungskuchen gehören zu jedem Stand, auch zum Stamm; das Spannenbild steht im Block `wenn hat.bild.wirtschaft.spanne`,
damit ohne Modell kein leerer Rahmen bleibt. Der Alternativtext der Mustertabelle (`w:tblDescription`) ist erst ab Office
2010 gültig; der Validator mit dem Maß Office 2007 lässt genau diesen Befund durch.

### B.2 Excel „Kurzmappe“

| Nr. | Blatt | Inhalt und Platzhalter |
|---|---|---|
| 1 | „Deckblatt“ (Anwenderblatt) | A1 `{{bericht.titel}}`; A3:B7 Beschriftung und Wert mit `{{projekt.name}}`, `{{projekt.kunde}}`, `{{projekt.bearbeiter}}`, `{{ersteller.firma}}`, `{{bericht.datum}}` (Datumsformat); Logo als Bild der Vorlage |
| 2 | „Kennzahlen“ (Anwenderblatt) | Stammwerte über Namen `EPOS.stamm.kennzahl.energie.waermebedarf` usw., Format „Standard“ → Kernformat; Excel-Tabelle `EPOS_tabelle_varianten` mit Formatmuster in der ersten Datenzeile |
| 3 | „Monate“ (Anwenderblatt) | feste Tabelle mit zwölf Zeilen aus Zellplatzhaltern; Excel-Liniendiagramm auf diesem Raster |
| 4 | Blattmarke `{{blatt.vergleich}}` in A1 | wird zum erzeugten Vergleichsblatt |
| 5 | Blattmarke `{{blatt.wirtschaftlichkeit}}` | wird zur Formelmappe; entfällt ohne Häkchen |
| 6 | Musterblatt `{{blatt.detail}}` | A1 `{{stand.anzeige}}`, A4 `{{stand.tabelle.kennzahlen}}`; je Stand geklont (E8) |

Die Mappe ist `.xlsx`, ohne reservierte Namen und ohne Makros.

### B.3 Standardvorlage (Word): Beispielvorlage aus dem bisherigen Bericht

`WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Beispiel.docx` und — ohne deren Kommentare — die
Standardvorlage `Berichtsvorlage_Standard.docx`, beide erzeugt vom Werkzeug `Werkzeuge/Berichtsvorlage` (Modus `beispiel`,
für die Standardvorlage mit `--standard`) aus der heutigen Vorlage — Seiteneinrichtung, Kopf- und Fußzeile, Stile
bereinigt (6.2) — und dem Aufbau des heutigen Berichts; die Standardvorlage ist eine Datei der Auslieferung (BV-Q19 b,
6.3), die Beispielvorlage steht in keinem Lieferweg; sprachneutral: Beschriftungen, Festtexte und Kapitelköpfe als
`{{text.<name>}}` (4.9).

| Nr. | Abschnitt | Inhalt und Platzhalter |
|---|---|---|
| 1 | Deckblatt (Abschnitt 1, eigene Seite ohne Kopf- und Fußzeile) | `{{bericht.titel}}` im Stil Titel, `{{bericht.untertitel}}` im Stil Untertitel; Tabelle Beschriftung · Wert mit `{{projekt.kunde}}`, `{{projekt.bearbeiter}}`, `{{ersteller.firma}}`, `{{bericht.varianten.liste}}`, `{{bericht.datum}}`; Absatz `{{bericht.gebaeudemodell.ausweis\|leer statt strich}}`; Zeile „Erstellt mit {{ersteller.programm}} {{ersteller.version}}“ (BV-Q8, „Erstellt mit“ als `text.*`); Abschnittswechsel „nächste Seite“ |
| 2 | Inhalt | `{{kapitel.inhalt}}` (Inhaltsverzeichnis; bringt seine Überschrift „Inhalt“ selbst mit) |
| 3 | Kapitel in heutiger Folge | je Kapitel ein Kapitelkopf `{{text.kapitel_<name>}}` im Format „EPOS Kapitelkopf“ (Gliederungsebene 1; entfällt mit dem Kapitel, 5.3; eigener Text statt des Platzhalters ist erlaubt), darunter in eigenem Absatz `{{kapitel.<name>\|ohne titel}}` für `projekt`, `komponenten`, `ergebnisse`, `vergleich`, `wirtschaftlichkeit`, `anhang`, `anhang_e` |
| 4 | Kopfzeile ab Abschnitt 2 | `{{ersteller.programm}}` statt „EPOS-Plan“; rechts das Logo als Bildplatzhalter — Alternativtext `{{bild.ersteller.logo}}`, Platzhalterbild „Logo“ (150 × 82 px) an Ort und in Größe des bisherigen Logos (6.5, Entscheid BV-E2-1) |
| 5 | Fußzeile ab Abschnitt 2 | links `{{ersteller.firma}}` statt „INEKON GmbH“, in der Mitte `{{bericht.datum}}` statt DATE, rechts „{{text.seite}} {PAGE} / {NUMPAGES}“ (BV-Q8) |
| 6 | Dokumenteigenschaften | `custom.xml` mit `EPOS.Katalogfassung` 4 und `EPOS.Vorlage` (`standard` bzw. `beispiel`) |

**Stufen.** In BV-E0 war die Datei Anschauung: Validator grün, der Anwender sah sie in Word; gefüllt wurde sie noch
nicht. In BV-E1 entstand aus ihr die Standardvorlage in der Stufe mit dem Sammelanker, als eigene Datei
`Berichtsvorlage_Standard.docx` (Entscheid BV-E1-1) — `{{bericht.inhalt}}` stand an der Stelle von Deckblatt,
Inhaltsverzeichnis und Kapiteln. **Mit BV-E2 (#520) gilt der volle Aufbau:** Die Standardvorlage entsteht mit
`--standard` ohne Kommentare, die Beispielvorlage mit denselben 36 Platzhaltern (35 Text-, ein Bildplatzhalter) und drei
Kommentaren — am Deckblatt samt Kopf- und Fußzeile und Logo, an `{{kapitel.inhalt}}` und am ersten Kapitelkopf; Word
erlaubt in Kopf- und Fußzeilen keine Kommentare. Das Häkchen „Deckblatt“ ist bei der Standardvorlage ausgegraut mit dem
Grund „Deckblatt kommt aus der Vorlage“, weil sie ihr Deckblatt selbst trägt (BV-Q1 c, 10.2). **Mit BV-E5 (#541)**
tragen beide Dateien die Katalogfassung 4 (Vorgabe des Werkzeugs `--katalogfassung 4`) bei gleichem Inhalt.


## Anhang C Prüfbefunde und ihre Beantwortung

Schwere h = hoch, m = mittel, g = gering. Befunde gleichen Inhalts stehen in einer Zeile.

| Nr. | Befund kurz | Antwort im Papier: Abschnitt |
|---|---|---|
| 1 (h), 55 (h), 80 (h) | Feste Stil-IDs; Vorlagen aus deutschem Word; doppelte Überschriftstile | 6.2, 11 Nr. 3, 12 |
| 2 (m) | Anker und Bildteile an den Hauptrumpf gebunden | 4.3, 6.5 |
| 3 (m), 86 (m) | Tabellenformatvorlage kann Rollen nicht ausdrücken | 6.4 |
| 4 (m) | Annahme nachverfolgter Änderungen ist Eigenbau | 6.1, 6.8 |
| 5 (m), 85 (m) | Füllregeln und Zustand von SDT nach dem Füllen | 6.6, BV-Q14 |
| 6 (m), 64 (m) | Kein Stand-Kontext in Excel; Delta-Formel; Formeln des Anwenders | 4.4, 4.7, 7.1, 7.2, 7.4, 9.4 |
| 7 (m) | ListObjects taugen nicht für Stand-Spalten; `ReplaceData` nutzt Reflection | 5.4, 7.3, 15.3 |
| 8 (m), 103 (g) | Entfallende Blätter mit Marke; `blatt.detail`; uneinheitliche Parameter | 4.8, 7.2 |
| 9 (m) | `FullCalculationOnLoad` nur bei EPOS-Formeln; `<v>` ungemessen | 7.4, 13 BV-E0 |
| 10 (m), 27 (h) | Zwischenablage gehört der Hülle; neue Naht, iOS-Änderung | 8.4, 9.6, BV-Q18 |
| 11 (m), 28 (m) | `CascadingValue` erreicht Windows-Dialoge nicht | 9.6, 15.3 |
| 12 (m) | Bericht auf iOS nie gelaufen | 2.3, 8.5 |
| 13 (m), 31 (m) | Ressourcen- und Excel-Namen nicht eindeutig; erzeugte Einträge ohne Text | 4.5, 5.5 |
| 14 (g) | Feste Satzspiegelbreite | 5.3 |
| 15 (g) | Stufe 1 verkleinert die Schrift | 6.5, 13 BV-E5 |
| 16 (g), 59 (m) | DATE-Feld; `updateFields` bei NUMPAGES | 6.3, 6.7, 11 Nr. 1 |
| 17 (g) | Größengrenze schützt nicht vor Zip-Bombe | 8.5 |
| 18 (g) | Fundstelle EBG:126; Standardschrift der Vorlage | 2.2, 7.1 |
| 19 (g), 73 (g) | Value gegen Zahlenformat; Laufmeldung, Einheit, Prozent | 4.10, 7.1, 9.4 |
| 20 (g) | „Alle Lizenzen MIT“ falsch | 8.2 |
| 21 (g) | Zeitziel unbelegt; doppeltes Füllen für Anhang E | 8.5, 11 Nr. 3 |
| 22 (g) | Paketvergleich nach Teilnamen meldet Fehlalarme | 7.4 |
| 23 (g), 98 (g) | Vorlage in Word geöffnet; Sperrdateien; gleicher Name | 6.8, 10.3 |
| 24 (g) | Baukasten nicht fehlerfrei füllbar | 6.3, 12 |
| 25 (g), 65 (m) | Kommentare der Lehrvorlage im Kundenbericht | 6.3, 6.7 |
| 26 (g), 48 (g) | iOS-Dateifilter ohne `.dotx`/`.xltx` | 8.5, 13 BV-E1, BV-E7 |
| 29 (m) | Umkehr einer dokumentierten Entscheidung; zwei statt drei Lieferwege | 2.3, 8.4, BV-Q19 |
| 30 (m), 61 (m) | „Im Ordner zeigen“, „In Word öffnen“; auf iOS Teilen | 10.2, 10.3 |
| 32 (m) | Baustein an Fachklasse; Orte im Kern | 5.1, 9.6 |
| 33 (m), 70 (g) | Zeilenmarken nur bei Fokus; dichte Tabellen, Zoomleiste | 9.4, 12, 15.3 |
| 34 (m), 69 (g), 39 (g) | Dauerhafte Leiste, ✕, „Kopfleiste“; Schalter im Kopf; Ort neben der Pille | 3, 9.4, 9.8, 15.3, BV-Q9 |
| 35 (m), 101 (g) | Prüfzeile als `Kennzeichen`; mitgelieferte Vorlage | 9.7, 9.8, 10.2 |
| 36 (m), 71 (g) | Eine Standardvorlage für zwei Sprachen; Syntax deutsch | 4.9, 6.3, BV-Q3, BV-Q7 |
| 37 (g) | Wie Änderungen der Vorlage erkannt werden | 6.8 |
| 38 (g) | Trefferflächen überlagern Nachbarn | 9.4, 9.7 |
| 40 (g) | Bauart der Überlagerungen | 9.7 |
| 41 (g), 43 (g), 76 (g) | Texte-Bündel; Auswahlfeld mit int-Ids; Kopieren der Standardvorlage versteckt | 10.2 |
| 42 (g) | KI-Abdeckungswache in BV-E1 rot | 13 BV-E1 |
| 44 (g) | Herkunft der Engine-Texte | 4.10 |
| 45 (g) | Ad-hoc-DDL in `BerichtCtrl` | 10.3, 13 BV-E0 |
| 46 (g), 47 (g) | Wiki-Regeln; Ordner der Testvorlagen | 12 |
| 49 (g) | BV-E3 ohne Referenzlauf | 5.1, 13 BV-E3 |
| 50 (g) | Verweis auf nicht vorhandene Entwürfe; Index, Status | 12, 15.2, 15.3 |
| 51 (g) | Code-Name „Marke“ mehrdeutig | 4.1 |
| 52 (g) | `IEinstellungen` gilt je Anwender; Schreibweise | 10.3 |
| 53 (g) | Keine `KiMeldungskennung` | 6.8 |
| 54 (h) | Marken nennen Schlüssel mit anderem Wert | 9.5, 9.8, Anhang A |
| 56 (m) | `stand.*` vor den Blöcken nutzlos | 5.6, 13 (Blöcke in BV-E4 vor Tabellen in BV-E5) |
| 57 (m) | Kopieren immer Text; kein Art-Verstoß | 4.2, 4.10, 9.4 |
| 58 (m) | Tippbarkeit, Umlaute, Autokorrektur | 4.2, 6.3, 13 BV-E0 |
| 60 (m), 94 (m) | Kopie veraltet still; offene Vorlage; gemeinsame Vorlagen | 6.8, 10.3, BV-Q15 |
| 62 (m) | Dauerwarnungen stumpfen ab; Meldungstexte | 5.6, 6.1, 6.8 |
| 63 (m) | Umbenennung bricht Vorlagen still | 5.6, 12 |
| 66 (m) | Häkchen gegen Vorlageninhalt | 5.3, 10.2 |
| 67 (m) | Zielbild zu weit; „In der App zeigen“ | 3, 9.4, 9.5 |
| 68 (m) | Probefüllung erst spät | entfällt, BV-Q12 (b) |
| 72 (g) | Zwei Schreibweisen der Blockgröße | 4.8 |
| 74 (g), 75 (g) | Regel des zweiten Einstiegs; Paarsicht unsichtbar | 4.7, 10.2 |
| 77 (g), 100 (g) | Vorlage am Dateinamen, Cloud-Ordner; Vergleichssicht und Verlaufsmappe | 10.1, 10.3 |
| 78 (h) | Parameter und Szenarien ohne Platzhalter | 5.2, Anhang A |
| 79 (h), 90 (m) | Gültigkeits- und Warnhinweise; Schalter im Standkontext | 4.7, 4.11, Anhang A |
| 81 (h), 92 (m), 96 (m) | Abdeckung der Orte; Anhang-E-Stelle in der App; ähnliche Gegenstücke, vorgemerkte Diagramme | 5.4, 9.5, Anhang A |
| 82 (m) | Mouse-over fehlt als Lesart | 9.1, 9.7, BV-Q9 |
| 83 (m), 105 (g) | Etappen unvollständig; kein Aufwand | 13 |
| 84 (m) | Keine Variantenwerte ohne Blöcke; `vergleich.*` leer | 4.7, 13, Anhang A |
| 87 (m) | Diagrammgestaltung nicht geregelt | 6.5, 10.1, BV-Q17 |
| 88 (m) | Verwaiste Überschriften vor Blöcken | 5.3, 13 BV-E2 |
| 89 (m) | Beschriftung hängt am Emissionsmodus | 5.2, Anhang A |
| 91 (m) | Beschreibung je Variante; Spalte Stromspeicher | 5.4, Anhang A |
| 93 (m) | Übergang bestehender Installationen | 10.3, 13 BV-E1 |
| 95 (m) | Pflegeweg auf dem iPad | 10.3, BV-Q16 |
| 97 (g) | Rasterprobe fehlt | 12, 13 BV-E6 |
| 99 (g) | Ort der Einstellung „Firma“ | 10.3, 13 BV-E1 |
| 102 (g) | Zur Laufzeit gebildete Zeilenschlüssel | 4.5, Anhang A |
| 104 (g) | Deckungswache nicht erfüllbar | 12 |
