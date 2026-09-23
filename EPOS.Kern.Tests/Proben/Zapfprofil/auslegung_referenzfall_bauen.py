#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FIKTIVER REFERENZFALL der Auslegung des Zapfprofilgenerators (Stufe Z2).

ALLE WERTE SIND ERFUNDEN. Eingabe (auslegung_referenzfall_eingabe.json) und Ergebnis
(auslegung_referenzfall_ergebnis.csv) enthalten keine Normzahl, kein Normprofil und keine
Hersteller- oder Produktdaten (Umsetzungskonzept Zapfprofilgenerator, Kapitel 6).

ZWECK. Unabhaengige Nachrechnung nach den Formeln des Umsetzungskonzepts - OHNE den C#-Code:
  4.5 a  Summenlinie nach DIN EN 12831-3 mit den Rechenregeln der Entwuerfe A100/A1:
         Minutenbilanz, Phi_N = min(Erzeuger, Uebertrager), Uebertrager aus U*A oder der
         Schaetzformel (Waechter: keine negative Flaeche), Einschaltpunkt, Verzoegerung,
         Speicherverlust und Zirkulation als Minutenlast, Ladespeicher und gemischter Speicher;
         kleinstes Volumen ueber Monotonieraster und Bisektion bzw. feinen Rasterlauf;
         Wertepaarkurve; Zeitkonstante (informativ).
  4.5 c  DIN-4708-Kennzahl N, W_z mit Fehlerfunktion (math.erf) bis zur Kappung, V_DIN.
  4.7    Speicherauslegung nach Vorlage V4: Lindley-Bilanz ueber zwei Wochen mit Ladefenster
         und Zirkulation, D_max in Woche 2, V_profil, Gleichzeitigkeitsfaktor, V_GLF, klassischer
         Faustwert, Band, Nenninhalt, Fuellstand und Reserve, Ladeleistung als Schaetzwert.
  N10    Fassadenfall: zwei Zonen am Durchfluss - Mengengeruest, Kalender mit Feiertagen und
         Ferien, Formvektor, f_KW,A und Wochenreihe der Auslegung, Zirkulation (Anteil) mit
         Laufzeitfenster um die Tagesmitte der Bilanz, Vorgaberegel mit gewaehltem Katalogtag,
         Skalierung auf die Bezugsmenge und Umrechnung auf theta_KW,A; Minutenspitze.
Rasterzahlen (20, 400), Bisektionsschritte (60) und die Verdopplung des Startvolumens sind
numerische Setzungen des Verfahrens; sie stehen hier wie im Papier (4.5 a) beschrieben.

