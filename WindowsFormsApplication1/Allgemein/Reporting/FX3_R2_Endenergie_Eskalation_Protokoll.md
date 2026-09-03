# FX3 — Befund R-2: Endenergie-Betriebskosten eskalieren mit p_E (Umsetzungsprotokoll)

Fortsetzung von FX1/FX2 (Anwenderdurchsicht der Rechenwege-Formelkarte). Ein
Entscheid vom 02.09.2026, wörtlich: **„R2: Umsetzen wie vorgeschlagen".**
Vorgeschlagen war: Betriebskostenpositionen mit Endenergie-Bemessung werden in
der Mehrjahresrechnung mit der **Energie**preissteigerung p_E eskaliert statt
mit der **Betriebs**preissteigerung p_B — konsistent durch Kapitalwert-Jahresreihe
UND Mehrjahrestabelle, einschließlich Startjahr-Zeilen (KD6) und
VALERI-Szenarien. Stand 02.09.2026 spät, Branch `lokal_dirk`, Basis HEAD
`5d86655` (SQLite-Betrieb).

Befundtext (Konzept konsolidiert, Zeile 1235): *„R-2 · Hilfsenergie steigt mit
p_B statt p_E (bei gleichen Sätzen null)"*. Fachlich: VDI 2067 und
DIN EN 17463 ordnen **bedarfsgebundene** Kosten der Energiepreisentwicklung zu;
eine Position „x % der Endenergiekosten" wächst der Sache nach mit den
Energiepreisen.

## 1. Umsetzung — vier Baustellen

### 1.1 Zwei Töpfe statt einem (`WirtschaftlichkeitCtrl`)

Neu: `BetriebsTopfe` (`internal sealed class`) und
`LiesBetriebskostenTopfe(idProjekt, szenario)`. Die eine Leseschleife der
Kategorie-2-Positionen füllt seither **zwei** Akkumulatoren:

| Topf | Inhalt | Preissteigerung |
|---|---|---|
| `BetriebSofort` / `BetriebAbJahr` | alles Bisherige | p_B |
| `EndenergieSofort` / `EndenergieAbJahr` | `PROZENT_ENDENERGIEKOSTEN`, `PROZENT_ENDENERGIEBEDARF` | **p_E** |

**Die Zuordnung entscheidet die Bemessungsart, nicht der Betrag** — eine Zeile,
deren Ableitung von einem gepflegten Best-/Worst-Case-Wert geschlagen wurde
(VALERI-Vorfahrt), bleibt eine Endenergie-Position (§ 1.4).

Öffentliche Fläche: **keine Signatur gebrochen.** Die beiden vorhandenen
Überladungen bleiben und rechnen unverändert die Gesamtsumme —
`LiesBetriebskosten(id, szenario)` (Anzeigen, `UcBkKosten`) liefert weiter
`Gesamt`, `LiesBetriebskosten(id, szenario, out abJahr)` fasst beide Töpfe
wieder zusammen (Sicht vor FX3) und ist als solche dokumentiert. Nur
`BaueEingabe` liest die neue Topf-Sicht.

`ProjektEingabe` bekommt `Endenergie` + `EndenergieAbJahr`;
`OhneKwkg` (Novellen-Szenario) kopiert beide mit — sonst rechnete das Szenario
gegen andere Betriebskosten als die Basis. Der **Ausweis** bleibt die
Gesamtzahl: `erg.BetriebskostenJahr = eingabe.Betrieb + eingabe.Endenergie`.

### 1.2 `KapitalwertRechner`

`Rechne(…)` nimmt zwei zusätzliche **optionale** Parameter am Ende:
`double endenergieJahr = 0` und
`IList<KeyValuePair<double,int>> endenergieAbJahr = null`. In der Jahresschleife:

