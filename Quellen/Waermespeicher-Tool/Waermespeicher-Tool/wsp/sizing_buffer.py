"""Auslegung des Heizungs-Pufferspeichers — vier Ansätze im Vergleich.

Siehe DESIGN.md Abschnitt 4:

1. **Abtauung** (Luft/Wasser-WP): ``V = v_spez · P_WP`` (Default 20 l/kW).
2. **Taktung**: ``V = P_WP,min · t_min / (1,163 · ΔT_puffer) · 1000``.
3. **EVU-Sperrzeit**: ``V = Q̄_sperr · t_sperr · 1000/(1,163 · ΔT_puffer)`` mit
   Q̄_sperr = maximale rollierende Mittelleistung über die Sperrdauer im
   Lastgang (ohne Profil: ``P_WP`` als Ersatzwert).
4. **Lastgang-Simulation**: Binärsuche über die Kapazität
   (``storage_sim.find_min_capacity``) mit ``p_gen = p_wp + p_bivalent`` und
   den Sperrzeiten als Verfügbarkeitsmaske.

Maßgebend ist das Kriterium mit dem größten Volumen; die Empfehlung wird auf
marktübliche Speichergrößen aufgerundet.
"""
from __future__ import annotations

from typing import Optional

import numpy as np
import pandas as pd

from . import storage_sim
from .models import (BufferParams, BufferResult, energy_to_volume,
                     round_to_standard_volume)

#: Praktische Obergrenze eines Heizungs-Pufferspeichers [l]. Darüber ist
#: ein Pufferspeicher baulich/wirtschaftlich nicht mehr darstellbar — das
#: Ergebnis ist dann rechnerisch richtig, aber als Auslegung unbrauchbar.
PRAXIS_GRENZE_L = 100_000.0

KRIT_ABTAU = "Abtauung"
KRIT_TAKT = "Taktung"
KRIT_SPERR = "EVU-Sperrzeit"
KRIT_SIM = "Lastgang-Simulation"


# ------------------------------------------------------------ Hilfsfunktionen

def availability_mask(index: pd.DatetimeIndex, sperrzeiten: list) -> pd.Series:
    """0/1-Verfügbarkeitsmaske aus täglich wiederkehrenden Sperrfenstern.

    ``sperrzeiten``: Liste von ``(start_hh, dauer_h)``, z. B. ``[(11, 2), (17, 2)]``.
    Fenster gilt als ``[start, start + dauer)`` in Stunden des Tages;
    Überlauf über Mitternacht wird umgebrochen.
    """
    mask = pd.Series(1.0, index=index)
    if not sperrzeiten:
        return mask
    stunde = index.hour + index.minute / 60.0 + index.second / 3600.0
    for eintrag in sperrzeiten:
        start_hh, dauer_h = float(eintrag[0]), float(eintrag[1])
        if dauer_h <= 0:
            continue
        if dauer_h >= 24:
            mask[:] = 0.0
            continue
        ende = start_hh + dauer_h
        if ende <= 24.0:
            im_fenster = (stunde >= start_hh) & (stunde < ende)
        else:  # Umbruch über Mitternacht
            im_fenster = (stunde >= start_hh) | (stunde < ende - 24.0)
        mask[np.asarray(im_fenster)] = 0.0
    return mask


def max_mittelleistung(load: pd.Series, dauer_h: float,
                       dt_h: float = 1.0) -> float:
    """Größte rollierende Mittelleistung [kW] über ein Fenster von ``dauer_h``."""
    if load is None or len(load) == 0 or dauer_h <= 0:
        return 0.0
    fenster = max(int(round(dauer_h / dt_h)), 1)
    if fenster > len(load):
        return float(load.mean())
    return float(load.astype(float).rolling(fenster).mean().max())


# ------------------------------------------------------------- Einzelkriterien

def v_abtauung(params: BufferParams) -> float:
    """Abtauvolumen: v_spez [l/kW] × P_WP. 0 bei Sole/Wasser (abtau_l_pro_kw=0)."""
    return max(params.abtau_l_pro_kw, 0.0) * max(params.p_wp, 0.0)


