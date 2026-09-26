#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Bildet die Katalogkennwerte der Nutzungsart "Hotel (aus Messung)" aus den drei norwegischen
Hotelreihen und schreibt sie nach Referenzlaeufe/Skripte/tww_hotel_aus_messung.json
(Umsetzungskonzept Zapfprofilgenerator, Kapitel 9 ZU36; Folge V6 des Validierungsberichts
Dokumentation/aktuell/Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md).

WARUM AUS MESSUNG. VDI 6002 fuehrt fuer Hotels weder Bedarfswerte noch Profile (die lokale
QUELLE.txt der Normtabellen sagt es ausdruecklich); eine Ableitung nach ZU19 gibt es deshalb nicht.
Die einzige offen lizenzierte Quelle mit gemessenen Hotels, die dem Projekt vorliegt, ist:

    Sorensen, A. L. et al.: Measurement data on domestic hot water consumption and related energy
    use in hotels, nursing homes and apartment buildings in Norway. Mendeley Data V2,
    doi:10.17632/m3xy22pf4j.2; Beschreibung Data in Brief 37 (2021) 107228,
    doi:10.1016/j.dib.2021.107228 - CC BY 4.0.

NUR LOKAL LAUFFAEHIG. Die Rohdateien (HO1_2.csv, HO2_2.csv, HO4_2.csv) liegen ausserhalb des
Repositoriums (Vorgabe C:\\Waermeplan\\Messreihen_extern\\norwegen_mendeley). Committet wird allein die
JSON-Datei mit GERUNDETEN Kennwerten: Tagesbedarf je Zimmer (niedrig, mittel, hoch), sieben
Wochenanteile und je Tagtyp 24 Stundenanteile - Verhaeltnis- und Kennwerte, keine gemessene Menge
eines Gebaeudes, keine Stundenreihe. Das Katalogskript tww_testkatalog_fiktiv.py liest nur die
JSON-Datei und laeuft ohne die Rohdaten.

DIE REGEL (deterministisch, wiederholbar):

  1. Je Hotel dasselbe Fenster wie der Konverter Werkzeuge/ZapfprofilValidierung/Konverter/norwegen.py
     (Kalenderjahr mit den meisten Werten, negative Stundenwerte auf 0, laengstes Fenster mit hoechstens
     5 % Luecken) - aber allein der Kanal Q_chw (Zapfenergie). Die Zirkulation Q_hwc bleibt draussen:
     Der Katalog fuehrt den Bedarf an der Zapfstelle (Bilanzgrenze 1), die Zirkulation rechnet der
     Generator selbst. HO3 fehlt, weil der Konverter es mangels Fenster uebergeht.
  2. Nur vollstaendige Tage (24 Stunden). Tagtyp: Montag bis Freitag Werktag, Samstag, Sonntag und
     die gesetzlichen Feiertage Norwegens (gemeinsam.feiertage_datum) Sonntag; Ruhetag = Sonntag.
  3. Je Hotel wie Messkalibrierung.Nichtwohnparameter im Kern: Tagesbedarf je Zimmer = Mittel der
     Tagessummen / Zimmerzahl (Tabelle 1 der Beschreibung); Wochenanteile f_i = m_i / Summe m_j mit
     m_i = Mittel der Tagessummen des Wochentags i; Stundenanteile je Tagtyp als kleinste Quadrate
     a_h = Summe(Q_t * x_t,h) / Summe(Q_t^2).
  4. Ueber die drei Hotels: Wochen- und Stundenanteile als ungewichtetes Mittel der drei Hotels
     (jedes Hotel zaehlt gleich, gleich wie gross es ist), danach auf Summe 1 gebracht;
     Tagesbedarf mittel = Mittel der drei Hotels, niedrig = kleinstes, hoch = groesstes.
  5. Gerundet: Tagesbedarf auf 0,1 kWh je Zimmer und Tag, Wochen- und Stundenanteile auf drei
     Nachkommastellen (das Katalogskript normiert beim Einspielen exakt auf Summe 1). Monatsfaktoren
     gibt es nicht - jede Reihe umfasst sechs bis zwanzig Wochen eines Jahres; der Jahresgang ist
     flach (alle zwoelf Faktoren 1), eine MODELLANNAHME.