```
endenergieT      = endenergieJahr + Σ (Startjahr-Positionen mit t ≥ Startjahr)
endenergieAusgabe = endenergieT ≠ 0 ? endenergieT × (1+p_E)^(t−1) : 0.0
ausgaben          = ( … Bestandsausdruck unverändert … ) + endenergieAusgabe
```

**Der Bestandszweig ist unangetastet**: Die „eine Klammer"
`(energieJahr + behgJahr) × (1+p_E)^(t−1)` steht Zeichen für Zeichen wie vorher,
beide K6-Zweige ebenso; angehängt wird ausschließlich ein Summand, der ohne
Endenergie-Topf eine echte `0.0` ist (deshalb bitgenaue Bestandsneutralität,
§ 3). Index 0 = Einmalzahlung/Investitionsjahr bleibt Index 0.

Neues Feld am `Zahlungsbild`: `EndenergieAnteilJeJahr` — **reiner Ausweis**,
Teilmenge von `BetriebJeJahr`, nirgends aufsummiert.

### 1.3 Mehrjahrestabelle (`WirtschaftlichkeitZeilen.Mehrjahresbild`)

`BetriebJeJahr[t]` trägt seither die **Summe beider Töpfe**
(`betriebT × (1+p_B)^(t−1) + endenergieAusgabe`). Genau deshalb bleibt die
Tabelle unverändert baubar: Die Summe der Positionsspalten ist weiterhin die
Spalte „Netto nominal", und die **Selbstprüfung** unter der Tabelle
(kumuliert(T) + Restwert = Kapitalwert) bleibt gültig — sie wurde **nicht**
aufgeweicht und läuft in allen vier Proben grün (§ 4).

Eine **eigene Spalte** bekäme der p_E-Anteil erst mit einem eigenen Anzeigetext;
das hieße `.resx` anfassen und ist deshalb nicht Teil dieses Pakets (offener
Punkt FX3-1). Der Code ist an der Stelle kommentiert.

### 1.4 VALERI-Szenarien

Die Topf-Zuordnung geschieht **vor** der Szenarienvorfahrt und hängt allein an
`Bemessung`. Ein gepflegter BEST-/WORST-Betrag auf einer Endenergie-Zeile
schlägt weiterhin die Ableitung (unverändert), eskaliert aber mit p_E wie der
Erwartungswert — sonst führe dasselbe Projekt in BEST und in ERWARTET mit
verschiedenen Preisraten. Nachweis § 4.3.

## 2. Dokumentierte Grenzen (bewusst NICHT umgestellt)

| Gegenstand | Bleibt | Begründung |
|---|---|---|
| `PROZENT_BRENNSTOFFKOSTEN`, `PROZENT_STROMKOSTEN` | **p_B** | Alt-Vorläufer von Weg A, projektweit statt anlagenscharf; sie laufen aus und sollen sich nicht mehr ändern (Anwenderentscheid). Bestand: **1** Zeile, Satz 0, Wert 0 |
| **„Weg C" der Hilfsenergie** = fester Jahresbetrag (`JAHRESBETRAG`/`BETRAG`) | **p_B** | Ein fester Betrag trägt keine Endenergie-Bemessung. Bestand Kat. 2: 19 × `JAHRESBETRAG` + 5 × `BETRAG` + 9 × NULL |
| „Eine Klammer" `(Energie_1 + CO2_1) × (1+p_E)^(t−1)` | **unverändert** | Keine Umformung, keine Refaktorierung — nur ein Summand angehängt |
| Sensitivität „Energiekosten ±10 %" | wirkt **nicht** auf den Endenergie-Topf | Der Ausschlag fragt nach den Energiekosten des Simulationslaufs; die Bezugsgröße der %-Zeile ist ein Ergebniswert des Laufs, kein Preisparameter. Die Sensitivität „Energiepreissteigerung ±" wirkt sehr wohl (sie kommt über `preisstEnergie` herein) — im Code begründet |

