# -*- coding: utf-8 -*-
"""Baut aus sql/schema/001..003 eine leere SQLite-Datenbank und faehrt die S2-Abnahme.

Nur Python-Standardbibliothek (sqlite3). Die Access-Quelle wird NICHT angefasst -
gelesen werden ausschliesslich die drei erzeugten SQL-Dateien und inventar.json.

    python baue_leere_db.py [--schema <verz>] [--db <datei>] [--json <datei>]

Rueckgabewert 0 = alle Proben bestanden, 1 = mindestens eine Abweichung.
"""

import argparse
import json
import os
import re
import sqlite3
import sys
import tempfile

MIN_SQLITE = (3, 37)          # STRICT-Tabellen gibt es ab 3.37.0


class Bericht:
    def __init__(self):
        self.zeilen = []
        self.pruefungen = []
        self.fehler = 0

    def sag(self, text=""):
        print(text)
        self.zeilen.append(text)

    def pruefe(self, name, soll, ist, ok=None):
        if ok is None:
            ok = (soll == ist)
        if not ok:
            self.fehler += 1
        self.pruefungen.append({"Pruefung": name, "Soll": soll, "Ist": ist, "OK": bool(ok)})
        self.sag("  [%s] %-46s Soll: %-24s Ist: %s"
                 % ("ok" if ok else "ABWEICHUNG", name, soll, ist))
        return ok


def lies(pfad):
    with open(pfad, "r", encoding="utf-8") as f:
        return f.read()


