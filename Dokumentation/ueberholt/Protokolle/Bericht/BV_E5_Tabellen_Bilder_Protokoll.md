# BV-E5 — Tabellen und Bilder (Protokoll)

Etappe BV-E5 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitt 13). Auftrag #541, Anwenderauftrag vom 26.09.2026: „Fahre fort“ (BV-E5 nach BV-E4). Der gültige Stand steht
im Konzept (Rev. 7) und in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier steht, wie es geworden ist.
Vorgänger: [`BV_E4_Bloecke_Protokoll.md`](BV_E4_Bloecke_Protokoll.md). Zweig `claude/intelligent-bohr-hthrk8` ab
`d8165b06`, umgesetzt am 26.09.2026; Opus 5.5 hat orchestriert, zwei Agenten (Opus 5.5) haben in eigenen Worktrees die
Tabellen (W1) und die Bilder (W2) gebaut, ein Merge-Agent hat beide zusammengeführt und geprüft, zwei weitere Agenten
haben Standardvorlage und Kurzbericht (W3) sowie Baukasten und Leistungstest (W4) gebaut, ein Abschluss-Agent hat
zusammengeführt, das Gate gezogen und die Papiere geschrieben — 14 Commits ohne Merges, 87 Dateien, +14.685/−1.524
Zeilen (Summe der Commits, Binärdateien ohne Zeilen). Kein Schemaschritt, kein Rechenweg berührt (Referenzlauf GESAMT:
PASS), keine Referenzbasis neu eingefroren; eingefroren sind die Schlüsselliste der Katalogfassung 4 und die
ChartProben-Messlatte `Messlatte_2026-09-26.sha256`. `EPOS.iOS/` ist berührt (MauiAsset des Kurzberichts), die
Auslieferung auch (zwei neue Dateien in `{app}\Vorlagen`).

| Agent | Gegenstand | Commits | Zusammenführung |
|---|---|---|---|
| W1 Tabellen | `Berichtstabelle` und Bauwege, Strukturtabellen im Vorlagenweg, Katalog v4 (Tabellen), Prüferregeln, Gebäudeergebnis und Anhang-E-Checkliste | `39b00047`, `bb8ca165`, `6c6bae36` | `9172a59d` |
| W2 Bilder | `Bildmass` und Stufe 2 im `ChartRenderer`, ChartProben, Modellfabrik `Berichtsbilder`, Bildplatzhalter, Katalog v4 (Bilder), Prüfer | `4a15d7ac`, `14e9113b`, `baf8e08e` | `b207e633` |
| Merge-Agent | beide Worktrees, dann `origin/ios_migration_september`, Gate | — | `6e4c27db` |
| W3 Standardvorlage, Kurzbericht | Standard- und Beispielvorlage Fassung 4, `Deckt` mit Tabellen und Bildern, Kurzbericht de/en, „Kopie von:“ | `c7b3c8d8`, `6802806a` | `4a3d86a5` |
| W4 Baukasten, Leistung | `WordBaukasten`, „Baukasten speichern…“, Leistungstest, ChartProben-Messlatte, Paarsicht als Muster, iOS teilt | `22c1c479`, `426977e5`, `e80f4a85`, `cf105e22`, `291c56ad`, `e2ffb1e4` | `25d36111` |
| Abschluss | Merge `origin/ios_migration_september`, Gate, Papiere | — | `f6101457` |

---

## 1 Tabellen (W1)

### 1.1 Modell und Bauwege

Neuer Ordner `EPOS.Kern/Allgemein/Bericht/Tabellen/`:

- **`Berichtstabelle`:** Kopf, Spalten mit Blockart (Fest, Stamm, Stand, Δ), Zeilen, Zellen mit Wert, Format und Rolle
  (Stamm, Gruppe, Summe, Warnung), Direktformatierung, Merkmal `Listentauglich`, `Bloecke(n)` mit der Vorgabe 3
  Varianten je Block, Breiten in DXA und in Prozent.
