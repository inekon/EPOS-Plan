# BV-E0 — Grundlagen, Messlatte, Messproben, Beispielvorlage (Protokoll)

Etappe BV-E0 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitt 13). Auftrag #500 vom 25.09.2026: „14 Entscheidfragen BV-Q1–BV-Q19: alle umsetzen, mit
folgenden Änderungen: … Starte BV-E0." Der gültige Stand steht im Konzept (Rev. 2) und in der Statusdatei;
hier steht, wie es geworden ist. Zweig `konzept-berichtvorlagen`, Basis b5a2e389; Fable 5.1 hat
orchestriert, vier Agenten (Opus 5.5) haben in eigenen Worktrees gearbeitet: Papier, Messlatte und
Vorlagentest, Vorlage und Beispielvorlage, Messproben und Ad-hoc-DDL.

---

## 1 Die Entscheide vom 25.09.2026

Alle 19 Fragen nach der Empfehlung des Papiers, mit fünf Änderungen des Anwenders:

| Frage | Entscheid | Folge im Papier (Rev. 2) |
|---|---|---|
| BV-Q8 | Ersteller mit Firma **und** Programmname und Versionsnummer | `ersteller.firma`, `ersteller.programm`, `ersteller.version`; `bericht.programmversion` bleibt Alias (4.5, 5.2, Anhang A, B) |
| BV-Q11 | Grafiken in Excel sind Excel-eigene Diagramme aus den Zahlen der Tabellen | Anwenderdiagramme auf `EPOS_<name>` bzw. `EPOS.reihe.*`; erzeugte Blätter bekommen Diagramme über das OpenXML SDK (5.4, 7.4, BV-E7/E8); Messprobe 6 |
| BV-Q12 | keine Probefüllung | „Vorlage testen" gestrichen (6.8, 9.7, BV-E1, BV-E5, Anhang C Nr. 68) |
| BV-Q15 | Vorlagenverzeichnis extern, wählbar mit Vorgabe | Einstellung `BerichtVorlagenordner`, Vorgabe `Dokumente/EPOS-Plan/Berichtsvorlagen`; unter Windows frei wählbar, iOS fest die Sandbox (10.3) |
| BV-Q19 | Vorlagen nicht im Kern, sie liegen extern vor; Beispielvorlage aus dem bisherigen Bericht | Auslieferung wie heute (`{app}\Vorlagen`, MauiAsset), Ort in BV-E1 über `IPfade.Berichtsvorlagen`; `Berichtsvorlage_Beispiel.docx` aus `Werkzeuge/Berichtsvorlage` (6.3, 8.4, 10.3, Anhang B.3) |

Das Papier trägt den Entscheid als fünfte Spalte der Tabelle in Abschnitt 14 (Commit 153ceb98). Das
Mockup gilt mit BV-Q9 (c) als angenommen.

## 2 Teil 1 — Wirkungstafel am Anker, Vorlagentest, Messlatte, Laufzeit

**BW:678.** `BausteineWirtschaftlichkeit.cs` schrieb die Wirkungstafel mit `k.Body.Append(t)` statt
`k.Fuege(t)`; mit Vorlage landete sie hinter der Abschnittsangabe (`w:sectPr`). Es war die einzige Stelle
in Bausteinen, `AnhangECheckliste` und `BerichtTexte`, die am Einfügeanker vorbeischreibt. Der neue Test
war vor der Behebung rot (beide Proben, je Office-Fassung sechs Meldungen, ein Element hinter der
Abschnittsangabe) und ist danach grün.

**Zweiter Fehler, nicht im Konzept.** `WordBerichtGenerator.SetzeUpdateFields` stellte `w:updateFields`
mit `PrependChild` an den Anfang der Einstellungen; mit Vorlage stand deren `w:displayBackgroundShape`
dahinter — in jeder Office-Fassung ungültig. Ohne Vorlage (Ersatzstile der Tests) fiel es nie auf.
Behoben mit `Settings.AddChild`, das an die vom Schema vorgegebene Stelle setzt. Das ändert jeden
Anwenderbericht mit Vorlage; das Konzept nennt den Befund in 2.4.

**Validator** (Office 2007, 2010, 2013, 2016, 2019, 2021; Proben 1030 und Gruppe):

