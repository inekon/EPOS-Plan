"""Import gemessener Lastgänge (CSV/XLSX) und Heizung/TWW-Split.

Referenzfall: SWSG-Messdaten — stündliche kW-Werte, Spalten 'Datum' /
'Lastgang Wärmebedarf', deutsches Dezimalkomma, Semikolon-getrennt.

Ablauf ``import_lastgang``:

1. Datei lesen (CSV oder Excel; Excel via openpyxl/pandas).
2. Zeitstempel parsen (dayfirst, tz-naiv), sortieren, Duplikate mitteln.
3. Auflösung automatisch aus dem Median der Zeitstempel-Differenzen.
4. Werte robust nach float (deutsches Dezimalkomma / Tausenderpunkt).
5. kWh je Intervall -> kW (Division durch dt_h), wenn ``value_unit='kWh'``.
6. Auf das regelmäßige Raster reindizieren; Lücken < 6 h linear
   interpolieren, größere mit 0 füllen und in ``meta['luecken']`` ausweisen.
7. Energieerhaltend auf 1 h resampeln (Downsampling: Mittelwert der
   kW-Werte; Upsampling: ffill, kW ist im Intervall konstant).

Ergebnis: ``ProfileSet`` mit Spalten ``Q_heiz`` / ``Q_tww`` [kW],
``resolution='h'``, ``source='gemessen'``.
"""
from __future__ import annotations

import os
from pathlib import Path
from typing import Optional, Union

import numpy as np
import pandas as pd

from .models import ProfileSet

#: Lücken bis zu dieser Dauer werden linear interpoliert [h]
LUECKE_INTERP_MAX_H = 6.0

_EXCEL_SUFFIXES = (".xlsx", ".xlsm", ".xltx", ".xltm", ".xls", ".xlsb", ".ods")


# --------------------------------------------------------------------- Helfer

def _filename(file) -> str:
    if isinstance(file, (str, os.PathLike)):
        return str(file)
    return str(getattr(file, "name", "") or "")


def _rewind(file) -> None:
    """Datei-artige Objekte (Streamlit-Upload, BytesIO) zurückspulen."""
    if hasattr(file, "seek"):
        try:
            file.seek(0)
        except Exception:
            pass


def _read_table(file, sep: Optional[str], decimal: str,
                sheet_name: Union[int, str]) -> pd.DataFrame:
    """CSV oder Excel einlesen (Format über Endung, sonst Fallback)."""
    suffix = Path(_filename(file)).suffix.lower()

    def _excel() -> pd.DataFrame:
        _rewind(file)
        df = pd.read_excel(file, sheet_name=sheet_name)
        if isinstance(df, dict):  # sheet_name=None
            df = next(iter(df.values()))
        return df

    def _csv() -> pd.DataFrame:
        _rewind(file)
        kwargs = {"decimal": decimal}
        if sep is None:
            kwargs["sep"] = None
            kwargs["engine"] = "python"
        else:
            kwargs["sep"] = sep
        return pd.read_csv(file, **kwargs)

    if suffix in _EXCEL_SUFFIXES:
        return _excel()
    if suffix in (".csv", ".txt", ".dat", ".tsv"):
        return _csv()
    # unbekannte/fehlende Endung: erst CSV, dann Excel probieren
    try:
        return _csv()
    except Exception:
        return _excel()


def _to_numeric(s: pd.Series, decimal: str) -> pd.Series:
    """Robuste Zahlkonvertierung inkl. deutschem Dezimalkomma."""
    if pd.api.types.is_numeric_dtype(s):
        return s.astype(float)
    txt = s.astype("string").str.strip()
    txt = txt.str.replace(" ", "", regex=False)
    txt = txt.str.replace(" ", "", regex=False)
    if decimal == ",":
        txt = txt.str.replace(".", "", regex=False)   # Tausenderpunkt
        txt = txt.str.replace(",", ".", regex=False)
    else:
        txt = txt.str.replace(",", "", regex=False)   # Tausenderkomma
    txt = txt.replace({"": None, "-": None, "n/a": None, "NA": None})
    return pd.to_numeric(txt, errors="coerce").astype(float)


