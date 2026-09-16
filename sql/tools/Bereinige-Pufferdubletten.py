#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Laeufer und Probe fuer sql/tools/Bereinige-Pufferdubletten.sql.

Der Schreibweg auf die versionierte Testdatenbank fuehrt IMMER ueber dieses
Skript - nie von Hand. Der Ablauf ist in beiden Betriebsarten derselbe:

  1. Die Datenbank wird in einen Arbeitsordner kopiert.
  2. Das SQL-Skript laeuft auf der Kopie, mit eingeschalteten Fremdschluesseln.
  3. Geprueft wird: Zeilenzahl vorher/nachher gegen die VORHER gezaehlte Zahl
     der Wiederholungen, dass GENAU die erwarteten Ids fallen und die
     behaltenen stehen bleiben, dass keine andere Zeile wandert (der Rest der
     Tabelle ist Zeile fuer Zeile derselbe), dass keine Nachbartabelle kleiner
     wird, PRAGMA foreign_key_check, PRAGMA integrity_check und ein zweiter
     Lauf (Wiederholbarkeit - er darf nichts mehr aendern).
  4. Nur mit --anwenden und nur nach einer fehlerfreien Probe wird dasselbe
     Skript danach auf die echte Datei ausgefuehrt und dort erneut geprueft.

Weil die erwartete Differenz aus der Datenbank selbst kommt, laeuft das Skript
auf einer bereits bereinigten Datei fehlerfrei durch und aendert nichts - es
ist damit auch sein eigener Nachweis.

Aufruf (nur Standardbibliothek):

    python3 sql/tools/Bereinige-Pufferdubletten.py --db Referenzlaeufe/Kenndaten_Test.sqlite
    python3 sql/tools/Bereinige-Pufferdubletten.py --db Referenzlaeufe/Kenndaten_Test.sqlite --anwenden

Rueckgabewert 0 = alles gut, 1 = mindestens eine Pruefung fehlgeschlagen.
"""

import argparse
import os
import shutil
import sqlite3
import sys
import tempfile

HIER = os.path.dirname(os.path.abspath(__file__))
SKRIPT = os.path.join(HIER, "Bereinige-Pufferdubletten.sql")

TABELLE = "Z_ProjektPufferSp"

# Die Gruppierung - woertlich dieselbe wie im SQL-Skript, damit Probe und
# Skript nicht auseinanderlaufen koennen.
GRUPPE = ("ID_Projekt, ID_Pufferspeicher, Pufferspeicher, Erzeuger, "
          "Vorlauf, Ruecklauf, Prioritaet, Schwelle_Ein, Schwelle_Aus")

# Nachbartabellen, die das Skript nie anfassen darf.
UNBERUEHRT = ("Tab_Pufferspeicher", "Tab_Energieanlagen", "Z_AnlageSenke",
              "Z_AnlagePufferVerbund", "Tab_ErgebnisPufferspeicher")


def zeilen(conn):
    """Die ganze Tabelle, nach Id geordnet - Id und Inhalt getrennt."""
    satz = conn.execute(
        "SELECT ID, " + GRUPPE + ' FROM "%s" ORDER BY ID' % TABELLE).fetchall()
    return [(r[0], r[1:]) for r in satz]


def erwartung(conn):
    """Welche Ids fallen, welche bleiben - VORHER aus der Datenbank gelesen."""
    behalten = set(r[0] for r in conn.execute(
        "SELECT MIN(ID) FROM \"%s\" GROUP BY %s" % (TABELLE, GRUPPE)).fetchall())
    alle = set(r[0] for r in conn.execute('SELECT ID FROM "%s"' % TABELLE).fetchall())
    return behalten, alle - behalten


def nachbarn(conn):
    return dict((t, conn.execute('SELECT COUNT(*) FROM "%s"' % t).fetchone()[0])
                for t in UNBERUEHRT)


def pruefe(pfad, skripttext, fehler, ueberschrift):
    """Ein Lauf des Skripts samt allen Pruefungen."""
    print(ueberschrift)
    conn = sqlite3.connect(pfad)
    try:
        conn.execute("PRAGMA foreign_keys = ON")
        if conn.execute("PRAGMA foreign_keys").fetchone()[0] != 1:
            fehler.append("PRAGMA foreign_keys liess sich nicht einschalten.")
            return

        vorher = zeilen(conn)
        behalten, fallen = erwartung(conn)
        nachbarn_vorher = nachbarn(conn)

        conn.executescript(skripttext)
        conn.commit()

        nachher = zeilen(conn)
        ids_nachher = set(z[0] for z in nachher)

        print("  %s %-24s %6d -> %-6d  Differenz %d (erwartet %d)"
              % ("OK " if len(vorher) - len(nachher) == len(fallen) else "ROT",
                 TABELLE, len(vorher), len(nachher),
                 len(vorher) - len(nachher), len(fallen)))
        if len(vorher) - len(nachher) != len(fallen):
            fehler.append("%s: Differenz %d statt %d"
                          % (TABELLE, len(vorher) - len(nachher), len(fallen)))

        print("  %s gefallen: %s"
              % ("OK " if ids_nachher == behalten else "ROT",
                 ", ".join(str(i) for i in sorted(fallen)) or "keine"))
        if ids_nachher != behalten:
            fehler.append("Es blieben %r statt %r stehen."
                          % (sorted(ids_nachher)[:10], sorted(behalten)[:10]))

        # KEINE ANDERE ZEILE WANDERT: Was stehen blieb, steht Zeile fuer Zeile
        # unveraendert da - gleiche Id, gleicher Inhalt, gleiche Reihenfolge.
        geblieben = [z for z in vorher if z[0] in behalten]
        gleich = geblieben == nachher
        print("  %s die %d verbliebenen Zeilen sind unveraendert"
              % ("OK " if gleich else "ROT", len(nachher)))
        if not gleich:
            fehler.append("Eine verbliebene Zeile hat sich geaendert.")

        nachbarn_nachher = nachbarn(conn)
        for t in UNBERUEHRT:
            heil = nachbarn_vorher[t] == nachbarn_nachher[t]
            print("  %s %-28s %6d Zeilen unberuehrt"
                  % ("OK " if heil else "ROT", t, nachbarn_nachher[t]))
            if not heil:
                fehler.append("%s: %d statt %d Zeilen"
                              % (t, nachbarn_nachher[t], nachbarn_vorher[t]))

        # Und die Bedingung selbst: keine Gruppe mehr als einmal besetzt.
        rest = conn.execute(
            "SELECT COUNT(*) FROM (SELECT 1 FROM \"%s\" GROUP BY %s HAVING COUNT(*) > 1)"
            % (TABELLE, GRUPPE)).fetchone()[0]
        print("  %s verbliebene Wiederholungen: %d" % ("OK " if not rest else "ROT", rest))
        if rest:
            fehler.append("%d Gruppe(n) sind weiterhin mehrfach besetzt." % rest)

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
    finally:
        conn.close()


def main():
    p = argparse.ArgumentParser(
        description="Wortgleiche Wiederholungen aus Z_ProjektPufferSp entfernen.")
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
    ordner = tempfile.mkdtemp(prefix="pufferdubletten_")
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
