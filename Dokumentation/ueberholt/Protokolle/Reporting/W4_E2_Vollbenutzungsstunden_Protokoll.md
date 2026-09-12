# W4 · Etappe E2 — Vollbenutzungsstunden des BHKW

**Stand: 18.08.2026.** Umsetzung der Etappe **E2** aus
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md), Leitentscheidung **L6**.
Ausgangsstand `befde0d`.

**Ergebnis in drei Sätzen.** Die KWKG-Deckelung rechnet ab jetzt mit **elektrischen**
Vollbenutzungsstunden statt mit der Summe thermischer über alle Module, und die
„installierte elektrische Leistung" ist die der **Anlagenzeilen** statt aller Gerätezeilen
des Projekts. Die Simulationsergebnisse ändern sich dadurch **nicht** (8/8 PASS,
191 von 194 CSV byte-identisch, die drei übrigen unterscheiden sich ausschließlich in den
drei neu hinzugekommenen Schlüsseln). Von den Wirtschaftlichkeitswerten ändert sich auf dem
heutigen Datenbestand **genau einer** — Projekt 1024, wo eine falsche Leistungssumme den
Zuschlag auf 0 gesetzt hatte.

---

## 1 Der Fehler

### 1.1 Fundstelle

`Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:853` (Stand `befde0d`):

```csharp
double stromMWh = v.Ergebnis.BHKW.Stromproduktion;
double vbh = v.Ergebnis.BHKW.Betriebsstunden_Gesamt;
```

`Betriebsstunden_Gesamt` ist **keine Betriebsstundenzahl**. Die Größe entsteht in
`Allgemein/Simulation/SimulationBHKW.cs:294-304` je Modul als

```
Laufzeiten[i] = Waerme_MWh[i] / P_therm[i] * 1000
```

— eine **thermische Vollbenutzungsstundenzahl** — und wird in `:353` über alle Module
**aufsummiert**. Zwei Fehler in einem Ausdruck:

| # | Fehler | Folge |
|---|---|---|
| 1 | **falsche Energieart** — thermisch statt elektrisch | Der KWK-Zuschlag wird je Kilowattstunde KWK-**Strom** gezahlt und über Vollbenutzungsstunden gedeckelt (KWKG 2025 § 8). |
| 2 | **falsche Aggregation** — Summe statt leistungsgewichtet | Die Summe kann 8.760 h überschreiten; die Deckelung greift dann nicht mehr an der Größe, die das Gesetz meint. |

Der Selbstkommentar bei `:842` nannte das „Näherung: erreichte Vbh = Betriebsstunden des
BHKW" — dass die Näherung bei Mehrmodulanlagen mit der Modulzahl skaliert, stand dort nicht.

### 1.2 Was die Messung dazu ergeben hat — zwei Befunde, die das Konzept korrigieren

**Befund 1: Je Modul sind thermische und elektrische Vbh im heutigen Modell identisch.**
Der Rechenkern fährt jedes Modul stets im festen Verhältnis `P_el / P_therm` — Volllast,
Teillast und der stromgeführte Zweig rechnen sämtlich proportional
(`SimulationBHKW.cs:495-512`, `:555-571`, `:693-717`; Stand nach E2). Damit gilt

```
Vbh_el,i = Strom_i / P_el,i = (Waerme_i · P_el,i / P_therm,i) / P_el,i = Waerme_i / P_therm,i = Vbh_th,i
```

Nachgewiesen an allen drei BHKW-Projekten des Bestands (Abschnitt 4.1) und an der
präparierten Kaskade (4.3). **Fehler 1 wirkt sich auf die Zahl je Modul also nicht aus** —
er ist trotzdem zu beheben, weil die Gleichheit eine Eigenschaft des heutigen Modells ist
und keine des Gesetzes: Sobald eine spätere Ausbaustufe modulierende Kennfelder mit
lastabhängiger Stromkennzahl abbildet, laufen die beiden Größen auseinander.

**Befund 2: Der Altstand rechnete den Zuschlag bei Kaskaden zu NIEDRIG, nicht zu hoch.**
Das Konzept (Abschnitt 1) nahm das Gegenteil an. `BaueKwkgReihe` normiert:

```csharp
double verguetet = Math.Min(vbh, Math.Min(deckel, rest)) * (1.0 - abschlag);
reihe[t] = bonusVoll * (verguetet / vbh);
rest -= verguetet;
```

Eine zu große `vbh` **senkt** den Bruch `verguetet / vbh`, sobald der Jahresdeckel greift,
**und** verbraucht das 30.000-h-Kontingent um den Faktor der Modulzahl zu schnell. Gemessen
an einer präparierten Zweimodul-Kaskade: Jahr 1 **242,90 € statt 485,81 €** — exakt die
Hälfte (Abschnitt 4.3).

### 1.3 Ein zweiter Fehler, in derselben Methode gefunden

`LiesBhkwLeistungKW` (Stand `befde0d`, `:991-1002`) las

```sql
SELECT SUM(Pel) FROM Tab_BHKW WHERE ID_Projekt = ?
```

Das ist nicht die installierte Leistung, sondern die Summe über **alle Gerätezeilen** des
Projekts — auch über solche, zu denen nie eine Anlagenzeile entstand oder deren Anlagenzeile
gelöscht wurde. Die Simulation baut ihre Modulliste dagegen ausschließlich aus
`Tab_Energieanlagen` (`SimulationControl.BHKW_Liste_Laden:1495-1526`).

**Gemessen am Bestand (Produktivdatenbank, 18.08.2026):**

| Projekt | BHKW-Anlagenzeilen | Σ P_el Anlagenzeilen | Gerätezeilen in `Tab_BHKW` | Σ P_el Gerätezeilen |
|---|---|---|---|---|
| 1017 | 1 | 10,0 kW | 1 | 10,0 kW |
| 1018 | 1 | 14,5 kW | 1 | 14,5 kW |
| 1024 | 1 | **21,0 kW** | 5 | **546,4 kW** |
| 1023 | 0 | — | 11 | 1.551,2 kW |
| 1022 | 0 | — | 2 | 260,0 kW |
| 1015 | 0 | — | 1 | 250,0 kW |

Die Summe des Projekts 1024 überschritt damit die 500-kW-Schwelle der
Ausschreibungslücke — für eine Anlage mit **21 kW**. Der Zuschlag wurde auf 0 gesetzt, mit
der Meldung „Σ installierte BHKW-Leistung 546 kW > 500 kW". Das ist die einzige
€-Auswirkung von E2 auf ein Bestandsprojekt.

---

## 2 Was umgesetzt wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | Elektrische Vbh **je Modul** im Rechenkern (`VbhElektrisch[]`) | `Allgemein/Simulation/SimulationBHKW.cs:136` (Feld), `:350-359` (Rechnung), `:914-922` (Rücksetzen) |
| 2 | **Leistungsgewichtete** elektrische Vbh der Anlage (`VbhElektrischGesamt`) | `SimulationBHKW.cs:152` (Feld), `:414-428` (Rechnung) |
| 3 | Übergabe in die Ergebniszeilen (Aggregat + je Modul) | `Allgemein/Simulation/SimulationRunner.cs:396-401`, `:517-526` |
| 4 | Ergebnismodell: `ErgebnisBHKWModel.VbhElektrisch`, `ErgebnisBHKWModulModel.VbhThermisch/.VbhElektrisch` | `Model/ErgebnisModel.cs:104-126`, `:152-175` |
| 5 | Schreib- und Leseweg der drei Spalten | `Controller/ErgebnisCtrl.cs:246-284` (INSERT BHKW), `:292-330` (INSERT Modul), `:724`, `:739-740` (Lesen) |
| 6 | Tolerante Rückfallebene vor dem Schreiben | `ErgebnisCtrl.cs:995-1001` (BHKW-Zeile), `:1038-1045` (Modulzeilen) |
| 7 | **Migrationsschritt 18** — Spaltenkatalog | `Allgemein/Update/SchemaKatalog.cs:579-644` |
| 8 | **Migrationsschritt 18** — Schrittnummer, Beschreibung, Ausführung, `ZIEL_VERSION 17 → 18` | `Allgemein/Update/SchemaMigration.cs:77`, `:330-355`, `:666-673`, `:1140-1152` |
| 9 | **Die Korrektur**: Deckelung auf elektrische Vbh | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:872-891` (`BaueKwkgReihe`), `:1088-1137` (`VbhElektrisch`) |
| 10 | Korrigierte Bezugsmenge von Σ P_el (Anlagenzeilen statt Gerätezeilen) | `WirtschaftlichkeitCtrl.cs:1026-1079` (`LiesBhkwLeistungKW`), `:1081-1086` (`PelKW`) |
| 11 | Bemessungsgrundlage im Ergebnis führen und persistieren | `WirtschaftlichkeitDaten.cs:255-271`, `WirtschaftlichkeitCtrl.cs:44-47` (Spaltenname), `:220-227` (`SpalteSicher`), `:1434`, `:1466` (INSERT), `:1595` (Lesen) |
| 12 | Ergebnisreiter: neue Zeile „Vollbenutzungsstunden elektrisch", zwei Bestandszeilen richtig benannt | `Views/Simulation/Form_Simulation_Detail.cs:1221`, `:1223-1305` (`InitBhkwVbhZeile`, `BhkwBrennstoffBlockOben`), `:1319-1339` (begrenztes Nachrücken), `:1505-1508` (Sichtbarkeit), `:3517-3529` (Wertzuweisung) |
| 13 | Kennzahlkatalog: Bestandszeile richtig benannt, neue Zeile für die elektrische Größe | `Allgemein/Bericht/KennzahlenKatalog.cs:119-133` |
| 14 | Variantenvergleichsbericht (Word) | `Views/Varianten/ProjektvergleichBericht.cs:207-213` |
| 15 | Wirtschaftlichkeitsreiter, Word-Baustein, Excel-Blatt | `Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs:541-547`, `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs:239-245`, `Allgemein/Bericht/ExcelBerichtGenerator.cs:269-271` |
| 16 | Drei Ressourcenschlüssel in beiden Sprachen samt Designer und Katalognachtrag | `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`, `Allgemein/Simulation/Lokalisierung_Katalog.md` |

**17 Quelldateien geändert** (660 Zeilen zugefügt, 19 entfernt) **und vier Dokumente**: dieses Protokoll (neu), `W4_Umsetzungsstand.md`, `Konzept_BHKW_Kosten_Erloese.md`, `Allgemein/Simulation/Lokalisierung_Katalog.md`.

---

## 3 Entwurfsentscheidungen

### 3.1 Die Spalte heißt `VbhThermisch`, nicht `Betriebsstunden`

Der Auftrag und das Konzept (Abschnitt 3) nannten die Spalte `Betriebsstunden`; der Auftrag
stellte zugleich frei, einen treffenderen Namen zu wählen. Er ist treffender:

Der gespeicherte Wert ist `Wärmeproduktion [MWh] × 1000 / P_therm [kW]` — eine
**Vollbenutzungsstundenzahl**. Betriebsstunden sind das nicht: Der Rechenkern bildet keine
Taktung ab. Ein Modul, das ein Jahr lang zur Hälfte moduliert läuft, hat **8.760
Betriebsstunden und 4.380 thermische Vbh**. Eine Spalte namens `Betriebsstunden` hätte genau
die Verwechslung festgeschrieben, die diese Etappe an anderer Stelle behebt — und sie hätte
spätestens bei der Wartung „je Betriebsstunde" (L7, Etappe E3) jemand für bare Münze
genommen.

Der Name sagt jetzt, wie der Wert gebildet ist. **Dass er als Näherung für Betriebsstunden
dient, steht an drei Stellen als Näherung dokumentiert**: am Feld
(`ErgebnisModel.cs`), an der Spaltenkonstante (`SchemaKatalog.cs`) und an der Quelle im
Rechenkern (`SimulationBHKW.cs`).

Die Aggregatfelder `Betriebsstunden_Gesamt` und `Betriebsstunden_Durchschnitt` behalten
dagegen ihre Namen: Die gleichnamigen Spalten in `Tab_ErgebnisBHKW` bestehen seit jeher, und
ein Umbenennen wäre ein Schemaeingriff ohne Gegenwert. Ihre **Anzeige** ist berichtigt
(Abschnitt 3.4).

### 3.2 Leistungsgewichtet, nicht Summe und nicht Mittelwert

Drei Aggregationen standen zur Wahl:

| Variante | Verhalten | Bewertung |
|---|---|---|
| Summe der Modulwerte (Altstand) | kann 8.760 h überschreiten; wächst mit der Modulzahl | **falsch** — die Deckelung greift an einer Zahl, die es physikalisch nicht gibt |
| arithmetisches Mittel | ≤ 8.760 h | gewichtet ein 5-kW-Modul so stark wie ein 500-kW-Modul |
| **Σ Strom / Σ P_el** | ≤ 8.760 h **konstruktiv** | die einzige Größe mit der Aussage „so viele Vollbenutzungsstunden hat die installierte elektrische Leistung erreicht" |

Gewählt ist die dritte. Sie hat eine Eigenschaft, die für E6 wichtig ist: **Solange alle
Module über dem Jahresdeckel liegen, liefert sie exakt dasselbe wie die modulscharfe
Rechnung.**

```
modulscharf   = Σᵢ min(Vbhᵢ, Deckel) · P_el,ᵢ · Satz
              = Deckel · Σ P_el · Satz                       (alle Vbhᵢ > Deckel)

