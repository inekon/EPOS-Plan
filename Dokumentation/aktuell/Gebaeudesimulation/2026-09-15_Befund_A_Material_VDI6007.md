# Befund A — das VDI-6007-Material auf Z: (15.09.2026)

**Protokoll.** Befund eines Analyse-Agenten (Modell Opus) im Auftrag des Konzepts
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Sitzung vom 15.09.2026. Gegenstand: die fünf Dateien unter
`Z:\…\15-Anleitungen_Literatur\Simulation-Gebäudemodell` (Python-Modell, C#-Projekt, zwei
Docx, Ergebnisbild). Wortlaut wie geliefert; bei Widerspruch gilt das Konzept.

---

Alle fünf Dateien wurden in den Arbeitsordner kopiert, die beiden .docx über Entpacken von `word/document.xml` und ein Perl-Skript in Text gewandelt (kein Python auf dem Rechner verfügbar), die ZIP entpackt, alle Quelltexte und das PNG gelesen. Zusätzlich wurde die Bauteilreduktion und der Testfall-1-Lauf in einem eigenen Perl-Skript unabhängig nachgerechnet, weil das Ergebnisbild physikalisch unplausibel ist. **Das Hauptergebnis vorweg: das Modell ist in seiner jetzigen Form fachlich falsch** (Details in A.4).

## A. Physik des Modells

**A.1 Modellansatz.** Umgesetzt ist das **7R2C-Raummodell nach VDI 6007 Blatt 1**, Einzonenmodell. Bauteile werden in zwei Klassen zerlegt: nicht-adiabate Außenbauteile (AW) und adiabate Innenbauteile (IW, symmetrisch, nur bis zur Symmetrieebene modelliert). Je Klasse ein Ersatz-RC-Glied.

Zustandsgrößen (2 Stück): `theta_m_AW`, `theta_m_IW` — Kapazitätsknotentemperaturen in °C. Algebraische Knoten (3 Stück): `T_surf_AW`, `T_surf_IW`, `T_air`. Widerstände: R1,AW; RRest,AW; R1,IW; Rconv,AW = 1/(h_conv·A_AW); Rconv,IW; R_rad = 1/(h_rad·(A_AW+A_IW)); R_ve = 1/H_ve. Kapazitäten: C1,AW, C1,IW [J/K].

**A.2 Bauteilreduktion.** Kettenmatrixverfahren im Frequenzbereich, Referenzperiode T = 86400 s (ω = 2π/T). Je Schicht: a = λ/(ρ·c_p), ξ = d·√(ω/(2a)), γ = ξ·(1+j), Kettenmatrix [[cosh γ, R·sinh γ/γ],[γ·sinh γ/R, cosh γ]] mit R = d/(λ·A). Multiplikation von innen nach außen. Extraktion: Z = a12/a22, **R1 = Re(Z), C1 = −1/(ω·Im(Z))**, R_rest = R_total − R1. Aggregation mehrerer Bauteile über Parallelschaltung (ΣC, Σ1/R). Dies ist eine plausible, aber **nicht normkonforme** Heuristik: VDI 6007-1 schreibt ein definiertes Identifikationsverfahren mit getrennter Anregung von innen und außen vor.

**A.3 Zeitschritt und Lösung.** Δt = 3600 s fest voreingestellt. Die Systemmatrix A wird **bewusst diagonal** angesetzt (a11 = −(G1,AW+GRest,AW)/C1,AW; a22 = −G1,IW/C1,IW; a12 = a21 = 0), womit exp(A·Δt) elementweise analytisch ist: Φ = exp(a·Δt), Γ = (Φ−1)/a. Damit ist der Löser für stückweise konstante Eingänge unbedingt stabil. Die algebraischen Knoten werden pro Zeitschritt mit **Gauß-Seidel, max. 15 Iterationen, Abbruch bei 1e-6 K** gelöst. Ergänzend T_op = 0,5·T_air + 0,5·T̄_surf (flächengewichtet).