- **Bauwege `Berichtstabellen`** (`Berichtstabellen.cs`, `.Projekt.cs`, `.Wirtschaft.cs`): Varianten,
  Simulationsstände, Komponentenmatrix, Kenndaten je Gewerk, Abweichungen, Standkennzahlen, Vergleich je Gruppe und
  gesamt, Δ-Prozent, Erzeuger, Brennstoffmengen, Wirtschaftskennzahlen (drei Szenarien), Szenarien, nicht monetäre
  Wirkungen, Betriebskosten, KWKG-Module, Mehrjahrestafel, vermiedene Kosten, Sensitivität, Strommengen,
  Emissionsbilanz, Kälteerzeuger, Speichertemperaturen, Gebäudeergebnis (je Gebäude eine Gruppenzeile),
  Anhang-E-Checkliste (Stelle aus den Kapitelstellen der Vorlage, `Berichtswerte.Kapitelstellen`).
- **Ein Bauweg für beide Wege:** Die Bausteine schreiben ihre Tabellen über dieselben Bauwege
  (`WordTabellenschreiber.Direkt`); der Bausteinweg bleibt byte-gleich — `document.xml` für 1030, 1019, 1017, 1047,
  Gruppen mit 1, 2, 4 und 8 Ständen und Paarsicht (Uhrzeiten neutralisiert). Die KWKG-Wache liest die Tafel jetzt in
  `Berichtstabellen.Wirtschaft.cs`.

### 1.2 Vorlagenweg

`EPOS.Kern/Allgemein/Bericht/Vorlagen/WordVorlagentabellen.cs`:

- `{{tabelle.…}}` **allein im Absatz** wird die Tabelle — im Rumpf, in einer Tabellenzelle (als geschachtelte Tabelle)
  und im Block-Steuerelement; `stand.tabelle.*` im Block `je stand`.
- **Blöcke** zu drei Varianten, mit `|block n` einstellbar; Warnsätze der Tabelle als „Hinweis“ darunter; Breite in
  Prozent des Satzspiegels.
- **Formatierung** in drei Stufen: die Tabellenformatvorlage „EPOS Tabelle“ der Vorlage über `w:name`; fehlt sie und
  trägt die Vorlage eine Mustertabelle `{{muster.tabelle}}` (Alternativtext oder Titel), wird „EPOS Tabelle“ angelegt
  (mit Hinweis in der Laufmeldung) und die Rollen Stamm, Gruppe, Summe, Warnung bekommen Schattierung und Zeichenformat
  der Musterzellen; ohne beides gilt die Direktformatierung des Bausteinwegs. Die Mustertabelle wird entfernt.
- **Leere Tabelle** → Leertext „— (Grund)“.

### 1.3 Katalog und Prüfer

- **37 Tabellenschlüssel** (26 `tabelle.*` für Gruppe und Bericht, 11 `stand.tabelle.*`), dazu `muster.tabelle` und je
  Tabelle ein Schalter `hat.tabelle.<name>` (37) in `Vorlagenfeldkatalog.Tabellen.cs`. `tabelle.komponenten.kenndaten.<gewerk>`
  führt die sieben Gewerke BHKW, Photovoltaik, Pufferspeicher, Solarthermie, Spitzenkessel, Stromspeicher, Wärmepumpe.
- **Nicht gebaut** (nur Excel-Quelle, BV-E7/E8): `tabelle.wirtschaft.parameter`, `tabelle.wirtschaft.verlauf`,
  `stand.tabelle.monatswerte`.
- **Prüfer** `VorlagenprueferTabellen.cs`: Mustertabelle ohne Rolle = Warnung (KI-Kennung
  `VF_PRUEF_MUSTER_OHNE_ROLLEN`), `muster.tabelle` als getippter Text = Fehler; Tabelle im Satz oder in der Kopfzeile wie
  bisher Fehler.

## 2 Bilder (W2)

### 2.1 Bildgröße Stufe 2

