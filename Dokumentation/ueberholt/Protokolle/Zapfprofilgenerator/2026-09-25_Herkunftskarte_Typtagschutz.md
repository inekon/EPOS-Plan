# Karte „Herkunft" und Größenschutz des Typtag-Paketlesers — Zapfprofilgenerator (Protokoll, 25.09.2026)

Statuszeile #516 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Nachtrag
**N19** im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md) samt den
Zeilen ZU25–ZU29 in Kapitel 9; die beiden Posten stammen aus den Folgen (b) und (c) des
Sammelpostens [N18](2026-09-25_Zapfprofil_Reste.md), Vorstufen in den Protokollen
[Z4](2026-09-24_Z4_Oberflaeche.md) und [Z4b](2026-09-24_Z4b_Typtage.md). Zweig `zh` von `d2200ebb`
(= `origin/ios_migration_september`, Schemastand 142, Referenzbasis `2026-09-25_R16_Anlagenprio`),
zwei Code-Commits `5946064f` (Posten B) und `8f92b7d7` (Posten A), die Papiere `83861a1b` und der
Merges `289211e0` von `origin` (`c02dbfb4`) und `9acde04b` (`bf129170`).
**Kein Schemaschritt, die Testdatenbank ist unberührt.** Alle Gates im Worktree, kein CI-Lauf bis
zum Push.

## Auftrag

Die Anwenderentscheide zu den fünf Folgen des Sammelpostens N18 aufnehmen und die beiden
umzusetzenden Posten in einem Zug erledigen, je ein Commit: (A) das Herkunftsprotokoll als eigene,
einklappbare Karte im Ergebnisbereich zeigen (ZU28); (B) den Größenschutz des Typtag-Paketlesers
nach dem Muster des Katalogimports nachziehen (ZU29). Kein Schemaschritt, kein Push, Hauptbaum
unberührt.

## Die Entscheide des Anwenders (25.09.2026)

Wörtlich: „1. … Empfehlung / 2. … Empfehlung / 5. Anzeigeort des Herkunftsprotokolls: Anzeige
ermöglichen (eigene karte im Ergebnisdialog) / 6. :Empfehlung"; zur dritten Frage die Rückfrage „Ist
Typtag für VDI 6007 erforderlich?".

| Nr. | Gegenstand | Entscheid |
|---|---|---|
| ZU25 | Konstruktorzeilen und redundanter Index | **ein** Schemaschritt, nach der Sichtabnahme Z1–Z5 |
| ZU26 | Katalogdialog auf iOS | eigene Welle nach iU11 |
| ZU27 | Bedeckungsgrad-Referenzfall | zurückgestellt, nicht an ZU7 gekoppelt |
| ZU28 | Anzeigeort des Herkunftsprotokolls | eigene Karte im Ergebnisdialog — **umgesetzt** |
| ZU29 | Größenschutz des Typtag-Paketlesers | Empfehlung angenommen — **umgesetzt** |

**Zur Rückfrage: Typtage sind für VDI 6007 nicht erforderlich.** Der Typtag-Weg nach VDI 4655 ist
eine wahlfreie Alternative des Jahresgangs **allein für Brauchwasser** (Konzept 4.2): Er verteilt
die Jahresenergie einer Zone über Typtage statt über Monats- und Wochenfaktoren. Das Gebäudemodell
nach VDI 6007 rechnet davon unabhängig aus Klimadaten, Bauteilen und Sollwerten; kein Rechenweg der
Gebäudesimulation liest eine Typtagzeile, und ohne eingespieltes Paket ist der Weg benannt nicht
verfügbar, während alles andere rechnet. Der Referenzfall für die Bewölkungsschwelle bräuchte einen
Bedeckungsgrad aus einem TRY-Import, den die Testdatenbank nicht führt, und käme sonst als dritte
Änderung in denselben Einfrierschritt wie ZU7. Er bleibt liegen, bis ein Anwender den Typtag-Weg
einsetzt; die Schwelle ist bis dahin an erfundenen Werten geprüft — benannt, nicht still.

## Was entstanden ist