Der Test EPOS.Kern.Tests/AuslegungReferenzfallTests liest Eingabe und CSV, rechnet mit
Summenlinie und TwwSpeicherauslegung und verlangt Abweichung 0 auf 1e-9
(|C# - Referenz| <= 0,5e-9 + 1e-12 * |Referenz|).

WIEDERHOLBAR. Das Skript liest nur die Eingabe und schreibt die CSV neben sich; ein zweiter
Lauf erzeugt dieselben Bytes.

Aufruf (Windows: py, sonst python3):
    py EPOS.Kern.Tests/Proben/Zapfprofil/auslegung_referenzfall_bauen.py
"""

import json
import math
import os

ORDNER = os.path.dirname(os.path.abspath(__file__))
EINGABE = os.path.join(ORDNER, "auslegung_referenzfall_eingabe.json")
ERGEBNIS = os.path.join(ORDNER, "auslegung_referenzfall_ergebnis.csv")

C_W = 1.163          # Wh/(l*K), physikalische Konstante (Konzept 4.0)
KJ_JE_WH = 3.6       # Einheitenumrechnung 1 Wh = 3,6 kJ (Zeitkonstante nach A1, c_w in kJ/(kg*K))
MINUTEN = 1440
RASTER_GROB = 20
RASTER_FEIN = 400
BISEKTION = 60
VERDOPPLUNGEN = 60


# ----------------------------------------------------------------------------------------
# Zeitfenster des Tages (Belegung als Ueberlappung, ueber Mitternacht fortgesetzt)
# ----------------------------------------------------------------------------------------

def stueck(a, b, s, laenge):
    von = a if a > s else s
    ende = s + laenge
    bis = b if b < ende else ende
    return bis - von if bis > von else 0.0


def ueberlappung(a, b, beginn, laenge, periode):
    return stueck(a, b, beginn, laenge) + stueck(a, b, beginn - periode, laenge)


def anteil_minute(i, beginn_h, laenge_h):
    return ueberlappung(float(i), float(i + 1), beginn_h * 60, laenge_h * 60, 1440.0)


def anteil_stunde(h, beginn_h, laenge_h):
    return ueberlappung(float(h), float(h + 1), beginn_h, laenge_h, 24.0)


# ----------------------------------------------------------------------------------------
# Summenlinie (4.5 a)
# ----------------------------------------------------------------------------------------

def minutenwerte(ereignisse):
    q = [0.0] * MINUTEN
    for beginn, dauer, energie in ereignisse:
        je = energie / dauer
        for k in range(dauer):
            q[(beginn + k) % MINUTEN] += je
    return q


def ue_ua(u, v):
    """U*A [W/K] beim Volumen v; (None, False) ohne Uebertrager oder bei reiner Leistungsangabe."""
    if u is None or u["leistung_kw"] is not None:
        return None, False
    if u["ua_w_k"] is not None:
        return u["ua_w_k"], False
    unplausibel = False
    if u["flaeche_m2"] is not None:
        a = u["flaeche_m2"]
    else:
        a = u["steigung"] * v + u["achsabschnitt"]
        if not (a > 0):
            unplausibel = True
            a = 0.0
    return u["u"] * a, unplausibel


def leistung(p, v):
    """Phi_N [kW] = min(Erzeuger, Uebertrager) ueber die bekannten."""
    u = p["uebertrager"]
    ue = None
    if u is not None:
        if u["leistung_kw"] is not None:
            ue = u["leistung_kw"]
        else:
            ua, _ = ue_ua(u, v)
            ue = ua * u["uebertemperatur_k"] / 1000.0
    e = p["erzeuger_kw"]
    if e is not None and ue is not None:
        return min(e, ue)
    return e if e is not None else ue


def nachweis(q, v, phi, p, verlauf=False):
    dt = p["speicher_c"] - p["kaltwasser_auslegung_c"]
    fl = p["ladungsfaktor"]
    s = p["sensorhoehe"]
    qmax = v * C_W * dt * fl / 1000.0
    qon = qmax * (1.0 - s)
    if p["speicherart"] == 2:
        qmin = v * C_W * (1.0 - s / 2.0) * (p["mischwasser_c"] - p["kaltwasser_auslegung_c"]) * fl / 1000.0
    else:
        qmin = 0.0
    inhalt = qmax
    ein = False
    ton = 0
    lauf = 0
    erste = -1
    kleinster = math.inf
    werte = []
    for i in range(MINUTEN):
        if verlauf:
            werte.append(inhalt)
        if not ein and inhalt <= qon:
            ein = True
            ton = i
        elif ein and inhalt >= qmax - (1e-9 + 1e-12 * abs(qmax)):
            ein = False
        phiv = p["speicherverlust_kw"] + p["zirkulation_kw"] * anteil_minute(
            i, p["zirkulation_beginn_h"], p["zirkulation_laufzeit_h"])
        phieff = phi - phiv if (ein and i - ton >= p["verzoegerung_min"]) else -phiv
        abstand = inhalt - q[i] - qmin
        if abstand < kleinster:
            kleinster = abstand
        if abstand < 0 and erste < 0:
            erste = i
        if ein:
            lauf += 1
        neu = inhalt - q[i] + phieff / 60
        inhalt = neu if neu < qmax else qmax
    if verlauf:
        werte.append(inhalt)
    return {"erfuellt": erste < 0, "kleinster": kleinster, "ladezeit": lauf / 60, "verlauf": werte}


def kleinstes_volumen(q, p):
    """Kleinstes Volumen mit Nachweis; Phi_N(V) = min(Erzeuger, Uebertrager(V)) - auch fuer die
    Wertepaarkurve (dort mit der Erzeugerleistung auf dem Raster)."""
    def gelingt(v):
        phi = leistung(p, v)
        n = nachweis(q, v, phi, p)
        return n["erfuellt"], phi, n["ladezeit"]

    ok, phi, lz = gelingt(0.0)
    if ok:
        return 0.0, phi, lz, 0

    energie = sum_seq(q)
    for i in range(MINUTEN):
        energie += (p["speicherverlust_kw"] + p["zirkulation_kw"] * anteil_minute(
            i, p["zirkulation_beginn_h"], p["zirkulation_laufzeit_h"])) / 60
    dt = p["speicher_c"] - p["kaltwasser_auslegung_c"]
    hoch = energie * 1000.0 / (C_W * dt * p["ladungsfaktor"])
    if not (hoch > 0):
        hoch = 1.0
    verdopplung = 0
    while not gelingt(hoch)[0]:
        verdopplung += 1
        if verdopplung > VERDOPPLUNGEN:
            raise ValueError("kein Volumen")
        hoch *= 2.0

    gel = [False] * (RASTER_GROB + 1)
    erster = -1
    for k in range(1, RASTER_GROB + 1):
        gel[k] = gelingt(hoch * k / RASTER_GROB)[0]
        if gel[k] and erster < 0:
            erster = k
    monoton = erster > 0
    k = erster
    while monoton and k <= RASTER_GROB:
        monoton = gel[k]
        k += 1

    if monoton:
        unten = 0.0 if erster == 1 else hoch * (erster - 1) / RASTER_GROB
        oben = hoch * erster / RASTER_GROB
        for _ in range(BISEKTION):
            mitte = 0.5 * (unten + oben)
            if mitte <= unten or mitte >= oben:
                break
            if gelingt(mitte)[0]:
                oben = mitte
            else:
                unten = mitte
        v = oben
        suche = 1
    else:
        v = hoch
        for k in range(1, RASTER_FEIN + 1):
            kandidat = hoch * k / RASTER_FEIN
            if gelingt(kandidat)[0]:
                v = kandidat
                break
        suche = 2
    _, phi, lz = gelingt(v)
    return v, phi, lz, suche


def sum_seq(werte):
    s = 0.0
    for x in werte:
        s += x
    return s


def summenlinie(fall, name, zeilen):
    q = minutenwerte(fall["ereignisse"])
    v, phi, lz, suche = kleinstes_volumen(q, fall)
    n = nachweis(q, v, phi, fall, True)
    zeilen.append((name + "_volumen_l", v))
    zeilen.append((name + "_leistung_kw", phi))
    zeilen.append((name + "_ladezeit_h", lz))
    zeilen.append((name + "_suche", float(suche)))
    zeilen.append((name + "_kleinster_abstand_kwh", n["kleinster"]))
    u = fall["uebertrager"]
    if fall["zeitkonstante_koeffizient"] is not None and u is not None:
        ua, _ = ue_ua(u, v)
        if ua is not None and ua > 0:
            # A1: tau = m * c_w / (U*A) * k_tau mit c_w in kJ/(kg*K), m = V (1 kg/l); 1 Wh = 3,6 kJ.
            zeilen.append((name + "_zeitkonstante_min", v * C_W * KJ_JE_WH / ua * fall["zeitkonstante_koeffizient"]))
    # Wertepaarkurve: Erzeugerleistung auf dem Raster bis zur Erzeugerleistung des Falls (ohne
    # Erzeuger bis Phi des Auslegungspunkts); Phi_N(V) = min(Phi_E,k, Phi_Ue(V)) - jedes Paar baubar.
    punkte = fall["wertepaare"]
    phi_max = fall["erzeuger_kw"] if fall["erzeuger_kw"] is not None else phi
    for k in range(1, punkte + 1):
        vk, phik, lzk, _ = kleinstes_volumen(q, dict(fall, erzeuger_kw=phi_max * k / punkte))
        zeilen.append((name + "_wertepaar_%d_leistung_kw" % k, phik))
        zeilen.append((name + "_wertepaar_%d_volumen_l" % k, vk))
        zeilen.append((name + "_wertepaar_%d_ladezeit_h" % k, lzk))
    for i, x in enumerate(n["verlauf"]):
        zeilen.append((name + "_inhalt_%04d" % i, x))


# ----------------------------------------------------------------------------------------
# DIN 4708 (4.5 c) und Speicherauslegung nach V4 (4.7)
# ----------------------------------------------------------------------------------------

def wz_kwh(n, d):
    if n == 0.0:
        return 0.0
    w = math.sqrt(n)
    u1 = d["a1"] * d["z"] * (1.0 + w) / w
    u2 = d["a2"] * d["z"] * (1.0 + w) / w

    def k(u):
        return math.erf(u) if u < d["kappung"] else 1.0

    return d["W_b"] * (n * k(u1) + w * k(u2)) / 1000.0


def speicherauslegung(sa, zeilen):
    stunden = [float(sa["stunden"].get(str(h), 0.0)) for h in range(24)]
    woche = []
    for f in sa["tagesfaktoren"]:
        for h in range(24):
            woche.append(stunden[h] * f)
    tage = []
    for k in range(7):
        s = 0.0
        for h in range(24):
            s += woche[k * 24 + h]
        tage.append(s)
    groesster = 0.0
    for s in tage:
        if s > groesster:
            groesster = s

    dt = sa["speicher_c"] - sa["kaltwasser_auslegung_c"]
    fnutz = sa["nutzanteil"]
    zs = sa["zuschlag"]
    zirk = sa["zirkulation_kw"]
    lb, ll = sa["ladefenster_beginn_h"], sa["ladefenster_h"]
    zb, zl = sa["zirkulation_beginn_h"], sa["zirkulation_laufzeit_h"]

    vorschlag = (groesster + zirk * zl) / ll
    lade = vorschlag if (sa["lade_auto"] or sa["lade_manuell_kw"] is None) else sa["lade_manuell_kw"]

    d = []
    vor = 0.0
    for t in range(1, 337):
        h = (t - 1) % 168
        st = h % 24
        c = zirk * anteil_stunde(st, zb, zl)
        l = lade * anteil_stunde(st, lb, ll)
        neu = vor + woche[h] + c - l
        vor = neu if neu > 0.0 else 0.0
        d.append(vor)
    dmax = 0.0
    tmax = -1
    for t in range(169, 337):
        if d[t - 1] > dmax:
            dmax = d[t - 1]
            tmax = t
    vprofil = dmax * 1000 / (C_W * dt) / fnutz * (1.0 + zs) if dmax > 0 else None

    din = sa["din"]
    summe = 0.0
    personen = 0.0
    for anzahl, p, wert in sa["wohnungen"]:
        w = din["w_b"] if wert is None else wert
        summe += anzahl * p * w
    for anzahl, p, _ in sa["wohnungen"]:
        personen += anzahl * p
    n = summe / (din["p_b"] * din["w_b"])
    wz = wz_kwh(n, din)
    vdin = wz * 1000 / (C_W * dt) / fnutz
    wz1 = wz_kwh(1.0, din)
    glf = wz1 / wz_kwh(n, din)
    vglf = personen * wz1 / din["p_b"] * 1000 / (C_W * dt) / fnutz * glf * (1.0 + zs)
    vklass = personen * sa["klassisch_liter"] * sa["klassisch_spreizung"] / dt * (1.0 + zs)

    band = []
    if vprofil is not None:
        band.append(vprofil)
    band.append(vdin)
    if n <= sa["glf_gueltigkeitsgrenze"]:
        band.append(vglf)
    bmin = min(band)
    bmax = max(band)
    nenn = None
    for x in sa["nenninhalte"]:
        if x >= bmax:
            nenn = x
            break
    if nenn is None:
        nenn = math.ceil(bmax / sa["nenninhalt_raster"]) * sa["nenninhalt_raster"]
    c = nenn * fnutz * C_W * dt / 1000
    mini = c
    for t in range(168, 336):
        soc = c - d[t]
        if soc < 0:
            soc = 0.0
        if soc < mini:
            mini = soc

    zeilen.append(("sa_ladeleistung_kw", lade))
    zeilen.append(("sa_lade_vorschlag_kw", vorschlag))
    zeilen.append(("sa_dmax_kwh", dmax))
    zeilen.append(("sa_zeitpunkt_stunde", float(tmax)))
    zeilen.append(("sa_volumen_profil_l", vprofil))
    zeilen.append(("sa_kennzahl_n", n))
    zeilen.append(("sa_personen", personen))
    zeilen.append(("sa_wz_kwh", wz))
    zeilen.append(("sa_volumen_din_l", vdin))
    zeilen.append(("sa_glf", glf))
    zeilen.append(("sa_volumen_glf_l", vglf))
    zeilen.append(("sa_volumen_klassisch_l", vklass))
    zeilen.append(("sa_band_min_l", bmin))
    zeilen.append(("sa_band_max_l", bmax))
    zeilen.append(("sa_nenninhalt_l", float(nenn)))
    zeilen.append(("sa_kapazitaet_kwh", c))
    zeilen.append(("sa_min_fuellstand_kwh", mini))
    zeilen.append(("sa_reserve", mini / c))
    for t, x in enumerate(d, start=1):
        zeilen.append(("sa_defizit_%03d" % t, x))


# ----------------------------------------------------------------------------------------
# Fassadenfall (N10): Wochenreihe, f_KW,A, Wahl des Bedarfstags, Laufzeitfenster
# ----------------------------------------------------------------------------------------

TAGE = 365
MONATSLAENGEN = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
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
    return [float(stunden.get(str(h), 0.0)) for h in range(24)]


def fassade(fa, zeilen):
    """Die Fassade der Auslegung fuer eine Gruppe am Durchfluss - nach den Formeln des Papiers
    (4.1 Mengengeruest, 4.2 Kalender und Formvektor, 4.3 Zirkulation und Laufzeitfenster (N7 (g)),
    4.2/4.5 Wochenreihe bei theta_KW,A, 4.5 Vorgaberegel des Bedarfstags, Katalogtag mit
    Bezugsmenge und Umrechnung auf theta_KW,A (N10))."""
    par = fa["parameter"]
    jan1 = fa["wochentag_jan1"]
    feiertage = set(fa["feiertage"])
    bezug_zapf, bezug_kalt = fa["bezug_zapftemperatur_c"], fa["bezug_kaltwasser_c"]
    projekt = fa["projekt"]
    kw_a = projekt["kaltwasser_auslegung_c"] if projekt["kaltwasser_auslegung_c"] is not None \
        else par["A100.Kaltwasser.Auslegung"]
    kw_katalog = par["A100.Kaltwasser.Auslegung"]
    mittel = par["Kaltwasser.Bilanz.Mittel"]
    amp = par["Kaltwasser.Bilanz.Amplitude"]
    m_max = par["Kaltwasser.Bilanz.MonatMaximum"]

    we = []
    for d in range(1, TAGE + 1):
        wt = (jan1 + d - 1) % 7
        we.append(wt in (5, 6) or d in feiertage)

    satz = fa["tagesgangsatz"]
    phi = {}
    for typ, schluessel in ((WERKTAG, "werktag"), (SAMSTAG, "samstag"), (SONNFEIERTAG, "sonntag"), (RUHETAG, "ruhetag")):
        g = gang(satz[schluessel])
        s = sum_seq(g)
        phi[typ] = [x / s for x in g] if s > 0 else None
    arten = {n["id"]: n for n in fa["nutzungsarten"]}

    def kalender(ferien):
        typen = []
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
            typen.append(typ)
        return typen

    def tagesmengen(q_jahr, n, typen, f_kw):
        w_roh = n["woche"]
        s_w = sum_seq(w_roh)
        w = [x / s_w for x in w_roh]
        g = []
        for d in range(1, TAGE + 1):
            typ = typen[d - 1]
            gewicht = 0.0
            if phi[typ] is not None:
                m = monat_von(d) - 1
                wt = (jan1 + d - 1) % 7
                if typ == WERKTAG:
                    w_t = w[wt]
                elif typ == SAMSTAG:
                    w_t = w[5]
                elif typ == SONNFEIERTAG:
                    w_t = w[6]
                else:
                    w_t = w[6] if n["ferienfaktor"] is None else n["ferienfaktor"] * (sum_seq(w) / 7)
                gewicht = n["monate"][m] * f_kw[m] * 7 * w_t
            g.append(gewicht)
        summe = sum_seq(g)
        return [q_jahr * x / summe for x in g]

    zonen = []
    for z in fa["zonen"]:
        n = arten[z["nutzungsart"]]
        zapf = bezug_zapf
        f_theta = (zapf - mittel) / (bezug_zapf - bezug_kalt)                         # 4.1
        q_a = z["bezugsmenge"] * n["bedarf"][z["niveau"] - 1] * TAGE * f_theta
        theta_m = [round(mittel + amp * math.cos(2.0 * math.pi * (m - m_max) / 12), 9) for m in range(1, 13)]
        f_kw = [(zapf - theta_m[m]) / (zapf - mittel) for m in range(12)]           # Bilanz (4.2)
        f_kwa = (zapf - kw_a) / (zapf - mittel)                                      # Auslegung (4.2)
        typen = kalender(ferientage(z["ferien"]))
        zonen.append({
            "z": z, "n": n, "q_a": q_a, "f_kwa": f_kwa, "typen": typen, "zapf": zapf,
            "z1": n["grenze"] == 1 and z["zirkulation"],
            "bilanz": tagesmengen(q_a, n, typen, f_kw),
            "auslegung": tagesmengen(q_a * f_kwa, n, typen, [1.0] * 12),
        })

    # 4.3: Zirkulation nach der Methode Anteil, Laufzeit aus dem Parameter; Laufzeitfenster um die
    # Tagesmitte der Zapfung der Zonen in Z1 (Tagesmengen der Bilanz mal Tagesgang, N7 (g)).
    assert projekt["zirk_methode"] == 2
    t_lauf = par["Zirkulation.Laufzeit"]
    summe_z1 = 0.0
    for z in zonen:
        if z["z1"]:
            summe_z1 += z["q_a"]
    p_zirk = par["Zirkulation.Anteil"] * (summe_z1 / TAGE) / t_lauf
    e_h = [0.0] * 24
    for z in zonen:
        if not z["z1"]:
            continue
        for h in range(24):
            for d in range(TAGE):
                e_h[h] += z["bilanz"][d] * phi[z["typen"][d]][h]
    zaehler, nenner = 0.0, 0.0
    for h in range(24):
        zaehler += (h + 0.5) * e_h[h]
        nenner += e_h[h]
    mitte = zaehler / nenner
    beginn = math.floor(mitte - t_lauf / 2.0 + 0.5)
    if beginn < 0:
        beginn = 0
    if beginn + t_lauf > 24:
        beginn = math.floor(24 - t_lauf)

    # 4.2/4.5: Wochenreihe - die sieben Tage mit der groessten Summe der Auslegungsmengen.
    s = [0.0] * TAGE
    for z in zonen:
        for d in range(TAGE):
            s[d] += z["auslegung"][d]
    beste, beste_summe = 1, -math.inf
    for d0 in range(1, TAGE - 7 + 2):
        w = 0.0
        for k in range(7):
            w += s[d0 - 1 + k]
        if w > beste_summe:
            beste, beste_summe = d0, w
    woche = []
    region = kalender(set())
    for k in range(7):
        stunden = [0.0] * 24
        for z in zonen:
            q = z["auslegung"][beste - 1 + k]
            f = phi[z["typen"][beste - 1 + k]]
            for h in range(24):
                stunden[h] += q * f[h]
        woche.extend(stunden)
    wochensumme = 0.0
    for k in range(7):
        t = 0.0
        for h in range(24):
            t += woche[k * 24 + h]
        wochensumme += t

    # 4.5: Vorgaberegel - ohne ausdrueckliche Quelle gilt der gewaehlte Katalogtag; skaliert auf die
    # Bezugsmenge der Gruppe (eine Bezugsart) und umgerechnet auf theta_KW,A des Projekts (N10).
    tag = fa["bedarfstag"]
    assert tag["id"] == projekt["id_bedarfstag"]
    arten_der_gruppe = set(z["n"]["bezugsart"] for z in zonen)
    assert len(arten_der_gruppe) == 1
    ziel = 0.0
    for z in zonen:
        ziel += z["z"]["bezugsmenge"]
    faktor = ziel / tag["bezugsmenge"]
    zapf_gruppe = set(z["zapf"] for z in zonen)
    assert len(zapf_gruppe) == 1
    zapf = zapf_gruppe.pop()
    f_tag = (zapf - kw_a) / (zapf - kw_katalog)
    faktor *= f_tag
    minuten = minutenwerte([(b, dauer, e * faktor) for b, dauer, e in tag["ereignisse"]])
    tagessumme = sum_seq(minuten)
    spitze = 0.0
    for x in minuten:
        if x > spitze:
            spitze = x
    stunde_max = 0.0
    for h in range(24):
        t = 0.0
        for m in range(60):
            t += minuten[h * 60 + m]
        if t > stunde_max:
            stunde_max = t

    for i, z in enumerate(zonen, start=1):
        zeilen.append(("fa_zone_%d_jahresenergie_kwh" % i, z["q_a"]))
        zeilen.append(("fa_zone_%d_fkwa" % i, z["f_kwa"]))
    zeilen.append(("fa_zirkulation_leistung_kw", p_zirk))
    zeilen.append(("fa_laufzeit_beginn_h", float(beginn)))
    zeilen.append(("fa_laufzeit_h", t_lauf))
    zeilen.append(("fa_woche_erster_tag", float(beste)))
    zeilen.append(("fa_woche_wochentag_erster_tag", float((jan1 + beste - 1) % 7)))
    for k in range(7):
        zeilen.append(("fa_woche_tagtyp_%d" % (k + 1), float(region[beste - 1 + k])))
    zeilen.append(("fa_woche_summe_kwh", wochensumme))
    zeilen.append(("fa_woche_fenstersumme_kwh", beste_summe))
    for t, x in enumerate(woche, start=1):
        zeilen.append(("fa_woche_%03d" % t, x))
    zeilen.append(("fa_bedarfstag_quelle", float(tag["quelle_art"])))
    zeilen.append(("fa_bedarfstag_faktor", f_tag))
    zeilen.append(("fa_bedarfstag_summe_kwh", tagessumme))
    zeilen.append(("fa_minutenspitze_kw", spitze * 60))
    zeilen.append(("fa_stundenspitze_kw", stunde_max))
    for i, x in enumerate(minuten):
        zeilen.append(("fa_tag_%04d" % i, x))


def main():
    with open(EINGABE, encoding="utf-8") as f:
        ein = json.load(f)
    zeilen = []
    for i, fall in enumerate(ein["summenlinie"], start=1):
        summenlinie(fall, "sl%d" % i, zeilen)
    speicherauslegung(ein["speicherauslegung"], zeilen)
    fassade(ein["fassade"], zeilen)
    with open(ERGEBNIS, "w", encoding="utf-8", newline="\r\n") as f:
        f.write("# FIKTIVER REFERENZFALL der Auslegung (Stufe Z2) - alle Werte erfunden; erzeugt von auslegung_referenzfall_bauen.py\n")
        f.write("groesse,wert\n")
        for name, wert in zeilen:
            f.write(name + "," + ("" if wert is None else format(wert, ".12f")) + "\n")
    print("%d Groessen geschrieben: %s" % (len(zeilen), ERGEBNIS))


if __name__ == "__main__":
    main()
