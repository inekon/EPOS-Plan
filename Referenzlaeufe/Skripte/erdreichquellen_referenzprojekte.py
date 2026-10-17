#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Saet die Erdreichquellen der Referenzprojekte der Testdatenbank und legt das Referenzprojekt 1057
"Referenz Erdsonde (Kopie 1029)" an (Konzept Simulationsablauf 23.7; Anwenderentscheide vom 07.10.2026:
Vorschlag uebernommen, Klimazone nach der Programmkarte 6, Energiegrenze bei der Bemessung, Spreizung
bleibt bei der Vorgabe 5 K und wird nicht gesaet).

AUSGANG. Die Sole-Waermepumpen der Referenzprojekte rechneten ohne gepflegte Quelle (WQ_Typ leer) und
damit an der Aussenluft; die Erdsonde mit Entzugsrueckwirkung (Konzept 23) stand in keinem
Referenzprojekt im Regressionsnetz. Einzig das Beispielprojekt 1029 fuehrte eine Sonde, ohne
Referenzrolle.

WAS DIESES SKRIPT TUT (eine Transaktion, kein VACUUM).
  1. Quellfelder an genau den Waermepumpen-Anlagen der Tabelle QUELLEN (Tab_Energieanlagen):
       WQ_Typ = Erdreich, WQ_Quellsystem = Sonde, WQ_Anzahl, WQ_Tiefe (m je Sonde),
       WQ_Bodentyp = MERGEL_LEHM, WQ_Flaeche und WQ_Spreizung leer (Spreizung = Vorgabe 5 K).
     Die Werte sind nach VDI 4640 Blatt 2 bemessen (spezifische Entzugsleistung des Bodens, Energiegrenze
     bei der Bemessung, Sondenlaenge 60 bis 120 m) - der Vorschlag, den der Anwender uebernommen hat.
  2. Klimazone 6 (Tab_Klimaregion.Klimazone_DIN4710) an der eigenen Klimaregion jedes dieser Projekte.
     Vorher geprueft: Jede Region gehoert genau diesem Projekt (ID_Projekt) und wird von keinem anderen
     Projekt benutzt.
  3. Die Kopie 1029 -> 1057 auf dem KOPIERWEG DES PROGRAMMS (ProjektDuplizierenCtrl.Duplizieren),
     derselbe Kopierweg wie fuer 1055 und 1056: Die Kopiermaschine wird aus
     referenzprojekt_1056_fahrplan.py geladen, nicht abgeschrieben. Dazu der Nachzug des Programms fuer
     die Geraeteanker der Kostenpositionen (KostenProjektPositionenCtrl.AnkerNachziehen) - 1029 fuehrt
     vier Kostenpositionen; eine Speicherauslegung fuehrt 1029 nicht (geprueft). Gegenprobe: jede
     kopierte Tabelle traegt fuer 1057 so viele Zeilen wie fuer 1029, und keine Zeile der Kopie
     verweist auf eine Zeile von 1029.
     1029 ist eine Variante von 1026 ("Erdwaerme", Tab_Variante); die Kopie loest diese Zeile, damit
     1057 ein eigenstaendiges Projekt ist und keine Variantengruppe von 1026 veraendert.
     An der Kopie: Quelle Sonde 4 x 90 m, MERGEL_LEHM, Flaeche und Spreizung leer, eigene Klimaregion in
     Zone 6, Beschreibung neutral, Kosten_Geaendert leer. 1029 selbst bleibt unberuehrt.

NICHT HIER. Die zweite Waermepumpe von 1008, 1019, 1023 und 1050 (Luft) bleibt ohne Quelle. Die Basis
friert ein eigener Schritt ein.

WIEDERHOLBAR. Steht alles schon (Quellen, Zonen, 1057), bricht das Skript ohne Schreiben mit Rueckgabe 0
ab. Ist die Id 1057 fremd belegt, weicht eine Vorlage ab oder misslingt eine Pruefung, rollt die
Transaktion zurueck (Rueckgabe 2).

FOLGE. Einfrierregel "gesaete Erdreichquellen der Referenzprojekte" (CLAUDE.md); gehalten von
`EPOS.Kern.Tests/ErdsondeReferenzprojektWacheTests`.

