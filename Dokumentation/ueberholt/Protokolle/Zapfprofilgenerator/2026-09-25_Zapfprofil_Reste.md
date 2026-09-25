# Sammelposten „Zapfprofil-Reste" — Zapfprofilgenerator (Protokoll, 25.09.2026)

Statuszeile #508 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Nachtrag
**N18** im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); die vier
Posten stammen aus den Nachträgen N13 (Folgen (b) und (s)), N15 (Gruppe 3, Folge) und N16
(Restlücke), Vorstufen in den Protokollen [Z4](2026-09-24_Z4_Oberflaeche.md),
[Z5](2026-09-25_Z5_Kalibrierung.md) und
[Folgeposten ZU20/ZU24](2026-09-25_Folgeposten_ZU20_ZU24.md). Zweig `zr` von `dbcaf63a`
(= `origin/ios_migration_september`, Schemastand 142, Referenzbasis
`2026-09-25_R16_Anlagenprio`), vier Code-Commits `0036c657`, `0a58ea52`, `6080f65a`, `43d32522`, ein
Aufräumcommit `ca257fb8`, Merge `da24a5d9` von `origin` (`b0eb1783`), Papiere `6c730a0e`. Nach der
**Gegenprüfung** drei weitere Commits `b73f46fc`, `4edd1f27`, `9829667b`, die Papiere `29bf185c` und
der zweite Merge `1b75c5af` von `origin` (`ae15e979`). **Kein Schemaschritt, die Testdatenbank ist unberührt.** Alle Gates im
Worktree, kein CI-Lauf bis zum Push.

## Auftrag

Vier offene Folgeposten in einem Zug, je ein Commit: (1) der fehlende Prüfposten der
Auslieferungsvorlage gegen ein Beispielprojekt auf dem Typtagweg bei leerer
`Tab_TwwTyptag_IMPORT`; (2) die Vermerke des Herkunftsprotokolls als `ZapfSatz` statt als deutscher
Klartext; (3) Größenschutz und Pfadprüfung des Katalogimports, dazu Bedarfstage und Parameter,
soweit N13 (s) sie beschreibt; (4) die Spitzenstreuung im Vergleichsbericht als Zahl statt als
Strich. Kein Schemaschritt, kein Push, Hauptbaum unberührt.

## Was entstanden ist

| Posten | Inhalt | Commit |
|---|---|---|
| Prüfposten Typtage | siebter Tww-Prüfposten in `Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs`: kein Projekt mit `Tab_TwwProjekt.Typtage_Aktiv = 1`, solange `Tab_TwwTyptag_IMPORT` leer ist; der Bericht nennt Projektkennung, Klimazone und Gebäudeart. Test T14 positiv und negativ | `0036c657` |
| Herkunftsprotokoll als Sätze | `Herkunftseintrag.Vermerk` ist ein `ZapfSatz` (`null` = nichts zu vermerken); 41 Muster `ZPG_SATZ_HERKUNFT_…` in beiden Sprachen, Designer nachgezogen; je Ausprägung einer Aufzählung eine eigene Kennung; Tests prüfen Kennung und Werte | `0a58ea52` |
| Katalogimport-Schutz | drei Konstanten (200 Einträge, 64 MB entpackt, 16 MB je Datei) und drei benannte Ablehnungen in `TwwNutzungsartCtrl.PaketLesen`; Prüfung aus dem Zentralverzeichnis vor dem Entpacken; Pfadprüfung gegen `..`, Wurzel und Laufwerk; Verzeichniseinträge fallen still. Drei Fälle in `TwwKatalogimportTests` | `6080f65a` |
| Spitzenstreuung | `Jahresensemble.StundenspitzenKw` → `ZonenErgebnis.StundenspitzenKw` → `Messvergleich.SynthetischeStundenspitzenKw` in der Hülle; mehrere Ensembles benannt abgelehnt (`MESSVERGLEICH_ENSEMBLE_ZONEN`); bunit-Fall und Wiki-Satz | `43d32522` |
| Aufräumen | `Mengengeruest.Z` (Zahlenformatierer der alten Klartextvermerke) entfernt — seit (b) ruft ihn niemand mehr | `ca257fb8` |
| Papiere | N18, dieses Protokoll, Statuszeile #508, Indexzeilen in `Dokumentation/LIESMICH.md` | `6c730a0e` |
| Gegenprüfung: Größenschutz beim Lesen | beide Paketleser lesen je Eintrag nur bis zur Grenze und ein Byte darüber, mit mitlaufender Summe; Eintragszahl und Gesamtgröße auch für Ordner und Einzeldatei; Summe bricht an der Grenze ab; `Pfadsicher` lehnt `:` nur als Laufwerksmuster ab. Prüfstand `EPOS.Kern.Tests/Archivluege.cs` | `b73f46fc` |
| Gegenprüfung: Aufräumen | ungenutztes `using System.Globalization` in `ZapfprofilRechner.cs` | `4edd1f27` |
| Gegenprüfung: eigener Strichgrund | `ZapfprofilMessvergleichDaten.EnsembleZonen` und der Vermerk `ZPG_VERGL_ENSEMBLE_ZONEN` in beiden Sprachen; Hüllen-Weiche und bunit-Fall | `9829667b` |

