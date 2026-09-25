# BV-E2 — Kapitel und Häkchen (Protokoll)

Etappe BV-E2 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitt 13). Auftrag #520, Anwenderauftrag vom 26.09.2026: „starte mit BV-E2“. Der gültige Stand steht im Konzept
(Rev. 4) und in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier steht, wie es geworden ist. Vorgänger:
[`BV_E1_Vorlagenwahl_Protokoll.md`](BV_E1_Vorlagenwahl_Protokoll.md). Zweig `konzept-berichtvorlagen` ab `84ac5aa8` (BV-E1
samt `origin/ios_migration_september`), umgesetzt am 26.09.2026; Fable 5.1 hat orchestriert, vier Agenten (Opus 5.5)
haben in eigenen Worktrees gearbeitet — 16 Commits, 77 Dateien, +8.502/−979 Zeilen (Ressourcen und Designer
eingerechnet; `EPOS.UI.Daten/Bericht/AnhangEKapitel.cs` entstand und entfiel wieder). Kein Schemaschritt, kein Rechenweg
berührt, keine Basis neu eingefroren, `EPOS.iOS/` nicht berührt.

| Agent | Gegenstand | Commits | Zusammenführung |
|---|---|---|---|
| K1 Kern | Katalog v2, `Deckt` und `Gedeckt`, Kapitel in Engine und Prüfer, Kapitelstellen, Logo, Standardvorlage, Messlatten | `d53ae02a`, `db47763b`, `8754bddd`, `f8ae2474`, `991f9926` | übernahm V und H mit `5f1052ea` |
| V Werkzeug | `beispiel` mit `--standard` und `--katalogfassung`, Kapitelköpfe und Logoplatzhalter, `custom.xml`, Beispielvorlage | `0e8771b3`, `88f802d8`, `2e3c8111` | auf den Zweig vorgespult |
| H Hülle und Oberfläche | Häkchenliste, Stelle in der Anhang-E-Überlagerung, Feld „Logo“ der Einstellungen | `bd2433aa`, `c171c0bd`, `a851447a` | `3380ba8a` |
| I Integration | Kapitelstellen und Anhang-E-Stelle aus dem Kern, Häkchentitel, Logo über den Controller, Häkchen „Deckblatt“, Werkzeugzeile der Wurzel-`CLAUDE.md` | `5954746d`, `9e50ca99`, `6482209b`, `77f38310`, `1cc3bcce` | übernahm K1 mit `eb61279e`; End-Merge mit `origin/ios_migration_september` (`ba798d8a`): `09787fc9` |

---

## 1 Entscheid BV-E2-1

**Anwenderentscheid vom 26.09.2026** zum Logo der Kopfzeile (BV-Q8, aus BV-E1 an BV-E2 verwiesen), gefasst nach der
Erläuterung zweier Lesarten:

| Lesart | Inhalt | Entscheid |
|---|---|---|
| (a) Entfall | Beispiel- und Standardvorlage tragen kein Logo; wer eines will, setzt es in seiner Kopie der Vorlage ein | verworfen |
| **(b) Bildplatzhalter** | Das Logo der Kopfzeile bleibt in Beispiel- und Standardvorlage als Platzhalterbild stehen — Alternativtext `{{bild.ersteller.logo}}`, neutrales graues Bild „Logo“, 150 × 82 Pixel — und wird beim Füllen durch die Bilddatei der Einstellung `BerichtLogo` ersetzt (Einstellungen › Bericht, Feld „Logo“ mit „Durchsuchen“ und „Entfernen“; PNG oder JPEG bis 5 MB), mit ihrem Seitenverhältnis in den Rahmen eingepasst. Ohne Logo entfällt das Bild; eine fehlende oder unlesbare Datei nennt der Berichtslauf als Warnung | **gewählt** |

Zweck, wie ihn die Werkzeug-LIESMICH festhält: Der Bericht nennt den Ersteller, nicht den Hersteller (BV-Q8) — das Logo
der Kopfzeile ist das des Lizenznehmers. Als Bildplatzhalter behält die Kopfzeile ihre Gestaltung, und jeder Lizenznehmer
bekommt sein Logo über die Einstellung, ohne eine eigene Vorlage anzulegen; wer ein festes Bild will, ersetzt es in seiner
Kopie und entfernt den Alternativtext.

**Folgen:** Der Katalog führt `bild.ersteller.logo` (Art Bild, Kontext Installation, nur Word, Fassung 2) — der erste
Bildplatzhalter, aus BV-E5 vorgezogen; die übrigen Bildschlüssel bleiben dort. Das Werkzeug setzt das Platzhalterbild in
jede Art von `beispiel` (Abschnitt 3), die Engine füllt es in allen Teilen (2.3), der `BerichtsvorlagenCtrl` lädt die Datei
(2.6), die Rubrik „Bericht“ der Einstellungen trägt das Feld (4.3). **Die Stilvorlage `Berichtsvorlage.docx` bleibt mit
Logo** — sie ist der Rückfall des Codes (Entscheid BV-E1-1) und die Quelle des Werkzeugs, das das Logo erst in seinen
Ausgaben tauscht. Das Konzept trägt den Entscheid mit Rev. 4 in 6.3, 6.5, 10.3, 14 (BV-Q8), Anhang A und B.3.

## 2 Kern (K1)

### 2.1 Katalog v2

`Vorlagenfeldkatalog.KATALOGFASSUNG = 2`: 184 Schlüssel und ein Alias (`bericht.programmversion` → `ersteller.version`).

| Bereich | Schlüssel | neu in Fassung 2 |
|---|---|---|
| `bericht.*` | 9 | – |
| `text.*` | 14 | sieben Kapitelköpfe `text.kapitel_<name>` |
| `ersteller.*` | 3 | – |
| `projekt.*` | 8 | – |
| `kapitel.*` | 9 | alle: `deckblatt`, `inhalt`, `projekt`, `komponenten`, `ergebnisse`, `vergleich`, `wirtschaftlichkeit`, `anhang`, `anhang_e` |
| `baustein.*` | 8 | alle, je Häkchen ein Schalter |
| `bild.*` | 1 | `bild.ersteller.logo` |
| `stamm.kennzahl.*` | 44 | – |
| `kennzahl.<k>.beschriftung`, `.einheit` | 88 | – |

Handgepflegt sind 52 Einträge (27 der Fassung 1, 25 neue mit `Seit` 2), je Kennzahl entstehen drei. Die Liste ist in
`EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v2.txt` eingefroren; die Wache vergleicht jede Fassung (v1 und v2) mit dem
Katalog und verlangt, dass jeder eingefrorene Schlüssel lebendig oder Alias bleibt.

