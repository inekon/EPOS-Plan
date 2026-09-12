# FX5 — Investitions-Sensitivität und Kohärenz-Fall 5 (Umsetzungsprotokoll)

Fortsetzung von [FX4](FX4_FX3Folgepunkte_Protokoll.md). Anwenderentscheid vom
03.09.2026, wörtlich: **„alle Empfehlungen umsetzen".** Konkret die beiden
offenen Punkte, die als Empfehlung im Raum standen. Stand 03.09.2026,
Branch `lokal_dirk`, Basis HEAD `6988cfb`.

| Paket | war offener Punkt | Gegenstand |
|---|---|---|
| **FX5-a** | FX4-1 | Der Ausschlag „Investition Variante ±10 %" zieht die investitionsgekoppelten Betriebskosten (`PROZENT_INVESTITION`) mit |
| **FX5-b** | S-2 | Fünfte Kohärenzzeile: Mischlage § 53/§ 53a Abs. 5 **neben** § 54 |

FX5-a ist das Spiegelbild von FX4-c: Was dort für die Energieseite entschieden
wurde („der Ausschlag zieht die abgeleiteten Betriebskosten mit"), gilt jetzt
auch für die Investseite.

## 1. Umsetzung

### 1.1 FX5-a — der Investitionsfaktor greift auch am Betriebs-Topf

**Bestandsmechanik.** `BaueSensitivitaet` rechnet jede Zeile als

```
diff = (zins, pE, investF, energieF) =>
        Math.Round( RechneBild(variante, p, zins, pE, investF, energieF).Kapitalwert
                  - RechneBild(stamm,    p, zins, pE, 1.0,     1.0    ).Kapitalwert , 2)
```

Die Zeile „Investition Variante ±10 %" ruft `diff(z, pe, 0,9, 1)` bzw.
`diff(z, pe, 1,1, 1)`; `SENS_DELTA_INVEST = 10.0`. Der Faktor wirkt **nur auf
die Variante**, **nur im Szenario Erwartet** und bisher **nur auf
`InvestPosition.Betrag`** — die davon abgeleiteten Betriebskosten blieben außen
vor. `PROZENT_INVESTITION` ist mit **20 von 66** Zeilen die häufigste
Kategorie-2-Bemessung; genau das war Befund FX4-1.

**Drei Änderungen, alle additiv:**

1. **`BetriebsTopfe`** bekommt `InvestGekoppeltSofort` (double) und
   `InvestGekoppeltAbJahr` (Liste (Betrag, Startjahr)).
2. **`LiesBetriebskostenTopfe`** füllt sie in derselben Leseschleife, aus
   demselben `beitrag` und mit demselben Startjahr-Schnitt; das Prädikat ist das
   vorhandene `IstProzentInvest`.
3. **`RechneBild`** korrigiert den Betriebs-Topf, wenn `investFaktor ≠ 1,0`.

**Die Doppelzählungsfrage — und warum es eine TEILMENGE wurde.** Der
investgekoppelte Betrag bleibt unverändert in `BetriebSofort` /
`BetriebAbJahr` stehen. Der neue Ausweis ist eine **Teilmenge**, kein dritter
Topf: `BetriebsTopfe.Gesamt` zählt ihn ausdrücklich **nicht** mit,
`LiesBetriebskosten` (beide Überladungen) sieht ihn nicht, und die
Eskalation bleibt p_B — die Position ist investitions-, nicht
energiegebunden.

Die Alternative wäre gewesen, den Anteil aus `BetriebSofort`
**herauszulösen** und in `RechneBild` als `Betrieb + f × Anteil` wieder
zusammenzuführen. Das ist rechnerisch dasselbe, ändert aber die
**Summationsreihenfolge** der Leseschleife: Statt einer Kette
`summe += beitrag` über alle Zeilen entstünden zwei Ketten, die erst am Ende
addiert werden — Gleitkommaaddition ist nicht assoziativ, und der Regellauf
(`investFaktor = 1,0`) wäre dann **nicht mehr bitgenau** der von vorher. Die
gewählte Fassung fasst `summe` nicht an; der zweite Akkumulator läuft daneben
her. Deshalb: **Teilmengen-Ausweis.**

**Wie skaliert wird — additive Korrektur.** In `RechneBild`, ausschließlich im
Zweig `investFaktor != 1.0`:

```
delta   = investFaktor − 1.0
betrieb = e.Betrieb + delta * e.InvestGekoppelt                  // = (Betrieb − Anteil) + f·Anteil
betriebAbJahr = Kopie von e.BetriebAbJahr
                + je Startjahr-Position ein Korrekturpaar (delta * Betrag, Startjahr)
```

Das Korrekturpaar ist zulässig, weil der Rechenkern die Paare nur aufsummiert
(`if (t >= vb.Value) betriebT += vb.Key;`) — eine zusätzliche Zeile mit
demselben Startjahr ist wertgleich zum Skalieren der Position und kommt ohne
eine Zuordnung Liste↔Liste aus (die bei gleichen Beträgen mehrdeutig wäre).

**IEEE-754-Begründung wie FX4-c:** Im Normallauf ist `investFaktor` exakt
`1.0`; der Zweig wird gar nicht betreten, weder `betrieb` noch
`betriebAbJahr` werden angefasst. Deshalb ist der Regellauf bitgenau
unverändert (§ 3.2, § 3.4).

**Die lineare Modellannahme, ausdrücklich.** Der Betrag einer
`PROZENT_INVESTITION`-Position entsteht in der Leseschleife **einmal** aus
Investitionssumme × Satz (H4a `InvestSummeFuer`, stufig
Anlage→Komponente→Projekt). Die Sensitivität löst die Kostenwelt **nicht neu
auf**; sie rechnet

```
Δ Position = (f − 1) × Jahr-1-Betrag
```

— also linear im Faktor. Fachlich heißt das: „die Investition steigt um 10 %"
wird als „die Bemessungsbasis dieser Position steigt um 10 %" gelesen. Das ist
für einen Ausschlag von ±10 % die naheliegende Lesart, aber eine
**Modellannahme** und keine Neurechnung. Sie steht als solche im Code
(`RechneBild`) und hier.

**Szenarien.** Die Zuordnung entscheidet die **Bemessungsart**, nicht der
Betrag — dieselbe Regel wie bei den beiden Preissteigerungstöpfen (FX3 § 1.4).
Ein gepflegter BEST-/WORST-Betrag auf einer `PROZENT_INVESTITION`-Zeile
schlägt weiterhin die Ableitung und geht **mit diesem Wert** in denselben
Ausweis; er skaliert also mit. Sonst führte dasselbe Projekt in BEST und in
ERWARTET verschiedene Ausschläge.

**Startjahr-Anteile** (KD6) wandern mit — auch sie sind Jahr-1-Beträge, nur
später fällig (Nachweis § 3.5).

**`OhneKwkg`** kopiert beide neuen Felder mit. Rechnerisch folgenlos (der
Ohne-KWKG-Vergleich läuft immer mit Faktor 1,0), aber die in FX4-a behauptete
**Vollständigkeit der Kopie** bliebe sonst eine Halbwahrheit.

**Der `KapitalwertRechner` wurde nicht angefasst** — wie schon bei FX4-c. Er
bekommt weiterhin einen Jahr-1-Betrag und eine Startjahr-Liste; dass der
Aufrufer sie im Sensitivitätslauf korrigiert hereinreicht, sieht er nicht.

### 1.2 FX5-b — Kohärenz-Fall 5: zwei Entlastungswelten nebeneinander

`KohaerenzPruefung` (Etappe B2, BW2/BF2 „nur warnen") führte vier Fälle plus
die B3-Hilfsenergie-Doppelpflege. **Neu: `MischlageEnergiesteuer`**, gerufen
aus `Pruefe` als eigener Schritt zwischen `Brennstoffseite` und `Stromseite`
(eigener `try`/`catch` wie die anderen Seiten).

**Warum ein eigener Schritt und nicht ein Zweig in `Brennstoffseite`:** Die
Bestandsmethode steigt vorher aus, wenn kein Träger mit Brennstoff und
`CarrierId > 0` gefunden wird, und sie liest die Preiszerlegung. Fall 5 braucht
weder das eine noch das andere — nur die Normwahlen.

**Die Regel.** Für jede Anlage der Steuerliste mit `BrennstoffMWh > 0` wird die
**wirksame** Wahl bestimmt (Anlagenwahl vor Projektwahl, B3a/BF6) und einer von
zwei Seiten zugeordnet:

| Seite | Wahlen |
|---|---|
| Stromerzeugung/KWK | `PARAGRAF_53`, `PARAGRAF_53A` |
| Produzierendes Gewerbe | `PARAGRAF_54` |

Sind **beide** Seiten besetzt, entsteht **eine** Zeile der Schwere `HINWEIS`,
**ohne Betrag**. Ist nur eine Seite besetzt (der Normalfall), schweigt die
Prüfung.

**Vier bewusste Festlegungen:**

1. **Angesetzt wird an der WAHL, nicht am gebuchten Betrag** — genau wie in
   Fall 3. Eine gewählte Norm, die an einer Bedingung scheitert
   (Unternehmensart, Jahresnutzungsgrad, 250-€-Sockel), begründet der
   `SteuerGutschriftRechner` bereits selbst; eine zweite Meldung wäre Rauschen.
   Folge: Die Zeile erscheint auch, wenn eine der beiden Seiten am Ende 0 €
   bucht (gemessen in § 3.6, Proben `misch` und `misch53a`).
2. **Kein Betrag an der Zeile.** Die gebuchte Energiesteuer-Entlastung ist die
   **Summe beider Seiten**; sie als „betroffenen Betrag" auszuweisen führte in
   die Irre. Eine Aufteilung wäre eine Rechnung, die es nicht gibt — dieselbe
   Begründung wie bei Fall 3.
3. **`HINWEIS`, nicht `WARNUNG`.** Die Rechnung selbst ist konsistent: Jede
   Anlage bringt ihre eigene Brennstoffmenge mit, nichts wird zweimal
   entlastet. Die Erinnerung gilt dem **Antrag** beim Hauptzollamt.
4. **Ohne Brennstoffeinsatz keine Zeile** (`BrennstoffMWh > 0`) — dieselbe
   Vorbedingung wie in `Brennstoffseite`: Ohne Menge gibt es nichts, was doppelt
   entlastet werden könnte.

**Die wirksame Wahl** liest ein neuer privater Helfer `WirksameWahl(a, e, out
eigen)`. Er **spiegelt** `SteuerGutschriftRechner.Wahl(a, e)`, das dort privat
ist; `SteuerGutschriftRechner.cs` gehörte nicht zum freigegebenen Änderungsraum
(Alternative wäre `private` → `internal` gewesen). Die Regel ist Zeichen für
Zeichen dieselbe (`null` und Leerstring heißen beide „kein eigener Wert"), und
der Kommentar am Helfer sagt, dass beide zusammen zu ändern sind. `eigen` liefert
zugleich die im Text ausgewiesene Herkunft.

**Neue Texte** — ausschließlich im GetString-Rückfallmuster
`T("KOH_…", "deutscher Rückfalltext")` wie im Bestand. **Keine `.resx`, keine
Designer-Datei angefasst.** Sechs Schlüssel:

| Schlüssel | deutscher Wortlaut (Rückfall im Code) |
|---|---|
| `KOH_FALL5_MISCHLAGE` | „Im Projekt stehen zwei Entlastungswelten nebeneinander: {0} gegen {1}. § 54 EnergieStG nimmt Mengen aus, die bereits nach § 53 / § 53a Abs. 5 entlastet wurden — dieselbe Brennstoffmenge darf nicht zweimal entlastet werden; die Anträge laufen als getrennte Verfahren beim Hauptzollamt. Gerechnet wird unverändert je Anlage nach ihrer Wahl." |
| `KOH_NORM_53` | „§ 53" |
| `KOH_NORM_53A` | „§ 53a Abs. 5" |
| `KOH_NORM_54` | „§ 54" |
| `KOH_HERKUNFT_PROJEKT` | „Projektwahl" |
| `KOH_HERKUNFT_ANLAGE` | „Anlagenwahl" |

Die Aufzählungsform ist die vorhandene: `MitGrund(name, grund)` → „Name
(Grund)", verkettet mit `string.Join(", ", …)` — dasselbe Muster wie die
Trägerliste in Fall 2. Der Anlagenname ist `SteuerAnlage.Bezeichner`, ersatzweise
der Trägername.

**Ausgabeort unverändert:** das nicht persistierte Feld
`WirtschaftlichkeitErgebnis.KohaerenzHinweise`; `UcWirtschaftlichkeit` rendert
die Liste generisch und wurde **nicht** angefasst.

## 2. Nachweise

Werkzeug: Wegwerf-Harness `..\dev\fx5\` (gitignored, `ProjectReference` statt
DLL-HintPath, `DataRepository.EngineModus()`, Schutzriegel gegen die
Produktivdatenbank). A/B über **DLL-Tausch** im Ausgabeordner:

| Stand | Herkunft | `EPOS_Plan.dll` MD5 |
|---|---|---|
| vorher | `git archive 6988cfb` → Scratchpad, dort gebaut | `E339082B99CFBC9B6F49064F092DA6CB` |
| nachher | Arbeitsbaum mit FX5 | `E4A2BFFD1F008162CD566909AB7A10A8` |

Je Lauf eine **frische** Kopie von `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`
(samt `-wal`/`-shm`) im Scratchpad; produktiv wurde nur gelesen. Zahlen im
Round-Trip-Format („R"), damit die letzte Gleitkommastelle sichtbar ist.

### 2.1 Bestandserhebung (lesend)

**FX5-a — 20 `PROZENT_INVESTITION`-Zeilen in Kategorie 2, keine einzige
wirksam.** Verteilt auf vier Projekte:

| Projekt | Zeilen | mit Satz | mit erfasstem Wert |
|---|---|---|---|
| 1018 | 6 | **0** | **0** |
| 1019 | 4 | **0** | **0** |
| 1026 | 6 | **0** | **0** |
| 1030 | 4 | **0** | **0** |

Gesamt: 20 Zeilen, `satz_ungleich_0` = 0, `menge_ungleich_0` = 0,
`wert_ungleich_0` = 0. Alle tragen `Menge = NULL`, `Einheitpreis = NULL`,
`EingegebenerWert/BestCase/WorstCase = 0`, `IstErloes = False`,
`StartJahr = NULL`. Ohne Satz greift die I-2-Regel („nicht rechenbar → der
erfasste Wert"), und der ist 0 — jede dieser Zeilen trägt **0** bei.
Bezeichnungen: Instandhaltung BHKW / Wärmezentrale / bauliche Anlagen /
Stromeinspeisung / Pufferspeicher / Wärmepumpe / Sonnenkollektoren /
Heizkessel / Stromspeicher / PV-Module, Personalkosten, „Steuern, Versicherung,
Verwaltung".

Bemessungsverteilung Kategorie 2 (66 Zeilen) — **unverändert gegenüber
FX3/FX4**: `PROZENT_INVESTITION` 20 · `JAHRESBETRAG` 19 · NULL 9 ·
`PROZENT_ENDENERGIEKOSTEN` 7 · `BETRAG` 5 · `EUR_PRO_KWH_ELEKTRISCH` 3 ·
`EUR_PRO_KWH_THERMISCH` 2 · `PROZENT_BRENNSTOFFKOSTEN` 1.

**Die Variantengruppen — und die daraus abgeleitete A/B-Erwartung.** Der
Ausschlag entsteht ausschließlich für eine **Variante** gegen ihren Stamm, und
der Faktor wirkt nur auf die **Variante**. Der Bestand führt heute **sechs
Varianten in vier Gruppen**:

| Stamm | Variante | Name | `PROZENT_INVESTITION`-Zeilen der Variante |
|---|---|---|---|
| 1018 | 1031 | groesserer Puffer | **0** |
| 1019 | 1023 | Test1 | **0** |
| 1019 | 1024 | Test2 | **0** |
| 1026 | 1027 | Andere WP | **0** |
| 1026 | 1029 | Erdwärme | **0** |
| 1042 | 1044 | Schichtspeicher | **0** |

> Zur Einordnung: FX4 sprach noch von „der einzigen Variantengruppe (Stamm
> 1019)". Die Produktivdatenbank hat seither weitere Gruppen bekommen
> (Anwenderarbeit, letzter Schreibzugriff 03.09.2026 02:03). Die Zahl der
> **persistierten** Sensitivitätszeilen ist trotzdem unverändert **8** — nur
> die 1019-Gruppe rechnet Kapitalwerte, die übrigen Varianten liefern keine.

**Keine Variante trägt eine solche Zeile, und keine der 20 Zeilen ist
wirksam.** Abgeleitete Erwartung: **kein einziger Ausschlag im Bestand darf
sich ändern.** Bestätigt in § 2.2.

**Startjahr-Zeilen im gesamten Bestand: 0** (unverändert gegenüber FX3/FX4).

**Rahmenparameter** (`Tab_ProjektWirtschaftlichkeit`) — weiterhin nur zwei
Zeilen: 1019 (Zins 3, p_E 0, p_B 0, T 20) und 1030 (Zins 3, p_E 2, p_B 1,5,
T 20).

Betriebskosten je Projekt (Summenschleife, Erwartet): 1019/1023/1024 =
**99,0000**; 1030 = **20.000,0000**; alle übrigen 20 Projekte **0,0000** —
zeichengleich mit FX3 und FX4.

**FX5-b — kein Bestandsprojekt trägt die Mischlage.**

| Erhebung | Befund |
|---|---|
| Projektwahl `Tab_ProjektWirtschaftlichkeit.Energiesteuer_Wahl` | 1019 = `KEINE`, 1030 = `KEINE` (mehr Zeilen gibt es nicht) |
| Unternehmensart | beide `KEIN_PROD_GEWERBE` |
| Anlagenwahl `Tab_Energieanlagen.Energiesteuer_Wahl` | **113 Anlagen, alle NULL** — 0 gepflegte Anlagenwahlen |
| wirksame Wahl ≠ `KEINE` (Anlagenwahl vor Projektwahl) | **0 Anlagen** |
| Projekte mit § 53/§ 53a **und** § 54 wirksam | **0** |

**Fall 5 erscheint heute in keinem Projekt.** Er erscheint erst, wenn jemand
eine Anlagen- oder Projektwahl pflegt — dort dann ohne jede Zahlenwirkung.

### 2.2 A/B über alle Projekte × Szenarien — bitgenau kein Unterschied

16 Projekte mit Simulationslauf; je Projekt wird die **ganze Variantengruppe**
gerechnet (die Varianten-IDs werden mitgegeben — sonst entstünde gar keine
Sensitivität).

Gemessene Zeilen je Stand: **66 KENN** · **48 BETRIEB** · **48 TOPF** ·
**8 SENS** · **0 KOH** = **170 Zeilen**.

`diff vorher nachher`: **leer.** (Die TOPF-Zeilen tragen im nachher-Stand zwei
zusätzliche **Harness**-Spalten `I=` und `Iab=` — den neuen Ausweis, den es im
vorher-Stand als Feld nicht gibt und den der Harness dort als `-` schreibt.
Nach Herausschneiden dieser beiden Messspalten ist der Vergleich zeichengleich;
alle 48 zeigen ohnehin `I=0` / `Iab=[]`.)

Anker, vorher == nachher, zeichengleich:

| Anker | Wert |
|---|---|
| Betrieb 1024 | **99,0000** |
| Invest 1018 / 1024 / 1042 | **45.312,5000 / 12.001,0000 / 13.000,0000** |
| KW 1024 | **−2.219.863,761540025** (ausgewiesen −2.219.863,7615) |
| KW 1030 | **−21.875.243,675724894** (ausgewiesen −21.875.243,6757) |

**Textkanal:** In beiden Ständen **0 Kohärenzzeilen** über alle 16 Projekte ×
3 Szenarien — es gibt im Bestand keine Mischlage und (B2-Befund, unverändert)
keine gepflegte Preiszerlegung. Kein Textdiff.

**Was dieser Befund beweist — und was nicht.** Er beweist
Bestandsneutralität, nicht die Richtigkeit. Die belegen die synthetischen
Proben.

### 2.3 FX5-a — `PROZENT_INVESTITION` mit Satz an einer Variante (Probe `sensinv`)

Präparat (frische Kopie, beide Stände identisch vorbereitet): Rahmen 1030 auf
Zins **3 %**, p_E **4 %**, p_B **1 %**, T **15 a**; Projekt **1024** als
Variante an Stamm **1030** gehängt (umgehängt, weil `Tab_Variante.ID_Projekt`
eindeutig ist); die eine Kategorie-2-Zeile der Variante (ID 101600079,
Komponente 1, Anlage 11262) auf `PROZENT_INVESTITION`, **Satz 15 %**,
erfasster Wert 1.800.

| Messung | Wert |
|---|---|
| Betriebskosten der Variante OHNE die Zeile | **0** |
| Betriebskosten der Variante MIT der Zeile | **900,15** |
| ⇒ Betrag der Position (Jahr 1) | **900,15** |
| `InvestGekoppeltSofort` (nachher-Stand) | **900,15** — deckungsgleich |
| `InvestGekoppeltSofort` (vorher-Stand) | Feld existiert nicht |

Topf-Ausweis, identisch in beiden Ständen bis auf die neuen Felder:

```
TOPF 1024  BetriebSofort=900.15  EndenergieSofort=0  Gesamt=900.15   InvestGekoppeltSofort=900.15
TOPF 1030  BetriebSofort=20000   EndenergieSofort=0  Gesamt=20000    InvestGekoppeltSofort=0
```

Ungerundete Ausschläge (Reflection auf `BaueEingabe`/`RechneBild`/`OhneKwkg` —
die persistierte Zeile rundet jeden Endpunkt einzeln auf 2 Stellen und taugt
nicht als exakter Beleg):

| Größe | vorher | nachher | Δ | Handrechnung | Abweichung |
|---|---|---|---|---|---|
| BASIS (f = 1,0) | 16.845.224,030315634 | 16.845.224,030315634 | **0** | 0 | — |
| Invest **−10 %** (f = 0,9) | 16.846.424,130315635 | **16.847.571,003353763** | **+1.146,8730381280184** | **+1.146,8730381299383** | **−1,920E−09** |
| Invest **+10 %** (f = 1,1) | 16.844.023,930315636 | **16.842.877,057277504** | **−1.146,8730381317437** | **−1.146,8730381299383** | **−1,805E−09** |
| Energie −10 % | 17.073.856,085051686 | 17.073.856,085051686 | **0** | 0 | — |
| Energie +10 % | 16.616.591,975579582 | 16.616.591,975579582 | **0** | 0 | — |
| OHNE_KWKG | 16.904.662,508416206 | 16.904.662,508416206 | **0** | 0 | — |
| Zins −1 %-Pkt | 18.226.049,222015493 | 18.226.049,222015493 | **0** | 0 | — |
| Zins +1 %-Pkt | 15.609.712,886281572 | 15.609.712,886281572 | **0** | 0 | — |

Handrechnung: `Δ = −(f−1) · Σ_{t=1..15} 900,15 · (1+p_B)^(t−1) · (1+i)^(−t)`
mit Barwert der Position **11.468,730381299383** (im Skript unabhängig
nachgerechnet: **11.468,730381299383**, bitgleich); also ±0,1 davon.

Persistierte (gerundete) Zeile „Investition Variante ±10 %":
`MINUS` **16.846.424,13 → 16.847.571,00**, `PLUS`
**16.844.023,93 → 16.842.877,06**.

**Nichts sonst ändert sich.** Die vier übrigen Sensitivitätszeilen sind
zeichengleich, ebenso alle Kapitalwerte in allen drei Szenarien:
1030 = **−19.155.014,308057457**, 1024 = **−2.309.790,2777418224**.

### 2.4 Gegenprobe ohne investgekoppelte Zeile (Probe `sensinvohne`)

Dieselbe Gruppe, dieselbe Zeile, aber als `BETRAG` mit 1.800 €/a (kein
Investbezug). **Alle Messzeilen bitgenau identisch** — Kennzahlen,
Sensitivitätszeilen, Selbstprüfung und die acht ungerundeten Rohwerte:

```
ROH BASIS              16833759.122207418   (vorher == nachher)
ROH INVEST_MINUS_0.9   16834959.222207416
ROH INVEST_PLUS_1.1    16832559.022207417
ROH ENERGIE_MINUS_0.9  17062391.17694347
ROH ENERGIE_PLUS_1.1   16605127.067471365
ROH OHNE_KWKG          16893197.60030799
ROH ZINS_MINUS         18213686.815900903
ROH ZINS_PLUS          15599054.008547395
```

Der Ausweis `InvestGekoppeltSofort` steht auch im nachher-Stand auf **0** — die
Zeile ist nicht investgekoppelt, und der Investfaktor lässt sie in Ruhe. Der
Ausschlag „Investition ±10 %" bewegt sich hier ausschließlich über die
Investitionspositionen (±1.200,10 gegenüber BASIS), wie vor FX5.

### 2.5 Startjahr-Variante (Probe `sensinvstart`, `StartJahr = 3`)

Dieselbe `PROZENT_INVESTITION`-Zeile mit Startjahr 3. Topf-Ausweis im
nachher-Stand:

```
TOPF 1024  BetriebSofort=0  BetriebAbJahr=[(900.15 ab 3)]  InvestGekoppeltSofort=0
                            InvestGekoppeltAbJahr=[(900.15 ab 3)]   Gesamt=900.15
```

| Größe | vorher | nachher | Δ | Handrechnung | Abweichung |
|---|---|---|---|---|---|
| BASIS | 16.846.954,924839154 | 16.846.954,924839154 | **0** | 0 | — |
| Invest **−10 %** | 16.848.155,02483915 | **16.849.128,80842493** | **+973,78358577936888** | **+973,78358577816152** | **+1,207E−09** |
| Invest **+10 %** | 16.845.754,824839152 | **16.844.781,041253373** | **−973,78358577936888** | **−973,78358577816152** | **−1,207E−09** |
| Energie ±10 %, OHNE_KWKG, Zins ± | zeichengleich | zeichengleich | **0** | 0 | — |

Handrechnung `Δ = −(f−1) · Σ_{t=3..15} 900,15 · (1+p_B)^(t−1) · (1+i)^(−t)`,
Barwert ab t = 3 = **9.737,835857781614** (unabhängig nachgerechnet
**9.737,8358577816143**). Die Summe läuft **erst ab dem Startjahr** — genau das
belegt, dass das Korrekturpaar mit demselben Startjahr angehängt wird und nicht
etwa ab t = 1 wirkt.

### 2.6 FX5-b — Fall 5 in acht Präparaten

Alle Proben auf Projekt **1030** (zwei BHKW mit Brennstoff; mit Projektwahl
§ 54 tritt zusätzlich der Heizkessel in die Steuerliste). Projekt- und
Anlagenwahlen werden je Probe frisch gesetzt.

Steuerliste des Laufs bei Projektwahl `KEINE`:

```
BHKW EW M 50 S [K] Erdgas | 862,1800 MWh | 50,0000 kW | Träger 63 | Stromerzeuger True
EC-POWER XRGI 9           | 186,0900 MWh |  9,0000 kW | Träger 63 | Stromerzeuger True
```

| Probe | Wahl / Rahmen | KW 1030 (**vorher == nachher**) | KOH vorher | KOH nachher | Fall-5-Zeile |
|---|---|---|---|---|---|
| `misch` | Anlage 1 § 53, Anlage 2 § 54 | **−21.797.304,008167088** | 1 | 2 | **ja** |
| `misch53` | nur § 53 | **−21.797.304,008167088** | 1 | 1 | nein |
| `misch54` | nur § 54 | **−21.875.243,675724894** | 0 | 0 | nein |
| `mischprod` | § 53 + § 54, prod. Gewerbe | **−20.503.866,646492936** | 2 | 3 | **ja** |
| `mischprod53` | nur § 53, prod. Gewerbe | **−20.504.368,128934287** | 2 | 2 | nein |
| `mischprod54` | nur § 54, prod. Gewerbe | **−20.581.806,31405074** | 2 | 2 | nein |
| `misch53a` | § 53a Abs. 5 + § 54, prod. Gewerbe | **−20.581.806,31405074** | 2 | 3 | **ja** |
| `mischprojekt` | Projekt § 54, Anlage 1 § 53 | **−20.381.314,758248** | 2 | 3 | **ja** |
| `mischalleeigen` | Projekt § 54, **alle** Anlagen § 53 | **−20.487.545,896064315** | 2 | 2 | nein |

**Alle Kennzahlen (KW, BK, EK, Barwerte) und alle Selbstprüfungen sind je Probe
vorher == nachher zeichengleich.** Die einzige Änderung ist die zusätzliche
Textzeile.

Wortlaut der neuen Zeile (Schwere `HINWEIS`, Betrag `null`), Probe `misch`:

> Im Projekt stehen zwei Entlastungswelten nebeneinander: BHKW EW M 50 S [K]
> Erdgas (§ 53, Anlagenwahl) gegen EC-POWER XRGI 9 (§ 54, Anlagenwahl). § 54
> EnergieStG nimmt Mengen aus, die bereits nach § 53 / § 53a Abs. 5 entlastet
> wurden — dieselbe Brennstoffmenge darf nicht zweimal entlastet werden; die
> Anträge laufen als getrennte Verfahren beim Hauptzollamt. Gerechnet wird
> unverändert je Anlage nach ihrer Wahl.

Probe `misch53a` — die zweite Stromerzeugungsnorm im Text:

> … BHKW EW M 50 S [K] Erdgas (**§ 53a Abs. 5**, Anlagenwahl) gegen EC-POWER
> XRGI 9 (§ 54, Anlagenwahl). …

Probe `mischprojekt` — **Herkunft Projektwahl**, und der Heizkessel ist mit
dabei (er tritt bei § 54 in die Steuerliste, BF5):

> … BHKW EW M 50 S [K] Erdgas (§ 53, **Anlagenwahl**) gegen EC-POWER XRGI 9
> (§ 54, **Projektwahl**), Vitocrossal 200 CM2 raumluftabhängig (§ 54,
> **Projektwahl**). …

**Die entscheidende Gegenprobe `mischalleeigen`:** Projektwahl § 54, aber
**jede** Anlage hängt sich mit § 53 aus. § 54 ist damit nirgends **wirksam** —
und es entsteht **keine** Zeile. Das belegt, dass die Prüfung die wirksame
Wahl je Anlage auswertet und nicht bloß zwei Spalten vergleicht.

**Zahlenprobe der Summenlogik (Probe `mischprod`):** Die gebuchte
Energiesteuer-Entlastung **5.272,477401904762 €/a** ist exakt die Summe der
beiden Seiten aus den Einzelproben — **5.238,769904761904** (§ 53, Probe
`mischprod53`) **+ 33,707497142857164** (§ 54, Probe `mischprod54`). Beide
Seiten rechnen also mit ihren **eigenen** Anlagenmengen; die Mischlage erzeugt
im Programm **keine** Doppelentlastung. Genau deshalb ist die Zeile ein
Hinweis auf das Antragsverfahren und keine Warnung über eine Fehlrechnung.

Nebenbefund derselben Probe: In `misch53a` bucht § 53a Abs. 5 **0 €** (der
Jahresnutzungsgrad-Nachweis fehlt) — die Fall-5-Zeile erscheint trotzdem, weil
sie an der Wahl ansetzt (§ 1.2, Festlegung 1).

### 2.7 Selbstprüfung der Mehrjahrestabelle — grün in allen Proben

Geprüft wird „kumuliert(T) + Restwert-Barwert = Kapitalwert" und zusätzlich
„Summe der Positionsspalten == Spalte NETTO", je Projekt der Gruppe, in beiden
Ständen.

| Probe (vorher == nachher) | Projekt | kumuliert(T) | + Restwert | = Kapitalwert | Diff | Σ Spalten − NETTO |
|---|---|---|---|---|---|---|
| `sensinv` | 1030 | −19.175.874,8213 | 20.860,5133 | −19.155.014,3081 | **0,000E+00** | 2,328E−10 |
| `sensinv` | 1024 | −2.309.790,2777 | 0,0000 | −2.309.790,2777 | **0,000E+00** | −2,910E−11 |
| `sensinvohne` | 1030 | −19.175.874,8213 | 20.860,5133 | −19.155.014,3081 | **0,000E+00** | 2,328E−10 |
| `sensinvohne` | 1024 | −2.321.255,1859 | 0,0000 | −2.321.255,1859 | **0,000E+00** | −2,910E−11 |
| `sensinvstart` | 1030 | −19.175.874,8213 | 20.860,5133 | −19.155.014,3081 | **0,000E+00** | 2,328E−10 |
| `sensinvstart` | 1024 | −2.308.059,3832 | 0,0000 | −2.308.059,3832 | **0,000E+00** | −2,910E−11 |
| `misch` / `misch53` | 1030 | −21.908.961,9519 | 111.657,9438 | −21.797.304,0082 | **0,000E+00** | 2,328E−10 |
| `misch54` | 1030 | −21.986.901,6195 | 111.657,9438 | −21.875.243,6757 | **0,000E+00** | 2,328E−10 |
| `mischprod` | 1030 | −20.615.524,5903 | 111.657,9438 | −20.503.866,6465 | **0,000E+00** | 4,657E−10 |
| `mischprod53` | 1030 | −20.616.026,0727 | 111.657,9438 | −20.504.368,1289 | **0,000E+00** | 2,328E−10 |
| `mischprod54` / `misch53a` | 1030 | −20.693.464,2578 | 111.657,9438 | −20.581.806,3141 | **0,000E+00** | 4,657E−10 |
| `mischprojekt` | 1030 | −20.492.972,7020 | 111.657,9438 | −20.381.314,7582 | **0,000E+00** | 4,657E−10 |
| `mischalleeigen` | 1030 | −20.599.203,8398 | 111.657,9438 | −20.487.545,8961 | **0,000E+00** | 2,328E−10 |

Spaltenbestand unverändert; er wächst je nach Präparat um die
Gutschriftsspalten (`ENERGIESTEUER_GUTSCHRIFT`, `STROMSTEUER_ENTLASTUNG`) —
das ist Bestandsverhalten der Steuerrechnung und keine FX5-Wirkung (die
Spaltenliste ist in beiden Ständen je Probe identisch).

### 2.8 Build und Hygiene

- `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64` → **exit 0**,
  **39 Warnungen** (Warnungsbild unverändert; derselbe Wert im vorher-Build aus
  dem Scratchpad).
- Sweep `git grep -n "^<<<<<<<" -- "*.cs"` → **0 Treffer**.
- `git status --porcelain`: die zwei `.cs` dieses Pakets, dieses Protokoll
  (unversioniert) sowie die **fremden**, schon vorher offenen Einträge
  `CLAUDE.md`, `WindowsFormsApplication1/CLAUDE.md` (beide `M`) und
  `WindowsFormsApplication1/STAND.md` (`??`) — nicht angefasst.
- Kodierung: beide bearbeiteten `.cs` bleiben **UTF-8 ohne BOM** mit CRLF
  (CR-Zahl == Zeilenzahl geprüft) — je wie im HEAD.
- **Keine `.resx`, keine Designer-Datei, keine `Views/`-Datei.** Die sechs
  neuen Textschlüssel leben ausschließlich als GetString-Rückfall im Code.
- Produktivdatenbank ausschließlich gelesen (kopiert); jeder Lauf gegen eine
  frische Kopie im Scratchpad, Schutzriegel im Harness.

## 3. Geänderte Dateien

```
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs   FX5-a: BetriebsTopfe.InvestGekoppelt*,
                                                         Leseschleife, ProjektEingabe.InvestGekoppelt*,
                                                         BaueEingabe, OhneKwkg, RechneBild
                                                         + Doku an RechneBild, IstProzentInvest,
                                                           LiesBetriebskostenTopfe, BaueSensitivitaet
Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs        FX5-b: MischlageEnergiesteuer (neu),
                                                         WirksameWahl, AnlagenName, Aufruf in Pruefe
                                                         + Doku am Klassenkopf und an HINWEIS
Allgemein/Reporting/FX5_InvestSensitivitaet_S2_Protokoll.md   dieses Protokoll
```

`KapitalwertRechner.cs`, `WirtschaftlichkeitZeilen.cs` und `DbWerte.cs` waren
**nicht nötig** — FX5-a kommt wie FX4-c ohne einen Eingriff in den Rechenkern
aus, und es entstand keine neue Persistenzkonstante.

## 4. Offene Punkte

| Nr. | Punkt |
|---|---|
| **FX5-1** | **Die sechs `KOH_`-Schlüssel liegen nur als Code-Rückfall vor**, nicht in `MyResource/Resource.resx` / `Resource.en-US.resx` (Auftrag: `.resx` nicht anfassen). Solange sie fehlen, erscheint die Zeile in **beiden** Sprachen deutsch. Der englische Vorschlag steht im Abschlussbericht; der `.resx`-Nachtrag ist ein eigener Schritt — sinnvollerweise gebündelt mit dem noch offenen FX3-1. |
| **FX5-2** | **Fall 5 setzt an der WAHL an, nicht am gebuchten Betrag** (§ 1.2, Festlegung 1). Folge: Die Zeile erscheint auch, wenn eine der beiden Seiten am Ende 0 € bucht — gemessen in `misch` (§ 54 scheitert an der Unternehmensart) und `misch53a` (§ 53a scheitert am Nutzungsgrad). Das ist die Fall-3-Hausregel; ob der Anwender sie hier auch will, oder ob die Zeile zusätzlich einen gebuchten Betrag auf **beiden** Seiten verlangen soll, ist ein **Anwenderentscheid**. |
| **FX5-3** | **`BrennstoffMWh > 0` als Vorbedingung** (§ 1.2, Festlegung 4) ist eine gesetzte, keine beauftragte Regel. Eine Anlage ohne Brennstoffeinsatz bringt keine entlastungsfähige Menge mit; wer die Zeile auch dort will, muss die Bedingung streichen. **Anwenderentscheid.** |
| **FX5-4** | **`WirksameWahl` spiegelt `SteuerGutschriftRechner.Wahl`** (dort `private`, Datei nicht im Änderungsraum). Zwei Stellen, eine Regel — sauberer wäre `private` → `internal` und ein einziger Aufruf. Kleiner, isolierter Nachtrag. |
| **FX5-5** | **Lineare Modellannahme der Investitions-Sensitivität** (§ 1.1): `Δ Position = (f−1) × Jahr-1-Betrag`. Wer will, dass der Ausschlag die Kostenwelt neu auflöst (`InvestSummeFuer` mit skalierten Investitionen), bekäme dieselbe Zahl nur, solange alle Investitionspositionen gleichmäßig skalieren — was der Ausschlag ja gerade unterstellt. Dokumentiert, nicht geändert. |
| **FX5-6** | **Sichtabnahme in der Oberfläche steht aus** (Reiter Wirtschaftlichkeit: neue Kohärenzzeile; Sensitivitätstabelle im Bericht) — die Nachweise hier sind rechnerisch, nicht visuell. |
| **FX5-7** | Nebenbefund ohne FX5-Bezug: Der Anlagenbezeichner „Vitocrossal 200 CM2 raumluftabh**?**ngig" (Projekt 1030) trägt in der Produktivdatenbank ein defektes Umlautbyte. Reine **Datenlage** (andere DB-Texte wie „Instandhaltung Wärmezentrale" sind sauber), vermutlich ein Altlast aus der `.accdb`-Übernahme. Nur vermerkt. |
| **FX3-1 / FX3-3 / FX4-2 / FX4-3** | unverändert offen (siehe FX4-Protokoll § 5). |

Harness (gitignored): `..\dev\fx5\` — Schritte
`erhebung | anker | ab | topf <projekt> | probe <modus> | sql "<select>"`,
Probe-Modi `sensinv | sensinvohne | sensinvstart | misch | misch53 | misch54 |
mischprod | mischprod53 | mischprod54 | misch53a | mischprojekt |
mischalleeigen`, Treiber
`..\dev\fx5\lauf.ps1 -Stand vorher|nachher -Schritt … [-Arg …]`.
