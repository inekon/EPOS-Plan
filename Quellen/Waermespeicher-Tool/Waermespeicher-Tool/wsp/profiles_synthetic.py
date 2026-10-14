"""Synthetische Lastprofile: VDI 4655 (Wohnen) + BDEW (GHD).

Beides über die Bibliothek ``demandlib`` (oemof, MIT, Version 0.2.2):

- **EFH/MFH** → ``demandlib.vdi.Region`` (VDI 4655 Referenzlastprofile).
  Die Region wird mit einem ``Climate``-Objekt aus TRY2010-Wetter
  (:mod:`wsp.weather`) parametriert. demandlib normiert die Typtag-Profile
  intern exakt auf die vorgegebenen Jahressummen.
- **GHD** → ``demandlib.bdew.HeatBuilding`` (BDEW-SLP Gas/Wärme).
  Heizung und Trinkwarmwasser werden aus zwei Läufen gebildet:
  ``ww_incl=False`` (nur Heizanteil, Sigmoid-Parameter d = 0) und
  ``ww_only=True`` (nur Warmwasseranteil, temperaturunabhängiges
  Grundband a = b = c = 0). Beide Profile sind normiert, d. h. die
  Jahressumme trifft die Vorgabe exakt.

Ergebnis ist ein :class:`wsp.models.ProfileSet` mit den Spalten
``Q_heiz`` und ``Q_tww`` in **kW** (mittlere Leistung im Intervall),
über alle Gebäude aufsummiert.

BDEW-Branchenschlüssel
----------------------
``models.BDEW_BRANCHES`` ist der Anzeige-Katalog der UI; demandlib kennt
intern folgende **Nicht-Wohn**-Profiltypen (``shlp_type``, Spalte in
``demandlib/bdew/bdew_data/shlp_sigmoid_factors.csv`` bei
``building_class == 0``)::

    GHD  Summenlastprofil Gewerbe/Handel/Dienstleistungen
    GMK  Metall und Kfz
    GHA  Einzel- und Großhandel
    GKO  Gebietskörperschaften, Kreditinstitute, Versicherungen (Büro)
    GBD  sonstige betriebliche Dienstleistungen
    GBA  Backstube (Bäckerei)
    GGA  Gaststätten
    GBH  Beherbergung
    GWA  Wäschereien / chemische Reinigung
    GGB  Gartenbau
    GPD  Papier und Druck
    GMF  haushaltsähnliche Gewerbebetriebe

(zusätzlich EFH/MFH — nur mit ``building_class`` 1..11, hier nicht genutzt.)

Abweichungen von ``models.BDEW_BRANCHES`` (models.py wird **nicht**
geändert, die Korrektur erfolgt über :data:`BDEW_BRANCH_MAP`):

* ``"gbα"`` — Tippfehler (griechisches Alpha); korrekt ist ``"gba"``
  (GBA, Backstube). Wird hier auf ``GBA`` gemappt.
* ``"ghα"`` — existiert bei BDEW nicht ("Handel mit Kühlung" ist kein
  eigener SLP-Typ). Wird auf ``GHA`` (Einzel-/Großhandel) gemappt.
* ``"gbd"`` ist bei BDEW **nicht** "Bäckerei", sondern "sonstige
  betriebliche Dienstleistungen" (Bäckerei = GBA). Der Anzeigename in
  models.py ist insofern irreführend, der Schlüssel selbst ist korrekt.
* ``"ggb"`` (Gartenbau) fehlt in ``models.BDEW_BRANCHES``, wird hier
  aber unterstützt.

Nicht auflösbare Schlüssel fallen mit einer ``UserWarning`` auf ``GHD``
zurück.
"""
from __future__ import annotations

import calendar
import warnings
from dataclasses import asdict

import numpy as np
import pandas as pd

from demandlib import bdew, vdi

from .models import Building, BuildingCategory, ProfileSet, WeatherConfig
from .weather import get_climate, load_try_weather, try_region_names

