"""Tests für wsp/sizing_dhw.py und wsp/sizing_buffer.py.

Grundsatz: Erwartungswerte werden **nicht** aus dem Produktivcode abgeleitet,
sondern in dieser Datei mit einer minimalen Referenz-SOC-Schleife bzw. per
Handrechnung gebildet. ``wsp.storage_sim`` wird nicht gemockt — Tests, die die
echte Implementierung brauchen, überspringen sich selbst, solange dort noch
``NotImplementedError`` fliegt.
"""
from __future__ import annotations

import os
import sys

import numpy as np
import pandas as pd
import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from vendor import din4708  # noqa: E402
from wsp import storage_sim  # noqa: E402
from wsp.models import (C_W, BufferParams, DHWParams,  # noqa: E402
                        round_to_standard_volume)
from wsp.sizing_buffer import (availability_mask, max_mittelleistung,  # noqa: E402
                               size_buffer)
from wsp.sizing_dhw import size_dhw  # noqa: E402


# --------------------------------------------------------------------- Helfer

def ref_max_defizit(load_kw, p_gen_kw, dt_h=1.0, availability=None) -> float:
    """Minimale Referenz-SOC-Schleife (bewusst naiv, rein in Python).

    Speicher startet voll; Defizit = entnommene Energie. Ohne Kapazitätsgrenze
    ist max(Defizit) genau die Kapazität, die der Speicher braucht, damit er
    nie leerläuft.
    """
    defizit = 0.0
    d_max = 0.0
    for i, last in enumerate(np.asarray(load_kw, dtype=float)):
        verf = 1.0 if availability is None else float(np.asarray(availability)[i])
        p = float(p_gen_kw) * verf
        defizit = max(0.0, defizit + (last - p) * dt_h)
        d_max = max(d_max, defizit)
    return d_max


def ref_volumen(q_kwh: float, delta_t: float) -> float:
    """Referenz-Umrechnung Energie -> Volumen (Handformel, c_w = 1.163)."""
    return q_kwh * 1000.0 / (1.163 * delta_t)


def zapfprofil(peak_kw=40.0, start_h=6, dauer_h=2, basis_kw=0.0,
               tage=365, jahr=2026) -> pd.Series:
    """Stündliches Profil: jeden Tag ``dauer_h`` Stunden Spitze ab ``start_h``."""
    idx = pd.date_range(f"{jahr}-01-01", periods=24 * tage, freq="h")
    werte = np.full(len(idx), float(basis_kw))
    stunde = idx.hour
    werte[(stunde >= start_h) & (stunde < start_h + dauer_h)] = peak_kw
    return pd.Series(werte, index=idx, name="Q_tww")


def storage_sim_verfuegbar() -> bool:
    """True, wenn storage_sim.find_min_capacity real implementiert ist."""
    idx = pd.date_range("2026-01-01", periods=24, freq="h")
    last = pd.Series(np.full(24, 10.0), index=idx)
    try:
        storage_sim.find_min_capacity(last, 10.0, dt_h=1.0)
    except NotImplementedError:
        return False
    except Exception:  # andere Fehler sollen sichtbar bleiben
        raise
    return True


requires_storage_sim = pytest.mark.skipif(
    not storage_sim_verfuegbar(),
    reason="wsp.storage_sim noch nicht implementiert (NotImplementedError)")


# ======================================================== (a) DIN 4708

def test_wz_einheitswohnung():
    """W_z(1) ≈ 5,82 kWh (Zapfdauer z = 10 min, Badewanne)."""
    w_z_kwh = din4708.W_z(1) / 1000.0
    assert w_z_kwh == pytest.approx(5.82, rel=0.05)


