"""Auswertung von Lastgängen: JDL, Spitzen, Tages-/Wochenprofile, Kennzahlen.

Alle Funktionen arbeiten auf Leistungen in kW (Zeitreihe mit DatetimeIndex).
Energien ergeben sich mit dt [h] je Zeitschritt (``ProfileSet.dt_h``).
"""
from __future__ import annotations

from typing import Union

import numpy as np
import pandas as pd

from .models import ProfileSet

try:  # optional, nur für Feiertagsbehandlung
    import holidays as _holidays
except Exception:  # pragma: no cover - Paket optional
    _holidays = None

WOCHENTAGE = ["Montag", "Dienstag", "Mittwoch", "Donnerstag",
              "Freitag", "Samstag", "Sonntag"]
TAGTYPEN = ["Werktag", "Samstag", "Sonntag"]
MONATSNAMEN = {1: "Jan", 2: "Feb", 3: "Mär", 4: "Apr", 5: "Mai", 6: "Jun",
               7: "Jul", 8: "Aug", 9: "Sep", 10: "Okt", 11: "Nov", 12: "Dez"}


# ---------------------------------------------------------------- Helfer

def _as_frame(obj: Union[ProfileSet, pd.DataFrame, pd.Series]) -> pd.DataFrame:
    if isinstance(obj, ProfileSet):
        return obj.df
    if isinstance(obj, pd.Series):
        return obj.to_frame()
    if isinstance(obj, pd.DataFrame):
        return obj
    raise TypeError(f"ProfileSet/DataFrame/Series erwartet, nicht {type(obj)}")


def _series(obj: Union[ProfileSet, pd.DataFrame, pd.Series],
            col: str = "Q_total") -> pd.Series:
    """Spalte holen; 'Q_total' wird bei Bedarf aus Q_heiz + Q_tww gebildet."""
    if isinstance(obj, pd.Series):
        return obj.astype(float)
    if isinstance(obj, ProfileSet) and col == "Q_total":
        return obj.q_total.astype(float)
    df = _as_frame(obj)
    if col in df.columns:
        s = df[col].astype(float)
        s.name = col
        return s
    if col == "Q_total" and {"Q_heiz", "Q_tww"}.issubset(df.columns):
        s = (df["Q_heiz"] + df["Q_tww"]).astype(float)
        s.name = "Q_total"
        return s
    raise KeyError(f"Spalte '{col}' nicht vorhanden. Verfügbar: {list(df.columns)}")


def _tagtyp(index: pd.DatetimeIndex, feiertage: bool = True) -> pd.Series:
    """Tagtyp je Zeitstempel: Werktag / Samstag / Sonntag (Feiertag = Sonntag)."""
    wd = index.dayofweek
    typ = np.where(wd == 6, "Sonntag", np.where(wd == 5, "Samstag", "Werktag"))
    typ = pd.Series(typ, index=index, name="Tagtyp")
    if feiertage and _holidays is not None and len(index) > 0:
        try:
            jahre = sorted({int(y) for y in index.year.unique()})
            fh = _holidays.country_holidays("DE", years=jahre)
            ist_fh = pd.Series(index.normalize(), index=index).isin(
                pd.to_datetime(sorted(fh.keys())))
            typ = typ.mask(ist_fh, "Sonntag")
        except Exception:  # pragma: no cover
            pass
    return typ


# ------------------------------------------------------------------- JDL

def jdl(series: pd.Series) -> pd.Series:
    """Jahresdauerlinie: Werte absteigend sortiert.

    Index = Rang 1..N ('Stunde' bei stündlicher Auflösung), Werte in kW.
    """
    s = series.astype(float).dropna()
    werte = np.sort(s.to_numpy())[::-1]
    out = pd.Series(werte,
                    index=pd.RangeIndex(1, len(werte) + 1, name="Stunde"),
                    name=series.name or "kW")
    return out


