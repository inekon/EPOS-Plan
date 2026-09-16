#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Laeufer und Probe fuer sql/tools/Bereinige-Probierpuffer.sql.

Der Schreibweg auf die versionierte Testdatenbank fuehrt IMMER ueber dieses
Skript - nie von Hand. Der Ablauf ist in beiden Betriebsarten derselbe:

  1. Die Datenbank wird in einen Arbeitsordner kopiert.
  2. Das SQL-Skript laeuft auf der Kopie, mit eingeschalteten Fremdschluesseln.
  3. Geprueft wird: Zaehlungen vorher/nachher je beruehrter Tabelle gegen die
     VORHER gezaehlte Trefferzahl derselben Bedingungen, PRAGMA
     foreign_key_check, PRAGMA integrity_check und ein zweiter Lauf
     (Wiederholbarkeit - er darf nichts mehr aendern).
  4. Nur mit --anwenden und nur nach einer fehlerfreien Probe wird dasselbe
     Skript danach auf die echte Datei ausgefuehrt und dort erneut geprueft.

Weil die erwartete Differenz aus der Datenbank selbst kommt, laeuft das Skript
auf einer bereits bereinigten Datei fehlerfrei durch und aendert nichts - es
ist damit auch sein eigener Nachweis.

Aufruf (nur Standardbibliothek):

    python3 sql/tools/Bereinige-Probierpuffer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
    python3 sql/tools/Bereinige-Probierpuffer.py --db Referenzlaeufe/Kenndaten_Test.sqlite --anwenden

