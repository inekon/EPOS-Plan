# BV-E4 — Blöcke, Schalter, Standwerte (Protokoll)

Etappe BV-E4 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitt 13). Auftrag #532, Anwenderauftrag vom 26.09.2026: „Fahre fort mit der Berichterstellung“ (BV-E4 auf Zuruf).
Der gültige Stand steht im Konzept (Rev. 6) und in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier
steht, wie es geworden ist. Vorgänger: [`BV_E3_Wertesatz_Protokoll.md`](BV_E3_Wertesatz_Protokoll.md). Zweig
`claude/intelligent-bohr-hthrk8` ab `942bd1ad`, umgesetzt am 26.09.2026; Opus 5.5 hat orchestriert, zwei Agenten
(Opus 5.5) haben in eigenen Worktrees gearbeitet, je ein weiterer Opus-Agent hat zusammengeführt und die Werte
Minimum/Maximum samt Merge von `origin/ios_migration_september` gebaut — 8 Commits ohne Merges, 30 Dateien,
+7.537/−289 Zeilen (`git diff --stat 942bd1ad ce60c3f6`). Kein Schemaschritt, kein Rechenweg berührt (Referenzlauf
GESAMT: PASS), keine Basis neu eingefroren, keine Oberfläche, `EPOS.iOS/` nicht berührt; neu eingefroren ist allein die
Schlüsselliste der Katalogfassung 3.

| Agent | Gegenstand | Commits | Zusammenführung |
|---|---|---|---|
| Orchestrierung | Vertrag: Blockkontext am Wertesatz | `c6ebc17c` | Grundlage beider Worktrees |
| W1 Block-Engine | `WordVorlagenbloecke.cs`, Blockregeln des Prüfers (`VorlagenprueferBloecke.cs`), Tests | `19c1198c`, `5d674466` | `f8418a1f` (in den Stand von W2) |
| W2 Katalogwerte | Leitversion auf `BesteVariante.Waehle`, Katalog Fassung 3 mit Standwerten, Wirtschaft, Gruppe, `hat.*`, Tests | `184116f5`, `a4013cd4`, `4d71575f` | `3b81e599` |
| Zusammenführung | Blockgrundlage regulär in Katalog v3 | `8c67c287` | auf dem Zweig |
| Min/Max | `vergleich.minimum/maximum.<k>`, Alias `vergleich.beste_variante` (BV-E4-2); Merge `origin/ios_migration_september` | `ce60c3f6` | `b59c5653` |

---

## 1 Block-Engine (W1)

### 1.1 Vertrag

Der Blockkontext hängt am Wertesatz (`Berichtswerte`, Commit `c6ebc17c`): `Staende` (Stamm und Varianten des Berichts
in ihrer Reihenfolge), `LaufenderStand`, `LaufendesGebaeude`, dazu `MitStand(stand)` und `MitGebaeude(zeile)`, die je
Wiederholung eine Kopie mit gesetztem Kontext liefern (`ImStandblock`, `ImGebaeudeblock`). Beide Worktrees bauten auf
diesem Vertrag.

### 1.2 Blockarten und Formen

Neue Datei `EPOS.Kern/Allgemein/Bericht/Vorlagen/WordVorlagenbloecke.cs`, `WordVorlagenfueller` als `partial`:

- **Wiederholung** `{{#je stand}}`, `{{#je variante}}`, `{{#je gebaeude}}` … `{{/je}}`; **Schalter** `{{#wenn …}}`,
  `{{#wenn nicht …}}` … `{{/wenn}}`.
- **Absatzblöcke:** Anfangs- und Endmarke in eigenen Absätzen; der Block darf ganze Tabellen umschließen.
- **Musterzeile:** stehen die Marken in den Zellen einer Tabellenzeile, wird die Zeile je Element geklont und die Marken
  fallen; ergibt der Block keine Zeile, entfällt die Tabelle.
