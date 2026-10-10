# Konzept — Teillast und Takten der Kältemaschine (KM3)

**Stufe 2 der Kälteanlagen-Empfehlung** · **Stand 09.10.2026 — Fassung 1, Fachkonzept zur Abnahme durch den
Anwender; Fragen KM3‑Q1 bis KM3‑Q11 offen (Abschnitt 9)** · Codestand `adb2c94db` (Zweig `ios_migration_september`,
Schemastand 205, Referenzbasis `2026-10-09_R46_Geraetegrenzen`) · Vorarbeit:
Recherche Kälteanlagen vom 08.10.2026 (dieser Ordner, Zeile im [Index](../LIESMICH.md)) (Abschnitte „Die
Teillast ist die eigentliche Lücke“, „Typische Werte für die Plausibilisierung“, „Stufe 2“) und
[Umsetzung KM1](../aktuell/Kälteanlagen/2026-10-09_Umsetzung_KM1_Typkennfelder.md) („Was Stufe 2 noch braucht“).
**Umsetzung:** [`Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md).

Ziel: Die Kältemaschine rechnet ihre Leistungsaufnahme heute mit dem EER des Kennfelds, gleich wie weit sie unter
Volllast läuft, und taktet unter der Mindestteillast ohne Verlust. Dieses Papier legt fest, wie eine **Lastachse**
(Teillastkurve EIRFPLR), der **Taktverlust** nach dem Hausmuster der Wärmepumpe, ein **Extrapolationsweg mit
konstantem Gütegrad** an den Kennfeldrändern und eine **Skalierung auf den Nennpunkt eines Datenblatts**
hinzukommen — als Opt-in, sodass jedes Bestandsprojekt und die Referenzbasis unverändert rechnen.

Eigene Ableitungen sind mit „(Abl.)“ gekennzeichnet. Vorgabewerte sind Hauswerte („Vorgabe“); Normen werden nur
genannt, ihre Tabellen und Texte stehen nicht im Repositorium.

---

## 1 Ziel und Abgrenzung

**Gegenstand** ist allein die Kältemaschine (Kaltwassersatz, Anlagenart 13, `Tab_Kaeltemaschine(_STAMM)`) mit ihrem
zweidimensionalen Kennfeld über Rückkühl- und Kaltwassertemperatur.

**Nicht Gegenstand:**

| Bereich | Bleibt bei | Stufe der Empfehlung |
|---|---|---|
| Wärmepumpe im Kühlbetrieb (Kühlkennlinie, Taktverlust über `Mindestleistung_kW` und `Taktverlustfaktor_Cd`) | vorhandenem Weg in `Kaeltekaskade` | Stufe 6 |
| Rückkühler als Katalog, Ventilatorkennlinie, Teil-Freikühlung | Festwerten in `KaelteFestwerte` | Stufe 3 |
| Kältemittel als Stammdatum mit Zulässigkeit | Textfeld `Kaeltemittel` ohne Rechenwirkung | Stufe 4 |
| Split, Multisplit, VRF | — | Stufe 5 |

**Warum jetzt.** Die Recherche zeigt an den offenen Kurvensätzen, dass der EER bei Teillast je Regelung deutlich
von Volllast abweicht — EER25/EER100 0,73 bei Turbo mit fester, 1,31 mit variabler Drehzahl; EPOS-Plan
setzt diesen Faktor für jede Maschine auf 1 und rechnet drehzahlgeregelte Maschinen in Teillast zu schlecht,
taktende Kleinmaschinen zu gut (Recherche, „Die Teillast ist die eigentliche Lücke“). Die Wärmepumpe im Kühlbetrieb
rechnet seit Welle M4 einen Taktverlust, die Kältemaschine nicht — beide Kälteerzeuger rechnen ungleich.

## 2 Ausgangslage im Code

**2.1 Kennfeld.** `KaeltemaschinenKennlinie` (`EPOS.Kern/Allgemein/Simulation/Kaelte/Kaeltemaschine.cs`) hält
Kälteleistung und EER je Kaltwasserstützstelle über der Rückkühltemperatur und wertet bilinear aus (`Auswerten`).
**Außerhalb der Stützstellen gilt der Randwert**; der Punkt trägt `Randwert = true`, die Kaskade zählt
`StundenRandwert` und der Lauf meldet die Stunden im Protokoll (`SimulationControl.Kaelte.cs`).

**2.2 Rückkühltemperatur.** `Kaeltemaschine.Rueckkuehltemperatur` je Stunde nach Rückkühlart aus
`KaelteFestwerte`: LUFT Außenluft + `GRAEDIGKEIT_LUFT_K` (5 K), TROCKENKUEHLER + 10 K, NASSKUEHLER Feuchtkugel + 5 K,
WASSER fest 25 °C. Freie Kühlung (Trocken- und Nasskühler) ganz oder gar nicht ab 3 K unter dem Kaltwasser, mit
EER-Ersatz 15.

**2.3 Stunde einer Maschine** (`Kaeltemaschine.StundeEinzeln`):

1. Rückkühltemperatur der Stunde; freie Kühlung prüfen (dann `q = min(Last, Q_nenn)`, Strom `q / 15`).
2. Kennfeldpunkt `p` bei (T_rk, T_kw): Kapazität `Q_av = p.LeistungKw`, `EER_KF = p.Eer`.
3. Deckung `Q = min(Last, Q_av)`; **Strom = Q / EER_KF** — der EER hängt nicht vom Lastgrad ab.
4. `P_min = min(Mindestteillast · Q_nenn, Q_av)`; `Takt = P_min > 0 und Q < P_min` — **nur gezählt**, kein
   Mehrstrom, keine Starts.
5. Hilfsstrom der Rückkühlung `= Hilfsstrom_Rueckkuehlung_kW · Q / Q_av` (Laufanteil).

**2.4 Mehrere gleiche Maschinen** (`Kaeltemaschine.Stunde`, `Anzahl` aus `Kaeltemaschine_Anzahl` der Anlagenzeile):
Die Last wird **gleichmäßig auf alle `Anzahl` Maschinen** geteilt und das Ergebnis vervielfacht — es gibt keine
Folgeschaltung. Für den heutigen linearen Weg ist das gleichgültig; mit einer Lastachse und einem Taktverlust nicht
mehr (Abschnitt 3.6).

**2.5 Kaskade und Kältespeicher** (`Kaeltekaskade.StundeRechnen`, `MaschineRechnen`): Ladewunsch der Kältespeicher,
freie Kühlung, Entladung, Erzeuger in Listenfolge (`Tool_1` …), Bereitschaftsverlust. Die Maschine sieht Raumlast
und Ladewunsch **als eine Last** (`last = rest + lade`); was über den Raum hinaus erzeugt wird, lädt. Der Strom der
Maschine ist `Verdichter · (1 + Kuehl_Hilfsstromanteil) + Hilfsstrom`. Zähler (`Kuehl_ID_Carrier`,
`Kuehl_EigenerZaehler`), Netzbezugsanteil, Kosten und Emissionen hängen am Stundenstrom.

**2.6 Ergebnisse.** `Tab_ErgebnisKaeltemaschine` (Schritt 183/184) führt `Kaelteproduktion_MWh`,
`Stromverbrauch_MWh`, `Hilfsstrom_MWh`, `FreieKuehlung_MWh`, `FreieKuehlung_Stunden`, **`Taktstunden`**,
`Unterdeckung_MWh`, `Stunden_Leistungsgrenze` und die Abrechnungsspalten; Kennzahlen `kaelte.km.*` im
`KennzahlenKatalog` (darunter `kaelte.km.jaz` und `kaelte.km.takt`); Bild `KaelteProduktionBild`.

**2.7 Das Hausmuster „Teillast und Takten“** (Welle M4, Schritt `ErzeugerTeillastSchema`;
[Entscheidungsvorlage Modellgrenzen](../aktuell/Entscheidungsvorlage_Modellgrenzen_Rechenwege.md), Punkt WP1):
`Waermepumpentakt` rechnet zustandslos — Lastverhältnis `CR = Q / P_min`, Teillastfaktor
`f = CR / (C_d · CR + (1 − C_d))` für 0 < CR < 1, Mehrstrom `P · (1/f − 1)`, Starts über
`Kesselkennlinie.StartsImTakt` mit `MINDESTLAUFZEIT_MIN` (10 min), Vorgabe `VORGABE_CD` = 0,9, wirksamer Wert
`CdWirksam` (gepflegt 0 … 1, sonst Vorgabe). Die Wärmepumpe im Kühlbetrieb nimmt diese Funktionen in
`Kaeltekaskade.Taktverlust`; der Mehrstrom geht **vor** dem Hilfsstromzuschlag in den Verdichterstrom und steht je
Stunde in `Taktstrom_stuendlich`. Kessel und BHKW tragen dieselbe Gruppe „Teillast und Takten“ im Katalogdialog
(Ressource vorhanden).

**2.8 Daten, die schon da sind.** Die 34 eingebauten Typkennfelder (`KaeltemaschinenTypkennfelder.json`) tragen die
unveränderten Copper-Sätze samt `eir-f-plr`, `min_plr`, `min_unloading`, `compressor_speed` — bisher ungenutzt.
Auswertung (Abl.): alle 34 Teillastkurven sind quadratisch in PLR (`type = quad`, `coeff1` bis `coeff3`);
`EIRFPLR(1)` liegt zwischen 0,988 und 1,005; drei Sätze (Luft Scroll 200 und 500 kW, Luft Schraube 2 000 kW) tragen
die Identität `EIRFPLR(PLR) = PLR` und rechnen damit wie heute linear; in 8 Sätzen steht in `coeff4` ein Wert, den
die quadratische Form nicht liest. Das EER-Verhältnis `PLR / EIRFPLR(PLR)` bei 50 % Last reicht von 0,89 (Turbo,
feste Drehzahl) über 1,00 bis 1,09 (Scroll) bis 1,23 bis 1,39 (drehzahlgeregelt).

**2.9 Referenzprojekte mit Kältemaschine.** In der Testdatenbank tragen **nur 1055** (20 kW, Trockenkühler,
Mindestteillast 20 %, eigener Zähler, Kältespeicher) **und 1059** (die Maschine aus 1055 auf 10 kW skaliert,
AK3‑K, Kälteschranke) eine Kältemaschine; 1061 und 1062 kühlen als Kopien von 1058 mit der Wärmepumpe. Kein
Referenzprojekt führt ein Typkennfeld.

## 3 Fachmodell

### 3.1 Größen

| Zeichen | Bedeutung | Einheit |
|---|---|---|
| T_rk, T_kw | Rückkühltemperatur (Eintritt Verflüssiger), Kaltwasservorlauf (Austritt Verdampfer) | °C |
| Q_av | verfügbare Kälteleistung einer Maschine aus dem Kennfeld bei (T_rk, T_kw) | kW |
| EER_KF | EER des Kennfelds bei Volllast und (T_rk, T_kw) | — |
| Q | gedeckte Kälte einer Maschine in der Stunde | kWh |
| PLR | Lastgrad `Q / (Q_av · 1 h)` | — |
| P_min | kleinste Dauerleistung `min(m · Q_nenn, Q_av)` mit Mindestteillast m (Bestand) | kW |
| PLR_min | `P_min / Q_av` | — |
| x_u | untere Gültigkeit der Teillastkurve | — |
| C_d | Teillastkoeffizient des Taktens | — |
| P_el | Verdichterstrom einer Maschine in der Stunde (ohne Hilfsstromzuschlag) | kWh |

### 3.2 Lastachse (EIRFPLR)

**Kurve.** Die Teillast-Leistungsaufnahme ist ein Polynom zweiten Grades im Lastgrad, wie im EIR-Modell der offenen
Kurvensätze:

    EIRFPLR(x) = a + b · x + c · x²

**Normierung (Abl.).** Damit Volllast genau das Kennfeld trifft, rechnet EPOS mit der auf Volllast normierten Kurve

    E(x) = EIRFPLR(x) / EIRFPLR(1)

(`EIRFPLR(1)` liegt bei den Typkennfeldern zwischen 0,988 und 1,005; ohne Normierung verschöbe die Kurve den
Volllast-EER um bis zu 1,2 %.)

**Teillast über der Mindestteillast** (PLR_min ≤ PLR ≤ 1):

    P_el = Q_av / EER_KF · E(max(PLR, x_u)) · PLR / max(PLR, x_u)

In Worten: Strom bei Volllast mal normierte Kurve; unter der Kurvengültigkeit x_u bleibt das EER-Verhältnis der
Kurve an x_u stehen (die Kurve wird nicht unter ihre Gültigkeit fortgesetzt). Daraus der EER der Stunde

    EER(PLR) = EER_KF · g(PLR),   g(PLR) = PLR / E(PLR)   [für PLR ≥ x_u]

**Linear als Vorgabe.** Mit `E(x) = x` ist g = 1 und `P_el = Q / EER_KF` — **genau der heutige Weg**. Er bleibt der
Weg jeder Maschine ohne Eingabe (Abschnitt 4.1, `Teillast_Weg` leer) und ist als „linear“ wählbar.

**Herkunft der Kurve.** (a) Copper-Satz: `coeff1`, `coeff2`, `coeff3` → a, b, c; `x_min` → x_u. (b) CSV-Vorlage:
optionale Zeilen `Teillast;Lastgrad;EER-Verhaeltnis` (EER bei Teillast durch EER bei Volllast, gleiche Temperaturen);
der Import passt die Kurve nach kleinsten Quadraten an (Abl.: aus `g_i` folgt `E(x_i) = x_i / g_i`; mindestens drei
Zeilen, sonst nur linear), x_u = kleinster Lastgrad der Zeilen. (c) Eingabe im Katalogdialog (drei Beiwerte).
(d) Vorgabekurve je Verdichterregelung, wenn `Teillast_Weg = KURVE` ohne Beiwerte gewählt ist (KM3‑Q3).

**Plausibilität (Prüfregel, Vorgabe).** Eine Kurve gilt, wenn `E(x) > 0` auf [x_u, 1], `0,9 ≤ EIRFPLR(1) ≤ 1,1` und
`0,3 ≤ g(x) ≤ 2,0` auf [max(x_u, 0,1), 1]; sonst lehnt `KaeltemaschineStammCtrl.Pruefen` sie benannt ab. Die untere
Grenze 0,3 fängt Unsinn ab, lässt aber die starke Teillastabwertung von Turbo- und Schraubenverdichtern mit fester
Drehzahl (g ≈ 0,35 bis 0,5) als reale Kurve zu. Dieselbe Grenze gilt für Import, Katalogprüfung und Vorgabekurven. Im
Lauf rechnet eine ungültige Kurve (etwa aus einem Altbestand) linear und meldet das einmal je Maschine im Protokoll.

**Zahlenbeispiel (nachgerechnet).** EER_KF = 4,0, Q_av = 20 kW, Kurve a = 0,10, b = 0,60, c = 0,30 (EIRFPLR(1) = 1,0),
x_u = 0,2. Last 10 kWh, PLR 0,5: E(0,5) = 0,475; P_el = 20/4 · 0,475 = **2,375 kWh**, EER 4,21 (linear: 2,500 kWh).

### 3.3 Takten unter der Mindestteillast

**Regel (Hausmuster, unverändert übernommen).** Liegt die Kälte der Stunde unter P_min, taktet die Maschine. Der
Strom ohne Taktverlust ist der Strom am Mindestpunkt, anteilig zur Laufzeit (Taktverhältnis wie im EIR-Modell):

    P_el,0 = Q / (EER_KF · g(PLR_min))

Darauf kommt der Taktverlust der Wärmepumpe (`Waermepumpentakt`):

    CR = Q / P_min,   f = CR / (C_d · CR + (1 − C_d))   für 0 < CR < 1
    P_el = P_el,0 / f,   Mehrstrom = P_el,0 · (1/f − 1)

C_d gepflegt 0 … 1, leer **Vorgabe 0,9** (`Waermepumpentakt.VORGABE_CD`, dieselbe Vorgabe wie an der Wärmepumpe;
EN 14825 nennt den Koeffizienten). Die Starts zählt `Waermepumpentakt.StartsImTakt` mit der Mindestlaufzeit 10 min,
eine Laufphase außerhalb des Taktens zählt einen Start wie in `Kaeltekaskade.Taktverlust`. **Keine zweite Fassung:**
Die Kältemaschine ruft dieselben Funktionen; der Mehrstrom geht wie an der Wärmepumpe vor dem Hilfsstromzuschlag in
den Verdichterstrom und steht in `Taktstrom_stuendlich`.

**Zahlenbeispiel (Fortsetzung).** Mindestteillast 20 % von Q_nenn 20 kW → P_min 4 kW, PLR_min 0,2. Last 2 kWh:
g(0,2) = 0,2/0,232 = 0,8621; P_el,0 = 2 / 3,448 = 0,580 kWh; CR 0,5; f = 0,5/0,55 = 0,9091; **P_el = 0,638 kWh**,
Mehrstrom 0,058 kWh (Bestand: 0,500 kWh; linear mit C_d: 0,550 kWh).

**Kältespeicher (Abl.).** Ein Kältespeicher puffert das Takten ohne eigene Regel: Die Kaskade gibt der Maschine Raum
und Ladewunsch als eine Last; in der Ladephase hebt der Ladewunsch die Last über P_min, die Maschine läuft im
Teillastbereich, und die Entladung deckt spätere Schwachlaststunden ohne Verdichter. Die Taktstunden sinken damit
von selbst; das Referenzprojekt 1063 (Abschnitt 8) weist es aus. Eine Vorrangregel „Speicher statt Takten“ ist nicht
vorgesehen.

**Verdichterregelung.** Stufen und Drehzahl gehen nicht als eigene Rechenachse ein, sondern über Kurve und
Mindestteillast: Eine gestufte Maschine taktet unter ihrer kleinsten Stufe (Mindestteillast = kleinste Stufe), eine
drehzahlgeregelte hat eine nach oben gewölbte g-Kurve und eine kleine Mindestteillast. Das Stammfeld
`Verdichterregelung` (EIN_AUS, STUFEN, DREHZAHL) wählt die Vorgabekurve, wenn keine Kurve gepflegt ist (KM3‑Q3), und
füllt beim Copper-Import `compressor_speed` (constant → STUFEN, variable → DREHZAHL, leer → leer).

### 3.4 Ränder: konstanter Gütegrad

**Heute** hält `KaeltemaschinenKennlinie` außerhalb der Stützstellen den Randwert. Bei gleitendem Kaltwasser über
der obersten Stützstelle (Kühlkurve KK, Kühldecken) unterschätzt das den EER, bei Rückkühlung über dem Kurvenende
überschätzt es ihn (Recherche, „Das EIR-Modell …“; KM1, „Was Stufe 2 noch braucht“).

**Neuer wählbarer Weg „Gütegrad“** (Muster EN 15316-4-2, nur genannt): Der EER wird außerhalb des Kennfelds mit dem
Gütegrad des nächsten Randpunkts fortgesetzt, die Leistung bleibt am Randwert:

    EER_C(T_kw, T_rk) = (T_kw + 273,15) / max(T_rk − T_kw, ΔT_min)
    η_R = EER_Rand / EER_C(Randpunkt)
    EER(T) = η_R · EER_C(T)

mit dem Randpunkt = Auswertung bei den auf das Kennfeld geklemmten Temperaturen (das ist der heutige Randwert).
**Vorgaben:** Mindesthub ΔT_min = 5 K; Extrapolationsweite höchstens 10 K je Achse, darüber bleibt der Wert der
10-K-Grenze; der EER ist auf den EER-Ersatz der freien Kühlung (15) gedeckelt. Die Stunde zählt weiter als
Randstunde und zusätzlich als „extrapoliert“.

**Zahlenbeispiele (nachgerechnet).** Kaltwasser über dem Rand: Rand EER 5,0 bei 12 °C / 30 °C → η_R = 0,3156; bei
16 °C Kaltwasser EER = 0,3156 · 20,654 = **6,52** (Randwert: 5,0). Rückkühlung über dem Rand: Rand EER 2,6 bei
7 °C / 45 °C → η_R = 0,3527; bei 50 °C EER = 0,3527 · 6,515 = **2,30** (Randwert: 2,6).

**Rasterregel der Typkennfelder.** Das KM1-Raster (Kaltwasser 5/7/10/15 °C) hält außerhalb der Copper-Gültigkeit
bereits beim Erzeugen den Kurvenrandwert — die Stützstelle 15 °C ist bei vielen Sätzen schon ein Randwert. Der
Gütegradweg wirkt deshalb erst voll, wenn auch die Rasterregel außerhalb der Gültigkeit mit Gütegrad statt mit
Klemmung rechnet (KM3‑Q4).

### 3.5 Herkunft: Skalierung auf den Nennpunkt eines Datenblatts

Das KM1-Papier beschreibt die Skalierung als vorbereitet, nicht angeboten. **Schnellwahl im Katalogdialog**
„Typkennfeld auf Datenblatt skalieren…“: Der Anwender wählt ein Typkennfeld und gibt Nennkälteleistung und Nenn-EER
seines Geräts am KM1-Nennpunkt ein (Kaltwasser 7 °C; Luft: Außenluft 35 °C, im Kennfeld Rückkühlung 40 °C; Wasser:
Kühlwasser-Eintritt 30 °C). Es entsteht ein **eigener Satz** (`ReadOnly = 0`) mit

    Q_i' = Q_i · Q_nenn,DB / Q_nenn,KF       EER_i' = EER_i · EER_nenn,DB / EER_nenn,KF

an allen Stützstellen; Teillastkurve, Mindestteillast, Verdichterregelung und Rückkühlart werden übernommen, die
Beschreibung nennt das Ausgangs-Typkennfeld (Text, keine ID — Hausregel KP1b). **Kontrollwert:** `Nenn_EER` wird in
`Pruefen` gegen den Kennfeld-EER am Nennpunkt gehalten; weicht er um mehr als 10 % (Vorgabe) ab, erscheint ein
Hinweis, kein Fehler.

### 3.6 Mehrere Maschinen einer Anlagenzeile

**Heute** gleichmäßige Teilung auf alle Maschinen (2.4). Mit Lastachse und Takten teilt das bei Schwachlast die Last
auf `Anzahl` taktende Maschinen. **Vorschlag (Abl., KM3‑Q6):** Mit gesetztem `Teillast_Weg` gilt Folgeschaltung —
es laufen `n = ⌈Q_gesamt / Q_av⌉` Maschinen (mindestens 1, höchstens `Anzahl`), die laufenden teilen gleichmäßig.
Ohne `Teillast_Weg` bleibt die gleichmäßige Teilung (byte-gleich). Die Listenfolge mehrerer Anlagenzeilen in der
Kaskade bleibt unverändert.

### 3.7 Ökodesign-Punkte A bis D als Ausweis

Die Teillastpunkte eines Ökodesign-Datenblatts (Verordnung (EU) 2016/2281, nur genannt) liegen auf einer Diagonale,
auf der Außentemperatur und Lastgrad zugleich sinken; ein Kennfeldraster liefern sie nicht (Recherche). EPOS weist
sie deshalb **nur aus dem eigenen Kennfeld** aus: eine **Auskunft im Katalogdialog**, in die der Anwender bis zu vier
Paare (Außentemperatur, Lastgrad) einträgt; EPOS rechnet je Paar Rückkühltemperatur nach Rückkühlart, Q_av und EER
nach 3.2 bis 3.4 bei Kaltwasser 7 °C und zeigt Leistung, Lastgrad und EER. Keine Normtafel, keine voreingetragenen
Punkte, keine Speicherung (KM3‑Q5). Eine SEER-ähnliche Jahreszahl entsteht **nur aus den Simulationsstunden**:
`Jahres-EER = Kälte / Strom` des Laufs (`kaelte.km.jaz`, vorhanden) und neu `Jahres-EER ohne Hilfsstrom` (Kälte /
Verdichterstrom).

### 3.8 Bezug +5 K bei Luft

KM1 hat als Konzeptentscheid festgelegt: EPOS wertet das Kennfeld einer luftgekühlten Maschine an Außenluft + 5 K
aus (`KaelteFestwerte.GRAEDIGKEIT_LUFT_K`), der Copper-Import wertet jede Rasterzeile an T_rk − 5 K aus. Das gilt auch
für die Lastachse (die Copper-Teillastkurve hängt nicht von der Temperatur ab) und für die Ökodesign-Auskunft
(Außentemperatur + 5 K). **KM3‑Q7** stellt den Entscheid zur Bestätigung, weil KM3 ihn in zwei weitere Wege trägt.

## 4 Eingaben

### 4.1 Neue Spalten an `Tab_Kaeltemaschine_STAMM` und `Tab_Kaeltemaschine` (Vorschlag)

Gerätewerte gehören zum Gerät — Katalog und Projektkopie, über `KaeltemaschineSchema.Fachspalten` an Projektkopie,
Schreibwegen und Katalogfassung. Alle Spalten nullbar; **leer = Vorgabe**.

| Spalte | Typ | Bereich / CHECK | Vorgabe bei NULL | Bedeutung |
|---|---|---|---|---|
| `Teillast_Weg` | TEXT | `CHECK (Teillast_Weg IN ('LINEAR','KURVE'))` | leer = **heutiger Weg** (linear, Takt ohne Verlust, gleichmäßige Teilung) | LINEAR: g = 1 mit Taktverlust; KURVE: Lastachse 3.2 mit Taktverlust |
| `Teillastkurve_a` | REAL | −1 … 2 | — (ohne Beiwerte: Vorgabekurve nach `Verdichterregelung`, sonst linear) | Beiwert a von EIRFPLR |
| `Teillastkurve_b` | REAL | −2 … 3 | — | Beiwert b |
| `Teillastkurve_c` | REAL | −2 … 3 | — | Beiwert c |
| `Teillastkurve_Lastgrad_Min` | REAL | `CHECK (… BETWEEN 0 AND 1)` | Mindestteillast, sonst 0 | untere Gültigkeit x_u |
| `Taktverlustfaktor_Cd` | REAL | `CHECK (… BETWEEN 0 AND 1)` | 0,9 (`Waermepumpentakt.VORGABE_CD`), wirksam nur mit `Teillast_Weg` | C_d; derselbe Name wie an `Tab_WP` |
| `Verdichterregelung` | TEXT | `CHECK (Verdichterregelung IN ('EIN_AUS','STUFEN','DREHZAHL'))` | leer = keine Angabe | wählt die Vorgabekurve |
| `Kennfeld_Randweg` | TEXT | `CHECK (Kennfeld_Randweg IN ('RANDWERT','GUETEGRAD'))` | `RANDWERT` | Extrapolation 3.4 |

Die Bereiche der Beiwerte sind Eingabegrenzen des Dialogs; maßgeblich ist die Plausibilitätsprüfung 3.2. Eine
**Stützstellentabelle für die Lastachse ist nicht vorgesehen**: Die CSV-Zeilen werden beim Import zu drei Beiwerten
angepasst, der Katalog hält nur die Kurve (eine Quelle).

### 4.2 Anlagenzeile

Keine neue Spalte an `Tab_Energieanlagen`: `Kaeltemaschine_Anzahl` trägt die Zahl gleicher Maschinen, die
Folgeschaltung hängt an `Teillast_Weg` (KM3‑Q6). `Kuehl_Vorlauf` und `Kuehl_Hilfsstromanteil` bleiben.

### 4.3 Import

| Weg | Was neu gelesen wird |
|---|---|
| Copper `chiller_curves.json` (`KaeltemaschineImportLeser`, `KaeltemaschinenKennfeld`) | `eir-f-plr` (`coeff1` bis `coeff3`, `x_min`), `compressor_speed` → `Verdichterregelung`; `Teillast_Weg = KURVE`, wenn die Kurve die Plausibilität besteht, sonst leer mit Info-Meldung; `coeff4` und folgende einer `quad`-Kurve werden nicht gelesen |
| CSV-Vorlage `Quellen/Kaeltemaschine_Kennfeldvorlage.csv` | Kopfzeilen `Verdichterregelung`, `Taktverlustfaktor_Cd`, `Kennfeld_Randweg`; Datenzeilen `Teillast;Lastgrad;EER-Verhaeltnis` (optional, ≥ 3 für eine Kurve); Kommentarzeilen der Vorlage erklären beides |
| Eingebaute Typkennfelder (`KaeltemaschinenTypkennfelder.Einspielen`) | Kurve, x_u und Verdichterregelung aus dem gespeicherten Copper-Satz beim Einspielen; neu `Ergaenzen()` für schon eingespielte Sätze: füllt die neuen Spalten nur, wo sie leer sind, und erneuert die Prüfsumme — idempotent wie Schritt 203 |

### 4.4 Vorgabekurven je Verdichterregelung (Abl., KM3‑Q3)

Gilt nur für `Teillast_Weg = KURVE` ohne gepflegte Beiwerte. Vorschlag: je Regelung die Kurve des Typkennfelds, dessen
g(0,5) dem Median der Gruppe am nächsten liegt (EIN_AUS: Hubkolben/Scroll, STUFEN: Schraube und Turbo mit fester
Drehzahl, DREHZAHL: drehzahlgeregelte Sätze), als benannte Konstanten in `KaelteFestwerte` mit Herkunftsvermerk
„abgeleitet aus den Typkennfeldern (BSD-2)“. Ohne Regelung gilt linear.

## 5 Rechenweg je Stunde

### 5.1 Reihenfolge (eine Maschine; ersetzt 2.3 Schritte 3 bis 5, nur mit `Teillast_Weg`)

1. Rückkühltemperatur, freie Kühlung — **unverändert**.
2. Kennfeldpunkt (Q_av, EER_KF) nach `Kennfeld_Randweg` (3.4).
3. Folgeschaltung: n laufende Maschinen, Last je Maschine `L = Last / n` (3.6).
4. `Q = min(L, Q_av)`, `PLR = Q / Q_av`, `P_min`, `PLR_min`.
5. `PLR ≥ PLR_min`: `P_el` nach 3.2. `0 < PLR < PLR_min`: `P_el,0` nach 3.3, Taktverlust über `Waermepumpentakt`.
6. Hilfsstrom der Rückkühlung `= Hilfsstrom_Rueckkuehlung_kW · Q / Q_av` — **unverändert** (Laufanteil der Kälte).
7. Kaskade: `Strom = P_el · (1 + Kuehl_Hilfsstromanteil) + Hilfsstrom` — **unverändert**; Mehrstrom in
   `Taktstrom_stuendlich`, Starts und Taktstunden je Maschine.

Ohne `Teillast_Weg` rechnet die Stunde **Zeichen für Zeichen wie heute** (Abschnitt 8.1).

### 5.2 Was unverändert bleibt

Kältespeicher (Ladewunsch, Entladung, Bereitschaftsverlust), freie Kühlung, Listenfolge der Kaskade, Kälteschranke
im AK3-Kreis, gleitender Vorlauf (KK), Zähler und Abrechnung (`Kaeltestromabrechnung`), Kosten und Emissionen — sie
lesen den Stundenstrom und bekommen den Mehrstrom ohne eigene Änderung.

### 5.3 Kennzahlen und Ergebnisspalten

Neue Spalten an `Tab_ErgebnisKaeltemaschine` (nullbar; leer bei Maschinen ohne `Teillast_Weg`, damit Bestandsläufe
dieselben Zeilen schreiben):

| Spalte | Typ | Inhalt |
|---|---|---|
| `Taktstrom_MWh` | REAL | Mehrstrom aus Taktverlust |
| `Starts` | INTEGER | Starts im Jahr |
| `Teillaststunden` | INTEGER | Stunden mit PLR_min ≤ PLR < 0,95 (Vorgabe) |
| `Lastgrad_Mittel` | REAL | kältegewichteter mittlerer Lastgrad |
| `Stunden_Extrapoliert` | INTEGER | Stunden mit Gütegrad-Extrapolation |

Kennzahlen (`KennzahlenKatalog`, Muster `kaelte.km.*`): `kaelte.km.taktstrom`, `kaelte.km.starts`,
`kaelte.km.teillastanteil` (Teillaststunden / Laufstunden), `kaelte.km.lastgrad`, `kaelte.km.jaz_verdichter`
(Jahres-EER ohne Hilfsstrom); `kaelte.km.takt` (Stunden unter der Mindestteillast) und `kaelte.km.jaz` bestehen.

### 5.4 Bericht, KI-Sicht, Export

**Tafel „Teillast und Takten der Kältemaschinen“** (Muster Bivalenztafel aus UB‑E4): je Maschine Regelung, Weg,
C_d (mit „Vorgabe“), Laufstunden, Teillast- und Taktstunden, Starts, mittlerer Lastgrad, Jahres-EER mit und ohne
Hilfsstrom, Mehrstrom. **Bild** (optional, KM3‑Q9): EER über Lastgrad aus dem Kennfeld bei Nennbedingungen samt den
Stundenpunkten des Laufs, gezeichnet im `ChartRenderer`. Vorlagenfelder in `Vorlagenfeldkatalog`, Katalogfassung der
Vorlage + 1. KI-Sicht des Katalog- und des Anlagendialogs mit den Spaltennamen als Schlüssel (Muster
`WaermepumpeStammDaten.TaktverlustfaktorCd`); CSV-Export der Ergebnistafel mit denselben Schlüsseln.

## 6 Schema

- **Ein Schritt** für Eingabe- und Ergebnisspalten: nächste freie Nummer, **gebaut als 210** (hängt an 209
  `KatalogkostenInvestitionSchema`; 208 und 209 sind mit KA1 gebaut). Die Nummer wird **vor dem Bau** in der Zeile „Schemaschritt angemeldet“
  der [Statusdatei](../aktuell/Status_iOS_Migration.md) angemeldet; die Klasse `KaeltemaschineTeillastSchema` hängt über
  `SCHRITT = <Vorgängerklasse>.SCHRITT + 1` an der Klasse, die zur Bauzeit die höchste Nummer trägt.
- Spalten nach 4.1 und 5.3, alle nullbar, Texte mit `CHECK … IN (…)`, Bereiche mit `CHECK … BETWEEN`; Tabellen
  bleiben `STRICT`; kein Boolean neu (sonst `CHECK (… IN (0,1))`).
- **DML** nur an den ausgelieferten Typkennfeldern (`ReadOnly = 1`, `Katalog_Schluessel` `KM:TYPKENNFELD_…`): Kurve,
  x_u, Verdichterregelung und — je KM3‑Q2 — `Teillast_Weg` aus dem eingebetteten Copper-Satz, nur wo leer; Prüfsumme
  über `KatalogSchluesselSaat` neu. Projektkopien und eigene Sätze des Anwenders bleiben unberührt. Ein zweiter Lauf
  ändert nichts.
- Eintrag in `SchemaStand.Zielversion`, Paketanhebung (Art Katalog), `SchemaMigration` der Windows-Schale,
  `Werkzeuge/Testdatenbankschema`, Testkopie in `EPOS.Kern.Tests` — wie Schritt 203.
- **Katalogfassung:** Die neuen Fachspalten gehen über `KaeltemaschineSchema.Fachspalten` in Register „KM“; leere
  Spalten ändern keine Prüfsumme, die ergänzten Typkennfelder heben sie über den gewohnten Weg. Die
  **Vorlagen-Katalogfassung** steigt von 17 auf 18, sobald die Vorlagenfelder aus 5.4 dazukommen (KM3‑Q9; zur Bauzeit
  die dann nächste Fassung).
- `SqlDialektPruefer` nach jeder neuen Anweisung.

## 7 Oberfläche und Wiki

**7.1 Katalogdialog** (`EPOS.UI/Dialoge/Erzeuger/KaeltemaschineKatalogDialog.razor`, Daten, Texte, KiSicht; Hülle
`EPOS.UI.Daten/Erzeuger/KaeltemaschineKatalogHuelle.cs`): neue Stammblattgruppe **„Teillast und Takten“** (Ressource
vorhanden, Muster Kessel, BHKW, Wärmepumpe) zwischen „Kenndaten“ und „Kennlinie“ mit

- Auswahl „Teillastrechnung“ (leer: wie bisher · linear · Kurve), Auswahl „Verdichterregelung“;
- Zahlenfelder a, b, c, „Kurve gültig ab Lastgrad“, „Taktverlustfaktor C_d“ (Platzhalter „0,9 (Vorgabe)“);
- Auswahl „Kennfeldrand“ (Randwert · Gütegrad);
- Lesezeile: g(0,25), g(0,5), g(0,75) und Hinweis bei verworfener Kurve; kleine Kurve EER-Verhältnis über Lastgrad
  wie im BHKW-Dialog;
- Schnellwahl **„Kurve aus Typkennfeld“** (übernimmt Beiwerte, x_u, Regelung eines gewählten Typkennfelds),
  **„Typkennfeld auf Datenblatt skalieren…“** (3.5), Auskunft **„Teillastpunkte prüfen…“** (3.7).

Die Mindestteillast bleibt in „Kenndaten“; die Gruppe verweist darauf. Ausgelieferte Sätze sind lesend.

**7.2 Anlagendialog** (`KaeltemaschineAnlageDialog.razor`): Lesewerte der Projektkopie (Weg, Regelung, C_d mit
Herkunft) und Hinweis „Folgeschaltung von n Maschinen“ bei `Anzahl` > 1 und gesetztem Weg; keine neuen
Anlagenfelder. **Reiter Ergebnis:** Kacheln Taktstrom, Starts, Teillastanteil, Jahres-EER ohne Hilfsstrom.

**7.3 KI-Felder:** `KaeltemaschineKatalogKiSicht` um die acht Felder, `KaeltemaschineAnlageKiSicht` um die Lesewerte;
Ressourcen in beiden Sprachen, `designer_neu.py schreiben`.

**7.4 Wiki** (Repo-Quellen, gebündelter Upload): `Projekte/Wiki/Programm Dokumentation - Kühlung.wiki`
(Abschnitt „Kältemaschine“: Gruppe „Teillast und Takten“, Schnellwahlen, Ergebnisse) und
`Projekte/Wiki/Grundlagen - Kühlung.wiki` (Lastgrad, Teillastkurve, Takten, Gütegrad an den Rändern — in Worten, ohne
Normtafel). **Logbuch-Entwurf** (Version beim Anwender erfragen), je ein Satz: „Die Kältemaschine rechnet auf Wunsch
mit einer Teillastkurve und einem Taktverlust unter ihrer Mindestteillast.“ · „Kennfelder der Kältemaschine lassen
sich mit konstantem Gütegrad über ihren Rand hinaus fortsetzen und auf den Nennpunkt eines Datenblatts skalieren.“
Gegenlesen mit dem Muster aus `CLAUDE.md`; keine Geräte- und Firmendaten.

## 8 Regressionsnetz

**8.1 Byte-Gleichheit.** Alle neuen Spalten entstehen leer; mit leerem `Teillast_Weg` ruft die Maschine den heutigen
Weg unverändert (keine Folgeschaltung, kein Mehrstrom, `Kennfeld_Randweg` leer = Randwert). 1055 und 1059 bleiben
gegen R46 unverändert, ebenso alle übrigen Projekte; die neuen Ergebnisspalten bleiben in Bestandsläufen leer, die
Referenz-CSV bekommt keine neue Größe für Maschinen ohne Weg.

**8.2 Referenzprojekt 1063** (nächste freie Projektnummer) als **Kopie von 1055** — Vorschlag der gesäten Werte an
der Projektkopie der Kältemaschine:

| Feld | Wert | Zweck |
|---|---|---|
| `Teillast_Weg` | `KURVE` | Lastachse wirksam |
| `Teillastkurve_a/b/c` | 0,10 / 0,60 / 0,30 | Kurve des Zahlenbeispiels 3.2 (g(0,5) = 1,05) |
| `Teillastkurve_Lastgrad_Min` | 0,2 | Gültigkeit |
| `Taktverlustfaktor_Cd` | leer | prüft die Vorgabe 0,9 |
| `Mindestteillast_Prozent` | 30 | Taktstunden sichtbar |
| `Verdichterregelung` | `STUFEN` | Anzeige, keine Vorgabekurve (Beiwerte gepflegt) |
| `Kennfeld_Randweg` | `GUETEGRAD` | Trockenkühler läuft im Winter unter 25 °C Rückkühlung (unter dem Kennfeld) |
| übrige Zeilen | wie 1055 (Kältespeicher, eigener Zähler, Trockenkühler) | Wirkung allein durch KM3 |

Wache `EPOS.Kern.Tests/KaeltemaschineTeillastReferenzprojektWacheTests` (Muster
`KaeltemaschineReferenzprojektWacheTests`): jede gesäte Zelle, Kopie von 1055, im Lauf Taktstunden > 0,
`Taktstrom_MWh` > 0, `Stunden_Extrapoliert` > 0, Jahres-EER ≠ 1055. **CI-Auswahl:** Vorschlag aufnehmen (KM3‑Q10),
weil kein CI-Projekt eine Kältemaschine rechnet. Neue Basis **R47** im Schritt des Referenzprojekts, begründet in
[`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md).

