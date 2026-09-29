# Berichtsvorlagen — Reste nach #581 (Protokoll)

Statuszeile #592. Abarbeitung der offenen Reste der Berichtsvorlagen, die keinen Anwenderentscheid brauchen: die Positionsform auf der
Wirtschaftlichkeitsseite aus „Nach #581“ (b) und die Feststellung der Unterpunkte aus „Nach #566“, „Nach #558“,
„Nach #556“, „Nach #549“ samt ihren Weiterverweisen („Nach #544“, „Nach #541“ und dahinter „Nach #532“, „Nach #528“,
„Nach #520“, „Nach #512“). Vorgänger: [`BV_E6_Nachzug_Marken_Protokoll.md`](BV_E6_Nachzug_Marken_Protokoll.md) und
[`BV_E9_Abschluss_Protokoll.md`](BV_E9_Abschluss_Protokoll.md). Der gültige Stand steht in der
[Statusdatei](../../../aktuell/Status_iOS_Migration.md) und in der Wiki-Quelle „Berichtsvorlagen“; hier steht, wie es
geworden ist. Ein Opus-5.5-Agent im eigenen Worktree ab `903f9265`, 27. bis 29.09.2026; Zusammenführung, Gate,
Statuszeile und Nach-Block macht die Orchestrierung. Kein Schemaschritt, kein Rechenweg, keine Referenzbasis, kein neuer
Platzhalter, keine Katalogfassung, keine Testdatenbank berührt; `EPOS.iOS/` und die Windows-Schale nicht geändert.

| Teil | Gegenstand | Commit |
|---|---|---|
| Positionsform | Sensitivität, Mehrjahrestafel und Zahlungsstrom der Wirtschaftlichkeitsseite nennen `stand.<n>.…`/`variante.<n>.…` | `8c00fbc1` |
| Baukasten | „Katalog…“ der leisen Zeile mit „Baukasten speichern…“ | `8b765c61` |
| TWW-Wache | `normiert()` des TWW-Testkatalogs mit `math.fsum`, fassungsfest | `866d5936` |
| KI-Feld | `katalogsuche` der Maske Berichtsseite angemeldet | `8a9c81db` |
| Anhang E | unlesbare eigene Vorlage: Bezugszeile „bezogen auf die Standardvorlage“ | `24c0440e` |
| Vorprüfung | Vorlage ohne Platzhalter ist im zweiten Einstieg nicht „ohne Wirtschaftlichkeit“ | `d64c2fa7` |
| Prüfer | Hinweis auf ein Kapitel im Wiederholblock | `dee03725` |
| Papiere | dieses Protokoll, Indexzeile | Papier-Commit |

## 1 Positionsform auf der Wirtschaftlichkeitsseite

Nach dem Muster von #581: Die Hülle bildet je Stand der Gruppe seine Position in der Folge des Berichts
(`VorlagenfeldpositionHuelle.Je`: Stamm = 1, dann die Varianten der Gruppe, gezählt über alle Varianten) und reicht sie
als `WirtschaftlichkeitStand.Vorlagenfeldpositionen` an die Seite.

- **Mehrjahrestafel und Zahlungsstrom** (ValERI-Block 2): Die Seite kaskadiert die Position des Stands, den die
  Klappliste zeigt; die Marken von `stand.tabelle.mehrjahres` und `stand.bild.zahlungsstrom` folgen der Wahl.
- **Sensitivität:** Die Tafel zeigt mehrere Stände; eine Marke je Tafel bleibt die Regel (Konzept 9.4, keine
  Zeilenmarken). Die Hülle legt die Positionen der Stände der Tafel samt Namen in
  `ErgebnisAnsicht.SensitivitaetPositionen`; `Vorlagenfeldknopf` nimmt sie über den neuen Parameter `Positionen`, der der
  kaskadierten Position vorgeht, und nennt vor den Formen jedes Stands seinen Namen. `Vorlagenfeldposition` trägt dafür
  den optionalen `Name`.
- Ortstabelle kommentiert (die drei Schlüssel standen schon darin), Wiki-Quelle „Berichtsvorlagen“ um einen Satz ergänzt.
- Prüfstände: `WirtschaftlichkeitPositionsformTests` (neu), `VorlagenfeldpositionTests` (Liste mit Namen, `Je`,
  Positionsrest der drei Schlüssel), `VorlagenfeldAnzeigewertWacheTests` (Positionen der Gruppen 1019 und 1030 gleich denen
  der einzelnen Seite: 1019 Stand 1, 1023 Stand 2/Variante 1, 1024 Stand 3/Variante 2).