| Stand | Meldungen je Fassung |
|---|---|
| vor der Behebung | 6 (1× `w:tbl` hinter `w:sectPr`, 4× doppelte Stil-ID, 1× Reihenfolge der Einstellungen) |
| nach BW:678 | 5 |
| nach der Einstellungs-Behebung | 4 (nur die doppelten Stile der Vorlage) |
| nach der Vorlagenbereinigung (Teil 2) | 0 — die vorläufige Ausschlussliste des Tests ist entfernt |

**Vorlagentest.** `WordBerichtGenerator.Erzeuge(daten, konfig, zielDatei, vorlagePfad)` ist neu und
rückwärtskompatibel (`null` = `FindeVorlage` wie bisher; eine benannte, fehlende Vorlage bricht mit ihrem
Pfad ab). `BerichtVorlagenMesslatteTests` erzeugt den Bericht mit der echten
`WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx` (repo-relativ über den
Repowurzel-Helfer) und hält: Validator 0 Meldungen, kein Element hinter der letzten `SectionProperties`.
Die synthetischen Daten liegen jetzt in `Berichtsdatenproben` (aus `WordBerichtSvgWacheTests`
verschoben, ohne Kopie): `Projektdaten1030`, `Gruppendaten(anzahlStaende)`, `VolleKonfiguration`,
dazu Wirtschaftlichkeit ohne Speichern und die Wirkungsprobe (`WirtschaftlichkeitBewertung.Wirkungen`
am Stamm, weil die synthetischen Kennungen 9101 ff. nicht in `Tab_Projekt` stehen).

**Strukturmesslatte.** `Berichtsstruktur` macht aus Dokument und Mappe eine Strukturliste (je
Rumpfkind: Absatz mit Stil-ID und Text, Tabelle mit Kopfzellen und Zeilenzahl, Bildstelle mit Maß und
SVG-Kennzeichen, Seitenumbruch, TOC-Feld, Kopf- und Fußzeile; je Blatt Namen, Ankerzeilen, benannte
Bereiche). Datum im Text wird `<datum>`, Zahlen in nicht fetten Kopfzellen und alle Zahlenzellen der
Mappe werden `#`, Excel-Formeln stehen wörtlich. Eingefroren nach der Behebung, mit echter Vorlage:

| Messlatte | Zeilen |
|---|---|
| `EPOS.Kern.Tests/Messlatten/Bericht_Word_1030.txt` | 95 |
| `EPOS.Kern.Tests/Messlatten/Bericht_Word_Gruppe.txt` | 124 |
| `EPOS.Kern.Tests/Messlatten/Bericht_Excel_1030.txt` | 95 |
| `EPOS.Kern.Tests/Messlatten/Bericht_Excel_Gruppe.txt` | 116 |

Die Probe 1030 rechnet mit festem KWKG-Förderbeginn 01.01.2027; ohne Datum nähme der Kern das Folgejahr
des Laufs, und die Liste kippte zum Jahreswechsel. Zwei Läufe hintereinander sind gleich; die Gegenprobe
mit einer verfälschten Messlatte meldet den abweichenden Block und schreibt die aktuelle Liste in den
Testausgabeordner. **Neu einfrieren** heißt: Datei aus dem Testausgabeordner nach
`EPOS.Kern.Tests/Messlatten/` kopieren (Kopfkommentar der Testklasse). Jede gewollte Änderung an
Bausteintexten, Wirtschaftlichkeitsrechnung, Kennzahlenkatalog oder den gesäten Daten von 1030 macht
die Messlatte rot — das ist ihr Zweck.

**Laufzeit heute** (Release, Windows 11, .NET 10.0.12, nur die Erzeugung, je drei Läufe):

| Probe | Word | Excel |
|---|---|---|
| 1030 | 888 / 814 / 768 ms | 352 / 289 / 281 ms |
| Gruppe mit sieben Ständen | 3.602 / 2.309 / 2.910 ms | 374 / 337 / 384 ms |

Die Sicherheitsgrenze des Tests liegt bei 120 s je Erzeugung; das Ziel „heute plus höchstens 10 %"
(Konzept 8.5) misst sich an diesen Zahlen. Die Linux-Zahl liefert die CI mit dem ersten Lauf, der den
Test enthält (Protokollausgabe des Tests).

## 3 Teil 2 — Vorlage bereinigt, Beispielvorlage aus dem bisherigen Bericht