def test_din4708_n_monoton_und_volumen_positiv():
    p = DHWParams()
    letzte_n, letzte_wz, letztes_v = -1.0, -1.0, -1.0
    for n_we in [1, 4, 12, 30, 80]:
        res = size_dhw(p, n_we=n_we, persons_per_we=2.5)
        assert res.n_bedarf > letzte_n
        assert res.w_z_kwh > letzte_wz
        assert res.v_din4708_l > letztes_v
        assert res.v_din4708_l > 0
        letzte_n, letzte_wz, letztes_v = res.n_bedarf, res.w_z_kwh, res.v_din4708_l


def test_din4708_n_formel_und_volumen_handrechnung():
    """N = WE · Personen / 3,5; V = W_z/(c_w·ΔT) / nutzbarer Anteil."""
    p = DHWParams(t_speicher=60.0, t_kalt=10.0, nutzbarer_anteil=0.8)
    res = size_dhw(p, n_we=30, persons_per_we=2.2)

    n_erwartet = 30 * 2.2 / 3.5
    assert res.n_bedarf == pytest.approx(n_erwartet)
    assert res.w_z_kwh == pytest.approx(din4708.W_z(n_erwartet) / 1000.0)
    v_erwartet = ref_volumen(res.w_z_kwh, 50.0) / 0.8
    assert res.v_din4708_l == pytest.approx(v_erwartet, rel=1e-9)
    assert f"{n_erwartet:.1f}" in res.nl_hinweis
    assert "NL" in res.nl_hinweis


def test_glf_faellt_monoton_mit_n():
    glf = [din4708.calc_GLF(n) for n in [1, 2, 5, 10, 30, 100]]
    assert all(a > b for a, b in zip(glf, glf[1:]))


# ==================================================== (b) Faustwert-Verfahren

def test_faustwert_30we_groessenordnung():
    """30 WE × 2,2 P, 60 °C: 66 P · 35 l = 2310 l (+15 %) = 2656 l."""
    p = DHWParams(t_speicher=60.0, t_kalt=10.0, zuschlag=0.15)
    res = size_dhw(p, n_we=30, persons_per_we=2.2)

    v_hand = 30 * 2.2 * 35.0 * (60 - 10) / (60 - 10) * 1.15
    assert v_hand == pytest.approx(2656.5)
    assert res.v_faust_l == pytest.approx(v_hand, rel=1e-9)
    assert 2000.0 <= res.v_faust_l <= 4000.0


def test_faustwert_temperaturumrechnung_energiegleich():
    """Bei 50 °C Speichertemperatur wird das Volumen um 50/40 größer."""
    p60 = DHWParams(t_speicher=60.0, t_kalt=10.0, zuschlag=0.0)
    p50 = DHWParams(t_speicher=50.0, t_kalt=10.0, zuschlag=0.0)
    v60 = size_dhw(p60, 10, 2.5).v_faust_l
    v50 = size_dhw(p50, 10, 2.5).v_faust_l
    assert v50 == pytest.approx(v60 * 50.0 / 40.0)
    # Energiegleichheit: gespeicherte Wärme identisch
    assert v60 * C_W * 50 == pytest.approx(v50 * C_W * 40)


def test_empfehlung_ist_max_und_marktgroesse():
    p = DHWParams()
    res = size_dhw(p, n_we=30, persons_per_we=2.2)
    v_max = max(res.v_din4708_l, res.v_faust_l, res.v_profil_l)
    assert res.v_empfehlung_l == round_to_standard_volume(v_max)
    assert res.v_empfehlung_l >= v_max
    assert "Legionellen" in res.details["legionellen_hinweis"] or \
        "DVGW" in res.details["legionellen_hinweis"]
    assert res.details["massgebend"] == "Faustwert"


def test_legionellen_hinweis_gross_vs_klein():
    gross = size_dhw(DHWParams(), n_we=30, persons_per_we=2.2)
    klein = size_dhw(DHWParams(), n_we=1, persons_per_we=2.0)
    assert gross.v_empfehlung_l > 400
    assert "FriWa" in gross.details["legionellen_hinweis"]
    assert klein.v_empfehlung_l <= 400
    assert "Kleinanlage" in klein.details["legionellen_hinweis"]


# ==================================================== (c) Profilbasiert (TWW)

