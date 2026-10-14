"""Tests für storage_sim, profiles_measured und analysis.

Ausführen:  pytest tests/test_measured_analysis_sim.py -v
"""
from __future__ import annotations

import os
import sys

import numpy as np
import pandas as pd
import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from wsp import analysis, profiles_measured, storage_sim  # noqa: E402
from wsp.models import ProfileSet  # noqa: E402


# ============================================================ Hilfsfunktionen

def rechtecklast(tage: int = 1, start: str = "2025-01-01",
                 grund_kw: float = 10.0, spitze_kw: float = 50.0,
                 spitze_von: int = 8, spitze_dauer: int = 10) -> pd.Series:
    """Rechtecklast: 'spitze_dauer' h à spitze_kw ab Stunde 'spitze_von',
    sonst grund_kw. Stündliche Auflösung."""
    idx = pd.date_range(start, periods=24 * tage, freq="h")
    werte = np.full(len(idx), grund_kw, dtype=float)
    stunde = idx.hour.to_numpy()
    peak = (stunde >= spitze_von) & (stunde < spitze_von + spitze_dauer)
    werte[peak] = spitze_kw
    return pd.Series(werte, index=idx, name="Q_last")


def profil_aus_serie(total: pd.Series, source: str = "gemessen") -> ProfileSet:
    df = pd.DataFrame({"Q_heiz": total.astype(float),
                       "Q_tww": pd.Series(0.0, index=total.index)})
    return ProfileSet(df=df, resolution="h", source=source, meta={})


# ==================================================== (a) storage_sim

# Analytische Herleitung des Referenzfalls (1 Tag, Start mit vollem Speicher):
#   Last:  10 h à 50 kW (Std. 8..17), sonst 10 kW  -> 640 kWh/d
#   P_gen: 20 kW konstant                          -> 480 kWh/d
#   Vor der Spitze (Std. 0..7) ist der Erzeuger im Überschuss (+10 kW),
#   der Speicher ist aber bereits voll -> kein Zugewinn.
#   Während der Spitze fehlen (50 - 20) = 30 kW über 10 h
#   -> Defizit = 30 kW * 10 h = 300 kWh, das der Speicher liefern muss.
#   Nach der Spitze ist der Erzeuger wieder im Überschuss.
#   => C_min = 300 kWh (exakt; bei C < 300 kWh entsteht Unterdeckung).
C_MIN_ANALYTISCH = 300.0


def test_simulate_rechtecklast_analytische_kapazitaet():
    last = rechtecklast(tage=1)
    assert last.sum() == pytest.approx(640.0)

    sim_exakt = storage_sim.simulate(last, 20.0, C_MIN_ANALYTISCH, dt_h=1.0)
    assert sim_exakt.unterdeckung_kwh == pytest.approx(0.0, abs=1e-6)
    assert sim_exakt.unterdeckung_h == 0
    assert sim_exakt.deckungsgrad == pytest.approx(1.0)
    assert sim_exakt.ok()
    # Speicher wird am Ende der Spitze exakt leer
    assert sim_exakt.df["SOC"].min() == pytest.approx(0.0, abs=1e-6)

    # eine Spur zu klein -> genau die fehlende Energie als Unterdeckung
    sim_klein = storage_sim.simulate(last, 20.0, C_MIN_ANALYTISCH - 25.0)
    assert sim_klein.unterdeckung_kwh == pytest.approx(25.0, abs=1e-6)
    assert sim_klein.unterdeckung_h == 1
    assert not sim_klein.ok()

    # ohne Speicher: 10 h à 30 kW fehlen
    sim_null = storage_sim.simulate(last, 20.0, 0.0)
    assert sim_null.unterdeckung_kwh == pytest.approx(300.0)
    assert sim_null.unterdeckung_h == 10
    assert sim_null.deckungsgrad == pytest.approx(1 - 300.0 / 640.0)


def test_find_min_capacity_trifft_analytischen_wert():
    last = rechtecklast(tage=1)
    sim = storage_sim.find_min_capacity(last, 20.0, dt_h=1.0)
    assert sim.ok()
    assert sim.capacity_kwh == pytest.approx(C_MIN_ANALYTISCH, abs=2.0)


