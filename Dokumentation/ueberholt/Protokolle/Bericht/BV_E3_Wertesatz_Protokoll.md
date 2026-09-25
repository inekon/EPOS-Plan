# BV-E3 — Reiner Wertesatz (Protokoll)

Etappe BV-E3 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitt 13). Auftrag #528, Anwenderauftrag vom 26.09.2026: „fahre fort mit BV-E3“. Der gültige Stand steht im Konzept
(Rev. 5) und in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier steht, wie es geworden ist. Vorgänger:
[`BV_E2_Kapitel_Protokoll.md`](BV_E2_Kapitel_Protokoll.md). Zweig `konzept-berichtvorlagen` ab `57c6c53b` (BV-E2 samt
`origin/ios_migration_september`), umgesetzt am 26.09.2026; Fable 5.1 hat orchestriert, zwei Agenten (Opus 5.5) haben in
eigenen Worktrees gearbeitet, eine Wache entstand parallel — 6 Commits, 23 Dateien, +3.200/−157 Zeilen. Kein
Schemaschritt, kein Rechenweg berührt (Referenzlauf GESAMT: PASS), keine Basis und keine Messlatte neu eingefroren, keine
Ressource, keine Oberfläche, `EPOS.iOS/` nicht berührt. Für den Anwender ändert sich nichts Sichtbares: kein
Logbucheintrag, keine Wiki-Quelle.

| Agent | Gegenstand | Commits | Zusammenführung |
|---|---|---|---|
| W1 Sammler | Wertesatz `BerichtsDaten.Wirtschaft` (`WirtschaftsBerichtswerte`, `Traegerpreissatz`), die Schreiber auf dem Wertesatz, `Berichtsbedarf` in Vorprüfung, Hülle und Sammler, Tests | `67a18de4`, `c7f3de35`, `89a33982` | `7cfd8cec` |
| W2 Beste Variante | `BesteVariante.Waehle` im Kern, Kacheln der Wirtschaftlichkeitsseite auf der Kernregel, Tests | `95375033` | auf den Zweig vorgespult |
| Wache | `BerichtSchreiberOhneDatenbankWacheTests`, Regelzeile in `EPOS.Kern/CLAUDE.md` (Abschnitt „Bericht“) | `eefb308c`, `45ede76c` | auf dem Zweig nach `81daa0b8` |

---

## 1 Wertesatz (W1)

### 1.1 Aufbau

`BerichtsDaten.Wirtschaft` (neu) ist die Wirtschaftlichkeit eines Berichtslaufs als reiner Wertesatz — Klasse
`WirtschaftsBerichtswerte` in `EPOS.Kern/Allgemein/Bericht/WirtschaftsBerichtswerte.cs`. Was Wirtschaftlichkeitsbaustein,
Anhang-E-Checkliste, Tabellenbericht, Formelmappe und die Kälteerzeugertafel der Projektbeschreibung beim Schreiben aus der
Datenbank luden oder daraus rechneten, steht darin; beim Schreiben wird die Datenbank nicht berührt.

- **Ermitteln im Sammler.** `BerichtsDatenSammler.SammleFuerBericht` ruft nach `RechneWirtschaftlichkeit` EINMAL
  `WirtschaftsBerichtswerte.Ermittle(daten, bedarf)` — mit der Sicht, der Referenz und der Bewertung, die die Rechnung am
  Baum hinterlassen hat — und legt den Satz als `daten.Wirtschaft` ab. `Ermittle` rechnet die Teile in der Folge, in der
  der Wortbericht sie las, danach, was Mappe, Formelmappe, Anhang E und Kälteerzeugertafel zusätzlich lesen. Word und
  Excel lesen denselben Satz; der Kapitalwertverlauf wird einmal je Lauf gerechnet statt einmal je Ausgabe.
- **Jeder Teil ist genau der Aufruf, den die Schreiber vorher selbst taten** — dieselbe Methode, dieselben Eingaben,
  dieselbe Fehlerbehandlung: Wo der Schreiber einen Fehler fing, fängt ihn der Teil, wo nicht, nicht. Das ist die Regel
  „Eine Auskunft ruft den Rechenweg des Laufs — sie schreibt ihn nicht ab“ (`EPOS.Kern/CLAUDE.md`). Ein Controller
  (`WirtschaftlichkeitCtrl`) dient allen Teilen, wie vorher einer je Schreiber — seine Zwischenspeicher (Referenzkessel,
  Tarif) sehen dieselbe Folge.
- **Scheitert ein Teil im Sammler,** bleibt er offen, und der Lauf geht weiter (ein Abbruch geht durch); der Schreiber
  rechnet ihn beim Lesen wie vorher — samt dessen Fehlerbild.
- **Rückfall ohne Sammler.** Ein Baum, der nicht durch den Sammler ging (Proben, Prüfstände), bekommt über
  `WirtschaftsBerichtswerte.Von(daten)` einen Satz, der jeden Teil erst beim ersten Lesen rechnet — Zahl für Zahl und
  Zugriff für Zugriff der Weg der Schreiber vor BV-E3, einmal je Schreiber; dieser Satz wird nicht am Baum abgelegt.
- **Nachgeholt.** Liest ein Schreiber nach dem Sammeln einen Teil, den der Sammler nicht gerechnet hat — etwa den Verlauf,
  obwohl der Bedarf ihn ausschloss, weil statt der geprüften Vorlage die Standardvorlage einsprang —, rechnet der Teil nach
  (derselbe Aufruf) und steht in `Nachgeholt`. Der Bericht bleibt vollständig; im Regelfall ist die Liste leer.
- **Kulturabhängige Texte** (Nachweiszeile der Parameter, Trägerpreiszeilen) hält der Satz je Kultur; schreibt ein
  Schreiber in einer anderen Kultur als der des Sammelns, rechnet der Teil für diese Kultur nach.
- Zum Nachweis des Bedarfs zählt der Satz, wie oft er Verlauf und Emissionsbilanz gerechnet hat (`VerlaufRechnungen`,
  `EmissionsbilanzRechnungen`, intern); `Gesammelt` und `Bedarf` sagen, wie er entstand.

### 1.2 Teile und gerufene Rechenwege