**8.3 Einfrierregel (Entwurf für `CLAUDE.md`, nicht dort eingetragen):**

> - gesäte Teillastdaten einer Kältemaschine eines Referenzprojekts: an ihrer Projektkopie `Teillast_Weg`,
>   `Teillastkurve_a`, `Teillastkurve_b`, `Teillastkurve_c`, `Teillastkurve_Lastgrad_Min`, `Taktverlustfaktor_Cd`,
>   `Verdichterregelung`, `Kennfeld_Randweg` und `Mindestteillast_Prozent`, die Festwerte des Rechenwegs (Mindesthub
>   und Extrapolationsweite des Gütegrads, Vorgabekurven je Verdichterregelung, Grenze der Teillaststunden) und die
>   Vorgabe `Waermepumpentakt.VORGABE_CD`, dazu das Anlegen oder Entfernen eines Referenzprojekts mit Teillastkurve.

## 9 Fragen an den Anwender

| Kennung | Gegenstand | Optionen | Empfehlung |
|---|---|---|---|
| **KM3‑Q1** | Vorgabe C_d | a 0,9 wie an der Wärmepumpe (`VORGABE_CD`) · b eigene Vorgabe der Kältemaschine | **a** — ein Wert für beide Kälteerzeuger, keine zweite Fassung |
| **KM3‑Q2** | Kurven der Typkennfelder | a beim Einspielen `Teillast_Weg = KURVE` setzen (wirksam) · b nur Beiwerte speichern, Weg leer | **a** — kein Referenzprojekt führt ein Typkennfeld, die Basis bleibt; bestehende Projektkopien bleiben unberührt |
| **KM3‑Q3** | Vorgabekurven je Verdichterregelung | a abgeleitet aus den Typkennfeldern (4.4) · b keine, ohne Beiwerte linear | **a** — gibt eigenen Sätzen ohne Kurve einen Weg; Herkunft benannt |
| **KM3‑Q4** | Extrapolationsweg | a „Gütegrad“ wählbar, Vorgabe Randwert, Rasterregel der Typkennfelder ebenfalls mit Gütegrad · b nur Laufzeit, Raster wie KM1 · c nicht | **a** — ohne Raster-Änderung bleibt die 15-°C-Stützstelle ein Randwert und KK gewinnt nichts |
| **KM3‑Q5** | Ökodesign-Punkte A–D | a Auskunft im Katalogdialog ohne Speicherung · b als Spalten gespeichert · c nicht | **a** — Ausweis ohne Normtafel und ohne Schema |
| **KM3‑Q6** | Mehrere Maschinen einer Anlagenzeile | a Folgeschaltung mit gesetztem Weg · b gleichmäßige Teilung bleibt | **a** — gleichmäßige Teilung ließe alle Maschinen gleichzeitig takten |
| **KM3‑Q7** | Bezug Außenluft + 5 K bei Luft | a bestätigen, auch für Lastachse und Auskunft · b Außenluft ohne Versatz | **a** — KM1-Konzeptentscheid, Rechenweg der Simulation unverändert |
| **KM3‑Q8** | Mindestlaufzeit | a keine Spalte, Starts mit 10 min wie die Wärmepumpe · b eigene Spalte `Mindestlaufzeit_min` | **a** — die Startzahl ändert den Strom nicht; Hausmuster der Wärmepumpe |
| **KM3‑Q9** | Bericht | a Tafel, Kennzahlen, Vorlagen-Katalogfassung + 1 · b zusätzlich Bild EER über Lastgrad · c nur Kennzahlen | **a**, Bild später — die Tafel trägt die Aussage |
| **KM3‑Q10** | 1063 in der CI-Auswahl | a ja · b nein | **a** — einziges CI-Projekt mit Kältemaschine |
| **KM3‑Q11** | Wiki-Umfang | a beide Kühlungsseiten und Logbuch (2 Sätze) · b nur Programmdokumentation | **a** |