def test_find_min_capacity_periodischer_lastgang():
    """3 identische Tage, P_gen = 30 kW.

    Defizit je Spitze = (50-30) kW * 10 h = 200 kWh; zwischen zwei Spitzen
    stehen 14 h à +20 kW = 280 kWh Ladung zur Verfügung (> 200 kWh),
    d. h. der Speicher wird jeden Tag wieder voll -> C_min = 200 kWh.
    """
    last = rechtecklast(tage=3)
    sim = storage_sim.find_min_capacity(last, 30.0, dt_h=1.0)
    assert sim.ok()
    assert sim.capacity_kwh == pytest.approx(200.0, abs=2.0)


def test_sperrzeiten_maske_reduziert_deckung():
    last = rechtecklast(tage=1)
    frei = storage_sim.simulate(last, 20.0, C_MIN_ANALYTISCH)

    # EVU-Sperrzeit 12:00-14:00 (2 h) mitten in der Spitze
    avail = pd.Series(1.0, index=last.index)
    avail[(last.index.hour >= 12) & (last.index.hour < 14)] = 0.0
    gesperrt = storage_sim.simulate(last, 20.0, C_MIN_ANALYTISCH,
                                    availability=avail)

    assert gesperrt.deckungsgrad < frei.deckungsgrad
    # es fehlt genau die im Sperrfenster nicht erzeugte Energie: 2 h * 20 kW
    assert gesperrt.unterdeckung_kwh == pytest.approx(40.0, abs=1e-6)
    assert gesperrt.deckungsgrad == pytest.approx(1 - 40.0 / 640.0)
    assert (gesperrt.df["P_gen_verf"] == 0).sum() == 2

    # mit Sperrzeit wird mehr Kapazität gebraucht
    c_frei = storage_sim.find_min_capacity(last, 20.0).capacity_kwh
    c_sperr = storage_sim.find_min_capacity(last, 20.0,
                                            availability=avail).capacity_kwh
    assert c_sperr > c_frei
    assert c_sperr == pytest.approx(340.0, abs=2.0)


def test_find_min_capacity_unmoeglich():
    """P_gen unter der mittleren Last -> auch c_max reicht nicht."""
    last = rechtecklast(tage=3)
    sim = storage_sim.find_min_capacity(last, 5.0, c_max_kwh=1000.0)
    assert not sim.ok()
    assert sim.capacity_kwh == pytest.approx(1000.0)
    assert sim.unterdeckung_kwh > 0


def test_simulate_zeitreihe_als_p_gen_und_soc0():
    last = rechtecklast(tage=1)
    p_gen = pd.Series(20.0, index=last.index)
    sim_a = storage_sim.simulate(last, p_gen, 300.0)
    sim_b = storage_sim.simulate(last, 20.0, 300.0)
    assert sim_a.unterdeckung_kwh == pytest.approx(sim_b.unterdeckung_kwh)

    # leerer Speicher zu Beginn -> Unterdeckung trotz ausreichender Kapazität
    sim_leer = storage_sim.simulate(last, 20.0, 300.0, soc0=0.0)
    assert sim_leer.unterdeckung_kwh > 0
    # Std. 0..7: +10 kW -> 80 kWh im Speicher, Spitze braucht 300 kWh
    assert sim_leer.unterdeckung_kwh == pytest.approx(220.0, abs=1e-6)


# ============================================ (b) CSV-Roundtrip / Import

def _synthetisches_jahr(jahr: int = 2025, freq: str = "h") -> pd.Series:
    idx = pd.date_range(f"{jahr}-01-01", f"{jahr}-12-31 23:00", freq=freq)
    tag = idx.dayofyear.to_numpy()
    stunde = idx.hour.to_numpy() + idx.minute.to_numpy() / 60.0
    werte = (30.0
             + 25.0 * np.cos(2 * np.pi * (tag - 15) / 365.0)
             + 6.0 * np.sin(2 * np.pi * (stunde - 6) / 24.0))
    return pd.Series(np.clip(werte, 1.0, None), index=idx,
                     name="Lastgang Wärmebedarf")


