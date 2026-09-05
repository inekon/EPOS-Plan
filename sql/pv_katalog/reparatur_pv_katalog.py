#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Runner fuer die Reparatur des PV-Modulkatalogs (Tab_PV_STAMM / Tab_PV).

Aufruf:
  python reparatur_pv_katalog.py <db-pfad>                    Trockenlauf
  python reparatur_pv_katalog.py <db-pfad> --ausfuehren       Echtlauf

Trockenlauf (Vorgabe):
  - Vorbedingungen pruefen (Datei da, keine -wal/-shm, DB nicht gesperrt)
  - Namen der geforderten Sicherungskopie ankuendigen (es wird KEINE angelegt)
  - Vorher-Werte drucken
  - SQL in einer Transaktion ausfuehren, changes() je UPDATE drucken
  - Nachher-Werte drucken
  - ROLLBACK: die Datenbank bleibt unveraendert

Echtlauf (--ausfuehren):
  - wie oben, aber zuerst Sicherungskopie
    <db>.vor-pv-reparatur-<JJJJ-MM-TT>.bak (shutil.copy2)
  - danach COMMIT
  - anschliessend Kontrollmessung ueber messung_pv_katalog.py

Schutz der Produktivdatenbank:
  C:\\ProgramData\\EPOS_PLAN\\Kenndaten.sqlite wird verweigert, solange nicht
  zusaetzlich --produktiv-freigegeben uebergeben wird. Dieser Schalter ist
  ausschliesslich fuer den Anwender gedacht, der die Freigabe bewusst erteilt
  (EPOS-Plan geschlossen, Sicherung vorhanden).

Optionen:
  --sql <pfad>           anderes SQL-Skript (Vorgabe: reparatur_pv_katalog.sql
                         neben diesem Runner)
  --kontrolle-md <pfad>  Kontrollmessung nach dem Echtlauf als Markdown ablegen
  --produktiv-freigegeben  Freigabe fuer die Produktivdatenbank (siehe oben)