Aufruf (Windows: `py`, sonst `python3`; vorher sichern):
    py Referenzlaeufe/Skripte/erdreichquellen_referenzprojekte.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import importlib.util
import os
import sqlite3
import sys

TYP_WP = 1
WQ_TYP = "Erdreich"
QUELLSYSTEM = "Sonde"
BODENTYP = "MERGEL_LEHM"
KLIMAZONE = 6

# Projekt: (Anlage, Anzahl Sonden, Tiefe je Sonde in m)
QUELLEN = {
    1008: (10132, 6, 110.0),
    1023: (11204, 3, 100.0),
    1050: (16960, 3, 100.0),
    1019: (14923, 3, 100.0),
    1039: (14720, 16, 105.0),
    1017: (10211, 5, 120.0),
    1047: (14946, 5, 120.0),
    1055: (23726, 5, 120.0),
    1056: (23781, 5, 120.0),
    1027: (11272, 8, 100.0),
}

VORLAGE = 1029
VORLAGE_NAME = "Beispiel WP WG 1 - Erdwärme"
NEU = 1057
NAME = "Referenz Erdsonde (Kopie 1029)"
NEU_QUELLE = (4, 90.0)
BESCHREIBUNG = (
    "Referenzprojekt Erdsonde: Kopie von Projekt 1029, die Sole-Wärmepumpe bezieht ihre Wärme aus einem "
    "Sondenfeld mit 4 Sonden zu 90 m in Mergel/Lehm (Klimazone 6). Es hält die Erdsonde mit "
    "Entzugsrückwirkung im Regressionsnetz.")

QUELLSPALTEN = ["WQ_Typ", "WQ_Quellsystem", "WQ_Anzahl", "WQ_Tiefe", "WQ_Bodentyp", "WQ_Flaeche", "WQ_Spreizung"]

ANKER = {"Wärmepumpe": "ID_WP", "Heizkessel": "ID_Kessel", "BHKW": "ID_BHKW", "Photovoltaik": "ID_PV",
         "Solarthermie": "ID_Solar", "Stromspeicher": "ID_SP", "Pufferspeicher": "ID_PUFFER"}


def kopiermaschine():
    """Laedt den Kopierweg des Programms aus dem Skript von 1056 und stellt ihn auf 1029 -> 1057."""
    pfad = os.path.join(os.path.dirname(os.path.abspath(__file__)), "referenzprojekt_1056_fahrplan.py")
    spec = importlib.util.spec_from_file_location("kopierweg_1056", pfad)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    m.VORLAGE = VORLAGE
    m.NAME = NAME
    m.NEU = NEU
    return m


def anzahl(c, sql, parameter=()):
    return c.execute(sql, parameter).fetchone()[0]


def soll_quelle(anzahl_sonden, tiefe):
    return {"WQ_Typ": WQ_TYP, "WQ_Quellsystem": QUELLSYSTEM, "WQ_Anzahl": anzahl_sonden, "WQ_Tiefe": tiefe,
            "WQ_Bodentyp": BODENTYP, "WQ_Flaeche": None, "WQ_Spreizung": None}


def quelle(c, anlage):
    z = c.execute("SELECT %s FROM Tab_Energieanlagen WHERE ID = ?" % ", ".join(QUELLSPALTEN), (anlage,)).fetchone()
    return dict(zip(QUELLSPALTEN, z)) if z else None


def region(c, projekt):
    return c.execute("SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?", (projekt,)).fetchone()[0]


def zone(c, projekt):
    return c.execute("SELECT Klimazone_DIN4710 FROM Tab_Klimaregion WHERE ID = ?", (region(c, projekt),)).fetchone()[0]