- **`Berichtskapitel`** (neu in `EPOS.Kern/Allgemein/Bericht/Vorlagen/`): an EINER Stelle die Zuordnung von
  Kapitelplatzhalter, Häkchen (`BerichtsKonfiguration.B_*`), Schalter, Kapitelkopf und `IBerichtsBaustein` in der Folge des
  heutigen Berichts; `WordBerichtGenerator.AktiveBausteine` liest sie. Anhang E ist ein eigenes Kapitel ohne eigenen
  Schalter am Häkchen „Wirtschaftlichkeit“; Deckblatt und Inhaltsverzeichnis haben keinen Kapitelkopf.
- **Kapitelkopf** `text.kapitel_<name>`: die Überschrift, die der Baustein selbst druckt, in der Sprache des Berichts —
  deutsch etwa „Berechnungsergebnisse je Variante“, nicht der Häkchentitel „Ergebnisse je Variante“; beim Anhang E der
  Titel der Checkliste (`WIRT_AE_TITEL`). **Schalter** `baustein.<name>`: ja, wenn das Häkchen gesetzt ist; er wirkt erst
  mit BV-E4 in `{{#wenn}}`.
- **Häkchentitel zweisprachig** in `MyResource` (`BK_BER_BAUSTEIN_*`, `BausteinDef.TitelIn`,
  `BerichtsKonfiguration.Titel(schluessel, englisch)`): die deutschen Wort für Wort wie zuvor, englisch „Title page“,
  „Table of contents“, „Project description“, „Components & variants“, „Results per variant“, „Variant comparison“,
  „Economic viability“, „Appendix“ (Konzept 13, „Bausteintitel nach MyResource“).
- **Ressourcen +60 je Sprache:** Häkchentitel `BK_BER_BAUSTEIN_*` (8); Beschreibungen `VF_KAPITEL__*`, `VF_BAUSTEIN__*`,
  `VF_TEXT__KAPITEL_*`, `VF_BILD__ERSTELLER__LOGO` (25); Prüfmeldungen `VF_PRUEF_*` (7) und ihre `KI_FRAGE_*` (3); die
  Logo-Warnungen `BV_VORLAGEN_LOGO_*` (3) und der Leergrund `BV_GRUND_KEIN_LOGO` („kein Logo eingestellt“); die Texte der
  Spalte „Stelle“ `WIRT_AE_*` (13).

### 2.2 Deckt und Gedeckt

- **`Deckt`** (Konzept 5.1): `bericht.inhalt` deckt alle neun `kapitel.*`; jedes Kapitel deckt seinen Kapitelkopf und
  seinen Schalter, dazu die Einzelschlüssel, die sein Baustein schreibt — das Deckblatt die zehn Deckblattangaben
  (`bericht.titel`, `bericht.untertitel`, `projekt.kunde`, `projekt.bearbeiter`, `bericht.varianten.liste`, `bericht.datum`,
  `bericht.gebaeudemodell.ausweis`, `ersteller.firma`, `ersteller.programm`, `ersteller.version`), die Projektbeschreibung
  alle acht `projekt.*`, Ergebnisse und Vergleich alle `stamm.kennzahl.*` und `kennzahl.*` (der Vergleich dazu
  `bericht.emissionsmodus` und `bericht.varianten.anzahl`), der Anhang `bericht.warnungen`.
- **`Vorlagenfeldkatalog.Gedeckt(schluessel)`** rechnet einen Fixpunkt: die Schlüssel der Vorlage, dazu alles, was deren
  Kapitel decken, bis nichts mehr hinzukommt. Ein Kapitel, das die Vorlage nicht führt, gilt als gedeckt, wenn sie jeden
  Einzelschlüssel führt, den es schreiben würde (ohne Schalter und Kapitelkopf, und nur, wenn es solche hat) — so deckt ein
  Deckblatt aus Platzhaltern `kapitel.deckblatt`, und `bericht.inhalt` gilt, sobald jedes Kapitel gilt. Aliasse zählen für
  ihren Eintrag. `Vorlagenfeldkatalog.Deckblattangaben` sind die zehn Deckblattangaben; die Deckung verlangt alle zehn, das
  Merkmal `DeckblattAusPlatzhaltern` von Prüfer und Engine (2.4) schon eine (Abschnitt 8).
- **Wo es wirkt:** in der Deckungswache (Konzept 12: jeder Word-Schlüssel der laufenden Fassung steht in der
  Standardvorlage, direkt oder gedeckt; sie verlangt dazu den Logoplatzhalter), in `Pruefbefund.HatWirtschaftlichkeit` (das
  Kapitel oder ein Bereich der Wirtschaftlichkeit, direkt oder gedeckt — die Grundlage des zweiten Einstiegs) und im Hinweis
  „Neues Kapitel“ (genutzt heißt gedeckt).

### 2.3 Engine

`WordVorlagenfueller`, `WordKontext`, `WordVorlagenstile`:

- **Kapitelplatzhalter** `{{kapitel.<name>}}` allein im Absatz des Rumpfs oder als Inhaltssteuerelement auf Blockebene —
  nicht in Tabellenzelle, Textfeld, Kopf- oder Fußzeile, Fuß- oder Endnote. Je Kapitel gilt die erste gültige Stelle
  (getippte Platzhalter in Dokumentfolge, danach die Steuerelemente); jede weitere bleibt gelb stehen
  (`Fuellbefundart.Doppelt`).
- **Entfall** (Konzept 5.3): Ist das Häkchen abgewählt oder hat das Kapitel keine Daten, schreibt es nichts, und ein
  unmittelbar davor stehender Absatz im Format „EPOS Kapitelkopf“ (aufgelöst über `w:name`, ohne Abschnittsangabe) entfällt
  mit — samt dem, was der Baustein vor den Kapitelkopf gesetzt hätte.
- **Sammelanker** `{{bericht.inhalt}}`: die angehakten Kapitel ohne die einzeln geführten.
- **`|ohne titel`:** Die erste Überschrift 1 oder 2 des Bausteins — seine Kapitelüberschrift — entfällt; was er davor
  schreibt (der Seitenumbruch vor Anhang E), kommt vor den Kapitelkopf der Vorlage, damit die Überschrift nicht allein am
  Seitenende hängt.
