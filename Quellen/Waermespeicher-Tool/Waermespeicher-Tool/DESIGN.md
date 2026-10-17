# Wärmespeicher-Tool — Design & Berechnungskonzept

Auslegungstool für **Trinkwarmwasser-Speicher** und **Heizungs-Pufferspeicher** in
Wärmepumpen-Anlagen (EFH/MFH/Wohnungswirtschaft + GHD).

## Quellen

- **lpagg** (J. Nettelstroth / Joris Zimmermann, MIT) — https://github.com/jnettels/lpagg
  → vendored: `vendor/din4708.py` (DIN 4708 NL-Zahl/Gleichzeitigkeit), `vendor/try_weather/` (DWD TRY2010, 15 Klimaregionen)
- **demandlib** (oemof, MIT) — https://github.com/oemof/demandlib (PyPI, `pip install demandlib`)
  → `demandlib.vdi` (VDI 4655 Referenzlastprofile Wohnen), `demandlib.bdew` (BDEW-Profile GHD).
  Hinweis: lpagg selbst nutzt mit `use_demandlib=True` genau diese Implementierung.
- DIN V 18599-10 (Nutzenergiebedarf TWW, Defaultwerte), VDI 6002, DIN 4708,
  Buderus Logalux Planungsunterlage (NL-Zahlen marktüblicher Speicher),
  SWSG-Lastgangauswertung (Referenz für Füllstands-/Unterdeckungslogik).

## Zeitreihen-Konvention

- `pandas.DataFrame`, `DatetimeIndex` (lokale Zeit, tz-naiv), Auflösung **1 h** (synthetisch intern 15 min → resample auf 1 h möglich; Auflösung wird in `ProfileSet.resolution` geführt).
- Spalten: `Q_heiz` [kW], `Q_tww` [kW] (thermische Leistung im Intervall), optional `P_el` [kW].
- Energie je Schritt = Leistung × dt[h]. Jahressummen in kWh/a.
- Konstante: **c_w = 1.163 Wh/(l·K)**; Volumen V[l] = Q[kWh]·1000 / (1.163 · ΔT[K]).

## Module (Paket `wsp/`) — Datei-Ownership für Agenten

| Datei | Inhalt | Owner |
|---|---|---|
| `models.py` | Dataclasses/Enums (FERTIG — nicht ändern, nur nutzen) | Fable |
| `profiles_synthetic.py` | VDI 4655 (Wohnen) + BDEW (GHD) → ProfileSet | Agent 1 |
| `weather.py` | TRY-Region wählen/laden (`vendor/try_weather`), `demandlib.vdi.read_dwd_weather_file` | Agent 1 |
| `profiles_measured.py` | Import gemessener Lastgänge (CSV/XLSX), kW/kWh-Erkennung, Lückenreport, Heizung/TWW-Split via Sommer-Baseline | Agent 2 |
| `analysis.py` | JDL, Spitzenwerte-Top-N, Tagesprofile (Monat × Werktag/Sa/So), Wochenlastgang, Kennzahlen (Volllaststunden, GLF) | Agent 2 |
| `storage_sim.py` | Generische Speicherbilanz-Simulation + Kapazitäts-Binärsuche | Agent 2 |
| `sizing_dhw.py` | TWW-Speicher: DIN 4708 (NL), profilbasiert, Faustwerte 18599-10 | Agent 3 |
| `sizing_buffer.py` | Puffer: Abtauung, Taktung, Sperrzeit, Lastgang-Simulation | Agent 3 |
| `export_excel.py` | Excel-Bericht (openpyxl, native Charts) | Agent 4 |
| `app.py` (Root) | Streamlit-UI | Agent 4 |

Vendored `vendor/din4708.py`: Funktionen `W_z(N)` (Wärmebedarf N Referenzwohnungen in Wh,
Zapfdauer z), `calc_GLF(N)`. Import über `from vendor import din4708`.

## Berechnungsmethodik