**Technische Festlegungen zur Kenntnis (Widerspruch möglich):** P_min bleibt auf die Nennkälteleistung bezogen
(Bestand), nicht auf Q_av; die Kurve wird auf Volllast normiert; unter x_u bleibt das EER-Verhältnis stehen; der
Hilfsstrom der Rückkühlung folgt dem Kälteanteil, nicht der Laufzeit; der Mehrstrom trägt den Hilfsstromzuschlag wie
an der Wärmepumpe.

## 10 Abnahmekriterien

| Prüfung | Messbar erfüllt, wenn |
|---|---|
| Rechenproben ohne Datenbank (`KaeltemaschineTeillastTests`) | Zahlenbeispiele 3.2/3.3/3.4 auf 1e‑6: 2,375 kWh; 0,638 kWh mit Mehrstrom 0,058 kWh; 6,519 und 2,298; linear mit C_d 0,550 kWh; ohne Weg Zeichen für Zeichen der Bestand; ungültige Kurve → linear mit Meldung; Folgeschaltung 2 × 20 kW bei 15 kWh → eine Maschine läuft |
| Schema | Schritt mit Wiederholprobe, CHECK-Proben, Typkennfelder ergänzt, eigene Sätze unberührt (`KaeltemaschineTeillastSchemaTests`); SQL-Dialekt grün |
| Import | Copper-Satz liefert a, b, c, x_u, Regelung; CSV mit drei Teillastzeilen liefert Kurve, mit zwei keine (`KaeltemaschineImportTests`) |
| Referenzlauf | alle 27 Projekte gegen R46 innerhalb der CI-Toleranz unverändert; 1063 neu; Basis R47 eingefroren und begründet |
| Wachen | `KaeltemaschineTeillastReferenzprojektWacheTests`, `DokumentationLinkWacheTests`, `WikiProduktdatenWacheTests`, Vorlagenwachen grün |
| Oberfläche | bunit für Gruppe, Schnellwahlen, Platzhalter „Vorgabe“, Lesemodus der Auslieferung; Windows-Schale baut auf Linux |
| Gate | `dotnet test WP-Plan.Kern.slnf` grün nach dem Merge jeder Etappe |

