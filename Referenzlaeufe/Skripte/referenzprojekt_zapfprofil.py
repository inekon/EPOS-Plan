#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Stellt Projekt 1045 der Testdatenbank als Referenzprojekt auf den Zapfprofilgenerator
(Umsetzungskonzept Zapfprofilgenerator 3.4, Anwenderentscheid ZU7; Einfrierregel „gesäte
Zapfprofil-Eingaben").

WARUM 1045. Ein Wohngebäude aus der Projektliste der CI (1030, 1007, 1017, 1045, 1046, 1047):
Der Generator rechnet damit bei jeder Push-Prüfung mit. 1045 trägt genau ein Gebäude
(10651 „EFH-A-U-347s", Einfamilienhaus, VDI-6007-Weg), ist nicht gekoppelt, kühlt nicht und
hat auf dem Bestandsweg ein echtes Warmwasserprofil („EFH Wohnen, 1 Person", Summe 5,0 MWh/a)
mit eigenem Brauchwasser-Puffer, Wärmepumpe und Kessel — der Generator erreicht damit Speicher
und Erzeuger. Nicht 1030 (kein Gebäude), nicht 1017 (Kälte), nicht 1046 (Flottenstand
eingefroren), nicht 1047 (Kopplung AK1), nicht 1007 (sein Bestandsprofil „Haushalt-3" ist ein
Stromprofil, das als Brauchwasser läuft — kein Vergleichsmaß für eine Warmwasserbilanz).

WAS DIESES SKRIPT TUT. Genau ZWEI Zeilen, nichts sonst:
  Tab_TwwProjekt: ID_Projekt 1045, Weg GENERATOR, Jahresreihe_Stochastisch 0 (deterministische
                  Bilanz — kein Ensemble), Seed 1045, Realisierungen 10, Perzentil 99,
                  Zirk_Auto 1, Zirk_Methode 3, Lade_Auto 1, Speicherart 1, Personen_Auto 1,
                  Typtage_Aktiv 0, alle übrigen Spalten NULL (Vorgaben des Rechenwegs),
                  Aenderungsdatum 2026-09-26.
  Tab_TwwZone:    ID_Projekt 1045, Nutzungsart „Wohnen groß (abgeleitet)" (Katalogversion
                  TEST-1; Kalenderart Wohnen, Bezugsart Personen, Bezug 60/12 °C,
                  Bilanzgrenze Zapfstelle), eigener Tagesgangsatz NULL (der der Nutzungsart),
                  Gebäude 10651, Reihenfolge 1, Name „Wohnen", Bezugsmenge 8,3 Personen,
                  Niveau 2 (mittel), Topologie 1 (Speicher), Zirkulation 0,
                  Tagesbedarf_Auto 1; Temperaturen, Kaltwasser, Ferien, Auslastung,
                  Bedarfsüberschreibung und Jahresmesswert NULL.
Keine Wohnungstypen (die Bezugsmenge gilt unmittelbar), keine Konstruktorzeile, kein Messwert.
Die Bestandszeile in Z_Projekt_Brauchwasser bleibt stehen — auf dem Generatorweg rechnet sie nicht
mit (Weiche, Konzept 2.2); ein Zurückstellen auf BESTAND stellt den alten Kanal wieder her.

DIE WERTE. Bilanzgrenze wie der Bestandsweg: dessen Profil ist Nutzwärme an der Zapfstelle,
also rechnet die Zone ohne Zirkulation (Zirkulation 0) auf der Grenze „Zapfstelle" ihrer
Nutzungsart. Die Bezugsmenge trifft den Jahresbedarf des Bestandswegs ungefähr: mittleres Niveau
1,618896 kWh je Person und Tag bei 60/12 °C, umgerechnet auf das Kaltwasser-Jahresmittel der
Parameter (11 °C) mit (60 − 11)/(60 − 12); 8,3 Personen · 365 d ergeben rund 5,0 MWh/a gegen
5,0 MWh/a des Bestandswegs. Der Jahresmesswert bleibt leer — der Generator rechnet.
Seed und Realisierungen wirken in der Bilanz nicht (deterministischer Weg), nur in der
Auslegung und einer stochastischen Jahresreihe; sie sind trotzdem gesetzt und eingefroren.

WIEDERHOLBAR. Stehen beide Zeilen schon mit genau diesen Werten, ändert das Skript nichts
(„0 Projektzeilen, 0 Zonen"). Steht für 1045 eine abweichende Zeile, fehlen Projekt, Gebäude
oder Nutzungsart, weichen ihre Kennzeichen ab oder trägt 1045 Wohnungstypen oder
Konstruktorzeilen, bricht es VOR jedem Schreiben ab (Rückgabe 2).

FOLGE. Beide Zeilen sind gesäte Zapfprofil-Eingaben eines Referenzprojekts (Einfrierregel
„gesäte Zapfprofil-Eingaben", `Referenzlaeufe/LIESMICH.md`); die Basis wird im selben Schritt neu
eingefroren. Die Wache `EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests` hält jede Zelle.

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/referenzprojekt_zapfprofil.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

PROJEKT = 1045
PROJEKTNAME = "Prüfprojekt Ost/West Stränge"
GEBAEUDE = 10651
GEBAEUDENAME = "EFH-A-U-347s"
NUTZUNGSART = "Wohnen groß (abgeleitet)"
KATALOGVERSION = "TEST-1"

# Zielwerte der Projektzeile (Spalte -> Wert); alle übrigen Spalten stehen auf NULL.
PROJEKTZEILE = {
    "Weg": "GENERATOR",
    "Jahresreihe_Stochastisch": 0,
    "Seed": 1045,
    "Realisierungen": 10,
    "Perzentil": 99,
    "Zirk_Auto": 1,
    "Zirk_Methode": 3,
    "Lade_Auto": 1,
    "Speicherart": 1,
    "Personen_Auto": 1,
    "Typtage_Aktiv": 0,
    "Aenderungsdatum": "2026-09-26",
}

# Zielwerte der Zone; ID_Nutzungsart wird aus dem Katalog gesucht, alle übrigen Spalten NULL.
ZONE = {
    "ID_Gebaeude": GEBAEUDE,
    "Reihenfolge": 1,
    "Name": "Wohnen",
    "Bezugsmenge": 8.3,
    "Niveau": 2,
    "Topologie": 1,
    "Zirkulation": 0,
    "Tagesbedarf_Auto": 1,
}

# Kennzeichen der Nutzungsart, auf die sich die Werte stützen.
NUTZUNGSART_KENNZEICHEN = {"Bezugsart": 1, "Kalenderart": 1, "Bilanzgrenze": 1,
                           "Bezug_Zapftemperatur": 60.0, "Bezug_Kaltwasser": 12.0}


def spalten(con, tabelle):
    return [r[1] for r in con.execute('PRAGMA table_info("' + tabelle + '")')]


def zeile_als_dict(con, tabelle, bedingung, parameter):
    namen = spalten(con, tabelle)
    zeilen = con.execute('SELECT * FROM "' + tabelle + '" WHERE ' + bedingung, parameter).fetchall()
    return [dict(zip(namen, z)) for z in zeilen]


def soll(ziel, namen, ohne):
    """Vollständige Zielzeile: gesetzte Werte, sonst NULL (ohne die Spalten in `ohne`)."""
    return {n: ziel.get(n) for n in namen if n not in ohne}


def abweichungen(ist, ziel):
    return [f"{k}: {ist.get(k)!r} statt {v!r}" for k, v in ziel.items() if ist.get(k) != v]


def main():
    if len(sys.argv) < 2:
        print("Aufruf: referenzprojekt_zapfprofil.py <Kenndaten_Test.sqlite>")
        return 2

    con = sqlite3.connect(sys.argv[1])
    con.execute("PRAGMA foreign_keys = ON")
    try:
        # 1. Vorbedingungen, bevor irgendetwas geschrieben wird.
        p = con.execute("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", (PROJEKT,)).fetchone()
        if p is None or p[0] != PROJEKTNAME:
            print(f"Projekt {PROJEKT} fehlt oder heißt nicht „{PROJEKTNAME}\" ({p}) - Abbruch ohne Schreiben.")
            return 2
        g = con.execute("SELECT ID_Projekt, Gebaeudename, Wohngebaeude_Nicht_Wohngebaeude FROM Tab_Gebaeude "
                        "WHERE ID = ?", (GEBAEUDE,)).fetchone()
        if g is None or g[0] != PROJEKT or g[1] != GEBAEUDENAME or g[2] != "Wohngebaeude":
            print(f"Gebäude {GEBAEUDE} fehlt oder weicht ab ({g}) - Abbruch ohne Schreiben.")
            return 2
        n = zeile_als_dict(con, "Tab_TwwNutzungsart_STAMM", "Bezeichner = ? AND Katalogversion = ?",
                           (NUTZUNGSART, KATALOGVERSION))
        if len(n) != 1:
            print(f"Nutzungsart „{NUTZUNGSART}\" ({KATALOGVERSION}) steht {len(n)}-mal im Katalog - Abbruch.")
            return 2
        n = n[0]
        falsch = abweichungen(n, NUTZUNGSART_KENNZEICHEN)
        if falsch:
            print("Nutzungsart weicht ab: " + "; ".join(falsch) + " - Abbruch ohne Schreiben.")
            return 2
        id_nutzungsart = n["ID"]
        print(f"Projekt {PROJEKT} „{PROJEKTNAME}\", Gebäude {GEBAEUDE} „{GEBAEUDENAME}\", "
              f"Nutzungsart {id_nutzungsart} „{NUTZUNGSART}\" (Bedarf mittel {n['Bedarf_Mittel']} kWh/(P·d))")

        projekt_namen = spalten(con, "Tab_TwwProjekt")
        zonen_namen = spalten(con, "Tab_TwwZone")
        projekt_soll = soll(PROJEKTZEILE, projekt_namen, {"ID", "ID_Projekt"})
        zone_soll = soll(dict(ZONE, ID_Nutzungsart=id_nutzungsart), zonen_namen, {"ID", "ID_Projekt"})

        projekt_ist = zeile_als_dict(con, "Tab_TwwProjekt", "ID_Projekt = ?", (PROJEKT,))
        zonen_ist = zeile_als_dict(con, "Tab_TwwZone", "ID_Projekt = ?", (PROJEKT,))
        wohnungen = con.execute("SELECT COUNT(*) FROM Tab_TwwWohnungstyp w JOIN Tab_TwwZone z ON z.ID = w.ID_Zone "
                                "WHERE z.ID_Projekt = ?", (PROJEKT,)).fetchone()[0]
        konstruktor = con.execute("SELECT COUNT(*) FROM Tab_TwwKonstruktorzeile k JOIN Tab_TwwProjekt t "
                                  "ON t.ID = k.ID_TwwProjekt WHERE t.ID_Projekt = ?", (PROJEKT,)).fetchone()[0]
        if wohnungen or konstruktor:
            print(f"Projekt {PROJEKT} trägt {wohnungen} Wohnungstypen und {konstruktor} Konstruktorzeilen - Abbruch.")
            return 2

        neu_projekt = neu_zone = 0
        if projekt_ist:
            falsch = abweichungen(projekt_ist[0], projekt_soll)
            if falsch:
                print("Tab_TwwProjekt 1045 steht mit anderen Werten: " + "; ".join(falsch) + " - Abbruch.")
                return 2
        if len(zonen_ist) > 1:
            print(f"Projekt {PROJEKT} trägt {len(zonen_ist)} Zonen statt einer - Abbruch.")
            return 2
        if zonen_ist:
            falsch = abweichungen(zonen_ist[0], zone_soll)
            if falsch:
                print("Tab_TwwZone von 1045 steht mit anderen Werten: " + "; ".join(falsch) + " - Abbruch.")
                return 2

        # 2. Schreiben - eine Transaktion.
        with con:
            if not projekt_ist:
                k = ["ID_Projekt"] + list(projekt_soll)
                con.execute('INSERT INTO Tab_TwwProjekt (' + ", ".join('"' + x + '"' for x in k) + ") VALUES ("
                            + ", ".join("?" for _ in k) + ")", [PROJEKT] + list(projekt_soll.values()))
                neu_projekt = 1
            if not zonen_ist:
                k = ["ID_Projekt"] + list(zone_soll)
                con.execute('INSERT INTO Tab_TwwZone (' + ", ".join('"' + x + '"' for x in k) + ") VALUES ("
                            + ", ".join("?" for _ in k) + ")", [PROJEKT] + list(zone_soll.values()))
                neu_zone = 1
        print(f"eingefügt: {neu_projekt} Projektzeilen, {neu_zone} Zonen")

        # 3. Nachprobe.
        pz = zeile_als_dict(con, "Tab_TwwProjekt", "ID_Projekt = ?", (PROJEKT,))
        zz = zeile_als_dict(con, "Tab_TwwZone", "ID_Projekt = ?", (PROJEKT,))
        gut = (len(pz) == 1 and not abweichungen(pz[0], projekt_soll)
               and len(zz) == 1 and not abweichungen(zz[0], zone_soll))
        if gut:
            print(f"nachher: Tab_TwwProjekt ID {pz[0]['ID']} Weg {pz[0]['Weg']}, Seed {pz[0]['Seed']}, "
                  f"Realisierungen {pz[0]['Realisierungen']}; Tab_TwwZone ID {zz[0]['ID']} „{zz[0]['Name']}\", "
                  f"Bezugsmenge {zz[0]['Bezugsmenge']}")
        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        fk = con.execute("PRAGMA foreign_key_check").fetchall()
        print(f"integrity_check: {pruefung}; foreign_key_check: {len(fk)} Fundstellen")
        return 0 if gut and pruefung == "ok" and not fk else 2
    finally:
        # WAL zurueck in die Hauptdatei schreiben, sonst bleiben -wal/-shm liegen
        # (BETRIEB_SQLITE.md: Journalmodus WAL, dateipersistent; keine Beidateien im Repo).
        try:
            con.execute("PRAGMA wal_checkpoint(TRUNCATE)")
        except sqlite3.Error:
            pass
        con.close()


if __name__ == "__main__":
    sys.exit(main())