projektweit   = bonusVoll · Deckel / Vbh_gew
              = (Σ Strom · Satz) · Deckel / (Σ Strom / Σ P_el)
              = Deckel · Σ P_el · Satz                       — identisch
```

Nachgeprüft an beiden präparierten Kaskaden (Abschnitt 4.3 und 4.4): In beiden Fällen
stimmt die neue projektweite Rechnung mit der modulscharfen überein. Der Unterschied
entsteht erst, wenn ein Teil der Module unter und ein anderer über dem Deckel liegt — das
ist genau der Fall, den **E6** modulscharf auflöst.

### 3.3 Kein Backfill der drei neuen Spalten

`DOUBLE` lässt Bestandszeilen auf `NULL`, und das ist die ehrliche Wahl: Ein Lauf vor E2 hat
diese Größen nie erhoben. Nachrechnen ließen sie sich auch nicht — der Nenner ist die
installierte Leistung **zum Zeitpunkt des Laufs**, und die steht nirgends im Ergebnis.
Dieselbe Begründung wie bei `Tab_ErgebnisHeizkessel.Quellwaerme` (Schritt 10, Etappe D4).

Die Wirtschaftlichkeit rechnet die elektrischen Vbh in diesem Fall **selbst** aus
`Stromproduktion` und der heute installierten Leistung — sichtbar als eigener Rechenweg
(`WirtschaftlichkeitCtrl.VbhElektrisch`), nicht als stiller Datenwert. Vorrang hat immer der
gespeicherte Wert, damit Ergebnisreiter, Bericht und Rechnung dieselbe Zahl zeigen.

### 3.4 Zwei Bestandsbeschriftungen sind berichtigt

Die BHKW-Ergebnisseite beschriftete ihre beiden Vbh-Felder mit „Betriebsstunden gesamt" und
„Betriebsstunden Durchschnitt". Beide Texte sind falsch, und beide standen im `.resx` der
Form. Sie werden jetzt zur Laufzeit aus dem Katalog `MyResource` ersetzt
(`InitBhkwVbhZeile`) — dasselbe Muster wie bei den Speicher-Kennzahlzeilen; Designer und
`.resx` der Form bleiben unangetastet.

| Feld | vorher | jetzt (DE) | jetzt (EN) |
|---|---|---|---|
| `textBox_Betriebsstunden` | Betriebsstunden gesamt | Vbh thermisch, Summe Module | Thermal FLH, sum of modules |
| `textBox_Betriebsstunden_Durchschnitt` | Betriebsstunden Durchschnitt | Vbh thermisch, Mittel Module | Thermal FLH, module average |
| *(neu)* `tb_BhkwVbhElektrisch` | — | Vollbenutzungsstunden elektrisch: | Full-load hours, electric: |

Die neue Zeile rückt in die 39 px, die zwischen der letzten Deckungszeile und der
Überschrift „Brennstoffverbrauch" frei sind; nachgerückt wird deshalb **nur bis zu dieser
Überschrift** (`BhkwZeilenNachruecken(abY, bisY, dy)`). Der Brennstoffblock endet
unverändert bei y ≈ 696 innerhalb der Entwurfshöhe 721 — nachgemessen am aufgebauten
Formular (Verifikation V12).

### 3.5 Die neue Zeile im Wirtschaftlichkeitsnachweis bleibt deutsch — wie ihre Nachbarn

Reiter, Word-Baustein und Excel-Blatt der Wirtschaftlichkeit führen **alle** Zeilentitel als
deutsche Literale („Investition I₀ [€]", „KWKG-Erlös Jahr 1 [€/a]" …); der Bereich hat keine
`de-DE.resx` und ist in der Lokalisierung bekanntlich lückenhaft
(`WindowsFormsApplication1/CLAUDE.md`, Fallstricke). „Vbh elektrisch (KWKG-Basis) [h/a]"
folgt dieser Konvention. **Eine einzelne lokalisierte Zeile zwischen vierzehn deutschen wäre
keine Lokalisierung, sondern eine Inkonsistenz mehr** — der Bereich gehört als Ganzes
umgestellt, und das ist ein eigener Vorgang. Der Ergebnisreiter, der sehr wohl über
`MyResource` läuft, bekommt seine drei Texte dagegen in beiden Sprachen (Abschnitt 3.4).

### 3.6 `KWKGVbhElektrisch` über `SpalteSicher`, nicht über einen Migrationsschritt

`Tab_ErgebnisWirtschaftlichkeit` wird von `WirtschaftlichkeitCtrl.StelleTabellenSicher`
angelegt und additiv nachgerüstet — seit W1 für 17 Spalten. Die achtzehnte über
`SchemaMigration` zu führen hätte für **eine** Tabelle zwei Mechanismen ergeben. Die
doppelte Wahrheit „`SchemaMigration` gegen eigenes DDL" ist als offener Punkt bekannt
(`W4_Umsetzungsstand.md`, Abschnitt 6) und wird dort aufgelöst, nicht hier vertieft.

---

## 4 Wirkung — mit Zahlen

**Gemeinsame Grundlage aller Zahlen dieses Abschnitts:** eine Wegwerf-Kopie der produktiven
`Kenndaten.accdb` vom 18.08.2026, mit dem E2-Stand auf Schemastand 18 migriert. Beide
Codestände lesen dieselbe Datei. KWKG-Parameter für die Probe: Zuschlag Eigenstrom
4,00 ct/kWh, Einspeisung 8,00 ct/kWh, Kontingent 30.000 h, Jahresdeckel aus der
KWKG-2025-Staffel, Zins 3,0 %, Betrachtungszeitraum 20 a.

> **Warum die Parameter gesetzt werden mussten:** Im gesamten Bestand führt
> `Tab_ProjektWirtschaftlichkeit` genau **eine** Zeile (Projekt 1019, ohne BHKW), und deren
> `KWKG_Bonus` steht auf 0. **Ohne gepflegten Zuschlagssatz rechnet `BaueKwkgReihe`
> überhaupt nicht** — auf dem unveränderten Datenbestand ist E2 damit ohne jede
> €-Auswirkung. Die Zahlen unten zeigen, was passiert, sobald ein Anwender den Satz pflegt.

### 4.1 Bestandsprojekte mit BHKW — je ein Modul

| Projekt | Module | Strom [MWh/a] | Vbh **alt** (Σ therm.) | Vbh **neu** (el., gew.) | KWKG Jahr 1 **alt** | **neu** | Δ € | Δ % |
|---|---|---|---|---|---|---|---|---|
| 1017 „WP_PV-Speicher" | 1 | 28,43 | 2.842,90 h | 2.842,90 h | 1.137,20 €/a | 1.137,20 €/a | **0,00** | **0 %** |
| 1018 „BHKW Test München" | 1 | 14,96 | 1.031,63 h | 1.031,63 h | 598,40 €/a | 598,40 €/a | **0,00** | **0 %** |
| 1024 „Wöhler - Test2" ¹ | 1 | 73,91 | 3.519,43 h | 3.519,43 h | **0,00 €/a** | **2.956,40 €/a** | **+2.956,40** | **+∞** |

¹ mit Inbetriebnahme 2024 gerechnet; mit einer Inbetriebnahme ab 2025 greift für dieses
Öl-BHKW der Heizöl-Ausschluss und der Zuschlag ist in **beiden** Ständen 0 — dann allerdings
aus dem fachlich richtigen Grund statt wegen einer falschen Leistungssumme.

**Barwerte der KWKG-Reihe über 20 Jahre** (der Anteil, den E2 am Kapitalwert verändert):

| Projekt | Reihe **alt** | Reihe **neu** | Δ Barwert | Δ Kapitalwert |
|---|---|---|---|---|
| 1017 | 10.068,70 € | 10.068,70 € | 0,00 € | **0,00 €** |
| 1018 | 8.902,68 € | 8.902,68 € | 0,00 € | **0,00 €** |
| 1024 ¹ | 0,00 € | 21.625,51 € | +21.625,51 € | **+21.625,51 €** |

Die Gleichheit bei 1017 und 1018 ist kein Zufall, sondern Befund 1 aus Abschnitt 1.2: Bei
**einem** Modul sind Summe und leistungsgewichtetes Mittel dieselbe Zahl, und thermische und
elektrische Vbh sind es ohnehin. Die Änderung bei 1024 kommt **nicht** aus der Vbh-Größe,
sondern aus der berichtigten Leistungssumme (Abschnitt 1.3).

**Kapitalwert und Amortisation lassen sich für diese drei Projekte nicht beziffern.** Alle
drei melden „Energiekosten nicht bestimmbar — Arbeitspreise/Träger in der Kostenmaske
prüfen"; ohne den größten Kostenposten bleibt der Kapitalwert `null` — in **beiden**
Ständen, vor und nach E2. Das ist eine Datenlücke der Projekte, keine Wirkung dieser Etappe.
Weil die KWKG-Reihe als `zusatzErloesJeJahr` abgezinst in den Kapitalwert eingeht
(`KapitalwertRechner.cs:124-141`) und sonst nichts verändert wird, gilt exakt
**Δ Kapitalwert = Δ Barwert der KWKG-Reihe** — die Spalte oben ist damit die vollständige
Kapitalwertwirkung, sobald die Preise gepflegt sind.

### 4.2 Kein Projekt des Bestands hat mehr als ein BHKW-Modul

`Tab_Energieanlagen` führt im gesamten Bestand **drei** BHKW-Zeilen (Projekte 1017, 1018,
1024), je eine pro Projekt. Der Kaskadenfall, für den die Korrektur gemacht ist, kommt in
den Referenzprojekten **nicht vor**. Er wird deshalb an zwei präparierten Kopien gezeigt.

### 4.3 Präparierte Kaskade A — zwei reale Module

Projekt 1017 auf einer eigenen Wegwerf-Kopie um ein **zweites, baugleiches Modul** erweitert
(eigene Gerätezeile + eigene Anlagenzeile, so wie es Migrationsschritt 17 verlangt);
2 × 10 kW el / 19 kW th, Inbetriebnahme 2026.

| Größe | Wert |
|---|---|
| Modul 1 | Wärme 54,02 MWh · Strom 28,43 MWh · Vbh_th **2.842,90 h** · Vbh_el **2.842,90 h** |
| Modul 2 | Wärme 2,71 MWh · Strom 1,42 MWh · Vbh_th **142,48 h** · Vbh_el **142,48 h** |
| **Vbh alt** (Σ thermisch) | **2.985,38 h** |
| **Vbh neu** (Σ Strom · 1000 / Σ P_el = 29,85 · 1000 / 20) | **1.492,69 h** |

| | KWKG Jahr 1 | Reihe nominal 20 a | **Barwert (i = 3 %)** | Kontingent erschöpft |
|---|---|---|---|---|
| **alt** | 1.194,00 € | 11.998,47 € | **10.101,04 €** | Jahr 12 |
| **neu** | 1.194,00 € | 23.880,00 € | **17.763,70 €** | nicht (29.854 h von 30.000 h) |
| **Δ** | 0,00 € | +11.881,53 € | **+7.662,66 € (+75,9 %)** | |

Jahr 1 ist gleich, weil 2.985 h noch unter dem Jahresdeckel 2026 (3.300 h) liegen. Ab Jahr 3
greift der sinkende Deckel, und vor allem verbraucht der Altstand das 30.000-h-Kontingent
mit 2.985 h/a doppelt so schnell wie die neue Rechnung mit 1.492,69 h/a — nach Jahr 12 ist
der Zuschlag im Altstand **weg**, in der neuen Rechnung läuft er über den ganzen
Betrachtungszeitraum.

**Gegenprobe modulscharf (E6-Vorgriff):** Beide Module liegen unter dem Deckel, modulscharf
wäre also der volle Zuschlag zu zahlen — 1.194,00 €/a. Genau das liefert die neue
projektweite Rechnung; der Altstand liefert weniger.

### 4.4 Präparierte Kaskade B — die alte Größe überschreitet 8.760 h

Dieselbe Kaskade, beide Module auf 1,84 kW el / 3,50 kW th verkleinert, damit sie ganzjährig
laufen.

| Größe | Wert |
|---|---|
| Modul 1 | Vbh_th **5.033,61 h** · Vbh_el **5.033,45 h** |
| Modul 2 | Vbh_th **4.090,74 h** · Vbh_el **4.090,64 h** |
| **Vbh alt** (Σ thermisch) | **9.124,34 h** — **mehr als das Jahr hat** |
| **Vbh neu** (leistungsgewichtet) | **4.562,05 h** |

| | KWKG Jahr 1 | Reihe nominal 20 a | **Barwert (i = 3 %)** |
|---|---|---|---|
| **alt** | 242,90 € | 2.208,16 € | **1.867,24 €** |
| **neu** | 485,81 € | 4.416,44 € | **3.734,58 €** |
| **Δ** | **+242,91 € (+100,0 %)** | +2.208,28 € | **+1.867,34 € (+100,0 %)** |

Exakt Faktor 2 — die Modulzahl. Beide Module liegen über dem Jahresdeckel (3.300 h), also
gilt die Identität aus Abschnitt 3.2: Die neue projektweite Rechnung ist hier **gleich der
modulscharfen** (3.300 h × 3,68 kW × 4 ct/kWh = 485,76 €, Rundung).

### 4.5 Nachweis der Obergrenze

| Fall | Vbh **alt** (Σ therm.) | Vbh **neu** (el., gew.) | > 8.760 h? |
|---|---|---|---|
| 1017 (1 Modul) | 2.842,90 h | 2.842,90 h | nein / nein |
| 1018 (1 Modul) | 1.031,63 h | 1.031,63 h | nein / nein |
| 1024 (1 Modul) | 3.519,43 h | 3.519,43 h | nein / nein |
| Kaskade A (2 Module) | 2.985,38 h | 1.492,69 h | nein / nein |
| **Kaskade B (2 kleine Module)** | **9.124,34 h** | **4.562,05 h** | **JA** / nein |

Die neue Größe **kann** 8.760 h nicht überschreiten — nicht empirisch, sondern
konstruktiv:

```
Vbh_gew = Σ Strom / Σ P_el ≤ Σ (P_el,ᵢ · 8760) / Σ P_el,ᵢ = 8760
```

Die alte Größe ist nur durch `n × 8.760` beschränkt; Kaskade B zeigt den Fall.

---

## 5 Verifikation

### 5.1 Referenzlauf A/B

Beide Stände aus einem eigenen Export gebaut (`git archive befde0d` für A, Arbeitskopie für
B), jeweils mit dem mitgelieferten `Referenzlauf.csproj` — Exe und DLL damit garantiert
konsistent. **Eine gemeinsame Wegwerf-Kopie** der produktiven Datenbank, mit dem E2-Stand
auf Schemastand 18 migriert, damit beide Stände dasselbe Schema sehen. Feature-Flag
`Kaskade_Zweikanalig` **AUS**, acht Projekte.

```
Referenzlauf.exe migration <ScratchDB> …                       (17 -> 18)
A = befde0d      -> projekt <id> <Scratch>\RA\Projekt_<id> <ScratchDB>
B = befde0d + E2 -> projekt <id> <Scratch>\RB\Projekt_<id> <ScratchDB>
Referenzlauf.exe vergleich RA RB --ohne BHKW.VbhElektrisch,BHKWModul[0].VbhThermisch,BHKWModul[0].VbhElektrisch
```

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (21 Dateien, 254143 Werte)
Projekt_1018: PASS (22 Dateien, 236642 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262918 Werte)
Projekt_1024: PASS (26 Dateien, 271695 Werte)

GESAMT: PASS (2 129 527 Werte innerhalb der Toleranz)
```

