"""Zentrale Datenmodelle des Wärmespeicher-Tools.

Diese Datei ist der API-Vertrag zwischen allen Modulen.
NICHT ohne Abstimmung ändern — alle Module bauen darauf auf.

Konventionen:
- Leistungen in kW (thermisch), Energien in kWh, Volumen in Liter,
  Temperaturen in °C, Zeitreihen als pandas.DataFrame mit DatetimeIndex.
- c_w = 1.163 Wh/(l*K)
"""
from __future__ import annotations

from dataclasses import dataclass, field, asdict
from enum import Enum
from typing import Optional

import pandas as pd

C_W = 1.163  # Wh/(l*K)

#: Marktübliche Speichergrößen [l] für Rundungsempfehlungen
STANDARD_VOLUMES = [100, 150, 200, 300, 400, 500, 800, 1000, 1500,
                    2000, 3000, 5000, 8000, 10000]


def energy_to_volume(q_kwh: float, delta_t: float) -> float:
    """Kapazität [kWh] -> Volumen [l] bei Temperaturspreizung delta_t [K]."""
    if delta_t <= 0:
        raise ValueError("delta_t muss > 0 sein")
    return q_kwh * 1000.0 / (C_W * delta_t)


def volume_to_energy(v_liter: float, delta_t: float) -> float:
    """Volumen [l] -> Kapazität [kWh] bei Temperaturspreizung delta_t [K]."""
    return v_liter * C_W * delta_t / 1000.0


def round_to_standard_volume(v_liter: float) -> int:
    """Nächstgrößere marktübliche Speichergröße."""
    for v in STANDARD_VOLUMES:
        if v >= v_liter:
            return v
    return int(round(v_liter, -2))


class BuildingCategory(str, Enum):
    EFH = "EFH"
    MFH = "MFH"
    GHD = "GHD"


#: BDEW-Branchen für GHD (Auszug demandlib) -> Anzeigename
BDEW_BRANCHES = {
    "ghd": "GHD Gesamt (Durchschnitt)",
    "gmk": "Metall/Kfz",
    "gha": "Einzel-/Großhandel",
    "gbd": "Bäckerei",
    "gko": "Gebietskörperschaft/Büro (öff.)",
    "gbh": "Beherbergung",
    "gga": "Gaststätte",
    "gbα": "Bäcker mit Backstube",  # falls demandlib abweicht: Agent prüft Keys
    "gwa": "Wäscherei",
    "gpd": "Papier/Druck",
    "gmf": "Haushaltsähnlich (Mehrfamilien-Mix)",
    "ghα": "Handel mit Kühlung",
}


@dataclass
class Building:
    """Ein Gebäude(-typ) für die synthetische Profilerzeugung.

    Für EFH/MFH (VDI 4655): n_we, n_persons (je WE), q_heiz_a, q_tww_a.
    Für GHD (BDEW): bdew_branch, q_heiz_a (Gesamtwärme), tww_share (0..1).
    copies: Anzahl identischer Gebäude (Gleichzeitigkeit via sigma).
    """
    name: str
    category: BuildingCategory = BuildingCategory.MFH
    n_we: int = 1                      # Wohneinheiten (EFH: 1)
    n_persons: float = 2.5             # Personen je WE
    q_heiz_a: float = 15000.0          # kWh/a Raumheizung (je Gebäude)
    q_tww_a: Optional[float] = None    # kWh/a TWW; None -> 500 kWh/(Person*a)
    copies: int = 1
    sigma: float = 4.0                 # Zeitversatz-Std.abw. [min] (Gleichzeitigkeit)
    bdew_branch: str = "ghd"           # nur GHD
    tww_share: float = 0.1             # nur GHD: TWW-Anteil an Gesamtwärme

    def q_tww_effective(self) -> float:
        if self.category == BuildingCategory.GHD:
            return self.q_heiz_a * self.tww_share
        if self.q_tww_a is not None:
            return self.q_tww_a
        return 500.0 * self.n_persons * self.n_we


@dataclass
class WeatherConfig:
    try_region: int = 4      # DWD TRY2010 Region 1..15
    year: int = 2026


