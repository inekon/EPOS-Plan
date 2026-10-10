# Entwurf AH — Vorheizen vor dem Kalendersprung: bestehender Rechenweg gegen neues Konzept

**Fassung 2, Stand 10.10.2026 · Entwurf zur Entscheidung, nichts gebaut.** Fassung 1 (gelesen auf `8100e397`, Basis R50
`2026-10-10_R50_Wochentagsraster`) hat der Anwender am 10.10.2026 mit der Klarstellung 1 und den Entscheiden F1–F10
beantwortet (Abschnitt 5.1, Wortlaut); F8 wurde am selben Tag durch die Wahl (a) ersetzt. Fassung 2 arbeitet die Entscheide
ein; die Messungen in Abschnitt 2.8 stammen aus der Basis R51 `2026-10-10_R51_FreieKuehlung`, deren Reihen für 1051 mit R50
übereinstimmen (Heizwärme 24,22 MWh, Spitze 30,00 kW, P_auf 31,62 kW). Grundlage bleiben
[Teilkonzept Konditionierungsprofile](../Konzept_Konditionierungsprofile_EPOS-Plan.md) Kapitel 4 (Aufheizoptimierung,
Stufe KP3), [Entwurf KP3](../../ueberholt/2026-10-02_Entwurf_KP3.md), [Konzept Heizlastspitzen](2026-10-03_Konzept_Heizlastspitzen_Glaettung.md)
(E60), die Entscheide E58–E60, E97 und E99 der [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md), das
[Protokoll KP3](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-02_KP3_Aufheizoptimierung.md), das
[A/B-Protokoll 1051](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-03_KP3_RP1_AB-Protokoll_1051.md) und die
Reihen des Referenzprojekts 1051.

**Auftrag (Anwender, Wortlaut):** „Aufheizleistung: Text und Funktion überarbeiten: Option 1: Aufheizleistungsrampe mit
Vorgabe Startzeit vor Kalender-Rampe (t-Vorheizen in Stunden), so dass die Heizleistung zum Zeitpunkt der Kalender-Rampe
nicht überschritten wird (bis auf x % — optionale Vorgabe). Es soll in einem Berechnungslauf geprüft werden, ob die
Zeitvorgabe t-Vorheizen ausreichend ist, um die Solltemperatur zu erreichen. 2. Die Zeitvorgabe soll berechnet werden aus
der verfügbaren zusätzlichen Heizleistung und der Vermeidung des Heizlastsprunges an der Kalender-Rampe (Heizlast durch
Kalenderrampe nicht erhöht). 4. Prüfe, wie es gegenwärtig berechnet wird, stelle das bestehende vs. neues Konzept dar.“

**Klarstellung 1 (Anwender, 10.10.2026, Wortlaut):** „Vorheizfenster: Ab t_V Stunden vor dem Sprung steht der Sollwert
schon auf dem Zielwert. Geheizt wird mit einer Leistung, die keinen zusätzlichen Sprung am Zeitpunkt der Temperaturänderung
(Kalender) erzeugt (Beispiel Nachtabsenkung von 20 °C auf 17 °C). Kalenderdeckel: die Zeit ist jeweils der Sprung der
Soll-Temperatur im Kalender, keine fixe Zeit.“

**Begriffe** (genau gefasst in 2.1). *Kalendersprung* h_s: die Stunde, in der der Heizkalender den Sollwert von θ_N auf θ_T
anhebt (1051: werktags 07:00, 16 → 20 °C). *Block*: die Stunden ab h_s bis zum nächsten Sprung nach unten. *Vorheizfenster*:
die t_V Stunden vor h_s, in denen der Sollwert schon auf θ_T steht. *Φ_ref(h)*: die Heizlast, die eine Stunde ohne
Aufheizanteil hat. *Φ_K,max*: ihr Jahresmaximum an den Kalendersprüngen. *Deckel P_K*: die stündliche Leistungsgrenze ab h_s
bis Blockende, aus Φ_K,max und der Toleranz. *Toleranz* x bzw. Δ: in Prozent von Φ_K,max oder in kW. *P_verf*: die
verfügbare Heizleistung. *Sprung S(h_s)*: die Differenz der Stundenmittel der Heizlast von h_s und der Stunde davor.
*Vorlauf*: der Lauf des Gebäudes ohne Vorheizen, der Φ_K,max und den heutigen Sprung liefert. *ε*: Regelgenauigkeit, das
Ankunftskriterium.

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

### 1.3 Die Spitze an der Kalenderstunde in 1051 (Basis R50, in R51 unverändert)

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

### 2.1 Begriffe, exakt

Alle Größen sind Stundenmittel in kW, soweit nichts anderes steht; „Augenblick“ heißt der Wert am Beginn einer Stunde,
wie ihn E58 F1 (b) für die Quelle Grenze nimmt.

| Begriff | Definition | Herkunft im Rechenweg |
|---|---|---|
| **Kalendersprung** h_s, Block, Absenkdauer D | wie heute: s(h_s) − s(h_s − 1) > 0,01 K (`Aufheizoptimierung.Spruenge`); der Block reicht von h_s bis zur Stunde vor dem nächsten Sprung nach unten; D sind die zusammenhängenden Stunden unter θ_T vor h_s. Zwei Anstiege (16 → 18 → 20 °C) sind zwei Sprünge (E58 F2) | Sprungliste des Plans |
| **Vorheizfenster** [h_s − t_V, h_s − 1] | Sollwert dort max(s, min(θ_T, θ_K − 1 K)) (Klarstellung 1; Kühlkappe wie heute); Länge min(t_V, D, 47) | Plan |
| **Heizlast ohne Aufheizanteil** Φ_ref(h) | die stationäre Heizlast der Stunde bei warmen Bauteilen: `Zonenmodell2K.StationaereHeizlastW`(θ_T, T_a(h), Erdreich des Tags, Zusatzleitwert der Stunde, ohne Sonne und Gewinne) — dieselbe Funktion wie `PhiStat` der heutigen Rampe. Das ist die Last, **auf die** der Kalendersprung führt, wenn nichts aufzuheizen ist | Vorlauf |
| **Jahresmaximum** Φ_K,max (F2) | max über alle Kalendersprünge des Jahres von Φ_ref(h_s) — eine Zahl je Zone und Jahr; mit Geltung Gebäude die Summe der Zonen in derselben Stunde, maximiert über die Sprungstunden. **Lesarten:** (i) ohne Aufheizanteil (Empfehlung, wie definiert); (ii) die Last der Kalenderstunde im Vorlauf **mit** Aufheizanteil — 1051: ≈ 42,8 kW, damit (1 + x)·Φ_K,max ≈ 51 kW, wirkungslos; (iii) das Jahresmaximum der **Differenz** S_0(h_s) als Steigungsgrenze je Stunde — ein anderer Mechanismus (Frage F11) | Vorlauf |
| **Toleranz** x bzw. Δ (F3) | x in Prozent von Φ_K,max (Vorgabe 20 %, konfigurierbar) oder Δ in kW; leer = Vorgabe | Projekt |
| **Deckel** P_K (F8 (a)) | P_K = (1 + x)·Φ_K,max bzw. Φ_K,max + Δ — eine Zahl je Zone (bzw. Gebäude) und Jahr, **stündlich wirksam im Lauf** als `heizleistungMaxW` jeder Stunde von h_s bis Blockende; wirkt nie unter Φ_ref(h) der Stunde (Festlegung, Abschnitt 2.2), wird je Stunde mit `Heizleistung_Max` geschnitten | Plan → `Stundenrand` |
| **Verfügbare Leistung** P_verf (F4) | wie das heutige P_auf: `Heizleistung_Max` der Zone, sonst (1 + ρ)·Φ_stat(θ_T,max, T_a,min) mit der Reserve ρ (Vorgabe 20 %); 1051: 31,62 kW | Bemessung wie heute |
| **Vorheizleistung** P_V (F4) | die Leistungsgrenze im Vorheizfenster: min(P_K, P_verf). Die ideale Regelung nimmt darunter, was sie braucht: Sie fährt die Grenze, bis die Luft θ_T erreicht, und klingt dann ab („Leistungsplateau“) | Plan → `Stundenrand` |
| **Sprung** S(h_s) | S(h_s) = Φ̄(h_s) − Φ̄(h_s − 1), Differenz der Stundenmittel der Heizlast der Kalenderstunde und der Stunde davor; negativ = kein Sprung. Im **Vorlauf** ohne Vorheizen ist S_0(h_s) der heutige Sprung, S_max,0 sein Jahresmaximum; im **Lauf** mit Vorheizen S(h_s) und S_max. Alternative Lesart: Mehrlast gegen die stationäre Last, S_B = Φ̄(h_s) − Φ_ref(h_s) — als Kennzahl mitgeführt, nicht Kriterium | Vorlauf, Lauf |
| **Ankunft** (F7) | die Luft am Beginn von h_s (Augenblick, Ende des Fensters) liegt bei θ_T − ε oder darüber; ε = Regelgenauigkeit (0,5 K, 1 K, 2 K oder Eingabe). Dazu als Kennzahl das Stundenmittel von h_s und die Unterschreitungsstunden im Block (θ̄_air < θ_T − ε) | Lauf |
| **Bedarf** t_nötig(h_s) | die kleinste Vorheizzeit t ≤ min(D, 47), mit der die Zone unter P_V ankommt — aus der Vorausschau im Vorlauf (2.4); t_V der Option 2 ist ihr Jahresmaximum (F5) | Vorlauf |
| **Unerreichbar** | kein t ≤ min(D, 47) kommt an: die Absenkung dieser Nacht entfällt, der Tag zählt | Vorlauf, Lauf |

