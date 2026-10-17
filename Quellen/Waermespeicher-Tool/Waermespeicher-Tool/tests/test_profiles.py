"""Tests für wsp.weather und wsp.profiles_synthetic.

Kernanforderung: **Energieerhaltung** — die Jahressumme des erzeugten
Profils muss die vorgegebenen Jahressummen auf ±1 % treffen.
"""
from __future__ import annotations

import os
import sys

import numpy as np
import pandas as pd
import pytest

# Projektwurzel importierbar machen (tests/ ist kein Package)
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from wsp import weather                                          # noqa: E402
from wsp.models import (Building, BuildingCategory,              # noqa: E402
                        ProfileSet, WeatherConfig)
from wsp.profiles_synthetic import (BDEW_BRANCH_MAP,             # noqa: E402
                                    DEMANDLIB_SHLP_TYPES,
                                    generate_profiles)

TOL = 0.01          # ±1 % Toleranz auf Jahressummen
YEAR = 2026
REGION = 4          # TRY 4 = Nordostdeutsches Tiefland (Potsdam)


# --------------------------------------------------------------------- Wetter

def test_try_region_names():
    names = weather.try_region_names()
    assert len(names) == 15
    assert names[4] == "Nordostdeutsches Tiefland"
    assert names[12] == "Oberrheingraben und unteres Neckartal"
    assert all(isinstance(v, str) and v for v in names.values())


def test_load_try_weather_hourly():
    df = weather.load_try_weather(REGION, YEAR)
    assert len(df) == 8760
    assert list(df.columns) == ["TAMB", "CCOVER"]
    assert df.index[0] == pd.Timestamp(f"{YEAR}-01-01 00:00")
    assert df.index[-1] == pd.Timestamp(f"{YEAR}-12-31 23:00")
    assert not df.isna().any().any()
    assert -40.0 < df["TAMB"].min() and df["TAMB"].max() < 50.0
    assert 0 <= df["CCOVER"].min() and df["CCOVER"].max() <= 9


def test_load_try_weather_leap_year():
    df = weather.load_try_weather(REGION, 2028)
    assert len(df) == 8784
    assert not df.isna().any().any()
    # 29.02. ist die Kopie des 28.02.
    assert np.allclose(df.loc["2028-02-29", "TAMB"].to_numpy(),
                       df.loc["2028-02-28", "TAMB"].to_numpy())


def test_get_climate():
    climate = weather.get_climate(REGION, YEAR)
    climate.check_attributes()          # wirft AttributeError, wenn unvollst.
    assert len(climate.temperature) == 365
    assert set(climate.cloud_coverage.unique()) <= {"B", "H"}


def test_invalid_region():
    with pytest.raises(ValueError):
        weather.load_try_weather(0, YEAR)
    with pytest.raises(ValueError):
        weather.load_try_weather(16, YEAR)


# ------------------------------------------------------ (a) MFH über VDI 4655

@pytest.fixture(scope="module")
def mfh_profile() -> ProfileSet:
    building = Building(
        name="MFH 30 WE",
        category=BuildingCategory.MFH,
        n_we=30,
        n_persons=2.2,
        q_heiz_a=150_000.0,
        q_tww_a=22_000.0,
    )
    return generate_profiles([building], WeatherConfig(try_region=REGION,
                                                       year=YEAR))


def test_mfh_structure(mfh_profile):
    ps = mfh_profile
    assert isinstance(ps, ProfileSet)
    assert ps.source == "synthetisch"
    assert ps.resolution == "h"
    assert list(ps.df.columns) == ["Q_heiz", "Q_tww"]
    assert len(ps.df) == 8760                       # 8760 Stunden
    assert isinstance(ps.df.index, pd.DatetimeIndex)
    assert ps.df.index[0] == pd.Timestamp(f"{YEAR}-01-01 00:00")
    assert ps.df.index[-1] == pd.Timestamp(f"{YEAR}-12-31 23:00")
    assert ps.df.index.tz is None


def test_mfh_no_nan_and_non_negative(mfh_profile):
    df = mfh_profile.df
    assert not df.isna().any().any(), "Profil enthält NaN"
    assert np.isfinite(df.to_numpy()).all()
    assert (df >= 0).all().all(), "Negative Leistungen im Profil"


