# Entwurf AH — Vorheizen vor dem Kalendersprung: bestehender Rechenweg gegen neues Konzept

**Stand 10.10.2026 · Entwurf zur Entscheidung, nichts gebaut.** Gelesen auf `8100e397` (Basis R50
`2026-10-10_R50_Wochentagsraster`). Grundlage: [Teilkonzept Konditionierungsprofile](../Konzept_Konditionierungsprofile_EPOS-Plan.md)
Kapitel 4 (Aufheizoptimierung, Stufe KP3), [Entwurf KP3](../../ueberholt/2026-10-02_Entwurf_KP3.md),
[Konzept Heizlastspitzen](2026-10-03_Konzept_Heizlastspitzen_Glaettung.md) (E60), die Entscheide E58–E60, E97 und E99 der
[Statusdatei](../Status_Gebaeudesimulation_VDI6007.md), das
[Protokoll KP3](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-02_KP3_Aufheizoptimierung.md), das
[A/B-Protokoll 1051](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-03_KP3_RP1_AB-Protokoll_1051.md) und die
Reihen des Referenzprojekts 1051 in der Basis R50.

**Auftrag (Anwender, Wortlaut):** „Aufheizleistung: Text und Funktion überarbeiten: Option 1: Aufheizleistungsrampe mit
Vorgabe Startzeit vor Kalender-Rampe (t-Vorheizen in Stunden), so dass die Heizleistung zum Zeitpunkt der Kalender-Rampe
nicht überschritten wird (bis auf x % — optionale Vorgabe). Es soll in einem Berechnungslauf geprüft werden, ob die
Zeitvorgabe t-Vorheizen ausreichend ist, um die Solltemperatur zu erreichen. 2. Die Zeitvorgabe soll berechnet werden aus
der verfügbaren zusätzlichen Heizleistung und der Vermeidung des Heizlastsprunges an der Kalender-Rampe (Heizlast durch
Kalenderrampe nicht erhöht). 4. Prüfe, wie es gegenwärtig berechnet wird, stelle das bestehende vs. neues Konzept dar.“

**Begriffe.** *Kalendersprung* (im Wortlaut „Kalender-Rampe“): die Stunde h_s, in der der Heizkalender den Sollwert von
θ_N auf θ_T anhebt (in 1051 werktags 07:00, 16 → 20 °C). *Vorheizen*: Heizbetrieb vor h_s, um die Bauteile vorzuwärmen.
*t_V*: Vorheizzeit in Stunden. *Φ_ref*: die Heizlast, die die Kalenderstunde ohne Aufheizen hätte (Abschnitt 2.1).
*x*: Toleranz am Kalendersprung. *P_V*: Vorheizleistung, die das Vorheizen höchstens beanspruchen darf.

---

## 1 Bestand: wie die Aufheizung heute gerechnet wird

### 1.1 Eingaben

| Eingabe | Ort | Wirkung |
|---|---|---|
| Schalter „Aufheizoptimierung rechnen“ | `Tab_Einstellungen.Aufheizoptimierung`; `Aufheizvorgabe.An` (`EPOS.Kern/Model/Aufheizvorgabe.cs:61`) | aus = kein Aufruf, Lauf bitgleich (`Vdi6007Rechenweg.cs:188`) |
| Bemessung (a) „kälteste Stunde“ / (b) „− ΔT_K“ | `Aufheiz_Bemessung`, `Aufheiz_Abzug_K` (`AufheizvorgabeSchema.cs:16–17`, Vorgabe 2 K `:77`) | legt nur die **längste** Aufheizzeit t_auf,max und die Erreichbarkeit fest, nicht P_auf (`Aufheizoptimierung.cs:439–444`) |
| Aufheizreserve ρ | `Aufheiz_Reserve` (`:18`, leer = 20 % `:83`) | P_auf = (1 + ρ)·Φ_stat(θ_T,max, T_a,min), wenn keine `Heizleistung_Max` gesetzt ist (`Aufheizoptimierung.cs:425–433`) |
| Art „täglich“ / „fest“ | `Aufheiz_Art` (`:19`) | täglich: kleinstes n, das P_auf hält; fest: n = t_auf,max + 1 an jedem Sprung (`Aufheizoptimierung.cs:295–341`) |
| Aufschlag h / % | `Aufheiz_Aufschlag_H`, `_Prozent` (`AufheizManuellSchema.cs:109`) | n′ = min(48, n + max(h, ⌈n·%/100⌉)), nur für n > 1 (`Aufheizoptimierung.cs:350–370`) |
| Aufheizzeit manuell je Gebäude | `Tab_Gebaeude.Aufheizzeit_Manuell_H` 1–47 h (`AufheizManuellSchema.cs:115–118`) | n = min(t + 1, D + 1) an jedem Sprung, ohne Leistungsgrenze und ohne Prüfung (`Aufheizoptimierung.cs:379`) |
| `Heizleistung_Max` des Gebäudes/der Zone | Gebäudedialog | Quelle „Grenze“: P_auf = Grenze, Bemessung am Augenblickswert; der Löser kappt dort (`Zonenmodell2K.cs:1113`) |

Die Oberfläche: Gruppe in `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor:444–518` — Schalter `:454`, Bemessung
`:460`, Abzug `:467`, Reserve `:474`, Art `:481`, Aufschlag `:486`/`:492`, Herleitungszeilen `:499–508`, Text bei „aus“
`:511`; Texte `SIMKONF_AUFH_*` in `EPOS.Kern/MyResource/Resource.resx:42119–42171`, `:43271–43308`, `:47486–47505`.

### 1.2 Rechenweg

Die Aufheizoptimierung ist ein **Vorab-Fahrplan**: Sie formt die Heizsollwertreihe einer Zone, bevor der Lauf beginnt
(`Vdi6007Rechenweg.cs:185–190` Einzone, `:256–259` Mehrzonen über `ZonenEingang.Bauen`). Der Lauf selbst rechnet danach
unverändert die ideale Regelung nach VDI 6007 (2K-Modell): Die Raumluft folgt dem Sollwert jeder Stunde, die Leistung ist,
was dafür nötig ist — begrenzt nur durch `Heizleistung_Max`, sofern gesetzt.

1. **Sprünge finden** (`Aufheizoptimierung.cs:769–818`): h_s mit s(h_s) − s(h_s − 1) > 0,01 K; Absenkdauer D = die
   zusammenhängenden Stunden unter θ_T davor. Übergänge aus „aus“ bekommen keine Rampe (W4).