__all__ = [
    "BDEW_BRANCH_MAP",
    "DEMANDLIB_SHLP_TYPES",
    "W_A_PER_WE_DEFAULT",
    "generate_profiles",
]

#: Von demandlib unterstützte BDEW-Profiltypen für Nicht-Wohngebäude
DEMANDLIB_SHLP_TYPES = (
    "GHD", "GMK", "GHA", "GKO", "GBD", "GBA", "GGA",
    "GBH", "GWA", "GGB", "GPD", "GMF",
)

#: models.BDEW_BRANCHES-Schlüssel -> demandlib ``shlp_type``
BDEW_BRANCH_MAP = {
    "ghd": "GHD",
    "gmk": "GMK",
    "gha": "GHA",
    "gko": "GKO",
    "gbd": "GBD",
    "gba": "GBA",
    "gbα": "GBA",   # Tippfehler in models.BDEW_BRANCHES
    "gga": "GGA",
    "gbh": "GBH",
    "gwa": "GWA",
    "ggb": "GGB",
    "gpd": "GPD",
    "gmf": "GMF",
    "ghα": "GHA",   # kein eigener BDEW-Typ -> Einzel-/Großhandel
}

#: Default-Jahresstrombedarf je Wohneinheit [kWh/a] (nur VDI-Pflichtfeld W_a,
#: wird im Ergebnis nicht verwendet)
W_A_PER_WE_DEFAULT = 3000.0

#: BDEW-Windklasse (0 = nicht windig, 1 = windig)
BDEW_WIND_CLASS = 0

#: VDI-4655-Temperaturgrenzen (Sommer/Winter) für die Typtag-Zuordnung
SUMMER_TEMPERATURE_LIMIT = 15.0
WINTER_TEMPERATURE_LIMIT = 5.0

_COLS = ["Q_heiz", "Q_tww"]


# ------------------------------------------------------------------- Helpers

def _shlp_type(branch: str) -> str:
    """BDEW-Branchenschlüssel -> demandlib ``shlp_type`` (robust)."""
    key = str(branch or "").strip()
    mapped = BDEW_BRANCH_MAP.get(key.lower())
    if mapped is None:
        # letzter Versuch: direkt als shlp_type interpretieren
        cand = key.upper().replace("Α", "A")
        mapped = cand if cand in DEMANDLIB_SHLP_TYPES else None
    if mapped is None:
        warnings.warn(
            f"Unbekannte BDEW-Branche {branch!r}; verwende Summenprofil "
            f"'GHD'. Unterstützt: {', '.join(sorted(DEMANDLIB_SHLP_TYPES))}",
            UserWarning, stacklevel=3)
        mapped = "GHD"
    return mapped


def _target_index(year: int, resolution: str) -> pd.DatetimeIndex:
    """Zeitindex des Jahres in der gewünschten Auflösung (Intervallbeginn)."""
    return pd.date_range(start=f"{year}-01-01 00:00",
                         end=f"{year}-12-31 23:59:59",
                         freq=resolution)


def _dt_hours(resolution: str) -> float:
    return pd.Timedelta(
        pd.tseries.frequencies.to_offset(resolution)).total_seconds() / 3600.0


def _resample_energy(df_kwh: pd.DataFrame, index: pd.DatetimeIndex,
                     dt_source_h: float, dt_target_h: float) -> pd.DataFrame:
    """Energie-Zeitreihe [kWh/Intervall] energieerhaltend umtakten."""
    if abs(dt_source_h - dt_target_h) < 1e-12:
        return df_kwh.reindex(index).fillna(0.0)
    if dt_target_h > dt_source_h:                      # verdichten
        out = df_kwh.resample(index.freqstr).sum()
    else:                                              # aufteilen
        ratio = dt_target_h / dt_source_h
        out = df_kwh.reindex(index, method="ffill") * ratio
    return out.reindex(index).fillna(0.0)