- **SDT-Wiederholabschnitte:** ein Inhaltssteuerelement mit dem Tag `#je …` bzw. `#wenn …` auf Block- und auf
  Zeilenebene; nach dem Füllen wird es ausgepackt. Beim Klonen fallen `w:id` und Textmarken weg, damit das Dokument
  gültig bleibt.

### 1.3 Gruppenblock `|block n`

`{{#je variante|block n}}` fasst die Varianten zu Gruppen von n. In der Gruppe gibt es keinen laufenden Stand: ein
inneres `je stand` läuft über Stamm und Varianten der Gruppe, ein inneres `je variante` über die Varianten der Gruppe,
`stand.*` direkt ist ein Kontextfehler, `hat.*` wertet über den ganzen Bericht (Abschnitt 8 c).

### 1.4 Regeln

- Höchstens zwei Ebenen.
- Ein Schalter ohne Wert gilt als falsch, mit Laufmeldung.
- Leerfälle ohne Reste: ein Block ohne Element hinterlässt weder Absatz noch Tabelle noch Steuerelement.
- Eine Überschrift unmittelbar vor einem entfallenden Block entfällt mit, wenn danach nur eine Überschrift oder das
  Abschnittsende folgt.
- Neue Befundarten `Fuellbefundart.Block` und `Fuellbefundart.Kontext`; `Fuellergebnis.Leermeldung` fasst Leerwerte je
  Schlüssel zusammen („leer bei x von y Ständen/Gebäuden“).

## 2 Prüfer (W1)

Neue Datei `EPOS.Kern/Allgemein/Bericht/Vorlagen/VorlagenprueferBloecke.cs`:

- Blockbereiche aus Markenpaaren und aus SDT-Tags; Ebenen; offene, gekreuzte und verwaiste Marken.
- Kontext: `stand.*`, `gebaeude.*` und die Standschalter nur im passenden Block.
- Paarsicht: `VF_PRUEF_PAARSICHT` (mit `_TUN`) meldet `stand.a/b` nur, wenn der Lauf mehr als eine Variante zeigt
  (`AnzahlVarianten > 1`).
- `|block n` nur an `je variante` und nur um ganze Absätze; eine Musterzeile mit `|block n` bleibt Prüferfehler.
- Ein Block in einer Zelle muss aus eigenen Absätzen bestehen; fehlerhafte SDT-Tags werden benannt.
- Die Meldungen „Block noch nicht unterstützt“ und „später“ entfallen (Ressource `VF_PRUEF_SPAETER_TUN_SCHALTER`
  entfernt).
- Ein Tag ohne Klammern (`#je stand`) zählt als Blockmarke; der BV-E1-Test, der ihn als unbekannten Tag erwartete, ist
  umgestellt.

## 3 Katalog Fassung 3 (W2, Zusammenführung, BV-E4-2)

### 3.1 Fassung und Liste

`KATALOGFASSUNG = 3`; eingefrorene Liste `EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v3.txt` mit 1.974 Zeilen
(1.971 Schlüssel, 3 Aliasse). Die Standwerte stehen in `Vorlagenfeldkatalog.Standwerte.cs` (neu), die Blockgrundlage
(Blockwörter, Schalter) ist mit `8c67c287` regulär Teil der Fassung 3.

### 3.2 Gruppen

