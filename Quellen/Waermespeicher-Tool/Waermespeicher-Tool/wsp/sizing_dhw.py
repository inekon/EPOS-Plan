"""Auslegung von Trinkwarmwasser-Speichern (TWW) — drei Verfahren im Vergleich.

Siehe DESIGN.md Abschnitt 3:

1. **DIN 4708** — Bedarfskennzahl N, Wärmebedarf der Zapfperiode ``W_z(N)``
   (vendored ``vendor/din4708.py`` aus lpagg, Werte dort in **Wh**).
2. **Profilbasiert** — SOC-Defizit-Rechnung über den Jahres-Lastgang bei
   konstanter Ladeleistung ``p_lade`` (WP im TWW-Betrieb).
3. **Faustwert** — 35 l/(Person·d) bei 60 °C, energetisch auf die tatsächliche
   Speichertemperatur umgerechnet.

Alle Volumina in Litern, Energien in kWh, Leistungen in kW.
Umrechnung Energie <-> Volumen über ``models.energy_to_volume``
(c_w = 1.163 Wh/(l·K)).
"""
from __future__ import annotations

from typing import Optional

import numpy as np
import pandas as pd

from vendor import din4708

from .models import DHWParams, DHWResult, energy_to_volume, round_to_standard_volume

#: Referenzbelegung der DIN-4708-Einheitswohnung [Personen]
PERSONS_EINHEITSWOHNUNG = 3.5

#: Faustwert-Zapfmenge [l/(Person·d)] bei 60 °C (DIN V 18599-10 / VDI-Praxis:
#: 30–45 l/(P·d); hier mittlerer Ansatz 35)
FAUST_L_PRO_PERSON_TAG = 35.0

#: Bezugstemperaturen des Faustwerts
FAUST_T_SPEICHER = 60.0
FAUST_T_KALT = 10.0

#: Faktor, ab dem die drei Verfahren als "stark abweichend" gelten und das
#: Ergebnis fachlich geprüft werden muss (Faustwert skaliert linear mit der
#: Personenzahl und verliert bei großen Wohnanlagen seine Gültigkeit).
VERGLEICH_WARN_FAKTOR = 3.0

#: Ab diesem Speichervolumen greift die Legionellen-Argumentation (VDI 6023 /
#: DVGW W 551: Großanlage > 400 l Speicher bzw. > 3 l Rohrleitungsinhalt)
LEGIONELLEN_GRENZE_L = 400.0


# --------------------------------------------------------------------- DIN 4708

def bedarfskennzahl_n(n_we: int, persons_per_we: float) -> float:
    """Bedarfskennzahl N (DIN 4708) — **vereinfachter Ansatz**.

    ``N = n_we * persons_per_we / 3,5``

    Die Einheitswohnung der DIN 4708 ist mit p = 3,5 Personen, einer Badewanne
    als Hauptzapfstelle und einem Wärmebedarf W_b = 5820 Wh definiert. Die
    **exakte** Bestimmung nach DIN 4708-2 gewichtet je Wohnung zusätzlich die
    Zapfstellen (v = Zapfstellenzahl/-art, w = Wohnungswertigkeit):

        N = Σ_i ( n_i · p_i · v_i · w_i ) / (3,5 · 1 · 1)

    Diese Tabellenwerte (v, w) liegen dem Tool nicht vor; hier wird deshalb
    v = w = 1 gesetzt (Normalausstattung: Bad mit Wanne + Küche). Für Wohnungen
    mit überdurchschnittlicher Ausstattung (2. Bad, große Wannen) liegt das
    Ergebnis auf der unsicheren Seite — dann N manuell erhöhen.
    """
    if n_we <= 0 or persons_per_we <= 0:
        return 0.0
    return float(n_we) * float(persons_per_we) / PERSONS_EINHEITSWOHNUNG


def _din4708_volumen(n_bedarf: float, params: DHWParams) -> tuple[float, float]:
    """(W_z [kWh], Speichervolumen [l]) nach DIN 4708 für Bedarfskennzahl N."""
    if n_bedarf <= 0:
        return 0.0, 0.0
    w_z_kwh = float(din4708.W_z(n_bedarf)) / 1000.0  # vendor liefert Wh
    delta_t = params.t_speicher - params.t_kalt
    v_nutz = energy_to_volume(w_z_kwh, delta_t)
    v_brutto = v_nutz / max(params.nutzbarer_anteil, 1e-9)
    return w_z_kwh, v_brutto