def wp_anlage(c, projekt):
    ids = [r[0] for r in c.execute("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                                   (projekt, TYP_WP))]
    if len(ids) != 1:
        raise LookupError("Projekt %d fuehrt %d Waermepumpen statt einer" % (projekt, len(ids)))
    return ids[0]


def schon_gesaet(c):
    for p, (a, n, h) in QUELLEN.items():
        if quelle(c, a) != soll_quelle(n, h) or zone(c, p) != KLIMAZONE:
            return False
    if not c.execute("SELECT 1 FROM Tab_Projekt WHERE ID = ? AND Projektname = ?", (NEU, NAME)).fetchall():
        return False
    if anzahl(c, "SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = ?", (NEU,)):
        return False
    return quelle(c, wp_anlage(c, NEU)) == soll_quelle(*NEU_QUELLE) and zone(c, NEU) == KLIMAZONE


def pruefe(c):
    """Anlagen, Regionen und Vorlage - None, wenn alles passt, sonst der Grund."""
    for p, (a, _, _) in QUELLEN.items():
        z = c.execute("SELECT ID_Projekt, ID_Type FROM Tab_Energieanlagen WHERE ID = ?", (a,)).fetchone()
        if z != (p, TYP_WP):
            return "Anlage %d: %r statt Waermepumpe von Projekt %d" % (a, z, p)
        q = quelle(c, a)
        if q["WQ_Typ"] not in (None, "", WQ_TYP):
            return "Anlage %d fuehrt die Quelle %r" % (a, q["WQ_Typ"])
    for p in list(QUELLEN) + [VORLAGE]:
        r = region(c, p)
        besitzer = c.execute("SELECT ID_Projekt FROM Tab_Klimaregion WHERE ID = ?", (r,)).fetchone()
        nutzer = [x[0] for x in c.execute("SELECT ID FROM Tab_Projekt WHERE ID_Klimaregion = ?", (r,))]
        if besitzer != (p,) or nutzer != [p]:
            return "Klimaregion %d von Projekt %d: Besitzer %r, Nutzer %r" % (r, p, besitzer, nutzer)
    name = c.execute("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (VORLAGE,)).fetchone()
    if name != (VORLAGE_NAME,):
        return "Vorlage %d heisst %r statt %r" % (VORLAGE, name, VORLAGE_NAME)
    q = quelle(c, wp_anlage(c, VORLAGE))
    if (q["WQ_Typ"], q["WQ_Quellsystem"], q["WQ_Anzahl"], q["WQ_Tiefe"], q["WQ_Bodentyp"]) != \
            (WQ_TYP, QUELLSYSTEM, NEU_QUELLE[0], NEU_QUELLE[1], BODENTYP):
        return "Vorlage %d: Quelle %r statt Sonde 4 x 90 m Mergel/Lehm" % (VORLAGE, q)
    if anzahl(c, "SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", (VORLAGE,)):
        return "Vorlage %d fuehrt eine Speicherauslegung - deren Bezugsnachzug fehlt hier" % VORLAGE
    return None


def quelle_setzen(c, anlage, anzahl_sonden, tiefe):
    n = c.execute("UPDATE Tab_Energieanlagen SET WQ_Typ = ?, WQ_Quellsystem = ?, WQ_Anzahl = ?, WQ_Tiefe = ?, "
                  "WQ_Bodentyp = ?, WQ_Flaeche = NULL, WQ_Spreizung = NULL WHERE ID = ?",
                  (WQ_TYP, QUELLSYSTEM, anzahl_sonden, tiefe, BODENTYP, anlage)).rowcount
    if n != 1 or quelle(c, anlage) != soll_quelle(anzahl_sonden, tiefe):
        raise RuntimeError("Anlage %d: Quelle nicht gesetzt" % anlage)


def zone_setzen(c, projekt):
    if c.execute("UPDATE Tab_Klimaregion SET Klimazone_DIN4710 = ? WHERE ID = ?",
                 (KLIMAZONE, region(c, projekt))).rowcount != 1:
        raise RuntimeError("Klimaregion von %d: Zone nicht gesetzt" % projekt)


def anker_nachziehen(c, projekt):
    """KostenProjektPositionenCtrl.AnkerNachziehen: Geraeteanker auf die kopierten Geraete."""
    for kid, komp in c.execute("SELECT ID, Komponente FROM Tab_KostenKomponente").fetchall():
        sp = ANKER.get(komp)
        if sp is None:
            continue
        anlage = "FROM Tab_Energieanlagen AS a WHERE a.ID = Tab_ProjektWerte.ID_Anlage AND a.ID_Projekt = ?"
        c.execute("UPDATE Tab_ProjektWerte SET ID_AnlageGeraet = (SELECT a.[%s] %s) WHERE ProjektID = ? "
                  "AND KomponentenID = ? AND EXISTS (SELECT 1 %s)" % (sp, anlage, anlage),
                  (projekt, projekt, kid, projekt))
        c.execute("UPDATE Tab_ProjektWerte SET ID_AnlageGeraet = NULL WHERE ProjektID = ? AND KomponentenID = ? "
                  "AND ID_AnlageGeraet IS NOT NULL AND NOT EXISTS (SELECT 1 %s)" % anlage,
                  (projekt, kid, projekt))


def verweise_auf_vorlage(c, m, specs):
    """Zaehlt Zellen der Kopie, die auf eine Zeile der Vorlage zeigen (ueber die Zielermittlung des Programms)."""
    fks = m.echte_fks(c)
    kopiert = {s.tabelle.lower(): s for s in specs if not s.ergebnis}
    alt = {}
    for t, s in kopiert.items():
        alt[t] = {r[0] for r in c.execute("SELECT [%s] FROM [%s] WHERE %s" % (s.pk, s.tabelle, m.filter_fuer(s, VORLAGE)))}
    treffer = []
    for t, s in kopiert.items():
        for col in s.spalten:
            ziel = m.ermittle_ziel(fks, s.tabelle, col, s.pk)
            if ziel is None or ziel.lower() not in kopiert or col.lower() == s.pk.lower() or not alt[ziel.lower()]:
                continue
            werte = {r[0] for r in c.execute("SELECT [%s] FROM [%s] WHERE %s" % (col, s.tabelle, m.filter_fuer(s, NEU)))}
            boese = werte & alt[ziel.lower()]
            if boese:
                treffer.append("%s.%s -> %s %r" % (s.tabelle, col, ziel, sorted(boese)[:5]))
    geraete = {r[0] for r in c.execute("SELECT ID_AnlageGeraet FROM Tab_ProjektWerte WHERE ProjektID = ? "
                                       "AND ID_AnlageGeraet IS NOT NULL", (NEU,))}
    for sp, tab in (("ID_WP", "Tab_WP"), ("ID_Kessel", "Tab_Heizkessel"), ("ID_BHKW", "Tab_BHKW"), ("ID_PV", "Tab_PV"),
                    ("ID_Solar", "Tab_Solarkollektoren"), ("ID_SP", "Tab_Stromspeicher"),
                    ("ID_PUFFER", "Tab_Pufferspeicher")):
        vorlage_geraete = {r[0] for r in c.execute("SELECT ID FROM [%s] WHERE ID_Projekt = ?" % tab, (VORLAGE,))}
        if geraete & vorlage_geraete:
            treffer.append("Tab_ProjektWerte.ID_AnlageGeraet -> %s %r" % (tab, sorted(geraete & vorlage_geraete)))
    return treffer


def main():
    if len(sys.argv) < 2:
        print("Aufruf: erdreichquellen_referenzprojekte.py <Kenndaten_Test.sqlite>")
        return 2
    con = sqlite3.connect(sys.argv[1])
    con.isolation_level = None
    try:
        con.execute("PRAGMA foreign_keys = ON")
        belegt = con.execute("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (NEU,)).fetchall()
        if belegt and belegt[0][0] == NAME and schon_gesaet(con):
            print("Quellen, Zonen und Projekt %d \"%s\" stehen schon - Abbruch ohne Schreiben." % (NEU, NAME))
            return 0
        if belegt:
            print("Id %d ist von \"%s\" belegt, der Stand ist unvollstaendig - Abbruch ohne Schreiben."
                  % (NEU, belegt[0][0]))
            return 2
        grund = pruefe(con)
        if grund:
            print(grund + " - Abbruch ohne Schreiben.")
            return 2

        m = kopiermaschine()
        stempel_vorher = dict(con.execute("SELECT ID, Kosten_Geaendert FROM Tab_Projekt").fetchall())
        con.execute("BEGIN")
        try:
            for p, (a, n, h) in QUELLEN.items():
                quelle_setzen(con, a, n, h)
                zone_setzen(con, p)
                print("gesetzt: Projekt %d Anlage %d Sonde %d x %.0f m %s, Region %d Zone %d"
                      % (p, a, n, h, BODENTYP, region(con, p), KLIMAZONE))

            neu_id, specs, bericht = m.duplizieren(con)
            if neu_id != NEU:
                raise RuntimeError("die Kopie faellt auf Id %d statt %d" % (neu_id, NEU))
            print("Kopie %d -> %d \"%s\" (Kopierweg des Programms):" % (VORLAGE, NEU, NAME))
            for tabelle, n in bericht:
                if n:
                    print("  %-32s %6d Zeilen" % (tabelle, n))
            print("  zusammen %d Zeilen in %d Tabellen" % (sum(n for _, n in bericht), sum(1 for _, n in bericht if n)))
            if m.gegenprobe(con, specs):
                raise RuntimeError("Tabelle(n) mit ungleicher Zeilenzahl")
            anker_nachziehen(con, NEU)
            boese = verweise_auf_vorlage(con, m, specs)
            if boese:
                raise RuntimeError("Verweise der Kopie auf die Vorlage: " + "; ".join(boese))
            print("Gegenprobe: Zeilenzahlen gleich der Vorlage, kein Verweis auf Zeilen von %d." % VORLAGE)

            if con.execute("DELETE FROM Tab_Variante WHERE ID_Projekt = ?", (NEU,)).rowcount != 1:
                raise RuntimeError("Tab_Variante: die Variantenzeile der Kopie fehlt")
            if con.execute("UPDATE Tab_Projekt SET Beschreibung = ? WHERE ID = ?", (BESCHREIBUNG, NEU)).rowcount != 1:
                raise RuntimeError("Tab_Projekt %d: Beschreibung nicht gesetzt" % NEU)
            wp = wp_anlage(con, NEU)
            quelle_setzen(con, wp, *NEU_QUELLE)
            zone_setzen(con, NEU)
            if region(con, NEU) == region(con, VORLAGE):
                raise RuntimeError("die Kopie teilt die Klimaregion der Vorlage")
            print("gesetzt: Projekt %d Anlage %d Sonde %d x %.0f m %s, Region %d Zone %d"
                  % (NEU, wp, NEU_QUELLE[0], NEU_QUELLE[1], BODENTYP, region(con, NEU), KLIMAZONE))
            # Die Schreibwege stempeln ueber die Trigger des Kostenstempels die Uhrzeit des Laufs in
            # Kosten_Geaendert - an 1057 und ueber die Variantenzeile an 1026. Jeder Stempel bleibt, wie er
            # vor dem Lauf war; die Kopie bekommt keinen (Muster 1051/1052/1055/1056).
            con.execute("UPDATE Tab_Projekt SET Kosten_Geaendert = NULL WHERE ID = ?", (NEU,))
            for pid, stempel in stempel_vorher.items():
                con.execute("UPDATE Tab_Projekt SET Kosten_Geaendert = ? WHERE ID = ?", (stempel, pid))
            if dict(con.execute("SELECT ID, Kosten_Geaendert FROM Tab_Projekt WHERE ID <> ?", (NEU,)).fetchall()) \
                    != stempel_vorher:
                raise RuntimeError("Kostenstempel nicht wiederhergestellt")
            if not schon_gesaet(con):
                raise RuntimeError("Gegenprobe der gesaeten Zellen misslungen")
            fremd = con.execute("PRAGMA foreign_key_check").fetchall()
            if fremd:
                raise RuntimeError("foreign_key_check: %d Zeilen" % len(fremd))
            con.execute("COMMIT")
        except Exception as ex:
            con.execute("ROLLBACK")
            print("%s - zurueckgerollt, nichts geschrieben." % ex)
            return 2

        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        fremd = con.execute("PRAGMA foreign_key_check").fetchall()
        print("integrity_check: %s; foreign_key_check: %d Zeilen" % (pruefung, len(fremd)))
        return 0 if pruefung == "ok" and not fremd else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