2. **Bemessen** (`:407–466`): kälteste Heizstunde T_a,min, θ_T,max der Nutzungszeit;
   P_auf = `Heizleistung_Max` oder (1 + ρ)·Φ_stat(θ_T,max, T_a,min); je Variante (a)/(b) das ungünstigste Sprungpaar,
   daraus t_auf,max = n_max − 1 (Stufenformel bei T_a,B).
3. **Je Sprung** (`:166–231`): Fenster W = min(D, t_auf,max + 1); θ_N = kleinster Sollwert im Fenster, T_a = kälteste
   Außenluft im Fenster, Φ_stat(θ_T, T_a); n aus der **Stufenformel** (`Aufheizstufen.cs:169`): das kleinste n, für das
   die Leistung der Sprungstunde ≤ P_auf ist —
   Stundenmittel Φ̄_n = Φ_stat + ΔT/(n·h)·z·Γ(n·h)·v (`Aufheizstufen.cs:84`) bei Zielleistung,
   Augenblick Φ̂_n = Φ_stat + ΔT/n·z·(Σ Φ(h)^m)·v (`:92`) bei Quelle Grenze; Gleichgewicht bei θ_N als Anfangszustand.
4. **Schreiben** (`:189–206`): s′(h_s − n + j) = max(s, θ_N + ΔT·j/n), j = 1 … n − 1 — eine **Sollwerttreppe**, die
   letzte Stufe fällt in die Sprungstunde; gekappt an θ_K − 1 K.
5. **Lauf**: unveränderte Stunden der idealen Regelung mit der Treppe. Danach Zähler und Hinweise W1–W5, das
   Nachweisband W3 (`:721`: Stundenleistung > 1,01·P_auf im Fenster der Rampe) und die Auslegungsgröße nach E60:
   Auslegungsheizlast Φ_HL (E97) + Aufheizzuschlag max(0, P_auf − Φ_stat(θ_T,max, T_a,B)) (`Aufheizplan.cs:315`).

**Was der Bestand nicht tut.** Er begrenzt die Leistung **nicht** im Lauf (ohne `Heizleistung_Max` ist die Rampe nur ein
Sollwertfahrplan) und prüft die Ankunft nicht — die ideale Regelung erreicht den Sollwert per Bau in jeder Stunde. Sein
Maßstab ist eine **feste Zahl je Zone** (P_auf, an der kältesten Stunde bemessen), nicht die Last des Tages: Die Treppe
verhindert nur, dass eine Stunde **über P_auf** liegt. An jedem milderen Tag darf die Kalenderstunde bis an P_auf
springen; an Tagen, an denen schon n = 1 hält, gibt es gar keine Rampe.

### 1.3 Die Spitze an der Kalenderstunde in 1051 (Basis R50)

1051 ist ein Bürobau (Katalogbau 289 = Verw_I_40, 572 m²), Kalender „Büro“ 16/20 °C, Sprung jeden Arbeitstag um 07:00,
D = 13 h werktags und 61 h nach dem Wochenende; Bemessung (b) 2 K, täglich, Reserve leer (20 %), ohne `Heizleistung_Max`.
Kennwerte aus `aggregate.csv`: Heizwärme 24,22 MWh, Spitze 30,00 kW, P_auf 31,62 kW (Quelle Zielleistung) bei T_a,B
−20,17 °C, t_auf,max 27 h, längste Rampe im Lauf 12 h, 105 Rampentage, 300 Aufheizstunden, W1/W2/W3 je 0,
Auslegungsheizlast 22,19 kW, Aufheizzuschlag 3,92 kW. Ohne Aufheizoptimierung lag die Spitze desselben Baus bei
42,83 kW (A/B-Protokoll, Abschnitt 2).

Ausgewertet aus `heizsollwert_0.csv`, `waermebedarf_gebaeude.csv`, `raumtemperatur_0.csv` und `stundentemperatur.csv`
(148 Kalendersprünge, alle um 07:00):

| Stufenzahl n | Sprünge | T_a Median | Last vor der Rampe | Rampenstunde vor h_s | **Kalenderstunde h_s** | Mittel h_s+3 … h_s+8 |
|---|---|---|---|---|---|---|
| 1 (keine Rampe) | 43 | 8,0 °C | 0,5 kW | — | **13,8 kW** (Median 16,9, max 27,9) | 3,3 kW |
| 2 | 33 | 3,7 °C | 1,7 kW | 10,8 kW | **19,7 kW** | 4,3 kW |
| 3 | 21 | 0,6 °C | 4,3 kW | 20,2 kW | **24,3 kW** | 5,6 kW |
| 4 | 23 | −1,6 °C | 6,4 kW | 23,6 kW | **25,2 kW** | 6,3 kW |
| 5 | 17 | −3,1 °C | 7,1 kW | 24,8 kW | **25,0 kW** | 7,6 kW |
| 6–13 | 11 | −9,8 °C | 4,6 kW | 27,2 kW | **26,6 kW** | 7,9 kW |

(Mittelwerte je Gruppe; „Mittel h_s+3 … h_s+8“ ist die Last der Nutzungsstunden danach, mit inneren Gewinnen.)

- **Der Sprung bleibt.** Median der Kalenderstunde 23,8 kW gegen 4,2 kW in den Nutzungsstunden danach; wo vor der Rampe
  geheizt wurde, liegt die Kalenderstunde im Median beim 4,2-Fachen der Stunde vor Rampenbeginn. Gegen die geschätzte
  stationäre Last Φ_stat(20 °C, T_a) ohne Gewinne (Leitwert 0,69 kW/K aus Φ_stat(T_a,B) = 31,62 − 3,92 = 27,70 kW)
  liegt sie im Median beim 1,74-Fachen (p90 2,39).
- **An milden Tagen am deutlichsten.** Die 43 Sprünge ohne Rampe springen von 0,5 kW auf im Mittel 13,8 kW, bis 27,9 kW —
  zulässig, weil das unter P_auf = 31,62 kW bleibt.
