# -*- coding: utf-8 -*-
"""Konverter: norwegische Messreihen (Mendeley Data) → Format des `Messreihenleser`.

Quelle (CC BY 4.0, Namensnennung in jedem Bericht):
Sørensen, Å. L. et al.: *Measurement data on domestic hot water consumption and related energy use
in hotels, nursing homes and apartment buildings in Norway.* Mendeley Data V2,
doi:10.17632/m3xy22pf4j.2; Beschreibung: Data in Brief 2021, doi:10.1016/j.dib.2021.107228.

Aufbau der Quelle: je Gebäude eine stündliche Datei `<Kennung>_2.csv` mit `Time` (Ortszeit CET),
`Q_chw` (Zapfenergie kWh/h) und — nicht überall — `Q_hwc` (Zirkulationsverluste kWh/h). Die erste
Spalte trägt in einigen Dateien keinen Namen; genommen wird sie unabhängig davon als Zeitstempel.

**Warum die Summe.** Verglichen wird die Messung gegen `Zapfung + Zirkulation` der Rechnung
(`ZapfprofilHuelle.Vergleichsbericht` bildet dieselbe Summe). Führt die Datei `Q_hwc`, wird sie
addiert und die Bilanzgrenze ist `MitVerteilung`; fehlt sie, bleibt es bei `Q_chw` und
`Zapfstelle`. Ein Mittelweg wäre eine stille Annahme.

Aufruf:
    py Werkzeuge/ZapfprofilValidierung/Konverter/norwegen.py --quelle <ordner> --ziel <ordner>
"""

import argparse
import csv
import datetime
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gemeinsam as g

QUELLENSATZ = ("Sørensen et al., Mendeley Data V2, doi:10.17632/m3xy22pf4j.2 (CC BY 4.0), "
               "stuendliche Datei, Ortszeit CET")

# Nutzungsart je Gebäudegruppe und die Bezugsmenge. Belegt ist nur, was die Beschreibung der Quelle
# nennt; alles andere ist ein runder PLATZHALTER, den der Anwender nachträgt.
GRUPPEN = {
    "AB": ("Wohnen groß (abgeleitet)", "Wohneinheiten"),
    "HO": ("Krankenhaus (abgeleitet)", "Betten"),
    "NH": ("Seniorenheim (abgeleitet)", "Betten"),
}
BEZUGSMENGEN = {
    "AB1": (60, False), "AB2": (56, True), "AB3": (60, False), "AB4": (60, False),
    "HO1": (434, True), "HO2": (200, False), "HO3": (200, False), "HO4": (200, False),
    "NH1": (148, True), "NH2": (100, False), "NH3": (100, False), "NH4": (100, False),
}

ZEITFORMATE = ("%Y-%m-%d %H:%M:%S", "%Y-%m-%d %H:%M", "%d.%m.%Y %H:%M:%S")


def zeit(text):
    for f in ZEITFORMATE:
        try:
            return datetime.datetime.strptime(text.strip(), f)
        except ValueError:
            pass
    return None


def datei_lesen(pfad):
    """Liefert (Werte, hat_zirkulation) — Werte als Liste (datetime, kWh)."""
    with io.open(pfad, encoding="utf-8-sig", newline="") as f:
        leser = csv.reader(f)
        kopf = next(leser)
        namen = [k.strip() for k in kopf]
        i_chw = namen.index("Q_chw") if "Q_chw" in namen else 1
        i_hwc = namen.index("Q_hwc") if "Q_hwc" in namen else None
        werte = []
        for zeile in leser:
            if not zeile or len(zeile) <= i_chw:
                continue
            z = zeit(zeile[0])
            if z is None or zeile[i_chw].strip() == "":
                continue
            w = float(zeile[i_chw])
            if i_hwc is not None and len(zeile) > i_hwc and zeile[i_hwc].strip() != "":
                w += float(zeile[i_hwc])
            werte.append((z, w))
    return werte, i_hwc is not None


def hauptlauf(quelle, ziel):
    dateien = sorted(d for d in os.listdir(quelle) if d.endswith("_2.csv"))
    if not dateien:
        print("Unter %s liegt keine Datei *_2.csv." % quelle)
        return 2
    geschrieben, uebergangen = 0, []
    for d in dateien:
        kennung = d[:-6]
        werte, mit_zirk = datei_lesen(os.path.join(quelle, d))
        if not werte:
            uebergangen.append((kennung, "kein einziger Wert"))
            continue
        jahr = g.jahr_mit_meisten(werte)
        werte = g.auf_jahr(werte, jahr)
        werte, null = g.nullsetzen(werte)
        teil, anteil = g.fenster(werte, 60)
        if teil is None:
            uebergangen.append((kennung, "laengstes Fenster mit hoechstens %.0f %% Luecken nur %.1f d, "
                                "gebraucht werden %d d" % (g.LUECKENANTEIL * 100, anteil or 0.0,
                                                           g.MINDESTTAGE)))
            continue

        art, einheit = GRUPPEN[kennung[:2]]
        menge, belegt = BEZUGSMENGEN[kennung]
        vermerk = ("Norwegen, %s; Kanal %s; Bezugsmenge %d %s (%s); %d negative Stundenwerte auf 0 "
                   "gesetzt (Artefakt der Zaehlerbilanz); Feiertage unbekannt."
                   % (kennung[:2], "Q_chw + Q_hwc" if mit_zirk else "Q_chw", menge, einheit,
                      "aus der Veroeffentlichung" if belegt else "PLATZHALTER, nachzutragen", null))
        ordner = os.path.join(ziel, "NO-" + kennung)
        g.reihe_schreiben(os.path.join(ordner, "messreihe.csv"), g.KOPF_ENERGIE, teil)
        g.objekt_schreiben(os.path.join(ordner, "objekt.json"), "NO-" + kennung, art, menge,
                           "Energie", "Ortszeit", jahr,
                           "MitVerteilung" if mit_zirk else "Zapfstelle",
                           vermerk, QUELLENSATZ, zirkulation=True)
        print(g.bericht("NO-" + kennung, teil, anteil, null,
                        "Jahr %d, %s" % (jahr, "mit Zirkulation" if mit_zirk else "ohne Zirkulation")))
        geschrieben += 1

    for kennung, grund in uebergangen:
        print("%-14s uebergangen: %s" % ("NO-" + kennung, grund))
    print("Geschrieben: %d Objekte, uebergangen: %d." % (geschrieben, len(uebergangen)))
    return 0 if geschrieben else 5


if __name__ == "__main__":
    p = argparse.ArgumentParser(description="Norwegische DHW-Messreihen umsetzen (CC BY 4.0).")
    p.add_argument("--quelle", required=True, help="Ordner mit den Dateien *_2.csv")
    p.add_argument("--ziel", required=True, help="Zielordner AUSSERHALB des Repositoriums")
    a = p.parse_args()
    sys.exit(hauptlauf(a.quelle, a.ziel))
