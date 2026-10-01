"""Generische Speicherbilanz-Simulation (Füllstandslogik wie SWSG-Excel).

Signaturen sind FIXIERT (API-Vertrag, sizing_buffer.py und app.py bauen darauf).

Bilanzmodell je Zeitschritt (Energiebilanz, kein Temperatur-/Schichtmodell):

    net[i]  = (P_gen_verf[i] - Q_last[i]) * dt          [kWh]
    SOC[i]  = clip(SOC[i-1] + net[i], 0, C)
    Unterdeckung[i] = max(0, -(SOC[i-1] + net[i])) / dt [kW]

Der Erzeuger deckt also zuerst die Last; ein Überschuss lädt den Speicher
(begrenzt durch die Kapazität C, keine Ladeleistungsbegrenzung), ein Defizit
wird aus dem Speicher gedeckt. Was der Speicher nicht mehr liefern kann, ist
Unterdeckung.
"""
from __future__ import annotations

from typing import Optional, Union

import numpy as np
import pandas as pd

from .models import SimResult

#: numerische Toleranz für "keine Unterdeckung" [kWh]
_EPS = 1e-9


def _to_array(x: Union[float, int, pd.Series, np.ndarray],
              index: pd.Index, name: str) -> np.ndarray:
    """Skalar oder Zeitreihe -> float-Array in der Länge/Reihenfolge von index."""
    if isinstance(x, pd.Series):
        s = x
        if not s.index.equals(index):
            s = s.reindex(index)
        arr = s.to_numpy(dtype=float)
        if np.isnan(arr).any():
            arr = np.nan_to_num(arr, nan=0.0)
    elif isinstance(x, np.ndarray):
        arr = np.asarray(x, dtype=float).ravel()
        if arr.size != len(index):
            raise ValueError(f"{name}: Länge {arr.size} != Lastgang {len(index)}")
    else:
        arr = np.full(len(index), float(x), dtype=float)
    return arr


def simulate(load: pd.Series,
             p_gen: Union[float, pd.Series],
             capacity_kwh: float,
             dt_h: float = 1.0,
             availability: Optional[pd.Series] = None,
             soc0: Optional[float] = None) -> SimResult:
    """Bilanziert Speicher-SOC über den Lastgang.

    load: Wärmelast [kW] je Zeitschritt (DatetimeIndex).
    p_gen: verfügbare Erzeugerleistung [kW] (konstant oder Zeitreihe).
    capacity_kwh: nutzbare Speicherkapazität.
    availability: 0/1-Maske (EVU-Sperrzeiten); None = immer verfügbar.
    soc0: Start-SOC [kWh]; None = capacity_kwh (voll).

    Logik je Schritt: Erzeuger deckt Last, Überschuss lädt (bis C),
    Defizit entlädt (bis 0); Rest = Unterdeckung [kW].
    """
    if not isinstance(load, pd.Series):
        load = pd.Series(load)
    if dt_h <= 0:
        raise ValueError("dt_h muss > 0 sein")
    capacity_kwh = float(max(capacity_kwh, 0.0))

    index = load.index
    n = len(index)
    q_last = np.nan_to_num(load.to_numpy(dtype=float), nan=0.0)
    p_nom = _to_array(p_gen, index, "p_gen")

    if availability is None:
        avail = np.ones(n, dtype=float)
    else:
        avail = _to_array(availability, index, "availability").astype(float)
        avail = np.clip(avail, 0.0, 1.0)

    p_verf = p_nom * avail

    soc_start = capacity_kwh if soc0 is None else float(
        min(max(soc0, 0.0), capacity_kwh))

    # Vektorisiert: Netto-Energie je Schritt [kWh]
    net = (p_verf - q_last) * dt_h

    soc = np.empty(n, dtype=float)
    deficit = np.zeros(n, dtype=float)  # kWh je Schritt

    s = soc_start
    for i in range(n):
        s_raw = s + net[i]
        if s_raw > capacity_kwh:
            s = capacity_kwh
        elif s_raw < 0.0:
            deficit[i] = -s_raw
            s = 0.0
        else:
            s = s_raw
        soc[i] = s

    deficit[deficit < _EPS] = 0.0

    bedarf_kwh = float(q_last.sum() * dt_h)
    unterdeckung_kwh = float(deficit.sum())
    n_steps = int((deficit > 0.0).sum())
    # Stunden mit Unterdeckung (bei dt_h = 1 h identisch mit der Schrittzahl)
    unterdeckung_h = int(np.ceil(n_steps * dt_h - 1e-12))

    if bedarf_kwh > 0:
        deckungsgrad = 1.0 - unterdeckung_kwh / bedarf_kwh
    else:
        deckungsgrad = 1.0

    df = pd.DataFrame(
        {
            "Q_last": q_last,
            "P_gen": p_nom,
            "P_gen_verf": p_verf,
            "SOC": soc,
            "Unterdeckung": deficit / dt_h,
        },
        index=index,
    )

    return SimResult(
        df=df,
        capacity_kwh=capacity_kwh,
        unterdeckung_kwh=unterdeckung_kwh,
        unterdeckung_h=unterdeckung_h,
        deckungsgrad=float(deckungsgrad),
    )


