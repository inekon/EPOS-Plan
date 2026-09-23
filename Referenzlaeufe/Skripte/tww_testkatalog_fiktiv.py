#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Spielt den FIKTIVEN Testkatalog des Zapfprofilgenerators in die Testdatenbank ein
(Umsetzungskonzept Zapfprofilgenerator, Stufe Z0, Posten P4; Kapitel 6 (b)).

WARUM FIKTIV. Die Testdatenbank ist Messlatte fuer Tests, Referenzlauf und CI und liegt im
Repository. Normzahlen, Kennwerte und Referenzprofile duerfen dort nie stehen (Kapitel 6 (a)
bis (c)); die Auslieferungswerte kommen aus einem Katalogpaket ausserhalb des Repositoriums.
Damit Controller, Dialog und spaetere Stufen trotzdem einen lesbaren Katalog vorfinden, traegt
die Testdatenbank einen kleinen, in sich stimmigen Satz mit ERFUNDENEN, runden Werten:

  - ein Tagesgangsatz mit den vier Tagtypen (je 24 Stundenanteile, Summe 1);
  - drei Nutzungsarten, alle auf diesen Satz (Monatsfaktoren Mittel 1, Wochenfaktoren Summe 1);
  - drei Parameter mit neutralen Schluesseln "Test.*" (keine Normkonstante, kein Normname);
  - die fuenfzehn Parameter des Bilanzrechenwegs (Schluessel wie ZapfParameter in
    EPOS.Kern/Allgemein/Zapfprofil/Zapfprofileingang.cs) mit runden, ERFUNDENEN Werten - kein
    Wert faellt mit einer Normvorgabe zusammen; sie machen den Generatorweg auf einer
    Projektkopie der Testdatenbank rechenbar (Stufe Z1, Gruppe 2);
  - ein Bedarfstag (Konstruktor) mit drei Ereignissen;
  - vier DIN-4708-Werte (zwei Belegungen, zwei Ausstattungsklassen) mit erfundenen Zahlen.

Jede Zeile: Status 'EIGEN', ReadOnly 0, Herkunftsart 'FIKTIV', Quelle "Testkatalog (fiktiv)",
Katalogversion "TEST-1", kein Beleg. KEINE Zeile mit Status 'AUSLIEFERUNG' oder 'IMPORT', keine
Zone und keine Zeile in Tab_TwwProjekt - kein Projekt steht auf dem Generator, der
Referenzlauf bleibt unberuehrt.

VORAUSSETZUNG. Schemastand 103 (die zehn Tww-Tabellen), nachgezogen mit
    dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite

WIEDERHOLBAR. Jede Zeile wird nur angelegt, wenn ihr natuerlicher Schluessel fehlt; die
Ereignisse des Bedarfstags nur zusammen mit ihrem neu angelegten Kopf. Eine vorhandene
Nutzungsart dieses Katalogs, deren Bezugstemperaturen von BEZUG_ZAPF/BEZUG_KALT abweichen, wird
auf diese nachgefuehrt - so erreicht ein geaenderter erfundener Wert die Testdatenbank; ebenso
ein vorhandener Parameter mit anderem Wert oder anderer Einheit und ein vorhandener DIN-4708-Wert
mit anderem Wert. Ein zweiter Lauf aendert nichts und meldet das. Steht in einer Tww-Katalogtabelle schon eine Zeile, die NICHT zu
diesem Katalog gehoert, bricht das Skript ohne Schreiben ab (Rueckgabe 2).

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

QUELLE = "Testkatalog (fiktiv)"
VERSION = "TEST-1"
HERKUNFT = "FIKTIV"
STATUS = "EIGEN"

SATZ = "Testsatz (fiktiv)"


def anteile(stunden_werte):
    """24 Stundenanteile: die genannten Stunden (1..24) mit ihrem Wert, sonst 0."""
    return [stunden_werte.get(h, 0.0) for h in range(1, 25)]


# Tagtyp 1 Werktag, 2 Samstag, 3 Sonn-/Feiertag, 4 Ruhetag - erfundene Formen, Summe je 1.
TAGESGAENGE = {
    1: anteile({7: 0.25, 8: 0.25, 19: 0.25, 20: 0.25}),
    2: anteile({9: 0.25, 10: 0.25, 11: 0.25, 12: 0.25}),
    3: anteile({10: 0.25, 11: 0.25, 12: 0.25, 13: 0.25}),
    4: anteile({h: 0.125 for h in range(8, 16)}),
}