### 2.2 Grundgedanke: Fenster, Deckel, Jahresbetrachtung

Die Klarstellung 1 und F8 (a) legen zwei Wirkorte fest:

1. **Vor h_s das Vorheizfenster.** Der Sollwert steht ab h_s − t_V auf θ_T; die Leistung ist auf P_V = min(P_K, P_verf)
   begrenzt. Die Zone erreicht θ_T so früh, wie die Grenze es erlaubt; danach fällt die Last ab. Der Sprung der Last fällt
   damit in die Nacht, auf den Fensterbeginn, und ist durch P_V begrenzt.
2. **Ab h_s der Deckel.** Von h_s bis Blockende ist die Leistung jeder Stunde auf P_K begrenzt. Sind die Bauteile warm, liegt
   die Last darunter, und der Deckel greift nicht; sind sie es nicht, bleibt die Luft unter θ_T, und der Lauf zählt es.
   **Der Deckel macht die Bedingung „kein Sprung über den Deckel“ im Lauf wahr; geprüft wird die Ankunft.**

**Die Jahresbetrachtung liefert die Bemessung, der Deckel wirkt stündlich** (F2, F5, F8 (a)): Φ_K,max ist das Jahresmaximum
der Heizlast am Kalenderpunkt — eine Zahl, die der Vorlauf vor dem eigentlichen Lauf bestimmt. Daraus entstehen P_K und,
in Option 2, aus dem schwersten Sprung des Jahres die Vorheizzeit t_V. Im Lauf gilt an jeder Stunde die Grenze; an keiner
Stunde wird eine Tageslast neu berechnet (das war die Schwäche des stündlichen Kalenderdeckels der Fassung 1: an milden
Tagen führte die Tageslast + 20 % auf Vorheizzeiten über die ganze Nacht, Tabelle 2.2 der Fassung 1).

**Warum der Deckel nicht unter Φ_ref(h) wirkt (Festlegung, Widerspruch möglich).** P_K ist am Jahresmaximum der
Kalendersprünge bemessen. Eine Blockstunde kann kälter sein als jede Kalenderstunde (ein kalter Nachmittag, eine Nacht mit
Nutzung); dort läge der Deckel unter der stationären Last, und die Zone könnte θ_T nicht einmal halten — der Deckel hieße
Komfortverlust statt Spitzenglättung. Die Grenze einer Blockstunde ist deshalb max(P_K, Φ_ref(h)); die Stunden, in denen
der zweite Zweig greift, werden gezählt („Deckel unter der Heizlast der Stunde“) und gemeldet (Frage F12).

**Was nicht begrenzt wird.** Außerhalb von Fenster und Block bleibt die Leistung wie heute (`Heizleistung_Max` oder
unbegrenzt). Ein Übergang aus „aus“ (Heizperiode, Kalender „aus“) bekommt wie heute kein Fenster (W4) und zählt nicht in
die Ankunftsprüfung.

**Verfahren wählbar (F1).** „Sollwertrampe“ ist der Bestand, bitgleich; „Vorheizzeit vorgeben“ ist Option 1, „Vorheizzeit
berechnen“ Option 2. Beide neuen Verfahren laufen auf demselben Kern (Vorlauf, Plan, Deckelreihe, Nachweis); Option 1 gibt
t_V vor, Option 2 berechnet es.

### 2.3 Physik im 2K-Modell nach VDI 6007

Zustand der Zone sind die zwei Massentemperaturen (Außen- und Innenbauteile); Luft und Oberflächen sind kapazitätslos.
Bei geregelter Heizung ist die Leistung affin im Zustand und im Sollwert; bei gedeckelter Heizung (Betriebsfall
`Heizgrenze`, `Zonenmodell2K.cs:460`, `:1113`) ist die Leistung fest und die Luft frei. Beide Fälle sind linear mit festen
Randwerten je Abschnitt und laufen über dieselben Matrixfunktionen (`Uebergangsrechner.Bei`); der Löser wechselt den Fall
innerhalb der Stunde, sobald die Grenze greift. **Der stündliche Deckel ist im Löser vorhanden:** `Stundenrand` trägt
`heizleistungMaxW` je Stunde (`Stundenrand.cs:48`, `:72`), heute gefüllt mit einem Skalar (`GebaeudeModellEingang.cs:284`,
`:697`). Fenster und Deckel brauchen keine neue Physik, nur eine Reihe statt eines Skalars.

**Verlauf eines Vorheizens** bei festen Randwerten, Anfangszustand x₀ nach der Absenkung:

1. *Plateau* (0 ≤ t < t₁): Φ = P_V, Luft steigt frei; t₁ ist die Wurzel von θ_air(t) = θ_T, eindeutig, weil θ_air bei
   P_V > Φ_stat(θ_T) monoton steigt (Summe zweier Exponentialfunktionen).
2. *Abklingen* (t ≥ t₁): geregelt bei θ_T; die Mehrlast über Φ_stat fällt mit den Zeitkonstanten τ₁, τ₂ des geregelten
   Falls (`Aufheizantwort`, Teilkonzept 4.2): Φ(t) − Φ_stat ≈ Σ_k r_k·e^(−(t − t₁)/τ_k).
3. *Kalenderstunde*: Ist die Luft am Beginn von h_s um δθ unter θ_T, hebt die Regelung sie unter dem Deckel an. Im
   2K-Modell gilt dafür **exakt** S_Augenblick(h_s) = G_0·δθ mit dem Sprungleitwert G_0 der `Aufheizantwort` — Luft und
   Oberflächen springen mit, die Massen nicht. Der Sprung am Kalenderpunkt und die Unterschreitung sind zwei Lesarten
   desselben Fehlbetrags; ein Deckelspielraum Δ entspricht einem Luftdefizit Δ/G_0 (Stundenmittel: Δ/ḡ mit dem
   Stundenmittel ḡ der Sprungantwort). An 1051 ist ḡ ≈ 3,5 kW/K (2.8): Δ = 4,7 kW (20 % von Φ_K,max) ≙ rund 1,3 K —
   dieselbe Größenordnung wie die Regelgenauigkeit 1 K (F7). Die beiden Kriterien passen zusammen.

**Erste Ordnung** (eine Masse C_w, Leitwert H_s; Teilkonzept 4.2) als Schätzer, Probe und Vorschlagswert:

```
ΔT_m(t)  ≈ ΔT·(1 − e^(−(D − t)/τ_frei))          Fehlbetrag der Massen zu Fensterbeginn (Absenkung D − t Stunden)
P_V,min  ≈ Φ_stat(θ_T, T̄_a) + C_w·ΔT_m/(t + τ_m)   Leistung, die in t Stunden gerade ankommt (τ_m = C_w/G_m)
t_nötig  = kleinstes t mit  t·(P_V − Φ_stat(θ_T, T̄_a)) ≥ C_w·ΔT_m(t)          Ankunft unter der Grenze P_V
```

T̄_a ist die mittlere Außenluft des Fensters (zur sicheren Seite die kälteste). **Die Bedingung ist monoton in t:** ein
früherer Beginn trifft wärmere Massen (ΔT_m kleiner) und hat mehr Stunden — t_nötig ist deshalb durch Bisektion findbar,
im Schätzer wie in der Vorausschau des 2K-Modells. Für t → D wird ΔT_m → 0: Ohne Absenkung ist jede Nacht „erreichbar“,
solange P_V ≥ Φ_stat — „unerreichbar“ heißt im neuen Verfahren, dass selbst der Verzicht auf die Absenkung nicht
ankommt, also P_V < Φ_stat(θ_T, T_a) in der Nacht.