| Teil | gerufener Rechenweg | im Sammler |
|---|---|---|
| `Ergebnisse`, `AusDiesemLauf` | die Ergebnisse dieses Laufs (`daten.Wirtschaftlichkeit`), sonst `WirtschaftlichkeitCtrl.LadeErgebnisse` — das Rückfallnetz, falls die Rechnung scheiterte | immer |
| `Parameter` | `WirtschaftlichkeitCtrl.LadeParameter` | immer |
| `Tarif` | `WirtschaftlichkeitCtrl.LadeTarif` | immer |
| `Bewertung` | die des Sammlers (`daten.Bewertung`), ohne sie `WirtschaftlichkeitBewertung.FuerBericht` | nur ohne Bewertung des Sammlers und mit Ergebnissen |
| `Wirkungen` | `ProjektWirkungCtrl.Laden` — der Rückfall der Checkliste ohne Bewertung | nur ohne Wirkungen der Bewertung |
| `Parameternachweis(kultur)` | `WirtschaftlichkeitParameter.Nachweis` (liest die KWKG-Lage über `KwkgAktivierung`) | in der Kultur des Laufs |
| `Bilanzkonvention` | `BilanzKonvention.Bestimme(p, new GesetzKatalog())` | immer |
| `Erzeuger` | `WirtschaftlichkeitCtrl.ErzeugerDerGruppe` (ein Fehler gibt `null`, wie im Baustein) | immer |
| `ErgebnisAktuell(e)` | `WirtschaftlichkeitCtrl.ErgebnisAktuell`, je Ergebnis einmal | je Stand mit Ergebnis „Erwartet“ |
| `Zeilen(idReferenz)`, `IdReferenzTafel` | `WirtschaftlichkeitZeilen.Kennzahlen(alle, tarif, idReferenz)` samt Speicherkontexten, ungefiltert — die Sichtbarkeit entscheidet der Schreiber (`Sichtbare`); Referenz der Tafel in Sicht 2 der Stand A, sonst die der Gruppe | gegen die Referenz der Tafel |
| `KwkgAktiv`, `ZeitreihenNoetig`, `VerlaufEntfaellt` | `KwkgAktivierung.IstAktiv` und das Konsistenz-Gate des Verlaufs: Brauchen die Zahlungsreihen Stundenreihen (wirksamer Tarif oder KWKG) und fehlen sie einem Stand ohne Fehler, entfällt der Verlauf mit Hinweis — eine Regel für Word und Excel | immer |
| `Verlauf` | `WirtschaftlichkeitCtrl.BerechneVerlaufSzenarienJeZeitraum` (ein Fehler gibt `null`, wie im Baustein) | nur mit Bedarf „Verlauf“ und wenn er nicht entfällt |
| `Traegerpreiszeile(szenario, kultur)` | `TraegerpreisSzenario.Nachweiszeile` über alle Stände | Ungünstig und Günstig |
| `Strommatrizen` | `WirtschaftlichkeitCtrl.LadeStromMatrix` | immer |
| `Referenzkessel` | `WirtschaftlichkeitCtrl.LiesReferenzkessel` | nur mit Bedarf „Emissionsbilanz“ und Kraftwerkspark |
| `Emissionsbilanz(id)` | `EmissionsBilanzRechner.Berechne(id, p)` | ebenso, je Stand mit aktuellem Ergebnis |
| `Traegerpreise(stand)` | `Traegerpreissatz.Lies` (neu): die Träger mit Szenariopreis (`EnergietraegerPreisCtrl.SzenarioJeTraeger`), ihr Name und die wirksamen Preise der drei Szenarien (`KostenEmissionRechner.PreisSatz`) — die Leseschritte, die im Parameterblock der Formelmappe standen, in derselben Folge; die Formelmappe ruft dieselbe Methode in ihren übrigen Überladungen | je Stand |
| `Traegername(id)` | `Emissionsquelle.TraegerName` — die Kühlträger der Kälteerzeugertafel | je Kühlträger der Stände |

Die Schreiber lesen nur noch den Satz: `Bausteine/BausteineWirtschaftlichkeit.cs` (Word),
`ExcelBerichtGenerator.BlattWirtschaftlichkeit` samt `ExcelFormelmappe.Parameterblock` (neue Überladung mit den
Trägerpreisen des Satzes), `AnhangECheckliste.cs` und die Kälteerzeugertafel in `Bausteine/BausteineProjekt.cs`
(`KuehltraegerText` mit übergebener Namensquelle). Die Wache aus Abschnitt 4 hält sie dort.

### 1.3 Verschwundene Zugriffe

Die Zugriffe und Nebenrechnungen, die beim Schreiben entfallen, mit ihren Zeilen im Stand `57c6c53b`:

| Datei | Zeilen | Aufrufe |
|---|---|---|
| `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` | :33, :41, :61, :69, :76, :80, :82, :116, :196, :208, :253, :271, :840, :992, :999, :1294, :1326 | Controller, `LadeErgebnisse`, `LadeParameter`, `FuerBericht`, `LadeTarif`, `p.Nachweis`, `BilanzKonvention.Bestimme`, `ErgebnisAktuell`, `LadeStromMatrix`, `LiesReferenzkessel`, `KwkgAktivierung.IstAktiv`, `BerechneVerlaufSzenarienJeZeitraum`, `WirtschaftlichkeitZeilen.Kennzahlen`, `ErgebnisAktuell` und `EmissionsBilanzRechner.Berechne` der Emissionsbilanz, `TraegerpreisSzenario.Nachweiszeile`, `ErzeugerDerGruppe` |
| `EPOS.Kern/Allgemein/Bericht/ExcelBerichtGenerator.cs` | :385, :390, :411, :412, :419, :421, :428, :442, :461, :513, :575, :767, :790, :972, :1030, :1031 | Controller, `LadeErgebnisse`, `LadeParameter`, `LadeTarif`, `FuerBericht`, `p.Nachweis`, `BilanzKonvention.Bestimme`, Parameterblock mit den Trägerpreisen, `ErgebnisAktuell`, `WirtschaftlichkeitZeilen.Kennzahlen`, `TraegerpreisSzenario.Nachweiszeile`, `KwkgAktivierung.IstAktiv`, `BerechneVerlaufSzenarienJeZeitraum` — der zweite Verlaufslauf des Berichts entfällt —, `LadeStromMatrix`, `ErgebnisAktuell` und `EmissionsBilanzRechner.Berechne` der Emissionsbilanz |
| `EPOS.Kern/Allgemein/Bericht/AnhangECheckliste.cs` | :389, :392, :393, :397, :404 | Controller, `LadeErgebnisse`, `LadeParameter`, `FuerBericht`, `ProjektWirkungCtrl.Laden` |
| `EPOS.Kern/Allgemein/Bericht/ExcelFormelmappe.cs` | :253–259 | `EnergietraegerPreisCtrl.SzenarioJeTraeger`, `Emissionsquelle.TraegerName`, dreimal `KostenEmissionRechner.PreisSatz` — jetzt `Traegerpreissatz.Lies` |
| `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineProjekt.cs` | :232 | `Emissionsquelle.TraegerName` der Kühlträger |
| `EPOS.UI.Daten/Bericht/BerichtSeiteGaben.cs` | :275–277 | die Leistungspreis-Regel der Stundenreihen — sie steht jetzt im Sammler (2.3) |