MODELLANNAHMEN, die der Katalogeintrag traegt (Herkunftsart EIGENKONSTRUKTION): drei Hotels eines
Landes und einer Region als Mittel fuer "Hotel"; ein Zimmer gilt als ein Bett (Bezugsart Betten,
wie im Validierungslauf); flacher Jahresgang; Bezugstemperaturen 60/12 Grad C wie die abgeleiteten
VDI-Zeilen (die Quelle misst Energie, die Umrechnung auf eine andere Zapftemperatur ist die des
Generators).

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/hotel_aus_messung_bauen.py [--quelle <ordner>] [--ziel <json>]
Die Ausgabe nennt nur die gerundeten Kennwerte der JSON-Datei und Zaehlungen, keine Messmenge.
"""

import argparse
import csv
import datetime
import io
import json
import os
import sys

HIER = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HIER))
sys.path.insert(0, os.path.join(REPO, "Werkzeuge", "ZapfprofilValidierung", "Konverter"))
import gemeinsam as g  # noqa: E402

QUELLE_VORGABE = r"C:\Waermeplan\Messreihen_extern\norwegen_mendeley"
ZIEL_VORGABE = os.path.join(HIER, "tww_hotel_aus_messung.json")

# Die drei Hotels des Validierungslaufs und ihre Zimmerzahl (Tabelle 1 der Beschreibung, wie
# norwegen.STAMMDATEN). HO3 uebergeht der Konverter (kein Fenster von 30 Tagen).
HOTELS = (("HO1", 434), ("HO2", 355), ("HO4", 151))

TAGTYPEN = ("werktag", "samstag", "sonntag")
WOCHENTAGE = ("mo", "di", "mi", "do", "fr", "sa", "so")


def reihe_lesen(pfad):
    """Die Stundenwerte des Kanals Q_chw als Liste (datetime, kWh)."""
    with io.open(pfad, encoding="utf-8-sig", newline="") as f:
        leser = csv.reader(f)
        namen = [k.strip() for k in next(leser)]
        i_chw = namen.index("Q_chw")
        werte = []
        for zeile in leser:
            if not zeile or len(zeile) <= i_chw or zeile[i_chw].strip() == "":
                continue
            z = datetime.datetime.strptime(zeile[0].strip(), "%Y-%m-%d %H:%M:%S")
            werte.append((z, float(zeile[i_chw])))
    return werte


def hotel_auswerten(pfad, zimmer):
    werte = reihe_lesen(pfad)
    jahr = g.jahr_mit_meisten(werte)
    werte = g.auf_jahr(werte, jahr)
    werte, _ = g.nullsetzen(werte)
    teil, _ = g.fenster(werte, 60)
    if teil is None:
        raise SystemExit(pfad + ": kein Fenster von %d Tagen" % g.MINDESTTAGE)
    feiertage = set(g.feiertage_datum("NO", jahr))

    je_tag = {}
    for z, w in teil:
        gang = je_tag.setdefault(z.date(), {})
        gang[z.hour] = gang.get(z.hour, 0.0) + w
    tage = {d: [gang[h] for h in range(24)] for d, gang in sorted(je_tag.items()) if len(gang) == 24}

    summen = {d: sum(x) for d, x in tage.items()}
    tagesbedarf = sum(summen.values()) / len(summen) / zimmer

    m = []
    for i in range(7):
        s = [q for d, q in summen.items() if d.weekday() == i]
        m.append(sum(s) / len(s) if s else None)
    ohne = [x for x in m if x is not None]
    m = [x if x is not None else sum(ohne) / len(ohne) for x in m]
    woche = [x / sum(m) for x in m]

    def tagtyp(d):
        if d in feiertage or d.weekday() == 6:
            return "sonntag"
        return "samstag" if d.weekday() == 5 else "werktag"

    gaenge, zahl = {}, {}
    for t in TAGTYPEN:
        auswahl = [d for d in tage if tagtyp(d) == t]
        zahl[t] = len(auswahl)
        nenner = sum(summen[d] ** 2 for d in auswahl)
        gaenge[t] = [sum(summen[d] * tage[d][h] for d in auswahl) / nenner for h in range(24)]
    return dict(tagesbedarf=tagesbedarf, woche=woche, gaenge=gaenge, tage=len(tage), zahl=zahl, jahr=jahr)


def gerundet(werte, stellen):
    """Auf `stellen` Nachkommastellen, nachdem die Werte exakt auf Summe 1 gebracht sind."""
    s = sum(werte)
    return [round(w / s, stellen) for w in werte]


def main():
    p = argparse.ArgumentParser(description="Katalogkennwerte 'Hotel (aus Messung)' bilden (CC BY 4.0).")
    p.add_argument("--quelle", default=QUELLE_VORGABE, help="Ordner mit HO1_2.csv, HO2_2.csv, HO4_2.csv")
    p.add_argument("--ziel", default=ZIEL_VORGABE)
    a = p.parse_args()

    je_hotel = [hotel_auswerten(os.path.join(a.quelle, k + "_2.csv"), n) for k, n in HOTELS]
    bedarf = [h["tagesbedarf"] for h in je_hotel]
    woche = gerundet([sum(h["woche"][i] for h in je_hotel) / 3.0 for i in range(7)], 3)
    gaenge = {t: gerundet([sum(h["gaenge"][t][s] for h in je_hotel) / 3.0 for s in range(24)], 3)
              for t in TAGTYPEN}

    ergebnis = {
        "kopf": {
            "nutzungsart": "Hotel (aus Messung)",
            "quelle": "Mittel aus drei Hotels, Sørensen et al. 2021, doi:10.1016/j.dib.2021.107228",
            "ausgabe": "Data in Brief 37 (2021) 107228, Messdaten Mendeley Data V2 (CC BY 4.0)",
            "daten": "Mendeley Data V2, doi:10.17632/m3xy22pf4j.2 (CC BY 4.0), Kanal Q_chw, Hotels HO1, HO2, HO4",
            "herkunftsart": "EIGENKONSTRUKTION",
            "regel": "Referenzlaeufe/Skripte/hotel_aus_messung_bauen.py (Kopf)",
            "bezug": "Zimmer, als Betten (Bezugsart 3); Bezugstemperaturen 60/12 Grad C",
            "einheit_bedarf": "kWh je Zimmer und Tag, gerundet auf 0,1",
            "modellannahmen": [
                "drei Hotels einer Region als Mittel fuer die Nutzungsart Hotel",
                "ein Zimmer gilt als ein Bett",
                "flacher Jahresgang (Monatsfaktoren 1), die Reihen umfassen nur Wochen eines Jahres",
                "niedrig und hoch sind das kleinste und das groesste der drei Hotels"
            ]
        },
        "bedarf": {
            "niedrig": round(min(bedarf), 1),
            "mittel": round(sum(bedarf) / 3.0, 1),
            "hoch": round(max(bedarf), 1)
        },
        "wochenanteile": dict(zip(WOCHENTAGE, woche)),
        "tagesprofile": gaenge
    }
    with open(a.ziel, "w", encoding="utf-8", newline="\r\n") as f:
        json.dump(ergebnis, f, ensure_ascii=False, indent=2)
        f.write("\n")

    print("Hotel (aus Messung): %d Hotels, volle Tage je Hotel %s"
          % (len(je_hotel), ", ".join(str(h["tage"]) for h in je_hotel)))
    print("  Tage je Tagtyp: %s" % "; ".join(
        "%s %s" % (k, "/".join(str(h["zahl"][t]) for t in TAGTYPEN)) for (k, _), h in zip(HOTELS, je_hotel)))
    print("  Bedarf niedrig/mittel/hoch (gerundet): %s" % ergebnis["bedarf"])
    print("Geschrieben: " + a.ziel)
    return 0


if __name__ == "__main__":
    sys.exit(main())