**Was x → 0 bedeutet.** Mit x = 0 ist P_K = Φ_K,max: Jede Kalenderstunde kälter als die kälteste bisher kann nur noch mit dem
Floor Φ_ref(h) gehalten werden, und im Fenster steht nur die stationäre Last der kältesten Kalenderstunde zur Verfügung —
Vorheizen braucht dann die ganze Nacht. Die Toleranz ist physikalisch nötig, nicht nur Bequemlichkeit; 0 % bleibt erlaubt,
mit Hinweis.

### 2.4 Der Vorlauf — gemeinsam für beide Optionen

Der Vorlauf ist der Lauf des Gebäudes **ohne Vorheizen** (reiner Kalender, keine Treppe, kein Fenster, kein Deckel) auf dem
Rechenweg, den das Gebäude auch im Lauf nimmt (Einzone, Mehrzonen, Kopplungsweg AK1/AK2; im AK3-Weg der Profilweg mit
Rückstufe `Ak3Kernstufe`). Er ist deterministisch, ohne Datenbank und ohne Ergebnisschreiben, und liefert:

- je Kalendersprung Φ_ref(h_s) und S_0(h_s), daraus **Φ_K,max** und **S_max,0** (die Jahresbetrachtung, F2);
- den Deckel **P_K** und **P_V** = min(P_K, P_verf) (F3, F4, F8 (a));
- die Zustandsreihe x₀(h) der Massen — Ausgangspunkt der **Vorausschau**: Von x₀(h_s − t) aus rechnet dieselbe
  `Zonenmodell2K.Schritt`-Folge (Zustand sichern, t Stunden mit Sollwert θ_T und Grenze P_V, die Stunde h_s mit Grenze P_K,
  Zustand zurücksetzen über `Zuruecksetzen(θ_MAw, θ_MIw)`) mit den **echten Randwerten der Stunden** (Außenluft, Sonne,
  Gewinne, Lüftung, Erdreich, Nachbarn auf der Vorlaufbahn) und sagt, ob die Zone am Beginn von h_s innerhalb ε ankommt.
  Die Zustände des Vorlaufs sind kälter oder gleich denen des späteren Laufs (am Vortag wurde nicht vorgeheizt, der
  Nutzungstag selbst ist in beiden Läufen bei θ_T geregelt), also zur sicheren Seite;
- je Sprung den **Bedarf** t_nötig (Bisektion über t ∈ {1 … min(D, 47)}, höchstens sechs Vorausschauen zu je ≤ 48
  Stunden) und die Kennzeichnung „unerreichbar“.

Der Vorlauf ersetzt die Vorausrechnung mit festen Randwerten und Gleichgewichtsform der Fassung 1: Er trifft den werktags
(D = 13 h) nur teilweise abgekühlten Bau (2.8: Fehlbetrag der Massen 3,2 K statt 4 K), rechnet mit den Gewinnen und der
Sonne des Morgens und braucht keine Nachbarform — die Nachbarn liegen auf der Vorlaufbahn. **Die Wahrheit bleibt der Lauf**
(Grundsatz 7 des Teilkonzepts): Der Lauf prüft Ankunft und Sprung noch einmal; wo er vom Vorlauf abweicht (AK3-Kreis,
gleichzeitig vorheizende Nachbarn), nennt der Hinweis die Tage.

**Rechenzeit (Abschätzung, V1 misst).** Ein Zonenjahr sind 8 760 Schritte mit wenigen 2×2-Matrixoperationen, Größenordnung
10–100 ms. Vorlauf 1 Zonenjahr; Vorausschauen in Option 2 höchstens 148 · 6 · 48 ≈ 43 000 Schritte (≈ 5 Zonenjahre), typisch
die Hälfte; Option 1 nur an den Tagen, die den Lauf verfehlen; dazu der Lauf. Zusammen unter zehn Zonenjahren je Zone,
also im Bereich einer Sekunde; begrenzt durch t ≤ 47, sechs Bisektionsschritte, feste Reihenfolge, Vergleiche über
`Rechenrand`. Die Alternative — Bisektion über t_V mit vollständigen Jahresläufen — kostet im AK3-Weg den ganzen Kreis je
Kandidat und wird nicht empfohlen.

### 2.5 Option 1 — Vorheizzeit vorgegeben, Prüfung im Lauf

**Eingaben:** t_V (h) je Projekt, übersteuerbar je Gebäude (`Aufheizzeit_Manuell_H`, 1–47 h) und mit Geltung Zone je Zone;
Toleranz x oder Δ (leer = Vorgabe 20 %); ε (0,5/1/2 K oder Eingabe); Geltung Gebäude/Zone; Schalter „Gebäude mit
Heizkreis einbeziehen“.

**Vorlauf** nach 2.4 (Φ_K,max, P_K, P_V, S_max,0); Bedarf t_nötig nur für die Sprünge, deren Vorausschau mit t_V verfehlt.

**Plan** (im Eingangsbauer, an der Stelle der heutigen Rampe, `Vdi6007Rechenweg.cs:185–190`): je Sprung mit D ≥ 1 das
Fenster [h_s − min(t_V, D, 47), h_s − 1]; Sollwert dort max(s, min(θ_T, θ_K − 1 K)); Deckelreihe P_max(h) = P_V im Fenster,
max(P_K, Φ_ref(h)) im Block, sonst `Heizleistung_Max` bzw. unbegrenzt; jede Stunde min(Reihe, `Heizleistung_Max`).
Ist t_V ≥ D, beginnt das Fenster am Blockende des Vortags: **die Absenkung dieser Nacht entfällt**, der Tag zählt
(„Nacht ohne Absenkung“).

**Lauf:** derselbe Löser mit der Deckelreihe. Je Sprung wird festgehalten: δθ am Beginn von h_s und die Ankunft
(δθ ≤ ε); θ̄_air(h_s); die Unterschreitungsstunden im Block und die größte Unterschreitung in K; S(h_s) und S_B(h_s);
die Deckelstunden (Stunden mit Kappungsanteil > 0 im Block) und die Floor-Stunden; bei Verfehlen der Bedarf t_nötig.

**Ergebnis je Gebäude (und Zone):** Tage „Vorheizzeit reicht nicht“, größte Unterschreitung, Unterschreitungsstunden,
S_max,0 (Vorlauf) und S_max (Lauf), Φ_K,max, P_K, P_V, Deckelstunden, Nächte ohne Absenkung, Bedarf t_nötig,max, Mehrwärme
des Vorheizens (Heizwärme Lauf − Vorlauf). Hinweis einmal je Gebäude: „Gebäude …: An 12 Tagen erreicht die Raumluft zum
Nutzungsbeginn den Sollwert nicht (bis 1,4 K darunter); nötig wären bis 9 h Vorheizen statt 6 h.“

### 2.6 Option 2 — Vorheizzeit berechnet

**Eingaben:** Toleranz, ε, Geltung, Heizkreis-Schalter; die Art der Anwendung „fest“ oder „täglich“ (Frage F13); keine Zeit.

**Rechenweg:**

1. Vorlauf nach 2.4; je Sprung der Bedarf t_nötig(h_s) unter P_V, Kennzeichnung „unerreichbar“ (t_nötig = min(D, 47),
   Absenkung entfällt).
2. **t_V = max über alle Sprünge des Jahres von t_nötig** (F5: aus dem Jahresmaximum die Zeit, die nötig ist, den Deckel
   nicht zu überschreiten) — je Zone; mit Geltung Gebäude das Maximum über die Zonen. t_V ist der **Bemessungswert**:
   Herleitungszeile, Bericht, Vorschlag für eine Gebäudeleittechnik.
3. Plan und Lauf wie Option 1 — mit t_V an jedem Sprung („fest“) oder mit t_nötig(h_s) je Sprung, höchstens t_V
   („täglich“, Frage F13). „Fest“ entspricht dem Wortlaut von F5 und der heutigen Art „fest“; „täglich“ behält an milden
   Tagen die Absenkung und kostet weniger Wärme (2.8).
4. Nachweis im Lauf wie Option 1; verfehlt der Lauf einen Tag, den die Vorausschau als erreichbar sah, nennt der Hinweis
   ihn als Abweichung (Nachbarn, Kreis).