def test_profil_defizit_analytisch():
    """40 kW über 2 h gegen 10 kW Ladeleistung -> Defizit 2·(40−10) = 60 kWh."""
    profil = zapfprofil(peak_kw=40.0, start_h=6, dauer_h=2)
    p = DHWParams(t_speicher=60.0, t_kalt=10.0, nutzbarer_anteil=0.8,
                  p_lade=10.0, zirkulation_kw=0.0, zuschlag=0.15)
    res = size_dhw(p, n_we=20, persons_per_we=2.0, profile_tww=profil, dt_h=1.0)

    d_analytisch = 2 * (40.0 - 10.0)
    d_referenz = ref_max_defizit(profil.values, 10.0, dt_h=1.0)
    assert d_referenz == pytest.approx(d_analytisch)
    assert res.details["profil"]["defizit_max_kWh"] == pytest.approx(d_analytisch)

    v_erwartet = ref_volumen(d_analytisch, 50.0) / 0.8 * 1.15
    assert v_erwartet == pytest.approx(1483.2, rel=1e-3)
    assert res.v_profil_l == pytest.approx(v_erwartet, rel=1e-9)


def test_profil_mit_zirkulation():
    """Zirkulation wirkt als Dauerlast: Defizit steigt auf 2·(41−10) = 62 kWh."""
    profil = zapfprofil(peak_kw=40.0, start_h=6, dauer_h=2)
    p = DHWParams(p_lade=10.0, zirkulation_kw=1.0)
    res = size_dhw(p, n_we=20, persons_per_we=2.0, profile_tww=profil)

    d_referenz = ref_max_defizit(profil.values + 1.0, 10.0, dt_h=1.0)
    assert d_referenz == pytest.approx(62.0)
    assert res.details["profil"]["defizit_max_kWh"] == pytest.approx(62.0)


def test_profil_referenzschleife_bei_zufallsprofil():
    """Vektorisierte Defizitrechnung == naive Referenzschleife."""
    rng = np.random.default_rng(42)
    idx = pd.date_range("2026-01-01", periods=24 * 30, freq="h")
    werte = rng.gamma(shape=1.5, scale=6.0, size=len(idx))
    profil = pd.Series(werte, index=idx)
    p = DHWParams(p_lade=8.0, zirkulation_kw=0.5, nutzbarer_anteil=1.0,
                  zuschlag=0.0)
    res = size_dhw(p, n_we=10, persons_per_we=2.0, profile_tww=profil)

    d_ref = ref_max_defizit(werte + 0.5, 8.0, dt_h=1.0)
    assert res.details["profil"]["defizit_max_kWh"] == pytest.approx(d_ref)
    assert res.v_profil_l == pytest.approx(ref_volumen(d_ref, 50.0))


def test_profil_15min_aufloesung():
    """dt_h = 0,25: 8 Viertelstunden à 40 kW gegen 10 kW -> 60 kWh."""
    idx = pd.date_range("2026-01-01", periods=4 * 24 * 10, freq="15min")
    werte = np.zeros(len(idx))
    maske = (idx.hour >= 6) & (idx.hour < 8)
    werte[maske] = 40.0
    profil = pd.Series(werte, index=idx)
    p = DHWParams(p_lade=10.0)
    res = size_dhw(p, n_we=10, persons_per_we=2.0, profile_tww=profil, dt_h=0.25)

    d_ref = ref_max_defizit(werte, 10.0, dt_h=0.25)
    assert d_ref == pytest.approx(60.0)
    assert res.details["profil"]["defizit_max_kWh"] == pytest.approx(60.0)


