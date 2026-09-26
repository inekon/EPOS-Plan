# -*- coding: utf-8 -*-
"""
Erzeugt die neun Ecodesign-Zapfprofile (XXS bis 4XL) des freien Paketteils
(Referenzlaeufe/Katalogpaket_frei/) aus der Rohtabelle der Verordnung (EU) Nr. 814/2013 der
Kommission, Anhang III, Tabelle 1 "Lastprofile von Warmwasserbereitern" (ABl. L 239 vom
6.9.2013, S. 162) - EU-Recht, keine Normzahl. Anwenderentscheid: "Abschnitt 1: Ecodesign -
erweitere Profil" - der freie Paketteil bekommt alle neun Lastprofile der Tabelle 1 (3XS ist
nicht Teil dieses Auftrags), nicht nur das bisherige Profil L.

Quelle der Rohwerte: ecodesign_profile_814_2013.json (Uhrzeit, Q_tap, Volumenstrom f,
Mindesttemperatur T_m, Spitzentemperatur T_p je Zapfung und Profil, dazu Q_ref der Verordnung
zur Gegenprobe). Dieses Skript LIEST die JSON-Datei und SCHREIBT daraus wiederholbar:

  - Referenzlaeufe/Katalogpaket_frei/Tab_TwwBedarfstag_STAMM.csv        (9 Zeilen, eine je Profil)
  - Referenzlaeufe/Katalogpaket_frei/Tab_TwwBedarfstagEreignis_STAMM.csv (alle Zapfungen)

Verfahren "Dauer der Ecodesign-Zapfungen" (LIESMICH.md des Paketteils, unveraendert seit Profil
L): Die Tabelle der Verordnung nennt Energie, Volumenstrom und Temperaturen, keine Dauer.
Setzung: Dauer = Volumen / Volumenstrom, Volumen = Q_tap * 1000 / (c_w * (Nutztemperatur -
10 C)), Nutztemperatur = Spitzentemperatur T_p, wo angegeben, sonst die Mindesttemperatur T_m;
c_w = 1,163 Wh/(l*K) (Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K); ganze Minuten
kaufmaennisch gerundet (Decimal, ROUND_HALF_UP), mindestens 1. Die Energie jeder Zapfung ist
Q_tap unveraendert. Profil L reproduziert mit diesem Verfahren byte-genau seinen bisherigen
Bestand (24 Ereignisse, ID 1) - die Kontrolle des Verfahrens.

Reihenfolge der Bedarfstage (Schluessel des Pakets, siehe LIESMICH.md Regel 3): Profil L behaelt
ID 1 und seine Ereignisse unveraendert (erste Zeile, unveraendert seit der Umsetzung des
Ecodesign-Zapfprofils); die uebrigen acht folgen in aufsteigender Groessenordnung XXS, XS, S, M,
XL, XXL, 3XL, 4XL mit den IDs 2 bis 9. Die ID ist allein Schluessel des Pakets - die Datenbank
vergibt beim Einspielen die echte ID (tww_testkatalog_fiktiv.py).

Aufruf (nur pruefen, schreibt nichts):
    py Referenzlaeufe/Skripte/ecodesign_profile_bauen.py
Aufruf (schreibt die zwei CSV-Dateien neu, bricht ab, wenn eine Pruefsumme nicht stimmt oder
Profil L nicht byte-gleich herauskommt):
    py Referenzlaeufe/Skripte/ecodesign_profile_bauen.py --schreiben
"""
import json
import os
import sys
from decimal import Decimal, ROUND_HALF_UP

ORDNER = os.path.dirname(os.path.abspath(__file__))
QUELLDATEI = os.path.join(ORDNER, "ecodesign_profile_814_2013.json")
PAKETTEIL = os.path.join(os.path.dirname(ORDNER), "Katalogpaket_frei")
DATEI_BEDARFSTAG = os.path.join(PAKETTEIL, "Tab_TwwBedarfstag_STAMM.csv")
DATEI_EREIGNIS = os.path.join(PAKETTEIL, "Tab_TwwBedarfstagEreignis_STAMM.csv")