def simulate_zweipunkt(load: pd.Series,
                       p_gen: Union[float, pd.Series],
                       capacity_kwh: float,
                       dt_h: float = 1.0,
                       min_soc_frac: float = 0.2,
                       availability: Optional[pd.Series] = None,
                       soc0: float = 0.0) -> SimResult:
    """Zweipunkt-Betrieb (Hysterese) — Logik der SWSG-Referenz-Excel.

    Anders als :func:`simulate` (Erzeuger läuft durchgehend mit, „Durchlauf")
    kennt dieses Modell nur zwei Zustände:

    * **Laden** (``Laden = 1``): Der Erzeuger ist an und deckt die Last *und*
      lädt den Speicher, so schnell die verfügbare Leistung es zulässt:
      ``P = min(Q_last + (C - SOC)/dt, P_max)``. Ist der Speicher voll,
      wird ab dem nächsten Schritt entladen.
    * **Entladen** (``Laden = 0``): Der Erzeuger ist **aus**, die Last wird
      allein aus dem Speicher gedeckt. Würde der Füllstand dabei unter den
      Mindestfüllstand ``MINF = min_soc_frac · C`` fallen, wird **im selben
      Zeitschritt** auf Laden umgeschaltet.

    Unterdeckung entsteht nur im Ladebetrieb, wenn die verfügbare Leistung die
    Last nicht deckt und der Speicher dabei den Mindestfüllstand erreicht; der
    Füllstand wird dann auf ``MINF`` gehalten und die Restleistung als
    Unterdeckung [kW] ausgewiesen.

    Args:
        load: Wärmelast [kW] je Zeitschritt (DatetimeIndex).
        p_gen: nominelle Erzeugerleistung [kW] (konstant oder Zeitreihe).
        capacity_kwh: nutzbare Speicherkapazität ``C``.
        dt_h: Zeitschrittweite [h].
        min_soc_frac: Mindestfüllstand als Anteil von ``C`` (Default 0,2).
        availability: 0/1-Maske (EVU-Sperrzeiten); ``P_max = p_gen · avail``.
        soc0: Start-Füllstand [kWh] (Default 0 = leer, Start im Ladebetrieb).

    Returns:
        SimResult mit ``df``-Spalten wie :func:`simulate` plus ``Laden`` (0/1).
        ``P_gen`` ist die nominelle, ``P_gen_verf`` die im Schritt tatsächlich
        abgegebene Erzeugerleistung (0 im Entladebetrieb).
        ``ladezyklen`` = Anzahl der Wechsel Entladen -> Laden.
    """
    if not isinstance(load, pd.Series):
        load = pd.Series(load)
    if dt_h <= 0:
        raise ValueError("dt_h muss > 0 sein")
    capacity_kwh = float(max(capacity_kwh, 0.0))
    min_soc_frac = float(min(max(min_soc_frac, 0.0), 1.0))

    index = load.index
    n = len(index)
    q_last = np.nan_to_num(load.to_numpy(dtype=float), nan=0.0)
    p_nom = _to_array(p_gen, index, "p_gen")

    if availability is None:
        avail = np.ones(n, dtype=float)
    else:
        avail = np.clip(_to_array(availability, index, "availability").astype(float),
                        0.0, 1.0)
    p_max = p_nom * avail

    c = capacity_kwh
    minf = min_soc_frac * c

    soc_arr = np.empty(n, dtype=float)
    p_arr = np.zeros(n, dtype=float)
    deficit = np.zeros(n, dtype=float)   # kW je Schritt
    laden_arr = np.zeros(n, dtype=int)

    soc = float(min(max(soc0, 0.0), c))
    mode = 1          # 1 = Laden, 0 = Entladen; Start: Laden
    zyklen = 0

    for i in range(n):
        last = q_last[i]
        u = 0.0
        p = 0.0
        geladen = False

        if mode == 0:                       # Entladen: Erzeuger aus
            nxt = soc - last * dt_h
            if nxt < minf:
                mode = 1                    # im SELBEN Schritt umschalten
                zyklen += 1
            else:
                soc = nxt
                p = 0.0

        if mode == 1:                       # Laden: Last decken + auffüllen
            geladen = True
            p = min(last + (c - soc) / dt_h, p_max[i])
            nxt = soc + (p - last) * dt_h
            if nxt < minf:                  # Füllstand auf MINF halten
                u = (minf - nxt) / dt_h
                nxt = minf
            soc = min(nxt, c)
            if soc >= c - 1e-9:
                mode = 0                    # voll -> ab nächstem Schritt entladen

        soc_arr[i] = soc
        p_arr[i] = p
        deficit[i] = u
        laden_arr[i] = 1 if geladen else 0

    deficit[deficit < _EPS] = 0.0

    bedarf_kwh = float(q_last.sum() * dt_h)
    unterdeckung_kwh = float(deficit.sum() * dt_h)
    n_steps = int((deficit > 0.0).sum())
    unterdeckung_h = int(np.ceil(n_steps * dt_h - 1e-12))
    deckungsgrad = (1.0 - unterdeckung_kwh / bedarf_kwh) if bedarf_kwh > 0 else 1.0

    df = pd.DataFrame(
        {
            "Q_last": q_last,
            "P_gen": p_nom,
            "P_gen_verf": p_arr,
            "SOC": soc_arr,
            "Unterdeckung": deficit,
            "Laden": laden_arr,
        },
        index=index,
    )

    return SimResult(
        df=df,
        capacity_kwh=capacity_kwh,
        unterdeckung_kwh=unterdeckung_kwh,
        unterdeckung_h=unterdeckung_h,
        deckungsgrad=float(deckungsgrad),
        ladezyklen=int(zyklen),
    )