| Gruppe | Schlüssel | Bemerkung |
|---|---|---|
| `stand.*` handgepflegt | `rolle`, `bezeichner`, `projektname`, `anzeige`, `hinweis`, `beschreibung`, `kunde`, `bearbeiter`, `stromspeicher`, `simulationsstand`, `ist_stamm`, `hat_fehler`, `veraltet`, `frisch`, `hat_zeitreihen`, `fehler`, `wirtschaft.warnungen`, `bandbreite.*` (6) | im Standblock |
| `stand.kennzahl.<k>` | 44 | wie `stamm.kennzahl.*` |
| `stand.delta.<k>`, `stand.delta_prozent.<k>` | je 32 | nur Kennzahlen mit Differenzanzeige |
| `stand.wirtschaft.<zeile>` | 68 × 3 Szenarien + 54 `.grund` | Wert = `WirtZeile.ExcelWert` aus `Wirtschaft.Zeilen(IdReferenzTafel)` |
| `stand.a.*`, `stand.b.*` | Paarzwillinge der Standwerte | Kontext Gruppe (Paarsicht) |
| `stamm.wirtschaft.*` | 204 | 68 × 3 |
| `wirtschaft.*` | 10 Gruppenwerte (`warnungen`, `referenzname`, `rechenstand`, `methodik`, `parameternachweis`, `vorschlag`, `szenarioabdeckung`, `valeri_hinweise`, `hinweise`, `deklarationen`); `beste.anzeige`, `beste.ist_stamm`, `beste.kapitalwert`, `beste.nettobarwert`, `beste.<zeile>` | `beste.kapitalwert` = Kachelwert (Variante: Kapitalwertdifferenz, Stammfall: Nettobarwert) |
| `wirtschaft.parameter.*` | 30 (10 × 3 Szenarien) | in Prozent (BV-E4-3) |
| `wirtschaft.szenario.<s>.name/annahmen/traegerpreise` | 9 | |
| `vergleich.spanne/minimum/maximum.<k>` | je 44 | Minimum und Maximum neutral (BV-E4-2) |
| Alias `vergleich.beste_variante` | → `wirtschaft.beste.anzeige` | BV-E4-2 |
| `gebaeude.*` | 18 | nur in `je gebaeude` |
| `hat.*` | 11: `varianten`, `ergebnis`, `zeitreihen`, `kaelte`, `wirtschaft`, `fehler`, `veraltet`, `gebaeude`, `emissionsbilanz`, `sensitivitaet`, `emissionsmodus_gwp` | im Standblock für den Stand, sonst Bericht |

### 3.3 Werte, Leerwerte, Bedarf

- Leerwert nie 0, immer mit Grund; neue Gründe `BV_GRUND_*` (11). `.grund` gibt es nur im Erwartungsfall.
- Stammfall der besten Variante: `wirtschaft.beste.ist_stamm` wahr, `beste.kapitalwert` zeigt den Nettobarwert wie die
  Kachel, `beste.kapitalwert_diff` ist leer mit Grund „nur Stammprojekt gerechnet“.
- Bedarf „Zeitreihen“ tragen nur die Kältestunden (Wert, Abweichung, Spanne, Minimum, Maximum) sowie `hat.zeitreihen`
  und `stand.hat_zeitreihen`. `Berichtsbedarf.AusVorlage` behandelt Einzelwerte der Wirtschaftlichkeit wie das Kapitel
  (`IstWirtschaftswert`).
- Der Wirtschaftlichkeitsbaustein hat seine Sätze in interne Helfer gelegt — Baustein und Katalog teilen denselben
  Wortlaut, die Ausgabe ist unverändert (Messlatten und Inhaltsabzüge gleich).
- Großschreibung eines Schlüssels wirkt als Alias über die Normierung in `Finde`.

### 3.4 Ressourcen und KI

Rund 90 neue Einträge in beiden `.resx` (u. a. 11 `BV_GRUND_*`, 16 `VF_MUSTER_*`, die `VF_*`-Beschreibungen,
`VF_PRUEF_PAARSICHT(_TUN)`, `KI_FRAGE_VF_PRUEF_PAARSICHT`), entfernt `VF_PRUEF_SPAETER_TUN_SCHALTER`; Designer mit
`designer_neu.py` erzeugt und wiederholbar. `KiMeldungskennung` +1, `HilfeWissenBerichtsvorlagen` nachgezogen.

## 4 Leitversion (W2, BV-E4-1)

`Zahlungsgliederungen.Leitversion` ruft `BesteVariante.Waehle`. Folgen:

