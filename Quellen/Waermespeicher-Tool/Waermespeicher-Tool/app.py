"""Wärmespeicher-Tool — Streamlit-Oberfläche.

Auslegung von Trinkwarmwasser- und Heizungs-Pufferspeichern für
Wärmepumpen-Anlagen. Berechnungslogik liegt vollständig im Paket ``wsp``
(siehe DESIGN.md); diese Datei enthält ausschließlich die Bedienoberfläche.

Start:  streamlit run app.py   (oder start_tool.bat unter Windows)
"""
from __future__ import annotations

import calendar
import datetime as dt
import io
import json
import os
import sys
import traceback

import numpy as np
import pandas as pd
import streamlit as st

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import plotly.graph_objects as go  # noqa: E402

from wsp import analysis  # noqa: E402
from wsp.models import (BDEW_BRANCHES, Building, BufferParams,  # noqa: E402
                        BuildingCategory, DHWParams, ProfileSet,
                        WeatherConfig, project_to_dict, volume_to_energy)
from wsp.sizing_buffer import betriebssimulation, size_buffer  # noqa: E402
from wsp.sizing_dhw import size_dhw  # noqa: E402
from wsp.weather import TRY_REGIONS, try_region_label  # noqa: E402

# ---------------------------------------------------------------- Seitensetup

st.set_page_config(page_title="Wärmespeicher-Tool",
                   page_icon="🔥",
                   layout="wide",
                   initial_sidebar_state="expanded")

FARBE_HEIZ = "#c0392b"
FARBE_TWW = "#2980b9"
FARBE_SOC = "#27ae60"
FARBE_MARK = "#7f8c8d"

QUELLE_SYNTH = "Synthetisch (VDI 4655 / BDEW)"
QUELLE_MESS = "Gemessener Lastgang (CSV/Excel)"


# ------------------------------------------------------------------- Helfer

def fmt(x, nk: int = 0) -> str:
    """Zahl in deutscher Schreibweise (Tausenderpunkt, Dezimalkomma)."""
    try:
        if x is None or (isinstance(x, float) and not np.isfinite(x)):
            return "—"
        s = f"{float(x):,.{nk}f}"
    except (TypeError, ValueError):
        return str(x)
    return s.replace(",", "#").replace(".", ",").replace("#", ".")


def fehler_anzeigen(exc: Exception, kontext: str = "") -> None:
    """Fehler benutzerfreundlich + Traceback im Expander ausgeben."""
    kopf = f"{kontext}: " if kontext else ""
    st.error(f"{kopf}{type(exc).__name__} — {exc}")
    with st.expander("Technische Details (Traceback)"):
        st.code("".join(traceback.format_exception(type(exc), exc,
                                                   exc.__traceback__)))


# ------------------------------------------------- BDEW-Branchen (Anzeige)

def _branch_katalog() -> dict:
    """{Branchenschlüssel: Anzeigename} — nutzt das Mapping aus
    ``profiles_synthetic`` (falls exportiert), sonst ``models.BDEW_BRANCHES``."""
    zusatz = {
        "gba": "Bäckerei mit Backstube",
        "ggb": "Gartenbau",
        "gbd": "Sonstige betriebliche Dienstleistungen",
        "gko": "Gebietskörperschaft / Büro / Bank",
        "gmf": "Haushaltsähnliches Gewerbe",
    }
    try:
        from wsp.profiles_synthetic import BDEW_BRANCH_MAP
        keys = [k for k in BDEW_BRANCH_MAP if k.isascii()]
    except Exception:
        keys = [k for k in BDEW_BRANCHES if k.isascii()]
    katalog = {}
    for k in keys:
        name = zusatz.get(k) or BDEW_BRANCHES.get(k) or k.upper()
        katalog[k] = name
    return dict(sorted(katalog.items()))


BRANCHEN = _branch_katalog()
BRANCH_LABELS = {k: f"{k.upper()} — {v}" for k, v in BRANCHEN.items()}
LABEL_ZU_BRANCH = {v: k for k, v in BRANCH_LABELS.items()}


def branch_label(key: str) -> str:
    return BRANCH_LABELS.get(str(key).lower(), BRANCH_LABELS.get("ghd", "GHD"))


def label_zu_branch(label: str) -> str:
    return LABEL_ZU_BRANCH.get(str(label), "ghd")


# ---------------------------------------------------- Gebäudetabelle <-> Modell

SPALTEN = {
    "Name": "name",
    "Kategorie": "category",
    "WE": "n_we",
    "Personen/WE": "n_persons",
    "Q_Heiz [kWh/a]": "q_heiz_a",
    "Q_TWW [kWh/a]": "q_tww_a",
    "Anzahl": "copies",
    "Sigma [min]": "sigma",
    "BDEW-Branche": "bdew_branch",
    "TWW-Anteil (GHD)": "tww_share",
}


def buildings_to_df(buildings: list) -> pd.DataFrame:
    zeilen = []
    for b in buildings:
        kat = b.category.value if isinstance(b.category, BuildingCategory) \
            else str(b.category)
        zeilen.append({
            "Name": b.name,
            "Kategorie": kat,
            "WE": int(b.n_we),
            "Personen/WE": float(b.n_persons),
            "Q_Heiz [kWh/a]": float(b.q_heiz_a),
            "Q_TWW [kWh/a]": (float(b.q_tww_a)
                              if b.q_tww_a is not None else np.nan),
            "Anzahl": int(b.copies),
            "Sigma [min]": float(b.sigma),
            "BDEW-Branche": branch_label(b.bdew_branch),
            "TWW-Anteil (GHD)": float(b.tww_share),
        })
    return pd.DataFrame(zeilen, columns=list(SPALTEN))


def df_to_buildings(df: pd.DataFrame) -> list:
    """Editor-Tabelle -> Liste von ``Building`` (leere Zeilen werden ignoriert)."""
    buildings = []
    for i, row in df.iterrows():
        name = str(row.get("Name") or "").strip()
        kat_roh = str(row.get("Kategorie") or "MFH").strip().upper()
        if not name and pd.isna(row.get("Q_Heiz [kWh/a]")):
            continue
        try:
            kategorie = BuildingCategory(kat_roh)
        except ValueError:
            raise ValueError(
                f"Zeile {i + 1}: Kategorie '{kat_roh}' unbekannt "
                f"(zulässig: EFH, MFH, GHD).")
        q_heiz = row.get("Q_Heiz [kWh/a]")
        if pd.isna(q_heiz) or float(q_heiz) <= 0:
            raise ValueError(
                f"Zeile {i + 1} ('{name or '?'}'): Q_Heiz muss > 0 kWh/a sein.")
        q_tww = row.get("Q_TWW [kWh/a]")
        q_tww = None if (q_tww is None or pd.isna(q_tww)) else float(q_tww)
        buildings.append(Building(
            name=name or f"Gebäude {i + 1}",
            category=kategorie,
            n_we=max(int(row.get("WE") or 1), 1),
            n_persons=float(row.get("Personen/WE") or 2.5),
            q_heiz_a=float(q_heiz),
            q_tww_a=q_tww,
            copies=max(int(row.get("Anzahl") or 1), 1),
            sigma=float(row.get("Sigma [min]") or 0.0),
            bdew_branch=label_zu_branch(row.get("BDEW-Branche")),
            tww_share=float(row.get("TWW-Anteil (GHD)") or 0.0),
        ))
    if not buildings:
        raise ValueError("Die Gebäudetabelle enthält keine gültige Zeile.")
    return buildings


# ------------------------------------------------------------ Session-Defaults

def _default_jahr() -> int:
    jahr = dt.date.today().year
    while calendar.isleap(jahr):
        jahr -= 1
    return jahr


DEFAULTS = {
    "proj_name": "Neues Projekt",
    "quelle": QUELLE_SYNTH,
    "try_region": 12,
    "jahr": _default_jahr(),
    "aufloesung": "h",
    # Messdaten
    "mess_einheit": "kW",
    "mess_dezimal": ",",
    "mess_sep": "automatisch",
    "mess_split": True,
    "mess_split_monate": [6, 7, 8],
    "mess_split_faktor": 1.0,
    # TWW
    "dhw_t_speicher": 60.0,
    "dhw_t_kalt": 10.0,
    "dhw_nutzbar": 0.80,
    "dhw_p_lade": 20.0,
    "dhw_zirk": 0.0,
    "dhw_zuschlag": 0.15,
    "dhw_auto_we": True,
    "dhw_n_we": 10,
    "dhw_personen": 2.5,
    # Puffer
    "buf_p_wp": 50.0,
    "buf_p_wp_min": 15.0,
    "buf_p_biv": 0.0,
    "buf_dt": 10.0,
    "buf_t_min": 10.0,
    "buf_abtau": 20.0,
    "buf_ziel": 1.0,
    "bs_volumen": 1000.0,
    "bs_min_pct": 20.0,
    "sperr_1_an": True, "sperr_1_start": 11, "sperr_1_dauer": 2.0,
    "sperr_2_an": True, "sperr_2_start": 17, "sperr_2_dauer": 2.0,
    "sperr_3_an": False, "sperr_3_start": 6, "sperr_3_dauer": 2.0,
}