def _to_datetime(s: pd.Series) -> pd.DatetimeIndex:
    """Zeitstempel parsen (deutsche Schreibweise bevorzugt), tz-naiv."""
    if pd.api.types.is_datetime64_any_dtype(s):
        ts = pd.to_datetime(s)
    else:
        try:
            ts = pd.to_datetime(s, dayfirst=True)
        except (ValueError, TypeError):
            ts = pd.to_datetime(s, dayfirst=True, format="mixed", errors="coerce")
    idx = pd.DatetimeIndex(ts)
    if idx.tz is not None:
        idx = idx.tz_localize(None)
    return idx


def _detect_resolution(index: pd.DatetimeIndex) -> tuple[str, float]:
    """Auflösung aus dem Median der Zeitstempel-Differenzen -> (freq, dt_h)."""
    if len(index) < 2:
        return "h", 1.0
    diffs = pd.Series(index).diff().dropna()
    diffs = diffs[diffs > pd.Timedelta(0)]
    if diffs.empty:
        return "h", 1.0
    minutes = float(np.median(diffs.dt.total_seconds().to_numpy())) / 60.0
    m = int(round(minutes))
    if m <= 0:
        m = 60
    if m % 60 == 0:
        h = m // 60
        freq = "h" if h == 1 else f"{h}h"
    else:
        freq = f"{m}min"
    return freq, m / 60.0


def _runs(mask: np.ndarray) -> list[tuple[int, int]]:
    """Zusammenhängende True-Bereiche als (start, stop_exklusiv)."""
    out: list[tuple[int, int]] = []
    if mask.size == 0:
        return out
    diff = np.diff(mask.astype(np.int8))
    starts = list(np.flatnonzero(diff == 1) + 1)
    stops = list(np.flatnonzero(diff == -1) + 1)
    if mask[0]:
        starts.insert(0, 0)
    if mask[-1]:
        stops.append(mask.size)
    return list(zip(starts, stops))


def _fill_gaps(s: pd.Series, dt_h: float) -> tuple[pd.Series, list[dict]]:
    """Lücken schließen: < 6 h linear interpolieren, größere mit 0 füllen."""
    report: list[dict] = []
    mask = s.isna().to_numpy()
    if not mask.any():
        return s.astype(float), report

    interp = s.interpolate(method="time", limit_area="inside")
    out = s.copy()
    for a, b in _runs(mask):
        dauer_h = (b - a) * dt_h
        randlage = (a == 0) or (b == len(s))
        if dauer_h < LUECKE_INTERP_MAX_H and not randlage:
            out.iloc[a:b] = interp.iloc[a:b]
            behandlung = "linear interpoliert"
        else:
            out.iloc[a:b] = 0.0
            behandlung = "mit 0 gefüllt (im Bericht ausgewiesen)"
        report.append({
            "start": s.index[a],
            "ende": s.index[b - 1],
            "n_schritte": int(b - a),
            "dauer_h": float(dauer_h),
            "behandlung": behandlung,
        })
    return out.astype(float), report


def _resample_1h(s: pd.Series, dt_h: float) -> pd.Series:
    """Energieerhaltend auf Stundenwerte [kW] bringen."""
    if abs(dt_h - 1.0) < 1e-9:
        return s.astype(float)
    if dt_h < 1.0:
        # Mittelwert der kW-Werte == Energieerhaltung bei gleichen Teilschritten
        return s.resample("h").mean().astype(float)
    # gröber als 1 h: kW ist im Intervall konstant -> ffill über das Intervall
    end = s.index[-1] + pd.Timedelta(hours=dt_h) - pd.Timedelta(hours=1)
    full = pd.date_range(s.index[0], end, freq="h")
    return s.reindex(full).ffill().astype(float)


# ------------------------------------------------------------------- Import