def main():
    ap = argparse.ArgumentParser()
    hier = os.path.dirname(os.path.abspath(__file__))
    ap.add_argument("--schema", default=os.path.join(hier, "..", "schema"))
    ap.add_argument("--db", default=None, help="Zieldatei (Vorgabe: temporaere Wegwerf-DB)")
    ap.add_argument("--json", default=None, help="Ergebnisse zusaetzlich als JSON ablegen")
    args = ap.parse_args()

    schema = os.path.abspath(args.schema)
    b = Bericht()

    # ---------------------------------------------------------------- 1) Version
    b.sag("=== S2-Abnahme: leere SQLite-Datenbank aus dem erzeugten Schema ===")
    b.sag("sqlite3-Bibliothek : %s (Python %s)" % (sqlite3.sqlite_version, sys.version.split()[0]))
    ver = tuple(int(x) for x in sqlite3.sqlite_version.split("."))
    if ver[:2] < MIN_SQLITE:
        b.sag("ABBRUCH: SQLite %s kann kein STRICT (noetig: >= %d.%d)."
              % (sqlite3.sqlite_version, MIN_SQLITE[0], MIN_SQLITE[1]))
        return 2
    b.pruefe("SQLite-Version >= 3.37 (STRICT)", ">= 3.37", sqlite3.sqlite_version, ver[:2] >= MIN_SQLITE)

    inv = json.loads(lies(os.path.join(schema, "inventar.json")))
    skripte = ["001_grundschema.sql", "002_views.sql", "003_indizes_fk.sql"]
    texte = {n: lies(os.path.join(schema, n)) for n in skripte}

    # ---------------------------------------------------------------- 2) Aufbau
    zieldatei = args.db
    tmp = None
    if zieldatei is None:
        tmp = tempfile.mkdtemp(prefix="epos_s2_")
        zieldatei = os.path.join(tmp, "Kenndaten_leer.sqlite")
    if os.path.exists(zieldatei):
        os.remove(zieldatei)
    b.sag("Zieldatenbank      : %s" % zieldatei)
    b.sag("")

    con = sqlite3.connect(zieldatei)
    b.sag("-- Aufbau ---------------------------------------------------------")
    for n in skripte:
        try:
            con.executescript(texte[n])
            con.commit()
            b.pruefe("%s laeuft fehlerfrei" % n, "fehlerfrei", "fehlerfrei", True)
        except sqlite3.Error as e:
            b.pruefe("%s laeuft fehlerfrei" % n, "fehlerfrei", "FEHLER: %s" % e, False)
            b.sag("Abbruch - ohne vollstaendiges Schema sind die weiteren Proben sinnlos.")
            return 1

    # ---------------------------------------------------------------- 3) Integritaet
    b.sag("")
    b.sag("-- Integritaet ----------------------------------------------------")
    ic = [r[0] for r in con.execute("PRAGMA integrity_check").fetchall()]
    b.pruefe("PRAGMA integrity_check", "ok", ", ".join(ic))
    fkc = con.execute("PRAGMA foreign_key_check").fetchall()
    b.pruefe("PRAGMA foreign_key_check", "leer", "%d Zeile(n)" % len(fkc), len(fkc) == 0)

    # ---------------------------------------------------------------- 4) Zaehlungen
    b.sag("")
    b.sag("-- Zaehlungen gegen inventar.json ---------------------------------")
    soll = inv["Zaehlungen"]
    tabellen = [r[0] for r in con.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' "
        "ORDER BY name").fetchall()]
    views = [r[0] for r in con.execute(
        "SELECT name FROM sqlite_master WHERE type='view' ORDER BY name").fetchall()]
    indizes = [r[0] for r in con.execute(
        "SELECT name FROM sqlite_master WHERE type='index' AND sql IS NOT NULL ORDER BY name").fetchall()]

    b.pruefe("Tabellen (ohne sqlite_%)", soll["Tabellen"], len(tabellen))
    b.pruefe("Views", soll["Views"], len(views))
    b.pruefe("Indizes (explizit angelegt)", soll["Indizes"], len(indizes))

    # CREATE-INDEX-Anweisungen in 003 gegenzaehlen
    idx003 = len(re.findall(r"(?im)^\s*CREATE\s+(?:UNIQUE\s+)?INDEX\b", texte["003_indizes_fk.sql"]))
    b.pruefe("CREATE INDEX-Anweisungen in 003", soll["Indizes"], idx003)

    # Spaltenzahlen je Tabelle
    abw_spalten = []
    summe_spalten = 0
    for t in tabellen:
        n_ist = len(con.execute('PRAGMA table_info("%s")' % t.replace('"', '""')).fetchall())
        summe_spalten += n_ist
        n_soll = len(inv["Tabellen"].get(t, {}).get("Spalten", []))
        if n_ist != n_soll:
            abw_spalten.append("%s: Soll %d / Ist %d" % (t, n_soll, n_ist))
    b.pruefe("Spalten gesamt", soll["Spalten"], summe_spalten)
    b.pruefe("Tabellen mit abweichender Spaltenzahl", 0, len(abw_spalten), len(abw_spalten) == 0)
    for a in abw_spalten:
        b.sag("        %s" % a)

    # Fremdschluessel: je Kindtabelle nach id gruppieren
    fk_klauseln = 0
    fk_upd = fk_del = 0
    for t in tabellen:
        rows = con.execute('PRAGMA foreign_key_list("%s")' % t.replace('"', '""')).fetchall()
        gruppen = {}
        for r in rows:
            gruppen.setdefault(r[0], []).append(r)
        fk_klauseln += len(gruppen)
        for g in gruppen.values():
            if g[0][5] == "CASCADE":
                fk_upd += 1
            if g[0][6] == "CASCADE":
                fk_del += 1
    b.pruefe("FOREIGN-KEY-Klauseln gesamt", soll["Fremdschluessel"], fk_klauseln)
    b.pruefe("davon ON UPDATE CASCADE", soll["OnUpdateCascade"], fk_upd)
    b.pruefe("davon ON DELETE CASCADE", soll["OnDeleteCascade"], fk_del)

    # Autowert-Spalten: AUTOINCREMENT-Tabellen stehen in sqlite_sequence bzw. im DDL
    autoinc = len(re.findall(r"(?i)PRIMARY KEY AUTOINCREMENT", texte["001_grundschema.sql"]))
    b.pruefe("INTEGER PRIMARY KEY AUTOINCREMENT", soll["Autowerte"], autoinc)
    strict = len(re.findall(r"(?im)^\)\s*STRICT;", texte["001_grundschema.sql"]))
    b.pruefe("Tabellen mit STRICT", soll["Tabellen"], strict)

    # Jede Tabelle hat einen Primaerschluessel
    ohne_pk = []
    for t in tabellen:
        info = con.execute('PRAGMA table_info("%s")' % t.replace('"', '""')).fetchall()
        if not any(c[5] for c in info):
            ohne_pk.append(t)
    b.pruefe("Tabellen ohne Primaerschluessel", 0, len(ohne_pk), len(ohne_pk) == 0)
    for t in ohne_pk:
        b.sag("        %s" % t)

    # Elternschluessel jedes FK muss PK oder UNIQUE sein (SQLite meldet das sonst erst
    # zur Laufzeit als "foreign key mismatch"; auf leeren Tabellen schweigt foreign_key_check).
    ungedeckt = []
    for t in tabellen:
        for r in con.execute('PRAGMA foreign_key_list("%s")' % t.replace('"', '""')).fetchall():
            eltern, elternspalte = r[2], r[4]
            if elternspalte is None:
                continue
            info = con.execute('PRAGMA table_info("%s")' % eltern.replace('"', '""')).fetchall()
            ist_pk = any(c[1].lower() == elternspalte.lower() and c[5] for c in info)
            if ist_pk:
                continue
            gedeckt = False
            for ix in con.execute('PRAGMA index_list("%s")' % eltern.replace('"', '""')).fetchall():
                if not ix[2]:
                    continue
                spalten = [c[2] for c in con.execute('PRAGMA index_info("%s")' % ix[1].replace('"', '""')).fetchall()]
                if len(spalten) == 1 and spalten[0] and spalten[0].lower() == elternspalte.lower():
                    gedeckt = True
                    break
            if not gedeckt:
                ungedeckt.append("%s -> %s.%s" % (t, eltern, elternspalte))
    b.pruefe("FK-Elternspalten ohne PK/UNIQUE-Deckung", 0, len(ungedeckt), len(ungedeckt) == 0)
    for u in ungedeckt:
        b.sag("        %s" % u)

    # ---------------------------------------------------------------- 5) Views
    b.sag("")
    b.sag("-- Views ausfuehrbar ----------------------------------------------")
    view_fehler = []
    for v in views:
        try:
            con.execute('SELECT * FROM "%s" LIMIT 0' % v.replace('"', '""')).fetchall()
        except sqlite3.Error as e:
            view_fehler.append("%s: %s" % (v, e))
    b.pruefe("Views per SELECT ... LIMIT 0 ausfuehrbar", len(views), len(views) - len(view_fehler))
    for f in view_fehler:
        b.sag("        %s" % f)

    # ---------------------------------------------------------------- 6) Proben
    b.sag("")
    b.sag("-- Proben (STRICT / Boolean-CHECK / Fremdschluessel) ---------------")

    def finde_spalte(bedingung):
        for tn, td in inv["Tabellen"].items():
            for c in td["Spalten"]:
                if bedingung(tn, td, c):
                    return tn, c
        return None, None

    con.execute("PRAGMA foreign_keys = OFF")

    # STRICT: Text in eine REAL-Spalte
    t_real, c_real = finde_spalte(lambda tn, td, c: c["SqliteTyp"] == "REAL" and not c["NotNull"])
    probe = "INSERT INTO \"%s\" (\"%s\") VALUES ('nicht-numerisch')" % (t_real, c_real["Name"])
    try:
        con.execute("BEGIN")
        con.execute(probe)
        ergebnis = "DURCHGELASSEN (STRICT wirkt nicht!)"
        ok = False
    except sqlite3.Error as e:
        ergebnis = "abgewiesen: %s" % e
        ok = True
    finally:
        con.rollback()
    b.pruefe("STRICT: TEXT in REAL-Spalte (%s.%s)" % (t_real, c_real["Name"]),
             "abgewiesen", ergebnis, ok)

    # Boolean-CHECK: -1 einfuegen (Access-Wahrheitswert ohne Wandlung)
    t_b, c_b = finde_spalte(lambda tn, td, c: c["DaoTypName"] == "dbBoolean")
    probe = "INSERT INTO \"%s\" (\"%s\") VALUES (-1)" % (t_b, c_b["Name"])
    try:
        con.execute("BEGIN")
        con.execute(probe)
        ergebnis = "DURCHGELASSEN (CHECK wirkt nicht!)"
        ok = False
    except sqlite3.Error as e:
        ergebnis = "abgewiesen: %s" % e
        ok = True
    finally:
        con.rollback()
    b.pruefe("Boolean-CHECK: -1 in %s.%s" % (t_b, c_b["Name"]), "abgewiesen", ergebnis, ok)

    # Fremdschluessel: unbekannter Elternwert
    con.execute("PRAGMA foreign_keys = ON")
    fk_t = fk_c = None
    for tn, td in inv["Tabellen"].items():
        for fk in td["Fremdschluessel"]:
            if len(fk["KindSpalten"]) == 1:
                fk_t, fk_c = tn, fk
                break
        if fk_t:
            break
    probe = "INSERT INTO \"%s\" (\"%s\") VALUES (987654321)" % (fk_t, fk_c["KindSpalten"][0])
    try:
        con.execute("BEGIN")
        con.execute(probe)
        con.commit()
        ergebnis = "DURCHGELASSEN (FK wirkt nicht!)"
        ok = False
    except sqlite3.Error as e:
        ergebnis = "abgewiesen: %s" % e
        ok = True
    finally:
        con.rollback()
    b.pruefe("FK: unbekannter Elternwert in %s.%s -> %s"
             % (fk_t, fk_c["KindSpalten"][0], fk_c["Eltern"]), "abgewiesen", ergebnis, ok)

    # Gegenprobe: mit foreign_keys = OFF laeuft derselbe INSERT durch
    con.execute("PRAGMA foreign_keys = OFF")
    try:
        con.execute("BEGIN")
        con.execute(probe)
        ergebnis = "durchgelassen"
        ok = True
    except sqlite3.Error as e:
        ergebnis = "unerwartet abgewiesen: %s" % e
        ok = False
    finally:
        con.rollback()
    b.pruefe("Gegenprobe: derselbe INSERT mit foreign_keys=OFF",
             "durchgelassen (PRAGMA ist Pflicht)", ergebnis, ok)

    # DEFAULT-Probe: Zeile allein aus Vorgabewerten
    t_d, c_d = finde_spalte(lambda tn, td, c: c["Default"] is not None and c["SqliteTyp"] == "REAL")
    if t_d:
        try:
            con.execute("BEGIN")
            con.execute('INSERT INTO "%s" DEFAULT VALUES' % t_d)
            wert = con.execute('SELECT "%s" FROM "%s"' % (c_d["Name"], t_d)).fetchone()
            ergebnis = "eingefuegt, %s = %r" % (c_d["Name"], wert[0] if wert else None)
            ok = True
        except sqlite3.Error as e:
            ergebnis = "FEHLER: %s" % e
            ok = False
        finally:
            con.rollback()
        b.pruefe("DEFAULT unter STRICT (%s.%s)" % (t_d, c_d["Name"]),
                 "eingefuegt", ergebnis, ok)

    # ---------------------------------------------------------------- Abschluss
    con.execute("PRAGMA foreign_keys = ON")
    con.commit()
    groesse = os.path.getsize(zieldatei)
    con.close()

    b.sag("")
    b.sag("-- Ergebnis -------------------------------------------------------")
    b.sag("Datenbankgroesse   : %d Bytes (leer)" % groesse)
    b.sag("Pruefungen         : %d, davon Abweichungen: %d" % (len(b.pruefungen), b.fehler))
    b.sag("Gesamt             : %s" % ("BESTANDEN" if b.fehler == 0 else "NICHT BESTANDEN"))

    if args.json:
        with open(args.json, "w", encoding="utf-8") as f:
            json.dump({
                "SqliteVersion": sqlite3.sqlite_version,
                "Datenbank": zieldatei,
                "Bytes": groesse,
                "Pruefungen": b.pruefungen,
                "Abweichungen": b.fehler,
                "Views": views,
                "ViewFehler": view_fehler,
                "Ausgabe": b.zeilen,
            }, f, ensure_ascii=False, indent=2)
    return 0 if b.fehler == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