def init_state() -> None:
    for key, wert in DEFAULTS.items():
        st.session_state.setdefault(key, wert)
    if "gebaeude_df" not in st.session_state:
        st.session_state["gebaeude_df"] = buildings_to_df([
            Building(name="MFH Musterstraße", category=BuildingCategory.MFH,
                     n_we=30, n_persons=2.2, q_heiz_a=150000.0,
                     q_tww_a=22000.0, copies=1, sigma=4.0),
        ])
    st.session_state.setdefault("profile", None)
    st.session_state.setdefault("kennzahlen", None)
    st.session_state.setdefault("dhw_result", None)
    st.session_state.setdefault("buffer_result", None)


# ------------------------------------------------------- Parameter aus State

def dhw_params_aus_state() -> DHWParams:
    return DHWParams(
        t_speicher=float(st.session_state["dhw_t_speicher"]),
        t_kalt=float(st.session_state["dhw_t_kalt"]),
        nutzbarer_anteil=float(st.session_state["dhw_nutzbar"]),
        p_lade=float(st.session_state["dhw_p_lade"]),
        zirkulation_kw=float(st.session_state["dhw_zirk"]),
        zuschlag=float(st.session_state["dhw_zuschlag"]),
    )


def sperrzeiten_aus_state() -> list:
    fenster = []
    for i in (1, 2, 3):
        if st.session_state.get(f"sperr_{i}_an"):
            dauer = float(st.session_state.get(f"sperr_{i}_dauer") or 0.0)
            if dauer > 0:
                fenster.append((int(st.session_state[f"sperr_{i}_start"]),
                                dauer))
    return fenster


def buffer_params_aus_state() -> BufferParams:
    return BufferParams(
        p_wp=float(st.session_state["buf_p_wp"]),
        p_wp_min=float(st.session_state["buf_p_wp_min"]),
        p_bivalent=float(st.session_state["buf_p_biv"]),
        dt_puffer=float(st.session_state["buf_dt"]),
        t_min_lauf_min=float(st.session_state["buf_t_min"]),
        abtau_l_pro_kw=float(st.session_state["buf_abtau"]),
        sperrzeiten=sperrzeiten_aus_state(),
        ziel_deckung=float(st.session_state["buf_ziel"]),
    )


def weather_aus_state() -> WeatherConfig:
    return WeatherConfig(try_region=int(st.session_state["try_region"]),
                         year=int(st.session_state["jahr"]))


# ------------------------------------------------------- Projekt speichern/laden

def projekt_daten() -> dict:
    """Alle Eingaben als Dict — Basis für Projekt-JSON **und** Excel-Export.

    Enthält die von ``models.project_to_dict`` serialisierten Eingaben
    (``buildings``, ``weather``, ``dhw``, ``buffer``), den Projektnamen auf
    oberster Ebene (so erwartet ihn ``export_excel.write_report``) sowie die
    UI-Einstellungen zur Wiederherstellung beim Laden.
    """
    try:
        buildings = df_to_buildings(st.session_state["gebaeude_df"])
    except Exception:
        buildings = []
    daten = project_to_dict(buildings, weather_aus_state(),
                            dhw_params_aus_state(), buffer_params_aus_state())
    daten["name"] = st.session_state["proj_name"]
    daten["datum"] = dt.date.today().isoformat()
    daten["projekt"] = {
        "name": st.session_state["proj_name"],
        "gespeichert_am": dt.datetime.now().isoformat(timespec="seconds"),
        "tool_version": "1.0",
    }
    daten["ui"] = {
        "quelle": st.session_state["quelle"],
        "aufloesung": st.session_state["aufloesung"],
        "mess_einheit": st.session_state["mess_einheit"],
        "mess_dezimal": st.session_state["mess_dezimal"],
        "mess_sep": st.session_state["mess_sep"],
        "mess_split": bool(st.session_state["mess_split"]),
        "mess_split_monate": list(st.session_state["mess_split_monate"]),
        "mess_split_faktor": float(st.session_state["mess_split_faktor"]),
        "dhw_auto_we": bool(st.session_state["dhw_auto_we"]),
        "dhw_n_we": int(st.session_state["dhw_n_we"]),
        "dhw_personen": float(st.session_state["dhw_personen"]),
        "sperrzeiten_ui": [
            {"aktiv": bool(st.session_state[f"sperr_{i}_an"]),
             "start": int(st.session_state[f"sperr_{i}_start"]),
             "dauer": float(st.session_state[f"sperr_{i}_dauer"])}
            for i in (1, 2, 3)],
    }
    return daten


def projekt_als_json() -> str:
    """Projektdaten als JSON-Text für den Download."""
    return json.dumps(projekt_daten(), indent=2, ensure_ascii=False,
                      default=str)


def projekt_anwenden(daten: dict) -> None:
    """JSON-Projekt in den Session-State schreiben (vor Widget-Erzeugung!)."""
    proj = daten.get("projekt") or {}
    if proj.get("name"):
        st.session_state["proj_name"] = str(proj["name"])

    gebaeude = daten.get("buildings") or []
    if gebaeude:
        objekte = []
        for g in gebaeude:
            g = dict(g)
            kat = g.get("category", "MFH")
            try:
                g["category"] = BuildingCategory(str(kat).upper())
            except ValueError:
                g["category"] = BuildingCategory.MFH
            erlaubt = {f: g.get(f) for f in
                       ("name", "category", "n_we", "n_persons", "q_heiz_a",
                        "q_tww_a", "copies", "sigma", "bdew_branch",
                        "tww_share") if f in g}
            objekte.append(Building(**erlaubt))
        st.session_state["gebaeude_df"] = buildings_to_df(objekte)

    wetter = daten.get("weather") or {}
    if "try_region" in wetter:
        st.session_state["try_region"] = int(wetter["try_region"])
    if "year" in wetter:
        st.session_state["jahr"] = int(wetter["year"])

    dhw = daten.get("dhw") or {}
    for feld, key in (("t_speicher", "dhw_t_speicher"),
                      ("t_kalt", "dhw_t_kalt"),
                      ("nutzbarer_anteil", "dhw_nutzbar"),
                      ("p_lade", "dhw_p_lade"),
                      ("zirkulation_kw", "dhw_zirk"),
                      ("zuschlag", "dhw_zuschlag")):
        if feld in dhw:
            st.session_state[key] = float(dhw[feld])

    puffer = daten.get("buffer") or {}
    for feld, key in (("p_wp", "buf_p_wp"), ("p_wp_min", "buf_p_wp_min"),
                      ("p_bivalent", "buf_p_biv"), ("dt_puffer", "buf_dt"),
                      ("t_min_lauf_min", "buf_t_min"),
                      ("abtau_l_pro_kw", "buf_abtau"),
                      ("ziel_deckung", "buf_ziel")):
        if feld in puffer:
            st.session_state[key] = float(puffer[feld])

    fenster = puffer.get("sperrzeiten") or []
    for i in (1, 2, 3):
        if i <= len(fenster):
            eintrag = fenster[i - 1]
            st.session_state[f"sperr_{i}_an"] = True
            st.session_state[f"sperr_{i}_start"] = int(float(eintrag[0]))
            st.session_state[f"sperr_{i}_dauer"] = float(eintrag[1])
        else:
            st.session_state[f"sperr_{i}_an"] = False

    ui = daten.get("ui") or {}
    if ui.get("quelle") in (QUELLE_SYNTH, QUELLE_MESS):
        st.session_state["quelle"] = ui["quelle"]
    for feld, key, typ in (("aufloesung", "aufloesung", str),
                           ("mess_einheit", "mess_einheit", str),
                           ("mess_dezimal", "mess_dezimal", str),
                           ("mess_sep", "mess_sep", str),
                           ("mess_split", "mess_split", bool),
                           ("mess_split_faktor", "mess_split_faktor", float),
                           ("dhw_auto_we", "dhw_auto_we", bool),
                           ("dhw_n_we", "dhw_n_we", int),
                           ("dhw_personen", "dhw_personen", float)):
        if feld in ui:
            st.session_state[key] = typ(ui[feld])
    if ui.get("mess_split_monate"):
        st.session_state["mess_split_monate"] = [int(m) for m in
                                                 ui["mess_split_monate"]]