## 11 Quellen

- Recherche Kälteanlagen vom 08.10.2026 (dieser Ordner, Zeile im [Index](../LIESMICH.md)) — EIR-Modell,
  Teillastlücke, Plausibilisierungswerte, Stufe 2, offene Klärungen (Normkauf EN 14825:2022 für Taktformeln und
  Bin-Verfahren beim Anwender; im Konzept nur genannt).
- [Umsetzung KM1](../aktuell/Kälteanlagen/2026-10-09_Umsetzung_KM1_Typkennfelder.md) — Formate, Rasterregel, Bezug +5 K, Nennpunkt,
  Typkennfelder, Schritt 203.
- [Entscheidungsvorlage Modellgrenzen](../aktuell/Entscheidungsvorlage_Modellgrenzen_Rechenwege.md) — WP1 Taktverlust.
- [Konzept Kühlung Gebäudesimulation](../aktuell/Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) — Kältemaschine,
  Kältespeicher, Kühlkurve.
- Vorlage für Aufbau und Entscheide: [Konzept Übergabegrenze und Bivalenz](Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md).
- Normen und Verordnungen, nur genannt: EN 14825, EN 15316-4-2, Verordnung (EU) 2016/2281, DIN/TS 18599-7.
- Offene Kurvendaten: PNNL Copper (BSD-2), Lizenzhinweis [`Quellen/LIZENZ_Kaeltemaschinen_Typkennfelder.txt`](../../Quellen/LIZENZ_Kaeltemaschinen_Typkennfelder.txt).


