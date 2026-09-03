# FX4 — die drei entschiedenen FX3-Folgepunkte (Umsetzungsprotokoll)

Fortsetzung von [FX3](FX3_R2_Endenergie_Eskalation_Protokoll.md). Drei
Anwenderentscheide vom 02.09.2026, wörtlich: **„FX3-2: ja, schließen ·
FX3-4: ja, gleichziehen · FX3-5: mitziehen".** Stand 03.09.2026,
Branch `lokal_dirk`, Basis HEAD `c1137f8`.

**FX3-1** (eigene Spalte für den p_E-Anteil in der Mehrjahrestabelle) ist
**nicht** Teil dieses Pakets — er kommt gebündelt mit B6. **FX3-3**
(Konzeptnachzug in der Repo-Wurzel) ebenfalls nicht.

| Paket | war offener Punkt | Gegenstand |
|---|---|---|
| **FX4-a** | FX3-2 | `OhneKwkg` kopiert `BetriebAbJahr` mit (Altlücke seit KD6) |
| **FX4-b** | FX3-4 | `PROZENT_BRENNSTOFFKOSTEN`/`PROZENT_STROMKOSTEN` wandern in den p_E-Topf |
| **FX4-c** | FX3-5 | Sensitivität „Energiekosten ±10 %" skaliert den p_E-Topf mit |

Das committete FX3-Protokoll bleibt unverändert (Historie); seine Tabelle
„Dokumentierte Grenzen" ist mit diesem Paket in zwei von drei Zeilen
überholt — die **Code**-Kommentare sind entsprechend nachgezogen (§ 2.4).

## 1. Umsetzung

### 1.1 FX4-a — die Altlücke im Ohne-KWKG-Vergleich

