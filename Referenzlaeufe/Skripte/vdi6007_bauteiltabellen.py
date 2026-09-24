#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Liest die Bauteiltabellen der zwoelf Testbeispiele aus der LOKALEN PDF-Kopie der VDI 6007 Blatt 1
(Anhang A1, Tabellen A1.1 ... A12.1, "Bauteildaten") und schreibt je Testbeispiel eine CSV-Datei
Testbeispiel<n>.csv in den Zielordner - gedacht fuer Referenzlaeufe/Normzahlen/vdi6007/ (gitignoriert).

NUR LOKAL LAUFFAEHIG. Dieses Skript enthaelt keine Zahl der Richtlinie; alle Werte kommen zur
Laufzeit aus der PDF-Datei und verlassen den Rechner nicht. Die CSV-Dateien werden nie
committet (.gitignore: Referenzlaeufe/Normzahlen/*). Gelesen werden sie von
EPOS.Kern.Tests/BauteilreduktionNormTests.cs (Stufe G3, Normnachweis des Bauteilwegs).

Voraussetzung: pypdf (py -m pip install pypdf). extract_text() liefert die Tabellen lesbar; die
Word-Textfassung der Richtlinie enthaelt sie nicht.

Aufruf (Windows: `py`, sonst `python3`):
    PYTHONIOENCODING=utf-8 py Referenzlaeufe/Skripte/vdi6007_bauteiltabellen.py <blatt1.pdf> <zielordner>

Das CSV-Format (Trennzeichen ';', Dezimalpunkt, UTF-8 ohne BOM, eine Zeile je Schicht, ein
Fenster als eine Zeile ohne Schichtwerte):
    komponente;bauteil;flaeche_m2;schicht;material;dicke_m;lambda_wmk;rho_kgm3;cp_jkgk;
    alpha_kon_a;alpha_kon_i;u_wert;neigung;orientierung;g_dir;g_diff;a_kon;luftschicht
- komponente: laufende Nummer des Bauteils in der Tabelle (dieselbe Kennung kann mehrfach stehen).
- cp_jkgk: in J/(kgK) umgerechnet (die Tabellen fuehren kJ/(kgK)).
- Die Bauteilwerte (alpha_kon_a, alpha_kon_i, u_wert, neigung, orientierung, g_dir, g_diff, a_kon)
  stehen in jeder Zeile des Bauteils; leer = nicht angegeben.
- luftschicht: 1, wenn das Material eine Luftschicht ist (Name beginnt mit "Luft").

Zuordnung der Zusatzspalten einer Schichtzeile (die Textfassung verliert die Spaltenlage):
zwei Zahlen = alpha_kon_a, alpha_kon_i; eine Zahl in Schicht 1 = alpha_kon_i, eine Zahl in einer
spaeteren Schicht = alpha_kon_a; Zahlen in Klammern = Neigung, Orientierung. Eine Fensterzeile
fuehrt alpha_kon_a, alpha_kon_i, U-Wert, (Neigung), (Orientierung), g_dir, g_diff, a_kon.
Fehlt einer Schichtzeile die Rohdichte und steht im Tabellenblock eine einzelne ganze Zahl, ist
das deren verschobener Druck; das Skript setzt sie ein und meldet das (ohne den Wert).

Die Ausgabe nennt je Testbeispiel nur Zahlen von Bauteilen und Schichten, nie einen Tabellenwert.
"""
import os
import re
import sys

KOPF = ['komponente', 'bauteil', 'flaeche_m2', 'schicht', 'material', 'dicke_m', 'lambda_wmk', 'rho_kgm3',
        'cp_jkgk', 'alpha_kon_a', 'alpha_kon_i', 'u_wert', 'neigung', 'orientierung', 'g_dir', 'g_diff',
        'a_kon', 'luftschicht']

KOMMA = re.compile(r'^-?\d+,\d+$')
GANZ = re.compile(r'^\d+$')
KLAMMER = re.compile(r'^\((-?\d+(?:,\d+)?)\)$')
ZEILE = re.compile(r'^(?:(?P<id>[A-Z]{2}\d+)\s+(?P<flaeche>\d+,\d+)\s+)?(?P<nr>\d+)\s*(?P<rest>\D.*)$')


def zahl(text):
    return float(text.replace(',', '.'))


def tabellenblock(seitentext):
    """Die Zeilen zwischen dem Kopf der Bauteiltabelle und der naechsten Tabelle."""
    zeilen = seitentext.splitlines()
    start = next((i for i, z in enumerate(zeilen) if z.startswith('Bauteilkenn')), None)
    if start is None:
        return []
    block = []
    for z in zeilen[start + 1:]:
        if z.startswith('Tabelle A'):
            break
        block.append(z.strip())
    return block


def zerlegen(block, nummer):
    """Zerlegt den Tabellenblock in Bauteile mit Schichten."""
    bauteile, verschoben, fehlend = [], [], []
    aktuell = None
    for z in block:
        if GANZ.match(z):
            verschoben.append(z)
            continue
        m = ZEILE.match(z)
        if not m:
            continue   # Kopfzeilen der Tabelle
        tokens = m.group('rest').split()
        # Materialname: alle Woerter bis zur ersten Dezimalzahl oder Klammer.
        name = []
        while tokens and not KOMMA.match(tokens[0]) and not KLAMMER.match(tokens[0]):
            name.append(tokens.pop(0))
        material = ' '.join(name)
        if m.group('id'):
            aktuell = {'bauteil': m.group('id'), 'flaeche': zahl(m.group('flaeche')), 'schichten': [], 'werte': {}}
            bauteile.append(aktuell)
        if aktuell is None:
            raise SystemExit('Testbeispiel %d: Schichtzeile ohne Bauteil' % nummer)
        klammern = [zahl(KLAMMER.match(t).group(1)) for t in tokens if KLAMMER.match(t)]
        zahlen = [t for t in tokens if not KLAMMER.match(t)]
        werte = aktuell['werte']
        if klammern:
            werte['neigung'] = klammern[0]
            if len(klammern) > 1:
                werte['orientierung'] = klammern[1]
        if material.startswith('Fenster'):
            namen = ['alpha_kon_a', 'alpha_kon_i', 'u_wert', 'g_dir', 'g_diff', 'a_kon']
            for n, t in zip(namen, zahlen):
                werte[n] = zahl(t)
            aktuell['fenster'] = True
            continue
        # Schicht: Dicke, lambda, (rho), c, dann Zusatzspalten.
        if len(zahlen) < 3:
            raise SystemExit('Testbeispiel %d: Schichtzeile unvollstaendig' % nummer)
        dicke, lam = zahl(zahlen[0]), zahl(zahlen[1])
        if GANZ.match(zahlen[2]):
            rho, c, zusatz = zahl(zahlen[2]), zahl(zahlen[3]), zahlen[4:]
        else:
            rho, c, zusatz = None, zahl(zahlen[2]), zahlen[3:]
        schicht = {'nr': int(m.group('nr')), 'material': material, 'dicke': dicke, 'lambda': lam, 'rho': rho,
                   'cp': c * 1000.0, 'luft': 1 if material.startswith('Luft') else 0}
        if rho is None:
            fehlend.append(schicht)
        aktuell['schichten'].append(schicht)
        zusatz = [zahl(t) for t in zusatz]
        if len(zusatz) >= 2:
            werte['alpha_kon_a'], werte['alpha_kon_i'] = zusatz[0], zusatz[1]
        elif len(zusatz) == 1:
            werte['alpha_kon_i' if schicht['nr'] == 1 else 'alpha_kon_a'] = zusatz[0]
    ergaenzt = 0
    for schicht in fehlend:
        if not verschoben:
            raise SystemExit('Testbeispiel %d: Rohdichte fehlt und ist nicht zuzuordnen' % nummer)
        schicht['rho'] = zahl(verschoben.pop(0))
        ergaenzt += 1
    return bauteile, ergaenzt


def schreiben(bauteile, pfad):
    def feld(w):
        return '' if w is None else repr(float(w))
    with open(pfad, 'w', encoding='utf-8', newline='\n') as f:
        f.write(';'.join(KOPF) + '\n')
        for k, b in enumerate(bauteile, start=1):
            w = b['werte']
            gemeinsam = [feld(w.get(n)) for n in ('alpha_kon_a', 'alpha_kon_i', 'u_wert', 'neigung', 'orientierung',
                                                  'g_dir', 'g_diff', 'a_kon')]
            if b.get('fenster'):
                zeilen = [[str(k), b['bauteil'], feld(b['flaeche']), '', 'Fenster', '', '', '', ''] + gemeinsam + ['0']]
            else:
                zeilen = [[str(k), b['bauteil'], feld(b['flaeche']), str(s['nr']), s['material'], feld(s['dicke']),
                           feld(s['lambda']), feld(s['rho']), feld(s['cp'])] + gemeinsam + [str(s['luft'])]
                          for s in b['schichten']]
            for z in zeilen:
                f.write(';'.join(z) + '\n')


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 2
    try:
        import pypdf
    except ImportError:
        print('pypdf fehlt: py -m pip install pypdf')
        return 2
    quelle, ziel = sys.argv[1], sys.argv[2]
    os.makedirs(ziel, exist_ok=True)
    leser = pypdf.PdfReader(quelle)
    gefunden = {}
    for seite in leser.pages:
        text = seite.extract_text() or ''
        m = re.search(r'Testbeispiel (\d+) /', text)
        if not m or 'Bauteilkenn' not in text:
            continue
        nummer = int(m.group(1))
        if nummer in gefunden:
            continue
        bauteile, ergaenzt = zerlegen(tabellenblock(text), nummer)
        if not bauteile:
            continue
        schreiben(bauteile, os.path.join(ziel, 'Testbeispiel%d.csv' % nummer))
        gefunden[nummer] = True
        schichten = sum(len(b['schichten']) for b in bauteile)
        fenster = sum(1 for b in bauteile if b.get('fenster'))
        hinweis = ', %d Rohdichte(n) aus verschobenem Druck ergaenzt' % ergaenzt if ergaenzt else ''
        print('Testbeispiel %2d: %d Bauteile (%d Fenster), %d Schichten%s' % (nummer, len(bauteile), fenster, schichten, hinweis))
    fehlend = [n for n in range(1, 13) if n not in gefunden]
    print('%d von 12 Testbeispielen geschrieben nach %s' % (len(gefunden), ziel))
    if fehlend:
        print('Nicht gefunden: ' + ', '.join(str(n) for n in fehlend))
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