**A.4 Fehler im Kern (entscheidend).** Durch die Diagonalisierung fehlt in der AW-Zustandsgleichung der Rückkopplungsterm `+G1,AW·θ_surf,AW/C1,AW`. Der Massenknoten hat damit nur noch **eine** Quelle, gewichtet mit GRest/(G1+GRest). Sein Stationärwert ist folglich θ_m,AW,∞ = T_eq · R1/(R1+RRest) statt θ_m,AW,∞ ≈ T_eq. Mit den vom Code selbst erzeugten Parametern (R1 = 0,00734 K/W, RRest = 0,169 K/W) ergibt das bei T_eq = 22 °C einen Stationärwert von **0,9 °C**. Die Außenwand wirkt dadurch als künstliche Kältesenke; die Energiebilanz ist nicht erhalten. Das ist kein Genauigkeits-, sondern ein Strukturfehler, und er steckt **identisch im Python-Modell, im C#-Projekt und im Codebeispiel der Doku**.

Unabhängige Nachrechnung des Testfalls 1 (T_out = T_eq = 22 °C konstant, 1000 W konvektiv 6–18 Uhr, keine Lüftung): Tag 1 max 23,56 / min 14,28 °C; Tag 10 max 10,83 / min 4,55 °C; Tag 60 max 10,74 / min 4,43 °C. Der Python-Docstring nennt als Sollwerte Tag 1 25,2/22,5; Tag 10 27,3/25,2; Tag 60 28,3/26,3 °C. **Abweichung rund 18–22 K bei einer Normtoleranz von ±0,1 K.** Die Werte decken sich exakt mit dem mitgelieferten PNG, das Ergebnisbild dokumentiert also den Fehlerzustand.

**A.5 Randbedingungen.** Außenluft `T_outside` [°C] und äquivalente Außentemperatur `T_eq_aw` [°C] werden als Skalare je Zeitschritt **von außen übergeben** — es gibt kein Strahlungsmodell, keine Orientierungen, keine Sonnenstandsberechnung, keine langwellige Gegenstrahlung. Die Umrechnung T_eq = T_out + α·I_sol/h_comb wird nur im Demo-Szenario hart ausgeschrieben (α = 0,6; h = 25). Innere Lasten als `phi_conv` / `phi_rad` [W], radiativ flächenproportional auf AW/IW verteilt. Lüftung/Infiltration ausschließlich als Verlustkoeffizient `H_ve` [W/K] = n·V·ρ·c_p/3600. **Nicht vorhanden:** Fenstermodell (kein U_w, kein g-Wert, kein Rahmenanteil, keine Verschattung), Erdreichkopplung, Nachbarzonen, Mehrzonigkeit, Flächenheiz-/Kühlsysteme, Feuchte.

**A.6 Regelung.** Ideal und unbegrenzt: `calculate_heating_cooling_load` rechnet einen Freilauf-Schritt, verwirft den Zustand, schätzt Φ_hc = H_total·(T_set − T_free) mit H_total = G_conv,AW + G_conv,IW + H_ve **linear und ohne Iteration** und rechnet den Schritt erneut. Da die Oberflächenknoten auf Φ_hc reagieren, wird der Sollwert systematisch verfehlt (im Bild: Sollwert 20 °C, erreicht ~22 °C). Keine Leistungsbegrenzung, kein Totband, keine Reglerdynamik. Nachtabsenkung existiert nur als Literal im Demo-Szenario (20 °C von 6–22 Uhr, sonst 16 °C).

## B. Eingangsdaten

