#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Stellt das Gebaeude des Referenzprojekts 1040 der Testdatenbank ausdruecklich auf den
Tagesbilanz-Weg (Gebaeudesimulation, Schlusswelle G1 + G2; Architekturfrage A15, mit E27
entschieden; Systementwurf 8.4, Umsetzungskonzept 1.8/1.9).

WARUM. Mit G1 + G2 rechnet ein Gebaeude ohne Angabe (`Gebaeude_Modell` NULL) nach
VDI 6007. Bis zur Stufe GA ("Altweg abloesen") steht GENAU EIN Referenzprojekt auf dem
Tagesbilanz-Weg und wird in der jeweils aktuellen Basis mit eingefroren; an ihm haengt der
Rueckweg-Test (`EPOS.Kern.Tests/GebaeudeRueckwegTests`). Gewaehlt ist Projekt 1040
"zwei Puffer je Kanal": ein Gebaeude (10645, "EFH-A-U-347s", 201 m2), nicht in den fuenf
Projekten der CI, und dasselbe Katalogobjekt steht in 1041, 1042 und 1045 auf dem VDI-Weg -
die Basis fuehrt so beide Rechenwege am selben Gebaeude.

WAS DIESES SKRIPT TUT. Genau EINE Zelle: `Tab_Gebaeude.Gebaeude_Modell` der Zeile 10645 wird
von NULL auf 'TAGESBILANZ' gesetzt. Nichts sonst - kein VACUUM, keine andere Zeile, kein
Katalogsatz (`Tab_Gebaeude_STAMM`).

WIEDERHOLBAR. Steht der Wert schon auf 'TAGESBILANZ', aendert das Skript nichts. Steht dort
etwas anderes als NULL oder 'TAGESBILANZ', gehoert die Zeile nicht zu Projekt 1040 oder hat
das Projekt mehr als dieses eine Gebaeude, bricht es ohne Schreiben ab (Rueckgabe 2).

FOLGE. Die Zelle gehoert zu den gesaeten Gebaeudedaten (Einfrierregel "gesaete
Gebaeudedaten", `Referenzlaeufe/LIESMICH.md`); sie ist Teil des Einfrierschritts G1 + G2.
Mit der Stufe GA wird sie auf NULL zurueckgesetzt (Loeschliste, Umsetzungskonzept 6.1).

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/gebaeude_1040_tagesbilanz.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

PROJEKT = 1040
GEBAEUDE = 10645
NEU = "TAGESBILANZ"


def main():
    if len(sys.argv) < 2:
        print("Aufruf: gebaeude_1040_tagesbilanz.py <Kenndaten_Test.sqlite>")
        return 2

    con = sqlite3.connect(sys.argv[1])
    try:
        gebaeude = con.execute(
            "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? ORDER BY ID", (PROJEKT,)).fetchall()
        if [g[0] for g in gebaeude] != [GEBAEUDE]:
            print(f"Projekt {PROJEKT} fuehrt {gebaeude} statt genau Gebaeude {GEBAEUDE} - "
                  "Abbruch ohne Schreiben.")
            return 2

        name, modell = con.execute(
            "SELECT Gebaeudename, Gebaeude_Modell FROM Tab_Gebaeude WHERE ID = ?",
            (GEBAEUDE,)).fetchone()
        print(f"vorher:  Gebaeude {GEBAEUDE} \"{name}\", Projekt {PROJEKT}, Gebaeude_Modell {modell!r}")

        if modell == NEU:
            print("Gebaeude_Modell steht schon auf TAGESBILANZ - nichts zu tun.")
            return 0
        if modell is not None:
            print(f"Gebaeude_Modell {modell!r} ist weder NULL noch {NEU!r} - Abbruch ohne Schreiben.")
            return 2

        with con:
            geaendert = con.execute(
                "UPDATE Tab_Gebaeude SET Gebaeude_Modell = ? WHERE ID = ? AND Gebaeude_Modell IS NULL",
                (NEU, GEBAEUDE)).rowcount
        if geaendert != 1:
            print(f"{geaendert} Zeilen geaendert statt einer - bitte pruefen.")
            return 2

        nachher = con.execute("SELECT Gebaeude_Modell FROM Tab_Gebaeude WHERE ID = ?",
                              (GEBAEUDE,)).fetchone()[0]
        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        print(f"nachher: Gebaeude_Modell {nachher!r}; integrity_check: {pruefung}")
        return 0 if nachher == NEU and pruefung == "ok" else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
