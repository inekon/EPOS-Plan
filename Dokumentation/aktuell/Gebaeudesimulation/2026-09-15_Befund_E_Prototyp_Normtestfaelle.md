# Befund E — Prototyp-Löser 7R2C gegen die zwölf VDI-6007-Testfälle (15.09.2026)

**Protokoll.** Befund eines Prüf-Agenten (Modell Opus, Workflow ‚gebaeudemodell-pruefen‘, Stufe Prototyp 1; Referenzdaten aus AixLib) im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `C:\Users\…pos-spike\` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

# Befund „Prototyp 1 — Löser validieren"

**Ergebnis: alle 12 VDI-6007-1-Testfälle bestanden** (Testfall 11 mit nachgebildeter AixLib-Messkette, siehe 5.3). Im Repository `C:\Waermeplan\EPOS-Plan` wurde nichts angelegt oder geändert (`git status --porcelain` liefert unverändert dieselben 24 Einträge wie zu Sitzungsbeginn).

---

## 1. Aufbau des Lösers

Projekt: `C:\Users\Dirk\AppData\Local\Temp\epos-spike\Prototyp\Prototyp.csproj` — `net10.0`, Konsolen-Exe `Vdi6007`, **kein NuGet-Paket** (nur BCL inkl. `System.Numerics.Complex` und `System.Text.Json`). 984 Zeilen C#, Build in 0,8 s.

| Datei | Zeilen | Inhalt |
|---|---|---|
| `Mat2.cs` | 128 | `readonly struct Mat2`, `readonly struct Vec2`, `sealed class Discretisation` |
| `Zone.cs` | 93 | `sealed record ZoneParams`, `sealed class ZoneNetwork`, `readonly record struct StepLoads` |
| `Solver.cs` | 116 | `sealed class ModeSystem`, `static class Eliminator` (Knoteneliminierung) |
| `Simulation.cs` | 162 | `sealed class Simulation` (Zeitschritt + Ereignisbehandlung) |
| `TimeTable.cs` | 51 | `sealed class TimeTable` (CombiTimeTable, LinearSegments + Periodic) |
| `Cases.cs` | 227 | `sealed class TestCase`, `readonly record struct ControlSpec`, `static class CaseLoader` |
| `Program.cs` | 207 | CLI, CSV-Ausgabe, Vergleich, Benchmark |

Zentrale Signaturen:

```csharp
public static ModeSystem Eliminator.Free(ZoneNetwork z, StepLoads l);                        // Solver.cs:30
public static ModeSystem Eliminator.ControlAir(ZoneNetwork z, StepLoads l, double tSet);     // Solver.cs:62
public static ModeSystem Eliminator.ControlIntSurface(ZoneNetwork z, StepLoads l, double tSet); // Solver.cs:89
public StepResult Simulation.Advance(StepLoads l, ControlSpec c, double h);                  // Simulation.cs:101
public Discretisation(Mat2 a, double h);            // Phi, Gamma, Psi                       // Mat2.cs:83
public Vec2 Discretisation.Step(Vec2 x0, Vec2 b);   // x(h)                                  // Mat2.cs:117
public Vec2 Discretisation.Mean(Vec2 x0, Vec2 b);   // (1/h)*Integral x dt                   // Mat2.cs:120
public static TestCase CaseLoader.Load(string dir, int n, bool solarByOrientation = true);   // Cases.cs:35
```

**Datenstruktur wie gefordert:** Parameter als `record ZoneParams` (unveränderlich, `with`-fähig), Eingänge als Zeitreihen-Arrays `StepLoads[]` und `ControlSpec[]` mit je 1440 Stundenwerten. Für den EPOS-Lauf im zweiten Schritt wird nur `CaseLoader` durch einen EPOS-Adapter ersetzt; `ZoneNetwork`, `Eliminator`, `Discretisation` und `Simulation` bleiben unverändert.

**Kommandozeile**

```
dotnet run -c Release -- [<nr>...|--all] [--ref <dir>] [--out <dir>]
                         [--tc11-window] [--matrix] [--diag] [--tc6-exact]
                         [--solar-flat] [--bench]