- **Die Jahresspitze liegt in der Rampe, nicht am Sprung:** 30,00 kW am 19.01. um 06:00 (Rampenstunde, T_a −12,4 °C);
  alle 50 größten Stunden liegen in Rampen- oder Sprungstunden, 26 davon sind Kalenderstunden; außerhalb der
  Rampenfenster ist die größte Stunde 26,03 kW. Die Treppe hat die Spitze also auf P_auf gesenkt, aber **verschoben,
  nicht geglättet**: Jede Stufe der Treppe ist selbst ein kleiner Sprung.
- **Die Ankunft ist trivial erfüllt:** Die Raumluft steht in jeder Kalenderstunde auf 20,00 °C — weil die Leistung
  unbegrenzt ist, nicht weil die Rampe gereicht hätte. Eine Aussage „reicht die Vorheizzeit?“ liefert der Bestand nicht.

### 1.4 Schwächen des heutigen Texts der Gruppe

1. **„Aus“-Text erklärt „Ein“** (`SIMKONF_AUFH_HRL_AUS`, `Resource.resx:42155`): Er sagt, die Rampe halte die
   Heizleistung unter „der Aufheizleistung“ — der Begriff ist an dieser Stelle nicht erklärt, und dass an milden Tagen der
   Sprung bleibt, steht nirgends.
2. **Vier Gegenstände in einem Absatz** (`SIMKONF_AUFH_HRL_AN`, `:42158`): Quelle von P_auf, Bemessung, täglich/fest und
   Geltungsbereich; „Die Bemessung legt die längste Aufheizzeit fest“ lässt offen, dass die Bemessung P_auf nicht ändert.
3. **Formelzeichen und Formeln in der Oberfläche**: „kälteste Stunde − ΔT_K“, „Aufheizreserve ρ“, die Aufschlagformel
   n′ = min(48, n + max(…)) in `SIMKONF_AUFH_AUFSCHLAG_HRL` (`:47492`); „Stufen“ und „n“ werden nirgends eingeführt.
4. **Zwei Zeiten ohne Unterscheidung**: Die Herleitungszeile nennt „t_auf,max 27 h“ (Bemessungsfall bei −20,17 °C), der
   Lauf rampt höchstens 12 h — der Text sagt nicht, dass t_auf,max eine Bemessungsgröße ist.
5. **Wort „Sollwertrampe“ fehlt**: Dass die Optimierung den Sollwert stufenweise anhebt (und nicht die Leistung führt),
   erfährt der Anwender nicht; „Aufheizrampe“ wird als Leistungsrampe gelesen — so auch im Auftrag.
6. **Uneinheitliche Begriffe**: Gruppe „Aufheizoptimierung“, Hilfeknopf „Aufheizung“, Bericht „Aufheizzuschlag“,
   Herleitung „P_auf“; „Sprung des Heizsollwerts“ und „Kalender“ stehen für dasselbe.
7. **Doppelte Sätze**: „Gilt für das ganze Projekt; jedes Feld wird sofort gespeichert“ steht in beiden Haupttexten.

---

## 2 Neues Konzept

### 2.1 Grundgedanke und Bezugsgröße

Der Auftrag verlangt zweierlei, was der Bestand nicht leistet: (1) an der Kalenderstunde **keinen Lastsprung** gegenüber
der Last, die der Tag ohnehin hat, bis auf eine Toleranz x; (2) eine **Prüfung im Lauf**, ob die Vorheizzeit reicht.
Beides setzt eine Leistungsgrenze **im Lauf** voraus — mit unbegrenzter Leistung erreicht die ideale Regelung jeden
Sollwert, und es gibt nichts zu prüfen.

**Bezugsgröße Φ_ref** (je Sprung, vor dem Lauf): die stationäre Last der Kalenderstunde am Zielsollwert,
Φ_ref = Φ_stat(θ_T, T_a), mit derselben Funktion wie heute (`Zonenmodell2K.StationaereHeizlastW`, Außenform mit
Erdreich des Tags, Zusatzleitwert der Sprungstunde, ohne Sonne und Gewinne; T_a = kälteste Außenluft im Vorheizfenster
und in h_s). Das ist die wörtliche „Heizleistung zum Zeitpunkt der Kalender-Rampe“ ohne Aufheizen. Die Alternative ist
die Auslegungsheizlast Φ_HL (E97) als feste Zahl je Zone — Frage F2.

**Zwei Leistungsgrenzen:**

| Grenze | gilt | Wert |
|---|---|---|
| Vorheizleistung P_V | im Vorheizfenster [h_s − t_V, h_s − 1] | die verfügbare Leistung P_verf = `Heizleistung_Max`, sonst (1 + ρ)·Φ_stat(θ_T,max, T_a,min) — das heutige P_auf, unverändert bemessen |
| Kalenderdeckel P_K | ab h_s bis zum Ende des Sollwertblocks (nächster Sprung nach unten) | (1 + x)·Φ_ref, höchstens P_verf |

Im Vorheizfenster steht der Sollwert sofort auf θ_T (gekappt an θ_K − 1 K wie heute), die Leistung auf P_V gedeckelt:
Die Zone heizt mit **voller verfügbarer Leistung, aber nicht mehr**, und erreicht θ_T so früh wie möglich
(leistungsbegrenzter Optimalstart, „Leistungsplateau“). Ab h_s deckelt P_K: Sind die Bauteile genügend vorgewärmt,
liegt die Last darunter, und der Deckel greift nicht; sind sie es nicht, bleibt die Raumluft unter θ_T, und der Lauf
zählt es. Die Bedingung „kein Sprung“ ist also **im Lauf garantiert**; geprüft wird die Ankunft.

Die Sollwerttreppe des Bestands bleibt als Verfahren „Rampe“ (Frage F1). Eine **lineare Leistungsrampe** im
Vorheizfenster (P steigt von Φ_stat(θ_N) auf P_V) ist als Variante möglich, verlangt aber längeres Vorheizen für dieselbe
Wärme — Frage F4.

### 2.2 Physik im 2K-Modell nach VDI 6007

Zustand der Zone sind die beiden Massentemperaturen (Außen- und Innenbauteile); Luft und Oberflächen sind
kapazitätslos. Bei geregelter Heizung ist die Leistung affin im Zustand, bei gedeckelter Heizung (Betriebsfall
„Heizgrenze“, `Zonenmodell2K.cs:1113`) ist die Leistung fest und die Raumluft frei. Beide Fälle sind linear mit festen
Randwerten je Abschnitt und laufen über dieselben Matrixfunktionen (`Uebergangsrechner.Bei`); der Löser wechselt den Fall
innerhalb der Stunde, sobald die Grenze greift. **Ein stündlicher Deckel ist im Löser schon vorhanden**: `Stundenrand`
trägt `heizleistungMaxW` je Stunde (`Stundenrand.cs:48`, `:72`); heute füllt ihn der Eingang mit einem Skalar
(`GebaeudeModellEingang.cs:284`, `:697`). Option 1 braucht keine neue Physik, nur eine Reihe statt eines Skalars.