**Byte-Vergleich: 191 von 194 CSV identisch.** Die drei Abweichungen sind ausschließlich die
`aggregate.csv` der drei BHKW-Projekte, und dort ausschließlich die drei neuen Schlüssel:

```
=== 1017 ===                        === 1018 ===              === 1024 ===
< BHKW.VbhElektrisch;               < …;                      < …;
> BHKW.VbhElektrisch;2842.9         > …;1031.63               > …;3519.43
< BHKWModul[0].VbhThermisch;        < …;                      < …;
> BHKWModul[0].VbhThermisch;2842.9  > …;1031.63               > …;3519.43
< BHKWModul[0].VbhElektrisch;       < …;                      < …;
> BHKWModul[0].VbhElektrisch;2842.9 > …;1031.63               > …;3519.43
```

Ohne `--ohne` meldet der Vergleich genau diese neun Einträge als Abweichung und sonst
nichts — dasselbe Bild wie bei `Heizkessel.Quellwaerme` in Etappe D4. Alle Ganglinien
(191 Vektordateien) sind in allen acht Projekten **byte-gleich**.

### 5.2 Verifikationstabelle

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| V1 | Simulationsergebnisse unverändert | `Referenzlauf.exe vergleich`, gemeinsame DB-Kopie, Flag AUS | **8/8 PASS**, 2 129 527 Werte |
| V2 | Byte-Identität der Ergebnisdateien | `cmp` je Datei | **191/194 gleich**; 3 Abweichungen = ausschließlich die neuen Schlüssel |
| V3 | Die drei Abweichungen enthalten keinen Altwert | Zeilendiff der drei `aggregate.csv` | **9 geänderte Zeilen, alle neu** |
| V4 | Migration 17 → 18 | `Referenzlauf.exe migration` auf Wegwerf-Kopie | **Schemastand 18**, Ergebnis ERFOLG |
| V5 | Spalten korrekt angelegt | `GetOleDbSchemaTable` | `Tab_ErgebnisBHKW` 22 → **23** Spalten (`VbhElektrisch` an Position 23), `Tab_ErgebnisBHKWModul` 8 → **10** (`VbhThermisch` 9, `VbhElektrisch` 10) — beide **angehängt** |
| V6 | Bestandswerte unversehrt | Vorher/Nachher-Dump derselben Datei | Zeilenzahlen 11 / 2 / 2 / 18 / 93 (`Tab_Ergebnis`, `Tab_ErgebnisBHKW`, `…Modul`, `Tab_Projekt`, `Tab_Energieanlagen`) **vorher = nachher**; die beiden BHKW-Ergebniszeilen wertgleich (57,44 · 125,82 · 2.735,19 und 13,23 · 25,61 · 661,65) |
| V7 | Kein Backfill | dito | `VbhElektrisch`/`VbhThermisch` der Bestandszeilen: **NULL**, nicht 0 |
| V8 | Doppelstart idempotent | zweiter Migrationslauf mit `--nokopie` | „Schritt 18 …: bereits erledigt", Stand bleibt 18, Spalten **nicht** doppelt |
| V9 | Vbh-Korrektur wirkt | Reflection-Harness auf `WirtschaftlichkeitCtrl.Berechne` und `BaueKwkgReihe`, A gegen B | Abschnitt 4 |
| V10 | Obergrenze 8.760 h | fünf Fälle, Abschnitt 4.5 | neue Größe **nie** über 8.760 h; alte in Kaskade B mit **9.124,34 h** darüber |
| V11 | Neue Rechnung = modulscharf, solange alle Module über dem Deckel liegen | Nachrechnung Kaskade A und B | **2/2 identisch** |
| V12 | Ergebnisreiter — Layout | Formularaufbau per Reflection, Bounds aller Steuerelemente der rechten Spalte | neue Zeile bei y = 397, Deckungszeilen auf 429/461 nachgerückt, Brennstoffblock unverändert 493/527…696 bei Seitenhöhe 721 |
| V13 | Ergebnisreiter — Beschriftungen | dieselbe Probe, deutsch und englisch | „Vbh thermisch, Summe Module" / „Thermal FLH, sum of modules"; „Vollbenutzungsstunden elektrisch:" / „Full-load hours, electric:" |
| V14 | Ressourcen in beiden Sprachen und im Designer | `grep` je Schlüssel | **3/3** in `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| V15 | `Resource.Designer.cs` ohne Dubletten | alle Eigenschaftsnamen sortiert, `uniq -d` | **0 Dubletten** (Visual Studio hat die Datei selbst neu erzeugt) |
| V16 | Build | `MSBuild WP-Plan.sln -t:Rebuild -p:Platform=x86`, Ausgabe in den Scratch-Ordner | **0 Fehler, exakt 6 Bestandswarnungen** |
| V17 | Kodierung und Zeilenenden | `file` je geänderter Datei, CR-Zählung, Suche nach U+FFFD | alle unverändert (11 × UTF-8 mit BOM, 6 × ohne), **alle Zeilen CRLF**, **0 Ersatzzeichen** |
| V18 | Produktivdatenbank nur gelesen | `Kenndaten.laccdb` vor jedem Zugriff geprüft (nicht vorhanden); alle Schreibproben auf Wegwerf-Kopien | **erfüllt** |
| V19 | `bin\` des Repos unberührt | Build ausschließlich mit `-p:OutDir=<Scratch>` | **erfüllt** |

---

## 6 Welche Größen sich ändern — und welche nicht

**Unverändert:**

- **jede Größe der Simulation** — Ganglinien, Jahressummen, Deckungsgrade,
  Brennstoffverbräuche, Emissionen. 191 von 194 Ergebnisdateien byte-identisch, die drei
  übrigen ausschließlich um die neuen Schlüssel erweitert.
- `Betriebsstunden_Gesamt` und `Betriebsstunden_Durchschnitt` — Werte identisch, nur die
  **Beschriftung** ist berichtigt.
- **Kapitalwert, Annuität, Amortisation, interner Zinsfuß, Gestehungskosten und
  Sensitivität aller Projekte OHNE gepflegten KWKG-Zuschlagssatz** — und das sind auf dem
  heutigen Datenbestand alle.
- Die Vbh-Größe selbst bei **Einmodulanlagen**: Summe und leistungsgewichtetes Mittel sind
  dort dieselbe Zahl.

**Verändert:**

- die **Bemessungsgrundlage der KWKG-Deckelung**: elektrische statt thermischer Vbh,
  leistungsgewichtet statt summiert;
- die **Bezugsmenge von Σ P_el** für den 500-kW-Guard und für die Vbh-Rechnung:
  Anlagenzeilen statt aller Gerätezeilen — auf dem Bestand betrifft das **Projekt 1024**
  (21 kW statt 546,4 kW);
- daraus folgend **KWKG-Erlös, Kapitalwert und Amortisation** aller Projekte **mit**
  gepflegtem KWKG-Satz, sobald sie mehr als ein BHKW-Modul führen oder Gerätezeilen ohne
  Anlagenzeile mitschleppen. Zahlen in Abschnitt 4.
- **Richtung der Korrektur:** Der Zuschlag fällt bei Kaskaden **höher** aus als bisher, nicht
  niedriger — das Konzept nahm das Gegenteil an (Abschnitt 1.2, Befund 2).

---

## 7 Offene Punkte

### Für Etappe E6

1. **Modulscharfe Deckelung.** Die projektweite Rechnung stimmt mit der modulscharfen
   überein, solange alle Module über oder alle unter dem Jahresdeckel liegen. Liegt ein Teil
   darüber und ein Teil darunter, weicht sie ab — der Deckel ist nach § 8 je Anlage zu
   führen. Die Daten dafür liegen seit dieser Etappe je Modul in
   `Tab_ErgebnisBHKWModul.VbhElektrisch`.
2. **Das Vbh-Kontingent gilt je Anlage.** `p.KwkgVbhKontingent` (30.000 h) wird heute
   projektweit geführt. Bei zwei Modulen stehen dem Projekt gesetzlich 2 × 30.000 h zu; die
   neue leistungsgewichtete Rechnung bildet das näherungsweise ab (halbe Vbh je Jahr bei
   gleichem Kontingent), exakt wird es erst modulscharf.
3. **Zuschlagssatz je Leistungsklasse.** Ein 2 × 250-kW-Projekt fällt modulscharf in eine
   andere Klasse als ein 500-kW-Modul. Der Katalog aus E1 hält die Sätze bereit.

### Aus dieser Etappe entstanden

4. **Die Modultabelle des Ergebnisreiters zeigt die neuen Größen nicht.**
   `dataGridView_BHKW` führt Nummer, Name, Wärme und Strom; die beiden Vbh-Spalten je Modul
   sind persistiert, aber nur im Bericht sichtbar. Zwei Spalten mehr wären ein
   Designer-Eingriff und wurden zurückgestellt.
5. **`Betriebsstunden_Gesamt` bleibt ein irreführender Spaltenname** in
   `Tab_ErgebnisBHKW`. Umbenennen hieße Schema, Schreibweg, Leseweg und alle
   Referenzlauf-Vergleichsschlüssel anzufassen — für einen reinen Namensgewinn zu viel. Die
   Anzeige ist berichtigt, der Code an drei Stellen kommentiert.
6. **Echte Betriebsstunden gibt es im Modell nicht.** Wer eine Wartung „je Betriebsstunde"
   bemisst (L7, Etappe E3), rechnet mit `VbhThermisch` als Näherung. Das ist zu **kennzeichnen**,
   sobald der Dialog entsteht — sonst wandert die Verwechslung in die Kostenrechnung.
7. **Der 500-kW-Guard prüft die Projektsumme.** Nach § 6 KWKG bezieht sich die Grenze auf
   die **Anlage**. Bei zwei 300-kW-Modulen wäre die Frage, ob 600 kW oder 2 × 300 kW gilt —
   fachlich zu klären, spätestens mit E6.
8. **Kein Referenzprojekt deckt den Kaskadenfall ab.** Die Wirkung dieser Etappe ist
   ausschließlich an präparierten Kopien belegbar. Für einen belastbaren Regressionstest
   fehlt ein Referenzprojekt mit mehr als einem BHKW-Modul — dieselbe Lücke, die schon für
   `Heizkessel.Quellwaerme` festgehalten ist.
9. **Die drei BHKW-Projekte liefern keinen Kapitalwert**, weil ihre Energiekosten mangels
   Arbeitspreis nicht bestimmbar sind. Das ist eine Datenlücke, kein Codefehler — sie
   verhindert aber, dass die Kapitalwert- und Amortisationswirkung an einem echten Projekt
   gezeigt werden kann.

### Referenzbasis

10. **Ein neuer Basis-Freeze steht aus.** `2026-08-16_B4` ist gegen den heutigen Datenstand
    nicht mehr vergleichbar (bereits in E1 belegt), und E2 fügt drei Schlüssel hinzu. Der
    Freeze gehört nach E8; bis dahin läuft jeder Vergleich als A/B auf einer gemeinsamen
    Wegwerf-Kopie.

---

# Nachtrag: 500-kW-Grenze je Anlage

**Stand: 19.08.2026.** Nutzerentscheidung vom 19.08.2026, umgesetzt auf dem Stand
`0892444` (= E2). Damit ist der offene Punkt **7** aus Abschnitt 7 erledigt.

**Ergebnis in drei Sätzen.** Die Ausschreibungsgrenze des KWKG greift ab jetzt für **jede
BHKW-Anlage einzeln** statt für die Projektsumme; Anlagen darüber verlieren ihren
Zuschlaganteil, die übrigen behalten ihn. Auf dem heutigen Datenbestand ändert sich
**nichts** — kein Referenzprojekt führt mehr als eine Anlage, und ohne gepflegten
KWKG-Satz rechnet die Reihe ohnehin nicht (8/8 PASS, 194/194 CSV byte-identisch, Flag AUS
**und** AN). An präparierten Kopien springt der Zuschlag von 0 € auf bis zu **606.318,99 €
Barwert** (zwei Anlagen zu je 300 kW).

---

## N1 Warum die Änderung

Der Guard bei `WirtschaftlichkeitCtrl.cs:919-928` (Stand `0892444`) prüfte

```csharp
double pelKW = PelKW(v.IdProjekt);           // Σ P_el ALLER Anlagen des Projekts
if (pelKW > KWKG_MAX_LEISTUNG_KW) { … Bonus = 0 … }
```

Das Gesetz stellt aber auf die **einzelne KWK-Anlage** ab: Oberhalb von 500 kW
elektrischer Leistung gibt es den Zuschlag nur noch über eine Ausschreibung
(§ 8a KWKG i.V.m. KWKAusV), und dieser Weg ist in EPOS-Plan nicht bedienbar. Zwei Module
zu je 300 kW sind damit **zwei förderfähige Anlagen**, keine nicht förderfähige
600-kW-Anlage — der Altstand nahm einer solchen Kaskade den Zuschlag vollständig.

Es ist derselbe Fehlertyp, den E2 schon einmal behoben hat (Abschnitt 1.3): eine Größe,
die je Anlage gilt, wurde über das Projekt summiert. E2 hat die **Bezugsmenge** der Summe
berichtigt (Anlagen- statt Gerätezeilen), dieser Nachtrag die **Bezugsebene** der Prüfung.

---

## N2 Was umgesetzt wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | Guard prüft **jede Anlage einzeln**, drei unterschiedene Fälle (nicht bestimmbar / alle über der Grenze / einzelne über der Grenze) | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:943-985` |
| 2 | **Zwischenlösung**: Bonus und Vbh um die ausgeschlossenen Anlagen bereinigt | `WirtschaftlichkeitCtrl.cs:1011-1030` |
| 3 | Prüfung und bereinigte Bezugsgrößen (`KwkgAnlagenauswahl`, `Anlagenauswahl`) | `WirtschaftlichkeitCtrl.cs:1180-1281` |
| 4 | Zuordnung Anlagenzeile ↔ Ergebnis-Modulzeile (Bezeichner, ersatzweise Reihenfolge) | `WirtschaftlichkeitCtrl.cs:1283-1308` |
| 5 | Anlagenzeilen mit Bezeichner und P_el lesen, je Lauf gecacht (`BhkwAnlage`, `LiesBhkwAnlagen`) | `WirtschaftlichkeitCtrl.cs:1165-1178`, `:1310-1348` |
| 6 | Grenzwert **aus dem Katalog** statt aus der Konstanten, Konstante als Rückfallebene | `WirtschaftlichkeitCtrl.cs:1219-1232` (`AusschreibungsgrenzeKW`), `:59-68` (Konstante, jetzt dokumentierte Rückfallebene) |
| 7 | Neuer Katalogschlüssel `KWKG_AUSSCHREIBUNG_GRENZE_KW` = 500 kW, Klasse `KWKG`, ab 2020 | `Allgemein/DbWerte.cs:652-662` (Konstante), `Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs:536-541` (Seed) |
| 8 | Caches des neuen Wegs beim Laufbeginn leeren | `WirtschaftlichkeitCtrl.cs:644` |
| 9 | Drei Ressourcenschlüssel in beiden Sprachen samt Designer und Katalognachtrag | `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`, `Allgemein/Simulation/Lokalisierung_Katalog.md` |