def test_profil_warnung_bei_zu_kleiner_ladeleistung():
    """Tagesbedarf 4·80 = 320 kWh > p_lade·24 = 120 kWh -> Warnung."""
    profil = zapfprofil(peak_kw=80.0, start_h=6, dauer_h=4)
    p = DHWParams(p_lade=5.0)
    res = size_dhw(p, n_we=40, persons_per_we=2.5, profile_tww=profil)

    assert res.details["profil"]["tagesbedarf_max_kWh"] == pytest.approx(320.0)
    assert res.details["profil"]["p_lade_ausreichend"] is False
    assert res.details["profil"]["p_lade_mindest_kW"] == pytest.approx(320.0 / 24)
    assert any("Ladeleistung" in w for w in res.details["warnungen"])


def test_profil_ohne_defizit_bei_grosser_ladeleistung():
    profil = zapfprofil(peak_kw=8.0, start_h=6, dauer_h=2)
    res = size_dhw(DHWParams(p_lade=20.0), 5, 2.0, profile_tww=profil)
    assert res.v_profil_l == 0.0
    assert "Profilbasiert" not in res.details["vergleich_l"]


def test_ohne_profil_kein_profilverfahren():
    res = size_dhw(DHWParams(), n_we=5, persons_per_we=2.5)
    assert res.v_profil_l == 0.0
    assert res.details["profil"] is None


def test_dhw_fehlerhafte_temperaturen():
    with pytest.raises(ValueError):
        size_dhw(DHWParams(t_speicher=10.0, t_kalt=10.0), 5, 2.5)


# ==================================================== (d) Pufferspeicher

def test_abtau_handrechnung():
    """20 l/kW × 50 kW = 1000 l."""
    res = size_buffer(BufferParams(p_wp=50.0, abtau_l_pro_kw=20.0))
    assert res.v_abtau_l == pytest.approx(1000.0)
    res_sole = size_buffer(BufferParams(p_wp=50.0, abtau_l_pro_kw=0.0))
    assert res_sole.v_abtau_l == 0.0


def test_taktung_handrechnung():
    """15 kW × 10 min = 2,5 kWh; V = 2500 Wh/(1,163 · 10 K) = 214,96 l."""
    p = BufferParams(p_wp_min=15.0, t_min_lauf_min=10.0, dt_puffer=10.0)
    res = size_buffer(p)
    v_hand = 15.0 * (10.0 / 60.0) * 1000.0 / (1.163 * 10.0)
    assert v_hand == pytest.approx(214.96, rel=1e-3)
    assert res.v_takt_l == pytest.approx(v_hand)
    # doppelte Spreizung -> halbes Volumen
    res2 = size_buffer(BufferParams(p_wp_min=15.0, t_min_lauf_min=10.0,
                                    dt_puffer=20.0))
    assert res2.v_takt_l == pytest.approx(v_hand / 2.0)


def test_sperrzeit_ohne_profil_nutzt_p_wp():
    """P_WP 50 kW × 2 h = 100 kWh -> 100000/(1,163·10) = 8598,5 l."""
    p = BufferParams(p_wp=50.0, dt_puffer=10.0, sperrzeiten=[(11, 2)])
    res = size_buffer(p)
    v_hand = 50.0 * 2.0 * 1000.0 / (1.163 * 10.0)
    assert v_hand == pytest.approx(8598.45, rel=1e-4)
    assert res.v_sperr_l == pytest.approx(v_hand)


def test_sperrzeit_aus_profil_rollierendes_mittel():
    """Profil: 40 kW von 0–4 h, sonst 10 kW. Max. 2-h-Mittel = 40 kW."""
    idx = pd.date_range("2026-01-01", periods=24 * 5, freq="h")
    werte = np.where(idx.hour < 4, 40.0, 10.0)
    profil = pd.Series(werte, index=idx)

    assert max_mittelleistung(profil, 2.0, 1.0) == pytest.approx(40.0)
    assert max_mittelleistung(profil, 4.0, 1.0) == pytest.approx(40.0)
    assert max_mittelleistung(profil, 6.0, 1.0) == pytest.approx(
        (4 * 40.0 + 2 * 10.0) / 6.0)

    p = BufferParams(p_wp=50.0, dt_puffer=10.0, sperrzeiten=[(11, 2)],
                     abtau_l_pro_kw=0.0)
    res = size_buffer(p, profile_heiz=profil)
    v_hand = 40.0 * 2.0 * 1000.0 / (1.163 * 10.0)
    assert v_hand == pytest.approx(6878.76, rel=1e-4)
    assert res.v_sperr_l == pytest.approx(v_hand)
    assert res.details["sperrzeit"]["fenster"][0]["q_mittel_kW"] == \
        pytest.approx(40.0)


