# -*- coding: utf-8 -*-
"""Konverter: hihAigua (Zenodo) → Format des `Messreihenleser`.

Quelle (CC BY 4.0, Namensnennung in jedem Bericht):
*hihAigua dataset*, Zenodo, doi:10.5281/zenodo.18456405 — zehn spanische Wohnhäuser
(`installation_0` … `installation_9`), Jahr 2025, Ablesungen etwa alle sieben Minuten.

Aufbau der Quelle: je Installation `devs.csv` (Spalten `_id`, `installation`, `asset`, `type`) und
`ts.csv` (`entity_id`, `key`, `ts`, `ts_dt`, `value`). Genommen wird die Einheit mit `type`
= `hotWater` und ihr Schlüssel `volume` — ein **kumulierter Zählerstand** in m³.

**Vom Zählerstand zur Stundenmenge.** Der Zuwachs zwischen zwei Ablesungen wird **zeitanteilig**
auf die Stunden verteilt, die das Intervall überdeckt; eine Stunde ohne Ablesung bekommt keine Zeile
und ist damit eine Lücke, die der Leser füllt und zählt. Ein *Rückwärtssprung* des Zählerstands
(Tausch, Rücksetzung) gilt als 0 und wird gezählt — ein negativer Zuwachs wäre keine Zapfung.

**Zwei Artefakte werden verworfen und gezählt** (erster Lauf: die Jahresspitze einiger Haushalte
war ein einzelnes Intervall mit dem Vielfachen der zweitgrößten Stunde):
* ein Intervall über `NACHHOLGRENZE_MIN` (Nachholwert nach einer Übertragungslücke — der Zuwachs
  gehört zu keiner bestimmten Stunde);
* ein mittlerer Durchfluss über `DURCHFLUSS_HOECHSTENS` Liter je Minute (unplausibel für einen
  Haushaltsstrang: Der Berechnungsdurchfluss einer Badewanne nach DIN EN 806-3 ist 0,3 l/s = 18 l/min).

**Die Zeitstempel sind UTC** (`ts` ist die Unix-Zeit in Millisekunden, `ts_dt` ihre Schreibweise).
Die Bewohner leben nach der Ortszeit; der Konverter rechnet jede Ablesung in **mitteleuropäische
Ortszeit** um (MEZ/MESZ, die Quelle nennt „Spain (GMT+1)") und schreibt `Ortszeit` — die doppelte
Stunde der Herbstumstellung steht zweimal in der Datei, wie der Leser sie erwartet.

**Bezugsmenge unbekannt.** Weder `devs.csv` noch die Beschreibung auf Zenodo nennen Bewohnerzahlen
(„10 Spanish homes … distributed across two different buildings"). Die Zahl in der `objekt.json` ist
deshalb nur ein Rechenwert (2,5 Personen, Herkunft `Unbekannt`); das Niveau kommt allein aus der
Kalibrierung, und die √N-Skalierung lässt die Objekte weg.

**Die Spreizung.** Die Reihe ist ein Volumen; die Energie folgt über θ_Zapf − θ̄_KW aus der
`objekt.json`. Genommen sind die Bezugstemperaturen des Katalogs (60/12 °C). Das ist eine Annahme —
sie wirkt aber auf **beide** Seiten des Vergleichs: Die gerechnete Jahresmenge trägt denselben
Temperaturfaktor, das Energieverhältnis ist gegen die Wahl der Spreizung fast unempfindlich.

Aufruf:
    py Werkzeuge/ZapfprofilValidierung/Konverter/hihaigua.py --archiv <zip> --ziel <ordner>
"""

import argparse
import csv
import datetime
import io
import os
import sys
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gemeinsam as g

QUELLENSATZ = ("hihAigua dataset, Zenodo, doi:10.5281/zenodo.18456405 (CC BY 4.0), "
               "Zaehlerstand hotWater/volume, Zeitstempel UTC, umgerechnet in MEZ/MESZ")

NUTZUNGSART = "Ein- und Zweifamilienhaus (abgeleitet)"
PERSONEN_RECHENWERT = 2.5
NACHHOLGRENZE_MIN = 120.0
DURCHFLUSS_HOECHSTENS = 20.0     # Liter je Minute


def hotwater_kennungen(archiv, ordner):
    """Die `entity_id` der Einheiten mit `type = hotWater` dieser Installation."""
    with archiv.open(ordner + "/devs.csv") as f:
        leser = csv.DictReader(io.TextIOWrapper(f, encoding="utf-8"))
        return {z["_id"] for z in leser if z.get("type") == "hotWater"}


def verteilen(eimer, von, bis, menge):
    """Verteilt `menge` zeitanteilig auf die UTC-Stunden des Intervalls (von, bis]."""
    dauer = (bis - von).total_seconds()
    if dauer <= 0.0:
        stunde = bis.replace(minute=0, second=0, microsecond=0)
        eimer[stunde] = eimer.get(stunde, 0.0) + menge
        return
    t = von
    while t < bis:
        stunde = t.replace(minute=0, second=0, microsecond=0)
        ende = min(bis, stunde + datetime.timedelta(hours=1))
        eimer[stunde] = eimer.get(stunde, 0.0) + menge * (ende - t).total_seconds() / dauer
        t = ende