def _apply_copies(values: np.ndarray, copies: int, sigma_min: float,
                  step_min: float, rng: np.random.Generator) -> np.ndarray:
    """``copies`` identische Gebäude mit zufälligem Zeitversatz addieren.

    Der Versatz wird aus N(0, sigma) in Minuten gezogen und auf ganze
    Zeitschritte gerundet (lpagg-Logik, ``numpy.roll`` → zyklisch, damit
    energieerhaltend). Bei Stundenauflösung und dem Default sigma = 4 min
    ergibt das durchweg 0 Schritte, d. h. die Kopien werden schlicht
    aufaddiert — der Gleichzeitigkeitseffekt ist erst bei feiner
    Auflösung (z. B. ``'15min'``) bzw. großem sigma sichtbar.
    """
    copies = max(int(copies), 1)
    if copies == 1:
        return values
    if sigma_min and sigma_min > 0 and step_min > 0:
        shifts = np.round(
            rng.normal(0.0, float(sigma_min) / float(step_min), copies)
        ).astype(int)
    else:
        shifts = np.zeros(copies, dtype=int)
    out = np.zeros_like(values)
    for shift in shifts:
        out += np.roll(values, int(shift), axis=0) if shift else values
    return out


def _normalize(df_kwh: pd.DataFrame, targets: dict,
               tol: float = 1e-9) -> dict:
    """Jahressummen exakt auf die Vorgaben ziehen (Energieerhaltung).

    demandlib normiert bereits selbst; diese Korrektur fängt nur
    Rundungs-/Resampling-Reste ab. Gibt die angewandten Faktoren zurück.
    """
    factors = {}
    for col, target in targets.items():
        total = float(df_kwh[col].sum())
        if target is None or abs(target) < 1e-12:
            factors[col] = 1.0
            continue
        if total <= 0:
            warnings.warn(
                f"Profil für {col!r} ist durchgängig 0, Jahressumme "
                f"{target:.0f} kWh kann nicht abgebildet werden.",
                UserWarning, stacklevel=2)
            factors[col] = 1.0
            continue
        factor = target / total
        if abs(factor - 1.0) > tol:
            df_kwh[col] *= factor
        factors[col] = factor
    return factors


def _unique_names(buildings) -> list:
    """Eindeutige Profilnamen (demandlib indiziert Häuser über den Namen)."""
    seen, names = {}, []
    for b in buildings:
        base = str(b.name or "Gebaeude")
        if base in seen:
            seen[base] += 1
            base = f"{base}#{seen[base]}"
        else:
            seen[base] = 1
        names.append(base)
    return names


# ------------------------------------------------------------ Teilgeneratoren

