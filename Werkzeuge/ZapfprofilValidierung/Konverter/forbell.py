# -*- coding: utf-8 -*-
"""Konverter: NREL/OpenEI-Messreihen zweier Mehrfamilienhäuser in New York → `Messreihenleser`.

Quelle (CC BY 4.0, Namensnennung in jedem Bericht):
NREL/OpenEI, *Domestic hot water distribution system losses and demand control* (Forbell),
doi:10.25984/2204257 — `DHW_1101_Forbell_EA_Analysis.xlsx` und `DHW_922_Forbell_EA_Analysis.xlsx`:
zwei Mehrfamilienhäuser mit zentraler Trinkwassererwärmung und Zirkulation, 5-Minuten-Werte von
Juni 2013 bis April 2014.

Aufbau der Quelle: je **Woche** ein Arbeitsblatt, dessen Name das Startdatum und die Regelstrategie
nennt (`C` continuous, `D` demand control, `TM` temperature modulation). Spalten unter anderem `SPR`
(Pumpenstatus), `TS`/`TS1`/`TS2` (Vorlauf), `TR` (Rücklauf), `TC` (Kaltwasser, °F), `FMW`/`FMW1`
(Wasserzähler), `QB1`/`QB2` (Kesselenergie), **`QU` (useful delivered energy)** und `QDL`
(Verteilverluste).

**Genommen wird `QU`** — die an den Zapfstellen abgegebene Energie, in **Btu je 5 Minuten**. Die
Einheit ist nicht angeschrieben; sie folgt aus der Probe gegen den Wasserzähler: Wochensumme `QU`
geteilt durch (Wochensumme `FMW` in Gallonen) ergibt eine Spreizung von etwa 55 K und passt damit zu
`TS` ≈ 160 °F und `TC` ≈ 60 °F. Die Regelstrategie der Woche wirkt auf die **Verteilverluste**, nicht
auf die Zapfungen; die Wochen werden deshalb ohne Unterscheidung aneinandergesetzt.

**Bilanzgrenze `Zapfstelle`, ohne Zirkulation**: `QU` schließt `QDL` nicht ein. Das Objekt rechnet
deshalb mit `zirkulation: false`, damit beide Seiten des Vergleichs dieselbe Grenze haben.

**Das Excel-Format wird ohne Zusatzpaket gelesen** — eine `.xlsx` ist ein ZIP mit XML, und gebraucht
werden nur Blattnamen, Zahlen und die gemeinsamen Zeichenketten. Ein Paket wie `openpyxl` liegt auf
diesem Rechner nicht, und ein Werkzeug des Repositoriums soll keine Abhängigkeit mitbringen, die nur
es selbst braucht.

Aufruf:
    py Werkzeuge/ZapfprofilValidierung/Konverter/forbell.py --quelle <ordner> --ziel <ordner>
"""

import argparse
import datetime
import os
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gemeinsam as g

M = "{http://schemas.openxmlformats.org/spreadsheetml/2006/main}"
R = "{http://schemas.openxmlformats.org/officeDocument/2006/relationships}"

QUELLENSATZ = ("NREL/OpenEI, Forbell DHW distribution losses and demand control, "
               "doi:10.25984/2204257 (CC BY 4.0), 5-Minuten-Werte, Ortszeit New York")

NUTZUNGSART = "Wohnen groß (abgeleitet)"

# STAMMDATEN - Wohnungszahl aus der Building America Case Study „Control Retrofits for Multifamily
# Domestic Hot Water Recirculation Systems, Brooklyn, New York", DOE/GO-102016-4704 (Dezember 2016):
# „Each building included approximately 50 apartments and was three stories tall." Der Katalog
# rechnet Wohnen in Personen; die Belegung 2,5 Personen je Wohnung ist eine ANNAHME.
WOHNUNGEN = {"US-922": 50, "US-1101": 50}
PERSONEN_JE_WOHNUNG = 2.5
WOCHENBLATT = re.compile(r"^\d{1,2}-\d{1,2}[A-Za-z+]*$")
EXCEL_NULL = datetime.datetime(1899, 12, 30)