## Abweichungen vom Auftrag

**(1) „Hülle und Dialog durchreichen" (Posten 2) entfällt — das Protokoll reist heute nirgends hin.**
`ZapfprofilErgebnis.Herkunft` und `Auslegungsergebnis.Herkunft` werden von keiner Hülle und keiner
Razor-Seite gelesen; das Protokoll ist Rechennachweis für Kern und Tests. Der Umbau macht es
anzeigefähig — die Hülle baut aus einem `ZapfSatz` den Satz in der Oberflächensprache, wie bei jedem
Hinweis —, führt die Anzeige aber nicht ein: Dafür fehlt der Ort in Kapitel 5 und ein Beschluss,
welche Stufe sie sieht. Als Folge (b) in N18 benannt.

**(2) Die Pfadprüfung des Katalogimports lehnt einen Unterordner NICHT ab.** Der Auftrag nannte ein
flaches Paket. Ein ZIP, das aus einem Ordner entstanden ist, trägt dessen Namen
(`paket/Tab_….csv`); der Bestand liest solche Pakete, und `TwwKatalogimportTests` prüft das
ausdrücklich („Zip, Ordner und eine Datei des Ordners lesen dasselbe Paket"). Eine flache Prüfung
hätte eine erlaubte Bedienung gebrochen. Abgelehnt wird deshalb, was **aus dem Archiv herauszeigt**
(`..`, Wurzel, Laufwerk) — und da nichts auf die Platte entpackt wird, ist das die ganze Gefahr.

**(3) Bedarfstage und Parameter bleiben außerhalb des Katalogimports.** N13 (s) und die Folge (s)
nennen die Erweiterung, beschreiben sie aber nicht: Dublettenregel, Versionsbildung und
Berichtszeilen für `Tab_TwwBedarfstag_STAMM`, `Tab_TwwBedarfstagEreignis_STAMM` und
`Tab_TwwParameter_STAMM` fehlen, und die Auslieferungsvorlage spielt diese Tabellen über einen
eigenen Weg ein (`--katalogpaket`). Nach der Regel „nur, wenn der Nachtrag es klar beschreibt"
bleibt der Posten offen — **nicht still:** Eine solche Datei im Paket steht mit Namen als
`KATALOGIMPORT_DATEI_UEBERGANGEN` im Bericht. Folge (s) in N18.

**(4) Die Spitzenstreuung steht nur bei EINER Zone mit Ensemble.** Bei mehreren Zonen zieht jede
Zone ihre Realisierungen für sich; die Spitze der Summe ist nicht die Summe der Spitzen. Eine
Stichprobe über die Summe bräuchte die Realisierungen aller Zonen gleichzeitig — eine Änderung am
Ensemble, nicht am Bericht. Die Hülle benennt den Fall (`MESSVERGLEICH_ENSEMBLE_ZONEN`) und schätzt
nicht; die Zeile trägt seit der Gegenprüfung ihren eigenen Strichvermerk
(`ZPG_VERGL_ENSEMBLE_ZONEN`) statt „ohne Ensemble".

## Gegenprüfung und Nachbesserung

Eine zweite Sitzung hat den Stand gegen die Regeln gehalten: **sechs Befunde**, alle behoben.
Einzelheiten und Begründung stehen in N18, Absatz „Gegenprüfung und Nachbesserung"; hier der Kern.

| Befund | Gewicht | Ergebnis |
|---|---|---|
| Zweite Wand beim ZIP-Lesen: beide Leser prüften die entpackte Größe nur am Zentralverzeichnis und lasen den Eintrag danach mit `ReadToEnd()` | mittel | je Eintrag bis zur Grenze und ein Byte darüber, mitlaufende Summe, Grenzen als Parameter mit den Konstanten als Vorgabe. **Gemessen:** `ZipArchiveEntry.Open` begrenzt den Entpackstrom selbst auf die ausgewiesene Größe — ein lügendes Verzeichnis kürzt, es bläht nicht auf; die zweite Wand ist Vorsorge, der gekürzte Eintrag fällt der Formprüfung zu |
| Mehrere Ensemble-Zonen zeigten „ohne Ensemble nicht entscheidbar" — den falschen Grund | mittel | `EnsembleZonen` im DTO, eigener Vermerk `ZPG_VERGL_ENSEMBLE_ZONEN` („mehrere stochastische Zonen — Stichprobe nicht bildbar") in beiden Sprachen |
| Eintragszahl und Gesamtgröße galten nur im Archiv | gering | `MengeZuGross` prüft beide auch für Ordner und Einzeldatei, aus dem Dateisystem |
| `Pfadsicher` lehnte jedes `:` ab | gering | abgelehnt wird allein `^[A-Za-z]:` am Anfang eines Pfadteils |
| Die Summe der ausgewiesenen Größen konnte überlaufen | gering | sie bricht **an** der Grenze ab, im Archiv wie im Dateisystem |
| ungenutztes `using System.Globalization` | gering | entfernt |

