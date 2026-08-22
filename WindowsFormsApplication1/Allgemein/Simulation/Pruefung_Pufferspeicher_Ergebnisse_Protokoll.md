# Prüfprotokoll: Pufferspeicher-Ergebnisse (Basis B6)

**Datum:** 21.08.2026 · **Gegenstand:** fachliche Prüfung der Pufferspeicher-Ergebnisse des
Simulationskerns anhand der eingefrorenen Referenzbasis
[`Referenzlaeufe/2026-08-19_B6`](../../../Referenzlaeufe/2026-08-19_B6/lauf_protokoll.md) —
Energiebilanz, SOC-Grenzen, Kennzahlen, Verlustmodell — plus Code-Verifikation der Formeln.
Kein neuer Lauf, keine Datenbankänderung; geprüft wurden ausschließlich die vorhandenen CSV der
Basis gegen die Engine-Logik.

**Ergebnis vorab: alle Bilanz- und Kennzahlprüfungen bestanden.** Kein Befund mit
Ergebniswirkung. Vier Einordnungen (E-1 bis E-4) erklären auffällige, aber korrekte Verläufe;
zwei bekannte Schwächen (S-1, S-2) bleiben als offene Punkte dokumentiert.

## 1. Prüfmenge

Fünf Speicher mit Ganglinien (je 3 × 8760 Stundenwerte: `*_ladung`, `*_entladung`, `*_soc`)
plus ein Speicher, der nur über Aggregatzeilen abgesichert ist:

| Projekt | Speicher | Verwendung | Q_max [kWh] | Ganglinien | Charakteristik |
|---|---|---|---|---|---|
| 1008 | Vitocell 140-E 600 l | Heizung | 6,96 | ja | dynamisch (SOC 0…6,87) |
| 1018 | Stora B 1000-6 ER 1 B | Heizung | 22,388 | ja | quasi-stationär bei 21,0 (8741 von 8760 h) |
| 1021 | allSTOR VPS 800/3-7 | Quelle | 4,514 | ja (`quellspeicher_10361_*`) | startet voll, nach 6 h leer |
| 1023 | Vitocell 140-E 600 l | Heizung | 13,92 | ja | dynamisch (SOC 0…13,83) |
| 1024 | Vitocell 140-E 600 l (2) | Brauchwasser | 10,44 | **nein** (S-1) | nur Aggregat |
| 1030 | Puffer 20 m³ BHKW-Kaskade | Heizung | 580 | ja | End-SOC konstant 550,86 (E-1) |