def stundenmengen(archiv, ordner, kennungen):
    """Die Stundenmengen [m³] aus dem kumulierten Zählerstand in ORTSZEIT; liefert (Liste,
    Zaehlung). Eimer ist die UTC-Stunde; jede wird danach auf ihre Ortszeit gelegt — so bleiben die
    beiden UTC-Stunden der Herbstumstellung zwei Zeilen mit derselben Ortszeit."""
    ablesungen = []
    with archiv.open(ordner + "/ts.csv") as f:
        leser = csv.reader(io.TextIOWrapper(f, encoding="utf-8"))
        kopf = next(leser)
        i_id, i_key = kopf.index("entity_id"), kopf.index("key")
        i_ts, i_wert = kopf.index("ts"), kopf.index("value")
        for zeile in leser:
            if len(zeile) <= i_wert or zeile[i_id] not in kennungen or zeile[i_key] != "volume":
                continue
            try:
                stand = float(zeile[i_wert])
                zeit = datetime.datetime.fromtimestamp(int(zeile[i_ts]) / 1000.0,
                                                       datetime.timezone.utc).replace(tzinfo=None)
            except (ValueError, OverflowError, OSError):
                continue
            ablesungen.append((zeit, stand))

    # In der Zeitfolge, doppelte Zeitstempel einmal: Die Datei ist nicht zwingend sortiert.
    ablesungen.sort()
    eimer, vorher, vorzeit = {}, None, None
    zaehl = {"ruecksprung": 0, "nachhol": 0, "unplausibel": 0}
    for zeit, stand in ablesungen:
        if vorher is not None and zeit > vorzeit:
            zuwachs = stand - vorher
            minuten = (zeit - vorzeit).total_seconds() / 60.0
            if zuwachs < 0.0:
                zaehl["ruecksprung"] += 1
                zuwachs = 0.0
            if minuten > NACHHOLGRENZE_MIN:
                zaehl["nachhol"] += 1
            elif zuwachs * 1000.0 / minuten > DURCHFLUSS_HOECHSTENS:
                zaehl["unplausibel"] += 1
            else:
                verteilen(eimer, vorzeit, zeit, zuwachs)
        if vorzeit is None or zeit > vorzeit:
            vorher, vorzeit = stand, zeit
    return [(g.eu_ortszeit(u), w) for u, w in sorted(eimer.items())], zaehl


def hauptlauf(archivpfad, ziel):
    if not os.path.isfile(archivpfad):
        print("Das Archiv %s gibt es nicht." % archivpfad)
        return 2
    geschrieben, uebergangen = 0, []
    with zipfile.ZipFile(archivpfad) as archiv:
        ordner = sorted({n.split("/")[0] for n in archiv.namelist()
                         if n.startswith("installation_") and "/" in n})
        for o in ordner:
            kennung = "ES-" + o.replace("installation_", "EFH")
            try:
                ids = hotwater_kennungen(archiv, o)
            except KeyError:
                uebergangen.append((kennung, "keine devs.csv"))
                continue
            if not ids:
                uebergangen.append((kennung, "keine Einheit mit type = hotWater"))
                continue
            werte, zaehl = stundenmengen(archiv, o, ids)
            if not werte:
                uebergangen.append((kennung, "keine Ablesung mit key = volume"))
                continue
            jahr = g.jahr_mit_meisten(werte)
            werte = g.auf_jahr(werte, jahr)
            teil, anteil = g.fenster(werte, 60)
            if teil is None:
                uebergangen.append((kennung, "laengstes Fenster mit hoechstens %.0f %% Luecken nur "
                                    "%.1f d, gebraucht werden %d d"
                                    % (g.LUECKENANTEIL * 100, anteil or 0.0, g.MINDESTTAGE)))
                continue
            if sum(w for _, w in teil) <= 0.0:
                uebergangen.append((kennung, "das Fenster traegt keine Menge"))
                continue

            vermerk = ("Spanien, Haushalt (zehn Haushalte in zwei Gebaeuden); Zaehler hotWater, "
                       "Zaehlerstand zeitanteilig in Stundenmengen, UTC in Ortszeit MEZ/MESZ; "
                       "Bewohnerzahl unbekannt, Bezugsmenge %.1f nur Rechenwert, Niveau aus der "
                       "Kalibrierung; %d Rueckspruenge auf 0, %d Nachholwerte und %d unplausible "
                       "Intervalle verworfen; landesweite Feiertage Spaniens %d; Spreizung aus den "
                       "Bezugstemperaturen des Katalogs."
                       % (PERSONEN_RECHENWERT, zaehl["ruecksprung"], zaehl["nachhol"],
                          zaehl["unplausibel"], jahr))
            unterordner = os.path.join(ziel, kennung)
            g.reihe_schreiben(os.path.join(unterordner, "messreihe.csv"), g.KOPF_VOLUMEN, teil)
            g.objekt_schreiben(os.path.join(unterordner, "objekt.json"), kennung, NUTZUNGSART,
                               PERSONEN_RECHENWERT, "Volumen", "Ortszeit", jahr, "Zapfstelle",
                               vermerk, QUELLENSATZ, zirkulation=False, herkunft="Unbekannt",
                               land="ES", region="Spanien, landesweite Feiertage")
            print(g.bericht(kennung, teil, anteil, zaehl["ruecksprung"],
                            "Jahr %d, Volumenreihe, %d Nachholwerte, %d unplausibel verworfen"
                            % (jahr, zaehl["nachhol"], zaehl["unplausibel"])))
            geschrieben += 1

    for kennung, grund in uebergangen:
        print("%-14s uebergangen: %s" % (kennung, grund))
    print("Geschrieben: %d Objekte, uebergangen: %d." % (geschrieben, len(uebergangen)))
    return 0 if geschrieben else 5


if __name__ == "__main__":
    p = argparse.ArgumentParser(description="hihAigua-Messreihen umsetzen (CC BY 4.0).")
    p.add_argument("--archiv", required=True, help="hihAigua_dataset.zip")
    p.add_argument("--ziel", required=True, help="Zielordner AUSSERHALB des Repositoriums")
    a = p.parse_args()
    sys.exit(hauptlauf(a.archiv, a.ziel))
