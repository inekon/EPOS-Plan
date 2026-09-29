# Berichtsvorlagen — die Häkchen wirken auf die Excel-Vorlage, Entscheid BV-Q2 (c) (Protokoll)

Statuszeile #605. Umsetzung des Anwenderentscheids **BV-Q2 (c)** („Häkchen in Excel: wirken auf die Blätter der
Excel-Vorlage“; Entscheidtabelle des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md), Zeile
BV-Q2: „(a) bis BV-E7, danach (c)“). Vorgänger: [`BV_E7_Excel_Rahmen_Protokoll.md`](BV_E7_Excel_Rahmen_Protokoll.md),
[`BV_E8_Excel_Diagramme_Protokoll.md`](BV_E8_Excel_Diagramme_Protokoll.md),
[`BV_E9_Abschluss_Protokoll.md`](BV_E9_Abschluss_Protokoll.md), [`BV_Reste_Protokoll.md`](BV_Reste_Protokoll.md). Der
gültige Stand steht im Code und in der Wiki-Quelle „Berichtsvorlagen“; hier steht, wie es geworden ist. Ein
Opus-5-Agent im eigenen Worktree, 29.09.2026; Zusammenführung, Gate und Statuszeile macht die Orchestrierung.

Kein Schemaschritt, kein Rechenweg, keine Referenzbasis. **Kein neuer Platzhalter, keine neue Blattmarke, keine neue
Katalogfassung, kein neuer Ressourcenschlüssel**; die mitgelieferten Vorlagen sind unverändert, die Messlatten
`Bericht_Excel_1030.txt`, `Bericht_Excel_Gruppe.txt` und alle `Bericht_Word_*.txt` byte-gleich.

| Teil | Gegenstand | Commit |
|---|---|---|
| Kern, Oberfläche, Tests | Blatt ↔ Häkchen, Blattstand der Excel-Vorlage, Füller, Ausgegraut-Regel | `407b1734` |
| Papiere | Wiki-Quelle, dieses Protokoll, Indexzeile | `98a61a75` |
| Nachzug | der Hüllentest der Excel-Zeile hakt die Blätter an, die er misst | `ca3134a1` |

## 1 Befund: wie die Häkchen vorher wirkten

**Word (BV-Q1 c, gebaut mit BV-E2).** Die Schnellprüfung der Word-Vorlage legt in `Pruefbefund.Bausteine`, welche
Häkchen die Vorlage führt: die Bausteine, deren Kapitelplatzhalter `{{kapitel.<name>}}` an gültiger Stelle steht, oder —
über den Sammelanker `{{bericht.inhalt}}` bzw. bei einer Vorlage ohne jeden Platzhalter — alle. `HatKapitel` sagt, ob
sie überhaupt Kapitel führt, `DeckblattAusPlatzhaltern`, ob sie ihr Deckblatt selbst trägt. Die Hülle macht daraus den
`Kapitelstand` (`BerichtsvorlagenGaben.Kapitel`), die Berichtsseite graut damit die Einträge aus
(`BerichtSeite.Ausgegraut`, weiche Sperre mit Grund am Element, Häkchen bleibt gespeichert) oder ersetzt die Liste durch
die leise Zeile „Den Inhalt bestimmt die Vorlage“. Einzelplatzhalter zählen dabei nicht als geführtes Kapitel. Beim
Füllen liefert ein abgewähltes Kapitel den leeren Wert, sein Block und sein Kapitelkopf entfallen.