# ---------------------------------------------------------------- Profilbasiert

def max_soc_defizit(load_kw: pd.Series, p_lade: float,
                    dt_h: float = 1.0) -> tuple[float, Optional[pd.Timestamp]]:
    """Maximale kumulierte Unterdeckung [kWh] eines Speichers.

    Speicherbilanz mit konstanter Ladeleistung ``p_lade``:

        Defizit(t) = max(0, Defizit(t-1) + (Last(t) - p_lade) · dt)

    Der Speicher startet voll; das Maximum dieser Reihe ist die nutzbare
    Kapazität, die der Speicher mindestens vorhalten muss. Vektorisiert über
    die Lindley-Rekursion (max. Drawdown der kumulierten Nettoenergie).

    Rückgabe: (max. Defizit [kWh], Zeitpunkt des Maximums).
    """
    if load_kw is None or len(load_kw) == 0:
        return 0.0, None
    net = (np.asarray(load_kw, dtype=float) - float(p_lade)) * float(dt_h)
    kum = np.cumsum(net)
    lauf_min = np.minimum(np.minimum.accumulate(kum), 0.0)
    defizit = kum - lauf_min
    i_max = int(np.argmax(defizit))
    d_max = float(defizit[i_max])
    if d_max <= 0.0:
        return 0.0, None
    ts = load_kw.index[i_max] if isinstance(load_kw, pd.Series) else None
    return d_max, ts


def _tagesbedarf_max(load_kw: pd.Series, dt_h: float) -> float:
    """Größter Tagesbedarf [kWh/d] im Profil."""
    if load_kw is None or len(load_kw) == 0:
        return 0.0
    energie = load_kw.astype(float) * float(dt_h)
    if isinstance(load_kw.index, pd.DatetimeIndex):
        return float(energie.groupby(load_kw.index.date).sum().max())
    return float(energie.sum())


# --------------------------------------------------------------------- Faustwert

def _faustwert_volumen(n_we: int, persons_per_we: float,
                       params: DHWParams) -> float:
    """35 l/(P·d) @ 60 °C, energiegleich auf t_speicher umgerechnet, + Zuschlag."""
    delta_t = params.t_speicher - params.t_kalt
    if delta_t <= 0:
        raise ValueError("t_speicher muss über t_kalt liegen")
    personen = max(float(n_we), 0.0) * max(float(persons_per_we), 0.0)
    v60 = personen * FAUST_L_PRO_PERSON_TAG
    return (v60 * (FAUST_T_SPEICHER - FAUST_T_KALT) / delta_t
            * (1.0 + params.zuschlag))


# ------------------------------------------------------------------ Hauptroutine

