# BV-E8 — Excel-Diagramme, Tabellen und Reihennamen (Protokoll)

Etappe BV-E8 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitte 4.4, 5.4, 5.6, 7.3, 7.4 und 13). Auftrag #558, Anwenderauftrag vom 26.09.2026: „In die Excel-Berichtsausgabe
sollen die Grafiken (als Excel-Grafik mit Daten) aufgenommen werden“. Der gültige Stand steht im Konzept (Rev. 11) und in
der [Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier steht, wie es geworden ist. Vorgänger:
[`BV_E7_Excel_Rahmen_Protokoll.md`](BV_E7_Excel_Rahmen_Protokoll.md) und
[`BV_E7_6_Muster_Protokoll.md`](BV_E7_6_Muster_Protokoll.md). Zweig `claude/intelligent-bohr-hthrk8`; der Agentenzweig
`worktree-agent-a9d1668864254bebe` ab `0f553dc5`, umgesetzt am 26.09.2026. Opus 5.5 hat orchestriert, ein Agent
(Opus 5.5) hat im eigenen Worktree Diagramme, Tabellen, Reihennamen und Tests gebaut (3 Commits, 31 Dateien,
+5.895/−114 Zeilen), ein Abschluss-Agent hat zusammengeführt, das Gate gezogen und die Papiere geschrieben. Kein
Schemaschritt, kein Rechenweg berührt (Referenzlauf GESAMT: PASS), keine Referenzbasis neu eingefroren, `EPOS.iOS/`
nicht berührt.

| Teil | Gegenstand | Commits |
|---|---|---|
| Diagramme der erzeugten Blätter | `Diagrammplan`, `Exceldiagramm`, `Exceldiagrammquellen`, `Exceldiagrammschreiber` (`EPOS.Kern/Allgemein/Bericht/Exceldiagramme/`), Blatt „Diagrammdaten“, Katalog Fassung 6 | `c60c4af3` |
| Vorlagenweg | `Excelbereiche`, `Excelreihen`, `Vorlagendiagramme`; Tabellen, Bildplatzhalter, Namen `EPOS.reihe.*`, Nachzug von Anwenderdiagrammen; Prüfer | `f155fe86` |
| Anzeige | Anzeigewache zählt sieben Blattmarken | `2dd4e481` |
| Abschluss | Merge, Gate, Papiere | `aa883210`, `cc353a60`, `f35192f8`, `8ae10fba` |

## 1 Diagramme der erzeugten Blätter

Die Standardmappe trägt — mit und ohne Vorlage — je Berichtsbild ein natives Excel-Diagramm auf dem Blatt seines
Kapitels, rechts neben dem Inhalt untereinander; die bestehenden Zellen bleiben unverändert (Entscheid BV-E8-2). Die
Diagramme entstehen immer, ohne eigenes Häkchen (Entscheid BV-E8-1).

| Blatt | Diagramm | Form |
|---|---|---|
| Übersicht | Speichertemperaturen (`stamm.bild.speichertemperaturen`) | Linien |
| Vergleich | vier Balken (`bild.vergleich.balken.*`: Brennstoff, Netzbezug, Wärmerest, JAZ) | Balken |
| Vergleich | Deckung Wärme und Strom je Stand (`stand.bild.deckung_waerme`, `.deckung_strom`) | Kreis |
| Detailblatt je Stand | Jahresverlauf Wärme, 365 Tage | gestapelte Fläche mit Bedarfslinie |
| Detailblatt je Stand | Dauerlinie, 8760 Stunden in voller Auflösung (Entscheid BV-E8-3) | Linie |
| Detailblatt je Stand | Strombilanz, 12 Monate | gestapelte Säulen mit Linien; Einspeisung gestrichelt |
| Detailblatt je Stand | Speicherverlauf, 3 × 168 Stunden | Linien |
| Wirtschaftlichkeit | Kapitalwert-Szenarien, Barwerte kumuliert | Linien |
| Wirtschaftlichkeit | Brücke | Schwebesäulen über Hilfsspalten |
| Wirtschaftlichkeit | Spanne | Schwebebalken über Hilfsspalten |
| Wirtschaftlichkeit | Zahlungsstrom je Stand | gestapelte Säulen |

Die Zahlen stehen im neuen Blatt **„Diagrammdaten“** am Ende der Mappe (Blöcke nebeneinander: Titel, Bezug, Kopfzeile,
Zahlen; fester Name in jeder Sprache, Blattmarke `blatt.diagrammdaten`). Die Reihenbezüge zeigen auf diese Zellen, der
Zwischenspeicher jedes Diagramms trägt dieselben Zahlen, damit Vorschauen ohne Rechenwerk richtig zeigen. Die Reihen
entstehen aus denselben Bauwegen wie das Wortbild (Reihen-Helfer des `ChartRenderer`, `Berichtsbilder.BrueckeDaten`) —
gleiche Zahlen wie im Word-Bericht. ChartProben bleiben byte-gleich.

## 2 Vorlagenweg: Tabellen, Bilder, Reihennamen

- `{{tabelle.*}}` allein in einer Zelle wird ein **erzeugter Bereich**; eine listentaugliche Tabelle wird zugleich
  Excel-Tabelle `EPOS_<schlüssel>`. Eine Excel-Tabelle `EPOS_<schlüssel>` der Vorlage wird gefüllt und wächst.
- `{{bild.*}}` bzw. `{{stand.bild.*}}` allein in einer Zelle wird das Excel-Diagramm an dieser Zelle, auf dem
  Musterblatt `blatt.detail` je Stand.
- Namen **`EPOS.reihe.*`** zeigen auf Rasterreihen des Stammprojekts im Blatt „Diagrammdaten“: Monatsreihen als Summen
  in MWh, Temperatur und Füllstand als Mittelwerte.
- **Diagramme der Anwendervorlage**, die auf gewachsene Tabellen zeigen, zieht EPOS über das SDK nach und trägt ihren
  Zwischenspeicher neu ein.
- **Katalog Fassung 6:** `blatt.diagrammdaten` (2.085 Schlüssel und 3 Aliasse, `Vorlagenfeldkatalog_v6.txt`); die 16
  Bildschlüssel haben Ausgabe Word und Excel; `KatalogfassungWord` bleibt 4.
- **Prüfer:** Tabellen und Bilder allein in einer Zelle sind zugelassen; er prüft `EPOS_`-Tabellen und Reihennamen und
  meldet eine Tabelle je Stand als Excel-Tabelle (nur Zellmarke auf dem Musterblatt).

## 3 Messbefunde

- **Reihenfolge:** ClosedXML schreibt die Blätter, danach trägt `Formelregister.Nachtragen` die Formelergebnisse nach,
  zuletzt legt das OpenXML SDK die Diagramme auf dem gespeicherten Paket an — ClosedXML verliert Diagramme beim erneuten
  Laden nicht, legt aber keine an.
- **`InsertRowsBelow` verschiebt Diagrammbezüge nicht:** Deshalb zieht EPOS die Bezüge von Anwenderdiagrammen auf
  gewachsene Tabellen selbst über das SDK nach.
- **Rundlauf:** Die Mappe übersteht Laden und Speichern mit ClosedXML verlustfrei (Diagramme, Bezüge, Zwischenspeicher).
- **Zahlen auf 17 Stellen:** ClosedXML schreibt Gleitkommazahlen mit 17 signifikanten Stellen; die Tests vergleichen
  Zwischenspeicher und Zellen deshalb relativ mit 1e-9.
- **Größe:** Die Beispielmappe der Gruppe 1019 misst mit der Dauerlinie in voller Auflösung rund 1,9 MB.

## 4 Zusammenführung

Merge `aa883210` in den Stand `64466938` (mit #556): Konflikt nur in `Resource.resx` und `Resource.en-US.resx` am
Dateiende — beide Seiten je Schlüssel vereinigt, dreiseitig gegen die Basis geprüft (keine Abweichung), UTF-8 mit BOM
und LF wie im Bestand; `Resource.Designer.cs` mit `designer_neu.py schreiben` bestätigt (+0). Katalog, Katalogwache,
Messlatten `Bericht_Excel_1030`/`_Gruppe` und `BerichtBlattstrukturWacheTests` hat `origin` seit der Basis nicht
geändert — sie tragen allein die Zusätze von BV-E8. Die Muster-Tests aus #556 (`BerichtsvorlagenMusterTests`) bleiben
mit der siebten Blattmarke grün: sie zählen die Blätter der Excel-Standardmappe über `ExcelVorlagenmappe.Blattmarken`;
der Kommentar an `ExcelVorlagenfueller.Standardmappe()` nennt jetzt sieben Blätter. `origin/ios_migration_september`
dreimal konfliktarm nachgezogen (`cc353a60` mit #553 und neuer Testdatenbank, `f35192f8` mit G6c und dem
Wiki-Sammel-Upload, `8ae10fba` mit #557); dabei trat die Nummer #556 doppelt auf (Wiki-Sammel-Upload und BV-E7-6): der
origin-Merge `8034733c` hatte die Statuszeile von BV-E7-6 verloren, sie steht wieder neben der des Wiki-Sammel-Uploads.

## 5 Entscheidungen

| Nr. | Entscheid | Herkunft |
|---|---|---|
| **BV-E8-1** | Die Excel-Diagramme entstehen immer, ohne eigenes Häkchen | Anwender 26.09.2026 |
| **BV-E8-2** | Die Diagramme stehen auf den Kapitelblättern, rechts neben dem Inhalt | Anwender 26.09.2026 |
| **BV-E8-3** | Die Dauerlinie steht in voller Auflösung (8760 Punkte) | Anwender 26.09.2026 |

**Abweichungen vom Konzept:** Die Zahlen stehen im Blatt „Diagrammdaten“ statt auf den Tabellenbereichen der Blätter —
so bleiben die bestehenden Blätter Zelle für Zelle gleich; die Einspeisung der Strombilanz ist eine gestrichelte Linie;
das Ersatzjahr-Band und die Beschriftungen an Brücke und Spanne sind nicht übertragen; die Platzhalteranzeige der App
nennt für `tabelle.*` noch „EPOS.<schlüssel>“.

**Nicht gebaut:** Excel-Baukasten, Anhang-E-Stelle als Blatt und Zelle, die drei Tabellen mit reiner Excel-Quelle
(`tabelle.wirtschaft.parameter`, `tabelle.wirtschaft.verlauf`, `stand.tabelle.monatswerte`), `EPOS_`-Tabellen je Stand.

## 6 Abnahme

**Tests der Etappe:** `ExcelDiagrammeTests` (6: 1030, Gruppe 1019, Gruppe mit drei Ständen, Standardmappe als Vorlage,
Bild- und Tabellenplatzhalter, Anwenderdiagramm auf `EPOS_`-Tabelle und Reihennamen) mit dem Prüfwerkzeug
`Exceldiagrammbefund` (Validator, Reihenbezüge gegen Zellen, Zwischenspeicher gegen Zahlen, Rundlauf),
`ExcelVorlagenprueferTests`, `ExcelVorlagenfuellerTests`, `WordVorlagenBilderTests`, `VorlagenfeldkatalogWacheTests`
(Fassung 6), `BerichtBlattstrukturWacheTests` (acht Blätter mit „Diagrammdaten“), Messlatten 1030 und Gruppe.

**Gate:** siehe Statuszeile #558 — Gate auf `8ae10fba` (Merge BV-E8 `aa883210` samt origin-Merges `cc353a60`, `f35192f8`, `8ae10fba`) — Kern-Filter Release und Windows-Schale (Linux, `EnableWindowsTargeting`) je 0 Fehler; Tests EPOS.Kern 8.401 bestanden / 1 übersprungen / 1 rot (`TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar` — Umgebung, wie in #549, #556), EPOS.UI 6.727, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 / 1 übersprungen; Designer wiederholbar (12.618 Einträge, +0); SQL-Prüfer 1.997 Texte / 0 Fundstellen; ChartProben 220 Bilder / 0 Verstöße, byte-gleich; Referenzlauf 6 Projekte (1030, 1007, 1017, 1045, 1046, 1047) gegen R21 `2026-09-26_R21_BhkwDeckung` GESAMT: PASS, 2.208.587 Werte (dasselbe Gate auch auf `aa883210`, `cc353a60` und `f35192f8` grün). Die Oberfläche der Berichtsseite ist nicht berührt — Marken- und Rasterprobe nicht nötig.

**Beispielmappe:** Gruppe 1019 aus dem zusammengeführten Stand über
`ExcelDiagrammeTests.Mappe_1019_traegt_die_Diagramme_des_Wortberichts_mit_Daten` mit `EPOS_BVE8_BEISPIEL` erzeugt
(`Bericht_1019_final.xlsx`, dem Anwender übergeben). Die Sichtprobe in Excel steht aus; LibreOffice lädt im Container
nicht.

## 7 Commitfolge

| Commit | Inhalt |
|---|---|
| `c60c4af3` | Excel-Diagramme der erzeugten Blätter mit Daten, Blatt „Diagrammdaten“, Katalog v6 |
| `f155fe86` | Tabellen, Bildplatzhalter, Reihennamen und Anwenderdiagramme in Excel |
| `2dd4e481` | Anzeigewache zählt die siebte Blattmarke |
| `aa883210` | Merge BV-E8 (`.resx` Vereinigung, Designer bestätigt) |
| `cc353a60`, `f35192f8`, `8ae10fba` | Merges `origin/ios_migration_september` (#553, G6c und Wiki-Upload, #557); im zweiten die Statuszeile #556 (BV-E7-6) wiederhergestellt |
| Papier-Commit | „Papiere #558: BV-E8 Excel-Diagramme“ — dieses Protokoll, Konzept Rev. 11, Statusdatei, Index, Wiki-Quelle, Kommentar `Standardmappe()` |

## 8 Offen

- **(a) Sichtprobe in Excel** der Beispielmappe `Bericht_1019_final.xlsx`: Diagrammtypen, Farben, Achsen, Legenden.
- **(b)** Anhang-E-Stelle als Blatt und Zelle, Excel-Baukasten, die drei Tabellen mit reiner Excel-Quelle,
  `EPOS_`-Tabellen je Stand (BV-E9 bzw. Nachzug).
- **(c)** Platzhalteranzeige: `tabelle.*` auf Zellmarke bzw. `EPOS_<schlüssel>` umstellen.
- **(d)** Einspeisung der Strombilanz, Beschriftungen an Brücke und Spanne.
- **(e) Wiki-Upload** der Seite „Berichtsvorlagen“ (Abschnitt „Excel-Vorlage“, Diagramme) und Logbuch.

**Logbuch-Vorschlag** (für den nächsten Wiki-Upload, Version beim Anwender zu erfragen, Stichwort `bericht`):

- „Der Excel-Bericht enthält die Diagramme des Word-Berichts als Excel-Diagramme mit ihren Daten.“