Rueckgabewert 0 = alles gut, 1 = mindestens eine Pruefung fehlgeschlagen.
"""

import argparse
import os
import shutil
import sqlite3
import sys
import tempfile

HIER = os.path.dirname(os.path.abspath(__file__))
SKRIPT = os.path.join(HIER, "Bereinige-Probierpuffer.sql")

# Die zwei Probierreste und ihre Anlagenzeilen.
PUFFER = (1007009, 1054216)
ANLAGEN = (11238, 14941)

# Die Bedingung, die genau die zwei Probierreste trifft - derselbe Wortlaut wie
# im SQL-Skript, damit Probe und Skript nicht auseinanderlaufen koennen.
WO_PUFFER = ("ID IN (1007009, 1054216) AND Bezeichner = 'test' "
             "AND Gesamtvolumen = 2 AND Investitionskosten = 4000.0 "
             "AND ID_Projekt IN (1007, 1046)")
WO_ANLAGE = ("ID IN (11238, 14941) AND Bezeichner = 'test' AND ID_Type = 12 "
             "AND ID_Projekt IN (1007, 1046) AND ID_PUFFER IN (1007009, 1054216)")

# Tabellen, deren Zeilenzahl vor und nach dem Lauf verglichen wird, samt der
# Bedingung, mit der die erwartete Differenz VORHER gezaehlt wird.
# None = das Skript darf in dieser Tabelle nie eine Zeile treffen.
GEMESSEN = [
    ("Tab_Pufferspeicher", WO_PUFFER),
    ("Tab_Energieanlagen", WO_ANLAGE),
    ("Z_ProjektPufferSp", "ID_Pufferspeicher IN (1007009, 1054216)"),
    ("Z_AnlageSenke", "ID_Anlage IN (11238, 14941) OR ID_Puffer IN (1007009, 1054216)"),
    ("Z_AnlagePufferVerbund", "ID_Anlage IN (11238, 14941) OR ID_Puffer IN (1007009, 1054216)"),
    ("Z_AnlageStrang", "ID_Anlage IN (11238, 14941)"),
    ("Tab_ProjektWerte", "ID_Anlage IN (11238, 14941) OR ID_AnlageGeraet IN (11238, 14941)"),
    ("Tab_ErgebnisPufferspeicher", None),
]


def stand(conn):
    """Zeilenzahl je gemessener Tabelle und die Zahl der Treffer der Bedingung."""
    zeilen, treffer = {}, {}
    for tabelle, wo in GEMESSEN:
        zeilen[tabelle] = conn.execute('SELECT COUNT(*) FROM "%s"' % tabelle).fetchone()[0]
        treffer[tabelle] = 0 if wo is None else conn.execute(
            'SELECT COUNT(*) FROM "%s" WHERE %s' % (tabelle, wo)).fetchone()[0]
    return zeilen, treffer


def pruefe(pfad, skripttext, fehler, ueberschrift):
    """Ein Lauf des Skripts samt allen Pruefungen."""
    print(ueberschrift)
    conn = sqlite3.connect(pfad)
    try:
        conn.execute("PRAGMA foreign_keys = ON")
        if conn.execute("PRAGMA foreign_keys").fetchone()[0] != 1:
            fehler.append("PRAGMA foreign_keys liess sich nicht einschalten.")
            return
        vorher, erwartet = stand(conn)
        conn.executescript(skripttext)
        conn.commit()
        nachher, rest = stand(conn)

        for tabelle, _ in GEMESSEN:
            soll, ist = erwartet[tabelle], vorher[tabelle] - nachher[tabelle]
            marke = "OK " if ist == soll else "ROT"
            print("  %s %-28s %6d -> %-6d  Differenz %d (erwartet %d)"
                  % (marke, tabelle, vorher[tabelle], nachher[tabelle], ist, soll))
            if ist != soll:
                fehler.append("%s: Differenz %d statt %d" % (tabelle, ist, soll))
            if rest[tabelle]:
                fehler.append("%s: %d Zeile(n) der Bedingung blieben stehen."
                              % (tabelle, rest[tabelle]))

        waisen = conn.execute("PRAGMA foreign_key_check").fetchall()
        print("  %s foreign_key_check: %s"
              % ("OK " if not waisen else "ROT",
                 "keine Waise" if not waisen else "%d Waisen" % len(waisen)))
        if waisen:
            fehler.append("foreign_key_check meldet %d Waisen: %r" % (len(waisen), waisen[:5]))

        heil = conn.execute("PRAGMA integrity_check").fetchone()[0]
        print("  %s integrity_check: %s" % ("OK " if heil == "ok" else "ROT", heil))
        if heil != "ok":
            fehler.append("integrity_check: %s" % heil)

        dritter = conn.execute(
            "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID = 1018022").fetchone()[0]
        print("  %s Satz 1018022 (Projekt 1023, 500 Ltr) %s"
              % ("OK " if dritter == 1 else "ROT",
                 "steht unberuehrt" if dritter == 1 else "fehlt - er soll bleiben"))
        if dritter != 1:
            fehler.append("Der dritte Satz 1018022 (Projekt 1023, 500 Ltr) fehlt.")
    finally:
        conn.close()


def main():
    p = argparse.ArgumentParser(description="Probierreste 'test' aus der Testdatenbank entfernen.")
    p.add_argument("--db", required=True, help="Pfad der Datenbank.")
    p.add_argument("--anwenden", action="store_true",
                   help="Nach fehlerfreier Probe dasselbe Skript auf die echte Datei anwenden.")
    args = p.parse_args()

    if not os.path.isfile(args.db):
        print("Datenbank nicht gefunden: %s" % args.db)
        return 1
    with open(args.db, "rb") as f:
        if f.read(15) != b"SQLite format 3":
            print("Das ist keine SQLite-Datei (LFS-Zeigerdatei?): %s" % args.db)
            return 1

    with open(SKRIPT, "r", encoding="utf-8") as f:
        skripttext = f.read()

    fehler = []
    ordner = tempfile.mkdtemp(prefix="probierpuffer_")
    try:
        kopie = os.path.join(ordner, "Probe.sqlite")
        shutil.copyfile(args.db, kopie)
        pruefe(kopie, skripttext, fehler, "Probe auf der Kopie %s" % kopie)
        pruefe(kopie, skripttext, fehler,
               "Wiederholung auf derselben Kopie (sie darf nichts mehr aendern)")
    finally:
        shutil.rmtree(ordner, ignore_errors=True)

    if fehler:
        print("\nPROBE ROT - %d Befund(e):" % len(fehler))
        for z in fehler:
            print("  - %s" % z)
        return 1
    print("\nPROBE GRUEN")

    if not args.anwenden:
        print("Nichts geschrieben. Mit --anwenden auf die echte Datei anwenden.")
        return 0

    pruefe(args.db, skripttext, fehler, "\nAnwendung auf %s" % args.db)
    if fehler:
        print("\nANWENDUNG ROT - %d Befund(e):" % len(fehler))
        for z in fehler:
            print("  - %s" % z)
        return 1
    print("\nANWENDUNG GRUEN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
