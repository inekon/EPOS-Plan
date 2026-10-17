"""Tests für den Zweipunkt-Betrieb (``storage_sim.simulate_zweipunkt``).

Der Zweipunkt-Modus repliziert die Füllstandslogik der SWSG-Referenz-Excel:
Erzeuger AN (deckt Last + lädt bis voll) bzw. AUS (Last allein aus dem
Speicher), Umschaltung bei Mindestfüllstand bzw. Vollfüllung.

Erwartungswerte werden hier **nicht** aus dem Produktivcode abgeleitet,
sondern von Hand gerechnet (Testfall A) bzw. als strukturelle Invarianten
formuliert (Testfall B, synthetischer Jahreslastgang).
"""
from __future__ import annotations

import os
import sys

import numpy as np
import pandas as pd
import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from wsp import storage_sim  # noqa: E402
from wsp.models import BufferParams  # noqa: E402
from wsp.sizing_buffer import availability_mask, betriebssimulation  # noqa: E402

BEISPIEL_CSV = os.path.join(os.path.dirname(os.path.dirname(
    os.path.abspath(__file__))), "examples", "beispiel_lastgang.csv")


# --------------------------------------------------------------------- Helfer

def stunden_index(n: int, start: str = "2025-01-01") -> pd.DatetimeIndex:
    return pd.date_range(start, periods=n, freq="h")


def konstante_last(n: int, kw: float) -> pd.Series:
    return pd.Series(np.full(n, float(kw)), index=stunden_index(n))


def bilanz_rest(sim, dt_h: float = 1.0, soc0: float = 0.0) -> float:
    """Energiebilanz-Residuum [kWh].

    Erzeugung + Speicherentnahme = Last - Unterdeckung
    mit Speicherentnahme = SOC(0) - SOC(Ende).
    """
    df = sim.df
    erzeugung = float(df["P_gen_verf"].sum() * dt_h)
    last = float(df["Q_last"].sum() * dt_h)
    unterdeckung = float(df["Unterdeckung"].sum() * dt_h)
    entnahme = soc0 - float(df["SOC"].iloc[-1])
    return (erzeugung + entnahme) - (last - unterdeckung)


# ------------------------------------------------ (a) handgerechneter Fall

def test_konstante_last_handrechnung():
    """Konstante Last 10 kW, PMAX 30 kW, C 40 kWh, MINF 8 kWh (20 %).

    Handrechnung (dt = 1 h, Start SOC = 0, Modus = Laden):
      h1: P = min(10 + 40/1, 30) = 30 -> SOC = 0 + 20 = 20
      h2: P = min(10 + 20, 30)   = 30 -> SOC = 40 -> voll -> Entladen
      h3: Entladen: 40 - 10 = 30 >= 8 -> SOC = 30, P = 0
      h4: SOC = 20
      h5: SOC = 10
      h6: 10 - 10 = 0 < 8 -> Umschalten im selben Schritt:
          P = min(10 + 30, 30) = 30 -> SOC = 10 + 20 = 30
      h7: P = min(10 + 10, 30) = 20 -> SOC = 40 -> voll -> Entladen
      h8..h10: 30, 20, 10  -> h11 wie h6 ...
    Ab h6 ist die Periode also 5 h (2 h Laden, 3 h Entladen).
    """
    n = 100
    sim = storage_sim.simulate_zweipunkt(
        konstante_last(n, 10.0), p_gen=30.0, capacity_kwh=40.0,
        dt_h=1.0, min_soc_frac=0.2, soc0=0.0)

    soc = sim.df["SOC"].to_numpy()
    p = sim.df["P_gen_verf"].to_numpy()
    laden = sim.df["Laden"].to_numpy()

    assert list(soc[:10]) == [20, 40, 30, 20, 10, 30, 40, 30, 20, 10]
    assert list(p[:10]) == [30, 30, 0, 0, 0, 30, 20, 0, 0, 0]
    assert list(laden[:10]) == [1, 1, 0, 0, 0, 1, 1, 0, 0, 0]

    # Periodizität: ab h6 (Index 5) Zyklus der Länge 5 h
    for i in range(5, n - 5):
        assert soc[i] == pytest.approx(soc[i + 5])

    # Ladezyklen: ein Wechsel Entladen->Laden je 5-h-Periode.
    # Schritt 0 ist der Startzustand (kein Wechsel), der erste Wechsel
    # passiert in Schritt 5, danach alle 5 Schritte: 5, 10, ..., 95.
    erwartet = len(range(5, n, 5))
    assert sim.ladezyklen == erwartet == 19

    # Keine Unterdeckung, Bilanz geschlossen, Grenzen eingehalten
    assert sim.unterdeckung_kwh == 0.0
    assert sim.unterdeckung_h == 0
    assert sim.deckungsgrad == pytest.approx(1.0)
    assert soc.min() >= 8.0 - 1e-9
    assert soc.max() <= 40.0 + 1e-9
    assert bilanz_rest(sim) == pytest.approx(0.0, abs=1e-9)

    # mittlere Laufzeit je Zyklus = 2 h (2 von 5 Stunden Laden)
    laufzeit = float(laden.sum()) / sim.ladezyklen
    assert laufzeit == pytest.approx(2.0, abs=0.15)


