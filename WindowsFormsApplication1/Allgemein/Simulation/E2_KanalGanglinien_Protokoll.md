# Paket E2 — Kanal-Ganglinien in der Detailansicht: Umsetzungsprotokoll

**Nachtragspaket zum Konzept**
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md) —
Kapitel **4.4** („Ergebnis je Kanal … der `ZeitreihenExtraktor` liefert die Kanalganglinien") und
**4.1** (Kanalsatz). Stand: 28.08.2026 · Branch `Pufferspeicher` (HEAD `85d1f95`) · Anlass:
Nutzerauftrag vom 28.08.2026.

> **Der Auftrag im Wortlaut.** „Unter Dialog Simulation → Detaillierte Simulation → Energiebedarf:
> Der Energiebedarf Gesamt wird angezeigt. Der Energiebedarf der jeweiligen Bedarfe (Heizung,
> Brauchwasser, Prozesswärme, …) soll auch ausgewählt und angezeigt werden. Diese Auswahl und
> Anzeige sollte auch bei Bereich Ergebnis möglich sein — und es sollte angezeigt werden, durch
> welchen Erzeuger die Deckung des Bedarfs jeweils erfolgt."

Build `WP-Plan.sln` + `Referenzlauf.csproj` x64 Debug: **0 Fehler, 0 Warnungen**.

---

## 1. Umfang

Paket **E1** hat die Kanalgrößen als **Jahressummen** persistiert und angezeigt. E2 löst dieselben
Größen nach **Stunden** auf und macht sie in den beiden Diagrammen sichtbar, nach denen der
Anwender gefragt hat:

| Seite | vorher | nachher |
|---|---|---|
| **Energiebedarf**, „Wärmelast Jahresganglinie" | EINE Kurve (Gesamtbedarf, normiert) | Schalterleiste **Gesamt · Heizung · Brauchwasser · Prozesswärme**, vier Kurven im selben Bild |
| **Ergebnis**, „Wärmeproduktion Jahresganglinie" | Erzeugerserien = **Produktion** | Auswahlliste **Bedarfsart**; bei Kanalwahl zeigen dieselben Serien die **Deckung dieses Kanals** |

E2 ist — wie E1 — **rechnerisch neutral**: Kein Stundenwert und kein Bilanzskalar ändert sich
(Nachweis Abschnitt 5.1: 332 von 332 CSV byte-gleich gegen den unveränderten HEAD).

---

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **`Kanalganglinie`** | Neue Transportklasse: `double[Kanal.ANZAHL][8760]` [kWh] mit `Buchen(kanal, stunde, menge)`, `Nullen`, `Uebernehmen`, `Jahressumme`, `AlsFloat` und der statischen `Deckung(kanal, params Kanalganglinie[])` (die Stundenfassung von `SimulationRunner.Summiere`) | `SimulationKanaele.cs` (hinter `Kanalsatz`) |
| **Buchung Direktdeckung** | `Kanalabzug.Abziehen(…)` bekommt die Überladung `(…, Kanalganglinie, int stunde)`; `Aufschluesseln` bucht **dieselbe Variable `abgezogen`** aus **demselben Schleifendurchlauf** zusätzlich in die Ganglinie. Die 4-Parameter-Fassungen reichen `(null, −1)` durch und sind Anweisung für Anweisung die bisherigen | `Kaskadenschleife.cs` (`Kanalabzug`) |
| **Buchung je Modul** | WP: `Direktdeckung_KanalStuendlich.Buchen(k, stunde, _deckungIteration[k])` in derselben Schleife wie `Direktdeckung_Kanal[k] +=`; Kessel/Solar/BHKW: die vorhandenen `Kanalabzug.Abziehen`-Aufrufe geben Ganglinie und Stunde mit; WP-Heizstab über die neue `SenkeAbziehen`-Überladung | `SimulationWaermepumpe.cs:1107/1515`, `SimulationSPK.cs:928`, `SimulationSolarthermie.cs:489`, `SimulationBHKW.cs:1187` |
| **Buchung Speicherentladung** | `Kaskadenschleife._entladungKanalStuendlich[art]` wird in `Anteil_Entladen` aus derselben Größe `teil` gebucht wie `_entladungJeArtKanal[i][kanal]`; die laufende Stunde steht in `_stundeAktuell` (am Kopf der Stundenschleife gesetzt — dieselbe Voraussetzung, unter der `_entladungJeArtStunde` seit Befund N4 rechnet). Am Laufende geht die Ganglinie **kopiert** (B0-2) an `Modul.Speicherentladung_KanalStuendlich` — neben dem bestehenden `KanalzeileUebergeben` | `Kaskadenschleife.cs:437ff / 952ff` |
| **Buchung am Speicher** | `SimulationPufferspeicher.Entladung_KanalStuendlich` in derselben Zeile wie `Entladung_Kanal[kanal] += umsatz` | `SimulationPufferspeicher.cs:611` |
| **Zugriffswege** | `SimulationControl.DeckungKanalStuendlich(art, kanal)` (Direktdeckung + zugerechnete Speicherentladung), `HeizstabKanalStuendlich(kanal)`, statisch `BedarfKanalStuendlich(bedarf, kanal)` (Kopie aus `KanaeleDrei()`) | `SimulationControl.cs` |
| **Debug-Probe** | `SimulationControl.KanalganglinienProbe()` (`#if DEBUG`, Muster `Kanalsatz.Selbsttest`): prüft für **jede** der neun Modul-Größen und **jeden** Speicher die Zusage „Jahressumme der Ganglinie == Bestands-Jahressumme des Kanals" und „Σ Kanäle == Bestandsskalar", Maßstab `Kanalsatz.ErhaltungOk` | `SimulationControl.cs` |
| **Bedarfsseite** | `InitBedarfKanalauswahl` (Schalterleiste, drei Chartserien), `BedarfKanalserienFuellen` (Normierung, Sortierung, Präsenzregel), `BedarfSerienSchalten`; angehängt an `checkBox_Sortiert_CheckedChanged` und an den Lauf. CSV-Export der Seite bekommt je angehaktem Kanal eine kWh-Spalte | `Views/Simulation/Form_Simulation_Detail.cs` |
| **Ergebnisseite** | `InitBedarfsartAuswahl`, `AktualisiereBedarfsartAuswahl`, `VektorenSetzen`, `Diagrammtitel`; die Auswahl trägt ihren Steuerwert selbst (`SchluesselEintrag`, Paket Q1). CSV-Spaltenköpfe nennen die gewählte Bedarfsart | `Views/Simulation/NavigatorWaerme.cs` |
| **Bericht** | `ZeitreihenSatz.BEDARF_PRAEFIX`/`DECKUNG_PRAEFIX`/`KANAL_SCHLUESSEL` + `BedarfSchluessel`/`DeckungSchluessel`; `ZeitreihenExtraktor.Kanalreihen` nimmt je Kanal MIT Bedarf die Bedarfsreihe und je gerechnetem Erzeuger seine Deckungsreihe auf | `Allgemein/Bericht/BerichtsDaten.cs`, `ZeitreihenExtraktor.cs` |

**Neue Ressourcen (2, de + en + Designer):** `SIM_LABEL_BEDARFSART` („Bedarfsart:" /
„Demand type:"), `CHART_TITEL_DECKUNG_JE_BEDARFSART` („Deckung {0} — Jahresganglinie" /
„{0} coverage — annual load profile"). Die Kanalnamen selbst nutzen die `KANAL_*_ANZEIGE` aus
Paket K1, „Gesamt" den Bestandsschlüssel `CHART_LEGENDE_GESAMT` — kein vierter Katalogeintrag.
Bestand danach **2624** Schlüssel, DE/EN deckungsgleich.

### 2.1 Abweichung von der Paket-Whitelist — begründet

Der Auftrag verortete das Ergebnis-Diagramm in `Form_Simulation_Detail.cs`. **Dort liegt es
nicht:** Der Reiter „Ergebnis" ist im Designer leer und wird zur Laufzeit vom
`TabNavigationManager` bespielt; das Diagramm „Wärmeproduktion Jahresganglinie" mit den
Serien-Checkboxen Gesamt/Wärmepumpe/Heizstab/Heizkessel/Solarthermie/BHKW/Speicherfüllstand,
„Wärmebedarf einblenden", „sortiert" und der Speicherauswahl ist
**`Views/Simulation/NavigatorWaerme.cs`**. Auftragspunkt C ist ohne diese Datei nicht umsetzbar;
sie ist deshalb mitgeändert. Kein weiterer View ist angefasst.

---

## 3. Die Invariante — und was sie NICHT ist

> **Die Jahressumme einer Kanalganglinie ist die Kanal-Jahressumme aus Paket E1.**

Sie gilt, weil beide Größen aus **einer** Buchung entstehen: dieselbe Variable, derselbe
Schleifendurchlauf, kein zweiter Rechenweg und keine zweite Verteilregel. Der verbleibende Rest ist
allein die **Assoziativität der double-Addition** (die Jahressumme läuft in EINEN Akkumulator, die
Ganglinie in 8760). Gemessen über drei Projekte: **99 Zusagen, 0 Verstöße, größter Rest
1,7·10⁻⁸ kWh** (Abschnitt 5.2).

**Was die Invariante ausdrücklich nicht behauptet.** Der Auftrag erwartete „Σ der drei
Kanalvektoren einer Art == deren Bestands-Gesamtganglinie je Stunde". Diese Gleichung ist
**falsch**, und zwar aus fachlichen Gründen: Die Bestands-Ganglinien, die das Ergebnis-Diagramm
heute zeichnet (`WP_Waermeproduktion_stuendlich`, `Kesselleistung_stuendlich`,
`SimulationSolarthermie.Waermeproduktion`, `SimulationBHKW.waermeproduktion`), sind
**PRODUKTIONS**-Ganglinien. Produktion enthält die **Speicherladung** und **nicht** die
Speicherentladung; Deckung ist genau umgekehrt. Beide sind nur in einem Projekt **ohne Speicher**
gleich.

Gemessen an Projekt 1023 (ein Heizungspuffer):

| Größe | Wert [kWh] |
|---|---|
| WP-Produktion (Bestandsganglinie `WP_Waermeproduktion_stuendlich`) | 109 993,24 |
| WP-**Deckung** Σ Kanäle (Direkt 0,00 + zugerechnete Entladung 109 638,38) | 109 638,38 |
| Differenz = im Speicher verbliebene bzw. als Verlust getragene WP-Wärme | 354,86 |

Die einzige Bestandsganglinie, die tatsächlich eine **Deckungs**größe ist, ist der Heizstab
(`Heizstab_stuendlich` — er lädt keinen Speicher). Für ihn gilt die Summenzusage über die Kanäle
**je Stunde und über das Jahr** und ist gemessen:

| Projekt | Σ Kanalganglinien [kWh] | `Heizstab_stuendlich` [kWh] |
|---|---|---|
| 1023 | 88 224,6371 | 88 224,6370 |

Die Umschaltung im Diagramm ist damit ein bewusster **Größenwechsel** (Produktion ↔ Deckung); der
Diagrammtitel nennt ihn („Deckung Heizung — Jahresganglinie"), und der CSV-Export schreibt die
Bedarfsart in die Spaltenköpfe. Bei Bedarfsart **„Gesamt"** bleibt es bei der Produktion — dem
unveränderten Bestandsbild (Abschnitt 5.3).

---

## 4. Oberfläche

### 4.1 Bedarfsseite — vier Serien in einem Bild

* **Schalterleiste** unter dem Diagramm, links beginnend: `Gesamt` (vorausgewählt, schwarz),
  `Heizung` (Rot), `Brauchwasser` (Himmelblau), `Prozesswärme` (Violett **#7E57A6**).
* **Farbwahl.** Rot und Himmelblau sind die Farben, mit denen die Wärmepumpen-Seite denselben
  Bedarf seit jeher aufteilt (`CHART_LEGENDE_HEIZWAERMEBEDARF` / `_WARMWASSERBEDARF`, chart3).
  Violett #7E57A6 ist die Prozess-Kantenfarbe der Schema-Ansicht (Paket E1, Befund S2-O7) — der
  Katalog kennt für den Prozesskanal genau diese eine Farbe. `chart1` führt keine Legende; die
  eingefärbten Schalter sind sie.
* **Datenquelle** sind die **Kanalvektoren des Laufs** (`KanaeleDrei()`, netzverlust-inklusive) —
  dieselben, aus deren Jahressummen `SimulationRunner.BedarfJeKanal` die drei Kennzahlen unter dem
  Diagramm bildet. Zahl und Kurve können nicht auseinanderlaufen (gemessen: 0 abweichende Stunden,
  Jahressummen auf 4 Nachkommastellen gleich, Abschnitt 5.2).
* **Normierung**: alle vier Serien auf **dieselbe** Bezugsgröße `Waermebedarf_Max` (die Höchstlast
  des Gesamtbedarfs), gebildet mit `BhkwPlan.Normieren` — derselben Routine, mit der die
  Bestandskurve entsteht. Je Kanal auf sein eigenes Maximum normiert begännen alle drei Kurven bei
  100 % und sagten nichts mehr über die Größenverhältnisse. Die %-Achse (Maximum 100,2) bleibt
  unverändert gültig.
* **„Sortiert" wirkt je Serie**: `Normieren → Heapsort → Reverse`, dieselbe Kette wie beim
  Gesamtbedarf. Eine Kanaldauerlinie ist damit **keine** Zerlegung der Gesamtdauerlinie.
* **Präsenzregel**: Ein Kanal ohne Bedarf bekommt keinen Schalter und keine Kurve (Muster
  `ErgebnisPraesenz`). Gemessen: 1023 (kein Prozessbedarf) → Serie `BEDARF_PROZESS` mit **0
  Punkten**, Schalter aus; 1011/1041 → 8760 Punkte, Schalter da.
* **Platz**: `chart1` gibt 26 px Höhe ab (318 → **292**, Unterkante 440 → 414), die Schalterzeile
  liegt bei y = 418. Der Streifen darunter ist im Entwurf belegt (`btn_Details` ab y = 445), und
  ein Nachrücken der ganzen linken Spalte samt E1-Kanalblock (bis y ≈ 662 bei 721 px Seitenhöhe)
  wäre ein Umbau mit Kollisionsrisiko gewesen.
* **CSV-Export der Seite**: Er exportiert **nicht** die gezeichnete Prozentkurve, sondern den
  kWh-Vektor („Wärmelast"). Die Kanalspalten folgen dieser Konvention — je **angehaktem** Kanal
  eine kWh-Spalte mit dem Kanalnamen im Kopf. Eine Prozentspalte neben einer kWh-Spalte wären zwei
  Maßstäbe in einer Datei.

### 4.2 Ergebnisseite — Auswahl „Bedarfsart"

* **Auswahlliste** am Ende der zweiten Schalterzeile (hinter der Speicherauswahl), Beschriftung
  „Bedarfsart:". Einträge: `Gesamt` (Vorbelegung) und je ein Kanal **mit Bedarf**.
  Gemessen: 1011 → `[Gesamt | Heizung | Brauchwasser | Prozesswärme]`, 1023 →
  `[Gesamt | Heizung | Brauchwasser]`.
* **Bei Kanalwahl** zeigen `Wärmepumpe`, `Heizkessel`, `Solarthermie`, `BHKW` die **Deckung dieses
  Kanals** (`DeckungKanalStuendlich` = Direktdeckung + zugerechnete Speicherentladung — dieselbe
  Zusammensetzung, aus der `SimulationRunner.Summiere` die Jahresdeckung je Kanal bildet),
  `Heizstab` seine eigene Kanalzeile (wie in `NavigatorUebersicht` seit E1), `Gesamt` die Summe der
  fünf, und „Wärmebedarf einblenden" den **Kanalbedarf**.
* **Speicherfüllstand bleibt kanalunabhängig.** Ein Füllstand ist der Inhalt eines Behälters, keine
  Kanalgröße. Die **Entladung** des gewählten Kanals steckt in den Erzeugerserien — zugerechnet
  nach Herkunftsart, exakt wie in der Jahresbilanz (Interimsregel „Vermischung im Speicher",
  `Kaskadenschleife.Anteil_Entladen`). Sichtbar in 1023: Bedarfsart „Heizung" weist der Wärmepumpe
  109 638,38 kWh zu, die vollständig aus der Speicherentladung stammen (Direktdeckung 0).
* **Diagrammtitel** nennt die Bedarfsart, **CSV-Spaltenköpfe** ebenso.

---

## 5. Verifikation

### 5.1 Referenzlauf — die zugesagte Messlatte

Dreizehn Projekte (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039, 1040, 1041, 1042).
Datenquelle: produktive `Kenndaten.accdb` (28.08.2026 **09:05:30**, 151 949 312 Bytes, MD5
`F7A4227759…`), **nur gelesen**; Arbeitskopie migriert auf Schemastand **54**.

**Gegen die eingefrorene Basis `2026-08-28_P1`:**

```
12 Projekte PASS · Projekt_1042 FAIL
MD5: 321 von 329 byte-gleich, 8 abweichend, 0 fehlend, 3 zusaetzlich
      — alle elf Abweichungen ausschliesslich in Projekt_1042
```

**Projekt 1042 ist eine Datenänderung des Anwenders, kein Codeeffekt.** Der Beweis ist ein
**A/B-Lauf gegen den unveränderten HEAD**: `git archive HEAD` nach `C:\Waermeplan\_e2base`
exportiert, dort gebaut, dieselben dreizehn Projekte auf derselben Quelle gerechnet.

```
A/B  Basis(HEAD)  ->  E2 :  13/13 PASS  (3 558 319 Werte)
MD5                       :  332 von 332 CSV byte-gleich,
                             0 abweichend, 0 fehlend, 0 zusaetzlich
Gegenprobe  2026-08-28_P1 -> Basis(HEAD) : dasselbe FAIL in 1042,
                             mit derselben Signatur
```

Die Signatur ist unverwechselbar strukturell und für Code unerreichbar:
`Sim.PufferWP_vorhanden` False → **True**, `Pufferspeicher[0].Verwendung` Kombi →
**Brauchwasser**, `Pufferspeicher[1]/[2].Verwendung` Brauchwasser → **Heizung**, und die drei
Dateien `puffer_soc/_ladung/_entladung.csv` erscheinen erstmals. Der Anwender hat 1042 zwischen dem
P1-Einfrieren und diesem Lauf umkonfiguriert — dieselbe Lage, die schon E1 §4.3 für dieses Projekt
festgehalten hat.

**Weitere Nachweise:**

* `pruefen`: **GESAMT plausibel**, keine NaN/Inf; die drei Hinweise „Gewerk aktiviert, aber kein
  Modul" sind Bestand.
* **Laufprotokoll**: 50 verschiedene Meldungen im Basislauf, 50 im E2-Lauf, **Mengenvergleich
  leer**.
* Der Probeordner ist nach dem Vergleich gelöscht; die Basis `2026-08-28_P1` bleibt unverändert
  gültig.

### 5.2 Wirkproben auf einer Wegwerf-Kopie

Wegwerf-Kopie über `Referenzlauf.exe migration` (Schemastand 54). Reflection-Harness nach dem
Muster [[ui-pfad-test-reflection-harness]] (Quellen unversioniert unter `dev\h2\src`), Projekte
**1011** (Prozessbedarf), **1023** (Heizstab auf Brauchwasser), **1041** (Prozess mit eigenem
Puffer). **Alle Proben PASS.**

**(a) Kanalganglinien-Probe der Engine**

| Projekt | geprüfte Zusagen | Verstöße | größter Rest |
|---|---|---|---|
| 1011 | 29 | **0** | 1,659·10⁻⁸ kWh |
| 1023 | 33 | **0** | 5,559·10⁻⁹ kWh |
| 1041 | 37 | **0** | 0 kWh |

**(b) Bedarfsseite — Kurve == Kanalsatz == angezeigte Zahl**

| Projekt | Kanal | abweichende Stunden | Kurvensumme [MWh] | Anzeige (E1) [MWh] |
|---|---|---|---|---|
| 1011 | Heizung | **0** | 4 736,1941 | 4 736,1941 |
| 1011 | Brauchwasser | **0** | 4,0597 | 4,0597 |
| 1011 | Prozesswärme | **0** | 365,0000 | 365,0000 |
| 1023 | Heizung | **0** | 329,7297 | 329,7297 |
| 1023 | Brauchwasser | **0** | 60,0000 | 60,0000 |
| 1041 | Prozesswärme | **0** | 30,0000 | 30,0000 |

Die Jahressummen von 1011 und 1041 sind zeichengleich mit der Kanal-Summenprobe des
E1-Protokolls (§4.4: 4 736,19 / 4,06 / 365,00 bzw. 124,74 / 5,00 / 30,00 MWh).

**Diagrammzustand (aus dem laufenden Formular gelesen):** `chart1` führt **4 Serien**, Höhe
**292**, Schalterzeile y = **418**. Sortiert = jede Serie für sich monoton fallend (erster Wert =
Maximum), unsortiert = chronologisch. Alle Kanalkurven bleiben unter der Gesamtkurve — 1011:
Gesamt 100,000 %, Heizung 96,281 %, Brauchwasser 0,194 %, Prozess 3,629 %.

| Projekt | `BEDARF_PROZESS` Punkte | Schalter „Prozesswärme" | nach Anhaken aktiv |
|---|---|---|---|
| 1011 | 8 760 | erscheint | Series1, BEDARF_HEIZUNG, BEDARF_BRAUCHWASSER, **BEDARF_PROZESS** |
| 1023 | **0** | **ausgeblendet** | Series1, BEDARF_HEIZUNG, BEDARF_BRAUCHWASSER |
| 1041 | 8 760 | erscheint | Series1, BEDARF_HEIZUNG, BEDARF_BRAUCHWASSER, **BEDARF_PROZESS** |

**(c) Ergebnis-Diagramm je Bedarfsart** (Jahressumme der gezeichneten Serien [kWh])

*Projekt 1011, Bedarfsart **Prozesswärme*** — Auswahl
`[Gesamt | Heizung | Brauchwasser | Prozesswärme]`:

| Serie | Diagramm | E1-Jahreszeile (Direkt + Entladung) |
|---|---|---|
| Wärmepumpe | 133 177,270 | 133 177,2704 + 0 |
| Solarthermie | 133,643 | 133,6429 + 0 |
| Heizkessel / BHKW / Heizstab | 0,000 | 0 |
| Gesamt | 133 310,913 | Summe der fünf |
| Wärmebedarf (Kanal) | 365 000,011 | = Kanalbedarf |

*Projekt 1023, Bedarfsart **Brauchwasser*** — Auswahl `[Gesamt | Heizung | Brauchwasser]`:

| Serie | Diagramm | E1-Jahreszeile |
|---|---|---|
| **Heizstab** | **54 382,935** | `Heizstab_Kanal[BRAUCHWASSER]` = **54 382,94** |
| Heizkessel | 5 617,058 | 5 617,0583 |
| Wärmepumpe | 0,000 | 0 (die WP bedient in 1023 nur den Heizkanal) |
| Gesamt / Wärmebedarf | 59 999,993 / 59 999,993 | Bedarf vollständig gedeckt |

Der Heizstab deckt in 1023 den Brauchwasserbedarf in **6 153 Stunden** — die bekannte
Heizstab-BW-Lage aus der Kanalbuchführung, jetzt als Ganglinie ablesbar.

*Projekt 1041, Bedarfsart **Prozesswärme***: Wärmepumpe 30 000,000 kWh = Kanalbedarf 30 000,000 —
der Prozesspuffer-Fall aus Konzept 11.1, vollständig durch die WP gedeckt.

**(d) „Gesamt" ist byte-identisch zum bisherigen Chartbild**

Datenreihen-Vergleich (nicht Pixel): Dieselbe Harness-Fassung wurde gegen die **Basis-DLL (HEAD)**
und gegen die **E2-DLL** gefahren; gedumpt wurden alle Serien des Ergebnis-Diagramms mit allen
8 760 Punkten je Serie im verlustfreien `R`-Format.

| Projekt | MD5 Basis | MD5 E2 | Zeilen / Bytes |
|---|---|---|---|
| 1011 | `828837670B48…` | `828837670B48…` | 52 566 / 618 355 |
| 1023 | `03A2C9486D4B…` | `03A2C9486D4B…` | 52 566 / 782 815 |
| 1041 | `14D64C556C3F…` | `14D64C556C3F…` | 61 327 / 1 005 347 |

**3 von 3 byte-gleich.** Bedarfsart „Gesamt" liefert exakt das bisherige Bild.

**(e) Zeitreihen des Berichts** — je Projekt zusätzlich aufgenommen:

| Projekt | Reihen gesamt | davon Kanalreihen | Beispiele |
|---|---|---|---|
| 1011 | 21 | **9** | `BEDARF_PROZESS` 365 000,01 kWh · `DECKUNG_WAERMEPUMPE_PROZESS` 133 177,27 kWh |
| 1023 | 19 | **7** | `DECKUNG_HEIZSTAB_BRAUCHWASSER` 54 382,94 kWh |
| 1041 | 24 | **7** | `DECKUNG_HEIZKESSEL_HEIZUNG` 124 739,25 kWh |

### 5.3 Produktive Datenbank unberührt

| | vorher (09:39) | nachher (09:53) |
|---|---|---|
| Größe | 151 949 312 Bytes | 151 949 312 Bytes |
| Zeitstempel | 28.08.2026 09:05:30 | 28.08.2026 09:05:30 |
| MD5 | `F7A422775915976127F2DA6B1E024ADF` | `F7A422775915976127F2DA6B1E024ADF` |
| `Kenndaten.laccdb` | keine | keine |

Alle Schreibproben liefen auf der Wegwerf-Kopie; die Anwendung des Anwenders lief während des
gesamten Pakets (PID 148592) — deshalb wurde ausschließlich in ein eigenes `OutDir`
(`dev\e2ref\`) gebaut.

---

## 6. Ein gefundener und behobener Fehler in der eigenen Arbeit

Die erste Fassung von `BedarfSerienSchalten` las die Präsenz eines Kanals aus
`chk_BedarfKanal[k].Visible` zurück. Das ist genau die Falle, die
`NavigatorWaerme.CheckboxenAnordnen` seit Paket 9 ausdrücklich benennt: **`Control.Visible` liefert
false, solange die Registerkarte nicht angezeigt wird** — und beim Befüllen ist das der Regelfall,
weil der automatische Lauf aus `Form…_Load` startet, während die Seite „Übersicht" vorne steht. Die
Kanalserien wären dabei stumm abgeschaltet worden. Die Wirkprobe hat es gezeigt („nach Anhaken
aller Kanäle aktiv: **Series1**"), der Fix führt die Präsenz im Feld `_bedarfKanalDa` mit; danach
meldet dieselbe Probe alle angehakten Serien.

---

## 7. Beifang

**Der CSV-Export-Button der Bedarfsseite verdeckte den E1-Kanalblock.** `InitCsvExportButtons`
setzte ihn auf den festen Punkt (22, 565) — der Block „davon Heizung/Brauchwasser/Prozesswärme"
beginnt seit Paket E1 bei y = 562 (Überschrift) bzw. 584 (erste Zeile), und der Button steht wegen
`BringToFront()` obenauf. Er rückt jetzt unter den Block (`BedarfKanalblockUnterkante() + 12`);
ohne Block bleibt es beim Entwurfspunkt. Kein E1-Fehler in der Rechnung, ein reiner Anzeigefehler —
gefunden beim Einpassen der neuen Schalterzeile.

---

## 8. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| E2-O1 | **Die Referenzbasis ist neu zu setzen.** `2026-08-28_P1` trägt für 1042 einen Datenstand, den es nicht mehr gibt (5.1). Die Neusetzung macht der Orchestrator | Orchestrator |
| E2-O2 | Der **Bericht zeichnet** die Kanalreihen noch nicht. Sein Ganglinienteil hat fünf feste Bildtypen (`ChartRenderer.Waermeganglinie/Stromganglinie/Speicherverlauf/…`); ein sechster wäre ein Layoutumbau und lag außerhalb von E2. Die Reihen stehen im `ZeitreihenSatz` und sind damit für einen Kanal-Ganglinienbaustein verfügbar | Berichtspaket |
| E2-O3 | **Produktion ≠ Deckung** (Abschnitt 3): Bei Bedarfsart „Gesamt" zeigt das Ergebnis-Diagramm die Produktion, bei Kanalwahl die Deckung. Der Titel benennt es. Ein Umschalter „Produktion / Deckung" auch für „Gesamt" wäre die konsequentere Bedienung — er war nicht beauftragt („Gesamt = exakt heutiges Verhalten") | bei Bedarf |
| E2-O4 | Die **Quellentnahme** eines Moduls aus seinem Quellpuffer wird weiter auf den Heizkanal gebucht (E1-O6, Konzept 4.2/F18). Die Kanalganglinien erben diese Näherung unverändert; sie betrifft nur die kanalfeine Anzeige, nie eine Bilanzsumme | B1/Aufräumen |
| E2-O5 | `SimulationPufferspeicher.Entladung_KanalStuendlich` wird gebucht, aber **noch nirgends gelesen** — die Erzeugerserien holen die Entladung über die Herkunftszurechnung der Kaskadenschleife. Sie steht bereit für eine Speicher-Kanalanzeige (Karte/Ergebnistabelle) | bei Bedarf |
| E2-O6 | Speicher **ohne** `Reset()` (Quellspeicher starten gefüllt) nullen ihre Ganglinie nicht am Laufanfang — dieselbe Lage wie beim Bestandsfeld `Entladung_Kanal`, das ebenfalls in `Reset()` genullt wird. Unkritisch, solange die Quellspeicherobjekte je Lauf neu entstehen; mit dem Aufräumpaket zusammen prüfen | Paket L |
| E2-O7 | Der **Speicher der 8760er-Ganglinien** wächst um rund 1,7 MB je Lauf (4 Erzeugerarten × 2 Größen × 3 Kanäle × 8760 × 8 Byte, plus Heizstab und je Speicher eine Zeile). Für einen Variantenbericht mit vielen Varianten wäre eine bedarfsweise Anlage die schärfere Form | bei Bedarf |
| E2-O8 | Die **Sichtbarkeit** der Bedarfsseiten-Schalter ließ sich im Harness nicht messen (`Control.Visible` bleibt bei einem nur programmatisch angezeigten MDI-Kindformular false). Nachgewiesen ist die Präsenzregel funktional über die Punktzahl der Serien (0 ↔ 8760) und über den Schaltzustand nach dem Anhaken | Messmethode |
