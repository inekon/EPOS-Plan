#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Messskript PV-Modulkatalog EPOS-Plan (Tab_PV_STAMM / Tab_PV).

Aufruf:  python messung_pv_katalog.py <db-pfad> [--md <ausgabe.md>]
         [--csv-dir <verzeichnis>] [--csv-praefix <praefix>] [--kein-csv]

Oeffnet die Datenbank IMMER nur lesend (URI mode=ro) und veraendert nichts.

Klassifiziert je Zeile und je Feld (alpha_SC, beta_OC, gamma_PMP, T_NOCT):
  =I_Kurzschluss  Wert != 0 und exakt gleich I_Kurzschluss (Kopierfehler)
  NULL            Feld ist NULL (die Simulation liest NULL als 0)
  0               Feld ist 0 (Koeffizient fehlt)
  plausibel       Wert liegt im Plausibilitaetsfenster
  unplausibel     Wert liegt ausserhalb des Fensters
"""

import argparse
import csv
import os
import sqlite3
import sys
from datetime import datetime

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

FELDER = ("alpha_SC", "beta_OC", "gamma_PMP", "T_NOCT")

# Plausibilitaetsfenster laut Fachvorgabe
FENSTER = {
    "alpha_SC":  ("A/K", "0 < x <= 0,05 (typisch 0,002...0,006)",
                  lambda v: 0.0 < v <= 0.05),
    "beta_OC":   ("V/K", "-0,5 <= x < 0 (typisch -0,10...-0,15)",
                  lambda v: -0.5 <= v < 0.0),
    "gamma_PMP": ("%/K", "-1,0 <= x < 0 (typisch -0,30...-0,45)",
                  lambda v: -1.0 <= v < 0.0),
    "T_NOCT":    ("Grad C", "20 <= x <= 60 (typisch 42...48; 0/NULL = nicht vorhanden)",
                  lambda v: 20.0 <= v <= 60.0),
}

KLASSEN = ("plausibel", "=I_Kurzschluss", "0", "NULL", "unplausibel")

ANZEIGE_BASIS = ("ID", "ID_Projekt", "Bezeichner", "I_Kurzschluss")
ANZEIGE_ENDE = ("Laenge", "Breite")


def klassifiziere(feld, wert, i_kurzschluss):
    """Liefert die Klasse eines Feldwertes."""
    if wert is None:
        return "NULL"
    try:
        w = float(wert)
    except (TypeError, ValueError):
        return "unplausibel"
    if w != 0.0 and i_kurzschluss is not None:
        try:
            if float(i_kurzschluss) == w:
                return "=I_Kurzschluss"
        except (TypeError, ValueError):
            pass
    if w == 0.0:
        return "0"
    if FENSTER[feld][2](w):
        return "plausibel"
    return "unplausibel"


def fmt(v):
    """Wert fuer die Ausgabe (Punkt-Dezimal, NULL sichtbar)."""
    if v is None:
        return "NULL"
    if isinstance(v, float):
        return repr(v)
    return str(v)


def csv_wert(v):
    if v is None:
        return ""
    if isinstance(v, float):
        return repr(v)
    return str(v)


def oeffne_ro(db_pfad):
    """Nur-Lese-Verbindung ueber URI (mode=ro)."""
    p = os.path.abspath(db_pfad).replace("\\", "/")
    uri = "file:" + p.replace("?", "%3f").replace("#", "%23") + "?mode=ro"
    return sqlite3.connect(uri, uri=True)


def spalten(con, tabelle):
    return [r[1] for r in con.execute("PRAGMA table_info(%s)" % tabelle)]


def messe(db_pfad, con=None):
    """Liest beide Tabellen und klassifiziert alle Felder.

    Ohne 'con' wird read-only geoeffnet. Mit 'con' (z. B. offene Transaktion
    des Reparatur-Runners) wird auf dieser Verbindung gemessen.

    Rueckgabe: dict tabelle -> {"spalten": [...],
               "zeilen": [{"werte": {...}, "klassen": {feld: klasse}}, ...]}
    """
    eigen = con is None
    if eigen:
        con = oeffne_ro(db_pfad)
    try:
        ergebnis = {}
        for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
            cols = spalten(con, tabelle)
            zeilen = []
            for row in con.execute("SELECT * FROM %s ORDER BY ID" % tabelle):
                werte = dict(zip(cols, row))
                isc = werte.get("I_Kurzschluss")
                klassen = dict((f, klassifiziere(f, werte.get(f), isc)) for f in FELDER)
                zeilen.append({"werte": werte, "klassen": klassen})
            ergebnis[tabelle] = {"spalten": cols, "zeilen": zeilen}
        return ergebnis
    finally:
        if eigen:
            con.close()


def reparaturbeduerftig(zeile):
    """(beduerftig, nur_t_noct_null) fuer eine Zeile."""
    abweichungen = [f for f in FELDER if zeile["klassen"][f] != "plausibel"]
    if not abweichungen:
        return False, False
    nur_tnoct = (abweichungen == ["T_NOCT"] and zeile["klassen"]["T_NOCT"] == "0")
    return True, nur_tnoct


def anzeige_spalten(cols):
    out = [c for c in ANZEIGE_BASIS if c in cols]
    out += list(FELDER)
    out += [c for c in ANZEIGE_ENDE if c in cols]
    return out


def markdown_bericht(daten, db_pfad, titel=None):
    L = []
    A = L.append
    A("# %s" % (titel or "Messung PV-Modulkatalog"))
    A("")
    A("- Datenbank: %s" % os.path.abspath(db_pfad))
    A("- Gemessen: %s (read-only, URI mode=ro)"
      % datetime.now().strftime("%d.%m.%Y %H:%M:%S"))
    A("- Skript: %s" % os.path.abspath(__file__))
    A("")
    A("## Plausibilitaetsfenster")
    A("")
    A("| Feld | Einheit | Fenster |")
    A("|---|---|---|")
    for f in FELDER:
        einheit, text, _ = FENSTER[f]
        A("| %s | %s | %s |" % (f, einheit, text))
    A("")
    A("Klassen: =I_Kurzschluss (Wert != 0 und exakt gleich I_Kurzschluss), "
      "NULL, 0, plausibel, unplausibel.")
    A("")

    for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
        block = daten[tabelle]
        cols = anzeige_spalten(block["spalten"])
        A("## %s (%d Zeilen)" % (tabelle, len(block["zeilen"])))
        A("")
        A("| " + " | ".join(cols) + " |")
        A("|" + "---|" * len(cols))
        for z in block["zeilen"]:
            zellen = []
            for c in cols:
                if c in FELDER:
                    zellen.append("%s [%s]" % (fmt(z["werte"].get(c)), z["klassen"][c]))
                else:
                    zellen.append(fmt(z["werte"].get(c)))
            A("| " + " | ".join(zellen) + " |")
        A("")

    A("## Zusammenfassung (Anzahl Zeilen je Klasse je Feld)")
    A("")
    for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
        block = daten[tabelle]
        A("### %s (%d Zeilen)" % (tabelle, len(block["zeilen"])))
        A("")
        A("| Feld | " + " | ".join(KLASSEN) + " |")
        A("|" + "---|" * (len(KLASSEN) + 1))
        for f in FELDER:
            zaehler = dict((k, 0) for k in KLASSEN)
            for z in block["zeilen"]:
                zaehler[z["klassen"][f]] += 1
            A("| %s | %s |" % (f, " | ".join(str(zaehler[k]) for k in KLASSEN)))
        A("")

    A("### Beide Tabellen zusammen")
    A("")
    A("| Feld | " + " | ".join(KLASSEN) + " |")
    A("|" + "---|" * (len(KLASSEN) + 1))
    for f in FELDER:
        zaehler = dict((k, 0) for k in KLASSEN)
        for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
            for z in daten[tabelle]["zeilen"]:
                zaehler[z["klassen"][f]] += 1
        A("| %s | %s |" % (f, " | ".join(str(zaehler[k]) for k in KLASSEN)))
    A("")

    A("## Reparaturbeduerftig")
    A("")
    A("Zeilen mit mindestens einer Klasse ungleich plausibel. Bei T_NOCT gilt der "
      "Wert 0 als hinnehmbar (Koeffizient in der Quelle nicht vorhanden), wird aber "
      "ausgewiesen.")
    A("")
    A("| Tabelle | ID | Bezeichner | betroffene Felder (Klasse) | Handlungsbedarf |")
    A("|---|---|---|---|---|")
    gesamt = 0
    nur_tnoct_zeilen = 0
    for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
        for z in daten[tabelle]["zeilen"]:
            bed, nur_tnoct = reparaturbeduerftig(z)
            if not bed:
                continue
            gesamt += 1
            if nur_tnoct:
                nur_tnoct_zeilen += 1
            felder = ", ".join(
                "%s=%s [%s]" % (f, fmt(z["werte"].get(f)), z["klassen"][f])
                for f in FELDER if z["klassen"][f] != "plausibel")
            A("| %s | %s | %s | %s | %s |" % (
                tabelle, fmt(z["werte"]["ID"]), z["werte"].get("Bezeichner", ""),
                felder,
                "nur T_NOCT=0 (hinnehmbar)" if nur_tnoct else "ja"))
    if gesamt == 0:
        A("| - | - | - | keine | - |")
    A("")
    A("**%d reparaturbeduerftige Zeile(n)**, davon %d nur wegen T_NOCT = 0 "
      "(hinnehmbar), also %d mit echtem Handlungsbedarf."
      % (gesamt, nur_tnoct_zeilen, gesamt - nur_tnoct_zeilen))
    A("")
    return "\n".join(L)


def schreibe_csv(daten, verzeichnis, praefix=""):
    pfade = []
    for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
        block = daten[tabelle]
        pfad = os.path.join(verzeichnis, "%s%s.csv" % (praefix, tabelle))
        with open(pfad, "w", encoding="utf-8", newline="") as f:
            w = csv.writer(f, delimiter=";", lineterminator="\r\n")
            w.writerow(block["spalten"])
            for z in block["zeilen"]:
                w.writerow([csv_wert(z["werte"].get(c)) for c in block["spalten"]])
        pfade.append(pfad)
    return pfade


def main(argv=None):
    p = argparse.ArgumentParser(description="Messung PV-Modulkatalog (read-only)")
    p.add_argument("db", help="Pfad zur SQLite-Datenbank (wird nur gelesen)")
    p.add_argument("--md", help="Pfad der Markdown-Ausgabe (sonst nach stdout)")
    p.add_argument("--csv-dir", dest="csv_dir",
                   help="Verzeichnis fuer den CSV-Vollabzug (Vorgabe: Verzeichnis von --md)")
    p.add_argument("--csv-praefix", dest="csv_praefix", default="",
                   help="Praefix der CSV-Dateinamen (Vorgabe: leer)")
    p.add_argument("--kein-csv", dest="kein_csv", action="store_true",
                   help="keinen CSV-Vollabzug schreiben")
    p.add_argument("--titel", help="Ueberschrift des Berichts")
    a = p.parse_args(argv)

    if not os.path.isfile(a.db):
        print("FEHLER: Datenbank nicht gefunden: %s" % a.db, file=sys.stderr)
        return 2

    daten = messe(a.db)
    text = markdown_bericht(daten, a.db, a.titel)

    if a.md:
        os.makedirs(os.path.dirname(os.path.abspath(a.md)) or ".", exist_ok=True)
        with open(a.md, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print("Markdown geschrieben: %s" % os.path.abspath(a.md))
    else:
        print(text)

    if not a.kein_csv:
        ziel = a.csv_dir or (os.path.dirname(os.path.abspath(a.md)) if a.md else os.getcwd())
        os.makedirs(ziel, exist_ok=True)
        for pfad in schreibe_csv(daten, ziel, a.csv_praefix):
            print("CSV geschrieben:      %s" % os.path.abspath(pfad))
    return 0


if __name__ == "__main__":
    sys.exit(main())