**Verlauf eines Vorheizens** bei festen Randwerten, Anfangszustand x₀ nach der Absenkung:

1. *Plateau* (0 ≤ t < t₁): Φ = P_V, Raumluft steigt frei; t₁ ist die Wurzel von θ_air(t) = θ_T — eine Summe zweier
   Exponentialfunktionen, eindeutig, weil θ_air bei P_V > Φ_stat(θ_T) monoton steigt.
2. *Abklingen* (t ≥ t₁): geregelt bei θ_T; die Mehrlast über Φ_stat fällt mit den Zeitkonstanten τ₁, τ₂ des geregelten
   Falls (dieselben Größen wie die `Aufheizantwort`, Teilkonzept 4.2): Φ(t) − Φ_stat ≈ Σ_k a_k·e^(−(t − t₁)/τ_k).
3. *Bedingung an h_s = t_V*: θ_air(h_s) ≥ θ_T − ε **und** Φ̂(h_s) ≤ (1 + x)·Φ_ref (Augenblickswert am Beginn der
   Stunde, wie die Kappung des Lösers).

**Erste Ordnung** (eine Masse C_w, Teilkonzept 4.2) als geschlossene Abschätzung und Vorschlagswert:

```
t₁ ≈ C_w·ΔT_m / (P_V − Φ_stat)                    Wärme bis zur Ankunft ÷ Leistungsreserve
t₂ ≈ τ₂ · ln( (Φ(t₁) − Φ_ref) / (x · Φ_ref) )     Abklingen der Mehrlast bis zur Toleranz
t_V ≈ ⌈t₁ + t₂⌉,   höchstens min(D, 47)
```

ΔT_m ist der Fehlbetrag der Massen gegenüber dem Gleichgewicht bei θ_T. **x → 0 heißt t₂ → ∞**: „Kein Sprung“ im
strengen Sinn ist asymptotisch; die Toleranz x ist physikalisch nötig, nicht nur Bequemlichkeit (Frage F3).

**Größenordnung an 1051.** Aus der Bemessung folgt die Überschusswärme eines 4-K-Sprungs: (P_auf − Φ_stat(T_a,B))·28 h
= 3,92 kW · 28 h ≈ 110 kWh (untere Schranke für C_w·ΔT). Vorgeheizt **nur mit dem Kalenderdeckel** (P_V = (1 + x)·Φ_ref)
ergibt die erste Ordnung je Sprung t_V ≈ 108 kWh / (x·Φ_ref):

| Bezug, Toleranz | t_V Median | t_V p90 | Sprünge mit t_V > D (von 148) |
|---|---|---|---|
| Tageslast Φ_stat(T_a), x = 20 % | 45 h | 77 h | 124 |
| Tageslast, x = 50 % | 18 h | 31 h | 106 |
| Tageslast, x = 100 % | 9 h | 15 h | 18 |
| Auslegungsheizlast Φ_HL = 22,19 kW, x = 20 % | 7 h | 11 h | 7 |

**Lesart.** Wer nur mit der Last des Tages vorheizt, braucht an milden Tagen ein Vielfaches der Nacht — die Absenkung
wäre aufgehoben. Deshalb trennt 2.1 die Vorheizleistung (verfügbare Leistung P_verf) vom Kalenderdeckel (Tageslast + x):
Nachts ist Leistung frei; nur die Kalenderstunde soll nicht springen. Mit P_V = 31,62 kW verkürzt sich t₁ an einem
Median-Tag (Φ_stat ≈ 11,9 kW) auf rund 5,5 h; t₂ hängt an τ₂ des Baus und wird in Welle V3 gemessen. Die Zahlen sind
Abschätzungen erster Ordnung ohne Gewinne, keine Messung.

### 2.3 Option 1 — Vorheizzeit vorgegeben, Prüfung im Lauf

**Eingaben:** t_V (h) je Projekt, am Gebäude übersteuerbar; Toleranz x (%); P_V und P_K nach 2.1.

**Vorab** (im Eingangsbauer, an der Stelle der heutigen Rampe): je Sprung mit D ≥ 1 das Fenster
[h_s − min(t_V, D), h_s − 1]; Sollwert dort max(s, min(θ_T, θ_K − 1 K)); Reihe P_max(h) = P_V im Fenster, P_K ab h_s bis
zum Ende des Sollwertblocks, sonst `Heizleistung_Max` bzw. unbegrenzt; jede Stunde min(Reihe, `Heizleistung_Max`).

**Im Lauf:** derselbe Löser mit der Deckelreihe. Je Sprung wird festgehalten:

- θ̄_air(h_s) und das Ankunftskriterium θ̄_air(h_s) ≥ θ_T − ε (ε nach Frage F7);
- die Unterschreitungsstunden im Sollwertblock (θ̄_air < θ_T − ε) und die größte Unterschreitung in K;
- das Sprungmaß Φ(h_s)/Φ_ref − 1 (zeigt, wie viel vom Deckel gebraucht wurde);
- bei Verfehlen der **Bedarf** t_V,nötig aus der Vorausrechnung von Option 2 — so sagt die Meldung, wie viele Stunden
  fehlen.

**Ergebnis:** Tage „Vorheizzeit reicht nicht“, Unterschreitungsstunden, größte Unterschreitung, größtes Sprungmaß,
größter Bedarf t_V,nötig; Hinweis einmal je Gebäude: „Gebäude …: An 12 Tagen erreicht die Raumluft zur Kalenderstunde
den Sollwert nicht (bis 1,4 K darunter); nötig wären bis 9 h Vorheizen statt 6 h.“

### 2.4 Option 2 — Vorheizzeit berechnet

**Eingaben:** Toleranz x; P_verf (wie heute P_auf: `Heizleistung_Max` oder Reserve ρ); keine Zeit.

