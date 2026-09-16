#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Laeufer und Probe fuer sql/tools/Setze-Energietraeger-Brenner.sql.

Der Schreibweg auf die versionierte Testdatenbank fuehrt IMMER ueber dieses
Skript - nie von Hand. Der Ablauf ist in beiden Betriebsarten derselbe:

  1. VOR jedem Schreiben: die Mehrdeutigkeitsprobe. Fuehrt der Katalog zum
     Brennstoff einer Brenner-Anlage ohne Traeger mehr als einen Traeger, und
     ist der Fall nicht die im SQL-Kopf benannte Ausnahme (Brennstoff 13 ->
     Traeger 60), bricht der Laeufer ab und nennt die Kandidaten.
  2. Die Datenbank wird in einen Arbeitsordner kopiert.
  3. Das SQL-Skript laeuft auf der Kopie, mit eingeschalteten Fremdschluesseln.
  4. Geprueft wird: Zaehlungen vorher/nachher je beruehrter Tabelle gegen die
     VORHER gezaehlte Trefferzahl derselben Bedingungen, je Anlage der
     erwartete Traeger, die Unberuehrtheit aller uebrigen Gewerke und aller
     bereits gepflegten Traeger, PRAGMA foreign_key_check, PRAGMA
     integrity_check und ein zweiter Lauf (Wiederholbarkeit - er darf nichts
     mehr aendern).
  5. Nur mit --anwenden und nur nach einer fehlerfreien Probe wird dasselbe
     Skript danach auf die echte Datei ausgefuehrt und dort erneut geprueft.

Weil die erwartete Differenz aus der Datenbank selbst kommt, laeuft das Skript
auf einer bereits nachgetragenen Datei fehlerfrei durch und aendert nichts - es
ist damit auch sein eigener Nachweis.

Aufruf (nur Standardbibliothek):

    python3 sql/tools/Setze-Energietraeger-Brenner.py --db Referenzlaeufe/Kenndaten_Test.sqlite
    python3 sql/tools/Setze-Energietraeger-Brenner.py --db Referenzlaeufe/Kenndaten_Test.sqlite --anwenden

