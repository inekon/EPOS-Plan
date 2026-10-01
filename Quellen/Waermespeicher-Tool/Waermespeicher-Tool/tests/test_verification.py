"""Unabhängige Verifikation (Handrechnungen + Referenzfall) — Regressionsnetz.

Diese Datei ist bewusst **redundant** zu ``test_sizing.py`` & Co.: Sie wurde
in einer unabhängigen Prüfung des fertigen Tools geschrieben und hält die
Handrechnungen fest, mit denen die Kernformeln gegen Norm bzw. Quelle
abgeglichen wurden.

Grundsatz: Erwartungswerte werden **nicht** aus dem Produktivcode abgeleitet.
Wo möglich, steht hier eine unabhängige Zweitimplementierung (``math.erf``
statt der Reihenentwicklung in ``vendor/din4708.py``, naive Python-Schleifen
statt der vektorisierten Rekursionen).

Inhalt
------
1. ``energy_to_volume`` gegen die Handformel V = Q·1000/(c_w·ΔT).
2. DIN 4708 ``W_z``/``calc_GLF`` gegen eine erf-basierte Zweitimplementierung
   und gegen das lpagg-Original.
3. Taktung / EVU-Sperrzeit / Abtauung / Faustwert-TWW gegen Handrechnung.
4. Referenzfall „MFH-Bestand" (150 WE + Büro, ~611 MWh/a): Jahresarbeit,
   Volllaststunden, Plausibilitätsgrenzen der Empfehlungen.
5. Beispieldateien in ``examples/`` (Schema + Kennwerte).
"""
from __future__ import annotations

import json
import math
import os
import sys

import numpy as np
import pandas as pd
import pytest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, ROOT)

from vendor import din4708  # noqa: E402
from wsp import analysis, profiles_measured, sizing_buffer, sizing_dhw  # noqa: E402
from wsp import storage_sim  # noqa: E402
from wsp.models import (C_W, Building, BufferParams, BuildingCategory,  # noqa: E402
                        DHWParams, ProfileSet, WeatherConfig,
                        energy_to_volume, volume_to_energy,
                        round_to_standard_volume)
from wsp.profiles_synthetic import generate_profiles  # noqa: E402

EXAMPLES = os.path.join(ROOT, "examples")

#: Referenzfall, an die reale SWSG-Größenordnung angelehnt
REF_JAHR = 2025
REF_TRY = 12
REF_JAHRESARBEIT_KWH = 611_000.0
REF_N_WE = 150


# ====================================================== 1) Energie <-> Volumen

def test_energy_to_volume_handrechnung_10kwh_10k():
    """10 kWh bei ΔT = 10 K -> 859,8 l (V = Q·1000/(1,163·ΔT))."""
    v = energy_to_volume(10.0, 10.0)
    assert v == pytest.approx(10.0 * 1000.0 / (1.163 * 10.0), rel=1e-12)
    assert round(v, 1) == 859.8
    assert C_W == 1.163


@pytest.mark.parametrize("q,dt", [(1.0, 5.0), (10.0, 10.0), (35.2, 50.0),
                                  (400.0, 7.5)])
def test_energie_volumen_umkehrbar(q, dt):
    assert volume_to_energy(energy_to_volume(q, dt), dt) == pytest.approx(q)


def test_energy_to_volume_verweigert_ungueltiges_delta_t():
    for dt in (0.0, -5.0):
        with pytest.raises(ValueError):
            energy_to_volume(10.0, dt)


def test_round_to_standard_volume_rundet_auf():
    assert round_to_standard_volume(1) == 100
    assert round_to_standard_volume(801) == 1000
    assert round_to_standard_volume(1000) == 1000
    assert round_to_standard_volume(2656.5) == 3000


# ============================================================== 2) DIN 4708

def _K_erf(u: float) -> float:
    """Unabhängige K(u)-Implementierung: die Reihe in vendor/din4708.py ist
    die Maclaurin-Reihe von ``erf(u)``; oberhalb u = 1,81 setzt die DIN 1,0."""
    return 1.0 if u >= 1.81 else math.erf(u)


def _W_z_ref(N: float, z: float = 1 / 6) -> float:
    """Zweitimplementierung von W_z nach DIN 4708 (Wh)."""
    Wb = 5820.0
    f = (1.0 + math.sqrt(N)) / math.sqrt(N)
    return Wb * (N * _K_erf(z * 0.244 * f) + math.sqrt(N) * _K_erf(z * 3.599 * f))