def test_kleinerer_speicher_taktet_haeufiger():
    """Halbe Kapazität -> deutlich mehr Ladezyklen, kürzere Laufzeit (Taktung!).

    C = 40 kWh: Periode 5 h (2 h Laden) -> 399 Wechsel in 2000 h.
    C = 20 kWh: Periode 2 h (1 h Laden) -> 999 Wechsel in 2000 h.
    """
    last = konstante_last(2000, 10.0)
    gross = storage_sim.simulate_zweipunkt(last, 30.0, 40.0, min_soc_frac=0.2)
    klein = storage_sim.simulate_zweipunkt(last, 30.0, 20.0, min_soc_frac=0.2)
    assert gross.ladezyklen == 399
    assert klein.ladezyklen == 999
    # mittlere Laufzeit je Zyklus sinkt von 2 h auf 1 h
    assert float(gross.df["Laden"].sum()) / gross.ladezyklen == pytest.approx(2.0, abs=0.02)
    assert float(klein.df["Laden"].sum()) / klein.ladezyklen == pytest.approx(1.0, abs=0.02)


# ----------------------------------------- (c) Umschaltung im selben Schritt

def test_umschaltung_im_selben_schritt():
    """Der Trigger-Schritt lädt bereits — SOC steigt, statt unter MINF zu fallen.

    C = 100, MINF = 20, Start voll (soc0 = 100 -> sofort Entladen).
    Last 30 kW: h1 SOC 70, h2 SOC 40, h3 wäre 10 < 20 -> im selben Schritt
    laden: P = min(30 + 60, 200) = 90 -> SOC = 40 + 60 = 100.
    """
    sim = storage_sim.simulate_zweipunkt(
        konstante_last(6, 30.0), p_gen=200.0, capacity_kwh=100.0,
        dt_h=1.0, min_soc_frac=0.2, soc0=100.0)
    soc = sim.df["SOC"].to_numpy()
    laden = sim.df["Laden"].to_numpy()

    # Start ist Laden-Modus, aber der Speicher ist schon voll -> P deckt nur
    # die Last, danach Umschalten auf Entladen.
    assert soc[0] == pytest.approx(100.0)
    assert sim.df["P_gen_verf"].iloc[0] == pytest.approx(30.0)
    assert list(soc[1:4]) == pytest.approx([70.0, 40.0, 100.0])
    assert list(laden[1:4]) == [0, 0, 1]
    # genau im Trigger-Schritt (Index 3) steigt der SOC wieder
    assert soc.min() >= 20.0 - 1e-9
    assert sim.unterdeckung_kwh == 0.0


def test_unterdeckung_haelt_min_fuellstand():
    """PMAX < Last: SOC bleibt exakt auf MINF, Rest ist Unterdeckung."""
    n = 5
    sim = storage_sim.simulate_zweipunkt(
        konstante_last(n, 50.0), p_gen=20.0, capacity_kwh=100.0,
        dt_h=1.0, min_soc_frac=0.2, soc0=0.0)
    soc = sim.df["SOC"].to_numpy()
    u = sim.df["Unterdeckung"].to_numpy()

    # Start SOC 0 < MINF 20: P = min(50 + 100, 20) = 20 -> nxt = -30 < 20
    # -> u = 50 kW, SOC = 20. Danach jeder Schritt identisch (u = 30 kW).
    assert soc == pytest.approx(np.full(n, 20.0))
    assert u[0] == pytest.approx(50.0)
    assert u[1:] == pytest.approx(np.full(n - 1, 30.0))
    assert sim.unterdeckung_h == n
    assert sim.unterdeckung_kwh == pytest.approx(50.0 + 4 * 30.0)
    assert bilanz_rest(sim) == pytest.approx(0.0, abs=1e-9)