**Neue Kennung:** `ZPG_VERGL_ENSEMBLE_ZONEN` (beide Sprachen, Designer nachgezogen) — sonst keine;
die Ablehnungen des Größenschutzes behalten `KATALOGIMPORT_DATEI_ZU_GROSS`,
`KATALOGIMPORT_ZU_GROSS` und `NORMVEKTOR_PAKET_ZU_GROSS`.

**Neue Prüfstände und Fälle:** `EPOS.Kern.Tests/Archivluege.cs` (schreibt die ausgewiesene Größe im
Zentralverzeichnis um, Nutzlast unberührt) mit je einem Fall in `TwwKatalogimportTests` und
`NormformvektorleserTests`; der Ordner- und Einzeldateiweg des Größenschutzes; die Hüllen-Weiche
„genau eine / zwei stochastische Zonen" in `ZapfprofilHuelleMessreihenTests` auf der Testdatenbank;
der bunit-Fall der Zeile für **beide** Strichgründe; und `KATALOGIMPORT_ZU_GROSS` wird nun samt
gemessener und erlaubter Gesamtgröße geprüft (`Werte[2]`, `Werte[3]`).

Der Satz der Wiki-Quelle „Brauchwasser-Zapfprofil" traf schon zu („es steht ein Strich mit diesem
Grund") und bleibt, wie er ist — jetzt trägt ihn auch die Maske.

## Entscheidungen im Posten

- **Aufzählungen im Vermerk bekommen je Ausprägung eine Kennung.** Ein Vermerk „Methode 3" wäre
  keine Aussage, und eine Zahl lässt sich nicht übersetzen. Bedarfsniveau (3), Übertragerwerkstoff
  (2), Zirkulationsmethode (3), Quelle der Speichertemperatur (3) und Erzeugerart der Schätzformel
  (2) tragen deshalb eigene Kennungen; die Auswahl steht als `switch` in einer eigenen kleinen
  Methode, damit die Wache jede Kennung als Literal findet.
- **`null` statt leerer Text.** `Vermerken` legte früher `""` ab, wo es nichts zu vermerken gab; das
  war ein Satz ohne Inhalt. Jetzt steht `null`, und der Unterschied „kein Vermerk" gegen „Vermerk
  mit Muster" ist im Typ sichtbar. Ein Test hält das fest.
- **Die Grenzen des Katalogimports folgen dem `Normformvektorleser`**, nicht eigenen Zahlen: dieselbe
  Eintragszahl (200) und dieselbe entpackte Gesamtgröße (64 MB), dazu 16 MB je Datei, weil dieser
  Leser jede Datei ganz im Speicher hält. Die Prüfung liest das Zentralverzeichnis — kein Byte eines
  Eintrags.
