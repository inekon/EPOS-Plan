#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Macht Projekt 1017 der Testdatenbank zum Referenzprojekt mit Kuehlung (Stufe KU1 der Kuehlung,
vierte Welle; Kuehlkonzept 10.4, Einfrierregel "gesaete Kaeltedaten").

WARUM 1017. Ein vorhandenes Einzelgebaeude-Projekt auf dem VDI-Weg (Gebaeude 10599
"GMH-D-S-118", 744,4 m2, Skalierung fast 1) mit genau einer Waermepumpe - sie bekommt in KU2
den Kuehlbetrieb - und mit PV und Stromspeicher, der ueblichen Umgebung einer reversiblen
Waermepumpe. 1017 gehoert zu den fuenf Projekten der CI: Die Kuehlung ist damit bei jedem
Push im Regressionsnetz. Nicht 1040 (Tagesbilanz-Weg, A15), nicht 1045 (Pruefprojekt Ost/West,
dessen Gebaeude die Tests fuer die Kuehlung auf Arbeitskopien ein- und ausschalten), nicht 1046
(Flottenstand eingefroren), nicht 1007 (dasselbe Gebaeude wie 1046, Skalierung 4,6).

WAS DIESES SKRIPT TUT. Genau VIER Zellen:
  Tab_Einstellungen (ID_Projekt 1017): Kuehlbetrieb        0    -> 1     (Projektschalter "Kuehlung rechnen")
  Tab_Gebaeude 10599 (Projekt 1017):   Kuehlung_Aktiv      0    -> 1     (Haken "Gebaeude wird gekuehlt")
                                       Kuehl_Sollwert      NULL -> 24,0  (Grad C, = Maximaleraumtemperatur)
                                       Kuehlleistung_Max   NULL -> 15,0  (kW, rund 20 W/m2)
Nichts sonst - kein VACUUM, keine andere Zeile, keine andere Spalte (Kuehl_Sollwert_Nacht
bleibt NULL, KU3).

DIE WERTE. Der Kuehlsollwert 24 Grad C ist die Maximaleraumtemperatur des Gebaeudes: Die Anlage
haelt die Grenze, gegen die die Ueberhitzungsstunden zaehlen (Kuehlkonzept 7.1), und er liegt
mehr als 1 K ueber dem hoechsten Heizsollwert 20 Grad C (Pruefregel 3.2). Die Grenze 15 kW liegt
unter der Spitze der Kuehllast ohne Grenze (rund 21 kW): In den heissesten Stunden rechnet der
Loeser den Fall "Kuehlgrenze", die Raumluft steigt dort ueber den Kuehlsollwert - so traegt die
Basis alle fuenf Betriebsfaelle (Kuehlkonzept 3.2).

WIEDERHOLBAR. Stehen die Zellen schon auf den Zielwerten, aendert das Skript nichts und meldet
das. Steht dort etwas anderes als der Ausgangs- oder der Zielwert, weichen Name, Projekt,
Nutzflaeche, Maximaleraumtemperatur oder die Heizsollwerte ab, oder fehlt die Zeile, bricht es
VOR jedem Schreiben ab (Rueckgabe 2).

FOLGE. Die vier Zellen sind gesaete Kaeltedaten eines Referenzprojekts (Einfrierregel
"gesaete Kaeltedaten", `Referenzlaeufe/LIESMICH.md`); die Basis wird im selben Schritt neu
eingefroren.

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/kuehlung_1017_referenzprojekt.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

PROJEKT = 1017
GEBAEUDE = 10599
NAME = "GMH-D-S-118"
FLAECHE = 744.4
MAX_RAUMTEMPERATUR = 24.0
HEIZSOLLWERTE_HOECHSTENS = 23.0     # Pruefregel: Kuehlsollwert >= hoechster Heizsollwert + 1 K

# (Tabelle, Schluesselspalte, Schluessel, Spalte, Ausgangswert, Zielwert)
ZELLEN = [
    ("Tab_Einstellungen", "ID_Projekt", PROJEKT, "Kuehlbetrieb", 0, 1),
    ("Tab_Gebaeude", "ID", GEBAEUDE, "Kuehlung_Aktiv", 0, 1),
    ("Tab_Gebaeude", "ID", GEBAEUDE, "Kuehl_Sollwert", None, 24.0),
    ("Tab_Gebaeude", "ID", GEBAEUDE, "Kuehlleistung_Max", None, 15.0),
]