- **`|ebene n`** (1 bis 9): Die Überschriften des Bausteins rücken um n − 1 Ebenen tiefer, höchstens bis 9; die
  Überschriften 4 bis 9 sucht die Engine über `w:name` („heading n“) und legt sie sonst an (`WordVorlagenstile.EBENE_MAX`).
- **Logo:** Ein Bild mit dem Alternativtext `{{bild.ersteller.logo}}` bekommt in jedem Teil (Rumpf, Kopf- und Fußzeilen) die
  Datei der Einstellung — neuer Bildteil am Teil des Bildes, die Maße mit dem Seitenverhältnis in Breite und Höhe von
  `wp:extent` eingepasst (fehlt ein Maß, gilt das andere), der Alternativtext wird der Dateiname, alter Bildteil und
  SVG-Fassung des Platzhalterbildes entfallen. Ohne Logo entfällt das Bild samt Lauf und Bildteil, ein danach leerer Absatz
  auch, außer er steht allein in seinem Teil oder trägt eine Abschnittsangabe; die Warnung der Erstellerangaben nennt der
  Lauf einmal. Andere Bildschlüssel bleiben stehen (BV-E5), in Fuß- und Endnoten steht ein Bild falsch.
  `Bildinhalt.Aus(daten, dateiname)` nimmt nur PNG und JPEG und liest die Maße aus dem Dateikopf; die Ausrichtung eines
  EXIF-JPEG wertet es nicht aus.
- **`Fuellergebnis.Kapitelstellen`:** je Kapitel die Überschrift vor dem Anker im gefüllten Bericht — die Stellen, die die
  Anhang-E-Checkliste des Berichts über `WordKontext.Kapitelstellen` liest.

### 2.4 Prüfer

- `kapitel.*` nach denselben Ortsregeln wie die Engine; eine andere Stelle ist „Platzhalter passt nicht an diese Stelle“.
- **`Pruefbefund.Bausteine`:** die Häkchen in Berichtsfolge, deren Kapitel die Vorlage an gültiger Stelle führt; mit
  Sammelanker oder ganz ohne Platzhalter (die Engine hängt dann den Sammelanker an) alle acht; der Anhang E zählt unter
  „Wirtschaftlichkeit“. Die Standardvorlage führt alle außer dem Deckblatt.
- **`HatKapitel`** (ein `kapitel.*` oder `baustein.*`), **`Kapitelstellen`** (je Stellenschlüssel — die acht Häkchen und
  `anhang_e` — der Kapitelkopf unmittelbar vor dem Anker, mit `|ohne titel` sonst die nächste Überschrift davor, Platzhalter
  darin aufgelöst; ohne sie die eigene Überschrift des Bausteins; über den Sammelanker die eigenen Überschriften; `null`,
  wenn die Vorlage das Kapitel nicht führt — ohne Rücksicht auf die Häkchen) und **`DeckblattAusPlatzhaltern`** (eine
  Deckblattangabe im Rumpf).
- **Meldungen:** Hinweis „Kapitel {{…}} steht mehrfach in der Vorlage – gefüllt wird nur die erste Stelle“
  (`VF_PRUEF_KAPITEL_DOPPELT`); Hinweis „Neues Kapitel …“ nur, wenn `custom.xml` eine ältere Katalogfassung trägt und die
  Vorlage das Kapitel weder führt noch deckt; `baustein.*` außerhalb von `{{#wenn}}` ist ein Ortsfehler, dazu der Hinweis
  „… erst in einer späteren Programmfassung“ (`VF_PRUEF_SPAETER`); `bild.*` als getippter Text oder im Steuerelement ist
  ein Fehler „erst in einer späteren Programmfassung“ mit dem Rat, ein Bild einzufügen und den Schlüssel als Alternativtext
  einzutragen; ein unbekannter Bildschlüssel ist ein Fehler; das Platzhalterbild ohne eingestelltes Logo ist kein Befund.
  Führt die Vorlage den Anhang E, warnt „Anhang E ohne Stelle für Kapitel „…“ – die Checkliste nennt dort „nicht im
  Bericht““ (`VF_PRUEF_ANHANG_E_STELLE`) je Kapitel, auf das die Checkliste verweist (Deckblatt, Projektbeschreibung,
  Komponenten, Ergebnisse, Vergleich, Wirtschaftlichkeit, Anhang) und das die Vorlage nicht führt.
- **KI-Wissen:** die drei Kennungen in `KiMeldungskennung.Berichtsvorlagen`, je ein Abschnitt in
  `HilfeWissenBerichtsvorlagen` und `KI_FRAGE_*` in beiden Sprachen.

### 2.5 Kapitelstellen und Anhang E

- **`BerichtCtrl.KapitelstellenDerVorlage(konfig, englisch)`**, ohne zu füllen: die Schnellprüfung der Vorlage, die der
  Lauf nähme (`BerichtsvorlagenCtrl.VorlageFuer`); lässt sie sich nicht lesen, die Standardvorlage; fehlt auch die, der
  bisherige Weg (`Berichtskapitel.EigeneStellen`). Schlüssel sind die acht Häkchen und `anhang_e`, Wert die Überschrift, unter
  der das Kapitel im Bericht steht; `null` heißt „nicht im Bericht“ — die Vorlage führt das Kapitel nicht, oder sein
  Häkchen fehlt (ein Deckblatt aus Platzhaltern steht unabhängig vom Häkchen).
- **`AnhangECheckliste.Punkte(lage, stellen)`** baut daraus die Spalte „Stelle“: das Kapitel in Anführungszeichen, bei
  Unterstellen mit „›“, oder „nicht im Bericht“; ohne Stellen wortgleich wie zuvor. Die englischen Texte nennen die
  englischen Überschriften, die der Bericht druckt. Die Checkliste im Bericht nimmt dieselben Stellen aus dem Füllen.

### 2.6 Logo im Controller

`BerichtsvorlagenCtrl`: Einstellung `EINSTELLUNG_LOGO = "BerichtLogo"` (Dateipfad, leer = kein Logo), `GRENZE_LOGO` 5 MB,
`LogoPfad`, `LogoVorhanden()` (lädt wie der Lauf), `SchreibeLogo(pfad)` (leer entfernt die Einstellung). `Ersteller()` lädt
das Logo EINMAL in die `Erstellerangaben` (`Logo`, `LogoDateiname`, `LogoWarnung`); fehlt die Datei, ist sie zu groß oder
kein PNG oder JPEG, bleibt das Logo leer, und die Warnung lautet „Logo nicht gefunden: …“, „Logo zu groß: … (x MB,
höchstens 5 MB)“ bzw. „Logo ist kein PNG- oder JPEG-Bild: …“.