`OhneKwkg(ProjektEingabe)` baut die flache Kopie für das Novellen-Szenario
(„KWKG-Bonus entfällt"). Sie kopierte seit KD6 alles außer **einem** Feld:
`BetriebAbJahr` — die Betriebskostenpositionen mit `StartJahr ≥ 2`. Die
FX3-Felder `Endenergie`/`EndenergieAbJahr` waren korrekt mitkopiert, das
ältere Feld daneben nicht.

Fix: eine Zeile im Objektinitialisierer, `BetriebAbJahr = e.BetriebAbJahr`.
**Damit ist die Kopie für `RechneBild` vollständig** — die Liste der von
`RechneBild` gelesenen Felder (Investitionen, Zuschuss, `Betrieb` +
`BetriebAbJahr`, `Endenergie` + `EndenergieAbJahr`, `Energie`, `Erloes`,
`Behg`, `BehgJeJahr`, `ErloesReihen`) ist jetzt deckungsgleich mit dem, was
`OhneKwkg` weiterreicht.

**Wo das wirkt: ausschließlich in der Sensitivität.** `OhneKwkg` wird an
genau **einer** Stelle gerufen — in `BaueSensitivitaet`, für die Zeile
„KWKG-Bonus entfällt (Regulierungsrisiko Novelle)", und nur, wenn Variante
oder Stamm eine KWKG-Reihe führen. Kennzahlen, Mehrjahrestabelle und
Bericht sind unberührt.

### 1.2 FX4-b — die zwei Alt-Arten in den p_E-Topf

Neu: `IstEnergiepreisArt(string bem)` neben dem vorhandenen
`IstEndenergieArt`. `LiesBetriebskostenTopfe` fragt für die
**Topf-Zuordnung** seither `IstEnergiepreisArt` (lokale Variable
`ausEndenergie` → `ausEnergiepreis` umbenannt); alles Übrige der
Leseschleife ist unverändert.

**Die beiden Fragen fallen auseinander und wurden bewusst nicht
zusammengelegt:**

| Frage | Prädikat | Umfang |
|---|---|---|
| Welche Art holt ihre **Menge** frisch aus dem Lauf? | `IstEndenergieArt` | `PROZENT_ENDENERGIEKOSTEN`, `PROZENT_ENDENERGIEBEDARF` |
| Welche Art gehört in den **p_E-Topf**? | `IstEnergiepreisArt` (neu) | dieselben zwei **+** `PROZENT_BRENNSTOFFKOSTEN`, `PROZENT_STROMKOSTEN` |

Hätte man stattdessen `IstEndenergieArt` erweitert, zöge das drei weitere
Stellen mit (`LiesBetriebskostenTopfe`-Mengenzweig,
`LiesBetriebskostenPositionen`, `AktualisiereMenge`) und die Alt-Arten
verlören ihre Konserven-Menge an einen Auflöser, der für sie gar nichts
liefert. Die **Mengenermittlung der Alt-Arten ist von FX4 nicht berührt**
(sie bleiben reine Konserve, Befund B-4 aus FX2).

Die Startjahr-Anteile wandern mit: `topfe.EndenergieAbJahr` statt
`topfe.BetriebAbJahr` — dieselbe Verzweigung, nur mit dem weiteren
Prädikat davor.

**Weg C bleibt bei p_B.** Der feste Jahresbetrag
(`JAHRESBETRAG`/`BETRAG`) trägt keine Endenergie-Bemessung; der Anwender
hat nur die zwei %-Arten beauftragt.

### 1.3 FX4-c — die Sensitivität zieht den p_E-Topf mit

**Bestandsmechanik zuerst, dann die Kopplung.** Der Ausschlag entsteht in
`BaueSensitivitaet`:

```
diff = (zins, pE, investF, energieF) =>
        Math.Round( RechneBild(variante, p, zins, pE, investF, energieF).Kapitalwert
                  - RechneBild(stamm,    p, zins, pE, 1.0,     1.0    ).Kapitalwert , 2)
```

Die Zeile „Energiekosten Variante ±10 % (inkl. CO₂-Abgabe)" ruft
`diff(z, pe, 1, 0,9)` bzw. `diff(z, pe, 1, 1,1)`; `SENS_DELTA_ENERGIE = 10.0`.
Der Faktor wirkt also

- **nur auf die VARIANTE** — der Stamm rechnet in derselben Klammer immer
  mit 1,0 (deshalb heißt die Zeile „…Variante…"),
- **nur im Szenario Erwartet** und nur für Nicht-Stamm-Projekte,
- und in `RechneBild` **am JAHR-1-BETRAG**, nicht an der Jahresreihe:
  `(e.Energie ?? 0) * energieFaktor` und `e.Behg * energieFaktor`; die
  jahresscharfe CO₂-Reihe `e.BehgJeJahr` wird gliedweise skaliert. Die
  Preissteigerung `(1+p_E)^(t−1)` läuft im Rechenkern unverändert darüber.

Genau in diese Mechanik ist der p_E-Topf eingehängt:

```
e.Endenergie * energieFaktor                    // Jahr-1-Betrag, wie e.Energie/e.Behg
e.EndenergieAbJahr[i].Key * energieFaktor       // Startjahr-Anteile sind auch Jahr-1-Beträge
```

Die Liste wird **nur kopiert, wenn `energieFaktor != 1.0`**; im Normallauf
bleibt die Originalliste stehen und `x * 1.0` ist in IEEE 754 wertgleich —
deshalb ist der Regellauf bitgenau unverändert (§ 3.1).

**Der `KapitalwertRechner` wurde für FX4-c nicht angefasst.** Er bekommt
weiterhin einen Jahr-1-Topf; dass der Aufrufer ihn im Sensitivitätslauf
bereits skaliert hereinreicht, sieht er nicht.

### 1.4 Die übrigen Sensitivitätsausschläge — Bestandsaufnahme

Erhoben, **nicht geändert** (Auftrag: nur berichten):

| Zeile | Wirkt über | p_E-Topf konsistent behandelt? |
|---|---|---|
| Zinssatz ±1 %-Pkt | `zinsProzent` → Abzinsung im Rechenkern | **ja** — die Abzinsung trifft alle Zahlungen, den Topf eingeschlossen |
| Energiepreissteigerung ±1 %-Pkt | `preisstEnergie` → `(1+p_E)^(t−1)` | **ja, seit FX3** — der Topf eskaliert mit derselben Rate |
| Investition Variante ±10 % | `investFaktor` auf `InvestPosition.Betrag` | **offener Befund, siehe unten** |
| Energiekosten Variante ±10 % | `energieFaktor` | **ja, seit FX4-c** |
| KWKG-Bonus entfällt | `OhneKwkg` | **ja, seit FX4-a** (Topf und Startjahr-Anteile werden mitkopiert) |

**Offener Befund FX4-1 (nicht beauftragt, nicht geändert):** Der
Investitions-Ausschlag skaliert die Investitionspositionen, **nicht** die
davon abgeleiteten Betriebskosten. Eine Betriebsposition mit
`PROZENT_INVESTITION` („x % der Investitionssumme") ist im Bestand die
**häufigste** Kategorie-2-Bemessung (20 von 66 Zeilen); ihr Betrag wird in
`BaueEingabe` einmal aus der Kostenwelt aufgelöst und danach vom
`investFaktor` nicht mehr berührt. Dieselbe Frage, die FX4-c für die
Energieseite beantwortet hat, steht für die Investseite also noch offen.
Sie ist eine Fachentscheidung des Anwenders.

## 2. Was die Doku sagt — und was daran nachgezogen wurde

### 2.1 `KapitalwertRechner`, Klassenkopf

Der FX3-Absatz „Was NICHT umgestellt wurde" nannte die zwei Alt-Arten und
Weg C. Er ist ersetzt durch einen FX4-Absatz (Alt-Arten sind jetzt drin,
Sensitivität koppelt) **plus** einen verkürzten Grenz-Absatz, der nur noch
Weg C und die unangetastete „eine Klammer" führt.

### 2.2 `KapitalwertRechner.Rechne`, Parameter `endenergieJahr`

Ergänzt: der Topf trägt seit FX4-b auch die zwei Alt-Arten, und der
Aufrufer reicht ihn im Sensitivitätslauf bereits skaliert herein.

### 2.3 `DbWerte.cs`

Im H1-Block: neuer FX4-b-Absatz („DIE ZWEI ALT-VORLAEUFER ZIEHEN GLEICH")
und FX4-c-Absatz; die Liste „AUSDRUECKLICH NICHT umgestellt" ist auf Weg C
zusammengeschrumpft. Zusätzlich je ein Zweizeiler direkt an
`BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN` und `BEMESSUNG_PROZENT_STROMKOSTEN`,
damit ein Leser dort nicht die alte Aussage findet.

### 2.4 `WirtschaftlichkeitCtrl.cs`

`BetriebsTopfe`-Klassenkopf (Grenzen), `ProjektEingabe.Endenergie`,
`BetriebsTopfe.EndenergieSofort`, die Leseschleife, `RechneBild`,
`OhneKwkg` und die zwei Prädikate tragen den FX4-Vermerk.
`WirtschaftlichkeitZeilen.cs`: eine Zeile am Kommentar der Spalte
`BETRIEB`.

**Nicht angefasst:** das FX3-Protokoll, `KONTEXT_*`- und Wurzelkonzepte
(dort steht R-2 noch als offener Befund — Nachzug bleibt FX3-3),
`CLAUDE.md`, `STAND.md`.

## 3. Nachweise

Werkzeug: Wegwerf-Harness `..\dev\fx4\` (gitignored, `ProjectReference`
statt DLL-HintPath, `DataRepository.EngineModus()`, Schutzriegel gegen die
Produktivdatenbank). A/B über **DLL-Tausch** im Ausgabeordner:

| Stand | Herkunft | `EPOS_Plan.dll` MD5 |
|---|---|---|
| vorher | `git archive c1137f8` → Scratchpad, dort gebaut | `814CAF1F0D69637E71427C4B0337D223` |
| nachher | Arbeitsbaum mit FX4 | `FE704583656D870BFB6C52D473B1154E` |

Je Lauf eine **frische** Kopie von `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite`
(samt `-wal`/`-shm`) im Scratchpad; produktiv wurde nie geschrieben. Zahlen
im Round-Trip-Format („R"), damit die letzte Gleitkommastelle sichtbar ist.

### 3.1 Bestandserhebung (lesend)

**FX4-a — Zeilen mit `StartJahr > 1`: 0.** `Tab_ProjektWerte` führt
**175** Zeilen, alle mit `StartJahr = NULL`; keine einzige Kategorie trägt
einen Startjahrwert. FX4-a ist damit bestandsneutral.

**FX4-b — die zwei Alt-Arten: 1 Zeile, unwirksam.**

| Projekt | ID | Kat. | Komponente | Bemessung | Wert | Menge | Satz | StartJahr |
|---|---|---|---|---|---|---|---|---|
| 1018 | 101600562 | 2 | 7 (BHKW) | `PROZENT_BRENNSTOFFKOSTEN` | 0 | **NULL** | **NULL** | NULL |

`PROZENT_STROMKOSTEN`: **0 Zeilen.** Zählung: 1 Zeile, davon
`satz_ungleich_0` = 0, `menge_ungleich_0` = 0, `wert_ungleich_0` = 0,
**WIRKSAM = 0**. Ohne Menge greift die I-2-Regel („nicht rechenbar → der
erfasste Wert"), und der ist 0 — die Zeile trägt in **jedem** Topf 0 bei.

Bemessungsverteilung Kategorie 2 (66 Zeilen, unverändert gegenüber FX3):
`PROZENT_INVESTITION` 20 · `JAHRESBETRAG` 19 · NULL 9 ·
`PROZENT_ENDENERGIEKOSTEN` 7 · `BETRAG` 5 · `EUR_PRO_KWH_ELEKTRISCH` 3 ·
`EUR_PRO_KWH_THERMISCH` 2 · `PROZENT_BRENNSTOFFKOSTEN` 1.

**Projekte mit p_E ≠ p_B: genau eines.** `Tab_ProjektWirtschaftlichkeit`
führt zwei Zeilen — 1019 (Zins 3, p_E 0, p_B 0, T 20) und **1030**
(Zins 3, **p_E 2**, **p_B 1,5**, T 20); alle übrigen Projekte rechnen mit
den Vorgabewerten p_E = p_B = 0.

Betriebskosten je Projekt (Summenschleife, Erwartet): 1019/1023/1024 =
**99,0000**; 1030 = **20.000,0000**; alle übrigen 20 Projekte **0,0000**.

**Abgeleitete Erwartung: keine Bestandswirkung** aus allen drei
Bausteinen. Bestätigt in § 3.2.

### 3.2 A/B über alle Projekte × Szenarien — bitgenau kein Unterschied

16 Projekte mit Simulationslauf; je Projekt wird die **ganze
Variantengruppe** gerechnet (die Varianten-IDs werden mitgegeben — sonst
entstünde gar keine Sensitivität, und genau die ändern FX4-a und FX4-c).

Gemessene Zeilen je Stand: **66 KENN** (Kapitalwert, Betriebs-/
Energiekosten, Barwerte, Restwert, Gestehungskosten, Fehlgrund) ·
**48 BETRIEB** · **48 TOPF** (die Topf-Aufteilung selbst, inkl. der
Startjahr-Listen) · **8 SENS** (persistierte Sensitivitätszeilen) =
**170 Zeilen**.

`diff vorher nachher`: **leer**.

Anker, vorher == nachher, zeichengleich:

| Anker | Wert |
|---|---|
| Betrieb 1024 | **99,0000** |
| Invest 1018 / 1024 / 1042 | **45.312,5000 / 12.001,0000 / 13.000,0000** |
| KW 1024 | **−2.219.863,7615** |
| KW 1030 | **−21.875.243,6757** |

**Was dieser Befund beweist — und was nicht.** Er beweist
Bestandsneutralität. Er beweist **nicht** die Richtigkeit von FX4-a und
FX4-c: Der Bestand hat keine Startjahr-Position, und seine einzige
Variantengruppe (Stamm 1019 mit 1023/1024) führt **keine KWKG-Reihe**,
also auch keine Zeile „KWKG-Bonus entfällt". Die Richtigkeit belegen die
synthetischen Proben (§ 3.3–§ 3.5).

### 3.3 FX4-a — Startjahr-Position im Ohne-KWKG-Vergleich

Präparat (frische Kopie, beide Stände identisch vorbereitet):
Rahmen 1030 auf Zins **3 %**, p_E **4 %**, p_B **1 %**, T **15 a**;
Projekt **1024** als Variante an Stamm **1030** gehängt (umgehängt, weil
`Tab_Variante.ID_Projekt` eindeutig ist) — nötig, weil die Sensitivität
nur für eine Variante gegen den Stamm entsteht und nur der BHKW-Stamm 1030
eine KWKG-Reihe führt; die eine Kategorie-2-Zeile der Variante auf
`JAHRESBETRAG` **8.000,00 €/a ab Jahr 3**.

Topf-Ausweis (identisch in beiden Ständen):

```
TOPF 1024  BetriebSofort=0      EndenergieSofort=0  BetriebAbJahr=[(8000 ab 3)]
TOPF 1030  BetriebSofort=20000  EndenergieSofort=0  BetriebAbJahr=[]
```

Zeile „KWKG-Bonus entfällt", **ungerundet** gemessen (über Reflection auf
`BaueEingabe`/`OhneKwkg`/`RechneBild` — die persistierte Zeile rundet
jeden Endpunkt einzeln auf 2 Stellen und taugt deshalb nicht als exakter
Beleg):

| Größe | Wert |
|---|---|
| vorher | **16.916.131,238797504** |
| nachher | **16.829.587,121858936** |
| Δ = nachher − vorher | **−86.544,116938568652** |
| Handrechnung `−Σ_{t=3..15} 8.000 · (1+p_B)^(t−1) · (1+i)^(−t)` | **−86.544,11693856903** |
| Abweichung | **3,783E−10** |

Persistierte (gerundete) Zeile: `MINUS` **16.916.131,24** →
**16.829.587,12**.

**Nichts sonst ändert sich.** Die vier übrigen Sensitivitätszeilen sind
zeichengleich (Zinssatz, Energiepreissteigerung, Investition,
Energiekosten), ebenso alle Kapitalwerte:
1030 = **−19.155.014,308057457**, 1024 = **−2.384.865,6642990923** in
beiden Ständen. Ungerundete Kontrollwerte, vorher == nachher:
`BASIS` **16.770.148,643758364**, `ENERGIE_MINUS_0.9`
**16.998.780,698494416**, `ENERGIE_PLUS_1.1` **16.541.516,589022312**.

### 3.4 FX4-b — die zwei Alt-Arten, mit und ohne Ratendifferenz

Präparat (Projekt **1030**, Rahmen Zins **3 %**, p_E **4 %**, p_B **1 %**,
T **15 a**): eine Kategorie-2-Zeile erst **neutralisiert**
(`JAHRESBETRAG 0` → Restsumme messbar), dann auf die Alt-Art gestellt mit
`Menge = 120.000 €`, `Satz = 1,5 %`.

| Messung | Wert |
|---|---|
| Betriebskosten OHNE die Zeile | **2.000** |
| Betriebskosten MIT der Zeile | **3.800** |
| ⇒ Betrag der Position (Jahr 1) | **1.800** (Handrechnung `Menge × Satz / 100` = **1.800**) |

**Topf-Zuordnung — der eigentliche Nachweis:**

| Stand | `BetriebSofort` | `EndenergieSofort` | `Gesamt` |
|---|---|---|---|
| vorher | **3.800** | **0** | 3.800 |
| nachher | **2.000** | **1.800** | 3.800 |

Identisch für **`PROZENT_BRENNSTOFFKOSTEN`** (Probe `alt`) und
**`PROZENT_STROMKOSTEN`** (Probe `strom`) — beide Arten, beide Zahlen.
Die ausgewiesene Gesamtsumme bleibt 3.800; nur die Fortschreibung ändert
sich.

Jahresreihe des Zahlungsbildes gegen die unabhängige Skript-Handrechnung:

| Stand | ggü. Hand NEU (p_B + p_E) | ggü. Hand ALT (nur p_B) | Barwert Betriebsreihe Hand == Programm |
|---|---|---|---|
| vorher | 0,000E+00 | **0,000E+00** | **48.415,4590** == 48.415,4590 (Diff 0,000E+00) |
| nachher | **0,000E+00** | 1,048E+03 | **53.554,0996** == 53.554,0996 (Diff 0,000E+00) |

Der vorher-Stand rechnet nachweislich beide Töpfe mit p_B, der
nachher-Stand die Alt-Art mit p_E.

| Kapitalwert (Erwartet, Projekt 1030) | Wert |
|---|---|
| vorher | **−18.948.611,561651804** |
| nachher | **−18.953.750,20216959** |
| Δ | **−5.138,6405177861452** |
| Handrechnung `−Σ_t Topf_E·[(1+p_E)^(t−1)−(1+p_B)^(t−1)]·(1+i)^(−t)` | **−5.138,640517788047** |
| Abweichung | **1,902E−09** |

Dieselben Zahlen für `PROZENT_STROMKOSTEN` (Probe `strom`), zeichengleich.

**Gegenprobe p_E == p_B (Probe `altgleich`, beide Raten 1 %):** Der Topf
teilt sich sichtbar auf (`3.800/0` → `2.000/1.800`), das Ergebnis bleibt
bitgenau:

| Stand | Kapitalwert | Barwert Betriebsreihe |
|---|---|---|
| vorher | **−15.549.275,921655215** | 48.415,4590 |
| nachher | **−15.549.275,921655215** | 48.415,4590 |

Größte Abweichung der Jahresreihe gegen „Hand ALT" im nachher-Stand:
**4,547E−13** — die Umstellung wirkt ausschließlich über die Differenz der
beiden Raten.

### 3.5 FX4-c — Sensitivität „Energiekosten ±10 %"

Präparat: Rahmen 1030 wie oben, 1024 als Variante angehängt, die eine
Kategorie-2-Zeile der **Variante** auf `PROZENT_ENDENERGIEKOSTEN` mit
erfasstem Wert **1.800,00 €/a** (Satz bewusst leer: dann greift die
I-2-Regel und der Betrag ist exakt vorgegeben statt vom Lauf abhängig —
die **Topf-Zuordnung hängt allein an der Bemessungsart**, FX3 § 1.4).

```
TOPF 1024  BetriebSofort=0  EndenergieSofort=1800   (identisch in beiden Ständen)
TOPF 1030  BetriebSofort=20000  EndenergieSofort=0
```

Ungerundete Ausschläge (Reflection auf `RechneBild`):

| Größe | vorher | nachher | Δ | Handrechnung | Abweichung |
|---|---|---|---|---|---|
| BASIS (f = 1,0) | 16.828.620,48168963 | 16.828.620,48168963 | **0** | 0 | — |
| Energie **−10 %** (f = 0,9) | 17.057.252,53642568 | **17.060.059,764326412** | **+2.807,2279007323086** | **+2.807,2279007305233** | **1,785E−09** |
| Energie **+10 %** (f = 1,1) | 16.599.988,426953577 | **16.597.181,199052846** | **−2.807,227900730446** | **−2.807,2279007305233** | **7,731E−11** |
| OHNE_KWKG | 16.888.058,9597902 | 16.888.058,9597902 | **0** | 0 | — |

Handrechnung: `Δ = −(f−1) · Σ_{t=1..15} Topf_E · (1+p_E)^(t−1) · (1+i)^(−t)`
mit Barwert des p_E-Topfes **28.072,27900730523**; also ±0,1 davon.

Persistierte (gerundete) Zeile „Energiekosten Variante ±10 % (inkl.
CO₂-Abgabe)": `MINUS` **17.057.252,54 → 17.060.059,76**, `PLUS`
**16.599.988,43 → 16.597.181,20**. Die Differenz der gerundeten Endpunkte
(**+2.807,22** bzw. **−2.807,23**) weicht bis 0,01 von der Handrechnung ab
— das ist ausschließlich die `Math.Round(…, 2)` je Endpunkt in
`BaueSensitivitaet`; die ungerundete Messung darüber ist der Beleg.

**Die drei übrigen Ausschläge sind zeichengleich** (Zinssatz
`18.207.984,27/15.594.413,78`, Energiepreissteigerung
`15.752.291,68/17.999.729,83`, Investition `16.829.820,58/16.827.420,38`),
ebenso alle Kapitalwerte (1030 **−19.155.014,308057457**, 1024
**−2.326.393,826367829**).

**Gegenprobe ohne Endenergie-Position (Probe `sensohne`):** dieselbe
Gruppe, dieselben 1.800 €/a, aber als `BETRAG` (p_B-Topf). Alle
Sensitivitätszeilen und alle vier ungerundeten Kontrollwerte sind
**bitgenau identisch**:

```
ROH BASIS              16833759.122207418   (vorher == nachher)
ROH ENERGIE_MINUS_0.9  17062391.17694347
ROH ENERGIE_PLUS_1.1   16605127.067471365
ROH OHNE_KWKG          16893197.60030799
```

### 3.6 Selbstprüfung der Mehrjahrestabelle — grün in allen Proben

Geprüft wird „kumuliert(T) + Restwert-Barwert = Kapitalwert" und
zusätzlich „Summe der Positionsspalten == Spalte NETTO", je Projekt der
Gruppe, in beiden Ständen.

| Probe / Stand | Projekt | kumuliert(T) | + Restwert | = Kapitalwert | Diff | Σ Spalten − NETTO |
|---|---|---|---|---|---|---|
| starta, vorher/nachher | 1030 | −19.175.874,8213 | 20.860,5133 | −19.155.014,3081 | **0,000E+00** | 2,328E−10 |
| starta, vorher/nachher | 1024 | −2.384.865,6643 | 0,0000 | −2.384.865,6643 | **0,000E+00** | −5,821E−11 |
| alt, vorher | 1030 | −18.969.472,0749 | 20.860,5133 | −18.948.611,5617 | **0,000E+00** | 2,328E−10 |
| alt, nachher | 1030 | −18.974.610,7155 | 20.860,5133 | −18.953.750,2022 | **0,000E+00** | 4,657E−10 |
| altgleich, vorher/nachher | 1030 | −15.570.136,4349 | 20.860,5133 | −15.549.275,9217 | **0,000E+00** | 2,328E−10 |
| strom, vorher | 1030 | −18.969.472,0749 | 20.860,5133 | −18.948.611,5617 | **0,000E+00** | 2,328E−10 |
| strom, nachher | 1030 | −18.974.610,7155 | 20.860,5133 | −18.953.750,2022 | **0,000E+00** | 4,657E−10 |
| sens, vorher/nachher | 1030 | −19.175.874,8213 | 20.860,5133 | −19.155.014,3081 | **0,000E+00** | 2,328E−10 |
| sens, vorher/nachher | 1024 | −2.326.393,8264 | 0,0000 | −2.326.393,8264 | **0,000E+00** | −2,910E−11 |
| sensohne, vorher/nachher | 1030 | −19.175.874,8213 | 20.860,5133 | −19.155.014,3081 | **0,000E+00** | 2,328E−10 |
| sensohne, vorher/nachher | 1024 | −2.321.255,1859 | 0,0000 | −2.321.255,1859 | **0,000E+00** | −2,910E−11 |

Spaltenbestand unverändert: `INVEST_ERSATZ, BETRIEB, ENERGIE, BEHG,
[KWKG_ZUSCHLAG,] NETTO, BARWERT, KUMULIERT`.

### 3.7 Build und Hygiene

- `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64` → **exit 0**,
  **39 Warnungen** (Warnungsbild unverändert; derselbe Wert im
  vorher-Build aus dem Scratchpad).
- Sweep `git grep -n "^<<<<<<<" -- "*.cs"` → **0 Treffer**.
- `git status --porcelain`: die vier `.cs` dieses Pakets, dieses Protokoll
  (unversioniert) sowie die **fremden**, schon vorher offenen Einträge
  `CLAUDE.md`, `WindowsFormsApplication1/CLAUDE.md` (beide `M`) und
  `WindowsFormsApplication1/STAND.md` (`??`) — nicht angefasst.
- Kodierung: alle vier bearbeiteten `.cs` behalten ihren BOM-Zustand
  (`KapitalwertRechner`/`WirtschaftlichkeitCtrl`/`WirtschaftlichkeitZeilen`
  ohne BOM, `DbWerte.cs` mit BOM — je wie im HEAD).
- **Keine neuen Text-Keys**, keine `.resx`, keine Designer-Datei, keine
  `Views/`-Datei.

## 4. Geänderte Dateien

```
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs   FX4-a: OhneKwkg kopiert BetriebAbJahr
                                                         FX4-b: IstEnergiepreisArt (neu), Topf-Zuordnung
                                                         FX4-c: RechneBild skaliert Endenergie/-AbJahr
                                                         + Doku an BetriebsTopfe, ProjektEingabe,
                                                           IstEndenergieArt
Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs       nur Doku (Klassenkopf, Parameter endenergieJahr)
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs nur Doku (Kommentar an der Spalte BETRIEB)
Allgemein/DbWerte.cs                                     nur Doku (H1-Block, die zwei Alt-Art-Konstanten)
Allgemein/Reporting/FX4_FX3Folgepunkte_Protokoll.md      dieses Protokoll
```

`EndenergieAufloeser.cs` wurde **nicht** gebraucht — die Mengenermittlung
ist von FX4 nicht berührt.

## 5. Offene Punkte

| Nr. | Punkt |
|---|---|
| **FX3-1** | Eigene **Spalte** für den p_E-Anteil in der Mehrjahrestabelle (`Zahlungsbild.EndenergieAnteilJeJahr` liegt bereit, es fehlt der Anzeigetext `WIRT_MJ_*`). **Bewusst nicht in FX4** — kommt gebündelt mit B6. |
| **FX3-3** | Konzeptnachzug in der Repo-Wurzel: `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` führt R-2 (Zeile 1235) noch als offenen Befund; jetzt kommt FX4 dazu. Wurzelkonzepte absichtlich nicht angefasst. |
| **FX4-1** | Der Sensitivitäts-Ausschlag „Investition Variante ±10 %" skaliert **nicht** die davon abgeleiteten Betriebskosten (`PROZENT_INVESTITION`, 20 der 66 Kategorie-2-Zeilen). Spiegelbildlich zu FX3-5 — **Anwenderentscheid** (§ 1.4). |
| **FX4-2** | `WirtschaftlichkeitErgebnis.BetriebskostenJahr` = `Betrieb + Endenergie` zählt **nur die Sofort-Anteile**; `LiesBetriebskosten(id, szenario)` (Anzeigen, `UcBkKosten`) liefert dagegen `Gesamt` **inklusive** der Startjahr-Beträge. In der Probe § 3.3 steht deshalb `BK = 0` bei 8.000 €/a ab Jahr 3. Bestandsverhalten seit KD6, von FX4 nicht berührt; beide Lesarten sind für sich begründbar („Jahr-1-Kosten" vs. „Kosten p. a."), aber sie stehen nebeneinander. **Anwenderentscheid**, ob das vereinheitlicht wird. |
| **FX4-3** | Sichtabnahme in der Oberfläche steht aus (Reiter Wirtschaftlichkeit, Sensitivitätstabelle im Bericht) — die Nachweise hier sind rechnerisch, nicht visuell. |

Harness (gitignored): `..\dev\fx4\` — Schritte
`erhebung | anker | ab | topf <projekt> | probe [starta|alt|altgleich|strom|sens|sensohne] | sql "<select>"`,
Treiber `..\dev\fx4\lauf.ps1 -Stand vorher|nachher -Schritt … [-Arg …]`.