1007 und 1011 führen laut Ausstattung einen Puffer, der aber nicht am Rechenpfad teilnimmt
(`Sim.Speicher_Anzahl;0`) — bekannter Stand seit D4 (Projekt 1011: „der Projekt-Puffer nimmt an
nichts teil"); 1017 hat keinen Puffer.

## 2. Prüfungen und Ergebnisse

Verluste je Stunde stehen in keinem Vektor; sie wurden aus der Stundenbilanz rückgerechnet:
`V[t] = L[t] − E[t] − (SOC[t] − SOC[t−1])`, Anfangs-SOC 0 (Senke) bzw. Q_max (Quelle).

| # | Prüfung | Ergebnis |
|---|---|---|
| P1 | Vektorlängen = 8760, keine NaN | **PASS** (alle 15 Vektoren) |
| P2 | `L[t] ≥ 0`, `E[t] ≥ 0`, `SOC[t] ≥ 0` | **PASS** (0 Verstöße in 131 400 Werten) |
| P3 | `SOC[t] ≤ Q_max` | **PASS** (0 Verstöße; Maxima 6,87 / 21,00 / 3,72 / 13,83 / 550,86) |
| P4 | Stundenbilanz: rückgerechneter Verlust nie < −1e-4 | **PASS** (0 von 43 800 Stunden) |
| P5 | Σ Verlustrückrechnung = `Verluste_gesamt` | **PASS** (z. B. 1018: 2315,683 ↔ 2315,683; 1021: 0,250 ↔ 0,25) |
| P6 | Jahresbilanz `L − E − V = SOC_Ende − SOC_Anfang` | **PASS** (1008: 6,759 ↔ 6,76; 1030: 550,90 ↔ 550,86, Δ = float-Summation, s. u.) |
| P7 | `SOC_Mittel = Σ SOC / 8760` | **PASS** (exakt, z. B. 1008: 3,72670 ↔ 3,72670322) |
| P8 | `SOC_Max = max(SOC)` | **PASS** (exakt) |
| P9 | `Vollzyklen`: Senke `L/Q_max`, Quelle `E/Q_max` | **PASS** (1030: 5081,657 ↔ 5081,65682; 1021: 0,945 ↔ 0,94) |
| P10 | `Vektor.*.Summe` ↔ `Puffer.*_gesamt` | **PASS** innerhalb float-Toleranz (max. Δ 1,4e-8 relativ, 1030-Entladung 2 945 596,43 ↔ 2 945 596,47) |
| P11 | `Pufferspeicher[i].*` = `Puffer.*` auf 2 Stellen gerundet | **PASS** (AwayFromZero, z. B. 1213,625 → 1213,63) |
| P12 | Verlustmodell datenseitig: `V/SOC[t]` konstant je Speicher | **PASS** (Streuung ≤ 7e-6; Konstanten s. Abschnitt 3) |

Zu P6/P10: Die Ganglinien sind `float[8760]`, die Aggregate double-Akkumulatoren — Differenzen
in der Größenordnung 1e-8 relativ sind die erwartete float-Summationsungenauigkeit, kein Befund
(Fundstellen: `SimulationPufferspeicher.cs` Vektoren `float[]`, `Ergebnisexport.cs`
double-Summe über float-Werte).

## 3. Verifizierte Formeln (Code ↔ Daten)

| Größe | Code (`SimulationPufferspeicher.cs`) | Datenbestätigung |
|---|---|---|
| Kapazität | `Q_max = Volumen · 1,16 · (Vorlauf − Rücklauf) / 1000`; ΔT-Rückfall 10 K, BHKW-Pendelspeicher 20 K; Quellspeicher: ΔT = `WQ_Spreizung`, Rücklauf 0 | 1008/1023 gleicher physischer Speicher, Q_max 6,96 / 13,92 (Temperaturpaar-Unterschied) |
| Verlust je Stunde | `verlust = VerlustProStunde · min(1, SOC/Q_max)` mit `VerlustProStunde = Bereitschaftsverluste[kWh/24h] / 24`; wirkt am Stundenende (Phase G), nach Laden **und** Entladen | `V/SOC` je Speicher konstant: 1008 1,273e-2 · 1018 1,259e-2 · 1023 6,326e-3 · 1030 2,515e-4 /h. Querprobe: `k·Q_max` (Verlustleistung bei voll) für 1008 und 1023 nahezu gleich (0,0886 ↔ 0,0881 kWh/h — gleicher Speicher, halbiertes k bei doppeltem Q_max) |
| SOC-Ganglinie | `SOC_stuendlich[h] = (float)SOC` **nach** Phase G — Momentaufnahme am Stundenende | erklärt E-1 |
| Anfangs-SOC | Senke 0 (`Reset()`); **Quellspeicher startet voll** (`WaermequelleClass.cs`: `sp.SOC = sp.Q_max`) | 1021 rückgerechneter Startwert 4,5137 = ungerundetes Q_max |
| Vollzyklen | Senke `Ladung_gesamt/Q_max`, Quelle `Entladung_gesamt/Q_max` | P9; 1021 mit `Ladung_gesamt = 0` belegt den Quell-Zweig |
| Hysterese/Reserve | Vorbelegung `SchwelleEin` 10 %, `SchwelleAus` 95 %; BHKW-Notreserve `SchwelleReserve` (Vorgabe 10 %, in 1030 wirksam 5 %) | erklärt die Pendelgrenzen in E-1 |

## 4. Einordnungen (korrektes Verhalten, das auffällig aussieht)

**E-1 — 1030/1018: konstanter End-SOC ist eine Momentaufnahme, kein Stillstand.**
`puffer_soc.csv` von 1030 enthält 8760-mal exakt 550,86145 — der Speicher zykelt aber real
**522 kWh je Stunde**: Phase A entlädt bis zur BHKW-Notreserve (580 · 5 % = 29 kWh,
Entnahmeklemme `EntnahmeObergrenze()`), Phasen C/D laden bis zur Abschaltschwelle
(580 · 95 % = 551 kWh; BHKW 371 + Kessel 151), Phase G zieht den Verlust 0,13855 ab →
Stundenende immer 550,86145. Die SOC-Ganglinie bildet innerstündliche Auslenkungen prinzipbedingt
nicht ab. Energiebilanz stundenscharf korrekt (P4). 1018 zeigt dasselbe eingeschwungene Muster
(21,004 von 22,388). Stunde 0 lädt in beiden Projekten exakt Q_max — Anfangsbefüllung aus dem
leeren Start.

**E-2 — „Vollzyklen" ist bei Durchreichbetrieb ein Durchsatzverhältnis.** 1030: 5081,66
„Vollzyklen" bei unverändertem End-SOC. Bereits im
[B5-Laufprotokoll](../../../Referenzlaeufe/2026-08-19_B5/lauf_protokoll.md) vermerkt
(„faktisch ein Durchsatzverhältnis, keine Zyklenzahl") — bleibt vermerkt, nicht korrigiert.
Der N6-Ausschluss betrifft nur Durchfluss oberhalb Q_max; das Pendel 29 → 551 → 29 liegt
darunter und zählt voll.

**E-3 — 1021: Quellspeicher ohne Lader ist nach 6 Stunden endgültig leer.** Startet voll
(4,514 kWh), WP entnimmt 5 × 0,7115 + 0,7061 kWh, ab Stunde 6 dauerhaft SOC 0; Verluste fallen
danach keine mehr an (leerer Speicher verliert nichts). Der bekannte „freistehende Quellpuffer"
aus D4/T4 — Projektdatenlage, kein Engine-Fehler.

**E-4 — Aggregat führt dieselbe Summe doppelt mit float/double-Differenz.**
`Puffer.Entladung_gesamt` (double-Akkumulator der Engine) und `Vektor.puffer_entladung.Summe`
(Summe der float-Ganglinie) dürfen in der 2. Nachkommastelle abweichen (1030: 0,04 auf 2,9 GWh).
Für Vergleichswerkzeuge ist das innerhalb der Toleranz.

## 5. Offene Schwächen (ohne Ergebniswirkung, dokumentiert)

**S-1 — Ganglinien-Export deckt nur den ersten Heizungspuffer und die Quellspeicher ab.**
`Ergebnisexport.cs` schreibt `puffer_*.csv` ausschließlich für `sim.puffer_wp`
(= erster Puffer der Registry-Aufnahmereihenfolge mit Verwendung „Heizung" im Rechenpfad).
Der Brauchwasser-Puffer von 1024 hat deshalb **keine Ganglinien und keinen `Puffer.*`-Block**
in der Basis; abgesichert sind nur seine `Pufferspeicher[0].*`-Kennzahlen (2 Nachkommastellen).
Eine Regression im Stundenverlauf dieses Speichers wäre in B6 unsichtbar. Gleiches gälte für
einen zweiten Heizungspuffer. Zusätzlich wählt `ErsterHeizpuffer()` nach Aufnahmereihenfolge,
nicht nach Entladepriorität (im Code selbst vermerkt, `SimulationControl.cs`) — bei mehreren
Heizpuffern zeigen `puffer_*.csv`, Bericht-Zeitreihen und `Kapazitaet_Pufferspeicher` auf den
zuerst aufgenommenen. Der `Puffer.*`-Block enthält außerdem kein `SOC_Ende` (nur
`Pufferspeicher[i].SOC_Ende`).

**S-2 — Der Pfad „zwei Puffer je Kanal" bleibt unbelegt.** Entlade-/Ladereihenfolge über
`Ladeordnung.Entladereihenfolge` (Prio, dann Puffer-ID) ist implementiert und mit Sicherungen
für den Mehrfachfall versehen (Durchsatzbudget nur einmal je Stunde, `DurchsatzEntladen` vor der
regulären Ordnung); in allen neun Referenzprojekten steht aber `Sim.Speicher_Anzahl;1` — kein
Projekt der Basis belegt den Fall. Deckt sich mit dem offenen Punkt 4b-4 aus
[`Paket4_EngineKern_Protokoll.md`](Paket4_EngineKern_Protokoll.md) und gehört zur Abnahme
(Konzept Kapitel 9, Paket 10). Ein Nachweis bräuchte eine präparierte Wegwerfkopie mit zweitem
Heizungspuffer — nicht Teil dieser Prüfung, da Datenbankänderung.

## 6. Prüfrezept (Reproduktion)

PowerShell, ohne Access, nur lesend auf den Basis-CSVs: je Speicher die drei Vektoren laden
(`Index;Wert`, Semikolon, Punkt-Dezimal, BOM), `aggregate.csv` als Name/Wert-Paare;
dann P1–P12 wie oben. Kernstück ist die Verlustrückrechnung
`V[t] = L[t] − E[t] − (SOC[t] − SOC[t−1])` mit Anfangs-SOC 0 (Senke) bzw. Q_max (Quelle):
Sie muss in jeder Stunde ≥ 0 sein und in Summe `Verluste_gesamt` treffen — das prüft
Stundenbilanz und Verlustmodell zugleich. Q_max für Quotienten aus `Puffer.Q_max`
(ungerundet), nicht aus `Pufferspeicher[i].Q_max` (2 Nachkommastellen).

**Konstanten zum Wiederverwenden** (Verlustsatz `k = V/SOC` je Stunde, aus B6):
1008 1,2732e-2 · 1018 1,2589e-2 · 1021 2,2733e-2 · 1023 6,3257e-3 · 1030 2,5151e-4 —
entspricht `(Bereitschaftsverluste/24) / Q_max` nach Abzugslogik Phase G; ändert sich einer
dieser Werte ohne Katalogänderung, hat sich das Verlustmodell verschoben.