`Bildmass` (neu, `EPOS.Kern/Allgemein/Bericht/Bildmass.cs`) als optionaler Parameter an zwölf Modellfunktionen des
`ChartRenderer` (Kuchen, Balken, Jahresverlauf, Dauerlinie, Strombilanz, Speicherverlauf, Speichertemperaturen,
Kapitalwertverlauf, Szenarien, Spanne, Brücke, Zahlungsstrom). **Ohne Maß byte-gleich** (161 alte Hashes der ChartProben
unverändert). Im Zielmaß bricht die Legende um, die Zeichenfläche räumt ihr Platz, der Titel passt sich ein.
**Mindestbreiten** in Modellpunkten: allgemein 560, Brücke 1000, Spanne 900, Szenarien 760; darunter zeichnet das
Modell in der Mindestbreite und wird wie in Stufe 1 verkleinert, mit Hinweis (Entscheid BV-E5-1). ChartProben: 22
Maßproben (halbe Satzspiegelbreite, hohe Form) und 22 Schrift-Gegenproben (`Program.Zielgroesse.cs`), zusammen
218 Bilder grün.

### 2.2 Modellfabrik und Bildteile

`Berichtsbilder` (Kern) baut die Zeichenmodelle der dreizehn Berichtsbilder aus `BerichtsDaten` — dieselben Aufrufe
für Bausteine und Bildplatzhalter; die Bausteine Vergleich, Projekt und Wirtschaftlichkeit rufen sie an der bisherigen
Stelle. `WordKontext.BildTeile` ist geteilt in „Blip bauen“ (PNG und SVG-Teil), „einfügen“ (`wp:inline`) und „Blip
ersetzen“ für den Vorlagenweg. Nachweis: 174 Bilder der Proben 1030 und Gruppe (3, 7 Stände), beide Wege, PNG- und
SVG-Bytes und Maße gleich dem Stand `d8165b06`.

### 2.3 Platzhalter, Engine, Prüfer

- **16 Bildschlüssel** (`Vorlagenfeldkatalog.Bilder.cs`): 7 `stand.bild.*` (`waerme_jahresverlauf`, `waerme_dauerlinie`,
  `strombilanz_monate`, `speicherverlauf`, `deckung_waerme`, `deckung_strom`, `zahlungsstrom`),
  `stamm.bild.speichertemperaturen`, 4 `bild.vergleich.balken.<k>`, 4 `bild.wirtschaft.*` (`kapitalwert_szenarien`,
  `barwerte_kumuliert`, `bruecke`, `spanne`), je Bild ein Schalter `hat.bild.<name>` (16); Quelle ist ein
  `Diagrammbild` (Modell im Zielmaß aus `Berichtsbilder`), Bedarf Zeitreihen bzw. Verlauf, kein Nachholen. Die
  App-Diagramme ohne Berichtsziel bleiben vorgemerkt (`VorgemerkteBilder`); Paarbilder gibt es nicht (Entscheid BV-E5-2).
- **Engine** `WordVorlagenbilder.cs`: Bild mit dem Schlüssel im Alternativtext → Blip ersetzt (PNG und SVG), Breite des
  Rahmens, Höhe nach dem Modell, `a:srcRect` entfernt, der Diagrammtitel wird Alternativtext; der Schlüssel als Text
  allein im Absatz → Bild in Satzspiegelbreite; ohne Modell ein Hinweisabsatz mit Grund und Laufmeldung;
  `stand.bild.*` nur im Standblock.
- **Prüfer** `VorlagenprueferBilder.cs`: Bild im Satz Fehler (mit Rat), unbekannter Bildschlüssel Fehler, vorgemerktes
  App-Diagramm „später“, Kontextregel, Bildrahmen unter 80 % der Modellbreite Warnung (`VF_PRUEF_BILDRAHMEN`, volle
  Prüfung).

## 3 Katalog Fassung 4