def test_mfh_annual_sums(mfh_profile):
    sums = mfh_profile.annual_sums()
    assert sums["Q_heiz"] == pytest.approx(150_000.0, rel=TOL)
    assert sums["Q_tww"] == pytest.approx(22_000.0, rel=TOL)
    total = float(mfh_profile.q_total.sum() * mfh_profile.dt_h)
    assert total == pytest.approx(172_000.0, rel=TOL)


def test_mfh_peak_load_plausible(mfh_profile):
    peak = float(mfh_profile.q_total.max())
    assert 40.0 < peak < 200.0, f"Spitzenlast {peak:.1f} kW unplausibel"
    # Volllaststunden im üblichen Rahmen für Wohngebäude
    vbh = 172_000.0 / peak
    assert 800.0 < vbh < 4000.0


def test_mfh_seasonality(mfh_profile):
    """Heizlast im Winter deutlich höher als im Sommer, TWW ganzjährig."""
    df = mfh_profile.df
    winter = df.loc[f"{YEAR}-01", "Q_heiz"].mean()
    summer = df.loc[f"{YEAR}-07", "Q_heiz"].mean()
    assert winter > 5.0 * summer
    assert df.loc[f"{YEAR}-07", "Q_tww"].sum() > 0.0


def test_mfh_meta(mfh_profile):
    meta = mfh_profile.meta
    assert meta["try_region"] == REGION
    assert meta["jahr"] == YEAR
    assert meta["try_region_name"] == "Nordostdeutsches Tiefland"
    ist = meta["jahressummen_ist_kwh"]["MFH 30 WE"]
    soll = meta["jahressummen_soll_kwh"]["MFH 30 WE"]
    assert ist["Q_heiz"] == pytest.approx(soll["Q_heiz"], rel=TOL)
    assert ist["Q_tww"] == pytest.approx(soll["Q_tww"], rel=TOL)
    assert abs(meta["abweichung_rel"]["Q_heiz"]) < TOL
    assert abs(meta["abweichung_rel"]["Q_tww"]) < TOL


# ----------------------------------------------------------- (b) GHD via BDEW

@pytest.fixture(scope="module")
def ghd_profile() -> ProfileSet:
    building = Building(
        name="Buerogebaeude",
        category=BuildingCategory.GHD,
        bdew_branch="gko",              # Gebietskörperschaft/Büro
        q_heiz_a=80_000.0,              # Gesamtwärme
        tww_share=0.15,
    )
    return generate_profiles([building], WeatherConfig(try_region=REGION,
                                                       year=YEAR))


def test_ghd_structure(ghd_profile):
    df = ghd_profile.df
    assert len(df) == 8760
    assert list(df.columns) == ["Q_heiz", "Q_tww"]
    assert not df.isna().any().any()
    assert (df >= 0).all().all()


def test_ghd_annual_sums(ghd_profile):
    sums = ghd_profile.annual_sums()
    # Gesamtwärme bleibt erhalten, Aufteilung gemäß tww_share
    assert sums["Q_heiz"] + sums["Q_tww"] == pytest.approx(80_000.0, rel=TOL)
    assert sums["Q_tww"] == pytest.approx(0.15 * 80_000.0, rel=TOL)
    assert sums["Q_heiz"] == pytest.approx(0.85 * 80_000.0, rel=TOL)


def test_ghd_peak_plausible(ghd_profile):
    peak = float(ghd_profile.q_total.max())
    assert 0.0 < peak < 200.0
    assert 80_000.0 / peak > 500.0          # Volllaststunden plausibel


def test_ghd_tww_band_is_flat(ghd_profile):
    """Das BDEW-Warmwasserprofil ist temperaturunabhängig (Grundband)."""
    tww = ghd_profile.df["Q_tww"]
    winter = tww.loc[f"{YEAR}-01"].mean()
    summer = tww.loc[f"{YEAR}-07"].mean()
    assert summer > 0.5 * winter


