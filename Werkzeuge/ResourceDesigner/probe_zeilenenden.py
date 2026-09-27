#!/usr/bin/env python3
"""Probe: haelt designer_neu.py gegen BOM und CRLF (Befund: ein LF-Lauf blieb im
Zeichen-Vergleich "unveraendert", waehrend die Datei auf der Platte von CRLF auf LF kippte).

Arbeitet auf der echten Resource.Designer.cs (das Werkzeug kennt keinen anderen Pfad),
sichert ihre Bytes vorher und stellt sie danach byte-genau wieder her - auch wenn die
Probe mitten drin abbricht.

Prueft:
  1. Prüfmodus auf unveraendertem Bestand meldet "unveraendert" (Byte-Vergleich, nicht
     nur Zeichen-Vergleich).
  2. Eine mit LF statt CRLF verfaelschte Kopie wird als "ABWEICHEND" erkannt.
  3. Eine Kopie ohne BOM wird als "ABWEICHEND" erkannt.
  4. "schreiben" auf unveraendertem Bestand laesst die Datei byte-gleich liegen
     (git-sichtbar: kein Diff) und ein zweiter Lauf bestaetigt "unveraendert".

Aufruf:
    python3 Werkzeuge/ResourceDesigner/probe_zeilenenden.py
"""
import os, subprocess, sys

hier = os.path.dirname(os.path.abspath(__file__))
wurzel = os.path.dirname(os.path.dirname(hier))
werkzeug = os.path.join(hier, 'designer_neu.py')
ziel = os.path.join(wurzel, 'EPOS.Kern', 'MyResource', 'Resource.Designer.cs')


def lauf(*argv):
    r = subprocess.run([sys.executable, werkzeug, *argv], cwd=wurzel,
                        capture_output=True, text=True, encoding='utf-8')
    return r.returncode, r.stdout + r.stderr


def pruefe(bezeichnung, bedingung):
    if not bedingung:
        raise SystemExit(f'PROBE FEHLGESCHLAGEN: {bezeichnung}')
    print(f'OK: {bezeichnung}')


original = open(ziel, 'rb').read()
try:
    # 1) Bestand unveraendert -> Pruefmodus meldet "unveraendert"
    code, out = lauf()
    pruefe('Pruefmodus auf Bestand: Exit 0', code == 0)
    pruefe('Pruefmodus auf Bestand: "unveraendert"', 'unveraendert' in out and 'ABWEICHEND' not in out)

    # 2) LF statt CRLF -> ABWEICHEND
    open(ziel, 'wb').write(original.replace(b'\r\n', b'\n'))
    code, out = lauf()
    pruefe('LF-Verfaelschung: als ABWEICHEND erkannt', 'ABWEICHEND' in out)

    # 3) BOM entfernt -> ABWEICHEND
    open(ziel, 'wb').write(original[3:] if original[:3] == b'\xef\xbb\xbf' else original)
    code, out = lauf()
    pruefe('Fehlende BOM: als ABWEICHEND erkannt', 'ABWEICHEND' in out)

    # 4) Bestand wiederhergestellt, schreiben laesst Datei byte-gleich liegen
    open(ziel, 'wb').write(original)
    code, out = lauf('schreiben')
    nachher = open(ziel, 'rb').read()
    pruefe('schreiben auf Bestand: byte-gleich', nachher == original)
    code, out = lauf()
    pruefe('zweiter Pruefmodus-Lauf: "unveraendert"', 'unveraendert' in out and 'ABWEICHEND' not in out)
finally:
    open(ziel, 'wb').write(original)
    wieder = open(ziel, 'rb').read()
    if wieder != original:
        raise SystemExit('Wiederherstellung fehlgeschlagen - Datei per "git checkout" zuruecksetzen!')

print('PROBE OK')