### 1.4 Messung

- **Zugriffe beim Schreiben** (Word / Excel), gezählt mit dem werfenden Zugriff der Tests (Abschnitt 6): vorher — Stand
  `57c6c53b` — 342 / 359 für 1030 und 383 / 400 für die Gruppe 1019, nachher 0. Gegenprobe ohne Sammler (Rückfall
  `Von`): 711 Zugriffe für 1019 — die Zählung sieht sie.
- **Vorher und nachher gleich:** Gegen den Stand `57c6c53b` (per `git archive` ausgepackt) sind die Inhaltsabzüge von Word
  auf dem bisherigen Weg, Word auf dem Vorlagenweg und Excel gleich — für 1030, die synthetische Gruppe der Messlatte,
  1019 und 1026.
- **Laufzeit:** Abschnitt 7.

## 2 Bedarf (W1)

### 2.1 Flags und Kapitel

`Berichtsbedarf` (neu, `EPOS.Kern/Allgemein/Bericht/Berichtsbedarf.cs`) sagt, was der Sammler über den Regellauf hinaus
erhebt: die Stundenreihen des Laufs (`Zeitreihen`), den Kapitalwertverlauf (`Verlauf`) und die Emissionsbilanz gekoppelt
gegen getrennt (`Emissionsbilanz`). Es sind die Flags des Katalogs (`Vorlagenbedarf`) — eine zweite Liste gibt es nicht —,
dazu `Alles`, `Nichts` und die Vereinigung `Mit`. Simulation und Wirtschaftlichkeitsrechnung laufen immer.

Die Kapitel tragen den Bedarf ihres Bausteins (`Berichtskapitel.Bedarf`, neu), der Katalog gibt ihn an
`{{kapitel.<name>}}` weiter; `VorlagenfeldkatalogWacheTests` hält die Liste:

| Schlüssel | Bedarf | Grund |
|---|---|---|
| `kapitel.ergebnisse` | Zeitreihen | Ganglinien aus den Stundenreihen |
| `kapitel.wirtschaftlichkeit` | Verlauf, Emissionsbilanz | Kapitalwertverlauf samt Brücke und Mehrjahrestafeln, Emissionsbilanz |
| `bericht.inhalt` | alle drei | Vereinigung aller Kapitel; im Lauf zählen nur die angehakten |
| `stamm.kennzahl.kaelte.stunden` | Zeitreihen | gezählt an der Kanalreihe des Laufs (im Katalog schon vorher so geführt) |
| die übrigen Kapitel und Schlüssel | keiner | sie lesen, was jeder Lauf erhebt; die Speichertemperaturen der Projektbeschreibung sind eine Beigabe, wenn die Stundenreihen ohnehin da sind (Abschnitt 8 a) |

### 2.2 Die drei Wege

- **`Vorgabe(konfig)`** — der Bedarf ohne Vorlage und der Bedarf der Mappe: die Vereinigung über die angehakten Kapitel,
  also Zeitreihen mit „Ergebnisse je Variante“, Verlauf und Emissionsbilanz mit „Wirtschaftlichkeit“; ohne Konfiguration
  alles. Das ist das Verhalten vor BV-E3.
- **`AusVorlage(pruefbefund, konfig)`** — die Vereinigung der `Bedarf` der genutzten Schlüssel einer geprüften Vorlage. Ein
  Kapitelplatzhalter zählt nur mit gesetztem Häkchen, sonst schreibt die Engine an seiner Stelle nichts; der Sammelanker
  `{{bericht.inhalt}}` und eine Vorlage ohne Platzhalter (die Engine hängt ihr den Sammelanker an) tragen den Bedarf der
  angehakten Kapitel; eine unlesbare Vorlage fällt auf die Vorgabe zurück. **Zusatzregel (`89a33982`):** Zeigt die Vorlage
  ein Kapitel am Häkchen „Wirtschaftlichkeit“ — Wirtschaftlichkeit oder Anhang E —, erhebt der Lauf die Stundenreihen wie
  ohne Vorlage nach dem Häkchen „Ergebnisse je Variante“, auch wenn die Vorlage das Kapitel „Ergebnisse“ nicht führt. Die
  Zahlen der Wirtschaftlichkeit rechnen mit den Stundenreihen, wo es sie gibt (Strommatrix, Aufteilung des KWK-Stroms,
  stündliche Einspeisung, Konsistenz-Gate des Verlaufs bei Tarif und KWKG): Die Vorlage bestimmt, was der Bericht zeigt,
  nicht, wie eine gezeigte Zahl entsteht. Eine Vorlage ohne Zahl der Wirtschaftlichkeit, etwa nur ein Deckblatt, lässt
  die Reihen weg.