def main():
    if len(sys.argv) < 2:
        print("Aufruf: kuehlung_1017_referenzprojekt.py <Kenndaten_Test.sqlite>")
        return 2

    con = sqlite3.connect(sys.argv[1])
    try:
        # 1. Das Gebaeude pruefen, bevor irgendetwas geschrieben wird.
        g = con.execute(
            "SELECT Gebaeudename, ID_Projekt, Nutzflaeche, Maximaleraumtemperatur, "
            "Raumsolltemperatur_Tag, Raumsolltemperatur_Nachtabsenkung, Raumsolltemperatur_Wochenende, "
            "Raumsolltemperatur_Ferien, Kuehl_Sollwert_Nacht, Gebaeude_Modell "
            "FROM Tab_Gebaeude WHERE ID = ?", (GEBAEUDE,)).fetchone()
        if g is None:
            print(f"Tab_Gebaeude {GEBAEUDE} fehlt - Abbruch ohne Schreiben.")
            return 2
        name, projekt, flaeche, tmax, tag, nacht, wochenende, ferien, nachtkuehl, modell = g
        print(f"Gebaeude {GEBAEUDE} \"{name}\", Projekt {projekt}, Nutzflaeche {flaeche} m2, "
              f"Maximaleraumtemperatur {tmax}, Heizsollwerte {tag}/{nacht}/{wochenende}/{ferien}, "
              f"Kuehl_Sollwert_Nacht {nachtkuehl}, Rechenweg {modell}")
        if name != NAME or projekt != PROJEKT or flaeche != FLAECHE:
            print("Name, Projekt oder Nutzflaeche weichen ab - Abbruch ohne Schreiben.")
            return 2
        if tmax != MAX_RAUMTEMPERATUR:
            print(f"Maximaleraumtemperatur {tmax} statt {MAX_RAUMTEMPERATUR} - Abbruch ohne Schreiben.")
            return 2
        if any(w is not None and w > HEIZSOLLWERTE_HOECHSTENS for w in (tag, nacht, wochenende, ferien)):
            print("Ein Heizsollwert liegt ueber 23 Grad C - der Kuehlsollwert 24 verletzte die Pruefregel. Abbruch.")
            return 2
        if nachtkuehl is not None:
            print("Kuehl_Sollwert_Nacht ist gesetzt - erwartet NULL (KU3). Abbruch ohne Schreiben.")
            return 2
        if modell is not None:
            print(f"Gebaeude_Modell {modell} statt NULL (VDI 6007 ohne Angabe) - Abbruch ohne Schreiben.")
            return 2
        zahl = con.execute("SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = ?", (PROJEKT,)).fetchone()[0]
        if zahl != 1:
            print(f"Tab_Einstellungen fuehrt {zahl} Zeilen fuer Projekt {PROJEKT} statt einer - Abbruch.")
            return 2

        # 2. Jede Zelle pruefen.
        offen = []
        for tabelle, schluesselspalte, schluessel, spalte, alt, neu in ZELLEN:
            wert = con.execute(
                "SELECT " + spalte + " FROM " + tabelle + " WHERE " + schluesselspalte + " = ?",
                (schluessel,)).fetchone()[0]
            print(f"vorher:  {tabelle} {schluesselspalte}={schluessel} {spalte} = {wert}")
            if wert == neu:
                print(f"         steht schon auf {neu} - nichts zu tun.")
                continue
            if wert != alt:
                print(f"         {wert} ist weder {alt} noch {neu} - Abbruch ohne Schreiben.")
                return 2
            offen.append((tabelle, schluesselspalte, schluessel, spalte, alt, neu))

        # 3. Schreiben - eine Transaktion, je Zelle genau eine Zeile.
        if offen:
            with con:
                for tabelle, schluesselspalte, schluessel, spalte, alt, neu in offen:
                    bedingung = spalte + " IS NULL" if alt is None else spalte + " = ?"
                    parameter = (neu, schluessel) if alt is None else (neu, schluessel, alt)
                    geaendert = con.execute(
                        "UPDATE " + tabelle + " SET " + spalte + " = ? WHERE " + schluesselspalte + " = ? AND " + bedingung,
                        parameter).rowcount
                    if geaendert != 1:
                        raise RuntimeError(f"{tabelle} {schluessel} {spalte}: {geaendert} Zeilen statt einer")

        # 4. Nachprobe.
        gut = True
        for tabelle, schluesselspalte, schluessel, spalte, _, neu in ZELLEN:
            nachher = con.execute(
                "SELECT " + spalte + " FROM " + tabelle + " WHERE " + schluesselspalte + " = ?",
                (schluessel,)).fetchone()[0]
            print(f"nachher: {tabelle} {schluesselspalte}={schluessel} {spalte} = {nachher}")
            gut = gut and nachher == neu
        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        print(f"integrity_check: {pruefung}")
        return 0 if gut and pruefung == "ok" else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