`KATALOGFASSUNG = 4`; eingefrorene Liste `EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v4.txt` mit 2.085 Zeilen
(2.078 Schlüssel, 3 Aliasse, 4 Kopfzeilen) — gegenüber v3 107 Schlüssel mehr: 37 Tabellen, `muster.tabelle`, 37
`hat.tabelle.*`, 16 Bilder, 16 `hat.bild.*`. `Vorlagenfeldkatalog_v3.txt` ist unverändert. `hat.tabelle.*` außerhalb
eines Blocks wertet „irgendein Stand hat die Tabelle“ (Orchestrator-Entscheid).

## 4 Standardvorlage und Kurzbericht (W3)

- **Standard- und Beispielvorlage** mit dem Werkzeug neu erzeugt, Vorgabe `--katalogfassung 4`; inhaltsgleich,
  `custom.xml` `EPOS.Katalogfassung` 4 (BV-E4-4 erledigt). Die Beispielvorlage bleibt vorerst (Orchestrator-Entscheid).
- **`Deckt` der Kapitel** (Entscheid BV-E5-3): jedes Kapitel führt die Tabellen und Bilder des Katalogs v4, die sein
  Baustein schreibt, je mit Schalter `hat.*`; `tabelle.varianten` gehört zu „Komponenten & Varianten“; `hat.tabelle.*`
  und `hat.bild.*` zählen in `Gedeckt` wie `baustein.*` nicht als Inhalt. Kapitelwache nachgezogen, neue
  Zuordnungswache; die Deckungswache prüft die Fassungen 1, 2 und 4 (ohne `muster.tabelle`).
- **Kurzbericht** `Berichtsvorlage_Kurzbericht.docx` und `Berichtsvorlage_Kurzbericht_en.docx` unter
  `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/`, erzeugt mit dem neuen Werkzeugbefehl
  `kurzbericht <quelle> <ziel> --sprache de|en`: Lehrvorlage mit neun Kommentaren, Einzelwerte im Fließtext,
  Musterzeile `je stand`, `wenn`-Blöcke, Strukturtabelle `tabelle.wirtschaft.szenarien`, drei Bildrahmen (das
  Spannenbild in voller Breite, zwei Deckungskuchen je Stand nebeneinander in halber Breite), Anhang als Kapitel,
  Mustertabelle; `custom.xml` mit `EPOS.Vorlage` `kurzbericht` und `EPOS.Sprache`. Byte-gleich wiederholbar
  (Platzhalterbild aus festen Bytes).
- **Lieferwege:** `WindowsFormsApplication1.csproj` und `EPOS.iOS/EPOS.iOS.csproj` (MauiAsset), Kommentar in
  `Setup/EPOS-Plan.iss`.
- **„Neue Vorlage…“ mit „Kopie von:“** Standard oder Kurzbericht der Oberflächensprache: `BerichtsvorlagenCtrl`
  (`DATEI_KURZBERICHT`, `DATEI_KURZBERICHT_EN`, `Vorlagenmuster`, `NeueVorlage(name, muster, englisch)`),
  `BerichtsvorlagenGaben` (`Mustereintraege`, `NeueVorlageAusMuster`), `BerichtSeite.razor` mit der Optionsgruppe
  „Kopie von:“; Ressourcen `BV_VORLAGEN_NEU_KURZBERICHT`, `BK_BER_VORLAGE_NEU_MUSTER*`; Eingabebilanz in
  `KiMaskenabdeckungWacheTests` für die Berichtsseite 3 → 4.
- **Abweichung:** `w:tblDescription` der Mustertabelle ist erst ab Office 2010 gültig; der Validator mit dem Maß
  Office 2007 lässt genau diesen Befund als einzige benannte Ausnahme durch.
- Wiki-Quelle „Berichtsvorlagen“ um einen Satz ergänzt; Zeilen in `CLAUDE.md` und `EPOS.Kern/CLAUDE.md` nachgezogen.

## 5 Baukasten (W4)