Zur Begriffsklärung: „Weg C" ist der dritte, vereinfachte Weg der
**Hilfsenergie-Bemessung** nach Konzept § 4.5 (A = % der Endenergiekosten,
B = % des Endenergiebedarfs, C = fester Jahresbetrag) — so benannt in
`KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` und
`Wirtschaftlichkeit_Kosten/Rechenweg/02_Betriebskosten_BHKW.md`.

## 3. Bestandserhebung (Produktiv-Kopie, lesend)

Alle Zahlen aus `C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite` (Kopie samt `-wal`
in den Scratchpad; produktiv wurde nie geschrieben).

**Kategorie-2-Zeilen mit den zwei Endenergie-Arten — 7 Stück in 3 Projekten,
keine einzige wirksam:**

| Projekt | ID | Komponente | ID_Anlage | Wert / Best / Worst | Menge | Satz | StartJahr |
|---|---|---|---|---|---|---|---|
| 1019 | 101600599 | 1 (WP) | 14922 | 0 / 0 / 0 | NULL | **NULL** | NULL |
| 1019 | 101600605 | 1 (WP) | 14923 | 0 / 0 / 0 | NULL | **NULL** | NULL |
| 1026 | 101600570 | 1 (WP) | 14917 | 0 / 0 / 0 | NULL | **NULL** | NULL |
| 1026 | 101600576 | 2 (Kessel) | 11275 | 0 / 0 / 0 | NULL | **NULL** | NULL |
| 1030 | 101600587 | 2 (Kessel) | 11334 | 0 / 0 / 0 | NULL | **NULL** | NULL |
| 1030 | 101600590 | 7 (BHKW) | 14920 | 0 / 0 / 0 | NULL | **NULL** | NULL |
| 1030 | 101600593 | 7 (BHKW) | 14921 | 0 / 0 / 0 | NULL | **NULL** | NULL |

`PROZENT_ENDENERGIEBEDARF`: **0 Zeilen**. Kategorie 1: keine Endenergie-Zeile.
Bemessungsverteilung Kategorie 2 (66 Zeilen): `PROZENT_INVESTITION` 20 ·
`JAHRESBETRAG` 19 · NULL 9 · `PROZENT_ENDENERGIEKOSTEN` 7 · `BETRAG` 5 ·
`EUR_PRO_KWH_ELEKTRISCH` 3 · `EUR_PRO_KWH_THERMISCH` 2 ·
`PROZENT_BRENNSTOFFKOSTEN` 1. Zeilen mit `StartJahr > 1`: **0** im gesamten
Bestand.

**Rahmenparameter** (`Tab_ProjektWirtschaftlichkeit`) — nur **zwei** Zeilen:

| ID_Projekt | Zins | p_E | p_B | T |
|---|---|---|---|---|
| 1019 | 3 | 0 | 0 | 20 |
| **1030** | 3 | **2** | **1,5** | 20 |

Alle übrigen Projekte haben keine Zeile und rechnen mit den Vorgabewerten
(p_E = p_B = 0). **Genau ein Projekt hat p_E ≠ p_B: 1030** — und dessen drei
Endenergie-Zeilen tragen keinen Satz, liefern also 0,00 €/a (seit FX2/I-2: der
erfasste Wert, hier ebenfalls 0).

**Abgeleitete Erwartung: keine Bestandswirkung.** Bestätigt in § 4.1.

Betriebskosten je Projekt (Summenschleife, Erwartet): 1019/1023/1024 =
**99,0000**; 1030 = **20.000,0000**; alle übrigen 20 Projekte **0,0000**.

## 4. Nachweise