**Werkzeug `Werkzeuge/Berichtsvorlage`** (Konsole, net10.0, OpenXML SDK, eigene Projektmappe, LIESMICH;
Zeile in der Werkzeugtabelle der Wurzel-`CLAUDE.md`), zwei Modi: `bereinigen <docx>` und
`beispiel <quelle.docx> <ziel.docx>` (Schalter `--sammelanker` für die Stufe BV-E1: Rumpf nur
`{{bericht.inhalt}}`, nicht im Repository). Ein zweiter Lauf von `bereinigen` meldet „nichts zu tun",
die Datei bleibt byte-gleich.

**Welche Stildefinition bleibt.** Gemessen mit Word 16.0 über COM: die Originaldatei, eine Fassung nur mit
der ersten und eine nur mit der letzten Definition, dazu der Bericht 1030 mit allen Bausteinen.

| Stil | erste Definition | zweite Definition | Word zeigt |
|---|---|---|---|
| Titel | 28 pt, next Normal, qFormat | 28 pt fett 1F4E79, 120/12 pt, kein next | Formatierung der zweiten, nächster Absatz „Standard" aus der ersten |
| Überschrift 1 | 16 pt, 2E74B5 | 15 pt fett 1F4E79, 18/8 pt, Rahmen unten, outlineLvl 0 | die zweite |
| Überschrift 2 | 13 pt, 2E74B5 | 12,5 pt fett 1F4E79, 14/6 pt, outlineLvl 1 | die zweite |
| Überschrift 3 | 12 pt, 1F4D78 | 11 pt fett 595959, 10/5 pt, outlineLvl 2 | die zweite |

Word führt die Definitionen zusammen, die spätere gewinnt; was nur die frühere trägt (`next` beim Titel,
`qFormat` bei allen vier), bleibt wirksam. Regel im Werkzeug: die letzte Definition bleibt, Angaben, die nur
eine frühere trägt, werden übernommen; nur die letzte trägt `w:outlineLvl` (Word ordnet Überschrift 1–3
auch ohne das Attribut zu, andere Leser brauchen es). Keine Definition setzt eine Schrift, Calibri kommt
aus Normal. Der Bericht 1030 mit alter und bereinigter Vorlage: Rümpfe gleich, nur `styles.xml`
unterscheidet sich; in Word jeder Absatz zeilengleich, Inhaltsverzeichnis mit 19 Einträgen, 9 Seiten.
Neu angelegt: Absatzformat `EPOSKapitelkopf` („EPOS Kapitelkopf", basedOn Heading1, outlineLvl 0, next
Normal) für die Kapitelköpfe der Beispielvorlage; der heutige Generator benutzt es nicht.

**Validator** (Office 2007 bis 2021): vorher 4 Fehler je Fassung (doppelte `styleId` Title, Heading1–3),
nachher 0 in jeder Fassung, für beide Dateien.

**Beispielvorlage `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Beispiel.docx`**
(Anhang B.3 Rev. 2), 28 Platzhalter — 24 im Rumpf, 1 in der Kopfzeile, 3 in der Fußzeile —, jeder
ungeteilt in einem eigenen Run mit `w:noProof`; „INEKON GmbH" kommt im Paket nicht mehr vor:

| Nr. | Abschnitt | Inhalt |
|---|---|---|
| 1 | Deckblatt (eigener Abschnitt ohne Kopf- und Fußzeile) | Titel `{{bericht.titel}}` (Kommentar 1), Untertitel `{{bericht.untertitel}}`; Tabelle 5×2 im Stil der heutigen Eigenschaftstabelle: `{{text.kunde}}` · `{{projekt.kunde}}`, `{{text.bearbeiter}}` · `{{projekt.bearbeiter}}`, `{{text.ersteller}}` · `{{ersteller.firma}}`, `{{text.varianten}}` · `{{bericht.varianten.liste}}`, `{{text.datum}}` · `{{bericht.datum}}`; Hinweis `{{bericht.gebaeudemodell.ausweis\|leer statt strich}}`; Hinweis `{{text.erstellt_mit}} {{ersteller.programm}} {{ersteller.version}}`; Abschnittswechsel |
| 2 | Inhalt | `{{kapitel.inhalt}}` ohne Überschrift (Kommentar 2) |
| 3 | Kapitel in heutiger Folge | je Kapitel eine Überschrift im Format „EPOS Kapitelkopf" und darunter `{{kapitel.<name>\|ohne titel}}`: Projektbeschreibung, Komponenten & Varianten, Berechnungsergebnisse je Variante, Variantenvergleich, Wirtschaftlichkeit, Anhang, Checkliste für den Bewertungsbericht (DIN EN 17463, Anhang E) — die Folge von `WordBerichtGenerator.AktiveBausteine`, die Titel die heute gedruckten |
| 4 | Kopfzeile | `{{ersteller.programm}} · Energie · Planung · Optimierung · Simulation`, rechts das Logo |
| 5 | Fußzeile | links `{{ersteller.firma}}`, Mitte `{{bericht.datum}}` an der Stelle des DATE-Felds, rechts `{{text.seite}}` mit PAGE / NUMPAGES |