def size_dhw(params: DHWParams,
             n_we: int,
             persons_per_we: float,
             profile_tww: Optional[pd.Series] = None,
             dt_h: float = 1.0) -> DHWResult:
    """Legt einen TWW-Speicher nach drei Verfahren aus und vergleicht sie.

    Args:
        params: Speicher-/Ladeparameter (Temperaturen, p_lade, Zirkulation, …).
        n_we: Anzahl Wohneinheiten.
        persons_per_we: mittlere Belegung je WE.
        profile_tww: TWW-Lastgang [kW] mit DatetimeIndex (ohne Zirkulation —
            ``params.zirkulation_kw`` wird als konstante Zusatzlast addiert).
            ``None`` -> profilbasiertes Verfahren entfällt (v_profil_l = 0).
        dt_h: Zeitschrittweite des Profils in Stunden.

    Returns:
        DHWResult. ``v_profil_l`` und ``v_faust_l`` enthalten bereits den
        Zuschlag ``params.zuschlag``; ``v_din4708_l`` ist der reine
        Norm-Speicherinhalt (die DIN-Bemessung enthält bereits Reserven).
        Rohwerte ohne Zuschlag stehen in ``details``.
    """
    delta_t = params.t_speicher - params.t_kalt
    if delta_t <= 0:
        raise ValueError("t_speicher muss über t_kalt liegen "
                         f"(ist {params.t_speicher} / {params.t_kalt} °C)")

    details: dict = {
        "delta_t_nutz_K": delta_t,
        "nutzbarer_anteil": params.nutzbarer_anteil,
        "zuschlag": params.zuschlag,
        "personen_gesamt": float(n_we) * float(persons_per_we),
        "warnungen": [],
        "hinweise": [],
    }

    # --- 1) DIN 4708 -------------------------------------------------------
    n_bedarf = bedarfskennzahl_n(n_we, persons_per_we)
    w_z_kwh, v_din = _din4708_volumen(n_bedarf, params)
    nl_hinweis = f"Speicher mit NL ≥ {n_bedarf:.1f} wählen"
    details["din4708"] = {
        "N": n_bedarf,
        "W_z_kWh": w_z_kwh,
        "W_z_Wh": w_z_kwh * 1000.0,
        "W_1h_kWh": float(din4708.W_1(n_bedarf)) / 1000.0 if n_bedarf > 0 else 0.0,
        "W_p_kWh": float(din4708.W_p(n_bedarf)) / 1000.0 if n_bedarf > 0 else 0.0,
        "GLF": float(din4708.calc_GLF(n_bedarf)) if n_bedarf > 0 else 0.0,
        "v_nutz_l": energy_to_volume(w_z_kwh, delta_t) if w_z_kwh else 0.0,
        "methode": ("N vereinfacht = n_WE · Personen/WE / 3,5 "
                    "(Einheitswohnung p=3,5; v=w=1). Exakte N-Berechnung nach "
                    "DIN 4708-2 erfordert zapfstellen-gewichtete Tabellenwerte "
                    "(v, w) je Wohnungstyp."),
    }

    # --- 2) Profilbasiert --------------------------------------------------
    v_profil = 0.0
    if profile_tww is not None and len(profile_tww) > 0:
        last = profile_tww.astype(float).fillna(0.0) + float(params.zirkulation_kw)
        d_max, t_max = max_soc_defizit(last, params.p_lade, dt_h)
        v_profil_roh = energy_to_volume(d_max, delta_t) / max(
            params.nutzbarer_anteil, 1e-9)
        v_profil = v_profil_roh * (1.0 + params.zuschlag)

        tages_max = _tagesbedarf_max(last, dt_h)
        p_lade_reicht = params.p_lade * 24.0 > tages_max
        details["profil"] = {
            "defizit_max_kWh": d_max,
            "defizit_max_zeitpunkt": str(t_max) if t_max is not None else None,
            "v_roh_l": v_profil_roh,
            "p_lade_kW": params.p_lade,
            "zirkulation_kW": params.zirkulation_kw,
            "tagesbedarf_max_kWh": tages_max,
            "jahresbedarf_kWh": float(last.sum() * dt_h),
            "p_max_last_kW": float(last.max()),
            "p_lade_ausreichend": bool(p_lade_reicht),
            "p_lade_mindest_kW": tages_max / 24.0,
        }
        if not p_lade_reicht:
            details["warnungen"].append(
                f"Ladeleistung p_lade = {params.p_lade:.1f} kW deckt den größten "
                f"Tagesbedarf ({tages_max:.0f} kWh/d) nicht: rechnerisch sind "
                f"mindestens {tages_max / 24.0:.1f} kW Dauerladeleistung nötig. "
                "Das Speichervolumen ist dann kein Ersatz — Ladeleistung erhöhen."
            )
        if d_max <= 0.0:
            details["hinweise"].append(
                "Ladeleistung übersteigt jede Momentanlast — profilbasiert "
                "ergibt sich rechnerisch kein Speicherbedarf (nur Bereitschafts-"
                "volumen). Verfahren wird für die Empfehlung nicht herangezogen."
            )
    else:
        details["profil"] = None
        details["hinweise"].append(
            "Kein TWW-Lastgang übergeben — profilbasiertes Verfahren entfällt.")

    # --- 3) Faustwert ------------------------------------------------------
    v_faust = _faustwert_volumen(n_we, persons_per_we, params)
    details["faustwert"] = {
        "l_pro_person_tag_60C": FAUST_L_PRO_PERSON_TAG,
        "v_60C_l": float(n_we) * float(persons_per_we) * FAUST_L_PRO_PERSON_TAG,
        "v_roh_l": v_faust / (1.0 + params.zuschlag),
        "tagesbedarf_kWh": (float(n_we) * float(persons_per_we)
                            * FAUST_L_PRO_PERSON_TAG
                            * (FAUST_T_SPEICHER - FAUST_T_KALT) * 1.163 / 1000.0),
        "methode": ("35 l/(Person·d) bei 60 °C, energiegleich auf "
                    "t_speicher umgerechnet: V = P·35·(60−10)/(t_sp−t_kalt)·"
                    "(1+Zuschlag)."),
    }

    # --- Vergleich / Empfehlung -------------------------------------------
    kandidaten = {"DIN 4708": v_din, "Faustwert": v_faust}
    if v_profil > 0:
        kandidaten["Profilbasiert"] = v_profil
    kandidaten = {k: v for k, v in kandidaten.items() if v > 0}

    if kandidaten:
        massgebend = max(kandidaten, key=lambda k: kandidaten[k])
        v_max = kandidaten[massgebend]
    else:
        massgebend, v_max = "—", 0.0
    v_empfehlung = round_to_standard_volume(v_max) if v_max > 0 else 0

    details["vergleich_l"] = kandidaten
    details["massgebend"] = massgebend
    details["v_max_ungerundet_l"] = v_max

    # --- Plausibilität: weichen die Verfahren stark voneinander ab? --------
    v_min_k = min(kandidaten.values()) if kandidaten else 0.0
    spreizung = (v_max / v_min_k) if v_min_k > 0 else 0.0
    details["vergleich_spreizung"] = spreizung
    if len(kandidaten) > 1 and spreizung > VERGLEICH_WARN_FAKTOR:
        liste = ", ".join(f"{k}: {v:.0f} l" for k, v in kandidaten.items())
        zyklen_txt = ""
        prof = details.get("profil") or {}
        tages_max = float(prof.get("tagesbedarf_max_kWh") or 0.0)
        if tages_max > 0 and params.p_lade > 0:
            zyklen = params.p_lade * 24.0 / tages_max
            zyklen_txt = (f" Die Ladeleistung reicht rechnerisch für "
                          f"{zyklen:.1f} Speicherladungen je Tag.")
        details["warnungen"].append(
            f"Die Verfahren weichen um den Faktor {spreizung:.1f} voneinander "
            f"ab ({liste}); maßgebend ist '{massgebend}'. Der Faustwert "
            "unterstellt EINE Speicherladung je Tag (V = Tagesbedarf) und "
            "wächst linear mit der Personenzahl — für größere Wohnanlagen "
            "liegt er deshalb systematisch zu hoch." + zyklen_txt +
            " Bei mehreren Ladungen je Tag sind DIN 4708 und das "
            "profilbasierte Verfahren die belastbaren Werte; das Ergebnis "
            "ist fachlich zu prüfen und nicht ungeprüft zu bestellen."
        )

    if v_empfehlung > LEGIONELLEN_GRENZE_L:
        legionellen = (
            f"Großanlage (> {LEGIONELLEN_GRENZE_L:.0f} l Speicherinhalt bzw. > 3 l "
            "Rohrleitungsinhalt je Strang, DVGW W 551): Speicheraustritt "
            "dauerhaft ≥ 60 °C, Zirkulationsrücklauf ≥ 55 °C, wöchentliche "
            "Erwärmung des gesamten Inhalts. Bei WP-Betrieb ist stattdessen eine "
            "Frischwasserstation (FriWa) mit Pufferspeicher zu prüfen — kleiner "
            "Trinkwasserinhalt, niedrigere Speichertemperatur, bessere JAZ."
        )
    else:
        legionellen = (
            f"Kleinanlage (≤ {LEGIONELLEN_GRENZE_L:.0f} l): erhöhte Anforderungen "
            "nach DVGW W 551 gelten nicht zwingend; Speichertemperatur ≥ 60 °C "
            "bzw. periodische Aufheizung dennoch empfohlen."
        )
    details["legionellen_hinweis"] = legionellen

    if params.t_speicher < 60.0:
        details["warnungen"].append(
            f"Speichertemperatur {params.t_speicher:.0f} °C < 60 °C — "
            "Legionellenschutz gesondert nachweisen (thermische Desinfektion "
            "oder FriWa-Konzept)."
        )

    return DHWResult(
        n_bedarf=n_bedarf,
        nl_hinweis=nl_hinweis,
        w_z_kwh=w_z_kwh,
        v_din4708_l=v_din,
        v_profil_l=v_profil,
        v_faust_l=v_faust,
        v_empfehlung_l=v_empfehlung,
        details=details,
    )