def _vdi_profiles(res_buildings: list, names: list, weather: WeatherConfig,
                  resolution: str, index: pd.DatetimeIndex,
                  rng: np.random.Generator) -> tuple:
    """VDI-4655-Profile für EFH/MFH über ``demandlib.vdi.Region``."""
    if calendar.isleap(weather.year):
        raise ValueError(
            f"demandlib 0.2.2 (VDI 4655) unterstützt keine Schaltjahre; "
            f"{weather.year} ist ein Schaltjahr. Bitte ein anderes Jahr "
            f"wählen (GHD/BDEW-Profile funktionieren auch im Schaltjahr).")

    houses = []
    for b, name in zip(res_buildings, names):
        n_we = max(int(b.n_we), 1)
        n_pers = float(b.n_persons) * n_we
        if b.category == BuildingCategory.EFH and n_pers > 12:
            warnings.warn(
                f"{name}: {n_pers:g} Personen — VDI 4655 ist für EFH bis "
                f"12 Personen definiert, Ergebnis ist extrapoliert.",
                UserWarning, stacklevel=2)
        if b.category == BuildingCategory.MFH and n_we > 40:
            warnings.warn(
                f"{name}: {n_we} WE — VDI 4655 ist für MFH bis 40 WE "
                f"definiert, Ergebnis ist extrapoliert.",
                UserWarning, stacklevel=2)
        houses.append(dict(
            name=name,
            house_type=b.category.value,          # 'EFH' | 'MFH'
            N_Pers=n_pers,
            N_WE=n_we,
            Q_Heiz_a=float(b.q_heiz_a),
            Q_TWW_a=float(b.q_tww_effective()),
            W_a=W_A_PER_WE_DEFAULT * n_we,
            summer_temperature_limit=SUMMER_TEMPERATURE_LIMIT,
            winter_temperature_limit=WINTER_TEMPERATURE_LIMIT,
        ))

    holidays_dict = {}
    try:
        import holidays as _holidays
        holidays_dict = _holidays.country_holidays("DE",
                                                   years=weather.year)
    except Exception:  # pragma: no cover - holidays ist optional
        warnings.warn("Paket 'holidays' nicht verfügbar — Feiertage werden "
                      "in den VDI-Profilen nicht berücksichtigt.",
                      UserWarning, stacklevel=2)

    region = vdi.Region(
        weather.year,
        get_climate(weather.try_region, weather.year),
        holidays=holidays_dict,
        houses=houses,
        resample_rule=resolution,
    )
    lc = region.get_load_curve_houses()          # kWh je Intervall

    step_min = _dt_hours(resolution) * 60.0
    total = pd.DataFrame(0.0, index=index, columns=_COLS)
    per_building = {}
    for b, name, house in zip(res_buildings, names, houses):
        sub = lc[name][b.category.value]
        df_h = pd.DataFrame({
            "Q_heiz": sub["Q_Heiz_TT"].to_numpy(dtype=float),
            "Q_tww": sub["Q_TWW_TT"].to_numpy(dtype=float),
        }, index=sub.index).reindex(index).fillna(0.0)

        copies = max(int(b.copies), 1)
        arr = _apply_copies(df_h.to_numpy(), copies, b.sigma, step_min, rng)
        df_h = pd.DataFrame(arr, index=index, columns=_COLS)

        _normalize(df_h, {"Q_heiz": house["Q_Heiz_a"] * copies,
                          "Q_tww": house["Q_TWW_a"] * copies})
        per_building[name] = df_h
        total += df_h
    return total, per_building


def _bdew_profiles(ghd_buildings: list, names: list, weather: WeatherConfig,
                   resolution: str, index: pd.DatetimeIndex,
                   rng: np.random.Generator) -> tuple:
    """BDEW-Profile für GHD über ``demandlib.bdew.HeatBuilding``."""
    hourly = load_try_weather(weather.try_region, weather.year)
    temperature = hourly["TAMB"]

    holidays_dict = {}
    try:
        import holidays as _holidays
        holidays_dict = _holidays.country_holidays("DE", years=weather.year)
    except Exception:  # pragma: no cover
        pass

    dt_target = _dt_hours(resolution)
    step_min = dt_target * 60.0
    total = pd.DataFrame(0.0, index=index, columns=_COLS)
    per_building = {}

    for b, name in zip(ghd_buildings, names):
        shlp = _shlp_type(b.bdew_branch)
        share = min(max(float(b.tww_share), 0.0), 1.0)
        q_ges = float(b.q_heiz_a)
        q_tww = float(b.q_tww_effective())          # = share * q_ges
        q_heiz = max(q_ges - q_tww, 0.0)

        def _profile(annual, **kw):
            if annual <= 0:
                return pd.Series(0.0, index=hourly.index)
            hb = bdew.HeatBuilding(
                hourly.index,
                temperature=temperature,
                shlp_type=shlp,
                building_class=0,          # Pflicht für Nicht-Wohngebäude
                wind_class=BDEW_WIND_CLASS,
                annual_heat_demand=annual,
                name=name,
                holidays=holidays_dict,
                **kw)
            return pd.Series(np.asarray(hb.get_bdew_profile(), dtype=float),
                             index=hourly.index)

        # Heizanteil: Sigmoid ohne Warmwasser-Offset d
        s_heiz = _profile(q_heiz, ww_incl=False)
        # TWW-Anteil: reines Warmwasserband (a=b=c=0 -> temperaturunabhängig)
        try:
            s_tww = _profile(q_tww, ww_only=True)
            if q_tww > 0 and (float(s_tww.sum()) <= 0
                              or not np.isfinite(s_tww).all()):
                raise ValueError("ungültiges BDEW-Warmwasserprofil")
        except Exception as exc:  # pragma: no cover - Fallback laut Design
            warnings.warn(
                f"{name}: BDEW-Warmwasserprofil nicht verfügbar ({exc}); "
                f"TWW wird als konstantes Band angesetzt.",
                UserWarning, stacklevel=2)
            s_tww = pd.Series(q_tww / len(hourly.index), index=hourly.index)

        df_h = pd.DataFrame({"Q_heiz": s_heiz, "Q_tww": s_tww})
        df_h = _resample_energy(df_h, index, 1.0, dt_target)

        copies = max(int(b.copies), 1)
        arr = _apply_copies(df_h.to_numpy(), copies, b.sigma, step_min, rng)
        df_h = pd.DataFrame(arr, index=index, columns=_COLS)

        _normalize(df_h, {"Q_heiz": q_heiz * copies,
                          "Q_tww": q_tww * copies})
        per_building[name] = df_h
        per_building[name].attrs["shlp_type"] = shlp
        per_building[name].attrs["tww_share"] = share
        total += df_h
    return total, per_building