**Wache `BerichtsvorlageDateiWacheTests`** (10 Tests): keine doppelte Stil-ID, die acht Stile und „EPOS
Kapitelkopf" vorhanden, Heading1–3 mit outlineLvl, Validator 0 Fehler je Fassung; die Beispielvorlage
trägt genau die erwartete Platzhaltermenge, jeden in einem Run mit noProof, Kapitelfolge und Anhang-E-Titel
gleich dem Code, kein „INEKON GmbH". Gegen die alte Vorlage laufen drei der zehn rot.

**Befunde aus Teil 2 für BV-E1/E2:** das INEKON-Logo in der Kopfzeile beider Vorlagen (Bildplatzhalter
oder Entfall, BV-Q8); die Kapitelköpfe sind deutscher Festtext (für „sprachneutral" `text.*` in BV-E2);
der Seitenumbruch vor Anhang E steht heute vor der Bausteinüberschrift und muss mit `|ohne titel` vor den
Kapitelkopf; `Normal` trägt kein `w:default="1"` (Word behandelt ihn trotzdem als Standardabsatz — die
Rollenauflösung braucht den Rückfall über den Namen); der eigene Stil „Beschriftung" landet im deutschen
Word auf der eingebauten Beschriftung (Rollenauflösung 6.2).

## 4 Teil 3 — Ad-hoc-DDL und Fremdschlüssel der Berichtskonfiguration

**Messung an der Testdatenbank.** `PRAGMA foreign_key_list(Berichtskonfiguration)` liefert
`Tab_Projekt(ID)` mit `ON DELETE CASCADE ON UPDATE CASCADE`; `sqlite_master` zeigt die Tabelle als
`STRICT` mit dem Index `UQ_BerichtKonfigProj`; fünf Zeilen, keine Waisen. Der Katalogeintrag steht in
`EPOS.Kern/Allgemein/Update/ProjektFremdschluessel.cs` (Zeile 205, Schemaschritt 96).

**Erzwingung.** `SqliteDatenzugriff.OeffneVerbindung()` setzt bei jedem Öffnen `Foreign Keys=True` und
`PRAGMA foreign_keys = ON`; alle Wege (`DataRepository` über `Vorgangsklammer.Leihe`, `DbVorgang`,
`StilleDb`, `RecordSet`) laufen dort durch. Ausgeschaltet wird nur in `VorgangOhneFremdschluessel`
(Schemaschritt 96, `FremdschluesselVorgabe`), beim Dispose wieder eingeschaltet (bestehender Test). Die
zwei übrigen Verbindungen (`Erstbereitstellung.Pruefe`, `Datenbanksicherung` mit `VACUUM INTO`) löschen
nichts. Der neue Test misst `PRAGMA foreign_keys = 1` über `DataRepository` und `StilleDb`.

**Entfernt:** `BerichtCtrl.StelleKonfigTabelleSicher` samt beiden Aufrufen (`Lade`, `Speichere`); die
Begründung steht an `TAB_KONFIG`. Die entfernte DDL hätte die Tabelle ohne Fremdschlüssel und ohne
`STRICT` angelegt — an so einer Tabelle bricht Schritt 96 ab, und die Simulation bliebe gesperrt.
Kommentare richtiggestellt in `ProjektCtrl` (`BerichtsKonfigurationEntfernen`, `Delete`),
`ProjektDuplizierenCtrl` (Ausnahmeliste) und `WirtschaftlichkeitCtrl` (kein Verweis auf das Muster mehr).

**Belassen: das Löschen der Konfigzeile per Hand** in `ProjektCtrl` vor dem Projekt-DELETE. Belegt sind
Fremdschlüssel und Erzwingung nur für Datenbanken ab Stand 96: Scheitert die Migration, startet die
Windows-Schale trotzdem und Projekte bleiben löschbar (`WindowsFormsApplication1/Program.cs:296-316`);
die iOS-Hülle migriert nicht (Seed-Kopie). Auf so einer Datenbank bliebe die Konfigzeile als Waise, und
eine Projektkopie mit derselben Kennung erbte die Berichtseinstellungen des gelöschten Projekts
(`FreieProjektId` fragt die ausgenommene Tabelle nicht ab). Der Kommentar nennt diesen Grund; der neue
Test `BerichtCtrlKonfigurationTests` zeigt beides: Der Löschweg der Anwendung hinterlässt keine Zeile,
und ein nacktes `DELETE FROM Tab_Projekt` nimmt die Konfiguration über die Kaskade mit — auf Zielstand
genügt die Kaskade, das DELETE per Hand lässt sich streichen, sobald der Fall unter Stand 96
ausgeschlossen ist.

**Nebenbefund, eigener Auftrag:** `WirtschaftlichkeitCtrl.StelleTabellenSicher` legt fünf Tabellen nach
demselben Muster ohne Fremdschlüssel und ohne `STRICT` an, obwohl alle fünf in der Testdatenbank `STRICT`
sind und den Fremdschlüssel mit CASCADE tragen. Hier nur der Kommentar angepasst.

## 5 Messproben (Tests `BerichtsvorlagenMessprobenTests`, ClosedXML 0.105.1, OpenXML SDK 3.5.1)

Jede Probe pinnt das gemessene Verhalten, damit ein Paketwechsel auffällt; Dateien nur im
Temp-Ordner; Validator Office 2016 in allen Proben 0 Fehler.

| Nr. | Frage des Konzepts | Ergebnis |
|---|---|---|
| 1 | Excel-Namen mit Punkt (4.4) | `EPOS.stamm.kennzahl.eff.jaz` und `EPOS.CO2` überstehen Speichern und Laden, mit ClosedXML wie mit dem SDK; ClosedXML rechnet `=EPOS.CO2*2`. Der Rückfall `EPOS_<schlüssel>` mit `__` wird nicht gebraucht. **Nebenbefund:** auch den nackten Zellbezug `CO2` nimmt ClosedXML als Namen an, der Validator schweigt — den Präfix muss EPOS selbst durchsetzen |
| 2 | ClosedXML-Rundlauf einer Vorlage (7.3, 7.4) | Diagramm, Excel-Tabelle und Name bleiben. `InsertRowsBelow` kostet die berechnete Spalte (neue Zeilen ohne Formel), eine bloße Zelländerung lässt sie stehen. **Das `<v>` jeder Formel geht verloren, auch ohne Änderung** — die Frage aus 7.4 ist mit „nein" beantwortet; `fullCalcOnLoad` setzt ClosedXML nur auf Anforderung |
| 3 | `.xltx` und `.dotx` (6.1, 7.1) | Nach `ChangeDocumentType` stimmt der Inhaltstyp, ClosedXML liest die Mappe, das Dokument behält seinen Text. **Nebenbefund:** aus einem Strom geladen speichert ClosedXML eine `.xltx` auch unter `.xlsx` als Vorlage weiter — der Weg `new XLWorkbook(stream)` braucht die Umstellung über das SDK zwingend |
| 4 | SDT und Alternativtext (4.2) | Text-SDT im Satz und Block-SDT über `w:tag`, das Bild über `docPr/@descr` sicher auffindbar; der auf drei Runs verteilte Platzhalter steht in keinem einzelnen `w:t`, nur im zusammengesetzten Absatztext |
| 5 | Vorlage aus deutschem Word (6.2), synthetisch | `WordKontext.MitStil("Heading1")` setzt einen Stil, den es nicht gibt (ebenso Title, Heading2, Normal), der Validator schweigt; über `w:name` ohne Groß-/Kleinschreibung und `w:default` lösen sich alle Rollen auf. Nachweis mit einem echten Word-365-Dokument steht aus |
| 6 | Excel-Diagramm über das SDK in einer ClosedXML-Mappe (BV-Q11) | Das per SDK angelegte Balkendiagramm übersteht einen ClosedXML-Lauf mit geänderter Zahl samt Teilen, Anker und unveränderten Reihenformeln; der Zwischenspeicher der Reihe bleibt alt (20 statt 25) — der Nachtrag gehört zu BV-E8 |
| 7 | Tippprobe (4.2) | ohne echtes Word nicht messbar; **offen** — der Anwender tippt in Word „Kunde: {{projekt.kunde}}" mit Autokorrektur und liefert die Datei |

## 6 Abnahme

**Gate auf dem Merge 07c2f0f5** (Zweig `konzept-berichtvorlagen` mit `origin/ios_migration_september` 99815b47): Kern-Filter 0 Fehler; voller Lauf 14.054 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern.Tests 6.849, EPOS.UI.Tests 6.243, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27); ChartProben 174 Bilder, 0 Verstöße; SQL-Prüfer 1.919 Texte, 0 Fundstellen; Dokumentationswachen grün. Vorher in den Agenten-Worktrees: Teil 1 voller Kern-Filter grün (6.733 / 6.194 / 549 / 386 / 27), Teil 2 voller Kern-Filter grün (6.738 / 6.194 / 549 / 386 / 27), Teil 3 Wächter 168/168 und SQL-Prüfer 1.909/0. Nach dem End-Merge mit origin b5cc1a61 (G4-8): Kern-Filter 0 Fehler, Berichtstests und Dokumentationswachen grün; kein Referenzlauf nötig (kein Rechenweg berührt), die Windows-Schale ist nicht angefasst; der Kern-Lauf der CI auf dem Push ist der Nachweis.