# Reihenfolge der Bedarfstage: L zuerst (ID 1, unveraendert), dann aufsteigend nach Groesse.
REIHENFOLGE_PROFILE = ["L", "XXS", "XS", "S", "M", "XL", "XXL", "3XL", "4XL"]

QUELLE_ECODESIGN = "Verordnung (EU) Nr. 814/2013 Anhang III"
AUSGABE_MUSTER = "ABl. L 239 vom 6.9.2013, Tabelle 1, Lastprofil {0}"
VERSION_PAKETTEIL = "FREI-1"
QUELLE_ART_ECODESIGN = 5
BEZUGSART_WOHNEINHEIT = 2

CW_WH_JE_L_K = Decimal("1.163")  # Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K
ZEHN = Decimal("10")

KOPF_BEDARFSTAG = "ID;Bezeichner;Quelle_Art;Bezugsmenge;Bezugsart;Quelle;Ausgabe;Version;Herkunftsart;Status;ReadOnly"
KOPF_EREIGNIS = "ID_Bedarfstag;Minute_Beginn;Dauer_min;Energie_Kwh;Reihenfolge"


def minute_aus_uhrzeit(uhrzeit):
    h, m = uhrzeit.split(":")
    return int(h) * 60 + int(m)


def dauer_minuten(q_tap_kwh, f_l_min, nutztemperatur_c):
    """Dauer = Volumen / Volumenstrom (Setzung LIESMICH.md), kaufmaennisch, mindestens 1."""
    q = Decimal(str(q_tap_kwh))
    f = Decimal(str(f_l_min))
    theta = Decimal(str(nutztemperatur_c))
    volumen_l = (q * Decimal(1000)) / (CW_WH_JE_L_K * (theta - ZEHN))
    roh = volumen_l / f
    ganz = int(roh.to_integral_value(rounding=ROUND_HALF_UP))
    return max(1, ganz)


def zahl_text(x):
    """Text der Energie ohne unnoetige Nachkommastellen, wie die Quelle (0.105, 1.4, 19.07, ...)."""
    if isinstance(x, float) and x == int(x):
        return str(int(x))
    return repr(x) if isinstance(x, float) else str(x)


def profile_bauen(quelle):
    """Je Profil die Liste der Ereignisse (Minute_Beginn, Dauer_min, Energie_Kwh) und die
    gepruefte Summe gegen Q_ref."""
    ergebnis = {}
    for name, zapfungen in quelle["zapfungen"].items():
        ereignisse = []
        for z in zapfungen:
            nutztemp = z["t_p_c"] if z["t_p_c"] is not None else z["t_m_c"]
            dauer = dauer_minuten(z["q_tap_kwh"], z["f_l_min"], nutztemp)
            ereignisse.append((minute_aus_uhrzeit(z["uhrzeit"]), dauer, z["q_tap_kwh"]))
        summe = sum(e[2] for e in ereignisse)
        q_ref = quelle["kopf"]["q_ref_kwh"][name]
        abweichung = abs(summe / q_ref - 1.0) if q_ref else 0.0
        assert abweichung < 1e-9, (
            f"Profil {name}: Summe der Zapfungen {summe} weicht von Q_ref {q_ref} ab "
            f"({abweichung:.2e}) - Uebertragungsfehler pruefen."
        )
        assert all(d >= 1 for _, d, _ in ereignisse), f"Profil {name}: eine Dauer unter 1 Minute."
        assert [e[0] for e in ereignisse] == sorted(e[0] for e in ereignisse), \
            f"Profil {name}: Zapfungen nicht in aufsteigender Uhrzeit."
        ergebnis[name] = {"ereignisse": ereignisse, "q_ref": q_ref}
    return ergebnis