# (Bezeichner, Bezugsart, Bedarf niedrig/mittel/hoch, Bilanzgrenze, Kalenderart,
#  Ferienfaktor, Monatsfaktoren, Wochenfaktoren Mo..So)
NUTZUNGSARTEN = [
    ("Testnutzung A (fiktiv)", 1, (1.0, 2.0, 3.0), 1, 1, None,
     [1.0] * 12, [0.15, 0.15, 0.15, 0.15, 0.15, 0.125, 0.125]),
    ("Testnutzung B (fiktiv)", 6, (0.5, 1.0, 2.0), 2, 2, None,
     [1.0] * 12, [0.2, 0.2, 0.2, 0.2, 0.2, 0.0, 0.0]),
    ("Testnutzung C (fiktiv)", 3, (2.0, 4.0, 6.0), 3, 4, 0.5,
     [0.5, 0.5, 1.0, 1.0, 1.5, 1.5, 1.5, 1.5, 1.0, 1.0, 0.5, 0.5],
     [0.1, 0.1, 0.1, 0.1, 0.2, 0.2, 0.2]),
]
# Bezugstemperaturen der Bedarfswerte (Grad C): erkennbar erfunden, KEIN normativer Wert - eine
# Kaltwassertemperatur, die mit einer Normvorgabe zusammenfiele, waere keine erfundene Zahl
# (Kapitel 6 (a), (b)). Ergebnisneutral: kein Referenzprojekt nutzt den Generator.
BEZUG_ZAPF = 50.0
BEZUG_KALT = 12.0

# (Schluessel, Wert, Einheit) - neutrale Schluessel, runde erfundene Werte.
PARAMETER = [
    ("Test.Faktor", 2.0, "-"),
    ("Test.Grenze", 100.0, "l"),
    ("Test.Dauer", 10.0, "min"),
]

# Die fuenfzehn Schluessel des Bilanzrechenwegs (ZapfParameter) - Werte rund und ERFUNDEN,
# bewusst neben jeder Normvorgabe gewaehlt (Kapitel 6 (a)): kein Kaltwassermittel der Norm, keine
# Laufzeit oder Kennwerte eines Regelwerks, keine Koeffizienten eines Verfahrens.
PARAMETER += [
    ("Kaltwasser.Bilanz.Mittel", 11.0, "°C"),
    ("Kaltwasser.Bilanz.Amplitude", 3.0, "K"),
    ("Kaltwasser.Bilanz.MonatMaximum", 9.0, "Monat"),
    ("DIN18599.Wohnen.a", 20.0, "kWh/(m²·a)"),
    ("DIN18599.Wohnen.b", 0.1, "kWh/(m⁴·a)"),
    ("DIN18599.Wohnen.c", 5.0, "kWh/(m²·a)"),
    ("Wohnen.FlaecheJeWe", 80.0, "m²"),
    ("Zirkulation.Anteil", 0.2, "-"),
    ("Zirkulation.Laufzeit", 20.0, "h"),
    ("Zirkulation.Lage", 1.0, "-"),
    ("Zirkulation.Kennwert.Lage1", 5.0, "kWh/(m²·a)"),
    ("Zirkulation.Kennwert.Lage2", 10.0, "kWh/(m²·a)"),
    ("Zirkulation.VerlustJeMeter", 8.0, "W/m"),
    ("Zapfprofil.Messwert.Rueckfrageschwelle", 0.5, "-"),
    ("Zapfprofil.Formvektor.Warnschwelle", 0.01, "-"),
]

BEDARFSTAG = "Testbedarfstag (fiktiv)"
BEDARFSTAG_QUELLE_ART = 4          # Konstruktor
BEDARFSTAG_BEZUGSMENGE = 10.0
# (Minute_Beginn, Dauer_min, Energie_Kwh, Reihenfolge)
EREIGNISSE = [
    (420, 10, 1.0, 1),
    (720, 5, 0.5, 2),
    (1140, 20, 2.0, 3),
]

# (Art, Schluessel, Wert)
DIN4708_WERTE = [
    ("BELEGUNG", "2", 1.0),
    ("BELEGUNG", "4", 3.0),
    ("AUSSTATTUNG", "Testklasse A", 10.0),
    ("AUSSTATTUNG", "Testklasse B", 20.0),
]

