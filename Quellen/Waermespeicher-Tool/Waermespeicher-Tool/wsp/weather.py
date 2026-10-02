"""TRY2010-Wetterdaten (DWD Testreferenzjahre) laden.

Die Rohdaten liegen als Original-DWD-Dateien in ``vendor/try_weather/``
(``TRY2010_{01..15}_Jahr.dat``, aus lpagg übernommen, 15 Klimaregionen).

Geparst wird mit :func:`demandlib.vdi.read_dwd_weather_file` — der Parser
sucht selbst die Kopfzeile vor der ``***``-Trennzeile und kommt mit den
vendorierten ``.dat``-Dateien ohne Anpassung klar (geprüft für alle 15
Regionen). Ein eigener Parser ist daher nicht nötig.

Öffentliche API
---------------
- :func:`load_try_weather` — Stunden-Wetter (TAMB [°C], CCOVER [1/8]) als
  DataFrame mit DatetimeIndex des Zieljahres. Das ist die Rohbasis sowohl
  für ``demandlib.vdi.Climate`` (Tagesmittel + Bewölkungskategorie) als auch
  für ``demandlib.bdew.HeatBuilding`` (Stundentemperatur).
- :func:`get_climate` — fertiges ``demandlib.vdi.Climate``-Objekt.
- :func:`try_region_names` — {1..15: Regionsname} aus den Dateiköpfen.
- :func:`try_region_stations` — {1..15: Referenzstation}.

Hinweis Zeitbezug: Die TRY-Datei enthält 8760 Stundenwerte eines generischen
Jahres (MEZ, Stunde 1..24). Diese werden 1:1 auf den Kalender des Zieljahres
gelegt (Index = Intervallbeginn, 01.01. 00:00 ... 31.12. 23:00). In
Schaltjahren wird der 29.02. durch Wiederholung des 28.02. ergänzt, damit
8784 Stundenwerte vorliegen.
"""
from __future__ import annotations

import calendar
import datetime as dt
import os
import re
from functools import lru_cache

import pandas as pd

from demandlib import vdi

from . import TRY_WEATHER_DIR

__all__ = [
    "TRY_REGIONS",
    "try_weather_file",
    "try_region_names",
    "try_region_stations",
    "try_region_label",
    "load_try_weather",
    "load_energy_factors",
    "get_climate",
]

#: Gültige TRY2010-Klimaregionen
TRY_REGIONS = tuple(range(1, 16))

#: Fallback-Namen (falls Dateikopf nicht lesbar) — DWD TRY2010 Klimaregionen
_TRY_REGION_NAMES_FALLBACK = {
    1: "Nordseeküste",
    2: "Ostseeküste",
    3: "Nordwestdeutsches Tiefland",
    4: "Nordostdeutsches Tiefland",
    5: "Niederrheinisch-westfälische Bucht und Emsland",
    6: "Nördliche und westliche Mittelgebirge, Randgebiete",
    7: "Nördliche und westliche Mittelgebirge, zentrale Bereiche",
    8: "Oberharz und Schwarzwald (mittlere Lagen)",
    9: "Thüringer Becken und Sächsisches Hügelland",
    10: "Südöstliche Mittelgebirge bis 1000 m",
    11: "Erzgebirge, Böhmer- und Schwarzwald oberhalb 1000 m",
    12: "Oberrheingraben und unteres Neckartal",
    13: "Schwäbisch-fränkisches Stufenland und Alpenvorland",
    14: "Schwäbische Alb und Baar",
    15: "Alpenrand und -täler",
}

_TRY_STATIONS_FALLBACK = {
    1: "Bremerhaven", 2: "Rostock", 3: "Hamburg", 4: "Potsdam", 5: "Essen",
    6: "Bad Marienberg", 7: "Kassel", 8: "Braunlage", 9: "Chemnitz",
    10: "Hof", 11: "Fichtelberg", 12: "Mannheim", 13: "Muehldorf",
    14: "Stoetten", 15: "Garmisch-Partenkirchen",
}


# --------------------------------------------------------------- Dateizugriff

def _check_region(region: int) -> int:
    try:
        region = int(region)
    except (TypeError, ValueError):
        raise ValueError(f"TRY-Region muss 1..15 sein, war: {region!r}")
    if region not in TRY_REGIONS:
        raise ValueError(f"TRY-Region muss 1..15 sein, war: {region}")
    return region


def try_weather_file(region: int) -> str:
    """Pfad zur TRY2010-Datei der Klimaregion (1..15)."""
    region = _check_region(region)
    path = os.path.join(TRY_WEATHER_DIR, f"TRY2010_{region:02d}_Jahr.dat")
    if not os.path.isfile(path):
        # Fallback: die in demandlib mitgelieferten Dateien
        alt = os.path.join(os.path.dirname(vdi.__file__), "resources_weather",
                           f"TRY2010_{region:02d}_Jahr.dat")
        if os.path.isfile(alt):
            return alt
        raise FileNotFoundError(f"TRY-Wetterdatei nicht gefunden: {path}")
    return path


def _read_header_lines(region: int, n: int = 2) -> list[str]:
    path = try_weather_file(region)
    for enc in ("utf-8", "cp1252", "latin-1"):
        try:
            with open(path, "r", encoding=enc) as fh:
                return [next(fh).rstrip("\n") for _ in range(n)]
        except (UnicodeDecodeError, StopIteration):
            continue
    return []