Werkzeug: Wegwerf-Harness `..\dev\fx3\` (gitignored, `ProjectReference` statt
DLL-HintPath, `DataRepository.EngineModus()`), A/B über **DLL-Tausch** im
Ausgabeordner — vorher `EPOS_Plan.dll` MD5 `6787baf40e265eea4acfbf728c27fa80`
(HEAD 5d86655), nachher `b85079f18b21dc161f51dcea7fb4a319` (FX3). Je Lauf eine
**frische** Kopie der Produktivdatenbank; Zahlen im Round-Trip-Format („R"),
damit auch die letzte Gleitkommastelle sichtbar ist.

### 4.1 A/B über alle Projekte — bitgenau kein Unterschied

16 Projekte mit Simulationslauf × 3 Szenarien = **48 Kennzahlzeilen**
(Kapitalwert, Betriebs-/Energiekosten, Barwerte, Restwert, Gestehungskosten,
Fehlgrund) plus 48 Betriebskostenzeilen. `diff vorher nachher`: **leer**.

Anker, vorher == nachher, zeichengleich:

| Anker | Wert |
|---|---|
| Betrieb 1024 | **99,0000** |
| Invest 1018 / 1024 / 1042 | **45.312,5000 / 12.001,0000 / 13.000,0000** |
| KW 1024 | **−2.219.863,7615** |
| KW 1030 | **−21.875.243,6757** |

Das ist der erwartete Befund aus § 3: Das einzige Projekt mit p_E ≠ p_B (1030)
hat keine **wirksame** Endenergie-Position. **Keine geheilte Zuordnung im
Bestand — die Wirkung von R-2 tritt erst ein, wenn jemand einen Satz pflegt.**

### 4.2 Synthetische Grundprobe (frische Kopie, Projekt 1030)

Präparat: Rahmen auf Zins **3 %**, p_E **4 %**, p_B **1 %**, T **15 a**; Zeile
**101600590** auf `PROZENT_ENDENERGIEKOSTEN`, Satz **2 %**, `ID_Anlage = NULL`
(Komponentensumme BHKW: Module „BHKW EW M 50 S [K] Erdgas" 862,18 MWh und
„EC-POWER XRGI 9" 186,09 MWh, Träger 63).

| Größe | Wert |
|---|---|
| Betriebskosten vor dem Präparat | 20.000,0000 |
| Betriebskosten nach dem Präparat | **21.677,2320** |
| ⇒ Endenergie-Topf (Jahr 1) | **1.677,2320 €/a** |

Jahresreihe (Auszug, nachher-Stand; „Hand" = Skriptformel, unabhängig vom
Programmcode gerechnet):

| t | Hand NEU (p_B + p_E) | Hand ALT (nur p_B) | Programm `BetriebJeJahr` | davon p_E-Anteil |
|---|---|---|---|---|
| 1 | 21.677,2320 | 21.677,2320 | **21.677,2320** | 1.677,2320 |
| 2 | 21.944,3213 | 21.894,0043 | **21.944,3213** | 1.744,3213 |
| 5 | 22.774,2044 | 22.557,4145 | **22.774,2044** | 1.962,1242 |
| 10 | 24.260,9296 | 23.708,0694 | **24.260,9296** | 2.387,2241 |
| 15 | 25.893,9074 | 24.917,4192 | **25.893,9074** | 2.904,4232 |

- nachher gegen Hand NEU: größte Abweichung **−3,638E−12**; p_E-Anteil
  **−4,547E−13**; Barwert der Betriebsreihe Hand **280.975,8303** == Programm
  **280.975,8303** (Diff **0,000E+00**).
- vorher gegen Hand **ALT**: größte Abweichung **0,000E+00** — der alte Stand
  rechnet nachweislich beide Töpfe mit p_B; Barwert Programm **276.187,6679**.

| Kapitalwert (Erwartet) | Wert |
|---|---|
| vorher (HEAD 5d86655) | **−19.176.383,770474706** |
| nachher (FX3) | **−19.181.171,932870780** |
| Δ = nachher − vorher | **−4.788,1623960733** |
| Handrechnung `−Σ_t Topf_E·[(1+p_E)^(t−1)−(1+p_B)^(t−1)]·(1+i)^(−t)` | **−4.788,1623960726** |
| Abweichung | **−7,4E−10** |

`BetriebskostenJahr` bleibt in **beiden** Ständen **21.677,232** — der Ausweis
schrumpft nicht, nur die Fortschreibung ändert sich.

### 4.3 Startjahr-Probe (`StartJahr = 3`) und VALERI-Probe (BEST)

**Startjahr:** dieselbe Zeile mit `StartJahr = 3`. Betriebsreihe t=1
**20.000,0000**, t=2 **20.200,0000** (nur p_B-Topf), ab t=3 beide Töpfe
(t=3 = **22.216,0941**). Abweichung gegen Hand NEU **−3,638E−12**.

| Kapitalwert (Erwartet) | Wert |
|---|---|
| vorher | **−19.173.158,628350094** |
| nachher | **−19.177.899,362185510** |
| Δ | **−4.740,7338354178** |
| Handrechnung (Summe erst ab t = 3) | **−4.740,7338354166** (Abw. −1,2E−9) |

**VALERI:** `BestCase = 5.000` auf derselben Zeile, Szenario **BEST**. Der
gepflegte Szenariowert schlägt die Ableitung (Betrieb Best = **25.000,0000**,
Endenergie-Topf **5.000,0000**) und eskaliert mit p_E: Programm gegen Hand NEU
**0,000E+00** (exakt), gegen Hand ALT bis **2.911,0112** im Jahr 15.

| Kapitalwert BEST | Wert |
|---|---|
| vorher | **−19.218.718,859417222** |
| nachher | **−19.232.992,860855527** |
| Δ | **−14.274,0014383048** |
| Handrechnung | **−14.274,0014383001** (Abw. −4,7E−9) |

Im selben Lauf liefert ERWARTET vorher/nachher **−19.176.383,770474706** /
**−19.181.171,932870780** — BEST und ERWARTET eskalieren also mit derselben
Rate; genau das war die Forderung des Entscheids.

### 4.4 Selbstprüfung der Mehrjahrestabelle — grün in allen Proben

Geprüft wird die ausgewiesene Probe „kumuliert(T) + Restwert-Barwert =
Kapitalwert" und zusätzlich „Summe der Positionsspalten == Spalte NETTO".

| Probe / Stand | kumuliert(T) | + Restwert | = Kapitalwert | Diff | Σ Spalten − NETTO |
|---|---|---|---|---|---|
| Grund, vorher | −19.197.244,2838 | 20.860,5133 | −19.176.383,7705 | **0,000E+00** | 2,328E−10 |
| Grund, nachher | −19.202.032,4462 | 20.860,5133 | −19.181.171,9329 | **0,000E+00** | 4,657E−10 |
| Startjahr, vorher | −19.194.019,1416 | 20.860,5133 | −19.173.158,6284 | **0,000E+00** | 2,328E−10 |
| Startjahr, nachher | −19.198.759,8755 | 20.860,5133 | −19.177.899,3622 | **0,000E+00** | 4,657E−10 |
| VALERI, vorher | −19.239.579,3727 | 20.860,5133 | −19.218.718,8594 | **0,000E+00** | 2,328E−10 |
| VALERI, nachher | −19.253.853,3741 | 20.860,5133 | −19.232.992,8609 | **0,000E+00** | 4,657E−10 |

Spaltenbestand unverändert: `INVEST_ERSATZ, BETRIEB, ENERGIE, BEHG,
KWKG_ZUSCHLAG, NETTO, BARWERT, KUMULIERT`. Der Kapitalwert der Tabelle ist
Zeichen für Zeichen der Kennzahl-Kapitalwert desselben Laufs.

### 4.5 Gegenprobe p_E == p_B — bitgenau ergebnisneutral

Dieselbe **wirksame** Endenergie-Position (1.677,2320 €/a), aber p_E = p_B =
1 %:

| Stand | Kapitalwert | Barwert Ausgaben |
|---|---|---|
| vorher | **−15.777.048,130478121** | 15.447.347,121869087 |
| nachher | **−15.777.048,130478121** | 15.447.347,121869087 |

Identisch **bis zur letzten Gleitkommastelle** — die Umstellung wirkt
ausschließlich über die Differenz der beiden Raten, wie der Befundtext
(„bei gleichen Sätzen null") es beschreibt.

### 4.6 Build und Hygiene

- `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64` → **exit 0**,
  **39 Warnungen** (Warnungsbild unverändert).
- Sweep `git grep -n "^<<<<<<<" -- "*.cs"` → **0 Treffer**.
- Kodierung: alle vier bearbeiteten `.cs` behalten ihren BOM-Zustand
  (`KapitalwertRechner`/`WirtschaftlichkeitCtrl`/`WirtschaftlichkeitZeilen` ohne
  BOM, `DbWerte.cs` mit BOM — je wie im HEAD).

## 5. Geänderte Dateien

```
Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs      endenergieJahr/-AbJahr, EndenergieAnteilJeJahr
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs  BetriebsTopfe, LiesBetriebskostenTopfe,
                                                        ProjektEingabe.Endenergie, RechneBild, OhneKwkg,
                                                        BetriebskostenJahr = beide Töpfe
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs  nur Kommentar an der Spalte BETRIEB
Allgemein/DbWerte.cs                                    nur Doku am Endenergie-Block (Umstellung + Grenzen)
Allgemein/Reporting/FX3_R2_Endenergie_Eskalation_Protokoll.md  dieses Protokoll
```

Keine `.resx`, keine Designer-Datei, keine `Views/`-Datei angefasst. **Keine
neuen Text-Keys** — es war kein neuer Anzeigetext nötig.

## 6. Offene Punkte

| Nr. | Punkt |
|---|---|
| **FX3-1** | Eigene **Spalte** für den p_E-Anteil in der Mehrjahrestabelle. Die Zahl liegt bereit (`Zahlungsbild.EndenergieAnteilJeJahr`), es fehlt nur der Anzeigetext (`WIRT_MJ_*`) — also ein `.resx`-Nachtrag. Solange sie fehlt, sieht der Leser die zwei Raten in der Spalte „Betrieb" nur als Summe. **Anwenderentscheid.** |
| **FX3-2** | `OhneKwkg` (Novellen-Szenario) kopiert seit KD6 `BetriebAbJahr` **nicht** mit — eine Bestandslücke, die FX3 nicht angefasst hat; die neuen Felder `Endenergie`/`EndenergieAbJahr` werden korrekt kopiert. Bestandswirkung heute **null** (0 Zeilen mit `StartJahr > 1`). Schließen ändert Zahlen und ist deshalb ein eigener Entscheid. |
| **FX3-3** | Konzeptnachzug: `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` führt R-2 noch als offenen Befund (Zeile 1235) und V-G1 verweist darauf. Die Wurzel-Konzepte wurden absichtlich nicht angefasst (parallele Sitzung). |
| **FX3-4** | Nicht umgestellt und **nicht beauftragt**: `PROZENT_BRENNSTOFFKOSTEN`/`PROZENT_STROMKOSTEN` (§ 2). Falls diese Alt-Arten weiterleben sollen, wäre dieselbe Zuordnung dort fachlich genauso begründbar. |
| **FX3-5** | Der Endenergie-Topf wird vom Sensitivitäts-`energieFaktor` („Energiekosten ±10 %") **nicht** skaliert (§ 2, im Code begründet). Wer will, dass der Ausschlag die Hilfsenergie mitzieht, trifft damit eine Fachentscheidung über die Bedeutung des Ausschlags. |

Harness (gitignored): `..\dev\fx3\` — Schritte `erhebung | anker | ab |
probe [start|valeri|gleich] | sql`.