### 2.7 Standardvorlage, Messlatten, Tests

- **`Berichtsvorlage_Standard.docx`** neu mit `beispiel --standard` (`8754bddd`): voller Aufbau (Konzept Anhang B.3), keine
  Kommentare, `EPOS.Katalogfassung` 2. Beide Prüfstufen ohne Befund (36 Platzhalter, 33 Schlüssel); sie führt jedes
  Kapitel außer dem Deckblatt einzeln, trägt das Deckblatt aus Platzhaltern, und ihre Stellen sind ihre Kapitelköpfe.
- **Messlatten des Vorlagenwegs** `EPOS.Kern.Tests/Messlatten/Bericht_Word_1030_Vorlage.txt` (92 Zeilen) und
  `Bericht_Word_Gruppe_Vorlage.txt` (121): der Rumpf, den die Standardvorlage über die Engine ergibt. Der Kapitelteil ab
  „Inhalt“ ist zeilengleich zur Messlatte des alten Wegs (`Bericht_Word_1030.txt`, `Bericht_Word_Gruppe.txt`) — einzige
  Umschrift ist das Format `EPOSKapitelkopf` statt `Heading1`, und der Seitenumbruch vor Anhang E steht vor dessen
  Kapitelkopf; abweichend ist allein das Deckblatt aus der Vorlage (Konzept 11 Nr. 1, Anhang B.3). Kopf- und Fußzeile hält
  `WordVorlagenfuellerTests`, weil das Logo an der Fassung des Werkzeugs hängt. Der alte Weg (Stilvorlage) bleibt
  unverändert grün.
- **Tests** (Testmethoden je Klasse vorher → nachher): `WordVorlagenfuellerTests` 27 → 38, `VorlagenprueferTests` 41 → 46,
  `VorlagenfeldkatalogWacheTests` 19 → 23, `BerichtCtrlVorlagenTests` 12 → 14, `BerichtsvorlagenCtrlTests` 20 → 22,
  `AnhangEChecklisteTests` 10 → 12, `BerichtBlattstrukturWacheTests` 17 → 18, `BerichtVorlagenMesslatteTests` 3 → 4,
  `BerichtswerteTests` 24 → 25 — zusammen 29 neue Methoden (Bericht K1: 208 → 237);
  `AuslieferungsvorlagenWacheTests`, `BerichtsvorlageDateiWacheTests` und `KiDialogaufrufTests` nachgezogen.

## 3 Werkzeug und Vorlagen (V)

- **`beispiel <quelle> <ziel> [--standard | --sammelanker] [--katalogfassung <n>]`**: ohne Schalter die Beispielvorlage
  (voller Aufbau, drei Kommentare, `EPOS.Vorlage` = `beispiel`), `--standard` die Standardvorlage im vollen Aufbau ohne
  Kommentare (`standard`), `--sammelanker` die Stufe von BV-E1 (Rumpf nur `{{bericht.inhalt}}`, `standard-sammelanker`);
  `--katalogfassung` setzt `EPOS.Katalogfassung` (Vorgabe 2). `--standard` und `--sammelanker` schließen einander aus;
  `Berichtsvorlage_Standard.docx` entsteht nur mit einem der beiden, `Berichtsvorlage_Beispiel.docx` nur ohne — sonst
  Rückgabe 2. So trägt die ausgelieferte Standardvorlage nie die Kommentare der Lehrvorlage.
- **Aufbau:** je Kapitel der Kapitelkopf `{{text.kapitel_<name>}}` im Format „EPOS Kapitelkopf“, darunter
  `{{kapitel.<name>|ohne titel}}` für die sieben Kapitel von der Projektbeschreibung bis Anhang E; davor das Deckblatt und
  `{{kapitel.inhalt}}` (Konzept Anhang B.3).
- **Logo als Platzhalterbild:** Das Logo der Kopfzeile der Stilvorlage bleibt an Ort, in Größe und Umbruch (`wp:inline`,
  857250 × 466725 EMU), trägt `{{bild.ersteller.logo}}` im Alternativtext und zeigt das neutrale Bild
  `Werkzeuge/Berichtsvorlage/Logoplatzhalter.png` (150 × 82 Pixel wie das Logo der Quelle, 160 dpi, 1.446 Byte: hellgrau
  mit Rahmen und dem Wort „Logo“), eingebettet als Ressource des Werkzeugs. Den Tausch macht `System.IO.Packaging` mit
  festem Teilnamen und der Beziehungskennung des alten Logos, jeder Eintrag des Pakets bekommt den Zeitstempel der Quelle —
  zwei Läufe schreiben byte-gleich. Das Werkzeug bricht ab, wenn das Pixelmaß des Platzhalters nicht dem Logo der Quelle
  entspricht.
- **`docProps/custom.xml`:** `EPOS.Katalogfassung` (`vt:i4`) und `EPOS.Vorlage` (`vt:lpwstr`); vorhandene Eigenschaften
  bleiben.
- **Beispielvorlage neu erzeugt** (`88f802d8`): 35 Textplatzhalter (Rumpf 31, Kopfzeile 1, Fußzeile 3) und ein
  Bildplatzhalter; drei Kommentare — am Deckblatt (Platzhalter, Formatangaben, Kopf- und Fußzeile samt Logo; Word erlaubt in
  Kopf- und Fußzeilen keine Kommentare), an `{{kapitel.inhalt}}` (die Kapitelplatzhalter) und am ersten Kapitelkopf (ein
  Platzhalter für den Kapiteltitel, der eigenem Text weichen darf). Sie steht weiter in keinem Lieferweg.
- **Prüfungen** vor dem Ersetzen des Ziels: Validator Office 2007–2021, Stilregeln, Platzhalterregeln (jeder
  Kapitelplatzhalter mit `|ohne titel` unmittelbar unter seinem Kapitelkopf, genau ein Bild in der Kopfzeile — der
  Logoplatzhalter —, die Zahl der Kommentare je Art, die Werte in `custom.xml`). Wache `BerichtsvorlageDateiWacheTests`
  20 Fälle; `Werkzeuge/Berichtsvorlage/LIESMICH.md` fortgeschrieben.

## 4 Hülle und Oberfläche (H)

### 4.1 Häkchenliste der Berichtsseite

