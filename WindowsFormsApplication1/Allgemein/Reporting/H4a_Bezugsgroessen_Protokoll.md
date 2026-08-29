# H4a — Rückfall-Bezugsgrößen: % der Investition, je kWh thermisch/elektrisch (Protokoll)

Etappe H4a des Pflichtpositionen-Vorhabens — erster Teil des offenen Punkts **H1-1b**
aus [`H2_Endenergie_Protokoll.md`](H2_Endenergie_Protokoll.md), umgesetzt am
**29.08.2026** auf Branch `Pufferspeicher`; Commit ohne Push (Etappenregel).

Definitionsgrundlage ist das Kostendialoge-Konzept § 5.3: „% der Investition" = Summe
der Investitionskosten der Komponente **vor Zuschussabzug**; „je kWh thermisch" =
erzeugte Wärme, „je kWh elektrisch" = erzeugter bzw. bezogener Strom aus dem
Simulationslauf. **„% der Erzeugerkosten" gehört zum Investitionsraster und bleibt
H4b**, ebenso die sechs Gerätewelt-Arten (kW/kWp/m²/Kapazität).

---

## 1 Umgesetzt

### 1.1 Die Regel: Rückfall statt Frischzwang

Anders als die Endenergie-Arten (H2: Menge **immer** frisch, per Konzept § 4.5) holen
die H4a-Arten ihre Bezugsgröße **nur, wenn keine Menge gepflegt ist** — eine
persistierte Menge behält Vorrang (der Alt-BHKW-Dialog schreibt welche; VALERI-Geist
„gepflegt schlägt abgeleitet"). Damit ist der Bestand konstruktiv unangetastet, und
die H3-Pflichtzeilen (Instandhaltung „% der Investition" — bislang 0 € trotz
gepflegtem Satz) werden erstmals rechenfähig.

### 1.2 Bausteine

| Baustein | Inhalt |
|---|---|
| `BetriebskostenCtrl.InvestSummeFuer` (neu, internal) | „% der Investition" **stufig**: Investitionszeilen an genau dieser Anlage → sonst Komponentensumme (die § 5.3-Regel) → notfalls Projektsumme. Kern ist die vorhandene K5-Abfrage (Zuschuss-Ausschluss, Kostenart-Toleranz) — um einen optionalen Anlagenfilter erweitert (Spaltenprobe Schritt 45), die Alt-Überladung delegiert unverändert |
| `EndenergieAufloeser.WaermeerzeugungKwh` / `StromgroesseKwh` (neu) | Laufmengen anlagenscharf über den Bezeichner, Komponentensumme als Rückfall — dasselbe H2-Verfahren. Wärme: BHKW/Heizkessel/Wärmepumpe/Solarthermie; Strom: BHKW/PV = Erzeugung, Wärmepumpe = Bezug (Stromverbrauch + Heizstab); übrige Komponenten bewusst null |
| `EndenergieAufloeser.FuerProjekt` | liefert jetzt auch **ohne Simulationslauf** eine Instanz — die Investitionsart braucht keinen Lauf; Lauf-Größen bleiben dann null (Festlegungstreu) |
| `WirtschaftlichkeitCtrl` | gemeinsamer Helfer `KomponenteUndAnlage` (H2-Refaktor), Artenweiche `IstRueckfallErmittelbareArt` + `RueckfallMenge`; eingebaut in Summenschleife **und** E7-Positionsliste (muss deren Summe treffen) |

## 2 Nachweise

**Build:** VS-MSBuild x64, OutDir umgeleitet — **grün**, Warnprofil = Altbestand
(2× CS0108, 2× CS0109, 1× CS1998). Erste Etappe auf der umbenannten `EPOS_Plan.dll`.

**Harness `..\dev\h4a\`** (gitignored; lesend gegen Produktiv, schreibend nur gegen
die Scratchpad-Kopie mit den H3-Pflichtzeilen):

| Probe | Ergebnis | Soll |
|---|---|---|
| [1] Bestand (Produktiv): alle 7 Zeilen der drei Arten | **Satz leer, Betrag 0** — der Rückfall ändert am Bestand nichts | strikte Neutralität |
| [2] „% der Investition": Kessel-Pflichtzeile (K2/A14854), Testbasis 50.000 € über den App-Schreibweg angelegt, Satz 2 % | **1.000,00 €/a**, Menge 50.000 in der Positionszeile | 1.000,00 |
| [2] „je kWh thermisch" **anlagenscharf**: Wartung der Haupt-WP (A14817), Satz 0,005 €/kWh | Menge **211.140 kWh** = exakt die SQL-Wärmesumme der Modulzeile „CS6800iAW MB + AW 10 OR-T"; **1.055,70 €/a** | 211,1 MWh × 0,005 |
| [2] Summen | `LiesBetriebskosten` = **2.055,70** = Erwartung; **E7-Probe: Positionssumme == Summenschleife JA** | deckungsgleich |
| [3] Regressionsläufe 1010/1018/1024/1035 | fehlerfrei; 1024 weiter 99,00 €/a (Bestandswert) | unverändert |

## 3 Offen (Fortschreibung H1-1b)

| Nr. | Punkt |
|---|---|
| ~~H4a~~ | ~~Kosten- und Laufwelt~~ — **erledigt mit dieser Etappe** |
| H4b | Gerätewelt-Arten (je kW Leistung/Heizleistung/elektrisch, je kWp, je kWh Kapazität, je m² Kollektorfläche — Leseketten über die Anlagen-Geräteverweise) und „% der Erzeugerkosten" samt Investitionsraster-Lesepunkt |
| H2-1 | Mengen-Ausweis beim Dialog-Speichern („Stand des Laufs vom …") — Dialogetappe |

## 4 Geänderte Dateien

```
Controller/BetriebskostenCtrl.cs                       InvestSumme + Anlagenfilter,
                                                        InvestSummeFuer, Spaltenprobe
Allgemein/Wirtschaftlichkeit/EndenergieAufloeser.cs    WaermeerzeugungKwh/StromgroesseKwh,
                                                        SummeKwh, Null-Lauf-Toleranz,
                                                        PV-/Solar-Komponentenkonstanten
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs KomponenteUndAnlage (Refaktor),
                                                        Rueckfall-Weiche in beiden Lesepunkten
```

Harness `..\dev\h4a\` gehört nicht zum Lieferumfang.