- **`FuerLauf(konfig, start, weg, mitExcel)`** — der Bedarf des Laufs: der der Vorlage, die der Word-Lauf füllt, mit Mappe
  vereinigt mit der Vorgabe (die Mappe folgt den Häkchen bis BV-E7). Ohne Startbefund (kein Word, oder die Vorprüfung
  scheiterte), mit dem Weg „Mit Standardvorlage“ statt einer eigenen Vorlage und beim Rückfall auf den bisherigen Weg gilt
  die Vorgabe — die Standardvorlage führt jedes angehakte Kapitel über den Sammelanker. Der Vorlagenteil wird mit der
  Konfiguration des Laufs gebildet.

### 2.3 Im Lauf

- `BerichtCtrl.PruefeVorStart` legt den Bedarf der Vorlage in `Startbefund.Bedarf` (beim Rückfall die Vorgabe).
- Die Hülle (`BerichtSeiteGaben`) bildet nur noch `mitExcel` und reicht `Berichtsbedarf.FuerLauf(konfig, start, weg,
  mitExcel)` an den Sammler; ihr Sammler-Haken für Prüfstände (`Sammler`) nimmt `Berichtsbedarf` statt `bool`.
- `SammleFuerBericht` hat eine neue Überladung mit `Berichtsbedarf` (`null` = alles). Die beiden bisherigen mit
  `bool mitZeitreihen` bleiben: Der Schalter wird der Zeitreihenteil, Verlauf und Emissionsbilanz erhebt der Lauf dort wie
  bisher immer.
- **Die Stundenreihen, die die Rechnung braucht,** ergänzt der Sammler unabhängig vom Bedarf: Ist am Stromträger ein
  Leistungspreis gepflegt (`KostenEmissionRechner.StromLeistungspreisGepflegt` über die ganze Gruppe; SP-W1, LS-E-2),
  braucht die Kostenseite die Bezugsspitze. Die Regel stand in der Hülle und gilt jetzt für jeden Aufrufer — der Bedarf
  sagt, was der Bericht zeigt, nicht, was die Zahlen richtig macht. `ZeitreihenErhoben` (intern) zählt die eingesammelten
  Sätze.

## 3 Beste Variante (W2)

### 3.1 Signatur

`EPOS.Kern/Allgemein/Wirtschaftlichkeit/BesteVariante.cs` (neu, statisch, plattformfrei und ohne Datenbank — die Klasse
bekommt die Ergebnisse gereicht, rechnet nichts und wählt nur aus):

```csharp
public static Auswahl Waehle(IReadOnlyList<WirtschaftlichkeitErgebnis> ergebnisse, int idStamm,
                             IReadOnlyList<int> staende = null,
                             string szenario = WirtschaftlichkeitSzenario.ERWARTET)
```

`Auswahl` trägt `IdProjekt`, `Ergebnis`, `Grund`, `Szenario` und `IstVariante` (gleich `Grund == BestesKriterium`).
`Auswahlgrund` ist `KeinErgebnis` (nichts zu zeigen: `Ergebnis` ist `null`, `IdProjekt` nennt `idStamm`),
`StammOhneVarianten` (keine Variante trägt eine Kapitalwertdifferenz, es steht der Stamm) oder `BestesKriterium`.

### 3.2 Regel

1. **Szenario:** Gewählt wird im Erwartungsfall — gleich, welches Szenario die Seite darunter zeigt; ein anderes nur, wenn
   der Aufrufer es ausdrücklich nennt.
2. **Stände:** die übergebenen Stände in ihrer Reihenfolge (auf der Seite die gewählten Spalten, in Sicht 2 A und B), je
   Stand sein erstes Ergebnis im Szenario; ein Stand ohne Ergebnis nimmt nicht teil. Ohne Standliste alle Ergebnisse des
   Szenarios in Listenfolge, je Stand das erste.
3. **Kriterium:** die größte `KapitalwertDiff` unter den Ergebnissen ohne den Merker `IstStamm`, die eine Differenz tragen.
   Die Referenz trägt keine und nimmt nie teil, der Stamm auch dann nicht, wenn er gegen eine Referenzvariante eine
   Differenz trägt. Auch eine negative Differenz gewinnt — ein Vergleich mit dem Stamm findet nicht statt; die Karte zeigt
   dann, um wie viel die beste Variante schlechter ist.
4. **Gleichstand:** Es bleibt der Stand, der in der Reihenfolge zuerst kommt.
5. **Ohne Variante mit Differenz:** das erste Ergebnis mit `IstStamm` (`StammOhneVarianten`); fehlt auch das,
   `KeinErgebnis`.

Stamm heißt Merker: Ob ein Ergebnis der Stamm ist, entscheidet `IstStamm`, nicht der Vergleich mit `idStamm`.

### 3.3 Hülle und Nachweis

Die Wirtschaftlichkeitsseite wählt nicht mehr selbst: `WirtschaftlichkeitSeiteGaben` ruft
`Kacheln(BesteVariante.Waehle(_ergebnisse, _idStamm, spaltenIds), kultur)` mit den Spalten der Wahl. Die Auswahlschleife der
Hülle (vorher `WirtschaftlichkeitSeiteGaben.cs:1433-1442`) ist entfallen; `Kacheln` zeigt nur noch, wie die Auswahl auf den
vier Karten aussieht, den Anzeigenamen der besten Variante löst weiter die Hülle auf. Die Regel ist unverändert übernommen:
Die Kachelwerte sind vorher und nachher byte-gleich in 107 Fällen (15 Stämme der Testdatenbank, alle Häkchenwahlen der
Stände, drei Szenarien, in Sicht 2 alle Paare). Tests: `BesteVarianteTests` (13 Fälle, synthetisch, ohne Datenbank) und
`BesteVarianteHuellenTests` (3 Fälle: Die Karten zeigen die Wahl des Kerns an der Gruppe 1019 in allen drei Ausgängen und
an 1030 ohne Ergebnis; in der gerechneten synthetischen Gruppe findet die Regel die günstigste Variante). Den Bericht füllt
die Regel mit `wirtschaft.beste.*` erst in BV-E4.

## 4 Wache „Schreiber ohne Datenbank“