Rueckgabewert 0 = alles gut, 1 = mindestens eine Pruefung fehlgeschlagen.
"""

import argparse
import os
import shutil
import sqlite3
import sys
import tempfile

HIER = os.path.dirname(os.path.abspath(__file__))
SKRIPT = os.path.join(HIER, "Setze-Energietraeger-Brenner.sql")

# Die Anlagentypen, die einen Brennstoff verbrennen und deshalb einen Traeger
# brauchen - dieselbe Liste wie im SQL-Skript.
BRENNER = (10, 11)

# Die benannte Ausnahme aus dem SQL-Kopf: Brennstoff -> Traeger, obwohl der
# Katalog mehrere Traeger fuehrt. Sie ist eine ANNAHME, keine Ableitung.
AUSNAHME = {13: 60}

# Der Brennstoff des Geraets - derselbe Ausdruck wie im SQL-Skript, damit Probe
# und Skript nicht auseinanderlaufen koennen.
BRENNSTOFF = ("COALESCE("
              "(SELECT k.Brennstoff FROM Tab_Heizkessel k WHERE k.ID = a.ID_Kessel),"
              "(SELECT b.Brennstoff FROM Tab_BHKW       b WHERE b.ID = a.ID_BHKW))")

# Die Bedingung, die genau die nachzutragenden Anlagen trifft.
WO_OFFEN = ("a.ID_Type IN (10, 11) AND (a.ID_Carrier IS NULL OR a.ID_Carrier = 0)")


def offene_anlagen(conn):
    """Je Brenner-Anlage ohne Traeger: ID, Projekt, Bezeichner, Brennstoff."""
    return conn.execute(
        "SELECT a.ID, a.ID_Projekt, a.Bezeichner, a.ID_Type, %s "
        "FROM Tab_Energieanlagen a WHERE %s ORDER BY a.ID_Projekt, a.ID"
        % (BRENNSTOFF, WO_OFFEN)).fetchall()


def kandidaten(conn, brennstoff):
    """Alle Traeger des Katalogs zu einem Brennstoff."""
    if brennstoff is None:
        return []
    return conn.execute(
        "SELECT id, name FROM energy_carrier WHERE ID_Brennstoff = ? ORDER BY id",
        (brennstoff,)).fetchall()


def sollzuordnung(conn, fehler):
    """Der erwartete Traeger je offener Anlage - oder ein Abbruchbefund.

    Eindeutig aufloesbar -> der eine Traeger. Mehrdeutig -> die benannte
    Ausnahme, sonst Abbruch mit Nennung der Kandidaten. Kein Traeger im
    Katalog -> Abbruch, denn dann fehlt die Grundlage.
    """
    soll = {}
    for anlage, projekt, bezeichner, typ, brennstoff in offene_anlagen(conn):
        name = bezeichner if bezeichner is not None else "(ohne Bezeichner)"
        if brennstoff is None:
            fehler.append("Anlage %d (Projekt %d, „%s“) hat kein Geraet mit "
                          "Brennstoff - der Traeger laesst sich nicht ableiten."
                          % (anlage, projekt, name))
            continue
        moegliche = kandidaten(conn, brennstoff)
        if len(moegliche) == 1:
            soll[anlage] = moegliche[0][0]
        elif brennstoff in AUSNAHME and AUSNAHME[brennstoff] in [m[0] for m in moegliche]:
            soll[anlage] = AUSNAHME[brennstoff]
        elif not moegliche:
            fehler.append("Anlage %d (Projekt %d, „%s“): Zu Brennstoff %d fuehrt "
                          "der Katalog KEINEN Energietraeger."
                          % (anlage, projekt, name, brennstoff))
        else:
            fehler.append("Anlage %d (Projekt %d, „%s“): Zu Brennstoff %d fuehrt "
                          "der Katalog %d Traeger, und es gibt keine benannte Ausnahme. "
                          "Kandidaten: %s"
                          % (anlage, projekt, name, brennstoff, len(moegliche),
                             ", ".join("%d „%s“" % (i, n) for i, n in moegliche)))
    return soll


def fremde_ids(conn):
    """Die Anlagen, die das Skript NICHT anfassen darf: alle Nicht-Brenner und
    jeder Brenner, der schon einen Traeger traegt. Die Liste wird VOR dem Lauf
    gezogen; danach wird genau sie nachgemessen - sonst waeren die eben erst
    gesetzten Brenner ploetzlich selbst Teil der Vergleichsmenge."""
    return [z[0] for z in conn.execute(
        "SELECT ID FROM Tab_Energieanlagen "
        "WHERE ID_Type NOT IN (10, 11) OR (ID_Carrier IS NOT NULL AND ID_Carrier <> 0)")]


def traegerstand(conn, ids):
    """Der Traeger je genannter Anlage - der Stand, der gleich bleiben muss."""
    if not ids:
        return {}
    return dict(conn.execute(
        "SELECT ID, COALESCE(ID_Carrier, -1) FROM Tab_Energieanlagen WHERE ID IN (%s)"
        % ", ".join(str(int(i)) for i in ids)))


def spaltenstand(conn):
    """Zeilenzahl der Anlagentabelle und alle uebrigen Spalten der spaeter
    beruehrten Zeilen - zum Nachweis, dass nur ID_Carrier wandert."""
    zeilen = conn.execute("SELECT COUNT(*) FROM Tab_Energieanlagen").fetchone()[0]
    spalten = [z[1] for z in conn.execute("PRAGMA table_info(Tab_Energieanlagen)")
               if z[1] != "ID_Carrier"]
    inhalt = dict((r[0], r[1:]) for r in conn.execute(
        "SELECT ID, %s FROM Tab_Energieanlagen" % ", ".join('"%s"' % s for s in spalten)))
    return zeilen, inhalt


def pruefe(pfad, skripttext, fehler, ueberschrift):
    """Ein Lauf des Skripts samt allen Pruefungen."""
    print(ueberschrift)
    conn = sqlite3.connect(pfad)
    try:
        conn.execute("PRAGMA foreign_keys = ON")
        if conn.execute("PRAGMA foreign_keys").fetchone()[0] != 1:
            fehler.append("PRAGMA foreign_keys liess sich nicht einschalten.")
            return

        soll = sollzuordnung(conn, fehler)
        if fehler:
            print("  ROT Mehrdeutigkeitsprobe - es wird nichts geschrieben.")
            return
        offen_vorher = len(offene_anlagen(conn))
        fremd = fremde_ids(conn)
        fremd_vorher = traegerstand(conn, fremd)
        zeilen_vorher, inhalt_vorher = spaltenstand(conn)

        conn.executescript(skripttext)
        conn.commit()

        # Die Testdatenbank laeuft im WAL-Modus: ohne Checkpoint bliebe die
        # Aenderung in der Nebendatei -wal stehen, und die versionierte Datei
        # ginge unveraendert in den Commit.
        conn.execute("PRAGMA wal_checkpoint(TRUNCATE)")

        offen_nachher = len(offene_anlagen(conn))
        print("  %s Brenner ohne Traeger: %d -> %d (erwartet %d gesetzt)"
              % ("OK " if offen_nachher == 0 else "ROT",
                 offen_vorher, offen_nachher, len(soll)))
        if offen_nachher:
            fehler.append("%d Brenner-Anlage(n) blieben ohne Traeger." % offen_nachher)
        if offen_vorher - offen_nachher != len(soll):
            fehler.append("Es wurden %d statt %d Anlagen gesetzt."
                          % (offen_vorher - offen_nachher, len(soll)))

        for anlage in sorted(soll):
            ist = conn.execute("SELECT ID_Carrier FROM Tab_Energieanlagen WHERE ID = ?",
                               (anlage,)).fetchone()[0]
            marke = "OK " if ist == soll[anlage] else "ROT"
            print("  %s Anlage %-6d Traeger %s (erwartet %d)"
                  % (marke, anlage, ist, soll[anlage]))
            if ist != soll[anlage]:
                fehler.append("Anlage %d traegt %s statt %d." % (anlage, ist, soll[anlage]))

        fremd_nachher = traegerstand(conn, fremd)
        gleich = fremd_vorher == fremd_nachher
        print("  %s Nicht-Brenner und gepflegte Traeger: %d Zeile(n) %s"
              % ("OK " if gleich else "ROT", len(fremd_vorher),
                 "unberuehrt" if gleich else "VERAENDERT"))
        if not gleich:
            fehler.append("Zeilen ausserhalb des Auftrags wurden veraendert.")

        zeilen_nachher, inhalt_nachher = spaltenstand(conn)
        print("  %s Zeilenzahl Tab_Energieanlagen: %d -> %d"
              % ("OK " if zeilen_vorher == zeilen_nachher else "ROT",
                 zeilen_vorher, zeilen_nachher))
        if zeilen_vorher != zeilen_nachher:
            fehler.append("Die Zeilenzahl der Anlagentabelle hat sich geaendert.")
        nur_carrier = inhalt_vorher == inhalt_nachher
        print("  %s Alle uebrigen Spalten der Anlagentabelle %s"
              % ("OK " if nur_carrier else "ROT",
                 "unberuehrt" if nur_carrier else "VERAENDERT"))
        if not nur_carrier:
            fehler.append("Neben ID_Carrier wurde eine andere Spalte veraendert.")

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

        ohne_katalog = conn.execute(
            "SELECT COUNT(*) FROM Tab_Energieanlagen a WHERE a.ID_Type IN (10, 11) "
            "AND a.ID_Carrier IS NOT NULL AND a.ID_Carrier <> 0 "
            "AND NOT EXISTS (SELECT 1 FROM energy_carrier ec WHERE ec.id = a.ID_Carrier)"
        ).fetchone()[0]
        print("  %s Jeder Brenner-Traeger steht im Katalog: %d Ausreisser"
              % ("OK " if ohne_katalog == 0 else "ROT", ohne_katalog))
        if ohne_katalog:
            fehler.append("%d Brenner tragen eine ID, die es in energy_carrier nicht gibt."
                          % ohne_katalog)
    finally:
        conn.close()


def bericht_vorher(pfad):
    """Was das Skript vorhaette - rein lesend, vor jedem Schreiben."""
    conn = sqlite3.connect("file:%s?mode=ro" % pfad, uri=True)
    try:
        offen = offene_anlagen(conn)
        if not offen:
            print("Kein Brenner ohne Energietraeger - nichts zu tun.")
            return
        print("Brenner-Anlagen ohne Energietraeger (%d):" % len(offen))
        for anlage, projekt, bezeichner, typ, brennstoff in offen:
            moegliche = kandidaten(conn, brennstoff)
            gewerk = "Heizkessel" if typ == 10 else "BHKW"
            if len(moegliche) == 1:
                weg = "eindeutig -> %d „%s“" % moegliche[0]
            elif brennstoff in AUSNAHME:
                weg = ("mehrdeutig (%s) -> ANNAHME %d"
                       % (", ".join(str(m[0]) for m in moegliche), AUSNAHME[brennstoff]))
            else:
                weg = "MEHRDEUTIG: %s" % ", ".join("%d „%s“" % m for m in moegliche)
            print("  Projekt %-5d Anlage %-6d %-11s Brennstoff %-4s %s"
                  % (projekt, anlage, gewerk, brennstoff, weg))
    finally:
        conn.close()


def main():
    p = argparse.ArgumentParser(
        description="Energietraeger an Brenner-Anlagen (Heizkessel, BHKW) nachtragen.")
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

    bericht_vorher(args.db)
    print("")

    fehler = []
    ordner = tempfile.mkdtemp(prefix="energietraeger_brenner_")
    try:
        kopie = os.path.join(ordner, "Probe.sqlite")
        shutil.copyfile(args.db, kopie)
        pruefe(kopie, skripttext, fehler, "Probe auf der Kopie %s" % kopie)
        if not fehler:
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