def _schreibe_csv(pfad, serie: pd.Series, unit_col: str = "Lastgang Wärmebedarf",
                  zeitformat: str = "%d.%m.%Y %H:%M") -> pd.DataFrame:
    df = pd.DataFrame({
        "Datum": serie.index.strftime(zeitformat),
        unit_col: [f"{v:.3f}".replace(".", ",") for v in serie.to_numpy()],
    })
    df.to_csv(pfad, sep=";", index=False, encoding="utf-8")
    return df


def test_csv_roundtrip_stuendlich_kw(tmp_path):
    """SWSG-Referenzfall: stündliche kW-Werte, ';', deutsches Dezimalkomma."""
    serie = _synthetisches_jahr()
    pfad = tmp_path / "lastgang.csv"
    _schreibe_csv(pfad, serie)

    ps = profiles_measured.import_lastgang(
        str(pfad), timestamp_col="Datum", value_col="Lastgang Wärmebedarf")

    assert ps.source == "gemessen"
    assert ps.resolution == "h"
    assert ps.meta["aufloesung_quelle"] == "h"
    assert ps.meta["dt_quelle_h"] == pytest.approx(1.0)
    assert len(ps.df) == len(serie) == 8760
    assert list(ps.df.columns) == ["Q_heiz", "Q_tww"]
    assert ps.df["Q_tww"].sum() == 0.0

    soll = float(serie.sum())
    ist = float(ps.q_total.sum())
    assert ist == pytest.approx(soll, rel=1e-3)          # Jahressumme ±0,1 %
    assert ps.meta["jahressumme_kwh"] == pytest.approx(soll, rel=1e-3)
    assert ps.annual_sums()["Q_heiz"] == pytest.approx(soll, rel=1e-3)
    assert not ps.meta["luecken"]
    assert ps.meta["duplikate"] == 0


def test_csv_15min_kwh_wird_energieerhaltend_resampelt(tmp_path):
    """15-min-Werte in kWh je Intervall -> stündliche kW, Energie erhalten."""
    serie = _synthetisches_jahr(freq="15min") * 0.25   # kWh je Viertelstunde
    serie.name = "Energie"
    pfad = tmp_path / "lastgang_15min.csv"
    _schreibe_csv(pfad, serie, unit_col="Energie")

    ps = profiles_measured.import_lastgang(
        str(pfad), timestamp_col="Datum", value_col="Energie",
        value_unit="kWh")

    assert ps.meta["aufloesung_quelle"] == "15min"
    assert ps.meta["dt_quelle_h"] == pytest.approx(0.25)
    assert ps.resolution == "h"
    assert len(ps.df) == 8760

    soll_kwh = float(serie.sum())
    assert float(ps.q_total.sum() * ps.dt_h) == pytest.approx(soll_kwh, rel=1e-3)


def test_import_erkennt_luecken_und_duplikate(tmp_path):
    serie = _synthetisches_jahr()
    df = pd.DataFrame({
        "Datum": serie.index.strftime("%d.%m.%Y %H:%M"),
        "Wert": [f"{v:.3f}".replace(".", ",") for v in serie.to_numpy()],
    })

    # kurze Lücke (3 h, wird interpoliert) und lange Lücke (10 h, Report)
    kurz = list(range(1000, 1003))
    lang = list(range(5000, 5010))
    df_luecken = df.drop(index=kurz + lang).reset_index(drop=True)
    # ein doppelter Zeitstempel
    df_luecken = pd.concat([df_luecken, df_luecken.iloc[[100]]],
                           ignore_index=True)

    pfad = tmp_path / "luecken.csv"
    df_luecken.to_csv(pfad, sep=";", index=False, encoding="utf-8")

    ps = profiles_measured.import_lastgang(
        str(pfad), timestamp_col="Datum", value_col="Wert")

    assert len(ps.df) == 8760                      # Raster wieder vollständig
    assert ps.meta["duplikate"] == 1
    assert ps.meta["luecken_anzahl"] == 2
    assert ps.meta["luecken_interpoliert"] == 1
    assert ps.meta["luecken_offen"] == 1
    dauern = sorted(r["dauer_h"] for r in ps.meta["luecken"])
    assert dauern == [3.0, 10.0]

    # kurze Lücke linear interpoliert -> nahe am Originalwert
    interp = ps.q_total.iloc[kurz].to_numpy()
    orig = serie.iloc[kurz].to_numpy()
    assert np.allclose(interp, orig, rtol=0.05)
    # lange Lücke bleibt 0 und ist im Report ausgewiesen
    assert np.allclose(ps.q_total.iloc[lang].to_numpy(), 0.0)
    assert any(r["dauer_h"] == 10.0 and "0" in r["behandlung"]
               for r in ps.meta["luecken"])