def test_sperrzeit_mehrere_fenster_maximum():
    p = BufferParams(p_wp=40.0, dt_puffer=10.0, sperrzeiten=[(11, 2), (17, 3)])
    res = size_buffer(p)
    v_hand = 40.0 * 3.0 * 1000.0 / (1.163 * 10.0)   # 3-h-Fenster ist maßgebend
    assert res.v_sperr_l == pytest.approx(v_hand)
    assert len(res.details["sperrzeit"]["fenster"]) == 2


def test_sperrzeit_ohne_definition_null():
    res = size_buffer(BufferParams(sperrzeiten=[]))
    assert res.v_sperr_l == 0.0


def test_availability_maske():
    idx = pd.date_range("2026-01-01", periods=48, freq="h")
    maske = availability_mask(idx, [(11, 2), (17, 2)])
    assert maske.sum() == pytest.approx(48 - 2 * 4)   # 2 Tage × 2 Fenster × 2 h
    assert maske.iloc[11] == 0.0 and maske.iloc[12] == 0.0
    assert maske.iloc[13] == 1.0
    assert maske.iloc[17] == 0.0 and maske.iloc[18] == 0.0

    # Umbruch über Mitternacht
    maske2 = availability_mask(idx, [(23, 2)])
    assert maske2.iloc[23] == 0.0 and maske2.iloc[0] == 0.0
    assert maske2.iloc[1] == 1.0


def test_massgebend_und_empfehlung():
    """Abtau 1000 l > Taktung 215 l, keine Sperrzeit -> Abtauung maßgebend."""
    p = BufferParams(p_wp=50.0, p_wp_min=15.0, abtau_l_pro_kw=20.0,
                     dt_puffer=10.0, sperrzeiten=[])
    res = size_buffer(p)
    assert res.massgebend == "Abtauung"
    assert res.v_empfehlung_l == round_to_standard_volume(1000.0) == 1000

    p2 = BufferParams(p_wp=50.0, p_wp_min=15.0, abtau_l_pro_kw=20.0,
                      dt_puffer=10.0, sperrzeiten=[(11, 2)])
    res2 = size_buffer(p2)
    assert res2.massgebend == "EVU-Sperrzeit"
    assert res2.v_empfehlung_l == round_to_standard_volume(res2.v_sperr_l)


def test_size_buffer_ohne_storage_sim_kein_absturz():
    """Solange storage_sim ein Stub ist, muss size_buffer trotzdem liefern."""
    idx = pd.date_range("2026-01-01", periods=24 * 7, freq="h")
    profil = pd.Series(np.full(len(idx), 30.0), index=idx)
    res = size_buffer(BufferParams(p_wp=50.0, sperrzeiten=[(11, 2)]),
                      profile_heiz=profil)
    assert res.v_abtau_l > 0
    assert res.v_empfehlung_l > 0
    if not storage_sim_verfuegbar():
        assert res.v_sim_l == 0.0
        assert "NotImplementedError" in res.details["simulation"]["status"]


def test_hinweis_fehlende_erzeugerleistung():
    idx = pd.date_range("2026-01-01", periods=24 * 3, freq="h")
    werte = np.where(idx.hour < 3, 125.0, 40.0)     # Spitze 125 kW (SWSG-Fall)
    profil = pd.Series(werte, index=idx)
    res = size_buffer(BufferParams(p_wp=80.0, p_bivalent=0.0), profile_heiz=profil)

    assert res.details["simulation"]["p_gen_kW"] == pytest.approx(80.0)
    assert res.details["simulation"]["fehlende_leistung_kW"] == pytest.approx(45.0)
    assert any("fehlen 45" in h for h in res.details["hinweise"])

    res2 = size_buffer(BufferParams(p_wp=80.0, p_bivalent=60.0),
                       profile_heiz=profil)
    assert res2.details["simulation"]["fehlende_leistung_kW"] == 0.0


