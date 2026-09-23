#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Korrigiert die wirksame Waermekapazitaet `Bauweise` des Gebaeudes 10612 und des
Katalogsatzes 233 der Testdatenbank - derselbe Datenfehler und dieselbe Herleitung wie bei
Gebaeude 10576 in der Stufe GB (Befund D, Entscheid E4 zu Q22;
`gebaeude_10576_bauweise.py`).

WAS FALSCH WAR. Der Katalogsatz `Tab_Gebaeude_STAMM` 233 "MFH-H-U-112" (304 m2) und seine
Projektkopie `Tab_Gebaeude` 10612 (Projekt 1009, kein Referenzprojekt) tragen
`Bauweise = 50` Wh/K - den nackten Rueckfallwert von `Gebaeudebauweise.BauweiseAusBauart`
statt der Kapazitaet des ganzen Gebaeudes: 0,16 Wh/(m2K) statt der 50 Wh/(m2K) der Bauart
"schwer", die alle Referenzgebaeude tragen. Der VDI-Weg lehnt das Gebaeude deshalb benannt
ab (`BauweiseUnplausibel`), der Tagesbilanz-Weg rechnet mit einer Zeitkonstante von Minuten.
Die Referenzkopie desselben Katalogsatzes (10576, Projekt 1008) ist seit GB korrigiert.

WAS DIESES SKRIPT TUT. Genau ZWEI Zellen: `Bauweise` der Zeilen `Tab_Gebaeude` 10612 und
`Tab_Gebaeude_STAMM` 233 wird auf 50 Wh/(m2K) x 304 m2 = 15 200 Wh/K gesetzt - der Wert
von 10576. Nichts sonst - kein VACUUM, keine andere Zeile, keine andere Spalte.

WIEDERHOLBAR. Steht der Wert schon auf 15 200, aendert das Skript an dieser Zeile nichts und
meldet das. Steht dort etwas anderes als 50 oder 15 200, weicht die Nutzflaeche von 304 m2
oder der Name von "MFH-H-U-112" ab, bricht es VOR jedem Schreiben ab (Rueckgabe 2).

FOLGE. Beide Zeilen gehoeren zu den gesaeten Gebaeudedaten (Einfrierregel "gesaete
Gebaeudedaten", `Referenzlaeufe/LIESMICH.md`). Keine davon rechnet in einem Referenzprojekt:
10612 gehoert zu Projekt 1009, und der Lauf liest die Projektkopien, nicht den Katalog. Der
Referenzlauf aller dreizehn Projekte bleibt byte-gleich; die Basis
`2026-09-23_R12_Gebaeudemodell` wird NICHT neu eingefroren.

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/gebaeude_10612_233_bauweise.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

NAME = "MFH-H-U-112"
FLAECHE = 304.0
ALT = 50.0
NEU = 15200.0   # 50 Wh/(m2K) x 304 m2, wie 10576

# (Tabelle, ID, Namensspalte)
ZEILEN = [
    ("Tab_Gebaeude", 10612, "Gebaeudename"),
    ("Tab_Gebaeude_STAMM", 233, "Bezeichner"),
]


def main():
    if len(sys.argv) < 2:
        print("Aufruf: gebaeude_10612_233_bauweise.py <Kenndaten_Test.sqlite>")
        return 2

    con = sqlite3.connect(sys.argv[1])
    try:
        # 1. Alles pruefen, bevor irgendetwas geschrieben wird.
        offen = []
        for tabelle, zeilen_id, namensspalte in ZEILEN:
            zeile = con.execute(
                "SELECT " + namensspalte + ", Nutzflaeche, Bauweise FROM " + tabelle + " WHERE ID = ?",
                (zeilen_id,)).fetchone()
            if zeile is None:
                print(f"{tabelle} {zeilen_id} fehlt - Abbruch ohne Schreiben.")
                return 2
            name, flaeche, bauweise = zeile
            print(f"vorher:  {tabelle} {zeilen_id} \"{name}\", Nutzflaeche {flaeche} m2, Bauweise {bauweise} Wh/K")
            if name != NAME:
                print(f"Name \"{name}\" statt \"{NAME}\" - Abbruch ohne Schreiben.")
                return 2
            if flaeche != FLAECHE:
                print(f"Nutzflaeche {flaeche} statt {FLAECHE} - Abbruch ohne Schreiben.")
                return 2
            if bauweise == NEU:
                print(f"{tabelle} {zeilen_id}: Bauweise steht schon auf dem korrigierten Wert - nichts zu tun.")
                continue
            if bauweise != ALT:
                print(f"Bauweise {bauweise} ist weder {ALT} noch {NEU} - Abbruch ohne Schreiben.")
                return 2
            offen.append((tabelle, zeilen_id))

        # 2. Schreiben - eine Transaktion, je Zeile genau eine Zelle.
        if offen:
            with con:
                for tabelle, zeilen_id in offen:
                    geaendert = con.execute(
                        "UPDATE " + tabelle + " SET Bauweise = ? WHERE ID = ? AND Bauweise = ?",
                        (NEU, zeilen_id, ALT)).rowcount
                    if geaendert != 1:
                        raise RuntimeError(f"{tabelle} {zeilen_id}: {geaendert} Zeilen statt einer")

        # 3. Nachprobe.
        gut = True
        for tabelle, zeilen_id, _ in ZEILEN:
            nachher = con.execute("SELECT Bauweise FROM " + tabelle + " WHERE ID = ?", (zeilen_id,)).fetchone()[0]
            print(f"nachher: {tabelle} {zeilen_id} Bauweise {nachher} Wh/K")
            gut = gut and nachher == NEU
        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        print(f"integrity_check: {pruefung}")
        return 0 if gut and pruefung == "ok" else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
