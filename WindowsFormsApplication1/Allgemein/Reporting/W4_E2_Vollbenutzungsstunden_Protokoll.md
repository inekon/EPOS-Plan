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