**Sechs Quelldateien geändert** (337 Zeilen zugefügt, 13 entfernt) **und drei Dokumente**:
dieses Protokoll, `W4_Umsetzungsstand.md`, `Allgemein/Simulation/Lokalisierung_Katalog.md`.
**Keine Migration** — der Katalog entsteht und wird eingesät über
`WirtschaftlichkeitCtrl.StelleTabellenSicher` → `GesetzKatalog.StelleKatalogSicher`
(Abschnitt N4, Probe N7).

---

## N3 Die Zwischenlösung — und was sie nicht löst

Die Zuschlagsermittlung rechnet **projektweit**; modulscharf wird sie erst mit **E6**.
Bis dahin werden die Bezugsgrößen vor der Reihenbildung **bereinigt**:

```
anteil   = Σ Strom der förderfähigen Anlagen / Σ Strom aller Anlagen
bonusVoll → bonusVoll · anteil
vbh       → Σ Strom der förderfähigen Anlagen · 1000 / Σ P_el derselben
```

Sind **alle** Anlagen über der Grenze, entfällt der Zuschlag wie bisher. Ist **keine**
Anlage ausgeschlossen, bleiben Bonus und Vbh **unangetastet** — dann rechnet der Code
Zeile für Zeile wie der Vorgängerstand. Genau das macht den Regressionsnachweis in
Abschnitt N5 möglich.

**Die vier Grenzen der Zwischenlösung** (alle in E6 aufzulösen):

1. **Jahresdeckel und 30.000-h-Kontingent laufen weiter über EINE gemeinsame Vbh-Größe.**
   Die bereinigte Vbh ist das leistungsgewichtete Mittel der verbleibenden Anlagen.
   Solange alle verbleibenden Anlagen über oder alle unter dem Jahresdeckel liegen, ist
   das mit der modulscharfen Rechnung identisch (Beweis in Abschnitt 3.2); liegt ein Teil
   darüber und ein Teil darunter, weicht es ab. Der Ausschluss ändert daran nichts, er
   verkleinert nur die Menge, über die gemittelt wird.
2. **Der Zuschlagssatz bleibt einer je Projekt.** Nach § 7 hängt er von der
   Leistungsklasse der **Anlage** ab; eine verbleibende 200-kW-Anlage bekäme einen anderen
   Satz als eine 50-kW-Anlage. Die Katalogschlüssel dafür liegen seit E1 bereit
   (`KWKG_ZUSCHLAG_*`), gelesen wird noch keiner.
3. **Der Bonus wird über die STROMMENGE gekürzt, nicht über die Vergütungsregel.** Bei
   gepflegter Stundenreihe (`StromMatrix`) entsteht `bonusVoll` aus dem Split
   Eigenstrom/Einspeisung des **ganzen Projekts**; der Anteilsfaktor unterstellt, dass sich
   dieser Split auf die verbleibenden Anlagen gleich verteilt. Modulscharfe Stundenreihen
   je Anlage gibt es nicht — dieselbe Näherung, die die Rechnung schon heute für den
   Jahresdeckel macht.
4. **Fristen und Heizöl-Ausschluss bleiben projektweit.** § 6 (Stichtag, Realisierungs-
   frist) und der Heizöl-Ausschluss gelten ebenfalls je Anlage, werden aber weiterhin aus
   **einem** Parametersatz je Projekt beziehungsweise über `COUNT(*)` über alle
   Gerätezeilen geprüft (Abschnitt N6).

**Wenn die Zuordnung nicht gelingt**, fällt der Guard auf die Projektsumme zurück — den
Weg des Altstands — und sagt das im Hinweis. Das ist der konservative Zweig: Er schließt
eher zu viel aus als zu wenig. Er greift, wenn das Projekt keine BHKW-**Anlagenzeile**
führt (dann liefert `LiesBhkwLeistungKW` seine Gerätezeilen-Summe), wenn der Lauf keine
Modulzeilen hinterlassen hat, oder wenn sich Anlagen und Modulzeilen weder über den
Bezeichner noch über die Reihenfolge paaren lassen.

> **Warum die Zuordnung zwei Wege braucht.** Der Regelfall ist der Bezeichner:
> `SimulationRunner` schreibt ihn als `Tab_ErgebnisBHKWModul.Modul`. Im echten Bestand
> passte er vor dem Referenzlauf trotzdem nicht — Projekt 1018 führte die Anlage
> „EC-POWER XRGI 15" und die Modulzeile „EC_Power_20kw.el_Gas" aus einem älteren Lauf.
> Deshalb der zweite Weg über die Reihenfolge bei gleicher Anzahl; die Modulzeilen
> entstehen in der Reihenfolge von `SimulationControl.BHKW_Liste_Laden`.