def test_ghd_branch_mapping():
    assert set(BDEW_BRANCH_MAP.values()) <= set(DEMANDLIB_SHLP_TYPES)
    assert BDEW_BRANCH_MAP["gko"] == "GKO"
    assert BDEW_BRANCH_MAP["gbα"] == "GBA"      # Tippfehler in models.py
    assert BDEW_BRANCH_MAP["ghα"] == "GHA"      # kein eigener BDEW-Typ


def test_ghd_unknown_branch_falls_back():
    building = Building(name="X", category=BuildingCategory.GHD,
                        bdew_branch="gibtsnicht", q_heiz_a=10_000.0,
                        tww_share=0.1)
    with pytest.warns(UserWarning):
        ps = generate_profiles([building], WeatherConfig(try_region=REGION,
                                                         year=YEAR))
    assert sum(ps.annual_sums().values()) == pytest.approx(10_000.0, rel=TOL)


def test_all_models_branches_resolvable():
    """Alle Keys aus models.BDEW_BRANCHES sind intern auflösbar."""
    from wsp.models import BDEW_BRANCHES
    unmapped = [k for k in BDEW_BRANCHES if k.lower() not in BDEW_BRANCH_MAP]
    assert unmapped == [], f"Nicht gemappte BDEW-Branchen: {unmapped}"


# ------------------------------------------------------------------ Sonstiges

def test_mixed_portfolio_with_copies():
    """EFH-Kopien + MFH + GHD: Jahressummen skalieren mit copies."""
    buildings = [
        Building(name="EFH", category=BuildingCategory.EFH, n_we=1,
                 n_persons=3.5, q_heiz_a=15_000.0, copies=10, sigma=4.0),
        Building(name="MFH", category=BuildingCategory.MFH, n_we=12,
                 n_persons=2.0, q_heiz_a=90_000.0, q_tww_a=15_000.0),
        Building(name="Laden", category=BuildingCategory.GHD,
                 bdew_branch="gha", q_heiz_a=40_000.0, tww_share=0.05),
    ]
    ps = generate_profiles(buildings, WeatherConfig(try_region=12, year=YEAR))
    sums = ps.annual_sums()
    q_heiz_soll = 10 * 15_000.0 + 90_000.0 + 0.95 * 40_000.0
    q_tww_soll = 10 * 500.0 * 3.5 + 15_000.0 + 0.05 * 40_000.0
    assert sums["Q_heiz"] == pytest.approx(q_heiz_soll, rel=TOL)
    assert sums["Q_tww"] == pytest.approx(q_tww_soll, rel=TOL)
    assert not ps.df.isna().any().any()


def test_resolution_15min_conserves_energy():
    building = Building(name="MFH", category=BuildingCategory.MFH, n_we=30,
                        n_persons=2.2, q_heiz_a=150_000.0, q_tww_a=22_000.0)
    ps = generate_profiles([building], WeatherConfig(try_region=REGION,
                                                     year=YEAR),
                           resolution="15min")
    assert ps.resolution == "15min"
    assert len(ps.df) == 8760 * 4
    assert ps.dt_h == pytest.approx(0.25)
    sums = ps.annual_sums()
    assert sums["Q_heiz"] == pytest.approx(150_000.0, rel=TOL)
    assert sums["Q_tww"] == pytest.approx(22_000.0, rel=TOL)


def test_tww_default_500_kwh_per_person():
    building = Building(name="MFH", category=BuildingCategory.MFH, n_we=10,
                        n_persons=2.0, q_heiz_a=60_000.0, q_tww_a=None)
    ps = generate_profiles([building], WeatherConfig(try_region=REGION,
                                                     year=YEAR))
    assert ps.annual_sums()["Q_tww"] == pytest.approx(10_000.0, rel=TOL)


def test_leap_year_residential_raises():
    """demandlib 0.2.2 kann VDI 4655 nicht für Schaltjahre rechnen."""
    building = Building(name="MFH", category=BuildingCategory.MFH, n_we=10,
                        n_persons=2.0, q_heiz_a=60_000.0)
    with pytest.raises(ValueError, match="Schaltjahr"):
        generate_profiles([building], WeatherConfig(try_region=REGION,
                                                    year=2028))


def test_empty_building_list_raises():
    with pytest.raises(ValueError):
        generate_profiles([], WeatherConfig(try_region=REGION, year=YEAR))
