#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FIKTIVER REFERENZFALL des Zapfprofilgenerators (Stufe Z1, Bilanz deterministisch).

ALLE WERTE SIND ERFUNDEN. Eingabe (referenzfall_eingabe.json) und Ergebnis
(referenzfall_stunden.csv, referenzfall_kennzahlen.csv) enthalten keine Normzahl, keinen
Normformvektor und keine Hersteller- oder Produktdaten (Umsetzungskonzept
Zapfprofilgenerator, Kapitel 6).

ZWECK. Unabhaengige Nachrechnung ueber 8760 Stunden nach den Formeln des Umsetzungskonzepts
(Abschnitte 4.1 Mengengeruest, 4.2 Kalender/Jahresgang/Tagesgang, 4.3 Zirkulation) - OHNE
den C#-Code. Der Test EPOS.Kern.Tests/ZapfprofilReferenzfallTests laedt Eingabe und CSV,
rechnet mit ZapfprofilRechner und verlangt je Stunde Abweichung 0 nach Rundung auf
1e-9 kWh sowie gleiche Monats- und Jahressummen (Kapitel 7, Zeile Z1; Methodikkonzept 3.6 P1).
Dieses Skript steht an Stelle der Tabellenkalkulation, die Kapitel 7 nennt (Nachtrag N7).

ABGEDECKT. Vier Zonen: Kalender mit Feiertagen und Ferienfenstern (auch ueber den
Jahreswechsel), Ruhetag mit und ohne Ferienfaktor, eigener Tagesgangsatz mit leerem Tagtyp,
Temperaturumrechnung, Messwert in m3 (Grenze 1) und in kWh mit Grenze 2 (Kalibrierung samt
Zirkulationsanteil der Zone), eine Zone mit Katalog-Grenze 2 (nicht in Z1) und eine Zone mit
Zirkulation "nein"; Zirkulation nach der Methode Flaechenkennwert mit gebaeudeweiter Flaeche
(mengengewichtetes alpha). Das Laufzeitfenster (Schwerpunkt der Zapfung in Z1, Beginn
floor(m - t/2 + 1/2), an den Tagesrand geschoben) ist eine Festlegung der Umsetzung zu 4.3
(N7); das Skript rechnet sie nach, prueft sie aber nicht unabhaengig vom Papier.

WIEDERHOLBAR. Das Skript liest nur die Eingabe und schreibt die zwei CSV-Dateien neben sich;
ein zweiter Lauf erzeugt dieselben Bytes.

Aufruf (Windows: py, sonst python3):
    py EPOS.Kern.Tests/Proben/Zapfprofil/referenzfall_bauen.py