def find_min_capacity(load: pd.Series,
                      p_gen: Union[float, pd.Series],
                      dt_h: float = 1.0,
                      availability: Optional[pd.Series] = None,
                      ziel_deckung: float = 1.0,
                      c_max_kwh: float = 50000.0) -> SimResult:
    """Binärsuche: kleinste Kapazität mit Deckungsgrad >= ziel_deckung.

    Gibt SimResult der gefundenen Kapazität zurück. Wenn selbst c_max_kwh
    nicht reicht (P_gen zu klein), SimResult mit capacity_kwh=c_max_kwh
    zurückgeben — Aufrufer prüft .ok().
    """
    tol_kwh = 1.0
    max_iter = 40

    def run(c: float) -> SimResult:
        return simulate(load, p_gen, c, dt_h=dt_h, availability=availability)

    sim_lo = run(0.0)
    if sim_lo.ok(ziel_deckung):
        return sim_lo

    sim_hi = run(float(c_max_kwh))
    if not sim_hi.ok(ziel_deckung):
        return sim_hi  # selbst c_max reicht nicht -> Aufrufer prüft .ok()

    lo, hi = 0.0, float(c_max_kwh)
    best = sim_hi
    for _ in range(max_iter):
        if hi - lo <= tol_kwh:
            break
        mid = 0.5 * (lo + hi)
        sim = run(mid)
        if sim.ok(ziel_deckung):
            hi = mid
            best = sim
        else:
            lo = mid
    return best