`EPOS.Kern/Allgemein/Bericht/Vorlagen/WordBaukasten.cs`: `WordBaukasten.Erzeuge(englisch|kultur, fassung)` erzeugt im
Kern die Bytes einer `.docx` aus dem Katalog — jeder Eintrag mit Ausgabe Word und `Seit` ≤ Fassung genau einmal
(1.304 Einträge), gegliedert nach Kontext (Bericht, Installation, Stamm, Gruppe, Stand im Block `je stand`, Gebäude im
Block `je gebaeude`, Paarsicht, Mustertabelle), je Eintrag Beschreibung und Platzhalter mit `w:noProof`, Bilder als
neutrales Musterbild, Schalter in `#wenn`-Mustern. **Paarsicht nur als Muster** (Entscheid BV-E5-4): statt 778
Paarschlüsseln drei Beispiele als Text ohne Klammern und die Regel im Hinweis — so ist der Baukasten in jeder Sicht
fehlerfrei füllbar. `BerichtsvorlagenCtrl.Baukasten/SpeichereBaukasten`, Hülle `BaukastenSpeichern`, Knopf
„Baukasten speichern…“ im `PlatzhalterkatalogDialog` (`VF_KATALOG_BTN_BAUKASTEN`, `VF_BAUKASTEN_*`). **iOS teilt**
(Entscheid BV-E5-5): nach dem Speichern öffnet die Hülle das Teilen-Blatt über `Dienste.Datei.MitSystemOeffnen`, wie der
gbXML-Export; scheitert es, nennt `VF_BAUKASTEN_TEILEN_FEHLER` den Pfad — keine Datei unter `EPOS.iOS/` berührt. Der
Excel-Baukasten kommt mit BV-E7. `WikiProduktdatenWacheTests.FundstellenIn` hält auch den Baukastentext.

## 6 Laufzeit

`BerichtsvorlagenLeistungTests` (`[Trait("Kategorie","Messung")]`): Gruppe mit 7 Ständen, alle Bausteine, 31 Tabellen,
54 Bilder, rund 60 Seiten; Bausteinweg mit der Stilvorlage gegen Vorlagenweg mit der Standardvorlage, abwechselnde
Läufe nach einem Aufwärmpaar, bis zu drei Durchgänge. Ergebnis im Container: **Median Bausteinweg 1.546 ms,
Vorlagenweg 1.608 ms, Verhältnis 1,040** (Grenze 1,10 — „heute + 10 %“ der Abnahme in Abschnitt 13 des Konzepts).

**ChartProben-Messlatte:** `Proben/ChartProben/Messlatte_2026-09-26.sha256` (183 Hashes, Linux, Release) ersetzt
`Messlatte_2026-09-20.sha256`: alle 91 alten Zeilen unverändert, 92 Bilder neu (E6, G2, AK1 samt Kälteseite, Z1, Z2,
Z4, E8a, Bildgröße Stufe 2), keines geändert; zweiter Lauf byte-gleich. `Proben/ChartProben/LIESMICH.md` mit Abschnitt
Bildgröße Stufe 2. Die Textnennungen der alten Messlatte in `Konzept_Diagramme_Interaktiv_EPOS-Plan.md` und
`Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md` sind mit dem Papier-Commit nachgezogen.

## 7 Entscheidungen

### 7.1 Entscheide des Anwenders (26.09.2026)

| Kennung | Entscheid |
|---|---|
| **BV-E5-1** | Unter der Mindestbreite eines Bildes zeichnet das Modell in der Mindestbreite und wird verkleinert (Stufe 1), mit Hinweis. |
| **BV-E5-2** | Keine Paarbilder (`stand.a/b.bild.*`). |
| **BV-E5-3** | `Deckt` der Kapitel führt ihre Tabellen und Bilder samt `hat.*`. |
| **BV-E5-4** | Die Paarsicht steht im Baukasten nur als Muster. |
| **BV-E5-5** | Auf iOS teilt die App den Baukasten nach dem Speichern. |

### 7.2 Entscheide der Orchestrierung

- Die Tabellenformatvorlage „EPOS Tabelle“ wird nur angelegt, wenn die Vorlage eine Mustertabelle trägt (Konzept 6.4).
- `hat.tabelle.*` außerhalb eines Blocks bedeutet „irgendein Stand hat die Tabelle“.
- Die Beispielvorlage bleibt vorerst neben Standardvorlage und Kurzbericht.