def blattliste(archiv):
    """Die Blätter als (Name, Pfad im Archiv), in der Reihenfolge der Arbeitsmappe."""
    rels = {r.get("Id"): r.get("Target")
            for r in ET.fromstring(archiv.read("xl/_rels/workbook.xml.rels"))}
    wb = ET.fromstring(archiv.read("xl/workbook.xml"))
    aus = []
    for s in wb.find(M + "sheets"):
        ziel = rels[s.get(R + "id")].lstrip("/")
        aus.append((s.get("name"), ziel if ziel.startswith("xl/") else "xl/" + ziel))
    return aus


def zeichenketten(archiv):
    try:
        wurzel = ET.fromstring(archiv.read("xl/sharedStrings.xml"))
    except KeyError:
        return []
    return ["".join(t.text or "" for t in si.iter(M + "t")) for si in wurzel]


def blatt_lesen(archiv, pfad, ketten):
    """Das Blatt als Liste von Zeilen; jede Zeile ist ein Wörterbuch Spaltenbuchstabe → Text."""
    wurzel = ET.fromstring(archiv.read(pfad))
    daten = wurzel.find(M + "sheetData")
    zeilen = []
    for zeile in daten if daten is not None else []:
        z = {}
        for c in zeile:
            spalte = "".join(ch for ch in (c.get("r") or "") if ch.isalpha())
            v = c.find(M + "v")
            text = v.text if v is not None else None
            if c.get("t") == "s" and text not in (None, ""):
                text = ketten[int(text)]
            z[spalte] = text
        zeilen.append(z)
    return zeilen


def spalte_von(kopf, name):
    """Der Spaltenbuchstabe mit dieser Kopfzeilenbeschriftung; <c>None</c> = keine."""
    for buchstabe, text in kopf.items():
        if (text or "").strip() == name:
            return buchstabe
    return None


def zahl(text):
    try:
        return float(text)
    except (TypeError, ValueError):
        return None


def werte_der_woche(zeilen):
    """Die 5-Minuten-Werte eines Wochenblatts als Liste (datetime, kWh) aus `QU` [Btu]."""
    if len(zeilen) < 2:
        return [], "leeres Blatt"
    kopf = zeilen[0]
    sp_qu = spalte_von(kopf, "QU")
    if sp_qu is None:
        return [], "keine Spalte QU"
    # Die dritte Spalte traegt Datum UND Uhrzeit als Excel-Serienzahl; sonst Datum plus Uhrzeit.
    sp_zeit = None
    for buchstabe in ("C", "A"):
        if buchstabe in kopf:
            sp_zeit = buchstabe
            break
    if sp_zeit is None:
        return [], "keine Zeitspalte"
    aus = []
    for z in zeilen[1:]:
        serie = zahl(z.get(sp_zeit))
        menge = zahl(z.get(sp_qu))
        if serie is None or menge is None:
            continue
        zeit = EXCEL_NULL + datetime.timedelta(days=serie)
        # Auf die Minute runden - die Serienzahl traegt Rundungsreste.
        zeit = (zeit + datetime.timedelta(seconds=30)).replace(second=0, microsecond=0)
        aus.append((zeit, max(0.0, menge) * g.BTU_JE_KWH))
    return aus, None


def haus(archivpfad):
    """Die Kennung des Hauses aus dem Dateinamen (`DHW_922_…` → `US-922`)."""
    teile = os.path.basename(archivpfad).split("_")
    return "US-" + (teile[1] if len(teile) > 1 else "MFH")