def test_import_excel_und_tww_spalte(tmp_path):
    idx = pd.date_range("2025-01-01", periods=24 * 14, freq="h")
    total = pd.Series(np.linspace(10, 40, len(idx)), index=idx)
    tww = pd.Series(4.0, index=idx)
    pfad = tmp_path / "messdaten.xlsx"
    pd.DataFrame({"Datum": idx, "Waerme": total.to_numpy(),
                  "TWW": tww.to_numpy()}).to_excel(pfad, index=False)

    ps = profiles_measured.import_lastgang(
        str(pfad), timestamp_col="Datum", value_col="Waerme", tww_col="TWW")

    assert ps.meta["aufloesung_quelle"] == "h"
    assert ps.df["Q_tww"].sum() == pytest.approx(4.0 * len(idx))
    assert float(ps.q_total.sum()) == pytest.approx(float(total.sum()), rel=1e-9)


# ================================================= (c) split_tww_sommer

def _konstruiertes_profil(band_kw: float = 5.0, heiz_kw: float = 20.0,
                          jahr: int = 2025) -> tuple[ProfileSet, pd.Series]:
    idx = pd.date_range(f"{jahr}-01-01", f"{jahr}-12-31 23:00", freq="h")
    total = pd.Series(band_kw, index=idx, dtype=float)
    winter = ~idx.month.isin([6, 7, 8])
    total[winter] += heiz_kw
    return profil_aus_serie(total), total


def test_split_tww_sommer_bekanntes_band():
    band_kw, heiz_kw = 5.0, 20.0
    ps, total = _konstruiertes_profil(band_kw, heiz_kw)

    out = profiles_measured.split_tww_sommer(ps)

    assert out.meta["tww_split"]["band_kw"] == pytest.approx(band_kw)
    # TWW = Band, ganzjährig
    assert np.allclose(out.df["Q_tww"].to_numpy(), band_kw)
    # Heizung nur außerhalb der Sommermonate
    sommer = out.df.index.month.isin([6, 7, 8])
    assert np.allclose(out.df.loc[sommer, "Q_heiz"].to_numpy(), 0.0)
    assert np.allclose(out.df.loc[~sommer, "Q_heiz"].to_numpy(), heiz_kw)
    # Energieerhaltung
    assert np.allclose(out.q_total.to_numpy(), total.to_numpy())
    assert float(out.q_total.sum()) == pytest.approx(float(total.sum()))
    assert out.meta["tww_split"]["q_tww_kwh"] == pytest.approx(band_kw * len(total))


def test_split_tww_sommer_deckelung_und_faktor():
    ps, total = _konstruiertes_profil(5.0, 20.0)
    # Winterstunden mit Schwachlast unter dem Band (z. B. Stillstand)
    total2 = total.copy()
    schwach = total2.index[(total2.index.month == 1) & (total2.index.day == 5)]
    total2.loc[schwach] = 2.0
    ps2 = profil_aus_serie(total2)

    out = profiles_measured.split_tww_sommer(ps2)
    assert out.meta["tww_split"]["band_kw"] == pytest.approx(5.0)
    # gedeckelt auf die jeweilige Stundenlast
    assert np.allclose(out.df.loc[schwach, "Q_tww"].to_numpy(), 2.0)
    assert np.allclose(out.df.loc[schwach, "Q_heiz"].to_numpy(), 0.0)
    assert np.allclose(out.q_total.to_numpy(), total2.to_numpy())

    # Faktor skaliert das Band
    out08 = profiles_measured.split_tww_sommer(ps2, faktor=0.8)
    assert out08.meta["tww_split"]["band_kw"] == pytest.approx(4.0)
    assert out08.df["Q_tww"].max() == pytest.approx(4.0)
    assert np.allclose(out08.q_total.to_numpy(), total2.to_numpy())


