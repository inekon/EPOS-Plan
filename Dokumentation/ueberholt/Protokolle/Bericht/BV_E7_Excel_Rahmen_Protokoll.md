# BV-E7 — Excel-Rahmen (Protokoll)

Etappe BV-E7 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitte 4.4, 7, 10 und 13). Auftrag #549, Anwenderauftrag vom 26.09.2026: „starte BV-E6 und dann BV-E7“. Der gültige
Stand steht im Konzept (Rev. 9) und in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier steht, wie es
geworden ist. Vorgänger: [`BV_E6_Kennzeichnung_Protokoll.md`](BV_E6_Kennzeichnung_Protokoll.md). Zweig
`claude/intelligent-bohr-hthrk8`; der Agentenzweig `worktree-agent-af23f6f02b7c7952b` ab `f46ce962` (ohne BV-E6),
umgesetzt am 26.09.2026. Opus 5.5 hat orchestriert, ein Agent (Opus 5.5) hat im eigenen Worktree Engine, Prüfer,
Einbindung und Tests gebaut (3 Commits, 32 Dateien, +6.974/−79 Zeilen), ein Abschluss-Agent hat ihn mit BV-E6
zusammengeführt, den Anwenderentscheid BV-E7-3 nachgezogen, das Gate gezogen und die Papiere geschrieben. Kein
Schemaschritt, kein Rechenweg berührt (Referenzlauf GESAMT: PASS), keine Referenzbasis neu eingefroren. `EPOS.iOS/` ist
berührt (`EPOS.iOS/Dienste/Dateifilter.cs`, Endung `.xltx`).

| Teil | Gegenstand | Commits |
|---|---|---|
| Engine, Prüfer, Katalog | `ExcelVorlagenfueller`, `ExcelVorlagenmappe.Beurteile`, `ExcelVorlagenpruefer`, `ExcelBerichtGenerator.SchreibeBlaetter`, Paketschutz, Katalog Fassung 5 | `dcd8d0a1` |
| Einbindung | Zeile „Excel-Vorlage“, Hülle, Lauf, KI-Feld, Dateifilter `.xltx` | `ad489014` |
| Tests | Rückfall der Mappe, Formelergebnisse im Vorlagenweg | `9db43194` |
| Abschluss | Merge mit BV-E6, Nachzug der Platzhalteranzeige, Nachzug BV-E7-3, Merge `origin/ios_migration_september`, Gate, Papiere | `5e9248b3`, `e4851cac`, `7d87c958` |

---

## 1 Engine

- **Laden.** Eine `.xltx` stellt das OpenXML SDK auf eine Arbeitsmappe um, danach lädt ClosedXML die Bytes
  (`new XLWorkbook(stream)`). `.xlsm`, `.xltm` und jede Mappe mit VBA-Projekt lehnt die Engine ab
  (`NotSupportedException`); eine Ladeausnahme wird ein benannter Fehler (`InvalidDataException`).
- **Zellplatzhalter.** Allein in der Zelle ein typisierter Wert — Zahl als Zahl, Datum als Datum; das Format der
  Zielzelle setzt die Engine nur, wenn die Zelle das Format „Standard“ trägt. **Prozentregel:** Kennzahlen mit „%“ und
  die Parameter `wirtschaft.parameter.*` gehen in eine Zelle im Prozentformat als Anteil; eine Zelle „Standard“ bekommt
  den Anteil mit `0.00%`. Im Satz wird Text ersetzt. Ein leerer Wert ergibt eine leere Zelle (BV-E7-5).
- **Namen.** `EPOS.<schlüssel>` und `EPOS_<schlüssel>` (mit `__` für den Punkt) bekommen ihren Wert — in ihre Zelle
  oder als Konstante über `RefersTo`.
- **Blattmarken.** `blatt.uebersicht`, `.vergleich`, `.wirtschaftlichkeit`, `.verlauf`, `.checkliste` ersetzen das
  markierte Blatt; die Marke bekommt vor dem Einsetzen einen Ersatznamen, das erzeugte Blatt trägt den heutigen Namen.
  Ohne Inhalt entfällt das Blatt samt Marke. Ohne Marke hängen die Blätter hinten an.
- **Musterblatt `blatt.detail`** (vorgezogen aus BV-E8): je Stand geklont, mit den `stand.*`-Platzhaltern gefüllt,
  benannt wie heute, an der Markenstelle eingefügt.
- **Gleichnamiges Anwenderblatt** wird zu „X (Vorlage)“ umbenannt, mit Warnung (BV-E7-1). **Reservierte Namen** der
  Formelmappe (`Zins_i`, `Zeitraum_T`, `p_E` …) entfernt die Engine aus der Vorlage, mit Warnung.
