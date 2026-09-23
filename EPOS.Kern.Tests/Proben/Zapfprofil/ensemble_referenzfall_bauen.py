#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FIKTIVER REFERENZFALL des Auslegungsensembles des Zapfprofilgenerators (Stufe Z3).

ALLE WERTE SIND ERFUNDEN. Eingabe (ensemble_referenzfall_eingabe.json) und Ergebnis
(ensemble_referenzfall_ergebnis.csv) enthalten keine Normzahl, keine Jordan/Vajen-Zahl, kein
Normprofil und keine Hersteller- oder Produktdaten (Umsetzungskonzept Zapfprofilgenerator,
Kapitel 6).

ZWECK. Unabhaengige Nachrechnung nach den Formeln des Umsetzungskonzepts 4.4 und 4.5 b - OHNE
den C#-Code:
  - Zufall: SplitMix64/xoshiro256** aus zufall_referenz_bauen.py (derselbe Ordner, ebenfalls
    ohne C#), Seed je Realisierung SplitMix64(SplitMix64(Seed) xor r), je Zone
    SplitMix64(. xor Index), je Einheit SplitMix64(. xor Einheit);
  - gestutztes Mittel des gekappten Volumenstroms exakt fuer z = Summe zwoelf Gleichverteilter
    minus 6 (Irwin-Hall als Stueckpolynom) samt oberer Kappung; Anteile auf Summe 1 normiert;
  - je Einheit und Kategorie: lambda = Anteil * Q / (V_mittel * Dauer * c_w / 1000 * dtheta),
    Poisson-Zahl, Stunde nach dem Tagesgang, Minute gleichverteilt, Volumenstrom
    min(max(0, mu + sigma * z), Kappung), Energie V * Dauer * c_w * dtheta / 1000;
  - Superposition der Einheiten je Zone und der Zonen zur Gruppe, Ereignisse gleichmaessig ueber
    ihre Minuten, ueber Mitternacht am Tagesanfang weiter;
  - je Realisierung Tagessumme, Minuten- und Stundenspitze, Minutenwerte; Perzentile nach dem
    naechsten Rang (P50/P90/P95/P99, Minimum, Maximum); Spitze je Einheit je Zone;
    Gleichzeitigkeit GLF_P; Vergleich mu + z * sigma / sqrt(N) aus der Einzelstatistik;
  - Summenlinie beim festen Phi_N (kleinstes Volumen, Funktion aus auslegung_referenzfall_bauen.py,
    ebenfalls ohne C#): Volumen je Realisierung, Perzentile, Volumen der ersten Einheit je Zone,
    GLF_V mit dem Anteil der Einheit - einmal ohne Zirkulation ("summenlinie") und einmal mit
    Zirkulation im Laufzeitfenster ("summenlinie_zirkulation", Groessen mit Praefix zirk_).

Der Test EPOS.Kern.Tests/ZapfensembleReferenzfallTests liest Eingabe und CSV, rechnet mit
Zapfensemble und verlangt Abweichung 0 auf 1e-9 (|C# - Referenz| <= 0,5e-9 + 1e-12 * |Referenz|).

WIEDERHOLBAR. Das Skript liest nur die Eingabe und schreibt die CSV neben sich; ein zweiter Lauf
erzeugt dieselben Bytes.

Aufruf (Windows: py, sonst python3):
    py EPOS.Kern.Tests/Proben/Zapfprofil/ensemble_referenzfall_bauen.py
"""

import json
import math
import os
import sys

ORDNER = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ORDNER)
sys.dont_write_bytecode = True   # kein __pycache__ im Probenordner

from zufall_referenz_bauen import Zufall, realisierungsseed, kindseed  # noqa: E402
from auslegung_referenzfall_bauen import kleinstes_volumen, minutenwerte  # noqa: E402

EINGABE = os.path.join(ORDNER, "ensemble_referenzfall_eingabe.json")
ERGEBNIS = os.path.join(ORDNER, "ensemble_referenzfall_ergebnis.csv")

C_W = 1.163          # Wh/(l*K), physikalische Konstante (Konzept 4.0)
MINUTEN = 1440
SUMMANDEN = 12
VERSATZ = 6.0


# ----------------------------------------------------------------------------------------
# Gestutztes Mittel (Irwin-Hall, exakt)
# ----------------------------------------------------------------------------------------

def fakultaet(n):
    f = 1.0
    for i in range(2, n + 1):
        f *= i
    return f


FAKULTAET = fakultaet(SUMMANDEN + 1)


def positivteil(c):
    """E[max(0, c + z)] fuer z = Summe von 12 Gleichverteilten minus 6."""
    if not c > -VERSATZ:
        return 0.0
    if c >= VERSATZ:
        return c
    if c > 0:
        return c + positivteil(-c)
    b = VERSATZ + c
    summe = 0.0
    binom = 1.0
    for k in range(SUMMANDEN):
        x = b - k
        if not x > 0:
            break
        p = 1.0
        for _ in range(SUMMANDEN + 1):
            p *= x
        summe += (binom if k % 2 == 0 else -binom) * p
        binom = binom * (SUMMANDEN - k) / (k + 1)
    return summe / FAKULTAET


def gestutzt(mu, sigma, kappung):
    if not sigma > 0:
        v = mu if mu > 0 else 0.0
        return kappung if (kappung is not None and v > kappung) else v
    e = sigma * positivteil(mu / sigma)
    if kappung is not None:
        e -= sigma * positivteil((mu - kappung) / sigma)
    return e


def kategoriensatz(kategorien, id_art):
    eigene = [k for k in kategorien if k["id_art"] == id_art]
    summe = 0.0
    for k in eigene:
        summe += k["anteil"]
    satz = []
    for k in eigene:
        mittel = gestutzt(k["mu"], k["sigma"], k["kappung"])
        satz.append({"k": k, "anteil": k["anteil"] / summe, "e1k": mittel * k["dauer"] * C_W / 1000.0})
    return satz


# ----------------------------------------------------------------------------------------
# Tageszeitdichte und Ereignisse einer Einheit
# ----------------------------------------------------------------------------------------

class Dichte:
    def __init__(self, gang):
        self.kum = []
        summe = 0.0
        self.letzte = -1
        for h in range(24):
            a = gang[h]
            summe += a
            self.kum.append(summe)
            if a > 0:
                self.letzte = h

    def minute(self, z):
        ziel = z.gleich() * self.kum[23]
        stunde = self.letzte
        for h in range(self.letzte):
            if ziel < self.kum[h]:
                stunde = h
                break
        return stunde * 60 + z.ganzzahl(60)


def ziehen(z, satz, q, spreizung, dichte):
    ereignisse = []
    if not q > 0 or dichte.letzte < 0:
        return ereignisse
    for w in satz:
        k = w["k"]
        lam = w["anteil"] * q / (w["e1k"] * spreizung)
        n = z.poisson(lam)
        if n == 0:
            continue
        faktor = k["dauer"] * C_W * spreizung / 1000.0
        for _ in range(n):
            minute = dichte.minute(z)
            v = k["mu"] + k["sigma"] * z.normal()
            if v < 0:
                v = 0.0
            if k["kappung"] is not None and v > k["kappung"]:
                v = k["kappung"]
            ereignisse.append((minute, k["dauer"], v * faktor))
    return ereignisse


# ----------------------------------------------------------------------------------------
# Auswertung
# ----------------------------------------------------------------------------------------

def perzentile(werte):
    w = sorted(werte)
    n = len(w)

    def rang(p):
        k = (p * n + 99) // 100
        return w[(k if k >= 1 else 1) - 1]
    return {"p50": rang(50), "p90": rang(90), "p95": rang(95), "p99": rang(99), "min": w[0], "max": w[-1]}


def spitzen(minuten):
    groesste = 0.0
    summe = 0.0
    for x in minuten:
        summe += x
        if x > groesste:
            groesste = x
    stunde = 0.0
    for h in range(24):
        s = 0.0
        for m in range(60):
            s += minuten[h * 60 + m]
        if s > stunde:
            stunde = s
    return summe, groesste * 60, stunde


def volumen(q, p, leistung):
    pp = dict(p)
    pp["erzeuger_kw"] = leistung
    pp["uebertrager"] = None
    try:
        return kleinstes_volumen(q, pp)[0]
    except ValueError:
        return math.inf


def rechnen(e):
    seed, r_zahl, perz = e["seed"], e["realisierungen"], e["perzentil"]
    zonen = e["zonen"]
    saetze = [kategoriensatz(e["kategorien"], z["id_art"]) for z in zonen]
    dichten = [Dichte(z["gang"]) for z in zonen]
    tage = []
    einheitsspitzen = [[] for _ in zonen]
    vertreter = [[] for _ in zonen]
    summe = [[0.0] * MINUTEN for _ in zonen]
    quadrat = [[0.0] * MINUTEN for _ in zonen]
    for r in range(r_zahl):
        rs = realisierungsseed(seed, r)
        gruppe = []
        for zi, z in enumerate(zonen):
            zs = kindseed(rs, z["index"])
            je = z["tagesmenge_kwh"] / z["einheiten"]
            s_r = [0.0] * MINUTEN
            q_r = [0.0] * MINUTEN
            for u in range(z["einheiten"]):
                zz = Zufall(kindseed(zs, u))
                ev = ziehen(zz, saetze[zi], je, z["spreizung_k"], dichten[zi])
                minuten = [0.0] * MINUTEN
                for (t0, d, en) in ev:
                    jm = en / d
                    for k in range(d):
                        minuten[(t0 + k) % MINUTEN] += jm
                groesste = 0.0
                for t in range(MINUTEN):
                    x = minuten[t]
                    if x > groesste:
                        groesste = x
                    s_r[t] += x
                    q_r[t] += x * x
                einheitsspitzen[zi].append(groesste * 60)
                if u == 0:
                    vertreter[zi].append(list(ev))
                gruppe.extend(ev)
            for t in range(MINUTEN):
                summe[zi][t] += s_r[t]
                quadrat[zi][t] += q_r[t]
        tage.append(minutenwerte(gruppe))

    werte = {}
    tagessummen, minutenspitzen, stundenspitzen = [], [], []
    for r, q in enumerate(tage):
        s, mk, sk = spitzen(q)
        tagessummen.append(s)
        minutenspitzen.append(mk)
        stundenspitzen.append(sk)
        werte[f"r{r}_tagessumme_kwh"] = s
        werte[f"r{r}_minutenspitze_kw"] = mk
        werte[f"r{r}_stundenspitze_kw"] = sk
        for t in range(MINUTEN):
            werte[f"r{r}_minute_{t:04d}"] = q[t]
    for name, reihe in (("minutenspitze_kw", minutenspitzen), ("stundenspitze_kw", stundenspitzen)):
        for k, v in perzentile(reihe).items():
            werte[f"{name}_{k}"] = v
    pz = f"p{perz}"
    nenner = 0.0
    for zi, z in enumerate(zonen):
        pw = perzentile(einheitsspitzen[zi])
        for k, v in pw.items():
            werte[f"zone{zi}_spitze_je_einheit_kw_{k}"] = v
        nenner += z["einheiten"] * pw[pz]
    werte["glf_p"] = perzentile(minutenspitzen)[pz] / nenner

    # Vergleich mu + z * sigma / sqrt(N) aus der Einzelstatistik.
    groesste = 0.0
    for t in range(MINUTEN):
        mittel = 0.0
        varianz = 0.0
        for zi, z in enumerate(zonen):
            stichproben = float(r_zahl) * z["einheiten"]
            m = summe[zi][t] / stichproben
            q2 = quadrat[zi][t] / stichproben
            v = q2 - m * m
            mittel += z["einheiten"] * m
            varianz += z["einheiten"] * (v if v > 0 else 0.0)
        w = mittel + e["quantil"] * math.sqrt(varianz)
        if w > groesste:
            groesste = w
    werte["wurzel_n_kw"] = groesste * 60

    # Summenlinie beim festen Phi_N - ohne Zirkulation und mit Zirkulation (Laufzeitfenster).
    volumina(werte, "", e["summenlinie"], tage, vertreter, zonen, pz)
    volumina(werte, "zirk_", e["summenlinie_zirkulation"], tage, vertreter, zonen, pz)
    return werte


def volumina(werte, praefix, summenlinie, tage, vertreter, zonen, pz):
    """Volumen je Realisierung beim festen Phi_N, Perzentile, Volumen der ersten Einheit je Zone
    (Anteil ihrer Tagesmenge an Phi_N, Speicherverlust und Zirkulation) und GLF_V."""
    p = dict(summenlinie)
    phi = p["leistung_kw"]
    vol = [volumen(q, p, phi) for q in tage]
    for r, v in enumerate(vol):
        werte[f"{praefix}r{r}_volumen_l"] = v
    pv = perzentile(vol)
    for k, v in pv.items():
        werte[f"{praefix}volumen_l_{k}"] = v
    tag_summe = 0.0
    for z in zonen:
        tag_summe += z["tagesmenge_kwh"]
    nenner = 0.0
    for zi, z in enumerate(zonen):
        anteil = z["tagesmenge_kwh"] / z["einheiten"] / tag_summe
        if not anteil > 0:
            continue
        eigen = dict(p)
        eigen["speicherverlust_kw"] = p["speicherverlust_kw"] * anteil
        eigen["zirkulation_kw"] = p["zirkulation_kw"] * anteil
        einzel = [volumen(minutenwerte(ev), eigen, phi * anteil) for ev in vertreter[zi]]
        pe = perzentile(einzel)
        for k, v in pe.items():
            werte[f"{praefix}zone{zi}_volumen_einheit_l_{k}"] = v
        nenner += z["einheiten"] * pe[pz]
    werte[f"{praefix}glf_v"] = pv[pz] / nenner


def main():
    with open(EINGABE, encoding="utf-8") as f:
        e = json.load(f)
    werte = rechnen(e)
    with open(ERGEBNIS, "w", encoding="utf-8", newline="\r\n") as f:
        f.write("# Referenzfall des Auslegungsensembles (Stufe Z3) - erzeugt von ensemble_referenzfall_bauen.py\n")
        f.write("groesse,wert\n")
        for name, wert in werte.items():
            f.write(f"{name},{wert:.12f}\n")
    print(f"{len(werte)} Groessen nach {ERGEBNIS} geschrieben.")


if __name__ == "__main__":
    main()