### 1. Synthetische Profile
- **Wohnen** (`EFH`/`MFH`): `demandlib.vdi.Region(year, Climate mit TRY-Wetter, houses=[...])`.
  Haus-Dict: `name, house_type ('EFH'|'MFH'), N_Pers, N_WE, Q_Heiz_a, Q_TWW_a, W_a, copies, sigma`
  (sigma = Gleichzeitigkeits-Zeitversatz in Minuten, Standard 4 für MFH-Aggregation).
  Q_TWW_a Default, wenn unbekannt: **500 kWh/(Person·a)** (VDI 4655) bzw. 11 kWh/(m²NGF·a) (DIN V 18599-10 Wohnen).
- **GHD**: `demandlib.bdew.HeatBuilding` je Branche (ghd, gha, gbd, gko, …) mit Jahreswärmebedarf;
  TWW-Anteil über BDEW-Warmwasserprofil (`ww_incl`-Logik der Lib; wo nicht trennbar: konfigurierbarer TWW-Anteil in %).
- Wetter: TRY2010-Region 1–15 (Auswahl per Dropdown, Default 12 Süddeutschland / 4 Potsdam), Jahr wählbar.

### 2. Gemessene Lastgänge
- CSV/XLSX-Upload, Spalten-Mapping (Zeitstempel + Leistung kW **oder** Energie kWh/Intervall), Auflösung automatisch erkennen, Resampling auf 1 h, Lücken-/Ausreißerreport.
- Wenn nur Gesamtwärme vorliegt: **Sommer-Baseline-Split** — mittlere Last Jun–Aug = TWW + Zirkulation; dieses Band wird ganzjährig als `Q_tww` angesetzt (saisonale Korrektur ±, Faktor konfigurierbar), Rest = `Q_heiz`.

### 3. TWW-Speicher (`sizing_dhw.py`) — drei Verfahren, Ergebnis als Vergleich
1. **DIN 4708**: N (Bedarfskennzahl) aus Wohnungsmix (WE × Belegung p; Referenzwohnung p=3,5 → N ≈ Σ(WE·p·v·w)/(3,5·5820 Wh) vereinfacht über `din4708.W_z`); Ausgabe **N_L-Bedarf**, W_z [kWh], Hinweis: Speicher mit N_L ≥ N_L,Bedarf wählen (z. B. Logalux-Reihe).
2. **Profilbasiert** (WP-gerecht): TWW-Profil q(t) [kW], Ladeleistung P_L [kW] (WP im TWW-Betrieb):
   `V_nutz = max_t Defizit(t) · 1000/(1.163·ΔT_nutz)` mit Defizit = SOC-Simulation (P_L konstant, Speicher puffert Zapfspitzen). ΔT_nutz = T_speicher − T_kaltwasser (Default 60 − 10 = 50 K, nutzbarer Anteil 80 %).
3. **Faustwerte**: DIN V 18599-10 / VDI-Praxis: Vd ≈ 30–45 l/(Person·d) bei 60 °C; Speicher ≈ 1,0–1,5 × Tagesbedarf⁄Ladezyklen.
- Zuschläge: Bereitschafts-/Totvolumen (+10–20 %), Zirkulationsverluste [kW] ganzjährig, Legionellen-Hinweis (>400 l / >3 l/kW Rohrleitung → 60 °C bzw. FriWa-Empfehlung).

### 4. Pufferspeicher (`sizing_buffer.py`) — vier Ansätze, Ergebnis als Vergleich
1. **Abtauung** (Luft/Wasser-WP): V ≥ v_spez · P_WP, v_spez Default **20 l/kW** (15–35).
2. **Taktung**: V ≥ P_WP,min · t_min / (1.163 · ΔT_puffer) · 1000, t_min Default 10 min Mindestlaufzeit, ΔT_puffer Default 10 K (5–15).
3. **EVU-Sperrzeit**: V = Q̄_sperr · t_sperr · 1000/(1.163·ΔT); Q̄_sperr = max. mittlere Last im ungünstigsten Sperrfenster aus dem Lastgang (Standard-Sperrfenster konfigurierbar, z. B. 3×2 h).
4. **Lastgang-Simulation** (SWSG-Logik): Erzeugerleistung P_gen < Spitzenlast wählen (mono-/bivalent) →
   `SOC(t+1) = clip(SOC(t) + (min(P_gen·verfügbar(t), Q_last(t)+Laderate) − Q_last(t))·dt, 0, C)`;
   Sperrzeiten als Verfügbarkeitsmaske; Ausgabe Unterdeckung [h, kWh]; **Binärsuche** über C bis Unterdeckung = 0 (oder Ziel-Deckungsgrad) → nötige Kapazität [kWh] → Volumen [l] über ΔT.