# -------------------------------------------- Tests mit echtem storage_sim

@requires_storage_sim
def test_sim_rechteckfall_gegen_referenzschleife():
    """Rechteck-Lastfall: analytisch bekannte Mindestkapazität."""
    idx = pd.date_range("2026-01-01", periods=24 * 10, freq="h")
    werte = np.where((idx.hour >= 6) & (idx.hour < 10), 100.0, 20.0)
    profil = pd.Series(werte, index=idx)
    p_gen = 40.0

    c_ref = ref_max_defizit(werte, p_gen, dt_h=1.0)
    assert c_ref == pytest.approx(4 * (100.0 - 40.0))   # 240 kWh

    p = BufferParams(p_wp=p_gen, p_bivalent=0.0, dt_puffer=10.0,
                     sperrzeiten=[], abtau_l_pro_kw=0.0)
    res = size_buffer(p, profile_heiz=profil)
    assert res.c_sim_kwh == pytest.approx(c_ref, rel=0.05)
    assert res.v_sim_l == pytest.approx(ref_volumen(c_ref, 10.0), rel=0.05)
    assert res.sim is not None and res.sim.ok(1.0)


@requires_storage_sim
def test_sim_mit_sperrzeiten_gegen_referenzschleife():
    """Sperrzeiten als Verfügbarkeitsmaske — Referenzschleife mit gleicher Maske."""
    idx = pd.date_range("2026-01-01", periods=24 * 10, freq="h")
    profil = pd.Series(np.full(len(idx), 30.0), index=idx)
    p = BufferParams(p_wp=60.0, p_bivalent=0.0, dt_puffer=10.0,
                     sperrzeiten=[(11, 2), (17, 2)], abtau_l_pro_kw=0.0)
    res = size_buffer(p, profile_heiz=profil)

    maske = availability_mask(idx, p.sperrzeiten)
    c_ref = ref_max_defizit(profil.values, 60.0, dt_h=1.0,
                            availability=maske.values)
    assert c_ref == pytest.approx(60.0)     # 2 h × 30 kW
    assert res.c_sim_kwh == pytest.approx(c_ref, rel=0.05)


@requires_storage_sim
def test_sim_ohne_zieldeckung_geht_nicht_in_empfehlung():
    """P_gen deckt die Jahresarbeit nicht -> Simulation ist nicht maßgebend."""
    idx = pd.date_range("2026-01-01", periods=24 * 30, freq="h")
    profil = pd.Series(np.full(len(idx), 100.0), index=idx)   # dauerhaft 100 kW
    p = BufferParams(p_wp=20.0, p_wp_min=10.0, abtau_l_pro_kw=20.0,
                     dt_puffer=10.0, sperrzeiten=[])
    res = size_buffer(p, profile_heiz=profil)

    assert res.details["simulation"]["gueltig"] is False
    assert res.massgebend != "Lastgang-Simulation"
    assert res.v_empfehlung_l == round_to_standard_volume(res.v_abtau_l)
    assert any("Erzeugerleistung" in w for w in res.details["warnungen"])


@requires_storage_sim
def test_sim_deckende_erzeugerleistung_braucht_keinen_speicher():
    idx = pd.date_range("2026-01-01", periods=24 * 5, freq="h")
    profil = pd.Series(np.full(len(idx), 20.0), index=idx)
    p = BufferParams(p_wp=50.0, dt_puffer=10.0, sperrzeiten=[],
                     abtau_l_pro_kw=0.0)
    res = size_buffer(p, profile_heiz=profil)
    assert res.c_sim_kwh == pytest.approx(0.0, abs=1.0)
