#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Korrigiert die wirksame Waermekapazitaet `Bauweise` des Gebaeudes 10576 der Testdatenbank
(Stufe GB der Gebaeudesimulation, Befund D; Entscheid E4 zu Q22).

WAS FALSCH WAR. Gebaeude 10576 "MFH-H-U-112" (Projekt 1008 "Heinestr 15", 304 m2) trug
`Tab_Gebaeude.Bauweise = 50` Wh/K - den nackten Rueckfallwert von
`Gebaeudebauweise.BauweiseAusBauart` (frueher kam dort der Index der Gebaeudeart-Liste
herein) statt der Kapazitaet des ganzen Gebaeudes. Das sind 0,16 Wh/(m2K) statt der
50 Wh/(m2K) der Bauart "schwer", die alle anderen Referenzgebaeude tragen; die
Zeitkonstante des Tagesbilanz-Modells bricht damit auf Minuten zusammen.

WAS DIESES SKRIPT TUT. Genau EINE Zelle: `Tab_Gebaeude.Bauweise` der Zeile 10576 wird
auf 50 Wh/(m2K) x 304 m2 = 15 200 Wh/K gesetzt. Nichts sonst - kein VACUUM, keine
andere Zeile, kein Katalogsatz (`Tab_Gebaeude_STAMM`) und kein Nicht-Referenzprojekt,
auch wenn dort derselbe Rueckfallwert steht.

WIEDERHOLBAR. Steht der Wert schon auf 15 200, aendert das Skript nichts und meldet das.
Steht dort etwas anderes als 50 oder 15 200, oder weicht die Wohnflaeche von 304 m2 ab,
bricht es ohne Schreiben ab (Rueckgabe 2).

FOLGE. Die Zeile gehoert zu den gesaeten Gebaeudedaten (Einfrierregel "gesaete
Gebaeudedaten", `Referenzlaeufe/LIESMICH.md`): Wer sie aendert, friert die Referenzbasis
im selben Schritt neu ein. Betroffen ist Projekt 1008 (Basis
`2026-09-22_R11_Bestandsbefunde`).

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/gebaeude_10576_bauweise.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

GEBAEUDE = 10576
WOHNFLAECHE = 304.0
ALT = 50.0
NEU = 15200.0   # 50 Wh/(m2K) x 304 m2


def main():
    if len(sys.argv) < 2:
        print("Aufruf: gebaeude_10576_bauweise.py <Kenndaten_Test.sqlite>")
        return 2

    con = sqlite3.connect(sys.argv[1])
    try:
        # Ab Schemaschritt 101 heisst die Bezugsflaeche Nutzflaeche (E19).
        spalten = [r[1] for r in con.execute("PRAGMA table_info(Tab_Gebaeude)")]
        flaechenspalte = "Nutzflaeche" if "Nutzflaeche" in spalten else "Wohnflaeche"
        zeile = con.execute(
            "SELECT Gebaeudename, ID_Projekt, " + flaechenspalte +
            ", Bauweise FROM Tab_Gebaeude WHERE ID = ?",
            (GEBAEUDE,)).fetchone()
        if zeile is None:
            print(f"Gebaeude {GEBAEUDE} fehlt - Abbruch ohne Schreiben.")
            return 2

        name, projekt, flaeche, bauweise = zeile
        print(f"vorher:  Gebaeude {GEBAEUDE} \"{name}\", Projekt {projekt}, "
              f"Wohnflaeche {flaeche} m2, Bauweise {bauweise} Wh/K")

        if flaeche != WOHNFLAECHE:
            print(f"Wohnflaeche {flaeche} statt {WOHNFLAECHE} - Abbruch ohne Schreiben.")
            return 2
        if bauweise == NEU:
            print("Bauweise steht schon auf dem korrigierten Wert - nichts zu tun.")
            return 0
        if bauweise != ALT:
            print(f"Bauweise {bauweise} ist weder {ALT} noch {NEU} - Abbruch ohne Schreiben.")
            return 2

        with con:
            geaendert = con.execute(
                "UPDATE Tab_Gebaeude SET Bauweise = ? WHERE ID = ? AND Bauweise = ?",
                (NEU, GEBAEUDE, ALT)).rowcount
        if geaendert != 1:
            print(f"{geaendert} Zeilen geaendert statt einer - bitte pruefen.")
            return 2

        nachher = con.execute("SELECT Bauweise FROM Tab_Gebaeude WHERE ID = ?",
                              (GEBAEUDE,)).fetchone()[0]
        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        print(f"nachher: Bauweise {nachher} Wh/K; integrity_check: {pruefung}")
        return 0 if nachher == NEU and pruefung == "ok" else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
