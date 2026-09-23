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
  - die Parameter der Auslegung (Schluessel wie ZapfAuslegungParameter in
    EPOS.Kern/Allgemein/Zapfprofil/Auslegungsparameter.cs: Summenlinie, DIN-4708-Kennzahl samt
    Zapfbloecken des Profils, Speicherauslegung samt Vorgabe der Nenninhaltsliste, Grossanlage,
    Konstruktorregel) mit runden,
    ERFUNDENEN Werten neben jeder Normvorgabe (Stufe Z2);
  - drei Bedarfstage mit erfundenen Ereignissen: Konstruktor, Referenztag, Normtag
    (Quelle_Art 4, 2, 3) - keiner ist ein Normprofil;
  - fuenf DIN-4708-Werte (drei Belegungen, zwei Ausstattungsklassen, Sigma v*w_v in Wh) mit
    erfundenen Zahlen;
  - Stufe Z3 (Schemaschritt T2, Schritt 114): Zapfkategorien je Nutzungsart und die fuenf
    Parameter der Stochastik (Schluessel wie ZapfStochastikParameter in
    EPOS.Kern/Allgemein/Zapfprofil/Zapfkategorie.cs), erfunden - keine Jordan/Vajen-Zahl.

ABGELEITETE VDI-6002-WERTE (Anwenderentscheid ZU19 vom 23.09.2026, Stufe Z3). Neben dem fiktiven
Katalog traegt die Testdatenbank vier Nutzungsarten mit GERINGFUEGIG ABWEICHENDEN VDI-6002-Werten
(je ein eigener Tagesgangsatz; Bedarf, Jahresgang, Wochengang, Tagesgaenge). Das Skript liest sie
allein aus tww_katalogwerte_abgeleitet.json neben diesem Skript - erzeugt von
normzahlen_abgeleitet_bauen.py nach der dort dokumentierten Regel; die Originale braucht dieses
Skript nicht. Die Zeilen tragen Herkunftsart 'EIGENKONSTRUKTION' (das Schema kennt keine eigene
Herkunftsart "abgeleitet") und die Quelle "VDI 6002 Blatt <n> (abgeleitet)"; ihre Zapfkategorien
bleiben fiktiv.

DAS ECODESIGN-ZAPFPROFIL (Stufe Z3, Konzept 4.5 Quelle (5)): ein Bedarfstag der Art 5 mit den 24
Zapfungen des Lastprofils L der Verordnung (EU) Nr. 814/2013, Anhang III, Tabelle 1 - EU-Recht,
frei verwendbar; Herkunftsart 'FREI'. Die Tagessumme ist Q_ref des Profils.

Jede fiktive Zeile: Status 'EIGEN', ReadOnly 0, Herkunftsart 'FIKTIV', Quelle "Testkatalog
(fiktiv)", Katalogversion "TEST-1", kein Beleg; die abgeleiteten und die freien Zeilen ebenso
'EIGEN', ReadOnly 0, "TEST-1". KEINE Zeile mit Status 'AUSLIEFERUNG' oder 'IMPORT', keine Zone
und keine Zeile in Tab_TwwProjekt - kein Projekt steht auf dem Generator, der Referenzlauf bleibt
unberuehrt.

