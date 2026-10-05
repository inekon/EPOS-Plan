import sys
# Aufruf: konflikt_union.py <datei> [S:E ...]  — Spannen (1-basiert, inkl.) werden als ours(S..E)+leer+theirs(S..E) ersetzt,
# alle übrigen Konflikt-Hunks als ours+theirs vereinigt. Bytes und Zeilenenden bleiben erhalten.
pfad = sys.argv[1]; spannen = [tuple(int(x) for x in s.split(':')) for s in sys.argv[2:]]
raw = open(pfad, 'rb').read(); zeilen = raw.split(b'\n'); zeilen = [z + b'\n' for z in zeilen[:-1]] + ([zeilen[-1]] if zeilen[-1] else [])
nl = b'\r\n' if zeilen and zeilen[0].endswith(b'\r\n') else b'\n'
def seiten(block):
    ours, theirs, modus = [], [], 0
    for z in block:
        s = z.rstrip(b'\r\n')
        if s.startswith(b'<<<<<<< '): modus = 1; continue
        if s == b'=======' and modus == 1: modus = 2; continue
        if s.startswith(b'>>>>>>> ') and modus == 2: modus = 0; continue
        if modus == 0: ours.append(z); theirs.append(z)
        elif modus == 1: ours.append(z)
        else: theirs.append(z)
    return ours, theirs
aus = []; i = 0; n = len(zeilen)
while i < n:
    nr = i + 1
    sp = next((s for s in spannen if s[0] == nr), None)
    if sp:
        block = zeilen[sp[0]-1:sp[1]]; o, t = seiten(block); aus += o + [nl] + t; i = sp[1]; continue
    if zeilen[i].startswith(b'<<<<<<< '):
        j = i
        while not zeilen[j].startswith(b'>>>>>>> '): j += 1
        o, t = seiten(zeilen[i:j+1]); aus += o + t; i = j + 1; continue
    aus.append(zeilen[i]); i += 1
open(pfad, 'wb').write(b''.join(aus))
print(pfad, 'Marker danach:', sum(1 for z in aus if z.startswith((b'<<<<<<< ', b'>>>>>>> ')) or z.rstrip(b'\r\n') == b'======='))
