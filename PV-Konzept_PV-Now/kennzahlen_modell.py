# -*- coding: utf-8 -*-
"""
kennzahlen_modell.py — Nachrechnung der pv@now-Wirtschaftlichkeitskennzahlen
============================================================================
Projekt  : INEKON Schulung 01 (pv@now manager 10.0.0, DGS Franken)
Plaene   : "Ueberschuss"      = Betreibermodell PV-Ueberschuss-Einspeisung 1 (I=B=G=V_G)
           "Volleinspeisung"  = Betreibermodell PV-Netz-Einspeisung 1        (I=B=G)
Zweck    : Transparente, monatsgenaue Nachbildung der Zahlungsreihe beider Plaene
           und Ableitung aller Kennzahlen; Abgleich mit den Tool-Werten.
Stand    : 25.08.2026 · alle Betraege NETTO
Aufruf   : python3 kennzahlen_modell.py            -> Ergebnistabelle + Verifikation
           python3 kennzahlen_modell.py --scan     -> zusaetzlich Variantenscan
----------------------------------------------------------------------------
GEWAEHLTE MODELLVARIANTE (Begruendung siehe Abschnitt "VARIANTEN" unten):
  1. Monatsraster, 250 Monate (3/2023-12/2043); Investition in t = 0.
  2. Rumpfjahr 2023 (Maerz-Dezember) beim ERTRAG mit saisonalem Monatsprofil
     (PVGIS-typisch Stuttgart, Sued 30 Grad -> Maerz-Dez = 92,07 % des Jahresertrags),
     NICHT mit 10/12 = 83,33 %.
     Bei den BETRIEBSKOSTEN dagegen streng zeitanteilig 10/12 - das ist durch die
     Tool-Summen 42.500 EUR (= 2.040 x 250/12) und 33.333 EUR (= 1.600 x 250/12) belegt.
  3. Leistungsminderung 5 % in gleichen Jahresstufen: Faktor(Kalenderjahr y) = 1 - 5 % * y/20
     mit y = 0 fuer 2023 -> 2023 = 100,00 %, 2024 = 99,75 %, ..., 2043 = 95,00 %.
  4. Netzstrompreis 30,00 ct/kWh, Sprung um 4 % zu jedem Kalenderjahreswechsel.
  5. Diskontierung monatlich mit (1 + KZS)^(-m/12).
  6. Mess-/Abrechnungskosten 100 EUR/a: ergebnisneutral (mit und ohne PV identisch)
     -> nicht Teil des PV-Zahlungsstroms.
  7. LCOE nach pv@now-Definition: Ausgaben UND Strommenge diskontiert;
     Zweitwert "KZS = 0 %": beides undiskontiert.
  8. IRR aus den Monats-Cashflows, annualisiert; Baldwin-MIRR aus den
     JAHRES-Einzahlungsueberschuessen (Jahresmitte-Konvention), n = 250/12 Jahre.
"""
from __future__ import annotations
import sys, math

# ===========================================================================
# 1  EINGABEPARAMETER  (Quelle: app_extraktion.md, pv@now-Projekt, 25.08.2026)
# ===========================================================================
KWP            = 30.0        # Nennleistung [kWp]
SPEZ_ERTRAG    = 1078.0      # spez. Jahresertrag [kWh/kWp]
G_NOM          = KWP * SPEZ_ERTRAG          # 32.340 kWh/a (erstes volles Jahr)
INVEST         = 54_000.0    # Investition [EUR], 100 % EK, t = 0
KZS            = 0.035       # Kalkulationszinssatz
MONATE         = 250         # 3/2023-12/2043 = 10 + 20*12 Monate
JAHRE          = MONATE / 12 # 20,8333 Jahre
MINDERUNG      = 0.05        # Leistungsminderung gesamt ueber die Laufzeit

# PV-Stromaufteilung Plan "Ueberschuss"
EV_KWH         = 26_962.0    # Eigenverbrauch [kWh/a]
EIN_KWH        =  5_378.0    # EEG-verguetete Netzeinspeisung [kWh/a]
EV_QUOTE       = EV_KWH  / G_NOM   # 0,83370
EIN_QUOTE      = EIN_KWH / G_NOM   # 0,16630

# Verguetungssaetze [EUR/kWh]
SATZ_UE        = 0.0747      # feste EEG-EV Teileinspeisung (Mischsatz 30 kWp)
SATZ_VOLL      = 0.1160      # feste EEG-EV Volleinspeisung (7,47 + 4,13)