def import_lastgang(file,
                    timestamp_col: str,
                    value_col: str,
                    value_unit: str = "kW",
                    tww_col: Optional[str] = None,
                    decimal: str = ",",
                    sep: Optional[str] = None,
                    sheet_name: Union[int, str] = 0) -> ProfileSet:
    """Gemessenen Lastgang aus CSV/Excel einlesen.

    Args:
        file: Pfad oder datei-artiges Objekt (z. B. Streamlit-Upload).
        timestamp_col: Spaltenname der Zeitstempel (z. B. 'Datum').
        value_col: Spalte mit der Wärmelast (Gesamtwärme).
        value_unit: 'kW' (Momentanleistung) oder 'kWh' (Energie je Intervall).
        tww_col: optional Spalte mit dem in ``value_col`` **enthaltenen**
            TWW-Anteil; dann Q_tww = tww_col und Q_heiz = value_col - tww_col
            (auf >= 0 begrenzt). Ohne Angabe: Q_tww = 0, Split später über
            :func:`split_tww_sommer`.
        decimal: Dezimaltrennzeichen der Datei (Default ',' = deutsch).
        sep: CSV-Trennzeichen; None = automatisch erkennen.
        sheet_name: Excel-Blatt (Name oder Index).

    Returns:
        ProfileSet mit stündlichen kW-Werten, ``source='gemessen'`` und
        einem Import-/Lückenreport in ``meta``.
    """
    if value_unit not in ("kW", "kWh"):
        raise ValueError("value_unit muss 'kW' oder 'kWh' sein")

    raw = _read_table(file, sep, decimal, sheet_name)
    raw.columns = [str(c).strip() for c in raw.columns]

    for col in [timestamp_col, value_col] + ([tww_col] if tww_col else []):
        if col not in raw.columns:
            raise KeyError(
                f"Spalte '{col}' nicht gefunden. Verfügbar: {list(raw.columns)}")

    idx = _to_datetime(raw[timestamp_col])
    val = _to_numeric(raw[value_col], decimal)
    tww = _to_numeric(raw[tww_col], decimal) if tww_col else None

    data = pd.DataFrame({"val": val.to_numpy(dtype=float)}, index=idx)
    if tww is not None:
        data["tww"] = tww.to_numpy(dtype=float)

    n_roh = len(data)
    ungueltige_ts = int(data.index.isna().sum())
    data = data[~data.index.isna()]
    data = data.sort_index()

    n_dupl = int(data.index.duplicated().sum())
    if n_dupl:
        data = data.groupby(level=0).mean()

    if data.empty:
        raise ValueError("Keine gültigen Datenzeilen gefunden.")

    freq_src, dt_src = _detect_resolution(data.index)

    # regelmäßiges Raster; unpassende Zeitstempel werden gezählt
    full = pd.date_range(data.index[0], data.index[-1], freq=freq_src)
    n_offgrid = int((~data.index.isin(full)).sum())
    data = data.reindex(full)

    warnungen: list[str] = []
    if ungueltige_ts:
        warnungen.append(f"{ungueltige_ts} Zeile(n) mit unlesbarem Zeitstempel verworfen")
    if n_dupl:
        warnungen.append(f"{n_dupl} doppelte Zeitstempel gemittelt")
    if n_offgrid:
        warnungen.append(
            f"{n_offgrid} Zeitstempel passen nicht ins {freq_src}-Raster "
            "(z. B. Zeitumstellung) und wurden verworfen")

    series = {"val": data["val"]}
    if tww is not None:
        series["tww"] = data["tww"]

    luecken: list[dict] = []
    gefuellt: dict[str, pd.Series] = {}
    for key, s in series.items():
        s_f, rep = _fill_gaps(s, dt_src)
        gefuellt[key] = s_f
        if key == "val":
            luecken = rep

    # Einheit -> kW
    if value_unit == "kWh":
        for key in gefuellt:
            gefuellt[key] = gefuellt[key] / dt_src

    # auf 1 h resampeln (energieerhaltend)
    stuendlich = {k: _resample_1h(v, dt_src) for k, v in gefuellt.items()}

    q_total = stuendlich["val"].astype(float)
    if tww is not None:
        q_tww = stuendlich["tww"].reindex(q_total.index).fillna(0.0).clip(lower=0.0)
        q_tww = pd.Series(np.minimum(q_tww.to_numpy(), q_total.to_numpy()),
                          index=q_total.index)
        n_clip = int((stuendlich["tww"].reindex(q_total.index).fillna(0.0)
                      > q_total + 1e-9).sum())
        if n_clip:
            warnungen.append(
                f"{n_clip} Stunde(n): TWW > Gesamtwärme -> auf Gesamtwärme begrenzt")
        q_heiz = (q_total - q_tww).clip(lower=0.0)
    else:
        q_tww = pd.Series(0.0, index=q_total.index)
        q_heiz = q_total.clip(lower=0.0)

    n_negativ = int((q_total < 0).sum())
    if n_negativ:
        warnungen.append(f"{n_negativ} negative Lastwerte auf 0 begrenzt")

    df = pd.DataFrame({"Q_heiz": q_heiz, "Q_tww": q_tww})
    df.index.name = "Zeit"

    luecken_h = float(sum(r["dauer_h"] for r in luecken))
    meta = {
        "datei": _filename(file) or "<stream>",
        "quelle": "Messdaten-Import",
        "spalten": {"zeit": timestamp_col, "wert": value_col, "tww": tww_col},
        "einheit_quelle": value_unit,
        "aufloesung_quelle": freq_src,
        "dt_quelle_h": dt_src,
        "zeitraum": (df.index[0], df.index[-1]),
        "n_zeilen_roh": n_roh,
        "n_stunden": int(len(df)),
        "duplikate": n_dupl,
        "luecken": luecken,
        "luecken_anzahl": len(luecken),
        "luecken_h": luecken_h,
        "luecken_interpoliert": sum(
            1 for r in luecken if r["behandlung"].startswith("linear")),
        "luecken_offen": sum(
            1 for r in luecken if not r["behandlung"].startswith("linear")),
        "warnungen": warnungen,
        "jahressumme_kwh": float(df.sum(axis=1).sum()),
        "p_max_kw": float(df.sum(axis=1).max()),
    }

    return ProfileSet(df=df, resolution="h", source="gemessen", meta=meta)