@pytest.mark.parametrize("N", [1, 1.5, 2, 5, 10, 18.857142857142858, 30, 94.28571428571429, 100])
def test_wz_gegen_unabhaengige_erf_implementierung(N):
    assert din4708.W_z(N) == pytest.approx(_W_z_ref(N), rel=1e-9)


def test_wz_einheitswohnung_und_n30():
    """W_z(1) = 5,83 kWh (Norm-Bezugswert W_b = 5820 Wh); W_z(30) = 31,3 kWh."""
    assert din4708.W_z(1) == pytest.approx(5830.46, abs=0.01)
    assert din4708.W_z(1) / 1000.0 == pytest.approx(5.82, rel=0.01)
    assert din4708.W_z(30) == pytest.approx(31278.63, abs=0.01)


def test_w_p_und_w_1_handrechnung():
    """W_p(1) = 5820·1·(1+1)/1 = 11640 Wh; W_1 = W_z mit z = 1 h."""
    assert din4708.W_p(1) == pytest.approx(11640.0)
    assert din4708.W_1(1) == pytest.approx(din4708.W_z(1, z=1.0))


def test_glf_definition_und_plausibilitaet():
    """GLF(N) = W_z(1)/W_z(N); fällt monoton, GLF(30) ≈ 0,19."""
    assert din4708.calc_GLF(1) == pytest.approx(1.0)
    glf30 = din4708.calc_GLF(30)
    assert glf30 == pytest.approx(din4708.W_z(1) / din4708.W_z(30), rel=1e-12)
    # lpagg-Originalwert: 0,1864 — knapp unter dem oft zitierten Band 0,2..0,3
    assert 0.15 <= glf30 <= 0.30
    assert glf30 == pytest.approx(0.1864, abs=0.001)
    werte = [din4708.calc_GLF(n) for n in (1, 2, 5, 10, 30, 100)]
    assert all(a > b for a, b in zip(werte, werte[1:]))


def test_bedarfskennzahl_n_handrechnung():
    """N = WE · Personen/WE / 3,5 (Einheitswohnung p = 3,5, v = w = 1)."""
    assert sizing_dhw.bedarfskennzahl_n(30, 2.2) == pytest.approx(30 * 2.2 / 3.5)
    assert sizing_dhw.bedarfskennzahl_n(150, 2.2) == pytest.approx(150 * 2.2 / 3.5)
    assert sizing_dhw.bedarfskennzahl_n(0, 2.2) == 0.0
    assert sizing_dhw.bedarfskennzahl_n(10, 0.0) == 0.0


def test_din4708_volumen_handrechnung():
    """V_DIN = W_z(N)/(c_w·ΔT) / nutzbarer Anteil (ohne Zuschlag)."""
    p = DHWParams(t_speicher=60.0, t_kalt=10.0, nutzbarer_anteil=0.8)
    res = sizing_dhw.size_dhw(p, n_we=30, persons_per_we=2.2)
    w_z_kwh = _W_z_ref(30 * 2.2 / 3.5) / 1000.0
    v_hand = w_z_kwh * 1000.0 / (1.163 * 50.0) / 0.8
    assert res.w_z_kwh == pytest.approx(w_z_kwh, rel=1e-9)
    assert res.v_din4708_l == pytest.approx(v_hand, rel=1e-9)
    assert round(v_hand) == 515


# ============================================== 3) Faustwert / Puffer-Formeln

def test_faustwert_tww_30we_22p_60grad():
    """30 WE × 2,2 P = 66 P · 35 l/(P·d) = 2310 l; +15 % Zuschlag = 2656,5 l."""
    p = DHWParams(t_speicher=60.0, t_kalt=10.0, zuschlag=0.15)
    res = sizing_dhw.size_dhw(p, n_we=30, persons_per_we=2.2)
    v_hand = 30 * 2.2 * 35.0 * (60.0 - 10.0) / (60.0 - 10.0) * 1.15
    assert v_hand == pytest.approx(2656.5)
    assert res.v_faust_l == pytest.approx(2656.5, rel=1e-9)
    # Energieäquivalent: 2310 l bei ΔT 50 K = 134,3 kWh Tagesbedarf
    assert res.details["faustwert"]["tagesbedarf_kWh"] == pytest.approx(
        2310.0 * 1.163 * 50.0 / 1000.0, rel=1e-9)