KATALOGTABELLEN = [
    "Tab_TwwTagesgangsatz_STAMM", "Tab_TwwTagesgang_STAMM", "Tab_TwwNutzungsart_STAMM",
    "Tab_TwwBedarfstag_STAMM", "Tab_TwwBedarfstagEreignis_STAMM", "Tab_TwwParameter_STAMM",
    "Tab_TwwDin4708Wert_STAMM",
]

# Die erwartete Zeilenzahl je Tabelle nach dem Lauf.
ERWARTET = {
    "Tab_TwwTagesgangsatz_STAMM": 1,
    "Tab_TwwTagesgang_STAMM": len(TAGESGAENGE),
    "Tab_TwwNutzungsart_STAMM": len(NUTZUNGSARTEN),
    "Tab_TwwBedarfstag_STAMM": 1,
    "Tab_TwwBedarfstagEreignis_STAMM": len(EREIGNISSE),
    "Tab_TwwParameter_STAMM": len(PARAMETER),
    "Tab_TwwDin4708Wert_STAMM": len(DIN4708_WERTE),
    "Tab_TwwZone": 0,
    "Tab_TwwWohnungstyp": 0,
    "Tab_TwwProjekt": 0,
}


def pruefe_summen():
    """Die erfundenen Formen sind in sich stimmig - vor jedem Schreiben."""
    for t, a in TAGESGAENGE.items():
        assert abs(sum(a) - 1.0) < 1e-12, f"Tagtyp {t}: Summe {sum(a)}"
    for n in NUTZUNGSARTEN:
        assert len(n[6]) == 12 and abs(sum(n[6]) / 12.0 - 1.0) < 1e-12, f"{n[0]}: Monatsmittel"
        assert len(n[7]) == 7 and abs(sum(n[7]) - 1.0) < 1e-12, f"{n[0]}: Wochensumme"


def zahl(con, sql, *p):
    return con.execute(sql, p).fetchone()[0]