VORAUSSETZUNG. Schemastand 114 (die zehn Tww-Tabellen und die Zapfkategorien), nachgezogen mit
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
(der Schalter --stochastik der Stufe Z2 ist ohne Wirkung - der Block ist dauerhaft an)
"""

import json
import os
import sqlite3
import sys
from decimal import Decimal, ROUND_HALF_UP

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

# --- Abgeleitete VDI-6002-Werte (ZU19) ---------------------------------------------------------
# Die Datei traegt KEINEN Originalwert; ihre Regel steht im Kopf von normzahlen_abgeleitet_bauen.py.
ABGELEITET_DATEI = os.path.join(os.path.dirname(os.path.abspath(__file__)), "tww_katalogwerte_abgeleitet.json")
HERKUNFT_ABGELEITET = "EIGENKONSTRUKTION"
AUSGABE_VDI = "2014-03"
ZUSATZ_ABGELEITET = " (abgeleitet)"
# Die Bedarfswerte der Quelle sind Liter je Einheit und Tag bei 60 Grad C; im Katalog stehen kWh bei
# den Bezugstemperaturen der Zeile: kWh = l * CW * (BEZUG_ZAPF_VDI - BEZUG_KALT_VDI) / 1000.
BEZUG_ZAPF_VDI = 60.0
BEZUG_KALT_VDI = BEZUG_KALT          # Setzung (kein normativer Wert), wie die fiktiven Zeilen
CW = 1.163                           # Wh/(l*K), wie Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K

# (Nutzungsart der Quelle, Bezugsart, Kalenderart, Tagtypen 1..4 aus den Tagtypen der Quelle).
# Nur die Nutzungsarten, deren Bezug das Schema kennt (Person, Bett) und die Profile tragen;
# Tagtyp 4 (Ruhetag) nimmt den Sonntag - die Quelle behandelt Feiertage wie Sonntage.
VDI_NUTZUNGSARTEN = [
    ("Wohnen groß", 1, 1, ("werktag", "samstag", "sonntag", "sonntag")),
    ("Studentenwohnheim", 1, 1, ("werktag", "samstag", "sonntag", "sonntag")),
    ("Seniorenheim", 3, 4, ("werktag", "samstag", "sonntag", "sonntag")),
    ("Krankenhaus", 3, 4, ("alle", "alle", "alle", "alle")),
]
WOCHENTAGE = ("mo", "di", "mi", "do", "fr", "sa", "so")
MONATE = ("jan", "feb", "mar", "apr", "mai", "jun", "jul", "aug", "sep", "okt", "nov", "dez")


def normiert(werte, ziel):
    """Die Werte exakt auf die Summe `ziel` gebracht (Tagesgang und Woche 1, Monate 12)."""
    s = sum(werte)
    return [w * ziel / s for w in werte]


def abgeleitete_saetze_und_arten():
    """Tagesgangsaetze und Nutzungsarten aus der JSON-Datei der abgeleiteten Werte."""
    with open(ABGELEITET_DATEI, encoding="utf-8") as f:
        d = json.load(f)
    bedarf = {b["nutzungsart"]: b for b in d["bedarf"]}
    saetze, arten = [], []
    for (art, bezugsart, kalender, tagtypen) in VDI_NUTZUNGSARTEN:
        b = bedarf[art]
        quelle = "VDI 6002 Blatt %s (abgeleitet)" % b["blatt"]
        name = art + ZUSATZ_ABGELEITET
        profile = d["tagesprofile"][art]
        saetze.append((name, {t + 1: normiert(profile[tagtypen[t]], 1.0) for t in range(4)},
                       quelle, AUSGABE_VDI, HERKUNFT_ABGELEITET))
        kwh = CW * (BEZUG_ZAPF_VDI - BEZUG_KALT_VDI) / 1000.0
        arten.append(dict(
            name=name, bezug=bezugsart,
            bedarf=(b["minimum"] * kwh, b["mittel"] * kwh, b["maximum"] * kwh),
            grenze=1, kalender=kalender, ferien=None,
            monate=normiert([d["saisonfaktoren"][art][m] for m in MONATE], 12.0),
            woche=normiert([d["wochenanteile"][art][t] for t in WOCHENTAGE], 1.0),
            satz=name, bezug_zapf=BEZUG_ZAPF_VDI, bezug_kalt=BEZUG_KALT_VDI,
            quelle=quelle, ausgabe=AUSGABE_VDI, herkunft=HERKUNFT_ABGELEITET))
    return saetze, arten


ABGELEITETE_SAETZE, ABGELEITETE_NUTZUNGSARTEN = abgeleitete_saetze_und_arten()

# Alle Tagesgangsaetze: (Bezeichner, {Tagtyp: 24 Anteile}, Quelle, Ausgabe, Herkunftsart).
SAETZE = [(SATZ, TAGESGAENGE, QUELLE, None, HERKUNFT)] + ABGELEITETE_SAETZE

# Alle Nutzungsarten als Zeilenbeschreibung (die fiktiven auf den Testsatz).
ALLE_NUTZUNGSARTEN = [
    dict(name=n, bezug=bz, bedarf=bd, grenze=g, kalender=k, ferien=fe, monate=mo, woche=wo,
         satz=SATZ, bezug_zapf=BEZUG_ZAPF, bezug_kalt=BEZUG_KALT, quelle=QUELLE, ausgabe=None, herkunft=HERKUNFT)
    for (n, bz, bd, g, k, fe, mo, wo) in NUTZUNGSARTEN
] + ABGELEITETE_NUTZUNGSARTEN

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

# Die Schluessel der Auslegung (ZapfAuslegungParameter, Stufe Z2) - Werte rund und ERFUNDEN,
# bewusst neben jeder Normvorgabe und jedem Wert der Vorlage gewaehlt (Kapitel 6 (a)).
PARAMETER += [
    ("A100.Kaltwasser.Auslegung", 12.0, "°C"),
    ("W551.Mindesttemperatur", 62.0, "°C"),
    ("Speicherauslegung.Speichertemperatur_Vorgabe", 56.0, "°C"),
    ("A100.Ladungsfaktor", 0.8, "-"),
    ("A100.Sensorhoehe", 0.5, "-"),
    ("A100.Mischwassertemperatur", 44.0, "°C"),
    ("A100.Verzoegerung", 2.0, "min"),
    ("A100.Uebertrager.U.Stahl", 400.0, "W/(m²·K)"),
    ("A100.Uebertrager.U.Edelstahl", 500.0, "W/(m²·K)"),
    ("A100.Uebertrager.Uebertemperatur", 20.0, "K"),
    ("A100.Uebertragerflaeche.Kessel.Steigung", 0.02, "m²/l"),
    ("A100.Uebertragerflaeche.Kessel.Achsabschnitt", -1.0, "m²"),
    ("A100.Uebertragerflaeche.Waermepumpe.Steigung", 0.01, "m²/l"),
    ("A100.Uebertragerflaeche.Waermepumpe.Achsabschnitt", -0.5, "m²"),
    ("A100.Zeitkonstante.Koeffizient", 25.0, "min·W/kJ"),
    ("A100.Vereinfachung.Anwendungsgrenze", 5.0, "WE"),
    ("A100.Vereinfachung.Sensorhoehe", 0.7, "-"),
    ("A100.Vereinfachung.Speichertemperatur", 58.0, "°C"),
    ("Summenlinie.Wertepaare", 5.0, "-"),
    ("DIN4708.a1", 0.3, "1/h"),
    ("DIN4708.a2", 3.0, "1/h"),
    ("DIN4708.z", 0.2, "h"),
    ("DIN4708.p_b", 4.0, "Personen"),
    ("DIN4708.w_b", 6000.0, "Wh"),
    ("DIN4708.W_b", 5000.0, "Wh"),
    ("DIN4708.Kappung", 1.5, "-"),
    ("DIN4708.Profil.Bloecke", 2.0, "-"),
    ("DIN4708.Profil.Block.1.Beginn", 420.0, "min"),
    ("DIN4708.Profil.Block.1.Dauer", 10.0, "min"),
    ("DIN4708.Profil.Block.1.Anteil", 1.0, "-"),
    ("DIN4708.Profil.Block.2.Beginn", 1080.0, "min"),
    ("DIN4708.Profil.Block.2.Dauer", 60.0, "min"),
    ("DIN4708.Profil.Block.2.Anteil", 2.0, "-"),
    ("Speicherauslegung.Nutzanteil", 0.75, "-"),
    ("Speicherauslegung.Zuschlag", 0.1, "-"),
    ("Speicherauslegung.Ladefenster.Laenge", 10.0, "h"),
    ("Speicherauslegung.Ladefenster.Beginn", 22.0, "h"),
    ("Speicherauslegung.GLF_Gueltigkeitsgrenze", 30.0, "-"),
    ("Speicherauslegung.Klassisch.LiterJePersonTag", 40.0, "l/(P·d)"),
    ("Speicherauslegung.Klassisch.Spreizung", 45.0, "K"),
    ("Speicherauslegung.Klassisch.Warnfaktor", 2.5, "-"),
    ("Speicherauslegung.Nenninhalt.Raster", 500.0, "l"),
    # Die Vorgabe der Nenninhaltsliste (Stufe Z2, Gruppe 2): erfundene, neutrale Stufen - keine Produktgroessen.
    ("Speicherauslegung.Nenninhalt.Liste.1", 120.0, "l"),
    ("Speicherauslegung.Nenninhalt.Liste.2", 250.0, "l"),
    ("Speicherauslegung.Nenninhalt.Liste.3", 400.0, "l"),
    ("Speicherauslegung.Nenninhalt.Liste.4", 650.0, "l"),
    ("Speicherauslegung.Nenninhalt.Liste.5", 900.0, "l"),
    ("Speicherauslegung.Nenninhalt.Liste.6", 1400.0, "l"),
    ("W551.Grossanlage.Speichervolumen", 450.0, "l"),
    ("W551.Grossanlage.Leitungsinhalt", 4.0, "l"),
    ("W551.Leitungsinhalt.JeMeter", 0.2, "l/m"),
    ("Konstruktor.Regel.Testbrause.Volumenstrom", 10.0, "l/min"),
    ("Konstruktor.Regel.Testbrause.Dauer", 4.0, "min"),
    ("Konstruktor.Regel.Testbrause.Temperatur", 40.0, "°C"),
]

# (Bezeichner, Quelle_Art, Bezugsmenge, [(Minute_Beginn, Dauer_min, Energie_Kwh, Reihenfolge)])
# Quelle_Art: 2 Referenztag, 3 Normtag, 4 Konstruktor - alle Ereignisse erfunden, kein Normprofil.
BEDARFSTAGE = [
    ("Testbedarfstag (fiktiv)", 4, 10.0, [
        (420, 10, 1.0, 1),
        (720, 5, 0.5, 2),
        (1140, 20, 2.0, 3),
    ]),
    ("Testreferenztag (fiktiv)", 2, 20.0, [
        (390, 30, 3.0, 1),
        (450, 15, 1.5, 2),
        (780, 10, 0.5, 3),
        (1110, 45, 4.0, 4),
    ]),
    ("Testnormtag (fiktiv)", 3, 5.0, [
        (420, 10, 2.0, 1),
        (1080, 60, 4.0, 2),
    ]),
]

# --- Das Ecodesign-Zapfprofil (Konzept 4.5 Quelle (5), Stufe Z3) ------------------------------
# Verordnung (EU) Nr. 814/2013 der Kommission vom 2. August 2013, Anhang III, Tabelle 1
# "Lastprofile von Warmwasserbereitern", Profil L (ABl. L 239 vom 6.9.2013, S. 162) - EU-Recht,
# frei verwendbar; abgerufen am 23.09.2026 ueber das Amt fuer Veroeffentlichungen
# (publications.europa.eu, CELEX 32013R0814, deutsche Fassung).
# (Uhrzeit, Q_tap in kWh, f in l/min, T_m in Grad C, T_p in Grad C oder None)
ECODESIGN_QUELLE = "Verordnung (EU) Nr. 814/2013 Anhang III"
ECODESIGN_AUSGABE = "ABl. L 239 vom 6.9.2013, Tabelle 1, Lastprofil L"
ECODESIGN_Q_REF = "11.655"           # Q_ref des Profils L in kWh (Tabelle 1, letzte Zeile)
ECODESIGN_L = [
    ("07:00", "0.105", 3, 25, None),
    ("07:05", "1.4", 6, 40, None),
    ("07:30", "0.105", 3, 25, None),
    ("07:45", "0.105", 3, 25, None),
    ("08:05", "3.605", 10, 10, 40),
    ("08:25", "0.105", 3, 25, None),
    ("08:30", "0.105", 3, 25, None),
    ("08:45", "0.105", 3, 25, None),
    ("09:00", "0.105", 3, 25, None),
    ("09:30", "0.105", 3, 25, None),
    ("10:30", "0.105", 3, 10, 40),
    ("11:30", "0.105", 3, 25, None),
    ("11:45", "0.105", 3, 25, None),
    ("12:45", "0.315", 4, 10, 55),
    ("14:30", "0.105", 3, 25, None),
    ("15:30", "0.105", 3, 25, None),
    ("16:30", "0.105", 3, 25, None),
    ("18:00", "0.105", 3, 25, None),
    ("18:15", "0.105", 3, 40, None),
    ("18:30", "0.105", 3, 40, None),
    ("19:00", "0.105", 3, 25, None),
    ("20:30", "0.735", 4, 10, 55),
    ("21:00", "3.605", 10, 10, 40),
    ("21:30", "0.105", 3, 25, None),
]
# Die Dauer einer Zapfung steht nicht in der Tabelle; die Setzung der Umsetzung (nicht der
# Verordnung): Dauer = Volumen / f, Volumen = Q_tap / (CW * (Nutztemperatur - Kaltwasser)),
# Nutztemperatur = T_p, wo angegeben, sonst T_m; Kaltwasser 10 Grad C; ganze Minuten
# kaufmaennisch, mindestens 1. Die Energie jeder Zapfung ist Q_tap unveraendert.
ECODESIGN_KALTWASSER = Decimal("10")


def ecodesign_dauer(q_tap, f, t_m, t_p):
    nutz = Decimal(t_p if t_p is not None else t_m)
    volumen = Decimal(q_tap) * 1000 / (Decimal(str(CW)) * (nutz - ECODESIGN_KALTWASSER))
    minuten = (volumen / Decimal(f)).quantize(Decimal(1), rounding=ROUND_HALF_UP)
    return max(1, int(minuten))


def ecodesign_ereignisse():
    liste = []
    for n, (uhrzeit, q_tap, f, t_m, t_p) in enumerate(ECODESIGN_L, start=1):
        h, m = uhrzeit.split(":")
        liste.append((int(h) * 60 + int(m), ecodesign_dauer(q_tap, f, t_m, t_p), float(q_tap), n))
    return liste


# Bedarfstage mit eigener Provenienz: (Bezeichner, Quelle_Art, Bezugsmenge, Ereignisse, Quelle,
# Ausgabe, Herkunftsart). Bezugsmenge NULL: der Tag wird nicht skaliert (nur Einfamilienhaus).
BEDARFSTAGE_FREI = [
    ("Ecodesign-Zapfprofil L", 5, None, ecodesign_ereignisse(), ECODESIGN_QUELLE, ECODESIGN_AUSGABE, "FREI"),
]
ALLE_BEDARFSTAGE = [(b, q, m, e, QUELLE, None, HERKUNFT) for (b, q, m, e) in BEDARFSTAGE] + BEDARFSTAGE_FREI

# (Art, Schluessel, Wert) - Belegung in Personen, Ausstattung als Sigma v*w_v in Wh.
DIN4708_WERTE = [
    ("BELEGUNG", "2", 1.0),
    ("BELEGUNG", "3", 1.5),
    ("BELEGUNG", "4", 3.0),
    ("AUSSTATTUNG", "Testklasse A", 4000.0),
    ("AUSSTATTUNG", "Testklasse B", 9000.0),
]

# --- Stufe Z3: Stochastik (Zapfkategorien T2 und Parameter) ---------------------------------------
# Dauerhaft an seit dem Schemaschritt T2 (Schritt 114, Tab_TwwZapfkategorie_STAMM). Die Werte sind
# ERFUNDEN: keine Jordan/Vajen-Zahl, kein Normquantil, keine Setzung eines fremden Generators
# (Kapitel 6). Die Spaltennamen folgen der DDL von T2 (TwwSchema.SQL_CREATE_ZAPFKATEGORIE).
STOCHASTIK_AKTIV = True

# (Schluessel, Wert, Einheit) - Schluessel wie ZapfStochastikParameter, runde erfundene Werte.
STOCHASTIK_PARAMETER = [
    ("Zapfprofil.Stochastik.Urlaubsversatz", 20.0, "d"),
    ("Zapfprofil.Stochastik.Auslegung.Vielfaches", 1.5, "-"),
    ("Zapfprofil.Stochastik.Konsistenzschwelle", 1.8, "-"),
    ("Zapfprofil.Stochastik.Quantil.P95", 1.7, "-"),
    ("Zapfprofil.Stochastik.Quantil.P99", 2.4, "-"),
]

# (Nutzungsart, Kategorie, Volumenstrom_l_min, Dauer_min, Anteil, Sigma_l_min, Kappung_l_min oder None)
# - je Nutzungsart Anteile mit Summe 1, jede Kategorie erfunden.
ZAPFKATEGORIEN = [
    ("Testnutzung A (fiktiv)", "Testkategorie A (fiktiv)", 2.0, 1, 0.20, 1.0, None),
    ("Testnutzung A (fiktiv)", "Testkategorie B (fiktiv)", 5.0, 2, 0.30, 2.0, None),
    ("Testnutzung A (fiktiv)", "Testkategorie C (fiktiv)", 12.0, 8, 0.15, 3.0, 15.0),
    ("Testnutzung A (fiktiv)", "Testkategorie D (fiktiv)", 7.0, 4, 0.35, 2.0, None),
    ("Testnutzung B (fiktiv)", "Testkategorie E (fiktiv)", 1.5, 3, 0.40, 2.0, None),
    ("Testnutzung B (fiktiv)", "Testkategorie F (fiktiv)", 6.0, 5, 0.60, 2.5, 10.0),
    ("Testnutzung C (fiktiv)", "Testkategorie G (fiktiv)", 4.0, 2, 0.50, 1.5, None),
    ("Testnutzung C (fiktiv)", "Testkategorie H (fiktiv)", 9.0, 6, 0.50, 3.0, 12.0),
]
# Die abgeleiteten Nutzungsarten bekommen den erfundenen Satz der Testnutzung A - ohne Kategorien
# rechnete eine stochastische Zone auf ihnen nicht.
ZAPFKATEGORIEN += [(a["name"],) + k[1:] for a in ABGELEITETE_NUTZUNGSARTEN
                   for k in ZAPFKATEGORIEN if k[0] == "Testnutzung A (fiktiv)"]
TABELLE_KATEGORIEN = "Tab_TwwZapfkategorie_STAMM"

KATALOGTABELLEN = [
    "Tab_TwwTagesgangsatz_STAMM", "Tab_TwwTagesgang_STAMM", "Tab_TwwNutzungsart_STAMM",
    "Tab_TwwBedarfstag_STAMM", "Tab_TwwBedarfstagEreignis_STAMM", "Tab_TwwParameter_STAMM",
    "Tab_TwwDin4708Wert_STAMM",
]

# Die erwartete Zeilenzahl je Tabelle nach dem Lauf.
ERWARTET = {
    "Tab_TwwTagesgangsatz_STAMM": len(SAETZE),
    "Tab_TwwTagesgang_STAMM": sum(len(s[1]) for s in SAETZE),
    "Tab_TwwNutzungsart_STAMM": len(ALLE_NUTZUNGSARTEN),
    "Tab_TwwBedarfstag_STAMM": len(ALLE_BEDARFSTAGE),
    "Tab_TwwBedarfstagEreignis_STAMM": sum(len(t[3]) for t in ALLE_BEDARFSTAGE),
    "Tab_TwwParameter_STAMM": len(PARAMETER),
    "Tab_TwwDin4708Wert_STAMM": len(DIN4708_WERTE),
    "Tab_TwwZone": 0,
    "Tab_TwwWohnungstyp": 0,
    "Tab_TwwProjekt": 0,
}


def pruefe_summen():
    """Die erfundenen Formen sind in sich stimmig - vor jedem Schreiben."""
    for name in {k[0] for k in ZAPFKATEGORIEN}:
        summe = sum(k[4] for k in ZAPFKATEGORIEN if k[0] == name)
        assert abs(summe - 1.0) < 1e-12, f"{name}: Anteile der Zapfkategorien {summe}"
        assert name in [n["name"] for n in ALLE_NUTZUNGSARTEN], f"{name}: keine Nutzungsart des Katalogs"
    for (name, gaenge, _q, _a, _h) in SAETZE:
        assert sorted(gaenge) == [1, 2, 3, 4], f"{name}: Tagtypen"
        for t, a in gaenge.items():
            assert len(a) == 24 and abs(sum(a) - 1.0) < 1e-12, f"{name}, Tagtyp {t}: Summe {sum(a)}"
    for n in ALLE_NUTZUNGSARTEN:
        assert len(n["monate"]) == 12 and abs(sum(n["monate"]) / 12.0 - 1.0) < 1e-12, f"{n['name']}: Monatsmittel"
        assert len(n["woche"]) == 7 and abs(sum(n["woche"]) - 1.0) < 1e-12, f"{n['name']}: Wochensumme"
        assert n["satz"] in [s[0] for s in SAETZE], f"{n['name']}: Tagesgangsatz"
    for a in ALLE_NUTZUNGSARTEN:
        assert any(k[0] == a["name"] for k in ZAPFKATEGORIEN), f"{a['name']}: keine Zapfkategorien"
    summe = sum(Decimal(e[1]) for e in ECODESIGN_L)
    assert summe == Decimal(ECODESIGN_Q_REF), f"Ecodesign L: Tagessumme {summe} statt Q_ref {ECODESIGN_Q_REF}"


def zahl(con, sql, *p):
    return con.execute(sql, p).fetchone()[0]


def main():
    if len(sys.argv) < 2:
        print("Aufruf: tww_testkatalog_fiktiv.py <Kenndaten_Test.sqlite> [--stochastik]")
        return 2
    pruefe_summen()
    stochastik = STOCHASTIK_AKTIV or "--stochastik" in sys.argv[2:]   # der Schalter bleibt fuer alte Aufrufe
    parameter = PARAMETER + (STOCHASTIK_PARAMETER if stochastik else [])
    erwartet = dict(ERWARTET)
    if stochastik:
        erwartet["Tab_TwwParameter_STAMM"] = len(parameter)
        erwartet[TABELLE_KATEGORIEN] = len(ZAPFKATEGORIEN)

    con = sqlite3.connect(sys.argv[1])
    try:
        con.execute("PRAGMA foreign_keys = ON")
        stand = zahl(con, "SELECT SchemaVersion FROM Tab_Applikation")
        fehlend = [t for t in erwartet
                   if zahl(con, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", t) == 0]
        if fehlend:
            print(f"Schemastand {stand}: es fehlen {', '.join(fehlend)} - erst Werkzeuge/Testdatenbankschema"
                  + (" (die Zapfkategorien brauchen den Schemaschritt T2)" if TABELLE_KATEGORIEN in fehlend else "")
                  + ". Abbruch ohne Schreiben.")
            return 2

        # Fremde Zeilen? Alles, was nicht Katalogversion TEST-1 / Status EIGEN / FIKTIV ist.
        fremd = 0
        for t in ("Tab_TwwTagesgangsatz_STAMM", "Tab_TwwNutzungsart_STAMM", "Tab_TwwBedarfstag_STAMM",
                  "Tab_TwwParameter_STAMM", "Tab_TwwDin4708Wert_STAMM"):
            fremd += zahl(con, f'SELECT COUNT(*) FROM "{t}" WHERE "Katalogversion" <> ? OR "Status" <> ? '
                               'OR "ReadOnly" <> 0', VERSION, STATUS)
        if stochastik:
            # Die Kategorien tragen keine eigene Katalogversion - sie gehoeren zu ihrer Nutzungsart.
            fremd += zahl(con, f'SELECT COUNT(*) FROM "{TABELLE_KATEGORIEN}" WHERE "Status" <> ? OR "ReadOnly" <> 0 '
                               'OR "ID_Nutzungsart" NOT IN (SELECT "ID" FROM "Tab_TwwNutzungsart_STAMM" '
                               'WHERE "Katalogversion" = ?)', STATUS, VERSION)
        for t in ("Tab_TwwZone", "Tab_TwwWohnungstyp", "Tab_TwwProjekt"):
            fremd += zahl(con, f'SELECT COUNT(*) FROM "{t}"')
        if fremd:
            print(f"{fremd} Zeile(n) gehoeren nicht zum fiktiven Testkatalog - Abbruch ohne Schreiben.")
            return 2

        angelegt = 0
        nachgefuehrt = 0
        with con:
            # --- Tagesgangsaetze und ihre vier Tagesgaenge (fiktiv und abgeleitet) -------------
            spalten = ", ".join(f'"Anteil_{h:02d}"' for h in range(1, 25))
            platz = ", ".join("?" for _ in range(24))
            id_saetze = {}
            for (satz, gaenge, quelle, ausgabe, herkunft) in SAETZE:
                if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwTagesgangsatz_STAMM" WHERE "Bezeichner" = ? '
                             'AND "Katalogversion" = ?', satz, VERSION) == 0:
                    con.execute('INSERT INTO "Tab_TwwTagesgangsatz_STAMM" ("Bezeichner", "Katalogversion", '
                                '"Status", "Beleg", "ReadOnly") VALUES (?, ?, ?, NULL, 0)', (satz, VERSION, STATUS))
                    angelegt += 1
                id_satz = zahl(con, 'SELECT "ID" FROM "Tab_TwwTagesgangsatz_STAMM" WHERE "Bezeichner" = ? '
                                    'AND "Katalogversion" = ?', satz, VERSION)
                id_saetze[satz] = id_satz
                for tagtyp, werte in sorted(gaenge.items()):
                    if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwTagesgang_STAMM" WHERE "ID_Tagesgangsatz" = ? '
                                 'AND "Tagtyp" = ?', id_satz, tagtyp) == 0:
                        con.execute(f'INSERT INTO "Tab_TwwTagesgang_STAMM" ("ID_Tagesgangsatz", "Tagtyp", {spalten}, '
                                    f'"Quelle", "Ausgabe", "Version", "Herkunftsart") VALUES (?, ?, {platz}, ?, ?, ?, ?)',
                                    (id_satz, tagtyp, *werte, quelle, ausgabe, VERSION, herkunft))
                        angelegt += 1

            # --- Nutzungsarten (fiktiv und abgeleitet) ------------------------------------------
            for n in ALLE_NUTZUNGSARTEN:
                if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwNutzungsart_STAMM" WHERE "Bezeichner" = ? '
                             'AND "Katalogversion" = ?', n["name"], VERSION) > 0:
                    continue
                prov = [n["quelle"], n["ausgabe"], VERSION, n["herkunft"]]
                namen = ["Bezeichner", "Katalogversion", "Bezugsart",
                         "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch",
                         "Bedarf_Quelle", "Bedarf_Ausgabe", "Bedarf_Version", "Bedarf_Herkunftsart",
                         "Bezug_Zapftemperatur", "Bezug_Kaltwasser", "Bilanzgrenze", "Kalenderart", "Ferienfaktor"]
                werte = [n["name"], VERSION, n["bezug"], *n["bedarf"], *prov,
                         n["bezug_zapf"], n["bezug_kalt"], n["grenze"], n["kalender"], n["ferien"]]
                namen += [f"Monat_{m}" for m in range(1, 13)]
                werte += n["monate"]
                namen += ["Jahresgang_Quelle", "Jahresgang_Ausgabe", "Jahresgang_Version", "Jahresgang_Herkunftsart"]
                werte += prov
                namen += [f"Woche_{w}" for w in range(1, 8)]
                werte += n["woche"]
                namen += ["Wochengang_Quelle", "Wochengang_Ausgabe", "Wochengang_Version", "Wochengang_Herkunftsart",
                          "ID_Tagesgangsatz", "ID_Vorlage", "Status", "Beleg", "Freigabe", "ReadOnly"]
                werte += prov + [id_saetze[n["satz"]], None, STATUS, None, None, 0]
                con.execute(f'INSERT INTO "Tab_TwwNutzungsart_STAMM" ({", ".join(chr(34) + s + chr(34) for s in namen)}) '
                            f'VALUES ({", ".join("?" for _ in namen)})', werte)
                angelegt += 1

            # --- Bezugstemperaturen vorhandener Zeilen nachfuehren -------------------------
            for n in ALLE_NUTZUNGSARTEN:
                cur = con.execute('UPDATE "Tab_TwwNutzungsart_STAMM" SET "Bezug_Zapftemperatur" = ?, '
                                  '"Bezug_Kaltwasser" = ? WHERE "Bezeichner" = ? AND "Katalogversion" = ? '
                                  'AND ("Bezug_Zapftemperatur" <> ? OR "Bezug_Kaltwasser" <> ?)',
                                  (n["bezug_zapf"], n["bezug_kalt"], n["name"], VERSION, n["bezug_zapf"], n["bezug_kalt"]))
                nachgefuehrt += cur.rowcount

            # --- Parameter --------------------------------------------------------------
            for (schluessel, wert, einheit) in parameter:
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

            # --- Bedarfstage samt Ereignissen (nur mit neu angelegtem Kopf) ---------------
            for (bezeichner, quelle_art, bezugsmenge, ereignisse, quelle, ausgabe, herkunft) in ALLE_BEDARFSTAGE:
                if zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwBedarfstag_STAMM" WHERE "Bezeichner" = ? '
                             'AND "Katalogversion" = ?', bezeichner, VERSION) > 0:
                    continue
                cur = con.execute('INSERT INTO "Tab_TwwBedarfstag_STAMM" ("Bezeichner", "Katalogversion", '
                                  '"Quelle_Art", "Bezugsmenge", "Quelle", "Ausgabe", "Version", "Herkunftsart", '
                                  '"Status", "Beleg", "ReadOnly") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, 0)',
                                  (bezeichner, VERSION, quelle_art, bezugsmenge,
                                   quelle, ausgabe, VERSION, herkunft, STATUS))
                id_tag = cur.lastrowid
                angelegt += 1
                for (beginn, dauer, energie, reihenfolge) in ereignisse:
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

            # --- Stufe Z3: Zapfkategorien (nur mit T2 und eingeschaltetem Block) -----------------
            if stochastik:
                reihenfolge = {}
                for (art, kategorie, volumenstrom, dauer, anteil, sigma, kappung) in ZAPFKATEGORIEN:
                    reihenfolge[art] = reihenfolge.get(art, 0) + 1
                    id_art = zahl(con, 'SELECT "ID" FROM "Tab_TwwNutzungsart_STAMM" WHERE "Bezeichner" = ? '
                                       'AND "Katalogversion" = ?', art, VERSION)
                    if zahl(con, f'SELECT COUNT(*) FROM "{TABELLE_KATEGORIEN}" WHERE "ID_Nutzungsart" = ? '
                                 'AND "Kategorie" = ?', id_art, kategorie) > 0:
                        continue
                    con.execute(f'INSERT INTO "{TABELLE_KATEGORIEN}" ("ID_Nutzungsart", "Kategorie", "Reihenfolge", '
                                '"Volumenstrom_l_min", "Dauer_min", "Anteil", "Sigma", "Kappung_l_min", '
                                '"Quelle", "Ausgabe", "Version", "Herkunftsart", "Status", '
                                '"Beleg", "ReadOnly") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, ?, ?, ?, NULL, 0)',
                                (id_art, kategorie, reihenfolge[art], volumenstrom, dauer, anteil, sigma, kappung,
                                 QUELLE, VERSION, HERKUNFT, STATUS))
                    angelegt += 1

        print(f"Schemastand {stand}: {angelegt} Zeile(n) angelegt, {nachgefuehrt} nachgefuehrt"
              + (" - der Testkatalog stand schon vollstaendig da." if angelegt == 0 and nachgefuehrt == 0 else "."))

        ok = True
        for t, soll in erwartet.items():
            ist = zahl(con, f'SELECT COUNT(*) FROM "{t}"')
            print(f"  {t}: {ist} Zeile(n)" + ("" if ist == soll else f"  (erwartet {soll})"))
            ok = ok and ist == soll
        ausgeliefert = sum(zahl(con, f'SELECT COUNT(*) FROM "{t}" WHERE "Status" IN (\'AUSLIEFERUNG\', \'IMPORT\')')
                           for t in ("Tab_TwwTagesgangsatz_STAMM", "Tab_TwwNutzungsart_STAMM",
                                     "Tab_TwwBedarfstag_STAMM", "Tab_TwwParameter_STAMM",
                                     "Tab_TwwDin4708Wert_STAMM") + ((TABELLE_KATEGORIEN,) if stochastik else ()))
        integritaet = con.execute("PRAGMA integrity_check").fetchone()[0]
        fk = con.execute("PRAGMA foreign_key_check").fetchall()
        print(f"  Status AUSLIEFERUNG/IMPORT: {ausgeliefert}; integrity_check: {integritaet}; "
              f"foreign_key_check: {len(fk)} Befund(e)")
        return 0 if ok and ausgeliefert == 0 and integritaet == "ok" and not fk else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