def test_faustwert_energiegleiche_temperaturumrechnung():
    """Bei 50/10 °C statt 60/10 °C wächst V um 50/40."""
    v60 = sizing_dhw.size_dhw(DHWParams(t_speicher=60.0, zuschlag=0.0),
                              10, 2.5).v_faust_l
    v50 = sizing_dhw.size_dhw(DHWParams(t_speicher=50.0, zuschlag=0.0),
                              10, 2.5).v_faust_l
    assert v50 == pytest.approx(v60 * 50.0 / 40.0)


def test_taktung_handrechnung():
    """V = P_min · t_min/60 / (1,163·ΔT) · 1000 = 15·10/60 = 2,5 kWh -> 215 l."""
    p = BufferParams(p_wp_min=15.0, t_min_lauf_min=10.0, dt_puffer=10.0)
    v = sizing_buffer.v_taktung(p)
    assert v == pytest.approx(2.5 * 1000.0 / (1.163 * 10.0), rel=1e-12)
    assert round(v, 1) == 215.0
    # doppelte Mindestlaufzeit -> doppeltes Volumen
    p2 = BufferParams(p_wp_min=15.0, t_min_lauf_min=20.0, dt_puffer=10.0)
    assert sizing_buffer.v_taktung(p2) == pytest.approx(2.0 * v)


def test_abtauung_handrechnung():
    assert sizing_buffer.v_abtauung(
        BufferParams(p_wp=50.0, abtau_l_pro_kw=20.0)) == pytest.approx(1000.0)
    assert sizing_buffer.v_abtauung(
        BufferParams(p_wp=50.0, abtau_l_pro_kw=0.0)) == 0.0


def test_sperrzeit_handrechnung_mit_und_ohne_profil():
    """V = Q̄·t/(1,163·ΔT)·1000 mit Q̄ = max. rollierendes Mittel."""
    idx = pd.date_range("2025-01-01", periods=24 * 7, freq="h")
    last = pd.Series(np.linspace(10.0, 60.0, len(idx)), index=idx)
    p = BufferParams(p_wp=50.0, dt_puffer=10.0, sperrzeiten=[(11, 2)])

    q_hand = float(last.rolling(2).mean().max())
    v_hand = q_hand * 2.0 * 1000.0 / (1.163 * 10.0)
    v, info = sizing_buffer.v_sperrzeit(p, last, 1.0)
    assert v == pytest.approx(v_hand, rel=1e-12)
    assert info["fenster"][0]["q_mittel_kW"] == pytest.approx(q_hand)

    # ohne Profil: Ersatzwert P_WP
    v0, info0 = sizing_buffer.v_sperrzeit(p, None, 1.0)
    assert v0 == pytest.approx(50.0 * 2.0 * 1000.0 / (1.163 * 10.0))
    assert "Ersatzwert" in info0["fenster"][0]["quelle"]


def test_sperrzeit_ist_unabhaengig_von_der_startstunde():
    """Dokumentiertes (konservatives) Verhalten: Q̄ ist das Maximum über ALLE
    rollierenden Fenster der Länge ``dauer_h`` — die Startstunde geht nicht
    ein. Zwei gleich lange Fenster liefern deshalb denselben Wert."""
    idx = pd.date_range("2025-01-01", periods=24 * 14, freq="h")
    last = pd.Series(20.0 + 15.0 * np.sin(np.arange(len(idx)) / 3.7) ** 2,
                     index=idx)
    p = BufferParams(dt_puffer=10.0, sperrzeiten=[(11, 2), (17, 2)])
    _, info = sizing_buffer.v_sperrzeit(p, last, 1.0)
    a, b = info["fenster"]
    assert a["q_mittel_kW"] == pytest.approx(b["q_mittel_kW"])


def test_availability_mask_ueber_mitternacht():
    idx = pd.date_range("2025-01-01", periods=48, freq="h")
    maske = sizing_buffer.availability_mask(idx, [(22, 4)])
    gesperrt = sorted({int(t.hour) for t, m in zip(idx, maske) if m == 0.0})
    assert gesperrt == [0, 1, 22, 23]
    # Dauer >= 24 h sperrt alles, Dauer <= 0 wird ignoriert
    assert sizing_buffer.availability_mask(idx, [(0, 24)]).sum() == 0.0
    assert sizing_buffer.availability_mask(idx, [(5, -1)]).sum() == len(idx)
    assert sizing_buffer.availability_mask(idx, []).sum() == len(idx)