**t_V ≥ Absenkdauer.** Werktags ist D = 13 h. Liegt t_nötig eines Werktags bei D, entfällt die Absenkung dieser Nacht (der
Bau wird durchgeheizt); liegt das Jahresmaximum t_V bei einem Wochenendsprung über 13 h, entfällt mit „fest“ die
Absenkung **jeder** Werktagsnacht — das ist in 1051 der Fall (2.8). Der Lauf zählt die Nächte ohne Absenkung und weist die
Mehrwärme gegen den Vorlauf aus; der Hinweis nennt beides.

**Bemessung (a)/(b) und Aufschlag** des Bestands entfallen in den neuen Verfahren: Die Jahresbetrachtung ersetzt den
Bemessungsfall, die Toleranz und die Prüfung ersetzen den Aufschlag. P_verf wird weiter wie heute aus `Heizleistung_Max`
oder (1 + ρ)·Φ_stat(θ_T,max, T_a,min) gebildet; die Reserve ρ bleibt Eingabe.

### 2.7 Mehrzonen, Geltung, Kopplung, Kalender, Kühlung

- **Geltung Zone (F6):** Φ_K,max, P_K, P_V, t_V, Nachweis je Zone (eigener Kalender, eigene Sprungliste); Gebäudewerte als
  Vereinigung der Tage, Maximum der Unterschreitung und von t_V, Summe von P_K (Muster `Aufheizoptimierung.Gebaeudewerte`).
- **Geltung Gebäude (F6):** Φ_K,max,Geb = max über die Sprungstunden aller Zonen von Σ_i Φ_ref,i(h); P_K,Geb daraus; die
  Zonen erhalten Anteile P_K,i = P_K,Geb·Φ_HL,i/Σ Φ_HL (Auslegungsheizlast nach E97 als Schlüssel, eine Zone ohne Φ_HL
  bleibt ungedeckelt und wird gemeldet); P_V,i = min(P_K,i, P_verf,i); t_V,Geb = max_i t_V,i; der Nachweis misst den Sprung
  an der **Summe** Σ_i Φ̄_i (das, was die gemeinsame Anlage sieht) an jeder Sprungstunde einer Zone, die Ankunft je Zone.
- **Vorausschau mit Nachbarn:** die Nachbarzonen liegen auf der Vorlaufbahn (nicht vorgeheizt) — zur sicheren Seite, weil
  gleichzeitig vorheizende Nachbarn die Last senken; der Lauf misst die Wahrheit.
- **AK1 (Heizkreis, Einzone; F10):** einbezogen, am Projekt abschaltbar. Vorlauf und Vorausschau laufen auf dem
  Kopplungsweg selbst (`SchrittUebergabe`, `Zonenmodell2K.cs:1168`): Übergabe, Heizkurve und P-Regler sind in der
  Vorausschau enthalten; P_V und P_K wirken als Begrenzungsgrund `HeizleistungMax` neben Übergabe und Vorlaufgrenze
  (`:956`, `:577`). Die Regelgenauigkeit ε deckt den Regelabstand des P-Reglers (`Regler_Proportionalband`); W5 entfällt
  für die neuen Verfahren.
- **AK2 (Verfügbarkeit):** Deckelreihe und Verfügbarkeitsschranke gelten zusammen (Minimum, `Stundenrand.cs:159`).
- **AK3 (geschlossener Kreis):** Vorlauf und Vorausschau im Profilweg (Rückstufe `Ak3Kernstufe`), der Plan — Sollwertreihe
  und Deckelreihe — reist über `AufheizplanSetzen` in den Stepper (`Vdi6007Rechenweg.cs:207`); ob der Erzeuger P_V liefert,
  zeigt der Kreis; der Nachweis misst am Kreisergebnis.
- **Wochenende und Ferien:** D = 61 h nach dem Wochenende, in 1051 bis 109 h nach Ferien; das Fenster ist auf 47 h
  begrenzt; die Vorlaufzustände liegen dort nahe dem Gleichgewicht bei θ_N.
- **Konditionierungskalender:** Sprünge kommen aus den Kalendern (KP1/KP2); ein zweiter Anstieg im Block (18 → 20 °C)
  ist ein eigener Sprung mit eigenem Fenster innerhalb des Blocks; derselbe P_K gilt, ein Konflikt entsteht nicht.
- **Heizperiode und „aus“:** W4 bleibt; Übergänge aus „aus“ zählen in einem eigenen Zähler.
- **Kühlung:** Vorheizsollwert ≤ θ_K − 1 K; Deckel und Fenster betreffen nur die Heizseite; an Tagen mit Tagesbetriebsart
  Kühlen (AK3-K) gibt es keinen Heizsprung. Vorkühlen (Gegenrichtung) ist dieselbe Rechnung gespiegelt und bleibt
  außerhalb (KP3b/KU3).
- **Nachtlüftung:** Φ_ref nimmt den Zusatzleitwert der Stunde; die bedingte Nachtlüftung (1051: 2 1/h bis 7 Uhr nur bei
  Raumluft über der Schwelle) lüftet in der Heizperiode nicht und stört das Vorheizen nicht.
- **Auslegung (E60):** Mit Fenster und Deckel liegt die Jahresspitze einer Zone bei höchstens max(P_V, P_K) = P_K (sofern
  P_K ≤ P_verf), 1051: 28,2 statt 30,0 kW. Die Auslegungsgröße nach E60 (Φ_HL + Aufheizzuschlag) bleibt; neu stehen
  daneben Deckel, Spitze des Laufs und Spitze der Nutzungszeit. Der Pufferauslegung (`HeizzoneRechner`) liefert der Plan
  weiter die Sollwertreihe; die Deckelreihe ist für sie ohne Belang.

### 2.8 Zahlen an 1051 (Basis R51) — Messung und Abschätzung

Gemessen an `heizsollwert_0.csv`, `waermebedarf_gebaeude.csv`, `raumtemperatur_0.csv`, `operative_temperatur_0.csv`,
`stundentemperatur.csv` (Skript `scratchpad/ah2/sprung1051.py`, nicht im Repositorium); 148 Kalendersprünge um 07:00,
116 werktags (D = 13 h), 28 nach dem Wochenende (D = 61 h), 4 nach Feiertagen oder Ferien (D 36–109 h). Als Massenproxy
dient die Strahlungstemperatur 2·θ_op − θ_air.

**Der Sprung heute (Bestand mit Treppe, Stundenmittel):**

| Größe | Jahresmaximum | p90 | Median |
|---|---|---|---|
| S(h_s) = Φ̄(h_s) − Φ̄(h_s − 1) | **23,9 kW** (31.12., T_a 3,5 °C, keine Rampe: 4,0 → 27,9 kW) | 20,9 | 3,9 |
| Φ̄(h_s) − Φ̄(vor Rampenbeginn) | 26,8 kW | 22,3 | 19,0 |
| S_B = Φ̄(h_s) − Mittel(h_s+3 … h_s+8) | 25,8 kW | 23,4 | 15,7 |
| größter Stundenanstieg in Rampe und h_s | 23,9 kW | 20,9 | 8,5 |
| Φ̄(h_s) | 29,4 kW (05.01., Montag, n = 2, T_a 1,5 °C) | 27,0 | 23,8 |
| Φ_ref(h_s) = 0,69·(20 − T_a(h_s)) | **23,5 kW** (27.02., T_a −14,1 °C, D = 13, Rampe 12 h) | 16,5 | 11,9 |

Jahresspitze 30,00 kW am 19.01. um 06:00 (Rampenstunde, −12,4 °C); kälteste Stunde 28.02. 05:00 mit −18,17 °C bei
Sollwert 16 °C und 15,8 kW. **Der Jahreshöchstsprung liegt an einem milden Tag ohne Rampe** — die Treppe hat die kalten
Tage geglättet und die milden offen gelassen (1.3). Ohne Treppe läge der Sprung des kältesten Kalendertags bei rund
42,8 − 16 ≈ 27 kW (A/B-Protokoll, Abschätzung); S_max,0 des Vorlaufs liegt damit bei 24–28 kW.

**Bezug und Deckel (Lesart (i)):** Φ_K,max = 23,5 kW. x = 20 % → **P_K = 28,2 kW**; Beispiel kW: Δ = 5 kW → 28,5 kW.
P_verf = 31,62 kW → **P_V = min(P_K, P_verf) = 28,2 kW**: Der Deckel ist im Fenster die strengere Grenze; erst ab x ≈ 34 %
oder Δ ≈ 8 kW begrenzt P_verf. Lesart (ii) gäbe P_K ≈ 51 kW, wirkungslos. Zum Vergleich: 20 % der Auslegungsheizlast
Φ_HL = 22,19 kW (E97) sind 4,4 kW, 20 % von P_verf 6,3 kW — Φ_K,max liegt zwischen beiden und ist, anders als P_verf, von ρ
unabhängig.