**Excel (BV-Q2 a, Stand nach BV-E9).** Zwei Häkchen wirkten schon: `ExcelBerichtGenerator.SchreibeBlaetter` legt die
Blätter Wirtschaftlichkeit, Verlauf und Checkliste nur mit `B_WIRTSCHAFT` an, das Detailblatt je Stand nur mit
`B_ERGEBNISSE`; Übersicht und Vergleich entstanden immer. Auf den Diagrammen wirkten zusätzlich `B_PROJEKT` und
`B_VERGLEICH` (`PlaneDiagramme`). Mit einer Excel-Vorlage hört der Füller über den Rückruf `erzeugen` mit: Ein Blatt,
das nicht entsteht, lässt seine Blattmarke entfallen — Markenblatt gelöscht, Hinweis `BV_XL_LAUF_ENTFAELLT`
(„Blattmarke {0} (Blatt „{1}“): kein Inhalt in diesem Bericht – das Blatt entfällt.“). Der Excel-Prüfer lieferte
dagegen `hatKapitel = false` und `bausteine = null`: Die Mappe sagte nichts über die Häkchen, die Oberfläche grante bei
Ausgabe „Excel“ nichts aus und rettete bei „Beide“ jeden Baustein mit `BausteinZeile.InExcel`
(= `!BausteinDef.NurWord`).

**Zuordnung Blatt ↔ Baustein.** Die Blattmarken `blatt.uebersicht`, `.vergleich`, `.wirtschaftlichkeit`, `.verlauf`,
`.detail`, `.checkliste`, `.diagrammdaten` (`ExcelVorlagenmappe.Blattmarken`) benennen je eine `Blattart`. Inhaltlich
trägt das Blatt „Übersicht“ den Projektkopf **und** die Komponentenmatrix, der „Vergleich“ den Variantenvergleich, die
Formelmappe samt Verlauf und Checkliste die Wirtschaftlichkeit, das Detailblatt die Ergebnisse je Variante. Damit ist
jeder Baustein mit `InExcel` genau abgedeckt; die Diagrammdaten gehören keinem.

## 2 Entscheidungsgründe

Das Konzept schweigt zu drei Punkten. Gewählt wurde jeweils die Word-nächste Variante:

1. **Was ist der Kapitelplatzhalter der Mappe?** Die Blattmarke. **Was ist der Sammelanker?** Das Anhängen der
   erzeugten Blätter ohne Marke — also jede Vorlage ohne `EPOS.Blattanhang` = `nein`. Eine solche Vorlage bekommt jedes
   erzeugte Blatt hinten angehängt und führt damit jeden Baustein, den die Mappe kennt; das ist der Regelfall und lässt
   die Häkchenliste frei.
2. **Eine Vorlage, die das Anhängen abschaltet, bildet ihre Blätter aus Einzelelementen nach** (so die ausgelieferte
   ausführliche Excel-Vorlage, BV-E9). Die strenge Word-Regel („Einzelplatzhalter zählen nicht“) würde neben ihr
   `Projektbeschreibung`, `Komponenten & Varianten` und `Variantenvergleich` mit „in dieser Vorlage nicht enthalten“
   ausgrauen, obwohl sie diese Blätter sehr wohl führt — eine sichtbar falsche Aussage über eine mitgelieferte Datei.
   Excel kennt keinen Kapitelplatzhalter; ein nachgebildetes Blatt ist allein an seinen Einzelelementen zu erkennen.
   Deshalb zählt in diesem Fall zusätzlich, was das Kapitel des Bausteins deckt (`Vorlagenfeld.Deckt`, dieselbe
   Zuordnung, mit der Word den Anhang E bewertet). Das Ausgrauen ist eine weiche Sperre: Im Zweifel frei zu lassen ist
   der schonende Fehler. Gemessen: Die ausführliche Vorlage führt damit alle fünf Excel-Bausteine (beide Sprachen).
   Ein neuer Platzhalter oder eine neue Blattmarke war dafür **nicht** nötig.
3. **Wo wirkt das Häkchen auf das Blatt?** Nur auf dem Vorlagenweg, im `ExcelVorlagenfueller` über den vorhandenen
   Rückruf `erzeugen`. Der Weg ohne Vorlage (`ExcelBerichtGenerator.Erzeuge`, Rückruf `null`) bleibt Zeile für Zeile,
   wie er war — er ist die eingefrorene Messlatte, und der Entscheid spricht von den Blättern **der Excel-Vorlage**.
   Ohne Excel-Vorlage bleibt deshalb auch die Häkchenliste frei.