- **Schrift.** Die erzeugten Blätter setzen Calibri 11 ausdrücklich (siehe Messbefund 3.1).
- **`FullCalculationOnLoad`**, sobald die Vorlage eine Formel trägt.
- **`ExcelBerichtGenerator.SchreibeBlaetter`** ist ausgelagert: dieselben Blattbauer schreiben den Weg ohne Vorlage
  (unverändert) und den Vorlagenweg. `Standardmappe()` im Code bleibt die Messlatte (1030, Gruppe).
- **Katalog Fassung 5:** sechs `blatt.*` (Art `Blatt`, nur Excel), 2.084 Schlüssel und 3 Aliasse, eingefroren in
  `EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v5.txt`. `Vorlagenfeldkatalog.KatalogfassungWord` = 4: die
  mitgelieferten Word-Vorlagen und der Baukasten tragen weiter Fassung 4 (BV-E7-4).

## 2 Prüfer und Einbindung

- **Prüfer** (`ExcelVorlagenpruefer`, dieselben Regeln wie der Füller über `ExcelVorlagenmappe.Beurteile`). Schnell:
  Größe, Format, Makros, unbekannte Schlüssel mit Vorschlag, Schlüssel ohne Ausgabe Excel, Tabellen und Bilder (erst
  BV-E8), Blöcke, Kontextverstöße (`stand.*` außerhalb des Musterblatts, `gebaeude.*`), Formatangaben, Blattmarken
  (nicht in A1, Blatt nicht leer, doppelt, Bezüge darauf), gleichnamige Blätter, reservierte Namen, EPOS-Namen auf
  Bereichen, Formelhinweis, Sprache aus `custom.xml`. Voll zusätzlich Endung und Paketschutz. Fundort „Blatt „…“,
  Zelle B3“ bzw. „Name „EPOS.…““.
- **Controller.** `BerichtsvorlagenCtrl.ListeExcel`, `FindeExcel`, `OhneExcelEintrag`, `ExcelVorlageFuer` (Abweichung
  des Stammprojekts, Vorgabe der Installation über die Einstellung `BerichtVorlageExcel`, sonst ohne Vorlage);
  `BerichtsKonfiguration.VorlageExcelQuelle`/`VorlageExcelDatei`. `BerichtCtrl.ErzeugeExcelLauf` mit Rückfall „ohne
  Vorlage“ bei Lese-, Lade- oder Makrofehler, `LaufmeldungExcel`.
- **Oberfläche.** Zeile „Excel-Vorlage“ in der Vorlagengruppe der Berichtsseite, nur bei Ausgabe Excel oder Beide:
  Auswahlfeld (Vorgabe „Ohne Vorlage (EPOS-Plan)“, eigene `.xlsx`/`.xltx`, fehlende gesperrt), Prüfzeile mit „anzeigen“
  und Prüfliste; „Hinzufügen…“ nimmt Excel-Vorlagen und macht sie zur Excel-Wahl. Hüllen `BerichtsvorlagenGaben`,
  `BerichtSeiteGaben`. KI-Feld `excel_vorlage` (`KiMaskenabdeckungWacheTests` Berichtsseite 4 → 5).
- **Dateifilter.** Windows über die Ressource `BK_BER_VORLAGE_DATEIFILTER` (samt `…_DLG_HINZUFUEGEN`,
  `…_TIP_HINZUFUEGEN`), iOS in `EPOS.iOS/Dienste/Dateifilter.cs` (`.xltx` → `org.openxmlformats.spreadsheetml.template`).
- **Ressourcen:** 84 neue (`BV_XL_*`, `VF_BLATT__*`, `BK_BER_VORLAGE_LBL_EXCEL`, `KI_DLG_BKB_EXCEL_VORLAGE_*`), drei
  geänderte Dateifilter-Texte; im Abschluss sechs weitere (`VF_ANZEIGE_EXCEL_BLATT`, `BV_XL_START_*`,
  `BV_XL_LAUF_OHNE_GEWAEHLT`).

## 3 Messbefunde

### 3.1 Schrift

ClosedXML schreibt Calibri als Schrift 0 der Vorlage: Trägt die Vorlage eine andere Standardschrift (Aptos), fällt ein
ausdrücklich gesetztes „Calibri 11“ mit der Vorlagenschrift zusammen und erbt sie. Umweg: Die erzeugten Blätter setzen
die Schrift mit Zeichensatz Ansi, so entsteht ein eigener Schrifteintrag. Die Aptos-Vorlage ist Testfall.

### 3.2 Paketvergleich (ClosedXML 0.105.1)