**Bauwerte aus den Reihen (Abschätzung):**

| Größe | Werktag (D = 13 h) | Nach dem Wochenende (D = 61 h) |
|---|---|---|
| Fehlbetrag der Massen vor Rampenbeginn ΔT_m | Median 3,2 K (max 3,8) | Median 4,0 K (max 4,2) |
| Luft vor Rampenbeginn | 16,0 °C (die Heizung hält 16 °C) | 16,0 °C |
| Mehrwärme je Aufheizung (Rampe + 6 h gegen h_s+6 … h_s+9) | Median 64 kWh | Median 91 kWh |
| C_w = Mehrwärme/ΔT_m | 24 kWh/K | 26 kWh/K |

C_w ≈ 25 kWh/K (≈ 45 Wh/(m²·K) bei 572 m²; Spanne 24–31, Ferien 30) — gegen 5,8 kWh/K des Hauses aus 1045 im Teilkonzept
4.2 ein schwerer Bau. Abklingen der Mehrlast nach h_s an Montagen: 18,9 / 15,3 / 10,3 / 6,1 / 3,6 / 1,6 kW für
h_s … h_s + 5 — Zeitkonstante 2–3 h, durch Sonne und Gewinne des Vormittags verzerrt. Freie Auskühlung nach Blockende:
Luft 19,0 → 18,0 °C in 3 h, 16 °C nach 12 h (Median); Massenproxy 19,8 → 18,0 °C in 6 h — τ_frei ≈ 6–7 h, passend zu
3,2 K Fehlbetrag nach 13 h. Stundenmittel der Sprungantwort aus den 43 Sprüngen ohne Rampe: ḡ ≈ 3,5 kW/K (p90 4,9).

**Vorheizzeit erster Ordnung je Sprung** (2.3; C_w 26 bzw. 30 kWh/K, ΔT_m aus dem Massenproxy der Stunde h_s − t, T̄_a des
Fensters; Abschätzung, keine Messung):

| Grenze P_V | t_nötig werktags max | t_nötig Wochenende max | Median | unerreichbar | „fest“ (t_V = Jahresmax): Nächte ohne Absenkung | Mehrverlust „fest“ |
|---|---|---|---|---|---|---|
| 28,2 kW (Deckel, 20 %) | 9 h | **15 h** | 5 h | 0 | 117 von 148 | ≈ 2,4–2,6 MWh (+10 %) |
| 28,5 kW (Δ = 5 kW) | 8–9 h | 15 h | 5 h | 0 | 117 | ≈ +10 % |
| 31,6 kW (P_verf; x ≥ 34 %) | 7–8 h | 12–13 h | 4,5–5 h | 0 | 1–117 (12 h liegt an der Kante von D = 13) | ≈ +10 % |

Lesart: **Option 2 „fest“ liefert in 1051 t_V ≈ 15 h — länger als die Werktagsnacht.** Der Bemessungswert kommt vom
kältesten Montag (voller Fehlbetrag 4 K nach 61 h, Fenster bei −12 °C); angewandt an jedem Werktag heißt er Durchheizen,
rund +10 % Heizwärme (Mehrverlust ≈ H_s·(θ_T − θ_Luft)·(t_V − t_nötig), Abschätzung nach oben). Option 2 „täglich“ braucht
werktags 4–9 h, behält 4–9 h Absenkung und kostet gegenüber dem Bestand (der schon 1–12 h rampt) wenig; die Jahresspitze
fällt in beiden Fällen auf ≤ 28,2 kW (−6 %), der Sprung am Kalenderpunkt von 23,9 kW auf höchstens den Deckelspielraum
(erwartet 0–5 kW, erst der Lauf misst). Option 1 mit t_V = 6 h verfehlt die Montage (Bedarf 9–15 h) und die kältesten
Werktage (7–9 h) — der Hinweis nennt sie mit dem Bedarf. „Unerreichbar“ tritt in 1051 nicht auf: P_K = 28,2 kW liegt über
Φ_stat(20 °C, −18,2 °C) = 26,4 kW; mit x = 10 % (25,9 kW) läge der Deckel unter der Nachtlast der kältesten Nacht, und
die Nächte um den 28.02. würden ohne Absenkung gerechnet und als unerreichbar gezählt.

### 2.9 Text der Gruppe, Felder, Bericht

Gruppe **„Aufheizen vor Nutzungsbeginn“** / *„Preheating before occupancy“* in `SimulationKonfigSeite.razor`; sichtbar je
Verfahren.

| Schlüssel (neu oder geändert) | Deutsch | Englisch | sichtbar bei |
|---|---|---|---|
| `SIMKONF_AUFH_GRP` | Aufheizen vor Nutzungsbeginn | Preheating before occupancy | immer |
| `SIMKONF_AUFH_LBL_SCHALTER` | Aufheizen rechnen | Calculate preheating | immer |
| `SIMKONF_AUFH_LBL_VERFAHREN` | Verfahren | Method | an |
| `SIMKONF_AUFH_VERFAHREN_RAMPE` | Sollwertrampe nach Aufheizleistung | Setpoint ramp by preheat power | — |
| `SIMKONF_AUFH_VERFAHREN_VORGABE` | Vorheizzeit vorgeben | Specify preheat time | — |
| `SIMKONF_AUFH_VERFAHREN_BERECHNET` | Vorheizzeit berechnen | Calculate preheat time | — |
| `SIMKONF_AUFH_LBL_VORHEIZZEIT` | Vorheizzeit (h) | Preheat time (h) | Vorgabe |
| `SIMKONF_AUFH_LBL_VORHEIZART` | Vorheizzeit anwenden | Apply preheat time | Berechnet |
| `SIMKONF_AUFH_VORHEIZART_FEST` | an jedem Tag gleich (Jahreswert) | same every day (annual value) | — |
| `SIMKONF_AUFH_VORHEIZART_TAEGLICH` | je Tag so kurz wie möglich | as short as possible each day | — |
| `SIMKONF_AUFH_LBL_TOLERANZ` | Zulässiger Sprung zum Nutzungsbeginn | Permitted step at start of occupancy | Vorgabe, Berechnet |
| `SIMKONF_AUFH_TOLERANZ_PROZENT` | % der höchsten Heizlast am Nutzungsbeginn | % of highest heating load at start of occupancy | — |
| `SIMKONF_AUFH_TOLERANZ_KW` | kW | kW | — |
| `SIMKONF_AUFH_LBL_GENAUIGKEIT` | Regelgenauigkeit (K) | Control accuracy (K) | Vorgabe, Berechnet |
| `SIMKONF_AUFH_GENAUIGKEIT_EINGABE` | Eingabe | Enter value | — |
| `SIMKONF_AUFH_LBL_GELTUNG` | Vorheizzeit gilt | Preheat time applies | Vorgabe, Berechnet |
| `SIMKONF_AUFH_GELTUNG_GEBAEUDE` | je Gebäude | per building | — |
| `SIMKONF_AUFH_GELTUNG_ZONE` | je Zone | per zone | — |
| `SIMKONF_AUFH_LBL_HEIZKREIS` | Gebäude mit Heizkreis einbeziehen | Include buildings with heating circuit | Vorgabe, Berechnet |
| `SIMKONF_AUFH_LBL_RESERVE` | Leistungsreserve (%) | Power reserve (%) | alle |

Auswahl Regelgenauigkeit: 0,5 K · 1 K · 2 K · Eingabe (Vorgabe 1 K, Frage F15). Toleranz: Art (% / kW) und Wert, leer = die
konfigurierbare Vorgabe (20 %, Frage F14), Hinweis „Vorgabe 20 %“ neben dem Feld.

**Herleitungstexte:**

- **Aus** — DE: „Der Heizsollwert springt zu der Uhrzeit, die der Heizkalender vorgibt. Die erste Nutzungsstunde trägt
  dann das Aufwärmen der Bauteile; ihre Heizlast kann ein Mehrfaches der Last danach betragen.“ — EN: “The heating
  setpoint changes at the time set by the heating calendar. The first hour of occupancy then also warms up the building
  mass; its heating load can be several times the load afterwards.”