# Betriebskosten [EUR/a], Kostentraeger Betreiber, keine Steigerungsrate
OPEX_UE        = 2_040.0     # Wartung 540 (1 % v. 54.000) + Vers. 500 + Verw. 1.000
OPEX_VOLL      = 1_600.0     # Messung 100 + Vers. 500 + Verw. 1.000

# Verbrauchssituation (nur Plan "Ueberschuss")
STROMPREIS_0   = 0.30        # Netzstrompreis [EUR/kWh] im Basisjahr
PREISSTEIG     = 0.04        # Steigerung [1/a]
MESSKOSTEN     = 100.0       # Mess-/Abrechnungskosten [EUR/a] (mit und ohne PV gleich)
GESAMTBEDARF   = 100_000.0   # Gesamtstrombedarf [kWh/a], Lastprofil G4

# Saisonales Monatsprofil des PV-Ertrags (PVGIS-typisch Stuttgart, Sued 30 Grad)
MONATSPROFIL   = [0.0295, 0.0450, 0.0755, 0.1035, 0.1155, 0.1180,
                  0.1235, 0.1140, 0.0925, 0.0640, 0.0345, 0.0245]
MONATSPROFIL   = [x / sum(MONATSPROFIL) for x in MONATSPROFIL]

# Tool-Werte (Soll) fuer den Abgleich
TOOL = {
    "UE":   dict(LUE=162_850.0, NPV=92_568.0, IRR=0.1599, MIRR=0.0858,
                 STAT=8, DYN=9, LCOE_I=0.1785, LCOE_0=0.1464),
    "VOLL": dict(LUE=-10_867.0, NPV=-22_979.0, IRR=-0.0210, MIRR=0.0078,
                 STAT=None, DYN=None, LCOE_I=0.1647, LCOE_0=0.1325),
    "VORTEIL": 162_850.0, "VORTEIL_A": 7_817.0,
    "EV_QUOTE": 0.83, "AUTARKIE": 0.26, "P_MITTEL": 0.4583,
}

# ===========================================================================
# 2  ZEITRASTER UND MENGENGERUEST
# ===========================================================================
def jahr_monat(m: int):
    """m = 1 entspricht Maerz 2023 -> (Jahresoffset ab 2023, Monatsindex 0..11)."""
    k = m - 1 + 2
    return k // 12, k % 12

def ertragsgewichte(rumpfjahr: str):
    """Anteil des Jahresertrags je Monat."""
    return [MONATSPROFIL[jahr_monat(m)[1]] if rumpfjahr == "saisonal" else 1/12
            for m in range(1, MONATE + 1)]

def minderungsfaktoren(modus: str):
    """Leistungsminderungsfaktor je Monat."""
    out = []
    for m in range(1, MONATE + 1):
        if   modus == "keine":  f = 1.0
        elif modus == "linear": f = 1.0 - MINDERUNG * (m - 1) / (MONATE - 1)
        elif modus == "jahr":   f = 1.0 - MINDERUNG * jahr_monat(m)[0] / 20.0
        elif modus == "ende":   f = 1.0 if jahr_monat(m)[0] < 20 else 1.0 - MINDERUNG
        else: raise ValueError(modus)
        out.append(f)
    return out

def preispfad(modus: str):
    """Netzstrompreis je Monat [EUR/kWh]."""
    return [STROMPREIS_0 * (1 + PREISSTEIG) ** (jahr_monat(m)[0] if modus == "kalenderjahr"
            else (m - 1) / 12.0) for m in range(1, MONATE + 1)]

# ===========================================================================
# 3  FINANZMATHEMATIK
# ===========================================================================
def npv(cf, ts, i):
    return sum(c / (1 + i) ** t for c, t in zip(cf, ts))

def irr(cf, ts, lo=-0.95, hi=3.0):
    """Interner Zinsfuss (Bisektion); NaN, wenn kein Vorzeichenwechsel."""
    if npv(cf, ts, lo) * npv(cf, ts, hi) > 0:
        return float("nan")
    for _ in range(400):
        mid = (lo + hi) / 2
        if npv(cf, ts, lo) * npv(cf, ts, mid) <= 0: hi = mid
        else:                                       lo = mid
    return (lo + hi) / 2

