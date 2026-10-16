#!/usr/bin/env python3
"""Löst Merge-Konflikte am Dateiende der .resx (Anhänge beider Seiten) auf: beide Seiten behalten,
zuerst die eigene (HEAD), dann ein schließendes </data>, dann die fremde Seite — wenn hinter dem
Konfliktblock das schließende </data> steht. Zeilenendung der Datei bleibt (LF oder CRLF), BOM bleibt.
Aufruf: resx_merge.py <datei> [<datei> …]; Rückgabe 1 bei unerwarteter Struktur oder Dubletten."""
import sys, collections
import xml.etree.ElementTree as ET

rc = 0
for p in sys.argv[1:]:
    raw = open(p, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    crlf = raw.count(b'\r\n') > raw.count(b'\n') // 2
    lines = raw.split(b'\n')
    st = lambda l: l.rstrip(b'\r')
    a = [i for i, l in enumerate(lines) if st(l).startswith(b'<<<<<<< ')]
    m = [i for i, l in enumerate(lines) if st(l) == b'=======']
    b = [i for i, l in enumerate(lines) if st(l).startswith(b'>>>>>>> ')]
    if not a:
        print(p, 'kein Konflikt'); continue
    if not (len(a) == len(m) == len(b)):
        print(p, 'FEHLER: Markerzahl', a, m, b); rc = 1; continue
    # von hinten auflösen, damit die Indizes der vorderen Blöcke gültig bleiben
    for ia, im, ib in reversed(list(zip(a, m, b))):
        if st(lines[ib + 1]).strip() != b'</data>':
            print(p, 'FEHLER: hinter dem Block steht kein </data>, sondern', lines[ib + 1][:60]); rc = 1; break
        schluss = b'  </data>\r' if crlf else b'  </data>'
        lines = lines[:ia] + lines[ia + 1:im] + [schluss] + lines[im + 1:ib] + lines[ib + 1:]
    else:
        out = b'\n'.join(lines)
        try:
            root = ET.fromstring(out[3:] if bom else out)
        except ET.ParseError as e:
            print(p, 'FEHLER: XML', e); rc = 1; continue
        names = [d.get('name') for d in root.findall('data')]
        dup = [n for n, c in collections.Counter(names).items() if c > 1]
        if dup:
            print(p, 'FEHLER: Dubletten', dup); rc = 1; continue
        open(p, 'wb').write(out)
        print(p, 'aufgelöst:', len(a), 'Block/Blöcke,', len(names), 'data,', 'CRLF' if crlf else 'LF')
sys.exit(rc)