Die Hülle leitet aus der Schnellprüfung der gewählten Vorlage den Datensatz `Kapitelstand(NichtEnthalten,
InhaltAusVorlage, DeckblattAusVorlage)` ab (`BerichtsvorlagenGaben.Kapitel(Pruefbefund)`), bei jedem Nachladen der Gruppe
„Vorlage“ neu; die Seite verbindet ihn mit dem Ausgabeformat. Ausgegraut ist ein Eintrag nach Konzept 10.2 nur, wenn weder
die Word- noch die Excel-Ausgabe das Kapitel führt — die Excel-Mappe folgt bis BV-E7 den Häkchen wie heute (BV-Q2):

| Gewählte Vorlage | Ausgabe Word | Ausgabe Beide | Ausgabe Excel |
|---|---|---|---|
| mit einzelnen Kapiteln | jedes nicht geführte Kapitel ausgegraut, Grund „in dieser Vorlage nicht enthalten“ | nur die reinen Word-Bausteine (Deckblatt, Inhaltsverzeichnis, Anhang), wenn nicht geführt | alle frei |
| Standardvorlage (Deckblatt aus Platzhaltern; Grund von I) | „Deckblatt“ ausgegraut, Grund „Deckblatt kommt aus der Vorlage“; die übrigen frei | ebenso | alle frei |
| mit `{{bericht.inhalt}}` | alle frei | alle frei | alle frei |
| nur Einzelplatzhalter, kein Kapitel | statt der Liste die leise Zeile „Den Inhalt bestimmt die Vorlage – sie führt einzelne Platzhalter, aber kein Kapitel.“ | Liste, die reinen Word-Bausteine ausgegraut | alle frei |
| ohne Platzhalter, nicht lesbar oder keine geprüft | alle frei | alle frei | alle frei |

- **Weiche Sperre:** Das Häkchen bleibt gespeichert und wirkt wieder, sobald eine Vorlage das Kapitel führt; der Grund
  steht am Eintrag. Ein Klick meldet ihn in einem Banner mit Verfall („„…“ ist in dieser Vorlage nicht enthalten – das
  Häkchen bleibt gespeichert und wirkt wieder, sobald eine Vorlage das Kapitel führt.“ bzw. der Deckblatt-Grund);
  `Mehrfachauswahl` meldet den Versuch über `GesperrtVersucht`, „Alle“ lässt gesperrte Einträge unverändert.
- Die Häkchen tragen ihren Titel in der Sprache der Oberfläche (mit I aus `BausteinDef.TitelIn` des Kerns).

### 4.2 Stelle in der Anhang-E-Überlagerung

- Die Überlagerung „Anhang-E-Checkliste…“ der Wirtschaftlichkeitsseite nennt in der Spalte „Stelle“ die Kapitel der auf der
  Berichtsseite gewählten Vorlage des Stammprojekts: Delegat `Kapitelstellen` der Hülle, durchgereicht
  `BerichteKostenHuelle` → `WirtschaftlichkeitSeiteGaben` → `WirtschaftlichkeitSeite` → `AnhangEChecklisteKnopf`. Gefragt
  wird mit den Häkchen des zweiten Einstiegs — den gespeicherten und stets „Wirtschaftlichkeit“
  (`BerichtSeiteGaben.BausteineFuerVergleich`) —, wie „Bericht erzeugen“ auf derselben Seite.
- Unter der Tafel steht eine leise Zeile: „Die Stellen im Bericht sind bezogen auf die Standardvorlage.“, wenn keine eigene
  Vorlage gewählt ist, sonst „Die Stellen im Bericht nennen die Kapitel der Vorlage „…“.“
- H baute die Spalte zunächst in der Hülle (`AnhangEKapitel.cs`); I ersetzte sie durch die Checkliste des Kerns (5.1).

### 4.3 Einstellungen › Bericht: Feld „Logo“

- „Logo:“ als `Dateiwahl` mit „Durchsuchen“ (Dateiwahl der Plattform über `Dienste.Datei`, Filter „Bilder
  (*.png;*.jpg;*.jpeg)“; auf iOS kopiert sie die Datei in die Sandbox) und „Entfernen“; darunter „Firmenlogo für die
  Kopfzeile des Berichts – eine PNG- oder JPEG-Datei; leer heißt ohne Logo.“ und, wenn die Datei im Feld fehlt, „Datei nicht
  gefunden.“ — übernommen wird der Pfad trotzdem. Geschrieben wird im OK-Weg wie Firma und Vorlagenordner; „Standardwerte“
  leert das Feld.
- Parameter `Logo`, `LogoChanged`, `LogoWaehler`, `LogoVorhanden`; ohne Rückweg der Hülle steht kein Feld. KI-Feld
  `bericht_logo` der Programmeinstellungen (`KiDialoge`, `EinstellungenKiSicht`); Eingabebilanz und KI-Dialogkatalog
  nachgezogen.
- **Ressourcen** H: 15 je Sprache (Häkchenliste 3, Anhang-E-Stelle 5, Logo 7). **Tests:** 50 neue Testmethoden —
  `BerichtSeiteHaekchenTests` 10, `MehrfachauswahlTests` 4, `BerichtsvorlagenHaekchenHuelleTests` 8, `AnhangEStellenTests` 5,
  `AnhangEStellenHuelleTests` 8, `EinstellungenBerichtLogoTests` 10, `EinstellungenBerichtLogoHuelleTests` 5; nach K1 und I
  stehen 11, 4, 7, 5, 9, 10 und 4 Methoden in diesen Klassen.

## 5 Integration und Entscheidungen der Agenten

### 5.1 Integration (I)

- **Kapitelstellen aus dem Kern:** der Delegat `Kapitelstellen` der Hülle ist `BerichtCtrl.KapitelstellenDerVorlage`; die
  Spalte „Stelle“ baut `AnhangECheckliste.Punkte` des Kerns — `AnhangEKapitel.cs` entfällt samt den Ressourcen
  `WIRT_AE_NICHT_IM_BERICHT` und `WIRT_AE_STELLE_FEHLT`; die Überlagerung fragt mit gesetztem Häkchen „Wirtschaftlichkeit“
  und zeigt eine Zeile je Punkt (`5954746d`).
- **Häkchentitel** über `BausteinDef.TitelIn` des Kerns statt einer Titelliste der Hülle (`9e50ca99`).
- **Logo der Einstellungen** über `BerichtsvorlagenCtrl.LogoPfad` und `SchreibeLogo`; die Existenzprüfung des Feldwerts
  bleibt in der Hülle (`EinstellungenBerichtGaben.LogoVorhanden`), `BerichtsvorlagenCtrl.LogoVorhanden()` bleibt ungenutzt
  (`6482209b`, Abschnitt 8).
