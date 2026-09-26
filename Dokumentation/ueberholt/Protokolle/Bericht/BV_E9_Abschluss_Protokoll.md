# BV-E9 — Abschluss der Berichtsvorlagen (Protokoll)

Letzte Etappe des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md), das mit
diesem Auftrag nach `ueberholt/` wandert (Anwenderentscheid „Jetzt mit BV-E9“). Auftrag #566, Anwenderaufträge vom
26.09.2026: „führe aus BV-E9“ (alle vier Posten), „die Berichtsvorlage soll zukünftig mit ausgeliefert werden und an den
aktuellen Stand angepasst werden“ — „sowohl word als auch excel“, „auch excel soll eine Vorlage sein mit allen
Konfigurationselementen als Vorlage“ —, „die excel vorlage für den Bericht sollte auch die detaillierten Komponente
enthalten damit der Benutzer diese Vorlage ändern kann“, dazu zwei Befunde des Anwenders aus der Probe: der abgeschnittene
Titel im Wirtschaftlichkeitsbild des Word-Exports und der Abbruch des Excel-Berichts im Ordner „Dokumente“. Der gültige
Stand steht in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md), in
[`Werkzeuge/Berichtsvorlage/LIESMICH.md`](../../../../Werkzeuge/Berichtsvorlage/LIESMICH.md) und in der Wiki-Quelle
„Berichtsvorlagen“; hier steht, wie es geworden ist. Vorgänger:
[`BV_E8_Excel_Diagramme_Protokoll.md`](BV_E8_Excel_Diagramme_Protokoll.md).

Zweig `claude/intelligent-bohr-hthrk8`, umgesetzt am 26.09.2026. Opus 5.5 hat orchestriert, zusammengeführt und die
Papiere geschrieben; sieben Opus-5.5-Agenten haben in eigenen Worktrees gearbeitet (ausführliche Word-Vorlage,
Bausteinvorlage, Sprache aus der Vorlage, Positionsadressierung, Excel komplett, Diagrammtexte im Word-SVG, Excel-Vorlage
aus Einzelelementen; der Excel-Agent wurde angehalten und vom Orchestrator zu Ende geführt, der Agent der Einzelelemente
nach dem Nutzungslimit fortgesetzt). Kein Schemaschritt, kein Rechenweg berührt (Referenzlauf gegen
`2026-09-26_R22_Solarthermie` GESAMT: PASS), keine Referenzbasis neu eingefroren. `EPOS.iOS/` nur im Lieferweg der
Vorlagen (`MauiAsset`) berührt; ein iOS-Lauf hat der Anwender zurückgestellt.

| Teil | Gegenstand | Commits |
|---|---|---|
| Ausführliche Word-Vorlage (BV-E8-4) | `Berichtsvorlage_Ausfuehrlich(_en).docx` aus Einzelelementen, Werkzeug `ausfuehrlich`, Rundlauf, Lieferwege | `1d4d1d85`, `57fb6f9f`, `98b79f27`, `d53f284a` |
| Sprache aus der Vorlage (BV-Q7 b) | Lauf, Startrückfrage und Seitenlauf in der Sprache der Vorlage | `173daa71`, `66bec8df`, `fda950d8`, `bf7a7367` |
| Bausteinvorlage | `Berichtsvorlage_Bausteine(_en).dotx`: jeder Platzhalter als Schnellbaustein, Werkzeug `bausteine` | `ad8b9217`, `911461d1`, `e408ad3c` |
| Positionsadressierung (BV-Q10 b) | `stand.<n>.*`, `variante.<n>.*` als Muster, Prüferhinweis, vier Word-Nachzüge | `928b8f00`, `217a3e71` |
| Diagrammtexte im Word-SVG | Grundlinie ausgerechnet statt `dominant-baseline`, Fußnote umgebrochen, „Jahr“ im Bild | `1b6dbe82`, `e7f5854f` |
| Excel komplett | ausführliche Excel-Vorlage, Excel-Baukasten, drei reine Excel-Tabellen, Excel-Zeile mit „Neue Excel-Vorlage…“ und Export, Sammelbefehl `alle`, Aktualitätswache | `ba998e07`, `fbceaf09`, `38ebaaa7` |
| Excel-Vorlage aus Einzelelementen | Nachbildung der Blätter des Standardberichts, Katalog v9, `EPOS.Blattanhang` | `7d4b59b1`, `580414f2`, `69e186da`, `fd13fdc9` |
| Gesperrter Ordner | benannte Meldung für Zielordner, Vorlagenordner und Muster | `c1d1ef94` |
| Zusammenführung | Merges, Katalogfassung 7/8/9, Ressourcen, Tests | `0f9a99e1`, `84fbf9bd`, `bb1dc2bd`, `8027df5c`, `79cce7cf`, `ff4d52ec`, `b5af0e4b`, `79ce130d`, `43aafa8c` |