def v_taktung(params: BufferParams) -> float:
    """Mindestvolumen für die geforderte Mindestlaufzeit bei kleinster Leistung."""
    q_kwh = max(params.p_wp_min, 0.0) * max(params.t_min_lauf_min, 0.0) / 60.0
    return energy_to_volume(q_kwh, params.dt_puffer)


def v_sperrzeit(params: BufferParams, profile_heiz: Optional[pd.Series] = None,
                dt_h: float = 1.0) -> tuple[float, dict]:
    """Volumen zur Überbrückung des ungünstigsten EVU-Sperrfensters."""
    info: dict = {"fenster": []}
    if not params.sperrzeiten:
        info["hinweis"] = "Keine Sperrzeiten definiert — Kriterium entfällt."
        return 0.0, info

    v_max = 0.0
    for eintrag in params.sperrzeiten:
        start_hh, dauer_h = float(eintrag[0]), float(eintrag[1])
        if dauer_h <= 0:
            continue
        if profile_heiz is not None and len(profile_heiz) > 0:
            q_mittel = max_mittelleistung(profile_heiz, dauer_h, dt_h)
            quelle = "Lastgang (max. rollierende Mittelleistung)"
        else:
            q_mittel = max(params.p_wp, 0.0)
            quelle = "Ersatzwert P_WP (kein Lastgang)"
        q_kwh = q_mittel * dauer_h
        v = energy_to_volume(q_kwh, params.dt_puffer)
        info["fenster"].append({
            "start_hh": start_hh,
            "dauer_h": dauer_h,
            "q_mittel_kW": q_mittel,
            "q_kWh": q_kwh,
            "v_l": v,
            "quelle": quelle,
        })
        v_max = max(v_max, v)
    return v_max, info


# ------------------------------------------------------- Betriebs-Simulation

def betriebssimulation(params: BufferParams,
                       profile: pd.Series,
                       capacity_kwh: float,
                       dt_h: float = 1.0,
                       min_soc_frac: float = 0.2):
    """Zweipunkt-Betriebssimulation eines **gewählten** Speichers.

    Ergänzt :func:`size_buffer` (Frage: „wie groß muss der Speicher sein?")
    um die Frage „wie verhält sich ein konkret gewählter Speicher im
    Zweipunktbetrieb?" — insbesondere die Taktung (Ladezyklen je Jahr).

    Dünner Wrapper um :func:`storage_sim.simulate_zweipunkt`:
    ``p_gen = p_wp + p_bivalent``, Sperrzeiten als Verfügbarkeitsmaske.

    Args:
        params: Puffer-/Erzeugerparameter (P_WP, P_bivalent, Sperrzeiten).
        profile: Wärmelast [kW] mit DatetimeIndex.
        capacity_kwh: nutzbare Kapazität des gewählten Speichers.
        dt_h: Zeitschrittweite des Profils [h].
        min_soc_frac: Mindestfüllstand als Anteil der Kapazität (Default 0,2).

    Returns:
        SimResult mit ``ladezyklen`` und der Spalte ``Laden`` im ``df``.
    """
    if profile is None or len(profile) == 0:
        raise ValueError("betriebssimulation braucht einen Lastgang")

    last = pd.Series(profile).astype(float).fillna(0.0)
    p_gen = max(params.p_wp, 0.0) + max(params.p_bivalent, 0.0)
    maske = availability_mask(last.index, params.sperrzeiten) \
        if isinstance(last.index, pd.DatetimeIndex) else None

    return storage_sim.simulate_zweipunkt(
        last, p_gen, float(capacity_kwh), dt_h=dt_h,
        min_soc_frac=min_soc_frac, availability=maske, soc0=0.0)


# ------------------------------------------------------------------ Hauptroutine