# ------------------------------------------------------ (d) availability-Maske

def test_availability_maske_sperrt_erzeuger():
    """In Sperrstunden ist PMAX = 0 — der Erzeuger liefert nichts."""
    n = 48
    idx = stunden_index(n)
    last = pd.Series(np.full(n, 10.0), index=idx)
    maske = availability_mask(idx, [(11, 2), (17, 2)])

    sim = storage_sim.simulate_zweipunkt(
        last, p_gen=30.0, capacity_kwh=40.0, dt_h=1.0,
        min_soc_frac=0.2, availability=maske, soc0=0.0)

    gesperrt = maske.to_numpy() == 0.0
    assert gesperrt.sum() == 8
    # Kein Erzeugerbetrieb während der Sperrzeit
    assert float(sim.df["P_gen_verf"].to_numpy()[gesperrt].max()) == 0.0
    # Ohne Maske gibt es zu diesen Stunden sehr wohl Ladebetrieb
    frei = storage_sim.simulate_zweipunkt(
        last, p_gen=30.0, capacity_kwh=40.0, min_soc_frac=0.2, soc0=0.0)
    assert float(frei.df["P_gen_verf"].to_numpy()[gesperrt].max()) > 0.0
    assert bilanz_rest(sim) == pytest.approx(0.0, abs=1e-9)


def test_availability_maske_erzwingt_unterdeckung():
    """Lange Sperrzeit + knapper Speicher -> Unterdeckung nur in der Sperrzeit."""
    n = 72
    idx = stunden_index(n)
    last = pd.Series(np.full(n, 20.0), index=idx)
    maske = availability_mask(idx, [(0, 8)])
    sim = storage_sim.simulate_zweipunkt(
        last, p_gen=60.0, capacity_kwh=40.0, dt_h=1.0,
        min_soc_frac=0.2, availability=maske, soc0=0.0)

    u = sim.df["Unterdeckung"].to_numpy()
    gesperrt = maske.to_numpy() == 0.0
    assert u.sum() > 0
    assert np.all(u[~gesperrt] == 0.0)
    assert sim.deckungsgrad < 1.0
    assert sim.df["SOC"].min() >= 0.2 * 40.0 - 1e-9


# ----------------------------------- (b) SWSG-nahe Regression (strukturell)

@pytest.fixture(scope="module")
def beispiel_lastgang() -> pd.Series:
    if not os.path.exists(BEISPIEL_CSV):
        pytest.skip("examples/beispiel_lastgang.csv fehlt")
    df = pd.read_csv(BEISPIEL_CSV, sep=";", decimal=",")
    zeit = pd.to_datetime(df.iloc[:, 0], format="%d.%m.%Y %H:%M")
    last = pd.Series(df.iloc[:, 1].astype(float).to_numpy(), index=zeit)
    assert len(last) == 8760
    return last


# SWSG-Referenz: 2000 l bei ΔT 35 K -> 79,08 kWh, Min-Füllstand 20 %,
# PMAX 100,08 kW. Die echten SWSG-Messdaten liegen hier nicht vor, deshalb
# wird gegen den synthetischen Beispiel-Lastgang nur strukturell geprüft.
SWSG_C = 79.080225
SWSG_MINF = 15.816045
SWSG_PMAX = 100.0808