- **Häkchen „Deckblatt“** bei einer Vorlage mit Deckblatt aus Platzhaltern, also bei der Standardvorlage: ausgegraut mit dem
  Grund „Deckblatt kommt aus der Vorlage“ (`Kapitelstand.DeckblattAusVorlage`, Ressourcen
  `BK_BER_VORLAGE_DECKBLATT_AUS_VORLAGE`, `BK_BER_VORLAGE_MSG_DECKBLATT_AUS_VORLAGE`; `77f38310`).
- **Wurzel-`CLAUDE.md`:** Werkzeugzeile mit `--standard` und `--katalogfassung` (`1cc3bcce`).

**Zusammenführung:** `3380ba8a` nahm H auf den Zweig, auf den V vorgespult war; `5f1052ea` führte V und H in K1, danach
passte `f8ae2474` die Hülle-Tests von H an den Kern an (Deckblatt ausgegraut, Standardstelle aus dem Kern); `eb61279e`
übernahm K1 in I; der End-Merge `09787fc9` mit `origin/ios_migration_september` (`ba798d8a`) löste den Konflikt in
`Resource.resx` und `Resource.en-US.resx` beidseitig auf — 11.698 Einträge je Sprache: 11.572 der Basis, BV-E2 netto +74
(K1 +60, H +15, davon `WIRT_AE_STELLE_KAPITEL` gleich mit K1 und einmal geführt, I +2 und −2), `origin` +52.

### 5.2 Abweichungen vom Konzept (Rev. 3)

Das Konzept Rev. 4 trägt sie nach.

| Konzept | Umgesetzt | Grund |
|---|---|---|
| 13 (BV-E2): Logo der Kopfzeile als Bildplatzhalter oder Entfall; 6.5 und 13: Bildplatzhalter erst mit BV-E5 | `bild.ersteller.logo` mit der Einstellung `BerichtLogo`, in BV-E2 vorgezogen; die übrigen Bildschlüssel bleiben BV-E5 | Entscheid BV-E2-1 (Abschnitt 1) |
| 10.2: ausgegraut nur „in dieser Vorlage nicht enthalten“ | zweiter Grund „Deckblatt kommt aus der Vorlage“ | die Standardvorlage trägt ihr Deckblatt aus Platzhaltern (Anhang B.3); ihr Häkchen wirkt dort nicht |
| 4.8, 5.3: `\|ebene 2` rückt eine Ebene tiefer | `\|ebene n` (1 bis 9) rückt n − 1 Ebenen tiefer; Überschrift 4 bis 9 werden bei Bedarf angelegt | allgemein statt nur Ebene 2 |
| 6.3, B.3: je Kapitel eine Überschrift im Format „EPOS Kapitelkopf“ | die Überschrift ist der Platzhalter `{{text.kapitel_<name>}}` | die Standardvorlage bleibt sprachneutral (4.9) |
| 10.2: „die Liste bleibt, sobald die Vorlage `kapitel.*` oder `baustein.*` nutzt, sonst ‚Den Inhalt bestimmt die Vorlage‘“ | die Zeile steht nur bei Ausgabe Word; mit Excel bleibt die Liste, bei „Beide“ sind nur die reinen Word-Bausteine ausgegraut | die Mappe folgt den Häkchen bis BV-E7 wie heute (BV-Q2) |
| 11 Nr. 3: die Stelle in der Checkliste des Berichts | dazu die Stelle in der Überlagerung der Wirtschaftlichkeitsseite, aus der gewählten Vorlage, mit Bezugszeile | Konzept 13, „Stelle in der Anhang-E-Überlagerung“ |

**Entscheidungen der Agenten**, wo das Konzept die Einzelheit offenließ:

- **K1:** `Berichtskapitel` als die eine Zuordnung, Anhang E als eigenes Kapitel ohne Schalter; `Gedeckt` als Fixpunkt;
  „Deckblatt aus Platzhaltern“ schon bei einer Deckblattangabe im Rumpf (Abschnitt 8); der Kapitelkopf nennt die gedruckte
  Überschrift, nicht den Häkchentitel; die Messlatte des Vorlagenwegs hält nur den Rumpf, mit fester Programmfassung
  `9.9.9.9`; ein doppeltes Kapitel ist ein Hinweis, kein Fehler.
- **V:** das Platzhalterbild als feste Datei statt einer Zeichnung zur Laufzeit (die hinge an den Schriften des Rechners);
  der Tausch über `System.IO.Packaging` statt über das SDK (das legte den Bildteil an der Paketwurzel an und vergäbe eine
  zufällige Beziehungskennung); die Erläuterung des Logos im Kommentar am Deckblatt; die Regel Zielname ↔ Art.
- **H:** weiche Sperre statt Ausblenden, das Häkchen bleibt gespeichert; die leise Zeile nur bei Ausgabe Word; „Datei nicht
  gefunden.“ prüft den Feldwert vor dem Speichern, nicht die Einstellung.
- **I:** die Überlagerung fragt mit den Häkchen des zweiten Einstiegs; die Existenzprüfung des Logos bleibt in der Hülle.

## 6 Abnahme

In den Agenten-Worktrees:

- **K1:** die berührten Kern-Testklassen grün (29 neue Methoden), die Standardvorlage in beiden Prüfstufen ohne Befund,
  Deckungswache und Messlatten des Vorlagenwegs grün, der alte Weg unverändert grün.
- **V:** Validator Office 2007–2021 ohne Fehler, zwei Läufe byte-gleich, `BerichtsvorlageDateiWacheTests` 20 Fälle.
- **H:** 50 neue Testmethoden grün (Abschnitt 4.3).
- **I:** Tests Kern 897 und UI 866 Fälle grün.

Die Etappe berührt keinen Rechenweg; ein Referenzlauf gehört nicht zu ihrer Abnahme (Konzept 13). Berührt sind Kern,
Oberfläche und Hülle (`EPOS.UI.Daten`), dazu die ausgelieferte Standardvorlage; `EPOS.iOS/` ist nicht berührt.