Verworfen: die Bindung in `SchreibeBlaetter` selbst (sie hätte den vorlagenlosen Weg bei abgewählter
Projektbeschreibung oder abgewähltem Vergleich sichtbar geändert, ohne dass der Entscheid das verlangt) und eine eigene
Laufmeldung für ein entfallenes Blatt (die vorhandene Art genügt, siehe 4).

## 3 Umsetzung

**Kern.**

- `ExcelBerichtGenerator.Blattbausteine` — je `Blattart` die Häkchen, deren Inhalt sie trägt: Übersicht →
  Projektbeschreibung und Komponenten & Varianten; Vergleich → Variantenvergleich; Wirtschaftlichkeit, Verlauf,
  Checkliste → Wirtschaftlichkeit; Detail → Ergebnisse je Variante; Diagrammdaten → keines.
  `BlattGewaehlt(art, konfig)` ist wahr, solange **eines** davon gesetzt ist (ohne Konfiguration immer).
- `ExcelBlattstand` (neu) — die Regel aus 2: `FuehrtBlaetter(marken, haengtAn)` und `Gefuehrt(marken, haengtAn,
  schluessel)`, dazu `Moegliche` (alle Bausteine außer `NurWord`).
- `ExcelVorlagenpruefer` — merkt in `PruefeMarken` die Markenarten und `OhneBlattanhang` und legt den Blattstand in
  `Pruefbefund.Bausteine` und `HatKapitel`, also in dieselben Felder, aus denen die Hülle den Kapitelstand der
  Word-Vorlage baut; eine unlesbare Vorlage sagt weiter nichts. Die beiden Feldkommentare in `VorlagenprueferTypen`
  nennen die Excel-Lesart.
- `ExcelVorlagenfueller` — der Rückruf `erzeugen` lehnt ein Blatt ab, dessen Häkchen alle fehlen; alles Weitere
  (Markenentfall samt Meldung, Musterblatt, `EPOS.Blattanhang`) bleibt, wie es war.

**Oberfläche.** `Vorlagenstand.ExcelBlattstand` (Typ `Kapitelstand`) und der gleichnamige Parameter der Berichtsseite;
die Hülle baut ihn mit `BerichtsvorlagenGaben.Blattstand` aus derselben Vorprüfung, die schon die Excel-Prüfzeile und
die Startrückfrage speist. Die Regel steht in `BerichtSeite` in zwei Fragen — `FuehrtWord(b)` und `FuehrtExcel(b)` —
und einer Weiche:

| Ausgabe | ausgegraut |
|---|---|
| Word | was die Word-Vorlage nicht führt (wie bisher) |
| Excel | was die Excel-Vorlage nicht führt; ohne Excel-Vorlage nichts |
| Beide | was **weder** die Word- **noch** die Excel-Vorlage führt; ohne Excel-Vorlage führt die Mappe jeden Baustein mit `InExcel` (wie bisher) |

`InhaltAusVorlage` (die leise Zeile statt der Liste) folgt derselben Weiche: bei „Beide“ nur, wenn beide Vorlagen ihren
Inhalt allein bestimmen.

## 4 Laufmeldung

Ein Blatt, das wegen eines abgewählten Bausteins entfällt, meldet die **vorhandene** Art `BV_XL_LAUF_ENTFAELLT` — genau
die Meldung, die `B_WIRTSCHAFT` ohne Häkchen seit BV-E7 bekommt; neu ist nur, dass sie auch Übersicht und Vergleich
treffen kann. Ein Blatt ohne Marke wird still nicht angehängt, wie bisher. Keine neue Meldungsart, kein neuer
Ressourcenschlüssel. Die Prüfliste bleibt unberührt: Sie hängt an der Prüfsumme der Vorlage, nicht an den Häkchen — ein
Befund über die Häkchen stünde dort veraltet, sobald ein Häkchen umgelegt wird.