@lru_cache(maxsize=1)
def try_region_names() -> dict:
    """{Regionsnummer: Regionsname} — gelesen aus den TRY-Dateiköpfen.

    Kopfzeile 1 hat die Form::

        TRY04   Nordostdeutsches Tiefland            (Klimaregion  4)

    Falls eine Datei fehlt/unlesbar ist, greift eine hartkodierte Liste.
    """
    names = {}
    for region in TRY_REGIONS:
        name = None
        try:
            lines = _read_header_lines(region, 1)
            if lines:
                m = re.match(r"\s*TRY\s*(\d+)\s+(.*?)\s*\(Klimaregion",
                             lines[0])
                if m and int(m.group(1)) == region:
                    name = m.group(2).strip()
        except (OSError, FileNotFoundError):
            name = None
        names[region] = name or _TRY_REGION_NAMES_FALLBACK[region]
    return names


@lru_cache(maxsize=1)
def try_region_stations() -> dict:
    """{Regionsnummer: Name der DWD-Referenzstation}."""
    stations = {}
    for region in TRY_REGIONS:
        station = None
        try:
            lines = _read_header_lines(region, 2)
            if len(lines) >= 2:
                m = re.match(r"\s*Station:\s*(.*?)\s{2,}WMO", lines[1])
                if m:
                    station = m.group(1).strip()
        except (OSError, FileNotFoundError):
            station = None
        stations[region] = station or _TRY_STATIONS_FALLBACK[region]
    return stations


def try_region_label(region: int) -> str:
    """Anzeigetext für Dropdowns, z. B. ``'4 — Nordostdeutsches Tiefland
    (Potsdam)'``."""
    region = _check_region(region)
    return (f"{region} — {try_region_names()[region]} "
            f"({try_region_stations()[region]})")


# ------------------------------------------------------------ Wetter einlesen

@lru_cache(maxsize=16)
def _read_raw(region: int) -> pd.DataFrame:
    """Rohdaten (8760 Zeilen, Index MM/DD/HH) einer TRY-Region."""
    df = vdi.read_dwd_weather_file(try_weather_file(region))
    if len(df) != 8760:
        raise ValueError(
            f"TRY-Datei der Region {region} hat {len(df)} statt 8760 Zeilen.")
    return df


def load_try_weather(region: int, year: int) -> pd.DataFrame:
    """TRY2010-Wetter der Klimaregion als Stunden-Zeitreihe des Jahres.

    Parameters
    ----------
    region : int
        DWD-TRY2010-Klimaregion 1..15.
    year : int
        Kalenderjahr, auf das die 8760 TRY-Stundenwerte gelegt werden.

    Returns
    -------
    pandas.DataFrame
        DatetimeIndex (tz-naiv, Intervallbeginn, Freq ``'h'``), Spalten:

        - ``TAMB``   Lufttemperatur [°C]
        - ``CCOVER`` Bedeckungsgrad [Achtel, 0..8; 9 = nicht bestimmbar]

        In Schaltjahren wird der 29.02. aus dem 28.02. dupliziert
        (8784 Zeilen).

    Notes
    -----
    Dieses DataFrame ist die gemeinsame Basis für
    ``demandlib.vdi.Climate`` (siehe :func:`get_climate`, braucht
    Tagesmittel von ``TAMB`` und die Bewölkungskategorie aus ``CCOVER``)
    und für ``demandlib.bdew.HeatBuilding`` (braucht ``TAMB`` stündlich).
    """
    region = _check_region(region)
    year = int(year)
    raw = _read_raw(region).copy()
    raw = raw.reset_index(drop=True)

    raw = raw[["TAMB", "CCOVER"]]

    if calendar.isleap(year):
        # 8760 TRY-Werte auf 8784 Stunden strecken: 29.02. = Kopie des 28.02.
        # (Werte-Ebene, damit der Kalender ab 01.03. unverschoben bleibt.)
        i_feb28 = 31 * 24 + 27 * 24          # Beginn 28.02. (0-basiert)
        i_mar01 = 31 * 24 + 28 * 24          # Beginn 01.03.
        raw = pd.concat([raw.iloc[:i_mar01],
                         raw.iloc[i_feb28:i_mar01],
                         raw.iloc[i_mar01:]], ignore_index=True)
        periods = 8784
    else:
        periods = 8760

    index = pd.date_range(dt.datetime(year, 1, 1, 0), periods=periods,
                          freq="h")
    df = raw.set_index(index)
    df.index.name = "Time"
    return df


def load_energy_factors(region: int) -> pd.DataFrame:
    """VDI-4655-Typtag-Faktoren der Klimaregion (aus demandlib)."""
    region = _check_region(region)
    fn = os.path.join(os.path.dirname(vdi.__file__), "vdi_data",
                      "VDI_4655_Typtag-Faktoren.csv")
    return pd.read_csv(fn, index_col=[0, 1, 2]).loc[region]


def get_climate(region: int, year: int) -> "vdi.Climate":
    """``demandlib.vdi.Climate`` aus TRY-Wetter der Region/des Jahres.

    Aggregiert das Stundenwetter zu Tagesmitteln, leitet die
    Bewölkungskategorie ab (``'B'`` bedeckt ab 5/8, sonst ``'H'``) und
    lädt die zur Region gehörenden VDI-4655-Typtag-Faktoren — analog zu
    ``lpagg.VDI4655.run_demandlib``.
    """
    weather = load_try_weather(region, year)
    daily = weather.resample("D").mean()
    daily["cloud_category"] = "H"
    daily.loc[daily["CCOVER"] >= 5, "cloud_category"] = "B"

    return vdi.Climate(
        temperature=daily["TAMB"],
        cloud_coverage=daily["cloud_category"],
        energy_factors=load_energy_factors(region),
    )
