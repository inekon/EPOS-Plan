#!/usr/bin/env python3
"""Löst Merge-Konflikte in Dokumentation/aktuell/Status_iOS_Migration.md zeilenweise auf.
- Tabellenblöcke (alle Zeilen `| **#n** |`): Vereinigung beider Seiten nach dem Titel (dritte Zelle);
  Zeilen der zweiten Seite, deren Titel fehlt, werden hinten angefügt und ab (höchste Nummer + 1) neu nummeriert.
  Reihenfolge: zuerst die Seite --zuerst (ours|theirs), dann die andere.
- Kopfzeile „Schemaschritt angemeldet“: die Seite --kopf (ours|theirs) gewinnt.
Aufruf: status_merge.py <datei> --zuerst theirs --kopf theirs
Gibt die Nummernzuordnung aus; Rückgabe 1 bei unerwarteter Struktur."""
import sys, re, io, argparse

ap = argparse.ArgumentParser()
ap.add_argument('datei'); ap.add_argument('--zuerst', choices=['ours', 'theirs'], default='theirs')
ap.add_argument('--kopf', choices=['ours', 'theirs'], default='theirs')
a = ap.parse_args()
s = io.open(a.datei, encoding='utf-8', newline='').read()
nl = '\r\n' if '\r\n' in s else '\n'
lines = s.split(nl)
titel = lambda l: (l.split('|')[3].strip() if l.count('|') > 3 else l)
num = lambda l: int(re.match(r'\| \*\*#(\d+)\*\*', l).group(1))
zuordnung = {}
i = 0
while i < len(lines):
    if not lines[i].startswith('<<<<<<< '):
        i += 1; continue
    m = next(j for j in range(i, len(lines)) if lines[j] == '=======')
    b = next(j for j in range(m, len(lines)) if lines[j].startswith('>>>>>>> '))
    ours, theirs = lines[i + 1:m], lines[m + 1:b]
    if all(l.startswith('| **#') for l in ours + theirs):
        erste, zweite = (theirs, ours) if a.zuerst == 'theirs' else (ours, theirs)
        vorhanden = {titel(l) for l in erste}
        start = max(num(l) for l in erste) + 1
        neu = list(erste)
        for l in zweite:
            if titel(l) in vorhanden: continue
            n = num(l); zuordnung[n] = start
            neu.append(l.replace(f'| **#{n}**', f'| **#{start}**', 1)); start += 1
        lines[i:b + 1] = neu
        i += len(neu)
    elif len(ours) == 1 and len(theirs) == 1 and ours[0].startswith('**Schemaschritt angemeldet'):
        lines[i:b + 1] = [theirs[0] if a.kopf == 'theirs' else ours[0]]
        i += 1
    else:
        print('FEHLER: unerwarteter Block ab Zeile', i + 1, ours[:1], theirs[:1]); sys.exit(1)
io.open(a.datei, 'w', encoding='utf-8', newline='').write(nl.join(lines))
print('Zuordnung alt→neu:', zuordnung)