- Ergebnis: Max-Kriterium + Empfehlung (gerundet auf marktübliche Größen: 100/200/300/500/800/1000/1500/2000/3000/5000 l).

**Zweiter Simulationsmodus — Zweipunkt-Betrieb** (`storage_sim.simulate_zweipunkt`,
Wrapper `sizing_buffer.betriebssimulation`): repliziert die validierte Logik der
SWSG-Referenz-Excel. Zwei Zustände statt Durchlauf: *Laden* (Erzeuger deckt Last
und lädt bis voll, `P = min(Q_last + (C−SOC)/dt, P_max)`) und *Entladen*
(Erzeuger aus, Last allein aus dem Speicher). Umschaltung auf Laden im **selben**
Zeitschritt, sobald der Mindestfüllstand `MINF = min_soc_frac·C` (Default 20 %)
unterschritten würde; Umschaltung auf Entladen, sobald `SOC = C`. Unterdeckung
nur im Ladebetrieb (SOC wird auf MINF gehalten). Zusatzausgabe: `Laden` (0/1) je
Schritt und `SimResult.ladezyklen` (Wechsel Entladen→Laden) → **Taktung**.
Abgrenzung: `size_buffer` beantwortet „wie groß muss der Speicher sein",
`betriebssimulation` „wie verhält sich ein gewählter Speicher im Betrieb".

### 5. Analyse-Kennzahlen
JDL (sortiert), Top-1000-Spitzen, Tagesprofile je Monat/Tagtyp, Wochenlastgang, Volllaststunden = Q_a/P_max, GLF nach DIN 4708 (`calc_GLF`), Bivalenzpunkt-Hilfe (P bei x % JDL).

## UI (Streamlit, `app.py`)

Sidebar: Projektname, Projekt speichern/laden (JSON, alle Eingaben). Tabs:
1. **Gebäude & Profil** — Gebäudetabelle (`st.data_editor`), TRY-Region, Jahr | ODER Messdaten-Upload mit Mapping. Button „Profil erzeugen", Plots (Jahresgang, Beispielwoche), Jahressummen-Check.
2. **Analyse** — JDL, Tagesprofile, Kennzahlen (plotly).
3. **TWW-Speicher** — Parameter-Formular, drei Verfahren nebeneinander, Empfehlung.
4. **Pufferspeicher** — Parameter, vier Ansätze, SOC-Plot der Simulation, Empfehlung.
5. **Export** — Excel-Bericht herunterladen.

## Excel-Export (`export_excel.py`)
Blätter: `Eingaben`, `Lastgang` (stündlich, Q_heiz/Q_tww), `JDL` (+Chart), `Tagesprofile` (+Chart), `TWW-Auslegung`, `Puffer-Auslegung`, `Kennzahlen`. openpyxl, native Excel-Charts (kein Bild-Embed).

## Deployment (Windows, ohne conda)
`requirements.txt`: streamlit, pandas, numpy, demandlib==0.2.2, openpyxl, plotly, holidays.
`start_tool.bat`: legt beim ersten Start venv an (`py -m venv .venv`), installiert requirements, startet `streamlit run app.py`. README.md mit Kurzanleitung.

## Tests (`tests/`)
- Synthetik: MFH 30 WE, Q_heiz 150 MWh, Q_tww 22 MWh → Jahressummen stimmen (±1 %), Spitze plausibel.
- DIN 4708: W_z(1) ≈ 5,82 kWh; GLF fällt monoton mit N.
- storage_sim: synthetischer Rechteck-Lastfall mit analytisch bekannter Kapazität.
- Puffer-Sim gegen SWSG-Kennwerte (Spitze 125 kW): P_gen = 80 kW → nötige Kapazität in plausibler Größenordnung.