- **Vorheizzeit vorgeben** — DE: „Die Heizung beginnt die eingestellte Zeit vor dem Nutzungsbeginn mit dem Sollwert der
  Nutzung. Ab Nutzungsbeginn ist die Heizleistung bis zum Ende der Nutzung auf die höchste Heizlast am Nutzungsbeginn des
  Jahres zuzüglich des zulässigen Sprungs begrenzt (Deckel); vorgeheizt wird höchstens mit dem Deckel und nie über der
  verfügbaren Heizleistung. Der Lauf prüft jeden Tag, ob die Raumtemperatur zum Nutzungsbeginn den Sollwert bis auf die
  Regelgenauigkeit erreicht, und nennt die Tage, an denen die Vorheizzeit nicht reicht, mit der nötigen Zeit. Ein Gebäude
  kann eine eigene Vorheizzeit haben.“ — EN: “Heating starts the set time before occupancy with the occupancy setpoint.
  From the start to the end of occupancy, heating power is capped at the year's highest heating load at start of
  occupancy plus the permitted step (cap); preheating uses at most the cap and never more than the available heating
  power. The run checks every day whether the room reaches the setpoint within the control accuracy at the start of
  occupancy and lists the days on which the preheat time is too short, with the time required. A building can have its
  own preheat time.”
- **Vorheizzeit berechnen** — DE: „Der Lauf rechnet das Jahr zuerst ohne Vorheizen und bestimmt daraus den Deckel und
  für jeden Tag die kürzeste Vorheizzeit, mit der die Raumtemperatur zum Nutzungsbeginn den Sollwert erreicht, ohne den
  Deckel zu überschreiten. Der größte Wert des Jahres ist die Vorheizzeit des Gebäudes; sie gilt an jedem Tag oder — nach
  Wahl — nur so lang wie an diesem Tag nötig. Reicht auch die ganze Absenkung nicht, wird die Nacht durchgeheizt und
  genannt.“ — EN: “The run first calculates the year without preheating and derives the cap and, for every day, the
  shortest preheat time that brings the room to the setpoint at the start of occupancy without exceeding the cap. The
  year's largest value is the building's preheat time; it applies every day or — by choice — only as long as needed on
  that day. Where even the whole setback is not enough, the night is heated through and reported.”
- **Deckel** — DE: „Der Deckel ist die höchste Heizlast, die eine Nutzungsbeginn-Stunde des Jahres ohne Aufheizen hätte,
  zuzüglich des zulässigen Sprungs (in Prozent davon oder in kW). Er gilt stündlich vom Nutzungsbeginn bis zum Ende der
  Nutzung und nie unter der Heizlast der Stunde.“ — EN: “The cap is the highest heating load any start-of-occupancy hour
  of the year would have without preheating, plus the permitted step (as a percentage of it or in kW). It applies hourly
  from the start to the end of occupancy and never below the hour's heating load.”
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

Formelzeichen (ρ, Φ_K,max, P_K, G_0) gehören in Hilfe und Herleitungszeile, nicht in Beschriftungen. Die Herleitungszeile
nennt: Verfahren, Deckel (Bezug, Toleranz, Wert), verfügbare Leistung (Quelle), Vorheizzeit (vorgegeben bzw. berechnet,
„fest“/„täglich“), Sprung ohne Vorheizen und im Lauf, Tage verfehlt, Nächte ohne Absenkung.

**Gebäudedialog** (Reiter „Konditionierung“): „Vorheizzeit (h)“ je Gebäude (Verfahren Vorgabe; dieselbe Spalte wie die
manuelle Aufheizzeit), Zonen: „Vorheizzeit (h)“ bei Geltung Zone; Vorschlag daneben: der Bedarf der letzten Rechnung.

**Bericht und Kennzahlen:** Absatz „Aufheizen vor Nutzungsbeginn“ im Bericht und Kurzbericht (statt des Aufheizabsatzes
der Katalogfassung 16, wenn das Verfahren ≠ Rampe ist): Verfahren, Deckel, Vorheizzeit, Sprung ohne/mit Vorheizen,
Tage verfehlt und größte Unterschreitung, Nächte ohne Absenkung, Mehrwärme, Spitze des Laufs gegen Deckel; Kennzahlgruppe
„Gebäude“ und Abweichungsmerkmale des Variantenvergleichs (`AbweichungsErmittler`, `BausteineVergleich`) um Verfahren,
Deckel, Vorheizzeit, Sprung und Tage erweitert.

---

## 3 Gegenüberstellung

| | Bestand (Sollwertrampe) | Option 1 (t_V vorgegeben) | Option 2 (t_V berechnet) |
|---|---|---|---|
| **Eingaben** | Schalter, Bemessung (a)/(b) mit ΔT_K, Reserve ρ, Art täglich/fest, Aufschlag h/%, manuelle Zeit je Gebäude | Schalter, t_V (Projekt, Gebäude, Zone), Toleranz % oder kW, ε, Geltung, Heizkreis-Schalter, Reserve ρ bzw. `Heizleistung_Max` | wie Option 1 ohne t_V; dazu „fest“/„täglich“ |
| **Rechenweg** | Vorab-Stufenformel, Sollwerttreppe n Stufen; Lauf ideal ohne Grenze | Vorlauf (Φ_K,max, Deckel); Fenster mit Sollwert θ_T und Grenze P_V; Deckel P_K stündlich im Block; Nachweis | Vorlauf; Vorausschau je Sprung (Bisektion über t), t_V = Jahresmaximum; dann wie Option 1 |
| **Ergebnis an der Kalenderstunde** | Last ≤ P_auf (feste Zahl), Sprung bleibt: 1051 bis 23,9 kW, Median 3,9 kW | Last ≤ P_K per Bau; Luft ggf. unter θ_T, gezählt; Sprung ≤ Deckelspielraum | wie Option 1; Ankunft im Modell gesichert bis auf „unerreichbar“ |
| **Prüfung „Sollwert erreicht“** | keine (per Bau erfüllt, Grenze fehlt) | ja, je Tag, mit Bedarf | ja, als Nachweis der Vorausschau |
| **Auslegungswirkung** | Φ_HL + (P_auf − Φ_stat); Jahresspitze in der Rampe (1051: 30,0 kW) | Jahresspitze ≤ P_K (1051: 28,2 kW); E60-Größe bleibt, Deckel daneben | wie Option 1, dazu t_V als Bemessungswert |
| **Wärme** | Mehrwärme der Rampe klein (täglich) | fest vorgegeben: an milden Tagen mehr als nötig; t_V ≥ D heißt Durchheizen | „fest“: 1051 ≈ +10 % (Werktage durchgeheizt); „täglich“: wenig Mehrwärme |
| **Rechenzeit** | ein Lauf | Lauf + Vorlauf + Vorausschau an verfehlten Tagen (≈ 2–3 Zonenjahre) | Lauf + Vorlauf + 148·≤ 6 Vorausschauen (≈ 4–7 Zonenjahre, unter 1 s je Zone, Abschätzung) |
| **Aufwand** | gebaut | 9,5–13 PT (alle Wellen außer V3) | 11,5–16 PT (mit V3) |
| **Risiken** | Text verspricht mehr als die Rechnung; AK1-Einzone ausgenommen | zu kurzes t_V → Unterschreitung (gezählt); Deckel unter der Blockstundenlast an kalten Nachmittagen (Floor, gezählt) | „fest“ hebt die Werktagsabsenkung auf; Vorausschau weicht im AK3-Kreis vom Lauf ab (gemeldet) |

---

## 4 Folgen

**Schema** (ein Schritt, Nummer bei Anmeldung gegen origin — 211 und 212 sind vergeben; Muster `AufheizvorgabeSchema`,
alles per `ADD COLUMN`, nullbar, mit Prüfklausel; Bestand bitgleich bei NULL):

- `Tab_Einstellungen`: `Aufheiz_Verfahren` TEXT CHECK IN ('RAMPE','VORGABE','BERECHNET') — NULL = RAMPE;
  `Aufheiz_Vorheizzeit_H` INTEGER CHECK BETWEEN 1 AND 47; `Aufheiz_Vorheizzeit_Art` TEXT CHECK IN ('FEST','TAEGLICH') —
  NULL = Vorgabe nach F13; `Aufheiz_Toleranz_Art` TEXT CHECK IN ('PROZENT','KW') — NULL = PROZENT;
  `Aufheiz_Toleranz` REAL CHECK (≥ 0) — Anteil bei PROZENT, kW bei KW, NULL = Vorgabe 20 %;
  `Aufheiz_Regelgenauigkeit_K` REAL CHECK (> 0 AND ≤ 5) — NULL = Vorgabe nach F15; `Aufheiz_Geltung` TEXT CHECK IN
  ('GEBAEUDE','ZONE') — NULL = GEBAEUDE; `Aufheiz_Heizkreis_Einbeziehen` INTEGER CHECK (IN (0,1)) — NULL = 1.