**Posten B — Größenschutz des Typtag-Paketlesers** (`5946064f`).
`EPOS.Kern/Controller/TwwTyptagCtrl.cs` führt drei Konstanten (`HOECHSTENS_EINTRAEGE` 200,
`HOECHSTENS_BYTE_ENTPACKT` 64 MB, `HOECHSTENS_BYTE_JE_DATEI` 16 MB) und vier neue Hilfsmethoden
(`ZuGross`, `MengeZuGross`, `EintragLesen`, `Pfadsicher` samt `Laufwerksanfang`) — dasselbe Muster
wie `TwwNutzungsartCtrl.PaketLesen`. Die beiden Byte-Grenzen sind Parameter mit den Konstanten als
Vorgabe, damit ein Test an Kilobyte messen kann statt an 64 MB. Im Archiv gilt die Mengengrenze aus
dem Zentralverzeichnis, bevor ein Byte entpackt wird, und ein zweites Mal beim Lesen; die Summe
bricht **an** der Grenze ab statt überzulaufen. Ordner- und Einzeldateiweg prüfen dieselben Grenzen
aus dem Dateisystem. Drei eigene Kennungen in beiden Sprachen: `TYPTAGIMPORT_ZU_GROSS`,
`TYPTAGIMPORT_DATEI_ZU_GROSS`, `TYPTAGIMPORT_PFAD_UNZULAESSIG`.

**Posten A — Karte „Herkunft"** (`8f92b7d7`). `ZapfprofilHuelle.Herkunftszeilen` übersetzt das
Protokoll des Kerns in Zeilen der Oberflächensprache (`ZapfprofilHerkunftZeile`): Vermerk als
`ZapfSatz`, Stand (`Wertstatus`) und Quelle (`Herkunftsart` samt Regelwerk, Ausgabe und
Katalogfassung). Beide Ergebnis-DTO tragen die Liste; die Auslegung dazu das Kennzeichen
`HerkunftSichtbar`, das die Hülle aus der Stufe setzt. Gezeigt wird sie als zugeklappte
`<details>`-Karte unter der Warnliste — im Zapfprofildialog ab Stufe Erweitert, in der Überlagerung
„Auslegung" über das Kennzeichen. 30 Ressourcenschlüssel in beiden Sprachen, fünf CSS-Regeln, ein
Wiki-Absatz mit Anker `herkunft`.

## Abweichungen und Entscheidungen im Posten