`BerichtWertesatzTests` weist das Füllen ohne Datenbank im Lauf nach (Abschnitt 6). Dass es so bleibt, hält die Wache
`EPOS.Kern.Tests/BerichtSchreiberOhneDatenbankWacheTests.cs` am Quelltext (639 Zeilen, `eefb308c`). Die Regel steht als
eigener Absatz am Ende des Abschnitts „Bericht“ in `EPOS.Kern/CLAUDE.md` (`45ede76c`): Die Berichtsschreiber (Bausteine,
Anhang E, Excel-Generator, Formelmappe, Vorlagenfüller) lesen nur `BerichtsDaten` — alles aus der Datenbank sammelt
`BerichtsDatenSammler.SammleFuerBericht` einmal in `BerichtsDaten.Wirtschaft`. Was ein Schreiber neu braucht, wird ein
Teil des Wertesatzes und kommt über den Sammler.

- **Bewacht** sind der ganze Ordner `EPOS.Kern/Allgemein/Bericht/Bausteine/` — ein neuer Baustein fällt ohne Zutun
  darunter — und die Einzelschreiber `AnhangECheckliste.cs`, `ExcelBerichtGenerator.cs`, `ExcelFormelmappe.cs`,
  `VerlaufExcel.cs` und `Vorlagen/WordVorlagenfueller.cs`.
- **Gelesen wird der Programmtext:** Ein eigener Leser blendet Kommentare, Präprozessorzeilen und den Inhalt von
  Zeichenketten und Zeichen aus, liest aber die Löcher interpolierter Zeichenketten und Folgezeilen, die mit `*`
  beginnen — die Schreiber setzen Texte zusammen, ein Aufruf in `$"…{…}…"` ist ein Aufruf.
- **28 Muster in drei Gruppen:** die Zugriffsschicht (`new DataRepository`, `DataRepository.`, `StilleDb.`,
  `RecordSet`, `IDatenzugriff`, `Datenzugriff.`, `DbParam`, `ExecuteScalar`/`ExecuteNonQuery`/`ExecuteReader`/
  `GetDataTable`, SQLite direkt); die Controller (`new …Ctrl(`, `…Ctrl.Glied`, `…Ctrl` als Feld, Parameter oder
  Variable); die Rechenwege mit Datenbank, die der Wertesatz einmal ruft (`LadeErgebnisse(`, `LadeParameter(`,
  `LadeTarif(`, `LadeStromMatrix(`, `LiesReferenzkessel(`, `EmissionsBilanzRechner.Berechne(` und `Lade…(`,
  `ProjektWirkungCtrl.Laden(`, `ErzeugerDerGruppe(`, `BerechneVerlaufSzenarien…(`, `KwkgAktivierung.IstAktiv(`,
  `Emissionsquelle.`, `KostenEmissionRechner.`, `Traegerpreissatz.Lies(`, `TraegerpreisSzenario.Nachweiszeile(`,
  `WirtschaftlichkeitBewertung.FuerBericht(`, `new GesetzKatalog(`).
- **Kein Fund ist `WirtschaftsBerichtswerte.Von(daten)`:** Nach dem Sammeln gibt er den gesammelten Satz zurück; den
  Rückfall, der jeden Teil beim ersten Lesen rechnet, trägt `WirtschaftsBerichtswerte.cs`, nicht der Schreiber.
- **Positivliste:** sieben Einträge für acht Stellen, je Datei, Muster und Treffer mit genauer Anzahl und Grund. Rot wird
  die Wache, wenn eine Anzahl nicht mehr stimmt oder ein Eintrag keinen Fund mehr hat — so wächst keine Ausnahme still
  mit, und keine bleibt als Lücke stehen.

| Stelle | Treffer | Grund |
|---|---|---|
| `ExcelFormelmappe.cs:1021`, `:1027`, `:1030`, `:1034` | `BetriebskostenCtrl.Bemessungsfaktor`, `.MengenEinheit`, `.SatzEinheit`, `.Betrag`, je einmal | reine Funktionen des Betriebskosten-Rechenwegs; `BemessungKatalog` ist eine Tafel im Code, keine Datenbank |
| `Bausteine/BausteineWirtschaftlichkeit.cs:1320` | `WirtschaftlichkeitCtrl.ErzeugerFlags` | nur der Typname, in `WirtschaftlichkeitCtrl` geschachtelt; der Wert kommt aus dem Wertesatz |
| `Bausteine/BausteineProjekt.cs:304`, `:315` | `Emissionsquelle.TraegerName`, zweimal | die Überladung `KuehltraegerText(m)` ohne Namensquelle und der Rückfall der Überladung mit Quelle; der Bericht übergibt die Namen des Wertesatzes |
| `ExcelFormelmappe.cs:123` | `Traegerpreissatz.Lies` | die Überladungen des Parameterblocks ohne Trägerpreisquelle; der Tabellenbericht übergibt `w.Traegerpreise` |

Drei Fälle: (1) `Kein_Berichtsschreiber_greift_auf_Datenbank_oder_Controller_zu` ist die Wache selbst.
(2) `Die_Wache_erkennt_einen_eingebauten_Verstoss`: Ein Probeschreiber trägt je Muster einen Verstoß — alle 28 fallen auf
der Einbauzeile auf, auch im Loch einer interpolierten Zeichenkette und auf einer `*`-Folgezeile —, zehn Nichtfälle
(Kommentar, Zeichenkette, Rohzeichenkette, Präprozessorzeile, der Wertesatz) bleiben stumm, und die Positivliste ist eng:
dieselbe Datei, dasselbe Glied, dieselbe Anzahl. (3) `Die_Wache_sieht_alle_Schreiber_und_jeden_Baustein`: Alle benannten
Schreiber sind da, jeder `IBerichtsBaustein` der Assembly ist in einer bewachten Datei deklariert, die drei Schreiber des
Wertesatzes (Wirtschaftlichkeitsbaustein, Anhang E, Tabellenbericht) lesen ihn über `WirtschaftsBerichtswerte.Von`, jede
Ausnahme ist gültig. Die Laufzeithälfte — `Nachgeholt` bleibt leer — steht in `BerichtWertesatzTests` und wird nicht
gedoppelt. **Rotprobe:** Ohne Positivliste meldet die Wache genau die acht Stellen mit ihren Zeilen, dazu siebenmal
„Ausnahme ohne Fund“. Ihre Abnahme steht in Abschnitt 6.

## 5 Entscheidungen der Agenten