def top_peaks(series: pd.Series, n: int = 20) -> pd.DataFrame:
    """Die n höchsten Lastwerte mit Zeitpunkt (absteigend).

    Spalten: 'Zeitpunkt', 'kW' (zusätzlich 'Wochentag', 'Stunde' als Kontext).
    """
    s = series.astype(float).dropna()
    n = int(min(max(n, 0), len(s)))
    top = s.sort_values(ascending=False).head(n)
    idx = pd.DatetimeIndex(top.index)
    out = pd.DataFrame({
        "Zeitpunkt": idx,
        "kW": top.to_numpy(dtype=float),
        "Wochentag": [WOCHENTAGE[d] for d in idx.dayofweek],
        "Stunde": idx.hour,
    })
    out.index = pd.RangeIndex(1, len(out) + 1, name="Rang")
    return out


# ------------------------------------------------------ Tages-/Wochenprofile

def tagesprofile(df: Union[ProfileSet, pd.DataFrame, pd.Series],
                 col: str = "Q_total") -> pd.DataFrame:
    """Mittlere Tagesprofile: Stunde (0..23) × (Monat, Tagtyp).

    Zeilen: Stunde des Tages 0..23.
    Spalten: MultiIndex (Monat, Tagtyp) mit Monat = 'Jahr' (alle Monate)
    sowie 'Jan'..'Dez'; Tagtyp = Werktag / Samstag / Sonntag.
    Werte: mittlere Leistung [kW].
    """
    s = _series(df, col)
    idx = pd.DatetimeIndex(s.index)
    hilfe = pd.DataFrame({
        "wert": s.to_numpy(dtype=float),
        "stunde": idx.hour,
        "monat": idx.month,
        "tagtyp": _tagtyp(idx).to_numpy(),
    })

    teile = {}
    jahr = hilfe.pivot_table(index="stunde", columns="tagtyp",
                             values="wert", aggfunc="mean")
    for t in TAGTYPEN:
        teile[("Jahr", t)] = jahr[t] if t in jahr.columns else np.nan

    for m in range(1, 13):
        sub = hilfe[hilfe["monat"] == m]
        if sub.empty:
            continue
        pv = sub.pivot_table(index="stunde", columns="tagtyp",
                             values="wert", aggfunc="mean")
        for t in TAGTYPEN:
            teile[(MONATSNAMEN[m], t)] = pv[t] if t in pv.columns else np.nan

    out = pd.DataFrame(teile, index=pd.RangeIndex(0, 24, name="Stunde"))
    out.columns = pd.MultiIndex.from_tuples(out.columns,
                                            names=["Monat", "Tagtyp"])
    return out


def wochenlastgang(df: Union[ProfileSet, pd.DataFrame, pd.Series],
                   col: str = "Q_total") -> pd.DataFrame:
    """Typische Woche: Mittelwert je Wochentag und Stunde.

    Zeilen: Stunde 0..23, Spalten: Montag..Sonntag, Werte in kW.
    (168er-Reihe für Plots: ``wochenlastgang(df).T.stack()``.)
    """
    s = _series(df, col)
    idx = pd.DatetimeIndex(s.index)
    hilfe = pd.DataFrame({
        "wert": s.to_numpy(dtype=float),
        "stunde": idx.hour,
        "wtag": idx.dayofweek,
    })
    pv = hilfe.pivot_table(index="stunde", columns="wtag",
                           values="wert", aggfunc="mean")
    pv = pv.reindex(index=range(24), columns=range(7))
    pv.columns = WOCHENTAGE
    pv.index = pd.RangeIndex(0, 24, name="Stunde")
    pv.columns.name = "Wochentag"
    return pv


# -------------------------------------------------------------- Kennzahlen