## Umsetzung — wie gebaut

Gebaut in den Etappen KM3‑E1 bis KM3‑E4 unter dem Entscheid E116 (Anwender, 09.10.2026), Statuszeilen #876 bis #879.

| Etappe | Gebaut | Protokoll |
|---|---|---|
| KM3‑E1 | Schemaschritt 210 `KaeltemaschineTeillastSchema` (Teillast- und Taktspalten im Katalog und in der Projektkopie, Ergebnisspalten), Import der Teillastkurve aus Copper und CSV, Vorgabekurven je Verdichterregelung (#876) | [`2026-10-09_KM3-E1_Schema_Katalog_Import.md`](Protokolle/Gebaeudesimulation/2026-10-09_KM3-E1_Schema_Katalog_Import.md) |
| KM3‑E2 | Rechenweg (Teillastkurve, Takten, Randweg, Folgeschaltung), Referenzprojekt 1063, Basis R49 (#877) | [`2026-10-09_KM3-E2_Rechenweg_1063_R49.md`](Protokolle/Gebaeudesimulation/2026-10-09_KM3-E2_Rechenweg_1063_R49.md) |
| KM3‑E3 | Katalogdialog (Gruppe „Teillast und Takten“), Lesewerte im Anlagendialog, Kachelzeile, Kennzahlen `kaelte.km.*`, Tafel `stand.tabelle.km_teillast`, Vorlagen-Katalogfassung 18 (#878) | [`2026-10-09_KM3-E3_Dialoge_Bericht_Vorlagen.md`](Protokolle/Gebaeudesimulation/2026-10-09_KM3-E3_Dialoge_Bericht_Vorlagen.md) |
| KM3‑E4 | Wiki-Quellen „Kühlung“ und „Grundlagen Kühlung“, Logbuch-Entwurf, diese Konzepte nach `ueberholt/` (#879) | [`2026-10-10_KM3-E4_Wiki_Logbuch_Konzepte.md`](Protokolle/Gebaeudesimulation/2026-10-10_KM3-E4_Wiki_Logbuch_Konzepte.md) |

**Abweichungen vom Konzept:** Schemaschritt 210 statt 208; Basis R49 statt R47; Plausibilitätsgrenze des Gütemaßes g ≥ 0,3 statt 0,5; ein gemeinsamer Taktstunden-Zähler; der Taktstrom der Kachel kommt im Kern in kWh; Vorlagen-Katalogfassung 18.

**Nachweis:** Referenzlauf 28 Projekte gegen R49, `GESAMT: PASS`, alle Projektdateien byte-gleich. Offen: Wiki-Upload (Freigabe und Versionsnummer beim Anwender).