def zeilen_bedarfstag(profile):
    zeilen = [KOPF_BEDARFSTAG]
    for lfd, name in enumerate(REIHENFOLGE_PROFILE, start=1):
        bezeichner = f"Ecodesign-Zapfprofil {name}"
        ausgabe = AUSGABE_MUSTER.format(name)
        zeilen.append(";".join([
            str(lfd), bezeichner, str(QUELLE_ART_ECODESIGN), "", str(BEZUGSART_WOHNEINHEIT),
            QUELLE_ECODESIGN, ausgabe, VERSION_PAKETTEIL, "FREI", "AUSLIEFERUNG", "1",
        ]))
    return zeilen


def zeilen_ereignis(profile):
    zeilen = [KOPF_EREIGNIS]
    for lfd, name in enumerate(REIHENFOLGE_PROFILE, start=1):
        for reihenfolge, (minute, dauer, energie) in enumerate(profile[name]["ereignisse"], start=1):
            zeilen.append(";".join([str(lfd), str(minute), str(dauer), zahl_text(energie), str(reihenfolge)]))
    return zeilen


def schreiben(pfad, zeilen):
    with open(pfad, "w", encoding="utf-8", newline="\r\n") as f:
        for z in zeilen:
            f.write(z + "\n")


def bestehende_zeilen(pfad):
    if not os.path.exists(pfad):
        return None
    with open(pfad, "r", encoding="utf-8", newline="") as f:
        return f.read().splitlines()


def main():
    schreibmodus = "--schreiben" in sys.argv[1:]

    with open(QUELLDATEI, "r", encoding="utf-8") as f:
        quelle = json.load(f)

    profile = profile_bauen(quelle)

    neu_bedarfstag = zeilen_bedarfstag(profile)
    neu_ereignis = zeilen_ereignis(profile)

    # Kontrolle des Verfahrens: Profil L (ID 1) reproduziert den bisherigen Bestand byte-genau.
    alt_ereignis = bestehende_zeilen(DATEI_EREIGNIS)
    l_alt = [z for z in (alt_ereignis or []) if z.startswith("1;")]
    l_neu = [z for z in neu_ereignis if z.startswith("1;")]
    if alt_ereignis is not None:
        assert l_neu == l_alt, (
            "Profil L (ID 1) weicht vom bisherigen Bestand ab - das Verfahren waere nicht "
            "byte-gleich:\nalt:\n" + "\n".join(l_alt) + "\nneu:\n" + "\n".join(l_neu)
        )

    print("Ecodesign-Zapfprofile (Verordnung (EU) Nr. 814/2013 Anhang III, Tabelle 1):")
    gesamt_ereignisse = 0
    for name in REIHENFOLGE_PROFILE:
        n = len(profile[name]["ereignisse"])
        gesamt_ereignisse += n
        summe = sum(e[2] for e in profile[name]["ereignisse"])
        print(f"  {name:>4}: {n:2d} Zapfungen, Summe {summe:.3f} kWh "
              f"(Q_ref {profile[name]['q_ref']:.3f} kWh)")
    print(f"  Bedarfstage: {len(REIHENFOLGE_PROFILE)}, Ereignisse gesamt: {gesamt_ereignisse}")
    print("  Profil L (ID 1): byte-gleich zum bisherigen Bestand." if alt_ereignis is not None
          else "  Profil L (ID 1): kein bisheriger Bestand zum Vergleich gefunden.")

    if not schreibmodus:
        print("\nNur geprueft (kein Schalter --schreiben) - keine Datei geschrieben.")
        return

    schreiben(DATEI_BEDARFSTAG, neu_bedarfstag)
    schreiben(DATEI_EREIGNIS, neu_ereignis)
    print(f"\nGeschrieben: {DATEI_BEDARFSTAG}\n           {DATEI_EREIGNIS}")


if __name__ == "__main__":
    main()