**Vorausrechnung je Sprung** (vor dem Lauf, deterministisch, ohne Datenbank, Muster `Aufheizstufen`): feste Randwerte
(T_a = kälteste Außenluft im Fenster, Erdreich des Tags, ohne Sonne und Gewinne — zur sicheren Seite),
1. Anfangszustand: eingeschwungen bei θ_T, dann D − t Stunden Absenkung auf θ_N (geregelt oder frei, wie der Löser
   rechnet) — das ersetzt die heutige Gleichgewichtsform bei θ_N, die werktags (D = 13 h) zu lang vorheizt;
2. t Stunden Plateau mit P_V, dann h_s mit P_K;
3. t_V = kleinstes t ∈ {0 … min(D, 47)} mit erfüllter Bedingung 2.2 Nr. 3. Mehr Vorheizen wärmt die Massen mehr; die
   Bedingung ist monoton in t, also Bisektion: höchstens sechs Kandidaten zu je ≤ 110 Stunden-Schritten (D ≤ 61 h in
   1051, Fenster ≤ 48 h) mit den Matrixfunktionen des Lösers — unter 1 ms je Sprung, kein Zweitlauf.
4. Hält kein t ≤ min(D, 47): „unerreichbar“ (Zähler wie W1), t_V = min(D, 47) — die Absenkung entfällt an diesem Tag.

Danach schreibt Option 2 dieselben Reihen wie Option 1, und **der Lauf prüft wie in Option 1** (die Vorausrechnung
irrt zur sicheren Seite; die Wahrheit ist der Lauf, Grundsatz 7 des Teilkonzepts). **Bemessung (a)/(b)** bleibt als
Bemessungsfall: t_V,max am kältesten Punkt (T_a,B) für Anzeige, Herleitungszeile und Auslegung. Die geschlossene
Abschätzung 2.2 dient als Probe (Prüforakel) und als Vorschlag im Gebäudedialog.

### 2.5 Mehrzonen, Kopplung, Kalender, Kühlung

- **Mehrzonen:** je Zone eigene Φ_ref, P_K, P_V (Zonenanteil wie heute) und Vorausrechnung in der Nachbarform
  (`Aufheizzone.AusZonen`); Nachbarn ohne Vorheizen — zur sicheren Seite, weil gleichzeitig vorheizende Nachbarn die
  Last senken. t_V aus Option 1 erben die Zonen vom Gebäude. Gebäudewerte: Tage als Vereinigung, Unterschreitung als
  Maximum, t_V als Maximum, P_K als Summe (Muster `Aufheizoptimierung.Gebaeudewerte`).
- **AK1 (Heizkreis, Einzone):** heute benannt nicht optimiert (W5). Option 1 braucht keine Vorhersage: Das
  Sollwert-Zeitprogramm des Heizkreises wird um t_V vorgezogen, der Deckel wirkt als Begrenzungsgrund `HeizleistungMax`
  im Übergabeweg (`Zonenmodell2K.cs:956`, `:577`), Übergabe und P-Regler begrenzen ohnehin; die Laufprüfung gilt
  unverändert. Option 2 rechnet die Vorausrechnung am idealen Modell (Vorschlag) und prüft im Lauf; eine Vorausrechnung
  mit Übergabe bei Heizkurvenvorlauf ist eine spätere Verfeinerung (KP3b). Empfehlung: W5 für die neuen Verfahren
  aufheben (Frage F10).
- **AK2 (Verfügbarkeit):** Deckel und Verfügbarkeitsschranke gelten zusammen (Minimum).
- **AK3 (geschlossener Kreis):** Der Stepper bekommt den Plan über `AufheizplanSetzen` (`Vdi6007Rechenweg.cs:207`);
  die Deckelreihe reist im Plan mit. Ob der Erzeuger P_V wirklich liefert, zeigt der Kreis; die Laufprüfung misst die
  Ankunft am Ergebnis des Kreises. Auskünfte rechnen mit Rückstufe auf den Profilweg (`Ak3Kernstufe`).
- **Wochenende und Ferien:** Nach 61 h ist die Zone nahe am Gleichgewicht bei θ_N, die Vorausrechnung wird dort zur
  heutigen Gleichgewichtsform; werktags (D = 13 h) sind die Massen wärmer, t_V kürzer — das gewinnt die neue
  Anfangsbedingung gegenüber dem Bestand. Ferien wie Wochenende, Deckel 47 h. Zwei Anstiege (16 → 18 → 20 °C) bleiben
  zwei Sprünge mit θ_N nach E58 F2.
- **Heizperiode und „aus“:** Ein Übergang aus „aus“ bekommt weiter kein Vorheizen (W4) und wird aus der Ankunftsprüfung
  herausgezählt (eigener Zähler).
- **Kühlung:** Der Vorheizsollwert bleibt unter θ_K − 1 K (heutige Kühlkappe); der Deckel betrifft nur die Heizseite;
  an Tagen mit Tagesbetriebsart Kühlen (AK3-K) gibt es keinen Heizsprung. Vorkühlen ist dieselbe Rechnung gespiegelt
  und bleibt außerhalb (KP3b/KU3).
- **Nachtlüftung:** Φ_ref nimmt den Zusatzleitwert der Sprungstunde wie heute; die bedingte Nachtlüftung (1051:
  2 1/h bis 7 Uhr) lüftet bei Raumluft unter der Schwelle nicht und stört das Vorheizen nicht.
- **Auslegung (E60):** Mit Kalenderdeckel ist die größte Stunde der Nutzungszeit ≤ (1 + x)·Φ_ref; die Spitze des Jahres
  liegt im Vorheizen und ist ≤ P_verf. Die Auslegungsgröße bleibt Φ_HL + Aufheizzuschlag (P_verf − Φ_stat); neu daneben
  steht die Spitze der Nutzungszeit.

### 2.6 Text der Gruppe (Vorschlag)

Gruppe **„Aufheizen vor Nutzungsbeginn“** / *„Preheating before occupancy“*.

