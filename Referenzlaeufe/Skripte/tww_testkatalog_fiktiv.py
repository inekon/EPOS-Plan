#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Spielt den FIKTIVEN Testkatalog des Zapfprofilgenerators in die Testdatenbank ein
(Umsetzungskonzept Zapfprofilgenerator, Stufe Z0, Posten P4; Kapitel 6 (b)) - samt den
abgeleiteten VDI-6002-Werten (ZU19) und dem FREIEN PAKETTEIL (Referenzlaeufe/Katalogpaket_frei/).

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
    erfundenen Zahlen.

ABGELEITETE VDI-6002-WERTE (Anwenderentscheide ZU19 vom 23.09.2026 und ZU20 vom 25.09.2026,
Stufe Z3; Katalogausbau Z5). Neben dem fiktiven Katalog traegt die Testdatenbank fuenf
Nutzungsarten mit GERINGFUEGIG ABWEICHENDEN VDI-6002-Werten (je ein eigener Tagesgangsatz; Bedarf,
Jahresgang, Wochengang, Tagesgaenge). Das Skript liest sie allein aus
tww_katalogwerte_abgeleitet.json neben diesem Skript - erzeugt von normzahlen_abgeleitet_bauen.py
nach der dort dokumentierten Regel; die Originale braucht dieses Skript nicht. Niedrig/mittel/hoch
der Nutzungsart sind Minimum/Mittel/Maximum der Datei (Kopf von normzahlen_abgeleitet_bauen.py).
Die Zeilen (Nutzungsart in allen drei Provenienzgruppen, Tagesgaenge) tragen Herkunftsart
'VERFAHREN' mit der Quelle "abgeleitet aus VDI 6002 Blatt <n>": Der Wert ist AUS EINEM VERFAHREN
GERECHNET - der Ableitungsregel von normzahlen_abgeleitet_bauen.py -, nicht der Richtlinie
entnommen und nicht aus einer frei verfuegbaren Quelle ('FREI' waere eine falsche Aussage ueber
VDI 6002). Mit ZU20 GEHOEREN SIE ZUR AUSLIEFERUNG: Ihre Traeger sind die drei CSV-Dateien
Tab_TwwTagesgangsatz_STAMM.csv, Tab_TwwTagesgang_STAMM.csv und Tab_TwwNutzungsart_STAMM.csv des
freien Paketteils (Status 'AUSLIEFERUNG', ReadOnly 1), die dieses Skript aus derselben JSON-Datei
ERZEUGT (Schalter --paketteil-schreiben) und bei jedem Lauf gegen die Dateien im Arbeitsbaum
haelt; Werkzeuge/Auslieferungsvorlage spielt sie in jede Vorlage ein. In der Testdatenbank stehen
dieselben Werte nach deren Regel (Kapitel 6 (c)): Status 'EIGEN', ReadOnly 0, Katalogversion
'TEST-1'. Die drei "Testnutzung A/B/C (fiktiv)" bleiben 'FIKTIV'/'EIGEN' und nur hier.