**(1) Die Spalte „Größe" trägt den Feldnamen des Protokolls als DATEN.** Der Auftrag sagte „Zeile je
Eintrag (Schritt/Größe, Satz)", ohne die Sprache der Größe festzulegen. Gezeigt wird der Bezeichner
des Rechenwegs — `Tagesbedarf`, `Zirkulation.Laufzeit`, `Auslegung.ErzeugerKw` —, in beiden Sprachen
derselbe, wie ein Zonen- oder Katalogname. Grund: Die Feldnamen sind Bezeichner des Rechennachweises
und stehen ebenso in Kern, Tests und Referenzlauf; **zwölf von zweiundvierzig leben als
Zeichenketten außerhalb von `ZapfFeld`** (die Größen der Auslegung), eine Namenstafel wäre also eine
zweite Quelle der Wahrheit und bliebe unbewacht. Was der Anwender liest, ist übersetzt: der Vermerk,
der Stand und die Quelle. Folge in N19 („Namen").

**(2) Die Karte steht ab Stufe Erweitert, nicht in allen Stufen.** Der Auftrag ließ beides zu, wenn
die Karte klein bleibt. Sie bleibt nicht klein: Das Protokoll trägt je Zone ein Dutzend Zeilen und
in der Summe leicht fünfzig. In der Stufe Einfach zeigt der Dialog nur das Notwendige.

**(3) Leer heißt „nichts zu vermerken", die Karte bleibt stehen.** Eine weggelassene Karte wäre von
einer fehlenden nicht zu unterscheiden — und `Herkunftseintrag.Vermerk` = `null` heißt seit N18 (b)
genau diesen Satz.

**(4) Die Auslegung entscheidet über ein Kennzeichen der Hülle, nicht über eine eigene Stufe.** Die
Überlagerung „Auslegung" führt keine Stufe; ihre Hülle bekommt sie dagegen als Parameter
(`AlsAuslegung(…, ZapfprofilStufe stufe, …)`). Deshalb setzt die Hülle
`ZapfprofilAuslegungDaten.HerkunftSichtbar`, und der Dialog fragt nur das Kennzeichen — kein neuer
Parameter an der Überlagerung, keine zweite Stufenlogik.

**(5) Keine neue KI-Feldkarte.** Die Karte trägt keine Eingabestelle. Wie die Warnliste ist sie reine
Auskunft, und der `KiDialoge`-Katalog führt auch die Warnliste nicht;
`KiMaskenabdeckungWacheTests` behält seine Zahlen (59 / 20).

**(6) Der Einzeldateiweg des Typtaglesers bleibt, wie er war.** Beim Katalogimport nimmt er die
gewählte Datei in den Satz auf, auch wenn ihr Name nicht auf das Muster passt. Der Typtagleser
sammelt `*.csv` des Ordners; eine gewählte Datei ohne `.csv` gehörte nie dazu, und sie aufzunehmen
wäre eine Änderung des Verhaltens ohne Anlass.

## Neue Kennungen und Prüfstände

**Neue Kennungen:** drei Ablehnungen des Typtaglesers (`TYPTAGIMPORT_ZU_GROSS`,
`TYPTAGIMPORT_DATEI_ZU_GROSS`, `TYPTAGIMPORT_PFAD_UNZULAESSIG`) und 30 Beschriftungen der Karte
(`ZPG_HERKUNFT_…`, `ZPG_GRP_HERKUNFT`, `ZPG_AUS_HERKUNFT_…`) — alle in beiden Sprachen, Designer
nachgezogen und wiederholbar.

**Neue Prüfstände:** `EPOS.Kern.Tests/TwwTyptagPaketleserTests` (sechs Fälle ohne Datenbank, darunter
der `Archivluege`-Prüfstand), `EPOS.Kern.Tests/ZapfprofilHuelleHerkunftTests` (drei Fälle),
`EPOS.UI.Tests/Dialoge/ZapfprofilDialogStufenTests.Herkunft` (drei bunit-Fälle) und
`…/ZapfprofilAuslegungDialogTests.Herkunft` (zwei). **Gemessen dabei:** Ein lügendes
Zentralverzeichnis bläht das Typtagpaket nicht auf — `ZipArchiveEntry.Open` begrenzt den
Entpackstrom auf die ausgewiesene Größe, der Eintrag kommt **gekürzt** herein, und der gekürzte
fällt der Formprüfung des `Normformvektorleser` zu, nicht dem Größenschutz. Die zweite Wand ist
damit auch hier Vorsorge.

## Gates im Worktree

Nach den beiden Merges von `origin` — `289211e0` (`c02dbfb4`; Konflikt allein in der Statusdatei —
die origin-Fassung von #513, danach #516 in Nummernfolge, im Block der offenen Punkte #516 vor #513)
und `9acde04b` (`bf129170`; Konflikte in beiden `.resx` und im Sammel-Upload-Papier: beide Seiten
hatten am Ende angefügt, aufgelöst mit **beiden** Blöcken samt dem verlorenen `</data>` an der Naht,
0 doppelte Schlüssel, 11 620 Einträge je Sprache; im Papier je Tafelzeile die Seite, die sie geändert
hat), Designer nach dem resx-Merge neu erzeugt und ohne Diff:

- `dotnet build WP-Plan.Kern.slnf -c Release` — **0 Fehler** (46 Warnungen, Bestand).
- Gefilterte Tests (`Zapfprofil|Tww|Typtag|KiMasken|Huellen|ZapfSaetze|DokumentationLinkWache|WikiProduktdatenWache|RepositoryOrdnungWache`)
  — Kern **675** grün, UI **220** grün (Zapfprofil, KiMasken, Stilblatt), Dokumentationswachen 29/29.
- Voller Lauf `dotnet test WP-Plan.Kern.slnf -c Release --no-build` mit den xUnit-Schaltern des
  Hauses — **14 632 grün / 0 rot / 2 übersprungen** (Kern 7 287, UI 6 383, KiKern 549,
  SpeicherEngine 386, SpeicherPlanung 27+1).
- Windows-Schale mit `-p:EnableWindowsTargeting=true` — **0 Fehler**.
- `SqlDialektPruefer` gegen die Testdatenbank — **1 920 Texte, 0 Fundstellen** (kein SQL-Text ist
  berührt).
- Referenzlauf der sechs CI-Projekte 1030/1007/1017/1045/1046/1047 gegen
  `2026-09-25_R16_Anlagenprio` — **alle PASS (2 208 587 Werte)**, dieselbe Zahl wie in #508: der
  Posten ist ergebnisneutral.
- `ResourceDesigner` ohne Diff und wiederholbar, Tabuwörter der Wiki-Quelle 0 Treffer, Arbeitsbaum
  sauber, keine Konfliktmarker im Baum.
- Schemastand **142** unverändert, Testdatenbank unberührt.

## Folgen

Die Folgentabelle steht im Nachtrag N19: ZU25 (ein Schemaschritt nach der Sichtabnahme), ZU26
(iOS-Welle nach iU11), ZU27 (zurückgestellt), N18 (s) (Katalogimport um Bedarfstage und Parameter),
die Namenstafel der Größen, der fehlende Archivfall mit kleiner Grenze beim Katalogimport, der
Logbuch-Satz und der Wiki-Upload, dazu die Sichtabnahme unter Windows.