| Schlüssel (neu oder geändert) | Deutsch | Englisch |
|---|---|---|
| `SIMKONF_AUFH_GRP` | Aufheizen vor Nutzungsbeginn | Preheating before occupancy |
| `SIMKONF_AUFH_LBL_SCHALTER` | Aufheizen rechnen | Calculate preheating |
| `SIMKONF_AUFH_LBL_VERFAHREN` | Verfahren | Method |
| `SIMKONF_AUFH_VERFAHREN_RAMPE` | Sollwertrampe nach Aufheizleistung | Setpoint ramp by preheat power |
| `SIMKONF_AUFH_VERFAHREN_VORGABE` | Vorheizzeit vorgeben | Specify preheat time |
| `SIMKONF_AUFH_VERFAHREN_BERECHNET` | Vorheizzeit berechnen | Calculate preheat time |
| `SIMKONF_AUFH_LBL_VORHEIZZEIT` | Vorheizzeit (h) | Preheat time (h) |
| `SIMKONF_AUFH_LBL_TOLERANZ` | Toleranz zum Nutzungsbeginn (%) | Tolerance at start of occupancy (%) |
| `SIMKONF_AUFH_LBL_RESERVE` | Leistungsreserve (%) | Power reserve (%) |
| `SIMKONF_AUFH_LBL_BEMESSUNG` | Bemessungspunkt | Design point |
| `SIMKONF_AUFH_BEMESSUNG_ABZUG` | kälteste Stunde, zusätzlich kälter um | coldest hour, additionally colder by |

**Herleitungstexte:**

- **Aus** — DE: „Der Heizsollwert springt zu der Uhrzeit, die der Heizkalender vorgibt. Die erste Nutzungsstunde trägt
  dann das Aufwärmen der Bauteile; ihre Heizlast kann ein Mehrfaches der Last danach betragen.“ — EN: “The heating
  setpoint changes at the time set by the heating calendar. The first hour of occupancy then also warms up the building
  mass; its heating load can be several times the load afterwards.”
- **Vorheizzeit vorgeben** — DE: „Die Heizung beginnt die eingestellte Zeit vor dem Nutzungsbeginn mit dem Sollwert der
  Nutzung, höchstens mit der verfügbaren Heizleistung. Ab Nutzungsbeginn ist die Heizleistung auf die Heizlast dieser
  Stunde zuzüglich der Toleranz begrenzt. Der Lauf prüft jeden Tag, ob die Raumtemperatur zum Nutzungsbeginn den Sollwert
  erreicht, und nennt die Tage, an denen die Vorheizzeit nicht reicht, mit der nötigen Zeit. Ein Gebäude kann eine eigene
  Vorheizzeit haben.“ — EN: “Heating starts the set time before occupancy with the occupancy setpoint, at most with the
  available heating power. From the start of occupancy, heating power is limited to that hour's heating load plus the
  tolerance. The run checks every day whether the room reaches the setpoint at the start of occupancy and lists the days
  on which the preheat time is too short, with the time required. A building can have its own preheat time.”
- **Vorheizzeit berechnen** — DE: „Der Lauf berechnet die Vorheizzeit für jeden Tag: so kurz wie möglich, aber so lang,
  dass die Raumtemperatur zum Nutzungsbeginn den Sollwert erreicht und die Heizleistung dann die Heizlast dieser Stunde
  zuzüglich der Toleranz nicht übersteigt. Vorgeheizt wird höchstens mit der verfügbaren Heizleistung und nie länger als
  die Absenkung; reicht das nicht, nennt der Lauf die Tage.“ — EN: “The run calculates the preheat time for every day: as
  short as possible, but long enough for the room to reach the setpoint at the start of occupancy without the heating
  power then exceeding that hour's heating load plus the tolerance. Preheating uses at most the available heating power
  and never lasts longer than the setback; where this is not enough, the run lists the days.”
- **Verfügbare Heizleistung** — DE: „Verfügbar ist die Heizleistungsgrenze des Gebäudes oder der Zone, ohne Grenze die
  Heizlast an der kältesten Stunde zuzüglich der Leistungsreserve.“ — EN: “Available is the heating power limit of the
  building or zone; without a limit, the heating load at the coldest hour plus the power reserve.”
- **Sollwertrampe (Bestand, neu gefasst)** — DE: „Vor jedem Nutzungsbeginn hebt der Lauf den Sollwert stundenweise an, so
  dass keine Stunde mehr als die verfügbare Heizleistung braucht. Die Raumtemperatur erreicht den Sollwert immer; an
  milden Tagen bleibt ein Sprung der Heizlast, solange er unter der verfügbaren Heizleistung liegt.“ — EN: “Before each
  start of occupancy the run raises the setpoint hour by hour so that no hour needs more than the available heating
  power. The room always reaches the setpoint; on mild days a step in heating load remains as long as it stays below the
  available heating power.”
- **Geltung** (einmal, unter der Gruppe) — DE: „Gilt für alle Gebäude nach VDI 6007 im Projekt; jedes Feld wird sofort
  gespeichert.“ — EN: “Applies to all VDI 6007 buildings in the project; every field is saved immediately.”

Formelzeichen (ρ, ΔT_K, n′) gehören in Hilfe und Herleitungszeile, nicht in Beschriftungen. Die Herleitungszeile
unterscheidet „Vorheizzeit am Bemessungspunkt“ (t_V,max) und „längste Vorheizzeit im Lauf“.

---

## 3 Gegenüberstellung

| | Bestand (Sollwertrampe) | Option 1 (t_V vorgegeben) | Option 2 (t_V berechnet) |
|---|---|---|---|
| **Eingaben** | Schalter, Bemessung (a)/(b) mit ΔT_K, Reserve ρ, Art täglich/fest, Aufschlag h/%, manuelle Zeit je Gebäude | Schalter, t_V (Projekt, Gebäude), Toleranz x, Reserve ρ bzw. `Heizleistung_Max` | Schalter, Toleranz x, Reserve ρ bzw. `Heizleistung_Max`, Bemessung (a)/(b) |
| **Rechenweg** | Vorab-Stufenformel, Sollwerttreppe n Stufen; Lauf ideal ohne Grenze | Sollwert θ_T ab h_s − t_V, Deckelreihe P_V / P_K im Lauf; Ankunftsprüfung | Vorausrechnung je Sprung (Absenkung → Plateau → Kalenderstunde, Bisektion), dann wie Option 1 |
| **Ergebnis an der Kalenderstunde** | Last ≤ P_auf (feste Zahl), Sprung bleibt: 1051 Median 23,8 kW gegen 4,2 kW danach | Last ≤ (1 + x)·Φ_ref per Bau; Raumluft ggf. unter Sollwert, gezählt | Last ≤ (1 + x)·Φ_ref; Ankunft bis auf „unerreichbar“ gesichert |
| **Prüfung „Sollwert erreicht“** | keine (per Bau erfüllt, Grenze fehlt) | ja, je Tag, mit nötiger Zeit | ja, als Nachweis der Vorausrechnung |
| **Auslegungswirkung** | Φ_HL + (P_auf − Φ_stat); Jahresspitze in der Rampe (1051: 30,00 kW) | Spitze im Vorheizen ≤ P_verf; Nutzungszeit ≤ (1 + x)·Φ_ref | wie Option 1, dazu t_V,max am Bemessungspunkt |
| **Wärme** | Mehrwärme der Rampe klein (täglich) | fest vorgegeben: an milden Tagen mehr Wärme als nötig | so wenig wie die Bedingung erlaubt; werktags kürzer als Gleichgewichtsform |
| **Aufwand** | gebaut | 8,5–12 PT (alle Wellen außer V3) | 10,5–15 PT (mit V3) |
| **Risiken** | Text verspricht mehr als die Rechnung; AK1-Einzone ausgenommen | zu kurzes t_V → Komfortunterschreitung (gezählt); Deckelreihe ändert jeden Lauf mit Schalter | strenger Tagesbezug und kleines x heben die Absenkung auf (1051 grob: x = 20 % → Absenkung an den meisten Tagen voll vorgeheizt); Vorausrechnung ohne Gewinne zur sicheren Seite |

