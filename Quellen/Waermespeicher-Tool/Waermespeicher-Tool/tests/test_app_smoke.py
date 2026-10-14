"""Headless-Smoketest der Streamlit-App.

Die App selbst wird **nicht** importiert — ein Import von ``app.py`` würde
Streamlit-Befehle ohne Skript-Runtime ausführen (``st.set_page_config`` &
Co.) und ist damit kein sinnvoller Test. Stattdessen wird geprüft:

1. ``app.py`` ist syntaktisch gültig (``py_compile``) und importiert nur
   Namen, die es in ``wsp`` auch wirklich gibt.
2. Streamlit und Plotly sind importierbar (Startvoraussetzung).
3. Der Kernpfad, den die App bei den Buttons aufruft, läuft durch:
   ``generate_profiles`` -> ``kennzahlen`` -> ``size_dhw`` -> ``size_buffer``
   -> (falls vorhanden) ``export_excel.write_report`` in einen ``BytesIO``.
"""
from __future__ import annotations

import ast
import io
import os
import py_compile
import sys

import pandas as pd
import pytest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, ROOT)

from wsp import analysis  # noqa: E402
from wsp.models import (Building, BufferParams, BuildingCategory,  # noqa: E402
                        DHWParams, ProfileSet, WeatherConfig)
from wsp.profiles_synthetic import generate_profiles  # noqa: E402
from wsp.sizing_buffer import size_buffer  # noqa: E402
from wsp.sizing_dhw import size_dhw  # noqa: E402

APP = os.path.join(ROOT, "app.py")

#: kleines, aber realistisches Testprojekt (Nicht-Schaltjahr!)
JAHR = 2023


# ------------------------------------------------------------------ Fixtures

@pytest.fixture(scope="module")
def profil() -> ProfileSet:
    """Ein einmal erzeugtes Jahresprofil für alle Tests des Moduls."""
    buildings = [
        Building(name="MFH Test", category=BuildingCategory.MFH, n_we=12,
                 n_persons=2.2, q_heiz_a=90000.0, q_tww_a=15000.0),
        Building(name="Buero Test", category=BuildingCategory.GHD,
                 q_heiz_a=40000.0, bdew_branch="gko", tww_share=0.1),
    ]
    ps = generate_profiles(buildings, WeatherConfig(try_region=12, year=JAHR),
                           resolution="h")
    ps.meta.setdefault("n_we", 12)
    return ps


# ------------------------------------------------------- Start-Voraussetzungen

def test_streamlit_und_plotly_importierbar():
    """Ohne diese Pakete startet die App nicht."""
    import plotly.graph_objects as go
    import streamlit as st

    assert hasattr(st, "data_editor")      # Gebäudetabelle
    assert hasattr(st, "download_button")  # Projekt/Export
    assert hasattr(go, "Figure")


def test_app_py_kompiliert(tmp_path):
    py_compile.compile(APP, cfile=str(tmp_path / "app.pyc"), doraise=True)


def test_app_importiert_nur_vorhandene_namen():
    """Alle ``from wsp... import X`` in app.py müssen auflösbar sein."""
    baum = ast.parse(open(APP, encoding="utf-8").read(), filename=APP)
    geprueft = 0
    for knoten in ast.walk(baum):
        if not isinstance(knoten, ast.ImportFrom) or not knoten.module:
            continue
        if not knoten.module.startswith(("wsp", "vendor")):
            continue
        if knoten.module == "wsp.export_excel":
            continue  # wird in der App bewusst mit try/except behandelt
        modul = __import__(knoten.module, fromlist=["*"])
        for alias in knoten.names:
            assert hasattr(modul, alias.name), \
                f"{knoten.module}.{alias.name} fehlt (app.py Zeile {knoten.lineno})"
            geprueft += 1
    assert geprueft > 5


def test_app_verwendet_export_signatur():
    """write_report wird mit der vereinbarten Signatur aufgerufen."""
    quelltext = open(APP, encoding="utf-8").read()
    assert "write_report(" in quelltext
    assert "projekt=" in quelltext
    assert "io.BytesIO()" in quelltext


# ------------------------------------------------------------ Kernpfad der App