## 7 Offen und Befunde für die nächsten Etappen

- **Tippprobe** und Nachweis mit echtem Word 365 bzw. Excel (Proben 1, 5, 6 in Excel oder Word öffnen)
  stehen aus; Geräteprobe iPad (BV-Q16) notiert, iU13.
- **Doppelte Bild-Kennungen:** Kopfzeile und erstes Bild im Rumpf tragen beide `docPr/@id` = 1; der
  Validator meldet es nicht — BV-E1 (Konzept 6.5: `docPr/@id` neu vergeben).
- **Probe 1030 mit dünner Wirtschaftlichkeit:** `Projektdaten1030` geht den Kostenschritt des Sammlers
  nicht; das Kapitel zeigt nur den Stamm mit „Energiekosten nicht bestimmbar", Verlauf, Brücke,
  Mehrjahresübersicht und Szenarien deckt die Gruppe ab. Soll 1030 voller werden, Kostenschritt ergänzen
  und neu einfrieren.
- **Doppelte Helfer:** `BerichtBlattstrukturWacheTests` führt weiter eigene `Gruppendaten` und
  `VolleKonfiguration`.
- **`WirtschaftlichkeitCtrl.StelleTabellenSicher`** — eigener Auftrag (Abschnitt 4).
- **Standardvorlage in BV-E1 mit anderem Namen als `Berichtsvorlage.docx`**, weil Inno Setup die Altdatei
  nicht löscht (`[InstallDelete]`) und die Übernahme beim ersten Start sie vergleicht (Konzept 10.3).