### 5.1 Abweichungen und Auslegungen des Konzepts (Rev. 4)

Das Konzept Rev. 5 trägt sie nach.

| Konzept | Umgesetzt | Grund |
|---|---|---|
| 5.1: Zugriffe und Nebenrechnungen des Wirtschaftlichkeitsbausteins und von Anhang E wandern in den Sammler | dazu die des Tabellenberichts, der Formelmappe (Trägerpreise) und der Kälteerzeugertafel (Trägernamen) | Word und Excel lesen denselben Satz, der Verlauf läuft einmal; kein Schreiber behält einen eigenen Datenbankweg |
| 5.1: der Sammler ruft dieselben Rechenwege | dazu der Rückfall `Von` für Bäume ohne Sammler und das Nachholen statt eines Fehlers | Proben und Prüfstände bleiben unverändert; ein Bericht bleibt vollständig, auch wenn statt der geprüften Vorlage die Standardvorlage einspringt |
| 5.1, 8.5: `Bedarf` steuert Zeitreihen, Verlauf, Emissionsbilanz | die Kapitel tragen den Bedarf ihres Bausteins; Zusatzregel für Wirtschaftlichkeit und Anhang E; die Leistungspreis-Regel im Sammler | die Zahlen der Wirtschaftlichkeit hängen an den Stundenreihen |
| 13, Abnahme: „Word gegen Excel für alle `stand.wirtschaft.*`“ | geprüft an den Zeilen der Kennzahltafel (`WirtschaftlichkeitZeilen`, aus denen `stand.wirtschaft.<zeile>` entsteht): Word „Erwartet“ Zelle für Zelle gegen die Mappe im Format der Zeile, die Mappe in allen drei Szenarien gegen den Wertesatz, die Szenarienübersicht gegen die Bandbreitentafel | die Schlüssel `stand.wirtschaft.*` kommen mit BV-E4 |
| 13, Abnahme: „Test mit werfendem `IDatenzugriff` beim Füllen“ | ein werfender `IDatenzugriff` unter `DataRepository.Zugriff`, dazu ein Datenbankpfad ins Leere und die Ausnahmen der ersten Chance | auch eine eigene Verbindung und ein Aufrufer, der den Fehler fängt, fallen auf |
| 5.1, 9.5: die beste Variante in den Kern | die Regel unverändert aus der Hülle übernommen; `wirtschaft.beste.*` folgt mit BV-E4 | BV-E3 füllt keine neuen Schlüssel; Kacheln byte-gleich |

### 5.2 Entscheidungen der Agenten

- **W1:** ein Teil je Aufruf der Schreiber samt deren Fehlerbehandlung; ein Controller für alle Teile; der Rückfall `Von`
  statt eines Pflichtsammlers; Nachholen statt eines Fehlers, benannt in `Nachgeholt`; kulturabhängige Texte je Kultur;
  die Trägerpreise als eigene Klasse `Traegerpreissatz` mit `Lies`, gerufen von Sammler und Formelmappe; den Bedarf der
  Kapitel am Baustein abgelesen und die Projektbeschreibung ohne Bedarf — so bleibt die Standardvorlage beim Bedarf von
  heute; die Überladungen von `SammleFuerBericht` mit `bool` bleiben; der Hinweis des entfallenden Verlaufs („… Baustein
  „Ergebnisse je Variante“ aktivieren“) bleibt unverändert — mit der Zusatzregel trifft er weiter zu.
- **W2:** Stamm heißt Merker; ein Stand ohne Ergebnis nimmt nicht teil, statt als Null zu zählen; ohne Standliste zählen
  alle Ergebnisse des Szenarios; ein anderes Szenario nur ausdrücklich; `Zahlungsgliederungen.Leitversion` ist nicht
  angeglichen (Abschnitt 8 b).
- **Wache:** ein eigener Leser statt der zeilenweisen Vereinfachung der übrigen Quelltextwachen, weil ein Aufruf im Loch
  einer interpolierten Zeichenkette zählt; der Bausteinordner ganz statt einer Dateiliste; eine Positivliste mit genauer
  Anzahl statt Musterausnahmen; die Überladungen für Aufrufer außerhalb des Berichtslaufs bleiben als Ausnahme stehen
  (Abschnitt 8 g); der Laufzeitnachweis bleibt in `BerichtWertesatzTests`.

### 5.3 Zusammenführung

W2 wurde auf den Zweig vorgespult (`95375033`), W1 mit `7cfd8cec` zusammengeführt, beides konfliktfrei; End-Merge mit
`origin/ios_migration_september` (`dec0a777`): `81daa0b8`. Die Wache folgte auf dem Zweig mit `eefb308c` (Wache) und
`45ede76c` (Regelzeile). Die Sitzung war zwischenzeitlich unterbrochen; der W1-Agent wurde mit seinem committeten Stand
wieder aufgenommen.

## 6 Abnahme

In den Agenten-Worktrees:

- **W1:** Kern-Filter 0 Fehler; gefilterter Testlauf 620 von 620 grün; Windows-Schale 0 Fehler; Referenzlauf der
  14 Projekte gegen die Basis R19 GESAMT: PASS (4.610.207 Werte); SQL-Prüfer ohne Fundstelle.
- **W2:** Kern-Suite 7.599 grün; Kachelwerte vorher wie nachher byte-gleich in 107 Fällen (Abschnitt 3.3).
- **Wache:** Build 0 Fehler; ihre drei Fälle zusammen mit Dokumentations-, Kodierungs- und Ordnungswache 30 von 30
  grün; `BerichtWertesatzTests` 8 von 8; Rotprobe ohne Positivliste wie in Abschnitt 4.