# ------------------------------------------------------- Heizung/TWW-Split

def split_tww_sommer(profile: ProfileSet,
                     monate: tuple = (6, 7, 8),
                     faktor: float = 1.0) -> ProfileSet:
    """Sommer-Baseline-Split: TWW+Zirkulation als ganzjähriges Grundband.

    Die mittlere Last der Sommermonate (nur Stunden > 0 gehen in den
    Mittelwert ein, damit Messlücken/Abschaltungen das Band nicht verfälschen)
    wird mit ``faktor`` skaliert und ganzjährig als ``Q_tww`` angesetzt,
    je Stunde gedeckelt auf die tatsächliche Gesamtlast. Der Rest ist
    ``Q_heiz``. Energieerhaltend: Q_heiz + Q_tww == Gesamtlast.

    Args:
        profile: ProfileSet (Gesamtlast = Q_heiz + Q_tww).
        monate: Monate der Sommer-Baseline (Default Jun/Jul/Aug).
        faktor: Korrekturfaktor auf das Band (z. B. 0.9 für saisonale
            Korrektur der Zirkulationsverluste).

    Returns:
        Neues ProfileSet mit aufgeteilten Spalten; ``meta['tww_split']``
        dokumentiert Band und Parameter.
    """
    total = profile.q_total.astype(float)
    if total.empty:
        raise ValueError("Leeres Profil")

    monate = tuple(int(m) for m in monate)
    sommer = total[total.index.month.isin(monate)]
    aktiv = sommer[sommer > 0]
    if aktiv.empty:
        band = 0.0
    else:
        band = float(aktiv.mean()) * float(faktor)
    band = max(band, 0.0)

    q_tww = pd.Series(np.minimum(total.to_numpy(dtype=float), band),
                      index=total.index).clip(lower=0.0)
    q_heiz = (total - q_tww).clip(lower=0.0)

    df = pd.DataFrame({"Q_heiz": q_heiz, "Q_tww": q_tww})
    df.index.name = profile.df.index.name

    dt = profile.dt_h
    meta = dict(profile.meta)
    meta["tww_split"] = {
        "methode": "Sommer-Baseline (mittlere Sommerlast > 0)",
        "monate": monate,
        "faktor": float(faktor),
        "band_kw": band,
        "n_sommerstunden": int(len(sommer)),
        "n_sommerstunden_aktiv": int(len(aktiv)),
        "q_tww_kwh": float(q_tww.sum() * dt),
        "q_heiz_kwh": float(q_heiz.sum() * dt),
        "anteil_tww": float(q_tww.sum() / total.sum()) if total.sum() > 0 else 0.0,
    }

    return ProfileSet(df=df, resolution=profile.resolution,
                      source=profile.source, meta=meta)