**Die angezeigte Vbh bleibt die der ganzen Anlage.** `e.VbhElektrisch` (Spalte
`KWKGVbhElektrisch`, Zeile „Vbh elektrisch (KWKG-Basis)") wird vor und unabhängig vom
Guard gebildet — sie steht auch dann im Ergebnis, wenn gar kein KWKG-Satz gepflegt ist.
Bei ausgeschlossenen Anlagen weicht sie deshalb von der intern verwendeten, bereinigten
Größe ab. Das ist bewusst so: Sie unverändert zu lassen hält den Regressionsnachweis
sauber, und die bereinigte Größe hat ohne modulscharfe Rechnung keinen eigenständigen
Aussagewert. Fall (d) in Abschnitt N5 zeigt den Unterschied (2.500 h gegen 1.000 h).

---

## N4 Der Grenzwert kommt aus dem Katalog

Der E1-Katalog kannte die 500 kW **nicht** — er führt die Leistungsstufen des § 7
(50/100/250/2.000 kW), nicht die Ausschreibungsschwelle des § 8a. Der Schlüssel ist
deshalb neu:

| Schlüssel | Klasse | JahrVon | Wert | Einheit | Status | Quelle |
|---|---|---|---|---|---|---|
| `KWKG_AUSSCHREIBUNG_GRENZE_KW` | KWKG | 2020 | 500 | kW | GESICHERT | KWKG 2025 § 8a i.V.m. KWKAusV — Ausschreibungspflicht je Anlage |

Der Seed wächst damit von 182 auf **183 Zeilen**. Gelesen wird über
`GesetzKatalog.Wert(…, Förderbeginn)` — dieselbe Stichtagsregel wie beim Jahresdeckel;
der Förderbeginn ist das Inbetriebnahmejahr. Fehlt der Schlüssel, gilt die Konstante
`KWKG_MAX_LEISTUNG_KW` mit demselben Wert.

**Erreicht der Seed eine Bestandsdatenbank?** Ja — und zwar aus einem Grund, der beim
Entwurf nicht selbstverständlich war: `StelleKatalogSicher` sät nur bei
`COUNT(*) = 0` ein, würde einen bestehenden Katalog also nicht ergänzen. Die produktive
`Kenndaten.accdb` vom 19.08.2026 hat aber **überhaupt keine** `Tab_Gesetzesparameter` —
E1 ist dort noch nie gelaufen. Beim nächsten Wirtschaftlichkeitslauf legt
`StelleTabellenSicher` die Tabelle an und sät sie vollständig ein, den neuen Schlüssel
eingeschlossen (Probe N7, nachgemessen: 183 Zeilen, ID 24).

> **Was das nicht abdeckt:** Eine Datenbank, deren Katalog **vor** diesem Nachtrag
> eingesät wurde, bekommt den Schlüssel nicht nachgereicht — dort gilt die Konstante
> (wertgleich, Probe N9). Wer den Wert dort pflegbar haben will, legt die Zeile über
> Administration → „Gesetzliche Parameter…" an. Eine additive Nachsaat fehlender Schlüssel
> wäre die allgemeine Lösung; sie würde aber auch bewusst gelöschte Zeilen wieder
> auferstehen lassen und gehört deshalb als eigener Vorgang entschieden
> (`W4_Umsetzungsstand.md`, Abschnitt 5).

---

## N5 Wirkung — mit Zahlen

**Gemeinsame Grundlage:** Wegwerf-Kopien der produktiven `Kenndaten.accdb` vom
19.08.2026. KWKG-Parameter der Probe: Zuschlag Eigenstrom 4,00 ct/kWh, Einspeisung
8,00 ct/kWh, Kontingent 30.000 h, Jahresdeckel aus der KWKG-2025-Staffel, Zins 3,0 %,
Betrachtungszeitraum 20 a, Stichtag und Inbetriebnahme 01.01.2026, keine Stundenreihe
(also Eigenstromsatz auf die Gesamtmenge — dieselbe Fahrweise wie in Abschnitt 4).

### N5.1 Bestandsprojekte — unverändert

| Projekt | Anlagen | Strom [MWh/a] | Vbh [h/a] | KWKG Jahr 1 **alt** | **neu** | Barwert 20 a **alt** | **neu** |
|---|---|---|---|---|---|---|---|
| 1017 | 1 × 10,0 kW | 28,43 | 2.842,90 | 1.137,20 €/a | 1.137,20 €/a | 10.068,70 € | 10.068,70 € |
| 1018 | 1 × 14,5 kW | 14,96 | 1.031,63 | 598,40 €/a | 598,40 €/a | 8.902,68 € | 8.902,68 € |
| 1024 | 1 × 21,0 kW | 73,91 | 3.519,43 | 0,00 €/a ¹ | 0,00 €/a ¹ | 0,00 € | 0,00 € |
| 1007, 1008, 1011, 1021, 1023 | kein BHKW-Ergebnis | 0 | 0 | 0,00 €/a | 0,00 €/a | 0,00 € | 0,00 € |

¹ Heizöl-Ausschluss (IBN 2026), in **beiden** Ständen und aus demselben Grund.

**Δ = 0,00 € in jeder Zeile.** Die Werte sind zugleich die aus Abschnitt 4.1 — E2 und
dieser Nachtrag verändern an Einanlagenprojekten nichts.

### N5.2 Präparierte Kopien — die Wirkung

Basis ist Projekt 1018 (Gas-BHKW). Anlagen-, Geräte- und Ergebniszeilen sind **auf
Datenebene** präpariert: `Tab_BHKW.Pel`, eine zweite `Tab_Energieanlagen`-Zeile, eine
zweite `Tab_ErgebnisBHKWModul`-Zeile und die Stromsummen. **Nicht neu simuliert** — der
Rechenkern ist von diesem Nachtrag nicht berührt (Abschnitt N6, V1/V2), und die geprüfte
Kette hängt ausschließlich an (P_el je Anlage, Strom je Modul).

| Fall | Anlagen | Strom je Anlage | Vbh Anlage | Vbh **bereinigt** | Jahr 1 **alt** | **neu** | Barwert 20 a **alt** | **neu** |
|---|---|---|---|---|---|---|---|---|
| **(a)** zwei Anlagen à 300 kW | 300 + 300 kW | 900 + 900 MWh | 3.000 h | — (nichts ausgeschlossen) | **0,00 €** | **72.000,00 €** | **0,00 €** | **606.318,99 €** |
| **(b)** 600 kW + 200 kW, gleiche Vbh | 600 + 200 kW | 1.800 + 600 MWh | 3.000 h | 3.000 h | **0,00 €** | **24.000,00 €** | **0,00 €** | **202.106,33 €** |
| **(c)** eine Anlage 600 kW | 600 kW | 1.800 MWh | 3.000 h | 0 h (alle aus) | 0,00 € | 0,00 € | 0,00 € | 0,00 € |
| **(d)** 600 kW + 200 kW, **ungleiche** Vbh | 600 + 200 kW | 1.800 + 200 MWh | 2.500 h | **1.000 h** | **0,00 €** | **8.000,00 €** | **0,00 €** | **119.019,80 €** |

Nachgerechnet: In Fall (a) sind 3.000 h ≤ Jahresdeckel 2026 (3.300 h), also volle
72.000 €/a (1.800 MWh × 4 ct/kWh); ab 2028 greift der sinkende Deckel und das
30.000-h-Kontingent ist nach zwölf Jahren erschöpft — der Barwert 606.318,99 € ist die
Summe dieser zwölf abgezinsten Jahresbeträge. Fall (b) ist derselbe Verlauf mit dem
Anteilsfaktor 600/2.400 = 0,25: **exakt ein Viertel** (202.106,33 € = 606.318,99 € / 3
gegenüber Fall a, weil dort 1.800 statt 600 MWh förderfähig sind). Fall (d) zeigt die
bereinigte Vbh: 200 MWh auf 200 kW sind 1.000 h, weit unter jedem Jahresdeckel — die
Reihe läuft über alle 20 Jahre durch (8.000 €/a × Rentenbarwertfaktor 14,8775 =
119.019,80 €).

Fall (c) ist die **Gegenprobe**: Eine echte 600-kW-Anlage bekommt weiterhin nichts. Die
Änderung macht keine nicht förderfähige Anlage förderfähig, sie hört nur auf, kleine
Anlagen für die Größe ihrer Nachbarn zu bestrafen.

### N5.3 Die Meldung

| Fall | Meldung (DE) |
|---|---|
| einzelne Anlagen über der Grenze | „KWKG: Über der Ausschreibungsgrenze von 500 kW und deshalb ohne Zuschlag: BHKW Modul 1 (600 kW) (der Weg über eine Ausschreibung nach § 8a KWKG/KWKAusV ist nicht abgebildet). Die übrigen Anlagen mit zusammen 200 kW rechnen weiter." |
| alle Anlagen über der Grenze | „KWKG: Jede BHKW-Anlage des Projekts liegt über der Ausschreibungsgrenze von 500 kW (BHKW Modul 1 (600 kW)) — der Zuschlag wäre nur über eine Ausschreibung nach § 8a KWKG/KWKAusV zu erlangen; Bonus = 0." |
| Leistung je Anlage nicht ermittelbar | „KWKG: Σ installierte BHKW-Leistung 800 kW über der Ausschreibungsgrenze von 500 kW; die Leistung je Anlage ließ sich nicht ermitteln, deshalb greift die Grenze ersatzweise auf die Projektsumme; Bonus = 0." |

Die alte Meldung nannte nur „Σ installierte BHKW-Leistung 800 kW > 500 kW" — sie sagte
weder, **welche** Anlage das Problem ist, noch dass die übrigen weiterrechnen. Alle drei
Texte stehen in beiden Sprachen im Katalog `MyResource` (Probe N10/N11).

---

## N6 Verifikation

### N6.1 Referenzlauf A/B

Beide Stände aus eigenen Exporten gebaut (`git archive 0892444` für A, Arbeitskopie für
B), jeweils mit dem mitgelieferten `Referenzlauf.csproj`. **Eine gemeinsame Wegwerf-Kopie**
der produktiven Datenbank, mit dem B-Stand migriert, danach von beiden Ständen gelesen.
Acht Projekte, Feature-Flag `Kaskade_Zweikanalig` **AUS und AN**.

```
Flag AUS                              Flag AN
Projekt_1007: PASS (29 Dateien)       Projekt_1007: PASS (29 Dateien)
Projekt_1008: PASS (21 Dateien)       Projekt_1008: PASS (21 Dateien)
Projekt_1011: PASS (29 Dateien)       Projekt_1011: PASS (29 Dateien)
Projekt_1017: PASS (21 Dateien)       Projekt_1017: PASS (21 Dateien)
Projekt_1018: PASS (22 Dateien)       Projekt_1018: PASS (22 Dateien)
Projekt_1021: PASS (21 Dateien)       Projekt_1021: PASS (21 Dateien)
Projekt_1023: PASS (25 Dateien)       Projekt_1023: PASS (25 Dateien)
Projekt_1024: PASS (26 Dateien)       Projekt_1024: PASS (26 Dateien)

GESAMT: PASS (2 129 527 Werte)        GESAMT: PASS (2 129 527 Werte)
```

**Byte-Vergleich: 194 von 194 CSV identisch, in beiden Flag-Stellungen** — anders als bei
E2 gibt es diesmal nicht einmal einen neuen Ergebnisschlüssel. Unter
`Allgemein/Simulation/` ist keine **Codedatei** geändert (`git diff --name-only` liefert
dort nur `Lokalisierung_Katalog.md`).

### N6.2 Verifikationstabelle

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| N1 | Simulationsergebnisse unverändert, Flag AUS | `Referenzlauf.exe vergleich`, gemeinsame DB-Kopie | **8/8 PASS**, 2 129 527 Werte |
| N2 | dito, Flag AN (`Kaskade_Zweikanalig` für alle 16 Projekteinstellungen gesetzt) | dito | **8/8 PASS**, 2 129 527 Werte |
| N3 | Byte-Identität der Ergebnisdateien | `cmp` je Datei, beide Flag-Stellungen | **194/194 gleich**, 0 Abweichungen |
| N4 | Wirtschaftlichkeitswerte der 8 Referenzprojekte unverändert | Reflection-Harness auf `BaueKwkgReihe`/`VbhElektrisch`/`PelKW`, A gegen B, unmigrierte **und** migrierte Kopie | **8/8 wertgleich**, Abschnitt N5.1 |
| N5 | Wirkung bei zwei Anlagen à 300 kW | präparierte Kopie, A gegen B | 0,00 € → **606.318,99 €** Barwert |
| N6 | Wirkung bei 600 kW + 200 kW | präparierte Kopien (gleiche und ungleiche Vbh) | 0,00 € → **202.106,33 €** bzw. **119.019,80 €** |
| N7 | Einzelne 600-kW-Anlage bleibt ohne Zuschlag | präparierte Kopie, A gegen B | **0,00 € in beiden Ständen** |
| N8 | Katalogschlüssel entsteht auf einer Bestandsdatenbank | `StelleTabellenSicher` auf Wegwerf-Kopie, danach `SELECT` | Tabelle neu, **183 Zeilen**, `KWKG_AUSSCHREIBUNG_GRENZE_KW` = 500 kW (ID 24) |
| N9 | Der Wert wird wirklich aus dem Katalog gelesen | Katalogzeile auf **250** gesetzt | Meldung und Prüfung folgen: „…Ausschreibungsgrenze von 250 kW…" |
| N10 | Rückfallebene ohne Katalogzeile | Katalogzeile gelöscht | Grenze wieder **500 kW**, Ergebnis wertgleich |
| N11 | Rückfall auf die Projektsumme | Anlagenzeilen des Projekts gelöscht | Ergebnis wie Altstand (Bonus 0), Hinweis nennt den Ersatzweg |
| N12 | Zuordnung über die Reihenfolge, wenn die Bezeichner abweichen | Bestandsprojekt 1018 („EC-POWER XRGI 15" gegen „EC_Power_20kw.el_Gas") | zugeordnet, Ergebnis wertgleich zu A |
| N13 | Ressourcen in beiden Sprachen | Harness mit `CurrentUICulture = en-US` | DE- und EN-Meldung erscheinen, **Zahlen byte-gleich** (202 106,3288 in beiden) |
| N14 | Ressourcen in beiden `.resx` und im Designer | `grep` je Schlüssel | **3/3** in `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| N15 | Build | `MSBuild WP-Plan.sln -t:Rebuild -p:Platform=x86`, Ausgabe in den Scratch-Ordner | **0 Fehler, exakt 6 Bestandswarnungen** |
| N16 | Kodierung und Zeilenenden | `file` je geänderter Datei, CR-Zählung, Suche nach U+FFFD | unverändert (5 × UTF-8 mit BOM, 1 × ohne), **alle Zeilen CRLF**, **0 Ersatzzeichen** |
| N17 | Produktivdatenbank nur gelesen | `Kenndaten.laccdb` vor dem Kopieren geprüft (nicht vorhanden); alle Proben auf Wegwerf-Kopien | **erfüllt** |
| N18 | `bin\` des Repos unberührt | jeder Build ausschließlich mit `-p:OutDir=<Scratch>` | **erfüllt** |

---

## N7 Dieselbe Verwechslung an anderen Stellen — nur berichtet

Gesucht wurde nach `KWKG_MAX_LEISTUNG_KW`, `LiesBhkwLeistungKW`, Leistungsklassen-Logik,
Heizöl-Ausschluss und Stichtagsprüfung. **Nichts davon ist mitgeändert.**

| Fundstelle | Befund | Bewertung |
|---|---|---|
| `BhkwMitHeizoel` (`WirtschaftlichkeitCtrl.cs:1400-1415`) | `SELECT COUNT(*) FROM Tab_BHKW … WHERE ID_Projekt = ? AND Kategorie = 2` — **zwei** Fehler auf einmal: projektweit statt je Anlage **und** über die **Gerätezeilen** statt die Anlagenzeilen | **Der gravierendste Restbefund.** Ein einziges Öl-BHKW im Katalogbestand des Projekts nimmt allen Anlagen den Zuschlag — auch wenn dazu nie eine Anlagenzeile entstand. Das ist exakt der Fehler, den E2 für `LiesBhkwLeistungKW` behoben hat, unbehoben an dieser Stelle. Auf dem heutigen Bestand betrifft es Projekt 1024 (Öl-BHKW, aber nur eine Anlage — Ergebnis zufällig richtig). |
| `p.KwkgStichtag`, `p.KwkgInbetriebnahme` (`:911-932`) | ein Datumspaar je **Projekt**; § 6 KWKG gilt je Anlage | Bei gemischten Inbetriebnahmen ist die Prüfung entweder zu streng oder zu großzügig. Fachlich zu klären, gehört zu E6. |
| `p.KwkgVbhKontingent` (`:1033`) | 30.000 h je **Projekt**; nach § 8 Abs. 1 stehen sie **jeder Anlage** zu | Bereits als E2-Punkt 2 festgehalten; die leistungsgewichtete Vbh bildet es näherungsweise ab. |
| `p.KwkgBonus` / `KwkgBonusEinspeisung` | **ein** Satz je Projekt; § 7 staffelt nach der Leistungsklasse der Anlage | E6, Punkt 3 in Abschnitt 7. Katalogschlüssel liegen seit E1 bereit. |
| `GESETZ_KWKG_LEISTUNGSSTUFE_1…4`, `GESETZ_KWKG_ZUSCHLAG_*` | im Katalog gepflegt, **von keiner Codezeile gelesen** (`grep`, 0 Treffer außerhalb von `DbWerte.cs` und `GesetzKatalog.cs`) | Erwartungsgemäß — die Leistungsklassen-Logik entsteht erst mit E6. |
| `GESETZ_STROMST_GRENZE_BEFREIUNG` (2.000 kW, § 9 Abs. 1 Nr. 3 StromStG) | ebenfalls ungelesen; auch diese Grenze ist eine **Anlagen**-Nennleistung | Vorsorglich vermerkt: Bei der Umsetzung in E4 nicht wieder die Projektsumme dagegen prüfen. |
| `LiesBhkwLeistungKW` / `PelKW` (`:1120-1160`) | Σ P_el über alle Anlagen des Projekts | **Kein Fehler.** Als Nenner der leistungsgewichteten Vbh ist die Projektsumme genau richtig; nur die Schwellenprüfung durfte sie nicht verwenden. |

---

# Nachtrag 2: Heizöl-Ausschluss je Anlage

**Stand: 19.08.2026.** Umgesetzt auf dem Stand `006e780` (= E2 + Nachtrag 1). Damit ist der
**Restbefund 1** aus Abschnitt N7 erledigt — der dort als „der gravierendste" bezeichnete.

**Ergebnis in drei Sätzen.** Der Heizöl-Ausschluss des KWKG greift ab jetzt für **jede
BHKW-Anlage einzeln** und stützt sich auf die **installierten Anlagen** statt auf die
Gerätezeilen des Projekts; ein Öl-BHKW verliert seinen Zuschlaganteil, ein daneben
stehendes Gas-BHKW behält ihn. Auf dem heutigen Datenbestand ändert sich **kein einziger
Zahlenwert** (8/8 PASS ×2, 194/194 CSV byte-identisch, alle acht Wirtschaftlichkeitswerte
gleich) — auch nicht bei Projekt 1024, dessen Ausschluss sich als **echter Befund**
erwiesen hat. An präparierten Kopien springt der Zuschlag von 0 € auf bis zu
**202.106,33 €** Barwert.

---

## N2-1 Warum die Änderung

`BhkwMitHeizoel` (Stand `006e780`, `WirtschaftlichkeitCtrl.cs:1400-1415`) lautete

```sql
SELECT COUNT(*) FROM Tab_BHKW AS b
INNER JOIN Tab_Brennstoff_Stamm AS bs ON b.Brennstoff = bs.ID
WHERE b.ID_Projekt = ? AND bs.ID_Kategorie = 2
```

Zwei Mängel in einer Zeile:

1. **Projektweit statt je Anlage.** Ein einziges Öl-BHKW nahm **allen** Anlagen des
   Projekts den Zuschlag. Der Ausschluss fossiler flüssiger Brennstoffe betrifft aber die
   **einzelne Anlage** — nichts daran wirkt auf das Nachbarmodul.
2. **Über die Gerätezeilen statt über die installierten Anlagen.** `Tab_BHKW` nimmt jede
   Katalogübernahme auf, auch solche, zu denen **nie eine Anlagenzeile** in
   `Tab_Energieanlagen` entstand. Genau diesen Fehler hat E2 für `LiesBhkwLeistungKW`
   behoben (Abschnitt 1.3: Projekt 1024 kam auf 546 kW statt 21 kW); an dieser Stelle war
   er unbehoben.

Es ist damit zum **dritten Mal** derselbe Fehlertyp in derselben Methodenfamilie: eine
Größe, die je Anlage gilt, wurde über das Projekt gebildet. E2 hat die **Bezugsmenge** der
Leistungssumme berichtigt, Nachtrag 1 die **Bezugsebene** der Ausschreibungsgrenze, dieser
Nachtrag beides zugleich für den Heizöl-Ausschluss.

**Was ausdrücklich NICHT geändert wurde.** Die Rechtsgrundlage bleibt die Sekundärquelle
(`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`, Abschnitt 6 Punkt 3 — BBH-Blog vom
13.02.2025, kein Primärbeleg), und der Ausschluss greift unverändert **nur für erkennbare
Neuanlagen** (Inbetriebnahme ≥ 2025). Bestandsanlagen rechnen wie bisher mit ihrem
historischen Satz weiter, Öl-Anlagen ohne Inbetriebnahmedatum bekommen wie bisher nur einen
Hinweis. Korrigiert ist ausschließlich der **Bezug**.

---

## N2-2 Was umgesetzt wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | Guard prüft Heizöl **je Anlage**, vier unterschiedene Fälle (nicht bestimmbar / alle ausgeschlossen / Teilausschluss / kein Ausschluss) | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:988-1076` |
| 2 | Brennstoffart je Anlage im Anlagenbefund (`BhkwAnlage.Heizoel`) | `WirtschaftlichkeitCtrl.cs:1246-1265` |
| 3 | Auswahl trägt **zwei Gründelisten und einen Ausschlusszähler** — jede Anlage fehlt in den Bezugsgrößen genau einmal | `WirtschaftlichkeitCtrl.cs:1267-1360` |
| 4 | `Anlagenauswahl` filtert beide Gründe in **einem** Durchlauf; Parameter `heizoelAusschliessen` trägt die Neuanlagen-Regel | `WirtschaftlichkeitCtrl.cs:1362-1412` |
| 5 | Anlagenzeilen lesen jetzt zusätzlich `ID_Carrier` und `Brennstoff` und lösen die Kategorie auf | `WirtschaftlichkeitCtrl.cs:1455-1513` (`LiesBhkwAnlagen`) |
| 6 | Kategorieauflösung Energieträger → Brennstoff → Kategorie, mit Rückfall auf die Gerätezeile | `WirtschaftlichkeitCtrl.cs:1515-1570` (`Ganzzahl`, `BrennstoffKategorie`, `LiesZuordnung`) |
| 7 | Kategorie „Öl" als benannte Konstante statt als SQL-Literal `2` | `WirtschaftlichkeitCtrl.cs:80-97` (`BRENNSTOFF_KATEGORIE_OEL`) |
| 8 | `BhkwMitHeizoel` bleibt, aber **nur noch als Rückfallebene**, und dokumentiert das | `WirtschaftlichkeitCtrl.cs:1624-1650` |
| 9 | Zwei neue Katalogcaches je Lauf leeren — in `Berechne` **und** in `BaueVerlauf` | `WirtschaftlichkeitCtrl.cs:674`, `:745` |
| 10 | Sechs Ressourcenschlüssel in beiden Sprachen samt Designer und Katalognachtrag | `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`, `Allgemein/Simulation/Lokalisierung_Katalog.md` |

**Vier Quelldateien geändert** (380 Zeilen zugefügt, 56 entfernt) **und drei Dokumente**:
dieses Protokoll, `W4_Umsetzungsstand.md`, `Allgemein/Simulation/Lokalisierung_Katalog.md`.
**Keine Migration, kein neuer Katalogschlüssel** — die Änderung liest ausschließlich
vorhandene Spalten.

---

## N2-3 Woran die Brennstoffart hängt — und warum nicht am `pricing_model`

Die Aufgabe stellte die Frage, ob die Kategorie-Zuordnung (`ID_Kategorie = 2`) anderswo
robuster gelöst ist, etwa über `ID_Carrier` und `energy_carrier.pricing_model`. Der Befund
aus dem Schema der produktiven Datenbank (19.08.2026) trennt die beiden Teile der Frage:

**Die Bezugsebene wechselt — auf `Tab_Energieanlagen.ID_Carrier`.** Seit dem
Energieträger-Umbau trägt jede Anlagenzeile einen Trägerverweis, und er ist die
**maßgebliche** Zuordnung: Aus ihm bildet die Anwendung Brennstoffverbrauch, Kosten und
Emissionen (`SimulationControl.EnergietraegerZuordnungLesen`, `KostenEmissionRechner`).
`Tab_BHKW.Brennstoff` beschreibt dagegen das **Kataloggerät**; wechselt der Anwender den
Träger der Anlage, bleibt die Gerätezeile stehen.

**Das Merkmal wechselt NICHT — die Kategorie bleibt.** `pricing_model` ist die gröbere
Angabe:

| Kategorie (`Tab_BrennstoffKategorien`) | `pricing_model` |
|---|---|
| **2 Öl** (Heizöl S/M/L/EL, EL schwefelarm, Bio 5/10/15/20) | `LIQUID_FUEL` |
| **8 Rapsöl** | `LIQUID_FUEL` |
| 9 Tierische Fette | `ANIMAL_FAT` |

Über `pricing_model = 'LIQUID_FUEL'` fiele **Rapsöl** mit unter den Ausschluss — ein
biogener Brennstoff, für den der Ausschluss fossiler flüssiger Brennstoffe gerade nicht
gilt. Das wäre ein **neuer** Fehlbefund, eingehandelt bei der Behebung eines alten. Die
Kategorie ist außerdem dasselbe Merkmal, das der Ausschluss schon vorher geprüft hat — die
Änderung bleibt damit sauber auf den Bezug beschränkt und der Regressionsnachweis in
N2-5 sauber führbar.

**Der Weg ist deshalb zweistufig:**

```
1. Tab_Energieanlagen.ID_Carrier → energy_carrier.ID_Brennstoff
                                 → Tab_Brennstoff_Stamm.ID_Kategorie
2. (Rückfall) Tab_BHKW.Brennstoff → Tab_Brennstoff_Stamm.ID_Kategorie
```

Stufe 2 ist **kein Randfall**: Die BHKW-Anlage des Projekts 1017 („BHKW EW K 10 S [K]
Heizol", trotz des Namens ein Stadtgas-Gerät) führt **keinen** Energieträger; die Engine
meldet das seit Frage 21 als Warnung und rechnet weiter. Sie greift ebenso, wenn der Träger
im Katalog fehlt oder wenn eine alte Datenbank die Tabelle `energy_carrier` gar nicht führt.
Ergibt keine der beiden Stufen eine Kategorie, gilt die Anlage als **nicht** ölbetrieben —
dieselbe Vorsicht, die schon der Altstand hatte (sein `COUNT(*)` zählte nur Zeilen mit
gültigem Verbund).

**Nachgemessen an den Trägerzeilen:** Alle 21 Träger der produktiven Datenbank haben einen
gültigen `ID_Brennstoff`; für die drei BHKW-Anlagen mit Träger stimmen beide Wege überein
(1018 Erdgas E, 1024 Heizöl L, 1030 Erdgas E).

---

## N2-4 Zwei Ausschlussgründe, eine Bilanz

Die Bereinigung der Bezugsgrößen ist unverändert die des Nachtrags 1 (Abschnitt N3):

```
anteil   = Σ Strom der förderfähigen Anlagen / Σ Strom aller Anlagen
bonusVoll → bonusVoll · anteil
vbh       → Σ Strom der förderfähigen Anlagen · 1000 / Σ P_el derselben
```

Neu ist nur die **Menge** der förderfähigen Anlagen: ausgeschlossen ist, wer über der
Ausschreibungsgrenze liegt **oder** mit Heizöl läuft. Beides entsteht in **einem**
Durchlauf über die Anlagen, und die Summen `PelFoerderfaehigKW`/`StromFoerderfaehigMWh`
werden nur einmal je Anlage fortgeschrieben. Eine Anlage, auf die **beide** Gründe
zutreffen, wird deshalb **genau einmal** gekürzt — nachgewiesen in N2-5, Fall (d) gegen
(d2): Beide liefern denselben Zuschlag und denselben Barwert auf die Nachkommastelle.

**Die Meldungen trennen die Gründe, die Anlagen bleiben eindeutig.** Eine Anlage, die
zugleich zu groß und ölbetrieben ist, erscheint **nur** in der Größen-Meldung
(`UeberGrenze`); die Heizöl-Meldung führt die Teilmenge `NurHeizoel`. So steht keine Anlage
zweimal im selben Hinweis. Die vollständige Liste der Öl-Anlagen (`MitHeizoel`) wird
getrennt geführt — sie trägt den Hinweis „Öl-BHKW ohne Inbetriebnahmedatum", der auch dann
gilt, wenn gar nichts ausgeschlossen wurde.

**Eine Nebenkorrektur.** Der Zweig „keine Anlage bleibt übrig" verlangt jetzt zusätzlich
einen **echten** Ausschluss. Ohne ihn hieße eine Restleistung von 0 nur, dass in den
Anlagenzeilen keine elektrische Nennleistung steht; der Altstand meldete dafür „jede Anlage
über der Ausschreibungsgrenze" mit **leerer** Aufzählung und setzte den Bonus auf 0. Dieser
Fall rechnet jetzt unbereinigt weiter — mit derselben Leistung aus der Gerätesumme, die
`VbhElektrisch` ohnehin schon verwendet. Kein Referenzprojekt erreicht diesen Zweig
(alle BHKW-Anlagen führen ein gepflegtes `Pel`).

---

## N2-5 Wirkung — mit Zahlen

**Gemeinsame Grundlage:** Wegwerf-Kopien der produktiven `Kenndaten.accdb` vom 19.08.2026.
KWKG-Parameter der Probe wie in N5: Zuschlag Eigenstrom 4,00 ct/kWh, Einspeisung
8,00 ct/kWh, Kontingent 30.000 h, Jahresdeckel aus der KWKG-2025-Staffel, Zins 3,0 %,
Betrachtungszeitraum 20 a, Stichtag und Inbetriebnahme 01.01.2026, keine Stundenreihe.

### N2-5.1 Bestandsprojekte — unverändert, und Projekt 1024 ist ein echter Befund

| Projekt | Anlagen | Strom [MWh/a] | Vbh [h/a] | KWKG Jahr 1 **alt** | **neu** | Barwert 20 a **alt** | **neu** |
|---|---|---|---|---|---|---|---|
| 1017 | 1 × 10,0 kW | 28,43 | 2.842,90 | 1.137,20 €/a | 1.137,20 €/a | 10.068,70 € | 10.068,70 € |
| 1018 | 1 × 14,5 kW | 14,96 | 1.031,63 | 598,40 €/a | 598,40 €/a | 8.902,68 € | 8.902,68 € |
| 1024 | 1 × 21,0 kW | 73,91 | 3.519,43 | 0,00 €/a ¹ | 0,00 €/a ¹ | 0,00 € | 0,00 € |
| 1007, 1008, 1011, 1021, 1023 | kein BHKW-Ergebnis | 0 | 0 | 0,00 €/a | 0,00 €/a | 0,00 € | 0,00 € |

¹ Heizöl-Ausschluss (IBN 2026), in **beiden** Ständen — aber ab jetzt aus dem geprüften Grund.

**Δ = 0,00 € in jeder Zeile.**

> ### Projekt 1024: echter Befund, kein Fehlbefund
>
> Die Aufgabe verlangte ausdrücklich die Klärung, ob die Meldung „0 € wegen Heizöl" bei
> Projekt 1024 auf einer verwaisten Gerätezeile beruht. **Sie tut es nicht.** Das Projekt
> führt fünf BHKW-**Gerätezeilen**, davon zwei mit Heizöl L (`EC_Power_6kw.el FL` 6 kW und
> `A-Tron_21_F` 21 kW) — aber genau **eine Anlagenzeile**, und die ist die ölbetriebene:
>
> | | Gerätezeile | Anlagenzeile | Brennstoff (Gerät) | Energieträger (Anlage) |
> |---|---|---|---|---|
> | **installiert** | `A-Tron_21_F`, 21 kW | ja (ID 11257) | 8 = Heizöl L (Kat. 2) | 71 = „Heizöl L var" → Heizöl L (Kat. 2) |
> | verwaist | `EC_Power_6kw.el FL`, 6 kW | — | 8 = Heizöl L | — |
> | verwaist | 2 × `EC_Power_15kw.el Gas`, `2G 400kw.el Gas` | — | 1 = Stadtgas | — |
>
> **Beide** Wege der Kategorieauflösung — Träger wie Gerät — liefern Kategorie 2. Der
> Ausschluss bleibt also bestehen, es gibt **keine Ergebniskorrektur** zu belegen. Neu ist
> allein, dass die Meldung die Anlage benennt: „Jede BHKW-Anlage des Projekts wird mit
> Heizöl betrieben (A-Tron_21_F (21 kW)) …" statt der pauschalen Altmeldung.
>
> **Projekt 1023 ist der Gegenfall**, den der Altstand falsch behandelt hätte: elf
> Gerätezeilen, darunter ein Öl-BHKW — und **keine einzige BHKW-Anlagenzeile**. Ein
> Zuschlag entsteht dort heute schon deshalb nicht, weil der Lauf kein BHKW-Ergebnis
> liefert; die Bezugskorrektur bleibt an diesem Projekt folgenlos, zeigt aber, dass der
> Fehler im Bestand real angelegt ist.

### N2-5.2 Präparierte Kopien — die Wirkung

Basis ist Projekt 1018 (Gas-BHKW, 14,5 kW, 13,23 MWh). Anlagen-, Geräte- und Ergebniszeilen
sind **auf Datenebene** präpariert (Klone der vorhandenen Zeilen mit geändertem Brennstoff,
Träger, `Pel` und Stromsumme); **nicht neu simuliert** — der Rechenkern ist von diesem
Nachtrag nicht berührt (N2-6, V1).

| Fall | Aufbau | Jahr 1 **alt** | **neu** | Barwert 20 a **alt** | **neu** |
|---|---|---|---|---|---|
| **(a)** Gas- und Öl-BHKW nebeneinander | 2 × 14,5 kW, je 13,23 MWh | **0,00 €** | **529,20 €** | **0,00 €** | **7.873,16 €** |
| **(b)** verwaiste Öl-**Gerätezeile** ohne Anlagenzeile | 1 × 14,5 kW Gas + Karteileiche | **0,00 €** | **529,20 €** | **0,00 €** | **7.873,16 €** |
| **(c)** einzige Anlage ölbetrieben | 1 × 14,5 kW Öl | 0,00 € | 0,00 € | 0,00 € | 0,00 € |
| **(d)** Anlage **zugleich** > 500 kW **und** Öl | 200 kW Gas + 600 kW Öl | **0,00 €** | **24.000,00 €** | **0,00 €** | **202.106,33 €** |
| **(d2)** Gegenprobe: dieselbe Anlage nur zu groß | 200 kW Gas + 600 kW Gas | 24.000,00 € | 24.000,00 € | 202.106,33 € | 202.106,33 € |
| **(e)** Öl **nur** am Energieträger der Anlage | Gerät Erdgas E, Träger Heizöl L | 529,20 € | **0,00 €** | 7.873,16 € | **0,00 €** |
| **(f)** Öl **nur** an der Gerätezeile, Anlage ohne Träger | Gerät Heizöl L, `ID_Carrier` NULL | 0,00 € | 0,00 € | 0,00 € | 0,00 € |
| **(g)** keine Anlagenzeile, Öl-Gerätezeile | Ersatzweg | 0,00 € | 0,00 € | 0,00 € | 0,00 € |
| **(m)** gemischt, nichts bleibt übrig | 600 kW Gas + 200 kW Öl | 0,00 € | 0,00 € | 0,00 € | 0,00 € |

**Die vier Kernaussagen der Tabelle:**

- **(a)** ist die eigentliche Korrektur: Ein Öl-Modul nimmt dem Gas-Modul nichts mehr. Der
  Betrag ist exakt der des einzelnen Gas-Moduls (529,20 €/a) — der Anteilsfaktor
  13,23/26,46 = 0,5 halbiert den verdoppelten Bonus punktgenau.
- **(b)** belegt den zweiten Mangel: Eine Gerätezeile ohne Anlagenzeile beeinflusst das
  Ergebnis nicht mehr. Das ist derselbe Nachweis, den E2 für die Leistungssumme geführt hat.
- **(d) gegen (d2)** belegt die Kombinierbarkeit: **byte-gleiche** Zahlen
  (24.000,0000 €/a; 202.106,3288 €) — die doppelt betroffene Anlage wird genau einmal
  gekürzt. Fiele sie zweimal aus den Summen, käme ein negativer oder halbierter Anteil heraus.
- **(e)** ist der einzige Fall, in dem der neue Stand **strenger** ist als der alte: Der
  Energieträger der Anlage schlägt den Brennstoff des Katalogeräts. Genau so rechnet die
  Anwendung an jeder anderen Stelle auch (Verbrauch, Kosten, Emissionen) — hier wird der
  Guard mit dem Rest der Anwendung konsistent.

### N2-5.3 Die Neuanlagen-Regel bleibt unberührt

| Fall | Parametrierung | alt | neu |
|---|---|---|---|
| (c) mit IBN **2020** | Bestandsanlage | 529,20 €/a, kein Hinweis | **identisch** |
| (c) **ohne** IBN | Datum fehlt | 529,20 €/a + Hinweis | 529,20 €/a + Hinweis, jetzt **mit Anlagennamen** |
| (a) **ohne** IBN | Datum fehlt, gemischt | 1.058,40 €/a + Hinweis | **identisch**, Hinweis nennt nur die Öl-Anlage |

### N2-5.4 Die Meldungen

| Fall | Meldung (DE) |
|---|---|
| Teilausschluss Heizöl | „KWKG: Mit Heizöl betrieben und deshalb ohne Zuschlag: OEL_B (14 kW) (KWKG 2025, Neuanlagen nur noch mit Erdgas; Näherung: gilt auch für Bio-Blends). Die übrigen Anlagen mit zusammen 14 kW rechnen weiter." |
| alle Anlagen Öl | „KWKG: Jede BHKW-Anlage des Projekts wird mit Heizöl betrieben (A-Tron_21_F (21 kW)) — als Neuanlage nicht mehr förderfähig (KWKG 2025, nur noch Erdgas; Näherung: gilt auch für Bio-Blends); Bonus = 0." |
| gemischt, nichts bleibt übrig | „KWKG: Keine BHKW-Anlage des Projekts ist zuschlagsberechtigt — über der Ausschreibungsgrenze von 500 kW: GAS_A (600 kW); mit Heizöl betrieben: OEL_200 (200 kW); Bonus = 0." |
| Öl-Anlagen ohne IBN-Datum | „KWKG: Öl-BHKW ohne Inbetriebnahmedatum: OEL_B (14 kW) — als Neuanlage wäre der Zuschlag für diese Anlagen ausgeschlossen (KWKG 2025); Datum im Parameterdialog pflegen." |
| Brennstoff je Anlage nicht ermittelbar | „KWKG: Das Projekt führt ein Öl-BHKW; welche Anlage damit betrieben wird, ließ sich nicht ermitteln, deshalb greift der Heizöl-Ausschluss ersatzweise auf alle Geräte des Projekts (KWKG 2025, Neuanlagen nur noch mit Erdgas); Bonus = 0." |
| dito, ohne IBN-Datum | „KWKG: Das Projekt führt ein Öl-BHKW, aber kein Inbetriebnahmedatum — als Neuanlage wäre der Zuschlag ausgeschlossen (KWKG 2025). Welche Anlage mit Öl betrieben wird, ließ sich nicht ermitteln; Datum im Parameterdialog pflegen." |

Die Altmeldung lautete in **allen** diesen Fällen gleich: „KWKG: Heizöl-BHKW sind als
Neuanlage nicht mehr förderfähig … Bonus = 0." Sie sagte weder, **welche** Anlage gemeint
ist, noch dass die übrigen weiterrechnen, noch ob überhaupt eine installierte Anlage
betroffen ist. Alle sechs Texte stehen in beiden Sprachen im Katalog `MyResource`
(Proben V13/V14). Die beiden Meldungen der Ausschreibungsgrenze aus Nachtrag 1 sind
**wortgleich unverändert** (Probe V8).

---

## N2-6 Verifikation

### N2-6.1 Referenzlauf A/B

Beide Stände aus eigenen Exporten gebaut (`git archive 006e780` für A, Arbeitskopie für B;
Unterschied nachgewiesen: **exakt vier Dateien**), jeweils mit dem mitgelieferten
`Referenzlauf.csproj`. **Eine gemeinsame Wegwerf-Kopie** der produktiven Datenbank, mit dem
B-Stand migriert, danach von beiden Ständen gelesen. Acht Projekte, Feature-Flag
`Kaskade_Zweikanalig` **AUS und AN**.

```
Flag AUS                                   Flag AN
Projekt_1007: PASS (29 Dateien)            Projekt_1007: PASS (29 Dateien)
Projekt_1008: PASS (21 Dateien)            Projekt_1008: PASS (21 Dateien)
Projekt_1011: PASS (29 Dateien)            Projekt_1011: PASS (29 Dateien)
Projekt_1017: PASS (21 Dateien)            Projekt_1017: PASS (21 Dateien)
Projekt_1018: PASS (22 Dateien)            Projekt_1018: PASS (22 Dateien)
Projekt_1021: PASS (21 Dateien)            Projekt_1021: PASS (21 Dateien)
Projekt_1023: PASS (25 Dateien)            Projekt_1023: PASS (25 Dateien)
Projekt_1024: PASS (26 Dateien)            Projekt_1024: PASS (26 Dateien)

GESAMT: PASS (2 129 527 Werte)             GESAMT: PASS (2 129 527 Werte)
```

**Byte-Vergleich: 194 von 194 CSV identisch, in beiden Flag-Stellungen.** Das ist
erwartungsgemäß und trotzdem nicht überflüssig: Der Referenzlauf ruft die
Wirtschaftlichkeit gar nicht auf (`grep` über `Referenzlauf/*.cs`: kein Treffer auf
„Wirtschaftlichkeit" oder „KWKG"), er beweist also die Unversehrtheit des **Rechenkerns**,
nicht die des Guards. Den Guard belegt der Harness in N2-6.2.

### N2-6.2 Verifikationstabelle

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| V1 | Engine unberührt | `git diff --name-only -- Allgemein/Simulation/` | **leer** (keine Datei) |
| V2 | Simulationsergebnisse unverändert, Flag AUS | `Referenzlauf.exe vergleich`, gemeinsame DB-Kopie | **8/8 PASS**, 2 129 527 Werte |
| V3 | dito, Flag AN (`Kaskade_Zweikanalig` für alle 17 Einstellungszeilen gesetzt) | dito | **8/8 PASS**, 2 129 527 Werte |
| V4 | Byte-Identität der Ergebnisdateien | `cmp` je Datei, beide Flag-Stellungen | **194/194 gleich**, 0 Abweichungen |
| V5 | Wirtschaftlichkeitswerte der 8 Referenzprojekte unverändert | Reflection-Harness auf `BaueKwkgReihe`/`VbhElektrisch`/`PelKW`, A gegen B, **unmigrierte und migrierte** Kopie | **8/8 wertgleich**; einzige Abweichung ist der Meldungstext von 1024 |
| V6 | Projekt 1024: echter Befund oder verwaiste Gerätezeile? | Schema-Dump der Anlagen-, Geräte- und Trägerzeilen | **echter Befund** — die einzige Anlagenzeile ist das Öl-BHKW, beide Auflösungswege stimmen überein |
| V7 | Fall (a) Gas + Öl nebeneinander | präparierte Kopie, A gegen B | 0,00 € → **529,20 €/a**, 7.873,16 € Barwert |
| V8 | Fall (b) verwaiste Öl-Gerätezeile | präparierte Kopie, A gegen B | 0,00 € → **529,20 €/a**; kein Ausschluss mehr |
| V9 | Fall (c) alle Anlagen Öl | präparierte Kopie, A gegen B | **0,00 € in beiden Ständen** |
| V10 | Fall (d) gegen (d2): eine Anlage zugleich zu groß und Öl | zwei präparierte Kopien, B gegen B | **24.000,0000 €/a und 202.106,3288 €** in beiden — genau einmal gekürzt |
| V11 | Träger schlägt Gerät (Fall e) | präparierte Kopie, A gegen B | 529,20 € → **0,00 €**; die Anlage wird über `ID_Carrier` erkannt |
| V12 | Rückfall auf die Gerätezeile bei fehlendem Träger (Fall f) | präparierte Kopie, A gegen B | **0,00 € in beiden Ständen** |
| V13 | Ersatzweg ohne Anlagenzeilen (Fall g) | präparierte Kopie, A gegen B | **0,00 € in beiden Ständen**, Hinweis nennt den Ersatzweg |
| V14 | Meldungen der Ausschreibungsgrenze unverändert | präparierte Kopie, beide Anlagen > 500 kW | Hinweis **wortgleich** A und B |
| V15 | Neuanlagen-Regel unberührt | IBN 2020 und IBN fehlt, je auf (a) und (c) | Bonus in A und B **gleich**; Hinweis nur präziser |
| V16 | Ressourcen in beiden Sprachen | Harness mit `CurrentUICulture = en-US`, Fall (a) | DE- und EN-Meldung erscheinen, **Zahlen byte-gleich** (529,2000 / 7 873,1597) |
| V17 | Ressourcen in beiden `.resx` und im Designer | `grep` je Schlüssel | **6/6** in `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| V18 | Build | `MSBuild WP-Plan.sln -t:Rebuild -p:Platform=x86`, Ausgabe in den Scratch-Ordner | **0 Fehler, exakt 6 Bestandswarnungen** |
| V19 | Kodierung und Zeilenenden | `file` je geänderter Datei, CR-Zählung, Suche nach U+FFFD | unverändert (3 × UTF-8 mit BOM, 1 × ohne), **alle Zeilen CRLF**, **0 Ersatzzeichen** |
| V20 | Produktivdatenbank nur gelesen | `Kenndaten.laccdb` vor jedem Kopieren geprüft (nie vorhanden); alle Proben auf Wegwerf-Kopien | **erfüllt** |
| V21 | `bin\` des Repos unberührt | jeder Build ausschließlich mit `-p:OutDir=<Scratch>` | **erfüllt** |

---

## N2-7 Verbleibende Restbefunde der Reihe „Projekt gegen Anlage"

Restbefund 1 aus N7 ist erledigt. Die Reihe ist damit **nicht** abgeschlossen:

| Fundstelle | Befund | Bewertung |
|---|---|---|
| `p.KwkgStichtag`, `p.KwkgInbetriebnahme` (`:958-982`) | **ein Datumspaar je Projekt**; § 6 KWKG (Stichtag, Realisierungsfrist) gilt je Anlage — und über dasselbe Datum entscheidet sich, ob der Heizöl-Ausschluss überhaupt greift | **Der neue gravierendste Restbefund.** Bei gemischten Inbetriebnahmen ist die Prüfung entweder zu streng oder zu großzügig, und ein einziges Datum entscheidet für alle Anlagen zugleich über Neuanlage/Bestandsanlage. Fachlich zu klären, gehört zu E6. |
| `p.KwkgVbhKontingent` (`:1121`) | 30.000 h je **Projekt**; nach § 8 Abs. 1 stehen sie **jeder Anlage** zu | Bereits als E2-Punkt 2 festgehalten; die leistungsgewichtete Vbh bildet es näherungsweise ab. |
| `p.KwkgBonus` / `KwkgBonusEinspeisung` | **ein** Satz je Projekt; § 7 staffelt nach der Leistungsklasse der Anlage | E6, Punkt 3 in Abschnitt 7. Katalogschlüssel liegen seit E1 bereit. |
| Jahresdeckel über **eine** gemeinsame Vbh-Größe | Näherung der Zwischenlösung, unverändert aus Nachtrag 1 (N3, Grenze 1) | E6. |
| `GESETZ_STROMST_GRENZE_BEFREIUNG` (2.000 kW, § 9 Abs. 1 Nr. 3 StromStG) | ungelesen; auch diese Grenze ist eine **Anlagen**-Nennleistung | Vorsorglich vermerkt: Bei der Umsetzung in E4 nicht die Projektsumme dagegen prüfen. |
| `KostenEmissionRechner.cs:225-235` | BEHG-Pflicht über dieselben Kategorien (`1, 2, 3, 4, 11`) — als **Zahlenliterale**, und die Biogas-Ausnahme hängt am **Bezeichnertext** | Kein Fehler in der Bezugsebene (dort wird ohnehin je Träger gerechnet), aber dieselben Katalogschlüssel ein zweites Mal als Literal. Eine gemeinsame Konstantenquelle für die Brennstoffkategorien wäre der nächste Aufräumschritt. |