- Gruppe nur mit dem Stamm → die Leitversion ist der Stamm; Differenzspalte und Brücke entfallen weiter, „Was daraus im
  Lauf wird“ zeigt die Tafel des Stamms.
- Sicht 2 mit B = Stamm → 0.
- Ist eine Variante die Referenz, nimmt der Stamm nicht teil.
- Messlatten und Inhaltsabzüge unverändert. `ZahlungsgliederungTests` angepasst (Gruppe {1,5} → 1 statt 5, {1} → 1
  statt 0, Sicht-2-Fall), neu `Die_Leitversion_ist_die_Wahl_der_besten_Variante`.

## 5 Entscheidungen

### 5.1 Entscheide des Anwenders (26.09.2026)

| Kennung | Entscheid |
|---|---|
| **BV-E4-1** | Die Leitversion folgt der besten Variante (`BesteVariante.Waehle`). |
| **BV-E4-2** | `vergleich.minimum/maximum.<k>` neutral statt `vergleich.bestwert.<k>`; `vergleich.beste_variante` ist Alias von `wirtschaft.beste.anzeige`. |
| **BV-E4-3** | Die Parameter `wirtschaft.parameter.*` stehen in Prozent. |
| **BV-E4-4** | Die Standardvorlage bleibt bei Katalogfassung 2 bis BV-E5. |
| Nach #528 (a) | Das Speichertemperaturbild der Projektbeschreibung bleibt, wie es ist — `kapitel.projekt` trägt keinen Bedarf. |
| Nach #528 (c) | Stammfall `wirtschaft.beste.*`: gelöst mit `beste.ist_stamm`, `beste.kapitalwert` (Kachelwert) und `beste.kapitalwert_diff` im Stammfall leer mit Grund. |

Entscheidweg: Die Orchestrierung legte die offenen Punkte aus „Nach #528“ und die Fragen aus dem Katalogbau (Min/Max statt
Bestwert, Einheit der Parameter, Fassung der Standardvorlage) dem Anwender am 26.09.2026 mit Lesarten und
Empfehlung vorgelegt; BV-E4-2 wurde nach der Zusammenführung als eigener Schritt gebaut (`ce60c3f6`).

### 5.2 Abweichungen von Anhang A (Rev. 5) mit Grund

| Anhang A (Rev. 5) | Umgesetzt | Grund |
|---|---|---|
| `stamm.kaeltestrom.*` | nicht angelegt | Doppelung zu `stamm.kennzahl.kaelte.*` |
| `vergleich.bestwert.<k>` | ersetzt durch `vergleich.minimum/maximum.<k>` | BV-E4-2 |
| `vergleich.beste_variante` als eigener Wert | Alias von `wirtschaft.beste.anzeige` | BV-E4-2, ein Wert, eine Regel |
| `stand.wirtschaft.erl_<block>_k_<komponente>`, `…_teil_…`, `vermieden_herleitung_<komponente>` | laufzeitgebildete Kopf-, Kohärenz- und Anlagenzeilen (`erl_kopf_a/b`, `erl_<b>_k_<k>`, `KOHAERENZ_n`, `ENERGIEKOSTEN_ANLAGE_<name>`) nicht angelegt | Anzahl und Namen hängen am Projekt; eine eingefrorene Liste kann sie nicht führen |
| `stand.wirtschaft.<zeile>.grund` | nur im Erwartungsfall | ein Grund je Zeile |
| Großschreibung als Alias | über die Normierung in `Finde`, nicht als Listeneintrag | die eingefrorene Liste führt nur die kleingeschriebenen Schlüssel |

### 5.3 Entscheidungen der Agenten

- **W1:** die Blöcke als `partial` des `WordVorlagenfueller` in eigener Datei; der Gruppenblock ohne laufenden Stand;
  höchstens zwei Ebenen; ein Tag ohne Klammern als Blockmarke.