```

Ausgaben nach `…\Prototyp\out\`: je Testfall `testfall_<n>.csv` (1440 Zeilen: Stundenmittel und Endwerte von θ_air, θ_m,AW, θ_m,IW, θ_s,AW, θ_s,IW, Q_HLK sowie die verwendeten Eingänge), `vergleich_<n>.csv` (72 Prüfpunkte), gesamt `validierung.csv` und `lauf.log`.

---

## 2. Herleitung der eliminierten Matrix

Zustände x = (θ_m,AW, θ_m,IW)ᵀ, algebraisch: θ_s,AW, θ_s,IW, θ_air. Leitwerte G1 = 1/R1,AW, G2 = 1/R1,IW, G_Rest = 1/RRest,AW, G_cAW = 1/Rconv,AW, G_cIW = 1/Rconv,IW, G_rad = 1/Rrad, G_ve = 1/Rve.

**Knotenbilanzen**

```
θ_s,AW:  G1(x1−θ_sAW) + G_rad(θ_sIW−θ_sAW) + G_cAW(θ_air−θ_sAW) + Q_sAW = 0
θ_s,IW:  G2(x2−θ_sIW) + G_rad(θ_sAW−θ_sIW) + G_cIW(θ_air−θ_sIW) + Q_sIW = 0
θ_air :  G_cAW(θ_sAW−θ_air) + G_cIW(θ_sIW−θ_air) + G_ve(T_ve−θ_air) + Q_air = 0
```

**Luftknoten auflösen** (VAir = 0, also rein algebraisch), mit S = G_cAW + G_cIW + G_ve:

```
θ_air = ( G_cAW θ_sAW + G_cIW θ_sIW + G_ve T_ve + Q_air ) / S
```

Einsetzen liefert das 2×2-System **M θ_s = K x + v** (Solver.cs:32–40):

```
M11 = G1 + G_rad + G_cAW − G_cAW²/S
M12 = M21 = −( G_rad + G_cAW·G_cIW/S )
M22 = G2 + G_rad + G_cIW − G_cIW²/S
K   = diag(G1, G2)
v   = [ Q_sAW + G_cAW(G_ve T_ve + Q_air)/S ,  Q_sIW + G_cIW(G_ve T_ve + Q_air)/S ]ᵀ
```

**Zustandsgleichungen** C1 ẋ1 = G_Rest(T_out − x1) + G1(θ_sAW − x1), C2 ẋ2 = G2(θ_sIW − x2), mit θ_s = M⁻¹(Kx + v):

```
A = C⁻¹ ( K M⁻¹ K − diag(G1 + G_Rest, G2) )                 C⁻¹ = diag(1/C1, 1/C2)
b = C⁻¹ ( K M⁻¹ v + [G_Rest·T_out, 0]ᵀ )
```

Der Off-Diagonalterm ist A12 = G1·G2·(M⁻¹)₁₂/C1 bzw. A21 = G1·G2·(M⁻¹)₁₂/C2 und verschwindet nur, wenn M12 = 0, also wenn es **weder** langwelligen Austausch (G_rad) **noch** einen gemeinsamen Luftknoten (G_cAW·G_cIW/S) gibt. Genau das hat die Vorlage weggeworfen.

**Zahlenbeleg Testfall 1** (`--matrix`):

```
A            = [ −4,9332988e−05   +3,4727253e−05 ;  +3,7470853e−06   −3,7470853e−06 ]  1/s
Eigenwerte   = −1,05191e−06 1/s (τ = 264,07 h)  und  −5,20282e−05 1/s (τ = 5,34 h)
exp(A·3600)  = [ 0,83802468  0,11378575 ; 0,012277531  0,9873894 ]
θ_air        = 0,1370666·x1 + 0,8629334·x2   (+ Lastanteil)
```

A12 ist **70 % von |A11|** — die von der Vorlage unterschlagene Kopplung ist der dominierende Term, nicht eine Korrektur.

**Ideale Regelung.** Bei θ_air = T_soll entfällt die Luftbilanz als Bestimmungsgleichung und wird zur Leistungsdefinition; es bleibt N θ_s = K x + w mit

```
N = [ G1+G_rad+G_cAW , −G_rad ; −G_rad , G2+G_rad+G_cIW ],  w = [Q_sAW+G_cAW·T_soll , Q_sIW+G_cIW·T_soll]ᵀ
Q_HLK(x) = G_cAW(T_soll−θ_sAW) + G_cIW(T_soll−θ_sIW) + G_ve(T_soll−T_ve) − Q_air
```

Für die **Kühldecke in TC11** (Senke am Knoten θ_s,IW statt an der Luft) wird stattdessen die Luftbilanz als Zwangsbedingung geführt (Solver.cs:89–115):

```
P = [ G1+G_rad+G_cAW , −G_rad ; G_cAW , G_cIW ]
w' = [ G_cAW·T_soll + Q_sAW , T_soll(G_cAW+G_cIW+G_ve) − G_ve·T_ve − Q_air ]ᵀ
Q_HLK = (G2+G_rad+G_cIW)·θ_sIW − G_rad·θ_sAW − G2·x2 − G_cIW·T_soll − Q_sIW
```

**Exakte Diskretisierung** (Mat2.cs:83–115). Für jede 2×2-Matrix A wird f(A) über die Sylvester-Formel mit den (ggf. komplexen) Eigenwerten λ₁,₂ = μ ± √(μ²−det A), μ = tr(A)/2, gebildet:

```
f(A) = Δ·(A − μI) + ½(f(λ1)+f(λ2))·I ,   Δ = (f(λ1)−f(λ2))/(λ1−λ2)
konfluent (|λ1−λ2| < 1e−9·max(1,|μ|)):  f(A) = f(μ)I + f′(μ)(A−μI)
```

mit drei Skalarfunktionen (Reihenentwicklung bei |λh| < 1e−6/1e−8 gegen Auslöschung):

```
Phi   = exp(A h)                        f0(λ) = e^{λh}
Gamma = ∫₀ʰ e^{Aτ}dτ                    f1(λ) = (e^{λh}−1)/λ
Psi   = ∫₀ʰ∫₀^τ e^{As}ds dτ             f2(λ) = (e^{λh}−1−λh)/λ²
```

Damit ist **sowohl der Endwert x(h) = Φx₀ + Γb als auch das Stundenmittel (1/h)(Γx₀ + Ψb) exakt** — letzteres ist zwingend, weil die AixLib-Referenz gleitende Stundenmittel (`Modelica.Blocks.Math.Mean(f=1/3600)`) vergleicht und keine Momentanwerte.

---

## 3. Abbildung AixLib → Modell (bestätigte Details)

| Punkt | Umsetzung | Beleg |
|---|---|---|
| RRest,AW | `RExtRem + 1/hConWall.k` (äußerer Übergang aufaddiert) | Zone.cs:52; TC1: 0,03895920 + 0,00380952 = 0,04276872 K/W |
| Strahlungssplit interne Gewinne | proportional {ΣAExt, ΣAWin, AInt}/ATot | Zone.cs:60; TC1: AW 0,122093 / IW 0,877907 |
| Strahlungssplit Fenstersolar | `splitFacVal`: AW_j = (ΣAExt−AExt[j])/(ATot−AExt[j]−AWin[j]) | Zone.cs:70 |
| Folge daraus | **TC5/10/12 (1 Orientierung): 100 % des Fenstersolars auf θ_s,IW, 0 % auf θ_s,AW** | Funktionsquelltext `splitFacVal.mo` abgerufen und verifiziert |
| TC8/9 (2 Orientierungen) | AW = [0,198675; 0,147887], IW = [0,801325; 0,852113] | Konsolenausgabe |
| ratioWinConRad | Anteil 0,09 konvektiv an die Luft, 0,91 radiativ auf die Flächen | Cases.cs:150–157 |
| äquiv. Außentemperatur | ΔT_LW = (T_Himmel−T_Luft)·h_rad/(h_rad+h_conWallOut); ΔT_SW = H_Sol·a_Ext/(h_rad+h_conWallOut); T_EqWall = T_Luft+ΔT_LW+ΔT_SW; T_EqWin = T_Luft+ΔT_LW·(1−sunblind); T_EqAir = Σ T_EqWall·wfWall + Σ T_EqWin·wfWin + T_Gro·wfGro | Cases.cs:104–126; Formeln aus `PartialVDI6007.mo` / `VDI6007.mo` verbatim abgerufen |
| Sonnenschutz | `sunblind = 1 − g`, g = 0,15 bei E > 100 W/m², sonst 1 (Add-Block k1=−1) | Cases.cs:102 |
| Lüftung TC12 | G_ve = ventRate · 0,000330375898 kg/s · 1005,45 J/(kg K); T_ve = Außenluft | Cases.cs:163 |
| VAir = 0,1 m³ (nur TC12) | **ignoriert** (C_Luft ≈ 86 J/K, τ ≈ 0,4 s bei 3600 s Schritt) | s. offene Punkte |

**Vorzeichen der Leistungsreferenz — hier war der bisherige Befund zu korrigieren:**

| Fall | Messkette | Referenz |
|---|---|---|
| TC6 | `heatFlowSensor.Q_flow` **direkt** auf `mean` (TestCase6.mo, connect `heatFlowSensor.Q_flow, mean.u`) | **negativ = Heizen** |
| TC7 | `gainMea(k=−1)` zwischen Sensor und `mean` (TestCase7.mo:123) | positiv = Heizen |
| TC11 | `add(k1=1, k2=−1)` aus Kühl- und Heizsensor (TestCase11.mo:148) | positiv = Heizen |

Im Löser über `TestCase.PowerSign` (= −1 nur bei TC6) abgebildet (Cases.cs:200).

**Referenzspalte TC11:** `columns={2,3,4}` → validiert wird `reference.y[2]` = **Tabellenspalte 3**, das ist Zeilenindex 2 des Rohdatensatzes (Cases.cs:180). Der erste Anlauf griff auf Spalte 4 zu und ergab 715 W Abweichung; nach der Korrektur 4,9 W.

---

## 4. Validierungstabelle

Jeweils 72 Prüfpunkte (Tag 1 Stunde 1–24, Tag 10, Tag 60; Startzeit 3600 s gemäß `VerifyDifferenceThreePeriods`). Alle Werte in der Einheit der Referenzgröße, Vorzeichen = Simulation − Referenz. Quelle: `…\Prototyp\out\validierung.csv`.

| TF | Größe | Tol. | **max \|Abw\|** | Tag 1 max / min | Tag 10 max / min | Tag 60 max / min | bestanden |
|---|---|---|---|---|---|---|---|
| 1 | TAir | 0,15 K | **0,055** | +0,040 / −0,044 | +0,052 / −0,055 | +0,050 / −0,050 | **ja** |
| 2 | TAir | 0,15 K | **0,052** | +0,044 / −0,049 | +0,045 / −0,044 | +0,052 / −0,049 | **ja** |
| 3 | TAir | 0,15 K | **0,059** | +0,040 / −0,059 | +0,042 / −0,048 | +0,046 / −0,040 | **ja** |
| 4 | TAir | 0,15 K | **0,056** | +0,036 / −0,051 | +0,038 / −0,050 | +0,056 / −0,044 | **ja** |
| 5 | TAir | 0,15 K | **0,058** | +0,039 / −0,058 | +0,041 / −0,048 | +0,046 / −0,044 | **ja** |
| 6 | Q | 1,5 W | **1,499** | +1,499 / −1,484 | +1,379 / −1,079 | +1,425 / −1,342 | **ja** (knapp) |
| 7 | Q | 1,5 W | **0,639** | +0,639 / −0,352 | +0,460 / −0,567 | +0,428 / −0,631 | **ja** |
| 8 | TAir | 0,15 K | **0,050** | +0,046 / −0,041 | +0,043 / −0,050 | +0,043 / −0,046 | **ja** |
| 9 | TAir | 0,15 K | **0,136** | +0,053 / −0,043 | +0,076 / −0,028 | +0,136 / +0,035 | **ja** (knapp) |
| 10 | TAir | 0,15 K | **0,143** | +0,047 / −0,143 | +0,115 / −0,063 | +0,143 / −0,019 | **ja** (knapp) |
| 11 | Q | 1,5 W | **1,376** ¹ | +0,546 / −0,277 | +1,000 / −1,376 | +0,907 / −0,724 | **ja** ¹ |
| 12 | TAir | 0,15 K | **0,054** | +0,048 / −0,049 | +0,046 / −0,054 | +0,049 / −0,048 | **ja** |

¹ mit `--tc11-window` (Nachbildung des AixLib-120-s-Umschaltfensters, s. 5.3). Ohne diese Nachbildung: max 4,918 W, 70 von 72 Punkten innerhalb der Toleranz.

---

## 5. Ursachenanalyse

### 5.1 Testfälle 1–5, 8, 12: Streuung ±0,06 K = Referenzauflösung
Die Abweichungen oszillieren vorzeichenwechselnd um Null und überschreiten nie 0,06 K. Die VDI-Referenztabellen sind auf 0,1 K gerundet, der maximal mögliche Rundungsrest ist 0,05 K. Es ist **kein systematischer Modellfehler** erkennbar (Tag-60-Profil TC5: +0,038 … −0,044, Mittelwert ≈ −0,003 K).

### 5.2 Testfall 6: 1,499 W bei 1,5 W Toleranz
Die Abweichung wächst mit der Leistung (bei −765 W → −1,48 W = 0,19 %; bei −351 W → −0,02 W). Geprüfte Ursachen:

- **Gerundete TC6-Parameter** (offene Frage 2 des vorigen Befunds): Rechnung mit den exakten TC1-Werten (`--tc6-exact`, `CExt=1600848,94`, `RInt=0,000595693407511`, `CInt=14836354,6282`) ergibt **1,465 W statt 1,499 W**. Der Rundungsunterschied trägt also nur **0,034 W** bei und ist *nicht* die Ursache. Die Frage ist damit beantwortet: die Parametrierung ist für die Bewertung irrelevant.
- Verbleibend ~1,5 W: Referenztabelle ganzzahlig in W (Rundungsrest bis 0,5 W) plus AixLib-Integratortoleranz 1e−6 auf einem steifen DAE. Zum Vergleich: die 0,05 K Übereinstimmung in TC2 (gleiche Zone, freilaufend) entspricht rechnerisch etwa ±10 W Leistungsunsicherheit — TC6 ist also der deutlich schärfere Test und wird eingehalten.
- Geändert wurde nichts; der Löser rechnet mit den Originalparametern des jeweiligen Testfalls.

### 5.3 Testfall 11: die beiden Umschaltstunden
Ohne Sonderbehandlung liegen **70 von 72 Punkten** innerhalb 1,5 W; die beiden Ausreißer sind **Tag 10 Stunde 10** (Ref −121 W, Sim −125,918 W, Δ −4,918 W) und **Tag 60 Stunde 10** (Ref −122 W, Sim −126,266 W, Δ −4,266 W) — exakt die Stunden, in denen von Heizen (Luftknoten) auf Kühlen (Innenwandoberfläche) umgeschaltet wird.

Ursache: AixLib misst in TC11 nicht den Modellwert, sondern schaltet 120 s nach jedem Moduswechsel auf **Referenzspalte 4** um (`timer` → `thr(threshold=120)` → `switchMea`, TestCase11.mo:164–176 und 245–249). Spalte 4 ist durchweg der um eine Stunde vorgezogene Spalte-3-Wert („Mittelwert der laufenden Stunde") — mit **einer bewussten Ausnahme**: bei t = 810000 s und t = 5130000 s steht dort **+100 W** statt der −121/−122 W. Genau diese 120 s heben den AixLib-Stundenmittelwert um ≈ +3,3 W an.

Nachgebildet (`--tc11-window`, Program.cs:74–83, Simulation.cs:118–128): Korrektur = (120 s / 3600 s)·(Spalte-4-Wert − Modellmittel im 120-s-Fenster). Ergebnis −122,376 W bzw. −122,894 W, Restabweichung **−1,376 W** und **−0,724 W** → innerhalb der Toleranz. Die Nachbildung greift korrekt nur bei Wechseln im Stundeninneren; die Wechsel auf Stundengrenzen (t = 64800, 799200 s) ändern das Ergebnis um weniger als 0,9 W (802800 s: 127,034 → 126,158 W gegen Referenz 126).

**Offen bleibt** die Restabweichung von 1,4 W: sie stammt aus dem LimPID-Transienten (k = 0,1; Ti = 1,2 s; Td = 5 s; Hysterese ±1e−7), den eine ideale Regelung prinzipiell nicht nachbilden kann. In den ungeregelten Sättigungsstunden ist die Übereinstimmung exakt (Δ = 0,0000 W).

### 5.4 Testfälle 9 und 10: langsamer Drift von ≈ +0,09 K bzw. +0,06 K
Beide nutzen `EquivalentAirTemperature.VDI6007`. Die Abweichung ist an Tag 1 noch im Rundungsband (±0,05 K) und wächst über 60 Tage auf +0,136 bzw. +0,143 K — Signatur eines kleinen konstanten Versatzes im Mittelwert von T_EqAir, der sich in die schwere Wandmasse integriert. Tag-60-Mittel der Abweichung: TC9 +0,090 K, TC10 +0,060 K, TC8 (gleicher Block, aber `withLongwave=false` und `wfGro=0`) ±0,05 K ohne Drift. Der Versatz tritt also nur dort auf, wo der Langwellen- bzw. der Erdreichanteil wirkt. **Beide Fälle bleiben innerhalb der Toleranz**, aber die Reserve ist nur noch 0,01 K — für EPOS-Läufe mit anderen Klimadaten ist das der empfindlichste Punkt. Kandidaten (nicht abschließend geklärt): Auflösung von H_Sky in der ersten Tagesstunde (Stützstellen 0 / 0,36 / 3600 s ergeben dort eine Rampe statt eines Treppenwerts) und die Nichtlinearität T_Himmel = 65,99081593·H_Sky^¼ innerhalb des Stundenintervalls. Das Erdreich-/Nachbarraumdetail in TC10 ist wie in AixLib **nur** über die äquivalente Außentemperatur abgebildet, nicht als separater Pfad.

### 5.5 Gegenprobe: Strukturfehler der Vorlage
Mit `--diag` (Systemmatrix künstlich diagonal, Solver.cs:21–23) versagt das Modell in allen 12 Fällen katastrophal:

| TF | 1 | 2 | 3 | 5 | 6 | 7 | 10 | 12 |
|---|---|---|---|---|---|---|---|---|
| max Abw. | 304,4 K | 304,4 K | 292,9 K | 300,9 K | 11 491 W | 1 000 W | 265,2 K | 202,2 K |

Die Diagonalisierung ist damit quantitativ als Ursache des Vorlagen-Fehlverhaltens belegt. Vom Vorlagenmaterial wurde ausschließlich das Datenmodell (Schicht/Bauteil/Zone/RC-Parameter, Lösungsschema) gelesen; **kein Quelltext übernommen**.

---

## 6. Laufzeit

Gemessen auf 1440 Schritten und auf 8760 Schritte hochgerechnet bzw. direkt gemessen (`--bench`, 20 Wiederholungen, Release, nach JIT-Warmlauf):

| Betriebsart | 8760 Schritte |
|---|---|
| Freilauf (TC1–5, 8–10, 12), dedizierter Benchmark | **7,0 ms** |
| Freilauf, einschließlich JIT-Warmlauf im ersten Testfall | 48–74 ms |
| Ideale Regelung mit Ereignissuche (TC6, TC7, TC11) | 87–121 ms |

Das sind je Zeitschritt drei 2×2-Matrixfunktionen (Φ, Γ, Ψ) plus eine 2×2-Inversion — ca. 0,8 µs im Freilauf. Die Ereignissuche kostet bis zu 60 Bisektionsschritte mit je einer Neuberechnung von Φ, daher der Faktor ~15 in den geregelten Fällen. Für EPOS-Jahresläufe ist das in jeder Hinsicht unkritisch; bei Bedarf lässt sich Φ(τ) in der Bisektion durch eine Interpolation ersetzen.

---

## 7. Abweichungen von der Aufgabenstellung (bewusst)

**Ideale Regelung: kontinuierlich gehalten statt „Sollwert am Schrittende exakt getroffen".** Weil VAir = 0 ist, ist der Luftknoten rein algebraisch; AixLib erzwingt mit `PrescribedTemperature` (TC6) bzw. dem schnellen LimPID (TC7/11) θ_air = T_soll **zu jedem Zeitpunkt**, und verglichen wird das Stundenmittel der Leistung. Eine über die Stunde konstante Leistung, die den Sollwert nur am Schrittende trifft, ergäbe in TC6 Stunde 7 einen völlig anderen Mittelwert (die ideale Leistung fällt innerhalb dieser Stunde von ~1000 auf ~700 W). Der Löser bestimmt daher Q(t) als affine Funktion des Zustands und bildet das exakte Integral über Ψ (Solver.cs:62–87, Mat2.cs:120). Beide Formulierungen sind dieselbe 2×2-Algebra; für EPOS mit kapazitivem Luftknoten ist die Schrittend-Variante mit einem zusätzlichen Skalar-Solve nachrüstbar.

---

## 8. Offene Punkte

1. **TC9/TC10 Drift +0,09 / +0,06 K** (Abschnitt 5.4) — bestanden, aber nur 0,01 K Reserve. Vor dem EPOS-Einsatz mit realen Wetterdaten zu klären.
2. **TC11 Restabweichung 1,4 W** in den Umschaltstunden — PID-Transiente, mit einer idealen Regelung nicht reproduzierbar.
3. **TC6 bei 1,499 W von 1,5 W** — Rundung der Referenz plus AixLib-Integratortoleranz; die Parameterrundung ist als Ursache ausgeschlossen (0,034 W).
4. **VAir = 0,1 m³ in TC12 vernachlässigt** (C ≈ 86 J/K, τ ≈ 0,4 s). Für EPOS mit echtem Luftvolumen wird der Luftknoten zum dritten Zustand; das Eliminationsschema wird dann zu einem 3×3-System — die `Mat2`-Funktionsmaschinerie müsste auf `Mat3` erweitert oder durch Skalierung + Quadrierung ersetzt werden.
5. **Bauweise L (TC3/4) ohne Doku-Gegenprobe** — die AixLib-RC-Werte wurden ungeprüft übernommen; die Validierung mit 0,059 K stützt sie aber indirekt.
6. **`nExt = nInt = 1` fest verdrahtet.** Längere RC-Ketten (nExt > 1) erfordern eine Erweiterung der Zustandsdimension; für EPOS-Bauteile mit mehreren Schichten ist zu prüfen, ob die VDI-6007-Reduktion auf eine Kapazität je Gruppe ausreicht.
7. **Lizenzauflage:** Referenzzahlen stammen aus AixLib (BSD 3-Clause, © 2010–2018 RWTH Aachen University, E.ON ERC, EBC). Bei Weitergabe von Prototyp oder Ergebnissen sind Copyright-Vermerk und Haftungsausschluss mitzuführen. Im Prototyp-Quelltext selbst ist kein AixLib-Code enthalten.