## 1 Mitgelieferte Vorlagen

Ausgeliefert werden in beiden Lieferwegen (Windows `WindowsFormsApplication1.csproj` und Setup, iOS `MauiAsset`) und im
Unterordner „Mitgeliefert“ des Vorlagenordners bereitgestellt:

| Datei | Rolle |
|---|---|
| `Berichtsvorlage_Standard.docx` | Standardvorlage (Kapitel) |
| `Berichtsvorlage_Kurzbericht(_en).docx` | Kurzbericht je Sprache |
| `Berichtsvorlage_Ausfuehrlich(_en).docx` | der Bericht aus Einzelelementen je Sprache — Ausgangspunkt für eine eigene Word-Vorlage |
| `Berichtsvorlage_Bausteine(_en).dotx` | Dokumentvorlage mit jedem Platzhalter als Schnellbaustein, Kategorien nach Kontext |
| `Berichtsvorlage_Excel_Standard.xlsx` | Excel-Standardmappe mit Blattmarken (im Code erzeugt) |
| `Berichtsvorlage_Excel_Ausfuehrlich(_en).xlsx` | alle Konfigurationselemente einer Excel-Vorlage und die Blätter des Standardberichts aus Einzelelementen, erläutert in Zellnotizen |
| Baukasten Word und Excel je Sprache | aus dem Katalog erzeugt |

Der Sammelbefehl `alle <vorlagenordner>` des Werkzeugs erzeugt alle zehn Vorlagendateien aus dem aktuellen Katalog neu
(Beispiel, Standard, Kurzbericht, ausführliche Vorlage, Bausteinvorlage, ausführliche Excel-Vorlage); ein zweiter Lauf
ist byte-gleich. Die Wache `AuslieferungsvorlagenAktualitaetWacheTests` zieht dieselben Schritte und meldet jede
veraltete Datei mit dem Befehl zum Neuerzeugen — damit ist „an den aktuellen Stand angepasst“ eine geprüfte Eigenschaft.

## 2 Excel-Vorlage aus Einzelelementen

Die ausführliche Excel-Vorlage baut die Blätter des Standard-Excelberichts nach (Anwenderentscheid „Ausführliche
erweitern“): Projektübersicht (Kopfwerte, `tabelle.varianten`, `tabelle.komponenten.matrix`, Speichertemperaturen),
Variantenvergleich (neu `tabelle.vergleich.liste`, vier Vergleichsbalken), Kapitalwertverlauf
(`tabelle.wirtschaft.verlauf`), Musterblatt je Stand (neu `stand.tabelle.kennzahlen.liste`, Erzeuger, Brennstoffmengen,
Monatswerte als Excel-Tabelle, vier Ganglinien, Deckungskreise, Tafeln der Wirtschaftlichkeit, Zahlungsstrom). Nicht aus
Einzelelementen erreichbar und deshalb als Blattmarke mit Zellnotiz geblieben: die Formelmappe der Wirtschaftlichkeit
(lebende Formeln auf die reservierten Namen; dieselben Zahlen als Werte stehen auf „Auswertung“), die Checkliste
Anhang E (ihre Stellen entstehen erst aus der gefüllten Mappe) und die Diagrammdaten.

## 3 Katalogfassungen

| Fassung | Inhalt | Ausgabe |
|---|---|---|
| 7 | `tabelle.wirtschaft.parameter`, `tabelle.wirtschaft.verlauf`, `stand.tabelle.monatswerte` | nur Excel |
| 8 | Positionsmuster `stand.<n>.*`, `variante.<n>.*` (nicht aufgezählt, v8 = v7) | Word und Excel |
| 9 | `tabelle.vergleich.liste`, `stand.tabelle.kennzahlen.liste` | nur Excel |

