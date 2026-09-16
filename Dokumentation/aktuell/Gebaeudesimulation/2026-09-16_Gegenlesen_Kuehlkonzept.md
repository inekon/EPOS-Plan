# Gegenlesen des Kühlkonzepts (16.09.2026)

**Papier:** [`Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md`](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md)
— gegengelesen als **Rev. 1** (1 409 Zeilen), nach Einarbeitung **Rev. 2** (1 851 Zeilen).

Das Papier ist am selben Tag entstanden, an dem es gegengelesen wurde: Es ist die Ausführung von
**Entscheid E12** („Kühlung als vierter Kanal, gedeckt durch Kälteerzeuger") und war noch nicht
im Feld. Drei Leser haben es unabhängig voneinander geprüft, jeder aus einem eigenen Blickwinkel:

| Leser | Blickwinkel | Fragte |
|---|---|---|
| 1 | **Bestand** | Stimmt jede Datei-, Zeilen-, Klassen- und Methodenangabe gegen den Arbeitsbaum? Tut die genannte Stelle wirklich das, was das Papier ihr zuschreibt? |
| 2 | **Konsistenz** | Widerspricht das Papier einem Schwesterpapier, einer Hausregel oder sich selbst? Stimmen Zählungen, Summen, Nummernbereiche und Verweise? |
| 3 | **Entscheid E15** | Trägt das Papier den Anwenderentscheid vom 16.09.2026 — Auswahl der kühlfähigen Wärmepumpen im Katalog und ihre Konfiguration? |

**Ergebnis: 26 Befunde** — **7 hoch**, **9 mittel**, **9 niedrig**, dazu **1 Bestätigung ohne
Änderung**. Alle sind eingearbeitet. Dazu kommen die sechs Ergänzungen aus E15 (Abschnitt „Was
E15 hinzufügt") und eine Messung, die der Befund W nicht geführt hatte (Abschnitt „Die Zählung").

**Der schwerste Einzelbefund ist B1.** Das Papier hatte seine zwei gefährlichsten Stellen —
`Summe()` und `NetzverlusteVerteilen` — der **falschen Klasse** zugeschrieben. Sie gehören zu
`Kanalsatz`, der produktiven Mehrkanalklasse, nicht zu `Waermekanaele`, der zweikanaligen
Altklasse ohne produktiven Aufrufer. Die Zeilenangaben waren richtig, der Klassenname war es
nicht — und ein Umsetzer, der nach `Waermekanaele.Summe()` sucht, findet eine Methode, die er
nicht anfassen muss, und lässt die stehen, die den Schaden anrichtet.

---

## Die Befunde

### Blickwinkel 1 — Bestand (B1–B14)

| Nr. | Schwere | Stelle | Befund in einem Satz | Erledigung |
|---|---|---|---|---|
| **B1** | **hoch** | 0.2, 4.1, 4.2, 4.3 #3/#4, 10.2, 13, 15 | `Summe()` (`SimulationKanaele.cs:645-656`) und `NetzverlusteVerteilen` (`:686-716`) gehören zu **`Kanalsatz`** (`:598-996`), nicht zu `Waermekanaele` (`:28-409`, zweikanalige Altklasse ohne produktiven Aufrufer, ihr eigenes `Summe()` bei `:48-54`). | Durchgehend auf `Kanalsatz` umgestellt; 4.1 bekommt einen eigenen Block „Welches Kanalfeld gemeint ist", der beide Klassen nebeneinanderstellt; K1 entsprechend umformuliert; die Konstanten heißen **`KANAELE_WAERME`/`KANAELE_KAELTE`** statt `WAERMEKANAELE`/`KAELTEKANAELE`, weil `Waermekanaele` als Klassenname vergeben ist (Begründung in 4.2, Festlegung in K16). |
| **B2** | **hoch** | 2.1 F-K6, 4.2, 4.4 | Die Laufprüfung „kein Wärmeerzeuger schreibt in `Deckung_Kuehlung`" war auf `SimulationKanaele.cs:339-360` gelegt — das ist `Waermekanaele.Selbsttest()`, ein **statischer Invariantentest** im Debug-Build ohne automatischen Aufrufer (`:172-199`); er kann über Laufergebnisse nichts aussagen. | 4.4 in vier Vorrichtungen gegliedert und die zwei Arten getrennt: **statische** Zusicherungen (Kanallisten disjunkt und vollständig, Ziel ↔ Senke der Kälteseite) in den Selbsttest, die **Laufprüfung** in eine **Kälteprobe je Stunde** nach dem Muster von `SimulationWaermebedarf.Energieprobe` (`:390` gerufen, `:458` gerechnet). Nachweis zu F-K6 angepasst. |
| **B3** | **hoch** | 2.1 F-K7, 5.1, 7.3, 7.5, 8.2, 10.2 | Der **Kühl-Vorlauf fehlte vollständig**: `Tab_Kenndaten_Kuehlung` ist über `Vorlauf` × `Temperatur` × `Last` aufgespannt (`sql/schema/001_grundschema.sql:1321-1330`), und die Heizseite wählt die Kennlinie über den projektseitigen Vorlauf (`SimulationWaermepumpe.cs:580`, `:584`, `:634`, Extrapolationshinweis `:1869`). Ohne Gegenstück wählt die Kälterechnung die Kennlinie zufällig. | `KU-S3` um **`Kuehl_Vorlauf`** (°C Kaltwasser-Vorlauf, NULL = kleinster Stützwert) erweitert; neue Festlegung 2 in 5.1; Auswahlfeld aus den Stützstellen in 8.2; Extrapolationswarnung wie auf der Heizseite; EER-Stützstellenprobe je Vorlauf in 10.2; ER-Bild in 7.5 ergänzt. **`Kuehl_Ruecklauf` und Spreizung werden benannt abgelehnt** (5.1). Neue Frage **K21**. |
| **B4** | **hoch** | 4.1, 6.4, 10.2, 13 | `DeckungKanal` (`KennzahlenKatalog.cs:85-101`) trägt den Kühlkanal **nicht**: Sie summiert über eine namentlich verdrahtete Wärmeerzeugerliste (`:95-98`) und rechnet über `e.Waermebedarf_Gesamt` (`:100`) um — für den Kühlkanal ein falscher Nenner, der eine plausible Zahl liefert statt eines Fehlers. `BedarfKanal` (`:62-68`) zieht dagegen mit. | 6.4 unterscheidet jetzt beide Fälle ausdrücklich; die Kühlseite bekommt einen **eigenen Zweig** `DeckungKanalKaelte` mit **`Kaeltebedarf_Gesamt`**; die Bestandsmethode bleibt wörtlich. Rechenprobe „Deckungsgrad, zwei Nenner" in 10.2; Risikozeile in 13; K16 erweitert. |
| **B5** | **hoch** | 4.7, 10.5, 11.2, K17 | `KU-S4` ist **nicht vergleichsneutral**: Der Referenzlauf-Export liest die Ergebniszeilen mit `SELECT *` (`Ergebnisexport.cs:449`, `:452`, `:495`), sechs neue Spalten erscheinen damit sofort als Schlüssel in `aggregate.csv`. | 10.5 um eine Tabelle „Datei gegen Schlüssel" erweitert; der Nachweis der Schemastufe lautet jetzt „byte-gleich **in allen alten Schlüsseln**, die sechs neuen mit `--ohne` benannt ausgenommen" (`Vergleich.cs:47-59`, `:61-62`, `:74-79`, `:96-98`, `:225`, `:250`). 4.7 und die Einfriertabelle angepasst. **Abweichung vom Rohbefund:** Er verortete den `SELECT *` bei `ErgebnisCtrl.cs:187-193`; dort steht der **INSERT** der Energiebedarfszeile. Der `SELECT *` steht im Referenzlauf-Export und ist in `Vergleich.cs:47-59` begründet — so belegt. |
| **B6** | mittel | 0.5, 4.7, 10.5, K17 | „Kein Schalter dagegen" gilt nur für die **Datei** (`Vergleich.cs:183-190`, `Schwere = double.MaxValue`); für **Schlüssel** gibt es `--ohne`. Der Schluss „KU1 gehört zu G1 + G2" bleibt richtig, aber aus **einem** Grund. | An allen vier Stellen geschärft; Punkt 5 des Ergebniskapitels trennt beide Lagen und nennt den verbliebenen Grund (die neue Vektordatei). |
| **B7** | mittel | 4.1, 4.3 #22, 7.4, 15 | Schritt 52 war an vier Stellen mit drei verschiedenen Zeilenbereichen zitiert (`:2443-2465`, `:2446-2470`, `:2447-2465`). | Einheitlich **`SchemaKatalog.cs:2417-2475`** (Schritt samt Begründungskopf), Spaltenzeilen **`:2447-2469`**. |
| **B8** | mittel | 5.1, Festlegung 3 | „Einziger Leser `AbweichungsErmittler.cs:81`" ist falsch: `Tab_WP.Kuehlleistung` hat weitere Leser — `ParameterVerwendung.cs:488-489`, `WaermepumpenKatalogZeile.cs:22`/`:56` (Auslegung „Heizen/Kühlen"), `WPStammCtrl.cs:137-139`/`:221`/`:240` (Filterkennzeichen `KUEHLEN`), `WPModel.cs:21`/`:44`. | Festlegung 3 zählt die Leser jetzt auf und hält fest: **keiner rechnet** — mit ihnen findet der Anwender die Maschine, gerechnet wird mit der Kennlinie. |
| **B9** | mittel | 5.0 (neu), 8.2, 8.5, 10.3 | Zwei Kriterien waren nicht getrennt: **kühlfähig im Katalog** = `Kuehlleistung > 0` gegen **rechenbar kühlfähig** = `KenndatenKuehlungCtrl.HatKenndaten(ID_WP)` (`:132-138`). Der Sperrgrund des Kühlbetriebs gehört an das zweite, nicht an das erste. | Neuer Abschnitt **5.0.1** mit beiden Begriffen und ihren Bestandsstellen (`WaermepumpeStammDialog.razor:154-159`, `WaermepumpenKatalogDialog.razor:26-29`); Sperrgrund in 8.2 an `HatKenndaten`; Warnung „Nennkühlleistung ohne Kühlkennlinie" in 8.5; Datenbankfall in 10.3; Risikozeile in 13. |
| **B10** | mittel | 4.3 | Vier Stellen fehlten in der Stellenliste, und alle vier fallen auf **Heizung** zurück, ohne zu melden: `Warnkriterien.KanalAnzeige` (`:882-890`), `Warnkriterien.Set_BedientKanal` (`:1001-1010`), `SchemaModell.PufferBedient` (`:225-236`) und `SchemaModell.DirektsenkeBedient` (`:245-260`, „Beides" liefert für jeden unbehandelten Kanal `true`), dazu `PufferSpCtrl.KlassenSet` (`:685-696`, drei `bool`-Felder). | Als Zeilen **#24–#27** aufgenommen, je mit Stufe; dazu der Absatz „Die Lehre aus #24 bis #27: der stille Rückfall auf Heizung" und eine Risikozeile in 13. |
| **B11** | niedrig | 4.3, 4.5, 4.6, 15 | Zeilenbereiche ungenau: Knappheitsparser-Warnblock, `AusText`, `Name`, `SimulationPufferspeicher`. | Am Quelltext nachgemessen: Warnblock **`:544-554`**, `AusText` **`:454-464`**, `Name` **`:467-475`**, `SimulationPufferspeicher.cs` einheitlich **`:19-47`**. **Abweichung vom Rohbefund:** Er nannte `:453-464` bzw. `:465-476`; die Methodenkörper beginnen und enden eine Zeile später bzw. früher. |
| **B12** | niedrig | 4.7 | Die Kanaldateien `waermebedarf_brauchwasser.csv` und `waermebedarf_prozess.csv` werden **unbedingt** geschrieben (`Ergebnisexport.cs:57-63`, Block „immer vorhanden"); die bedingte Kühldatei ist damit eine Abweichung vom Bestandsmuster. | In 4.7 als **gewollte Abweichung benannt** und begründet (zwölf Reihen voller Nullen erspart); K17 entsprechend. |
| **B13** | niedrig | 10.5 | Die Mermaid-Bilder tragen; kosmetisch waren zwei Selbstübergänge im Einfrierbild zusammenziehbar. | Zusammengezogen (`M2 bis M4 und KU-S1 bis KU-S4` in einem Übergang). |
| **B14** | — | Verweise, Indexzeile | Verweise und Indexzeile in Ordnung — keine Änderung nötig. | Bestätigt; die neuen Verweise (Protokoll, Katalogfilter-Konzept, N1.20) sind ergänzt. |

### Blickwinkel 2 — Konsistenz (K1–K12)

| Nr. | Schwere | Stelle | Befund in einem Satz | Erledigung |
|---|---|---|---|---|
| **K1** | **hoch** | 10.4 | Eine „**fünfte** Einfrierregel" gibt es nicht: Der [Systementwurf](../Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 8.3 (`:841-848`) hält ausdrücklich fest, dass Regeln **über ihren Gegenstand** benannt werden, nicht über eine Ordnungszahl — „gesäte Gebäudedaten" (mit GB, `:148`), „gesäte Klimareihen", „gesäte Zonendaten" (mit G6d). | 10.4 umgeschrieben: „**gesäte Kältedaten**" ohne Ordnungszahl, mit der Begründung aus 8.3 und dem vollständigen Gegenstand der Regel. |
| **K2** | **hoch** | 4.7, 6.4 | Zweite Wahrheit: Die [Softwarearchitektur](../Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 4.4 (`:1791-1793`) legt mit G1 `gebaeude_<n>_kuehlbedarf.csv` **je Gebäude** fest (kWh, `ID_ProjektGebaeude`) samt den Skalaren „Kühlenergie" und „Stunden mit Kühlbedarf"; das Kühlkonzept führte daneben `waermebedarf_kuehlung.csv` **je Projekt** plus „Jahreskälte", ohne das Verhältnis zu klären. | In 4.7 zwei neue Zeilen („Verhältnis zur Gebäudereihe", „Wer ‚Jahreskälte' führt") und in 6.4 ein Absatz: **Gebäudereihe = Rohbedarf je Gebäude** (G1/G2), **Kanalreihe = Summe über Gebäude + externe Ganglinien**; „Jahreskälte" führt der **Kanal**, die Gebäudeskalare bleiben unverändert. |
| **K3** | mittel | 6.4 | Der Bestandseintrag `gebaeude.kuehlbedarf` heißt „Kühlbedarf (**informativ**)" (Softwarearchitektur `:1738`); mit E12 trifft der Zusatz nicht mehr zu. | In 6.4 als Umbenennung festgelegt: Zusatz entfällt in **beiden** `.resx`, danach `Werkzeuge/ResourceDesigner`; Rechenweg, Schlüssel, Einheit und Aggregation unverändert; der Posten steht in KU1 (11.1). |
| **K4** | mittel | 9.1 | Der Import sollte `Kuehlung_Aktiv = 1` setzen — mit der Begründung, ein Autorensystem schreibe `DesignCoolT` nur für gekühlte Zonen. Das [Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) hält für genau dieses Feld fest, es „fehlt oft" und es gebe „keine Zahlenvorgabe im Import" (`:417`; IFC-Gegenstück `:822`). | 9.1 umgeschrieben: Der Import setzt `Kuehlung_Aktiv` **nicht**; der Wert ist ein **Vorschlag mit Herkunftsmarke**, und der **Importbericht nennt die Zonen mit Kühlsollwert**. Risikozeile in 13. |
| **K5** | mittel | Kopf, Kapitel 12 | Der Kopf nannte „Fragen K1–K18", die Tabelle führte bis K19 samt K8a–K8c und K18a. | Kopf auf **K1–K23**; Kapitel 12 bekommt einen Vorspann, der den Bereich und die Herkunft (K20–K23 aus E15 und dem Gegenlesen) nennt; KU0 in 11.1 entsprechend. |
| **K6** | mittel | Indexzeile, ganzes Papier | Die Indexzeile in [`Dokumentation/LIESMICH.md`](../../LIESMICH.md) nennt bereits **E15** und die Katalogauswahl — das Papier führte beides nicht. | E15 eingearbeitet (Abschnitt „Was E15 hinzufügt"); die Indexzeile trifft jetzt zu. |
| **K7** | niedrig | Schwesterpapier-Tabelle | „Schemaschritt 77 (1.6)" ist eine Zahl, die wandert: `SchemaStand.Zielversion` steht auf **78** (`SchemaStand.cs:106`). | In der Tabelle auf „**der Gebäude-Schemaschritt (1.6)**" geändert — **nur hier**. Meldung ans Umsetzungskonzept siehe unten. |
| **K8** | niedrig | 4.3 | Die Kopfzeile nannte die Herkunft der Liste nicht genau, und die W-Nummern fehlten. | Kopfzeile: „Stellenliste aus Befund W 1.2, neu durchgezählt und um vier ergänzt; die Spalte ‚Beleg' nennt die W-Nummer und die Fundstelle." Jede Zeile trägt jetzt ihre W-Nummer, die vier neuen sind als **neu** gekennzeichnet. |
| **K9** | niedrig | 10.5 | Dem Einfrierbild fehlte der Anschluss an G6d. | `Basis_KU2 --> Basis_G6 : G6d — Zonenprojekt` ergänzt, danach `[*]`. |
| **K10** | niedrig | 11.1 | Der Vergleich „dieselbe Klasse wie G1 + G2 zusammen" stimmt nicht. | Am [Umsetzungskonzept](../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 4 nachgesehen: Dort steht **eine** Zeile „G1 + G2 — das Modell und seine Darstellung" mit **15–21 PT**. **Abweichung vom Rohbefund:** Er las „15–21 PT je Stufe" und schloss auf das Anderthalbfache; gegen die gemeinsame Zeile ist die Kühlsumme (47–74 PT) rund das **Dreifache**. So ist es geschrieben, mit der Begründung, dass die Kühlung Kanal, Erzeuger, Strom, Wirtschaftlichkeit, Oberfläche und Regressionsnetz zugleich berührt. |
| **K11** | niedrig | 11.1 | KU3 führte den Kältespeicher im Aufwand, obwohl **K7** ihn noch offen lässt. | KU3 jetzt **17–26 PT**, mit Kältespeicher (K7) **20–31 PT**; Summe **47–74 PT**, ohne Kältespeicher **44–69 PT**. KU2 steigt von 14–22 auf **16–26 PT** (Kühl-Vorlauf, eigener Deckungszweig, Katalogauswahl aus E15) — mit einem Absatz, der die Erhöhung begründet. |
| **K12** | niedrig | 10.1 | Ein Halbsatz („nennt nur ihre Nummern, keine Werte") stand gegen den Produktausweis, der zwei Beträge nennt. | Geschärft: Das Papier nennt **keine Normreferenzwerte**; die zwei Beträge des Produktausweises sind **eigene Messwerte** (der Abstand zum Band, von EPOS gemessen) und stehen nach **E10** im Wortlaut, an einer Stelle gepflegt und über einen Ressourcenschlüssel gezogen. |

---

## Was E15 hinzufügt

**Entscheid E15 (Anwender, 16.09.2026), im Wortlaut:**

> „Konzept: es gibt Wärmepumpen mit Kühlfunktion. Diese sind auch im Katalog. Es sollte eine
> Auswahl mit Wärmepumpen mit Kühlfunktion geben und entsprechender Konfiguration, so dass die
> Anforderungen an einen Erzeuger erfüllt sind und insbes. die Gebäudesimulation nach VDI 6007
> dann möglich wird."

Sechs Ergänzungen sind daraus geworden:

| Nr. | Was | Wo |
|---|---|---|
| **1** | Neuer Abschnitt **5.0 „Wärmepumpen mit Kühlfunktion: Auswahl und Konfiguration"** vor 5.1, in sechs Unterabschnitten: die zwei Kühlbegriffe (B9), die Messung (unten), die Katalogauswahl samt der Stelle, die sie heute verhindert, die drei Übernahmewege, die vier Konfigurationsfelder, die Zusage und ihre Vorbedingung | Kühlkonzept 5.0 |
| **2** | `KU-S3` um **`Kuehl_Vorlauf`** (°C) und **`Kuehl_Hilfsstromanteil`** in `Tab_WP` **und** `Tab_WP_STAMM` erweitert — vier Spalten je Tabelle, acht `SchemaSpalte`-Einträge | Kühlkonzept 7.3, 7.5 |
| **3** | **8.2 umgeschrieben** in drei Schritte: Katalogauswahl mit Filter, Stammdialog bleibt Anzeige, Projektdialog mit Kühl-Vorlauf als Auswahl über die Stützstellen, Sperrgrund an `HatKenndaten`, Warnung „Nennkühlleistung ohne Kühlkennlinie" | Kühlkonzept 8.2, 8.5 |
| **4** | **8.6** bekommt den Schritt „Katalogauswahl Wärmepumpe mit Kühlfunktion" **vor** dem Erzeugerdialog; **10.3** den Datenbankfall „Katalogübernahme kopiert die Kühlkennlinie vollständig" samt zwei weiteren Fällen; **10.2** die EER-Stützstellenprobe je Vorlauf | Kühlkonzept 8.6, 10.2, 10.3 |
| **5** | Vier neue Fragen: **K20** (Filter — Ausnahme von der JaNein-Regel oder eigene Filterart? Empfehlung: eigene Filterart), **K21** (Kühl-Vorlauf — Auswahl oder freie Eingabe mit Interpolation? Empfehlung: Auswahl), **K22** (führt die `COP`-Spalte der Kühltabelle wirklich den EER? vor KU2 an den Herstellerdaten prüfen), **K23** (Hilfsstrom je Anlage oder pauschal? Empfehlung: je Anlage); Kopf auf **K1–K23** | Kühlkonzept 12.1 |
| **6** | Die **Zählung** der kühlfähigen Katalog-Wärmepumpen, gemessen statt geschätzt | Befund W, Nachtrag; Kühlkonzept 5.0.2 |

**Was am Bestand für E15 bereits fertig ist.** Die Übernahme eines Katalogsatzes ins Projekt
bringt die Kühlkennlinie auf **allen drei Wegen** mit — Katalogsatz (`WPCtrl.CopyFromStamm`,
Kühlblock `:313-331`, Nachzug fehlender Kennlinien `:395`), Gewerkübernahme
(`KomponentenUebernahmeCtrl.cs:118`) und Projektduplikat (`ProjektDuplizierenCtrl.cs:155`).
**Daran ist nichts zu bauen**, wohl aber ein Datenbankfall, der es festhält (10.3).

**Was E15 im Weg steht.** Die Katalogspalte „Kühlen" ist als Ja/Nein-Kennzeichen definiert
(`Katalogfilterprofil.cs:340`, `:521`) und damit vom Filtern ausgeschlossen:
`Filterbar = filterbar && art != Katalogspaltenart.JaNein` (`:79`). Das ist **Absicht** — der
[Katalogfilter-Konzept](../Konzept_Katalogfilter_EPOS-Plan.md) 5.6.2 begründet es damit, dass
„enthält ja" für zwei Werte ein Bedienelement ohne Gewinn sei und die Sortierung genüge. Für die
Kühlung trägt die Begründung nicht mehr, weil die Kühlfähigkeit ab KU2 eine **Vorbedingung** ist
und keine Zusatzangabe. Der Weg — benannte Ausnahme oder eigene Filterart — ist **K20**; das
Papier empfiehlt die eigene Filterart, damit die Regel eine Regel bleibt.

**Die Zusage und ihre Grenze.** Eine Projektkonfiguration mit reversibler Wärmepumpe rechnet die
Simulation mit Kühlung vollständig — **frühestens nach G1 + G2 + KU1 + KU2**. Das steht so in
5.0.6 und in [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
**N1.20**, dem neuen Abschnitt am Ende des Nachtrags 1.

---

## Die Zählung (Messung vom 16.09.2026)

Der Auftrag verlangte, die Zahl der kühlfähigen Katalog-Wärmepumpen zu **messen** statt zu
schätzen. Das vorhandene Prüfwerkzeug `DbProbe` unter
`C:\Users\Dirk\AppData\Local\Temp\epos-spike\` kann das nicht: Sein Einstieg ist
`static int Main()` **ohne Argumente**, es führt einen festen Satz von Abfragen für die
Prototyp-Vergleiche aus und nimmt keine freie SQL-Abfrage entgegen. Deshalb ist neben ihm ein
zweites, ebenso **nur lesendes** Werkzeug entstanden
(`C:\Users\Dirk\AppData\Local\Temp\epos-spike\KuehlZaehlung\`, dieselbe Bauart, dieselbe
`Microsoft.Data.Sqlite`-Fassung, Verbindungszeichenfolge mit **`Mode=ReadOnly`**). Es liegt wie
`DbProbe` **außerhalb des Repositoriums**. Die Testdatenbank ist unverändert.

| Größe | Zahl |
|---|---|
| Katalogsätze `Tab_WP_STAMM` | **51** |
| davon kühlfähig im Katalog (`Kuehlleistung > 0`) | **15** |
| Sätze mit Kühlkennlinie (`COUNT(DISTINCT ID_WP)` in `Tab_Kenndaten_Kuehlung_STAMM`) | **7** |
| davon rechenbar kühlfähig (beides) | **6** |
| Nennkühlleistung ohne Kennlinie | **9** |
| Kennlinie ohne Nennkühlleistung | **1** |
| Kühlkennlinienzeilen / Vorlauf-Stützstellen / Laststufen | **174 / 2 (7 °C und 18 °C) / 10** |
| Projektseite `Tab_WP` / mit Nennkühlleistung / mit Kühlkennlinie | **29 / 7 / 0** |

**Drei Aussagen stecken darin**, und alle drei sind ins Papier eingearbeitet:

1. **Ein Filter lohnt sich** (15 von 51) — das ist die sachliche Begründung für K20.
2. **Die Lücke zwischen beiden Kühlbegriffen ist groß** (nur 6 von 15 tragen eine Kennlinie) —
   das ist die sachliche Begründung dafür, den Sperrgrund an `HatKenndaten` zu hängen (B9).
3. **Die Projektseite trägt heute keine einzige Kühlkennlinie** — das Referenzprojekt mit
   Kältedeckung (10.4) entsteht also durch **Saat**, nicht durch Auswahl, und gehört damit unter
   die Einfrierregel „gesäte Kältedaten" (K1).

Die Zahlen stehen als **Nachtrag (16.09.2026)** am Ende von
[Befund W](2026-09-16_Befund_W_Kuehlung_Bestand.md) und werden im Kühlkonzept 5.0.2 zitiert.

---

## Meldung an das Umsetzungskonzept (aus K7)

**Nicht geändert, nur gemeldet** — der Auftrag erlaubte am Umsetzungskonzept keine Änderung:

Das [Umsetzungskonzept](../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) führt den
Gebäude-Schemaschritt in 1.6 und in der Aufwandstabelle (Zeile „M3 — Schema") als **„Schritt 77
(+78 verschmolzen, U5)"**. Im Arbeitsbaum steht `SchemaStand.Zielversion` auf **78**
(`EPOS.Kern/Allgemein/Update/SchemaStand.cs:106`). Eine Schrittnummer, die vor ihrer Umsetzung
in einem Papier steht, wandert mit jedem Schritt, der dazwischen beauftragt wird — dieselbe
Regel, die das Kühlkonzept in Kapitel 7 für `KU-S1 … KU-S4` schon anwendet („Die Nummer vergibt
der Schritt bei seiner Beauftragung").

**Vorschlag:** Im Umsetzungskonzept 1.6 und in der Aufwandstabelle die feste Zahl durch die
Benennung über den Gegenstand ersetzen („der Gebäude-Schemaschritt"), wie es das Kühlkonzept
jetzt in seiner Schwesterpapier-Tabelle tut. Das ist eine kleine Textänderung und gehört in den
nächsten Auftrag, der das Umsetzungskonzept ohnehin anfasst.

---

## Was nicht geändert wurde

- **Die Kapitelnummern** des Kühlkonzepts. Neu sind allein **5.0** (mit 5.0.1–5.0.6) und die
  Fragen **K20–K23** sowie die Anforderung **F-K17**; nichts Bestehendes ist umnummeriert.
- **Die Abgrenzung (Kapitel 14).** E15 betrifft den Weg des Anwenders zum Kälteerzeuger, nicht
  den Umfang des Vorhabens: Feuchte und Entfeuchtung, Bauteilaktivierung als Funktion,
  Kältemittelemissionen und die Kühllast nach VDI 2078 als Nachweis bleiben ausgeschlossen.
- **Normzahlen.** Es ist keine Zahl aus VDI 6007 und nichts aus VDI 6020:2022 hinzugekommen
  (B-K1, E5, E6).
- **Die Testdatenbank.** Die Messung lief `Mode=ReadOnly`; es ist nichts geschrieben worden.

---

## Geänderte Dateien

| Datei | Was |
|---|---|
| [`Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md`](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) | Rev. 2 — 26 Befunde und E15 eingearbeitet; 1 409 → 1 851 Zeilen |
| [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) | neuer Abschnitt **N1.20** am Ende des Nachtrags 1 (Entscheid E15) |
| [`Status_Gebaeudesimulation_VDI6007.md`](../Status_Gebaeudesimulation_VDI6007.md) | Abschnitt 1: neue Zeile **E15**; Abschnitt 3: Kühlkonzept auf **Rev. 2** |
| [`2026-09-16_Befund_W_Kuehlung_Bestand.md`](2026-09-16_Befund_W_Kuehlung_Bestand.md) | **Nachtrag (16.09.2026)** mit der Zählung |
| dieses Protokoll | neu |

Die Indexzeile für dieses Protokoll in [`Dokumentation/LIESMICH.md`](../../LIESMICH.md) setzt der
Orchestrator.