- `Tab_Gebaeude.Aufheizzeit_Manuell_H` bleibt und trägt in „Vorgabe“ die Vorheizzeit des Gebäudes (1–47 h); kein
  Katalogfeld. `Tab_Zone.Vorheizzeit_H` INTEGER CHECK BETWEEN 1 AND 47 — NULL = erbt (nur Geltung Zone, Verfahren Vorgabe).
- Ergebnis (`Tab_ErgebnisGebaeude`, `Tab_ErgebnisZone`): `Aufheiz_Verfahren`, `Vorheizzeit_H` (wirksam bzw. Jahreswert),
  `Vorheizzeit_Bedarf_Max_H`, `Vorheiz_Bezug_Kw` (Φ_K,max), `Vorheiz_Deckel_Kw`, `Vorheiz_Leistung_Kw` (P_V),
  `Vorheiz_Sprung_Vorlauf_Max_Kw`, `Vorheiz_Sprung_Lauf_Max_Kw`, `Vorheiz_Tage_Verfehlt`, `Vorheiz_Unterschreitung_Max_K`,
  `Vorheiz_Unterschreitung_H`, `Vorheiz_Deckelstunden_H`, `Vorheiz_Floorstunden_H`, `Vorheiz_Naechte_Ohne_Absenkung`,
  `Vorheiz_Unerreichbar_Tage`, `Vorheiz_Mehrwaerme_Kwh` — neue Spalten statt einer Erweiterung des CHECK von `Aufheiz_Art`.

**Kern:** Deckelreihe im Eingang (`GebaeudeModellEingang`, `ZonenEingang`, Stepper) statt Skalar, bitgleich ohne Reihe;
`Vorheizplan` neben `Aufheizplan` (Vorlauf, Φ_K,max, P_K, P_V, Fenster, Deckelreihe, Bedarf, Zähler); die Weiche nach
Verfahren in `Aufheizoptimierung.Anwenden` bzw. daneben; der Vorlauf ruft `Laufen` ohne Plan und ohne Ergebnisschreiben
(Einzone, Mehrzonen, Kopplungsweg; AK3 über den Profilweg); die Vorausschau sichert und setzt den Zustand des Modells
(`Zuruecksetzen(θ_MAw, θ_MIw)`) und nutzt `Schritt` mit den Randwerten der Stunden; Nachweis im Ergebnis
(`GebaeudeModellErgebnis`, `Komfortkennzahlen`); Laufhinweise `SIMENG_VORHEIZ_*` neu; Auskunft (`Aufheizauskunft`,
`AufheizauskunftCtrl`) und Herleitungszeile; `KonfigurationCtrl.AufheizvorgabeLesen/Schreiben`; die Vorgabe der Toleranz
als Konstante im Kern (20 %) und als Schlüssel in `Dienste.Einstellungen` für die Vorbelegung der Oberfläche (F14).
Vergleiche an Grenzen über `Rechenrand.SchwelleErreicht`; keine `(int)`-Abschneidung; feste Feldgrößen.

**Oberfläche:** Gruppe nach 2.9 in `SimulationKonfigSeite.razor` mit Verfahrensauswahl und je Verfahren sichtbaren
Feldern; Gebäudefeld und Zonenfeld; Hülle in `EPOS.UI.Daten`; KI-Felder (`KiDialoge`); Texte beider Sprachen,
`ResourceDesigner` ziehen; bunit-Tests; Windows-Schale kompiliert.

**Bericht und Kennzahlen:** `Aufheizbericht`, `KennzahlenKatalog`, `Vorlagenfeldkatalog.Standwerte`, Kurzbericht-Absatz
(Katalogfassung anheben), `AbweichungsErmittler`, `BausteineVergleich`; Export `Geb[n].Vorheiz*` nur bei Verfahren ≠ RAMPE —
so bleibt jede Basisdatei der Bestandsprojekte byte-gleich.

**Referenzprojekt und Basis (F9):** 1051 bleibt auf „Rampe“ und hält den Bestand. Neu ein Projekt als Kopie von 1051
(Nummer bei Anlage) mit „Vorheizzeit berechnen“, Toleranz 20 % (Prozent), ε nach F15, Geltung Gebäude, Vorheizzeit-Art
nach F13; dazu eine Testzeile für „Vorgabe“ in den Kerntests; Wache nach dem Muster `KonditionierungReferenzprojektWacheTests`
(Skript neben `referenzprojekt_1051_konditionierung.cs`), neue Basis, Aufnahme in die CI-Auswahl. Die Einfrierregel „gesäte
Konditionierungsdaten“ (`Referenzlaeufe/LIESMICH.md`) wird um die neuen Spalten an `Tab_Einstellungen` (`Aufheiz_Verfahren`,
`Aufheiz_Vorheizzeit_H`, `Aufheiz_Vorheizzeit_Art`, `Aufheiz_Toleranz_Art`, `Aufheiz_Toleranz`, `Aufheiz_Regelgenauigkeit_K`,
`Aufheiz_Geltung`, `Aufheiz_Heizkreis_Einbeziehen`), `Tab_Zone.Vorheizzeit_H` und das neue Projekt erweitert; wer 1051
umstellt statt zu kopieren, friert ebenfalls neu ein. **Bestand bitgleich:** Verfahren NULL/RAMPE nimmt den heutigen Weg;
V1 und V2 weisen den Referenzlauf byte-gleich nach.

**Wiki-Quellen** (`Projekte/Wiki/`): „Programm Dokumentation - Gebäudemodell VDI 6007“ (Ziel des Hilfeknopfs),
„Simulation konfigurieren und starten“, „Programm Dokumentation - Simulationsergebnisse“, „Grundlagen -
Wärmebedarfsrechnung“, „Programm Dokumentation - Mehrzonenmodell“, „Programm Dokumentation - Berichtsvorlagen“; ein
Logbuch-Satz beim gebündelten Upload, Versionsnummer beim Anwender erfragen.

---

## 5 Entscheide und Fragen

### 5.1 Entschieden (Anwender, 10.10.2026) — Wortlaut und Umsetzung in diesem Entwurf

| | Wortlaut | Umsetzung |
|---|---|---|
| Klarstellung 1 | „Vorheizfenster: Ab t_V Stunden vor dem Sprung steht der Sollwert schon auf dem Zielwert. Geheizt wird mit einer Leistung, die keinen zusätzlichen Sprung am Zeitpunkt der Temperaturänderung (Kalender) erzeugt (Beispiel Nachtabsenkung von 20 °C auf 17 °C). Kalenderdeckel: die Zeit ist jeweils der Sprung der Soll-Temperatur im Kalender, keine fixe Zeit.“ | Fenster mit Sollwert θ_T (2.1); Grenze P_V im Fenster, Deckel ab dem Kalendersprung (2.2); Zeitpunkt aus der Sprungliste, keine feste Uhrzeit |
| F1 | „Aufheizoptimierung optional, vom Nutzer wählbar.“ | Verfahren Rampe / Vorgabe / Berechnet, Schalter wie heute (2.2, 2.9) |
| F2 | „Es gilt der höchste Sprung der Heizlast über ein Jahr am Punkt der Temperaturänderung (Kalender).“ | Φ_K,max aus dem Vorlauf, Lesart (i) ohne Aufheizanteil (2.1; Frage F11 zu den Lesarten) |
| F3 | „Sprung (Toleranz) wählbar als % oder kW, Vorgabe 20 %, die Vorgabe ist konfigurierbar.“ | `Aufheiz_Toleranz_Art`, `Aufheiz_Toleranz`; Kern-Konstante 20 %, Vorbelegung über `Dienste.Einstellungen` (Frage F14) |
| F4 | „Leistung so, dass der Sprung nicht über den Deckel geht, bzw. höchstens P_verf.“ | P_V = min(P_K, P_verf) im Fenster (2.1) |
| F5 | „wahlweise (1) t_V vorgegeben oder (2) t_V berechnet, so dass der Sprung der Heizleistung am Temperatursprung minimiert ist — über ein Jahr den maximalen Sprung berechnen und daraus die Zeit t_V, die erforderlich ist, um den Deckel nicht zu überschreiten.“ | Option 1 (2.5) und Option 2 (2.6): t_V = Jahresmaximum des Bedarfs aus Vorlauf und Vorausschau |
| F6 | „wählbar je Zone oder je Gebäude.“ | `Aufheiz_Geltung`, Rechenregeln in 2.7 |
| F7 | „Ankunftskriterium als Vorgabe der Regelgenauigkeit der Übergabe (0,5 K; 1 K; 2 K) oder Eingabe.“ | ε mit Auswahl und Eingabe, gemessen am Beginn von h_s (2.1; Vorgabe Frage F15) |
| F8 | **(a)** — ersetzt die frühere Vorgabe: „Der Kalenderdeckel gilt im Lauf ab dem Sollwertsprung des Kalenders bis zum Ende des Sollwertblocks (nächster Sprung nach unten). Er ergänzt die Jahresbetrachtung (F2; F5 Option 2), ersetzt sie nicht.“ | Deckel P_K aus Jahresmaximum und Toleranz, **stündlich wirksam** von h_s bis Blockende als `heizleistungMaxW`; die Jahresbetrachtung liefert die Bemessung (Φ_K,max, P_K, t_V), der Deckel wirkt im Lauf (2.2); Floor Φ_ref(h) als Festlegung (Frage F12) |
| F9 | „neues Referenzprojekt als Kopie von 1051 mit dem neuen Verfahren, 1051 bleibt beim Bestand.“ | Abschnitt 4, Einfrierregel erweitert |
| F10 | „AK1-Einzonengebäude einbeziehen, optional ausnehmbar.“ | Vorlauf und Vorausschau auf dem Kopplungsweg, Schalter `Aufheiz_Heizkreis_Einbeziehen` (2.7; Frage F16 zur Ebene) |