"""

import json
import math
import os
import sys

ORDNER = os.path.dirname(os.path.abspath(__file__))
EINGABE = os.path.join(ORDNER, "referenzfall_eingabe.json")
STUNDEN_CSV = os.path.join(ORDNER, "referenzfall_stunden.csv")
KENNZAHLEN_CSV = os.path.join(ORDNER, "referenzfall_kennzahlen.csv")

TAGE = 365
MONATSLAENGEN = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
C_W = 1.163  # Wh/(l*K), physikalische Konstante (Konzept 4.0)

WERKTAG, SAMSTAG, SONNFEIERTAG, RUHETAG = 1, 2, 3, 4


def monat_von(tag):
    """Monat 1..12 des Jahrestags 1..365 (Jahr ohne Schaltjahr)."""
    grenze = 0
    for m, laenge in enumerate(MONATSLAENGEN, start=1):
        grenze += laenge
        if tag <= grenze:
            return m
    raise ValueError(tag)


def ferientage(paare):
    """Konzept 4.2: 0 und 366 = keine Angabe; >365 -> 365; Beginn leer -> 1..Ende;
    Beginn > Ende -> Beginn..365 und 1..Ende; Ende leer -> kein Fenster."""
    tage = set()

    def angabe(x):
        if x is None or x <= 0 or x == 366:
            return None
        return min(x, 365)

    for beginn, ende in paare:
        b, e = angabe(beginn), angabe(ende)
        if e is None:
            continue
        if b is None:
            tage.update(range(1, e + 1))
        elif b <= e:
            tage.update(range(b, e + 1))
        else:
            tage.update(range(b, 366))
            tage.update(range(1, e + 1))
    return tage


def gang(stunden):
    """24 Anteile aus einer Zuordnung Stunde -> Anteil."""
    return [float(stunden.get(str(h), 0.0)) for h in range(24)]


def main():
    with open(EINGABE, encoding="utf-8") as f:
        ein = json.load(f)

    par = ein["parameter"]
    jan1 = ein["wochentag_jan1"]
    feiertage = set(ein["feiertage"])

    # Kennzeichen der Klimaregion: Wochenende und Feiertage.
    we = []
    for d in range(1, TAGE + 1):
        wt = (jan1 + d - 1) % 7
        we.append(wt in (5, 6) or d in feiertage)

    saetze = {}
    for s in ein["tagesgangsaetze"]:
        saetze[s["id"]] = {WERKTAG: gang(s["werktag"]), SAMSTAG: gang(s["samstag"]),
                           SONNFEIERTAG: gang(s["sonntag"]), RUHETAG: gang(s["ruhetag"])}
    arten = {n["id"]: n for n in ein["nutzungsarten"]}

    zonen = []
    for z in ein["zonen"]:
        n = arten[z["nutzungsart"]]
        bezug_zapf, bezug_kalt = n["bezug_zapftemperatur_c"], n["bezug_kaltwasser_c"]
        t_zapf = z["zapftemperatur_c"] if z["zapftemperatur_c"] is not None else bezug_zapf
        t_kw = z["kaltwasser_mittel_c"] if z["kaltwasser_mittel_c"] is not None else par["Kaltwasser.Bilanz.Mittel"]
        amp = z["kaltwasser_amplitude_k"] if z["kaltwasser_amplitude_k"] is not None else par["Kaltwasser.Bilanz.Amplitude"]
        m_max = par["Kaltwasser.Bilanz.MonatMaximum"]

        # 4.1: Q_a = n * q_spez(Niveau) * 365 * f_theta
        f_theta = (t_zapf - t_kw) / (bezug_zapf - bezug_kalt)
        q_spez = n["bedarf_kwh_je_einheit_tag"][z["niveau"] - 1]
        q_katalog = z["bezugsmenge"] * q_spez * TAGE * f_theta

        # 4.1: Messwert in m3 -> kWh an der Zapfstelle (Grenze 1): Q = V * c_w * (theta_Zapf - theta_KW);
        # Messwert in kWh mit ausdruecklicher Grenze. Kalibriert wird erst nach dem Zirkulationsanteil.
        q_mess, grenze_mess = None, None
        if z.get("messwert_m3") is not None:
            q_mess = z["messwert_m3"] * C_W * (t_zapf - t_kw)
            grenze_mess = 1
        elif z.get("messwert_kwh") is not None:
            q_mess = z["messwert_kwh"]
            grenze_mess = z["messwert_grenze"]

        # 4.2: Kaltwasser-Jahresgang, Monatswerte auf neun Stellen gerundet
        theta_m = [round(t_kw + amp * math.cos(2.0 * math.pi * (m - m_max) / 12.0), 9) for m in range(1, 13)]
        f_kw = [(t_zapf - theta_m[m]) / (t_zapf - t_kw) for m in range(12)]

        # Wochenfaktoren und Tagesgaenge normiert
        w_roh = n["woche"]
        w = [x / sum(w_roh) for x in w_roh]
        w_mittel = sum(w) / 7.0
        satz = saetze[z["tagesgangsatz"] if z["tagesgangsatz"] is not None else n["tagesgangsatz"]]
        phi = {}
        for typ, g in satz.items():
            s = sum(g)
            phi[typ] = [x / s for x in g] if s > 0 else None   # None: Tagtyp ohne Zapfung

        ferien = ferientage(z["ferien"])

        # 4.2: Tagtyp und Gewicht je Tag
        typen, gewichte = [], []
        for d in range(1, TAGE + 1):
            wt = (jan1 + d - 1) % 7
            if d in ferien:
                typ = RUHETAG
            elif we[d - 1] and wt == 5:
                typ = SAMSTAG
            elif we[d - 1]:
                typ = SONNFEIERTAG
            else:
                typ = WERKTAG
            if typ == WERKTAG:
                w_t = w[wt]
            elif typ == SAMSTAG:
                w_t = w[5]
            elif typ == SONNFEIERTAG:
                w_t = w[6]
            else:
                w_t = n["ferienfaktor"] * w_mittel if n["ferienfaktor"] is not None else w[6]
            m = monat_von(d) - 1
            g = 0.0 if phi[typ] is None else n["monate"][m] * f_kw[m] * 7.0 * w_t
            typen.append(typ)
            gewichte.append(g)

        zonen.append({
            "name": z["name"], "q_katalog": q_katalog, "typen": typen, "gewichte": gewichte, "phi": phi,
            "z1": n["bilanzgrenze"] == 1 and z["zirkulation"], "q_mess": q_mess, "grenze_mess": grenze_mess,
            "t_kw": t_kw, "bezugsmenge": z["bezugsmenge"],
        })

    # 4.3: Zirkulation, Methode Flaechenkennwert mit gebaeudeweiter Flaeche A_N (Zirk_Flaeche_m2):
    # alpha mengengewichtet (keine Zone traegt eine Flaeche), Anteil je Zone in Z1 nach Q_a vor Kalibrierung.
    summe_alle = sum(z["q_katalog"] for z in zonen)
    summe_z1 = sum(z["q_katalog"] for z in zonen if z["z1"])
    alpha = summe_z1 / summe_alle
    t_lauf = par["Zirkulation.Laufzeit"]
    k_a = par["Zirkulation.Kennwert.Lage1"] if par["Zirkulation.Lage"] == 1 else par["Zirkulation.Kennwert.Lage2"]
    a_n = ein["projekt"]["zirk_flaeche_m2"]
    p_zirk = alpha * k_a * a_n / (TAGE * t_lauf)
    q_zirk = p_zirk * t_lauf * TAGE
    for z in zonen:
        z["anteil"] = q_zirk * z["q_katalog"] / summe_z1 if z["z1"] else 0.0

    # 4.1: Kalibrierung nach dem Zirkulationsanteil. Grenze 1: Q_a = Q_Mess; Grenze 2: Zapfung und
    # Zirkulationsanteil der Zone gemeinsam skaliert, zusammen = Q_Mess.
    for z in zonen:
        z["q_a"], z["kalibrierfaktor"] = z["q_katalog"], None
        if z["q_mess"] is None:
            continue
        if z["grenze_mess"] == 1:
            z["kalibrierfaktor"] = z["q_mess"] / z["q_katalog"]
            z["q_a"] = z["q_mess"]
        elif z["grenze_mess"] == 2:
            f_kal = z["q_mess"] / (z["q_katalog"] + z["anteil"])
            z["kalibrierfaktor"] = f_kal
            z["q_a"] = f_kal * z["q_katalog"]
            z["anteil"] = f_kal * z["anteil"]
        else:
            raise ValueError("Grenze 3 ist im Referenzfall nicht vorgesehen")

    # 4.2: Tagesmengen und Stundenwerte aus der kalibrierten Jahresmenge
    for z in zonen:
        summe_g = sum(z["gewichte"])
        tagesmengen = [z["q_a"] * g / summe_g for g in z["gewichte"]]
        stunden = []
        for d in range(TAGE):
            p = z["phi"][z["typen"][d]]
            for h in range(24):
                stunden.append(0.0 if p is None else tagesmengen[d] * p[h])
        z["stunden"] = stunden

    # Laufzeitfenster um die Tagesmitte der Zapfung der Zonen in Z1
    e_h = [0.0] * 24
    for z in zonen:
        if z["z1"]:
            for h in range(24):
                for d in range(TAGE):
                    e_h[h] += z["stunden"][d * 24 + h]
    mitte = sum((h + 0.5) * e_h[h] for h in range(24)) / sum(e_h)
    beginn = math.floor(mitte - t_lauf / 2.0 + 0.5)
    beginn = max(beginn, 0)
    if beginn + t_lauf > 24:
        beginn = math.floor(24 - t_lauf)
    ende = beginn + t_lauf
    fenster = [max(0.0, min(h + 1, ende) - max(h, beginn)) for h in range(24)]

    zirk_stunden = [0.0] * (TAGE * 24)
    for z in zonen:
        z["zirk"] = [0.0] * (TAGE * 24)
        if not z["z1"]:
            continue
        leistung = z["anteil"] / (TAGE * t_lauf)
        for d in range(TAGE):
            for h in range(24):
                z["zirk"][d * 24 + h] = leistung * fenster[h]
    zapf_stunden = [sum(z["stunden"][i] for z in zonen) for i in range(TAGE * 24)]
    zirk_stunden = [sum(z["zirk"][i] for z in zonen) for i in range(TAGE * 24)]

    with open(STUNDEN_CSV, "w", encoding="utf-8", newline="\n") as f:
        f.write("# FIKTIV - Referenzfall Zapfprofil Z1, erzeugt von referenzfall_bauen.py; kWh je Stunde\n")
        f.write("stunde,zapfung_kwh,zirkulation_kwh\n")
        for i in range(TAGE * 24):
            f.write(f"{i + 1},{zapf_stunden[i]:.9f},{zirk_stunden[i]:.9f}\n")

    def monatssummen(reihe):
        summen, h = [], 0
        for laenge in MONATSLAENGEN:
            summen.append(sum(reihe[h:h + laenge * 24]))
            h += laenge * 24
        return summen

    gesamt = [zapf_stunden[i] + zirk_stunden[i] for i in range(TAGE * 24)]
    groesster = max(gesamt)
    anzeige = ein["anzeigetemperatur_c"]
    liter = sum(z["q_a"] / TAGE * 1000.0 / (C_W * (anzeige - z["t_kw"])) for z in zonen)

    zeilen = [
        ("jahr_zapfung_kwh", sum(zapf_stunden)),
        ("jahr_zirkulation_kwh", sum(zirk_stunden)),
    ]
    zeilen += [(f"monat_{m + 1:02d}_zapfung_kwh", x) for m, x in enumerate(monatssummen(zapf_stunden))]
    zeilen += [(f"monat_{m + 1:02d}_zirkulation_kwh", x) for m, x in enumerate(monatssummen(zirk_stunden))]
    for i, z in enumerate(zonen, start=1):
        zeilen.append((f"zone_{i}_zapfung_kwh", sum(z["stunden"])))
        zeilen.append((f"zone_{i}_zirkulation_kwh", sum(z["zirk"])))
    for i, z in enumerate(zonen, start=1):
        if z["kalibrierfaktor"] is not None:
            zeilen.append((f"zone_{i}_kalibrierfaktor", z["kalibrierfaktor"]))
    zeilen += [
        # Jahresverlust des Ansatzes VOR der Kalibrierung (jahr_zirkulation_kwh ist der verbuchte)
        ("zirkulation_gewicht", alpha),
        ("zirkulation_leistung_kw", p_zirk),
        ("zirkulation_jahresverlust_kwh", q_zirk),
        ("tagesmitte_h", mitte),
        ("laufzeit_beginn_h", float(beginn)),
        ("groesster_stundenwert_kw", groesster),
        ("volllaststunden_h", sum(gesamt) / groesster),
        ("zirkulationsanteil", sum(zirk_stunden) / sum(gesamt)),
        ("zapfung_liter_je_tag", liter),
        ("stunden_ueber_schwelle", float(sum(1 for x in gesamt if x > ein["schwelle_kw"]))),
    ]
    with open(KENNZAHLEN_CSV, "w", encoding="utf-8", newline="\n") as f:
        f.write("# FIKTIV - Kennzahlen des Referenzfalls Zapfprofil Z1, erzeugt von referenzfall_bauen.py\n")
        f.write("groesse,wert\n")
        for name, wert in zeilen:
            f.write(f"{name},{wert:.9f}\n")

    print(f"Referenzfall geschrieben: 8760 Stunden, Zapfung {sum(zapf_stunden):.3f} kWh, "
          f"Zirkulation {sum(zirk_stunden):.3f} kWh, Tagesmitte {mitte:.4f} h, Beginn {beginn} h")
    return 0


if __name__ == "__main__":
    sys.exit(main())