def test_kernpfad_profil_bis_auslegung(profil):
    # --- Schritt 1: Profil (Tab 1) ---------------------------------------
    assert isinstance(profil, ProfileSet)
    assert list(profil.df.columns) == ["Q_heiz", "Q_tww"]
    assert len(profil.df) == 8760
    assert isinstance(profil.df.index, pd.DatetimeIndex)
    summen = profil.annual_sums()
    # MFH 90.000 + 15.000, GHD 40.000 (Gesamtwärme inkl. TWW-Anteil)
    assert summen["Q_heiz"] + summen["Q_tww"] == pytest.approx(145000.0,
                                                               rel=0.01)

    # --- Schritt 2: Kennzahlen + Analyse-Plots (Tab 2) --------------------
    kz = analysis.kennzahlen(profil)
    assert kz["p_max_kw"] > 0
    assert 0 < kz["anteil_tww"] < 1
    assert kz["p_90_prozent_kw"] <= kz["p_95_prozent_kw"] <= kz["p_max_kw"]

    kurve = analysis.jdl(profil.q_total)
    assert len(kurve) == len(profil.df)
    assert kurve.iloc[0] == pytest.approx(kz["p_max_kw"])
    assert analysis.tagesprofile(profil).shape[0] == 24
    assert analysis.wochenlastgang(profil).shape == (24, 7)
    assert len(analysis.top_peaks(profil.q_total, 20)) == 20

    # --- Schritt 3: TWW-Speicher (Tab 3) ---------------------------------
    dhw = size_dhw(DHWParams(p_lade=25.0), n_we=12, persons_per_we=2.2,
                   profile_tww=profil.df["Q_tww"], dt_h=profil.dt_h)
    assert dhw.v_empfehlung_l > 0
    assert dhw.n_bedarf > 0
    assert dhw.v_din4708_l > 0 and dhw.v_faust_l > 0
    assert "massgebend" in dhw.details

    # --- Schritt 4: Pufferspeicher (Tab 4) -------------------------------
    puffer = size_buffer(
        BufferParams(p_wp=60.0, p_wp_min=15.0, dt_puffer=10.0,
                     sperrzeiten=[(11, 2), (17, 2)]),
        profile_heiz=profil.df["Q_heiz"], dt_h=profil.dt_h)
    assert puffer.v_empfehlung_l > 0
    assert puffer.massgebend in ("Abtauung", "Taktung", "EVU-Sperrzeit",
                                "Lastgang-Simulation")
    assert puffer.sim is not None
    assert {"Q_last", "SOC", "Unterdeckung"} <= set(puffer.sim.df.columns)
    # SOC-Plot der App braucht einen DatetimeIndex und eine Kapazität
    assert isinstance(puffer.sim.df.index, pd.DatetimeIndex)
    assert puffer.sim.capacity_kwh >= 0


def test_kernpfad_ohne_profil():
    """Die App erlaubt Auslegung auch ohne Lastgang — darf nicht abstürzen."""
    dhw = size_dhw(DHWParams(), n_we=8, persons_per_we=2.5, profile_tww=None)
    assert dhw.v_profil_l == 0
    assert dhw.v_empfehlung_l > 0

    puffer = size_buffer(BufferParams(p_wp=30.0), profile_heiz=None)
    assert puffer.v_sim_l == 0
    assert puffer.v_empfehlung_l > 0


def test_excel_export_in_bytesio(profil):
    """Export-Tab: write_report(BytesIO, ...) — noch nicht vorhanden = skip."""
    export = pytest.importorskip(
        "wsp.export_excel",
        reason="wsp/export_excel.py wird parallel gebaut — Export übersprungen")
    kz = analysis.kennzahlen(profil)
    dhw = size_dhw(DHWParams(), n_we=12, persons_per_we=2.2,
                   profile_tww=profil.df["Q_tww"], dt_h=profil.dt_h)
    puffer = size_buffer(BufferParams(p_wp=60.0, sperrzeiten=[(11, 2)]),
                         profile_heiz=profil.df["Q_heiz"], dt_h=profil.dt_h)

    puff = io.BytesIO()
    export.write_report(puff, profil, kz, dhw, puffer,
                        projekt={"name": "Smoketest",
                                 "datum": "2026-01-01"})
    daten = puff.getvalue()
    assert len(daten) > 5000
    assert daten[:2] == b"PK"          # xlsx = ZIP-Container


# ------------------------------------------------------- Oberfläche (AppTest)

def test_app_laeuft_headless_durch():
    """Die App komplett headless durchspielen (Streamlit-eigener AppTest).

    Klickt nacheinander „Profil erzeugen", „TWW-Speicher berechnen" und
    „Pufferspeicher berechnen" mit den Voreinstellungen und prüft, dass die
    Oberfläche dabei keine Exception und keine ``st.error``-Meldung erzeugt.
    """
    testing = pytest.importorskip("streamlit.testing.v1",
                                  reason="Streamlit-AppTest nicht verfügbar")
    at = testing.AppTest.from_file(APP, default_timeout=600)
    at.run()
    assert not at.exception, [e.value for e in at.exception]
    assert [t.label for t in at.tabs][:1] == ["1 · Gebäude & Profil"]

    beschriftungen = [b.label for b in at.button]
    assert "▶ Profil erzeugen" in beschriftungen

    at.button[0].click().run()          # Profil erzeugen
    assert not at.exception, [e.value for e in at.exception]
    assert not at.error, [e.value for e in at.error]
    assert any("profil" in s.value.lower() for s in at.success)

    for label in ("▶ TWW-Speicher berechnen", "▶ Pufferspeicher berechnen"):
        knopf = [b for b in at.button if b.label == label]
        assert knopf, f"Button '{label}' fehlt"
        knopf[0].click().run()
        assert not at.exception, (label, [e.value for e in at.exception])
        assert not at.error, (label, [e.value for e in at.error])

    werte = {m.label: m.value for m in at.metric}
    assert "Wärme gesamt" in werte
    assert "DIN 4708" in werte and "EVU-Sperrzeit" in werte


# --------------------------------------------------- Hilfslogik der Oberfläche

def test_beispielwoche_logik(profil):
    """Die App wählt die Woche mit dem lastintensivsten Werktag."""
    s = profil.q_total
    tage = s.resample("D").sum()
    werktage = tage[tage.index.dayofweek < 5]
    tag = pd.Timestamp(werktage.idxmax())
    start = (tag - pd.Timedelta(days=int(tag.dayofweek))).normalize()
    ende = start + pd.Timedelta(days=7) - pd.Timedelta(seconds=1)
    woche = profil.df.loc[start:ende]
    assert start.dayofweek == 0
    assert 160 <= len(woche) <= 168