def test_swsg_referenzfall_struktur(beispiel_lastgang):
    sim = storage_sim.simulate_zweipunkt(
        beispiel_lastgang, p_gen=SWSG_PMAX, capacity_kwh=SWSG_C,
        dt_h=1.0, min_soc_frac=0.2, soc0=0.0)

    soc = sim.df["SOC"].to_numpy()

    # MINF-Konsistenz mit der Referenz
    assert 0.2 * SWSG_C == pytest.approx(SWSG_MINF, abs=1e-6)

    # Füllstandsgrenzen
    assert soc.min() >= SWSG_MINF - 1e-6
    assert soc.max() <= SWSG_C + 1e-9

    # Taktung: sehr kleiner Speicher an einem 8760-h-Lastgang
    assert 300 <= sim.ladezyklen <= 2000

    # Unterdeckung nur in Spitzenlaststunden: nach der Startbefüllung (der
    # Speicher startet leer) tritt Unterdeckung ausschließlich dort auf, wo
    # die Last die verfügbare Erzeugerleistung übersteigt.
    u = sim.df["Unterdeckung"]
    betroffen = u[u > 0]
    assert len(betroffen) > 0, "Beispielprofil sollte Spitzen über PMAX haben"
    # Ausnahme ist allein der erste Zeitschritt: der Speicher startet leer
    # (SOC 0 < MINF) und wird auf den Mindestfüllstand gebracht.
    ohne_start = betroffen.index[betroffen.index > sim.df.index[0]]
    assert bool((sim.df.loc[ohne_start, "Q_last"] > SWSG_PMAX).all())
    assert len(ohne_start) >= len(betroffen) - 1
    # Unterdeckung kann die Energie oberhalb PMAX nie übersteigen
    ueber_pmax = float((sim.df["Q_last"] - SWSG_PMAX).clip(lower=0.0).sum())
    assert sim.unterdeckung_kwh <= ueber_pmax + 1e-6
    assert 0.0 < sim.deckungsgrad < 1.0
    assert sim.deckungsgrad == pytest.approx(
        1.0 - sim.unterdeckung_kwh / float(sim.df["Q_last"].sum()))

    # Ladebetrieb/Entladebetrieb kommen beide vor
    assert 0 < int(sim.df["Laden"].sum()) < len(sim.df)

    # Energiebilanz geschlossen
    assert bilanz_rest(sim) == pytest.approx(0.0, abs=1e-6)


def test_swsg_referenzfall_starker_erzeuger(beispiel_lastgang):
    """Deckt der Erzeuger die Jahresspitze, verschwindet die Unterdeckung."""
    p_max = float(beispiel_lastgang.max())
    sim = storage_sim.simulate_zweipunkt(
        beispiel_lastgang, p_gen=p_max, capacity_kwh=SWSG_C,
        dt_h=1.0, min_soc_frac=0.2, soc0=0.0)
    # nur die Startbefüllung aus dem leeren Speicher kann noch fehlen
    assert sim.unterdeckung_h <= 1
    assert sim.deckungsgrad > 0.9999
    # Kehrseite: der kleine Speicher taktet dann sehr häufig
    assert sim.ladezyklen > 2000


def test_swsg_referenzfall_ueber_wrapper(beispiel_lastgang):
    """betriebssimulation() liefert dasselbe wie der direkte Aufruf."""
    params = BufferParams(p_wp=SWSG_PMAX, p_bivalent=0.0, dt_puffer=35.0,
                          sperrzeiten=[])
    sim = betriebssimulation(params, beispiel_lastgang, SWSG_C,
                             dt_h=1.0, min_soc_frac=0.2)
    direkt = storage_sim.simulate_zweipunkt(
        beispiel_lastgang, SWSG_PMAX, SWSG_C, dt_h=1.0, min_soc_frac=0.2,
        soc0=0.0)
    assert sim.ladezyklen == direkt.ladezyklen
    assert sim.unterdeckung_kwh == pytest.approx(direkt.unterdeckung_kwh)
    assert sim.df["SOC"].to_numpy() == pytest.approx(direkt.df["SOC"].to_numpy())


def test_wrapper_addiert_bivalenten_erzeuger(beispiel_lastgang):
    params = BufferParams(p_wp=60.0, p_bivalent=40.0808, dt_puffer=35.0,
                          sperrzeiten=[])
    sim = betriebssimulation(params, beispiel_lastgang, SWSG_C)
    assert float(sim.df["P_gen"].max()) == pytest.approx(SWSG_PMAX)


def test_wrapper_ohne_lastgang():
    with pytest.raises(ValueError):
        betriebssimulation(BufferParams(), pd.Series(dtype=float), 50.0)


# ------------------------------------------------------------ Abgrenzung API

def test_simulate_unveraendert():
    """Die Durchlauf-Simulation hat weiterhin kein ladezyklen-Feld gesetzt."""
    last = konstante_last(24, 10.0)
    sim = storage_sim.simulate(last, 12.0, 50.0)
    assert sim.ladezyklen is None
    assert "Laden" not in sim.df.columns


def test_zweipunkt_spalten():
    sim = storage_sim.simulate_zweipunkt(konstante_last(24, 10.0), 30.0, 40.0)
    assert list(sim.df.columns) == ["Q_last", "P_gen", "P_gen_verf", "SOC",
                                    "Unterdeckung", "Laden"]
    assert set(sim.df["Laden"].unique()) <= {0, 1}
    assert sim.ladezyklen is not None