### 7.3 Abweichungen vom Plan mit Grund

| Plan (Rev. 6) | Umgesetzt | Grund |
|---|---|---|
| `tabelle.wirtschaft.parameter`, `tabelle.wirtschaft.verlauf`, `stand.tabelle.monatswerte` in E5 | nicht gebaut, BV-E7/E8 | nur eine Excel-Quelle, kein Bauweg des Wortberichts |
| Kurzbericht B.1 Nr. 8: zwei Bilder im Block `{{#je variante\|block 1}}` | Block `{{#je stand}}` mit Überschrift `{{stand.anzeige}}` | die Deckungskuchen gehören zu jedem Stand, auch zum Stamm |
| Kurzbericht B.1 Nr. 6: Spannenbild immer | im Block `{{#wenn hat.bild.wirtschaft.spanne}}` | ohne Modell entfällt der Rahmen samt Hinweis |
| Validator Office 2007 ohne Befund | Ausnahme `w:tblDescription` | der Alternativtext der Mustertabelle ist erst ab Office 2010 gültig |

## 8 Abnahme

**Tests der Etappe:** `BerichtstabelleTests` 8, `WordVorlagenTabellenTests` 12, `WordVorlagenBilderTests` 11,
`KurzberichtRundlaufTests` (1030 de/en: keine Prüferfehler, kein `{{` im Ergebnis, Validator grün, SVG und PNG),
`WordBaukastenTests` 6 (Gliederung, Gültigkeit, Rundlauf 1030, 1019 und Gruppe mit drei Ständen in Sicht 1 und 2, de
und en, Produktdatenwache), `BerichtsvorlagenLeistungTests`, Deckungs-, Kapitel-, Datei- und Auslieferungswachen.