@dataclass
class ProfileSet:
    """Ergebnis der Profilerzeugung bzw. des Messdaten-Imports.

    df: DatetimeIndex, Spalten 'Q_heiz' [kW], 'Q_tww' [kW]; optional 'P_el'.
    resolution: pandas-Freq-String ('h' oder '15min').
    source: 'synthetisch' | 'gemessen'
    meta: freie Zusatzinfos (Gebäudeliste, Importdatei, Lückenreport, ...)
    """
    df: pd.DataFrame
    resolution: str = "h"
    source: str = "synthetisch"
    meta: dict = field(default_factory=dict)

    @property
    def dt_h(self) -> float:
        return pd.Timedelta(pd.tseries.frequencies.to_offset(
            self.resolution)).total_seconds() / 3600.0

    @property
    def q_total(self) -> pd.Series:
        s = self.df["Q_heiz"] + self.df["Q_tww"]
        s.name = "Q_total"
        return s

    def annual_sums(self) -> dict:
        dt = self.dt_h
        return {c: float(self.df[c].sum() * dt) for c in self.df.columns}


# ---------------------------------------------------------------- TWW-Speicher

@dataclass
class DHWParams:
    t_speicher: float = 60.0     # °C Speichertemperatur
    t_kalt: float = 10.0         # °C Kaltwasser
    nutzbarer_anteil: float = 0.8  # nutzbarer Volumenanteil (Schichtung/Totvolumen)
    p_lade: float = 20.0         # kW WP-Ladeleistung TWW-Betrieb
    zirkulation_kw: float = 0.0  # kW Dauerverlust Zirkulation (0 = ignorieren)
    zuschlag: float = 0.15       # Sicherheits-/Bereitschaftszuschlag auf V


@dataclass
class DHWResult:
    n_bedarf: float              # DIN 4708 Bedarfskennzahl N
    nl_hinweis: str              # Textempfehlung N_L >= ...
    w_z_kwh: float               # Wärmebedarf Zapfperiode W_z(N) [kWh]
    v_din4708_l: float           # Volumenabschätzung aus W_z
    v_profil_l: float            # profilbasiert (SOC-Defizit)
    v_faust_l: float             # Faustwert-Verfahren
    v_empfehlung_l: int          # gerundete Empfehlung
    details: dict = field(default_factory=dict)


# --------------------------------------------------------------- Pufferspeicher

@dataclass
class BufferParams:
    p_wp: float = 50.0            # kW Heizleistung WP (am Auslegungspunkt)
    p_wp_min: float = 15.0        # kW minimale Modulationsleistung
    p_bivalent: float = 0.0       # kW zweiter Erzeuger (0 = monovalent)
    dt_puffer: float = 10.0       # K nutzbare Spreizung Puffer
    t_min_lauf_min: float = 10.0  # min Mindestlaufzeit (Taktung)
    abtau_l_pro_kw: float = 20.0  # l/kW (Luft/Wasser-Abtauung); 0 bei Sole
    sperrzeiten: list = field(default_factory=list)
    # Sperrzeiten: Liste von (start_hh, dauer_h), z.B. [(11,2),(17,2)] tägl.
    ziel_deckung: float = 1.0     # 1.0 = keine Unterdeckung zulässig


@dataclass
class BufferResult:
    v_abtau_l: float
    v_takt_l: float
    v_sperr_l: float
    v_sim_l: float                # aus Lastgang-Simulation (0 wenn ohne Profil)
    c_sim_kwh: float              # zugehörige Kapazität
    massgebend: str               # welches Kriterium maßgebend ist
    v_empfehlung_l: int
    sim: Optional["SimResult"] = None
    details: dict = field(default_factory=dict)


# ------------------------------------------------------------------- Simulation

@dataclass
class SimResult:
    """Ergebnis der Speicherbilanz-Simulation (storage_sim.simulate)."""
    df: pd.DataFrame              # Spalten: Q_last, P_gen, SOC [kWh], Unterdeckung [kW]
    capacity_kwh: float
    unterdeckung_kwh: float
    unterdeckung_h: int
    deckungsgrad: float           # 1 - Unterdeckung/Jahresbedarf
    #: Anzahl der Wechsel Entladen -> Laden (nur Zweipunkt-Betrieb,
    #: storage_sim.simulate_zweipunkt); None bei der Durchlauf-Simulation.
    ladezyklen: Optional[int] = None

    def ok(self, ziel: float = 1.0) -> bool:
        return self.deckungsgrad >= ziel - 1e-9


def project_to_dict(buildings: list, weather: WeatherConfig,
                    dhw: DHWParams, buffer: BufferParams) -> dict:
    """Serialisierung für Projekt-Speichern (JSON)."""
    return {
        "buildings": [asdict(b) for b in buildings],
        "weather": asdict(weather),
        "dhw": asdict(dhw),
        "buffer": asdict(buffer),
    }