DIE NUTZUNGSART "Hotel (aus Messung)" (ZU36, Folgeposten #546). Dieselben drei Traegerdateien fuehren
einen sechsten Tagesgangsatz und eine sechste Nutzungsart, die NICHT aus VDI 6002 stammen (die
Richtlinie fuehrt kein Hotel), sondern aus dem Mittel dreier gemessener Hotels: gerundete Kennwerte
der Datei tww_hotel_aus_messung.json, gebildet von hotel_aus_messung_bauen.py. Herkunftsart
'EIGENKONSTRUKTION' (Modellannahme von INEKON), Quelle "Mittel aus drei Hotels, Soerensen et al.
2021, doi:...", Bezugsart Betten, Kalenderart Betrieb, flacher Jahresgang.

DIE ZWEI HINWEISSCHWELLEN (Folgeposten #546). Zapfprofil.Messwert.Rueckfrageschwelle und
Zapfprofil.Formvektor.Warnschwelle sind INEKON-Setzungen des freien Paketteils: das Skript erzeugt
ihre Zeilen aus zapfprofil_setzungen_inekon.json hinter denen der Vorlage V4 (Herkunftsart
'EIGENKONSTRUKTION'); im fiktiven Testkatalog stehen sie nicht mehr. Aus derselben JSON-Datei kommen
die drei Setzungen des Bandkriteriums der Validierung (Anwenderentscheid ZU35, Statuszeile #553):
Zapfprofil.Validierung.Band.Unten 0,95, .Band.Oben 0,999 und .Band.MindestEinheiten 10.

DER FREIE PAKETTEIL (Stufe Z3). Die Zapfkategorien (Jordan/Vajen, IEA SHC Task 26;
Modellannahme), die fuenf Parameter Zapfprofil.Stochastik.*, die drei Setzungen der Stufe Z4
(Zapfprofil.Zirkulation.Hinweisverhaeltnis, Zapfprofil.Anzeigetemperatur, Zapfprofil.Stundenschwelle),
die Setzungen der Validierung der Stufe Z5 (Zapfprofil.Validierung.*; die drei Bandsetzungen nach ZU35 siehe oben)
und das Ecodesign-Zapfprofil L (Verordnung (EU) Nr. 814/2013 Anhang III) sind freie Daten. Sie stehen EINMAL im Repositorium, als CSV-Dateien
im Paketformat N2 unter Referenzlaeufe/Katalogpaket_frei/ (Aufbau und Quellen in dessen
LIESMICH.md); Werkzeuge/Auslieferungsvorlage spielt denselben Ordner in jede Vorlage ein. Vier
dieser Dateien (Parameter, Bedarfstag samt Ereignissen, Zapfkategorien) LIEST dieses Skript, die
drei Traegerdateien der abgeleiteten Werte ERZEUGT es (siehe oben). Die gelesenen Zeilen gehen mit
Herkunftsart 'FREI' wie im Paket, aber nach der Regel der Testdatenbank (Kapitel 6 (c)) Status 'EIGEN',
ReadOnly 0 und die Katalogversion des Testkatalogs (der Paketteil fuehrt keine eigene). Die
Kategorien des Paketteils sind VORGABESAETZE ohne Nutzungsart, je Nutzungsartengruppe einer
(Steuerspalte "Gruppe": Wohnen, Nichtwohnen - Stufe Z5): Jede Nutzungsart dieses Katalogs bekommt
den Satz ihrer Gruppe, und die Gruppe folgt der Kalenderart (1 Wohnen = "Wohnen", 2 bis 5 =
"Nichtwohnen"; dieselbe Regel wie TwwSchema.Kategoriengruppe und die Auslieferungsvorlage).

DIE SETZUNGEN DER SPEICHERAUSLEGUNG AUS DER VORLAGE V4 (Nachtrag N28). Die Auslieferungswerte der
Setzungen Speicherauslegung.* (Speichertemperatur, Nutzanteil, Zuschlag, Ladefenster.Laenge,
Klassisch.*, Nenninhalt.Raster und Nenninhalt.Liste.*) stammen aus der INEKON-eigenen Vorlage
TWW-Auslegung_V4.xlsx - kein Normwert, kein Produktwert. Das Skript liest sie allein aus
speicherauslegung_v4.json neben diesem Skript (Wert, Einheit, Blatt, Zelle, Beschriftung) und ERZEUGT
daraus ihre Zeilen in Tab_TwwParameter_STAMM.csv des freien Paketteils (Schalter
--paketteil-schreiben; die uebrigen Zeilen dieser Datei bleiben, wie sie sind, die erzeugten stehen
am Ende): Herkunftsart 'EIGENKONSTRUKTION' (eine Setzung von INEKON, keine frei verfuegbare Quelle),
Quelle "INEKON-Vorlage TWW-Auslegung V4 (Version 2.1.2, 30.07.2026), Blatt <b>, Zeile <n>, Spalte <s>", Ausgabe
= Beschriftung der Zelle. Die fiktiven Werte dieser Schluessel fallen dafuer aus dem Testkatalog; die
Testdatenbank fuehrt die Werte der Vorlage nach ihrer Regel (EIGEN, ReadOnly 0, TEST-1). Fiktiv
bleiben die zwei Setzungen, fuer die V4 keinen Wert hat (Ladefenster.Beginn,
GLF_Gueltigkeitsgrenze, Kopf "offen" der JSON-Datei) - sie werden nicht ausgeliefert.

Jede fiktive Zeile: Status 'EIGEN', ReadOnly 0, Herkunftsart 'FIKTIV', Quelle "Testkatalog
(fiktiv)", Katalogversion "TEST-1", kein Beleg; die abgeleiteten und die freien Zeilen ebenso
'EIGEN', ReadOnly 0, "TEST-1". KEINE Zeile mit Status 'AUSLIEFERUNG' oder 'IMPORT', keine Zone
und keine Zeile in Tab_TwwProjekt - kein Projekt steht auf dem Generator, der Referenzlauf bleibt
unberuehrt.

VORAUSSETZUNG. Schemastand 124 (die zehn Tww-Tabellen, die Zapfkategorien aus Schritt 115 und die
Bezugsart am Bedarfstag aus Schritt 124), nachgezogen mit
    dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite

WIEDERHOLBAR UND NACHFUEHREND. Jede Zeile wird ueber ihren natuerlichen Schluessel gesucht, fehlt
sie, angelegt, steht sie da, auf den Stand dieses Skripts (bzw. der JSON- und CSV-Dateien)
nachgefuehrt - Werte, Provenienz, Status; die Ereignisse eines Bedarfstags werden ersetzt, wenn
sie abweichen. Eine Zapfkategorie einer Nutzungsart dieses Katalogs und ein freier Parameter oder
Bedarfstag, die der Paketteil nicht (mehr) fuehrt, fallen. Ein zweiter Lauf aendert nichts und
meldet das. Steht in einer Tww-Katalogtabelle schon eine Zeile, die NICHT zu diesem Katalog
gehoert, bricht das Skript ohne Schreiben ab (Rueckgabe 2).

Aufruf (Windows: `py`, sonst `python3`):
    py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite
    py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py <db> --paketteil-schreiben
(der Schalter --stochastik der Stufe Z2 ist ohne Wirkung; --paketteil-schreiben erzeugt die drei
Traegerdateien der abgeleiteten Werte und die Zeilen der Speicherauslegung in der Parameterdatei des
freien Paketteils neu und laeuft dann normal weiter)
"""

import csv
import io
import json
import os
import sqlite3
import sys

QUELLE = "Testkatalog (fiktiv)"
VERSION = "TEST-1"
HERKUNFT = "FIKTIV"
STATUS = "EIGEN"

# Das einzige Referenzprojekt auf dem Zapfprofilgenerator (ZU7, referenzprojekt_zapfprofil.py):
# seine Projektzeile und seine eine Zone sind gesaet und erwartet, keine fremde Zeile.
REFERENZPROJEKT_GENERATOR = 1045

# Die Provenienz-Version (Spalte Version) der Zeilen des freien Paketteils - der Stand des
# Paketteils, nicht die Katalogversion (Regel 2 der LIESMICH.md des Paketteils).
VERSION_PAKETTEIL = "FREI-1"

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
# ZU20: aus einem Verfahren gerechnet (der Ableitungsregel) - weder Normwert noch Eigenkonstruktion
# noch eine frei verfuegbare Quelle; die Quelle nennt Richtlinie und Blatt.
HERKUNFT_ABGELEITET = "VERFAHREN"
AUSGABE_VDI = "2014-03"
ZUSATZ_ABGELEITET = " (abgeleitet)"
QUELLE_ABGELEITET = "abgeleitet aus VDI 6002 Blatt %s"
# Die Bedarfswerte der Quelle sind Liter je Einheit und Tag bei 60 Grad C; im Katalog stehen kWh bei
# den Bezugstemperaturen der Zeile: kWh = l * CW * (BEZUG_ZAPF_VDI - BEZUG_KALT_VDI) / 1000.
BEZUG_ZAPF_VDI = 60.0
BEZUG_KALT_VDI = BEZUG_KALT          # Setzung (kein normativer Wert), wie die fiktiven Zeilen
CW = 1.163                           # Wh/(l*K), wie Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K

# (Nutzungsart der Quelle, Bezugsart, Kalenderart, Tagtypen 1..4 aus den Tagtypen der Quelle,
#  Nutzungsart, deren Formen - Tagesgaenge, Wochenanteile, Monatsfaktoren - gelten; None: die eigenen).
# Aufgenommen ist JEDE Nutzungsart der Originale, deren Bezug eine Bezugsart des Schemas trifft
# (Person -> 1, Bett -> 3); Tagtyp 4 (Ruhetag) nimmt den Sonntag - die Quelle behandelt Feiertage
# wie Sonntage. NICHT aufgenommen, benannt (Stufe Z5):
#   - Campingplatz: Bezug "belegter Stellplatz" - keine der sieben Bezugsarten des Schemas;
#   - Standardhallenbad und Gut ausgestattetes Hallenbad: Bezug "Besucher der sommerlichen
#     Schwachlastperiode" - keine Bezugsart des Schemas, und die Richtlinie fuehrt fuer sie weder
#     Tages- noch Wochenprofil.
# Ein- und Zweifamilienhaus ist aufgenommen (Bezug Person), obwohl die Richtlinie ihm weder Profile
# noch einen Mittelwert gibt. Dafuer zwei SETZUNGEN der Umsetzung (ZU21, kein neuer Zahlenwert):
#   (1) Wochenanteile und Monatsfaktoren sind die des grossen Wohngebaeudes, und es TEILT dessen
#       Tagesgangsatz - derselbe Kalender "Wohnen", dieselben abgeleiteten Werte, keine gedoppelte
#       Zeile (der geteilte Satz macht die Setzung in der Oberflaeche sichtbar und sperrt das
#       Bearbeiten auf eine Kopie);
#   (2) der mittlere Bedarf ist die Mitte der abgeleiteten Spanne, (Minimum + Maximum) / 2.
VDI_NUTZUNGSARTEN = [
    ("Wohnen groß", 1, 1, ("werktag", "samstag", "sonntag", "sonntag"), None),
    ("Ein- und Zweifamilienhaus", 1, 1, ("werktag", "samstag", "sonntag", "sonntag"), "Wohnen groß"),
    ("Studentenwohnheim", 1, 1, ("werktag", "samstag", "sonntag", "sonntag"), None),
    ("Seniorenheim", 3, 4, ("werktag", "samstag", "sonntag", "sonntag"), None),
    ("Krankenhaus", 3, 4, ("alle", "alle", "alle", "alle"), None),
]
WOCHENTAGE = ("mo", "di", "mi", "do", "fr", "sa", "so")

# --- Die Nutzungsart "Hotel (aus Messung)" (ZU36, Folge V6 des Validierungsberichts) -------------
# VDI 6002 fuehrt fuer Hotels weder Bedarf noch Profile; der Katalogtyp kommt deshalb aus dem MITTEL
# DREIER GEMESSENER HOTELS (Soerensen et al. 2021, CC BY 4.0). Das Skript liest allein die gerundeten
# Kennwerte der JSON-Datei, die hotel_aus_messung_bauen.py aus den Rohdaten bildet (Regel dort im
# Kopf); die Rohdaten braucht dieses Skript nicht. Herkunftsart EIGENKONSTRUKTION: eine Modellannahme
# von INEKON (drei Hotels als Mittel, ein Zimmer = ein Bett, flacher Jahresgang), weder ein Verfahren
# ueber eine Richtlinie noch eine frei uebernommene Zahl. Bezugsart Betten (3), Kalenderart Betrieb (4)
# wie das Krankenhaus - damit Gruppe Nichtwohnen. Tagtyp 4 (Ruhetag) nimmt den Sonntag. Der Bedarf der
# Datei ist gemessene Energie je Zimmer und Tag; er gilt bei den Bezugstemperaturen 60/12 Grad C der
# abgeleiteten Zeilen (Modellannahme, die Quelle misst Energie, keine Temperaturen).
HOTEL_DATEI = os.path.join(os.path.dirname(os.path.abspath(__file__)), "tww_hotel_aus_messung.json")
HERKUNFT_HOTEL = "EIGENKONSTRUKTION"
HOTEL_BEZUGSART = 3
HOTEL_KALENDER = 4
HOTEL_TAGTYPEN = ("werktag", "samstag", "sonntag", "sonntag")
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
    for (art, bezugsart, kalender, tagtypen, formart) in VDI_NUTZUNGSARTEN:
        b = bedarf[art]
        form = formart or art                      # Setzung (1): fremde Formen, wo die Quelle keine fuehrt
        quelle = QUELLE_ABGELEITET % b["blatt"]
        name = art + ZUSATZ_ABGELEITET
        satzname = form + ZUSATZ_ABGELEITET        # geliehene Formen teilen den Satz, statt ihn zu doppeln
        profile = d["tagesprofile"][form]
        if formart is None:
            saetze.append((satzname, {t + 1: normiert(profile[tagtypen[t]], 1.0) for t in range(4)},
                           quelle, AUSGABE_VDI, HERKUNFT_ABGELEITET, VERSION_PAKETTEIL))
        kwh = CW * (BEZUG_ZAPF_VDI - BEZUG_KALT_VDI) / 1000.0
        # Setzung (2): ohne Mittelwert in der Quelle die Mitte der abgeleiteten Spanne.
        mittel = b["mittel"] if b["mittel"] is not None else (b["minimum"] + b["maximum"]) / 2.0
        arten.append(dict(
            name=name, bezug=bezugsart,
            bedarf=(b["minimum"] * kwh, mittel * kwh, b["maximum"] * kwh),
            grenze=1, kalender=kalender, ferien=None,
            monate=normiert([d["saisonfaktoren"][form][m] for m in MONATE], 12.0),
            woche=normiert([d["wochenanteile"][form][t] for t in WOCHENTAGE], 1.0),
            satz=satzname, bezug_zapf=BEZUG_ZAPF_VDI, bezug_kalt=BEZUG_KALT_VDI,
            quelle=quelle, ausgabe=AUSGABE_VDI, herkunft=HERKUNFT_ABGELEITET, version=VERSION_PAKETTEIL))
    return saetze, arten


def hotel_satz_und_art():
    """Tagesgangsatz und Nutzungsart "Hotel (aus Messung)" aus der JSON-Datei der Hotelkennwerte."""
    with open(HOTEL_DATEI, encoding="utf-8") as f:
        d = json.load(f)
    kopf = d["kopf"]
    assert kopf["herkunftsart"] == HERKUNFT_HOTEL, "tww_hotel_aus_messung.json: Herkunftsart"
    assert ";" not in kopf["quelle"] + kopf["ausgabe"], "tww_hotel_aus_messung.json: Semikolon im Text"
    name = kopf["nutzungsart"]
    b = d["bedarf"]
    assert 0 < b["niedrig"] <= b["mittel"] <= b["hoch"], "tww_hotel_aus_messung.json: Bedarfsstufen"
    satz = (name, {t + 1: normiert(d["tagesprofile"][HOTEL_TAGTYPEN[t]], 1.0) for t in range(4)},
            kopf["quelle"], kopf["ausgabe"], HERKUNFT_HOTEL, VERSION_PAKETTEIL)
    art = dict(
        name=name, bezug=HOTEL_BEZUGSART, bedarf=(b["niedrig"], b["mittel"], b["hoch"]),
        grenze=1, kalender=HOTEL_KALENDER, ferien=None,
        monate=[1.0] * 12,                                    # flacher Jahresgang (Modellannahme)
        woche=normiert([d["wochenanteile"][t] for t in WOCHENTAGE], 1.0),
        satz=name, bezug_zapf=BEZUG_ZAPF_VDI, bezug_kalt=BEZUG_KALT_VDI,
        quelle=kopf["quelle"], ausgabe=kopf["ausgabe"], herkunft=HERKUNFT_HOTEL, version=VERSION_PAKETTEIL)
    return satz, art


ABGELEITETE_SAETZE, ABGELEITETE_NUTZUNGSARTEN = abgeleitete_saetze_und_arten()
_HOTEL_SATZ, _HOTEL_ART = hotel_satz_und_art()
ABGELEITETE_SAETZE.append(_HOTEL_SATZ)
ABGELEITETE_NUTZUNGSARTEN.append(_HOTEL_ART)

# Alle Tagesgangsaetze: (Bezeichner, {Tagtyp: 24 Anteile}, Quelle, Ausgabe, Herkunftsart, Version).
SAETZE = [(SATZ, TAGESGAENGE, QUELLE, None, HERKUNFT, VERSION)] + ABGELEITETE_SAETZE

# Alle Nutzungsarten als Zeilenbeschreibung (die fiktiven auf den Testsatz).
ALLE_NUTZUNGSARTEN = [
    dict(name=n, bezug=bz, bedarf=bd, grenze=g, kalender=k, ferien=fe, monate=mo, woche=wo,
         satz=SATZ, bezug_zapf=BEZUG_ZAPF, bezug_kalt=BEZUG_KALT, quelle=QUELLE, ausgabe=None,
         herkunft=HERKUNFT, version=VERSION)
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
    # Zapfprofil.Messwert.Rueckfrageschwelle und Zapfprofil.Formvektor.Warnschwelle kommen als
    # INEKON-Setzung aus zapfprofil_setzungen_inekon.json (freier Paketteil, Folgeposten #546).
]

# Die Schluessel der Auslegung (ZapfAuslegungParameter, Stufe Z2) - Werte rund und ERFUNDEN,
# bewusst neben jeder Normvorgabe und jedem Wert der Vorlage gewaehlt (Kapitel 6 (a)).
PARAMETER += [
    ("A100.Kaltwasser.Auslegung", 12.0, "°C"),
    ("W551.Mindesttemperatur", 62.0, "°C"),
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
    # Die uebrigen Setzungen der Speicherauslegung kommen aus der Vorlage V4 (speicherauslegung_v4.json,
    # freier Paketteil, N28); fiktiv bleiben nur die zwei, fuer die V4 keinen Wert hat.
    ("Speicherauslegung.Ladefenster.Beginn", 22.0, "h"),
    ("Speicherauslegung.GLF_Gueltigkeitsgrenze", 30.0, "-"),
    ("W551.Grossanlage.Speichervolumen", 450.0, "l"),
    ("W551.Grossanlage.Leitungsinhalt", 4.0, "l"),
    ("W551.Leitungsinhalt.JeMeter", 0.2, "l/m"),
    ("Konstruktor.Regel.Testbrause.Volumenstrom", 10.0, "l/min"),
    ("Konstruktor.Regel.Testbrause.Dauer", 4.0, "min"),
    ("Konstruktor.Regel.Testbrause.Temperatur", 40.0, "°C"),
]

# (Bezeichner, Quelle_Art, Bezugsmenge, [(Minute_Beginn, Dauer_min, Energie_Kwh, Reihenfolge)])
# Quelle_Art: 2 Referenztag, 3 Normtag, 4 Konstruktor - alle Ereignisse erfunden, kein Normprofil.
# Die Bezugsart (Schritt 124) bleibt bei den fiktiven Tagen leer: Sie skalieren auf die eine
# Bezugsart der Gruppe, wie die Faelle der Tests es erwarten.
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

# (Art, Schluessel, Wert) - Belegung in Personen, Ausstattung als Sigma v*w_v in Wh.
DIN4708_WERTE = [
    ("BELEGUNG", "2", 1.0),
    ("BELEGUNG", "3", 1.5),
    ("BELEGUNG", "4", 3.0),
    ("AUSSTATTUNG", "Testklasse A", 4000.0),
    ("AUSSTATTUNG", "Testklasse B", 9000.0),
]

# --- Der freie Paketteil (Stufe Z3) -------------------------------------------------------------
# EINE Quelle: die CSV-Dateien unter Referenzlaeufe/Katalogpaket_frei/ (Paketformat N2), dieselben,
# die Werkzeuge/Auslieferungsvorlage in jede Vorlage einspielt. Hier steht kein Wert daraus.
PAKETTEIL = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Katalogpaket_frei")
T_BEDARFSTAG = "Tab_TwwBedarfstag_STAMM"
T_EREIGNIS = "Tab_TwwBedarfstagEreignis_STAMM"
T_PARAMETER = "Tab_TwwParameter_STAMM"
TABELLE_KATEGORIEN = "Tab_TwwZapfkategorie_STAMM"
PAKETTEIL_TABELLEN = (T_BEDARFSTAG, T_EREIGNIS, T_PARAMETER, TABELLE_KATEGORIEN)
HERKUNFT_FREI = "FREI"
STATUS_PAKET = "AUSLIEFERUNG"
EREIGNISSPALTEN = ("Minute_Beginn", "Dauer_min", "Energie_Kwh", "Reihenfolge")
# Die EINE Steuerspalte des Paketteils, die keine Spalte der Tabelle ist (Stufe Z5).
SPALTE_GRUPPE = "Gruppe"
GRUPPE_WOHNEN = "Wohnen"
GRUPPE_NICHTWOHNEN = "Nichtwohnen"


def gruppe(kalenderart):
    """Die Nutzungsartengruppe einer Kalenderart - wie TwwSchema.Kategoriengruppe."""
    return GRUPPE_WOHNEN if kalenderart == 1 else GRUPPE_NICHTWOHNEN


# --- Die Traegerdateien der abgeleiteten Werte im Paketteil (ZU20) ------------------------------
# Das Skript ERZEUGT sie aus derselben JSON-Datei, aus der es die Testdatenbank saet: EINE Quelle,
# keine Handarbeit. Werkzeuge/Auslieferungsvorlage spielt sie in jede Vorlage ein (Status
# AUSLIEFERUNG, ReadOnly 1, ohne Katalogversion); die Testdatenbank fuehrt dieselben Werte nach
# ihrer eigenen Regel (EIGEN, ReadOnly 0, Katalogversion TEST-1). Jeder Lauf haelt die Dateien im
# Arbeitsbaum gegen das Erzeugnis - so kann keine der drei Ablagen von der JSON-Datei abdriften.
T_TAGESGANGSATZ = "Tab_TwwTagesgangsatz_STAMM"
T_TAGESGANG = "Tab_TwwTagesgang_STAMM"
T_NUTZUNGSART = "Tab_TwwNutzungsart_STAMM"
PAKETTEIL_ABGELEITET = (T_TAGESGANGSATZ, T_TAGESGANG, T_NUTZUNGSART)
STUNDEN = tuple("Anteil_%02d" % h for h in range(1, 25))
SCHALTER_SCHREIBEN = "--paketteil-schreiben"

# --- Die Setzungen der Speicherauslegung aus der Vorlage V4 (N28) -------------------------------
# EINE Quelle: speicherauslegung_v4.json neben diesem Skript. Das Skript erzeugt daraus die Zeilen
# der Speicherauslegung in Tab_TwwParameter_STAMM.csv (am Ende der Datei; die uebrigen Zeilen
# bleiben unberuehrt) und haelt sie bei jedem Lauf dagegen.
HERKUNFT_EIGENKONSTRUKTION = "EIGENKONSTRUKTION"
# Die Herkunftsarten der Parameterdatei des Paketteils: FREI und die INEKON-Setzungen aus V4.
PARAMETER_HERKUNFT = (HERKUNFT_FREI, HERKUNFT_EIGENKONSTRUKTION)
with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "speicherauslegung_v4.json"), encoding="utf-8") as _f:
    SPEICHERAUSLEGUNG_V4 = json.load(_f)
# Die zwei Hinweisschwellen als INEKON-Setzung (Folgeposten #546, Pruefliste ZU21 Abschnitt 3 -> 1):
# EINE Quelle, zapfprofil_setzungen_inekon.json; ihre Zeilen stehen hinter denen der Vorlage V4.
with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "zapfprofil_setzungen_inekon.json"), encoding="utf-8") as _f:
    SETZUNGEN_INEKON = json.load(_f)
PARAMETERKOPF = ["Schluessel", "Wert", "Einheit", "Quelle", "Ausgabe", "Version", "Herkunftsart", "Status", "ReadOnly"]


def v4_zeilen():
    """Die Zeilen der Speicherauslegung im Paketformat N2, in der Reihenfolge der JSON-Datei."""
    kopf = SPEICHERAUSLEGUNG_V4["kopf"]
    zeilen = []
    for p in SPEICHERAUSLEGUNG_V4["parameter"]:
        # Die Fundstelle in Worten (Zeile, Spalte): eine Zelladresse wie "B387" traegt das Muster einer
        # Typbezeichnung (WikiProduktdatenWacheTests); die Adresse selbst steht in "zelle" der JSON-Datei.
        quelle = "%s, Blatt %s, %s" % (kopf["vorlage"], p["blatt"], p["fundstelle"])
        assert ";" not in quelle + p["beschriftung"], p["schluessel"] + ": Semikolon im Text"
        zeilen.append([p["schluessel"], float(p["wert"]), p["einheit"], quelle, p["beschriftung"],
                       VERSION_PAKETTEIL, kopf["herkunftsart"], STATUS_PAKET, 1])
    schluessel = [z[0] for z in zeilen]
    assert len(set(schluessel)) == len(schluessel), "speicherauslegung_v4.json: Schluessel doppelt"
    assert not set(schluessel) & set(kopf["offen"]), "speicherauslegung_v4.json: Schluessel zugleich offen"
    assert not set(schluessel) & {p[0] for p in PARAMETER}, "Schluessel der Vorlage V4 auch im fiktiven Testkatalog"
    return zeilen


def inekon_zeilen():
    """Die Zeilen der INEKON-Setzungen (zapfprofil_setzungen_inekon.json) im Paketformat N2."""
    kopf = SETZUNGEN_INEKON["kopf"]
    zeilen = []
    for p in SETZUNGEN_INEKON["parameter"]:
        assert ";" not in p["quelle"] + p["ausgabe"], p["schluessel"] + ": Semikolon im Text"
        zeilen.append([p["schluessel"], float(p["wert"]), p["einheit"], p["quelle"], p["ausgabe"],
                       VERSION_PAKETTEIL, kopf["herkunftsart"], STATUS_PAKET, 1])
    schluessel = [z[0] for z in zeilen]
    assert len(set(schluessel)) == len(schluessel), "zapfprofil_setzungen_inekon.json: Schluessel doppelt"
    assert not set(schluessel) & {p[0] for p in PARAMETER}, "INEKON-Setzung auch im fiktiven Testkatalog"
    assert not set(schluessel) & {z[0] for z in v4_zeilen()}, "INEKON-Setzung zugleich Zeile der Vorlage V4"
    return zeilen


def parameter_traeger():
    """Die Parameterdatei des Paketteils: ihre uebrigen Zeilen unveraendert, dahinter die Zeilen aus V4
    und die INEKON-Setzungen (zapfprofil_setzungen_inekon.json)."""
    pfad = os.path.join(PAKETTEIL, T_PARAMETER + ".csv")
    with open(pfad, encoding="utf-8-sig", newline="") as f:
        zeilen = [z for z in csv.reader(io.StringIO(f.read()), delimiter=";") if z and any(s.strip() for s in z)]
    assert zeilen[0] == PARAMETERKOPF, T_PARAMETER + ".csv: Kopfzeile " + ";".join(zeilen[0])
    v4 = v4_zeilen() + inekon_zeilen()
    erzeugt = {z[0] for z in v4}
    uebrige = [z for z in zeilen[1:] if z[0] not in erzeugt]
    return "".join(";".join(z) + "\r\n" for z in [PARAMETERKOPF] + uebrige) + csv_text(PARAMETERKOPF, v4).split("\r\n", 1)[1]


def feld(w):
    """Ein Feld im Paketformat N2: Punkt als Dezimaltrenner, leer = NULL, Zahl rundreisefest."""
    if w is None:
        return ""
    if isinstance(w, float):
        return repr(w)                              # kuerzeste Schreibweise, die float() zurueckgibt
    return str(w)


def csv_text(kopf, zeilen):
    """Eine Datei des Paketformats N2: Kopfzeile, Trenner ';', Zeilenende CRLF, UTF-8 ohne BOM."""
    return "".join(";".join(feld(w) for w in z) + "\r\n" for z in [list(kopf)] + zeilen)


def abgeleitete_traeger():
    """Die drei Traegerdateien der abgeleiteten Werte als {Tabelle: Text}."""
    # Die ID ist allein Schluessel des Pakets (wie beim Bedarfstag); die Datenbank vergibt die echte.
    nummer = {s[0]: i + 1 for i, s in enumerate(ABGELEITETE_SAETZE)}
    saetze = [[nummer[s[0]], s[0], STATUS_PAKET, 1] for s in ABGELEITETE_SAETZE]
    gaenge = []
    for (satz, werte, quelle, ausgabe, herkunft, version) in ABGELEITETE_SAETZE:
        for tagtyp, anteile_ in sorted(werte.items()):
            gaenge.append([nummer[satz], tagtyp] + list(anteile_) + [quelle, ausgabe, version, herkunft])
    arten = []
    for n in ABGELEITETE_NUTZUNGSARTEN:
        provenienz = [n["quelle"], n["ausgabe"], n["version"], n["herkunft"]]
        z = [n["name"], n["bezug"], n["bedarf"][0], n["bedarf"][1], n["bedarf"][2]] + provenienz
        z += [n["bezug_zapf"], n["bezug_kalt"], n["grenze"], n["kalender"], n["ferien"]]
        z += list(n["monate"]) + provenienz + list(n["woche"]) + provenienz
        z += [nummer[n["satz"]], STATUS_PAKET, 1]
        arten.append(z)
    return {
        T_TAGESGANGSATZ: csv_text(["ID", "Bezeichner", "Status", "ReadOnly"], saetze),
        T_TAGESGANG: csv_text(["ID_Tagesgangsatz", "Tagtyp"] + list(STUNDEN) +
                              ["Quelle", "Ausgabe", "Version", "Herkunftsart"], gaenge),
        T_NUTZUNGSART: csv_text(
            ["Bezeichner", "Bezugsart", "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch",
             "Bedarf_Quelle", "Bedarf_Ausgabe", "Bedarf_Version", "Bedarf_Herkunftsart",
             "Bezug_Zapftemperatur", "Bezug_Kaltwasser", "Bilanzgrenze", "Kalenderart", "Ferienfaktor"] +
            ["Monat_%d" % m for m in range(1, 13)] +
            ["Jahresgang_Quelle", "Jahresgang_Ausgabe", "Jahresgang_Version", "Jahresgang_Herkunftsart"] +
            ["Woche_%d" % t for t in range(1, 8)] +
            ["Wochengang_Quelle", "Wochengang_Ausgabe", "Wochengang_Version", "Wochengang_Herkunftsart"] +
            ["ID_Tagesgangsatz", "Status", "ReadOnly"], arten),
    }


def traeger_pruefen_oder_schreiben(schreiben):
    """Haelt die drei Traegerdateien gegen das Erzeugnis; mit `schreiben` werden sie neu geschrieben.
    Rueckgabe: die Zahl der geaenderten Dateien. Verglichen wird zeilenweise - der Arbeitsbaum
    checkt sie je nach Plattform mit CRLF oder LF aus (text=auto)."""
    geaendert = 0
    traeger = abgeleitete_traeger()
    traeger[T_PARAMETER] = parameter_traeger()
    for t, text in sorted(traeger.items()):
        pfad = os.path.join(PAKETTEIL, t + ".csv")
        ist = None
        if os.path.exists(pfad):
            with open(pfad, encoding="utf-8-sig", newline="") as f:
                ist = f.read()
        if ist is not None and ist.splitlines() == text.splitlines():
            continue
        if not schreiben:
            raise AssertionError(
                "%s.csv des freien Paketteils fehlt oder weicht vom Erzeugnis dieses Skripts ab - "
                "mit %s neu erzeugen und im selben Schritt mitcommitten." % (t, SCHALTER_SCHREIBEN))
        with open(pfad, "w", encoding="utf-8", newline="") as f:
            f.write(text)
        geaendert += 1
    return geaendert


def vorgabesatz(zeilen, gr):
    """Die Kategoriezeilen der Gruppe `gr`; ohne solche die Zeilen ohne Gruppe (Rueckfall)."""
    satz = [z for z in zeilen if (z.get(SPALTE_GRUPPE) or None) == gr]
    return satz if satz else [z for z in zeilen if not z.get(SPALTE_GRUPPE)]


def paketteil_lesen():
    """Je Tabelle die Zeilen des Paketteils als dict Spaltenname -> Text (leeres Feld = None)."""
    teil = {}
    for t in PAKETTEIL_TABELLEN:
        pfad = os.path.join(PAKETTEIL, t + ".csv")
        with open(pfad, encoding="utf-8-sig", newline="") as f:
            text = f.read()
        trenner = ";" if ";" in text.split("\n", 1)[0] else ","
        zeilen = [z for z in csv.reader(io.StringIO(text), delimiter=trenner) if z and any(s.strip() for s in z)]
        kopf = [s.strip() for s in zeilen[0]]
        teil[t] = []
        for n, z in enumerate(zeilen[1:], start=2):
            assert len(z) == len(kopf), f"{t}.csv Zeile {n}: {len(z)} Felder, die Kopfzeile nennt {len(kopf)}"
            teil[t].append({k: (w if w != "" else None) for k, w in zip(kopf, z)})
    return teil


PAKET = paketteil_lesen()


def pruefe_paketteil(teil):
    """Die Regeln des Paketteils (LIESMICH.md des Ordners) - vor jedem Schreiben."""
    for t, zeilen in teil.items():
        assert zeilen, f"{t}.csv: keine Zeile"
        assert "Katalogversion" not in zeilen[0], f"{t}.csv: der Paketteil fuehrt keine Katalogversion"
        if t == T_EREIGNIS:
            continue
        zulaessig = PARAMETER_HERKUNFT if t == T_PARAMETER else (HERKUNFT_FREI,)
        for z in zeilen:
            assert z.get("Herkunftsart") in zulaessig, f"{t}.csv: Herkunftsart {z.get('Herkunftsart')} statt {'/'.join(zulaessig)}"
            assert z.get("Status") == STATUS_PAKET, f"{t}.csv: Status {z.get('Status')} statt AUSLIEFERUNG"
            assert z.get("ReadOnly") in (None, "1"), f"{t}.csv: ReadOnly {z.get('ReadOnly')} statt 1"
    assert "ID_Nutzungsart" not in teil[TABELLE_KATEGORIEN][0], "Kategorien des Paketteils: Vorgabesatz ohne Nutzungsart"
    koepfe = {z["ID"] for z in teil[T_BEDARFSTAG]}
    for e in teil[T_EREIGNIS]:
        assert e["ID_Bedarfstag"] in koepfe, "Ereignis ohne Bedarfstag im Paketteil"
        assert sorted(k for k in e if k not in ("ID", "ID_Bedarfstag")) == sorted(EREIGNISSPALTEN), "Ereignisspalten"
    # Je Gruppe EIN Vorgabesatz, und je Satz summieren die Anteile auf 1.
    gruppen = {z.get(SPALTE_GRUPPE) for z in teil[TABELLE_KATEGORIEN]}
    assert gruppen <= {None, GRUPPE_WOHNEN, GRUPPE_NICHTWOHNEN}, f"Kategorien des Paketteils: Gruppen {gruppen}"
    for gr in sorted(g for g in gruppen if g) or [None]:
        satz = vorgabesatz(teil[TABELLE_KATEGORIEN], gr)
        summe = sum(float(k["Anteil"]) for k in satz)
        assert abs(summe - 1.0) < 1e-12, f"Vorgabesatz {gr}: Anteile {summe}"
        namen = [k["Kategorie"] for k in satz]
        assert len(set(namen)) == len(namen), f"Vorgabesatz {gr}: Kategorie doppelt"


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
    "Tab_TwwBedarfstag_STAMM": len(BEDARFSTAGE) + len(PAKET[T_BEDARFSTAG]),
    "Tab_TwwBedarfstagEreignis_STAMM": sum(len(t[3]) for t in BEDARFSTAGE) + len(PAKET[T_EREIGNIS]),
    "Tab_TwwParameter_STAMM": len(PARAMETER) + len(PAKET[T_PARAMETER]),
    "Tab_TwwDin4708Wert_STAMM": len(DIN4708_WERTE),
    # Seit ZU7 traegt genau das eine Referenzprojekt 1045 eine Zeile in Tab_TwwZone/-Projekt
    # (referenzprojekt_zapfprofil.py); keine Wohnungstypen.
    "Tab_TwwZone": 1,
    "Tab_TwwWohnungstyp": 0,
    "Tab_TwwProjekt": 1,
    # Je Nutzungsart der Vorgabesatz IHRER Gruppe (Stufe Z5).
    TABELLE_KATEGORIEN: sum(len(vorgabesatz(PAKET[TABELLE_KATEGORIEN], gruppe(n["kalender"])))
                            for n in ALLE_NUTZUNGSARTEN),
}


def pruefe_summen():
    """Die erfundenen Formen sind in sich stimmig, der Paketteil haelt seine Regeln - vor jedem Schreiben."""
    for (name, gaenge, _q, _a, _h, _v) in SAETZE:
        assert sorted(gaenge) == [1, 2, 3, 4], f"{name}: Tagtypen"
        for t, a in gaenge.items():
            assert len(a) == 24 and abs(sum(a) - 1.0) < 1e-12, f"{name}, Tagtyp {t}: Summe {sum(a)}"
    for n in ALLE_NUTZUNGSARTEN:
        assert len(n["monate"]) == 12 and abs(sum(n["monate"]) / 12.0 - 1.0) < 1e-12, f"{n['name']}: Monatsmittel"
        assert len(n["woche"]) == 7 and abs(sum(n["woche"]) - 1.0) < 1e-12, f"{n['name']}: Wochensumme"
        assert n["satz"] in [s[0] for s in SAETZE], f"{n['name']}: Tagesgangsatz"
    pruefe_paketteil(PAKET)


def zahl(con, sql, *p):
    return con.execute(sql, p).fetchone()[0]


def spaltentypen(con, tabelle):
    return {r[1]: (r[2] or "").upper() for r in con.execute("SELECT * FROM pragma_table_info(?)", (tabelle,))}


def typisiert(roh, typ):
    """Ein CSV-Feld im Typ der Spalte (wie TwwKataloge.Wert): INTEGER, REAL oder Text; None bleibt."""
    if roh is None:
        return None
    if typ.startswith("INT"):
        return int(roh)
    if typ.startswith("REAL"):
        return float(roh)
    return roh


def paketwerte(con, tabelle, zeile, ohne=()):
    """Die Werte einer Paketzeile im Typ ihrer Spalten, ohne die genannten Spalten."""
    typen = spaltentypen(con, tabelle)
    for s in zeile:
        assert s in typen, f"{tabelle}.csv: die Spalte {s} gibt es in {tabelle} nicht"
    return {s: typisiert(w, typen[s]) for s, w in zeile.items() if s not in ohne}


def spaltenliste(namen):
    return ", ".join('"' + s + '"' for s in namen)


def upsert(con, tabelle, schluessel, werte):
    """Legt die Zeile an oder fuehrt sie nach: (angelegt, nachgefuehrt, ID)."""
    bedingung = " AND ".join(f'"{k}" = ?' for k in schluessel)
    zeile = con.execute(f'SELECT "ID" FROM "{tabelle}" WHERE {bedingung}', list(schluessel.values())).fetchone()
    if zeile is None:
        alle = dict(schluessel)
        alle.update(werte)
        cur = con.execute(f'INSERT INTO "{tabelle}" ({spaltenliste(alle)}) VALUES ({", ".join("?" for _ in alle)})',
                          list(alle.values()))
        return 1, 0, cur.lastrowid
    setzen = ", ".join(f'"{k}" = ?' for k in werte)
    abweichend = " OR ".join(f'"{k}" IS NOT ?' for k in werte)
    cur = con.execute(f'UPDATE "{tabelle}" SET {setzen} WHERE "ID" = ? AND ({abweichend})',
                      list(werte.values()) + [zeile[0]] + list(werte.values()))
    return 0, cur.rowcount, zeile[0]


def ereignisse_setzen(con, id_tag, ereignisse):
    """Die Ereignisse des Bedarfstags genau so (dicts mit EREIGNISSPALTEN, in Reihenfolge); ersetzt,
    wenn sie abweichen: (angelegt, nachgefuehrt)."""
    soll = [{s: e[s] for s in EREIGNISSPALTEN} for e in ereignisse]
    ist = [dict(zip(EREIGNISSPALTEN, r)) for r in con.execute(
        f'SELECT {spaltenliste(EREIGNISSPALTEN)} FROM "{T_EREIGNIS}" WHERE "ID_Bedarfstag" = ? '
        'ORDER BY "Reihenfolge", "ID"', (id_tag,))]
    if ist == soll:
        return 0, 0
    alt = con.execute(f'DELETE FROM "{T_EREIGNIS}" WHERE "ID_Bedarfstag" = ?', (id_tag,)).rowcount
    for e in soll:
        con.execute(f'INSERT INTO "{T_EREIGNIS}" ("ID_Bedarfstag", {spaltenliste(EREIGNISSPALTEN)}) '
                    f'VALUES (?, {", ".join("?" for _ in EREIGNISSPALTEN)})', [id_tag] + list(e.values()))
    return (len(soll), 0) if alt == 0 else (0, len(soll))


def main():
    if len(sys.argv) < 2:
        print("Aufruf: tww_testkatalog_fiktiv.py <Kenndaten_Test.sqlite> [--stochastik] "
              "[" + SCHALTER_SCHREIBEN + "]")
        return 2
    pruefe_summen()
    schreiben = SCHALTER_SCHREIBEN in sys.argv[2:]
    geaendert = traeger_pruefen_oder_schreiben(schreiben)
    if schreiben:
        print("Freier Paketteil: %d von %d erzeugten Datei(en) neu geschrieben (drei Traeger der abgeleiteten "
              "Werte, Parameterdatei mit den Setzungen der Speicherauslegung aus V4)."
              % (geaendert, len(PAKETTEIL_ABGELEITET) + 1))
        # Die Parameterdatei ist zugleich gelesener Teil des Pakets: neu lesen und pruefen.
        PAKET.clear()
        PAKET.update(paketteil_lesen())
        pruefe_paketteil(PAKET)
        ERWARTET["Tab_TwwParameter_STAMM"] = len(PARAMETER) + len(PAKET[T_PARAMETER])

    con = sqlite3.connect(sys.argv[1])
    try:
        con.execute("PRAGMA foreign_keys = ON")
        stand = zahl(con, "SELECT SchemaVersion FROM Tab_Applikation")
        fehlend = [t for t in ERWARTET
                   if zahl(con, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", t) == 0]
        if fehlend:
            print(f"Schemastand {stand}: es fehlen {', '.join(fehlend)} - erst Werkzeuge/Testdatenbankschema"
                  + (" (die Zapfkategorien brauchen den Schemaschritt T2, Schritt 115)" if TABELLE_KATEGORIEN in fehlend else "")
                  + ". Abbruch ohne Schreiben.")
            return 2
        if "BEZUGSART" not in {k.upper() for k in spaltentypen(con, T_BEDARFSTAG)}:
            print(f"Schemastand {stand}: {T_BEDARFSTAG}.Bezugsart fehlt - erst Werkzeuge/Testdatenbankschema "
                  "(Schemaschritt T3, Schritt 124). Abbruch ohne Schreiben.")
            return 2

        # Fremde Zeilen? Alles, was nicht Katalogversion TEST-1 / Status EIGEN / ReadOnly 0 ist.
        fremd = 0
        for t in ("Tab_TwwTagesgangsatz_STAMM", "Tab_TwwNutzungsart_STAMM", "Tab_TwwBedarfstag_STAMM",
                  "Tab_TwwParameter_STAMM", "Tab_TwwDin4708Wert_STAMM"):
            fremd += zahl(con, f'SELECT COUNT(*) FROM "{t}" WHERE "Katalogversion" <> ? OR "Status" <> ? '
                               'OR "ReadOnly" <> 0', VERSION, STATUS)
        # Die Kategorien tragen keine eigene Katalogversion - sie gehoeren zu ihrer Nutzungsart.
        fremd += zahl(con, f'SELECT COUNT(*) FROM "{TABELLE_KATEGORIEN}" WHERE "Status" <> ? OR "ReadOnly" <> 0 '
                           'OR "ID_Nutzungsart" NOT IN (SELECT "ID" FROM "Tab_TwwNutzungsart_STAMM" '
                           'WHERE "Katalogversion" = ?)', STATUS, VERSION)
        # Tab_TwwZone/-Projekt tragen seit ZU7 genau die eine gesaete Zeile des Referenzprojekts
        # 1045 (referenzprojekt_zapfprofil.py); jede andere Zeile bleibt fremd.
        for t in ("Tab_TwwZone", "Tab_TwwProjekt"):
            fremd += zahl(con, f'SELECT COUNT(*) FROM "{t}" WHERE "ID_Projekt" <> ?', REFERENZPROJEKT_GENERATOR)
        fremd += zahl(con, 'SELECT COUNT(*) FROM "Tab_TwwWohnungstyp"')
        if fremd:
            print(f"{fremd} Zeile(n) gehoeren nicht zum fiktiven Testkatalog - Abbruch ohne Schreiben.")
            return 2

        zaehler = [0, 0]                        # angelegt, nachgefuehrt

        def zaehlen(ergebnis):
            zaehler[0] += ergebnis[0]
            zaehler[1] += ergebnis[1]
            return ergebnis[2] if len(ergebnis) > 2 else None

        testdb = {"Status": STATUS, "ReadOnly": 0}
        with con:
            # --- Tagesgangsaetze und ihre vier Tagesgaenge (fiktiv und abgeleitet) -------------
            id_saetze = {}
            for (satz, gaenge, quelle, ausgabe, herkunft, version) in SAETZE:
                id_satz = zaehlen(upsert(con, "Tab_TwwTagesgangsatz_STAMM",
                                         {"Bezeichner": satz, "Katalogversion": VERSION},
                                         {"Status": STATUS, "Beleg": None, "ReadOnly": 0}))
                id_saetze[satz] = id_satz
                for tagtyp, werte in sorted(gaenge.items()):
                    w = {f"Anteil_{h:02d}": werte[h - 1] for h in range(1, 25)}
                    w.update({"Quelle": quelle, "Ausgabe": ausgabe, "Version": version, "Herkunftsart": herkunft})
                    zaehlen(upsert(con, "Tab_TwwTagesgang_STAMM", {"ID_Tagesgangsatz": id_satz, "Tagtyp": tagtyp}, w))

            # --- Nutzungsarten (fiktiv und abgeleitet) ------------------------------------------
            id_arten = []
            for n in ALLE_NUTZUNGSARTEN:
                w = {"Bezugsart": n["bezug"], "Bedarf_Niedrig": n["bedarf"][0], "Bedarf_Mittel": n["bedarf"][1],
                     "Bedarf_Hoch": n["bedarf"][2],
                     "Bezug_Zapftemperatur": n["bezug_zapf"], "Bezug_Kaltwasser": n["bezug_kalt"],
                     "Bilanzgrenze": n["grenze"], "Kalenderart": n["kalender"], "Ferienfaktor": n["ferien"]}
                for g in ("Bedarf", "Jahresgang", "Wochengang"):
                    w.update({f"{g}_Quelle": n["quelle"], f"{g}_Ausgabe": n["ausgabe"], f"{g}_Version": n["version"],
                              f"{g}_Herkunftsart": n["herkunft"]})
                w.update({f"Monat_{m}": n["monate"][m - 1] for m in range(1, 13)})
                w.update({f"Woche_{t}": n["woche"][t - 1] for t in range(1, 8)})
                w.update({"ID_Tagesgangsatz": id_saetze[n["satz"]], "ID_Vorlage": None, "Status": STATUS,
                          "Beleg": None, "Freigabe": None, "ReadOnly": 0})
                id_arten.append(zaehlen(upsert(con, "Tab_TwwNutzungsart_STAMM",
                                               {"Bezeichner": n["name"], "Katalogversion": VERSION}, w)))

            # --- Parameter: fiktiv, dann die des Paketteils ---------------------------------
            for (schluessel, wert, einheit) in PARAMETER:
                zaehlen(upsert(con, T_PARAMETER, {"Schluessel": schluessel, "Katalogversion": VERSION},
                               {"Wert": wert, "Einheit": einheit, "Quelle": QUELLE, "Ausgabe": None, "Version": VERSION,
                                "Herkunftsart": HERKUNFT, "Status": STATUS, "Beleg": None, "ReadOnly": 0}))
            frei_parameter = []
            for z in PAKET[T_PARAMETER]:
                w = paketwerte(con, T_PARAMETER, z, ohne=("ID", "Schluessel"))
                w.update(testdb)
                zaehlen(upsert(con, T_PARAMETER, {"Schluessel": z["Schluessel"], "Katalogversion": VERSION}, w))
                frei_parameter.append(z["Schluessel"])

            # --- Bedarfstage samt Ereignissen: fiktiv, dann die des Paketteils --------------
            for (bezeichner, quelle_art, bezugsmenge, ereignisse) in BEDARFSTAGE:
                id_tag = zaehlen(upsert(con, T_BEDARFSTAG, {"Bezeichner": bezeichner, "Katalogversion": VERSION},
                                        {"Quelle_Art": quelle_art, "Bezugsmenge": bezugsmenge, "Bezugsart": None,
                                         "Quelle": QUELLE,
                                         "Ausgabe": None, "Version": VERSION, "Herkunftsart": HERKUNFT,
                                         "Status": STATUS, "Beleg": None, "ReadOnly": 0}))
                zaehlen(ereignisse_setzen(con, id_tag, [dict(zip(EREIGNISSPALTEN, e)) for e in ereignisse]))
            frei_tage = []
            for z in PAKET[T_BEDARFSTAG]:
                w = paketwerte(con, T_BEDARFSTAG, z, ohne=("ID", "Bezeichner"))
                w.update(testdb)
                id_tag = zaehlen(upsert(con, T_BEDARFSTAG, {"Bezeichner": z["Bezeichner"], "Katalogversion": VERSION}, w))
                zaehlen(ereignisse_setzen(con, id_tag, [paketwerte(con, T_EREIGNIS, e, ohne=("ID", "ID_Bedarfstag"))
                                                        for e in PAKET[T_EREIGNIS] if e["ID_Bedarfstag"] == z["ID"]]))
                frei_tage.append(z["Bezeichner"])

            # --- DIN-4708-Werte ---------------------------------------------------------
            for (art, schluessel, wert) in DIN4708_WERTE:
                zaehlen(upsert(con, "Tab_TwwDin4708Wert_STAMM",
                               {"Art": art, "Schluessel": schluessel, "Katalogversion": VERSION},
                               {"Wert": wert, "Quelle": QUELLE, "Ausgabe": None, "Version": VERSION,
                                "Herkunftsart": HERKUNFT, "Status": STATUS, "Beleg": None, "ReadOnly": 0}))

            # --- Zapfkategorien: je Nutzungsart der Vorgabesatz IHRER Gruppe (Stufe Z5) ---------
            for id_art, n in zip(id_arten, ALLE_NUTZUNGSARTEN):
                satz = vorgabesatz(PAKET[TABELLE_KATEGORIEN], gruppe(n["kalender"]))
                namen = [z["Kategorie"] for z in satz]
                for z in satz:
                    ohne_gruppe = {s: w for s, w in z.items() if s != SPALTE_GRUPPE}
                    w = paketwerte(con, TABELLE_KATEGORIEN, ohne_gruppe, ohne=("ID", "Kategorie"))
                    w.update(testdb)
                    zaehlen(upsert(con, TABELLE_KATEGORIEN, {"ID_Nutzungsart": id_art, "Kategorie": z["Kategorie"]}, w))
                zaehler[1] += con.execute(
                    f'DELETE FROM "{TABELLE_KATEGORIEN}" WHERE "ID_Nutzungsart" = ? AND "Kategorie" NOT IN '
                    f'({", ".join("?" for _ in namen)})', [id_art] + namen).rowcount

            # --- Freie Zeilen, die der Paketteil nicht (mehr) fuehrt, fallen ------------------
            zaehler[1] += con.execute(
                f'DELETE FROM "{T_PARAMETER}" WHERE "Herkunftsart" IN (?, ?) AND "Katalogversion" = ? AND "Schluessel" NOT IN '
                f'({", ".join("?" for _ in frei_parameter)})', list(PARAMETER_HERKUNFT) + [VERSION] + frei_parameter).rowcount
            zaehler[1] += con.execute(
                f'DELETE FROM "{T_BEDARFSTAG}" WHERE "Herkunftsart" = ? AND "Katalogversion" = ? AND "Bezeichner" NOT IN '
                f'({", ".join("?" for _ in frei_tage)})', [HERKUNFT_FREI, VERSION] + frei_tage).rowcount

        angelegt, nachgefuehrt = zaehler
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
                                     "Tab_TwwDin4708Wert_STAMM", TABELLE_KATEGORIEN))
        integritaet = con.execute("PRAGMA integrity_check").fetchone()[0]
        fk = con.execute("PRAGMA foreign_key_check").fetchall()
        print(f"  Status AUSLIEFERUNG/IMPORT: {ausgeliefert}; integrity_check: {integritaet}; "
              f"foreign_key_check: {len(fk)} Befund(e)")
        return 0 if ok and ausgeliefert == 0 and integritaet == "ok" and not fk else 2
    finally:
        con.close()


if __name__ == "__main__":
    sys.exit(main())