def projekt_upload_verarbeiten() -> None:
    """Hochgeladene Projektdatei übernehmen.

    Wird zweifach ausgelöst: als ``on_change``-Callback des Uploaders (läuft
    vor dem Skript) **und** ganz oben im Skript, bevor Eingabe-Widgets
    erzeugt werden. Beides ist zulässig, um Widget-Werte zu setzen; die
    Dateikennung in ``_projekt_geladen`` verhindert doppelte Anwendung.
    """
    datei = st.session_state.get("projekt_upload")
    if datei is None:
        st.session_state["_projekt_geladen"] = None
        return
    kennung = f"{getattr(datei, 'name', '')}:{getattr(datei, 'size', 0)}"
    if st.session_state.get("_projekt_geladen") == kennung:
        return
    try:
        datei.seek(0)
        daten = json.loads(datei.read().decode("utf-8"))
        projekt_anwenden(daten)
        name = (daten.get("name")
                or (daten.get("projekt") or {}).get("name") or "?")
        st.session_state["_projekt_geladen"] = kennung
        st.session_state["_projekt_meldung"] = (
            "ok", f"Projekt '{name}' geladen. Profil bitte neu erzeugen.")
        st.session_state["profile"] = None
        st.session_state["kennzahlen"] = None
        st.session_state["dhw_result"] = None
        st.session_state["buffer_result"] = None
    except Exception as exc:
        st.session_state["_projekt_geladen"] = kennung
        st.session_state["_projekt_meldung"] = (
            "fehler", f"Projektdatei konnte nicht gelesen werden: {exc}")


# ------------------------------------------------------------------- Plots

def plot_jahresgang(profile: ProfileSet) -> go.Figure:
    """Tagesmittelwerte über das Jahr (gestapelte Flächen)."""
    tag = profile.df.resample("D").mean()
    fig = go.Figure()
    fig.add_trace(go.Scatter(x=tag.index, y=tag["Q_heiz"], name="Heizung",
                             mode="lines", stackgroup="q",
                             line=dict(width=0.5, color=FARBE_HEIZ)))
    fig.add_trace(go.Scatter(x=tag.index, y=tag["Q_tww"], name="Trinkwarmwasser",
                             mode="lines", stackgroup="q",
                             line=dict(width=0.5, color=FARBE_TWW)))
    fig.update_layout(title="Jahresgang (Tagesmittelwerte)",
                      xaxis_title="Datum", yaxis_title="Leistung [kW]",
                      hovermode="x unified", height=380,
                      margin=dict(l=40, r=20, t=50, b=40))
    return fig


def kaelteste_woche(s: pd.Series) -> tuple:
    """(Start, Ende) der Woche mit dem lastintensivsten Werktag."""
    if not isinstance(s.index, pd.DatetimeIndex) or s.empty:
        return None, None
    tage = s.resample("D").sum()
    werktage = tage[tage.index.dayofweek < 5]
    if werktage.empty or float(werktage.max()) <= 0:
        werktage = tage
    tag = pd.Timestamp(werktage.idxmax())
    start = (tag - pd.Timedelta(days=int(tag.dayofweek))).normalize()
    ende = start + pd.Timedelta(days=7) - pd.Timedelta(seconds=1)
    return start, ende


def plot_beispielwoche(profile: ProfileSet) -> go.Figure:
    start, ende = kaelteste_woche(profile.q_total)
    aus = profile.df.loc[start:ende] if start is not None else profile.df
    fig = go.Figure()
    fig.add_trace(go.Scatter(x=aus.index, y=aus["Q_heiz"], name="Heizung",
                             mode="lines", stackgroup="q",
                             line=dict(width=0.5, color=FARBE_HEIZ)))
    fig.add_trace(go.Scatter(x=aus.index, y=aus["Q_tww"], name="Trinkwarmwasser",
                             mode="lines", stackgroup="q",
                             line=dict(width=0.5, color=FARBE_TWW)))
    titel = "Beispielwoche (Woche mit dem lastintensivsten Werktag)"
    if start is not None:
        titel += f" — {start:%d.%m.%Y} bis {ende:%d.%m.%Y}"
    fig.update_layout(title=titel, xaxis_title="Zeit",
                      yaxis_title="Leistung [kW]", hovermode="x unified",
                      height=380, margin=dict(l=40, r=20, t=50, b=40))
    return fig


def plot_jdl(profile: ProfileSet, kz: dict) -> go.Figure:
    kurve = analysis.jdl(profile.q_total)
    fig = go.Figure()
    fig.add_trace(go.Scatter(x=kurve.index, y=kurve.to_numpy(),
                             name="Jahresdauerlinie (gesamt)", mode="lines",
                             line=dict(color=FARBE_HEIZ, width=2)))
    for anteil, key in ((90, "p_90_prozent_kw"), (95, "p_95_prozent_kw"),
                        (99, "p_99_prozent_kw")):
        wert = kz.get(key)
        if wert:
            fig.add_hline(y=float(wert), line_dash="dot", line_color=FARBE_MARK,
                          annotation_text=f"P bei {anteil} % der Jahresarbeit: "
                                          f"{fmt(wert, 1)} kW",
                          annotation_position="top right")
    fig.update_layout(title="Jahresdauerlinie", height=420,
                      xaxis_title=f"Rang ({profile.resolution}-Schritte)",
                      yaxis_title="Leistung [kW]",
                      margin=dict(l=40, r=20, t=50, b=40))
    return fig


def plot_tagesprofile(tp: pd.DataFrame, monat: str) -> go.Figure:
    fig = go.Figure()
    farben = {"Werktag": FARBE_HEIZ, "Samstag": FARBE_TWW, "Sonntag": FARBE_SOC}
    for tagtyp in ("Werktag", "Samstag", "Sonntag"):
        if (monat, tagtyp) in tp.columns:
            reihe = tp[(monat, tagtyp)]
            fig.add_trace(go.Scatter(x=reihe.index, y=reihe.to_numpy(),
                                     name=tagtyp, mode="lines+markers",
                                     line=dict(color=farben[tagtyp])))
    fig.update_layout(title=f"Mittlere Tagesprofile — {monat}",
                      xaxis_title="Stunde des Tages",
                      yaxis_title="Mittlere Leistung [kW]",
                      hovermode="x unified", height=380,
                      margin=dict(l=40, r=20, t=50, b=40))
    fig.update_xaxes(dtick=2)
    return fig


def plot_wochenheatmap(wl: pd.DataFrame) -> go.Figure:
    fig = go.Figure(go.Heatmap(
        z=wl.to_numpy().T, x=list(wl.index), y=list(wl.columns),
        colorscale="YlOrRd", colorbar=dict(title="kW"),
        hovertemplate="%{y}, %{x}:00 Uhr<br>%{z:.1f} kW<extra></extra>"))
    fig.update_layout(title="Wochenlastgang (Mittelwerte je Wochentag/Stunde)",
                      xaxis_title="Stunde des Tages", yaxis_title="",
                      height=400, margin=dict(l=40, r=20, t=50, b=40))
    fig.update_yaxes(autorange="reversed")
    return fig


def plot_soc(sim_df: pd.DataFrame, kapazitaet: float) -> go.Figure:
    start, ende = kaelteste_woche(sim_df["Q_last"])
    aus = sim_df.loc[start:ende] if start is not None else sim_df
    fig = go.Figure()
    fig.add_trace(go.Scatter(x=aus.index, y=aus["SOC"], name="Speicherinhalt",
                             mode="lines", fill="tozeroy",
                             line=dict(color=FARBE_SOC)))
    fig.add_trace(go.Scatter(x=aus.index, y=aus["Q_last"], name="Wärmelast",
                             mode="lines", yaxis="y2",
                             line=dict(color=FARBE_HEIZ, width=1)))
    if "P_gen_verf" in aus.columns:
        fig.add_trace(go.Scatter(x=aus.index, y=aus["P_gen_verf"],
                                 name="Erzeuger verfügbar", mode="lines",
                                 yaxis="y2", line=dict(color=FARBE_TWW,
                                                       width=1, dash="dot")))
    if kapazitaet > 0:
        fig.add_hline(y=float(kapazitaet), line_dash="dash",
                      line_color=FARBE_MARK,
                      annotation_text=f"Kapazität {fmt(kapazitaet, 1)} kWh")
    titel = "Speicherfüllstand in der lastintensivsten Woche"
    if start is not None:
        titel += f" ({start:%d.%m.} – {ende:%d.%m.%Y})"
    fig.update_layout(title=titel, xaxis_title="Zeit",
                      yaxis=dict(title="Speicherinhalt [kWh]"),
                      yaxis2=dict(title="Leistung [kW]", overlaying="y",
                                  side="right", showgrid=False),
                      hovermode="x unified", height=420,
                      legend=dict(orientation="h", y=-0.2),
                      margin=dict(l=40, r=40, t=50, b=40))
    return fig