# ================================================ 4) Simulation gegen Referenz

def test_storage_sim_gegen_naive_schleife():
    """Vektorisierte Bilanz == naive Python-Schleife (inkl. Sperrzeitmaske)."""
    rng = np.random.default_rng(11)
    idx = pd.date_range("2025-01-01", periods=500, freq="h")
    last = pd.Series(rng.uniform(0.0, 40.0, len(idx)), index=idx)
    maske = sizing_buffer.availability_mask(idx, [(11, 2), (17, 2)])
    p_gen, cap = 25.0, 120.0

    s, unter, n_unter = cap, 0.0, 0
    for i in range(len(idx)):
        roh = s + (p_gen * float(maske.iloc[i]) - float(last.iloc[i])) * 1.0
        if roh > cap:
            s = cap
        elif roh < 0.0:
            unter += -roh
            n_unter += 1
            s = 0.0
        else:
            s = roh
    sim = storage_sim.simulate(last, p_gen, cap, availability=maske)
    assert sim.unterdeckung_kwh == pytest.approx(unter, rel=1e-9)
    assert sim.unterdeckung_h == n_unter
    assert sim.deckungsgrad == pytest.approx(1.0 - unter / float(last.sum()))


def test_max_soc_defizit_gegen_naive_lindley():
    rng = np.random.default_rng(3)
    idx = pd.date_range("2025-01-01", periods=400, freq="h")
    last = pd.Series(rng.uniform(0.0, 30.0, len(idx)), index=idx)
    for p_lade in (0.0, 5.0, 15.0, 40.0):
        d, dmax = 0.0, 0.0
        for x in last.to_numpy():
            d = max(0.0, d + (x - p_lade))
            dmax = max(dmax, d)
        assert sizing_dhw.max_soc_defizit(last, p_lade)[0] == pytest.approx(dmax)


def test_find_min_capacity_ist_minimal():
    """Gefundene Kapazität erfüllt das Ziel, 10 % weniger nicht mehr."""
    idx = pd.date_range("2025-01-01", periods=600, freq="h")
    last = pd.Series(20.0 + 18.0 * np.sin(np.arange(600) / 4.1) ** 2, index=idx)
    res = storage_sim.find_min_capacity(last, 28.0)
    assert res.ok(1.0)
    assert not storage_sim.simulate(last, 28.0, res.capacity_kwh * 0.9).ok(1.0)


# ================================================== 5) Referenzfall MFH-Bestand

@pytest.fixture(scope="module")
def referenzfall() -> ProfileSet:
    """150 WE MFH (5 × 30 WE) + Bürogebäude, TRY 12, ~611 MWh/a."""
    buildings = [
        Building(name="MFH-Bestand", category=BuildingCategory.MFH, n_we=30,
                 n_persons=2.2, q_heiz_a=88000.0, q_tww_a=22000.0, copies=5,
                 sigma=4.0),
        Building(name="Buero", category=BuildingCategory.GHD,
                 q_heiz_a=61000.0, bdew_branch="gko", tww_share=0.08),
    ]
    return generate_profiles(buildings, WeatherConfig(REF_TRY, REF_JAHR),
                             "h", seed=42)


def test_referenzfall_jahresarbeit_und_spitze(referenzfall):
    k = analysis.kennzahlen(referenzfall)
    assert k["q_total_kwh"] == pytest.approx(REF_JAHRESARBEIT_KWH, rel=0.01)
    assert len(referenzfall.df) == 8760
    # TWW-Anteil einer MFH-Bestandsanlage: 15..25 %
    assert 0.15 <= k["anteil_tww"] <= 0.25
    # Synthetische Gleichzeitigkeitsspitze deutlich über der Messung (125 kW)
    assert 250.0 <= k["p_max_kw"] <= 350.0
    # ... und entsprechend niedrigere Volllaststunden als die Messung (~4900 h)
    assert 1700.0 <= k["vollaststunden_h"] <= 2400.0
    # Der reale Messwert 125 kW liegt in der Größenordnung des 90-%-Punkts
    assert 100.0 <= k["p_90_prozent_kw"] <= 160.0