---

## 4 Folgen

**Schema** (ein Schritt, Nummer bei Anmeldung gegen origin; Muster `AufheizvorgabeSchema`, alles per `ADD COLUMN`,
nullbar, mit Prüfklausel):

- `Tab_Einstellungen`: `Aufheiz_Verfahren` TEXT CHECK IN ('RAMPE','VORGABE','BERECHNET') — NULL = RAMPE, der Bestand
  bitgleich; `Aufheiz_Vorheizzeit_H` INTEGER CHECK BETWEEN 1 AND 47; `Aufheiz_Toleranz` REAL CHECK (≥ 0 AND ≤ 1) — NULL =
  Vorgabe nach F3; nur nach F2 (b) zusätzlich `Aufheiz_Bezug` TEXT; nur nach F7 eine Spalte für ε.
- `Tab_Gebaeude.Aufheizzeit_Manuell_H` bleibt und trägt in „Vorgabe“ die Vorheizzeit des Gebäudes (gleiche Spanne
  1–47 h); keine neue Gebäudespalte, kein Katalogfeld.
- Ergebnis (`Tab_ErgebnisGebaeude`, `Tab_ErgebnisZone`): `Aufheiz_Verfahren`, `Vorheiz_Tage_Verfehlt`,
  `Vorheiz_Unterschreitung_Max_K`, `Vorheiz_Unterschreitung_H`, `Vorheiz_Sprungmass_Max`, `Vorheizzeit_Bedarf_Max_H` —
  als neue Spalten statt einer Erweiterung des CHECK von `Aufheiz_Art` (die einen Neubau verlangte).

**Kern:** Deckelreihe im Eingang (`GebaeudeModellEingang`, `ZonenEingang`, Stepper) statt Skalar, bitgleich ohne Reihe;
`Vorheizplan` neben `Aufheizplan` (Option 1: Fenster, Sollwert, Deckel; Option 2: Vorausrechnung); Laufprüfung im
Ergebnis (`GebaeudeModellErgebnis`, `Komfortkennzahlen`); Laufhinweise `SIMENG_AUFH_*` neu; Auskunft
(`Aufheizauskunft`, `AufheizauskunftCtrl`) und Herleitungszeile; `KonfigurationCtrl.AufheizvorgabeLesen/Schreiben`.
Der Pufferauslegung (`HeizzoneRechner`) liefert der Plan weiter die Sollwertreihe; die Deckelreihe ist für sie ohne Belang.

**Oberfläche:** Gruppe in `SimulationKonfigSeite.razor` mit Verfahrensauswahl und je Verfahren sichtbaren Feldern
(Rampe: wie heute; Vorgabe: Vorheizzeit, Toleranz, Reserve; Berechnet: Toleranz, Reserve, Bemessungspunkt); Feld
„Aufheizzeit manuell“ im Reiter „Konditionierung“ des Gebäudedialogs als „Vorheizzeit (h)“ je nach Verfahren; Hülle in
`EPOS.UI.Daten`; KI-Felder (`KiDialoge`); Texte beider Sprachen, `ResourceDesigner` ziehen; bunit-Tests.

**Bericht und Kennzahlen:** `Aufheizbericht`, `KennzahlenKatalog`, `Vorlagenfeldkatalog.Standwerte`, Kurzbericht-Absatz
(Katalogfassung), Abweichungsmerkmale und Variantenvergleich (`AbweichungsErmittler`, `BausteineVergleich`); neue
Kennzahlen: Tage „Vorheizzeit reicht nicht“, größte Unterschreitung, Spitze der Nutzungszeit, Vorheizzeit am
Bemessungspunkt und längste im Lauf. Export `Geb[n].Vorheiz*` nur bei Verfahren ≠ RAMPE — so bleibt jede Basisdatei der
Bestandsprojekte byte-gleich.

**Referenzprojekt und Basis:** 1051 bleibt auf dem Verfahren „Rampe“ und hält den Bestand. Neu ein Projekt als Kopie von
1051 mit „Vorheizzeit berechnen“, Toleranz 20 % (Nummer bei Anlage), dazu eine Testzeile für „Vorgabe“ in den
Kerntests; Wache nach dem Muster `KonditionierungReferenzprojektWacheTests`, neue Basis, Aufnahme in die CI-Auswahl.
Die Einfrierregel „gesäte Konditionierungsdaten“ (`Referenzlaeufe/LIESMICH.md`, Abschnitt der Regel) wird um die neuen
Spalten `Aufheiz_Verfahren`, `Aufheiz_Vorheizzeit_H`, `Aufheiz_Toleranz` und das neue Projekt erweitert; wer 1051
umstellt statt zu kopieren, friert ebenfalls neu ein.

**Wiki-Quellen** (`Projekte/Wiki/`): „Programm Dokumentation - Gebäudemodell VDI 6007“ (Ziel des Hilfeknopfs),
„Simulation konfigurieren und starten“, „Programm Dokumentation - Simulationsergebnisse“, „Grundlagen -
Wärmebedarfsrechnung“, „Programm Dokumentation - Mehrzonenmodell“, „Programm Dokumentation - Berichtsvorlagen“; ein
Logbuch-Satz beim gebündelten Upload, Versionsnummer beim Anwender erfragen.