def hauptlauf(quelle, ziel):
    archive = sorted(d for d in os.listdir(quelle) if d.lower().endswith(".xlsx"))
    if not archive:
        print("Unter %s liegt keine .xlsx." % quelle)
        return 2
    geschrieben, uebergangen = 0, []
    for datei in archive:
        kennung = haus(datei)
        alle, negativ, ohne = [], 0, []
        with zipfile.ZipFile(os.path.join(quelle, datei)) as archiv:
            ketten = zeichenketten(archiv)
            for name, pfad in blattliste(archiv):
                if not WOCHENBLATT.match(name):
                    continue
                werte, grund = werte_der_woche(blatt_lesen(archiv, pfad, ketten))
                if grund:
                    ohne.append("%s (%s)" % (name, grund))
                    continue
                alle.extend(werte)
        if not alle:
            uebergangen.append((kennung, "kein Wochenblatt mit QU"))
            continue

        alle.sort(key=lambda p: p[0])
        # Doppelte Zeitstempel (ueberlappende Wochenblaetter) fallen weg; der spaetere Wert gilt.
        entdoppelt, vorher = [], None
        for z, w in alle:
            if z == vorher:
                entdoppelt[-1] = (z, w)
            else:
                entdoppelt.append((z, w))
                vorher = z
        stunden = g.stunden_summieren(entdoppelt, 5)
        jahr = g.jahr_mit_meisten(stunden)
        stunden = g.auf_jahr(stunden, jahr)
        teil, anteil = g.fenster(stunden, 60)
        if teil is None:
            uebergangen.append((kennung, "laengstes Fenster mit hoechstens %.0f %% Luecken nur %.1f d, "
                                "gebraucht werden %d d"
                                % (g.LUECKENANTEIL * 100, anteil or 0.0, g.MINDESTTAGE)))
            continue

        vermerk = ("New York, Mehrfamilienhaus mit Zirkulation; Kanal QU (useful delivered energy, "
                   "Btu je 5 min in kWh, Verteilverluste QDL NICHT enthalten); Wochenblaetter "
                   "aneinandergesetzt, Stunden nur bei allen zwoelf Schritten; Bezugsmenge etwa %d "
                   "Wohnungen (DOE/GO-102016-4704) mal %.1f Personen (Annahme); Bundesfeiertage USA %d.%s"
                   % (WOHNUNGEN.get(kennung, 50), PERSONEN_JE_WOHNUNG, jahr,
                      (" Ohne Werte: " + ", ".join(ohne) + ".") if ohne else ""))
        ordner = os.path.join(ziel, kennung)
        g.reihe_schreiben(os.path.join(ordner, "messreihe.csv"), g.KOPF_ENERGIE, teil)
        g.objekt_schreiben(os.path.join(ordner, "objekt.json"), kennung, NUTZUNGSART,
                           WOHNUNGEN.get(kennung, 50) * PERSONEN_JE_WOHNUNG, "Energie", "Ortszeit",
                           jahr, "Zapfstelle", vermerk, QUELLENSATZ, zirkulation=False,
                           herkunft="Abgeleitet" if kennung in WOHNUNGEN else "Platzhalter",
                           land="US", region="USA, Bundesfeiertage")
        print(g.bericht(kennung, teil, anteil, negativ,
                        "Jahr %d, %d Wochenblaetter ohne Werte" % (jahr, len(ohne))))
        geschrieben += 1

    for kennung, grund in uebergangen:
        print("%-14s uebergangen: %s" % (kennung, grund))
    print("Geschrieben: %d Objekte, uebergangen: %d." % (geschrieben, len(uebergangen)))
    return 0 if geschrieben else 5


if __name__ == "__main__":
    p = argparse.ArgumentParser(description="Forbell-DHW-Messreihen umsetzen (CC BY 4.0).")
    p.add_argument("--quelle", required=True, help="Ordner mit den beiden .xlsx")
    p.add_argument("--ziel", required=True, help="Zielordner AUSSERHALB des Repositoriums")
    a = p.parse_args()
    sys.exit(hauptlauf(a.quelle, a.ziel))