Die Tests der Etappe: `BerichtWertesatzTests` mit 8 Fällen — (a) nach `SammleFuerBericht` für 1030 und die Gruppe 1019
(zwei Varianten, Kraftwerkspark: Emissionsbilanz und Referenzkessel) mit werfendem Zugriff und Datenbankpfad ins Leere:
Word auf dem bisherigen Weg, Word über die Standardvorlage und die Mappe entstehen gültig, 0 Zugriffe, nichts nachgeholt;
(b) Word gegen Excel (Abschnitt 5.1); (c)/(d) die Proben 1030 und Gruppe der Messlatte treffen mit und ohne Sammler die
eingefrorenen Messlatten des bisherigen Wegs, des Vorlagenwegs und der Mappe und sind Zahl für Zahl gleich — Absätze,
Zellen, Bildprüfsummen, Excel-Namen —, nichts neu eingefroren; (e) Bedarf: Die Vorgabe folgt den Häkchen, der Bedarf
einer Vorlage kommt aus ihren Platzhaltern, eine Vorlage nur mit Deckblattangaben erhebt weder Zeitreihen noch Verlauf
(Sammleraufrufe gezählt), die Standardvorlage beides wie heute, und der Weg der Hülle nach dem Sammeln braucht keine
Datenbank; (f) Laufzeit (Abschnitt 7). Dazu `BerichtsvorlagenHuelleTests` (ein neuer Fall: Der Lauf reicht den Bedarf an
den Sammler — Deckblattvorlage nur Word: nichts, mit Mappe: deren Häkchen, nur Mappe: die Vorgabe),
`VorlagenfeldkatalogWacheTests` (die Bedarfswache kennt die Kapitel), `BesteVarianteTests` 13,
`BesteVarianteHuellenTests` 3 und `BerichtSchreiberOhneDatenbankWacheTests` 3.

Die Etappe berührt keinen Rechenweg; der Referenzlauf gehört nach Konzept 13 trotzdem zu ihrer Abnahme und ist in W1
erbracht. Berührt sind Kern, Hülle (`EPOS.UI.Daten`), Kern-Tests und `EPOS.Kern/CLAUDE.md` (Regelzeile der Wache); keine
Oberfläche, keine Ressource, keine Vorlage der Auslieferung, `EPOS.iOS/` nicht.

**Gate auf `81daa0b8`** (Zweig `konzept-berichtvorlagen` mit W1, W2 und `origin/ios_migration_september`) grün:
Windows-Schale 0 Fehler; Ressourcen-Designer unverändert; voller Lauf 0 Fehler — EPOS.Kern.Tests 7.649 (1 übersprungen),
EPOS.UI.Tests 6.450, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 (1 übersprungen); ChartProben
174 Bilder, 0 Verstöße; SQL-Prüfer 1.940 Texte, 0 Fundstellen. Die Wache kam danach auf den Zweig; ihre Abnahme steht
oben.

**Abschluss (Orchestrierung, 26.09.2026):** Merge von `origin/ios_migration_september` (ed35221b) konfliktfrei als f7f584b5; Gate auf f7f584b5: Kern-Filter und Windows-Schale 0 Fehler, Designer wiederholbar (+0), voller Lauf 15.089 bestanden / 0 Fehler / 2 übersprungen (KiKern 549, SpeicherEngine 386, SpeicherPlanung 27, EPOS.UI 6.454, EPOS.Kern 7.673), ChartProben 174 Bilder / 0 Verstöße, SQL-Prüfer 1.946 Texte / 0 Fundstellen; Papiere als #528 committet, Push und CI-Nachweis stehen in der Statuszeile #528.

## 7 Laufzeit

Median dreier Läufe, gemessen von W1; „vorher“ ist der Stand `57c6c53b` bzw. der Weg, auf dem jeder Schreiber selbst
rechnet:

| Messung | Probe | Ausgabe | vorher | nachher |
|---|---|---|---|---|
| Messlatte (`BerichtVorlagenMesslatteTests.Laufzeit`, Proben ohne Sammler) | 1030 | Word | 628 ms | 622 ms |
| | 1030 | Excel | 301 ms | 251 ms |
| | Gruppe mit sieben Ständen | Word | 3.871 ms | 3.711 ms |
| | Gruppe mit sieben Ständen | Excel | 611 ms | 584 ms |
| Betriebsweg (`BerichtWertesatzTests.Laufzeit_des_Wertesatzes`: jeder Schreiber rechnet selbst gegen Wertesatz vorab und beide Schreiber) | 1030 | Word und Excel | 475 ms | 396 ms |
| | Gruppe mit sieben Ständen | Word und Excel | 2.922 ms | 2.377 ms |

Das Ziel des Konzepts (8.5: „heute plus höchstens 10 %“) ist gehalten. Der Rückfall ohne Sammler ist nicht langsamer als
vorher; der Betriebsweg ist um 17 % (1030) bzw. 19 % (Gruppe) schneller, weil der Kapitalwertverlauf einmal statt zweimal
— für Word und für Excel — gerechnet wird.

## 8 Offen

- **(a) Anwenderentscheid: Speichertemperaturbild der Projektbeschreibung.** Die Projektbeschreibung trägt keinen Bedarf;
  ihr Bild der Speichertemperaturen ist eine Beigabe, wenn die Stundenreihen ohnehin da sind. Vor BV-E3 hing es am Häkchen
  „Ergebnisse je Variante“; eine eigene Vorlage nur mit `{{kapitel.projekt}}` zeigt es jetzt nicht mehr, auch mit
  gesetztem Häkchen (ohne gepflegten Strom-Leistungspreis). Standardvorlage und bisheriger Weg zeigen es wie vorher.
  Lesarten: so lassen, oder `kapitel.projekt` trägt den Bedarf „Zeitreihen“ — dann erhebt jeder Lauf mit
  Projektbeschreibung die Stundenreihen, auch mit der Standardvorlage ohne „Ergebnisse je Variante“.
- **(b) Regel der Leitversion (Entscheid in BV-E4).** `Zahlungsgliederungen.Leitversion` wählt den Stand, dessen
  Differenz Differenzspalte, Brückenbild und „Was daraus im Lauf wird“ zeigen, nach einer zweiten Regel: Sie schließt die
  Referenz aus statt der Ergebnisse mit `IstStamm` und nimmt ohne Differenz den ersten Stand außer der Referenz statt des
  Stamms. Ob sie `BesteVariante.Waehle` folgt, entscheidet BV-E4.