def test_referenzfall_glf_kennzahl_wird_berechnet(referenzfall):
    """meta['n_we'] muss gesetzt sein, sonst fehlt die GLF aus DESIGN.md §5."""
    assert referenzfall.meta.get("n_we") == REF_N_WE
    assert referenzfall.meta.get("n_personen") == pytest.approx(REF_N_WE * 2.2)
    k = analysis.kennzahlen(referenzfall)
    assert k["glf_din4708"] == pytest.approx(din4708.calc_GLF(REF_N_WE))
    assert 0.0 < k["glf_din4708"] < 0.2


def test_referenzfall_tww_empfehlung_und_warnung(referenzfall):
    """DIN 4708 und profilbasiert liegen MFH-üblich; der Faustwert nicht —
    dann muss die Auslegung eine Abweichungswarnung liefern."""
    res = sizing_dhw.size_dhw(DHWParams(p_lade=45.0, zirkulation_kw=4.0),
                              n_we=REF_N_WE, persons_per_we=2.2,
                              profile_tww=referenzfall.df["Q_tww"], dt_h=1.0)
    assert res.n_bedarf == pytest.approx(REF_N_WE * 2.2 / 3.5)
    assert 1000.0 <= res.v_din4708_l <= 5000.0
    assert 500.0 <= res.v_profil_l <= 5000.0
    # Faustwert skaliert linear mit 330 Personen -> > 10 m³, unrealistisch
    assert res.v_faust_l > 10000.0
    assert res.details["vergleich_spreizung"] > 3.0
    assert any("Faustwert" in w for w in res.details["warnungen"])


@pytest.mark.parametrize("p_wp", [80.0, 100.0])
def test_referenzfall_puffer_80_und_100_kw(referenzfall, p_wp):
    params = BufferParams(p_wp=p_wp, p_wp_min=0.3 * p_wp, dt_puffer=10.0,
                          t_min_lauf_min=10.0, abtau_l_pro_kw=20.0,
                          sperrzeiten=[(11, 2), (17, 2)], ziel_deckung=1.0)
    res = sizing_buffer.size_buffer(params, referenzfall.df["Q_heiz"], 1.0)

    # Abtauung und Taktung sind Handrechnungen und bleiben klein
    assert res.v_abtau_l == pytest.approx(20.0 * p_wp)
    assert res.v_takt_l == pytest.approx(
        0.3 * p_wp * 10.0 / 60.0 * 1000.0 / (1.163 * 10.0))

    if p_wp == 80.0:
        # Erzeuger zu klein: Simulation erreicht Ziel-Deckung nicht und geht
        # NICHT in die Empfehlung ein -> Sperrzeit ist maßgebend.
        assert res.details["simulation"]["gueltig"] is False
        assert res.massgebend == sizing_buffer.KRIT_SPERR
    else:
        # 100 kW schafft die Jahresarbeit, aber nur mit Saisonalspeicher.
        assert res.details["simulation"]["gueltig"] is True
        assert res.massgebend == sizing_buffer.KRIT_SIM
        assert res.details["praxis_plausibel"] is False
        assert any("Obergrenze" in w for w in res.details["warnungen"])


def test_puffer_praxisgrenze_warnt_nicht_bei_normalfall():
    """Gegenprobe: ausreichend dimensionierter Erzeuger -> keine Warnung."""
    idx = pd.date_range("2025-01-01", periods=24 * 30, freq="h")
    last = pd.Series(20.0 + 10.0 * np.sin(np.arange(len(idx)) / 3.9) ** 2,
                     index=idx)
    res = sizing_buffer.size_buffer(
        BufferParams(p_wp=40.0, p_wp_min=12.0, dt_puffer=10.0,
                     abtau_l_pro_kw=20.0, sperrzeiten=[(11, 2)]), last, 1.0)
    assert res.details["praxis_plausibel"] is True
    assert res.v_empfehlung_l <= sizing_buffer.PRAXIS_GRENZE_L
    assert not any("Obergrenze" in w for w in res.details["warnungen"])


def test_sperrzeit_und_simulation_konvergieren_bei_starkem_erzeuger(referenzfall):
    """Deckt P_gen die Spitzenlast, bleibt nur die Sperrzeit als Ursache —
    Simulation und Sperrzeit-Kriterium müssen dann dieselbe Größenordnung
    liefern (Quervalidierung zweier unabhängiger Rechenwege)."""
    last = referenzfall.df["Q_heiz"]
    p_gen = float(last.max()) * 1.05
    params = BufferParams(p_wp=p_gen, p_wp_min=0.3 * p_gen, dt_puffer=10.0,
                          abtau_l_pro_kw=0.0, sperrzeiten=[(11, 2), (17, 2)])
    res = sizing_buffer.size_buffer(params, last, 1.0)
    assert res.details["simulation"]["gueltig"] is True
    assert res.v_sim_l == pytest.approx(res.v_sperr_l, rel=0.25)


