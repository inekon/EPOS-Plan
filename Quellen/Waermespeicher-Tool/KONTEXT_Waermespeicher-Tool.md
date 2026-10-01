# Projekt-Kontext — Wärmespeicher-Tool

Stand: 2026-07-28 · Session: Cowork (Claude) · Arbeitsweise: Design/Konzept mit Fable 5, Implementierung/Agenten mit Opus 5

## Was gebaut wurde

Streamlit-Auslegungstool für **TWW-Speicher** und **Heizungs-Pufferspeicher** (WP-Anlagen,
EFH/MFH/GHD), Ordner `Waermespeicher-Tool/`. Lastprofile wahlweise **synthetisch**
(VDI 4655 via `demandlib.vdi`, BDEW-GHD via `demandlib.bdew`, DWD-TRY-2010-Wetter aus lpagg)
oder **gemessen** (CSV/XLSX-Import mit Auflösungs-/Einheitserkennung, Lückenreport,
TWW-Split per Sommer-Baseline). Excel-Berichtsexport (7 Blätter, native Charts).
Start unter Windows: `start_tool.bat` (legt .venv an). 146 pytest-Tests, alle grün.

## Quellen (vom Nutzer vorgegeben)

- https://github.com/jnettels/lpagg — vendored: `vendor/din4708.py` (byte-identisch),
  `vendor/try_weather/` (15 TRY-Regionen). lpagg selbst ist pip-seitig nicht installierbar
  (veraltetes setup.py, `install_layout`-Fehler) → deshalb Vendoring.
- https://github.com/Pyosch/demandlib bzw. oemof/demandlib — als PyPI-Paket
  `demandlib==0.2.2` eingebunden (enthält `vdi`-Modul; lpagg nutzt mit
  `use_demandlib=True` dieselbe Implementierung).

## Wichtigste fachliche Entscheidungen

1. **Zeitreihen-Konvention**: kW je Zeitschritt, DatetimeIndex tz-naiv, c_w = 1,163 Wh/(l·K);
   `V[l] = Q[kWh]·1000/(1,163·ΔT)`.
2. **TWW**: drei Verfahren (DIN 4708 mit N = WE·Pers/3,5 vereinfacht — v/w-Wertigkeiten = 1;
   profilbasiert = max. SOC-Defizit bei konstanter Ladeleistung/Lindley-Rekursion;
   Faustwert 35 l/(P·d)@60 °C). Empfehlung = Max der anwendbaren Verfahren, gerundet.
   Faustwert bekommt ab 3× Spreizung eine Warnung (dominiert sonst stumm bei großen Anlagen).
3. **Puffer**: vier Kriterien (Abtau 20 l/kW; Taktung P_min·t_min/(1,163·ΔT);
   Sperrzeit über max. rollierendes Mittel des GESAMTEN Lastgangs — konservativ;
   Durchlauf-Simulation + Bisektion der Mindestkapazität). Praxisgrenze 100 m³:
   größere Ergebnisse werden als „nicht baubar" markiert (Auswege: Bivalenz,
   Teildeckung, größerer Erzeuger).
4. **Zweipunkt-Betriebssimulation** (`storage_sim.simulate_zweipunkt`): repliziert die
   SWSG-Excel-Logik exakt — Laden bis voll (P = min(Last + (C−SOC)/dt, P_max)),
   dann Erzeuger AUS, Entladen bis Min-Füllstand (20 % von C), Umschalten im SELBEN
   Schritt, SOC-Floor = Min-Füllstand (Unterdeckung dagegen gemessen).
   Liefert `ladezyklen` (Taktung).
5. **Schaltjahre** sind für VDI-4655-Profile gesperrt (demandlib-0.2.2-Grenze).
6. models.py ist der API-Vertrag; SimResult wurde nur additiv erweitert (`ladezyklen`).

## Validierung (Kernergebnis)

Parity-Check der Zweipunkt-Logik gegen `Lastgangauswertung_Heizung_SWSG-2026-05-29-V2.xlsm`
(direkt auf den Originaldaten): **0 Abweichungen über 8.760 h** — Unterdeckung
67,8598 kWh / 11 h, 911 Ladezyklen; Parameter: C = 79,080225 kWh (2.000 l @ ΔT 35 K),
MINF 20 %, P_max = 100,0808 kW, Booster-Entzug (3,077 kW) als Zusatzlast in Trigger UND
Bilanz. Excel-Eigenheiten: Spalte M („Benötigte Leistung") zeigt P OHNE Booster an,
die Bilanz rechnet aber MIT; SOC-Floor ist der Min-Füllstand, nicht 0.

Referenzfall Synthetik vs. Messung (611 MWh/a): synthetische Spitze ~298 kW vs.
gemessene 125 kW (Faktor 2,4) — Messspitze ≈ P-90-%-Punkt der synthetischen JDL.
Konsequenz: Erzeuger nach JDL/Bivalenzpunkt auslegen, nicht nach Synthetik-Spitze.

## Bekannte Grenzen / offene Punkte

- DIN-4708-N ohne Zapfstellen-Wertigkeiten (DIN 4708-2-Tabellen) — mögliche Ausbaustufe.
- Kein Abgleich mit Hersteller-NL-Zahlen (Logalux-Katalog) — nur Texthinweis.
- Faustwert-Parameter (30–45 l/(P·d), Ladezyklen-Divisor) nicht parametrierbar.
- Speichermodelle ohne Schichtung/Verluste (reine Energiebilanz).
- `vollaststunden_h` (Tippfehler im Kennzahlen-Key) ist API — nicht umbenennen.

## Dateien

- `Waermespeicher-Tool.zip` — komplettes Tool (aus dem Chat speichern und entpacken)
- `Dokumentation_Waermespeicher-Tool.docx` — Beschreibung, Bedienung, Methodik (Teil A/B/C)
- im Tool: `DESIGN.md` (Konzept), `README.md` (Kurzanleitung), `examples/` (Beispielprojekt
  + Beispiel-Lastgang 611 MWh/Spitze 125 kW), `tests/` (146 Tests)