- **Die Stundenspitzen reisen je Zone, nicht je Projekt.** Am Ensemble hängen sie, und die
  Entscheidung, ob daraus eine Stichprobe wird, gehört in die Hülle — sie kennt die Zonen des
  Vergleichs. Der Kern bleibt bei „was ich habe", die Hülle bei „was daraus zu bilden ist".

## Gates im Worktree

Zweimal gefahren: nach dem ersten Merge (`da24a5d9`) und — nach der Gegenprüfung — auf dem zweiten
Merge `ae15e979`. Die Zahlen unten sind die des zweiten Laufs; der Konflikt des Merges lag allein in
der Statusdatei (die origin-Fassung von #510, danach #508 und #511 in Nummernfolge), der Designer
wurde nach dem resx-Merge neu erzeugt und war ohne Diff.

| Probe | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| voller Testlauf des Kern-Filters (`xUnit.ParallelizeTestCollections=false`, `MaxParallelThreads=2`) | 14 487 grün / 0 rot / 2 übersprungen (Kern 7 219, UI 6 306, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1) |
| `Werkzeuge/Auslieferungsvorlage.Tests` (eigenes Projekt, mit T14) | 36/36 grün |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler |
| `Werkzeuge/SqlDialektPruefer` gegen `Referenzlaeufe/Kenndaten_Test.sqlite` | 1 918 Texte, 0 Fundstellen |
| Referenzlauf der CI-Projekte 1030, 1007, 1017, 1045, 1046, 1047 gegen `2026-09-25_R16_Anlagenprio` | PASS, 2 208 587 Werte |
| Dokumentationswachen (Link-, Wiki-Produktdaten-, Repository-Ordnungswache) | 29/29 grün |
| `ResourceDesigner` (`designer_neu.py` ohne Argument) | ohne Diff, wiederholbar |
| Schemastand der Testdatenbank (`Tab_Applikation.SchemaVersion`) | 142, unverändert |

Die Referenzprojekte nutzen den Generator nicht; der Lauf war wie erwartet unverändert. Die Wächter
der Papiere (Doku-Link-, Wiki-Produktdaten-, Repository-Ordnungswache) und
`KiMaskenabdeckungWacheTests` liefen im vollen Testlauf mit — keine Eingabestelle kam hinzu.

## Folgen

- **Ein künftiger Schemaschritt des Zapfprofils trägt zwei Kleinigkeiten zusammen:** die
  Konstruktorzeilen des Bedarfstags (N13 Folge (p)) und den redundanten Index auf
  `Tab_TwwMessreihe.ID_Projekt` (N15 Folge (a)). Beides ist reines DDL; ein eigener Schritt je
  Kleinigkeit kostet eine Nummer und einen Referenzlauf.
- **Katalogdialog auf iOS** (N13 Folge (r)) bleibt eine eigene Welle **nach iU11**; bis dahin lehnt
  die Hülle ihn dort benannt ab, und das ist der Stand.
- **Referenzfall der Wetterkopplung** mit `Tab_Solar.Bedeckungsgrad` aus einem TRY-Import (N14 Folge
  (c), N15 Folge (m)) steht weiter offen: Die Testdatenbank führt keinen Bedeckungsgrad, die
  Bewölkungsschwelle der Typtagzuordnung ist nur an erfundenen Werten geprüft.
- **Katalogimport um Bedarfstage und Parameter:** erst Dublettenregel, Versionsbildung und
  Berichtszeilen je Tabelle festlegen, dann umsetzen.
- **Herkunftsprotokoll anzeigen:** Ort in Kapitel 5 festlegen (Herleitungszeilen der Stufe Experte
  oder eigene Karte), dann Hülle und Dialog; der Kern ist vorbereitet.
- **Logbuch:** ein Satz vorgeschlagen („Der Vergleich einer Messreihe zeigt die Streuung der
  Realisierungsspitzen, wenn die Jahresreihe stochastisch gerechnet ist."), Version beim Anwender zu
  erfragen. Die übrigen drei Posten sind Kleinigkeiten ohne sichtbare Bedienungsänderung und
  bekommen keinen Eintrag.
- **Sichtabnahme unter Windows:** Reiter Kennzahlen mit stochastischer Jahresreihe und **einer** Zone
  — die Streuung steht als Zahl; mit zwei stochastischen Zonen steht ein Strich mit dem Vermerk
  „mehrere stochastische Zonen — Stichprobe nicht bildbar", dazu der Grund in der Warnliste.