# ================================================== 6) Beispieldateien

def test_beispiel_projekt_json_passt_zum_app_schema():
    pfad = os.path.join(EXAMPLES, "beispiel_projekt.json")
    assert os.path.exists(pfad), "examples/beispiel_projekt.json fehlt"
    with open(pfad, encoding="utf-8") as f:
        daten = json.load(f)

    # von app.projekt_anwenden gelesene Blöcke
    for key in ("buildings", "weather", "dhw", "buffer", "projekt", "ui"):
        assert key in daten, f"Schlüssel '{key}' fehlt im Projekt-JSON"
    assert daten["projekt"].get("name")
    assert daten["weather"]["year"] == REF_JAHR
    assert 1 <= daten["weather"]["try_region"] <= 15

    felder = {"name", "category", "n_we", "n_persons", "q_heiz_a", "q_tww_a",
              "copies", "sigma", "bdew_branch", "tww_share"}
    objekte = []
    for g in daten["buildings"]:
        assert felder.issuperset(g.keys()), f"unbekannte Felder: {set(g) - felder}"
        g = dict(g)
        g["category"] = BuildingCategory(str(g["category"]).upper())
        objekte.append(Building(**g))

    kategorien = {b.category for b in objekte}
    assert BuildingCategory.MFH in kategorien and BuildingCategory.GHD in kategorien
    assert sum(b.n_we * b.copies for b in objekte
               if b.category != BuildingCategory.GHD) == REF_N_WE

    # Sperrzeiten müssen als (start, dauer)-Paare lesbar sein
    for start, dauer in daten["buffer"]["sperrzeiten"]:
        assert 0 <= float(start) < 24 and float(dauer) > 0


def test_beispiel_projekt_json_erzeugt_referenzprofil():
    with open(os.path.join(EXAMPLES, "beispiel_projekt.json"),
              encoding="utf-8") as f:
        daten = json.load(f)
    objekte = []
    for g in daten["buildings"]:
        g = dict(g)
        g["category"] = BuildingCategory(str(g["category"]).upper())
        objekte.append(Building(**g))
    ps = generate_profiles(objekte,
                           WeatherConfig(daten["weather"]["try_region"],
                                         daten["weather"]["year"]), "h", 42)
    assert ps.annual_sums()["Q_heiz"] + ps.annual_sums()["Q_tww"] == \
        pytest.approx(REF_JAHRESARBEIT_KWH, rel=0.01)


def test_beispiel_lastgang_csv_importiert_sauber():
    pfad = os.path.join(EXAMPLES, "beispiel_lastgang.csv")
    assert os.path.exists(pfad), "examples/beispiel_lastgang.csv fehlt"
    with open(pfad, encoding="utf-8") as f:
        kopf = f.readline().strip()
    assert kopf == "Datum;Lastgang Wärmebedarf [kW]"

    ps = profiles_measured.import_lastgang(
        pfad, "Datum", "Lastgang Wärmebedarf [kW]",
        value_unit="kW", decimal=",", sep=";")
    assert ps.meta["n_stunden"] == 8760
    assert ps.meta["luecken_anzahl"] == 0
    assert ps.meta["warnungen"] == []
    assert ps.meta["jahressumme_kwh"] == pytest.approx(REF_JAHRESARBEIT_KWH,
                                                       rel=0.01)
    # gemessene Charakteristik: Spitze ~125 kW, ~4900 Volllaststunden
    assert 115.0 <= ps.meta["p_max_kw"] <= 135.0
    vlh = ps.meta["jahressumme_kwh"] / ps.meta["p_max_kw"]
    assert 4500.0 <= vlh <= 5300.0

    geteilt = profiles_measured.split_tww_sommer(ps)
    anteil = geteilt.meta["tww_split"]["anteil_tww"]
    assert 0.12 <= anteil <= 0.30
    assert geteilt.df["Q_heiz"].sum() + geteilt.df["Q_tww"].sum() == \
        pytest.approx(ps.df.sum().sum(), rel=1e-9)


def test_examples_readme_vorhanden():
    assert os.path.exists(os.path.join(EXAMPLES, "README.md"))