- **W2:** die Werte der Wirtschaftlichkeitszeilen aus `WirtZeile.ExcelWert` (Word und Excel dieselbe Zahl); die Sätze des
  Bausteins in internen Helfern, die Katalog und Baustein teilen; die Leitversion ohne eigene Regel.

### 5.4 Zusammenführung

W2 mit `3b81e599`, W1 mit `f8418a1f` in den Stand von W2, beide ohne inhaltlichen Konflikt; Korrektur `8c67c287` (die
Blockgrundlage steht regulär in Fassung 3). Danach Min/Max `ce60c3f6` und der
Merge von `origin/ios_migration_september` als `b59c5653`; vor den Papieren ein weiterer Merge von origin
(`3a7b7aee`, CI-Wächter #529/#531, konfliktfrei).

## 6 Abnahme

**Tests der Etappe:** `WordVorlagenBloeckeTests` 14 Fälle, `VorlagenprueferBloeckeTests` 11,
`VorlagenfeldStandwerteTests` (9 Fälle und die Min/Max-Fälle: synthetisch drei Stände, Leerwert, NaN; für 1030 und 1019
gilt maximum − minimum = spanne; Alias), `ZahlungsgliederungTests`. 1030 und 1019 lösen 3.367 bzw. 6.734 Schlüssel ohne
Datenbank und ohne Nachholen auf; Word gegen Excel in 51 bzw. 123 Zahlenzellen in drei Szenarien; der OpenXmlValidator
ist auf allen gefüllten Dokumenten grün.

**Gate auf `b59c5653`:** Kern-Filter und Windows-Schale 0 Fehler; Tests EPOS.Kern 8.054 bestanden / 1 übersprungen /
1 rot — `TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar`, Umgebung: der Container hat Python 3.11, `sum()` in
`normiert()` von `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` summiert erst ab Python 3.12 kompensiert; schon auf
dem Basisstand rot, auf der CI grün —; EPOS.UI 6.530, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 / 1
übersprungen; Designer wiederholbar; SQL-Prüfer 1.988 Texte / 0 Fundstellen; ChartProben 174 Bilder / 0 Verstöße;
Referenzlauf 6 Projekte gegen R19 GESAMT: PASS, 2.208.587 Werte.

**Nach dem Merge `3a7b7aee`:** Kern-Filter 0 Fehler; gefilterte Tests (Vorlage, Bericht, Dokumentation) und
Dokumentationswachen grün.

Kein Schemaschritt, kein Rechenweg, nichts eingefroren außer der Schlüsselliste v3, keine Oberfläche, `EPOS.iOS/` nicht
berührt.

## 7 Commitfolge

| Commit | Inhalt |
|---|---|
| `c6ebc17c` | Vertrag: Blockkontext am Wertesatz (laufender Stand, Gebäude) |
| `184116f5` | W2: Leitversion folgt `BesteVariante` (BV-E4-1) |
| `a4013cd4` | W2: Katalog v3 mit Standwerten, Wirtschaft, Gruppe, `hat.*` |
| `4d71575f` | W2: Tests der Standwerte (1030, 1019, synthetisch) |
| `19c1198c` | W1: Block-Engine in Word und Blockregeln des Prüfers |
| `5d674466` | W1: Tests der Blöcke, Blockmarke als Tag auch ohne Klammern |
| `3b81e599` | Merge W2 |
| `f8418a1f` | Merge W1 in W2 |
| `8c67c287` | Korrektur: Blockgrundlage regulär in Katalog v3 |
| `ce60c3f6` | `vergleich.minimum/maximum`, Alias `beste_variante` (BV-E4-2) |
| `b59c5653` | Merge `origin/ios_migration_september` vor der Abnahme |
| `3a7b7aee` | Merge `origin/ios_migration_september` vor den Papieren |
| Papier-Commit | „Papiere #532: BV-E4 Blöcke, Schalter, Standwerte“ — dieses Protokoll, Konzept Rev. 6, Statusdatei, Index |

## 8 Offen

- **(a) Anwenderproben unter Windows** mit Blockvorlagen: 0, 1, 3 und 7 Varianten, Paarsicht, Gebäudeblock.
- **(b) Kapitel in einem Wiederholblock:** nur die erste Wiederholung füllt das Kapitel; der Prüfer meldet es noch nicht.
- **(c) `hat.*` im Gruppenblock `|block n`** wertet über den ganzen Bericht — `Berichtswerte` kennt keinen
  Gruppenkontext.
- **(d) Schalter je Tabelle und Bild** kommen mit BV-E5.
- **(e) Standardvorlage auf Fassung 3** heben mit BV-E5 (BV-E4-4).
- **(f) Excel** nutzt die neuen Schlüssel erst mit BV-E7/E8; die Parameter stehen dort als Anteil (÷ 100).
- **(g) TWW-Wache fassungsfest:** `math.fsum` in `normiert()` von `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py`,
  eigener Auftrag.
- **(h) Weiter offen** aus BV-E1 bis BV-E3 („Nach #512“, „Nach #520“, „Nach #528“ (e) bis (i) in der Statusdatei):
  Nachholen ohne Meldung, Datenbefunde 1029/1043, Aufräumkandidaten der Wache, Anwenderproben, Tippprobe, Regel
  „Deckblatt aus Platzhaltern“, Wiki-Upload „Berichtsvorlagen“ und die übrigen Reste.
- **(i) Setup- und iOS-Lauf:** BV-E4 berührt weder die Auslieferung noch `EPOS.iOS/` und gibt keinen eigenen Anlass.

**Logbuch-Vorschlag** (für den nächsten Wiki-Upload, Version beim Anwender zu erfragen, Stichwort `bericht`): „Eigene
Word-Berichtsvorlagen können Abschnitte je Variante oder je Gebäude wiederholen und Abschnitte nach Bedingungen ein- oder
ausblenden; dazu stehen Einzelwerte je Variante, der Wirtschaftlichkeit und des Variantenvergleichs als Platzhalter
bereit.“

## 9 Dateien

8 Commits ohne Merges (Tafel in Abschnitt 7), `git diff --stat 942bd1ad ce60c3f6`: 30 Dateien, +7.537/−289 Zeilen.

| Bereich | Dateien |
|---|---|
| Kern, Vorlagen | neu `EPOS.Kern/Allgemein/Bericht/Vorlagen/WordVorlagenbloecke.cs`, `VorlagenprueferBloecke.cs`, `Vorlagenfeldkatalog.Standwerte.cs`; geändert `Berichtswerte.cs`, `Vorlagenfeld.cs`, `Vorlagenfeldkatalog.cs`, `Vorlagenpruefer.cs`, `WordVorlagenergebnis.cs`, `WordVorlagenfueller.cs`, `WordVorlagentexte.cs` |
| Kern, Bericht | `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs`, `BerichtTexte.cs`, `Berichtsbedarf.cs` |
| Kern, Wirtschaftlichkeit | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/Zahlungsgliederung.cs` |
| Kern, KI und Ressourcen | `EPOS.Kern/Allgemein/KI/HilfeWissenBerichtsvorlagen.cs`, `KiMeldungskennung.cs`; `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| Tests Kern | neu `WordVorlagenBloeckeTests.cs`, `VorlagenprueferBloeckeTests.cs`, `VorlagenfeldStandwerteTests.cs`, `Messlatten/Vorlagenfeldkatalog_v3.txt`; geändert `BerichtKiKennungenTests.cs`, `BerichtswerteTests.cs`, `KiDialogaufrufTests.cs`, `VorlagenfeldkatalogWacheTests.cs`, `VorlagenprueferTests.cs`, `WordVorlagenfuellerTests.cs`, `ZahlungsgliederungTests.cs` |
| Papiere (dieser Auftrag) | dieses Protokoll, Konzept Rev. 6, `Dokumentation/aktuell/Status_iOS_Migration.md`, `Dokumentation/LIESMICH.md` |