def test_split_tww_sommer_ignoriert_nullstunden():
    """Nullstunden (Messlücke/Abschaltung) verfälschen das Band nicht."""
    ps, total = _konstruiertes_profil(5.0, 20.0)
    total2 = total.copy()
    aus = total2.index[(total2.index.month == 7) & (total2.index.day <= 5)]
    total2.loc[aus] = 0.0
    out = profiles_measured.split_tww_sommer(profil_aus_serie(total2))
    assert out.meta["tww_split"]["band_kw"] == pytest.approx(5.0)
    assert out.meta["tww_split"]["n_sommerstunden_aktiv"] < \
        out.meta["tww_split"]["n_sommerstunden"]


# ======================================================= analysis (Zusatz)

def test_jdl_und_top_peaks():
    ps, total = _konstruiertes_profil(5.0, 20.0)
    kurve = analysis.jdl(total)
    assert len(kurve) == len(total)
    assert kurve.is_monotonic_decreasing
    assert kurve.iloc[0] == pytest.approx(total.max())
    assert kurve.index[0] == 1

    peaks = analysis.top_peaks(total, n=5)
    assert list(peaks.columns[:2]) == ["Zeitpunkt", "kW"]
    assert len(peaks) == 5
    assert peaks["kW"].iloc[0] == pytest.approx(total.max())


def test_tagesprofile_und_wochenlastgang():
    ps, total = _konstruiertes_profil(5.0, 20.0)
    tp = analysis.tagesprofile(ps, col="Q_total")
    assert tp.shape[0] == 24
    assert ("Jahr", "Werktag") in tp.columns
    assert ("Jul", "Sonntag") in tp.columns
    assert tp[("Jul", "Werktag")].max() == pytest.approx(5.0)

    wl = analysis.wochenlastgang(ps, col="Q_total")
    assert wl.shape == (24, 7)
    assert list(wl.columns) == analysis.WOCHENTAGE
    assert wl.to_numpy().min() > 0


def test_kennzahlen_und_bivalenzpunkt():
    ps, total = _konstruiertes_profil(5.0, 20.0)
    split = profiles_measured.split_tww_sommer(ps)
    split.meta["n_we"] = 12

    kz = analysis.kennzahlen(split)
    assert kz["q_total_kwh"] == pytest.approx(float(total.sum()))
    assert kz["p_max_kw"] == pytest.approx(25.0)
    assert kz["p_max_tww_kw"] == pytest.approx(5.0)
    assert kz["p_max_heiz_kw"] == pytest.approx(20.0)
    assert kz["vollaststunden_h"] == pytest.approx(
        kz["q_total_kwh"] / 25.0)
    assert 0 < kz["anteil_tww"] < 1
    assert 0 < kz["glf_din4708"] < 1

    # p_bei_jdl_anteil analytisch: konstante Last -> P = anteil * Last
    konstant = pd.Series(10.0, index=total.index)
    assert analysis.p_bei_jdl_anteil(konstant, 0.5) == pytest.approx(5.0, abs=1e-6)
    assert analysis.p_bei_jdl_anteil(konstant, 1.0) == pytest.approx(10.0)
    assert analysis.p_bei_jdl_anteil(konstant, 0.0) == 0.0

    # monoton in 'anteil' und deckt tatsächlich den geforderten Anteil
    p90 = analysis.p_bei_jdl_anteil(total, 0.90)
    p99 = analysis.p_bei_jdl_anteil(total, 0.99)
    assert p90 < p99 <= total.max()
    gedeckt = np.minimum(total.to_numpy(), p90).sum() / total.sum()
    assert gedeckt == pytest.approx(0.90, abs=1e-4)