## 8 Dateien

| Bereich | Dateien |
|---|---|
| Kern | `EPOS.Kern/Allgemein/Bericht/WordBerichtGenerator.cs` (Überladung mit Vorlagenpfad, `SetzeUpdateFields`), `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` (Wirkungstafel am Anker), `EPOS.Kern/Controller/BerichtCtrl.cs` (Ad-hoc-DDL entfernt), `EPOS.Kern/Controller/ProjektCtrl.cs`, `EPOS.Kern/Controller/ProjektDuplizierenCtrl.cs`, `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` (Kommentare) |
| Tests | `EPOS.Kern.Tests/BerichtVorlagenMesslatteTests.cs`, `Berichtsdatenproben.cs`, `Berichtsstruktur.cs`, `Messlatten/Bericht_Word_1030.txt`, `Bericht_Word_Gruppe.txt`, `Bericht_Excel_1030.txt`, `Bericht_Excel_Gruppe.txt`, `BerichtsvorlageDateiWacheTests.cs`, `BerichtsvorlagenMessprobenTests.cs`, `BerichtCtrlKonfigurationTests.cs`, `WordBerichtSvgWacheTests.cs` (Helfer ausgelagert) |
| Vorlagen | `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx` (bereinigt), `Berichtsvorlage_Beispiel.docx` (neu, noch nicht ausgeliefert) |
| Werkzeug | `Werkzeuge/Berichtsvorlage/` mit `LIESMICH.md`; Zeile in der Wurzel-`CLAUDE.md` |
| Papiere | Konzept Rev. 2 (Entscheide, 2.4, 11, 13), `Dokumentation/LIESMICH.md`, `Dokumentation/aktuell/Status_iOS_Migration.md`, dieses Protokoll |