- **(c) Stammfall von `wirtschaft.beste.*` (BV-E4, BV-E6).** Ohne Variante mit Differenz zeigt die Karte „Kapitalwert ggü.
  Stamm“ den Nettobarwert des Stamms; `wirtschaft.beste.kapitalwert_diff` träfe diesen Wert nicht — nach der Regel „eine
  Marke nennt nur einen Schlüssel, der genau den angezeigten Wert erzeugt“ (Konzept 9.5) ist die Marke dieses Falls offen.
  Den Anzeigenamen der besten Variante löst die Hülle auf; `wirtschaft.beste.anzeige` braucht ihn aus dem Kern.
- **(d) Bedarf der neuen `wirtschaft.*`-Schlüssel (BV-E4).** Jeder Schlüssel, der Verlauf, Brücke, Mehrjahrestafeln oder
  die Emissionsbilanz zeigt, trägt seinen Bedarf im Katalog — sonst erhebt der Sammler den Teil nicht, und der Schreiber
  holt ihn nach.
- **(e) Nachholen ohne Meldung.** Scheitert ein Teil im Sammler, rechnet der Schreiber ihn beim Lesen nach, mit dem
  Fehlerbild von vorher. `WirtschaftsBerichtswerte.Nachgeholt` lesen nur die Tests; die Laufmeldung nennt ein Nachholen
  nicht.
- **(f) Datenbefunde der Testdatenbank (W2).** Die gespeicherten Ergebnisse der Variante 1029 (Stamm 1026) tragen den
  Merker `IstStamm` = 1 — mit ihnen nimmt 1029 an der Wahl nicht als Variante teil; 1043 führt je Szenario zwei
  Ergebniszeilen, die Regel nimmt je Stand die erste.
- **(g) Aufräumkandidaten der Wache.** Die Überladung `KuehltraegerText(m)` ohne Namensquelle
  (`Bausteine/BausteineProjekt.cs`) und die Überladungen des Parameterblocks ohne Trägerpreisquelle
  (`ExcelFormelmappe.cs`) rufen nur noch Tests (`KaeltestromAbrechnungTests`; `RisikoModulTests`,
  `SzenarioParameterTests`, `WiederholperiodeTests`). Fallen sie, werden die zwei Einträge der Positivliste zu
  `Emissionsquelle.TraegerName` und `Traegerpreissatz.Lies` gestrichen.
- **(h) Aus BV-E1 und BV-E2 weiter offen** („Nach #512“, „Nach #520“ in der Statusdatei): die Anwenderproben unter
  Windows, die Tippprobe mit echtem Word, die Schärfung der Regel „Deckblatt aus Platzhaltern“, die Bezugszeile der
  Anhang-E-Überlagerung bei unlesbarer eigener Vorlage, die ungenutzte Logo-Prüfung des Kerns, die Zukunft der
  Beispielvorlage, der Wiki-Upload der Seite „Berichtsvorlagen“, „Original geändert – übernehmen?“, eine Bedienung der
  Vorgabe, das KI-Feld der Katalogsuche, der Abgleich der Kern-Vorprüfung für Vorlagen ohne Platzhalter und die
  Startrückfrage mit der gespeicherten Versionsauswahl.
- **(i) Setup- und iOS-Lauf:** BV-E3 berührt weder die Auslieferung noch `EPOS.iOS/` und gibt keinen eigenen Anlass; die
  Läufe aus BV-E1 und BV-E2 bleiben nach Rückfrage offen.

## 9 Dateien

Sechs Commits (Tafel im Kopf): die vier der Agenten W1 und W2, belegt mit `git log --no-merges --stat 9be9d745..81daa0b8`
ohne die Commits von `origin/ios_migration_september` — zusammen (`git diff --stat 57c6c53b 7cfd8cec`) 21 Dateien,
+2.557/−157 Zeilen —, und die zwei der Wache (`git diff --stat 81daa0b8 45ede76c`: 2 Dateien, +643 Zeilen); insgesamt
23 Dateien, +3.200/−157 Zeilen:

| Bereich | Dateien |
|---|---|
| Kern, Wertesatz und Bedarf | neu `EPOS.Kern/Allgemein/Bericht/WirtschaftsBerichtswerte.cs` (mit `Traegerpreissatz`), `EPOS.Kern/Allgemein/Bericht/Berichtsbedarf.cs`; geändert `BerichtsDaten.cs` (`Wirtschaft`), `BerichtsDatenSammler.cs` (Überladung mit Bedarf, `Ermittle`, Leistungspreis-Regel, `ZeitreihenErhoben`), `Vorlagen/Berichtskapitel.cs` (`Bedarf`), `Vorlagen/Vorlagenfeldkatalog.cs` (Bedarf an `kapitel.*` und `bericht.inhalt`), `Vorlagen/Berichtslauf.cs` (`Startbefund.Bedarf`), `EPOS.Kern/Controller/BerichtCtrl.cs` (`PruefeVorStart`) (W1) |
| Kern, Schreiber | `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs`, `Bausteine/BausteineProjekt.cs`, `AnhangECheckliste.cs`, `ExcelBerichtGenerator.cs`, `ExcelFormelmappe.cs`, `WordBerichtGenerator.cs` (Kommentar) (W1) |
| Kern, Wirtschaftlichkeit | neu `EPOS.Kern/Allgemein/Wirtschaftlichkeit/BesteVariante.cs` (W2) |
| Hülle | `EPOS.UI.Daten/Bericht/BerichtSeiteGaben.cs` (W1), `EPOS.UI.Daten/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` (W2) |
| Tests Kern | neu `EPOS.Kern.Tests/BerichtWertesatzTests.cs` (W1), `EPOS.Kern.Tests/BesteVarianteTests.cs` mit `BesteVarianteHuellenTests` (W2); geändert `BerichtsvorlagenHuelleTests.cs`, `VorlagenfeldkatalogWacheTests.cs` (W1) |
| Wache | neu `EPOS.Kern.Tests/BerichtSchreiberOhneDatenbankWacheTests.cs` (`eefb308c`); `EPOS.Kern/CLAUDE.md`, Regelzeile als eigener Absatz am Ende des Abschnitts „Bericht“ (`45ede76c`) |
| Papiere (dieser Auftrag) | dieses Protokoll, Konzept Rev. 5, `Dokumentation/aktuell/Status_iOS_Migration.md`, `Dokumentation/LIESMICH.md` |