## 2 Befund je Unterpunkt

Stand: **erledigt** (am Code geprüft, mit Beleg), **umgesetzt** (in diesem Auftrag), **Windows** (nur beim Anwender
prüfbar), **Lauf** (iOS- oder Setup-Lauf nach Rückfrage), **Upload** (Wiki-Sammel-Upload beim Anwender), **Entscheid**
(braucht einen Anwenderentscheid), **offen** (mit Empfehlung in Abschnitt 4).

### 2.1 Nach #581

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Windows-Nachweis der festen Bildhöhe | Windows | drei Berichtstests der Gruppe unter Windows |
| (b) | Positionsform Wirtschaftlichkeitsseite | umgesetzt | `8c00fbc1` |
| (c) | `VorgemerkteBilder` | erledigt | `73f8144a` (#581) |
| (d) | Anwenderabnahme der Marken | Windows | wie BV-E6 (a) |
| (e) | Wiki-Upload und Logbuch | Upload | mit #566 |

### 2.2 Nach #566

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Anwenderproben Word/Excel, Schnellbausteine, Sprache, Diagramme | Windows | — |
| (b) | Überwachter Ordnerzugriff | Windows | nur auf einem Rechner mit aktiver Sperre |
| (c) | Setup-Lauf | erledigt | Lauf 36294558005 |
| (d) | iOS-Lauf | Lauf | vom Anwender zurückgestellt |
| (e) | Wiki-Quelle „Berichtsvorlagen“ | Upload | — |
| (f) | Leerzeichen vor „[€]“, Messlattenzeile | erledigt | `01c18538` |
| (g) | weiter aus #558, #556, #549 | siehe 2.3 bis 2.5 | — |

### 2.3 Nach #558

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Sichtprobe in Excel | Windows | Beispielmappe beim Anwender |
| (b) | Anhang-E-Stelle als Blatt und Zelle, Excel-Baukasten, drei reine Excel-Tabellen, `EPOS_`-Tabellen je Stand | erledigt | `ba998e07` (BV-E9): `ExcelVorlagenfueller.SetzeAnhangEStellen`, `ExcelBaukasten`, Katalog v7, Klon des Musterblatts füllt `EPOS_`-Tabellen |
| (c) | Platzhalteranzeige `tabelle.*` → Zellmarke bzw. `EPOS_<schlüssel>` | erledigt | `ba998e07`: `VorlagenfeldanzeigeHuelle.Excel` (`VF_ANZEIGE_EXCEL_TABELLE`, `…_STAND`) |
| (d) | Einspeisung der Strombilanz, Beschriftungen an Brücke und Spanne (dazu das Ersatzjahr-Band des Zahlungsstroms) | offen | benannte Abweichung von BV-E8; nur mit Excel zu beurteilen |
| (e) | Wiki-Upload, Logbuch | Upload | — |

### 2.4 Nach #556 (BV-E7-6)

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Anwenderprobe `Mitgeliefert`, Export, Schreibschutz | Windows | — |
| (b) | Schreibschutz in der App „Dateien“ | Lauf | iPad, iOS-Lauf zurückgestellt |
| (c) | Explorer öffnet nach dem Export ohne Rückfrage | Windows | in der Anwenderprobe beurteilen |
| (d) | Menü „…“ mit Export für Excel-Vorlagen | erledigt | `ba998e07`: `BerichtsvorlagenGaben.ExcelHandlungen`, `HANDLUNG_EXCEL_EXPORTIEREN` |
| (e) | Wiki-Upload, Logbuch | Upload | — |

### 2.5 Nach #549 (BV-E7)

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Anwenderprobe Excel-Vorlage | Windows | — |
| (b) | iOS-Lauf; Setup-Lauf | Lauf; erledigt | Setup 36240375450 |
| (c) | Excel-Tabellen, Bereiche, Diagramme, `EPOS.reihe.*`, Excel-Baukasten, Anhang-E-Stelle | erledigt | BV-E8 (#558) und `ba998e07` |
| (d1) | Menü „…“ für Excel-Vorlagen | erledigt | `ba998e07` |
| (d2) | „Neue Excel-Vorlage…“ | erledigt | `ba998e07`: `NeueExcelVorlageAusMuster`, `ExcelMustereintraege` |
| (d3) | Bedienung der Vorgabe `BerichtVorlageExcel` | offen | ohne Bedienung wie `BerichtVorlageWord` (Nach #512 (g)) |
| (d4) | Häkchen auf die Blätter der Excel-Vorlage (BV-Q2 c) | offen | die Häkchenregel kennt nur die Word-Vorlage (`BerichtSeite.Ausgegraut`) |
| (e) | Wiki-Upload | Upload | — |

### 2.6 Nach #544 (BV-E6)

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Anwenderabnahme Windows und iPad | Windows | — |
| (b) | iOS-Lauf | Lauf | zurückgestellt |
| (c) | Deckungsgrad und JAZ Kälte Dashboard gegen Katalog | offen | eigener Auftrag mit Referenzlauf |
| (d) | Erzeugerbilder und Kacheln Autarkie/Stromspeicher | erledigt | `ef888e71`, `080d8190` (#581, Katalog v10) |
| (e) | „Katalog…“ der leisen Zeile mit Baukasten | umgesetzt | `8b765c61` |
| (f) | Wiki-Upload | erledigt bis BV-E6 | #556, Revision 610; Späteres siehe „Upload“ |
| (g) | Rasterprobe Z6 | — | Bestand, unabhängig von den Berichtsvorlagen |

### 2.7 Nach #541 (BV-E5)

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Anwenderprobe Kurzbericht, Baukasten, Tabellen, Bilder | Windows | — |
| (b) | iOS-Lauf; Setup-Lauf | Lauf; erledigt | Setup 36240375450 |
| (c) | Excel-Seite der Tabellen und Bilder | erledigt | BV-E8 (#558), `ba998e07` |
| (d) | Paarzwillinge für Tabellen und Bilder | kein Handlungsbedarf | Entscheid BV-E5-2 |
| (e) | Wiki-Upload | erledigt bis BV-E6 | #556, Revision 610 |
| (f) | TWW-Wache fassungsfest (Nach #532 (g)) | umgesetzt | `866d5936` |
| (g) | Textnennungen der Messlatte | erledigt | #541 |
| (h) | weiter aus #512, #520, #528, #532 | siehe 2.8 bis 2.11 | — |

### 2.8 Nach #532 (BV-E4)

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Anwenderproben mit Blockvorlagen | Windows | — |
| (b) | Kapitel in einem Wiederholblock, der Prüfer meldet es nicht | umgesetzt | `dee03725` (Hinweis `VF_PRUEF_KAPITEL_IM_BLOCK`) |
| (c) | `hat.*` im Gruppenblock `\|block n` wertet über den ganzen Bericht | Entscheid | Gruppenkontext in `Berichtswerte` wäre neue Semantik |
| (d), (e) | Schalter je Tabelle und Bild; Standardvorlage Fassung 3 | erledigt | #541 |
| (f) | Excel nutzt die neuen Schlüssel, Parameter als Anteil | erledigt | `dcd8d0a1` (BV-E7): Prozentregel 7.1 in `ExcelVorlagenfueller` |
| (g) | TWW-Wache fassungsfest | umgesetzt | `866d5936` |
| (i) | Setup- und iOS-Lauf | — | BV-E4 berührt beides nicht |

### 2.9 Nach #528 (BV-E3)

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) bis (d) | Speichertemperaturbild, Leitversion, Stammfall, Bedarf | erledigt | #532 |
| (e) | `Nachgeholt` nennt die Laufmeldung nicht | offen | neue Meldung, Wortlaut zu entscheiden |
| (f) | Datenbefunde 1029 (`IstStamm` = 1), 1043 (zwei Ergebniszeilen je Szenario) | offen | Änderung der Testdatenbank |
| (g) | Aufräumkandidaten `KuehltraegerText(m)`, Parameterblock ohne Trägerpreisquelle | offen | nach der Zusammenführung der Bericht-Nachbarn |
| (h), (i) | weiter aus #512, #520; Läufe | siehe 2.10, 2.11 | — |

### 2.10 Nach #520 (BV-E2)

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a) | Anwenderprobe | Windows | — |
| (b) | Regel „Deckblatt aus Platzhaltern“ schärfen | Entscheid | fachlich: welche Angaben ein Deckblatt ausmachen (`Vorlagenfeldkatalog.Deckblattangaben`) |
| (c) | Bezugszeile der Anhang-E-Überlagerung bei unlesbarer eigener Vorlage | umgesetzt | `24c0440e` |
| (d) | `BerichtsvorlagenCtrl.LogoVorhanden()` ungenutzt | offen | die Hülle prüft bewusst nur das Vorhandensein |
| (e) | Beispielvorlage neben dem Kurzbericht | Entscheid | — |
| (f) | Wiki-Upload | erledigt | #556, Revision 610 |
| (g) | Setup-Lauf; iOS-Lauf | erledigt; Lauf | Setup 36240375450; iOS-Teile gebaut (`MauiAsset`, `IosPfade.Berichtsvorlagen`, Dateifilter `.dotx`/`.xltx`), Lauf zurückgestellt |

### 2.11 Nach #512 (BV-E1)

| Punkt | Gegenstand | Stand | Beleg |
|---|---|---|---|
| (a), (e) | Anwenderprobe; Tippprobe mit echtem Word | Windows | — |
| (b) | Setup-Lauf | erledigt | 36240375450, 36294558005 |
| (c) | iOS-Teile | erledigt (gebaut); Lauf | `EPOS.iOS.csproj` `MauiAsset`, `IosPfade.cs`, `Dateifilter.cs` |
| (d) | Wiki-Upload „Berichtsvorlagen“, „Wirtschaftlichkeit“ | erledigt | #556, Revisionen 610 und 597 |
| (f) | „Original geändert – übernehmen?“ | offen | `BerichtsvorlagenCtrl.OriginalGeaendert` ohne Bedienung |
| (g) | Bedienung der Vorgabe `BerichtVorlageWord` | offen | zusammen mit Nach #549 (d3) |
| (h) | KI-Feld der Katalogsuche | umgesetzt | `8a9c81db` |
| (i) | Kern-Vorprüfung für Vorlagen ohne Platzhalter | umgesetzt | `d64c2fa7` |
| (j) | Startrückfrage mit der gespeicherten Versionsauswahl | kein Handlungsbedarf | der Lauf prüft neu und nennt Befunde in der Laufmeldung |
| (k), (l) | Logo; BV-E2 | erledigt | #520 |

## 3 Umsetzungen im Einzelnen

- **Baukasten der leisen Zeile:** `Vorlagenfeldhalter.Baukasten` (Delegat, mit `Zuruecksetzen` zurückgesetzt);
  `VorlagenfeldanzeigeHuelle.Einhaengen` setzt ihn auf `BerichtsvorlagenGaben.BaukastenSpeichern`. Beide Schalen rufen
  `Einhaengen` schon — keine Schale, kein iOS-Adapter geändert; auf dem iPad derselbe Weg wie im Katalog der
  Berichtsseite (danach „Teilen“).
- **TWW-Wache:** `sum()` summiert Gleitkommazahlen erst ab Python 3.12 kompensiert; unter 3.11 wichen die normierten
  Tagesgänge in der letzten Stelle ab, und `TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar` war im Container rot.
  `math.fsum` ist exakt gerundet und trifft die unter 3.12 erzeugten Werte: Lauf gegen eine Kopie der Testdatenbank unter
  3.10, 3.11, 3.12 und 3.13 je „0 Zeile(n) angelegt, 0 nachgefuehrt“, Trägerdateien des Paketteils unverändert. Die
  Testdatenbank, der Paketteil und die Einfrierregeln bleiben unberührt; das Voranstellen von 3.12 im Gate (#588) ist
  damit nicht mehr nötig, schadet aber nicht.
- **KI-Feld `katalogsuche`:** `KiDialoge.BerichtKatalogsuchfeld` (Text, leer erlaubt) auf
  `BerichtSeiteKiSicht.Katalogsuche`; Maske Berichtsseite 7 → 8 Felder; Ressourcen `KI_DLG_BKB_KATALOGSUCHE_NAME/_ERL`.
- **Anhang E:** `BerichtsvorlagenGaben.AnhangEStellenDerVorlage` prüft die eigene Vorlage schnell; unlesbar heißt die
  Bezugszeile „bezogen auf die Standardvorlage“, wie der Kern rechnet.
- **Vorprüfung:** `BerichtCtrl.PruefeVorStart` verlangt für „ohne Wirtschaftlichkeit“ wenigstens einen Platzhalter —
  eine Vorlage ohne Platzhalter bekommt den Bericht an ihr Ende. Der Pfad hat heute keinen Aufrufer außerhalb der Tests.
- **Prüfer:** Die Engine erweitert die Blöcke vor dem Sammeln der Kapitelstellen; ein Kapitel in `{{#je …}}` wird nur
  in der ersten Wiederholung gefüllt. `PruefeKapitelImBlock` gibt je Stelle einen Hinweis mit Block und „Was tun“;
  Kennung `VF_PRUEF_KAPITEL_IM_BLOCK` mit Wissensabschnitt und KI-Frage (Berichtsvorlagen-Kennungen 53 → 54).

Neue Ressourcenschlüssel (je Deutsch und Englisch): `KI_DLG_BKB_KATALOGSUCHE_NAME`, `KI_DLG_BKB_KATALOGSUCHE_ERL`,
`VF_PRUEF_KAPITEL_IM_BLOCK`, `VF_PRUEF_KAPITEL_IM_BLOCK_TUN`, `KI_FRAGE_VF_PRUEF_KAPITEL_IM_BLOCK`; Designer
wiederholbar.

## 4 Offen mit Empfehlung

- **Nach #558 (d):** Einspeisung als Nebenbalken, Beschriftungen an Brücke und Spanne, Ersatzjahr-Band — erst nach der
  Sichtprobe (a) in Excel entscheiden, ob die benannten Abweichungen stören; Datenbeschriftungen verlangen einen Blick
  in Excel, den der Container nicht hat.
- **Vorgaben `BerichtVorlageWord`/`BerichtVorlageExcel` (Nach #512 (g), #549 (d3)):** eine gemeinsame Bedienung —
  Empfehlung: zwei Auswahlfelder in Einstellungen › Bericht über `SetzeVorgabeWord`/`SetzeVorgabeExcel`; der Ort ist
  beim Anwender zu bestätigen (kleiner Auftrag).
- **Häkchen auf die Blätter der Excel-Vorlage (BV-Q2 c):** Prüfer und Kapitelstand für Excel, Regel in
  `BerichtSeite.Ausgegraut`, Wirkung im Füller — mehr als ein halber Tag; nach der Zusammenführung der Szenariowahl auf
  der Berichtsseite als eigener Auftrag.
- **„Original geändert – übernehmen?“ (Nach #512 (f)):** Zeile und Handlung in der Gruppe „Vorlage“ samt Semantik der
  Übernahme — eigener Auftrag, Entwurf der Zeile vorab zeigen.
- **Kälte Dashboard gegen Katalog (Nach #544 (c)):** eigener Auftrag mit Referenzlauf.
- **Logo-Prüfung (Nach #520 (d)):** die Kernprobe (PNG oder JPEG, höchstens 5 MB) mit benanntem Grund in
  Einstellungen › Bericht — kleiner Auftrag mit neuen Texten.
- **Nach #528 (e) bis (g):** Laufmeldung für Nachgeholtes (Wortlaut), Datenbefunde der Testdatenbank (eigener Auftrag
  mit LFS-Commit), Aufräumkandidaten nach der Zusammenführung der Bericht-Nachbarn.

## 5 Abnahme

Auf `dee03725` samt diesem Protokoll: Kern-Filter Release 0 Fehler; `EPOS.UI.Tests` vollständig 6.851 bestanden,
0 rot; `EPOS.Kern.Tests` mit den betroffenen Klassen (Vorlagenfeld, Vorlagenprüfer, Berichtsvorlagen, `BerichtCtrl`,
Anhang E, `TwwKatalogWache` unter Python 3.11, KI-Dialoge und -Wissen, Auslieferungsvorlagen) samt
`DokumentationLinkWache`, `RepositoryOrdnungWache`, `WikiProduktdatenWache` 476 bestanden, 0 rot; Windows-Schale (Linux,
`EnableWindowsTargeting`) 0 Fehler; Designer wiederholbar (zweiter Lauf +0). Kein Diagrammbild geändert (ChartProben
nicht berührt), kein SQL geändert (SQL-Dialekt-Prüfer nicht nötig), kein Referenzlauf nötig (kein Rechenweg).

**Logbuch-Vorschlag** (Version beim Anwender zu erfragen): „Auf der Seite Wirtschaftlichkeit nennen die Platzhaltermarken
von Sensitivität, Zahlungsreihen und Zahlungsstrom auch die feste Position der Stände.“
