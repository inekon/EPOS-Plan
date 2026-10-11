#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Leitet aus den BDEW-Standardlastprofilen Strom 2025 die Katalogsaat der „Datenbank Strombedarf"
ab (H25, G25, L25; dazu die Netzbezugsprofile P25 und S25, keine Verbrauchsprofile) und schreibt sie
als C#-Konstanten nach EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs.

Aufruf aus der Repowurzel (Windows: py mit PYTHONIOENCODING=utf-8 davor, sonst python3):
    ableiten.py               prüft nur, ob die Konstantendatei dem Stand der Excel entspricht
    ableiten.py schreiben     schreibt die Konstantendatei (UTF-8 mit BOM, CRLF)
    ableiten.py kennzahlen    gibt die Kennzahlen je Profil aus
    Option --excel <pfad>     eine andere Excel als Quellen/Standardlastprofile/…xlsx

Rückgabe: 0 = unverändert bzw. geschrieben, 1 = Datei weicht ab oder fehlt (Prüfmodus), 2 = Fehler.

Die Rechenregeln stehen in LIESMICH.md daneben. Das Werkzeug liest die Excel ohne Zusatzpakete
(zipfile + XML), rechnet mit math.fsum und Decimal und schreibt deterministisch: gleiche Excel,
gleiche Datei, Byte für Byte.
"""
import hashlib
import math
import os
import sys
import zipfile
import datetime
import xml.etree.ElementTree as ET
from decimal import Decimal, ROUND_HALF_UP

WURZEL = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
EXCEL_REL = 'Quellen/Standardlastprofile/BDEW_Repraesentative_Profile_H25_G25_L25_P25_S25.xlsx'
ZIEL_REL = 'EPOS.Kern/Allgemein/Update/StandardlastprofilSaat.cs'

# Blatt (= Kürzel in C1), Titel in A1, Katalogsatz (Bezeichner), Typprofil (Typname), Dynamisierung nach PDF S. 3-5,
# Netzbezug: bei P25 und S25 die Lieferstelle, deren Bezug aus dem Netz nach dem Eigenverbrauch das Profil beschreibt
# (PDF S. 4-5; KEIN Verbrauchsprofil, eigene Liste und eigener Schemaschritt), sonst None
PROFILE = (
    ('H25', 'Haushalt', 'BDEW_H25_Haushalt', 'BDEW_H25', True, None),
    ('G25', 'Gewerbe allgemein', 'BDEW_G25_Gewerbe', 'BDEW_G25', False, None),
    ('L25', 'Landwirtschaftsbetriebe', 'BDEW_L25_Landwirtschaft', 'BDEW_L25', False, None),
    ('P25', 'Kombinationsprofil', 'BDEW_P25_Haushalt_PV', 'BDEW_P25', True, 'eines Haushalts mit PV-Anlage'),
    ('S25', 'Kombinationsprofil', 'BDEW_S25_Haushalt_PV_Speicher', 'BDEW_S25', True,
     'eines Haushalts mit PV-Anlage und Batteriespeicher'),
)
TYPTAGE = ('SA', 'FT', 'WT')                     # Spaltenfolge je Monat in der Excel
SA, FT, WT = 0, 1, 2
MONATSTAGE = (31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31)   # Raster 365 Tage, kein Schaltjahr
WOCHENTAGE = ('Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag', 'Sonntag')
JAHRESSUMME_MWH = 1000                           # BDEW: 1 Mio. kWh Jahresverbrauch
STELLEN = 6                                      # Nachkommastellen der Konstanten
QUELLE = 'BDEW, Standardlastprofile Strom 2025, Veröffentlichung vom 17.03.2025'

NS = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
NSR = '{http://schemas.openxmlformats.org/officeDocument/2006/relationships}'


class Fehler(Exception):
    """Excel fehlt oder ist anders aufgebaut als erwartet."""


def runden(wert, stellen):
    """Kaufmännisch auf <stellen> Nachkommastellen (wie ROUND in Excel)."""
    return float(Decimal(repr(wert)).quantize(Decimal(1).scaleb(-stellen), rounding=ROUND_HALF_UP))


def spalte_nummer(buchstaben):
    n = 0
    for b in buchstaben:
        n = n * 26 + ord(b) - 64
    return n


# ---------------------------------------------------------------------------------------------
#  Excel lesen und prüfen
# ---------------------------------------------------------------------------------------------

def blatt_zellen(z, pfad, texte):
    """{(zeile, spalte): wert} eines Blatts; Zahlen als float, Texte als str. Formeln sind ein Fehler."""
    zellen = {}
    for zeile in ET.fromstring(z.read(pfad)).find(NS + 'sheetData'):
        for c in zeile:
            if c.find(NS + 'f') is not None:
                raise Fehler('Formel in ' + pfad + ' ' + c.get('r'))
            art, v = c.get('t'), c.find(NS + 'v')
            if art == 'inlineStr':
                wert = ''.join(t.text or '' for t in c.iter(NS + 't'))
            elif v is None:
                continue
            elif art == 's':
                wert = texte[int(v.text)]
            elif art in (None, 'n'):
                wert = float(v.text)
            else:
                wert = v.text
            ref = c.get('r')
            i = 0
            while ref[i].isalpha():
                i += 1
            zellen[(int(ref[i:]), spalte_nummer(ref[:i]))] = wert
    return zellen


def viertelstunde(q):
    a, b = q * 15, (q + 1) * 15 % 1440
    return '%02d:%02d-%02d:%02d' % (a // 60, a % 60, b // 60, b % 60)


def excel_lesen(pfad):
    """{Blatt: werte[monat][typtag][viertel]} der fünf Profile, Typtag in der Folge SA, FT, WT."""
    if not os.path.isfile(pfad):
        raise Fehler('Excel nicht gefunden: ' + pfad)
    ergebnis = {}
    with zipfile.ZipFile(pfad) as z:
        texte = []
        if 'xl/sharedStrings.xml' in z.namelist():
            for si in ET.fromstring(z.read('xl/sharedStrings.xml')).findall(NS + 'si'):
                texte.append(''.join(t.text or '' for t in si.iter(NS + 't')))
        ziele = {r.get('Id'): r.get('Target') for r in ET.fromstring(z.read('xl/_rels/workbook.xml.rels'))}
        blaetter = {}
        for s in ET.fromstring(z.read('xl/workbook.xml')).find(NS + 'sheets'):
            ziel = ziele[s.get(NSR + 'id')]
            blaetter[s.get('name')] = ziel.lstrip('/') if ziel.startswith('/') else 'xl/' + ziel
        for blatt, titel, _, _, dyn, netz in PROFILE:
            if blatt not in blaetter:
                raise Fehler('Blatt ' + blatt + ' fehlt')
            zellen = blatt_zellen(z, blaetter[blatt], texte)
            kopf = ' '.join(str(v) for (r, _), v in sorted(zellen.items()) if r == 1)
            # P25 und S25 tragen beide den Titel „Kombinationsprofil " (mit Leerzeichen); das Kürzel in C1 trennt sie.
            if str(zellen.get((1, 1), '')).strip() != titel or zellen.get((1, 3)) != blatt or '1 Mio kWh' not in kopf:
                raise Fehler(blatt + ': Kopfzeile unerwartet: ' + kopf)
            if ('nicht anzuwenden' not in kopf) != dyn or 'Dynamisierungsfunktion' not in kopf:
                raise Fehler(blatt + ': Hinweis zur Dynamisierung passt nicht: ' + kopf)
            if ('SOT-Zeitreihe' in kopf) != (netz is not None):
                raise Fehler(blatt + ': Hinweis zur SOT-Zeitreihe (Netzbezug mit PV) passt nicht: ' + kopf)
            if zellen.get((4, 2)) != '[kWh]':
                raise Fehler(blatt + ': Einheit in B4 ist nicht [kWh]')
            werte = [[[0.0] * 96 for _ in range(3)] for _ in range(12)]
            for k in range(36):
                spalte, monat, typ = 3 + k, k // 3, k % 3
                if zellen.get((4, spalte)) != TYPTAGE[typ]:
                    raise Fehler('%s: Typtag in Spalte %d ist %r' % (blatt, spalte, zellen.get((4, spalte))))
                datum = zellen.get((3, spalte))
                if not isinstance(datum, float) or \
                        (datetime.date(1899, 12, 30) + datetime.timedelta(days=int(datum))).month != monat + 1:
                    raise Fehler('%s: Monatskopf in Spalte %d ist %r' % (blatt, spalte, datum))
                for q in range(96):
                    if k == 0 and zellen.get((5 + q, 2)) != viertelstunde(q):
                        raise Fehler('%s: Viertelstunde in Zeile %d ist %r' % (blatt, 5 + q, zellen.get((5 + q, 2))))
                    x = zellen.get((5 + q, spalte))
                    if not isinstance(x, float) or not x > 0:
                        raise Fehler('%s: Wert in Zeile %d, Spalte %d ist %r' % (blatt, 5 + q, spalte, x))
                    werte[monat][typ][q] = x
            if any(not (r <= 100 and s <= 38) for (r, s) in zellen):
                raise Fehler(blatt + ': Zellen außerhalb des Rasters 12 x 3 x 96')
            ergebnis[blatt] = werte
    return ergebnis


# ---------------------------------------------------------------------------------------------
#  Rechnen
# ---------------------------------------------------------------------------------------------

def faktor(t):
    """Dynamisierungsfaktor des Tags t (1 … 365), auf 4 Nachkommastellen (PDF S. 4)."""
    return runden(-3.92e-10 * t ** 4 + 3.2e-7 * t ** 3 - 7.02e-5 * t ** 2 + 2.1e-3 * t + 1.24, 4)


def typtag(wochentag):
    """Typtag eines Wochentags (Montag = 0 … Sonntag = 6): Mo-Fr Werktag, Sa Samstag, So Sonn-/Feiertag."""
    return WT if wochentag < 5 else (SA if wochentag == 5 else FT)


def monatsenergien(werte, dyn):
    """
    Monatsenergien [MWh] der Abrollung über das Jahr, als Mittel über die sieben möglichen Wochentage
    des 1. Januar (auf dem Raster von 365 Tagen ist das der 28-Jahre-Kalenderzyklus); Werktag, Samstag,
    Sonntag ohne Feiertage. Mit Dynamisierung: je Tag des Jahres t der Faktor F(t), der Wert danach auf
    3 Nachkommastellen (PDF S. 4).
    """
    je_start = [[0.0] * 12 for _ in range(7)]
    for start in range(7):
        t = 0
        for monat in range(12):
            tage = []
            for _ in range(MONATSTAGE[monat]):
                t += 1
                tag = werte[monat][typtag((start + t - 1) % 7)]
                if dyn:
                    f = faktor(t)
                    tage.append(math.fsum(runden(x * f, 3) for x in tag))
                else:
                    tage.append(math.fsum(tag))
            je_start[start][monat] = math.fsum(tage)
    return [math.fsum(je_start[s][m] for s in range(7)) / 7 / 1000 for m in range(12)]


def monatswerte(roh):
    """Die zwölf Monatswerte, exakt auf JAHRESSUMME_MWH skaliert und auf STELLEN Nachkommastellen
    gebracht; der Rundungsrest geht nach dem größten Rest (Summe der Konstanten = 1.000,000000)."""
    gesamt = math.fsum(roh)
    einheit = 10 ** STELLEN
    genau = [Decimal(repr(r)) * Decimal(JAHRESSUMME_MWH * einheit) / Decimal(repr(gesamt)) for r in roh]
    ganz = [int(g) for g in genau]                                   # abgerundet (alle Werte > 0)
    rest = JAHRESSUMME_MWH * einheit - sum(ganz)
    for i in sorted(range(12), key=lambda i: (genau[i] - ganz[i], -i), reverse=True)[:rest]:
        ganz[i] += 1
    return [g / einheit for g in ganz]


def wochenwerte(werte):
    """
    168 Stundenwerte Montag 0 Uhr … Sonntag 23 Uhr [kWh je Stunde bei 1.000 MWh/a]: je Typtag und Stunde
    die Summe der vier Viertelstunden, gemittelt über die zwölf Monate mit der Zahl ihrer Tage (im
    28-Jahre-Zyklus fällt jeder Typtag anteilig gleich auf jeden Tag des Monats); ohne Dynamisierung.
    """
    je_typ = []
    for typ in range(3):
        stunden = []
        for s in range(24):
            stunden.append(math.fsum(MONATSTAGE[m] * math.fsum(werte[m][typ][4 * s:4 * s + 4])
                                     for m in range(12)) / sum(MONATSTAGE))
        je_typ.append(stunden)
    woche = []
    for wochentag in range(7):
        woche += je_typ[typtag(wochentag)]
    return [runden(v, STELLEN) for v in woche]


def ableiten(pfad):
    """Je Profil ein Satz mit Monatswerten, Wochenwerten und Kennzahlen."""
    saetze = []
    excel = excel_lesen(pfad)
    for blatt, titel, bezeichner, typname, dyn, netz in PROFILE:
        roh = monatsenergien(excel[blatt], dyn)
        saetze.append({
            'kuerzel': blatt, 'titel': titel, 'bezeichner': bezeichner, 'typname': typname, 'dyn': dyn,
            'netz': netz, 'roh': roh, 'monate': monatswerte(roh), 'woche': wochenwerte(excel[blatt]),
        })
    return saetze


# ---------------------------------------------------------------------------------------------
#  C#-Datei
# ---------------------------------------------------------------------------------------------

def beschreibung(s):
    if s['netz']:
        return ('BDEW-Standardlastprofil 2025 %s: Netzbezug %s — kein Verbrauchsprofil; normiert auf 1.000 MWh/a '
                'Netzbezug, im Projekt auf den Jahresnetzbezug skalieren; nicht mit einer eigenen PV-Rechnung von '
                'EPOS-Plan kombinieren (PV zählte doppelt); Feiertage über den Betriebskalender' % (s['kuerzel'], s['netz']))
    return ('BDEW-Standardlastprofil 2025 %s (%s), normiert auf 1.000 MWh/a; im Projekt auf den '
            'Jahresverbrauch skalieren; Feiertage über den Betriebskalender' % (s['kuerzel'], s['titel']))


def typbeschreibung(s):
    if s['netz']:
        return ('BDEW-Standardlastprofil 2025 %s (Netzbezug %s, kein Verbrauchsprofil): Wochenprofil aus dem '
                'Jahresmittel je Typtag (Mo–Fr Werktag, Sa Samstag, So Sonn- und Feiertag), kWh je Stunde bei '
                '1.000 MWh/a Netzbezug' % (s['kuerzel'], s['netz']))
    return ('BDEW-Standardlastprofil 2025 %s (%s): Wochenprofil aus dem Jahresmittel je Typtag (Mo–Fr Werktag, '
            'Sa Samstag, So Sonn- und Feiertag), kWh je Stunde bei 1.000 MWh/a' % (s['kuerzel'], s['titel']))


def zahlen(werte, einzug):
    return (',\n' + einzug).join(', '.join('%.*f' % (STELLEN, v) for v in werte[i:i + 12])
                                 for i in range(0, len(werte), 12))


def saetze_cs(a, saetze):
    """Die Konstruktoraufrufe einer Liste von Sätzen, durch Kommas getrennt."""
    for n, s in enumerate(saetze):
        e = ' ' * 16
        a('            new StandardlastprofilSaat(')
        a('                "%s", "%s", "%s",' % (s['kuerzel'], s['bezeichner'], s['typname']))
        a('                "%s",' % beschreibung(s))
        a('                "%s",' % typbeschreibung(s))
        a('                new[]')
        a('                {')
        a('                    // Januar bis Dezember [MWh]')
        a('                    ' + zahlen(s['monate'], e + '    ') + ',')
        a('                },')
        a('                new[]')
        a('                {')
        for tag in range(7):
            a('                    // ' + WOCHENTAGE[tag] + ' 0 bis 23 Uhr (' + ('Werktag' if tag < 5 else ('Samstag' if tag == 5 else 'Sonn- und Feiertag')) + ')')
            a('                    ' + zahlen(s['woche'][24 * tag:24 * tag + 24], e + '    ') + ',')
        a('                })' + (',' if n < len(saetze) - 1 else ''))


def cs_text(saetze, sha):
    verbrauch = [s for s in saetze if not s['netz']]
    netzbezug = [s for s in saetze if s['netz']]
    z = []
    a = z.append
    a('using System;')
    a('using System.Collections.Generic;')
    a('')
    a('namespace WindowsFormsApplication1')
    a('{')
    a('    // ====================================================================================')
    a('    // ERZEUGT von Werkzeuge/Standardlastprofile/ableiten.py - NICHT VON HAND AENDERN.')
    a('    // Pruefen: py Werkzeuge/Standardlastprofile/ableiten.py (ohne Argument), schreiben: ... schreiben.')
    a('    //')
    a('    // DIE BDEW-STANDARDLASTPROFILE STROM 2025 als Katalogsaat der "Datenbank Strombedarf",')
    a('    // gesaet mit dem Schemaschritt StandardlastprofilSchema.SCHRITT.')
    a('    // Die Netzbezugsprofile P25 und S25 saet StandardlastprofilPvSchema.SCHRITT.')
    a('    //')
    a('    // QUELLE. ' + QUELLE.replace('ö', 'oe') + '; Datei')
    a('    // ' + EXCEL_REL)
    a('    // (SHA-256 ' + sha + '). Die Dateien tragen keine Lizenzangabe;')
    a('    // ausgeliefert werden allein die abgeleiteten Werte unten (Quellen/Standardlastprofile/LIESMICH.md).')
    a('    //')
    a('    // ABLEITUNG (Werkzeuge/Standardlastprofile/LIESMICH.md):')
    a('    //   Wochenwerte: 168 Stunden Mo 0 Uhr ... So 23 Uhr; Mo-Fr aus dem Werktag (WT), Sa aus dem Samstag')
    a('    //   (SA), So aus dem Sonn- und Feiertag (FT); Stunde = Summe der vier Viertelstundenenergien;')
    a('    //   Jahresmittel je Typtag, die Monate mit der Zahl ihrer Tage gewichtet; ohne Dynamisierung.')
    a('    //   Einheit kWh je Stunde bei 1.000 MWh/a - die Skala ist fuer die Rechnung gleichgueltig,')
    a('    //   BhkwPlan.StromWocheToJahr normiert je Monat auf den Monatswert.')
    a('    //   Monatswerte: Monatsenergie [MWh] der Abrollung ueber 365 Tage, gemittelt ueber die sieben')
    a('    //   Wochentage des 1. Januar (28-Jahre-Zyklus), ohne Feiertage; H25 mit der Dynamisierung')
    a('    //   F(t) je Tag des Jahres (PDF S. 4), G25 und L25 ohne; danach exakt auf 1.000 MWh skaliert.')
    a('    //   P25 und S25 wie H25 mit F(t) (PDF S. 4-5).')
    a('    //   Feiertage legt ein Betriebskalender an der Zuordnung auf den Sonntag (BDEW: Feiertag = FT).')
    a('    //')
    a('    // NETZBEZUG. P25 und S25 beschreiben den Bezug eines Haushalts mit PV-Anlage bzw. mit PV-Anlage und')
    a('    // Batteriespeicher aus dem Netz nach dem Eigenverbrauch (PDF S. 4-5) - KEIN VERBRAUCHSPROFIL; die')
    a('    // 1.000 MWh/a sind Netzbezug. Mit einer eigenen PV-Rechnung des Projekts zaehlte die PV doppelt.')
    a('    // ====================================================================================')
    a('')
    a('    /// <summary>')
    a('    /// Ein BDEW-Standardlastprofil der Saat — Kopfsatz für <c>Tab_Stromverbraucher_STAMM</c> und Typprofil')
    a('    /// für <c>Tab_Stromverbrauchertyp_STAMM</c>, verknüpft über den Namen (<c>Typ</c> = <c>Typname</c>).')
    a('    /// </summary>')
    a('    public sealed class StandardlastprofilSaat')
    a('    {')
    a('        internal StandardlastprofilSaat(string kuerzel, string bezeichner, string typname, string beschreibung,')
    a('                                        string typbeschreibung, double[] monatswerte, double[] wochenwerte)')
    a('        {')
    a('            Kuerzel = kuerzel;')
    a('            Bezeichner = bezeichner;')
    a('            Typname = typname;')
    a('            Beschreibung = beschreibung;')
    a('            Typbeschreibung = typbeschreibung;')
    a('            Monatswerte = Array.AsReadOnly(monatswerte);')
    a('            Wochenwerte = Array.AsReadOnly(wochenwerte);')
    a('        }')
    a('')
    a('        /// <summary>Das Kürzel des BDEW (H25, G25, L25).</summary>')
    a('        /// <remarks>Die Netzbezugsprofile tragen P25 und S25 (<see cref="StandardlastprofilSaattabelle.Netzbezug"/>).</remarks>')
    a('        public string Kuerzel { get; }')
    a('')
    a('        /// <summary>Der Name des Katalogsatzes (<c>Tab_Stromverbraucher_STAMM.Bezeichner</c>).</summary>')
    a('        public string Bezeichner { get; }')
    a('')
    a('        /// <summary>Der Name des Typprofils (<c>Tab_Stromverbrauchertyp_STAMM.Typname</c>), zugleich <c>Typ</c> des Kopfsatzes.</summary>')
    a('        public string Typname { get; }')
    a('')
    a('        /// <summary>Die Beschreibung des Kopfsatzes.</summary>')
    a('        public string Beschreibung { get; }')
    a('')
    a('        /// <summary>Die Beschreibung des Typprofils.</summary>')
    a('        public string Typbeschreibung { get; }')
    a('')
    a('        /// <summary>Die zwölf Monatswerte [MWh], Januar zuerst; Summe 1.000 MWh.</summary>')
    a('        public IReadOnlyList<double> Monatswerte { get; }')
    a('')
    a('        /// <summary>Die 168 Wochenstunden [kWh je Stunde bei 1.000 MWh/a], Montag 0 Uhr zuerst.</summary>')
    a('        public IReadOnlyList<double> Wochenwerte { get; }')
    a('    }')
    a('')
    a('    /// <summary>')
    a('    /// <b>Die drei Sätze der Saat</b> (H25, G25, L25) mit Quelle und Prüfsumme der Excel, aus der sie')
    a('    /// abgeleitet sind.')
    a('    /// Dazu die zwei Netzbezugsprofile P25 und S25 (<see cref="StandardlastprofilSaattabelle.Netzbezug"/>) — keine Verbrauchsprofile.')
    a('    /// </summary>')
    a('    public static class StandardlastprofilSaattabelle')
    a('    {')
    a('        /// <summary>Die Quelle der Werte.</summary>')
    a('        public const string QUELLE = "' + QUELLE + '";')
    a('')
    a('        /// <summary>Die Excel im Repositorium, repo-relativ.</summary>')
    a('        public const string QUELLDATEI = "' + EXCEL_REL + '";')
    a('')
    a('        /// <summary>SHA-256 der Excel, aus der die Werte abgeleitet sind.</summary>')
    a('        public const string QUELLDATEI_SHA256 = "' + sha + '";')
    a('')
    a('        /// <summary>Die Jahressumme jedes Satzes [MWh] — die Normierung des BDEW (1 Mio. kWh).</summary>')
    a('        public const double JAHRESSUMME_MWH = %d.0;' % JAHRESSUMME_MWH)
    a('')
    a('        /// <summary>Die Sätze in der Folge H25, G25, L25.</summary>')
    a('        /// <remarks>Die Verbrauchsprofile, gesät von <see cref="StandardlastprofilSchema"/>; die Netzbezugsprofile P25 und S25 stehen in <see cref="StandardlastprofilSaattabelle.Netzbezug"/>.</remarks>')
    a('        public static IReadOnlyList<StandardlastprofilSaat> Alle { get; } = new[]')
    a('        {')
    saetze_cs(a, verbrauch)
    a('        };')
    a('')
    a('        /// <summary>')
    a('        /// <b>Die Netzbezugsprofile</b> in der Folge P25, S25 — der Bezug eines Haushalts mit PV-Anlage bzw. mit PV-Anlage')
    a('        /// und Batteriespeicher aus dem Netz nach dem Eigenverbrauch (BDEW-Veröffentlichung S. 4–5): <b>kein')
    a('        /// Verbrauchsprofil</b>, die 1.000 MWh/a sind Netzbezug. Gesät von <see cref="StandardlastprofilPvSchema"/>.')
    a('        /// </summary>')
    a('        public static IReadOnlyList<StandardlastprofilSaat> Netzbezug { get; } = new[]')
    a('        {')
    saetze_cs(a, netzbezug)
    a('        };')
    a('    }')
    a('}')
    return '\n'.join(z) + '\n'


# ---------------------------------------------------------------------------------------------
#  Kennzahlen
# ---------------------------------------------------------------------------------------------

def kennzahlen(saetze):
    print('Kennzahlen je Profil (Monatswerte und Jahressumme in MWh, Woche in kWh je Stunde bei 1.000 MWh/a)')
    for s in saetze:
        w = s['woche']
        mittel = math.fsum(w) / 168
        wt, sa, so = math.fsum(w[0:24]), math.fsum(w[120:144]), math.fsum(w[144:168])
        print('\n%s %s (%s, Typ %s)%s%s' % (s['kuerzel'], s['titel'], s['bezeichner'], s['typname'],
                                             ', dynamisiert' if s['dyn'] else '',
                                             ', NETZBEZUG ' + s['netz'] + ' (kein Verbrauchsprofil)' if s['netz'] else ''))
        print('  Jahressumme der Konstanten %.6f; Abrollung vor der Skalierung %.3f MWh' %
              (math.fsum(s['monate']), math.fsum(s['roh'])))
        print('  Monate: ' + ' | '.join('%.3f' % v for v in s['monate']))
        m = s['monate']
        print('  Monatsanteil Januar / Juni / Dezember: %.2f / %.2f / %.2f %%' %
              tuple(100 * m[i] / math.fsum(m) for i in (0, 5, 11)))
        print('  Spitzenfaktor der Woche (Höchstwert / Mittel): %.3f (Höchstwert %.3f, Mittel %.3f, Tiefstwert %.3f)' %
              (max(w) / mittel, max(w), mittel, min(w)))
        print('  Tagessummen Werktag / Samstag / Sonntag: %.1f / %.1f / %.1f kWh, Verhältnis 1 : %.3f : %.3f' %
              (wt, sa, so, sa / wt, so / wt))
        print('  Jahresenergie aus Woche x 365/7: %.3f MWh' % (math.fsum(w) * 365 / 7 / 1000))


# ---------------------------------------------------------------------------------------------

def main(argv):
    pfad = os.path.join(WURZEL, *EXCEL_REL.split('/'))
    modus = 'pruefen'
    args = list(argv)
    while args:
        a = args.pop(0)
        if a == '--excel' and args:
            pfad = os.path.abspath(args.pop(0))
        elif a in ('schreiben', 'kennzahlen'):
            modus = a
        else:
            print('Unbekanntes Argument: ' + a + ' (erlaubt: schreiben, kennzahlen, --excel <pfad>)')
            return 2
    try:
        saetze = ableiten(pfad)
    except (Fehler, zipfile.BadZipFile, KeyError, ET.ParseError) as ex:
        print('FEHLER: ' + str(ex))
        return 2
    with open(pfad, 'rb') as f:
        sha = hashlib.sha256(f.read()).hexdigest()
    if modus == 'kennzahlen':
        kennzahlen(saetze)
        return 0

    text = cs_text(saetze, sha)
    ziel = os.path.join(WURZEL, *ZIEL_REL.split('/'))
    bytes_neu = b'\xef\xbb\xbf' + text.replace('\n', '\r\n').encode('utf-8')
    if modus == 'schreiben':
        with open(ziel, 'wb') as f:
            f.write(bytes_neu)
        print('geschrieben: %s (%d Byte, %d Profile, Excel %s)' % (ZIEL_REL, len(bytes_neu), len(saetze), sha[:12]))
        return 0

    if not os.path.isfile(ziel):
        print('FEHLT: ' + ZIEL_REL + ' - mit "schreiben" erzeugen')
        return 1
    with open(ziel, 'rb') as f:
        alt = f.read()
    if not alt.startswith(b'\xef\xbb\xbf'):
        print('VERALTET: ' + ZIEL_REL + ' ohne BOM')
        return 1
    alt_text = alt[3:].decode('utf-8').replace('\r\n', '\n')
    if alt_text != text:
        for nr, (x, y) in enumerate(zip(alt_text.split('\n'), text.split('\n')), 1):
            if x != y:
                print('VERALTET: %s weicht ab Zeile %d ab\n  Datei:  %s\n  Excel:  %s' % (ZIEL_REL, nr, x[:160], y[:160]))
                break
        else:
            print('VERALTET: ' + ZIEL_REL + ' weicht in der Länge ab')
        return 1
    print('unverändert: %s entspricht der Excel (SHA-256 %s)' % (ZIEL_REL, sha[:12]))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