def mirr_baldwin(cf, ts, i, n):
    """IRR modifiziert nach Baldwin: Wiederanlage der Ueberschuesse zum KZS.
       MIRR = (Endwert aller Einzahlungsueberschuesse / Barwert aller Auszahlungen)^(1/n) - 1"""
    endwert = sum(c * (1 + i) ** (n - t) for c, t in zip(cf, ts) if c > 0)
    barwert = sum(-c / (1 + i) ** t      for c, t in zip(cf, ts) if c < 0)
    if endwert <= 0 or barwert <= 0: return float("nan")
    return (endwert / barwert) ** (1 / n) - 1

def amortisation(cf_jahr, startsaldo):
    """Erstes Jahr (1-basiert), in dem der kumulierte Saldo >= 0 ist; None = keine."""
    kum = startsaldo
    for k, c in enumerate(cf_jahr, start=1):
        kum += c
        if kum >= 0: return k
    return None

# ===========================================================================
# 4  DAS MODELL
# ===========================================================================
def rechne(rumpfjahr="saisonal", minderung="jahr", preis="kalenderjahr",
           diskont="monatlich", messkosten_im_cf=False, lcoe_menge_diskontiert=True):
    w  = ertragsgewichte(rumpfjahr)
    f  = minderungsfaktoren(minderung)
    p  = preispfad(preis)
    ts = [(i + 1) / 12.0 for i in range(MONATE)]           # Monatsende in Jahren
    df = [(1 + KZS) ** (-t) for t in ts] if diskont == "monatlich" \
         else [(1 + KZS) ** (-jahr_monat(i + 1)[0]) for i in range(MONATE)]

    G = [G_NOM * w[i] * f[i] for i in range(MONATE)]        # Erzeugung je Monat [kWh]
    erg = {"G": G, "p": p, "df": df, "ts": ts}

    for plan in ("UE", "VOLL"):
        opex_a = OPEX_UE if plan == "UE" else OPEX_VOLL
        opex   = [opex_a / 12.0] * MONATE                   # streng zeitanteilig
        if plan == "UE":
            e_eeg = [G[i] * EIN_QUOTE * SATZ_UE for i in range(MONATE)]
            e_ev  = [G[i] * EV_QUOTE  * p[i]    for i in range(MONATE)]
        else:
            e_eeg = [G[i] * SATZ_VOLL for i in range(MONATE)]
            e_ev  = [0.0] * MONATE
        mess = [MESSKOSTEN / 12.0 if messkosten_im_cf else 0.0] * MONATE
        cf   = [e_eeg[i] + e_ev[i] - opex[i] - mess[i] for i in range(MONATE)]

        # Jahresaggregate (Kalenderjahre 2023..2043)
        jahre = sorted({jahr_monat(m)[0] for m in range(1, MONATE + 1)})
        agg   = {y: dict(G=0.0, EEG=0.0, EV=0.0, OPEX=0.0, CF=0.0, CFD=0.0) for y in jahre}
        for i in range(MONATE):
            y = jahr_monat(i + 1)[0]
            agg[y]["G"]    += G[i];      agg[y]["EEG"]  += e_eeg[i]
            agg[y]["EV"]   += e_ev[i];   agg[y]["OPEX"] += opex[i] + mess[i]
            agg[y]["CF"]   += cf[i];     agg[y]["CFD"]  += cf[i] * df[i]
        cf_j  = [agg[y]["CF"]  for y in jahre]
        cfd_j = [agg[y]["CFD"] for y in jahre]
        # Zeitpunkte der Jahres-CF in Jahresmitte des tatsaechlichen Zahlungszeitraums
        ts_j  = [(10/12)/2 if y == 0 else 10/12 + (y - 1) + 0.5 for y in jahre]

        erg[plan] = dict(
            jahre = jahre, agg = agg, cf_monat = cf, cf_jahr = cf_j,
            LUE    = sum(cf) - INVEST,
            NPV    = -INVEST + sum(cf[i] * df[i] for i in range(MONATE)),
            IRR    = irr([-INVEST] + cf, [0.0] + ts),
            MIRR   = mirr_baldwin([-INVEST] + cf_j, [0.0] + ts_j, KZS, JAHRE),
            STAT   = amortisation(cf_j,  -INVEST),
            DYN    = amortisation(cfd_j, -INVEST),
            LCOE_I = (INVEST + sum(opex[i] * df[i] for i in range(MONATE))) /
                     sum(G[i] * (df[i] if lcoe_menge_diskontiert else 1.0) for i in range(MONATE)),
            LCOE_0 = (INVEST + sum(opex)) / sum(G),
            SUM_G  = sum(G), SUM_EEG = sum(e_eeg), SUM_EV = sum(e_ev), SUM_OPEX = sum(opex),
        )

    # ---- Vorteil/Nachteil durch PV (nur Plan "Ueberschuss") -----------------
    # ohne PV : Netzbezug 100.000 kWh/a x Preis + Messkosten
    # mit  PV : Restnetzbezug x Preis + Messkosten + Betriebskosten + Investition
    #           - EEG-Erloes  (der Eigenverbrauch ersetzt Netzbezug)
    ev_kwh = [erg["G"][i] * EV_QUOTE for i in range(MONATE)]
    ohne   = sum(GESAMTBEDARF / 12.0 * p[i] + MESSKOSTEN / 12.0 for i in range(MONATE))
    mit    = sum(max(GESAMTBEDARF / 12.0 - ev_kwh[i], 0.0) * p[i] + MESSKOSTEN / 12.0
                 + OPEX_UE / 12.0 - erg["G"][i] * EIN_QUOTE * SATZ_UE for i in range(MONATE))
    erg["VORTEIL"]   = ohne - mit - INVEST
    erg["VORTEIL_A"] = erg["VORTEIL"] / JAHRE
    erg["EV_QUOTE"]  = sum(ev_kwh) / sum(erg["G"])
    erg["AUTARKIE"]  = sum(ev_kwh) / (GESAMTBEDARF * JAHRE)
    erg["P_MITTEL"]  = sum(p) / MONATE
    return erg