"""

import argparse
import os
import shutil
import sqlite3
import sys
from datetime import datetime

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

PRODUKTIV_DB = r"C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite"
FELDER = ("alpha_SC", "beta_OC", "gamma_PMP", "T_NOCT")


# --------------------------------------------------------------------------
# Messfunktion: bevorzugt aus messung_pv_katalog.py, sonst lokale Kopie
# --------------------------------------------------------------------------
def _lade_messmodul():
    hier = os.path.dirname(os.path.abspath(__file__))
    for kandidat in (hier,
                     os.path.join(hier, "..", "messung"),
                     os.path.join(os.path.dirname(hier), "messung")):
        kandidat = os.path.abspath(kandidat)
        if os.path.isfile(os.path.join(kandidat, "messung_pv_katalog.py")):
            if kandidat not in sys.path:
                sys.path.insert(0, kandidat)
            try:
                import messung_pv_katalog as m
                return m
            except Exception:
                pass
    return None


MESS = _lade_messmodul()

if MESS is not None:
    klassifiziere = MESS.klassifiziere
    fmt = MESS.fmt
else:  # Notfall-Duplikat der Klassifikation (identische Regeln)
    _FENSTER = {
        "alpha_SC": lambda v: 0.0 < v <= 0.05,
        "beta_OC": lambda v: -0.5 <= v < 0.0,
        "gamma_PMP": lambda v: -1.0 <= v < 0.0,
        "T_NOCT": lambda v: 20.0 <= v <= 60.0,
    }

    def klassifiziere(feld, wert, i_kurzschluss):
        if wert is None:
            return "NULL"
        try:
            w = float(wert)
        except (TypeError, ValueError):
            return "unplausibel"
        if w != 0.0 and i_kurzschluss is not None and float(i_kurzschluss) == w:
            return "=I_Kurzschluss"
        if w == 0.0:
            return "0"
        return "plausibel" if _FENSTER[feld](w) else "unplausibel"

    def fmt(v):
        return "NULL" if v is None else (repr(v) if isinstance(v, float) else str(v))


# --------------------------------------------------------------------------
# Hilfsfunktionen
# --------------------------------------------------------------------------
def lies_sql_anweisungen(pfad):
    """Zerlegt das SQL-Skript in (label, anweisung).

    Kommentarzeilen (--) werden als Label gesammelt und nicht ausgefuehrt;
    die auskommentierte T_NOCT-Variante bleibt damit automatisch inaktiv.
    BEGIN/COMMIT/ROLLBACK werden uebersprungen, der Runner fuehrt die
    Transaktion selbst.
    """
    with open(pfad, "r", encoding="utf-8") as f:
        zeilen = f.read().splitlines()

    anweisungen = []
    puffer = []
    erster_kommentar = None   # erste Kommentarzeile seit der letzten Anweisung
    for zeile in zeilen:
        s = zeile.strip()
        if not puffer and (not s or s.startswith("--")):
            if not s:
                # Leerzeile trennt Kommentarbloecke: nur der Block unmittelbar
                # vor der Anweisung liefert deren Bezeichnung.
                erster_kommentar = None
            else:
                text = s.lstrip("-").strip()
                if text and not set(text) <= set("- =") and erster_kommentar is None:
                    erster_kommentar = text
            continue
        puffer.append(zeile)
        if s.endswith(";"):
            sql = "\n".join(puffer).strip()
            puffer = []
            kopf = sql.split()[0].upper().rstrip(";")
            if kopf in ("BEGIN", "COMMIT", "ROLLBACK", "END"):
                erster_kommentar = None
                continue
            anweisungen.append((erster_kommentar or sql.split("\n")[0], sql))
            erster_kommentar = None
    if puffer:
        raise ValueError("SQL-Skript endet mit unvollstaendiger Anweisung.")
    return anweisungen


def lies_zustand(con):
    """Aktuelle Werte der vier Felder je Zeile beider Tabellen."""
    zustand = {}
    for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
        rows = con.execute(
            "SELECT ID, Bezeichner, I_Kurzschluss, alpha_SC, beta_OC, gamma_PMP, T_NOCT "
            "FROM %s ORDER BY ID" % tabelle).fetchall()
        zustand[tabelle] = rows
    return zustand


def drucke_zustand(zustand, ueberschrift):
    print("")
    print("--- %s" % ueberschrift)
    for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
        print("  %s" % tabelle)
        print("    %-9s %-38s %-9s %-14s %-14s %-10s %-10s" % (
            "ID", "Bezeichner", "I_Kurzs.", "alpha_SC", "beta_OC", "gamma_PMP", "T_NOCT"))
        for (rid, bez, isc, a, b, g, t) in zustand[tabelle]:
            print("    %-9s %-38s %-9s %-14s %-14s %-10s %-10s" % (
                rid, (bez or "")[:38], fmt(isc), fmt(a), fmt(b), fmt(g), fmt(t)))


def drucke_klassen(zustand, ueberschrift):
    print("")
    print("--- %s" % ueberschrift)
    offen = 0
    for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
        for (rid, bez, isc, a, b, g, t) in zustand[tabelle]:
            werte = {"alpha_SC": a, "beta_OC": b, "gamma_PMP": g, "T_NOCT": t}
            klassen = dict((f, klassifiziere(f, werte[f], isc)) for f in FELDER)
            schlecht = [f for f in FELDER if klassen[f] != "plausibel"]
            nur_tnoct0 = (schlecht == ["T_NOCT"] and klassen["T_NOCT"] == "0")
            if not schlecht:
                status = "OK"
            elif nur_tnoct0:
                status = "OK (T_NOCT=0 hinnehmbar, im PAN nicht enthalten)"
            else:
                status = "OFFEN: " + ", ".join(
                    "%s=%s [%s]" % (f, fmt(werte[f]), klassen[f]) for f in schlecht)
                offen += 1
            print("  %-13s %-9s %-38s %s" % (tabelle, rid, (bez or "")[:38], status))
    print("  -> %d Zeile(n) mit echtem Handlungsbedarf" % offen)
    return offen


def pruefe_vorbedingungen(db, produktiv_freigegeben):
    if not os.path.isfile(db):
        return "Datenbank nicht gefunden: %s" % db
    if os.path.normcase(os.path.abspath(db)) == os.path.normcase(PRODUKTIV_DB):
        if not produktiv_freigegeben:
            return ("Ziel ist die Produktivdatenbank %s. Abbruch. Nur mit dem "
                    "Schalter --produktiv-freigegeben (bewusste Freigabe durch den "
                    "Anwender, EPOS-Plan geschlossen, Sicherung vorhanden)."
                    % PRODUKTIV_DB)
        print("HINWEIS: Produktivdatenbank ist per --produktiv-freigegeben freigegeben.")
    for endung in ("-wal", "-shm"):
        if os.path.exists(db + endung):
            return ("Journaldatei %s vorhanden - die Datenbank ist noch in Benutzung. "
                    "Bitte EPOS-Plan schliessen und erneut starten." % (db + endung))
    return None


def oeffne_rw(db):
    con = sqlite3.connect(db, isolation_level=None, timeout=2.0)
    con.execute("PRAGMA foreign_keys = ON")
    return con


# --------------------------------------------------------------------------
def main(argv=None):
    p = argparse.ArgumentParser(
        description="Reparatur der PV-Temperaturkoeffizienten (Trockenlauf ohne --ausfuehren)")
    p.add_argument("db", help="Pfad zur SQLite-Datenbank")
    p.add_argument("--ausfuehren", action="store_true",
                   help="Aenderungen wirklich festschreiben (sonst Trockenlauf mit ROLLBACK)")
    p.add_argument("--produktiv-freigegeben", dest="produktiv", action="store_true",
                   help="Freigabe fuer %s (nur vom Anwender zu setzen)" % PRODUKTIV_DB)
    p.add_argument("--sql", help="Pfad des SQL-Skripts (Vorgabe: neben diesem Runner)")
    p.add_argument("--kontrolle-md", dest="kontrolle_md",
                   help="Kontrollmessung nach dem Echtlauf als Markdown ablegen")
    a = p.parse_args(argv)

    db = os.path.abspath(a.db)
    sql_pfad = os.path.abspath(
        a.sql or os.path.join(os.path.dirname(os.path.abspath(__file__)),
                              "reparatur_pv_katalog.sql"))

    print("=" * 78)
    print("Reparatur PV-Modulkatalog - %s"
          % ("ECHTLAUF (--ausfuehren)" if a.ausfuehren else "TROCKENLAUF (ROLLBACK)"))
    print("Zeit:        %s" % datetime.now().strftime("%d.%m.%Y %H:%M:%S"))
    print("Datenbank:   %s" % db)
    print("SQL-Skript:  %s" % sql_pfad)
    print("Messmodul:   %s" % (getattr(MESS, "__file__", None) or "lokales Duplikat"))
    print("=" * 78)

    fehler = pruefe_vorbedingungen(db, a.produktiv)
    if fehler:
        print("ABBRUCH: %s" % fehler)
        return 2
    if not os.path.isfile(sql_pfad):
        print("ABBRUCH: SQL-Skript nicht gefunden: %s" % sql_pfad)
        return 2

    anweisungen = lies_sql_anweisungen(sql_pfad)
    print("SQL-Anweisungen im Skript: %d" % len(anweisungen))

    # Sicherung
    sicherung = "%s.vor-pv-reparatur-%s.bak" % (db, datetime.now().strftime("%Y-%m-%d"))
    if a.ausfuehren:
        if os.path.exists(sicherung):
            sicherung = "%s.vor-pv-reparatur-%s.bak" % (
                db, datetime.now().strftime("%Y-%m-%d_%H%M%S"))
        shutil.copy2(db, sicherung)
        print("Sicherung angelegt: %s (%d Bytes)" % (sicherung, os.path.getsize(sicherung)))
    else:
        print("Sicherung (im Trockenlauf NICHT angelegt, im Echtlauf zwingend):")
        print("  %s" % sicherung)

    try:
        con = oeffne_rw(db)
    except sqlite3.Error as e:
        print("ABBRUCH: Datenbank laesst sich nicht oeffnen (%s). "
              "Bitte EPOS-Plan schliessen." % e)
        return 2

    gesamt = 0
    try:
        try:
            con.execute("BEGIN IMMEDIATE")
        except sqlite3.OperationalError as e:
            print("ABBRUCH: Datenbank ist gesperrt (%s). Bitte EPOS-Plan schliessen." % e)
            con.close()
            return 2

        vorher = lies_zustand(con)
        drucke_zustand(vorher, "VORHER")

        print("")
        print("--- UPDATEs")
        for nr, (label, sql) in enumerate(anweisungen, start=1):
            cur = con.execute(sql)
            geaendert = con.execute("SELECT changes()").fetchone()[0]
            gesamt += geaendert
            if geaendert == 1:
                marke, zusatz = "OK ", ""
            elif geaendert == 0:
                marke, zusatz = "-- ", "  (Guard greift nicht: Zeile bereits repariert oder abweichend)"
            else:
                marke, zusatz = "!! ", "  (unerwartet: mehr als eine Zeile getroffen)"
            print("  %s%2d. changes() = %d  | %s%s" % (marke, nr, geaendert, label, zusatz))
            if cur.rowcount not in (-1, geaendert):
                print("      Hinweis: rowcount = %d" % cur.rowcount)
        print("  Summe geaenderter Zeilen: %d von %d Anweisungen" % (gesamt, len(anweisungen)))
        if gesamt == 0:
            print("  -> nichts zu tun: der Zielstand ist bereits hergestellt (idempotent).")

        nachher = lies_zustand(con)
        drucke_zustand(nachher, "NACHHER")

        if a.ausfuehren:
            con.execute("COMMIT")
            print("")
            print("COMMIT ausgefuehrt - die Aenderungen sind festgeschrieben.")
        else:
            con.execute("ROLLBACK")
            print("")
            print("ROLLBACK ausgefuehrt - die Datenbank ist unveraendert.")
    except Exception as e:
        print("")
        print("ABBRUCH nach Fehler: %s: %s" % (type(e).__name__, e))
        try:
            con.execute("ROLLBACK")
            print("ROLLBACK ausgefuehrt - die Datenbank ist unveraendert.")
        except Exception:
            pass
        return 3
    finally:
        con.close()

    if a.ausfuehren:
        # Kontrolle ueber die Messfunktion, frisch und read-only
        if MESS is not None:
            daten = MESS.messe(db)
            zustand = {}
            for tabelle in ("Tab_PV_STAMM", "Tab_PV"):
                zustand[tabelle] = [
                    (z["werte"]["ID"], z["werte"].get("Bezeichner"),
                     z["werte"].get("I_Kurzschluss"), z["werte"].get("alpha_SC"),
                     z["werte"].get("beta_OC"), z["werte"].get("gamma_PMP"),
                     z["werte"].get("T_NOCT"))
                    for z in daten[tabelle]["zeilen"]]
            offen = drucke_klassen(zustand, "KONTROLLE (messung_pv_katalog.messe, read-only)")
            if a.kontrolle_md:
                text = MESS.markdown_bericht(daten, db, "Kontrollmessung nach Reparatur")
                with open(a.kontrolle_md, "w", encoding="utf-8", newline="\n") as f:
                    f.write(text)
                print("  Kontrollbericht: %s" % os.path.abspath(a.kontrolle_md))
        else:
            con = sqlite3.connect("file:%s?mode=ro" % db.replace("\\", "/"), uri=True)
            offen = drucke_klassen(lies_zustand(con), "KONTROLLE (lokale Klassifikation)")
            con.close()
        print("")
        print("Sicherung liegt unter: %s" % sicherung)
        return 0 if offen == 0 else 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
