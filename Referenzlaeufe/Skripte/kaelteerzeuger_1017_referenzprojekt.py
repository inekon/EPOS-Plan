#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Gibt dem Referenzprojekt 1017 der Testdatenbank seinen Kaelteerzeuger (Stufe KU2 der Kuehlung,
vierte Welle; Kuehlkonzept 10.4/10.5, Einfrierregel "gesaete Kaeltedaten").

AUSGANG. Seit KU1 rechnet 1017 Kaelte (kuehlung_1017_referenzprojekt.py: Projektschalter, Gebaeude
10599 mit Haken, Kuehlsollwert 24 Grad C, Kuehlleistungsgrenze 15 kW) - ungedeckt, weil die einzige
Waermepumpe des Projekts (Anlage 10211, Projektgeraet 1017033, Sole-Wasser, 35 kW, Waermequelle
nicht gepflegt, also die Aussenluft) auf keinem Kaskadenplatz steht, nicht auf Kuehlbetrieb und
keine Kuehlkennlinie traegt. KU2 laesst sie kuehlen.

WAS DIESES SKRIPT TUT. Vier Zellen und zehn Zeilen:
  Tab_Einstellungen (ID_Projekt 1017):  Tool_3                  ''   -> 'Waermepumpe' (mit Umlaut)
  Tab_WP 1017033:                        Kuehlbetrieb            0    -> 1
                                         Kuehl_Vorlauf           NULL -> 18   (Grad C, Stuetzstelle)
                                         Kuehl_Hilfsstromanteil  NULL -> 0,05 (Anteil an der Verdichterarbeit)
  Tab_Kenndaten_Kuehlung:                10 neue Zeilen, ID 1-10, ID_WP 1017033 (die Tabelle war leer;
                                         SQLite fuehrt dazu die Zeile 'Tab_Kenndaten_Kuehlung' in
                                         sqlite_sequence)
Unveraendert bleiben - und werden geprueft -: der Kuehltraeger der Anlage (Kuehl_ID_Carrier NULL = wie
Heizbetrieb, der Referenzfall ohne den Sonderweg aus E34), die Abrechnungsart (Kuehl_EigenerZaehler
NULL), die Nennkuehlleistung Tab_WP.Kuehlleistung (NULL - eine Berichts-, keine Rechengroesse,
Kuehlkonzept 5.1 Festlegung 3), die Waermequelle der Anlage (WQ_Typ leer: kein Quellspeicher, 5.1
Festlegung 6) und die Kuehleingaben aus KU1. Kein VACUUM, keine andere Zeile, keine andere Spalte.

DIE WERTE.
  Kaskadenplatz 3 - hinter BHKW und Elektrokessel: Die Kaeltekaskade rechnet die Waermepumpen des
      Laufs; ohne Kaskadenplatz rechnet die Maschine gar nicht. Auf dem dritten Platz uebernimmt sie in
      der Heizzeit nur, was BHKW und Elektrokessel uebrig lassen - die Waermeseite bleibt fast, wie sie
      ist. Der Platz ist der, auf dem die Proben der Wellen 2 und 3 rechneten.
  Kuehl-Vorlauf 18 Grad C - eine der zwei Stuetzstellen (K21): Flaechenkuehlung ueber dem Taupunkt,
      die Lage, in der sensible Kaelte ohne Entfeuchtung (K5) gerechnet wird.
  Hilfsstromanteil 0,05 - ein gesetzter Wert, damit die Basis den Zuschlag des Hilfsstroms traegt
      (Kaeltestrom = Kaelte / EER x 1,05); NULL hiesse "kein Zuschlag", und der Zweig bliebe im
      Regressionsnetz unbewacht. Ein runder Beispielwert, kein Messwert.
  Kuehlkennlinie - eine ausdrueckliche Saat (Kuehlkonzept 10.4: Das Katalogsatzgeraet 33 der Anlage
      traegt keine Kuehlkennlinie, eine Uebernahme gibt es also nicht): zwei Vorlauf-Stuetzstellen
      (7 und 18 Grad C) x fuenf Aussentemperaturen (20 bis 40 Grad C), Laststufe 100 (die hoechste,
      MAX(Last)), EER in der Spalte COP (K22) - in Kaltwasserlage (jede Temperatur waermer als der
      Vorlauf), Achsen richtig (Temperatur ist nie der Vorlauf), keine Dubletten. Runde, erfundene
      Werte - dieselbe Phantasie-Kennlinie wie die Proben der Wellen 2 und 3 und die Faelle der
      KaelteerzeugerTests; kein Produkt, keine Normzahl:
          Temperatur        20    25    30    35    40
          Vorlauf  7: EER   4,0   3,6   3,2   2,8   2,4    Pkuehl 12,0 11,5 11,0 10,5 10,0 kW
          Vorlauf 18: EER   5,5   5,0   4,5   4,0   3,5    Pkuehl 15,0 14,5 14,0 13,5 13,0 kW
      Die Kaelteleistung liegt bei 18 Grad C in heissen Stunden unter der Kuehlleistungsgrenze des
      Gebaeudes (15 kW) - so traegt die Basis auch den Fall "Maschine an ihrer Grenze".