# ===========================================================================
# 5  AUSGABE
# ===========================================================================
def de(x, nk=0):
    if x is None or (isinstance(x, float) and math.isnan(x)): return "-"
    s = f"{x:,.{nk}f}"
    return s.replace(",", "\x00").replace(".", ",").replace("\x00", ".")

def abw(ist, soll, art="rel"):
    """art='pp': ist/soll sind bereits Prozentwerte -> Differenz in Prozentpunkten."""
    if soll is None or ist is None: return "-"
    if art == "pp":  return (f"{ist - soll:+.2f}".replace(".", ",")) + " %-Pkt."
    if soll == 0:    return "-"
    return (f"{(ist - soll) / abs(soll) * 100:+.2f}".replace(".", ",")) + " %"

def bericht(r):
    L = []
    A = L.append
    A("=" * 92)
    A("NACHRECHNUNG pv@now — Projekt INEKON Schulung 01 (Stand 25.08.2026, alle Betraege netto)")
    A("=" * 92)
    A(f"Anlage 30,00 kWp · 1.078 kWh/kWp · {de(G_NOM)} kWh/a · Invest {de(INVEST)} EUR · KZS 3,50 %")
    A(f"Laufzeit 3/2023-12/2043 = {MONATE} Monate = " + f"{JAHRE:.4f}".replace(".", ",") + " Jahre")
    anteil = f"{r['UE']['SUM_G'] / (G_NOM * JAHRE) * 100:.2f}".replace(".", ",")
    A(f"Erzeugung gesamt (mit 5 % Leistungsminderung): {de(r['UE']['SUM_G'])} kWh "
      f"= {anteil} % der nominellen {de(G_NOM * JAHRE)} kWh")
    A("")

    # ---- Jahr-1-Herleitung (volles Referenzjahr, ohne Rumpfjahr-/Minderungseffekt)
    A("-" * 92)
    A("A) HERLEITUNG REFERENZJAHR (volles Betriebsjahr zu Startkonditionen)")
    A("-" * 92)
    A(f"  Plan Ueberschuss")
    A(f"    Erzeugung                          {de(G_NOM)} kWh")
    A(f"    Eigenverbrauch  {de(EV_KWH)} kWh x 30,00 ct/kWh = {de(EV_KWH*STROMPREIS_0,2)} EUR (vermiedener Netzbezug)")
    A(f"    Einspeisung      {de(EIN_KWH)} kWh x  7,47 ct/kWh = {de(EIN_KWH*SATZ_UE,2)} EUR")
    A(f"    - Betriebskosten                        {de(OPEX_UE,2)} EUR")
    A(f"    = Cashflow Referenzjahr                 {de(EV_KWH*STROMPREIS_0+EIN_KWH*SATZ_UE-OPEX_UE,2)} EUR")
    A(f"  Plan Volleinspeisung")
    A(f"    Einspeisung     {de(G_NOM)} kWh x 11,60 ct/kWh = {de(G_NOM*SATZ_VOLL,2)} EUR")
    A(f"    - Betriebskosten                        {de(OPEX_VOLL,2)} EUR")
    A(f"    = Cashflow Referenzjahr                 {de(G_NOM*SATZ_VOLL-OPEX_VOLL,2)} EUR")
    A("")

    # ---- Jahrestabelle
    for plan, name in (("UE", "Plan UEBERSCHUSS (PV-Ueberschuss-Einspeisung 1)"),
                       ("VOLL", "Plan VOLLEINSPEISUNG (PV-Netz-Einspeisung 1)")):
        d = r[plan]
        A("-" * 92)
        A(f"B) JAHRESTABELLE — {name}")
        A("-" * 92)
        A(f"{'Jahr':>4} {'Kal.':>5} {'Erzeug.kWh':>11} {'EEG EUR':>10} {'EV-Wert EUR':>12} "
          f"{'BK EUR':>9} {'CF EUR':>11} {'kum. CF EUR':>13}")
        kum = -INVEST
        A(f"{0:>4} {'t=0':>5} {'':>11} {'':>10} {'':>12} {'':>9} {de(-INVEST):>11} {de(kum):>13}")
        for k, y in enumerate(d["jahre"], start=1):
            a = d["agg"][y]; kum += a["CF"]
            A(f"{k:>4} {2023+y:>5} {de(a['G']):>11} {de(a['EEG']):>10} {de(a['EV']):>12} "
              f"{de(a['OPEX']):>9} {de(a['CF']):>11} {de(kum):>13}")
        A(f"{'SUM':>4} {'':>5} {de(d['SUM_G']):>11} {de(d['SUM_EEG']):>10} {de(d['SUM_EV']):>12} "
          f"{de(d['SUM_OPEX']):>9} {de(sum(d['cf_jahr'])):>11} {de(kum):>13}")
        A("")

    # ---- Verifikation
    A("=" * 92)
    A("C) VERIFIKATION — Tool-Wert vs. nachgerechnet")
    A("=" * 92)
    A(f"{'Kennzahl':<42}{'Tool':>14}{'nachgerechnet':>16}{'Abweichung':>16}")
    A("-" * 92)
    rows = [
        ("Liquiditaetsueberschuss Ueberschuss [EUR]", TOOL["UE"]["LUE"],  r["UE"]["LUE"],  0, "rel"),
        ("Liquiditaetsueberschuss Volleinsp.  [EUR]", TOOL["VOLL"]["LUE"], r["VOLL"]["LUE"], 0, "rel"),
        ("Kapitalwert Ueberschuss             [EUR]", TOOL["UE"]["NPV"],  r["UE"]["NPV"],  0, "rel"),
        ("Kapitalwert Volleinspeisung         [EUR]", TOOL["VOLL"]["NPV"], r["VOLL"]["NPV"], 0, "rel"),
        ("Interner Zinsfuss Ueberschuss       [%]",   TOOL["UE"]["IRR"]*100,  r["UE"]["IRR"]*100,  2, "pp"),
        ("Interner Zinsfuss Volleinspeisung   [%]",   TOOL["VOLL"]["IRR"]*100, r["VOLL"]["IRR"]*100, 2, "pp"),
        ("IRR Baldwin Ueberschuss             [%]",   TOOL["UE"]["MIRR"]*100,  r["UE"]["MIRR"]*100,  2, "pp"),
        ("IRR Baldwin Volleinspeisung         [%]",   TOOL["VOLL"]["MIRR"]*100, r["VOLL"]["MIRR"]*100, 2, "pp"),
        ("Stromgestehungskosten Ue. mit KZS [ct/kWh]", TOOL["UE"]["LCOE_I"]*100, r["UE"]["LCOE_I"]*100, 2, "rel"),
        ("Stromgestehungskosten Ue. KZS=0   [ct/kWh]", TOOL["UE"]["LCOE_0"]*100, r["UE"]["LCOE_0"]*100, 2, "rel"),
        ("Stromgestehungskosten Vo. mit KZS [ct/kWh]", TOOL["VOLL"]["LCOE_I"]*100, r["VOLL"]["LCOE_I"]*100, 2, "rel"),
        ("Stromgestehungskosten Vo. KZS=0   [ct/kWh]", TOOL["VOLL"]["LCOE_0"]*100, r["VOLL"]["LCOE_0"]*100, 2, "rel"),
        ("Vorteil durch PV gesamt             [EUR]", TOOL["VORTEIL"],   r["VORTEIL"],   0, "rel"),
        ("Vorteil durch PV je Jahr            [EUR]", TOOL["VORTEIL_A"], r["VORTEIL_A"], 0, "rel"),
        ("Eigenverbrauchsquote                [%]",   TOOL["EV_QUOTE"]*100, r["EV_QUOTE"]*100, 1, "pp"),
        ("Autarkiegrad                        [%]",   TOOL["AUTARKIE"]*100, r["AUTARKIE"]*100, 1, "pp"),
        ("Netzstrompreis Mittel             [ct/kWh]", TOOL["P_MITTEL"]*100, r["P_MITTEL"]*100, 2, "rel"),
    ]
    for lab, soll, ist, nk, art in rows:
        A(f"{lab:<42}{de(soll,nk):>14}{de(ist,nk):>16}{abw(ist,soll,art):>16}")
    A(f"{'Statische Amortisation Ueberschuss  [J.]':<42}{TOOL['UE']['STAT']:>14}"
      f"{r['UE']['STAT']:>16}{'exakt' if r['UE']['STAT']==TOOL['UE']['STAT'] else 'ABWEICHUNG':>16}")
    A(f"{'Dynamische Amortisation Ueberschuss [J.]':<42}{TOOL['UE']['DYN']:>14}"
      f"{r['UE']['DYN']:>16}{'exakt' if r['UE']['DYN']==TOOL['UE']['DYN'] else 'ABWEICHUNG':>16}")
    A(f"{'Amortisation Volleinspeisung        [J.]':<42}{'keine':>14}"
      f"{('keine' if r['VOLL']['STAT'] is None else r['VOLL']['STAT']):>16}"
      f"{'exakt' if r['VOLL']['STAT'] is None else 'ABWEICHUNG':>16}")
    A("-" * 92)
    rel = [abs(i - s) / abs(s) for _, s, i, _, a in rows if a == "rel" and s]
    A("Mittlere relative Abweichung der wertmaessigen Kennzahlen: "
      + f"{sum(rel)/len(rel)*100:.2f}".replace(".", ",") + " %")
    return "\n".join(L)