## 5 Tests

- `EPOS.Kern.Tests/ExcelHaekchenBlattstandTests` (neu, 9 Fälle): die Zuordnung ist vollständig und kreuzt sich mit
  `ExcelBlattstand.Moegliche`; ein Blatt steht, solange eines seiner Häkchen gesetzt ist; die Blattstandregel auf ihren
  drei Größen; der Prüfer an echten Vorlagen — eine Vorlage, die anhängt, führt alles, eine mit
  `EPOS.Blattanhang` = nein nur ihre Marken, eine ohne Marke und ohne Anhang gar keine Blätter, die ausgelieferte
  ausführliche Vorlage alle fünf Bausteine (deutsch und englisch), eine unlesbare nichts; der Füller mit abgewähltem
  Variantenvergleich (Marke entfällt samt Meldung), mit der Übersicht an zwei Häkchen und mit allen Häkchen
  (unveränderte Blattfolge).
- `EPOS.UI.Tests/Seiten/BerichtSeiteHaekchenTests` (6 Fälle ergänzt, 17 grün): Ausgegraut bei „Excel“ allein nach dem
  Blattstand, ohne Excel-Vorlage frei, bei „Beide“ nur was keine der beiden führt, bei „Word“ ohne Wirkung, die leise
  Zeile bei „Excel“, und der Wechsel der Excel-Vorlage über `VorlagenNeuLaden`.
- Messlatten: `Bericht_Excel_1030.txt`, `Bericht_Excel_Gruppe.txt` und alle `Bericht_Word_*.txt` unverändert
  (`BerichtVorlagenMesslatteTests`, `ExcelVorlagenfuellerTests.Standardmappe_fuellt_wie_ohne_Vorlage_…`).
- **Ein bestehender Fall war anzupassen:** `BerichtsvorlagenHuelleTests.Excelzeile_Wahl_Hinzufuegen_Pruefzeile_und_Lauf`
  hakte allein das Deckblatt an und erwartete trotzdem Vergleich und Übersicht in der Mappe. Sein Gegenstand ist die
  Excel-Zeile, nicht die Häkchen; er hakt jetzt Projektbeschreibung und Variantenvergleich mit an und misst weiter
  Blattmarke und Anhängen. Kein weiterer Fall des Bestands hat sich geändert.
- **Gate** (Linux, Release): `WP-Plan.Kern.slnf` 0 Fehler; `EPOS.Kern.Tests` gefiltert auf Excel und Vorlagen 490 grün,
  auf Bericht, Anhang E, Lokalisierung und KI 815 grün, die drei Wachen 35 grün; `EPOS.UI.Tests` vollständig 6 913 grün;
  die Windows-Schale kompiliert (0 Fehler).

## 6 Offene Punkte

- Der Blattstand ist auf Linux gegen die im Code erzeugten Vorlagen gemessen; **wie die gefüllte Mappe in Excel
  aussieht, wenn ein Blatt entfällt** (Bezüge des Anwenders auf ein entfallenes Blatt zeigen dann ins Leere), bleibt
  eine Windows-Sichtprobe. Der Prüfer warnt vor solchen Bezügen schon seit BV-E7 (`BV_XL_PRUEF_BLATT_BEZUG`).
- Eine Excel-Vorlage mit `EPOS.Blattanhang` = nein, die nur einen Teil der Blätter nachbildet, bekommt für die
  nachgebildeten Blätter freie Häkchen, deren Abwahl das nachgebildete Blatt **nicht** leert — dieselbe Lage wie bei
  Einzelplatzhaltern im Wortbericht. Wollte man das ändern, bräuchte es je Blatt eine eigene Marke oder einen
  Blockschalter in Excel; das wäre ein neuer Platzhalter und damit eine neue Katalogfassung.