**Schicht** (`WallLayer`): `name`; `thickness` d [m]; `conductivity` λ [W/(m·K)]; `density` ρ [kg/m³]; `capacity`/`SpecificHeat` c_p [J/(kg·K)]. Abgeleitet: R/A = d/λ [m²K/W], C/A = ρ·c_p·d [J/(m²K)], (C#) Diffusivität a = λ/(ρc_p).

**Bauteil** (`BuildingElement`): `name`; `element_type` "exterior"/"interior" bzw. `ElementType.Exterior/Interior`; `area` A [m²]; `layers` (Reihenfolge **innen → außen**, bei Innenbauteilen nur bis zur Symmetrieebene); `h_conv_inner` = **2,7** W/(m²K); `h_comb_outer` = **25,0** W/(m²K). C# zusätzlich `UValueCore` und `UValue` (letzterer rechnet innen mit h_conv + 5).

**Zone** (`ThermalZoneConfig`, nur Python): `name` = "Raum"; `volume` V = **52,5** m³; Bauteillisten; `h_rad` = **5,0** W/(m²K); `initial_temp` = **22,0** °C. Im C# liegen hRad/initialTemp/dt direkt am Konstruktor des Modells (5,0 / 22,0 / 3600).

**Reduktionsparameter:** `period` = **86400,0** s.

**Zeitreihen je Schritt** (C#: `ZoneInputs`): `TOutside` [°C], `TEquivalentAW` [°C], `PhiConvective` [W], `PhiRadiative` [W], `PhiSolar` [W] (nur C#), `VentilationLossCoefficient` H_ve [W/K], `PhiHeatingCooling` [W].

**Nötige Gebäudekenngrößen:** Schichtaufbauten mit d/λ/ρ/c_p und Flächen aller opaken Bauteile, getrennt nach außen/innen; Raumvolumen; Übergangskoeffizienten; Luftwechsel als n [1/h] oder H_ve. **Nicht verlangt und nicht verwertbar:** U-Werte als Eingang (werden aus Schichten gerechnet), g-Werte, Fensterflächen je Orientierung, n50, Absorptionsgrade (nur implizit im Demo).

**Klimadaten:** es gibt **kein Einleseformat**. Kein TRY-Parser, keine CSV/EPW-Schnittstelle, keine Spaltendefinition. Beide Implementierungen erzeugen synthetische Sinusprofile im Code (Python: T_out = 3 + 5·sin(2π(h−6)/24); Solar 150·sin(π(h−7)/10) für 7–17 Uhr).

## C. Ausgangsdaten

Je Zeitschritt (`ZoneOutputs` / dict): `T_air`, `T_operative`, `T_surf_AW`, `T_surf_IW`, `theta_m_AW`, `theta_m_IW`, alle in °C; aus `CalculateLoad` zusätzlich Φ_hc [W] (>0 Heizen, <0 Kühlen). Aggregiert werden in den Demos Heiz-/Kühlenergie [kWh] als Summe/1000, maximale Heizlast [W], Mittel-/Min-/Max-Raumtemperatur.

Das Raster ist **frei wählbar über die Schleifenlänge** — es gibt kein 8760er-Raster, keinen Kalender, keine Zeitstempel. Die Demos laufen 60·24 = 1440 Schritte (Testfall) und 14·24 = 336 Schritte (Szenario). Keine Datei-, CSV- oder JSON-Ausgabe; Ergebnisse gehen auf die Konsole bzw. in das matplotlib-PNG.

## D. Aufbau des Python-Codes

1211 Zeilen, 59 KB, sehr ausführlich kommentiert (ASCII-Kästen, Schritt-1-bis-9-Didaktik). Bibliotheken: **numpy** (nur für Arrays, Matrixmultiplikation und komplexe cosh/sinh — kein scipy, insbesondere **kein `expm`**), **matplotlib** (pyplot + dates), `dataclasses`, `typing`, `datetime` (praktisch ungenutzt).

Struktur: Dataclasses `WallLayer`, `BuildingElement`, `ThermalZoneConfig`, `RCParameters` → Funktionen `compute_layer_chain_matrix(layer, area, omega) -> np.ndarray(2,2,complex)`, `reduce_single_element(element, period=86400.0) -> (R1, C1, R_rest)`, `aggregate_elements(elements, period=86400.0) -> RCParameters` → Klasse `ThermalZoneModel(config, dt=3600.0)` mit `_build_system_matrices()`, `_precompute_matrix_exponential()`, `step(T_eq_aw, T_outside, phi_conv=0, phi_rad=0, vent_loss_coeff=0, phi_hc=0) -> dict`, `calculate_heating_cooling_load(T_set, T_eq_aw, T_outside, phi_conv, phi_rad, vent_loss_coeff) -> (phi_hc, dict)`, `_print_summary()` → `create_vdi6007_test_room_heavy()`, `run_testcase_1()`, `run_realistic_scenario()`, `create_plots(...)`.

**Validierung:** nur Testfall 1, schwere Bauweise S. Die Sollwerte stehen als Text im Docstring, es gibt **keinen Assert, keinen Toleranzvergleich, keine Testfälle 2–12**. Das Programm druckt die (falschen) Werte kommentarlos aus. Die Schlussbemerkung räumt selbst ein, die Kettenmatrixrechnung sei „eine vereinfachte Fassung" und für den Produktiveinsatz sei ein Abgleich mit TEASER/AixLib nötig.

**Laufzeit:** unkritisch — 1776 Zeitschritte × ≤15 Iterationen sind reine Skalararithmetik, deutlich unter 0,1 s; die Rechenzeit dominiert matplotlib.

**Portabilitätsmangel:** `plt.savefig("/home/claude/vdi6007_ergebnisse.png")` ist ein fest verdrahteter Linux-Pfad, das Skript bricht auf Windows am Ende ab.

## E. .NET-Stand (ZIP und Doku)

**ZIP (`VDI6007_VisualStudio.zip`, 21,7 KB, 10 Dateien, Ordner `VDI6007_VS/`).** Ein einziges Projekt `VDI6007.Simulation` (`OutputType=Exe`), Solution im VS-17.9-Format. Zielframework **net8.0**, `LangVersion 12`, `ImplicitUsings enable`, `Nullable enable`, Version 1.0.0. **Keine einzige NuGet-Abhängigkeit**; komplexe Arithmetik über `System.Numerics.Complex` aus der BCL. Namensräume: `VDI6007.Simulation.Models`, `.Core`, `.Validation`; Program.cs als Top-Level-Statements.

Portiert ist der **vollständige Umfang des Python-Modells**, teils sauberer: `WallLayer` (record, Ctor-basiert), `BuildingElement` (+ `TotalResistancePerArea`, `TotalCapacityPerArea`, `UValueCore`, `UValue`), `RcParameters` (+ `TimeConstant`, `TimeConstantHours`), `ZoneInputs`/`ZoneOutputs` als records, `ChainMatrixReducer.ReduceElement(element, period = DefaultPeriod)` und `.AggregateElements(IReadOnlyList<BuildingElement>, period)`, `ThermalZoneModel(aw, iw, hRad = 5.0, initialTemp = 22.0, dt = 3600.0)` mit `Step(ZoneInputs)`, `CalculateLoad(double tSet, ZoneInputs)`, `Reset(double)`, `PrintSummary(TextWriter?)`. `Validation/TestCases.cs` liefert drei Konstruktionen (`TestRooms.HeavyConstruction()`, `.OfficeRoom()`, `.LightConstruction()`) und `SimulationRunner.RunTestCase1(model, nDays = 60)`, `.RunWinterScenario(model, nDays = 14)`, `.PrintDaySummary(double[], int)`.

**Unterschiede zum Python-Modell:** `ZoneInputs.PhiSolar` existiert zusätzlich und wird in `Step` zu `PhiRadiative` addiert; `RRest` wird im C# **inklusive äußerem Übergangswiderstand** gerechnet, Python lässt ihn weg; die Zeile `rRestPerArea = TotalResistancePerArea − R1·A` mischt flächenbezogene und absolute Größen; Testfall 1 setzt `VentilationLossCoefficient = 1e-5` statt 0; `double rTotal` in `ReduceElement` ist tote Variable; `Reset()` wird im Konstruktor gerufen, bevor die `readonly`-Felder gesetzt sind.

**Qualität:** **keine Unit-Tests, kein Testprojekt, keine Referenzdatensätze, kein CI**. „Validierung" heißt hier ausschließlich: Zahlen auf die Konsole drucken. Der Physikfehler aus A.4 ist 1:1 übernommen (`_a11`, `_a22`, `_bCoeffAW`). Lizenz im README: „Dieses Projekt dient Lehr- und Studienzwecken." — das ist **keine brauchbare Lizenz** für ein kommerzielles Produkt.

Das Projekt konnte nicht kompiliert werden: der Scratchpad-Pfad ist 311 Zeichen lang, MSBuild und Windows PowerShell 5.1 scheitern an der Win32-Pfadlänge (`dotnet` 10.0.401 ist installiert). Die Nachrechnung erfolgte deshalb durch Reimplementierung der Formeln.

**Doku-Docx (`VDI6007_Dokumentation_DotNet.docx`).** 12 Kapitel, fachlich die beste Datei des Pakets: korrekte Zuordnung der Blätter (Blatt 2 = Fenstermodell, **Blatt 3 = Solarstrahlungsmodell**, mit dem expliziten Hinweis, dass VDI 6007 keine Feuchtebilanz vorsieht), Tabelle der 7 Widerstände/2 Kapazitäten, Energiebilanzen der Knoten, Zustandsraumform, Diskretisierung über Matrixexponential mit Toleranzangabe ±0,1 K / ±1 W, Liste aller **12 VDI-Testfälle** (1–2 konvektiv, 3 zwei Außenwände, 4 Außentemperatur, 5–6 radiativ+solar, 7–9 Lastberechnung mit Sollwert, 10 Nachbarraum, 11 Kühldecke/FBH, 12 Lüftung; Testraum 52,5 m³ / 17,5 m², Auswertung Tag 1, 10, 60), Standardwerte (2,7 / 25 / 5 W(m²K)), Vergleich mit ISO 13790 (5R1C) und ISO 52016, Quellenliste (TEASER, AixLib, Buildings, EUReCA, Vivian 2017, Lauster 2014, Rouvel/Zimmermann 2004).

Zwei Vorbehalte: Das dort abgedruckte Codebeispiel (Kapitel 10) ist **eine ältere, schlechtere Fassung als die ZIP** (1/3-Näherung statt Kettenmatrix, `BuildInputVector` ausdrücklich „vereinfacht", unbenutzte `integral`-Matrix, Partikulärlösung mit unklarem Vorzeichen). Und die in 3.3 als „Referenzwerte VDI 6007 Testraum 1, Bauweise S" genannten Größen (R1,IW = 0,000596 K/W; C1,IW = 14.836.000 J/K; R1,AW = 0,00437 K/W; RRest,AW = 0,04277 K/W; C1,AW = 1.600.800 J/K) werden vom Code **nicht reproduziert**: die Nachrechnung des mitgelieferten Wandaufbaus ergibt R1,AW = 0,00734 (+68 %), C1,AW = 1.930.872 J/K (+21 %), RRest,AW = 0,169 (**+295 %**), R1,IW = 0,001293 (+117 %), C1,IW = 13.734.254 J/K (−7 %). Es gibt keinen automatisierten Abgleich dieser Werte.

## F. Anleitung

`VDI_6007_Gebaeudemodell_Anleitung.docx` ist eine allgemeine Programmieranleitung in 8 Kapiteln mit Python-Pseudocode (`class Raum`, `systemmatrizen_kontinuierlich`, `diskretisieren` mit `scipy.linalg.expm`, `simuliere`). Inhalt: thermisch-elektrische Analogie, Zustandsraum, A/B-Matrizen, Diskretisierung (Matrixexponential vs. Euler), Schichtreduktion, typische Parameterbereiche (C1 50–150 kJ/K leicht / 200–800 kJ/K schwer; R1 0,001–0,01 bzw. 0,005–0,05 K/W usw.), Erweiterungen, Best Practices, Fehlerkatalog.

**Sie beschreibt weder die Bedienung des Python-Skripts noch die des C#-Projekts** und ist auch nicht deren Spezifikation: Das dort dargestellte Netzwerk ist ein anderes (Kettentopologie T_außen–R3–C2–R2–C1–R1–T_raum plus Rw und Rv, mit **nicht-diagonaler** A-Matrix), die Bezeichner passen nicht. Die genannten „Testfälle TF 1–TF 5" sind **frei erfunden** und nicht die 12 Testfälle der Norm. **Blatt 3 wird falsch als „Modellierung von Heiz- und Kühlsystemen" bezeichnet** (richtig: Solarstrahlungsmodell) — direkter Widerspruch zur Doku-Docx. Es sind **keinerlei Validierungsergebnisse, Vergleichsrechnungen oder Grenzen** enthalten, nur die Forderung „Abweichung < 0,1 K".

Beide .docx sind laut Metadaten maschinell erzeugt (Autor „Un-named", Revision 1, 07.02.2026) und nicht redaktionell geprüft.

## G. Bewertung für die Integration in einen .NET-10-Rechenkern

**Übernehmbar (mit Umbenennung, ca. 30 % des Materials):** das Datenmodell Schicht → Bauteil → Klasse AW/IW → Zone (tragfähig, normnah, deutsch benennbar); die Kettenmatrix-Mechanik (γ = ξ(1+j), cosh/sinh, 2×2-Produkt, `System.Numerics.Complex`) als Baustein — die Parameterextraktion daraus muss neu; das Lösungsschema analytisches exp(A·Δt) + Γ für stückweise konstante Eingänge, ergänzt um Gauß-Seidel für die masselosen Knoten (richtiger Ansatz, nur derzeit falsch parametriert); die Doku-Docx als Anforderungs- und Prüfliste (12 Testfälle, Toleranzen, Standardwerte).

**Muss neu:** Systemmatrix A vollständig (Rückkopplungsterm, algebraische Elimination der Oberflächenknoten, exp(A·Δt) der vollen 2×2-Matrix); normkonforme Bauteilreduktion inkl. korrekter R1/RRest-Aufteilung und Außenübergang, verifiziert gegen die Referenzwerte der Norm; Regelung (iterative oder analytisch invertierte Lastbestimmung, Leistungsbegrenzung, Sollwertprofile, Totband); Fenster (U_w, g-Wert, Rahmenanteil, Fläche je Orientierung) und solare Einträge (Blatt 3 oder ein vorhandenes Strahlungsmodul), Erdreich, Nachbarzonen; Klimadaten-Schnittstelle auf festes 8760-Raster; Jahresorchestrierung mit Einschwingphase; Testprojekt mit den 12 VDI-Testfällen als Assert gegen ±0,1 K / ±1 W; Trennung Bibliothek/Konsole.

**Risiken:** fachlich hoch (Strukturfehler ~20 K); numerische Stabilität gering (analytisches exp(A·Δt) ist A-stabil; bei ξ ≳ 350 droht Overflow in cosh/sinh, für stündliche/tägliche Perioden und Bauteildicken unter ~1 m unkritisch); Rechenzeit kein Thema; Abhängigkeiten kein Thema (ausschließlich BCL, plattformfrei); Lizenz klärungsbedürftig (README-Formel „Lehr- und Studienzwecke", Herkunft und Urheber unklar — Code als Referenz lesen, Rechenkern neu schreiben; VDI 6007 ist ein kostenpflichtiges Regelwerk, Testfalldaten nicht ungeprüft ins Repository; Lizenzen von TEASER, AixLib, Buildings, EUReCA vor einer Übernahme prüfen).

**Empfehlung:** Doku-Docx als Spezifikation nehmen, Datenmodell und Lösungsschema übernehmen, Systemmatrix und Bauteilreduktion neu herleiten, und **zuerst** das Testprojekt mit den 12 Testfällen aufsetzen.