# ===========================================================================
# 6  VARIANTENSCAN (Sensitivitaet der unklaren Modellannahmen)
# ===========================================================================
def scan():
    ziele = [("UE","LUE"),("VOLL","LUE"),("UE","NPV"),("VOLL","NPV"),
             ("UE","LCOE_I"),("UE","LCOE_0"),("VOLL","LCOE_I"),("VOLL","LCOE_0")]
    print("\n" + "=" * 92)
    print("D) VARIANTENSCAN — mittlere relative Abweichung je Annahmenkombination")
    print("=" * 92)
    print(f"{'Rumpfjahr':<12}{'Minderung':<12}{'Preis':<15}{'Diskont':<12}{'Mess':<7}{'Ø Abw.':>10}")
    print("-" * 92)
    res = []
    for rj in ("saisonal", "flach"):
        for mi in ("linear", "jahr", "ende", "keine"):
            for pr in ("kalenderjahr", "monatlich"):
                for di in ("monatlich", "jaehrlich"):
                    for mc in (False, True):
                        r = rechne(rj, mi, pr, di, mc)
                        d = sum(abs(r[p][k] - TOOL[p][k]) / abs(TOOL[p][k]) for p, k in ziele) / len(ziele)
                        res.append((d, rj, mi, pr, di, mc))
    res.sort()
    for d, rj, mi, pr, di, mc in res[:12]:
        print(f"{rj:<12}{mi:<12}{pr:<15}{di:<12}{str(mc):<7}{d*100:>9.3f} %")
    print("...")
    for d, rj, mi, pr, di, mc in res[-3:]:
        print(f"{rj:<12}{mi:<12}{pr:<15}{di:<12}{str(mc):<7}{d*100:>9.3f} %")

if __name__ == "__main__":
    r = rechne()          # gewaehlte Hauptvariante
    print(bericht(r))
    if "--scan" in sys.argv:
        scan()