def plot_soc_zweipunkt(sim_df: pd.DataFrame, kapazitaet: float,
                       min_soc_kwh: float) -> go.Figure:
    """SOC-Verlauf im Zweipunkt-Betrieb (kälteste Woche) mit Min-Füllstand."""
    start, ende = kaelteste_woche(sim_df["Q_last"])
    aus = sim_df.loc[start:ende] if start is not None else sim_df
    fig = go.Figure()
    fig.add_trace(go.Scatter(x=aus.index, y=aus["SOC"], name="Speicherinhalt",
                             mode="lines", fill="tozeroy",
                             line=dict(color=FARBE_SOC, shape="hv")))
    fig.add_trace(go.Scatter(x=aus.index, y=aus["Q_last"], name="Wärmelast",
                             mode="lines", yaxis="y2",
                             line=dict(color=FARBE_HEIZ, width=1)))
    fig.add_trace(go.Scatter(x=aus.index, y=aus["P_gen_verf"],
                             name="Erzeuger (an/aus)", mode="lines", yaxis="y2",
                             line=dict(color=FARBE_TWW, width=1, shape="hv")))
    if kapazitaet > 0:
        fig.add_hline(y=float(kapazitaet), line_dash="dash",
                      line_color=FARBE_MARK,
                      annotation_text=f"voll {fmt(kapazitaet, 1)} kWh")
    fig.add_hline(y=float(min_soc_kwh), line_dash="dot", line_color=FARBE_HEIZ,
                  annotation_text=f"Min-Füllstand {fmt(min_soc_kwh, 1)} kWh",
                  annotation_position="bottom right")
    titel = "Zweipunkt-Betrieb: Füllstand in der lastintensivsten Woche"
    if start is not None:
        titel += f" ({start:%d.%m.} – {ende:%d.%m.%Y})"
    fig.update_layout(title=titel, xaxis_title="Zeit",
                      yaxis=dict(title="Speicherinhalt [kWh]"),
                      yaxis2=dict(title="Leistung [kW]", overlaying="y",
                                  side="right", showgrid=False),
                      hovermode="x unified", height=420,
                      legend=dict(orientation="h", y=-0.2),
                      margin=dict(l=40, r=40, t=50, b=40))
    return fig


# =============================================================== Anwendung

init_state()
projekt_upload_verarbeiten()

# ------------------------------------------------------------------ Sidebar

with st.sidebar:
    st.title("Wärmespeicher-Tool")
    st.caption("Auslegung TWW-Speicher & Heizungspuffer für Wärmepumpen")

    st.text_input("Projektname", key="proj_name")

    st.divider()
    st.subheader("Projekt")

    dateiname = ("".join(c if c.isalnum() or c in "-_" else "_"
                         for c in st.session_state["proj_name"]) or "Projekt")
    try:
        st.download_button(
            "💾 Projekt speichern (JSON)",
            data=projekt_als_json().encode("utf-8"),
            file_name=f"{dateiname}_{dt.date.today():%Y-%m-%d}.json",
            mime="application/json", width="stretch")
    except Exception as exc:  # pragma: no cover - defensiv
        st.error(f"Projekt konnte nicht serialisiert werden: {exc}")

    st.file_uploader("📂 Projekt laden (JSON)", type=["json"],
                     key="projekt_upload",
                     on_change=projekt_upload_verarbeiten)

    meldung = st.session_state.pop("_projekt_meldung", None)
    if meldung:
        (st.success if meldung[0] == "ok" else st.error)(meldung[1])

    st.divider()
    profil_state: ProfileSet | None = st.session_state.get("profile")
    if profil_state is None:
        st.info("Noch kein Lastprofil erzeugt.")
    else:
        summen = profil_state.annual_sums()
        st.success(f"Profil: {profil_state.source}, "
                   f"{profil_state.resolution}, "
                   f"{len(profil_state.df)} Schritte")
        st.caption(f"Q_ges = {fmt(sum(summen.values()))} kWh/a")

    st.divider()
    st.caption("Methodik: siehe DESIGN.md — VDI 4655/BDEW (demandlib), "
               "DIN 4708, DIN V 18599-10, VDI 6002, DVGW W 551.")


tab_profil, tab_analyse, tab_tww, tab_puffer, tab_export = st.tabs([
    "1 · Gebäude & Profil", "2 · Analyse", "3 · TWW-Speicher",
    "4 · Pufferspeicher", "5 · Export"])


# ========================================================= Tab 1: Profil