def p_bei_jdl_anteil(series: pd.Series, anteil: float) -> float:
    """Leistung P, die ``anteil`` (0..1) der Jahresarbeit deckt.

    Kleinstes P mit ``sum(min(last, P)) >= anteil * sum(last)`` — die Arbeit
    unterhalb der gekappten Jahresdauerlinie. Bivalenzpunkt-Hilfe:
    z. B. anteil=0.9 -> Leistung eines Erzeugers, der 90 % der Wärmearbeit
    liefert (Rest über Spitzenlasterzeuger). Numerisch per Bisektion.
    """
    v = series.astype(float).dropna().to_numpy()
    v = v[v > -np.inf]
    if v.size == 0:
        return 0.0
    v = np.clip(v, 0.0, None)
    gesamt = float(v.sum())
    if gesamt <= 0:
        return 0.0
    anteil = float(anteil)
    if anteil <= 0:
        return 0.0
    if anteil >= 1.0:
        return float(v.max())

    ziel = anteil * gesamt
    lo, hi = 0.0, float(v.max())
    for _ in range(200):
        mid = 0.5 * (lo + hi)
        if float(np.minimum(v, mid).sum()) >= ziel:
            hi = mid
        else:
            lo = mid
        if hi - lo <= 1e-9 * max(hi, 1.0):
            break
    return float(hi)


def kennzahlen(profile: ProfileSet) -> dict:
    """Kennzahlen-Dict eines ProfileSets.

    Enthält Jahressummen [kWh], Spitzenleistungen [kW] (gesamt/Heizung/TWW),
    Volllaststunden [h], TWW-Anteil, Bivalenz-Hilfswerte (P bei 90/95/99 %
    der Jahresarbeit) und — wenn ``meta['n_we']`` gesetzt ist — den
    Gleichzeitigkeitsfaktor nach DIN 4708 (``vendor.din4708.calc_GLF``).
    """
    df = profile.df
    dt = profile.dt_h
    q_heiz = df["Q_heiz"].astype(float) if "Q_heiz" in df else pd.Series(
        0.0, index=df.index)
    q_tww = df["Q_tww"].astype(float) if "Q_tww" in df else pd.Series(
        0.0, index=df.index)
    total = (q_heiz + q_tww).astype(float)

    e_heiz = float(q_heiz.sum() * dt)
    e_tww = float(q_tww.sum() * dt)
    e_total = e_heiz + e_tww
    p_max = float(total.max()) if len(total) else 0.0
    p_max_heiz = float(q_heiz.max()) if len(q_heiz) else 0.0
    p_max_tww = float(q_tww.max()) if len(q_tww) else 0.0

    out = {
        "n_schritte": int(len(df)),
        "aufloesung": profile.resolution,
        "dt_h": dt,
        "zeitraum": (df.index[0], df.index[-1]) if len(df) else (None, None),
        "q_heiz_kwh": e_heiz,
        "q_tww_kwh": e_tww,
        "q_total_kwh": e_total,
        "jahressummen": profile.annual_sums(),
        "p_max_kw": p_max,
        "p_max_heiz_kw": p_max_heiz,
        "p_max_tww_kw": p_max_tww,
        "p_mittel_kw": float(total.mean()) if len(total) else 0.0,
        "p_min_kw": float(total.min()) if len(total) else 0.0,
        "vollaststunden_h": (e_total / p_max) if p_max > 0 else 0.0,
        "vollaststunden_heiz_h": (e_heiz / p_max_heiz) if p_max_heiz > 0 else 0.0,
        "vollaststunden_tww_h": (e_tww / p_max_tww) if p_max_tww > 0 else 0.0,
        "anteil_tww": (e_tww / e_total) if e_total > 0 else 0.0,
        "p_90_prozent_kw": p_bei_jdl_anteil(total, 0.90),
        "p_95_prozent_kw": p_bei_jdl_anteil(total, 0.95),
        "p_99_prozent_kw": p_bei_jdl_anteil(total, 0.99),
    }

    n_we = profile.meta.get("n_we", profile.meta.get("N_WE"))
    if n_we:
        try:
            from vendor import din4708
            out["n_we"] = float(n_we)
            out["glf_din4708"] = float(din4708.calc_GLF(float(n_we)))
            out["w_z_kwh_din4708"] = float(din4708.W_z(float(n_we)) / 1000.0)
        except Exception as exc:  # pragma: no cover
            out["glf_din4708"] = None
            out["glf_fehler"] = str(exc)

    return out