### 5.2 Offene Fragen (höchstens sechs, je mit Empfehlung)

1. **F11 — Bezug Φ_K,max:** (a) Jahresmaximum der Heizlast am Kalenderpunkt **ohne Aufheizanteil** (Φ_ref, dieselbe
   stationäre Form wie die heutige Rampe; 1051: 23,5 kW → Deckel 28,2 kW); (b) Jahresmaximum der Kalenderstunde **mit**
   Aufheizanteil aus dem Vorlauf (1051: ≈ 42,8 kW → Deckel ≈ 51 kW, ohne Wirkung); (c) Toleranz als Steigungsgrenze je
   Stunde auf das Jahresmaximum des Sprungs S_max,0 (Rampenbegrenzung Φ(h) ≤ Φ(h − 1) + Δ — anderer Mechanismus, kein
   Leistungsdeckel). **Empfehlung (a):** wörtlich „Heizlast am Punkt der Temperaturänderung“, von ρ unabhängig, als Deckel
   wirksam und vor dem Lauf bekannt.
2. **F12 — Floor des Deckels:** (a) die Grenze einer Blockstunde ist max(P_K, Φ_ref(h)), Floor-Stunden werden gezählt
   und gemeldet; (b) P_K streng, auch unter der stationären Last (Komfortverlust, gezählt). **Empfehlung (a)**: Der Deckel
   soll den Aufheizanteil begrenzen, nicht das Halten des Sollwerts.
3. **F13 — Anwendung der berechneten Vorheizzeit:** (a) „fest“: t_V (Jahresmaximum) an jedem Sprung — Wortlaut F5,
   1051: ≈ 15 h, Werktage durchgeheizt, ≈ +10 % Heizwärme; (b) „täglich“: je Sprung der Bedarf t_nötig, höchstens t_V,
   t_V bleibt Bemessungswert und Ausgabe — 1051: Median 5 h, Absenkung bleibt. **Empfehlung (b) als Vorgabe**, (a) als
   Auswahl (`Aufheiz_Vorheizzeit_Art`), beide auf demselben Kern; Option 1 ist von Natur aus „fest“.
4. **F14 — Ort der konfigurierbaren Vorgabe 20 %:** (a) Kern-Konstante 20 % für leere Felder (deterministisch für
   Referenzlauf und Tests) plus Vorbelegung neuer Projekte über `Dienste.Einstellungen` (Programmeinstellung, Gruppe
   „Weitere Einstellungen“); (b) eine Vorgabetabelle in der Datenbank. **Empfehlung (a)**: wie die Reserve ρ; die
   Datenbank bleibt frei von Programmvorgaben.
5. **F15 — Vorgabe der Regelgenauigkeit und Messpunkt:** (a) ε = 1 K, gemessen am Beginn von h_s (Augenblick, Ende des
   Fensters), Stundenmittel von h_s als Kennzahl; (b) 0,5 K; (c) Stundenmittel von h_s als Kriterium. **Empfehlung (a)**:
   1 K entspricht dem Spielraum der 20 % (2.3) und dem Regelabstand eines P-Reglers der Übergabe; der Augenblick ist
   „zum Kalenderzeitpunkt“ im Wortsinn und folgt E58 F1 (b).
6. **F16 — Ebene der Heizkreis-Ausnahme (F10):** (a) ein Schalter je Projekt; (b) ein Schalter je Gebäude. **Empfehlung
   (a)**: ein Projekt hat selten gemischte Fälle; je Gebäude kann später folgen.

---

## 6 Wellenplan

| Welle | Inhalt | Abnahme | PT |
|---|---|---|---|
| V0 | Entscheide nachziehen: Teilkonzept Konditionierungsprofile 4 (neuer Abschnitt Vorheizen mit Deckel), Glossar, Register, Statuszeile; Fragen F11–F16 entschieden einarbeiten | Linkwache | 0,5 |
| V1 | Kern: Deckelreihe je Stunde in Eingang, Zonen, Kopplungsweg (AK1/AK2) und Stepper (AK3); Zustand sichern/setzen für die Vorausschau; ohne Reihe bitgleich; Messung der Rechenzeit eines Zonenjahrs | Referenzlauf byte-gleich, Kerntests | 1,5–2 |
| V2 | Kern: Vorlauf (Φ_ref, Φ_K,max, S_0, Zustände), Deckel P_K/P_V mit Toleranz % und kW, Vorheizplan Option 1 (Fenster, Sollwert, Kühlkappe, Deckelreihe, Floor, W4, Nacht ohne Absenkung), Nachweis im Lauf (Ankunft am Beginn von h_s, Unterschreitung, Sprung, Deckel- und Floor-Stunden, Mehrwärme), Hinweise, Gebäudewerte | neue Tests, Probe gegen die erste Ordnung (2.3), Bestand byte-gleich | 2–3 |
| V3 | Kern: Option 2 — Vorausschau je Sprung (Bisektion über t), Bedarf, „unerreichbar“, t_V fest/täglich, Geltung Zone/Gebäude mit Zonenanteilen, AK1-Einbezug, AK3-Profilweg; Messung τ, C_w, t_V und Mehrwärme an der Kopie von 1051 gegen 2.8 | Prüforakel, Messprotokoll, Rechenzeit unter der Schranke | 2–3 |
| V4 | Schema und Datenweg: ein Schritt (Einstellungen, Zone, Ergebnis Gebäude/Zone), Controller, Testdatenbank anheben, `SqlDialektPruefer` | Schema- und Controllertests | 1–1,5 |
| V5 | Oberfläche: Gruppe mit Verfahren und Feldern nach 2.9, Texte beider Sprachen, Gebäude- und Zonenfeld, Vorbelegung aus `Dienste.Einstellungen`, Hülle, KI-Felder, bunit | UI-Tests, Windows-Schale kompiliert | 1,5–2 |
| V6 | Bericht, Kennzahlen, Export, Kurzbericht (Katalogfassung), Variantenvergleich | Berichtstests, `Berichtsvorlage` gezogen | 1–1,5 |
| V7 | Referenzprojekt (Kopie von 1051), Wache, Einfrierregel, neue Basis, CI-Auswahl | Referenzlauf gegen neue Basis | 1–1,5 |
| V8 | Wiki-Quellen und Logbuch-Entwurf | Gegenlese-Muster, Produktdatenwache | 0,5–1 |
| | **Summe** (beide Optionen) | | **11,5–16** |

Nur Option 1 (ohne V3; Bedarf in der Meldung dann aus der Vorausschau an den verfehlten Tagen, Geltung nur Gebäude):
9,5–13 PT. Reihenfolge: V0 → V1 → V2 → V4 → V3 → V5/V6 parallel → V7 → V8; V1 und V2 ändern ohne Verfahrenswahl keine
Zahl. Vor V2 sind F11, F12 und F15 zu entscheiden, vor V3 F13, vor V4 F14 und F16.