WIEDERHOLBAR. Stehen Zellen und Zeilen schon auf den Zielwerten, aendert das Skript nichts und meldet
das. Steht irgendwo etwas anderes als der Ausgangs- oder der Zielwert, weichen Projekt, Geraet,
Anlage, Kaskade oder die Kuehleingaben aus KU1 ab, oder traegt Tab_Kenndaten_Kuehlung andere Zeilen,
bricht es VOR jedem Schreiben ab (Rueckgabe 2).

FOLGE. Die Zellen und Zeilen sind gesaete Kaeltedaten eines Referenzprojekts (Einfrierregel "gesaete
Kaeltedaten", `Referenzlaeufe/LIESMICH.md`); die Basis wird im selben Schritt neu eingefroren.

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/kaelteerzeuger_1017_referenzprojekt.py Referenzlaeufe/Kenndaten_Test.sqlite
"""

import sqlite3
import sys

PROJEKT = 1017
WP = 1017033
ANLAGE = 10211
GEBAEUDE = 10599
WAERMEPUMPE = "Wärmepumpe"          # DbWerte.ERZEUGER_WAERMEPUMPE

# Die Kaskade von 1017 ausser Platz 3 - sie bleibt, wie sie ist.
KASKADE = {"Tool_1": "BHKW", "Tool_2": "Heizkessel", "Tool_4": "", "Tool_5": "", "Tool_6": "Stromspeicher"}

# (Tabelle, Schluesselspalte, Schluessel, Spalte, Ausgangswert, Zielwert)
ZELLEN = [
    ("Tab_Einstellungen", "ID_Projekt", PROJEKT, "Tool_3", "", WAERMEPUMPE),
    ("Tab_WP", "ID", WP, "Kuehlbetrieb", 0, 1),
    ("Tab_WP", "ID", WP, "Kuehl_Vorlauf", None, 18),
    ("Tab_WP", "ID", WP, "Kuehl_Hilfsstromanteil", None, 0.05),
]

# Zellen, die stehen bleiben muessen: (Tabelle, Schluesselspalte, Schluessel, Spalte, Wert)
BLEIBT = [
    ("Tab_Einstellungen", "ID_Projekt", PROJEKT, "Kuehlbetrieb", 1),
    ("Tab_Gebaeude", "ID", GEBAEUDE, "Kuehlung_Aktiv", 1),
    ("Tab_Gebaeude", "ID", GEBAEUDE, "Kuehl_Sollwert", 24.0),
    ("Tab_Gebaeude", "ID", GEBAEUDE, "Kuehlleistung_Max", 15.0),
    ("Tab_WP", "ID", WP, "Kuehlleistung", None),
    ("Tab_Energieanlagen", "ID", ANLAGE, "Kuehl_ID_Carrier", None),
    ("Tab_Energieanlagen", "ID", ANLAGE, "Kuehl_EigenerZaehler", None),
    ("Tab_Energieanlagen", "ID", ANLAGE, "WQ_Typ", None),
    ("Tab_Energieanlagen", "ID", ANLAGE, "ID_Carrier", None),
]

TEMPERATUREN = [20, 25, 30, 35, 40]
EER = {7: [4.0, 3.6, 3.2, 2.8, 2.4], 18: [5.5, 5.0, 4.5, 4.0, 3.5]}
PKUEHL = {7: [12.0, 11.5, 11.0, 10.5, 10.0], 18: [15.0, 14.5, 14.0, 13.5, 13.0]}


def kennlinie():
    """Die zehn Zeilen (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, Last) in fester Reihenfolge."""
    zeilen = []
    zeilen_id = 1
    for i, t in enumerate(TEMPERATUREN):
        for vorlauf in (7, 18):
            zeilen.append((zeilen_id, WP, vorlauf, t, EER[vorlauf][i], PKUEHL[vorlauf][i], 100))
            zeilen_id += 1
    return zeilen


def wert(con, tabelle, schluesselspalte, schluessel, spalte):
    zeile = con.execute(
        "SELECT " + spalte + " FROM " + tabelle + " WHERE " + schluesselspalte + " = ?",
        (schluessel,)).fetchall()
    if len(zeile) != 1:
        raise LookupError(f"{tabelle} {schluesselspalte}={schluessel}: {len(zeile)} Zeilen statt einer")
    return zeile[0][0]


def main():
    if len(sys.argv) < 2:
        print("Aufruf: kaelteerzeuger_1017_referenzprojekt.py <Kenndaten_Test.sqlite>")
        return 2

    con = sqlite3.connect(sys.argv[1])
    try:
        con.execute("PRAGMA foreign_keys = ON")

        # 1. Projekt, Geraet und Anlage pruefen, bevor irgendetwas geschrieben wird.
        g = con.execute("SELECT ID_Projekt, Typ, ID_Stamm, Nennleistung FROM Tab_WP WHERE ID = ?", (WP,)).fetchone()
        a = con.execute("SELECT ID_Projekt, ID_WP, ID_Type FROM Tab_Energieanlagen WHERE ID = ?", (ANLAGE,)).fetchone()
        print(f"Projektgeraet {WP}: {g}; Anlage {ANLAGE}: {a}")
        if g is None or a is None:
            print("Geraet oder Anlage fehlt - Abbruch ohne Schreiben.")
            return 2
        if g[0] != PROJEKT or g[1] != "Sole-Wasser" or g[2] != 33 or a[0] != PROJEKT or a[1] != WP or a[2] != 1:
            print("Geraet oder Anlage weichen ab (Projekt, Bauart, Katalogsatz, Verweis, Anlagenart) - Abbruch.")
            return 2
        anlagen = con.execute("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = 1",
                              (PROJEKT,)).fetchone()[0]
        if anlagen != 1:
            print(f"Projekt {PROJEKT} fuehrt {anlagen} Waermepumpenanlagen statt einer - Abbruch.")
            return 2
        if con.execute("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung_STAMM WHERE ID_WP = 33").fetchone()[0] != 0:
            print("Der Katalogsatz 33 traegt eine Kuehlkennlinie - dann waere die Uebernahme der Weg. Abbruch.")
            return 2
        for spalte, soll in KASKADE.items():
            ist = wert(con, "Tab_Einstellungen", "ID_Projekt", PROJEKT, spalte)
            if (ist or "") != soll:
                print(f"Kaskade {spalte} = {ist!r} statt {soll!r} - Abbruch ohne Schreiben.")
                return 2
        for tabelle, schluesselspalte, schluessel, spalte, soll in BLEIBT:
            ist = wert(con, tabelle, schluesselspalte, schluessel, spalte)
            if ist != soll:
                print(f"{tabelle} {schluessel} {spalte} = {ist!r} statt {soll!r} - Abbruch ohne Schreiben.")
                return 2

        # 2. Jede Zelle pruefen.
        offen = []
        for tabelle, schluesselspalte, schluessel, spalte, alt, neu in ZELLEN:
            ist = wert(con, tabelle, schluesselspalte, schluessel, spalte)
            print(f"vorher:  {tabelle} {schluesselspalte}={schluessel} {spalte} = {ist!r}")
            if ist == neu:
                print(f"         steht schon auf {neu!r} - nichts zu tun.")
                continue
            if ist != alt:
                print(f"         {ist!r} ist weder {alt!r} noch {neu!r} - Abbruch ohne Schreiben.")
                return 2
            offen.append((tabelle, schluesselspalte, schluessel, spalte, alt, neu))

        # 3. Die Kennlinie pruefen: leer (dann saeen) oder genau die Saat (dann nichts tun).
        soll = kennlinie()
        ist = con.execute("SELECT ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, Last FROM Tab_Kenndaten_Kuehlung "
                          "ORDER BY ID").fetchall()
        if ist == soll:
            print("Tab_Kenndaten_Kuehlung traegt schon die zehn Zeilen der Saat - nichts zu tun.")
            saeen = False
        elif not ist:
            saeen = True
        else:
            print(f"Tab_Kenndaten_Kuehlung traegt {len(ist)} andere Zeilen - Abbruch ohne Schreiben.")
            return 2

        # 4. Schreiben - eine Transaktion; je Zelle genau eine Zeile.
        if offen or saeen:
            with con:
                for tabelle, schluesselspalte, schluessel, spalte, alt, neu in offen:
                    bedingung = spalte + " IS NULL" if alt is None else spalte + " = ?"
                    parameter = (neu, schluessel) if alt is None else (neu, schluessel, alt)
                    geaendert = con.execute(
                        "UPDATE " + tabelle + " SET " + spalte + " = ? WHERE " + schluesselspalte + " = ? AND " +
                        bedingung, parameter).rowcount
                    if geaendert != 1:
                        raise RuntimeError(f"{tabelle} {schluessel} {spalte}: {geaendert} Zeilen statt einer")
                if saeen:
                    con.executemany("INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, Last) "
                                    "VALUES (?, ?, ?, ?, ?, ?, ?)", soll)

        # 5. Nachprobe.
        gut = True
        for tabelle, schluesselspalte, schluessel, spalte, _, neu in ZELLEN:
            nachher = wert(con, tabelle, schluesselspalte, schluessel, spalte)
            print(f"nachher: {tabelle} {schluesselspalte}={schluessel} {spalte} = {nachher!r}")
            gut = gut and nachher == neu
        kl = con.execute("SELECT ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, Last FROM Tab_Kenndaten_Kuehlung "
                         "ORDER BY ID").fetchall()
        print(f"nachher: Tab_Kenndaten_Kuehlung {len(kl)} Zeilen, gleich der Saat: {kl == soll}")
        gut = gut and kl == soll
        pruefung = con.execute("PRAGMA integrity_check").fetchone()[0]
        fremd = con.execute("PRAGMA foreign_key_check").fetchall()
        print(f"integrity_check: {pruefung}; foreign_key_check: {len(fremd)} Zeilen")
        return 0 if gut and pruefung == "ok" and not fremd else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