Formen gehen beim Füllen verloren (der Prüfer nennt sie in der vollen Stufe). `<controls>`, `externalLink` und nicht
referenzierte Teile bleiben erhalten.

### 3.3 Word gegen Excel

195 Zahlen an Projekt 1019 gleich zwischen Word-Vorlage und Excel-Vorlage; die Standardmappe trägt im Vorlagenweg
dieselben Formeln mit denselben nachgetragenen Ergebnissen wie der Weg ohne Vorlage (1030, Gruppe).

## 4 Zusammenführung mit BV-E6

- **`Resource.resx`/`Resource.en-US.resx`:** Konflikt am Dateiende (beide Etappen hängen an); aufgelöst als Vereinigung
  je Schlüssel gegen die Basis `f46ce962` — 84 neue Einträge aus BV-E7, die drei geänderten Dateifilter-Texte aus BV-E7,
  keine Duplikate; `Resource.Designer.cs` mit `designer_neu.py schreiben` neu erzeugt.
- **Automatisch zusammengeführt:** `KiMaskenabdeckungWacheTests` (BV-E6-Seiten und Berichtsseite 5), `BerichtSeite.razor`,
  `BerichtsvorlagenGaben`, `ExcelBerichtGenerator`. Die BV-E6-Wachen (`VorlagenfeldAbdeckungWacheTests`,
  `VorlagenfeldorteWacheTests`, `VorlagenfeldAnzeigewertWacheTests`) verlangen keinen Ort je Katalogschlüssel und
  bleiben mit Fassung 5 grün; keine Ausnahme nötig.
- **Nachzug der Platzhalteranzeige** (`VorlagenfeldanzeigeHuelle`): Die sechs `blatt.*` kamen mit Fassung 5 in die
  Anzeige und hätten als Excel-Namen `EPOS.blatt.…` gegolten; sie zeigen jetzt „nur in Excel: Blattmarke allein in A1
  eines leeren Blattes“ (`VF_ANZEIGE_EXCEL_BLATT`), Test in `VorlagenfeldanzeigeHuelleTests`.
- `origin/ios_migration_september` (`ef20793f`, mit Gebäudesaat, Diagramm-Zoom und neuer Fassung der Testdatenbank) vor
  der Abnahme konfliktfrei zusammengeführt (`7d87c958`); die Testdatenbank kam über LFS neu.

## 5 Nachzug BV-E7-3: Excel-Befunde in der Startrückfrage

- **Kern:** `BerichtCtrl.PruefeExcelVorStart(konfig, englisch, sicht)` wählt die Excel-Vorlage wie der Lauf, liest sie
  einmal und prüft genau diese Bytes schnell; `Excelstartbefund` hält Wahl, Befund, Bytes und die Fehler als
  `Berichtsmeldung` („Excel-Vorlage: …“, `BV_XL_START_PUNKT`). `ErzeugeExcelLauf(daten, konfig, start, ohneVorlage)`
  füllt die geprüften Bytes, wenn der Befund zur Wahl passt; `ohneVorlage` erzeugt die Mappe ohne Vorlage und nennt es
  (`BV_XL_LAUF_OHNE_GEWAEHLT`).
- **Hülle:** `BerichtsvorlagenGaben.Stand()` prüft die Excel-Vorlage bei Ausgabe Excel oder Beide mit; die erweiterte
  Rückfrage (`Rueckfrage(start, excel)`) nimmt ihre Fehler unter die der Word-Vorlage. Wege: „Mit meiner Vorlage“ füllt
  beide gewählten Vorlagen; der zweite Weg („Mit Standardvorlage“, allein für Excel „Ohne Excel-Vorlage“) nimmt die
  Standardvorlage, wenn die Word-Vorlage befragt ist, und erzeugt die Mappe ohne Vorlage; „Abbrechen“. „Mit meiner
  Vorlage“ steht, solange jede befragte Vorlage lesbar ist. Der Lauf (`BerichtSeiteGaben`) holt den gehaltenen Befund
  (`ExcelStartFuerLauf`), `WegExcel` übersetzt die Antwort; ohne Antwort gehen die Befunde in die Laufmeldung. Galt die
  Rückfrage allein der Excel-Vorlage, bleibt Word bei seiner Vorlage.
- **Tests:** `BerichtsvorlagenHuelleTests` (Vorprüfung und Weg im Kern, Rückfrage bei Ausgabe Excel, Beide und Word,
  Lauf auf beide Antworten), bunit `BerichtSeiteVorlagenTests.Die_Rueckfrage_der_Excel_Vorlage_nimmt_denselben_Weg`.
  Die Seite selbst blieb unverändert — sie zeigt jede Rückfrage der Hülle.

## 6 Entscheidungen