**Gate auf `f6101457`** (nach allen Merges): Kern-Filter Release, Windows-Schale (Linux, `EnableWindowsTargeting`) und `Werkzeuge/Berichtsvorlage` je 0 Fehler; Tests EPOS.Kern 8.207 bestanden / 1 übersprungen / 1 rot (`TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar` — Umgebung, Container mit Python 3.11, wie in #532; auf der CI grün), EPOS.UI 6.548, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 / 1 übersprungen; Designer wiederholbar (12.329 Einträge); SQL-Prüfer 1.990 Texte / 0 Fundstellen; ChartProben 218 Bilder / 0 Verstöße; Referenzlauf 6 Projekte (1030, 1007, 1017, 1045, 1046, 1047) gegen R20 GESAMT: PASS, 2.208.587 Werte; die vier Vorlagen mit dem Werkzeug neu erzeugt ohne Änderung.

**Wiederholbarkeit der Vorlagen:** Beispiel-, Standardvorlage und beide Kurzberichte mit den Befehlen aus
`Werkzeuge/Berichtsvorlage/LIESMICH.md` erneut erzeugt — Regeln grün, Validator 0 Fehler, `git status` ohne Änderung.

Kein Schemaschritt, kein Rechenweg, keine Referenzbasis neu eingefroren.

## 9 Commitfolge

| Commit | Inhalt |
|---|---|
| `39b00047` | W1: Berichtstabelle und Bauwege, Bausteine schreiben darüber |
| `bb8ca165` | W1: Strukturtabellen im Vorlagenweg, Katalog v4, Prüferregeln |
| `6c6bae36` | W1: Gebäudeergebnis und Anhang-E-Checkliste als Tabellen |
| `4a15d7ac` | W2: Bildgröße Stufe 2 im ChartRenderer, ChartProben |
| `14e9113b` | W2: Modellfabrik Berichtsbilder, BildTeile geteilt |
| `baf8e08e` | W2: Bildplatzhalter `bild.*`, `stand.bild.*`, Katalog v4, Prüfer |
| `9172a59d` | Merge W1 |
| `b207e633` | Merge W2 |
| `6e4c27db` | Merge `origin/ios_migration_september` vor dem Gate |
| `c7b3c8d8` | W3: Standardvorlage Fassung 4, `Deckt` v4, Kurzbericht je Sprache |
| `6802806a` | W3: Eingabebilanz der Berichtsseite um die Musterwahl |
| `22c1c479` | W4: Baukasten der Word-Vorlagen aus dem Katalog |
| `426977e5` | W4: Baukasten speichern im Platzhalterkatalog |
| `e80f4a85` | W4: Leistungstest Vorlagenweg gegen Bausteinweg |
| `cf105e22` | W4: ChartProben-Messlatte auf 183 Bilder, LIESMICH Stufe 2 |
| `291c56ad` | W4: Paarsicht im Baukasten nur als Muster (BV-E5-4) |
| `e2ffb1e4` | W4: Baukasten auf iOS nach dem Speichern teilen (BV-E5-5) |
| `4a3d86a5` | Merge W3 (konfliktfrei) |
| `25d36111` | Merge W4 (konfliktfrei) |
| `f6101457` | Merge `origin/ios_migration_september` vor der Abnahme (Konflikt in beiden `.resx`: Vereinigung) |
| Papier-Commit | „Papiere #541: BV-E5 Tabellen und Bilder“ — dieses Protokoll, Konzept Rev. 7, Statusdatei, Index, Messlatten-Nennungen in zwei Konzepten |

## 10 Offen

- **(a) Anwenderprobe unter Windows:** Kurzbericht, Baukasten, Tabellen mit Mustertabelle, Bilder in schmalen Rahmen.
- **(b) iOS-Lauf** (MauiAsset des Kurzberichts) und **Setup-Lauf** (zwei neue Dateien in `{app}\Vorlagen`), je nach
  Rückfrage.
- **(c) Excel-Seite** mit BV-E7/E8: listentaugliche Tabellen als Excel-Tabellen, Bilder als Excel-Diagramme, die drei
  Tabellen mit reiner Excel-Quelle, Excel-Baukasten.
- **(d) Paarzwillinge** für Tabellen und Bilder bewusst nicht (BV-E5-2).
- **(e) Wiki-Upload** der Seite „Berichtsvorlagen“ und Logbuch.
- **(f) TWW-Wache fassungsfest** (weiter offen aus „Nach #532“ (g)).

**Logbuch-Vorschläge** (für den nächsten Wiki-Upload, Version beim Anwender zu erfragen, Stichwort `bericht`):

- „Eigene Word-Berichtsvorlagen können Tabellen und Diagramme als Platzhalter aufnehmen; Diagramme werden in der Größe
  des Bildrahmens gezeichnet.“
- „‚Neue Vorlage…‘ kann auch den Kurzbericht kopieren.“
- „Der Platzhalterkatalog speichert auf Wunsch einen Baukasten mit allen Platzhaltern als Word-Datei.“

## 11 Dateien

14 Commits ohne Merges (Tafel in Abschnitt 9): 87 Dateien, +14.685/−1.524 Zeilen.

| Bereich | Dateien |
|---|---|
| Kern, Tabellen | neu `EPOS.Kern/Allgemein/Bericht/Tabellen/Berichtstabelle.cs`, `Berichtstabellen.cs`, `Berichtstabellen.Projekt.cs`, `Berichtstabellen.Wirtschaft.cs`, `WordTabellenschreiber.cs` |
| Kern, Bilder | neu `EPOS.Kern/Allgemein/Bericht/Bildmass.cs`, `Berichtsbilder.cs`; geändert `ChartRenderer.cs`, `WordBerichtGenerator.cs` |
| Kern, Bausteine und Bericht | `Bausteine/BausteineProjekt.cs`, `BausteineStandard.cs`, `BausteineVergleich.cs`, `BausteineWirtschaftlichkeit.cs`; `AnhangECheckliste.cs`, `BerichtTexte.cs`, `Berichtsbedarf.cs` |
| Kern, Vorlagen | neu `Vorlagen/Vorlagenfeldkatalog.Tabellen.cs`, `Vorlagenfeldkatalog.Bilder.cs`, `VorlagenprueferTabellen.cs`, `VorlagenprueferBilder.cs`, `WordVorlagentabellen.cs`, `WordVorlagenbilder.cs`, `Diagrammbild.cs`, `WordBaukasten.cs`; geändert `Berichtswerte.cs`, `Platzhalterwert.cs`, `Vorlagenfeldkatalog.cs`, `Vorlagenpruefer.cs`, `WordVorlagenfueller.cs`, `WordVorlagenstile.cs`, `WordVorlagentexte.cs` |
| Kern, Controller, KI, Ressourcen | `EPOS.Kern/Controller/BerichtsvorlagenCtrl.cs`; `Allgemein/KI/HilfeWissenBerichtsvorlagen.cs`, `KiMeldungskennung.cs`; `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| Hülle und Oberfläche | `EPOS.UI.Daten/Bericht/BerichtsvorlagenGaben.cs`; `EPOS.UI/Seiten/Berichte/BerichtSeite.razor`, `BerichtDaten.cs`, `BerichtSeiteVorlagentexte.cs`; `EPOS.UI/Dialoge/Berichte/PlatzhalterkatalogDialog.razor`, `PlatzhalterkatalogTexte.cs` |
| Vorlagen und Lieferwege | `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Standard.docx`, `Berichtsvorlage_Beispiel.docx`, neu `Berichtsvorlage_Kurzbericht.docx`, `Berichtsvorlage_Kurzbericht_en.docx`; `WindowsFormsApplication1/WindowsFormsApplication1.csproj`, `EPOS.iOS/EPOS.iOS.csproj`, `Setup/EPOS-Plan.iss` |
| Werkzeug | `Werkzeuge/Berichtsvorlage/Kurzbericht.cs` (neu), `Beispielvorlage.cs`, `Program.cs`, `Pruefung.cs`, `Berichtsvorlage.csproj`, `LIESMICH.md` |
| ChartProben | `Proben/ChartProben/Program.Zielgroesse.cs` (neu), `Program.cs`, `Program.GruppeC.cs`, `Program.GruppeD.cs`, `ChartProben.csproj`, `LIESMICH.md`, `Messlatte_2026-09-26.sha256` (ersetzt `Messlatte_2026-09-20.sha256`) |
| Tests Kern | neu `BerichtstabelleTests.cs`, `WordVorlagenTabellenTests.cs`, `WordVorlagenBilderTests.cs`, `KurzberichtRundlaufTests.cs`, `WordBaukastenTests.cs`, `BerichtsvorlagenLeistungTests.cs`, `Messlatten/Vorlagenfeldkatalog_v4.txt`; geändert `AuslieferungsvorlagenWacheTests.cs`, `BerichtKiKennungenTests.cs`, `BerichtsvorlageDateiWacheTests.cs`, `BerichtsvorlagenCtrlTests.cs`, `BerichtsvorlagenHuelleTests.cs`, `KiDialogaufrufTests.cs`, `KwkgSatzHerkunftTests.cs`, `VorlagenfeldkatalogWacheTests.cs`, `VorlagenprueferTests.cs`, `WikiProduktdatenWacheTests.cs` |
| Tests UI | `EPOS.UI.Tests/Seiten/BerichtSeiteVorlagenTests.cs`, `Dialoge/PlatzhalterkatalogDialogTests.cs`, `Dialoge/Hilfe/KiMaskenabdeckungWacheTests.cs` |
| Regeln und Wiki | `CLAUDE.md`, `EPOS.Kern/CLAUDE.md`, `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki` |
| Papiere (dieser Auftrag) | dieses Protokoll, Konzept Rev. 7, `Dokumentation/aktuell/Status_iOS_Migration.md`, `Dokumentation/LIESMICH.md`, `Konzept_Diagramme_Interaktiv_EPOS-Plan.md`, `Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md` |