---

## 5 Fragen an den Anwender

1. **F1 — Ergänzen oder ersetzen?** (a) Die neuen Verfahren ergänzen die Sollwertrampe als Auswahl „Verfahren“;
   (b) sie ersetzen sie, Bestandsprojekte rechnen anders. **Empfehlung (a):** Bestand bitgleich, 1051 bleibt Wache;
   „Art fest“ und „Aufschlag“ werden im neuen Verfahren nicht angeboten (Toleranz und Prüfung übernehmen ihre Rolle).
   Ob die Rampe später entfällt, entscheidet sich nach dem neuen Referenzprojekt.
2. **F2 — Bezug der Kalenderstunde:** (a) stationäre Last der Kalenderstunde Φ_stat(θ_T, T_a) des Tages (wörtlich);
   (b) Auslegungsheizlast Φ_HL als feste Zahl. **Empfehlung (a)**, zusammen mit F4 (a) — sonst hebt das Vorheizen an
   milden Tagen die Absenkung auf (Tabelle 2.2).
3. **F3 — Toleranz x:** leer = (a) 0 %, (b) 20 %, (c) Pflichtfeld. **Empfehlung (b)** wie die Reserve: leer rechnet mit
   20 % und meldet es einmal; Spanne 0–100 %; 0 % erlaubt, mit Hinweis, dass die Ankunft dann nur asymptotisch gelingt.
4. **F4 — Leistung im Vorheizfenster:** (a) verfügbare Leistung P_verf als Plateau; (b) nur der Kalenderdeckel
   (1 + x)·Φ_ref; (c) lineare Leistungsrampe bis P_verf. **Empfehlung (a)** — kürzeste Vorheizzeit bei gegebener
   Leistung, und „verfügbare zusätzliche Heizleistung“ ist der Wortlaut von Option 2.
5. **F5 — Option 1, Option 2 oder beide?** **Empfehlung beide** auf einem Kern: Option 2 als Vorgabe für neue
   Projekte, Option 1 für Planer mit fester Vorheizzeit (Gebäudeleittechnik) und als Gegenprobe; Option 1 liefert in der
   Meldung den Bedarf aus Option 2.
6. **F6 — Geltung von t_V:** (a) je Projekt mit Übersteuerung je Gebäude, Zonen erben; (b) zusätzlich je Zone.
   **Empfehlung (a)** über die vorhandene Spalte `Aufheizzeit_Manuell_H`; Zonen haben eigene Kalender, aber eine
   gemeinsame Anlage.
7. **F7 — Ankunftskriterium:** Raumluft im Stundenmittel der Kalenderstunde ≥ θ_T − ε mit (a) ε = 0,5 K fest,
   (b) ε = 1 K wie das Kriterium von KP3b, (c) ε als Eingabe. **Empfehlung (a)**, ohne Eingabefeld.
8. **F8 — Dauer des Kalenderdeckels:** (a) bis zum Ende des Sollwertblocks; (b) nur die Kalenderstunde; (c) kein Deckel,
   nur Meldung des Sprungs. **Empfehlung (a)** — mit (b) springt die Last eine Stunde später.
9. **F9 — Referenzprojekt:** (a) Kopie von 1051 mit „berechnen“, 1051 bleibt „Rampe“; (b) 1051 umstellen.
   **Empfehlung (a)**; beide Wege brauchen eine neue Basis, (a) behält die Wache des Bestands.
10. **F10 — AK1-Einzonengebäude:** (a) in den neuen Verfahren einbeziehen (W5 entfällt dort); (b) weiter ausnehmen.
    **Empfehlung (a)** — die Prüfung im Lauf braucht keine geschlossene Vorhersage; Option 2 gibt dort einen
    Vorschlag aus dem idealen Modell.

---

## 6 Wellenplan

| Welle | Inhalt | Abnahme | PT |
|---|---|---|---|
| V0 | Entscheide nachziehen: Teilkonzept Konditionierungsprofile 4 (neuer Abschnitt Vorheizen), Glossar, Register, Statuszeile | Linkwache | 0,5 |
| V1 | Kern: Deckelreihe je Stunde in Eingang, Zonen, Kopplungsweg (AK1/AK2) und Stepper (AK3); ohne Reihe bitgleich | Referenzlauf byte-gleich, Kerntests | 1,5–2 |
| V2 | Kern: Vorheizplan Option 1 (Fenster, Sollwert, Deckel, Kühlkappe, W4), Laufprüfung (Ankunft, Unterschreitung, Sprungmaß), Hinweise, Mehrzonen-Gebäudewerte | neue Tests, Bestand byte-gleich | 1,5–2 |
| V3 | Kern: Option 2 — Vorausrechnung je Sprung (Absenkung, Plateau, Bisektion), Bemessungsfall t_V,max, Nachbarform; Probe gegen die erste Ordnung und gegen den Lauf; Messung τ₂ und t_V an der Kopie von 1051 | Prüforakel, Messprotokoll | 2–3 |
| V4 | Schema und Datenweg: ein Schritt (Einstellungen, Ergebnis Gebäude/Zone), Controller, Testdatenbank anheben, `SqlDialektPruefer` | Schema- und Controllertests | 1–1,5 |
| V5 | Oberfläche: Gruppe mit Verfahren, Texte beider Sprachen nach 2.6, Gebäudefeld, Hülle, KI-Felder, bunit | UI-Tests, Windows-Schale kompiliert | 1,5–2 |
| V6 | Bericht, Kennzahlen, Export, Kurzbericht (Katalogfassung), Variantenvergleich | Berichtstests, `Berichtsvorlage` gezogen | 1–1,5 |
| V7 | Referenzprojekt (Kopie von 1051), Wache, Einfrierregel, neue Basis, CI-Auswahl | Referenzlauf gegen neue Basis | 1–1,5 |
| V8 | Wiki-Quellen und Logbuch-Entwurf | Gegenlese-Muster, Produktdatenwache | 0,5–1 |
| | **Summe** (beide Optionen) | | **10,5–15** |

Nur Option 1 (ohne V3, Bedarf in der Meldung dann aus der ersten Ordnung): 8,5–12 PT. Reihenfolge: V0 → V1 → V2 →
V4 → V3 → V5/V6 parallel → V7 → V8; V1 und V2 ändern ohne Verfahrenswahl keine Zahl.