**Gate auf 58d4a9ff** (Zweig `konzept-berichtvorlagen` nach dem Merge mit `origin/ios_migration_september` 81b83ae2): Kern-Filter 0 Fehler; Windows-Schale (Debug x64) 0 Fehler; Ressourcen-Designer wiederholbar; voller Lauf 14.915 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern.Tests 7.527, EPOS.UI.Tests 6.426, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27); ChartProben 174 Bilder, 0 Verstöße; SQL-Prüfer 1.939 Texte, 0 Fundstellen; Dokumentationswachen grün. Ein erster Gate-Lauf auf 09787fc9 war rot, weil beim Auflösen des Ressourcenkonflikts mit origin das `</data>` hinter `IMP_GEB_PROT_NACHTZEIT_UNGUELTIG` verloren gegangen war (beide `.resx` kein gültiges XML, Designer ohne die neuen Schlüssel; Folge: Textbündel-, Zapfprofil- und Schemawachen rot, ChartProben-Bau MSB3103) — behoben mit 9cf444ca, danach Merge mit dem neuesten origin und alle zuvor roten Tests grün. Kein Referenzlauf nötig (kein Rechenweg berührt); Änderungen an `EPOS.iOS/` keine; Setup-Lauf nach Rückfrage; der Kern-Lauf der CI auf dem Push ist der Nachweis.

## 7 Anwenderprobe (Windows)

Mit dem Anwender-Build des Endstands (`EPOS_Plan.exe`), installiertem Word und einem Stammprojekt mit mindestens einer
Variante; ausgeliefert muss die neue Standardvorlage sein (Build oder Setup-Lauf).

| Nr. | Schritt | Erwartet |
|---|---|---|
| 1 | „Berichte & Kosten › Bericht“ mit „Standard (EPOS-Plan)“; Ausgabe Word | Prüfzeile „geprüft, 36 Platzhalter, keine Befunde“ (35 Text-, 1 Bildplatzhalter); unter den Berichtsbausteinen ist „Deckblatt“ ausgegraut mit dem Grund „Deckblatt kommt aus der Vorlage“, ein Klick darauf nennt den Grund und dass das Häkchen gespeichert bleibt; die übrigen Häkchen sind frei, „Alle“ lässt „Deckblatt“ unverändert |
| 2 | „Erstellen“ | Deckblatt aus der Vorlage auf eigener Seite ohne Kopf- und Fußzeile (Titel, Untertitel, Tabelle, „Erstellt mit EPOS-Plan …“), Inhaltsverzeichnis, die Kapitel unter ihren Kapitelköpfen; Kopfzeile ohne Logo (keins eingestellt), Fußzeile mit Firma, Datum und Seite |
| 3 | „Administration › Einstellungen“, Abschnitt „Bericht“: „Logo“ über „Durchsuchen“ auf ein PNG oder JPEG setzen, OK; Bericht erneut erstellen | das Logo steht in der Kopfzeile ab Seite 2, im Rahmen des Platzhalters mit seinem Seitenverhältnis; „Entfernen“ bzw. „Standardwerte“ leeren das Feld, der nächste Bericht hat keins |
| 4 | im Feld „Logo“ einen Pfad auf eine gelöschte Datei, OK; Bericht erstellen | unter dem Feld „Datei nicht gefunden.“; die Laufmeldung warnt „Logo nicht gefunden: …“, der Bericht entsteht ohne Logo |
| 5 | „Neue Vorlage…“, in Word die Kapitelfolge umstellen (Kapitelkopf und Kapitelplatzhalter zusammen verschieben), ein Kapitel samt Kopf löschen, speichern, „Prüfen“; ein weiteres Häkchen abwählen, „Erstellen“ | das Häkchen des gelöschten Kapitels ist ausgegraut „in dieser Vorlage nicht enthalten“; der Bericht folgt der Kapitelfolge der Vorlage, das abgewählte Kapitel fehlt samt seinem Kapitelkopf — keine verwaiste Überschrift |
| 6 | in derselben Vorlage einen Kapitelplatzhalter ein zweites Mal einfügen; „Prüfen“, „Erstellen“ | die Prüfliste nennt den Hinweis „Kapitel … steht mehrfach in der Vorlage“; im Bericht bleibt die zweite Stelle gelb markiert stehen |
| 7 | eine Vorlage nur mit Einzelplatzhaltern (etwa `{{projekt.klimaregion}}`) wählen; Ausgabe Word, dann „Beide“ | bei Word statt der Liste die Zeile „Den Inhalt bestimmt die Vorlage – sie führt einzelne Platzhalter, aber kein Kapitel.“; bei „Beide“ die Liste mit ausgegrautem Deckblatt, Inhaltsverzeichnis und Anhang |
| 8 | Wirtschaftlichkeitsseite, „Anhang-E-Checkliste…“, einmal mit der Vorlage aus Schritt 5, einmal mit der Standardvorlage | die Spalte „Stelle“ nennt die Kapitelköpfe der gewählten Vorlage, das gelöschte Kapitel als „nicht im Bericht“; darunter „Die Stellen im Bericht nennen die Kapitel der Vorlage „…““ bzw. „… bezogen auf die Standardvorlage.“ |

## 8 Offen

- **(a) Anwenderprobe unter Windows** in acht Schritten (Abschnitt 7): Standardvorlage mit ausgegrautem Häkchen
  „Deckblatt“ und Deckblatt aus der Vorlage, Logo setzen und im Bericht sehen, fehlende Logodatei, eigene Vorlage mit
  umgestellter Kapitelfolge und abgewähltem Häkchen ohne verwaiste Überschrift, doppeltes Kapitel, Vorlage nur mit
  Einzelplatzhaltern, Stelle in der Anhang-E-Überlagerung.
- **(b) Regel „Deckblatt aus Platzhaltern“ schärfen:** Prüfer und Engine werten schon eine einzelne Deckblattangabe im Rumpf
  (etwa nur `{{bericht.datum}}`) als Deckblatt der Vorlage — dann ist das Häkchen „Deckblatt“ ausgegraut, und die Checkliste
  nennt die Stelle „Deckblatt“. Fachlich zu schärfen über `Vorlagenfeldkatalog.Deckblattangaben`.
- **(c) Unlesbare eigene Vorlage:** Die Anhang-E-Überlagerung nennt dann die Stellen der Standardvorlage, die Bezugszeile
  aber die eigene Vorlage.
- **(d) Logo-Prüfung:** `BerichtsvorlagenCtrl.LogoVorhanden()` ist ungenutzt; die Hülle prüft nur, ob die Datei da ist —
  eine zu große oder falsche Datei meldet erst die Laufmeldung.
- **(e) Beispielvorlage:** Sie unterscheidet sich von der Standardvorlage nur noch durch ihre Kommentare; ob sie neben dem
  Kurzbericht (BV-E5) bleibt, ist offen.