| Nr. | Entscheid | Herkunft |
|---|---|---|
| **BV-E7-1** | Ein gleichnamiges Anwenderblatt wird zu „X (Vorlage)“ umbenannt, mit Warnung | Orchestrator nach Empfehlung |
| **BV-E7-2** | Ein Markenblatt mit Inhalt wird ersetzt, mit Warnung | Orchestrator nach Empfehlung |
| **BV-E7-3** | Fehler der Excel-Vorlage stehen in der erweiterten Startrückfrage | Anwender 26.09.2026, umgesetzt im Nachzug |
| **BV-E7-4** | Die mitgelieferten Word-Vorlagen bleiben auf Katalogfassung 4 (`KatalogfassungWord`) | Orchestrator nach Empfehlung |
| **BV-E7-5** | Ein leerer Wert ergibt eine leere Zelle | Orchestrator nach Empfehlung |

**Nicht gebaut (BV-E8 oder später):** Menü „…“ für Excel-Vorlagen, Bedienung der Vorgabe `BerichtVorlageExcel`, „Neue
Excel-Vorlage…“, Excel-Tabellen, erzeugte Bereiche, Diagramme, Namen `EPOS.reihe.*`, Excel-Baukasten, Anhang-E-Stelle
als Blatt und Zelle, Häkchen auf die Blätter der Vorlage (BV-Q2 c).

## 7 Abnahme

**Tests der Etappe:** `ExcelVorlagenfuellerTests` (14), `ExcelVorlagenprueferTests` (7), `BerichtsvorlagenHuelleTests`
(3 aus BV-E7, 3 aus dem Nachzug), Formelergebnisse im Vorlagenweg (1), `BerichtSeiteVorlagenTests`,
`VorlagenfeldkatalogWacheTests` (Fassung 5, `KatalogfassungWord`), `KiMaskenabdeckungWacheTests`,
`VorlagenfeldanzeigeHuelleTests`.

**Gate auf `7d87c958`:** siehe Statuszeile #549 (Kern-Filter Release und Windows-Schale je 0 Fehler, voller Testlauf,
Designer, SQL-Prüfer, ChartProben, Referenzlauf der sechs Projekte gegen die Basis R20 `2026-09-26_R20_Zapfprofil` —
die Basis R21 aus #548 stand zur Abnahme noch nicht auf `origin`). Die Oberfläche der Berichtsseite ist nur
über die Hülle berührt (keine neue Marke, kein Raster) — Markenprobe und Rasterprobe nicht nötig.

## 8 Commitfolge

| Commit | Inhalt |
|---|---|
| `dcd8d0a1` | Excel-Vorlagenfüller, Prüfer, Paketschutz und Katalog v5 |
| `ad489014` | Zeile „Excel-Vorlage“, Hülle, Lauf und Dateifilter `.xltx` |
| `9db43194` | Tests Rückfall der Mappe und Formelergebnisse im Vorlagenweg |
| `5e9248b3` | Merge BV-E7 (`.resx` Vereinigung, Designer neu, Blattmarken in der Platzhalteranzeige) |
| `e4851cac` | Excel-Befunde in der Startrückfrage (BV-E7-3) |
| `7d87c958` | Merge `origin/ios_migration_september` vor der Abnahme (konfliktfrei) |
| Papier-Commit | „Papiere #549: BV-E7 Excel-Rahmen“ — dieses Protokoll, Konzept Rev. 9, Statusdatei, Index, Wiki-Quelle |

## 9 Offen

- **(a) Anwenderprobe** unter Windows: eigene Mappe mit Platzhaltern, Namen, Blattmarken, Musterblatt; Aptos-Vorlage.
- **(b) iOS- und Setup-Lauf:** vom Anwender am 26.09.2026 freigegeben, nach dem Push zu starten — Kennungen und Ergebnis
  in der Statuszeile nachtragen.
- **(c) BV-E8:** Excel-Tabellen, erzeugte Bereiche, Diagramme, `EPOS.reihe.*`, Excel-Baukasten, Anhang-E-Stelle als
  Blatt und Zelle.
- **(d)** Menü „…“ für Excel-Vorlagen, „Neue Excel-Vorlage…“, Bedienung der Vorgabe `BerichtVorlageExcel`.
- **(e) Wiki-Upload** der Seite „Berichtsvorlagen“ (Abschnitt „Excel-Vorlage“) und Logbuch.

**Logbuch-Vorschlag** (für den nächsten Wiki-Upload, Version beim Anwender zu erfragen, Stichwort `bericht`):

- „Die Excel-Mappe des Berichts kann aus einer eigenen Excel-Vorlage mit Platzhaltern und Blattmarken entstehen.“
