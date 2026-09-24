#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FIKTIVER REFERENZFALL des Jahresensembles des Zapfprofilgenerators (Stufe Z3, Rechenweg der
Jahresreihe "stochastisch").

ALLE WERTE SIND ERFUNDEN. Eingabe (jahresensemble_referenzfall_eingabe.json) und Ergebnis
(jahresensemble_referenzfall_ergebnis.csv) enthalten keine Normzahl, keine Jordan/Vajen-Zahl, kein
Normprofil und keine Hersteller- oder Produktdaten (Umsetzungskonzept Zapfprofilgenerator,
Kapitel 6).

ZWECK. Unabhaengige Nachrechnung des Jahresensembles nach dem Umsetzungskonzept 2.3 Satz 3, 4.2
und 4.4 - OHNE den C#-Code, rein in Python:
  - Kalender der Klimaregion: Wochentag aus dem Wochentag des 1. Januar, Wochenende und Feiertage,
    Ferienfenster -> Ruhetag; Samstag, Sonn-/Feiertag, Werktag;
  - Entkopplung der Urlaube: je Einheit ein Versatz v gleichverteilt in [-V; V] als erste Ziehung
    ihres Zufallsstroms, Ferienfenster um v Tage verschoben, ueber den Jahreswechsel umlaufend
    (ein Fenster ueber den Jahreswechsel wird geteilt);
  - Tagesmengen je Einheit Q_d = (Q_a / n_E) * g(d) / Summe g, g(d) = f_Monat * f_KW * 7 * w_T(d),
    w_T nach Tagtyp (Ruhetag: Ferienfaktor * Wochenmittel);
  - je Tag die Ereignisse nach dem Generator (Zufall aus zufall_referenz_bauen.py, Ziehung aus
    ensemble_referenzfall_bauen.py, beide ohne C#) mit der Spreizung des Monats (Monatsspreizung);
    Energie gleichmaessig ueber die Minuten, ueber Mitternacht in den naechsten Tag, am Jahresende
    an den Jahresanfang (Jahreswechsel); Stunde = Summe ihrer Minuten;
  - Seeds: je Realisierung SplitMix64(SplitMix64(Seed) xor r), je Zone SplitMix64(. xor Index), je
    Einheit SplitMix64(. xor Einheit);
  - Konsistenzprobe aus den R Jahren: Mittel der Stundenwerte, mittlere Jahresenergie, s_R,
    Toleranz max(1 % * E_det, 3 * s_R / sqrt(R)), erfuellt, groesste Abweichung der Anteile je
    Tagesstunde gegen den deterministischen Pfad (Formvektor);
  - BILANZ: die eine Realisierung zum Seed (r = 0) mal dem Faktor der Energieprobe E_det / E_0 -
    nie das Mittel der R Jahre.
Jede Summe laeuft in fester Folge als Schleife (Python 3.12 summiert sum() kompensiert).

Der Test EPOS.Kern.Tests/ZapfJahresensembleReferenzfallTests liest Eingabe und CSV, rechnet mit
Jahresensemble und verlangt Abweichung 0 auf 1e-9 (|C# - Referenz| <= 0,5e-9 + 1e-12 * |Referenz|;
die Werte stehen mit zwoelf Nachkommastellen in der CSV, gerechnet wird in derselben Folge).

DER FALL DECKT (das Skript bricht sonst ab): Urlaubsversatz mit einem Fenster, das ueber den
Jahreswechsel verschoben und geteilt wird; ein Ereignis der Realisierung zum Seed, das vom
31. Dezember in den 1. Januar laeuft; zwoelf verschiedene Spreizungen; einen Energiefaktor ungleich 1;
einen Feiertag an einem Werktag; mehrere Realisierungen und Einheiten, Zone mit Index ungleich 0.

WIEDERHOLBAR. Das Skript liest nur die Eingabe und schreibt die CSV neben sich; ein zweiter Lauf
erzeugt dieselben Bytes.

Aufruf (Windows: py, sonst python3):
    py EPOS.Kern.Tests/Proben/Zapfprofil/jahresensemble_referenzfall_bauen.py
"""

import json
import math
import os
import sys

ORDNER = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ORDNER)
sys.dont_write_bytecode = True   # kein __pycache__ im Probenordner

from zufall_referenz_bauen import Zufall, realisierungsseed, kindseed  # noqa: E402
from ensemble_referenzfall_bauen import Dichte, ziehen, kategoriensatz  # noqa: E402

EINGABE = os.path.join(ORDNER, "jahresensemble_referenzfall_eingabe.json")
ERGEBNIS = os.path.join(ORDNER, "jahresensemble_referenzfall_ergebnis.csv")

TAGE = 365
MINUTEN = 1440
MINUTEN_JAHR = TAGE * MINUTEN
STUNDEN = TAGE * 24
MONATSLAENGEN = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
WERKTAG, SAMSTAG, SONNFEIERTAG, RUHETAG = 1, 2, 3, 4
SAMSTAG_WT, SONNTAG_WT = 5, 6


# ----------------------------------------------------------------------------------------
# Kalender und Formvektor (4.2)
# ----------------------------------------------------------------------------------------

def monat_von(tag):
    """Monat 0..11 des Jahrestags 1..365 (Jahr ohne Schaltjahr)."""
    rest = tag
    for m, laenge in enumerate(MONATSLAENGEN):
        if rest <= laenge:
            return m
        rest -= laenge
    return 11


def in_ferien(ferien, tag):
    for beginn, ende in ferien:
        if beginn <= tag <= ende:
            return True
    return False


def kalender_bilden(jan1, we, ferien):
    typen = []
    for d in range(1, TAGE + 1):
        wt = (jan1 + d - 1) % 7
        if in_ferien(ferien, d):
            typen.append(RUHETAG)
        elif we[d - 1] and wt == SAMSTAG_WT:
            typen.append(SAMSTAG)
        elif we[d - 1]:
            typen.append(SONNFEIERTAG)
        else:
            typen.append(WERKTAG)
    return typen


def versetzt(ferien, versatz):
    """Ferienfenster um versatz Tage verschoben, ueber den Jahreswechsel umlaufend."""
    neu = []
    for beginn, ende in ferien:
        laenge = ende - beginn + 1
        if laenge >= TAGE:
            neu.append((1, TAGE))
            continue
        b = ((beginn - 1 + versatz) % TAGE + TAGE) % TAGE + 1
        e = b + laenge - 1
        if e <= TAGE:
            neu.append((b, e))
        else:
            neu.append((b, TAGE))
            neu.append((1, e - TAGE))
    return neu


def tagesgewicht(s, typ, wt):
    w = s["woche"]
    if typ == WERKTAG:
        return w[wt]
    if typ == SAMSTAG:
        return w[SAMSTAG_WT]
    if typ == SONNFEIERTAG:
        return w[SONNTAG_WT]
    if s["ferienfaktor"] is None:
        return w[SONNTAG_WT]
    summe = 0.0
    for i in range(7):
        summe += w[i]
    return s["ferienfaktor"] * (summe / 7)


def tagesmengen(q_a, s, kalender, jan1, f_kw):
    """Q_d = Q_a * g(d) / Summe g, g(d) = f_Monat * f_KW * 7 * w_T(d); leerer Tagtyp -> 0."""
    g = []
    summe = 0.0
    for d in range(1, TAGE + 1):
        typ = kalender[d - 1]
        gewicht = 0.0
        if not s["leer"][typ - 1]:
            m = monat_von(d)
            wt = tagesgewicht(s, typ, (jan1 + d - 1) % 7)
            gewicht = s["monate"][m] * f_kw[m] * 7 * wt
        g.append(gewicht)
        summe += gewicht
    return [q_a * x / summe for x in g]


def stundenreihe(tage, s, kalender):
    reihe = []
    for d in range(TAGE):
        gang = s["gaenge"][kalender[d] - 1]
        for h in range(24):
            reihe.append(tage[d] * gang[h])
    return reihe


# ----------------------------------------------------------------------------------------
# Summen in fester Folge (wie die Bilanzreihe)
# ----------------------------------------------------------------------------------------

def jahr_und_monate(reihe):
    jahr = 0.0
    monate = []
    h = 0
    for laenge in MONATSLAENGEN:
        monat = 0.0
        for _ in range(laenge * 24):
            w = reihe[h]
            monat += w
            jahr += w
            h += 1
        monate.append(monat)
    return jahr, monate


def tagesgangabweichung(a, b):
    sa, _ = jahr_und_monate(a)
    sb, _ = jahr_und_monate(b)
    if not sa > 0 or not sb > 0:
        return 0.0
    groesste = 0.0
    for h in range(24):
        ah = 0.0
        bh = 0.0
        for d in range(TAGE):
            ah += a[d * 24 + h]
            bh += b[d * 24 + h]
        diff = abs(ah / sa - bh / sb)
        if diff > groesste:
            groesste = diff
    return groesste


# ----------------------------------------------------------------------------------------
# Das Jahresensemble
# ----------------------------------------------------------------------------------------

def struktur(e):
    namen = ("werktag", "samstag", "sonntag", "ruhetag")
    gaenge = [list(map(float, e["gaenge"][n])) for n in namen]
    leer = []
    for g in gaenge:
        s = 0.0
        for x in g:
            s += x
        leer.append(s == 0.0)
    return {"monate": e["monate"], "woche": e["woche"], "ferienfaktor": e["ferienfaktor"],
            "gaenge": gaenge, "leer": leer}


def realisierung(e, s, satz, dichten, seed, r, deckung):
    z = e["zone"]
    stunden = [0.0] * STUNDEN
    zs = kindseed(realisierungsseed(seed, r), z["index"])
    je_einheit = z["jahresmenge_kwh"] / z["einheiten"]
    v_max = e["urlaubsversatz_tage"]
    versaetze = []
    for u in range(z["einheiten"]):
        zufall = Zufall(kindseed(zs, u))
        # Entkopplung der Urlaube: erste Ziehung des Stroms der Einheit.
        v = zufall.ganzzahl(2 * v_max + 1) - v_max
        versaetze.append(v)
        fenster = versetzt(e["ferien"], v)
        if any(b == 1 for b, _ in fenster) and any(en == TAGE for _, en in fenster) and len(fenster) > len(e["ferien"]):
            deckung["fenster_geteilt"] = True
        kalender = kalender_bilden(e["wochentag_jan1"], e["we"], fenster)
        tage = tagesmengen(je_einheit, s, kalender, e["wochentag_jan1"], e["kaltwasserfaktor"])
        for d in range(TAGE):
            ev = ziehen(zufall, satz, tage[d], e["spreizung_k"][monat_von(d + 1)], dichten[kalender[d] - 1])
            beginn = d * MINUTEN
            for (t0, dauer, energie) in ev:
                je_minute = energie / dauer
                for k in range(dauer):
                    m = (beginn + t0 + k) % MINUTEN_JAHR
                    stunden[m // 60] += je_minute
                    if r == 0 and d == TAGE - 1 and beginn + t0 + k >= MINUTEN_JAHR:
                        deckung["jahreswechsel_kwh"] += je_minute
    return stunden, versaetze


def rechnen(e):
    z = e["zone"]
    seed, r_zahl = e["seed"], e["realisierungen"]
    jan1 = e["wochentag_jan1"]
    feiertage = set(e["feiertage"])
    e["we"] = [((jan1 + d - 1) % 7) in (SAMSTAG_WT, SONNTAG_WT) or d in feiertage for d in range(1, TAGE + 1)]
    e["ferien"] = [tuple(f) for f in e["ferien"]]
    s = struktur(e)
    satz = kategoriensatz(e["kategorien"], z["id_art"])
    dichten = [Dichte(g) for g in s["gaenge"]]
    kalender_zone = kalender_bilden(jan1, e["we"], e["ferien"])
    deckung = {"jahreswechsel_kwh": 0.0, "fenster_geteilt": False}

    werte = {}
    energien = []
    mittel = [0.0] * STUNDEN
    jahr0 = None
    for r in range(r_zahl):
        stunden, versaetze = realisierung(e, s, satz, dichten, seed, r, deckung)
        if r == 0:
            jahr0 = stunden
        jahr, _ = jahr_und_monate(stunden)
        energien.append(jahr)
        werte[f"r{r}_jahr_kwh"] = jahr
        for u, v in enumerate(versaetze):
            werte[f"r{r}_einheit{u}_versatz"] = float(v)
        for h in range(STUNDEN):
            mittel[h] += stunden[h]
    for h in range(STUNDEN):
        mittel[h] /= r_zahl
    mittel_jahr, mittel_monate = jahr_und_monate(mittel)
    werte["mittel_jahr_kwh"] = mittel_jahr
    for m, x in enumerate(mittel_monate):
        werte[f"mittel_monat_{m + 1:02d}_kwh"] = x

    # Konsistenzprobe aus den R Jahren gegen den deterministischen Pfad.
    det_reihe = stundenreihe(tagesmengen(z["jahresmenge_kwh"], s, kalender_zone, jan1, e["kaltwasserfaktor"]),
                             s, kalender_zone)
    det, _ = jahr_und_monate(det_reihe)
    summe = 0.0
    for x in energien:
        summe += x
    m_e = summe / r_zahl
    q = 0.0
    for x in energien:
        q += (x - m_e) * (x - m_e)
    s_r = math.sqrt(q / (r_zahl - 1)) if r_zahl > 1 else 0.0
    toleranz = max(0.01 * abs(det), 3.0 * s_r / math.sqrt(r_zahl))
    e0, _ = jahr_und_monate(jahr0)
    faktor = det / e0
    werte["deterministisch_kwh"] = det
    werte["mittel_kwh"] = m_e
    werte["standardabweichung_kwh"] = s_r
    werte["toleranz_kwh"] = toleranz
    werte["erfuellt"] = 1.0 if abs(mittel_jahr - det) <= toleranz else 0.0
    werte["jahr_zum_seed_kwh"] = e0
    werte["faktor"] = faktor
    werte["tagesgangabweichung"] = tagesgangabweichung(mittel, det_reihe)

    # Bilanz: die Realisierung zum Seed mal dem Faktor der Energieprobe - nie das Mittel.
    bilanz = [x * faktor for x in jahr0]
    b_jahr, b_monate = jahr_und_monate(bilanz)
    werte["bilanz_jahr_kwh"] = b_jahr
    for m, x in enumerate(b_monate):
        werte[f"bilanz_monat_{m + 1:02d}_kwh"] = x
    for h in range(STUNDEN):
        werte[f"bilanz_stunde_{h:04d}"] = bilanz[h]

    # Deckung des Falls: sonst ist der Referenzfall wertlos.
    fehlt = []
    if not deckung["jahreswechsel_kwh"] > 0:
        fehlt.append("kein Ereignis der Realisierung zum Seed laeuft vom 31. Dezember in den 1. Januar")
    if not deckung["fenster_geteilt"]:
        fehlt.append("kein versetztes Ferienfenster wird am Jahreswechsel geteilt")
    if len(set(e["spreizung_k"])) != 12:
        fehlt.append("die Spreizung ist nicht in jedem Monat eine andere")
    if faktor == 1.0 or r_zahl < 2 or z["einheiten"] < 2 or z["index"] == 0:
        fehlt.append("Energiefaktor 1, zu wenige Realisierungen oder Einheiten oder Zone mit Index 0")
    if not any(((jan1 + d - 1) % 7) < SAMSTAG_WT for d in feiertage):
        fehlt.append("kein Feiertag an einem Werktag")
    if fehlt:
        raise SystemExit("Der Referenzfall deckt nicht, was er soll: " + "; ".join(fehlt))
    return werte, deckung


def main():
    with open(EINGABE, encoding="utf-8") as f:
        e = json.load(f)
    werte, deckung = rechnen(e)
    with open(ERGEBNIS, "w", encoding="utf-8", newline="\r\n") as f:
        f.write("# Referenzfall des Jahresensembles (Stufe Z3) - erzeugt von jahresensemble_referenzfall_bauen.py\n")
        f.write("groesse,wert\n")
        for name, wert in werte.items():
            f.write(f"{name},{wert:.12f}\n")
    print(f"{len(werte)} Groessen nach {ERGEBNIS} geschrieben; ueber den Jahreswechsel "
          f"{deckung['jahreswechsel_kwh']:.6f} kWh (Realisierung zum Seed).")


if __name__ == "__main__":
    main()