with tab_profil:
    st.header("Gebäude & Lastprofil")
    st.radio("Datenquelle", [QUELLE_SYNTH, QUELLE_MESS], key="quelle",
             horizontal=True)

    if st.session_state["quelle"] == QUELLE_SYNTH:
        st.subheader("Gebäude")
        st.caption("Zeilen über das „+“ am Tabellenende ergänzen. "
                   "EFH/MFH werden nach VDI 4655 abgebildet, GHD über "
                   "BDEW-Standardlastprofile. Q_TWW leer lassen = "
                   "500 kWh/(Person·a) nach VDI 4655.")

        bearbeitet = st.data_editor(
            st.session_state["gebaeude_df"],
            num_rows="dynamic", width="stretch", hide_index=True,
            column_config={
                "Name": st.column_config.TextColumn(
                    "Name", help="Bezeichnung des Gebäudes", width="medium"),
                "Kategorie": st.column_config.SelectboxColumn(
                    "Kategorie", options=[c.value for c in BuildingCategory],
                    required=True, width="small"),
                "WE": st.column_config.NumberColumn(
                    "WE", help="Wohneinheiten je Gebäude (EFH = 1)",
                    min_value=1, max_value=500, step=1, format="%d"),
                "Personen/WE": st.column_config.NumberColumn(
                    "Personen/WE", min_value=0.5, max_value=12.0, step=0.1,
                    format="%.1f"),
                "Q_Heiz [kWh/a]": st.column_config.NumberColumn(
                    "Q_Heiz [kWh/a]",
                    help="Jahres-Raumheizwärme je Gebäude; bei GHD die "
                         "Gesamtwärme (Heizung + TWW)",
                    min_value=0.0, step=1000.0, format="%.0f"),
                "Q_TWW [kWh/a]": st.column_config.NumberColumn(
                    "Q_TWW [kWh/a]",
                    help="Jahres-Trinkwarmwasserwärme; leer = VDI-Default",
                    min_value=0.0, step=500.0, format="%.0f"),
                "Anzahl": st.column_config.NumberColumn(
                    "Anzahl", help="identische Gebäude dieses Typs",
                    min_value=1, max_value=200, step=1, format="%d"),
                "Sigma [min]": st.column_config.NumberColumn(
                    "Sigma [min]",
                    help="Zeitversatz-Streuung für die Gleichzeitigkeit "
                         "mehrerer Gebäude (wirkt erst bei 15-min-Auflösung)",
                    min_value=0.0, max_value=120.0, step=1.0, format="%.0f"),
                "BDEW-Branche": st.column_config.SelectboxColumn(
                    "BDEW-Branche (nur GHD)",
                    options=list(BRANCH_LABELS.values()), width="medium"),
                "TWW-Anteil (GHD)": st.column_config.NumberColumn(
                    "TWW-Anteil (GHD)",
                    help="Anteil des TWW an der GHD-Gesamtwärme (0…1)",
                    min_value=0.0, max_value=1.0, step=0.01, format="%.2f"),
            })
        st.session_state["gebaeude_df"] = bearbeitet

        st.subheader("Wetter & Zeitraster")
        s1, s2, s3 = st.columns(3)
        with s1:
            try:
                regionen = list(TRY_REGIONS)
                st.selectbox("TRY-Klimaregion (DWD 2010)", regionen,
                             key="try_region", format_func=try_region_label)
            except Exception as exc:
                fehler_anzeigen(exc, "TRY-Regionen nicht lesbar")
        with s2:
            heute = dt.date.today().year
            jahre = [j for j in range(heute - 8, heute + 8)
                     if not calendar.isleap(j)]
            if st.session_state["jahr"] not in jahre:
                st.session_state["jahr"] = jahre[len(jahre) // 2]
            st.selectbox("Jahr", jahre, key="jahr",
                         help="Schaltjahre sind ausgeblendet — demandlib 0.2.2 "
                              "(VDI 4655) kann keine Schaltjahre rechnen.")
        with s3:
            st.selectbox("Auflösung", ["h", "15min"], key="aufloesung",
                         format_func=lambda x: "1 Stunde" if x == "h"
                         else "15 Minuten",
                         help="15 min erhöht Rechenzeit und Speicherbedarf "
                              "deutlich, zeigt aber Zapfspitzen realistischer.")

        if st.button("▶ Profil erzeugen", type="primary",
                     width="stretch"):
            try:
                with st.spinner("Lastprofile werden erzeugt "
                                "(VDI 4655 / BDEW über demandlib) …"):
                    from wsp.profiles_synthetic import generate_profiles
                    buildings = df_to_buildings(st.session_state["gebaeude_df"])
                    profil = generate_profiles(
                        buildings, weather_aus_state(),
                        resolution=st.session_state["aufloesung"])
                    we_gesamt = sum(int(b.n_we) * int(b.copies)
                                    for b in buildings
                                    if b.category != BuildingCategory.GHD)
                    if we_gesamt > 0:
                        profil.meta.setdefault("n_we", we_gesamt)
                    profil.meta["projekt"] = st.session_state["proj_name"]
                    st.session_state["profile"] = profil
                    st.session_state["kennzahlen"] = analysis.kennzahlen(profil)
                    st.session_state["dhw_result"] = None
                    st.session_state["buffer_result"] = None
                st.success("Lastprofil erzeugt.")
            except Exception as exc:
                fehler_anzeigen(exc, "Profilerzeugung fehlgeschlagen")

    # ------------------------------------------------------ gemessener Lastgang
    else:
        st.subheader("Messdaten importieren")
        datei = st.file_uploader(
            "Lastgang-Datei (CSV, XLSX, XLSM)",
            type=["csv", "txt", "xlsx", "xlsm"], key="mess_upload")

        blatt = 0
        if datei is not None:
            m1, m2, m3 = st.columns(3)
            with m1:
                st.selectbox("Einheit der Werte", ["kW", "kWh"],
                             key="mess_einheit",
                             help="kW = Momentanleistung, kWh = Energie je "
                                  "Zeitintervall")
            with m2:
                st.selectbox("Dezimaltrennzeichen", [",", "."],
                             key="mess_dezimal")
            with m3:
                st.selectbox("CSV-Trennzeichen",
                             ["automatisch", ";", ",", "Tabulator", "|"],
                             key="mess_sep")

            if str(datei.name).lower().endswith((".xlsx", ".xlsm")):
                try:
                    datei.seek(0)
                    blaetter = pd.ExcelFile(datei).sheet_names
                    blatt = st.selectbox("Tabellenblatt", blaetter)
                except Exception as exc:
                    st.warning(f"Tabellenblätter nicht lesbar ({exc}) — "
                               "es wird das erste Blatt verwendet.")
                    blatt = 0

            sep = {"automatisch": None, "Tabulator": "\t"}.get(
                st.session_state["mess_sep"], st.session_state["mess_sep"])

            vorschau = None
            try:
                from wsp.profiles_measured import _read_table
                vorschau = _read_table(datei, sep,
                                       st.session_state["mess_dezimal"], blatt)
                vorschau.columns = [str(c).strip() for c in vorschau.columns]
            except Exception as exc:
                fehler_anzeigen(exc, "Datei konnte nicht gelesen werden")

            if vorschau is not None and not vorschau.empty:
                st.markdown("**Vorschau (erste 8 Zeilen)**")
                st.dataframe(vorschau.head(8), width="stretch")

                spalten = list(vorschau.columns)
                z1, z2, z3 = st.columns(3)
                with z1:
                    ts_col = st.selectbox("Spalte Zeitstempel", spalten,
                                          index=0)
                with z2:
                    val_col = st.selectbox(
                        "Spalte Wärmelast (gesamt)", spalten,
                        index=min(1, len(spalten) - 1))
                with z3:
                    tww_col = st.selectbox(
                        "Spalte TWW-Anteil (optional)", ["— keine —"] + spalten)
                tww_col = None if tww_col == "— keine —" else tww_col

                st.markdown("**Heizung/TWW-Split**")
                st.checkbox(
                    "TWW über Sommer-Baseline abtrennen", key="mess_split",
                    help="Mittlere Last der Sommermonate = TWW + Zirkulation; "
                         "dieses Band wird ganzjährig als TWW angesetzt.",
                    disabled=tww_col is not None)
                sp1, sp2 = st.columns([2, 1])
                with sp1:
                    st.multiselect(
                        "Sommermonate für die Baseline",
                        list(range(1, 13)), key="mess_split_monate",
                        format_func=lambda m: analysis.MONATSNAMEN[m],
                        disabled=tww_col is not None
                        or not st.session_state["mess_split"])
                with sp2:
                    st.number_input(
                        "Korrekturfaktor", min_value=0.1, max_value=2.0,
                        step=0.05, key="mess_split_faktor",
                        help="< 1 korrigiert saisonal überhöhte "
                             "Zirkulationsverluste.",
                        disabled=tww_col is not None
                        or not st.session_state["mess_split"])

                if st.button("▶ Profil erzeugen", type="primary",
                             width="stretch"):
                    try:
                        with st.spinner("Messdaten werden eingelesen …"):
                            from wsp.profiles_measured import (import_lastgang,
                                                               split_tww_sommer)
                            profil = import_lastgang(
                                datei, timestamp_col=ts_col,
                                value_col=val_col,
                                value_unit=st.session_state["mess_einheit"],
                                tww_col=tww_col,
                                decimal=st.session_state["mess_dezimal"],
                                sep=sep, sheet_name=blatt)
                            if tww_col is None and st.session_state["mess_split"]:
                                monate = tuple(
                                    st.session_state["mess_split_monate"]
                                    or (6, 7, 8))
                                profil = split_tww_sommer(
                                    profil, monate=monate,
                                    faktor=float(
                                        st.session_state["mess_split_faktor"]))
                            profil.meta["projekt"] = st.session_state["proj_name"]
                            st.session_state["profile"] = profil
                            st.session_state["kennzahlen"] = \
                                analysis.kennzahlen(profil)
                            st.session_state["dhw_result"] = None
                            st.session_state["buffer_result"] = None
                        st.success("Lastgang importiert.")
                    except Exception as exc:
                        fehler_anzeigen(exc, "Import fehlgeschlagen")
        else:
            st.info("Bitte eine CSV-/Excel-Datei mit Zeitstempel- und "
                    "Lastspalte hochladen.")

    # ------------------------------------------------------------- Ergebnisse
    profil = st.session_state.get("profile")
    if profil is not None:
        st.divider()
        st.subheader("Ergebnis")
        summen = profil.annual_sums()
        q_heiz = summen.get("Q_heiz", 0.0)
        q_tww = summen.get("Q_tww", 0.0)
        e1, e2, e3, e4 = st.columns(4)
        e1.metric("Wärme gesamt", f"{fmt(q_heiz + q_tww)} kWh/a")
        e2.metric("davon Heizung", f"{fmt(q_heiz)} kWh/a")
        e3.metric("davon TWW", f"{fmt(q_tww)} kWh/a",
                  f"{fmt((q_tww / (q_heiz + q_tww) * 100) if (q_heiz + q_tww) else 0, 1)} %")
        e4.metric("Spitzenlast", f"{fmt(profil.q_total.max(), 1)} kW")

        try:
            st.plotly_chart(plot_jahresgang(profil), width="stretch")
            st.plotly_chart(plot_beispielwoche(profil), width="stretch")
        except Exception as exc:
            fehler_anzeigen(exc, "Plot fehlgeschlagen")

        with st.expander("Soll-/Ist-Bilanz und Metadaten"):
            meta = profil.meta
            if meta.get("summe_soll_kwh"):
                bilanz = pd.DataFrame({
                    "Soll [kWh/a]": meta["summe_soll_kwh"],
                    "Ist [kWh/a]": meta["summe_ist_kwh"],
                    "Abweichung [%]": {k: v * 100 for k, v in
                                       meta.get("abweichung_rel", {}).items()},
                })
                st.dataframe(bilanz.style.format("{:,.1f}"),
                             width="stretch")
            gezeigt = {k: v for k, v in meta.items()
                       if k not in ("gebaeude", "jahressummen_soll_kwh",
                                    "jahressummen_ist_kwh", "luecken")}
            st.json(gezeigt, expanded=False)

        if profil.source == "gemessen":
            with st.expander("Import- und Lückenreport", expanded=True):
                meta = profil.meta
                l1, l2, l3 = st.columns(3)
                l1.metric("Zeitschritte", fmt(meta.get("n_stunden", 0)))
                l2.metric("Lücken", fmt(meta.get("luecken_anzahl", 0)),
                          f"{fmt(meta.get('luecken_h', 0), 1)} h")
                l3.metric("Quell-Auflösung",
                          str(meta.get("aufloesung_quelle", "?")))
                for warnung in meta.get("warnungen", []):
                    st.warning(warnung)
                luecken = meta.get("luecken") or []
                if luecken:
                    st.dataframe(pd.DataFrame(luecken),
                                 width="stretch")
                else:
                    st.success("Keine Lücken gefunden.")
                if meta.get("tww_split"):
                    st.markdown("**TWW-Split (Sommer-Baseline)**")
                    st.json(meta["tww_split"])


# ========================================================= Tab 2: Analyse

with tab_analyse:
    st.header("Analyse des Lastgangs")
    profil = st.session_state.get("profile")
    if profil is None:
        st.info("Bitte zuerst im Tab „Gebäude & Profil“ ein Lastprofil "
                "erzeugen oder importieren.")
    else:
        kz = st.session_state.get("kennzahlen")
        if kz is None:
            try:
                kz = analysis.kennzahlen(profil)
                st.session_state["kennzahlen"] = kz
            except Exception as exc:
                fehler_anzeigen(exc, "Kennzahlen fehlgeschlagen")
                kz = {}

        try:
            st.plotly_chart(plot_jdl(profil, kz), width="stretch")
            st.caption("Die Markierungslinien zeigen die Erzeugerleistung, "
                       "mit der 90 / 95 / 99 % der Jahreswärmearbeit gedeckt "
                       "werden (Bivalenzpunkt-Hilfe).")
        except Exception as exc:
            fehler_anzeigen(exc, "JDL-Plot fehlgeschlagen")

        with st.expander("Größte Lastspitzen (Top 20)"):
            try:
                st.dataframe(analysis.top_peaks(profil.q_total, 20),
                             width="stretch")
            except Exception as exc:
                fehler_anzeigen(exc, "Spitzenwerte fehlgeschlagen")

        st.subheader("Tagesprofile")
        try:
            spalte = st.selectbox(
                "Größe", ["Gesamt", "Heizung", "Trinkwarmwasser"],
                key="analyse_groesse")
            col = {"Gesamt": "Q_total", "Heizung": "Q_heiz",
                   "Trinkwarmwasser": "Q_tww"}[spalte]
            tp = analysis.tagesprofile(profil, col=col)
            monate = list(dict.fromkeys(tp.columns.get_level_values(0)))
            monat = st.selectbox("Zeitraum", monate, index=0,
                                 key="analyse_monat")
            st.plotly_chart(plot_tagesprofile(tp, monat),
                            width="stretch")
            st.plotly_chart(plot_wochenheatmap(analysis.wochenlastgang(
                profil, col=col)), width="stretch")
        except Exception as exc:
            fehler_anzeigen(exc, "Tages-/Wochenprofile fehlgeschlagen")

        st.subheader("Kennzahlen")
        try:
            beschriftung = [
                ("Wärme gesamt", "q_total_kwh", "kWh/a", 0),
                ("Wärme Heizung", "q_heiz_kwh", "kWh/a", 0),
                ("Wärme Trinkwarmwasser", "q_tww_kwh", "kWh/a", 0),
                ("TWW-Anteil", "anteil_tww", "—", 3),
                ("Spitzenlast gesamt", "p_max_kw", "kW", 1),
                ("Spitzenlast Heizung", "p_max_heiz_kw", "kW", 1),
                ("Spitzenlast TWW", "p_max_tww_kw", "kW", 1),
                ("Mittlere Last", "p_mittel_kw", "kW", 1),
                ("Minimale Last", "p_min_kw", "kW", 1),
                ("Vollbenutzungsstunden gesamt", "vollaststunden_h", "h/a", 0),
                ("Vollbenutzungsstunden Heizung", "vollaststunden_heiz_h",
                 "h/a", 0),
                ("Vollbenutzungsstunden TWW", "vollaststunden_tww_h", "h/a", 0),
                ("P bei 90 % der Jahresarbeit", "p_90_prozent_kw", "kW", 1),
                ("P bei 95 % der Jahresarbeit", "p_95_prozent_kw", "kW", 1),
                ("P bei 99 % der Jahresarbeit", "p_99_prozent_kw", "kW", 1),
                ("Wohneinheiten", "n_we", "WE", 0),
                ("Gleichzeitigkeitsfaktor (DIN 4708)", "glf_din4708", "—", 3),
                ("W_z nach DIN 4708", "w_z_kwh_din4708", "kWh", 2),
            ]
            zeilen = [{"Kennzahl": text, "Wert": fmt(kz.get(key), nk),
                       "Einheit": einheit}
                      for text, key, einheit, nk in beschriftung
                      if kz.get(key) is not None]
            st.dataframe(pd.DataFrame(zeilen), width="stretch",
                         hide_index=True)
            st.caption(f"Zeitraum {kz.get('zeitraum', ('?', '?'))[0]} bis "
                       f"{kz.get('zeitraum', ('?', '?'))[1]}, "
                       f"Auflösung {kz.get('aufloesung', '?')}, "
                       f"{fmt(kz.get('n_schritte'))} Zeitschritte.")
        except Exception as exc:
            fehler_anzeigen(exc, "Kennzahlen-Tabelle fehlgeschlagen")


# ====================================================== Tab 3: TWW-Speicher

with tab_tww:
    st.header("Trinkwarmwasser-Speicher")
    profil = st.session_state.get("profile")

    st.subheader("Parameter")
    p1, p2, p3 = st.columns(3)
    with p1:
        st.number_input("Speichertemperatur [°C]", min_value=35.0,
                        max_value=90.0, step=1.0, key="dhw_t_speicher",
                        help="Austrittstemperatur des Speichers; ≥ 60 °C "
                             "wegen Legionellenschutz (DVGW W 551).")
        st.number_input("Kaltwassertemperatur [°C]", min_value=2.0,
                        max_value=25.0, step=1.0, key="dhw_t_kalt")
    with p2:
        st.number_input("Nutzbarer Volumenanteil [–]", min_value=0.3,
                        max_value=1.0, step=0.05, key="dhw_nutzbar",
                        help="Schichtungsgüte / Totvolumen; typ. 0,7–0,9.")
        st.number_input("Ladeleistung WP im TWW-Betrieb [kW]", min_value=0.5,
                        max_value=1000.0, step=1.0, key="dhw_p_lade")
    with p3:
        st.number_input("Zirkulationsverlust [kW]", min_value=0.0,
                        max_value=100.0, step=0.1, key="dhw_zirk",
                        help="Dauerlast der Zirkulation (ganzjährig); "
                             "0 = nicht berücksichtigen.")
        st.number_input("Sicherheitszuschlag [–]", min_value=0.0,
                        max_value=0.5, step=0.05, key="dhw_zuschlag",
                        help="Bereitschafts-/Totvolumenzuschlag, typ. 0,10–0,20.")

    # Wohneinheiten / Personen — bei synthetischer Quelle vorbelegt
    we_auto, pers_auto = None, None
    try:
        gebaeude = df_to_buildings(st.session_state["gebaeude_df"])
        wohnen = [b for b in gebaeude if b.category != BuildingCategory.GHD]
        we_auto = sum(int(b.n_we) * int(b.copies) for b in wohnen)
        if we_auto > 0:
            pers_auto = sum(int(b.n_we) * int(b.copies) * float(b.n_persons)
                            for b in wohnen) / we_auto
    except Exception:
        we_auto, pers_auto = None, None

    synth = (st.session_state["quelle"] == QUELLE_SYNTH
             and we_auto is not None and we_auto > 0)

    st.markdown("**Bedarfsseite (DIN 4708 / Faustwert)**")
    if synth:
        st.checkbox("Wohneinheiten und Belegung aus der Gebäudetabelle "
                    "übernehmen", key="dhw_auto_we")
    else:
        st.session_state["dhw_auto_we"] = False

    g1, g2 = st.columns(2)
    if synth and st.session_state["dhw_auto_we"]:
        n_we = int(we_auto)
        personen = float(pers_auto)
        g1.metric("Wohneinheiten (Summe)", fmt(n_we))
        g2.metric("Personen je WE (gewichtet)", fmt(personen, 2))
    else:
        with g1:
            st.number_input("Wohneinheiten", min_value=1, max_value=5000,
                            step=1, key="dhw_n_we")
        with g2:
            st.number_input("Personen je WE", min_value=0.5, max_value=12.0,
                            step=0.1, key="dhw_personen")
        n_we = int(st.session_state["dhw_n_we"])
        personen = float(st.session_state["dhw_personen"])

    if profil is None:
        st.info("Ohne Lastprofil entfällt das profilbasierte Verfahren — "
                "DIN 4708 und Faustwert werden trotzdem gerechnet.")

    if st.button("▶ TWW-Speicher berechnen", type="primary",
                 width="stretch"):
        try:
            with st.spinner("TWW-Auslegung wird gerechnet …"):
                profil_tww = None
                dt_h = 1.0
                if profil is not None and "Q_tww" in profil.df:
                    profil_tww = profil.df["Q_tww"]
                    dt_h = profil.dt_h
                st.session_state["dhw_result"] = size_dhw(
                    dhw_params_aus_state(), n_we=n_we,
                    persons_per_we=personen, profile_tww=profil_tww,
                    dt_h=dt_h)
        except Exception as exc:
            fehler_anzeigen(exc, "TWW-Auslegung fehlgeschlagen")

    ergebnis = st.session_state.get("dhw_result")
    if ergebnis is not None:
        st.divider()
        st.subheader("Ergebnis")
        massgebend = (ergebnis.details or {}).get("massgebend", "")
        v1, v2, v3 = st.columns(3)
        v1.metric("DIN 4708", f"{fmt(ergebnis.v_din4708_l)} l",
                  "maßgebend" if massgebend == "DIN 4708" else None,
                  help=f"N = {fmt(ergebnis.n_bedarf, 1)}, "
                       f"W_z = {fmt(ergebnis.w_z_kwh, 2)} kWh")
        v2.metric("Profilbasiert",
                  f"{fmt(ergebnis.v_profil_l)} l" if ergebnis.v_profil_l
                  else "—",
                  "maßgebend" if massgebend == "Profilbasiert" else None,
                  help="SOC-Defizit bei konstanter Ladeleistung")
        v3.metric("Faustwert", f"{fmt(ergebnis.v_faust_l)} l",
                  "maßgebend" if massgebend == "Faustwert" else None,
                  help="35 l/(Person·d) bei 60 °C")

        st.markdown(
            f"<div style='padding:1.1rem 1.4rem;border-radius:.6rem;"
            f"background:#eaf4ea;border:1px solid #cfe3cf;margin:.6rem 0;'>"
            f"<div style='font-size:.95rem;color:#2c5d2c;'>Empfehlung "
            f"(maßgebend: {massgebend})</div>"
            f"<div style='font-size:2.6rem;font-weight:700;color:#1e4620;"
            f"line-height:1.15;'>{fmt(ergebnis.v_empfehlung_l)} Liter</div>"
            f"<div style='font-size:.9rem;color:#2c5d2c;'>"
            f"{ergebnis.nl_hinweis} · gerundet auf marktübliche Baugröße "
            f"(ungerundet {fmt((ergebnis.details or {}).get('v_max_ungerundet_l', 0))} l)"
            f"</div></div>", unsafe_allow_html=True)

        details = ergebnis.details or {}
        for warnung in details.get("warnungen", []):
            st.warning(warnung)
        for hinweis in details.get("hinweise", []):
            st.info(hinweis)
        if details.get("legionellen_hinweis"):
            with st.expander("Hinweis Legionellenschutz / Trinkwasserhygiene",
                             expanded=True):
                st.write(details["legionellen_hinweis"])
        with st.expander("Rechenweg und Zwischenwerte"):
            st.json(details, expanded=False)


# ==================================================== Tab 4: Pufferspeicher

with tab_puffer:
    st.header("Heizungs-Pufferspeicher")
    profil = st.session_state.get("profile")

    st.subheader("Parameter")
    b1, b2, b3 = st.columns(3)
    with b1:
        st.number_input("Heizleistung WP [kW]", min_value=1.0,
                        max_value=5000.0, step=1.0, key="buf_p_wp",
                        help="Wärmeleistung am Auslegungspunkt.")
        st.number_input("Minimale Modulationsleistung [kW]", min_value=0.0,
                        max_value=5000.0, step=1.0, key="buf_p_wp_min",
                        help="Kleinste Dauerleistung der WP — maßgeblich für "
                             "die Taktung.")
    with b2:
        st.number_input("Zweiter Erzeuger [kW]", min_value=0.0,
                        max_value=5000.0, step=1.0, key="buf_p_biv",
                        help="Bivalenter Erzeuger (Kessel/Heizstab); "
                             "0 = monovalent.")
        st.number_input("Nutzbare Spreizung Puffer [K]", min_value=3.0,
                        max_value=30.0, step=1.0, key="buf_dt",
                        help="ΔT zwischen Lade- und Entladetemperatur, "
                             "typ. 5–15 K.")
    with b3:
        st.number_input("Mindestlaufzeit [min]", min_value=1.0,
                        max_value=60.0, step=1.0, key="buf_t_min")
        st.number_input("Abtauvolumen [l/kW]", min_value=0.0, max_value=60.0,
                        step=1.0, key="buf_abtau",
                        help="Luft/Wasser-WP typ. 15–35 l/kW; bei Sole/Wasser "
                             "0 setzen.")

    st.number_input("Ziel-Deckungsgrad der Simulation [–]", min_value=0.5,
                    max_value=1.0, step=0.01, key="buf_ziel",
                    help="1,00 = keine Unterdeckung zulässig.")

    st.markdown("**EVU-Sperrzeiten** (täglich wiederkehrend, max. 3 Fenster)")
    for i in (1, 2, 3):
        c1, c2, c3 = st.columns([1, 1, 1])
        with c1:
            st.checkbox(f"Fenster {i} aktiv", key=f"sperr_{i}_an")
        with c2:
            st.number_input(f"Start {i} [Uhr]", min_value=0, max_value=23,
                            step=1, key=f"sperr_{i}_start",
                            disabled=not st.session_state[f"sperr_{i}_an"])
        with c3:
            st.number_input(f"Dauer {i} [h]", min_value=0.0, max_value=8.0,
                            step=0.5, key=f"sperr_{i}_dauer",
                            disabled=not st.session_state[f"sperr_{i}_an"])

    if profil is None:
        st.info("Ohne Lastprofil entfallen Simulation und lastgangbasierte "
                "Sperrzeitrechnung — Abtauung und Taktung werden trotzdem "
                "gerechnet.")

    if st.button("▶ Pufferspeicher berechnen", type="primary",
                 width="stretch"):
        try:
            with st.spinner("Puffer-Auslegung inkl. Lastgang-Simulation "
                            "(Binärsuche über die Kapazität) …"):
                profil_heiz, dt_h = None, 1.0
                if profil is not None and "Q_heiz" in profil.df:
                    profil_heiz = profil.df["Q_heiz"]
                    dt_h = profil.dt_h
                st.session_state["buffer_result"] = size_buffer(
                    buffer_params_aus_state(), profile_heiz=profil_heiz,
                    dt_h=dt_h)
                # Vorbelegung der Betriebs-Simulation: empfohlenes Volumen
                empf = st.session_state["buffer_result"].v_empfehlung_l
                if empf:
                    # auf den Wertebereich des Eingabefelds begrenzen
                    st.session_state["bs_volumen"] = float(
                        min(max(float(empf), 50.0), 200000.0))
        except Exception as exc:
            fehler_anzeigen(exc, "Puffer-Auslegung fehlgeschlagen")

    ergebnis = st.session_state.get("buffer_result")
    if ergebnis is not None:
        st.divider()
        st.subheader("Ergebnis")
        mg = ergebnis.massgebend
        k1, k2, k3, k4 = st.columns(4)
        k1.metric("Abtauung", f"{fmt(ergebnis.v_abtau_l)} l",
                  "maßgebend" if mg == "Abtauung" else None)
        k2.metric("Taktung", f"{fmt(ergebnis.v_takt_l)} l",
                  "maßgebend" if mg == "Taktung" else None)
        k3.metric("EVU-Sperrzeit", f"{fmt(ergebnis.v_sperr_l)} l",
                  "maßgebend" if mg == "EVU-Sperrzeit" else None)
        k4.metric("Lastgang-Simulation",
                  f"{fmt(ergebnis.v_sim_l)} l" if ergebnis.v_sim_l else "—",
                  "maßgebend" if mg == "Lastgang-Simulation" else None,
                  help=f"Kapazität {fmt(ergebnis.c_sim_kwh, 1)} kWh")

        st.markdown(
            f"<div style='padding:1.1rem 1.4rem;border-radius:.6rem;"
            f"background:#eaf1f8;border:1px solid #cddcec;margin:.6rem 0;'>"
            f"<div style='font-size:.95rem;color:#24506f;'>Empfehlung "
            f"(maßgebend: {mg})</div>"
            f"<div style='font-size:2.6rem;font-weight:700;color:#173a52;"
            f"line-height:1.15;'>{fmt(ergebnis.v_empfehlung_l)} Liter</div>"
            f"<div style='font-size:.9rem;color:#24506f;'>gerundet auf "
            f"marktübliche Baugröße (ungerundet "
            f"{fmt((ergebnis.details or {}).get('v_max_ungerundet_l', 0))} l)"
            f"</div></div>", unsafe_allow_html=True)

        details = ergebnis.details or {}
        for warnung in details.get("warnungen", []):
            st.warning(warnung)
        for hinweis in details.get("hinweise", []):
            st.info(hinweis)

        # Praxis-Hinweis: unrealistisch große Sperrzeit-Volumina
        andere = max(ergebnis.v_abtau_l, ergebnis.v_takt_l, ergebnis.v_sim_l)
        if ergebnis.v_sperr_l > 0 and (ergebnis.v_sperr_l > 3000
                                       or ergebnis.v_sperr_l > 2.5 * max(andere, 1.0)):
            st.warning(
                "Das Sperrzeit-Kriterium liefert ein sehr großes Volumen: Eine "
                "vollständige Überbrückung der EVU-Sperrzeit aus dem Puffer ist "
                "bei größeren Anlagen praktisch nicht darstellbar. Üblich ist "
                "eine **Teildeckung während der Sperrzeit** (Absenkbetrieb, "
                "Gebäudespeichermasse, bivalenter Erzeuger) — dann ist das "
                "Sperrzeit-Volumen anteilig anzusetzen bzw. das Kriterium "
                "nachrangig zu behandeln.")

        sim = ergebnis.sim
        if sim is not None and getattr(sim, "df", None) is not None \
                and not sim.df.empty:
            u1, u2, u3 = st.columns(3)
            u1.metric("Deckungsgrad", f"{fmt(sim.deckungsgrad * 100, 2)} %")
            u2.metric("Unterdeckung (Arbeit)",
                      f"{fmt(sim.unterdeckung_kwh, 1)} kWh")
            u3.metric("Unterdeckung (Dauer)", f"{fmt(sim.unterdeckung_h)} h")
            try:
                st.plotly_chart(plot_soc(sim.df, sim.capacity_kwh),
                                width="stretch")
            except Exception as exc:
                fehler_anzeigen(exc, "SOC-Plot fehlgeschlagen")

        # ------------------------------------------- Betriebs-Simulation
        with st.expander("Betriebs-Simulation (Zweipunkt, wie SWSG-Excel)",
                         expanded=False):
            st.caption(
                "Die **Auslegungs-Simulation** oben beantwortet: *Wie groß muss "
                "der Speicher sein, damit keine Unterdeckung entsteht?* — der "
                "Erzeuger läuft dort durchgehend mit (Durchlauf-Logik). Die "
                "**Betriebs-Simulation** hier beantwortet die andere Frage: "
                "*Wie verhält sich ein konkret gewählter Speicher im "
                "Zweipunktbetrieb?* Der Erzeuger ist entweder AN (deckt die "
                "Last und lädt den Speicher voll) oder AUS (die Last wird "
                "allein aus dem Speicher gedeckt, bis der Mindestfüllstand "
                "erreicht ist). Entscheidend ist hier die **Taktung**: je "
                "kleiner der Speicher, desto mehr Ladezyklen pro Jahr.")

            if profil is None or "Q_heiz" not in profil.df:
                st.info("Für die Betriebs-Simulation wird ein Lastprofil "
                        "benötigt.")
            else:
                s1, s2 = st.columns(2)
                with s1:
                    st.number_input(
                        "Gewähltes Speichervolumen [l]", min_value=50.0,
                        max_value=200000.0, step=50.0, key="bs_volumen",
                        help="Vorbelegt mit der Empfehlung aus der Auslegung; "
                             "hier kann eine marktübliche Baugröße geprüft "
                             "werden.")
                with s2:
                    st.number_input(
                        "Minimaler Füllstand [%]", min_value=0.0,
                        max_value=90.0, step=5.0, key="bs_min_pct",
                        help="Unterschreitet der Füllstand diesen Wert, "
                             "schaltet der Erzeuger ein (SWSG-Excel: 20 %).")
                st.caption(
                    "Verwendet werden die oben eingestellten Parameter: "
                    f"P_gen = {fmt(st.session_state['buf_p_wp'] + st.session_state['buf_p_biv'], 1)} kW "
                    "(WP + bivalenter Erzeuger), ΔT = "
                    f"{fmt(st.session_state['buf_dt'], 1)} K sowie die "
                    "EVU-Sperrzeiten. Lastgang: Heizlast (Q_heiz).")

                try:
                    volumen_l = float(st.session_state["bs_volumen"])
                    min_frac = float(st.session_state["bs_min_pct"]) / 100.0
                    dt_puffer = float(st.session_state["buf_dt"])
                    kapazitaet = volume_to_energy(volumen_l, dt_puffer)
                    dt_h = profil.dt_h
                    bs = betriebssimulation(
                        buffer_params_aus_state(), profil.df["Q_heiz"],
                        kapazitaet, dt_h=dt_h, min_soc_frac=min_frac)

                    lade_h = float(bs.df["Laden"].sum()) * dt_h
                    laufzeit = (lade_h / bs.ladezyklen) if bs.ladezyklen else 0.0

                    st.markdown(
                        f"**{fmt(volumen_l)} l** bei ΔT "
                        f"{fmt(dt_puffer, 1)} K → nutzbare Kapazität "
                        f"**{fmt(kapazitaet, 1)} kWh**, Mindestfüllstand "
                        f"{fmt(min_frac * 100, 0)} % = "
                        f"{fmt(kapazitaet * min_frac, 1)} kWh.")

                    z1, z2, z3, z4 = st.columns(4)
                    z1.metric("Ladezyklen", f"{fmt(bs.ladezyklen)} /a",
                              help="Wechsel Entladen → Laden über den "
                                   "Simulationszeitraum.")
                    z2.metric("Unterdeckung",
                              f"{fmt(bs.unterdeckung_kwh, 1)} kWh",
                              f"{fmt(bs.unterdeckung_h)} h", delta_color="off")
                    z3.metric("Deckungsgrad",
                              f"{fmt(bs.deckungsgrad * 100, 2)} %")
                    z4.metric("Ø Laufzeit je Zyklus", f"{fmt(laufzeit, 2)} h",
                              help="Ladestunden geteilt durch Ladezyklen.")

                    if bs.ladezyklen and laufzeit < 1.0:
                        st.warning(
                            f"Sehr kurze Laufzeiten ({fmt(laufzeit, 2)} h je "
                            f"Zyklus) und {fmt(bs.ladezyklen)} Ladezyklen: Der "
                            "Speicher ist für einen ruhigen Zweipunktbetrieb "
                            "zu klein — größeres Volumen oder größere Spreizung "
                            "ΔT prüfen.")

                    try:
                        st.plotly_chart(
                            plot_soc_zweipunkt(bs.df, kapazitaet,
                                               kapazitaet * min_frac),
                            width="stretch")
                    except Exception as exc:
                        fehler_anzeigen(exc, "SOC-Plot (Zweipunkt) "
                                             "fehlgeschlagen")

                    # Kennwerte für den Excel-Bericht ablegen
                    details["betriebssimulation"] = {
                        "volumen_l": volumen_l,
                        "kapazitaet_kwh": float(kapazitaet),
                        "min_soc_frac": min_frac,
                        "ladezyklen": int(bs.ladezyklen or 0),
                        "unterdeckung_kwh": float(bs.unterdeckung_kwh),
                        "unterdeckung_h": int(bs.unterdeckung_h),
                        "deckungsgrad": float(bs.deckungsgrad),
                        "laufzeit_mittel_h": float(laufzeit),
                    }
                    ergebnis.details = details
                except Exception as exc:
                    fehler_anzeigen(exc, "Betriebs-Simulation fehlgeschlagen")

        with st.expander("Rechenweg und Zwischenwerte"):
            st.json(details, expanded=False)


# ========================================================= Tab 5: Export

with tab_export:
    st.header("Excel-Bericht")
    profil = st.session_state.get("profile")
    if profil is None:
        st.info("Bitte zuerst ein Lastprofil erzeugen — ohne Profil kann kein "
                "Bericht erstellt werden.")
    else:
        st.write("Der Bericht enthält die Eingaben, den Lastgang, die "
                 "Jahresdauerlinie, Tagesprofile, die Speicherauslegungen und "
                 "die Kennzahlen als Excel-Arbeitsmappe mit nativen Diagrammen.")
        if st.session_state.get("dhw_result") is None:
            st.caption("Hinweis: Es liegt noch kein TWW-Ergebnis vor — das "
                       "entsprechende Blatt bleibt leer.")
        if st.session_state.get("buffer_result") is None:
            st.caption("Hinweis: Es liegt noch kein Puffer-Ergebnis vor — das "
                       "entsprechende Blatt bleibt leer.")

        if st.button("▶ Excel-Bericht erzeugen", type="primary",
                     width="stretch"):
            try:
                with st.spinner("Excel-Bericht wird erstellt …"):
                    try:
                        from wsp.export_excel import write_report
                    except ImportError as exc:
                        raise ImportError(
                            "Das Modul wsp/export_excel.py ist (noch) nicht "
                            f"verfügbar: {exc}") from exc
                    kz = st.session_state.get("kennzahlen") \
                        or analysis.kennzahlen(profil)
                    puffer = io.BytesIO()
                    write_report(puffer, profil, kz,
                                 st.session_state.get("dhw_result"),
                                 st.session_state.get("buffer_result"),
                                 projekt=projekt_daten())
                    puffer.seek(0)
                    st.session_state["_export_bytes"] = puffer.getvalue()
                st.success("Bericht erstellt.")
            except Exception as exc:
                st.session_state.pop("_export_bytes", None)
                fehler_anzeigen(exc, "Excel-Export fehlgeschlagen")

        daten = st.session_state.get("_export_bytes")
        if daten:
            name = ("".join(c if c.isalnum() or c in "-_" else "_"
                            for c in st.session_state["proj_name"]) or "Bericht")
            st.download_button(
                "⬇ Excel-Bericht herunterladen", data=daten,
                file_name=f"{name}_Waermespeicher_{dt.date.today():%Y-%m-%d}.xlsx",
                mime=("application/vnd.openxmlformats-officedocument."
                      "spreadsheetml.sheet"),
                width="stretch")

    st.divider()
    st.caption("Berechnungsgrundlagen: VDI 4655 / BDEW-SLP (demandlib, MIT), "
               "DIN 4708 (vendored aus lpagg, MIT), DIN V 18599-10, VDI 6002, "
               "DVGW W 551. Details siehe DESIGN.md.")