Zwei Agenten hatten beide Fassung 7 belegt; beim Zusammenführen blieb 7 bei den Excel-Tabellen, die Positionsmuster
bekamen 8. `KatalogfassungWord` bleibt 4 — keine Word-Vorlage musste neu entstehen.

## 4 Befunde des Anwenders

- **Titel im Word-Diagramm abgeschnitten:** Word zeigt das eingebettete SVG und kennt `dominant-baseline` nicht; jeder
  Text stand um seinen Aufstieg zu hoch. Das Druck-SVG rechnet die Grundlinie jetzt aus der Schriftmetrik des PNG-Malers
  (`SkiaMaler.Drucksvg`); die Oberfläche behält ihr SVG. Dazu bricht die Fußnote der Verlaufsbilder um, und „Jahr“ liegt
  ganz im Bild (20 ChartProben-Bilder neu eingefroren, die Messlatten der Wortberichte um die höhere Szenarienzeile
  nachgezogen). Nachgewiesen mit `rsvg-convert` und Chromium gegen das PNG.
- **Excel-Bericht bricht ab:** `UnauthorizedAccessException` beim Anlegen des vorhandenen Ordners „Dokumente“ — der
  Überwachte Ordnerzugriff von Windows sperrt das Programm. Ein vorhandener Zielordner wird nicht mehr angelegt; ein
  verweigerter Zugriff wird als „Zielordner … nicht beschreibbar“ mit dem Weg zur Freigabe gemeldet, ebenso beim
  Bereitstellen der Muster und beim Anlegen eigener Vorlagen (`OrdnerGesperrtException`). Abhilfe beim Anwender:
  `EPOS_Plan.exe` als zulässige App eintragen.
- **Diagramme fehlten in der Excel-Datei:** Der Installer des Setup-Laufs war vor BV-E8 gebaut; der Code war richtig.

## 5 Entscheidungen

| Kennung | Inhalt | Quelle |
|---|---|---|
| BV-E8-4 | Die Standardvorlage bleibt; der Bericht aus Einzelelementen ist eine dritte Word-Vorlage | Anwender |
| BV-Q7 b | Der Lauf nimmt die Sprache der Vorlage; tragen Word- und Excel-Vorlage verschiedene Sprachen, gilt Word | Anwender |
| BV-Q10 b | `stand.<n>` zählt ab 1 in der Folge der Stände (1 = Stamm), `variante.<n>` nur die Varianten; kein `stand.0` | Anwender |
| BV-E9-1 | Die Excel-Vorlage mit allen Konfigurationselementen wird ausgeliefert, Word und Excel aktuell gehalten (Sammelbefehl und Wache) | Anwender |
| BV-E9-2 | Die ausführliche Excel-Vorlage bildet die Blätter des Standardberichts aus Einzelelementen nach | Anwender („Ausführliche erweitern“) |
| BV-E9-3 | Dateieigenschaft `EPOS.Blattanhang = nein`: erzeugte Blätter ohne Blattmarke entfallen statt angehängt zu werden; die Diagrammdaten kommen immer dazu | Anwender („Ja, so übernehmen“) |
| BV-E9-4 | Ein gesperrter Ordner wird benannt, mit dem Weg zur Freigabe | Anwender |
| BV-E9-5 | Die Bausteinvorlage (`.dotx`) liegt im Musterordner, nicht unter „Neue Vorlage…“ — der Bericht braucht eine `.docx` | Orchestrator |

## 6 Abnahme

Gate auf dem Stand nach dem letzten Merge von `origin` (siehe Statuszeile #566): Kern-Filter und Windows-Schale
0 Fehler, Designer wiederholbar, SQL-Dialekt-Prüfer ohne Fundstelle, ChartProben 220 Bilder ohne Verstoß, Referenzlauf
1030, 1007, 1017, 1045, 1046, 1047, 1049 gegen R22 GESAMT: PASS, voller Testlauf ohne Fehler bis auf
`TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar` (Python 3.11 im Container, auf der CI grün). Sammelbefehl
`alle` zweimal: zehn Vorlagen aktuell, byte-gleich.

## 7 Offen

Siehe „Nach #566“ in der Statusdatei.