- **(f) Wiki-Upload** der Seite „Berichtsvorlagen“ (Neuanlage, mit BV-E2 um Kapitel, Häkchen und Logo erweitert, neue Anker
  `kapitel`, `haekchen`, `logo`) im Sammel-Upload; der Logbuch-Satz steht in `Wiki_Update_2026-09-26.md`, die Version ist
  beim Anwender zu erfragen.
- **(g) Setup-Lauf** nach Rückfrage (die neue Standardvorlage in `{app}\Vorlagen`); **iOS-Lauf** nach Rückfrage — BV-E2
  berührt `EPOS.iOS/` nicht, ungebaut bleiben die Änderungen aus BV-E1.
- **(h) Aus BV-E1 weiter offen** („Nach #512“): die Anwenderprobe von BV-E1, die Tippprobe mit echtem Word, „Original
  geändert – übernehmen?“, eine Bedienung der Vorgabe `BerichtVorlageWord`, das KI-Feld der Katalogsuche, der Abgleich der
  Kern-Vorprüfung für Vorlagen ohne Platzhalter und die Startrückfrage mit der gespeicherten Versionsauswahl.
- **(i) Grenzen:** Die Ausrichtung eines EXIF-JPEG wertet `Bildinhalt` nicht aus; Schalter `baustein.*` wirken erst mit
  BV-E4, die übrigen Bildschlüssel kommen mit BV-E5.

## 9 Dateien

16 Commits der Agenten (Tafel im Kopf), belegt mit `git log --no-merges --stat` auf dem Zweig ohne die Commits von
`origin/ios_migration_september`:

| Bereich | Dateien |
|---|---|
| Kern, Vorlagen | `EPOS.Kern/Allgemein/Bericht/Vorlagen/`: neu `Berichtskapitel.cs`, `Bildinhalt.cs`; geändert `Berichtswerte.cs`, `Platzhalterwert.cs`, `Vorlagenfeld.cs`, `Vorlagenfeldkatalog.cs`, `Vorlagenpruefer.cs`, `VorlagenprueferTypen.cs`, `WordVorlagenergebnis.cs`, `WordVorlagenfueller.cs`, `WordVorlagenstile.cs`, `WordVorlagentexte.cs` (K1) |
| Kern, Bericht | `EPOS.Kern/Allgemein/Bericht/WordBerichtGenerator.cs`, `BerichtsKonfiguration.cs`, `BerichtTexte.cs`, `AnhangECheckliste.cs`, `Bausteine/BausteineProjekt.cs`, `BausteineStandard.cs`, `BausteineVergleich.cs`, `BausteineWirtschaftlichkeit.cs` (K1) |
| Kern, Controller und KI | `EPOS.Kern/Controller/BerichtCtrl.cs`, `BerichtsvorlagenCtrl.cs`, `EPOS.Kern/Allgemein/KI/KiMeldungskennung.cs`, `HilfeWissenBerichtsvorlagen.cs` (K1); `EPOS.Kern/Allgemein/KI/Dialoge/KiDialoge.cs`, `KiDialogTexte.cs` (H) |
| Ressourcen | `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` — K1 +60, H +15, I +2 und −2 je Sprache; End-Merge `09787fc9` |
| Tests Kern | neu `EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v2.txt`, `Messlatten/Bericht_Word_1030_Vorlage.txt`, `Messlatten/Bericht_Word_Gruppe_Vorlage.txt` (K1), `BerichtsvorlagenHaekchenHuelleTests.cs`, `AnhangEStellenHuelleTests.cs`, `EinstellungenBerichtLogoHuelleTests.cs` (H, angepasst von K1 und I); geändert `WordVorlagenfuellerTests.cs`, `VorlagenprueferTests.cs`, `VorlagenfeldkatalogWacheTests.cs`, `BerichtCtrlVorlagenTests.cs`, `BerichtsvorlagenCtrlTests.cs`, `AnhangEChecklisteTests.cs`, `BerichtBlattstrukturWacheTests.cs`, `BerichtVorlagenMesslatteTests.cs`, `BerichtswerteTests.cs`, `AuslieferungsvorlagenWacheTests.cs`, `KiDialogaufrufTests.cs` (K1), `BerichtsvorlageDateiWacheTests.cs` (V, K1) |
| Oberfläche | `EPOS.UI/Seiten/Berichte/BerichtSeite.razor`, `BerichtDaten.cs`, `BerichtSeiteVorlagentexte.cs` (H, I), `AnhangEChecklisteKnopf.razor`, `AnhangEStellen.cs` (neu), `WirtschaftlichkeitSeite.razor` (H, I); `EPOS.UI/Bausteine/Mehrfachauswahl.razor` (H); `EPOS.UI/Dialoge/Admin/EinstellungenDialog.razor`, `EinstellungenBerichtTexte.cs`, `EinstellungenKiSicht.cs` (H) |
| Tests Oberfläche | neu `EPOS.UI.Tests/Seiten/BerichtSeiteHaekchenTests.cs`, `Seiten/AnhangEStellenTests.cs`, `Bausteine/MehrfachauswahlTests.cs`, `Dialoge/EinstellungenBerichtLogoTests.cs` (H, angepasst von K1 und I); geändert `Dialoge/Hilfe/KiDialogkatalogTests.cs`, `Dialoge/Hilfe/KiMaskenabdeckungWacheTests.cs` (H) |
| Hülle | `EPOS.UI.Daten/Bericht/BerichtSeiteGaben.cs`, `BerichtsvorlagenGaben.cs`, `EinstellungenBerichtGaben.cs`, `BerichteKostenHuelle.cs`, `EPOS.UI.Daten/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` (H, I); `EPOS.UI.Daten/Bericht/AnhangEKapitel.cs` (von H angelegt, von I entfernt) |
| Vorlagen der Auslieferung | `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Standard.docx` (K1), `Berichtsvorlage_Beispiel.docx` (V) |
| Werkzeug | `Werkzeuge/Berichtsvorlage/Beispielvorlage.cs`, `Program.cs`, `Berichtsvorlage.csproj`, `Logoplatzhalter.png` (neu), `LIESMICH.md` (V) |
| Wurzel | `CLAUDE.md` (I, Werkzeugzeile) |
| Papiere und Wiki (dieser Auftrag) | dieses Protokoll, Konzept Rev. 4, `Dokumentation/LIESMICH.md`, `Dokumentation/aktuell/Status_iOS_Migration.md`, `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`, `Dokumentation/aktuell/Konzept_Hilfesystem_Wikidokumentation.md` (Anker), `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki` |