def size_buffer(params: BufferParams,
                profile_heiz: Optional[pd.Series] = None,
                dt_h: float = 1.0) -> BufferResult:
    """Legt den Pufferspeicher nach vier Kriterien aus und vergleicht sie.

    Args:
        params: Puffer-/Erzeugerparameter.
        profile_heiz: Heizlastgang [kW] mit DatetimeIndex; ``None`` -> die
            Simulation entfällt und die Sperrzeit wird mit ``p_wp`` gerechnet.
        dt_h: Zeitschrittweite des Profils in Stunden.

    Returns:
        BufferResult mit den vier Volumina, dem maßgebenden Kriterium und der
        auf Marktgrößen gerundeten Empfehlung.
    """
    if params.dt_puffer <= 0:
        raise ValueError("dt_puffer muss > 0 sein")

    details: dict = {"hinweise": [], "warnungen": []}

    # --- 1) Abtauung -------------------------------------------------------
    v_abtau = v_abtauung(params)
    details["abtauung"] = {
        "v_spez_l_pro_kW": params.abtau_l_pro_kw,
        "p_wp_kW": params.p_wp,
        "v_l": v_abtau,
        "methode": "V = v_spez · P_WP (Luft/Wasser-WP; 0 l/kW bei Sole/Wasser)",
    }

    # --- 2) Taktung --------------------------------------------------------
    v_takt = v_taktung(params)
    details["taktung"] = {
        "p_wp_min_kW": params.p_wp_min,
        "t_min_lauf_min": params.t_min_lauf_min,
        "q_kWh": params.p_wp_min * params.t_min_lauf_min / 60.0,
        "dt_puffer_K": params.dt_puffer,
        "v_l": v_takt,
        "methode": "V = P_WP,min · t_min / (1,163 · ΔT_puffer) · 1000",
    }

    # --- 3) EVU-Sperrzeit --------------------------------------------------
    v_sperr, sperr_info = v_sperrzeit(params, profile_heiz, dt_h)
    sperr_info["v_l"] = v_sperr
    details["sperrzeit"] = sperr_info

    # --- 4) Lastgang-Simulation -------------------------------------------
    v_sim = 0.0
    c_sim = 0.0
    sim = None
    sim_gueltig = False
    p_gen = max(params.p_wp, 0.0) + max(params.p_bivalent, 0.0)
    sim_info: dict = {"p_gen_kW": p_gen, "ziel_deckung": params.ziel_deckung}

    if profile_heiz is not None and len(profile_heiz) > 0:
        last = profile_heiz.astype(float).fillna(0.0)
        maske = availability_mask(last.index, params.sperrzeiten) \
            if isinstance(last.index, pd.DatetimeIndex) else None
        p_max_last = float(last.max())
        sim_info["p_max_last_kW"] = p_max_last
        sim_info["jahresbedarf_kWh"] = float(last.sum() * dt_h)
        if maske is not None:
            sim_info["sperrzeit_anteil"] = float(1.0 - maske.mean())

        try:
            sim = storage_sim.find_min_capacity(
                last, p_gen, dt_h=dt_h, availability=maske,
                ziel_deckung=params.ziel_deckung)
            c_sim = float(sim.capacity_kwh)
            v_sim = energy_to_volume(c_sim, params.dt_puffer)
            sim_info["status"] = "ok"
            sim_info["deckungsgrad"] = float(sim.deckungsgrad)
            sim_info["unterdeckung_kWh"] = float(sim.unterdeckung_kwh)
            sim_info["unterdeckung_h"] = int(sim.unterdeckung_h)
            sim_gueltig = bool(sim.ok(params.ziel_deckung))
            sim_info["gueltig"] = sim_gueltig
            if not sim_gueltig:
                details["warnungen"].append(
                    f"Auch mit der maximal geprüften Kapazität "
                    f"({c_sim:.0f} kWh) wird der Ziel-Deckungsgrad "
                    f"{params.ziel_deckung:.2f} nicht erreicht — die "
                    f"Erzeugerleistung P_gen = {p_gen:.1f} kW ist zu klein; "
                    "der Speicher kann eine fehlende Jahresarbeit nicht ersetzen. "
                    "Die Simulation geht deshalb NICHT in die Empfehlung ein "
                    "(v_sim_l ist nur der Wert an der Suchobergrenze)."
                )
        except NotImplementedError:
            sim_info["status"] = ("storage_sim.find_min_capacity noch nicht "
                                  "implementiert (NotImplementedError) — "
                                  "Simulation übersprungen")
            details["hinweise"].append(sim_info["status"])

        # Hinweis: Speicher deckt Leistungsspitzen ab
        if p_gen > 0 and p_gen < p_max_last:
            fehlend = p_max_last - p_gen
            details["hinweise"].append(
                f"P_gen = {p_gen:.1f} kW < Spitzenlast {p_max_last:.1f} kW: "
                f"es fehlen {fehlend:.1f} kW ({fehlend / p_max_last * 100:.0f} % "
                "der Spitze). Der Puffer muss die Leistungsspitzen abfangen — "
                "das simulierte Volumen sinkt deutlich, wenn die Erzeugerleistung "
                "(WP oder bivalenter Erzeuger) erhöht wird."
            )
            sim_info["fehlende_leistung_kW"] = fehlend
        elif p_gen >= p_max_last:
            sim_info["fehlende_leistung_kW"] = 0.0
            details["hinweise"].append(
                f"P_gen = {p_gen:.1f} kW deckt die Spitzenlast "
                f"({p_max_last:.1f} kW) — Speicherbedarf resultiert nur aus "
                "Sperrzeiten/Taktung."
            )
    else:
        sim_info["status"] = "kein Lastgang übergeben — Simulation entfällt"
        details["hinweise"].append(sim_info["status"])

    details["simulation"] = sim_info

    # --- Vergleich / Empfehlung -------------------------------------------
    kandidaten = {
        KRIT_ABTAU: v_abtau,
        KRIT_TAKT: v_takt,
        KRIT_SPERR: v_sperr,
        KRIT_SIM: v_sim,
    }
    aktive = {k: v for k, v in kandidaten.items() if v > 0}
    if not sim_gueltig:
        # Simulation ohne erreichten Ziel-Deckungsgrad liefert nur die
        # Suchobergrenze -> nicht bemessungsrelevant.
        aktive.pop(KRIT_SIM, None)
    if aktive:
        massgebend = max(aktive, key=lambda k: aktive[k])
        v_max = aktive[massgebend]
    else:
        massgebend, v_max = "—", 0.0

    details["vergleich_l"] = kandidaten
    details["v_max_ungerundet_l"] = v_max
    details["praxis_grenze_l"] = PRAXIS_GRENZE_L
    details["praxis_plausibel"] = bool(0.0 < v_max <= PRAXIS_GRENZE_L)

    if v_max > PRAXIS_GRENZE_L:
        details["warnungen"].append(
            f"Das maßgebende Kriterium '{massgebend}' fordert {v_max:.0f} l "
            f"({v_max / 1000.0:.0f} m³) — mehr als die praktische Obergrenze "
            f"von {PRAXIS_GRENZE_L / 1000.0:.0f} m³. Ein Pufferspeicher dieser "
            "Größe ist keine Auslegung, sondern ein Saisonalspeicher: Bei "
            f"P_gen = {p_gen:.1f} kW und Ziel-Deckungsgrad "
            f"{params.ziel_deckung:.2f} muss der Speicher eine dauerhaft "
            "fehlende Erzeugerleistung überbrücken. Realistische Auswege: "
            "Erzeugerleistung erhöhen, bivalenten Spitzenlasterzeuger "
            "vorsehen (p_bivalent), Ziel-Deckungsgrad < 1 wählen oder die "
            "Sperrzeit nur teilweise aus dem Puffer decken. Der Zahlenwert "
            "ist rechnerisch korrekt, aber NICHT als Speichergröße zu "
            "bestellen."
        )

    return BufferResult(
        v_abtau_l=v_abtau,
        v_takt_l=v_takt,
        v_sperr_l=v_sperr,
        v_sim_l=v_sim,
        c_sim_kwh=c_sim,
        massgebend=massgebend,
        v_empfehlung_l=round_to_standard_volume(v_max) if v_max > 0 else 0,
        sim=sim,
        details=details,
    )