def main():
    if len(sys.argv) < 2:
        print("Aufruf: tww_testkatalog_fiktiv.py <Kenndaten_Test.sqlite>")
        return 2
    pruefe_summen()

    con = sqlite3.connect(sys.argv[1])
    try:
        con.execute("PRAGMA foreign_keys = ON")
        stand = zahl(con, "SELECT SchemaVersion FROM Tab_Applikation")
        fehlend = [t for t in ERWARTET
                   if zahl(con, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", t) == 0]
        if fehlend:
            print(f"Schemastand {stand}: es fehlen {', '.join(fehlend)} - erst Werkzeuge/Testdatenbankschema. "
                  "Abbruch ohne Schreiben.")
            return 2

        # Fremde Zeilen? Alles, was nicht Katalogversion TEST-1 / Status EIGEN / FIKTIV ist.
        fremd = 0
        for t in ("Tab_TwwTagesgangsatz_STAMM", "Tab_TwwNutzungsart_STAMM", "Tab_TwwBedarfstag_STAMM",
                  "Tab_TwwParameter_STAMM", "Tab_TwwDin4708Wert_STAMM"):
            fremd += zahl(con, f'SELECT COUNT(*) FROM "{t}" WHERE "Katalogversion" <> ? OR "Status" <> ? '
                               'OR "ReadOnly" <> 0', VERSION, STATUS)
        for t in ("Tab_TwwZone", "Tab_TwwWohnungstyp", "Tab_TwwProjekt"):
            fremd += zahl(con, f'SELECT COUNT(*) FROM "{t}"')
        if fremd:
            print(f"{fremd} Zeile(n) gehoeren nicht zum fiktiven Testkatalog - Abbruch ohne Schreiben.")
            return 2

        angelegt = 0
        nachgefuehrt = 0
        with con:
            # --- Tagesgangsatz und seine vier Tagesgaenge -------------------------------
            if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwTagesgangsatz_STAMM" WHERE "Bezeichner" = ? '
                         'AND "Katalogversion" = ?', SATZ, VERSION) == 0:
                con.execute('INSERT INTO "Tab_TwwTagesgangsatz_STAMM" ("Bezeichner", "Katalogversion", '
                            '"Status", "Beleg", "ReadOnly") VALUES (?, ?, ?, NULL, 0)', (SATZ, VERSION, STATUS))
                angelegt += 1
            id_satz = zahl(con, 'SELECT "ID" FROM "Tab_TwwTagesgangsatz_STAMM" WHERE "Bezeichner" = ? '
                                'AND "Katalogversion" = ?', SATZ, VERSION)

            spalten = ", ".join(f'"Anteil_{h:02d}"' for h in range(1, 25))
            platz = ", ".join("?" for _ in range(24))
            for tagtyp, werte in TAGESGAENGE.items():
                if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwTagesgang_STAMM" WHERE "ID_Tagesgangsatz" = ? '
                             'AND "Tagtyp" = ?', id_satz, tagtyp) == 0:
                    con.execute(f'INSERT INTO "Tab_TwwTagesgang_STAMM" ("ID_Tagesgangsatz", "Tagtyp", {spalten}, '
                                f'"Quelle", "Ausgabe", "Version", "Herkunftsart") VALUES (?, ?, {platz}, ?, NULL, ?, ?)',
                                (id_satz, tagtyp, *werte, QUELLE, VERSION, HERKUNFT))
                    angelegt += 1

            # --- Nutzungsarten ----------------------------------------------------------
            for (name, bezug, bedarf, grenze, kalender, ferien, monate, woche) in NUTZUNGSARTEN:
                if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwNutzungsart_STAMM" WHERE "Bezeichner" = ? '
                             'AND "Katalogversion" = ?', name, VERSION) > 0:
                    continue
                namen = ["Bezeichner", "Katalogversion", "Bezugsart",
                         "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch",
                         "Bedarf_Quelle", "Bedarf_Ausgabe", "Bedarf_Version", "Bedarf_Herkunftsart",
                         "Bezug_Zapftemperatur", "Bezug_Kaltwasser", "Bilanzgrenze", "Kalenderart", "Ferienfaktor"]
                werte = [name, VERSION, bezug, *bedarf,
                         QUELLE, None, VERSION, HERKUNFT,
                         BEZUG_ZAPF, BEZUG_KALT, grenze, kalender, ferien]
                namen += [f"Monat_{m}" for m in range(1, 13)]
                werte += monate
                namen += ["Jahresgang_Quelle", "Jahresgang_Ausgabe", "Jahresgang_Version", "Jahresgang_Herkunftsart"]
                werte += [QUELLE, None, VERSION, HERKUNFT]
                namen += [f"Woche_{w}" for w in range(1, 8)]
                werte += woche
                namen += ["Wochengang_Quelle", "Wochengang_Ausgabe", "Wochengang_Version", "Wochengang_Herkunftsart",
                          "ID_Tagesgangsatz", "ID_Vorlage", "Status", "Beleg", "Freigabe", "ReadOnly"]
                werte += [QUELLE, None, VERSION, HERKUNFT, id_satz, None, STATUS, None, None, 0]
                con.execute(f'INSERT INTO "Tab_TwwNutzungsart_STAMM" ({", ".join(chr(34) + n + chr(34) for n in namen)}) '
                            f'VALUES ({", ".join("?" for _ in namen)})', werte)
                angelegt += 1

            # --- Bezugstemperaturen vorhandener Zeilen nachfuehren -------------------------
            for (name, *_rest) in NUTZUNGSARTEN:
                cur = con.execute('UPDATE "Tab_TwwNutzungsart_STAMM" SET "Bezug_Zapftemperatur" = ?, '
                                  '"Bezug_Kaltwasser" = ? WHERE "Bezeichner" = ? AND "Katalogversion" = ? '
                                  'AND ("Bezug_Zapftemperatur" <> ? OR "Bezug_Kaltwasser" <> ?)',
                                  (BEZUG_ZAPF, BEZUG_KALT, name, VERSION, BEZUG_ZAPF, BEZUG_KALT))
                nachgefuehrt += cur.rowcount

            # --- Parameter --------------------------------------------------------------
            for (schluessel, wert, einheit) in PARAMETER:
                if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwParameter_STAMM" WHERE "Schluessel" = ? '
                             'AND "Katalogversion" = ?', schluessel, VERSION) > 0:
                    # Vorhanden: Wert und Einheit nachfuehren, wenn sie abweichen.
                    cur = con.execute('UPDATE "Tab_TwwParameter_STAMM" SET "Wert" = ?, "Einheit" = ? '
                                      'WHERE "Schluessel" = ? AND "Katalogversion" = ? '
                                      'AND ("Wert" <> ? OR "Einheit" IS NOT ?)',
                                      (wert, einheit, schluessel, VERSION, wert, einheit))
                    nachgefuehrt += cur.rowcount
                    continue
                con.execute('INSERT INTO "Tab_TwwParameter_STAMM" ("Schluessel", "Wert", "Einheit", "Katalogversion", '
                            '"Quelle", "Ausgabe", "Version", "Herkunftsart", "Status", "Beleg", "ReadOnly") '
                            'VALUES (?, ?, ?, ?, ?, NULL, ?, ?, ?, NULL, 0)',
                            (schluessel, wert, einheit, VERSION, QUELLE, VERSION, HERKUNFT, STATUS))
                angelegt += 1

            # --- Bedarfstag samt Ereignissen (nur mit neu angelegtem Kopf) ----------------
            if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwBedarfstag_STAMM" WHERE "Bezeichner" = ? '
                         'AND "Katalogversion" = ?', BEDARFSTAG, VERSION) == 0:
                cur = con.execute('INSERT INTO "Tab_TwwBedarfstag_STAMM" ("Bezeichner", "Katalogversion", '
                                  '"Quelle_Art", "Bezugsmenge", "Quelle", "Ausgabe", "Version", "Herkunftsart", '
                                  '"Status", "Beleg", "ReadOnly") VALUES (?, ?, ?, ?, ?, NULL, ?, ?, ?, NULL, 0)',
                                  (BEDARFSTAG, VERSION, BEDARFSTAG_QUELLE_ART, BEDARFSTAG_BEZUGSMENGE,
                                   QUELLE, VERSION, HERKUNFT, STATUS))
                id_tag = cur.lastrowid
                angelegt += 1
                for (beginn, dauer, energie, reihenfolge) in EREIGNISSE:
                    con.execute('INSERT INTO "Tab_TwwBedarfstagEreignis_STAMM" ("ID_Bedarfstag", "Minute_Beginn", '
                                '"Dauer_min", "Energie_Kwh", "Reihenfolge") VALUES (?, ?, ?, ?, ?)',
                                (id_tag, beginn, dauer, energie, reihenfolge))
                    angelegt += 1

            # --- DIN-4708-Werte ---------------------------------------------------------
            for (art, schluessel, wert) in DIN4708_WERTE:
                if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwDin4708Wert_STAMM" WHERE "Art" = ? AND "Schluessel" = ? '
                             'AND "Katalogversion" = ?', art, schluessel, VERSION) > 0:
                    # Vorhanden: den Wert nachfuehren, wenn er abweicht.
                    cur = con.execute('UPDATE "Tab_TwwDin4708Wert_STAMM" SET "Wert" = ? WHERE "Art" = ? '
                                      'AND "Schluessel" = ? AND "Katalogversion" = ? AND "Wert" <> ?',
                                      (wert, art, schluessel, VERSION, wert))
                    nachgefuehrt += cur.rowcount
                    continue
                con.execute('INSERT INTO "Tab_TwwDin4708Wert_STAMM" ("Art", "Schluessel", "Wert", "Katalogversion", '
                            '"Quelle", "Ausgabe", "Version", "Herkunftsart", "Status", "Beleg", "ReadOnly") '
                            'VALUES (?, ?, ?, ?, ?, NULL, ?, ?, ?, NULL, 0)',
                            (art, schluessel, wert, VERSION, QUELLE, VERSION, HERKUNFT, STATUS))
                angelegt += 1

        print(f"Schemastand {stand}: {angelegt} Zeile(n) angelegt, {nachgefuehrt} nachgefuehrt"
              + (" - der Testkatalog stand schon vollstaendig da." if angelegt == 0 and nachgefuehrt == 0 else "."))

        ok = True
        for t, soll in ERWARTET.items():
            ist = zahl(con, f'SELECT COUNT(*) FROM "{t}"')
            print(f"  {t}: {ist} Zeile(n)" + ("" if ist == soll else f"  (erwartet {soll})"))
            ok = ok and ist == soll
        ausgeliefert = sum(zahl(con, f'SELECT COUNT(*) FROM "{t}" WHERE "Status" IN (\'AUSLIEFERUNG\', \'IMPORT\')')
                           for t in ("Tab_TwwTagesgangsatz_STAMM", "Tab_TwwNutzungsart_STAMM",
                                     "Tab_TwwBedarfstag_STAMM", "Tab_TwwParameter_STAMM",
                                     "Tab_TwwDin4708Wert_STAMM"))
        integritaet = con.execute("PRAGMA integrity_check").fetchone()[0]
        fk = con.execute("PRAGMA foreign_key_check").fetchall()
        print(f"  Status AUSLIEFERUNG/IMPORT: {ausgeliefert}; integrity_check: {integritaet}; "
              f"foreign_key_check: {len(fk)} Befund(e)")
        return 0 if ok and ausgeliefert == 0 and integritaet == "ok" and not fk else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