# ---------------------------------------------------------------- Hauptroutine

def generate_profiles(buildings: list[Building],
                      weather: WeatherConfig,
                      resolution: str = "h",
                      seed: int = 42) -> ProfileSet:
    """Synthetische Wärmelastprofile für eine Gebäudeliste erzeugen.

    Parameters
    ----------
    buildings : list[Building]
        Gebäude bzw. Gebäudetypen. ``BuildingCategory.EFH``/``MFH`` werden
        über VDI 4655 abgebildet, ``GHD`` über BDEW-Standardlastprofile.
    weather : WeatherConfig
        TRY-Klimaregion (1..15) und Kalenderjahr.
    resolution : str
        pandas-Freq-String der Ausgabe, z. B. ``'h'`` (Default) oder
        ``'15min'``.
    seed : int
        Startwert des Zufallsgenerators für den Gleichzeitigkeits-Versatz
        der ``copies`` (reproduzierbare Ergebnisse).

    Returns
    -------
    ProfileSet
        ``df`` mit Spalten ``Q_heiz``/``Q_tww`` in **kW**, Summe über alle
        Gebäude (inkl. ``copies``); ``meta`` enthält Jahressummen je
        Gebäude, Soll-/Ist-Vergleich und die Wetterparameter.

    Notes
    -----
    Energieerhaltung: demandlib normiert die Profile bereits auf die
    vorgegebenen Jahressummen; Kopien (zyklischer ``np.roll``) und das
    Umtakten sind ebenfalls energieerhaltend. Zusätzlich wird je Gebäude
    und Energieart nachnormiert, sodass die Jahressummen die Vorgaben
    exakt treffen.
    """
    if not buildings:
        raise ValueError("Es wurde kein Gebäude übergeben.")
    if weather is None:
        raise ValueError("WeatherConfig fehlt.")

    year = int(weather.year)
    index = _target_index(year, resolution)
    dt_h = _dt_hours(resolution)
    rng = np.random.default_rng(seed)

    names = _unique_names(buildings)
    res_pairs = [(b, n) for b, n in zip(buildings, names)
                 if b.category in (BuildingCategory.EFH,
                                   BuildingCategory.MFH)]
    ghd_pairs = [(b, n) for b, n in zip(buildings, names)
                 if b.category == BuildingCategory.GHD]

    total = pd.DataFrame(0.0, index=index, columns=_COLS)
    per_building = {}

    if res_pairs:
        part, sub = _vdi_profiles([p[0] for p in res_pairs],
                                  [p[1] for p in res_pairs],
                                  weather, resolution, index, rng)
        total += part
        per_building.update(sub)
    if ghd_pairs:
        part, sub = _bdew_profiles([p[0] for p in ghd_pairs],
                                   [p[1] for p in ghd_pairs],
                                   weather, resolution, index, rng)
        total += part
        per_building.update(sub)

    # --- Soll-/Ist-Bilanz je Gebäude -------------------------------------
    soll, ist = {}, {}
    for b, name in zip(buildings, names):
        copies = max(int(b.copies), 1)
        q_tww = float(b.q_tww_effective()) * copies
        if b.category == BuildingCategory.GHD:
            q_heiz = max(float(b.q_heiz_a) * copies - q_tww, 0.0)
        else:
            q_heiz = float(b.q_heiz_a) * copies
        soll[name] = {"Q_heiz": q_heiz, "Q_tww": q_tww,
                      "kategorie": b.category.value, "copies": copies}
        df_b = per_building[name]
        ist[name] = {"Q_heiz": float(df_b["Q_heiz"].sum()),
                     "Q_tww": float(df_b["Q_tww"].sum())}

    # Wohneinheiten/Personen der Wohngebäude — Basis für die DIN-4708-
    # Kennzahlen in analysis.kennzahlen (GLF, W_z); ohne diesen Meta-Eintrag
    # bliebe die in DESIGN.md §5 geforderte GLF-Kennzahl unberechnet.
    n_we_gesamt = sum(max(int(b.n_we), 1) * max(int(b.copies), 1)
                      for b in buildings
                      if b.category in (BuildingCategory.EFH,
                                        BuildingCategory.MFH))
    n_pers_gesamt = sum(max(int(b.n_we), 1) * max(int(b.copies), 1)
                        * float(b.n_persons) for b in buildings
                        if b.category in (BuildingCategory.EFH,
                                          BuildingCategory.MFH))

    summe_soll = {c: float(sum(v[c] for v in soll.values())) for c in _COLS}
    summe_ist = {c: float(total[c].sum()) for c in _COLS}
    abweichung = {
        c: (summe_ist[c] / summe_soll[c] - 1.0) if summe_soll[c] > 0 else 0.0
        for c in _COLS}

    # kWh je Intervall -> kW
    df_kw = total / dt_h
    df_kw = df_kw[_COLS].astype(float)

    meta = {
        "quelle": "VDI 4655 (demandlib.vdi) / BDEW (demandlib.bdew)",
        "demandlib_version": getattr(__import__("demandlib"), "__version__",
                                     "0.2.2"),
        "try_region": int(weather.try_region),
        "try_region_name": try_region_names().get(int(weather.try_region)),
        "jahr": year,
        "aufloesung": resolution,
        "seed": int(seed),
        "gebaeude": [asdict(b) for b in buildings],
        "namen": names,
        "n_we": int(n_we_gesamt),
        "n_personen": float(n_pers_gesamt),
        "jahressummen_soll_kwh": soll,
        "jahressummen_ist_kwh": ist,
        "summe_soll_kwh": summe_soll,
        "summe_ist_kwh": summe_ist,
        "abweichung_rel": abweichung,
        "p_max_kw": {c: float(df_kw[c].max()) for c in _COLS},
        "p_max_total_kw": float((df_kw["Q_heiz"] + df_kw["Q_tww"]).max()),
        "hinweis_strom": (
            f"W_a = {W_A_PER_WE_DEFAULT:.0f} kWh/(WE·a) ist nur "
            f"VDI-4655-Pflichtparameter und wird nicht ausgegeben."),
    }

    return ProfileSet(df=df_kw, resolution=resolution, source="synthetisch",
                      meta=meta)
